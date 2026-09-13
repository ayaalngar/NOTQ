from datasets import load_from_disk


TRAIN_DIR = "notq_training_pool_encoded_train"
VAL_DIR = "notq_training_pool_encoded_validation"


def main():

    print("Loading train dataset...")
    train_ds = load_from_disk(TRAIN_DIR)

    print("Loading validation dataset...")
    val_ds = load_from_disk(VAL_DIR)

    train_words = set(train_ds["transcription"])
    val_words = set(val_ds["transcription"])

    missing = sorted(val_words - train_words)

    print("\n========== WORD COVERAGE ==========")

    print("Train unique words:", len(train_words))
    print("Validation unique words:", len(val_words))
    print("Validation-only words:", len(missing))

    if not missing:
        print("\nSUCCESS: No validation-only words.")

    else:
        print("\nValidation-only words:")

        for word in missing:

            # Find all occurrences of this word in validation
            rows = [
                (src, spk)
                for w, src, spk in zip(
                    val_ds["transcription"],
                    val_ds["source"],
                    val_ds["speaker_id"]
                )
                if w == word
            ]

            speakers = sorted(set(rows))

            print(
                f"\n{word}"
                f"\n  speakers: {len(speakers)}"
                f"\n  details: {speakers}"
            )


if __name__ == "__main__":
    main()