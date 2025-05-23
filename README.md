# seagull-backend

## DOCS

### API
API docs: http://localhost:8080/scalar
> or any other domain you use

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
docker compose up -d --build
```

### Backend reload

```bash
docker compose up -d --build seagull-backend
```