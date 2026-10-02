# Backend-core-cases-auth-audit-
a working skeleton (upload → stored case → visible in the UI) by mid-build, plus the authorization and audit guarantees
This person owns the hub and the API contract that everyone else codes against.

- Scaffold ASP.NET Core with EF Core and Postgres. SQLite is fine for the first hours. Create these entities: `Case`, `Applicant`, `Document`, `ExtractedField`, `Finding`, `AiReview`, `Decision`, `AuditEvent`.
- Publish the OpenAPI spec in the first two hours, and generate the Angular client from it.
- **Upload endpoint:** stream files to S3, or to local disk in fixture mode. Reject wrong file types and oversized files with clear error messages.
- **State machine:** enforce transitions on the server. Each transition and its audit event are written in a single transaction.
- **Auth:** use a development-only auth handler with seeded users (`analyst1`, `analyst2`, `supervisor`) and two demo firms. Every query is scoped to the caller's firm, taken from their identity and never from a request parameter. Document that this handler is for development only.
- **Decisions:** use an idempotency key and a concurrency token. A duplicate or stale approval is rejected, and no second audit event is written.
- **Audit events:** record actor, action, case, timestamp, outcome, correlation ID, and the AI review version that was approved. Keep PII out of logs.

## Status

Backend scaffold is up: entities, EF Core (SQLite by default, Postgres via config), the
dev-only auth handler, the case state machine, file upload, AI-review recording, and the
idempotent/concurrency-safe decision flow are implemented and covered by integration tests.
Not done yet: the Angular client generated from the OpenAPI spec, and a real S3 storage
backend (`Storage:Mode=S3` intentionally throws `NotImplementedException` for now).

## Running locally

Requires the .NET 8 SDK (`dotnet --version`).

```bash
# run the API (applies EF migrations automatically in Development)
cd src/CaseAuth.Api
dotnet run
# -> http://localhost:5020 (launchSettings.json's "http" profile), Swagger UI at /swagger

# run the tests
cd ../..
dotnet test
```

All endpoints require a seeded dev identity via the `X-Dev-User` header:
`analyst1` (FIRM-A, Analyst), `analyst2` (FIRM-B, Analyst), `supervisor` (FIRM-A, Supervisor).
This header-based handler only runs in the Development environment - see
`src/CaseAuth.Api/Auth/DevAuthenticationHandler.cs`.

Example flow (see `src/CaseAuth.Api/CaseAuth.Api.http` or Swagger for the full set):

```bash
curl -X POST localhost:5020/api/cases -H "X-Dev-User: analyst1" -H "Content-Type: application/json" \
  -d '{"applicantFullName":"Jane Doe"}'
# submit -> start-review -> ai-reviews -> request-decision, then as supervisor:
curl -X POST localhost:5020/api/cases/{id}/decisions -H "X-Dev-User: supervisor" \
  -H "Idempotency-Key: <uuid>" -H "If-Match: <case RowVersion>" \
  -H "Content-Type: application/json" -d '{"outcome":"Approved"}'
```

To switch to Postgres, set `Database:Provider=Postgres` and
`ConnectionStrings:Postgres` (see `appsettings.json`).
