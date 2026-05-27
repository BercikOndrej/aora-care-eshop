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
  Api/          # Minimal API endpoint groups, mapped in Program.cs
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

- **Minimal API** over MVC controllers — endpoints are registered as groups in `*Endpoints.cs` files.
- **EF Core configuration** lives in `IEntityTypeConfiguration<T>` classes, not data annotations.
- **FluentValidation** validators are registered via DI and called explicitly in endpoint handlers or via a filter.
- **Zustand** is used only for client-owned state (cart contents, logged-in session); all server state goes through TanStack Query.
- **Zod** schemas are the single source of truth for form validation; they are reused in API client typings where possible.

---

## Commands

> Commands will be added here once the project is scaffolded. Expect:
>
> - `docker compose up` — start Postgres locally
> - `dotnet watch run` — backend dev server with hot reload
> - `dotnet ef migrations add <Name>` — add an EF Core migration
> - `npm run dev` — frontend dev server (Vite)
> - `npm run lint` / `npm run typecheck` — frontend quality checks
> - `dotnet test` — backend unit/integration tests
