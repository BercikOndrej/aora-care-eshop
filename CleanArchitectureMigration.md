# Migrace `aoraCareApi` na Clean Architecture se 4 samostatnými projekty

> Návod k ruční implementaci. Navazuje na sekce **Pravidlo toku dat** a **Od konvence k
> vynucení: rozdělení na samostatné projekty** v `CSharp/ASP.NET Core.md`.

## 1. Proč a co se mění

Dnes je `Controllers/`, `Application/`, `Domain/`, `Infrastructure/` jen 4 složky v jednom
`aoraCareApi.csproj`. Směr závislostí (`Controllers → Application → Domain ← Infrastructure`)
hlídá jen disciplína v `using` — a už je porušený:

- `Application/Services/ProductService.cs` a `CategoryService.cs` injectují
  `Infrastructure.Data.AppDbContext` přímo do konstruktoru a volají EF Core
  (`_db.Products.Add(...)`, `.AsNoTracking()`, `.SaveChangesAsync()`) uvnitř byznys logiky.
  → `Application` fakticky závisí na `Infrastructure`, opačně než by mělo.
- `Application/Services/Interfaces/ICategoryService.cs` má nepoužívaný
  `using Microsoft.AspNetCore.Mvc;` — únik HTTP vrstvy do Application.

Cíl: rozdělit na **4 samostatné `.csproj`**, aby špatný `using` přestal jít **zkompilovat**,
místo aby ho jen odchytil code review nebo `ArchUnitNET` test v CI.

## 2. Cílová struktura

```
backend/
├── aoraCare.slnx
├── Directory.Build.props                — sdílené TargetFramework/Nullable/ImplicitUsings
├── aoraCareApi.Domain/                   — žádný PackageReference, žádný ProjectReference
│   ├── aoraCareApi.Domain.csproj
│   ├── Product.cs, Category.cs, Order.cs, OrderItem.cs, Payment.cs, Review.cs, ProductVariant.cs
│   ├── Enums/
│   ├── Common/SlugHelper.cs
│   └── Repositories/ICategoryRepository.cs, IProductRepository.cs, IUnitOfWork.cs   (nové)
├── aoraCareApi.Application/              — ProjectReference: jen Domain
│   ├── aoraCareApi.Application.csproj
│   ├── Dtos/
│   ├── Services/ (IProductService.cs, ProductService.cs, ICategoryService.cs, CategoryService.cs)
│   └── DependencyInjection.cs            (nové — AddApplication)
├── aoraCareApi.Infrastructure/           — ProjectReference: jen Domain (NIKDY Application); EF Core + Npgsql
│   ├── aoraCareApi.Infrastructure.csproj
│   ├── Data/AppDbContext.cs, Data/Configurations/, Data/Migrations/
│   ├── Data/Repositories/CategoryRepository.cs, ProductRepository.cs   (nové)
│   ├── Data/UnitOfWork.cs                (nové)
│   └── DependencyInjection.cs            (nové — AddInfrastructure)
├── aoraCareApi/                          — Sdk.Web, Api = composition root
│   ├── aoraCareApi.csproj                — ProjectReference: Application + Infrastructure
│   ├── Controllers/, Program.cs, Properties/, appsettings*.json
└── aoraCareApi.Tests/                    — nové, ProjectReference: Application (tranzitivně Domain)
    └── aoraCareApi.Tests.csproj          — xUnit + Moq
```

| Projekt | Smí referencovat | Typický obsah |
| --- | --- | --- |
| `Domain` | nikoho | entity, enumy, rozhraní repository/UoW |
| `Application` | jen `Domain` | DTO, services, DI extension |
| `Infrastructure` | jen `Domain` | `DbContext`, EF konfigurace, repository implementace, DI extension |
| `aoraCareApi` (Api) | `Application` + `Infrastructure` | Controllers, `Program.cs` |
| `aoraCareApi.Tests` | `Application` | unit testy services (mockované repozitáře) |

## 3. Ověřený současný stav (než začneš)

- **Git:** repo má rozjetou nekomitnutou práci — `aoraCareApi/` je celé **untracked**,
  `Psql-db/docker-compose.yml` je modifikovaný, a `git status` ukazuje jako smazanou starou
  strukturu `backend/AoraCare.sln`, `backend/src/Api/*`, `backend/tests/UnitTests/*`.
  **Doporučení:** než začneš, commitni současný stav `aoraCareApi/` (a rozhodni, co se starou
  smazanou strukturou — buď `git rm` nadobro, nebo revert), ať je v historii jasná hranice
  "před refaktorem" a diff jde přehledně revertovat.
