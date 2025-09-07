# Vitura Orders API (.NET 8)

Single endpoint `GET /orders` that lists pharmacy orders with filtering, sorting, pagination, and a configurable `needsReview` rule.

---

## How to run locally

### Start the API

```bash
# From the solution root
dotnet restore
dotnet build -c Release

dotnet run -c Release --project VituraOrdersApi/VituraOrdersApi.csproj
```

### Try it

* Swagger UI (Development): `http://localhost:5000/swagger` (or the port shown in console)
* Endpoint:

  ```text
  GET /orders?pharmacyId=PH001&status=Pending&status=Shipped&from=2025-09-01&to=2025-09-07&sort=createdAt&dir=desc&page=1&pageSize=20
  ```

### Configuration

* Business rule threshold (set per environment):

  * `Review:DailyOrderThresholdCents` (default 100000)
* Correlation ID header: `X-Correlation-ID` (generated if not provided)
* Page bounds: `page >= 1`, `1 <= pageSize <= 100`

Environment variable overrides (examples):

```bash
# bash
export Review__DailyOrderThresholdCents=200000

# PowerShell
$env:Review__DailyOrderThresholdCents=200000
```

---
