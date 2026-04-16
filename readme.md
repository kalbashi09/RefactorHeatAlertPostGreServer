# HeatAlert Refactored – Complete Project Documentation

## 1. Overview

**HeatAlert** is a real‑time heat index monitoring and alerting system for Talisay City, Cebu. It collects temperature readings from a network of **static sensors**, **mobile Telegram‑based sensors**, and **external devices** (such as hardware sensors or virtual simulators like Wokwi). When dangerous heat levels are detected, alerts are broadcast via a Telegram bot. A live web map and an admin dashboard provide visualisation and management.

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
│   │   ├── Sensor.cs             # Includes IsExternal flag
│   │   ├── HeatLog.cs
│   │   ├── Subscriber.cs
│   │   ├── AdminUser.cs
│   │   └── AlertResult.cs        # Not a DB entity
│   ├── Dto/
│   │   ├── SensorDto.cs
│   │   ├── HeatLogDto.cs
│   │   ├── AuthDto.cs
│   │   ├── WokwiReadingDto.cs    # For external sensor data
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

| Table Name        | Purpose                                      |
| ----------------- | -------------------------------------------- |
| `sensor_registry` | Static, mobile, and external sensor metadata |
| `heat_logs`       | Time‑series heat index readings              |
| `subscribers`     | Telegram chat IDs subscribed to alerts       |
| `auth_personnel`  | Admin user credentials (hashed)              |

### 5.3 Sensor Types (`IsExternal` Flag)

The `Sensor` entity includes a boolean `IsExternal` property (default `false`). This flag distinguishes sensors that receive data from external sources (real hardware, virtual simulators) from those that are simulated internally.

| Sensor Type                     | `IsExternal` | Data Source                               |
| ------------------------------- | ------------ | ----------------------------------------- |
| Internal static/mobile          | `false`      | `SimulationBackgroundService` (every 30s) |
| External (Wokwi, real hardware) | `true`       | External device HTTP POST requests        |

### 5.4 Repositories

Each aggregate has a repository interface and implementation:

- `ISensorRepository` / `SensorRepository`
- `IHeatLogRepository` / `HeatLogRepository`
- `ISubscriberRepository` / `SubscriberRepository`
- `IAdminUserRepository` / `AdminUserRepository`

These abstract database access and are registered as **scoped** services.

### 5.5 Automatic Data Pruning

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

#### 🔍 Automatic Barangay Detection from GPS Coordinates

The system includes a **geospatial auto‑fill capability** that assigns the correct barangay to any sensor when only latitude and longitude are provided. This is used both in the admin dashboard and during Telegram bot mobile sensor creation.

**How It Works:**

1. The `GeoService` singleton loads the GeoJSON boundary file (`talisaycitycebu.json`) once at startup and keeps it in memory.
2. When a sensor is created or updated via the API, the controller checks if the `barangay` field is **empty, null, or the placeholder "string"**.
3. If a barangay is missing, the controller calls `_geoService.GetBarangay(latitude, longitude)`.
4. The service uses a **point‑in‑polygon ray‑casting algorithm** to test which barangay’s polygon contains the given coordinate.
5. The detected barangay name is written back to the `Barangay` property and saved to the database.

**Where It's Applied:**
| Action | Auto‑Detection Trigger |
|--------|------------------------|
| Register new sensor (admin dashboard) | If `barangay` is empty or `"string"` |
| Update sensor coordinates (admin dashboard) | If latitude/longitude change and no explicit `barangay` is provided |
| Telegram mobile sensor location ping | Always (barangay is determined from the shared GPS location) |

**Fallback:** If the coordinate lies outside all known barangay polygons, the method returns `"Outside of Talisay City"`.

### 6.5 `ITelegramBotService` (Singleton)

- Wraps the `TelegramBotClient`.
- Implements `IUpdateHandler` to process commands and location messages.
- **Important**: Because it is a singleton, all scoped dependencies (repositories, `IAlertService`) are resolved **per‑handler** via `IServiceProvider.CreateScope()`.

### 6.6 External Sensor Integration (Wokwi & Real Hardware)

The system supports **external sensors**—devices or simulators that push temperature readings directly to the backend. These sensors are marked with `IsExternal = true` and are **excluded from the internal simulation engine**, ensuring that real data is never overwritten by simulated values.

#### How It Works