- Skutečné názvy: `Controllers/CategoriesController.cs` (bez překlepu),
  `Application/Services/IProductService.cs` přímo v `Services/`, ale
  `Application/Services/Interfaces/ICategoryService.cs` v podsložce `Interfaces/` —
  nekonzistentní umístění, sjednoť při přesunu (viz krok 3 níže).
- `AppDbContext` má dnes jen `DbSet<Category>` a `DbSet<Product>`.
  `Domain/{Order,OrderItem,Payment,Review,ProductVariant}.cs` jsou prázdné stuby — přesunou se
  beze změny obsahu, žádná nová abstrakce pro ně teď nevzniká (nemají konzumenta).
- `appsettings.json`: `ConnectionStrings:Default = ""`; `appsettings.Development.json` má
  reálný connection string. Beze změny v tomto refaktoru.
- `.vscode/settings.json` je prázdné `{}` — nic tam není potřeba upravovat.
- .NET SDK 10.0.301. Žádný testovací projekt v repu zatím neexistuje.

## 4. Postup krok za krokem

Cílem pořadí je, aby šlo **průběžně buildit** a chyby kompilátoru tě naváděly, co je ještě
potřeba doplnit.

### Krok 0 — Ukliď git
```bash
cd ~/Projects/aora-care
git add backend/aoraCareApi backend/aoraCare.slnx backend/.vscode backend/docs .gitignore Plan.md Problem.md
git rm -r --cached backend/src backend/AoraCare.sln backend/.config 2>/dev/null  # stará smazaná struktura
git commit -m "chore: commit current aoraCareApi baseline before Clean Architecture split"
```
Uprav dle vlastního uvážení, hlavně ať máš čistý výchozí bod.

### Krok 1 — `Directory.Build.props`
`backend/Directory.Build.props`:
```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```
Z každého `.csproj` pak smaž tyhle 3 řádky (zdědí se automaticky ze složky nad sebou).

### Krok 2 — Scaffold nových projektů
```bash
cd ~/Projects/aora-care/backend
dotnet new classlib -n aoraCareApi.Domain -o aoraCareApi.Domain
dotnet new classlib -n aoraCareApi.Application -o aoraCareApi.Application
dotnet new classlib -n aoraCareApi.Infrastructure -o aoraCareApi.Infrastructure
dotnet new xunit -n aoraCareApi.Tests -o aoraCareApi.Tests
rm aoraCareApi.Domain/Class1.cs aoraCareApi.Application/Class1.cs aoraCareApi.Infrastructure/Class1.cs
```
Přidej všechny do `aoraCare.slnx` (buď ručně, nebo `dotnet sln aoraCare.slnx add <cesta>` — pozor,
`.slnx` je nový XML formát, `dotnet sln` na něj funguje od SDK 9+).

### Krok 3 — Přesun souborů
Namespacy zůstávají **stejné** (`aoraCareApi.Domain`, `aoraCareApi.Application.*`,
`aoraCareApi.Infrastructure.*`) — mění se jen fyzické umístění:

```bash
cd ~/Projects/aora-care/backend
git mv aoraCareApi/Domain/* aoraCareApi.Domain/
git mv aoraCareApi/Application/* aoraCareApi.Application/
git mv aoraCareApi/Infrastructure/* aoraCareApi.Infrastructure/
# sjednoť umístění service rozhraní:
git mv aoraCareApi.Application/Services/Interfaces/ICategoryService.cs aoraCareApi.Application/Services/ICategoryService.cs
rmdir aoraCareApi.Application/Services/Interfaces
```

### Krok 4 — `ProjectReference`
```xml
<!-- aoraCareApi.Application.csproj -->
<ItemGroup>
  <ProjectReference Include="../aoraCareApi.Domain/aoraCareApi.Domain.csproj" />
</ItemGroup>

<!-- aoraCareApi.Infrastructure.csproj -->
<ItemGroup>
  <ProjectReference Include="../aoraCareApi.Domain/aoraCareApi.Domain.csproj" />
</ItemGroup>

<!-- aoraCareApi/aoraCareApi.csproj (Api) -->
<ItemGroup>
  <ProjectReference Include="../aoraCareApi.Application/aoraCareApi.Application.csproj" />
  <ProjectReference Include="../aoraCareApi.Infrastructure/aoraCareApi.Infrastructure.csproj" />
</ItemGroup>
```
`Infrastructure` **nesmí** mít referenci na `Application` — to je ten rozdíl oproti dnešku.

