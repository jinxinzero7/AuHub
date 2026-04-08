# Шпаргалка для демонстрации AuHub

## Перед демонстрацией (утром)

### 1. Запустить проект
```bash
cd C:\Users\niko\Desktop\AuHub
docker-compose up -d
```

Подожди 2-3 минуты пока все запустится.

### 2. Проверить что работает
```bash
docker-compose ps
```

Должны быть запущены:
- `auhub-postgres` (healthy)
- `auhub-api` (up)

### 3. Открыть Swagger
http://localhost:5108/swagger

---

## Что показать

### 1. Структура проекта (VSCodium)
Открой папку `AuHub` в VSCodium и покажи:
- `src/Services/Auctions/` - микросервис
- `Auctions.Domain/` - DDD entities
- `Auctions.Application/` - CQRS (Commands/Queries)
- `Auctions.Infrastructure/` - EF Core + PostgreSQL
- `Auctions.API/` - FastEndpoints
- `docker-compose.yml` - контейнеризация
- `Dockerfile` - сборка образа

### 2. Код Domain слоя
Открой `Auctions.Domain/Entities/Lot.cs`:
- Приватные конструкторы
- Фабричные методы
- Бизнес-логика в entity (PlaceBid, Publish, Complete)
- Domain-driven design

### 3. CQRS в Application
Открой `Auctions.Application/Commands/CreateLot/`:
- CreateLotCommand (что делаем)
- CreateLotCommandHandler (как делаем)
- CreateLotCommandValidator (FluentValidation)

### 4. FastEndpoints вместо Controllers
Открой `Auctions.API/Endpoints/Lots/CreateLotEndpoint.cs`:
- Vertical Slice Architecture
- Минималистичный подход
- Встроенная валидация

### 5. Swagger документация
Открой http://localhost:5108/swagger и покажи:
- POST /api/lots - создание лота
- GET /api/lots - получение списка

### 6. Создать тестовый лот
В Swagger, POST /api/lots:
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

### 7. Получить список лотов
GET /api/lots - покажет созданный лот

### 8. База данных (Beekeeper Studio)
Подключись к PostgreSQL:
- Host: localhost
- Port: 5432
- User: postgres
- Password: password
- Database: auctionhub

Покажи таблицы `Lots` и `Bids`.

---

## Что сказать

### Введение
"Проект AuHub - это современная платформа для онлайн-аукционов, построенная на микросервисной архитектуре."

### Технологии
"Использую .NET 10 LTS - самую свежую версию платформы с поддержкой до 2028 года. FastEndpoints вместо традиционных Controllers для лучшей производительности. PostgreSQL как основная база данных. Docker для контейнеризации."

### Архитектура
"Применяю Clean Architecture с разделением на слои:
- Domain - бизнес-логика и entities
- Application - CQRS паттерн с командами и запросами
- Infrastructure - работа с БД через EF Core
- API - FastEndpoints для обработки HTTP запросов"

### CQRS
"CQRS - это разделение команд (изменение данных) и запросов (чтение данных). Это упрощает код и делает систему более масштабируемой."

### Domain-Driven Design
"В Domain слое применяю DDD подход - бизнес-логика находится прямо в entities. Например, метод PlaceBid проверяет что лот активен, ставка выше текущей цены, и аукцион не завершен."

### Docker
"Весь проект контейнеризирован. Это значит что для запуска нужна только одна команда: docker-compose up. Не нужно устанавливать .NET SDK или PostgreSQL - все работает в изолированных контейнерах."

### Миграция
"Это полная переработка старого проекта. Раньше был монолит на .NET 9 с Controllers. Теперь - микросервисная архитектура на .NET 10 с FastEndpoints и Docker."

### Что дальше
"Следующие этапы:
- Микросервис Users для аутентификации через JWT
- Микросервис Notifications для real-time уведомлений через SignalR
- Background Services для автоматического завершения аукционов
- API Gateway для единой точки входа"

---

## Ответы на возможные вопросы

### Почему микросервисы?
"Микросервисы позволяют независимо развертывать и масштабировать части системы. Если нужно обновить логику ставок - обновляю только сервис Auctions, не трогая Users или Notifications."

### Почему FastEndpoints?
"FastEndpoints быстрее традиционных Controllers, требует меньше boilerplate кода, и естественно поддерживает Vertical Slice Architecture - когда код организован по фичам, а не по слоям."

### Почему .NET 10?
"Это LTS релиз с поддержкой до 2028 года. Лучшая производительность, новые фичи C# 14, и это самый актуальный стек на данный момент."

### Как развернуть в продакшене?
"Docker Compose для разработки. Для продакшена - Kubernetes или Docker Swarm для оркестрации контейнеров, с автоматическим масштабированием и балансировкой нагрузки."

### Сколько времени заняла разработка?
"Архитектурная основа создана за один вечер - около 4 часов. Это демонстрирует силу современных инструментов и правильной архитектуры."

---

## Если что-то не работает

### API не запускается
```bash
docker-compose logs auctions-api
```

### PostgreSQL не работает
```bash
docker-compose restart postgres
```

### Пересобрать все
```bash
docker-compose down
docker-compose up -d --build
```

---

## Команды для демонстрации

```bash
# Показать запущенные контейнеры
docker-compose ps

# Показать логи API
docker-compose logs -f auctions-api

# Подключиться к PostgreSQL
docker-compose exec postgres psql -U postgres -d auctionhub

# Посмотреть таблицы
\dt

# Посмотреть лоты
SELECT * FROM "Lots";

# Выйти из psql
\q
```

---

## Структура для показа

```
AuHub/
├── src/Services/Auctions/
│   ├── Auctions.Domain/         ← DDD entities
│   ├── Auctions.Application/    ← CQRS
│   ├── Auctions.Infrastructure/ ← EF Core
│   └── Auctions.API/            ← FastEndpoints
├── docker-compose.yml           ← Оркестрация
├── Dockerfile                   ← Сборка образа
└── README.md                    ← Документация
```

---

**Удачи на демонстрации! 🎓**

Создано: 08.04.2026  
Автор: jinxinzero7  
Проект: https://github.com/jinxinzero7/AuHub
