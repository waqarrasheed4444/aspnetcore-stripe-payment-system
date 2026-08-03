# 🚀 Enterprise .NET 8 Web API — Clean Architecture & CQRS

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![C# 12](https://img.shields.io/badge/C%23-12.0-239120?style=for-the-badge&logo=c-sharp)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![EF Core](https://img.shields.io/badge/EF%20Core-8.0-512BD4?style=for-the-badge&logo=dotnet)](https://docs.microsoft.com/en-us/ef/core/)
[![MediatR](https://img.shields.io/badge/MediatR-CQRS-blue?style=for-the-badge)](https://github.com/jbogard/MediatR)
[![Docker](https://img.shields.io/badge/Docker-Supported-2496ED?style=for-the-badge&logo=docker)](https://www.docker.com/)
[![Swagger](https://img.shields.io/badge/Swagger-OpenAPI-85EA2D?style=for-the-badge&logo=swagger)](https://swagger.io/)

A production-grade, enterprise-ready **ASP.NET Core 8 Web API** template built using **Clean Architecture** principles, **CQRS with MediatR**, **Entity Framework Core**, **FluentValidation**, and **xUnit** unit tests.

Designed as a showcase repository demonstrating high-performance, maintainable backend architectural patterns for enterprise software development.

---

## 🏛️ Architecture Overview

The solution strictly enforces separation of concerns across 4 distinct layers:

```
                            ┌─────────────────────────────────┐
                            │     CleanArchitecture.WebApi    │
                            │   (Controllers, Swagger, DI)    │
                            └────────────────┬────────────────┘
                                             │
                        ┌────────────────────┴────────────────────┐
                        ▼                                         ▼
         ┌──────────────────────────────┐        ┌──────────────────────────────┐
         │ CleanArchitecture.            │        │ CleanArchitecture.           │
         │ Infrastructure               │        │ Application                  │
         │ (EF Core, DB Context, Seeds) │        │ (CQRS, MediatR, Validation)  │
         └──────────────┬───────────────┘        └──────────────┬───────────────┘
                        │                                       │
                        └────────────────────┬──────────────────┘
                                             ▼
                            ┌─────────────────────────────────┐
                            │     CleanArchitecture.Domain    │
                            │   (Entities, Value Objects)     │
                            └─────────────────────────────────┘
```

### Layer Responsibilities

1. **Domain Layer (`CleanArchitecture.Domain`)**: Core enterprise entities, domain events, base audit abstractions (`AuditableEntity`), and enums. Contains zero third-party framework dependencies.
2. **Application Layer (`CleanArchitecture.Application`)**: Business logic implemented via **CQRS (Command Query Responsibility Segregation)** using **MediatR**. Includes:
   - **Queries**: Paginated list fetching, single entity queries with DTO projections.
   - **Commands**: Entity creation, updates, and deletion.
   - **Validation Behaviors**: Automatic input validation pipeline via **FluentValidation** before execution.
   - **Logging Behaviors**: Structured request execution logging.
3. **Infrastructure Layer (`CleanArchitecture.Infrastructure`)**: Entity Framework Core persistence with DbContext configurations, audit interceptors, and automated database initialisation & seeding.
4. **Web API Layer (`CleanArchitecture.WebApi`)**: RESTful API Controllers, Swagger UI, Health Checks, and global exception handling middleware returning RFC 7807 `ProblemDetails`.

---

## ✨ Key Features & Technical Highlights

* **CQRS Pattern**: Decoupled read and write data flows utilizing MediatR handlers.
* **MediatR Pipeline Behaviors**: Cross-cutting concerns (logging, validation) execute transparently without cluttering command handlers.
* **Standardized Exception Handling**: Global middleware converts exceptions into RFC 7807 compliant `ProblemDetails` (`400 Bad Request`, `404 Not Found`, `500 Server Error`).
* **Zero-Config Developer Experience**: Boots automatically out of the box using **EF Core In-Memory Database** with pre-seeded sample data. Can be effortlessly switched to **SQL Server** via `appsettings.json`.
* **Automated Unit Testing**: Includes unit tests covering MediatR handlers, validation rules, and pipeline behaviors using `xUnit`, `FluentAssertions`, and `Moq`.
* **Containerization Ready**: Includes multi-stage `Dockerfile` and `docker-compose.yml`.

---

## 📑 API Endpoints Summary

| Method | Endpoint | Description | Status Code |
| :--- | :--- | :--- | :---: |
| **GET** | `/api/products` | Get paginated list of products (supports `SearchTerm` & `CategoryId`) | `200 OK` |
| **GET** | `/api/products/{id}` | Get product details by ID | `200 OK`, `404` |
| **POST** | `/api/products` | Create a new product | `201 Created`, `400` |
| **PUT** | `/api/products/{id}` | Update an existing product | `204 NoContent`, `400`, `404` |
| **DELETE** | `/api/products/{id}` | Delete a product | `204 NoContent`, `404` |
| **GET** | `/api/categories` | Get all product categories | `200 OK` |
| **GET** | `/health` | Application health check endpoint | `200 OK` |

---

## 🛠️ How to Run Locally

### Prerequisites
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* Visual Studio 2022 / VS Code / JetBrains Rider

### Step-by-Step

1. **Clone the repository**:
   ```bash
   git clone https://github.com/YOUR_USERNAME/dotnet-clean-architecture-api.git
   cd dotnet-clean-architecture-api
   ```

2. **Restore dependencies & build**:
   ```bash
   dotnet build
   ```

3. **Run unit tests**:
   ```bash
   dotnet test
   ```

4. **Start the API**:
   ```bash
   dotnet run --project src/CleanArchitecture.WebApi
   ```

5. **Open Swagger UI**:
   Navigate to `https://localhost:7124` or `http://localhost:5000` in your web browser. Swagger UI will load automatically.

---

## 🐳 Running with Docker

You can run the entire application inside Docker without needing local .NET installation:

```bash
docker-compose up --build
```
Access Swagger UI at `http://localhost:8080`.

---

## 🧪 Unit Testing Strategy

Unit tests are located in `tests/CleanArchitecture.Application.Tests`:

```bash
dotnet test --logger "console;verbosity=detailed"
```

Tested scenarios include:
- `CreateProductCommandHandlerTests`: Validates entity persistence & category non-existence checks.
- `GetProductsWithPaginationQueryHandlerTests`: Tests pagination logic, page size limits, and entity counts.
- `ValidationBehaviorTests`: Ensures invalid commands throw structured `ValidationException` before handler execution.

---

## 📜 License & Contact

Designed & Maintained by **Full Stack .NET Developer**.  
Feel free to connect on [Upwork](https://www.upwork.com) or [LinkedIn](https://www.linkedin.com).
