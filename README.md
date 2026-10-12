# 🏢 CommunityHub

A desktop application for building management and community life, developed as a team project for the **Software Specification and Modeling 2025/2026** course. It is built using **.NET 10 / WPF** along with a **PostgreSQL** database.

The application features four roles (Building Manager, Tenant, Neighborhood Coordinator, Citizen). All users log in through a single shared form, and the corresponding screen opens based on their role.

---

## 📱 Building Manager (Manager)

The manager's screen is designed to look and behave like a **mobile application** (window size `430 × 750`, fixed size), even though it runs as a desktop WPF application.

### Design and Navigation

| Element | Description |
|---|---|
| **Header** | Dynamic title of the current page, back button `«`, and `☰` button for the side menu |
| **Frame Navigation** | All pages are loaded into a single `Frame` (`MainFrame`), just like screens in a mobile app |
| **Bottom Navigation** | Quick access: Buildings, Membership req., Rental req., Assemblies |
| **Side Menu** | Overlay with all options: Buildings, Building registration, Membership requests, Rental requests, Assemblies, Unresolved problems, Demo Mode, Log out |
| **Toast Notifications** | Green (success) and red (error) notifications in the top right corner |
| **Demo Mode** | Automatic walkthrough of the application (simulated clicks and text input), with a `STOP` button in the header |

### Features

- **Building Registration** – A two-step wizard (address, neighborhood, city and country, number of floors, apartment numbers per floor, building images)
- **Building Access Requests** – Review, filtering by status (Pending / Accepted / Denied), sorting by date, and approving or rejecting with an explanation
- **Memberships** – Overview of all building members (tenant, floor, apartment, join date)
- **Common Areas** – Creation of common rooms per floor with a calendar showing available and occupied days
- **Rental Requests** – Filtering by status (All / Pending / Pending (Date Change) / Approved / Rejected), selecting an available day for single-day rentals, and proposing a new date range for multi-day rentals
- **Notice Board** – Review of notices (active and archived, "Needs Help" / "Offers Help"), statistics by year or month, and **PDF report** generation (QuestPDF)
- **Homeowners Assemblies** – Scheduling assemblies (blocking time slots that are already taken or in the past), overview of details and attendance by apartment, and tenant-proposed topics
- **Problem Reporting** – Overview by building with priority and status, marking a problem as resolved (status changes to `Potentially resolved`), and a dedicated view for unresolved problems

---

## 🧱 Architecture

```
CommunityHub/
├── src/
│   ├── CommunityHub.Application/     # Domain, services, database access
│   │   ├── Database/
│   │   │   ├── PostgresConnection.cs
│   │   │   ├── Repositories/         # Npgsql repositories
│   │   │   └── Scripts/              # database.sql, seed.sql, migration_*.sql
│   │   ├── Domain/                   # Models and repository interfaces
│   │   └── Services/                 # Business logic + DTO classes
│   ├── CommunityHub.Ui/              # WPF user interface
│   │   ├── Views/
│   │   │   ├── ManagerViews/         # Manager (mobile layout)
│   │   │   ├── TenantViews/          # Tenant
│   │   │   ├── CoordinatorViews/     # Neighborhood Coordinator
│   │   │   └── CitizenViews/         # Citizen
│   │   ├── ViewModels/
│   │   ├── Converters/
│   │   ├── Infrastructure/           # ManagerDependencyInjector
│   │   └── Security/                 # UserSession
│   ├── BuildingLogo.jpg
│   └── PanoramaBuilding.jpg
└── CommunityHub.slnx
```

The application is split into two layers: **Application** (domain, services, repositories) and **Ui** (WPF windows and pages). All users are stored in a single `users` table, and the role is determined by the `role` column.

### Technologies

- **.NET 10**, **WPF** (XAML + code-behind, MVVM where applied)
- **PostgreSQL** + **Npgsql**
- **QuestPDF** – PDF report generation

---

## 🚀 Getting Started

### Prerequisites

1. [.NET 10 SDK](https://dotnet.microsoft.com/download)
2. Visual Studio with WPF support (Windows)
3. [PostgreSQL](https://www.postgresql.org/download/) and pgAdmin

### Database Setup

1. Create a database named `communityhub`.
2. In pgAdmin, run the scripts from `src/CommunityHub.Application/Database/Scripts/` in the following order:
   1. `database.sql`
   2. `migration_add_rental_system.sql`
   3. `migration_add_booking_system.sql`
   4. `migration_add_help_ads_type.sql`
   5. `migration_add_homeowners_association_tables.sql`
   6. `migration_add_problems_table.sql`
   7. `seed.sql`
3. Verify the connection string in `src/CommunityHub.Application/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=communityhub;Username=postgres;Password=postgres"
  }
}
```

> ⚠️ The default port in the configuration is **5433**. If your PostgreSQL is running on the standard port **5432**, modify the `Port` value accordingly.

### Running the Application

1. Open `CommunityHub.slnx` in Visual Studio.
2. Set `CommunityHub.Ui` as the **Startup project**.
3. Run the application (`F5`).

### Test Accounts

From the `seed.sql` script:

| Role | Username | Password |
|---|---|---|
| Manager | `marko` | `marko123` |
| Citizen | `ana` | `ana123` |
| Tenant | `petar` | `petar123` |
| Tenant | `jovana` | `jovana123` |
| Coordinator | `stefan` | `stefan123` |

To preview the manager's mobile interface, log in as **marko / marko123**, and you can then start **Demo Mode** from the side menu (`☰`).

---

## 🛠️ Troubleshooting

| Error | Cause | Solution |
|---|---|---|
| `Connection refused` | PostgreSQL is not running or the port is incorrect | Start the service and verify the `Port` in `appsettings.json` |
| `Relation '...' does not exist` | Not all SQL scripts have been executed | Run `database.sql` and all `migration_*.sql` scripts |
| Incorrect username or password | `seed.sql` has not been executed | Run `seed.sql` |

---

## 📄 Documentation

The complete feature specification can be found in the PDF file located at the root of the repository (`Specifikacija projekta za četvoročlane timove - 2026-2.pdf`).