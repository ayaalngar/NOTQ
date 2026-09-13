# NOTQ (نُطق) — AI-Powered Child Speech Screening Platform

<p align="center">
  <strong>منصة ذكية للفحص المبكر وملاحظة نطق الأطفال باللغة العربية</strong><br>
  <em>An interactive, non-diagnostic speech screening ecosystem bridging Flutter Mobile, .NET 10 Clean Architecture, and AI Speech Inference.</em>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet" alt=".NET 10" />
  <img src="https://img.shields.io/badge/Flutter-Cross--Platform-02569B?style=flat-square&logo=flutter" alt="Flutter" />
  <img src="https://img.shields.io/badge/Python-FastAPI%20%2F%20AI-3776AB?style=flat-square&logo=python" alt="Python FastAPI" />
  <img src="https://img.shields.io/badge/EF%20Core-SQL%20Server-00758F?style=flat-square" alt="SQL Server" />
  <img src="https://img.shields.io/badge/License-MIT-green.svg?style=flat-square" alt="License: MIT" />
</p>

---

## 🌟 Overview | نبذة عن المشروع

**NOTQ (نُطق)** is an innovative digital screening platform designed to assess Arabic speech and pronunciation in children through engaging, child-friendly exercises. By combining automated acoustic analysis with structured clinical screening rules, NOTQ helps parents, educators, and speech therapists identify potential phonological patterns early—fostering timely, supportive guidance.

> [!IMPORTANT]
> **Medical & Non-Diagnostic Compliance:**  
> NOTQ is strictly an **early-awareness and screening tool**, not a medical diagnostic device. It highlights pronunciation patterns (e.g., sound substitutions or omissions) and provides observational screening summaries. It does not provide medical diagnoses or replace certified Speech-Language Pathologists (SLPs).

---

## 🏛️ System Architecture | هيكلية النظام

The NOTQ platform operates as a coordinated monorepo ecosystem with clear boundaries between the client, application services, and intelligent inference:

```text
               ┌───────────────────────────────┐
               │     Flutter Mobile App        │
               │   (Child & Parent Client)     │
               └───────────────┬───────────────┘
                               │  REST API / Multipart Audio
                               ▼
               ┌───────────────────────────────┐
               │    ASP.NET Core Web API       │
               │   (.NET 10 Clean Architecture)│
               │                               │
               │  NOTQ.API      NOTQ.App       │
               │  NOTQ.Domain   NOTQ.Infra     │
               └───────┬───────────────┬───────┘
                       │               │
       Persist / Query │               │ Inference Request
                       ▼               ▼
          ┌────────────────┐     ┌───────────────────────┐
          │   SQL Server   │     │  AI Inference Service │
          │   Persistence  │     │  (FastAPI / PyTorch)  │
          └────────────────┘     └───────────────────────┘
```

### Data Flow & Parallel Contract
1. **Child Practice:** The child listens to and pronounces target Arabic words through gamified cards in the Flutter app.
2. **Audio Upload:** High-fidelity audio recordings are uploaded to the .NET Web API.
3. **Speech Analysis:** The backend coordinates with the AI Speech Inference Service (or built-in `MockSpeechAnalysisService` during isolated development) to evaluate accuracy and identify issue types (e.g., Substitution on `/س/`).
4. **Scoring & Insights:** Results are stored, evaluated by the pattern detection engine, and visualized in the parent/specialist screening dashboard.

---

## 📂 Repository Structure | هيكل المستودع

The repository is organized into four primary functional directories:

```text
NOTQ/
│
├── Backend/                 # ASP.NET Core 10 Web API (Clean Architecture)
│   ├── NOTQ.API/            # API Controllers, Middleware, Auth & Swagger
│   ├── NOTQ.Application/    # Use Cases, DTOs, Business Logic & Contracts
│   ├── NOTQ.Domain/         # Core Entities, Enums & Domain Rules
│   ├── NOTQ.Infrastructure/ # EF Core, Audio Storage, Security & AI Client
│   ├── NOTQ.Tests/          # Unit & Integration Test Suite (xUnit)
│   └── NOTQ.slnx            # Solution File
│
├── AI/                      # Speech Recognition & Acoustic Analysis Engine
│   └── (Inference server, audio preprocessing, model checkpoints)
│
├── Mobile/                  # Cross-Platform Mobile Client (Flutter)
│   └── (Child gamified UI, audio recording, parent dashboard)
│
├── Documentation/           # Architecture Specs, Schemas, & Meeting Notes
│   └── (System design, API specs, SRS, wireframes)
│
├── .gitignore               # Multi-stack Git ignore rules (.NET, Flutter, Python)
├── README.md                # Project master documentation
└── LICENSE                  # MIT License
```

