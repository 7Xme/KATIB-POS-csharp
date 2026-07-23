# Database Schema Documentation

## Overview

The application uses SQLite with Entity Framework Core (code-first). The database file is stored at `%LOCALAPPDATA%/KetabaPOS/ketaba.db`.

## Entity Relationships

```
User (1) ────< Sale (N)
Customer (1) ──< Sale (N)
Customer (1) ──< Loan (N)
Supplier (1) ──< Purchase (N)
Supplier (1) ──< Loan (N)
Category (1) ──< Product (N)
Category (1) ──< Category (self-ref: ParentId)
Product (1) ──< SaleItem (N)
Product (1) ──< PurchaseItem (N)
Product (1) ──< InventoryTransaction (N)
Sale (1) ──< SaleItem (N)
Purchase (1) ──< PurchaseItem (N)
Loan (1) ──< LoanPayment (N)
```

## Tables

### Users
| Column | Type | Notes |
|--------|------|-------|
| Id | INTEGER | PK |
| Username | TEXT | Unique, required |
| PasswordHash | TEXT | BCrypt hash |
| DisplayName | TEXT | |
| Role | TEXT | Admin/Manager/Cashier/Viewer |
| IsActive | INTEGER | Boolean |
| LastLogin | DATETIME | |

### Products
| Column | Type | Notes |
|--------|------|-------|
| Id | INTEGER | PK |
| Name | TEXT | |
| NameAr | TEXT | Arabic name |
| Barcode | TEXT | Indexed |
| SKU | TEXT | Indexed |
| CategoryId | INTEGER | FK → Categories |
| CostPrice | DECIMAL(18,2) | |
| RetailPrice | DECIMAL(18,2) | |
| WholesalePrice | DECIMAL(18,2) | |
| StockQuantity | REAL | |
| MinStockLevel | REAL | |
| ImagePath | TEXT | |
| IsActive | INTEGER | |

### Categories
| Column | Type | Notes |
|--------|------|-------|
| Id | INTEGER | PK |
| Name | TEXT | |
| NameAr | TEXT | |
| ParentId | INTEGER | FK → Categories (self) |

### Sales / SaleItems
See the `Sale` and `SaleItem` entities in code for full schema.

### Inventory Transactions
Tracks all stock changes with quantity before/after, reference number, and transaction type (Sale/Purchase/Adjustment/Transfer/Return).

### Loans / LoanPayments
Tracks customer and supplier loans with payment history, interest rates, and due dates.

### Settings
Key-value store for application configuration (company info, tax rate, theme, etc.).

### BackupLogs
Tracks database backup history with file info and success status.

## Soft Delete

All entities use `IsDeleted` flag with `HasQueryFilter` for automatic filtering.