1. **Sensor Registration**: When an external sensor first sends data, it is automatically registered in the database with `IsExternal = true` (if not already present).
2. **Data Ingestion**: A dedicated API endpoint (`POST /api/alerts/wokwi-reading`) accepts temperature and humidity readings in JSON format.
3. **Simulation Exclusion**: The `SimulationBackgroundService` filters out any sensor with `IsExternal == true`, so the internal simulation never generates fake readings for external devices.

#### API Endpoint: `POST /api/alerts/wokwi-reading`

**Request Body:**

```json
{
  "temperature": 34.5,
  "humidity": 68.2
}
```

**Implementation (in `AlertsController.cs`):**

```csharp
[HttpPost("wokwi-reading")]
public async Task<IActionResult> ReceiveWokwiReading([FromBody] WokwiReadingDto reading)
{
    if (reading == null) return BadRequest();

    // Find or create the external sensor
    var sensor = await _sensorRepository.GetByCodeAsync("WOKWI-VIRTUAL-01");
    if (sensor == null)
    {
        sensor = new Sensor
        {
            SensorCode = "WOKWI-VIRTUAL-01",
            DisplayName = "Wokwi Virtual Sensor",
            Barangay = "Virtual Lab",
            Latitude = 0, Longitude = 0,
            IsActive = true,
            IsExternal = true   // ⭐ Marks it as external
        };
        sensor = await _sensorRepository.CreateAsync(sensor);
    }

    // Process the heat reading (saves to DB and broadcasts alerts if needed)
    await _alertService.ProcessHeatReadingAsync(sensor, (int)reading.Temperature);
    return Ok(new { message = "Reading processed" });
}
```

#### Filtering External Sensors in Simulation

In `SimulationBackgroundService.cs`:

```csharp
var sensors = await sensorRepository.GetAllActiveAsync(cancellationToken);
sensors = sensors.Where(s => !s.IsExternal).ToList();   // Exclude external sensors
```

#### Wokwi Virtual Sensor Setup

