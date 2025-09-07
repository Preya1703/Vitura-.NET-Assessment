# Vitura Orders API (NET 8)

A single endpoint `GET /orders` that lists pharmacy orders with filtering, sorting, pagination, and a configurable `needsReview` rule.

---

## Overview

* **Tech**: ASP.NET Core 8 Web API, in‑memory data from `sample-orders.json` (no DB).
* **Endpoint**: `GET /orders` (filters, sort, pagination).
* **Business rule**: add `needsReview: true` if `totalCents > Review:DailyOrderThresholdCents` (from config).
* **Correlation**: reads/returns `X-Correlation-ID` per request.

---

## How to run locally

### Prerequisites

* [.NET 8 SDK](https://dotnet.microsoft.com/download)

### Start

```bash
# from solution root
 dotnet restore
 dotnet build -c Release
 dotnet run --project VituraOrdersApi/VituraOrdersApi.csproj -c Release
```

### Swagger

* Development only: `http://localhost:<port>/swagger`

---


* Env var override example:

```bash
# bash
export Review__DailyOrderThresholdCents=200000
```

* Correlation header: `X-Correlation-ID` (auto-generated if not provided).

---

## Data

* Loaded once at startup from `sample-orders.json` into an in‑memory list.
* Enums (e.g., `Status`, `PaymentMethod`, `DeliveryType`) are supported when serialized as strings.

---

## Endpoint

```
GET /orders
```

**Query parameters**

* `pharmacyId` (string, optional)
* `status` (repeatable, optional): e.g. `status=Pending&status=Shipped`
* `from`, `to` (ISO dates, optional): inclusive by date component of `createdAt`
* `sort` in `{createdAt,totalCents}`, default `createdAt`
* `dir` in `{asc,desc}`, default `desc`
* `page` int, default `1`
* `pageSize` int, default `20`, **max `100`**

**Response**

```json
{
  "page": 1,
  "pageSize": 20,
  "total": 1000,
  "items": [
    {
      "id": "...",
      "pharmacyId": "PH001",
      "status": "Pending",
      "createdAt": "2025-09-03T12:34:56Z",
      "totalCents": 12345,
      "itemCount": 3,
      "paymentMethod": "Card",
      "deliveryType": "Standard",
      "notes": null,
      "needsReview": false
    }
  ]
}
```

**Business rule**

* `needsReview = (totalCents > Review:DailyOrderThresholdCents)`.

**Validation**

* Returns `400 Bad Request` with a short message for invalid inputs, e.g.:

  * `page must be >= 1`
  * `pageSize must be between 1 and 100.`
  * `sort must be one of: createdAt, totalCents`
  * `dir must be one of: asc, desc`
  * `from must be <= to`

**Logging**

* One info log per request: correlation id, elapsed ms, page/pageSize, total, item count, sort/dir.

---

## Testing

Run unit tests:

```bash
 dotnet test -c Release
```

Included tests (service-level):

* `needsReview` applied correctly using config threshold.
* Pagination stability for same query.
* Filtering by `pharmacyId`, `status`, and date range.

---

## Indexing / query approach for production (SQL Server) — Key points

* **Clustered index**: `(CreatedAt ASC, Id ASC)` for append-friendly inserts; scan backwards for `createdAt DESC`.
* **Primary key**: `Id` as **nonclustered** to keep clustered key focused on feed ordering.
* **Nonclustered indexes**:

  * By **pharmacy**: `(PharmacyId, CreatedAt DESC, Id ASC)` with `INCLUDE (TotalCents, Status)`.
  * By **status**: `(Status, CreatedAt DESC, Id ASC)`; consider filtered indexes for hot statuses.
  * Optional for **totalCents** sort: `(TotalCents DESC, CreatedAt DESC, Id ASC)`.
* **Pagination**: handled in **code** using LINQ `OrderBy/ThenBy/Skip/Take` with stable ordering (tie-breaker `Id`) and page size cap (≤100).

---

## What I would do next

* Move data to **SQL Server** and add the indexes above.
* Keep **pagination in code** (LINQ) with stable ordering and page size cap.
* Add **better logs & metrics** (structured logs, timing, correlation scopes).
* **Redis caching** with TTL so we don't hit DB for every request.
* **Repository layer**:

    * Introduce **IOrdersRepository** for data access (async CRUD/query methods); implement with EF Core or Dapper for SQL Server.
* **FluentValidation** for richer input rules and consistent error messages.
* **JWT authentication (Bearer)**: wire ASP.NET Core auth, add minimal policy, and enable Swagger auth.
* **Rate limiting***: use ASP.NET Core rate limiting middleware `return 429`.
* **CORS Policy** if required.
 
---

## Sample requests (`orders.http`)

```http
@host = http://localhost:5000
@cid = {{$guid}}

### Default page
GET {{host}}/orders
X-Correlation-ID: {{cid}}

### Filter by pharmacy + statuses + date range
GET {{host}}/orders?pharmacyId=ph1&status=Pending&status=Shipped&from=2025-09-01&to=2025-09-03&sort=createdAt&dir=desc&page=1&pageSize=20
X-Correlation-ID: {{cid}}

### Sort by totalCents ascending
GET {{host}}/orders?sort=totalCents&dir=asc&page=1&pageSize=10
X-Correlation-ID: {{cid}}

### Bad input example (expect 400)
GET {{host}}/orders?pageSize=101
X-Correlation-ID: {{cid}}
```
