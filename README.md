# HotelListing Web API

[![Build and Deploy to Azure](https://github.com/OrkoD/HotelListing.API/actions/workflows/main_hotellisting-api-orest.yml/badge.svg)](https://github.com/OrkoD/HotelListing.API/actions/workflows/main_hotellisting-api-orest.yml)
![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
![C#](https://img.shields.io/badge/C%23-13.0-239120?logo=csharp)
![Azure](https://img.shields.io/badge/Azure-App%20Service%20%26%20SQL-0078D4?logo=microsoftazure)
![Swagger](https://img.shields.io/badge/Documentation-Swagger%20%2F%20OpenAPI-85EA2D?logo=swagger)

A production-ready, enterprise-grade RESTful API for managing countries, hotels, and guest bookings built with **ASP.NET Core (.NET 10)**, **Entity Framework Core**, and **Microsoft Azure**.

---

## 🌐 Live Cloud Deployment

- **Interactive Swagger UI:** [https://hotellisting-api-orest-hug6ghf8bsa8chgj.polandcentral-01.azurewebsites.net/swagger/index.html](https://hotellisting-api-orest-hug6ghf8bsa8chgj.polandcentral-01.azurewebsites.net/swagger/index.html)
- **Health Checks Endpoint:** [https://hotellisting-api-orest-hug6ghf8bsa8chgj.polandcentral-01.azurewebsites.net/health](https://hotellisting-api-orest-hug6ghf8bsa8chgj.polandcentral-01.azurewebsites.net/health)

---

## 🏛 Clean Architecture

The solution follows Clean Architecture principles, ensuring loose coupling and separation of concerns across distinct layers:

```
HotelListing.API/
├── HotelListing.Api/            # Presentation Layer: Controllers, Middleware, Swagger, Program.cs
├── HotelListing.Api.Application/ # Application Layer: Business Logic, Services, DTOs, AutoMapper Profiles
├── HotelListing.Api.Domain/      # Domain Layer: Data Context, Domain Entities, Identity Models
├── HotelListing.Api.Common/      # Cross-Cutting: Constants, Shared Models, Utilities
└── .github/workflows/           # CI/CD: Automated GitHub Actions pipeline to Azure
```

---

## ✨ Key Features & Technical Highlights

### 1. Authentication & Authorization

- **ASP.NET Core Identity:** Full user management with seeded roles (`User`, `Administrator`).
- **JWT Bearer Authentication:** Secure token-based authentication with expiration, claims, and issuer/audience validation.
- **Custom API Key Validation:** Header-based API key authentication for machine-to-machine integrations.
- **Basic Authentication:** Alternative endpoint authorization mechanism.

### 2. API Versioning & Documentation

- **URL Segment Versioning:** Support for `/api/v1/...` and `/api/v2/...` routes using `Asp.Versioning.Mvc`.
- **Interactive Swagger / OpenAPI:** Versioned API specification documents with XML docstrings, custom schemas, and JWT Bearer authorization filters.

### 3. Resilience & Performance

- **Output Caching:** Custom authenticated user caching policy using ASP.NET Core Output Cache to optimize response latency.
- **Rate Limiting:** Fixed-window rate limiting policies to prevent DDoS and API abuse.
- **EF Core Transient Fault Handling:** Exponential backoff retry strategies on database connections (`EnableRetryOnFailure`).
- **DbContext Pooling:** High-throughput connection pooling via `AddDbContextPool`.
- **Global Error Handling:** RFC 7807 compliant `ProblemDetails` via a centralized `IExceptionHandler`.

### 4. Observability & Monitoring

- **Structured Logging:** Serilog integration streaming structured log events to Console, File, and Seq.
- **Health Checks:** Built-in `/health` endpoint reporting both application host and Azure SQL connectivity.

### 5. Automated CI/CD

- **GitHub Actions Workflow:** Automatically triggers on every push to `main`:
  - Compiles with the latest .NET 10 SDK on Ubuntu.
  - Publishes a self-contained release artifact.
  - Deploys securely to **Azure Linux App Service** via OneDeploy.

---

## 🛣 API Endpoints Overview

| Method | Endpoint                 | Description                               | Auth Required                          |
| :----- | :----------------------- | :---------------------------------------- | :------------------------------------- |
| `POST` | `/api/Auth/register`     | Register a new user account               | Anonymous                              |
| `POST` | `/api/Auth/login`        | Authenticate user and receive JWT token   | Anonymous                              |
| `GET`  | `/api/v1/Countries`      | List countries with filtering and sorting | Anonymous                              |
| `GET`  | `/api/v1/Countries/{id}` | Get country details and associated hotels | Anonymous                              |
| `POST` | `/api/v1/Countries`      | Create a new country record               | `[Authorize(Roles = "Administrator")]` |
| `GET`  | `/api/v2/Countries`      | Version 2 listing with enhanced metadata  | Anonymous                              |
| `GET`  | `/api/Hotels`            | List all hotels with paging parameters    | Anonymous                              |
| `POST` | `/api/Hotels`            | Create a new hotel listing                | `[Authorize]`                          |
| `GET`  | `/api/ApiKey`            | Verify access using an API key            | `X-Api-Key` Header                     |
| `GET`  | `/health`                | Health status of API and database         | Anonymous                              |

---

## 🛠 Local Development Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/) (for local SQL Server and Seq)

### 1. Clone the Repository

```bash
git clone https://github.com/OrkoD/HotelListing.API.git
cd HotelListing.API
```

### 2. Start Local Dependencies

Run Microsoft SQL Server in Docker:

```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=P@ssword123!" -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:2022-latest
```

### 3. Apply Migrations

Apply the EF Core migrations to populate the local database:

```bash
dotnet ef database update --project HotelListing.Api.Domain --startup-project HotelListing.Api
```

### 4. Run the API

```bash
dotnet run --project HotelListing.Api
```

Visit Swagger at: `https://localhost:7143/swagger` (or port shown in console).

---

## 📄 License

This project is licensed under the MIT License.
