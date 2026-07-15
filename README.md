# Trading Course Platform

This repository contains the source code for the APSRA Trading Course Platform, an ASP.NET Core (.NET 9) Web API and MVC web application.

## Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [SQL Server LocalDB](https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb) (installed by default with Visual Studio Data Storage workload) or any SQL Server instance.
- Visual Studio 2022 or VS Code.

## Getting Started (Fetch and Run)

Follow these instructions to quickly get the project running on your local machine.

### 1. Clone the Repository
```bash
git clone https://github.com/eramit70/course-selling-platform.git
cd course-selling-platform
```

### 2. Restore Dependencies
Run the following command in the root folder (where `APSRA_TradingCourse.sln` is located) to restore NuGet packages:
```bash
dotnet restore APSRA_TradingCourse.sln
```

### 3. Database Configuration
By default, the application runs in `Development` environment and targets a local SQL Server Express database named `TradingCourseDb`. 

You can review the connection string in:
`TradingCourse.Web/appsettings.Development.json`

### 4. Apply Migrations (Create Database)
Before running the application, you must apply Entity Framework Core migrations to generate the local database schema.
From the command line (make sure you are in the Web project directory or specify it):
```bash
dotnet ef database update --project TradingCourse.Web/TradingCourse.Web.csproj
```
*(Note: If you use Package Manager Console in Visual Studio, simply run `Update-Database` with the Default Project set to `TradingCourse.Web`)*

### 5. Build and Run
You can run the application either through Visual Studio by pressing **F5**, or via the CLI:
```bash
dotnet run --project TradingCourse.Web/TradingCourse.Web.csproj
```

The application should start and be accessible via `https://localhost:5001` (or whichever port is assigned).

## Project Structure
- **TradingCourse.Web**: The main ASP.NET Core application containing API Controllers, MVC Views, and Razor pages for the Admin dashboard.
- **TradingCourse.Application**: Contains the core business logic, application services, models, and Entity Framework DbContext.
- **TradingCourse.Shared**: Contains shared DTOs, API wrappers, and constants used across layers.
