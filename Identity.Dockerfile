FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy shared library
COPY ["src/Shared/AuHub.Shared/AuHub.Shared.csproj", "src/Shared/AuHub.Shared/"]

# Copy Identity service projects
COPY ["src/Services/Identity/Identity.API/Identity.API.csproj", "src/Services/Identity/Identity.API/"]
COPY ["src/Services/Identity/Identity.Application/Identity.Application.csproj", "src/Services/Identity/Identity.Application/"]
COPY ["src/Services/Identity/Identity.Domain/Identity.Domain.csproj", "src/Services/Identity/Identity.Domain/"]
COPY ["src/Services/Identity/Identity.Infrastructure/Identity.Infrastructure.csproj", "src/Services/Identity/Identity.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "src/Services/Identity/Identity.API/Identity.API.csproj"

# Copy everything else
COPY . .

# Build
WORKDIR "/src/src/Services/Identity/Identity.API"
RUN dotnet build "Identity.API.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "Identity.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Identity.API.dll"]
