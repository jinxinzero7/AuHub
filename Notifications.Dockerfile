FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy shared library
COPY ["src/Shared/AuHub.Shared/AuHub.Shared.csproj", "src/Shared/AuHub.Shared/"]

# Copy Notifications service projects
COPY ["src/Services/Notifications/Notifications.API/Notifications.API.csproj", "src/Services/Notifications/Notifications.API/"]
COPY ["src/Services/Notifications/Notifications.Application/Notifications.Application.csproj", "src/Services/Notifications/Notifications.Application/"]
COPY ["src/Services/Notifications/Notifications.Domain/Notifications.Domain.csproj", "src/Services/Notifications/Notifications.Domain/"]
COPY ["src/Services/Notifications/Notifications.Infrastructure/Notifications.Infrastructure.csproj", "src/Services/Notifications/Notifications.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "src/Services/Notifications/Notifications.API/Notifications.API.csproj"

# Copy everything else
COPY . .

# Build
WORKDIR "/src/src/Services/Notifications/Notifications.API"
RUN dotnet build "Notifications.API.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "Notifications.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Notifications.API.dll"]
