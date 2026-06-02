# Contributing / Dev Notes

These are my working notes for running this project locally and picking up where I left off.

---

## Local setup

```bash
# 1. Clone and copy env
git clone https://github.com/yourusername/factorypulse.git
cd factorypulse
cp .env.example .env
# Edit .env and set a real DB_PASSWORD and JWT_SECRET

# 2. Start everything
docker compose up --build

# 3. Seed an admin user (run once after first boot)
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin1234!","fullName":"Admin User","role":"ADMIN"}'
# This will fail with 401 since there's no admin yet — see note below
```

> **First-time user seeding:** The auth service seeds a default admin on first run if no users exist.
> Username: `admin` / Password: `Admin1234!` — **change this immediately in production.**

---

## Running services individually

### Auth service (Spring Boot)
```bash
cd auth-service
mvn spring-boot:run
# Runs on :8080
```

### Inspection service (.NET)
```bash
cd inspection-service/src/main
dotnet run
# Runs on :5001
```

### Reporting service (.NET)
```bash
cd reporting-service/src/main
dotnet run
# Runs on :5002
```

### API Gateway
```bash
cd api-gateway/src/main
dotnet run
# Runs on :5000 — routes to all services
```

### Mobile app
```bash
cd mobile-app
npm install
npx expo start
# Then press 'a' for Android emulator or 'i' for iOS simulator
```

---

## Building the native C library

The inspection service uses a thin C library for parsing equipment status flags. Build it before running:

```bash
cd inspection-service/src/main/Native
gcc -shared -fPIC -O2 -o libstatusflag.so status_flags.c
# On macOS:
# gcc -shared -fPIC -O2 -dynamiclib -o libstatusflag.dylib status_flags.c
```

The .NET service loads this via P/Invoke. If the .so isn't found it logs a warning and falls back gracefully — everything still works, you just won't get detailed flag parsing.

---

## Things that are rough / known issues

- No refresh token yet — JWTs expire after 24h and you have to re-login
- Cross-service data sync (inspection → reporting) is currently pull-based (HTTP). This means reports query the inspection service on-demand. Works fine, but adds latency on large date ranges. Proper fix is a message queue.
- The mobile app only covers inspection entry and equipment view. Admin features (user management, status overrides) are missing from mobile.
- No integration tests yet — only unit tests for the auth service. Inspection and reporting services need test coverage.
- Docker healthchecks for the .NET services are missing — they sometimes start before SQL Server is ready on first boot. If they fail, just `docker compose restart inspection-service`.

---

## Project decisions I might revisit

**Why three separate services?** Mostly to learn the pattern. At this scale one service would be fine. But it was useful to figure out auth propagation, CORS across services, and SignalR through a gateway.

**Why both Spring Boot and .NET?** I wanted practice with both stacks. Auth in Spring Boot because Spring Security + JWT is well-documented and I wanted to learn it. Inspection and reporting in .NET because SignalR is native there and EPPlus/iTextSharp have good .NET APIs.

**Why YARP for the gateway?** It's a proper reverse proxy library, not just a manual HttpClient forwarder. Config-driven routing is clean and it handles WebSocket proxying (needed for SignalR) out of the box.
