FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy shared library
COPY ["src/Shared/AuHub.Shared/AuHub.Shared.csproj", "src/Shared/AuHub.Shared/"]

# Copy Payment service projects
COPY ["src/Services/Payment/Payment.API/Payment.API.csproj", "src/Services/Payment/Payment.API/"]
COPY ["src/Services/Payment/Payment.Application/Payment.Application.csproj", "src/Services/Payment/Payment.Application/"]
COPY ["src/Services/Payment/Payment.Domain/Payment.Domain.csproj", "src/Services/Payment/Payment.Domain/"]
COPY ["src/Services/Payment/Payment.Infrastructure/Payment.Infrastructure.csproj", "src/Services/Payment/Payment.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "src/Services/Payment/Payment.API/Payment.API.csproj"

# Copy everything else
COPY . .

# Build
WORKDIR "/src/src/Services/Payment/Payment.API"
RUN dotnet build "Payment.API.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "Payment.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Payment.API.dll"]
