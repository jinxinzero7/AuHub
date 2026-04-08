FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/Services/Auctions/Auctions.API/Auctions.API.csproj", "Auctions.API/"]
COPY ["src/Services/Auctions/Auctions.Application/Auctions.Application.csproj", "Auctions.Application/"]
COPY ["src/Services/Auctions/Auctions.Domain/Auctions.Domain.csproj", "Auctions.Domain/"]
COPY ["src/Services/Auctions/Auctions.Infrastructure/Auctions.Infrastructure.csproj", "Auctions.Infrastructure/"]

RUN dotnet restore "Auctions.API/Auctions.API.csproj"

# Copy everything else and build
COPY src/Services/Auctions/ .
WORKDIR "/src/Auctions.API"
RUN dotnet build "Auctions.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Auctions.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Auctions.API.dll"]
