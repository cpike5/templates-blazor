# Blazor Template - Purpose

This is a Blazor Server template for rapid prototyping with authentication, role-based navigation, and basic user management. It provides a foundation for building web applications with ASP.NET Core Identity and configurable navigation.

## Key Features

- ASP.NET Core Identity with Entity Framework
- Role-based access control (Administrator, User roles)
- Google OAuth support
- Configurable navigation system via appsettings.json
- Responsive sidebar layout
- User management services
- Serilog logging

## Tech Stack

- .NET 8, Blazor Server
- Entity Framework Core, SQL Server
- ASP.NET Core Identity
- Bootstrap 5, Font Awesome 6
- Serilog

## Project Structure

```
src/blazor-template/
├── Components/
│   ├── Account/          # Identity pages and components
│   ├── Layout/           # Layout components (navbar, sidebar)
│   └── Pages/            # Application pages
├── Configuration/        # Configuration models
├── Data/                 # Entity Framework context and models
├── Services/             # Business logic services
└── wwwroot/             # Static files (CSS, JS)
```

## Database

The application uses Entity Framework Core with SQL Server. The database schema is managed through migrations in the `Data/Migrations` folder.

**Key tables:**
- `AspNetUsers` - User accounts
- `AspNetRoles` - Application roles
- `AspNetUserRoles` - User-role assignments