### Krok 5 — Přesun balíčků
Z `aoraCareApi/aoraCareApi.csproj` přesuň do `aoraCareApi.Infrastructure/aoraCareApi.Infrastructure.csproj`:
`Microsoft.EntityFrameworkCore`, `.Analyzers`, `.Design`, `.Relational`, `.Tools`,
`Npgsql.EntityFrameworkCore.PostgreSQL`. V Api projektu zůstane jen `Microsoft.AspNetCore.OpenApi`.

### Krok 6 — Kontrolní build (očekávaně selže)
```bash
dotnet build backend/aoraCare.slnx
```
V tuhle chvíli **nepůjde zkompilovat** — `CategoryService`/`ProductService` pořád injectují
`AppDbContext`, na který `Application.csproj` už nemá referenci. To je očekávané a je to
důkaz, že rozdělení funguje — pokračuj rovnou krokem 7.

### Krok 7 — Repository/UoW rozhraní (`aoraCareApi.Domain/Repositories/`)

```csharp
// ICategoryRepository.cs
namespace aoraCareApi.Domain.Repositories;

public interface ICategoryRepository
{
    Task<ICollection<Category>> GetAllAsync(CancellationToken ct = default);
    Task<ICollection<Category>> GetAllActiveAsync(CancellationToken ct = default);
    Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default);          // no-tracking + Include(Products)
    Task<Category?> GetByIdTrackedAsync(Guid id, CancellationToken ct = default);   // tracked, bez Include
    Task<List<Category>> GetAllOrderedBySortAsync(CancellationToken ct = default);  // tracked, ORDER BY SortOrder
    Task<int?> GetMaxSortOrderAsync(CancellationToken ct = default);
    Task AddAsync(Category category, CancellationToken ct = default);
    void Remove(Category category);
}

// IProductRepository.cs — jen to, co ProductService dnes reálně používá
namespace aoraCareApi.Domain.Repositories;

public interface IProductRepository
{
    Task AddAsync(Product product, CancellationToken ct = default);
}

// IUnitOfWork.cs
namespace aoraCareApi.Domain.Repositories;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
```

### Krok 8 — Implementace (`aoraCareApi.Infrastructure/Data/Repositories/`)

```csharp
// CategoryRepository.cs
namespace aoraCareApi.Infrastructure.Data.Repositories;

public class CategoryRepository(AppDbContext db) : ICategoryRepository
{
    public async Task<ICollection<Category>> GetAllAsync(CancellationToken ct = default) =>
        await db.Categories.AsNoTracking().ToListAsync(ct);

    public async Task<ICollection<Category>> GetAllActiveAsync(CancellationToken ct = default) =>
        await db.Categories.AsNoTracking().Where(c => c.IsActive).ToListAsync(ct);

    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Categories.AsNoTracking().Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Category?> GetByIdTrackedAsync(Guid id, CancellationToken ct = default) =>
        await db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<List<Category>> GetAllOrderedBySortAsync(CancellationToken ct = default) =>
        await db.Categories.OrderBy(c => c.SortOrder).ToListAsync(ct);

    public async Task<int?> GetMaxSortOrderAsync(CancellationToken ct = default) =>
        await db.Categories.MaxAsync(c => (int?)c.SortOrder, ct);

    public async Task AddAsync(Category category, CancellationToken ct = default) =>
        await db.Categories.AddAsync(category, ct);

    public void Remove(Category category) => db.Categories.Remove(category);
}

// ProductRepository.cs
namespace aoraCareApi.Infrastructure.Data.Repositories;

public class ProductRepository(AppDbContext db) : IProductRepository
{
    public async Task AddAsync(Product product, CancellationToken ct = default) =>
        await db.Products.AddAsync(product, ct);
}

// Data/UnitOfWork.cs — sourozenec AppDbContext.cs, obaluje celý DbContext, ne jeden agregát
namespace aoraCareApi.Infrastructure.Data;

public class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
```

