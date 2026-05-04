# Руководство по тестированию JWT авторизации

## Быстрый старт

```bash
cd C:\Users\niko\Desktop\workSpace\projects\auhub\AuHub
docker compose up -d
```

Подожди 15-20 секунд, затем открой: http://localhost:5108/swagger

---

## Тестовый сценарий для демонстрации

### 1. Регистрация Admin пользователя

**POST /api/auth/register**

```json
{
  "email": "admin@auhub.com",
  "password": "Admin123!",
  "name": "Admin User",
  "role": 1
}
```

**Результат:** Получишь `accessToken` и `refreshToken`

**Важно:** `role: 1` = Admin, `role: 0` = User

---

### 2. Авторизация в Swagger

1. Скопируй `accessToken` из ответа
2. Нажми кнопку **Authorize** в Swagger UI
3. Введи: `Bearer {accessToken}`
4. Нажми **Authorize**

---

### 3. Создать лот (только Admin)

**POST /api/lots**

```json
{
  "title": "Vintage Rolex Watch",
  "description": "Rare Rolex Submariner from 1965",
  "startingPrice": 5000,
  "startTime": "2026-05-04T21:00:00Z",
  "endTime": "2026-05-04T21:10:00Z"
}
```

**Результат:** Лот создан, получишь `lotId`

**Примечание:** `sellerId` автоматически берется из JWT токена

---

### 4. Опубликовать лот (только владелец)

**POST /api/lots/{lotId}/publish**

Без body, просто вставь `lotId` из предыдущего шага.

**Результат:** Лот активен и принимает ставки

---

### 5. Регистрация обычного пользователя

**POST /api/auth/register**

```json
{
  "email": "user@auhub.com",
  "password": "User123!",
  "name": "Regular User",
  "role": 0
}
```

**Результат:** Получишь новый `accessToken` для User

---

### 6. Авторизоваться как User

1. Скопируй новый `accessToken`
2. Нажми **Authorize** в Swagger
3. Введи: `Bearer {новый_accessToken}`
4. Нажми **Authorize**

---

### 7. Сделать ставку (любой авторизованный пользователь)

**POST /api/lots/{lotId}/bids**

```json
{
  "amount": 5500
}
```

**Результат:** Ставка принята, `currentPrice` = 5500

**Примечание:** `bidderId` автоматически берется из JWT токена

---

### 8. Сделать ещё одну ставку

**POST /api/lots/{lotId}/bids**

```json
{
  "amount": 6000
}
```

**Результат:** Ставка принята, `currentPrice` = 6000

---

### 9. Посмотреть историю ставок (публичный endpoint)

**GET /api/lots/{lotId}/bids**

**Результат:** Список всех ставок, отсортированных по времени

---

## Проверка защиты endpoints

### ❌ User пытается создать лот (должно быть 403 Forbidden)

1. Авторизуйся как User (role: 0)
2. Попробуй **POST /api/lots**
3. **Результат:** 403 Forbidden - "User does not have the required role(s): Admin"

---

### ❌ Неавторизованный пользователь делает ставку (должно быть 401 Unauthorized)

1. Нажми **Authorize** → **Logout**
2. Попробуй **POST /api/lots/{lotId}/bids**
3. **Результат:** 401 Unauthorized

---

### ❌ User пытается опубликовать чужой лот (должно быть 403 Forbidden)

1. Авторизуйся как Admin и создай лот
2. Авторизуйся как User
3. Попробуй **POST /api/lots/{lotId}/publish**
4. **Результат:** 403 Forbidden - "You are not the owner of this lot"

---

## Что реализовано

✅ **JWT авторизация** (30 минут access token, 30 дней refresh token)
✅ **Роли** (Admin, User)
✅ **Защита endpoints:**
  - POST /api/lots - только Admin
  - POST /api/lots/{id}/publish - только владелец
  - POST /api/lots/{id}/complete - только владелец
  - POST /api/lots/{id}/cancel - только владелец
  - POST /api/lots/{id}/bids - любой авторизованный пользователь

✅ **Валидация паролей:**
  - Минимум 8 символов
  - Минимум 1 заглавная буква
  - Минимум 1 строчная буква
  - Минимум 1 цифра
  - Минимум 1 спецсимвол

✅ **Автоматическое извлечение userId из JWT токена**
✅ **Проверка владельца лота** в handlers
✅ **BCrypt** для хеширования паролей (12 rounds)

---

## Endpoints

### Auth (публичные)
- POST /api/auth/register - регистрация
- POST /api/auth/login - вход
- POST /api/auth/refresh - обновление токена

### Lots
- POST /api/lots - создать лот [Admin only]
- GET /api/lots - список лотов [публичный]
- GET /api/lots/{id} - детали лота [публичный]
- POST /api/lots/{id}/publish - опубликовать [Owner only]
- POST /api/lots/{id}/complete - завершить [Owner only]
- POST /api/lots/{id}/cancel - отменить [Owner only]

### Bids
- POST /api/lots/{id}/bids - сделать ставку [Authenticated]
- GET /api/lots/{id}/bids - история ставок [публичный]

---

## Технические детали

**JWT Secret:** `AuHub-Super-Secret-Key-2026-Min-32-Chars-Long-For-Security!`
**Issuer:** `AuHub`
**Audience:** `AuHub-Users`

**Access Token:** 30 минут
**Refresh Token:** 30 дней

**Password Hashing:** BCrypt (12 rounds)

---

## Если что-то не работает

```bash
# Пересобрать и перезапустить
docker compose down
docker compose up -d --build

# Посмотреть логи
docker compose logs -f auctions-api

# Проверить статус
docker compose ps
```

---

**Создано:** 04.05.2026  
**Автор:** jinxinzero7  
**Проект:** AuHub - JWT Authentication Implementation
