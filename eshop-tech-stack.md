# E-Shop Tech Stack

## Backend — C# / ASP.NET Core

| Package                     | Purpose                                           |
| --------------------------- | ------------------------------------------------- |
| **ASP.NET Core 9**          | REST API framework                                |
| **Entity Framework Core**   | ORM                                               |
| **Npgsql EF Core Provider** | PostgreSQL driver                                 |
| **FluentValidation**        | Request validation                                |
| **Hangfire**                | Background jobs (order processing, email sending) |

---

## Frontend — TypeScript / React

| Package                   | Purpose                      |
| ------------------------- | ---------------------------- |
| **Vite + React**          | App bundler & UI framework   |
| **TypeScript**            | Type safety                  |
| **Tailwind CSS**          | Utility-first styling        |
| **shadcn/ui**             | Component library            |
| **TanStack Query**        | Server state & data fetching |
| **Zustand**               | Client state (cart, session) |
| **React Hook Form + Zod** | Forms & schema validation    |

---

## Database

| Tool               | Purpose          |
| ------------------ | ---------------- |
| **PostgreSQL 16+** | Primary database |

---

## Authentication

Custom JWT implementation — stateless tokens, no third-party dependency.

---

## Payments

| Tool      | Purpose                                      |
| --------- | -------------------------------------------- |
| **GoPay** | Czech / Slovak market, local payment methods |

---

## Email (Transactional)

| Tool       | Purpose                            |
| ---------- | ---------------------------------- |
| **Resend** | Order confirmations, notifications |

---

## Infrastructure

| Tool                        | Purpose                        |
| --------------------------- | ------------------------------ |
| **DigitalOcean Droplet**    | VPS hosting                    |
| **Docker + Docker Compose** | Containerization               |
| **Nginx**                   | Reverse proxy, SSL termination |

---

## Domain, DNS & CDN

| Tool                     | Purpose                                               |
| ------------------------ | ----------------------------------------------------- |
| **Cloudflare**           | DNS, CDN, DDoS protection, free SSL                   |
| **Cloudflare R2**        | Product image storage (S3-compatible, no egress fees) |
| **Cloudflare Registrar** | Domain registration at wholesale price                |

---

## Dev & CI/CD

| Tool               | Purpose        |
| ------------------ | -------------- |
| **GitHub**         | Source control |
| **GitHub Actions** | CI/CD pipeline |

---

## Testing

### Backend — Unit Tests

| Tool                 | Purpose                     |
| -------------------- | --------------------------- |
| **xUnit**            | Test runner                 |
| **NSubstitute**      | Mocking                     |
| **FluentAssertions** | Readable assertions         |

Scope: Domain logic, Application services, FluentValidation validators.  
Project: `tests/AoraCare.UnitTests/`

---

### Frontend — Unit Tests

| Tool                      | Purpose                    |
| ------------------------- | -------------------------- |
| **Vitest**                | Vite-native test runner    |
| **React Testing Library** | Component behavior testing |
| **MSW**                   | Network-level API mocking  |

Scope: Zod schemas, Zustand stores, utility functions, critical components.  
Pattern: colocated `*.test.ts(x)` files next to source.

---

### E2E Tests

| Tool           | Purpose                                         |
| -------------- | ----------------------------------------------- |
| **Playwright** | Cross-browser, covers integration + happy paths |

Critical flows: auth, product catalog, cart, checkout.  
Location: `e2e/` at monorepo root.

---

### CI/CD — TODO

> GitHub Actions workflows to be defined once the project is scaffolded.
