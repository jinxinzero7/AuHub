# Инструкция по развертыванию AuHub в колледже

## Требования

- Docker Desktop (или Docker Engine + Docker Compose)
- Git

## Быстрый старт (3 команды!)

### 1. Клонировать репозиторий
```bash
git clone https://github.com/jinxinzero7/AuHub.git
cd AuHub
```

### 2. Запустить все сервисы
```bash
docker-compose up -d
```

Эта команда:
- Скачает PostgreSQL образ
- Соберет .NET приложение
- Запустит PostgreSQL
- Запустит API
- Применит миграции автоматически

### 3. Открыть приложение
- **API:** http://localhost:5108
- **Swagger:** http://localhost:5108/swagger

---

## Применение миграций (если нужно)

Если миграции не применились автоматически:

```bash
docker-compose exec auctions-api dotnet ef database update
```

---

## Полезные команды

### Посмотреть логи
```bash
docker-compose logs -f auctions-api
```

### Остановить все
```bash
docker-compose down
```

### Остановить и удалить данные
```bash
docker-compose down -v
```

### Пересобрать после изменений
```bash
docker-compose up -d --build
```

### Подключиться к PostgreSQL
```bash
docker-compose exec postgres psql -U postgres -d auctionhub
```

---

## Проверка работоспособности

### 1. Проверить что контейнеры запущены
```bash
docker-compose ps
```

Должны быть запущены:
- `auhub-postgres` (PostgreSQL)
- `auhub-api` (API)

### 2. Проверить API
```bash
curl http://localhost:5108/api/lots
```

### 3. Открыть Swagger
Открой в браузере: http://localhost:5108/swagger

---

## Тестирование через Swagger

1. Открой http://localhost:5108/swagger
2. Найди `POST /api/lots`
3. Нажми "Try it out"
4. Используй этот JSON:
```json
{
  "title": "Test Auction",
  "description": "Test Description",
  "startingPrice": 100,
  "startTime": "2026-04-09T10:00:00Z",
  "endTime": "2026-04-10T10:00:00Z",
  "sellerId": "123e4567-e89b-12d3-a456-426614174000"
}
```
5. Нажми "Execute"
6. Проверь `GET /api/lots` - должен вернуть созданный лот

---

## Troubleshooting

### Порт 5108 занят
Измени порт в `docker-compose.yml`:
```yaml
ports:
  - "5109:8080"  # Вместо 5108
```

### PostgreSQL не запускается
Проверь что порт 5432 свободен:
```bash
docker-compose down
docker-compose up -d
```

### API не подключается к БД
Проверь логи:
```bash
docker-compose logs auctions-api
```

---

## Архитектура

```
┌─────────────────┐
│   Swagger UI    │
│ localhost:5108  │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Auctions API   │
│   (Container)   │
│   FastEndpoints │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│   PostgreSQL    │
│   (Container)   │
│   Port: 5432    │
└─────────────────┘
```

---

## Что внутри контейнеров

### auhub-api
- .NET 10 Runtime
- Auctions.API
- Все зависимости (EF Core, FastEndpoints, etc.)
- Порт: 8080 (внутри) → 5108 (снаружи)

### auhub-postgres
- PostgreSQL 16 Alpine
- База данных: auctionhub
- Порт: 5432
- Данные сохраняются в Docker volume

---

## Преимущества Docker подхода

✅ Не нужно устанавливать .NET SDK  
✅ Не нужно устанавливать PostgreSQL  
✅ Работает одинаково на всех компьютерах  
✅ Одна команда для запуска  
✅ Легко откатиться к чистому состоянию  
✅ Изолированная среда  

---

## Для преподавателя

Проект демонстрирует:
- Микросервисную архитектуру
- Clean Architecture (Domain, Application, Infrastructure, API)
- CQRS паттерн
- FastEndpoints вместо Controllers
- Entity Framework Core 10
- Docker контейнеризацию
- PostgreSQL
- Swagger документацию

**Время развертывания:** 5-10 минут (зависит от скорости интернета)

**Требования к компьютеру:**
- 4GB RAM минимум
- 10GB свободного места
- Docker Desktop

---

Создано: 08.04.2026  
Автор: jinxinzero7  
Репозиторий: https://github.com/jinxinzero7/AuHub
