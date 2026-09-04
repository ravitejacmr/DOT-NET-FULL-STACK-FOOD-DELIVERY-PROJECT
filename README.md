# FoodFlow — ASP.NET Core full-stack food delivery platform

This is the .NET implementation of the FoodFlow learning project. It keeps the Angular user experience and replaces Django REST Framework with ASP.NET Core 8 Minimal APIs, Entity Framework Core, JWT authentication, and XAMPP MariaDB/MySQL.

## Stack

- Backend: C#, ASP.NET Core 8, Minimal APIs, EF Core, Pomelo MySQL provider, JWT bearer authentication, Swagger/OpenAPI.
- Frontend: Angular 20 standalone components, TypeScript, RxJS, responsive custom CSS.
- Database: `foodflow_dotnet_db` on XAMPP MariaDB/MySQL.

The backend is divided into `Data`, `Models`, `Services`, and feature-focused `Endpoints`. Critical rules cover role and ownership authorization, one-restaurant carts, price snapshots, coupon calculations, order transitions, mock payments, delivery assignment, reviews, notifications, and role-specific dashboards.

## Prerequisites

- .NET 8 SDK
- Node.js 20 or newer and npm
- XAMPP MySQL/MariaDB on port 3306

## Run the application

1. Start **MySQL** in XAMPP.
2. Create the database in phpMyAdmin by importing `database/create_database.sql`, or run:

```powershell
C:\xampp\mysql\bin\mysql.exe -u root < database\create_database.sql
```

3. Start the API from the project root:

```powershell
.\start-backend.ps1
```

4. In another terminal, start Angular:

```powershell
.\start-frontend.ps1
```

Entity Framework creates the tables and seed data automatically the first time the API starts.

## URLs

| Service | URL |
|---|---|
| Angular home page | http://localhost:4200/ |
| REST API | http://127.0.0.1:5000/api/ |
| Swagger | http://127.0.0.1:5000/swagger/ |
| Health check | http://127.0.0.1:5000/api/health/ |
| phpMyAdmin | http://localhost/phpmyadmin/ |

## Demo accounts

All demo passwords are `FoodFlow@123`.

| Role | Email |
|---|---|
| Administrator | `admin@example.com` |
| Customer | `customer@example.com` |
| Restaurant owner | `owner@example.com` |
| Delivery partner | `delivery@example.com` |

## Configuration

The development connection string is in `backend/src/FoodFlow.Api/appsettings.json`. If the XAMPP root user has a password, update the `Password` value. For production, provide `ConnectionStrings__FoodFlow` and `Jwt__Key` through environment variables or secret storage and use HTTPS.

## Build verification

```powershell
dotnet build FoodFlow.sln
cd frontend
npm install
npm run build
```

Mock checkout does not charge real money. Image uploads, real email delivery, payment gateways, and live GPS are intentional extension points.
