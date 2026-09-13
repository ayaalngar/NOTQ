"""
Notq — vocab / tokenizer / dataset-encoding pipeline
=====================================================
Input : a HuggingFace `datasets` folder saved via save_to_disk(), with
        columns {"transcription": str, "audio": Audio(16kHz)}.
        (This is the "notq_training_pool" = Abjad + ASMDD, correct-only.)

Output: - vocab.json                  (char-level CTC vocab)
        - notq_processor/             (Wav2Vec2CTCTokenizer + FeatureExtractor, saved as one Processor)
        - notq_training_pool_encoded/ (dataset with input_values + labels, ready for Trainer)
        - dropped_rows_log.json       (rows dropped for missing transcription, and why)

Notes
-----
- No torch/model needed for this step — only tokenizer + feature extractor.
- We tag each row with `source` (abjad/asmdd) and `speaker_id`, extracted
  from filename patterns, so you can do a SPEAKER-LEVEL train/test split
  later instead of a random split (a child's voice/room otherwise leaks
  across train and test). See the technical review discussion on this.
- 532 rows had transcription == None (mostly un-joined ASMDD numeric-ID
  files, e.g. "51-2.wav", "72-N.wav"). They're dropped here and logged.
  If you have ASMDD's ID -> word lookup table, re-join those instead of
  dropping them — you'd recover ~530 extra training examples.
"""

import io
import json
import re
#from collections import Counter
import random
from collections import Counter, defaultdict
import numpy as np

import soundfile as sf
from datasets import Audio, load_from_disk
from transformers import (
    Wav2Vec2CTCTokenizer,
    Wav2Vec2FeatureExtractor,
    Wav2Vec2Processor,
)

INPUT_DIR = "notq_training_pool"
VOCAB_PATH = "vocab.json"
PROCESSOR_DIR = "notq_processor"
ENCODED_DIR = "notq_training_pool_encoded"
DROPPED_LOG = "dropped_rows_log.json"

NUMERIC_PAT = re.compile(r"^\d+(-\d+)?\.wav$")            # ASMDD-style: "01.wav", "51-2.wav"
UUID_PAT = re.compile(
    r"^(.*?)_([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})(_\d+)?\.wav$"
)  # Abjad-style: "Alam_adam khatab_<uuid>.wav"


def load_and_clean(path):
    ds = load_from_disk(path)
    ds = ds.cast_column("audio", Audio(decode=False))  # decode manually via soundfile (no torchcodec dependency)

    transcriptions = ds["transcription"]
    paths = ds["audio"]
    none_rows = [(i, paths[i]["path"]) for i, t in enumerate(transcriptions) if t is None]

    with open(DROPPED_LOG, "w", encoding="utf-8") as f:
        json.dump(
            {"count": len(none_rows), "filenames": [p for _, p in none_rows]},
            f, ensure_ascii=False, indent=2,
        )
    print(f"Dropping {len(none_rows)} rows with missing transcription -> see {DROPPED_LOG}")

    keep_idx = [i for i, t in enumerate(transcriptions) if t is not None]
    return ds.select(keep_idx)


def build_vocab(ds_clean):
    chars = set()
    for t in ds_clean["transcription"]:
        chars.update(list(t))
    chars = sorted(chars)
    vocab_dict = {c: i for i, c in enumerate(chars)}

    if " " in vocab_dict:
        vocab_dict["|"] = vocab_dict.pop(" ")
    else:
        vocab_dict["|"] = len(vocab_dict)
    vocab_dict["[UNK]"] = len(vocab_dict)
    vocab_dict["[PAD]"] = len(vocab_dict)

    with open(VOCAB_PATH, "w", encoding="utf-8") as f:
        json.dump(vocab_dict, f, ensure_ascii=False)
    print(f"vocab size: {len(vocab_dict)} -> saved to {VOCAB_PATH}")
    return vocab_dict


def build_processor():
    tokenizer = Wav2Vec2CTCTokenizer(
        VOCAB_PATH, unk_token="[UNK]", pad_token="[PAD]", word_delimiter_token="|",
    )
    feature_extractor = Wav2Vec2FeatureExtractor(
        feature_size=1, sampling_rate=16000, padding_value=0.0,
        do_normalize=True, return_attention_mask=True,
    )
    processor = Wav2Vec2Processor(feature_extractor=feature_extractor, tokenizer=tokenizer)
    processor.save_pretrained(PROCESSOR_DIR)
    return processor


def tag_metadata(example):
    path = example["audio"]["path"] or ""
    m = UUID_PAT.match(path)
    if m:
        example["source"] = "abjad"
        parts = m.group(1).split("_")
        example["speaker_id"] = parts[-1] if len(parts) >= 2 else "unknown"
    elif NUMERIC_PAT.match(path):
        example["source"] = "asmdd"
        example["speaker_id"] = path.split("-")[0].split(".")[0]  # child/folder index
    else:
        example["source"] = "unknown"
        example["speaker_id"] = "unknown"
    return example


def encode_batch(batch, processor):
    arrays = [sf.read(io.BytesIO(a["bytes"]))[0].astype(np.float32) for a in batch["audio"]]
    inputs = processor.feature_extractor(arrays, sampling_rate=16000)
    batch["input_values"] = inputs.input_values
    batch["labels"] = processor.tokenizer(batch["transcription"]).input_ids
    return batch


SPLIT_MANIFEST = "speaker_split_manifest.json"


