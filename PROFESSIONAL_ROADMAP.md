# Ketaba POS — Professional Roadmap

## Current Project State

**Stack:** .NET 9 WPF, EF Core 9 + SQLite, CommunityToolkit.Mvvm, MaterialDesignThemes 5.2.1  
**Architecture:** MVVM with DI (Singleton DbContext, Singleton services, Transient ViewModels)  
**DB:** Single SQLite file at `%LOCALAPPDATA%/KetabaPOS/ketaba.db`  
**Auth:** BCrypt password hashing, single default user (admin/admin123)  
**Modules existing:** Dashboard, POS, Products (with categories), Customers, Suppliers, Sales, Loans, Settings

### Entities (15)
BaseEntity, User, Product, Category, Customer, Supplier, Sale, SaleItem, Purchase, PurchaseItem, Loan, LoanPayment, InventoryTransaction, Setting, BackupLog

### Enums (6)
UserRole (Admin/Manager/Cashier/Viewer), PaymentMethod (Cash/Card/BankTransfer/Credit), TransactionType (Sale/Purchase/Adjustment/Transfer/Return), LoanType (CustomerLoan/SupplierLoan), LoanStatus (Active/Paid/Overdue/WrittenOff), SaleStatus (Active/Completed/Cancelled/Refunded)

---

## 1. MISSING BUSINESS-CRITICAL FEATURES

### 1.1 Purchasing & Stock Management
| Feature | Why Needed | Effort |
|---------|-----------|--------|
| **Purchase Orders UI** — Create/receive purchase orders from suppliers | Purchase entity exists but no UI to create or receive POs | High |
| **Stock Adjustments** — Manual inventory corrections (damage, loss, found) | InventoryTransaction entity supports Adjust type but no UI | Medium |
| **Stock Transfers** — Move stock between virtual warehouses | Transfer type exists in enum but no UI | Medium |
| **Low Stock Alerts** — Visual warning when stock hits MinStockLevel | Data exists (`MinStockLevel`) but no alert anywhere | Low |
| **Stock Count / Inventory Audit** — Periodic physical count vs system | No feature at all | High |

### 1.2 Sales & Customers
| Feature | Why Needed | Effort |
|---------|-----------|--------|
| **Hold / Park Sale** — Temporarily save a cart and recall it later | Essential for busy retail | Medium |
| **Return / Refund** — Process full or partial returns | SaleStatus.Refunded exists but no UI | High |
| **Discount by % or fixed amount** — Per-item and per-sale discounts | Only per-item DiscountAmount exists, no percentage | Medium |
| **Customer loyalty / points** — Points tracking exists on Customer entity | `LoyaltyPoints` field but never used | Low |
| **Credit sales** — Sell on credit and track payments | PaymentMethod.Credit exists but no flow | Medium |

### 1.3 Multi-User & Security
| Feature | Why Needed | Effort |
|---------|-----------|--------|
| **User management UI** — Add/edit/disable users | UserRole enum exists (Admin/Manager/Cashier/Viewer) but no user admin page | Medium |
| **Role-based access control** — Hide Settings from cashiers | Roles defined but never checked anywhere | Medium |
| **Audit log** — Track who did what and when | No audit trail for non-inventory actions | High |
| **Session management** — Auto-logout after idle time | No timeout | Low |

### 1.4 Reports & Analytics
| Feature | Why Needed | Effort |
|---------|-----------|--------|
| **Sales reports** — Daily/monthly/custom range with charts | Dashboard has basic KPIs, no printable reports | High |
| **Profit reports** — Cost vs retail, margin per product | CostPrice and RetailPrice exist but no profit calc UI | Medium |
| **Inventory reports** — Stock valuation, slow movers, expiry | No reporting at all | High |
| **Export to Excel/CSV/PDF** — Any DataGrid export | No export anywhere | Medium |

---

## 2. ARCHITECTURE & TECHNICAL DEBT