**What is Wokwi?**  
[Wokwi](https://wokwi.com) is an online simulator for Arduino, ESP32, and other microcontrollers. It allows you to prototype IoT devices entirely in the browser, including WiFi connectivity and virtual sensors like the DHT22.

**Firmware for ESP32 with DHT22 (Original Version – Real Sensor Readings)**

The following Arduino sketch runs on a virtual ESP32 in Wokwi. It reads the **simulated DHT22 sensor values** (which are generated by Wokwi's physics engine) and sends them to your HeatAlert backend every 10 seconds.

```cpp
#include <WiFi.h>
#include <HTTPClient.h>
#include <DHT.h>

// --- WiFi Configuration ---
const char* ssid = "Wokwi-GUEST"; // Wokwi's built-in virtual network
const char* password = "";        // No password required

// --- Server Configuration ---
const char* serverUrl = "https://refactorheatalertpostgreserver.onrender.com/api/alerts/wokwi-reading";

// --- Sensor Configuration ---
#define DHTPIN 15          // The pin your DHT22 is connected to
#define DHTTYPE DHT22
DHT dht(DHTPIN, DHTTYPE);

void setup() {
  Serial.begin(115200);
  dht.begin();

  // Connect to WiFi
  WiFi.begin(ssid, password);
  Serial.print("Connecting to WiFi");
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }
  Serial.println("\nWiFi connected!");
}

void loop() {
  // Read sensor data from the virtual DHT22
  float temperature = dht.readTemperature();
  float humidity = dht.readHumidity();

  // Check if readings are valid
  if (isnan(temperature) || isnan(humidity)) {
    Serial.println("Failed to read from DHT sensor!");
    delay(2000);
    return;
  }

  // Only send data if connected to WiFi
  if (WiFi.status() == WL_CONNECTED) {
    HTTPClient http;
    http.begin(serverUrl);
    http.addHeader("Content-Type", "application/json");

    // Create JSON payload
    String payload = "{\"temperature\":" + String(temperature) +
                     ",\"humidity\":" + String(humidity) + "}";

    // Send HTTP POST request
    int httpResponseCode = http.POST(payload);

    if (httpResponseCode > 0) {
      Serial.print("Data sent, response code: ");
      Serial.println(httpResponseCode);
    } else {
      Serial.print("Error sending data: ");
      Serial.println(http.errorToString(httpResponseCode).c_str());
    }
    http.end(); // Close connection
  } else {
    Serial.println("WiFi Disconnected");
  }

  delay(10000); // Send data every 10 seconds
}
```

**Setting Up Wokwi**

1. Go to [Wokwi.com](https://wokwi.com) and create a new **ESP32** project.
2. In the `diagram.json` tab, add a **DHT22** sensor and connect it to pin 15 (or change the `DHTPIN` accordingly).
3. Paste the above code into the `sketch.ino` tab.
4. Run the simulation – the virtual ESP32 will connect to the internet and start POSTing data to your backend.

**Local Development with Wokwi Private Gateway**  
When the backend is running on `localhost`, the Wokwi simulation cannot reach it directly. Use the **Wokwi Private Gateway**:

1. Download the `wokwigw` tool from [GitHub](https://github.com/wokwi/wokwigw/releases).
2. Run the executable on your computer.
3. In the Wokwi editor, press `F1` → **"Enable Private Wokwi IoT Gateway"**.
4. Change the `serverUrl` in the sketch to `http://host.wokwi.internal:5083/api/alerts/wokwi-reading`.

#### Real Hardware Sensors

The same pattern applies to any physical IoT device (Arduino, ESP32, Raspberry Pi) with a DHT22 or other temperature sensor. Simply point the device to the same `/api/alerts/wokwi-reading` endpoint (or create a new endpoint for each device). Set `IsExternal = true` for those sensors to prevent simulation override.

---

## 7. Background Services

Two hosted services run continuously.

### 7.1 `SimulationBackgroundService`

- Executes every **30 seconds**.
- Retrieves all active sensors **with `IsExternal == false`**.
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

| Method | Endpoint                                 | Description                                        |
| ------ | ---------------------------------------- | -------------------------------------------------- |
| `GET`  | `/api/health`                            | Health check (returns DB connection status)        |
| `GET`  | `/api/alerts/current`                    | Latest heat reading                                |
| `GET`  | `/api/alerts/history?limit=100&offset=0` | Paginated heat history (with sensor details)       |
| `POST` | `/api/alerts/wokwi-reading`              | Receive temperature/humidity from external sensors |
| `POST` | `/api/auth/login`                        | Admin login (`personnelId`, `passcode`)            |

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

> **Note**: The API key is validated via the `ApiKeyMiddleware` for all write operations.

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

| Symptom                                  | Likely Cause                              | Solution                                                                                    |
| ---------------------------------------- | ----------------------------------------- | ------------------------------------------------------------------------------------------- |
| Telegram bot not responding              | Bot token invalid or bot not started      | Check `BotSettings:TelegramToken` and that `TelegramBotService.StartReceiving()` is called. |
| CORS errors in browser                   | Frontend origin not allowed               | Update CORS policy in `Program.cs`.                                                         |
| Database connection failure              | Wrong connection string or Neon suspended | Verify `NEON_DATABASE_URL`; ping the database URL to wake it.                               |
| “HOTTEST NOW” badge missing              | Time parsing fails                        | Ensure `recordedAt` is present; use frontend fix with `toPHTime()`.                         |
| Manual simulation not broadcasting       | Override not set correctly                | Check `SimulationService.SetManualOverride` call in `TelegramBotService`.                   |
| External sensor overridden by simulation | `IsExternal` flag not set                 | Ensure sensor is created with `IsExternal = true`.                                          |

### 13.3 Adding New Sensors

Use the admin dashboard **Register New Sensor** form. The backend will automatically determine the barangay from coordinates if not provided. For external sensors, ensure `IsExternal` is set to `true`.

### 13.4 Updating the GeoJSON Boundary

Replace `sharedresource/talisaycitycebu.json` and restart the application. The `GeoService` loads the file once at startup.

---

## 14. Roadmap / Future Enhancements

- **JWT Authentication** for admin API (currently simple API key).
- **Real‑time WebSocket updates** for the map (instead of polling).
- **Historical analytics** – charts of heat trends per barangay.
- **Multi‑city support** – load multiple GeoJSON files dynamically.
- **Support for multiple external sensor types** with unique identifiers.

---

## 15. Conclusion

The refactored HeatAlert system is now:

- **Maintainable** – clear separation of concerns and dependency injection.
- **Scalable** – can easily add new sensors or extend functionality.
- **Cloud‑ready** – runs on Render + Neon with automatic keep‑alive.
- **Developer‑friendly** – well‑structured codebase with comprehensive logging.
- **Extensible** – supports both internal simulation and external hardware/virtual sensors.

This documentation serves as the definitive reference for understanding, deploying, and extending the application.
