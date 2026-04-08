# AuHub - Modern Auction Platform

Современная платформа для онлайн-аукционов, построенная на микросервисной архитектуре с использованием .NET 10 и современных паттернов проектирования.

## Технологический стек

- **.NET 10 (LTS)** - платформа с поддержкой до 2028 года
- **FastEndpoints** - высокопроизводительная альтернатива Controllers
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

Открой Swagger UI и протестируй endpoints:

**POST /api/lots** - Создать лот:
```json
{
  "title": "Vintage Watch",
  "description": "Beautiful vintage watch from 1960s",
  "startingPrice": 100,
  "startTime": "2026-04-09T10:00:00Z",
  "endTime": "2026-04-10T10:00:00Z",
  "sellerId": "123e4567-e89b-12d3-a456-426614174000"
}
```

**GET /api/lots** - Получить список лотов

## Особенности реализации

### Domain-Driven Design
- Entities с инкапсулированной бизнес-логикой
- Фабричные методы вместо публичных конструкторов
- Value Objects для сложных типов

### CQRS Pattern
- Команды для изменения состояния (CreateLot)
- Запросы для чтения данных (GetLots)
- Разделение ответственности

### Vertical Slice Architecture
- Код организован по фичам, а не по слоям
- Каждая фича содержит: Command/Query, Handler, Validator, Endpoint

### Автоматические миграции
- Миграции применяются автоматически при старте API
- Не требуется ручное выполнение команд

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
- Микросервис Auctions с CQRS
- Domain модели (Lot, Bid)
- FastEndpoints (Create Lot, Get Lots)
- EF Core + автоматические миграции
- Docker контейнеризация
- Swagger документация

### 📋 В планах
- Микросервис Users (JWT аутентификация)
- Микросервис Notifications (SignalR)
- Background Services для автозавершения аукционов
- API Gateway
- Unit & Integration тесты

## Автор

**jinxinzero7**  
Дипломный проект, 2026

## Лицензия

MIT
