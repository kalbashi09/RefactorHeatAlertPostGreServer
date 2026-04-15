# HeatAlert Refactored – Complete Project Documentation

## 1. Overview

**HeatAlert** is a real‑time heat index monitoring and alerting system for Talisay City, Cebu. It simulates heat readings from a network of static sensors (and temporary mobile sensors) and broadcasts alerts via a Telegram bot when dangerous heat levels are detected. A live web map and an admin dashboard provide visualisation and management.

This document describes the **fully refactored version** of the system, which replaces a monolithic, raw‑SQL spaghetti codebase with a clean, layered architecture built on **.NET 10**, **Entity Framework Core**, **PostgreSQL**, and **Telegram.Bot**. The refactoring introduces dependency injection, separation of concerns, automated background services, and a robust deployment setup for **Render** and **Neon**.

---

## 2. High‑Level Architecture

The application follows a **clean/onion architecture** with the following layers:

```
┌─────────────────────────────────────────────────────────────┐
│                     Presentation Layer                       │
│  (Minimal API Controllers, Telegram Bot Interface)          │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                     Application Layer                        │
│  (Services: Simulation, Alert, Notification, Geo, Telegram) │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                        Domain Layer                          │
│  (Entities, DTOs, Enums, Interfaces)                        │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Infrastructure Layer                      │
│  (EF Core DbContext, Repositories, Background Services,     │
│   External API Integrations)                                │
└─────────────────────────────────────────────────────────────┘
```

All external dependencies (database, Telegram Bot API, GeoJSON file) are abstracted behind interfaces, making the system testable and maintainable.

---

## 3. Project Structure

```
RefactorHeatAlertPostGre/
├── Controllers/                  # Minimal API endpoints
│   ├── AlertsController.cs
│   ├── AuthController.cs
│   ├── HealthController.cs
│   ├── SensorsController.cs
│   └── SubscribersController.cs
├── Data/                         # EF Core DbContext and Repositories
│   ├── AppDbContext.cs
│   ├── DbInitializer.cs
│   ├── UnitOfWork.cs
│   ├── Configurations/           # Fluent API entity configurations
│   │   ├── SensorConfiguration.cs
│   │   ├── HeatLogConfiguration.cs
│   │   ├── SubscriberConfiguration.cs
│   │   └── AdminUserConfiguration.cs
│   └── Repositories/
│       ├── ISensorRepository.cs
│       ├── SensorRepository.cs
│       ├── IHeatLogRepository.cs
│       ├── HeatLogRepository.cs
│       ├── ISubscriberRepository.cs
│       ├── SubscriberRepository.cs
│       ├── IAdminUserRepository.cs
│       └── AdminUserRepository.cs
├── Infrastructure/               # Cross‑cutting concerns
│   ├── BackgroundServices/
│   │   ├── SimulationBackgroundService.cs
│   │   └── RenderKeepAliveService.cs
│   ├── Extensions/
│   │   └── ServiceCollectionExtensions.cs
│   └── Middleware/
│       └── ApiKeyMiddleware.cs
├── Models/                       # Domain models
│   ├── Entities/
│   │   ├── Sensor.cs
│   │   ├── HeatLog.cs
│   │   ├── Subscriber.cs
│   │   ├── AdminUser.cs
│   │   └── AlertResult.cs        # Not a DB entity
│   ├── Dto/
│   │   ├── SensorDto.cs
│   │   ├── HeatLogDto.cs
│   │   ├── AuthDto.cs
│   │   └── ApiResponse.cs
│   └── Enums/
│       └── DangerLevel.cs
├── Services/                     # Business logic
│   ├── Interfaces/
│   │   ├── ISimulationService.cs
│   │   ├── IAlertService.cs
│   │   ├── INotificationService.cs
│   │   ├── IGeoService.cs
│   │   └── ITelegramBotService.cs
│   ├── SimulationService.cs
│   ├── AlertService.cs
│   ├── NotificationService.cs
│   ├── GeoService.cs
│   └── TelegramBotService.cs
├── sharedresource/               # Static GeoJSON boundary file
│   └── talisaycitycebu.json
├── Program.cs                    # Application entry point
├── appsettings.json              # Local configuration (not in source control)
├── appsettings.Development.json
├── Dockerfile                    # Production container definition
├── .dockerignore
├── .env                          # Environment variables template
└── RefactorHeatAlertPostGre.csproj
```

---

## 4. Setup & Configuration

### 4.1 Prerequisites