### 2.1 Critical Fixes
| Issue | Impact | Fix |
|-------|--------|-----|
| **Singleton DbContext thread-safety** | Concurrent operations from different ViewModels on same DbContext can throw `InvalidOperationException` | Use `IDbContextFactory<AppDbContext>` or register as scoped with scope-per-operation |
| **Fire-and-forget `Execute(null)` in navigation** | 8 async commands fired via `ICommand.Execute` — exceptions become unobserved task exceptions | Call `LoadAsync()` directly in an async void handler with proper error handling, or use `IAsyncRelayCommand.ExecuteAsync()` |
| **Direct `AppDbContext` injection in 3 ViewModels** (Customers, Suppliers, Loans) | Bypasses service layer, makes unit testing impossible | Create `ICustomerService`, `ISupplierService`, `ILoanService` interfaces like the rest of the app |
| **No global error logger** | Errors shown via MessageBox only, no persistent log | Add `Serilog` or `NLog` file logging |

### 2.2 Medium Priority
| Issue | Impact | Fix |
|-------|--------|-----|
| **ViewModel caching never clears** | `_viewModels` dictionary grows indefinitely with each navigation (though VMs are Transient, they're cached) | Clear cache on logout, or use WeakReference |
| **Hardcoded strings in XAML** | No localization support for Arabic market | Move all strings to `.resx` resource files |
| **No async void safety** | `partial void OnSelectedLoanTypeChanged` uses `_ = LoadLoansAsync()` — fire and forget | Call `async void` handler with try-catch |
| **Invoice number uses local time** | `DateTime.Now` instead of `UtcNow` for invoice numbering | DST-safe UTC with offset display |
| **No database migrations strategy** | `EnsureCreated()` works for initial deploy but blocks schema changes | Use proper EF Core migrations in production |

### 2.3 Performance
| Issue | Impact | Fix |
|-------|--------|-----|
| **No pagination on Customers/Suppliers** | Loading 10k customers loads all into memory | Add Skip/Take like Products page |
| **No async initialization** | `GetAwaiter().GetResult()` on startup blocks thread | Use `async Task` startup pattern |
| **All settings loaded individually** | 5+ separate DB queries on Settings load | Use `GetSettingsByGroupAsync` to load in one query |

---

## 3. UI/UX IMPROVEMENTS

### 3.1 Professional Touches
| Feature | Why Needed | Effort |
|---------|-----------|--------|
| **Dark / Light theme toggle** | Toggle exists in ViewModel but no visual effect | Medium |
| **RTL support** | Arabic market — toggle exists but no FlowDirection wiring | Medium |
| **Keyboard shortcuts** — F1-F12 for POS actions, Ctrl+N new sale, Esc to cancel | Essential for fast POS operation | Medium |
| **Touch-friendly POS layout** — Larger buttons, swipe gestures | Required for touchscreen POS terminals | High |
| **Responsive window** — Minimum size, proper scaling | Window is fixed layout | Low |
| **Print preview** — See receipt before printing | Currently prints directly | Medium |
| **Email receipt** — Send receipt PDF to customer email | Customer.Email exists | Medium |

### 3.2 Dashboard
| Feature | Why Needed | Effort |
|---------|-----------|--------|
| **Sales chart (last 7 days)** | `SalesLast7Days` data exists but no chart rendering | Medium |
| **Top selling products** | No data tracked | Medium |
| **Real-time clock** | No clock on any page | Low |
| **Quick action buttons** | Shortcuts to common tasks from dashboard | Low |

### 3.3 POS Screen
| Feature | Why Needed | Effort |
|---------|-----------|--------|
| **Product quick-search with keyboard** | Currently a search box + button; should search-as-you-type | Low |
| **Numpad for quick price/qty entry** | Faster than typing on a keyboard for touch users | Medium |
| **Cart item quantity editor (inline)** | Can't change qty without remove/re-add | Low |
| **Customer selection before sale** | Customer can be selected but no quick-create if not found | Low |
| **Split payment** — Pay with cash + card combined | Single payment method only | Medium |

---

## 4. PROFESSIONAL FEATURES (Competitive Advantage)

### 4.1 Hardware Integration
| Feature | Why Needed | Effort |
|---------|-----------|--------|
| **ESC/POS thermal printer support** — Direct thermal printing, not via PrintDialog | Required for receipt printers in most stores | High |
| **Barcode scanner auto-detect** — Auto-focus barcode field on POS load | Already partially done | Low |
| **Cash drawer integration** — Open cash drawer after sale | Standard ESC/POS command | Low |
| **Customer-facing display** — Second screen showing cart total | Common in retail | Medium |
| **Receipt printer paper cut** — Auto-cut after printing | ESC/POS command | Low |

### 4.2 Business Operations
| Feature | Why Needed | Effort |
|---------|-----------|--------|
| **Multi-store / multi-warehouse** — Separate inventory per location | Essential for chains | High |
| **Tax configurations** — Multiple tax rates, tax-exempt items, VAT reports | Single hardcoded tax rate currently | Medium |
| **Pricing tiers** — Retail, wholesale, special customer pricing | WholesalePrice exists but never selectable in POS | Medium |
| **Barcode label printing** — Print product barcode labels | Required for inventory management | Medium |
| **SMS / WhatsApp notifications** — Send invoice/receipt to customer phone | Customer.Phone exists | Medium |
| **Shift management** — Cashier shift in/out with float count | No shift tracking | High |
| **Offline mode** — Work without internet, sync later | SQLite local file is inherently offline | Low |

### 4.3 Data & Compliance
| Feature | Why Needed | Effort |
|---------|-----------|--------|
| **Z-report / X-report** — End-of-day sales summary | Standard retail requirement | Medium |
| **Fiscal / Tax invoice** — Country-specific tax invoice format (e.g., ZATCA in Saudi Arabia) | Required for legal compliance in many markets | High |
| **Data retention / purge** — Archive old sales automatically | DB grows unbounded | Medium |
| **Multi-currency** — Support foreign currency sales | Single currency currently | High |

---

## 5. IMPLEMENTATION PRIORITY MATRIX

```
Priority    Feature                              Effort    Business Value
─────────────────────────────────────────────────────────────────────
P0 (NOW)    Purchasing UI (receive POs)           High      Critical
P0 (NOW)    Returns / Refunds                     High      Critical
P0 (NOW)    User management + RBAC                Medium    Critical
P1 (SOON)   Sales reports (daily/monthly)         High      High
P1 (SOON)   ESC/POS thermal printing              High      High
P1 (SOON)   Stock alerts (low stock warnings)     Low       High
P1 (SOON)   Search-as-you-type product search     Low       High
P2 (LATER)  Dark theme + RTL                      Medium    Medium
P2 (LATER)  Chart on dashboard                    Medium    Medium
P2 (LATER)  Export to Excel/CSV                   Medium    Medium
P2 (LATER)  Audit log                             High      Medium
P2 (LATER)  Discount by %                         Low       Medium
P3 (NICE)   Split payment                         Medium    Low
P3 (NICE)   Email receipt                         Medium    Low
P3 (NICE)   Multi-store                           High      Low*
P3 (NICE)   SMS notifications                     Medium    Low
```

*\*Multi-store is P3 for single-location business, P0 for chains.*

---

## 6. SUMMARY (Top 5 Actions)

1. **Build Purchasing UI** — Purchase entity + PurchaseItem exist in the DB but there is zero UI to create/receive purchase orders. Stock can only decrease (via sales) but never increase through the app.

2. **Implement Returns/Refunds** — SaleStatus.Refunded is defined but no workflow exists. Without returns, the system cannot handle the most basic retail transaction type after a sale.

3. **Add User Management Page** — UserRole (Admin/Manager/Cashier/Viewer) is fully defined. The DbSeeder creates only one user. There is no way to add cashiers or managers.

4. **Fix Singleton DbContext** — Three ViewModels inject `AppDbContext` directly (CustomersVM, SuppliersVM, LoansVM). Combined with Singleton registration, this is a thread-safety time bomb. Extract into service interfaces.

5. **Add Thermal Receipt Printing** — Current receipt printing uses WPF's `PrintDialog` which sends text to any printer. Dedicated ESC/POS thermal printers (Epson TM series, Star) require raw byte commands for proper formatting, paper cutting, and cash drawer opening.
