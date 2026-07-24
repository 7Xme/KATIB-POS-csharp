# Ketaba POS — Professional Analysis & Improvement Roadmap

## Project Overview

| Dimension | Current State |
|-----------|--------------|
| **Runtime** | .NET 9 WPF (`net9.0-windows`) |
| **Architecture** | MVVM with DI (CommunityToolkit.Mvvm + Microsoft.Extensions.DependencyInjection) |
| **ORM** | Entity Framework Core 9.0 + SQLite |
| **UI Theme** | MaterialDesignThemes 5.2.1 + custom styles |
| **Auth** | BCrypt.Net-Next 4.0.3 |
| **Testing** | xUnit (5 basic model-level tests) |
| **Localization** | English + Arabic (resx + TranslationSource singleton) |
| **Database** | 14 tables, 1 migration (InitialCreate), soft-delete, audit fields |
| **Git** | 23 commits, mostly generic messages ("up", "UP") |

---

## Critical Issues (Must Fix)

### 1. Singleton DbContext — Thread-Safety Violation

**File:** `App.xaml.cs` (line 33-40)

`AppDbContext` is registered as a **Singleton** but EF Core `DbContext` is **not thread-safe**. Multiple ViewModels/commands accessing the DB concurrently will cause `InvalidOperationException`, data corruption, or silent failures.

**Fix:** Register `AppDbContext` as **Scoped** per-operation, or inject `IDbContextFactory<AppDbContext>` and create fresh contexts per operation. The cleanest approach for WPF is to use `IDbContextFactory`:

```csharp
services.AddDbContextFactory<AppDbContext>(opts =>
    opts.UseSqlite(connectionString));
```

Then inject `IDbContextFactory<AppDbContext>` into services instead of `AppDbContext` directly.

### 2. Missing Transactions in Cancel/Stock-Reversal Operations

- `SaleService.CancelSaleAsync` — reverses stock without a DB transaction
- `PurchaseService.CancelPurchaseAsync` — reverses stock without a DB transaction

If the process fails mid-way, stock and status become inconsistent.

**Fix:** Wrap both methods in `await using var transaction = await _context.Database.BeginTransactionAsync();`

### 3. Backup/Restore Is Dangerous

**File:** `SettingsService.cs`

- `BackupDatabaseAsync` copies the SQLite file without flushing the WAL journal — backup may be incomplete
- `RestoreDatabaseAsync` overwrites the live database without refreshing any open `DbContext` — all existing connections become stale

**Fix:** Call `PRAGMA wal_checkpoint(TRUNCATE)` before backup. For restore, dispose all contexts and reinitialize.

### 4. Three ViewModels Bypass the Service Layer

`CustomersViewModel`, `SuppliersViewModel`, and `LoansViewModel` inject `AppDbContext` directly instead of using a service interface.

**Fix:** Create `ICustomerService`, `ISupplierService`, `ILoanService` with appropriate CRUD methods and register them in DI.

---

## Architecture Improvements

### 5. Add Repository Layer

`Infrastructure/Repositories/` exists but is **completely empty**. Direct `AppDbContext` usage in services couples business logic to EF Core, making unit testing difficult.

Introduce a generic repository:

```csharp
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task SoftDeleteAsync(int id);
    Task<int> CountAsync(Expression<Func<T, bool>>? filter = null);
}
```

### 6. SRP Violations in Services

- `ProductService.GetCustomersAsync()` — customer CRUD in a product service
- `PurchaseService.GetProductsAsync()` — product queries in a purchase service

Move misplaced methods to the correct services.

### 7. Replace `DbContext.Update()` with Targeted Updates

All services use `_context.Users.Update(user)` which marks **every property** as modified. Use:

```csharp
var entry = _context.Entry(user);
entry.Property(x => x.PropertyName).IsModified = true;
```

Or use AutoMapper to handle partial updates cleanly.

### 8. Add Custom Exception Types

`Core/Exceptions/` is empty. Add domain-specific exceptions:

- `EntityNotFoundException`
- `DuplicateEntityException`
- `InsufficientStockException`
- `InvalidOperationException`
- `AuthenticationException`
- `BusinessRuleViolationException`

Catch these in ViewModels and show appropriate user messages.

---

## Missing Business Features

### 9. Real-time Stock Alerts (Low Stock)