- **.NET SDK 10.0** or later
- **PostgreSQL** (local or Neon cloud)
- **Telegram Bot Token** (from [@BotFather](https://t.me/BotFather))
- **Render account** (for deployment, optional)

### 4.2 Local Development

1. **Clone the repository** and navigate to the project root.
2. **Restore packages**:
   ```bash
   dotnet restore
   ```
3. **Configure secrets** (recommended):
   ```bash
   dotnet user-secrets set "BotSettings:TelegramToken" "YOUR_BOT_TOKEN"
   dotnet user-secrets set "ApiSettings:ApiKey" "YOUR_API_KEY"
   ```
4. **Set up the database connection**:
   - Edit `appsettings.json` or use environment variables.
   - Example local connection string:
     ```json
     "DefaultConnection": "Host=localhost;Port=5432;Database=HeatIndicatorV2;Username=postgres;Password=yourpassword;Pooling=true"
     ```
5. **Apply database migrations**:
   ```bash
   dotnet ef database update
   ```
6. **Run the application**:
   ```bash
   dotnet run
   ```
   The API will be available at `http://localhost:5083` (or the port configured in `launchSettings.json`).

### 4.3 Environment Variables

The application supports the following environment variables (overriding `appsettings.json`):

| Variable                     | Description                                                    | Required       |
| ---------------------------- | -------------------------------------------------------------- | -------------- |
| `NEON_DATABASE_URL`          | Neon PostgreSQL connection URL (overrides `DefaultConnection`) | For production |
| `DATABASE_URL`               | Alternative name for Neon URL                                  | For production |
| `BotSettings__TelegramToken` | Telegram Bot token                                             | Yes            |
| `ApiSettings__ApiKey`        | API key for protected endpoints                                | Yes            |
| `RENDER_PING_URL`            | URL for keep‑alive self‑ping (defaults to Render backend URL)  | No             |

---

## 5. Database Layer

### 5.1 Entity Framework Core Setup

- **DbContext**: `AppDbContext` with `DbSet<T>` for `Sensor`, `HeatLog`, `Subscriber`, `AdminUser`.
- **Fluent Configurations**: Located in `Data/Configurations/`. They map entity properties to PostgreSQL table/column names and define indexes.
- **Migrations**: Use `dotnet ef migrations add <Name>` and `dotnet ef database update` to manage schema changes.

### 5.2 Tables

| Table Name        | Purpose                                |
| ----------------- | -------------------------------------- |
| `sensor_registry` | Static and mobile sensor metadata      |
| `heat_logs`       | Time‑series heat index readings        |
| `subscribers`     | Telegram chat IDs subscribed to alerts |
| `auth_personnel`  | Admin user credentials (hashed)        |

### 5.3 Repositories

Each aggregate has a repository interface and implementation:

- `ISensorRepository` / `SensorRepository`
- `IHeatLogRepository` / `HeatLogRepository`
- `ISubscriberRepository` / `SubscriberRepository`
- `IAdminUserRepository` / `AdminUserRepository`

These abstract database access and are registered as **scoped** services.

### 5.4 Automatic Data Pruning

- The `SimulationBackgroundService` calls `IHeatLogRepository.PruneOldLogsAsync(keepCount: 3000)` when the total log count exceeds **3500**.
- This keeps the database size small (≈ 3 MB for 3000 logs).

---

## 6. Core Services

All business logic is encapsulated in services.

### 6.1 `ISimulationService` (Singleton)

- **`GenerateReading(Sensor sensor)`**: Produces a realistic heat index based on baseline temperature and random fluctuations.
- **`GetDangerLevel(int heatIndex)`**: Maps heat index to `DangerLevel` enum (Cool → Extreme Danger).
- **`CreateAlertResult(Sensor sensor, int heatIndex)`**: Creates an `AlertResult` domain object.
- **Manual Override**: Static methods (`SetManualOverride`, `DecrementManualSession`) allow temporary fixed heat values (used by Telegram simulation commands).

### 6.2 `IAlertService` (Scoped)

- **`ProcessHeatReadingAsync`**: Saves a `HeatLog` and, if the heat index ≥ 38°C, broadcasts an immediate alert via `INotificationService`.
- **`BroadcastHeartbeatSummaryAsync`**: Every 30 seconds, sends a summary of all alarming locations (with inline keyboard button to open the live map).

### 6.3 `INotificationService` (Scoped)

- Handles actual Telegram message sending.
- **`BroadcastAlertAsync`**: Sends plain text to all active subscribers.
- **`BroadcastAlertWithKeyboardAsync`**: Sends a message with an inline keyboard button (used for the radar link).

### 6.4 `IGeoService` (Singleton)

- Loads the `talisaycitycebu.json` GeoJSON file once at startup.
- **`GetBarangay(latitude, longitude)`**: Determines which barangay a coordinate falls into using ray‑casting point‑in‑polygon algorithm.
- **`IsValidCoordinate`**, **`CalculateDistance`**, **`GetAllBarangays`**.

### 6.5 `ITelegramBotService` (Singleton)

- Wraps the `TelegramBotClient`.
- Implements `IUpdateHandler` to process commands and location messages.
- **Important**: Because it is a singleton, all scoped dependencies (repositories, `IAlertService`) are resolved **per‑handler** via `IServiceProvider.CreateScope()`.

---

## 7. Background Services

Two hosted services run continuously.

### 7.1 `SimulationBackgroundService`

- Executes every **30 seconds**.
- Retrieves all active sensors.
- For each sensor, generates a reading (or uses manual override if active).
- Saves logs and triggers alerts.
- Broadcasts heartbeat summary.
- Prunes old logs when needed.

### 7.2 `RenderKeepAliveService`

- Pings the application’s own public URL every **10 minutes**.
- Prevents Render’s free tier from spinning down due to inactivity.

---

## 8. API Endpoints

All endpoints are prefixed with `/api`. Responses are wrapped in a standard `ApiResponse<T>`:

```json
{
  "success": true,
  "message": "Success",
  "data": { ... },
  "errors": []
}
```

### 8.1 Public Endpoints (No API Key)

| Method | Endpoint                                 | Description                                  |
| ------ | ---------------------------------------- | -------------------------------------------- |
| `GET`  | `/api/health`                            | Health check (returns DB connection status)  |
| `GET`  | `/api/alerts/current`                    | Latest heat reading                          |
| `GET`  | `/api/alerts/history?limit=100&offset=0` | Paginated heat history (with sensor details) |
| `POST` | `/api/auth/login`                        | Admin login (`personnelId`, `passcode`)      |

### 8.2 Protected Endpoints (Require `X-API-KEY` header)

| Method   | Endpoint                            | Description                                        |
| -------- | ----------------------------------- | -------------------------------------------------- |
| `GET`    | `/api/sensors?includeInactive=true` | List all sensors                                   |
| `GET`    | `/api/sensors/{id}`                 | Get sensor by ID                                   |
| `GET`    | `/api/sensors/code/{code}`          | Get sensor by code                                 |
| `POST`   | `/api/sensors`                      | Register a new sensor                              |
| `PATCH`  | `/api/sensors/{id}`                 | Update sensor (partial)                            |
| `DELETE` | `/api/sensors/{id}`                 | Delete sensor and its logs                         |
| `POST`   | `/api/alerts/report`                | Submit a manual heat report (for external sensors) |
| `GET`    | `/api/subscribers`                  | List active subscribers                            |
| `POST`   | `/api/subscribers`                  | Add a subscriber (Telegram chat ID)                |
| `DELETE` | `/api/subscribers/{chatId}`         | Unsubscribe                                        |

> **Note**: The API key is validated in `AlertsController.ReportHeat` and `SensorsController` write operations. A global middleware (`ApiKeyMiddleware`) is available but not enabled by default.

---

## 9. Telegram Bot Commands

| Command                         | Action                                          |
| ------------------------------- | ----------------------------------------------- |
| `/start` or `/subscribeservice` | Subscribe to alerts                             |
| `/unsubscribeservice`           | Unsubscribe                                     |
| `/status`                       | Show active sensors and subscriber count        |
| `/help`                         | List all commands                               |
| `/exdanger`                     | Simulate **Extreme Danger** (requires location) |
| `/danger`                       | Simulate **Danger**                             |
| `/caution`                      | Simulate **Caution**                            |
| `/normal`                       | Simulate **Normal**                             |
| `/cool`                         | Simulate **Cool**                               |

After sending a simulation command, the bot requests the user’s live location. A temporary `MOBILE_{chatId}` sensor is created (if not already existing) and its location updated. The sensor remains active for **5 cycles** (2.5 minutes) and then auto‑deactivates.

---

## 10. Deployment

### 10.1 Docker

The included `Dockerfile` uses a multi‑stage build:

- **Build stage**: `mcr.microsoft.com/dotnet/sdk:10.0`
- **Runtime stage**: `mcr.microsoft.com/dotnet/aspnet:10.0`
- Timezone set to `Asia/Manila`.
- Non‑root user `appuser` for security.

**Building and running locally**:

```bash
docker build -t heatalert-api .
docker run -d -p 5083:80 --env-file .env heatalert-api
```

### 10.2 Render.com

1. **Backend Web Service**:
   - **Runtime**: Docker
   - **Dockerfile Path**: `./Dockerfile`
   - **Environment Variables**: Set `NEON_DATABASE_URL`, `BotSettings__TelegramToken`, `ApiSettings__ApiKey`.
   - **Health Check Path**: `/api/health`

2. **Frontend Static Site**:
   - Host the `mapUI.html`, `admindash.html`, `logindash.html`, and associated JS/CSS files.
   - Configure CORS on the backend to allow the frontend origin (`https://heatsync-zs03.onrender.com`).

### 10.3 Neon Database

- Use the **pooled connection URL** (port 5432).
- The connection string is built automatically by `ConvertPostgresUrlToConnString()`.
- SSL is required (`SSL Mode=Require;Trust Server Certificate=true`).

---

## 11. Frontend Integration

The frontend consists of three main pages:

- **`mapUI.html`** – Public live map displaying heat readings.
- **`admindash.html`** – Admin panel for sensor management and log export.
- **`logindash.html`** – Admin login.

**Key Configuration (`config.js`)**:

```javascript
const HEALERTSYS_CONFIG = {
  apiBase: "https://refactorheatalertpostgreserver.onrender.com/api",
  apiHistoryURL:
    "https://refactorheatalertpostgreserver.onrender.com/api/alerts/history",
  apiKey: "h43dsHfjKS956032b8a9e5c1f0e4b",
};
```

**Time Zone Handling**:

- Backend stores all timestamps in UTC.
- Frontend converts UTC `recordedAt` to Philippine Time (`Asia/Manila`) for display and for the “hottest now” logic.

**Excel Export**:

- Uses **ExcelJS** to generate `.xlsx` files with conditional formatting matching the heat index colour scale.

---

## 12. Security Considerations

- **Telegram Bot Token** and **API Key** must be stored securely (environment variables, never in source control).
- **Admin Passwords** are hashed using **BCrypt**.
- **CORS** is restricted to known frontend origins.
- **API Key** protects all write endpoints.
- The Docker container runs as a **non‑root user**.

---

## 13. Maintenance & Troubleshooting

### 13.1 Logging

- Logs are written to the console with `ILogger<T>`.
- In production, consider adding a logging provider (e.g., Serilog) for persistent logs.

### 13.2 Common Issues

| Symptom                            | Likely Cause                              | Solution                                                                                    |
| ---------------------------------- | ----------------------------------------- | ------------------------------------------------------------------------------------------- |
| Telegram bot not responding        | Bot token invalid or bot not started      | Check `BotSettings:TelegramToken` and that `TelegramBotService.StartReceiving()` is called. |
| CORS errors in browser             | Frontend origin not allowed               | Update CORS policy in `Program.cs`.                                                         |
| Database connection failure        | Wrong connection string or Neon suspended | Verify `NEON_DATABASE_URL`; ping the database URL to wake it.                               |
| “HOTTEST NOW” badge missing        | Time parsing fails                        | Ensure `recordedAt` is present; use frontend fix with `toPHTime()`.                         |
| Manual simulation not broadcasting | Override not set correctly                | Check `SimulationService.SetManualOverride` call in `TelegramBotService`.                   |

### 13.3 Adding New Sensors

Use the admin dashboard **Register New Sensor** form. The backend will automatically determine the barangay from coordinates if not provided.

### 13.4 Updating the GeoJSON Boundary

Replace `sharedresource/talisaycitycebu.json` and restart the application. The `GeoService` loads the file once at startup.

---

## 14. Roadmap / Future Enhancements

- **JWT Authentication** for admin API (currently simple API key).
- **Real‑time WebSocket updates** for the map (instead of polling).
- **Historical analytics** – charts of heat trends per barangay.
- **Multi‑city support** – load multiple GeoJSON files dynamically.

---

## 15. Conclusion

The refactored HeatAlert system is now:

- **Maintainable** – clear separation of concerns and dependency injection.
- **Scalable** – can easily add new sensors or extend functionality.
- **Cloud‑ready** – runs on Render + Neon with automatic keep‑alive.
- **Developer‑friendly** – well‑structured codebase with comprehensive logging.

This documentation should serve as the definitive reference for understanding, deploying, and extending the application.