def speaker_level_split(ds_encoded, val_fraction=0.15, seed=42):
    """
    Speaker-level split with word-coverage awareness.

    - Same speaker never appears in both train and validation.
    - Tries to minimize words that appear only in validation.
    - Keeps approximately val_fraction of speakers for validation.
    """

    from collections import defaultdict
    import random

    # ---------------------------------------------------------
    # 1. Collect words spoken by each speaker
    # ---------------------------------------------------------
    speakers_by_source = defaultdict(set)
    words_by_speaker = defaultdict(set)
    total_word_counts = Counter()

    for src, spk, word in zip(
        ds_encoded["source"],
        ds_encoded["speaker_id"],
        ds_encoded["transcription"]
    ):
        key = (src, spk)

        speakers_by_source[src].add(spk)
        words_by_speaker[key].add(word)
        total_word_counts[word] += 1

    rng = random.Random(seed)

    val_speakers = {}

    # ---------------------------------------------------------
    # 2. Select validation speakers while preserving
    #    as much word coverage as possible in training
    # ---------------------------------------------------------
    for src, speakers in speakers_by_source.items():

        speakers = sorted(speakers)

        n_val = max(1, round(len(speakers) * val_fraction))

        # Random initial candidate
        rng.shuffle(speakers)

        selected_val = []

        for _ in range(n_val):

            best_speaker = None
            best_score = None

            remaining_candidates = [
                s for s in speakers
                if s not in selected_val
            ]

            for spk in remaining_candidates:

                candidate_words = words_by_speaker[(src, spk)]

                # Count how many words would become
                # validation-only if this speaker is selected
                validation_only_risk = 0

                for word in candidate_words:

                    # How many total examples of this word?
                    total_count = total_word_counts[word]

                    # If this word has very few examples,
                    # putting its speaker in validation is risky.
                    if total_count <= 3:
                        validation_only_risk += 10
                    elif total_count <= 5:
                        validation_only_risk += 3

                # Prefer speakers whose removal causes
                # less risk to train vocabulary coverage.
                score = validation_only_risk

                # Small random component to avoid always
                # choosing exactly the same speaker
                score += rng.random() * 0.01

                if best_score is None or score < best_score:
                    best_score = score
                    best_speaker = spk

            selected_val.append(best_speaker)

        val_speakers[src] = set(selected_val)

        print(
            f"source={src}: "
            f"{len(speakers)} speakers, "
            f"{len(selected_val)} held out for validation"
        )

    # ---------------------------------------------------------
    # 3. Create split column
    # ---------------------------------------------------------
    split_col = [
        "validation"
        if spk in val_speakers.get(src, set())
        else "train"
        for src, spk in zip(
            ds_encoded["source"],
            ds_encoded["speaker_id"]
        )
    ]

    ds_encoded = ds_encoded.add_column("split", split_col)

    train_ds = ds_encoded.filter(
        lambda x: x["split"] == "train"
    )

    val_ds = ds_encoded.filter(
        lambda x: x["split"] == "validation"
    )

    # ---------------------------------------------------------
    # 4. Statistics
    # ---------------------------------------------------------
    print("\n========== SPLIT RESULTS ==========")

    print("train rows:", len(train_ds))
    print("validation rows:", len(val_ds))

    print("\ntrain by source:")
    print(Counter(train_ds["source"]))

    print("\nvalidation by source:")
    print(Counter(val_ds["source"]))

    # ---------------------------------------------------------
    # 5. Check word coverage
    # ---------------------------------------------------------
    train_words = set(train_ds["transcription"])
    val_words = set(val_ds["transcription"])

    missing = sorted(val_words - train_words)

    print("\n========== WORD COVERAGE ==========")
    print("train unique words:", len(train_words))
    print("validation unique words:", len(val_words))
    print("validation-only words:", len(missing))

    if missing:
        print(
            "\nWARNING:",
            len(missing),
            "words appear ONLY in validation:"
        )

        print(missing)

    else:
        print(
            "SUCCESS: Every validation word "
            "also exists in training."
        )

    # ---------------------------------------------------------
    # 6. Save reproducible manifest
    # ---------------------------------------------------------
    with open(
        SPLIT_MANIFEST,
        "w",
        encoding="utf-8"
    ) as f:

        json.dump(
            {
                "seed": seed,
                "val_fraction_speakers": val_fraction,
                "val_speakers": {
                    k: sorted(v)
                    for k, v in val_speakers.items()
                },
                "note": (
                    "Speaker-level split with "
                    "word-coverage-aware speaker selection."
                ),
            },
            f,
            ensure_ascii=False,
            indent=2,
        )

    print("\nsaved ->", SPLIT_MANIFEST)

    return train_ds, val_ds


def main():
    ds_clean = load_and_clean(INPUT_DIR)
    print("rows after cleaning:", len(ds_clean))

    build_vocab(ds_clean)
    processor = build_processor()

    ds_clean = ds_clean.map(tag_metadata, desc="tagging source/speaker")
    print("source counts:", Counter(ds_clean["source"]))

    ds_encoded = ds_clean.map(
        lambda b: encode_batch(b, processor),
        batched=True, batch_size=32, remove_columns=["audio"],
        desc="encoding audio+text",
    )
    ds_encoded.save_to_disk(ENCODED_DIR)
    print("saved encoded dataset ->", ENCODED_DIR)
    print(ds_encoded)

    # --- next step: speaker-level split (no child in both train & val) ---
    train_ds, val_ds = speaker_level_split(ds_encoded)
    train_ds.save_to_disk(ENCODED_DIR + "_train")
    val_ds.save_to_disk(ENCODED_DIR + "_validation")


if __name__ == "__main__":
    main()
