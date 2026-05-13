FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy Gateway project
COPY ["src/Gateway/AuHub.Gateway/AuHub.Gateway.csproj", "src/Gateway/AuHub.Gateway/"]

# Restore dependencies
RUN dotnet restore "src/Gateway/AuHub.Gateway/AuHub.Gateway.csproj"

# Copy everything else
COPY . .

# Build
WORKDIR "/src/src/Gateway/AuHub.Gateway"
RUN dotnet build "AuHub.Gateway.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "AuHub.Gateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AuHub.Gateway.dll"]
