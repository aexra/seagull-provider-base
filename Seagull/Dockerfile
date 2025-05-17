# Базовый образ для сборки
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Копируем ТОЛЬКО .csproj файлы (для оптимизации кэша)
COPY Seagull.API/Seagull.API.csproj Seagull.API/
COPY Seagull.Core/Seagull.Core.csproj Seagull.Core/
COPY Seagull.Infrastructure/Seagull.Infrastructure.csproj Seagull.Infrastructure/

# Восстанавливаем зависимости
RUN dotnet restore "Seagull.API/Seagull.API.csproj"

# Копируем ВЕСЬ исходный код 
COPY . .

# Собираем и публикуем
WORKDIR "/src/Seagull.API"
RUN dotnet publish "Seagull.API.csproj" -c Release -o /app/publish

# Финальный образ
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Seagull.API.dll"]