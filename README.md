# AuHub - Modern Auction Platform

Платформа для онлайн-аукционов на **микросервисной архитектуре** с использованием .NET 10 и современных паттернов проектирования.

## Технологический стек

- **.NET 10 (LTS)** — платформа
- **FastEndpoints** — высокопроизводительная альтернатива Controllers
- **YARP** — API Gateway (reverse proxy)
- **JWT Authentication** — безопасная авторизация с ролями (Admin, User)
- **BCrypt** — хеширование паролей (12 rounds)
- **CQRS** — разделение команд и запросов
- **Clean Architecture** — четкое разделение слоев
- **EF Core 10** — ORM для работы с PostgreSQL
- **PostgreSQL 16** — основная база данных
- **Docker** — контейнеризация
- **FluentValidation** — валидация запросов
- **Result Pattern** — типобезопасная обработка ошибок
- **SignalR** — real-time обновления

## Микросервисная архитектура

```
                     ┌─────────────────┐
                     │  API Gateway    │
                     │   (YARP)        │
                     │   Port: 5000    │
                     └────────┬────────┘
                              │
            ┌─────────────────┼─────────────────┐
            │                 │                 │
    ┌───────▼────────┐ ┌─────▼──────┐  ┌───────▼────────┐
    │ Identity.API   │ │ Auctions.API│  │ Notifications  │
    │  Port: 5109    │ │  Port: 5108 │  │  Port: 5110    │
    │                │ │             │  │  (в разработке)│
    │ - Register     │ │ - Lots      │  │                │
    │ - Login        │ │ - Bids      │  │ - Email        │
    │ - RefreshToken │ │ - SignalR   │  │ - In-app       │
    └───────┬────────┘ └──────┬──────┘  └───────┬────────┘
            │                 │                 │
    ┌───────▼────────┐ ┌─────▼──────┐  ┌───────▼────────┐
    │  identity-db   │ │auctions-db │  │notifications-db│
    │  PostgreSQL    │ │ PostgreSQL │  │  PostgreSQL    │
    │  Port: 5433    │ │ Port: 5432 │  │  Port: 5434    │
    └────────────────┘ └────────────┘  └────────────────┘
```

### Сервисы

#### 1. Identity Service (Port: 5109)
- **Ответственность:** Управление пользователями и аутентификация
- **База данных:** identity-db (PostgreSQL)
- **Endpoints:**
  - `POST /api/auth/register` — Регистрация пользователя
  - `POST /api/auth/login` — Вход в систему
  - `POST /api/auth/refresh` — Обновление токена
- **Технологии:** JWT, BCrypt, FastEndpoints

#### 2. Auctions Service (Port: 5108)
- **Ответственность:** Управление лотами и ставками
- **База данных:** auctions-db (PostgreSQL)
- **Endpoints:**
  - `POST /api/lots` — Создание лота [Admin only]
  - `GET /api/lots` — Список лотов [Public]
  - `GET /api/lots/{id}` — Детали лота [Public]
  - `POST /api/lots/{id}/publish` — Публикация лота [Owner only]
  - `POST /api/lots/{id}/complete` — Завершение лота [Owner only]
  - `POST /api/lots/{id}/cancel` — Отмена лота [Owner only]
  - `POST /api/lots/{id}/bids` — Создание ставки [Authenticated]
  - `GET /api/lots/{id}/bids` — История ставок [Public]
- **Технологии:** CQRS, Background Services, FastEndpoints, SignalR

#### 3. API Gateway (Port: 5000)
- **Ответственность:** Единая точка входа, маршрутизация, CORS, Rate Limiting
- **Маршрутизация:**
  - `/api/auth/*` → Identity Service
  - `/api/lots/*` → Auctions Service
  - `/api/notifications/*` → Notifications Service
  - `/hubs/auction` → Auctions Service (SignalR WebSocket passthrough)
- **Технологии:** YARP (Yet Another Reverse Proxy)

#### 4. Notifications Service (Port: 5110) — в разработке
- **Ответственность:** Email и in-app уведомления
- **База данных:** notifications-db (PostgreSQL)
- **Типы уведомлений:** NewBid, Outbid, WonAuction, LotCompleted, AuctionEndingSoon

## Быстрый старт

### Требования

- Docker Desktop
- Git

### Запуск проекта

```bash
# Клонировать репозиторий
git clone https://github.com/jinxinzero7/AuHub.git
cd AuHub

# Запустить все сервисы
docker compose up -d --build

# Подождать 1-2 минуты пока все запустится
docker compose ps
```

### Доступ к приложению

- **API Gateway:** http://localhost:5000
- **Identity Service:** http://localhost:5109/swagger
- **Auctions Service:** http://localhost:5108/swagger

### Тестирование через Gateway

**1. Регистрация Admin:**
```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@auhub.com",
    "password": "Admin123!",
    "name": "Admin User",
    "role": 1
  }'
```

**2. Логин:**
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@auhub.com",
    "password": "Admin123!"
  }'
