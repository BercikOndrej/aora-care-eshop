# AoraCare — Implementation Plan

E-shop for cosmetic products (CZ/SK market). ~16 products across 3–4 product lines.
Design target: luxury, natural, earthy. Runs on a single VPS via Docker Compose + Nginx.

## Locked decisions

- **Payment at launch:** GoPay online payment (card / bank button) from day one.
- **Customer accounts:** OPEN — guest checkout for MVP, but the data model is designed so
  accounts can be added later with no schema rework (see Order model below).
- **Packeta:** pickup-point selection at checkout in MVP; automatic parcel/label creation via
  Packeta API is deferred to Phase 2 (labels created manually in admin at first).
- **MVP shape:** "Customer-complete, back-office manual" (Option C).

## New dependencies / wiring to add (flagged per repo conventions)

- **Frontend:** `react-router` — not yet in `package.json`; required for multi-page site.
- **Backend:** FluentValidation is referenced (`Api.csproj`) but **not yet wired** in
  `Program.cs` (no `AddValidatorsFromAssembly` / auto-validation). Wire in F0.
- **Email:** Resend (HTTP API behind `IEmailSender`). **Payments:** GoPay SDK/HTTP client.
- **Images:** Cloudflare R2 (S3-compatible) — Phase 2; MVP can serve images from `wwwroot`.

---

## Data model (Phase 1)

> Money stored as **integer minor units** (`long PriceMinor`, e.g. haléře) to avoid float
> rounding and because GoPay expects amounts in minor units. Currency fixed to `CZK` for MVP.

- **ProductLine**:
  - `Id`, `Name`, `Slug`, `Description`, `SortOrder`, `IsActive`, `CreatedAt`.
- **Product**:
  - `Id`, `LineId` (FK), `Name`, `Slug`, `Description`, `PriceMinor`, `Currency`,
    `StockCount`, `ImageUrl`, `IsActive`, `SortOrder`, `CreatedAt`, `UpdatedAt`.
- **Order**:
  - `Id`, `OrderNumber` (human-readable), `Status` (enum), `CustomerEmail`,
    `CustomerName`, `CustomerPhone`, `TotalMinor`, `Currency`, `DeliveryMethod`,
    `PacketaPointId`, `PacketaPointName`, `ShippingAddress?` (home delivery), `CreatedAt`,
    `PaidAt?`, **`UserId?` (nullable, reserved for future accounts)**.
- **OrderItem**:
  - `Id`, `OrderId` (FK), `ProductId` (FK), `ProductNameSnapshot`,
    `UnitPriceMinor`, `Quantity`. (Name + price snapshotted so historical orders stay correct.)
- **Payment**:
  - `Id`, `OrderId` (FK), `GoPayPaymentId`, `Status`, `AmountMinor`,
    `RawCallbackJson`, `CreatedAt`.
- **AdminUser**:
  - `Id`, `Email`, `PasswordHash`, `CreatedAt`. (Single seeded admin for MVP.)

**Order status flow:** `AwaitingPayment → Paid → Shipped → Delivered`, plus `Failed`,
`Cancelled`. Stock is **decremented when payment is confirmed** (in the GoPay callback job),
not at order creation — availability is validated at creation with optimistic concurrency on
`Product.StockCount` to guard against oversell.

---

## Phase 1 — MVP (ordered, vertical slices)

Each slice is end-to-end (DB → API → UI where relevant) and ships **with unit tests**.

### F0 — Foundations (cross-cutting)

- Wire FluentValidation auto-validation + register validators from assembly.
- Add `react-router` and define routes: `/`, `/products`, `/products/:slug`, `/cart`,
  `/checkout`, `/order/:orderNumber`, `/admin/*`.
- Global `ProblemDetails` error handling on the API; typed axios error mapping on FE.
- **Tests:** validation pipeline returns 400 + ProblemDetails on invalid input.

### F1 — Catalog data model + seed

- `Product` + `ProductLine` entities, `IEntityTypeConfiguration<T>` classes, initial migration.
- Seed 3–4 lines + 16 products via migration (per Problem.md: new products added through migrations).
- **Tests:** config maps slugs unique; seed migration applies cleanly.

### F2 — Public catalog API

- `GET /api/products` with `?line=<slug>&search=<name>` (filter by line, search by name).
- `GET /api/lines` for the filter UI.
- Projection to DTOs (no entity leakage); only `IsActive` products.
- **Tests:** filter by line, case-insensitive name search, inactive products excluded.

### F3 — Product detail API

- `GET /api/products/{slug}` → detail DTO (404 ProblemDetails when missing/inactive).
- **Tests:** found / not-found / inactive cases.

