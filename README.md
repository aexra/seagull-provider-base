# seagull-backend

## Migrations

From `seagull-backend/`:
```bash
dotnet ef migrations add Initial --startup-project Seagull/Seagull.API --project Seagull/Seagull.Infrastructure
```

From `seagull-backend/Seagull/`:
```bash
dotnet ef migrations add Initial --startup-project Seagull.API --project Seagull.Infrastructure
```

To apply migrations use [Docker backend reload](#backend-reload)

## Docker

### Startup

```bash
docker compose up --build
```

### Backend reload

```bash
docker compose up --build seagull-backend
```