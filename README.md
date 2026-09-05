# NOTQ Backend — Child Speech Screening Platform MVP

NOTQ is a speech-pronunciation screening application designed for children. This repository contains the ASP.NET Core Web API backend built with **Clean Architecture**, **.NET 10**, **Entity Framework Core**, and **SQL Server**.

---

## 🏛️ Architecture Overview

The backend orchestrates communication between the **Flutter mobile application**, **SQL Server persistence**, and **AI speech inference**:

```text
Flutter App (Client)
      ↓ (HTTP / Multipart REST API)
NOTQ.API (Controllers, Swagger, JWT Auth, Global Exception Middleware)
      ↓
NOTQ.Application (DTOs, Commands, Validations, Orchestration)
      ↓
NOTQ.Domain (Entities, Enums, Value Objects, Screening Rules)
      ↑
NOTQ.Infrastructure (EF Core, SQL Server, Local Audio Storage, BCrypt, Mock/Real AI Service)
      ↓
AI Inference Service (FastAPI / Model Server)
```

### Clean Architecture Layers:
1. **`NOTQ.Domain`**: Pure C# enterprise entities (`User`, `Child`, `PracticeSession`, `PracticeWord`, `AudioAttempt`, `AnalysisResult`, `RefreshToken`), enums, and domain logic. Zero external package dependencies.
2. **`NOTQ.Application`**: Application contracts, business service interfaces (`IAuthService`, `IChildService`, `ISessionService`, `IAttemptService`, `ISpeechAnalysisService`, etc.), FluentValidation validators, and DTOs.
3. **`NOTQ.Infrastructure`**: EF Core database context, migrations, seed data, JWT token generation, BCrypt password hashing, local file storage for audio recordings, scoring engine, pattern detection, and both `MockSpeechAnalysisService` and `AiSpeechAnalysisService`.
4. **`NOTQ.API`**: REST controllers (`/api/v1`), Swagger documentation with Bearer authentication, and global exception handling with structured error envelopes.
5. **`NOTQ.Tests`**: Unit and integration test suite (xUnit, FluentAssertions, Moq, EF InMemory).

---

## 🚀 Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server LocalDB (`MSSQLLocalDB`) or SQL Server

### 1. Database Setup & Migrations
The database schema and Arabic practice words are pre-configured with EF Core migrations.
To apply migrations:
```bash
dotnet ef database update --project NOTQ.Infrastructure --startup-project NOTQ.API
```
*(Note: In Development mode, the API automatically verifies and applies pending migrations on startup!)*

### 2. Running the API
```bash
dotnet run --project NOTQ.API --launch-profile http
```
The server will start listening on:
- API Base: `http://localhost:5088/api/v1`
- Swagger UI: `http://localhost:5088/swagger`

---

## 🤝 Parallel Team Workflow

The backend is built around strict contracts and abstractions so **Flutter, Backend, and AI teams work without blocking each other**:

```text
                  SHARED CONTRACT
                        │
          ┌─────────────┼─────────────┐
          │             │             │
          ▼             ▼             ▼
       Flutter       Backend          AI
          │             │             │
          │             ▼             │
          │     MockSpeechAnalysis    │
          │             │             │
          └─────────────┼─────────────┘
                        │
                  Integration
                        │
                        ▼
                AiSpeechAnalysis
```

### 📱 For Flutter Team:
1. You can immediately run the backend and interact via Swagger or directly from Flutter.
2. The default configuration uses `MockSpeechAnalysisService`, which provides realistic pronunciation analysis for Arabic words (e.g. "سمكة", "سيارة", "شمس") with substitution issues and friendly Arabic feedback.
3. Audio recordings can be uploaded directly via `POST /api/v1/sessions/{sessionId}/attempts` with `multipart/form-data`.

### 🧠 For AI Team:
1. Train, fine-tune, and expose your Python FastAPI inference server independently.
2. Your inference server only needs to expose:
   ```http
   POST /predict
   Content-Type: multipart/form-data

   Fields:
   - audio: WAV audio stream
   - expectedWord: string (e.g. "سمكة")
   ```
   Returning:
   ```json
   {
     "prediction": "Incorrect",
     "confidence": 0.87,
     "issueType": "Substitution",
     "detectedWord": "تمكة"
   }
   ```
