# Bank Account Simulator

A small bank-account simulation REST API built with ASP.NET Core (.NET 10), EF Core and PostgreSQL.

**Features:** Registration and login (BCrypt password hashing, JWT tokens), balance, deposit, withdrawal
(the balance can never go below zero), operation history, Swagger UI with JWT support, unit tests.

## Quick start with Docker (one command)

```bash
docker compose up --build
```

Swagger UI: <http://localhost:8080/swagger>. The database schema is created automatically on first start.

## Run locally

Requirements: .NET 10 SDK, PostgreSQL.

1. Adjust the connection string in `appsettings.json` (`ConnectionStrings:DefaultConnection`).
2. Set a JWT signing key (at least 32 characters). In Development a demo key is already in
   `appsettings.Development.json`; for anything else use user-secrets or an environment variable:
   ```bash
   dotnet user-secrets init
   dotnet user-secrets set "Jwt:Key" "<long random string>"
   ```
3. Apply migrations and run:
   ```bash
   dotnet ef database update
   dotnet run --launch-profile http
   ```
4. Open <http://localhost:5175/swagger>.

## Run the tests

```bash
dotnet test
```

## Using the API

Swagger: call `POST /api/auth/login`, copy the token, click **Authorize**, paste it (without `Bearer `).

Or with curl (replace the port if you run with Docker: `8080`):

```bash
BASE=http://localhost:5175

# 1. Register
curl -X POST $BASE/api/auth/register -H "Content-Type: application/json" \
  -d '{"name":"Ann","email":"ann@example.com","password":"Secret123!"}'

# 2. Login -> {"token":"...","expiresAt":"..."}
TOKEN=$(curl -s -X POST $BASE/api/auth/login -H "Content-Type: application/json" \
  -d '{"email":"ann@example.com","password":"Secret123!"}' | sed -E 's/.*"token":"([^"]+)".*/\1/')

# 3. Deposit
curl -X POST $BASE/api/account/deposit -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"amount":150.50,"description":"Salary"}'

# 4. Withdraw
curl -X POST $BASE/api/account/withdraw -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"amount":40}'

# 5. Balance
curl $BASE/api/account/balance -H "Authorization: Bearer $TOKEN"

# 6. History (newest first, paged)
curl "$BASE/api/account/transactions?page=1&pageSize=20" -H "Authorization: Bearer $TOKEN"
```

## Endpoints

| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/register` | no | Create a customer and a default USD account |
| POST | `/api/auth/login` | no | Get a JWT |
| GET | `/api/auth/me` | yes | Identity from the token |
| GET | `/api/account/balance` | yes | Current balance |
| POST | `/api/account/deposit` | yes | Deposit money |
| POST | `/api/account/withdraw` | yes | Withdraw money (422 if insufficient funds) |
| GET | `/api/account/transactions` | yes | Paged operation history |

## Error format

All errors use the RFC 7807 "problem details" format:

```json
{
   "title": "Insufficient Funds",
   "status": 422,
   "detail": "Insufficient funds: balance is 10.00, requested 50.00.",
   "traceId": "00-..."
}
```

| Status | When |
|---|---|
| 400 | Invalid input (validation errors are listed per field in `errors`) |
| 401 | Missing/invalid token or wrong credentials |
| 404 | Account not found |
| 409 | E-mail already registered / concurrent-operation conflict |
| 422 | Insufficient funds |
| 500 | Unexpected error (details are logged, never sent to the client) |

## Project structure

```
BankAccSim.Tests/   xunit tests (in-memory DB, Moq)
Controllers/   HTTP only: routing, status codes, reading the user id from the token
Data/          DbContext
Dtos/          Request/response contracts - entities are never returned directly
Exceptions/    Business exceptions + global exception handler
Migrations/    EF Core migrations
Models/        EF Core entities
Services/      Business logic (AuthService, AccountService, TokenService)
```

## Design notes

- Passwords are hashed with BCrypt (salted, slow); the plain password is never stored or logged.
- The balance is not stored: it is the sum of all transactions, so it can't drift from the history.
- Money uses `decimal` / `numeric(18,2)`, never floating point.
- The customer id is taken from the JWT, so users can only access their own account.
- Withdrawals run in a serializable transaction so parallel requests can't overdraw the account.
