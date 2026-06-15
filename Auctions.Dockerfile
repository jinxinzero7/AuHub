FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy shared library
COPY ["src/Shared/AuHub.Shared/AuHub.Shared.csproj", "src/Shared/AuHub.Shared/"]

# Copy Auctions service projects
COPY ["src/Services/Auctions/Auctions.API/Auctions.API.csproj", "src/Services/Auctions/Auctions.API/"]
COPY ["src/Services/Auctions/Auctions.Application/Auctions.Application.csproj", "src/Services/Auctions/Auctions.Application/"]
COPY ["src/Services/Auctions/Auctions.Domain/Auctions.Domain.csproj", "src/Services/Auctions/Auctions.Domain/"]
COPY ["src/Services/Auctions/Auctions.Infrastructure/Auctions.Infrastructure.csproj", "src/Services/Auctions/Auctions.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "src/Services/Auctions/Auctions.API/Auctions.API.csproj"

# Copy everything else
COPY . .

# Build
WORKDIR "/src/src/Services/Auctions/Auctions.API"
RUN dotnet build "Auctions.API.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "Auctions.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Auctions.API.dll"]
