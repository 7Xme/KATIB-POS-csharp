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



### 2. Missing Transactions in Cancel/Stock-Reversal Operations

- `SaleService.CancelSaleAsync` — reverses stock without a DB transaction
- `PurchaseService.CancelPurchaseAsync` — reverses stock without a DB transaction

If the process fails mid-way, stock and status become inconsistent.

**Fix:** Wrap both methods in `await using var transaction = await _context.Database.BeginTransactionAsync();`

###
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



### 13. Reports & Export

**Current:** Basic dashboard KPIs + `GetSalesSummaryAsync`.

**Needed:**
- **Sales Reports:** Daily/Monthly/Yearly summaries, by product, by category, by cashier, by payment method
- **Inventory Reports:** Stock valuation, stock movement, slow-moving items, inventory aging
- **Financial Reports:** Profit & Loss, tax summary, revenue trends
- **Customer Reports:** Top customers, customer balance aging, purchase history
- **Export:** CSV, Excel (EPPlus or ClosedXML), PDF report generation



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

## 