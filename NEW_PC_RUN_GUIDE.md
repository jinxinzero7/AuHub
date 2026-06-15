# AuHub: запуск на новом ПК

Короткий путь для первого запуска проекта через Docker.

## 1. Что нужно установить

- Git
- Docker Desktop

## 2. Склонировать репозитории

Важно: frontend должен лежать рядом с backend и называться `auhub-frontend`.

```powershell
mkdir C:\Projects\auhub
cd C:\Projects\auhub

git clone https://github.com/jinxinzero7/AuHub.git AuHub
git clone https://github.com/jinxinzero7/AuHub-Frontend.git auhub-frontend
```

Итоговая структура:

```text
C:\Projects\auhub
  AuHub
  auhub-frontend
```

## 3. Создать `.env`

```powershell
cd C:\Projects\auhub\AuHub
copy .env.example .env
```

Открой `.env` и замени `change-me-*` значения. Для локального демо можно использовать:

```env
POSTGRES_USER=postgres
POSTGRES_PASSWORD=AuHub_Postgres_Local_2026!
JWT_SECRET=AuHubLocalJwtSecret_2026_This_Is_Long_Enough_For_Hmac_Sha256
INTERNAL_API_KEY=AuHubLocalInternalApiKey_2026_32PlusChars
RABBITMQ_USER=auhub
RABBITMQ_PASSWORD=AuHub_Rabbit_Local_2026!
MINIO_ROOT_USER=auhub-minio-admin
MINIO_ROOT_PASSWORD=AuHub_MinIO_Local_2026!
ADMIN_BOOTSTRAP_EMAIL=admin@auhub.local
ADMIN_BOOTSTRAP_PASSWORD=Admin123!Local
ADMIN_BOOTSTRAP_NAME=AuHub Admin
ADMIN_BOOTSTRAP_PHONE_NUMBER=+70000000000
ADMIN_BOOTSTRAP_NICKNAME=auhub_admin
ROBOKASSA_MERCHANT_LOGIN=
ROBOKASSA_PASSWORD1=
ROBOKASSA_PASSWORD2=
ROBOKASSA_PAYMENT_URL=https://auth.robokassa.ru/Merchant/Index.aspx
ROBOKASSA_CULTURE=ru
ROBOKASSA_IS_TEST=true
```

## 4. Запустить проект

```powershell
docker compose up -d --build
```

Первый запуск может занять несколько минут.

## 5. Проверить

```powershell
docker compose ps
```

Все основные сервисы должны быть `Up`, backend API и Gateway должны быть `healthy`.

Открыть:

- Frontend: http://localhost:3000
- Gateway health: http://localhost:5000/health
- Identity Swagger: http://localhost:5109/swagger
- Auctions Swagger: http://localhost:5108/swagger
- Payment Swagger: http://localhost:5111/swagger
- Notifications Swagger: http://localhost:5110/swagger

Admin для демо:

- email: `admin@auhub.local`
- password: `Admin123!Local`

## 6. Остановить

```powershell
docker compose down
```

## 7. Полностью сбросить базы

Осторожно: команда удалит Docker volumes проекта и все локальные данные.

```powershell
docker compose down -v
docker compose up -d --build
```

Если поменял `POSTGRES_PASSWORD` после первого запуска, старые volumes будут хранить старый пароль. Тогда используй старый пароль или сделай полный сброс через `docker compose down -v`.
