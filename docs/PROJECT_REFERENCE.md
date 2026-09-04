# FoodFlow project reference

Keep this document as the first place to check when returning to the project later.

## Project locations

| Project | Local folder |
|---|---|
| Python/Django version | `D:\01 - PROJECTS\01 - NOTES\FULL STACK PYTHON NOTES\FULL STACK PYTHON PROJECT 1\food_delivery_app` |
| ASP.NET Core version | `D:\01 - PROJECTS\01 - NOTES\DOT NET NOTES\DOT NET FULL STACK PROJECT` |
| .NET GitHub repository | `https://github.com/ravitejacmr/DOT-NET-FULL-STACK-FOOD-DELIVERY-PROJECT` |

The Python project and .NET project are independent. Changes in one do not automatically update the other.

## Port assignments

Two applications cannot listen on the same IP address and port at the same time.

### Recommended when running only one stack

| Service | Port |
|---|---:|
| Python Angular frontend | `4200` |
| Django REST API | `8000` |
| .NET Angular frontend | `4200` |
| ASP.NET Core API | `5000` |
| XAMPP MariaDB/MySQL | `3306` |

Both Angular projects may use port `4200` when only one frontend is running.

### Recommended when running both stacks together

| Service | URL |
|---|---|
| Python Angular frontend | `http://localhost:4200/` |
| Django REST API | `http://127.0.0.1:8000/` |
| .NET Angular frontend | `http://localhost:4201/` |
| ASP.NET Core API | `http://127.0.0.1:5000/` |

Start the .NET frontend on port `4201` with:

```powershell
cd frontend
npm start -- --port 4201
```

The Angular API URL is configured in `frontend/src/environments/environment.ts`. The .NET version must point to `http://127.0.0.1:5000/api`.

## Databases

| Stack | Database |
|---|---|
| Python/Django | `food_delivery_db` |
| ASP.NET Core | `foodflow_dotnet_db` |

Start MySQL in XAMPP before starting either backend. Create the .NET database with `database/create_database.sql`.

The .NET API uses `Database.EnsureCreatedAsync()` during development. It creates tables and runs the seed routine when the database is empty. This is convenient for learning and local development; use EF Core migrations before a real production deployment.

## Starting the .NET application

From the .NET project root, open two PowerShell terminals.

Terminal 1:

```powershell
.\start-backend.ps1
```

Terminal 2:

```powershell
.\start-frontend.ps1
```

Important URLs:

- Home: `http://localhost:4200/`
- API: `http://127.0.0.1:5000/api/`
- Swagger: `http://127.0.0.1:5000/swagger/`
- Health check: `http://127.0.0.1:5000/api/health/`
- phpMyAdmin: `http://localhost/phpmyadmin/`

If the Python frontend is already using `4200`, start the .NET frontend manually on `4201` instead.

## Demo accounts

All local demo accounts use the password `FoodFlow@123`.

| Role | Email |
|---|---|
| Administrator | `admin@example.com` |
| Customer | `customer@example.com` |
| Restaurant owner | `owner@example.com` |
| Second restaurant owner | `owner2@example.com` |
| Delivery partner | `delivery@example.com` |

These credentials are only for local development. Never reuse them in a hosted environment.

## Backend structure

