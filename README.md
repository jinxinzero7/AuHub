# AuHub — Online Auction Platform

AuHub is a diploma and portfolio project: a microservices-based online auction platform with real-time bidding, image storage, wallet/payment flow, notifications, Docker deployment and an automated test suite under active stabilization.

## Status

Current architecture:
- 4 backend microservices: Identity, Auctions, Notifications, Payment;
- YARP API Gateway;
- Next.js 16.2 frontend;
- PostgreSQL database per service;
- RabbitMQ + MassTransit;
- SignalR real-time events;
- MinIO image storage;
- Docker Compose deployment;
- GitHub Actions CI.

Verified on 2026-06-08:
- backend build passes with 0 warnings and 0 errors;
- backend test run contains 231 xUnit cases, currently 231 passed / 0 failed;
- all backend API services use FastEndpoints 8.1.0;
- Auctions demo seed no longer calls the invalid `Approve()` then `Publish()` chain;
- manual auction completion is admin-only through `/api/admin/lots/{id}/force-complete`;
- public registration always creates regular users; admin self-registration is disabled;
- Payment internal operations and Notifications direct-send endpoint require `X-Internal-Api-Key`;
- public payment balance is JWT-scoped and no longer supports arbitrary public `userId` lookup;
- Payment no longer registers the duplicate `AuctionCompletedEvent` consumer;
- lot moderation now uses `Draft -> PendingModeration -> Active`; seller submit endpoint is `/api/lots/{id}/submit-for-moderation`;
- lot creation supports seller-selected delivery providers: `Cdek`, `YandexDelivery`, `RussianPost`;
- auction completion opens a 3-day winner delivery request window; winner request endpoint is `/api/lots/{id}/delivery-request`;
- overdue delivery requests automatically move to `DeliveryRequestExpired`; sellers can mark requested delivery as shipped through `/api/lots/{id}/ship`;
- bidding tests cover previous bidder release, same-bidder non-release and reserve compensation on exhausted concurrency;
- Payment command tests cover reserve, charge, release, refund and seller payout handlers;
- backend integration projects contain API smoke tests; `.NET E2E.Tests` is still a placeholder;
- frontend production build passes without Google Fonts network dependency;
- frontend lint passes cleanly;
- CI unit-test step runs unit test projects explicitly.

Project docs:
- project context: `../CONTEXT.md`
- active backlog: `../TODO.md`
- diploma explanatory note: `docs/пояснительная-записка-auhub.docx`
- frontend repo/folder: `../auhub-frontend`

## Tech Stack

| Area | Technology |
|---|---|
| Backend | .NET 10, ASP.NET Core, FastEndpoints 8.1.0 |
| Architecture | Clean Architecture, CQRS, DDD |
| Gateway | YARP |
| Auth | JWT, refresh token rotation, BCrypt, role-based auth |
| Database | PostgreSQL 16, EF Core |
| Messaging | RabbitMQ, MassTransit |
| Real-time | SignalR |
| Storage | MinIO |
| Frontend | Next.js 16.2, React 19, TypeScript, TailwindCSS 4 |
| Tests | xUnit, NSubstitute, FluentAssertions, coverlet |
| Infra | Docker Compose, GitHub Actions |

## Architecture

```text
Frontend (Next.js :3000)
        |
        v
YARP Gateway (:5000)
        |
        +--> Identity.API (:5109)       -> identity-db (:5433)
        +--> Auctions.API (:5108)       -> auctions-db (:5432)
        +--> Notifications.API (:5110)  -> notifications-db (:5434)
        +--> Payment.API (:5111)        -> payment-db (:5435)

Shared infrastructure:
- RabbitMQ (:5672, management :15672)
- MinIO (:9000, console :9001)
```

Each service follows the same broad structure:

```text
API             FastEndpoints, auth, request/response mapping
Application     commands, queries, handlers, service interfaces
Domain          entities, value objects, events, business rules
Infrastructure  EF Core, repositories, external clients
```

## Services

