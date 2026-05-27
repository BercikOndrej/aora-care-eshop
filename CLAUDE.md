# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**aora-care** is an e-shop platform targeting the Czech/Slovak market. The repository will grow into a monorepo with a C# backend and a TypeScript/React frontend.

---

## Tech Stack

See [`eshop-tech-stack.md`](./eshop-tech-stack.md) for the full stack reference.

---

## Planned Architecture

### Backend structure (expected)

```
src/
  Api/          # MVC controllers, mapped via MapControllers() in Program.cs
  Domain/       # EF Core entities, value objects, domain logic
  Infrastructure/
    Data/       # DbContext, IEntityTypeConfiguration<T> configs, migrations
    Jobs/       # Hangfire job classes
    Email/      # Resend integration behind IEmailSender
    Payments/   # GoPay integration
  Application/  # Service classes, DTOs, FluentValidation validators
```

### Frontend structure (expected)

```
src/
  components/   # Shared UI built on shadcn/ui primitives
  features/     # Feature-sliced modules (cart, checkout, product, auth)
  store/        # Zustand stores (cart, session)
  api/          # TanStack Query hooks wrapping axios
  lib/          # Zod schemas, utilities
```

### Key architectural decisions

- **MVC Controllers** — endpoints live in `*Controller.cs` files under `src/Api/Controllers/`, registered via `MapControllers()` in `Program.cs`.
- **EF Core configuration** lives in `IEntityTypeConfiguration<T>` classes, not data annotations.
- **FluentValidation** validators are registered via DI and called explicitly in endpoint handlers or via a filter.
- **Zustand** is used only for client-owned state (cart contents, logged-in session); all server state goes through TanStack Query.
- **Zod** schemas are the single source of truth for form validation; they are reused in API client typings where possible.

---

## Commands

### Backend

```bash
# Start Postgres locally (from repo root)
docker compose -f Psql-db/docker-compose.yml up -d

# Backend dev server with hot reload
dotnet watch run --project backend/src/Api

# Add an EF Core migration
dotnet ef migrations add <Name> --project backend/src/Api

# Apply pending migrations
dotnet ef database update --project backend/src/Api

# Run all backend tests
dotnet test backend/AoraCare.sln
```

> EF tool is pinned in `backend/.config/dotnet-tools.json`.
> Run `dotnet tool restore` inside `backend/` after a fresh clone.

### Frontend

```bash
# Dev server (run from frontend/)
npm run dev

# Type-check + lint
npm run typecheck
npm run lint
```
