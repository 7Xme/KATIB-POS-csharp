# Ketaba POS - Point of Sale & Inventory Management System

A modern Windows desktop application for small-to-medium retail businesses built with WPF and .NET 9.

## Features

- **POS System** - Fast cash register interface with barcode search, cart management, and receipt printing
- **Product Management** - Full CRUD with categories, barcodes, and bulk import/export
- **Inventory Control** - Stock tracking, low-stock alerts, and audit trail
- **Customer Management** - Purchase history, loyalty points, and debt tracking
- **Supplier Management** - Purchase orders and payment tracking
- **Sales Reports** - Daily/monthly sales analytics and profit/loss statements
- **Loans/Debts** - Credit management with installment tracking
- **Multi-language** - Arabic (RTL) and English support
- **Dark/Light Theme** - Toggle between dark and light mode
- **Backup & Restore** - Database backup and restore functionality

## Technology Stack

- **Framework:** .NET 9 WPF
- **UI:** MaterialDesignInXamlToolkit
- **Architecture:** MVVM (CommunityToolkit.Mvvm)
- **ORM:** Entity Framework Core 9 + SQLite
- **Auth:** BCrypt password hashing
- **DI:** Microsoft.Extensions.DependencyInjection

## Getting Started

### Prerequisites

- .NET 9 SDK or later
- Visual Studio 2022 or JetBrains Rider

### Setup

```bash
# Clone the repository
git clone <repo-url>
cd KetabaPOS

# Restore dependencies
dotnet restore

# Apply migrations and seed data (auto-run on first launch)
dotnet run --project KetabaPOS.Desktop
```

### Default Login

- **Username:** admin
- **Password:** admin123

## Project Structure

```
KetabaPOS.Desktop/
├── Core/                  # Domain models, interfaces, enums
│   ├── Models/            # EF Core entities
│   ├── Interfaces/        # Service & repository contracts
│   └── Enums/             # Status enums
├── Infrastructure/        # Data access, services, repositories
│   ├── Data/              # DbContext, migrations, seeding
│   ├── Repositories/      # Data access layer
│   └── Services/          # Business logic implementation
├── Presentation/          # WPF UI layer
│   ├── ViewModels/        # MVVM ViewModels
│   ├── Views/             # XAML views
│   ├── Controls/          # Reusable custom controls
│   ├── Converters/        # Value converters
│   └── Resources/         # Styles, resources, localization
└── Configuration/         # App settings
```

## Database

SQLite database is stored at: `%LOCALAPPDATA%/KetabaPOS/ketaba.db`

## License

MIT