**Current:** Low-stock count shown on dashboard only.

**Needed:** Configurable threshold per product, push notification (system tray balloon), color-coded rows in POS product grid (green/yellow/red based on stock level), auto-low-stock report generation.

### 10. Purchasing Workflow UI

**Current:** `PurchaseService`, `Purchase`, `PurchaseItem`, `PurchasesViewModel` exist but the UI has only basic list/form.

**Needed:**
- Purchase Order creation with auto-PO-number
- Supplier selection with balance check
- Line-item entry with barcode scanning
- Receive/Partial Receive workflow
- Purchase history by supplier
- Pending vs Received PO filtering

### 11. Returns & Refunds (Full Workflow)

**Current:** `SaleService.RefundSaleAsync` exists. The SalesViewModel shows a Refund button.

**Needed:**
- Full refund dialog: select items to return, choose refund method (cash/store credit), auto-calculate restocking fees
- Partial refund support (return some items, keep others)
- Return reason categorization (defective, wrong item, customer changed mind)
- RMA number generation
- Return-to-supplier workflow integration

### 12. User Management / RBAC

**Current:** `UserRole` enum (Admin, Manager, Cashier, Accountant) exists. `UsersViewModel`, `AuthService` with user CRUD exists.

**Needed:**
- Role-based permission system (not just role names but actual permissions):
  ```csharp
  public enum Permission
  {
      Product_Create, Product_Edit, Product_Delete,
      Sale_Create, Sale_Refund, Sale_Cancel,
      User_Manage, User_ViewReports,
      Settings_Edit, Settings_BackupRestore,
      Loan_Approve, Loan_WriteOff
  }
  ```
- Permission-to-role mapping table
- UI for assigning permissions to roles
- Enforce permissions at both UI level (hide/show buttons) and service level (guard checks)

### 13. Reports & Export

**Current:** Basic dashboard KPIs + `GetSalesSummaryAsync`.

**Needed:**
- **Sales Reports:** Daily/Monthly/Yearly summaries, by product, by category, by cashier, by payment method
- **Inventory Reports:** Stock valuation, stock movement, slow-moving items, inventory aging
- **Financial Reports:** Profit & Loss, tax summary, revenue trends
- **Customer Reports:** Top customers, customer balance aging, purchase history
- **Export:** CSV, Excel (EPPlus or ClosedXML), PDF report generation

### 14. Audit Log

**Current:** `InventoryTransaction` exists for stock changes. No general audit trail.

**Needed:** An `AuditLog` table that records every significant action:

| Column | Type | Description |
|--------|------|-------------|
| Id | int | PK |
| UserId | int | Who did it |
| Action | string | e.g., "SaleCreated", "UserLoggedIn", "ProductUpdated" |
| EntityType | string | e.g., "Sale", "Product" |
| EntityId | int | The affected entity ID |
| OldValues | string (JSON) | Before state |
| NewValues | string (JSON) | After state |
| Timestamp | DateTime | When |

### 15. Cash Management

- Opening/Closing cash register amounts
- Cash-in/Cash-out transactions (petty cash, cash deposits)
- Shift management with cashier assignments
- X-report (mid-shift summary) and Z-report (end-of-day closeout)

### 16. Multi-Store / Warehouse Support

**Current:** Single-tenant, single-store.

**Needed:**
- `Store` entity (Name, Address, Phone, Code)
- Product stock by store (`StockQuantity` moves to a junction table `ProductStore`)
- User-to-store assignment
- Inventory transfer between stores with transfer orders
- Per-store sales and reporting

---

## Security Issues

### 17. No Password Complexity Validation

`CreateUserAsync` and `ChangePasswordAsync` accept any password without validation.

**Fix:** Enforce minimum length (8+), require mixed case + digit + special character. Use `Microsoft.AspNetCore.Identity` password validators or custom regex.

### 18. No Brute-Force Protection

`LoginAsync` has no rate-limiting or account lockout.

**Fix:** Track failed attempts in `User` entity (`FailedLoginAttempts`, `LockedUntilUtc`). After 5 failed attempts, lock the account for 15 minutes.

### 19. No RBAC Enforcement

`UserRole` is defined but never checked before sensitive operations. Any user with access could potentially perform admin actions.

**Fix:** Add an authorization check at the start of every service method:

