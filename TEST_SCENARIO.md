# Тестовый сценарий для демонстрации AuHub

## Запуск проекта

```bash
cd C:\Users\niko\Desktop\workSpace\projects\auhub\AuHub
docker compose up -d --build
```

Подожди 2-3 минуты пока все запустится.

Проверь статус:
```bash
docker compose ps
```

Swagger UI: http://localhost:5108/swagger

---

## Доступные Endpoints (10 штук)

1. **POST /api/lots** - создать лот
2. **GET /api/lots** - список лотов (с пагинацией)
3. **GET /api/lots/{id}** - детали лота
4. **POST /api/lots/{id}/publish** - опубликовать лот
5. **POST /api/lots/{id}/bids** - сделать ставку
6. **GET /api/lots/{id}/bids** - история ставок
7. **POST /api/lots/{id}/complete** - завершить лот вручную
8. **POST /api/lots/{id}/cancel** - отменить лот
9. **Background Service** - автозавершение аукционов
10. **Пагинация** - page/pageSize параметры

---

## Сценарий демонстрации (полный цикл аукциона)

### 1. Создать лот (Draft)

**POST /api/lots**

```json
{
  "title": "Vintage Rolex Watch",
  "description": "Rare Rolex Submariner from 1965 in excellent condition",
  "startingPrice": 5000,
  "startTime": "2026-05-04T19:00:00Z",
  "endTime": "2026-05-04T19:05:00Z"
}
```

**Результат:** Получишь `lotId` (например: `a1b2c3d4-...`)

**Примечание:** `sellerId` автоматически устанавливается (в будущем будет из JWT токена)

---

### 2. Опубликовать лот (Draft → Active)

**POST /api/lots/{lotId}/publish**

Без body, просто вставь `lotId` из предыдущего шага.

**Результат:** Лот теперь активен и принимает ставки.

---

### 3. Посмотреть список активных лотов

**GET /api/lots?onlyActive=true**

**Результат:** Увидишь созданный лот в статусе "Active".

---

### 4. Посмотреть детали лота

**GET /api/lots/{lotId}**

**Результат:** Полная информация о лоте + список ставок (пока пустой).

---

### 5. Сделать первую ставку

**POST /api/lots/{lotId}/bids**

```json
{
  "amount": 5500
}
```

**Результат:** Ставка принята, `currentPrice` = 5500.

**Примечание:** `bidderId` генерируется автоматически (в будущем будет из JWT токена)

---

### 6. Сделать вторую ставку (выше)

**POST /api/lots/{lotId}/bids**

```json
{
  "amount": 6000
}
```

**Результат:** Ставка принята, `currentPrice` = 6000.

---

### 7. Попробовать сделать ставку ниже текущей (должна отклониться)

**POST /api/lots/{lotId}/bids**

```json
{
  "amount": 5800
}
```

**Результат:** Ошибка 400 - "Bid amount must be higher than current price".

---

### 8. Посмотреть историю ставок

**GET /api/lots/{lotId}/bids**

**Результат:** Список всех ставок (2 штуки), отсортированных по времени (новые сверху).

---

### 9. Сделать третью ставку

**POST /api/lots/{lotId}/bids**

```json
{
  "amount": 7000
}
```

**Результат:** Ставка принята, `currentPrice` = 7000.

---

### 10. Завершить лот вручную (опционально)

Если не хочешь ждать автозавершения, можешь завершить лот вручную:

**POST /api/lots/{lotId}/complete**

Без body, просто вставь `lotId`.

**Результат:** 
- Лот завершён вручную
- `status` = "Completed"
- `finalPrice` = текущая цена

---

### 11. Проверить что аукцион завершён

**GET /api/lots/{lotId}**

**Результат:** 
- `status` = "Completed"
- `currentPrice` = 7000 (финальная цена)
- `bidsCount` = 3

---

### 12. Попробовать сделать ставку на завершённый аукцион (должна отклониться)

**POST /api/lots/{lotId}/bids**

```json
{
  "amount": 8000
}
```

**Результат:** Ошибка 400 - "Lot is not active".

---

## Дополнительные функции

### Отмена лота

**POST /api/lots/{lotId}/cancel**

Без body. Отменяет лот (Draft или Active → Cancelled).

**Результат:** Лот отменён, больше нельзя делать ставки.

---

### Пагинация списка лотов

**GET /api/lots?page=1&pageSize=5&onlyActive=true**

Параметры:
- `page` - номер страницы (по умолчанию 1)
- `pageSize` - размер страницы (по умолчанию 10)
- `onlyActive` - только активные лоты (по умолчанию false)

**Результат:**
```json
{
  "success": true,
  "lots": [...],
  "page": 1,
  "pageSize": 5,
  "totalCount": 15,
  "totalPages": 3
}
```

---

### Автоматическое завершение аукционов

Если не завершаешь лот вручную, **Background Service** автоматически завершит его через 1-2 минуты после `endTime`.

Проверяет каждую минуту все активные лоты и завершает истёкшие.

Логи можно посмотреть:
```bash
docker compose logs -f auctions-api
```

---

## Что показывать преподавателю

### 1. Архитектура (VSCode/VSCodium)
- Clean Architecture (Domain, Application, Infrastructure, API)
- CQRS (Commands/Queries разделены)
- Domain-Driven Design (бизнес-логика в Lot entity)
- FastEndpoints вместо Controllers

### 2. Код
- `Lot.cs` - Domain entity с инкапсуляцией и бизнес-логикой
- `PlaceBidCommandHandler.cs` - CQRS handler
- `PlaceBidEndpoint.cs` - FastEndpoint
- `AuctionCompletionService.cs` - Background Service

### 3. Функциональность (Swagger)
- Полный цикл аукциона (создание → публикация → ставки → завершение)
- Валидация (нельзя сделать ставку ниже текущей)
- История ставок
- Автоматическое завершение аукционов (Background Service)
- Ручное завершение и отмена лотов
- Пагинация списка лотов

### 4. Технологии
- .NET 10 (LTS до 2028)
- PostgreSQL 16
- EF Core 10
- FastEndpoints
- Docker + Docker Compose
- Background Services

---

## Если что-то не работает

### API не запускается
```bash
docker compose logs auctions-api
```

### PostgreSQL не работает
```bash
docker compose restart postgres
```

### Пересобрать всё
```bash
docker compose down
docker compose up -d --build
```

---

## Ключевые моменты для защиты

1. **Микросервисная архитектура** - сейчас один сервис (Auctions), в планах Users и Notifications
2. **Clean Architecture** - чёткое разделение слоёв, зависимости направлены внутрь
3. **CQRS** - команды изменяют состояние, запросы читают данные
4. **DDD** - бизнес-логика в domain entities (PlaceBid, Publish, Complete, Cancel)
5. **Background Services** - автоматическое завершение аукционов
6. **Docker** - вся инфраструктура в контейнерах, запуск одной командой
7. **FastEndpoints** - современная альтернатива Controllers, лучше производительность
8. **Пагинация** - масштабируемость для больших списков данных

---

**Удачи на демонстрации! 🎓**

Создано: 04.05.2026  
Обновлено: 04.05.2026 (добавлены Complete, Cancel, Pagination)