---

## 🧩 Core Components | مكونات المشروع

### 1. ⚙️ Backend (`Backend/`)
Built with **.NET 10** following **Clean Architecture** principles:
- **Authentication & Security:** JWT tokens, refresh token rotation, and BCrypt password hashing.
- **Child & Parent Management:** Ownership-enforced profiles and session isolation.
- **Word Catalog:** Curated Arabic practice words categorized by difficulty, syllables, and target consonant sounds.
- **Scoring & Pattern Detection:** Deterministic rules to flag repeated phonological patterns (e.g., substituting `/س/` with `/ت/`).
- **Resilience & Testing:** Pluggable AI service interface (`MockSpeechAnalysisService` vs `AiSpeechAnalysisService`) with 100% passing xUnit test coverage.

👉 *See [Backend/README.md](file:///C:/Users/mos18/source/repos/NOTQ/Backend/README.md) for full API endpoint documentation and database migration steps.*

---

### 2. 📱 Mobile Application (`Mobile/`)
Built with **Flutter** for Android and iOS:
- **Child Experience:** Colorful, voice-guided interactive cards with playful animations and real-time positive feedback.
- **Audio Capture:** High-quality local recording with waveform visualization and upload retry mechanisms.
- **Parent Portal:** Detailed visual progress charts, history tracking, and downloadable screening summaries.

---

### 3. 🧠 AI Inference Service (`AI/`)
Python-based microservice powered by **FastAPI**:
- Evaluates children's spoken Arabic utterances against expected target phonemes.
- Classifies pronunciation outcomes: `Correct`, `Incorrect`, or `Unclear`.
- Detects error categories: `Substitution` (إبدال), `Omission` (حذف), or `Distortion` (تشويه).

---

### 4. 📄 Documentation (`Documentation/`)
Central hub for team documentation:
- Software Requirements Specification (SRS).
- Database Entity-Relationship Diagrams (ERD).
- Screening vocabulary and linguistic phoneme distribution sheets.
- UI/UX wireframes and workflow diagrams.

---

## 🚀 Getting Started | كيفية البدء والتطوير

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [SQL Server](https://www.microsoft.com/sql-server) or LocalDB
- [Flutter SDK](https://docs.flutter.dev/get-started/install) (for mobile)
- [Python 3.10+](https://www.python.org/) (for AI service)

---

### 1. Setting Up the Backend
```bash
# Navigate to the Backend folder
cd Backend

# Apply Database Migrations (Seeds initial Arabic words automatically)
dotnet ef database update --project NOTQ.Infrastructure --startup-project NOTQ.API

# Run the API Server
dotnet run --project NOTQ.API --launch-profile http
```
- **API Base URL:** `http://localhost:5088/api/v1`
- **Swagger Documentation:** `http://localhost:5088/swagger`

To run automated tests:
```bash
dotnet test NOTQ.slnx
```

---

### 2. Setting Up the Mobile App
```bash
# Navigate to the Mobile folder
cd Mobile

# Install Flutter dependencies
flutter pub get

# Run on emulator or connected device
flutter run
```

---

### 3. Setting Up the AI Inference Service
```bash
# Navigate to the AI folder
cd AI

# Create and activate virtual environment
python -m venv venv
# Windows:
venv\Scripts\activate
# Linux/macOS:
source venv/bin/activate

# Install dependencies and start service
pip install -r requirements.txt
uvicorn main:app --reload --port 8000
```

---

## 🛡️ Screening Language Standards | معايير لغة الفحص

To guarantee ethical alignment and prevent misleading medical implications, all system outputs strictly adhere to non-diagnostic screening language:

| ✅ Compliant Screening Language | ❌ Prohibited Diagnostic Language |
|:---|:---|
| *"Repeated sound substitution detected on target sound /س/."* | *"The child has a speech disorder."* |
| *"Pronunciation inconsistency observed during practice."* | *"Child diagnosed with Dyslalia."* |
| *"A consultation with a certified speech specialist is recommended."* | *"Prescribed treatment plan..."* |

---

## 🤝 Contribution Guidelines | المساهمة والتطوير

1. Fork or branch from `main`.
2. Create a feature branch: `git checkout -b feature/amazing-feature`.
3. Commit your changes following conventional commits: `git commit -m "feat(backend): add child audio report endpoint"`.
4. Ensure all tests pass: `dotnet test Backend/NOTQ.slnx`.
5. Open a Pull Request for code review.

---

## 📄 License | الترخيص

This project is licensed under the **MIT License** — see the [LICENSE](file:///C:/Users/mos18/source/repos/NOTQ/LICENSE) file for details.