```csharp
private void AssertPermission(User user, Permission required)
{
    if (!_permissionService.HasPermission(user.Id, required))
        throw new UnauthorizedAccessException($"User {user.Username} lacks permission {required}");
}
```

### 20. Password Change Requires Old Password

`ChangePasswordAsync` correctly requires the old password — good. But no confirmation of new password (handled in UI only).

### 21. Connection String in AppSettings

The SQLite connection string is in `appsettings.json` (plain text). For a desktop app this is acceptable, but consider encrypting sensitive settings using `DataProtectionConfigurationProvider` or OS-level encryption (DPAPI).

---

## Performance Issues

### 22. Dashboard Makes 6+ Sequential DB Queries

`DashboardService.GetDashboardDataAsync` makes individual round-trips for today's sales, transactions count, low stock count, active loans, chart data, and recent activity.

**Fix:** Use `Task.WhenAll` to parallelize independent queries.

### 23. Receipt Generation Loads All Settings Every Time

`SaleService.GenerateReceiptAsync` reads **all** settings from DB into a dictionary on every single receipt print.

**Fix:** Cache settings in memory (with invalidation on update) or inject a `ISettingsService` that caches.

### 24. TranslationSource Fires PropertyChanged for All Bindings

`SwitchTo` fires `PropertyChanged` with `string.Empty`, which WPF interprets as "refresh all bindings". For 200+ resource keys, this is expensive.

**Fix:** Track which keys are currently bound and only notify for those, or use a dedicated mechanism like `WeakReference` pattern.

### 25. Race Condition in Invoice Number Generation

Both `SaleService` and `PurchaseService` generate document numbers using `CountAsync() + 1`, which is not atomic under concurrency.

**Fix:** Use a dedicated sequence/counter table with `Interlocked.Increment` or a DB sequence.

### 26. No UI Virtualization

DataGrids for Products, Sales, etc. load all items at once (even with server-side pagination, the UI renders the whole page). For pages of 50+ items with complex templates, this can be slow.

**Fix:** Enable `VirtualizingPanel.IsVirtualizing="True"` and `VirtualizingPanel.VirtualizationMode="Recycling"` on all ListBox/DataGrid elements.

---

## UI/UX Improvements

### 27. Complete Arabic Localization

**Current:** Only nav bar buttons use `TranslationSource` bindings. Page headers, form labels, grid headers, buttons, tooltips, and validation messages still have hardcoded English strings.

**Fix:** Convert all ~200 remaining XAML label bindings to use `{Binding Source={x:Static services:TranslationSource.Instance}, Path=[KeyName]}`.

### 28. Keyboard Shortcuts & Touch Support

- **Essential shortcuts:** F1-F12 for nav, F2 for search, F5 for refresh, Ctrl+N for new sale, Ctrl+P for print, Esc for back/close, Ctrl+F for find
- **Touch mode:** Increase min touch target to 48x48px, add swipe gestures, use larger fonts on touch-enabled devices
- **Barcode scanner auto-focus** already has `AutoFocusBehavior` — good, but ensure it works with all common scanner models (HID keyboard wedge emulation)

### 29. Offline Mode / Resilience

SQLite is inherently local, but the app has no graceful degradation. Add:
- Connection health check on startup
- Graceful error UI when DB is locked/corrupt
- Auto-recovery: backup on crash detection, auto-restore from last good backup
- Progress indicators for long operations (backup, restore, large report generation)

### 30. Print Preview

`GenerateReceiptAsync` returns raw text bytes. Users need to see what will print before sending to the printer.

**Needed:**
- WPF `PrintDialog` with page setup
- Print preview window with zoom
- ESC/POS thermal printer support (raw socket/COM port printing)
- Receipt template customization (header/footer logo, item alignment, barcode on receipt)

### 31. Dark Theme

**Current:** Theme combo is in Settings but `PaletteHelper` was removed because it crashed (MaterialDesign theme dictionaries not merged).

**Fix:** Merge MaterialDesign themes properly in `App.xaml`:

```xml
<ResourceDictionary.MergedDictionaries>
    <ResourceDictionary Source="pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Light.xaml" />
    <ResourceDictionary Source="pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Dark.xaml" />
    <ResourceDictionary Source="Presentation/Resources/Styles.xaml" />
</ResourceDictionary.MergedDictionaries>
```

