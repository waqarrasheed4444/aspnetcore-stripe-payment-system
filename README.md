# 💳 ASP.NET Core Stripe Payment System — Clean Architecture

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C# 12](https://img.shields.io/badge/C%23-12.0-239120?style=for-the-badge&logo=c-sharp&logoColor=white)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Stripe](https://img.shields.io/badge/Stripe-API%20v45+-635BFF?style=for-the-badge&logo=stripe&logoColor=white)](https://stripe.com)
[![EF Core](https://img.shields.io/badge/EF%20Core-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://docs.microsoft.com/en-us/ef/core/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-2022-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/sql-server)
[![MediatR](https://img.shields.io/badge/MediatR-CQRS-blue?style=for-the-badge)](https://github.com/jbogard/MediatR)
[![FluentValidation](https://img.shields.io/badge/FluentValidation-11.x-B71C1C?style=for-the-badge)](https://docs.fluentvalidation.net/)
[![xUnit](https://img.shields.io/badge/xUnit-22%20Tests%20Passed-brightgreen?style=for-the-badge&logo=xunit)](https://xunit.net/)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?style=for-the-badge&logo=docker&logoColor=white)](https://www.docker.com/)
[![Swagger](https://img.shields.io/badge/Swagger-OpenAPI-85EA2D?style=for-the-badge&logo=swagger&logoColor=black)](https://swagger.io/)

A complete, production-ready **Stripe Payment & Subscription Integration** built on **ASP.NET Core 8 Web API** using **Clean Architecture**, **CQRS (MediatR)**, **Entity Framework Core**, and the official **Stripe.net SDK**.

Designed with strict security standards: prices are always validated from the database, webhooks are cryptographically verified, and events are processed with bulletproof idempotency to eliminate duplicate charges.

---

## 🌟 Key Features

### 🛒 One-Time Payments (Stripe Checkout)
- **Hosted Checkout Sessions**: Securely redirects customers to Stripe's high-converting, mobile-responsive checkout flow.
- **Server-Authoritative Pricing**: Line items, product details, and prices are retrieved directly from the database — never trusted from the client.
- **Customer Mapping & Reuse**: Automatically creates or retrieves existing Stripe Customers by user email to prevent duplicate records.
- **Metadata Tagging**: Tags checkout sessions and PaymentIntents with internal `OrderId`, `UserId`, and `PaymentId` for full traceability.

### 🔄 Subscription Billing
- **Recurring Plans**: Create subscription checkout sessions for monthly/yearly plans.
- **Customer Billing Portal**: Generate authenticated sessions for the Stripe Customer Portal so users can manage payment methods, upgrade/downgrade, or cancel.
- **Lifecycle Webhooks**: Synchronizes subscription states (`Active`, `PastDue`, `Canceled`, `Trialing`) based on Stripe invoice events.
- **Cancellation Controls**: Support for both end-of-period cancellation and immediate termination.

### 🛡️ Webhook Security & Idempotency
- **Cryptographic Signature Verification**: Every incoming webhook payload is verified using `Stripe-Signature` and your secret endpoint key (`EventUtility.ConstructEvent`).
- **Idempotency Guard**: All processed event IDs are persisted in a `ProcessedWebhookEvents` table. Duplicate events sent by Stripe retries are safely ignored.
- **Authoritative Status Sync**: Payment and order statuses (`Paid`, `Failed`, `Refunded`) are modified **only** upon valid webhook confirmation, never on browser redirects.

### 💸 Refunds Management
- **Full & Partial Refunds**: Refund any paid transaction via authenticated REST endpoints.
- **Over-Refund Protection**: Automatically calculates refundable balances and rejects refund attempts exceeding the original payment amount.
- **Ledger Tracking**: Every refund creates a granular `PaymentTransaction` record linked to the parent payment.

---

## 🏛️ Clean Architecture Design

The project enforces strict separation of concerns across 4 layers with unidirectional dependency flow:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ CleanArchitecture.WebApi                                                    │
│  - Controllers: Payments, Subscriptions, Orders, Webhooks, Products         │
│  - Middleware: ExceptionHandlingMiddleware (RFC 7807 ProblemDetails)        │
│  - Services: CurrentUserService (HttpContext / Claims extraction)           │
└──────────────────────────────────────┬──────────────────────────────────────┘
                                       │  depends on
                        ┌──────────────┴──────────────┐
                        ▼                             ▼
┌────────────────────────────────────────┐   ┌────────────────────────────────┐
│ CleanArchitecture.Infrastructure       │   │ CleanArchitecture.Application  │
│  - Stripe:                             │   │  - Payments (Commands/Queries) │
│     * StripePaymentService             │   │  - Subscriptions (Commands/Q)  │
│     * StripeSubscriptionService        │   │  - Orders (Commands/Queries)   │
│     * StripeSettings (Options)         │   │  - Products (Commands/Queries) │
│  - Persistence:                        │   │  - MediatR Behaviors:          │
│     * ApplicationDbContext             │   │     * ValidationBehavior       │
│     * EF Core Entity Configurations    │   │     * LoggingBehavior          │
│     * DbContextInitialiser (Seed Data) │   │  - FluentValidation Validators │
└───────────────────┬────────────────────┘   └────────────────┬───────────────┘
                    │                                         │
                    └────────────────────┬────────────────────┘
                                         │  both depend on
                                         ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ CleanArchitecture.Domain (Zero external dependencies)                        │
│  - Entities: Order, OrderItem, Payment, PaymentTransaction,                 │
│              StripeCustomer, Subscription, ProcessedWebhookEvent, Product   │
│  - Enums: PaymentStatus, OrderStatus, SubscriptionStatus,                   │
│           PaymentTransactionType, PlanInterval                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 📑 API Reference

### 💳 Payments (`/api/payments`)
| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `POST` | `/api/payments/checkout` | Create a Stripe Checkout session for an order |
| `POST` | `/api/payments/refund` | Issue a full or partial refund for a payment |
| `GET` | `/api/payments/{id}` | Get payment details and transaction ledger |
| `GET` | `/api/payments/user/{userId}` | Get payment history for a user |

### 🔁 Subscriptions (`/api/subscriptions`)
| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `POST` | `/api/subscriptions/checkout` | Create subscription checkout session |
| `POST` | `/api/subscriptions/customer-portal` | Generate Stripe Customer Billing Portal URL |
| `POST` | `/api/subscriptions/cancel` | Cancel an active subscription |
| `GET` | `/api/subscriptions/user/{userId}` | Get active subscription details for a user |

### 📦 Orders (`/api/orders`)
| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `POST` | `/api/orders` | Create an order with items (prices validated from DB) |
| `GET` | `/api/orders/{id}` | Get order details with payment status |
| `GET` | `/api/orders/user/{userId}` | Get all orders for a user |

### 🪝 Webhooks (`/api/webhooks`)
| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `POST` | `/api/webhooks/stripe` | Authoritative Stripe webhook receiver (verifies signature) |

---

## ⚡ Getting Started

### 1. Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Stripe Account](https://dashboard.stripe.com/register) (Free test mode)
- [Stripe CLI](https://stripe.com/docs/stripe-cli) *(for local webhook testing)*
- Visual Studio 2022 / VS Code / JetBrains Rider

---

### 2. Configuration

Set your Stripe API keys in `src/CleanArchitecture.WebApi/appsettings.json` or via environment variables:

```json
{
  "UseInMemoryDatabase": true,
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=StripePaymentSystemDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "Stripe": {
    "PublishableKey": "pk_test_YOUR_STRIPE_PUBLISHABLE_KEY",
    "SecretKey": "sk_test_YOUR_STRIPE_SECRET_KEY",
    "WebhookSecret": "whsec_YOUR_STRIPE_WEBHOOK_SECRET",
    "SuccessUrl": "https://localhost:5001/checkout/success?session_id={CHECKOUT_SESSION_ID}",
    "CancelUrl": "https://localhost:5001/checkout/cancel",
    "PortalReturnUrl": "https://localhost:5001/account/billing"
  }
}
```

> **Security Tip**: Never commit real secret keys to Git. Use **.NET User Secrets** for local development:
> ```bash
> dotnet user-secrets set "Stripe:SecretKey" "sk_test_..." --project src/CleanArchitecture.WebApi
> dotnet user-secrets set "Stripe:WebhookSecret" "whsec_..." --project src/CleanArchitecture.WebApi
> ```

---

### 3. Run Locally

```bash
# Clone the repository
git clone https://github.com/waqarrasheed4444/aspnetcore-stripe-payment-system.git
cd aspnetcore-stripe-payment-system

# Build the solution
dotnet build AspNetCoreStripePaymentSystem.slnx

# Run all 22 unit tests
dotnet test AspNetCoreStripePaymentSystem.slnx

# Start the Web API
dotnet run --project src/CleanArchitecture.WebApi
```

Open `http://localhost:5000` to access the interactive **Swagger UI**.

---

## 🪝 Local Webhook Testing with Stripe CLI

Stripe webhooks cannot reach `localhost` directly. Use the official **Stripe CLI** to forward events to your local API:

### Step 1: Install Stripe CLI
- **Windows (Scoop)**: `scoop install stripe`
- **Windows (Chocolatey)**: `choco install stripe-cli`
- **macOS (Homebrew)**: `brew install stripe/stripe-cli/stripe`

### Step 2: Login to Stripe
```bash
stripe login
```

### Step 3: Forward Webhooks to Local API
```bash
stripe listen --forward-to https://localhost:5001/api/webhooks/stripe
```

The CLI will print your local webhook signing secret:
```
> Ready! Your webhook signing secret is whsec_1234567890abcdef...
```

Copy this secret into your `appsettings.json` under `"Stripe:WebhookSecret"`.

### Step 4: Trigger Test Events
```bash
# Trigger a successful checkout
stripe trigger checkout.session.completed

# Trigger a failed payment
stripe trigger payment_intent.payment_failed

# Trigger a refund
stripe trigger charge.refunded

# Trigger subscription events
stripe trigger customer.subscription.created
stripe trigger invoice.paid
```

---

## 💳 Stripe Test Cards Reference

Use these test cards in Stripe test mode:

| Card Type | Card Number | Expiry | CVC | Expected Result |
| :--- | :--- | :---: | :---: | :--- |
| **Standard Success** | `4242 4242 4242 4242` | Any future date | Any 3 digits | ✅ Payment Succeeds |
| **3D Secure (3DS)** | `4000 0027 6000 3184` | Any future date | Any 3 digits | 🛡️ Prompts 3DS Challenge |
| **Declined (Insufficient Funds)** | `4000 0002 1111 1111` | Any future date | Any 3 digits | ❌ Card Declined |
| **Incorrect CVC** | `4000 0000 0000 0127` | Any future date | Any 3 digits | ❌ CVC Check Fails |
| **Expired Card** | `4000 0000 0000 0069` | Any past date | Any 3 digits | ❌ Expired Card |

---

## 🧪 Unit Testing

The test suite covers full checkout creation, over-refund prevention, idempotency guards, and webhook signature verification:

```bash
dotnet test --logger "console;verbosity=detailed"
```

### Test Results: **22 / 22 Passed ✅**

| Test Class | Scenarios Covered |
| :--- | :--- |
| `CreatePaymentCheckoutCommandHandlerTests` | Pending order checkout, existing customer reuse, invalid order handling, paid order rejection |
| `RefundPaymentCommandHandlerTests` | Full refunds, partial refunds, over-refund prevention, unpaid order refund rejection |
| `ProcessStripeWebhookCommandHandlerTests` | `checkout.session.completed` handling, `payment_intent.payment_failed` logging, **duplicate event idempotency guard** |
| `SubscriptionCommandHandlerTests` | Subscription checkout creation, customer mapping, cancellation scheduling |
| `CreateOrderCommandHandlerTests` | Server price calculation from DB, stock validation, missing product rejection |
| `ValidationBehaviorTests` | MediatR pipeline validation before handler execution |
| `CreateProductCommandHandlerTests` | Product persistence and category foreign-key validation |
| `GetProductsWithPaginationQueryHandlerTests` | Pagination calculations and search filtering |

---

## 🚀 Production Deployment Checklist

Before going live:
- [ ] Replace `sk_test_...` and `pk_test_...` with live keys (`sk_live_...`, `pk_live_...`).
- [ ] Create an official Webhook Endpoint in the [Stripe Dashboard](https://dashboard.stripe.com/webhooks) pointing to `https://yourdomain.com/api/webhooks/stripe`.
- [ ] Configure the live `WebhookSecret` (`whsec_...`) in production environment variables.
- [ ] Enable HTTPS on all endpoints.
- [ ] Set `UseInMemoryDatabase: false` and point `DefaultConnection` to production SQL Server.
- [ ] Run EF Core database migrations:
  ```bash
  dotnet ef database update --project src/CleanArchitecture.Infrastructure --startup-project src/CleanArchitecture.WebApi
  ```

---

## 📜 License

This project is licensed under the [MIT License](LICENSE).

---

**Designed and developed by [Waqar Hussain](https://github.com/waqarrasheed4444)**  
*Full Stack .NET Developer* • [LinkedIn](https://linkedin.com) • [GitHub](https://github.com/waqarrasheed4444)

