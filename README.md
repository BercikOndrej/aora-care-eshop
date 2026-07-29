# Aora Care

E-shop platform targeting the Czech/Slovak market. Monorepo with a C#/.NET backend and a TypeScript/React frontend.

## Structure

- `backend/` — .NET solution (Api, Domain, Application, Infrastructure + tests)
- `frontend/` — React/TypeScript app
- `Psql-db/` — local Postgres via Docker Compose

See [`CLAUDE.md`](./CLAUDE.md) for architecture conventions and [`eshop-tech-stack.md`](./eshop-tech-stack.md) for the full tech stack.

## Getting started

```bash
# Start Postgres locally
docker compose -f Psql-db/docker-compose.yml up -d

# Backend dev server
dotnet watch run --project backend/src/AoraCare.Api

# Frontend dev server
cd frontend && npm install && npm run dev
```

> More detailed setup and commands to follow.