> Poznámka k `GetAllAsync`/`GetAllActiveAsync`: dnes se projektuje přímo do `CategoryDto` na
> úrovni SQL (užší sloupcová projekce). Po přechodu na vracení entit se natáhne celý řádek —
> u malé tabulky `Category` zanedbatelné, vědomý kompromis ve prospěch správného vrstvení.

### Krok 9 — DI extension metody

```csharp
// aoraCareApi.Infrastructure/DependencyInjection.cs
namespace aoraCareApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(o => o.UseNpgsql(config.GetConnectionString("Default")));
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }
}

// aoraCareApi.Application/DependencyInjection.cs
namespace aoraCareApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICategoryService, CategoryService>();
        return services;
    }
}
```

### Krok 10 — Přepsat services

`CategoryService.cs` — konstruktor `(ICategoryRepository categoryRepository, IUnitOfWork unitOfWork)`,
smazat `using aoraCareApi.Infrastructure.Data;` a `using Microsoft.EntityFrameworkCore;`. Metody
volají repository místo `_db` (`GetAll()` → `categoryRepository.GetAllAsync()` + `.Select(c => c.ToDto())`,
`Create` → `AddAsync` + `unitOfWork.SaveChangesAsync()`, `Update`/`Delete`/`ReorderCategory` obdobně —
**logika reorderu se nemění**, jen zdroj dat a save).

`ProductService.cs` — konstruktor `(IProductRepository productRepository, IUnitOfWork unitOfWork)`,
smazat `using aoraCareApi.Infrastructure.Data;`. `CreateAsync` volá `productRepository.AddAsync` +
`unitOfWork.SaveChangesAsync`. `DetermineInitialStateAsync` **necháváš beze změny** (pořád
`throw new NotImplementedException()`) — jen přepiš komentář, který dnes odkazuje na `_db`, aby
odkazoval obecně na `productRepository` (žádnou novou metodu na repository nepřidávej, dokud ji
tahle metoda reálně nepotřebuje).

### Krok 11 — Smazat stray using
`ICategoryService.cs`: smazat `using Microsoft.AspNetCore.Mvc;` (po přesunu do
`aoraCareApi.Application.csproj`, což je obyčejná class library bez ASP.NET Core frameworku, by
se stejně nezkompiloval).

### Krok 12 — Zjednodušit `Program.cs`
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();
// zbytek pipeline (UseHttpsRedirection, UseAuthorization, MapControllers, MapOpenApi...) beze změny
```

### Krok 13 — Build
```bash
dotnet build backend/aoraCare.slnx
grep -rn "Infrastructure\|EntityFrameworkCore" backend/aoraCareApi.Application/   # musí být prázdné
```

### Krok 14 — Ověřit EF migrace přes hranici projektů
`AppDbContext` teď žije v `Infrastructure`, ale connection string se čte z konfigurace `Api`
projektu:
```bash
dotnet ef migrations list --project backend/aoraCareApi.Infrastructure --startup-project backend/aoraCareApi
```
Pokud selže s chybou o chybějícím design-time factory, přidej do `Infrastructure`:
```csharp
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=aoracare;Username=aoracare;Password=aoracare")
            .Options;
        return new AppDbContext(options);
    }
}
```

### Krok 15 — Testovací projekt
`aoraCareApi.Tests/aoraCareApi.Tests.csproj`:
```xml
<ItemGroup>
  <PackageReference Include="Moq" Version="4.20.72" />
  <ProjectReference Include="../aoraCareApi.Application/aoraCareApi.Application.csproj" />
