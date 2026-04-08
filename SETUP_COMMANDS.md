# Шаблон для создания проектов после установки .NET SDK

## Команды для создания проектов:

```bash
# Перейти в корень решения
cd C:\Users\niko\Desktop\AuctionHub.v2

# Создать solution
dotnet new sln -n AuctionHub

# Domain проект (Class Library)
cd src/Services/Auctions/Auctions.Domain
dotnet new classlib -f net10.0
dotnet sln ../../../../AuctionHub.sln add Auctions.Domain.csproj

# Application проект (Class Library)
cd ../Auctions.Application
dotnet new classlib -f net10.0
dotnet sln ../../../../AuctionHub.sln add Auctions.Application.csproj
dotnet add reference ../Auctions.Domain/Auctions.Domain.csproj
dotnet add package FluentValidation

# Infrastructure проект (Class Library)
cd ../Auctions.Infrastructure
dotnet new classlib -f net10.0
dotnet sln ../../../../AuctionHub.sln add Auctions.Infrastructure.csproj
dotnet add reference ../Auctions.Domain/Auctions.Domain.csproj
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL

# API проект (Web API)
cd ../Auctions.API
dotnet new web -f net10.0
dotnet sln ../../../../AuctionHub.sln add Auctions.API.csproj
dotnet add reference ../Auctions.Application/Auctions.Application.csproj
dotnet add reference ../Auctions.Infrastructure/Auctions.Infrastructure.csproj
dotnet add package FastEndpoints
dotnet add package FastEndpoints.Swagger

# Вернуться в корень
cd ../../../../

# Собрать решение
dotnet build
```

## После создания проектов:

1. Удалить автоматически созданные Class1.cs файлы
2. Запустить Docker с PostgreSQL: `docker-compose up -d postgres`
3. Создать миграции: `dotnet ef migrations add InitialCreate -p src/Services/Auctions/Auctions.Infrastructure -s src/Services/Auctions/Auctions.API`
4. Применить миграции: `dotnet ef database update -p src/Services/Auctions/Auctions.Infrastructure -s src/Services/Auctions/Auctions.API`