Then use `PaletteHelper` to switch at runtime.

### 32. Barcode / Label Printing

Add barcode/label printing for products:
- Standard label sizes (2x1, 3x1, 4x2 inches)
- Barcode as Code128 or EAN-13
- Product name, price, and barcode on label
- Batch label printing (select multiple products)
- Price tag printing from POS (print label when adding new product)

---

## Code Quality Issues

### 33. Fire-and-Forget Async Commands

Several ViewModels use `new RelayCommand(async _ => await method())`. This creates fire-and-forget async void commands. If the task throws, it crashes the process (caught only by `TaskScheduler.UnobservedTaskException`).

**Fix:** Use `RelayCommand`'s built-in async support: `RelayCommand.CreateFromTask(async () => await method())` or `AsyncRelayCommand`.

### 34. No Global Logger

Errors are only shown in a `MessageBox` (via global exception handlers). No structured logging.

**Fix:** Integrate Serilog:

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.File(Path.Combine(logFolder, "ketaba-.log"), rollingInterval: RollingInterval.Day)
    .WriteTo.Debug()
    .CreateLogger();
```

Log all operations (login, sale, refund, backup), warnings (low stock, failed login), and errors.

### 35. `GetAwaiter().GetResult()` Blocks UI Thread

In `App.xaml.cs`, `DbSeeder.SeedAsync(context).GetAwaiter().GetResult()` blocks the UI thread during startup.

**Fix:** Call synchronously (`SeedAsync(...).Result`) or run seeding in a background thread with a splash screen. Since seeding only happens on first run, this is low priority.

### 36. Missing XML Documentation

No public API has XML doc comments. While .NET doesn't require them, they help maintainability and will show up in IntelliSense for future developers.

### 37. No StyleCop / Analyzer Rules

No `.editorconfig` or `StyleCop.Analyzers` ruleset. Adding one would enforce consistent coding standards.

---

## Testing Gaps

### 38. Existing Tests Are Insufficient

The test project has 5 basic model-property assertions — no service tests, no integration tests, no edge cases, no negative tests.

**Needed:**
- **Unit tests for services:** Mock `AppDbContext` (or use SQLite in-memory) and test every service method
- **Integration tests:** Spin up a test SQLite DB, run migrations, and test full workflows (create sale → refund, create purchase → receive, etc.)
- **ViewModel tests:** Test navigation, command execution, and state management
- **Localization tests:** Verify all resx keys exist in both languages and have non-empty values

---

## DevOps & Tooling

### 39. CI/CD Pipeline

**Needed:**
- GitHub Actions workflows:
  - Build + test on PR
  - CodeQL security analysis
  - Release build with ClickOnce or Squirrel.Windows installer
  - Auto-versioning (GitVersion)

### 40. Database Migration Strategy

**Current:** Uses `EnsureCreated()` on every startup, then `SeedAsync()`. No migration workflow.

**Fix:** Use `context.Database.Migrate()` instead. After schema changes, run `dotnet ef migrations add ...` and apply on startup.

### 41. Application Updater

Desktop apps need an update mechanism:
- **Squirrel.Windows** — simplest for WPF, supports delta updates
- **ClickOnce** — built into Visual Studio, limited customization
- **Self-updater** — check GitHub releases, download + install

---

## Prioritized Roadmap

### P0 — Ship-Blocking (Do First)

| # | Item | Impact | Effort |
|---|------|--------|--------|
| 1 | Fix Singleton DbContext → `IDbContextFactory` | Prevents crashes | 2-3 days |
| 2 | Add DB transactions to CancelSaleAsync + CancelPurchaseAsync | Prevents data corruption | 1 day |
| 3 | Create ICustomerService, ISupplierService, ILoanService | Architecture fix | 1 day |
| 4 | Add input validation (password, duplicate barcode) | Security | 1 day |
| 5 | Complete Arabic localization (all XAML labels) | Usability | 2 days |

### P1 — High Value

| # | Item | Impact | Effort |
|---|------|--------|--------|
| 6 | Full refund/return UI dialog | Business requirement | 2 days |
| 7 | Purchasing receive workflow with partial receive | Business requirement | 2 days |
| 8 | RBAC with permissions | Security | 3 days |
| 9 | Sales reports by date/product/cashier | Management need | 2 days |
| 10 | Audit log | Compliance | 2 days |
| 11 | Dark theme (fix MaterialDesign merge) | UX | 1 day |

### P2 — Professional Polish

| # | Item | Impact | Effort |
|---|------|--------|--------|
| 12 | Serilog global logging | Maintenance | 1 day |
| 13 | ESC/POS thermal printing | Hardware integration | 2 days |
| 14 | Keyboard shortcuts | Productivity | 1 day |
| 15 | Low-stock real-time alerts | Operations | 2 days |
| 16 | Print preview + label printing | Feature | 2 days |
| 17 | X-Report / Z-Report | Retail standard | 2 days |
| 18 | Unit tests for services | Quality | 3 days |

### P3 — Growth Features

| # | Item | Impact | Effort |
|---|------|--------|--------|
| 19 | Multi-store / warehouse | Scalability | 5 days |
| 20 | Cash drawer integration | Hardware | 1 day |
| 21 | SMS/WhatsApp notifications | Customer engagement | 3 days |
| 22 | Loyalty program (points, tiers) | Retention | 3 days |
| 23 | Gift cards | Feature | 2 days |
| 24 | Cloud sync / off-site backup | Disaster recovery | 4 days |
| 25 | Mobile companion app | Expansion | Large |

---

## Quick Wins (1 Hour or Less)

- [ ] Replace `DateTime.Now` with `DateTime.UtcNow` in invoice/PO number generation
- [ ] Remove `Microsoft.AspNetCore` from `appsettings.json` logging (WPF app)
- [ ] Add `VirtualizingPanel.IsVirtualizing` to all DataGrids
- [ ] Create a single `.editorconfig` with basic C# conventions
- [ ] Add XML `<summary>` comments to all public interface methods
- [ ] Move misplaced `GetCustomersAsync()` out of `ProductService`
- [ ] Move misplaced `GetProductsAsync()` out of `PurchaseService`
- [ ] Remove redundant `Get()` method from `TranslationSource` (duplicate of indexer)
- [ ] Fix `SalesSummary` divide-by-zero guard from `> 0` to `!= 0`

---

## Technology Recommendations

| Category | Current | Recommended | Reason |
|----------|---------|-------------|--------|
| PDF Generation | None | QuestPDF or DinkToPdf | Free, reliable, .NET-native |
| Excel Export | None | ClosedXML | MIT license, no Office required |
| Logging | None (MessageBox) | Serilog | Structured, file/console/seq sinks |
| ORM | EF Core 9.0 | EF Core 9.0 (keep) | Best for SQLite |
| Auth | BCrypt.Net-Next | BCrypt.Net-Next (keep) | Industry standard |
| DI | Microsoft.Extensions.DI | Microsoft.Extensions.DI (keep) | Standard for .NET |
| Reporting | In-code aggregation | SQL views + Dapper for read models | Faster than EF for aggregates |
| Migration | EnsureCreated | EF Core Migrations | Schema versioning |
| Validation | None | FluentValidation | Clean separation of validation rules |
| Mapping | Manual | AutoMapper or Mapster | Reduces boilerplate |

---

## Files to Create

```
Core/
  Exceptions/
    EntityNotFoundException.cs
    InsufficientStockException.cs
    UnauthorizedAccessException.cs
    DuplicateEntityException.cs
    BusinessRuleViolationException.cs
  Interfaces/
    ICustomerService.cs           (new)
    ISupplierService.cs           (new)
    ILoanService.cs               (new)
    IPermissionService.cs         (new)
    IAuditLogService.cs           (new)
    IReportService.cs             (new)
    IRepository.cs                (new)
  Models/
    AuditLog.cs                   (new)
    Permission.cs                 (new)
    RolePermission.cs             (new)
    Store.cs                      (new)
    ProductStore.cs               (new)
    Shift.cs                      (new)
    CashTransaction.cs            (new)
Infrastructure/
  Repositories/
    Repository.cs                 (new — directory currently empty)
  Services/
    CustomerService.cs            (new)
    SupplierService.cs            (new)
    LoanService.cs                (new)
    PermissionService.cs          (new)
    AuditLogService.cs            (new)
    ReportService.cs              (new)
    ThermalPrinterService.cs      (new)
Presentation/
  ViewModels/
    ReportViewModel.cs            (new)
```