```

**3. Создать лот (с JWT токеном):**
```bash
curl -X POST http://localhost:5000/api/lots \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {accessToken}" \
  -d '{
    "title": "Vintage Watch",
    "description": "Beautiful vintage watch from 1960s",
    "startingPrice": 100,
    "startTime": "2026-05-14T21:00:00Z",
    "endTime": "2026-05-14T21:10:00Z"
  }'
```

## Особенности реализации

### Микросервисная архитектура
- **Независимые сервисы:** Каждый сервис имеет свою БД и может разворачиваться отдельно
- **API Gateway:** YARP — единая точка входа, нативная поддержка WebSocket/SignalR
- **Shared библиотека:** Общий Result Pattern для всех сервисов
- **JWT валидация:** Auctions Service валидирует JWT локально, не зависит от доступности Identity

### JWT Authentication & Authorization
- **Access Token:** 30 минут
- **Refresh Token:** 30 дней
- **Роли:** Admin (создание лотов), User (ставки)
- **Локальная валидация:** Auctions Service проверяет JWT без обращения к Identity
- **Независимость:** Auctions продолжает работать даже если Identity недоступен

### Domain-Driven Design
- Entities с инкапсулированной бизнес-логикой
- Фабричные методы вместо публичных конструкторов
- Value Objects для сложных типов

### CQRS Pattern
- Команды для изменения состояния (CreateLot, PlaceBid)
- Запросы для чтения данных (GetLots, GetBidsByLot)
- Разделение ответственности

### Vertical Slice Architecture
- Код организован по фичам, а не по слоям
- Каждая фича содержит: Command/Query, Handler, Validator, Endpoint

### Background Services
- Автоматическое завершение истекших аукционов
- Проверка каждую минуту

## Структура проекта

```
AuHub/
├── src/
│   ├── Shared/
│   │   └── AuHub.Shared/              # Result Pattern, константы
│   │
│   ├── Services/
│   │   ├── Identity/
│   │   │   ├── Identity.Domain/       # User, RefreshToken entities
│   │   │   ├── Identity.Application/  # Auth Commands, AuthService
│   │   │   ├── Identity.Infrastructure/# IdentityDbContext, Repositories
│   │   │   └── Identity.API/          # FastEndpoints, JWT generation
│   │   │
│   │   └── Auctions/
│   │       ├── Auctions.Domain/       # Lot, Bid entities
│   │       ├── Auctions.Application/  # CQRS Commands/Queries
│   │       ├── Auctions.Infrastructure/# AuctionsDbContext, Background Services
│   │       └── Auctions.API/          # FastEndpoints, JWT validation
│   │
│   └── Gateway/
│       └── AuHub.Gateway/             # YARP API Gateway
│
├── docker-compose.yml
├── Identity.Dockerfile
├── Auctions.Dockerfile
├── Gateway.Dockerfile
└── AuctionHub.slnx
```

## Тестирование

### Автоматическое тестирование

```bash
# Перезапустить с чистой БД
docker compose down
docker compose up -d

# Подождать 20 секунд для инициализации
# Запустить тесты
powershell -ExecutionPolicy Bypass -File test_comprehensive_en.ps1
```

**Тестовый скрипт проверяет:**
- Аутентификацию (регистрация, логин, валидация)
- Авторизацию по ролям (Admin/User)
- Создание и управление лотами
- Систему ставок (включая проверку CurrentPrice в БД)
- Завершение и отмену лотов
- Валидацию всех входных данных

**Результат:** 35 тестов за ~5 секунд

### Ручное тестирование

Смотри подробные инструкции:
- `AUTH_TESTING_GUIDE.md` — тестирование JWT авторизации
- `TEST_SCENARIO.md` — полный сценарий демонстрации
- `DEMO_GUIDE.md` — шпаргалка для показа проекта

## Полезные команды

```bash
# Посмотреть логи всех сервисов
docker compose logs -f

# Посмотреть логи конкретного сервиса
docker compose logs -f identity-api
docker compose logs -f auctions-api
docker compose logs -f gateway

# Остановить все сервисы
docker compose down

# Пересобрать и запустить
docker compose up -d --build

# Подключиться к Identity DB
docker compose exec identity-db psql -U postgres -d identitydb

# Подключиться к Auctions DB
docker compose exec auctions-db psql -U postgres -d auctionsdb
```

## Roadmap

### Реализовано
- Микросервисная архитектура с API Gateway
- Identity Service: Register, Login, RefreshToken
- Auctions Service: Lots, Bids, Background Services
- API Gateway: YARP для маршрутизации
- Shared библиотека: Result Pattern
- JWT Authentication: Генерация в Identity, валидация в Auctions
- Docker: Отдельные контейнеры для каждого сервиса
- Независимость сервисов: Auctions работает без Identity

### В работе
- YARP Migration (замена Ocelot)
- Frontend на Next.js 15 (отдельный репозиторий)
- Notifications Service (email + in-app уведомления)
- SignalR Hub интеграция

### В планах
- Unit & Integration тесты
- CI/CD pipeline
- Kubernetes deployment
- Monitoring (Prometheus, Grafana)
- Distributed tracing (OpenTelemetry)

## Автор

**jinxinzero7**
Дипломный проект, 2026

## Лицензия

MIT