| Location | Purpose |
|---|---|
| `backend/src/FoodFlow.Api/Program.cs` | Services, middleware, authentication, CORS, Swagger, endpoint registration, and startup seeding |
| `backend/src/FoodFlow.Api/Models/Entities.cs` | Database entities and role constants |
| `backend/src/FoodFlow.Api/Data/FoodFlowDbContext.cs` | EF Core database context, indexes, relationships, and decimal configuration |
| `backend/src/FoodFlow.Api/Data/DatabaseSeeder.cs` | Demo users, restaurants, foods, coupons, address, and notification |
| `backend/src/FoodFlow.Api/Services/JwtTokenService.cs` | JWT access tokens and rotating refresh tokens |
| `backend/src/FoodFlow.Api/Endpoints/AuthEndpoints.cs` | Registration, login, refresh, logout, profile, password, and address APIs |
| `backend/src/FoodFlow.Api/Endpoints/CatalogEndpoints.cs` | Restaurants, categories, foods, search, filters, and owner management |
| `backend/src/FoodFlow.Api/Endpoints/CartOrderEndpoints.cs` | Cart, coupons, checkout, orders, transitions, cancellation, and mock payments |
| `backend/src/FoodFlow.Api/Endpoints/EngagementEndpoints.cs` | Favourites, reviews, notifications, and coupon administration |
| `backend/src/FoodFlow.Api/Endpoints/DashboardDeliveryEndpoints.cs` | Customer, owner, rider, and administrator dashboards plus delivery assignment |

## Main business rules

- Authentication uses short-lived JWT access tokens and rotating refresh tokens.
- Authorization checks both the user role and resource ownership.
- A customer cart can contain food from only one restaurant.
- Checkout stores food names, prices, and the delivery address as snapshots so later edits do not change old orders.
- The cart calculates 5% tax, delivery fee, coupon discount, and grand total centrally.
- Restaurant flow: `pending` → `accepted` → `preparing` → `ready`.
- Delivery flow: `ready` → `assigned` → `picked_up` → `on_the_way` → `delivered`.
- Customers can cancel only early-stage orders and review only their delivered orders.
- Online payment is a simulator and never charges real money.
- Delivery GPS, image upload storage, email sending, and a real payment gateway remain extension points.

## Configuration and secrets

Development configuration is in `backend/src/FoodFlow.Api/appsettings.json`.

- Change the connection-string password if the XAMPP `root` account has a password.
- The included JWT key is a development-only value.
- For deployment, set `ConnectionStrings__FoodFlow` and `Jwt__Key` using environment variables or secret storage.
- Use HTTPS and restrict CORS origins before deployment.
- Do not commit real database passwords, API keys, payment credentials, or production JWT keys.

## Build and verification commands

Backend:

```powershell
C:\Users\ADMIN\AppData\Local\Microsoft\dotnet-sdk\dotnet.exe build FoodFlow.sln
```

If a system-wide .NET SDK is installed, use `dotnet build FoodFlow.sln` instead.

Frontend:

```powershell
cd frontend
npm install
npm run build
```

Verified on 4 September 2026:

- ASP.NET Core build completed with zero errors and zero warnings.
- Angular production build completed successfully.
- API health, restaurant listing, login, token refresh, cart, and all role dashboards were exercised.
- A complete order was tested from customer checkout through restaurant preparation, rider delivery, payment completion, and customer review.

## Common problems

### `ERR_CONNECTION_REFUSED` on port 4200

The Angular server is not running. Execute `.\start-frontend.ps1` and wait for compilation to finish.

### API connection refused on port 5000

Run `.\start-backend.ps1`. If startup fails, confirm XAMPP MySQL is running and that `foodflow_dotnet_db` exists.

### `Unable to connect to any of the specified MySQL hosts`

Start MySQL in XAMPP, confirm port `3306`, and verify the connection string in `appsettings.json`.

### `Access denied for user 'root'`

Add the local XAMPP root password to the development connection string. Do not commit the password.

### Port already in use

Stop the other application using the port or start the second Angular frontend on `4201`.

### .NET runtime exists but the SDK is missing

Install the .NET 8 SDK. This computer also has a local SDK at `C:\Users\ADMIN\AppData\Local\Microsoft\dotnet-sdk\dotnet.exe`.

## Git workflow

Before pushing future changes:

```powershell
git status
dotnet build FoodFlow.sln
cd frontend
npm run build
cd ..
git add .
git commit -m "Describe the change"
git push
```

Build output, logs, `node_modules`, and local IDE files are excluded by `.gitignore`.