### Identity

- register/login/refresh;
- JWT generation;
- refresh token rotation;
- replay detection;
- role-based access;
- user ban state.

### Auctions

- lot lifecycle;
- bids;
- moderation/admin actions;
- SignalR events;
- image metadata;
- auction completion background service;
- optimistic concurrency;
- idempotent bid placement;
- outbox/domain events.

### Notifications

- in-app notifications;
- unread count;
- mark as read;
- protected direct service-to-service send endpoint;
- RabbitMQ consumers for auction events.

### Payment

- wallet balance;
- frozen balance;
- top-up;
- internal reserve/release funds;
- internal charge winner;
- internal transfer to seller;
- internal refund;
- transaction history.

## Docker Compose

The compose file defines these services:

- `identity-db`
- `auctions-db`
- `notifications-db`
- `payment-db`
- `identity-api`
- `auctions-api`
- `notifications-api`
- `payment-api`
- `gateway`
- `rabbitmq`
- `minio`
- `frontend`

Start:

```powershell
docker compose up -d --build
```

Stop:

```powershell
docker compose down
```

View logs:

```powershell
docker compose logs -f gateway
```

## Local URLs

| Service | URL |
|---|---|
| Frontend | http://localhost:3000 |
| Gateway | http://localhost:5000 |
| Identity Swagger | http://localhost:5109/swagger |
| Auctions Swagger | http://localhost:5108/swagger |
| Notifications Swagger | http://localhost:5110/swagger |
| Payment Swagger | http://localhost:5111/swagger |
| RabbitMQ UI | http://localhost:15672 |
| MinIO Console | http://localhost:9001 |

## Tests

Run all tests:

```powershell
dotnet test AuctionHub.slnx
```

Test projects:
- `Shared.UnitTests`
- `Auctions.UnitTests`
- `Identity.UnitTests`
- `Notifications.UnitTests`
- `Payment.UnitTests`
- `*.IntegrationTests`
- `E2E.Tests`

Current state:
- unit tests cover core domain/application behavior and currently pass;
- bidding/payment money-side-effect coverage has been strengthened, but full escrow/idempotency integration coverage is still pending;
- backend integration projects cover API host startup and basic auth/internal-key guards;
- persistence-backed integration tests and UI E2E still need real coverage;
- CI workflow exists and runs real unit test projects; integration tests remain disabled until CI strategy is updated.

## Main User Flow

1. User registers/logs in through Identity.
2. User creates a lot through Auctions.
3. Images are uploaded to MinIO.
4. Seller submits the draft lot for moderation.
5. Admin approves the `PendingModeration` lot into active status.
6. Another user places a bid.
7. Auctions checks business rules and payment balance.
8. Payment reserves bidder funds and releases previous bidder funds.
9. Auctions saves bid with idempotency and optimistic concurrency protection.
10. SignalR pushes real-time update to clients.
11. RabbitMQ/MassTransit notifies other services asynchronously.

Internal Payment operations and direct notification send are protected by `X-Internal-Api-Key`. This is a diploma baseline for service-to-service protection; stronger network isolation/service auth is still future hardening.

## Important Patterns

- Clean Architecture
- CQRS
- DDD entities and domain events
- Result Pattern
- Refresh token rotation
- Optimistic concurrency
- Idempotency keys
- Outbox Pattern
- Soft delete
- Background services
- API Gateway
- Async messaging

## Development Notes

- Keep `../CONTEXT.md` updated when architecture or service behavior changes.
- Keep `../TODO.md` short and action-oriented.
- Keep the diploma explanatory note in `docs/` synchronized with major architecture and business-flow changes.
- Do not reintroduce outdated numbers like 7 services or 35 tests.
- Treat `../CONTEXT.md` as the source of truth for current gaps and verified test/build state.
- Prefer verifying with `dotnet build`, `dotnet test`, frontend build and Docker Compose config after meaningful changes.

## Author

Nikolay / `jinxinzero7`  
Diploma project, 2026.
