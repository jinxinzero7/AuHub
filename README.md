# AuHub - Modern Auction Platform

Современная платформа для онлайн-аукционов, построенная на микросервисной архитектуре с использованием .NET 10 и современных паттернов проектирования.

## Технологический стек

- **.NET 10 (LTS)** - платформа с поддержкой до 2028 года
- **FastEndpoints** - высокопроизводительная альтернатива Controllers
- **JWT Authentication** - безопасная авторизация с ролями (Admin, User)
- **BCrypt** - хеширование паролей (12 rounds)
- **CQRS** - разделение команд и запросов
- **Clean Architecture** - четкое разделение слоев
- **EF Core 10** - ORM для работы с PostgreSQL
- **PostgreSQL 16** - основная база данных
- **Docker** - контейнеризация для простого развертывания
- **FluentValidation** - валидация запросов
- **Result Pattern** - типобезопасная обработка ошибок

## Архитектура

Проект построен на принципах Clean Architecture с разделением на слои:

```
Auctions.API          → FastEndpoints, HTTP
    ↓
Auctions.Application  → CQRS, Business Logic
    ↓
Auctions.Domain       → Entities, Value Objects
    ↑
Auctions.Infrastructure → EF Core, PostgreSQL
```

## Быстрый старт

### Требования

- Docker Desktop
- Git

### Запуск проекта

```bash
# Клонировать репозиторий
git clone https://github.com/jinxinzero7/AuHub.git
cd AuHub

# Запустить все сервисы (PostgreSQL + API)
docker-compose up -d

# Подождать 1-2 минуты пока все запустится
docker-compose ps
```

### Доступ к приложению

- **API:** http://localhost:5108
- **Swagger UI:** http://localhost:5108/swagger

### Тестирование

Открой Swagger UI и протестируй endpoints.

**Важно:** Большинство endpoints требуют JWT авторизацию. Смотри [AUTH_TESTING_GUIDE.md](AUTH_TESTING_GUIDE.md) для полной инструкции.

#### Быстрый старт:

1. **Регистрация Admin:**
```json
POST /api/auth/register
{
  "email": "admin@auhub.com",
  "password": "Admin123!",
  "name": "Admin User",
  "role": 1
}
```

2. **Авторизация в Swagger:**
   - Скопируй `accessToken` из ответа
   - Нажми **Authorize** в Swagger UI
   - Введи: `Bearer {accessToken}`

3. **Создать лот (только Admin):**
```json
POST /api/lots
{
  "title": "Vintage Watch",
  "description": "Beautiful vintage watch from 1960s",
  "startingPrice": 100,
  "startTime": "2026-05-04T21:00:00Z",
  "endTime": "2026-05-04T21:10:00Z"
}
```

**Примечание:** `sellerId` автоматически берется из JWT токена

## Особенности реализации

### JWT Authentication & Authorization
- **Access Token:** 30 минут
- **Refresh Token:** 30 дней
- **Роли:** Admin (создание лотов), User (ставки)
- **Защита endpoints:** role-based и ownership-based
- **Валидация паролей:** мин 8 символов, заглавная, строчная, цифра, спецсимвол

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

### Автоматическое создание БД
- БД создается автоматически при старте API (dev режим)
- Не требуется ручное выполнение миграций

## Структура проекта

```
AuHub/
├── src/Services/Auctions/
│   ├── Auctions.Domain/         # Entities, Value Objects, Interfaces
│   ├── Auctions.Application/    # Commands, Queries, Handlers
│   ├── Auctions.Infrastructure/ # EF Core, Repositories
│   └── Auctions.API/            # FastEndpoints, Program.cs
├── docker-compose.yml           # Оркестрация контейнеров
├── Dockerfile                   # Сборка API образа
└── DEMO_GUIDE.md               # Руководство для демонстрации
```

## Полезные команды

```bash
# Посмотреть логи API
docker-compose logs -f auctions-api

# Остановить все сервисы
docker-compose down

# Пересобрать и запустить
docker-compose up -d --build

# Подключиться к PostgreSQL
docker-compose exec postgres psql -U postgres -d auctionhub
```

## Roadmap

### ✅ Реализовано
- **Микросервис Auctions** с CQRS
- **Domain модели:** Lot, Bid, User, RefreshToken
- **JWT Authentication:** Register, Login, RefreshToken
- **Authorization:** Role-based (Admin, User) + Ownership checks
- **Endpoints:** 
  - Auth: Register, Login, RefreshToken
  - Lots: Create, Publish, Complete, Cancel, GetAll, GetById
  - Bids: PlaceBid, GetBidsByLot
- **Background Service:** автозавершение аукционов
- **Пагинация** для списка лотов
- **EF Core** + автоматическое создание БД
- **Docker** контейнеризация
- **Swagger** документация с JWT авторизацией

### 📋 В планах
- Микросервис Notifications (SignalR для real-time уведомлений)
- API Gateway
- Unit & Integration тесты
- CI/CD pipeline

## Автор

**jinxinzero7**  
Дипломный проект, 2026

## Лицензия

MIT
