# Training Management System (TMS) API

![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
![C# 13](https://img.shields.io/badge/C%23-13.0-239120?logo=csharp)
![Entity Framework Core](https://img.shields.io/badge/EF%20Core-10.0-512BD4)
![Scalar API](https://img.shields.io/badge/API%20Docs-Scalar-10B981)
![License](https://img.shields.io/badge/License-MIT-blue)

A modern, high-performance **Training Management System (TMS) RESTful Web API** built with **ASP.NET Core 10** following **Clean Architecture** principles and **CQRS pattern**. 

This system provides comprehensive management for educational institutions and training centers, supporting **Student Profiles, Course Catalogs, Real-time Enrollments, Assessment Grading, Certificate Generation, and Transcript Export Processing**.

---

## 🌟 Key Features

### 🏛️ Architecture & Design Patterns
- **Clean Architecture**: Decoupled multi-project structure (`Api`, `Application`, `Infrastructure`, `Domain`, and `Tests`).
- **CQRS & MediatR Pattern**: Clear separation of command mutations and query operations.
- **Decorator Pattern Caching**: `CachedCourseService` wrapping data access with high-performance `IMemoryCache`.
- **API Versioning (V1 & V2)**: Seamless API evolution supported by custom `V1DeprecationMiddleware` providing standard HTTP `Deprecation` and `Sunset` headers.

### 🔐 Security & Authorization
- **JWT & Cookie Authentication**: Secure cookie handling with XSRF-TOKEN antiforgery protection.
- **Claim-Based Authorization**: Custom policy handlers like `CourseInstructorHandler` for fine-grained resource security.
- **Password Hashing**: Industry-standard password hashing via BCrypt.

### ⚡ Real-Time Features & Background Processing
- **SignalR Real-Time Hubs**: `TmsHub` for live notification dispatches to connected clients.
- **Background Worker Services**: `TranscriptWorker` hosted service for async transcript export jobs.
- **Resilient Upstream Integration**: Circuit-breaker and retry logic simulation for certificate issuance services.

### 📊 API Visibility, Health & Documentation
- **Scalar OpenAPI Documentation**: Modern, interactive API reference available at `/scalar/v1`.
- **Request Tracing Middleware**: `RequestLoggingMiddleware` with custom `X-Correlation-Id` headers.
- **Health Check Endpoints**: `/health/live` (liveness) and `/health/ready` (readiness) probes.
- **Automated Data Seeding**: `DataSeeder` service for instant database population in dev/testing environments.

---

## 🛠️ Technology Stack

- **Framework**: .NET 10 (C# 13)
- **Data Access**: Entity Framework Core 10, LINQ, In-Memory / SQL Server Context
- **Validation**: FluentValidation
- **Real-Time Communication**: ASP.NET Core SignalR
- **Documentation**: Microsoft.AspNetCore.OpenApi & Scalar.AspNetCore
- **Testing**: xUnit, Moq, Microsoft.AspNetCore.Mvc.Testing

---

## 📂 Project Structure

```text
TmsApi/
├── TmsApi.Api/                       # Presentation Layer (Controllers, Middleware, Hubs)
│   ├── Authorization/                # Custom Requirement Handlers
│   ├── Controllers/                  # V1 & V2 API Endpoints
│   ├── Middleware/                   # Logging & Versioning Middleware
│   ├── Hubs/                         # SignalR WebSockets Hub
│   ├── Program.cs                    # Application Pipeline Configuration
│   └── appsettings.json              # System Settings & Database Connection Strings
├── TmsApi.Application/               # Application Layer (Use Cases, CQRS Commands/Queries)
│   ├── Enrollments/                  # Enrollment Commands & Validators
│   └── Services/                     # Core Business Services & Interfaces
├── TmsApi.Infrastructure/            # Infrastructure Layer (Data Persistence & External Services)
│   ├── Persistence/                  # DbContext & DataSeeder
│   ├── Services/                     # Cached Services & Decorators
│   └── Workers/                      # Background Hosted Workers
├── TmsApi.Domain/                    # Domain Layer (Entities, Enums, Interfaces)
│   ├── Entities/                     # Student, Course, Enrollment, Assessment models
│   └── Common/                       # Shared Value Objects & Domain Exceptions
└── TmsApi.Tests/                     # Testing Layer (Unit & Integration Tests)
    ├── AssessmentsApiTests.cs        # End-to-End API Integration Tests
    ├── GetCourseHandlerTests.cs      # CQRS Query Unit Tests
    └── EnrollStudentHandlerTests.cs  # Command Execution Unit Tests
```

---

## 🚀 Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or higher
- IDE: Visual Studio 2022 / VS Code / JetBrains Rider

### Installation & Setup

1. **Clone the Repository**
   ```bash
   git clone https://github.com/YourUsername/TMSAPI.git
   cd TMSAPI
   ```

2. **Restore Dependencies**
   ```bash
   dotnet restore
   ```

3. **Run Database Migrations & Seeding**
   The application automatically seeds sample data on startup via `DataSeeder`.

4. **Run the Application**
   ```bash
   cd TmsApi.Api
   dotnet run
   ```

5. **Access Interactive API Docs**
   Open your browser and navigate to:
   - **Scalar API Reference**: `http://localhost:5000/scalar/v1`
   - **OpenAPI JSON Spec**: `http://localhost:5000/openapi/v1.json`

---

## 🧪 Running Tests

Execute the full suite of unit and integration tests:

```bash
dotnet test
```

---

## 🌐 API Endpoints Overview

| Category | Method | Endpoint | Description |
| :--- | :--- | :--- | :--- |
| **Auth** | `POST` | `/api/auth/login` | Authenticate user and issue JWT / Cookie |
| **Courses** | `GET` | `/api/v1/courses` | List all active courses |
| **Courses** | `GET` | `/api/v2/courses` | Paginated course catalog with caching |
| **Students** | `GET` | `/api/v1/students` | Retrieve student profiles & GPAs |
| **Enrollments**| `POST` | `/api/v1/enrollments` | Enroll student in a course |
| **Assessments**| `GET` | `/api/assessments` | Fetch student grades and course assessments |
| **Certificates**| `POST`| `/api/v2/certificates` | Request course completion certificate |
| **Transcripts** | `POST`| `/api/v2/transcripts` | Trigger background transcript export worker |
| **Health** | `GET` | `/health/live` | Liveness health check probe |
| **Health** | `GET` | `/health/ready` | Readiness health check probe |

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).