</ItemGroup>
```

### Krok 16 — Testy

`CategoryServiceTests.cs` (mock `ICategoryRepository` + `IUnitOfWork`):
- `GetAll_ReturnsAllCategoriesMappedToDto`, `GetAllActive_ReturnsOnlyActiveCategoriesMappedToDto`
- `Get_ExistingId_ReturnsDto`, `Get_NonExistingId_ReturnsNull`
- `Create_NoExistingCategories_SetsSortOrderZero`, `Create_WithExistingCategories_SetsSortOrderToMaxPlusOne`
- `Create_IsActiveDefaultsTrue_WhenDtoIsActiveNull`, `Create_IsActiveRespectsDtoValue_WhenProvided`
- `Update_NonExistingId_ReturnsNull_AndNeverCallsSaveChanges`
- `Update_ExistingId_NoSortOrderChange_UpdatesFieldsAndCallsSaveChangesOnce`
- `Update_WithSortOrder_ReordersAllCategoriesAndSavesOnce`
- `Update_SortOrderOutOfRange_ThrowsArgumentOutOfRangeException` (přes veřejný `Update`)
- `Delete_NonExistingId_ReturnsFalse_AndNeverCallsRemoveOrSaveChanges`
- `Delete_ExistingId_ReturnsTrue_RemovesAndSavesOnce`

`ProductServiceTests.cs` (mock `IProductRepository` + `IUnitOfWork`):
- `CreateAsync_ThrowsNotImplementedException_BecauseDetermineInitialStateIsNotYetImplemented` —
  `[Fact]`, dokumentuje dnešní reálné chování, drží `dotnet test` zelený.
- `CreateAsync_PersistsAndMapsProduct_OnceDetermineInitialStateIsImplemented` —
  `[Fact(Skip = "Enable once ProductService.DetermineInitialStateAsync TODO is implemented")]`
  s cílovými asercemi (`AddAsync` zavolán jednou, `SaveChangesAsync` zavolán jednou, DTO mapping sedí).

### Krok 17 — Test run
```bash
dotnet test backend/aoraCare.slnx
```
Vše zelené, skip test reportuje *Skipped*, ne *Failed*.

### Krok 18 — Smoke test
```bash
dotnet run --project backend/aoraCareApi
```
Ověř `GET /Categories`, `GET /Categories/active`, `GET /Categories/{id}`, `POST /Categories`,
a `POST /Products` (musí pořád házet stejnou `NotImplementedException` jako dřív — potvrzuje,
že DI přeskládání nezměnilo chování na API hranici).

## 5. Mimo rozsah (vědomě nezahrnuto)

- `Update`/`Delete` na `CategoriesController` nejsou dnes napojené na HTTP endpointy (service
  metody existují, controller je nevolá) — zůstává tak.
- `ProductVariant`/`Order`/`OrderItem`/`Payment`/`Review` nemají service ani repository —
  přesunou se beze změny obsahu, žádná nová abstrakce pro ně teď nevzniká (nemají konzumenta).
- `DetermineInitialStateAsync` TODO se nedoplňuje za tebe — zůstává tvoje rozhodnutí
  (SortOrder/IsActive trade-off, viz komentář v `ProductService.cs`).
- `appsettings` connection string, GoPay, Hangfire, FluentValidation wiring a cokoliv jiného
  z `Plan.md` se týká jiných fází projektu, ne tohoto refaktoru.

## 6. Checklist

- [ ] 0 — Commit/uklizení git baseline
- [ ] 1 — `Directory.Build.props`
- [ ] 2 — Scaffold `aoraCareApi.Domain/Application/Infrastructure/Tests`, přidat do `aoraCare.slnx`
- [ ] 3 — Přesun souborů + sjednocení `Services/Interfaces` → `Services/`
- [ ] 4 — `ProjectReference` graf (Application→Domain, Infrastructure→Domain, Api→oba)
- [ ] 5 — Přesun EF Core/Npgsql balíčků do Infrastructure
- [ ] 6 — Kontrolní build (očekávaně selže na `AppDbContext` v services)
- [ ] 7 — `Domain/Repositories/{ICategoryRepository,IProductRepository,IUnitOfWork}.cs`
- [ ] 8 — `Infrastructure/Data/Repositories/{CategoryRepository,ProductRepository}.cs` + `UnitOfWork.cs`
- [ ] 9 — `Infrastructure/DependencyInjection.cs` + `Application/DependencyInjection.cs`
- [ ] 10 — Přepsat `CategoryService.cs`/`ProductService.cs`
- [ ] 11 — Smazat stray `using Microsoft.AspNetCore.Mvc;` z `ICategoryService.cs`
- [ ] 12 — Zjednodušit `Program.cs`
- [ ] 13 — `dotnet build` zelený + grep kontrola
- [ ] 14 — EF migrace fungují přes `--project`/`--startup-project`
- [ ] 15 — Scaffold `aoraCareApi.Tests` (xUnit + Moq)
- [ ] 16 — Napsat `CategoryServiceTests`/`ProductServiceTests`
- [ ] 17 — `dotnet test` zelený
- [ ] 18 — Smoke test přes `dotnet run`
