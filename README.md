# AuHub — Modern Auction Platform

Платформа для онлайн-аукционов на **микросервисной архитектуре** с real-time обновлениями, загрузкой изображений и SSR-фронтендом.

## Технологический стек

| Компонент | Технология |
|-----------|-----------|
| Backend | .NET 10 (LTS), FastEndpoints, CQRS, Clean Architecture |
| Gateway | YARP (reverse proxy, rate limiting, session affinity) |
| Auth | JWT (access + refresh), BCrypt, role-based |
| Real-time | SignalR (auto-reconnect, bid events) |
| Storage | MinIO (S3-compatible, image proxy) |
| Database | PostgreSQL 16, EF Core 10 |
| Frontend | Next.js 16.2, TypeScript, TailwindCSS 4, SignalR client |
| Infra | Docker Compose (7 сервисов), multi-stage builds |

## Архитектура

```
                     ┌─────────────────┐
                     │  API Gateway    │
                     │   (YARP :5000)  │
                     └────────┬────────┘
                              │
            ┌─────────────────┼─────────────────┐
            │                 │                 │
    ┌───────▼────────┐ ┌─────▼──────┐  ┌───────▼────────┐
    │ Identity.API   │ │ Auctions.API│  │  MinIO         │
    │  :5109         │ │  :5108     │  │  :9000/9001    │
    │ - Register     │ │ - Lots     │  │ - Images       │
    │ - Login        │ │ - Bids     │  │ - S3 API       │
    │ - RefreshToken │ │ - SignalR  │  │                │
    └───────┬────────┘ └──────┬──────┘  └────────────────┘
            │                 │
    ┌───────▼────────┐ ┌─────▼──────┐
    │  identity-db   │ │auctions-db │
    │  PostgreSQL    │ │ PostgreSQL │
    │  :5433         │ │ :5432      │
    └────────────────┘ └────────────┘

    ┌─────────────────┐
    │  Frontend       │
    │  Next.js :3000  │
    └─────────────────┘
```

## Быстрый старт

```bash
git clone https://github.com/jinxinzero7/AuHub.git
cd AuHub
cp .env.example .env
docker compose up -d --build
```

### Доступ

| Сервис | URL |
|--------|-----|
| Frontend | http://localhost:3000 |
| Gateway | http://localhost:5000 |
| Auctions Swagger | http://localhost:5108/swagger |
| Identity Swagger | http://localhost:5109/swagger |
| MinIO Console | http://localhost:9001 |

## API Endpoints

### Identity (`/api/auth/*`)
- `POST /register` — регистрация (Admin/User)
- `POST /login` — вход
- `POST /refresh` — обновление токена

### Auctions (`/api/lots/*`)
- `POST /api/lots` — создать лот [Admin]
- `GET /api/lots` — список с пагинацией [public]
- `GET /api/lots/{id}` — детали лота [public]
- `POST /api/lots/{id}/publish` — опубликовать [Owner]
- `POST /api/lots/{id}/bids` — сделать ставку [Auth]
- `GET /api/lots/{id}/bids` — история ставок [public]
- `POST /api/lots/{id}/images` — загрузить фото [Admin]
- `GET /api/lots/{id}/images` — список фото [public]
- `GET /api/lots/{id}/images/{fileName}` — прокси картинки [public]
- `DELETE /api/lots/{id}/images/{imageId}` — удалить фото [Admin]

### SignalR
- Hub: `/hubs/auction`
- Events: `NewBidPlaced`, `LotCompleted`

## Тестирование

```bash
powershell -ExecutionPolicy Bypass -File test_comprehensive_en.ps1
```

**35 тестов, 100% pass rate** — auth, lots, bids, completion, validation.

## Структура проекта

```
AuHub/
├── src/
│   ├── Shared/AuHub.Shared/          # Result Pattern
│   ├── Services/
│   │   ├── Identity/                 # Auth микросервис
│   │   │   ├── Domain/
│   │   │   ├── Application/
│   │   │   ├── Infrastructure/
│   │   │   └── API/
│   │   └── Auctions/                 # Auctions микросервис
│   │       ├── Domain/
│   │       ├── Application/
│   │       ├── Infrastructure/
│   │       └── API/
│   └── Gateway/AuHub.Gateway/        # YARP
├── docker-compose.yml
├── Identity.Dockerfile
├── Auctions.Dockerfile
├── Gateway.Dockerfile
└── AuctionHub.slnx
```

## Frontend

Отдельный репозиторий: https://github.com/jinxinzero7/AuHub-Frontend

- Next.js 16.2 (App Router, SSR для SEO)
- TypeScript, TailwindCSS 4
- SignalR real-time, JWT auth с auto-refresh
- Dark/light mode
- Docker multi-stage build

## Roadmap

### Реализовано
- Микросервисная архитектура (Identity + Auctions + Gateway)
- JWT auth с ролями, BCrypt
- CQRS + Clean Architecture + DDD
- SignalR real-time (bid events, auto-complete)
- MinIO image storage + backend proxy
- Next.js SSR фронтенд
- Docker Compose (7 сервисов)
- 35 автоматизированных тестов

### В планах
- EF Core Migrations (вместо auto-migrate)
- Health Checks для YARP
- Rate limiting для Identity Service
- Notifications Service (email + in-app)
- Kubernetes deployment
- Prometheus + Grafana мониторинг

## Автор

**jinxinzero7** — дипломный проект, 2026

## Лицензия

MIT