3. When ready, switch `AiService:UseMock` in `NOTQ.API/appsettings.json` to `false` and set `BaseUrl` to your service URL (`http://localhost:8000`). No Flutter changes required!

---

## 📋 API Contract Summary

### 1. Authentication (`/api/v1/auth`)
- `POST /api/v1/auth/register` — Register a parent account.
- `POST /api/v1/auth/login` — Login and receive Access Token & Refresh Token.
- `POST /api/v1/auth/refresh-token` — Refresh expired access token.
- `GET /api/v1/auth/me` *(Auth)* — Get current parent profile.

### 2. Children Management (`/api/v1/children`)
- `POST /api/v1/children` *(Auth)* — Register a child for the authenticated parent.
- `GET /api/v1/children` *(Auth)* — Retrieve all children for the parent.
- `GET /api/v1/children/{id}` *(Auth)* — Retrieve specific child details (strictly ownership-verified).
- `PUT /api/v1/children/{id}` *(Auth)* — Update child details.
- `DELETE /api/v1/children/{id}` *(Auth)* — Delete child profile.

### 3. Practice Words (`/api/v1/words`)
- `GET /api/v1/words` — List all practice words (filters: `difficulty`, `targetSound`).
- `GET /api/v1/words/{id}` — Get single practice word.

### 4. Practice Sessions (`/api/v1/sessions`)
- `POST /api/v1/sessions` *(Auth)* — Start a new practice session for a child (`{ "childId": "..." }`).
- `GET /api/v1/sessions/{id}` *(Auth)* — Get session details.
- `POST /api/v1/sessions/{id}/complete` *(Auth)* — Complete session, calculate score and attempts summary.
- `GET /api/v1/sessions/child/{childId}` *(Auth)* — Get session history for a child.

### 5. Audio Attempt Upload (`/api/v1/sessions/{sessionId}/attempts`)
- `POST /api/v1/sessions/{sessionId}/attempts` *(Auth)*
  - `Content-Type: multipart/form-data`
  - Form Fields:
    - `audio`: Audio file (`.wav`, `.m4a`, `.mp3`, etc.)
    - `wordId`: Integer ID of the practiced word
  - Sample Response:
    ```json
    {
      "success": true,
      "data": {
        "attemptId": "caf853e0-6598-49f0-842e-cac40499edc0",
        "wordId": 1,
        "word": "سمكة",
        "audioUrl": "/uploads/audio/2026/09/2031a92a0fe848bf928294659199d3fb.wav",
        "prediction": "Incorrect",
        "confidence": 0.87,
        "issueType": "Substitution",
        "detectedWord": "تمكة",
        "feedback": {
          "type": "Retry",
          "message": "حاول تاني!"
        },
        "createdAt": "2026-09-03T15:58:51.31Z"
      }
    }
    ```

### 6. Progress & Reports
- `GET /api/v1/children/{childId}/progress` *(Auth)* — Aggregated statistics, average score, and trend (`Improving`, `Stable`, `Declining`, `InsufficientData`).
- `GET /api/v1/children/{childId}/report` *(Auth)* — Full child screening report with pattern detection and guidance.
- `GET /api/v1/sessions/{sessionId}/report` *(Auth)* — Session-specific report.

---

## 🛡️ Screening Language Compliance

NOTQ is strictly an **early-awareness and screening tool**, not a medical diagnostic system. All reports, pattern observations, and guidance strictly adhere to non-diagnostic screening language:
- ✅ *"Repeated pronunciation pattern detected on target sound /س/."*
- ✅ *"Some pronunciation inconsistencies were observed."*
- ✅ *"Professional evaluation by a certified speech-language specialist may be recommended."*
- ❌ **NEVER**: *"The child has a speech disorder"* or *"The child is diagnosed with..."*

---

## 🧪 Automated Testing

Execute the automated test suite covering authentication, authorization, scoring, pattern detection, audio attempts, and AI failure resilience:
```bash
dotnet test NOTQ.slnx
```
**Test Results:** 21 passed, 0 failed.