### F4 — Landing page (FE)

- Hero + brand story + featured lines. Luxury/natural/earthy visual language.
- **Tests:** renders sections; featured lines link to catalog.

### F5 — Product list page (FE)

- TanStack Query list, line filter chips, search box (debounced). Zod-typed API responses.
- **Tests (Vitest + MSW):** renders products, filter calls API with `line`, search debounce.

### F6 — Product detail page (FE)

- Detail view + "Add to cart". Out-of-stock disables add.
- **Tests:** renders detail, add-to-cart dispatches to store, OOS disabled.

### F7 — Cart (FE, client-only)

- Zustand store: add/remove/update qty, line total, persisted to `localStorage`.
- **Tests:** add/merge same product, qty update, remove, total recompute, persistence.

### F8 — Order domain model

- `Order`, `OrderItem`, `Payment` entities + configs + migration. Status enum + `OrderNumber`.
- **Tests:** total = sum(items); status transitions guarded.

### F15 — Admin auth

- `AdminUser` entity + seeded admin (hashed password, e.g. PBKDF2/BCrypt).
- `POST /api/admin/login` → JWT. Admin endpoints require `[Authorize]`.
- **Tests:** correct/incorrect password, token validates, protected route rejects anon.

### F16 — Admin product & stock management

- Admin CRUD-update: edit description, price, **stock count**, active flag (FluentValidation).
- Admin UI under `/admin/products`.
- **Tests:** validator rules (price ≥ 0, stock ≥ 0), update persists.

### F17 — Admin order management

- `GET /api/admin/orders` (list + status), `PATCH` status (e.g. mark Shipped/Delivered).
- Admin UI under `/admin/orders` — this is where labels are created manually pre-Phase 2.
- **Tests:** list pagination, status update guarded by allowed transitions.

### F12 — Packeta pickup-point selection (FE + persist)

- Integrate Packeta widget; capture `pointId` + `pointName` into checkout state.
- **Tests:** selecting a point stores id/name; checkout blocked until a point chosen (for pickup).

### F9 — Checkout form (FE)

- RHF + Zod: customer name/email/phone, delivery method, (pickup point from F12).
- **Tests:** Zod schema validation, required fields, submit payload shape.

### F10 — Create-order API

- `POST /api/orders`: validate stock + recompute totals server-side (never trust client prices),
  create `Order` in `AwaitingPayment`. Optimistic concurrency on stock.
- **Tests:** total recomputed from DB prices, OOS rejected, order persisted.

### F13 — GoPay payment

- Create GoPay payment for order, return redirect URL. Webhook/callback endpoint → on `PAID`:
  mark order `Paid`, decrement stock, enqueue confirmation email. **Idempotent** (handle
  duplicate callbacks via `GoPayPaymentId` + status check).
- **Tests:** callback marks paid once, duplicate callback is a no-op, failed payment → `Failed`.

### F11 — Order confirmation email (Resend)

- `IEmailSender` (Resend impl) + Hangfire job enqueued after payment confirmation. Retry-safe.
- **Tests:** job renders order summary, sender called once, failure retried not lost.

### F-DEPLOY — Production deploy

- Prod `docker-compose` (api + postgres + nginx), Nginx reverse proxy + SSL via Cloudflare,
  Hangfire dashboard gated behind admin auth in prod (currently Dev-only — `Program.cs:64`).

---

## Phase 2 (deferred)

- **F14** — Automatic Packeta parcel/label creation via API (Hangfire job on `Paid`).
- **Customer accounts** — registration/login (reuse JWT), order history via `Order.UserId`,
  password reset, bot-prevention on registration (the optional features in Problem.md).
- **Cloudflare R2** image hosting + admin image upload.
- E2E (Playwright): auth, catalog, cart, checkout happy paths.
- CI/CD: GitHub Actions (build, test, deploy).

---

## Open questions

1. **Customer accounts at launch?** Currently guest-only; model is account-ready. Decide before
   F9/F10 finalize the checkout UX.
2. **Home delivery in addition to Packeta pickup?** Affects `ShippingAddress` requiredness and
   delivery-method options in F9.
3. **VAT / invoicing** — does the shop need to show/store VAT and issue invoices at MVP?

## Key risks

- **Oversell under concurrency** — mitigated by stock validation + optimistic concurrency (F10/F13).
- **GoPay webhook reliability/idempotency** — duplicate or out-of-order callbacks (F13).
- **Price tampering** — totals always recomputed server-side from DB (F10).
- **Email failures blocking orders** — isolated in retry-safe Hangfire job, never inline (F11).
