# 🏗️ Enterprise Web API — Clean Architecture & CQRS

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-12.0-239120?style=for-the-badge&logo=c-sharp&logoColor=white)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![EF Core](https://img.shields.io/badge/EF%20Core-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://docs.microsoft.com/en-us/ef/core/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-2022-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/sql-server)
[![MediatR](https://img.shields.io/badge/MediatR-12.x-blue?style=for-the-badge)](https://github.com/jbogard/MediatR)
[![FluentValidation](https://img.shields.io/badge/FluentValidation-11.x-B71C1C?style=for-the-badge)](https://docs.fluentvalidation.net/)
[![xUnit](https://img.shields.io/badge/xUnit-Tests-brightgreen?style=for-the-badge&logo=xunit)](https://xunit.net/)
[![Docker](https://img.shields.io/badge/Docker-Supported-2496ED?style=for-the-badge&logo=docker&logoColor=white)](https://www.docker.com/)
[![Swagger](https://img.shields.io/badge/Swagger-OpenAPI-85EA2D?style=for-the-badge&logo=swagger&logoColor=black)](https://swagger.io/)

> Designed using enterprise development practices including **Clean Architecture**, **CQRS**, **Dependency Injection**, **Structured Logging**, **Input Validation**, **Docker**, and **Unit Testing**. This project serves as a production-ready template for building scalable, maintainable, and testable ASP.NET Core applications.

---

## ✔ Features

| Feature | Status |
| :--- | :---: |
| Clean Architecture (4-Layer) | ✅ |
| CQRS Pattern | ✅ |
| MediatR (Commands & Queries) | ✅ |
| MediatR Pipeline Behaviors (Validation + Logging) | ✅ |
| FluentValidation | ✅ |
| Entity Framework Core 8 | ✅ |
| SQL Server / In-Memory DB Support | ✅ |
| Repository Pattern (via EF Core DbContext abstraction) | ✅ |
| Global Exception Handling Middleware (RFC 7807 ProblemDetails) | ✅ |
| Swagger / OpenAPI Documentation | ✅ |
| Health Check Endpoint | ✅ |
| Docker & Docker Compose | ✅ |
| Audit Fields (CreatedAt, ModifiedAt, CreatedBy) | ✅ |
| Paginated Queries | ✅ |
| Unit Testing (xUnit + FluentAssertions + Moq) | ✅ |

---

## 🏛️ Architecture Overview

The solution strictly enforces separation of concerns across 4 distinct layers with unidirectional dependency flow:

```
┌───────────────────────────────────────────────┐
│           CleanArchitecture.WebApi             │
│    Controllers │ Swagger │ Middleware │ DI     │
└──────────────────────┬────────────────────────┘
                       │  depends on
        ┌──────────────┴───────────────┐
        ▼                             ▼
┌────────────────────┐   ┌──────────────────────────┐
│  CleanArchitecture │   │   CleanArchitecture.     │
│  .Infrastructure   │   │      Application         │
│                    │   │                          │
│  EF Core DbContext │   │  MediatR Handlers        │
│  DB Configurations │   │  Commands & Queries      │
│  Seed Data         │   │  FluentValidation        │
│  SQL Server        │   │  Pipeline Behaviors      │
└─────────┬──────────┘   └────────────┬─────────────┘
          │                           │
          └──────────────┬────────────┘
                         │  both depend on
                         ▼
          ┌──────────────────────────────┐
          │   CleanArchitecture.Domain   │
          │                              │
          │   Entities (Product, Cat.)   │
          │   Enums (ProductStatus)      │
          │   Base Auditable Entity      │
          │   Zero Dependencies          │
          └──────────────────────────────┘
```

### Layer Responsibilities

| Layer | Responsibility | Dependencies |
| :--- | :--- | :--- |
| **Domain** | Core entities, enums, base abstractions | None |
| **Application** | Business logic, CQRS handlers, validation, DTOs | Domain |
| **Infrastructure** | EF Core persistence, DB configs, seeding | Application, Domain |
| **WebApi** | REST endpoints, Swagger, middleware, DI wiring | Infrastructure, Application |
| **Tests** | Unit tests for handlers, validators, behaviors | Application, Infrastructure |

---

## 📑 API Endpoints

| Method | Endpoint | Description | Response |
| :---: | :--- | :--- | :--- |
| `GET` | `/api/products` | Paginated list (supports `searchTerm` & `categoryId`) | `200 OK` |
| `GET` | `/api/products/{id}` | Get product by ID | `200 OK` / `404` |
| `POST` | `/api/products` | Create a new product | `201 Created` / `400` |
| `PUT` | `/api/products/{id}` | Update a product | `204 NoContent` / `400` / `404` |
| `DELETE` | `/api/products/{id}` | Delete a product | `204 NoContent` / `404` |
| `GET` | `/api/categories` | Get all categories | `200 OK` |
| `GET` | `/health` | Application health check | `200 OK` |

---

## 📂 Solution Structure

```
enterprise-webapi-clean-architecture/
├── CleanArchitecture.sln
├── Dockerfile
├── docker-compose.yml
├── src/
│   ├── CleanArchitecture.Domain/
│   │   ├── Common/          → BaseEntity, AuditableEntity
│   │   ├── Entities/        → Product, Category
│   │   └── Enums/           → ProductStatus
│   ├── CleanArchitecture.Application/
│   │   ├── Common/
│   │   │   ├── Behaviors/   → ValidationBehavior, LoggingBehavior
│   │   │   ├── Exceptions/  → ValidationException, NotFoundException
│   │   │   ├── Interfaces/  → IApplicationDbContext
│   │   │   └── Models/      → Result<T>, PaginatedList<T>
│   │   └── Products/
│   │       ├── Commands/    → Create, Update, Delete
│   │       ├── Queries/     → GetById, GetWithPagination
│   │       └── Dtos/        → ProductDto
│   ├── CleanArchitecture.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── ApplicationDbContext.cs
│   │   │   ├── ApplicationDbContextInitialiser.cs
│   │   │   └── Configurations/ → ProductConfig, CategoryConfig
│   │   └── DependencyInjection.cs
│   └── CleanArchitecture.WebApi/
│       ├── Controllers/     → ApiControllerBase, Products, Categories
│       ├── Middleware/      → ExceptionHandlingMiddleware
│       ├── Program.cs
│       └── appsettings.json
└── tests/
    └── CleanArchitecture.Application.Tests/
        ├── Products/        → CreateCommandTests, PaginationQueryTests
        └── Common/          → ValidationBehaviorTests
```

---

## 🛠️ How to Run Locally

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 / VS Code / JetBrains Rider *(any one)*

### Steps

**1. Clone the repository**
```bash
git clone https://github.com/waqarrasheed4444/enterprise-webapi-clean-architecture.git
cd enterprise-webapi-clean-architecture
```

**2. Build the solution**
```bash
dotnet build
```

**3. Run all unit tests**
```bash
dotnet test
```

**4. Start the API** *(uses In-Memory DB with auto-seeded sample data — no SQL Server needed!)*
```bash
dotnet run --project src/CleanArchitecture.WebApi
```

**5. Open Swagger UI**  
Navigate to `http://localhost:5000` — Swagger UI loads automatically at the root URL.

---

## 🐳 Running with Docker

No local .NET SDK required. The API runs fully inside Docker:

```bash
docker-compose up --build
```

Open `http://localhost:8080` to access the Swagger UI.

---

## 🔌 Switching to SQL Server

To connect to a real SQL Server database:

1. Open `src/CleanArchitecture.WebApi/appsettings.json`
2. Change:
   ```json
   "UseInMemoryDatabase": false,
   "ConnectionStrings": {
     "DefaultConnection": "Server=YOUR_SERVER;Database=CleanArchitectureDb;Trusted_Connection=True;"
   }
   ```
3. Apply EF Core migrations:
   ```bash
   dotnet ef database update --project src/CleanArchitecture.Infrastructure --startup-project src/CleanArchitecture.WebApi
   ```

---

## 🧪 Unit Testing

```bash
dotnet test --logger "console;verbosity=detailed"
```

Test results: **5 / 5 Passed ✅**

| Test Class | What is Tested |
| :--- | :--- |
| `CreateProductCommandHandlerTests` | Product creation & category validation |
| `GetProductsWithPaginationQueryHandlerTests` | Pagination logic and total count |
| `ValidationBehaviorTests` | MediatR pipeline validation before handler execution |

---

## ⚠️ Error Handling

All exceptions return standardized RFC 7807 `application/problem+json` responses:

```json
{
  "title": "Validation Error",
  "status": 400,
  "detail": "One or more validation failures have occurred.",
  "instance": "/api/products",
  "errors": {
    "Name": ["Product name is required."],
    "Price": ["Price must be greater than zero."]
  }
}
```

---

## 📜 License

This project is open source and available under the [MIT License](LICENSE).

---

**Designed and maintained by Waqar Hussain**  
[GitHub](https://github.com/waqarrasheed4444) • [LinkedIn](https://linkedin.com)
