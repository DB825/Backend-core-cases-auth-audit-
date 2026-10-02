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
The OpenAPI spec (`openapi/openapi.json`) and the generated Angular client
(`clients/angular/`) are also in place - see `clients/angular/README.md` for how to consume it
and `scripts/generate-client.sh` to regenerate both after a contract change.

The case state machine matches the project's full architecture doc:
`Uploaded → Extracted → Screened → AiReviewed → AwaitingDecision → Approved / Rejected / Escalated`,
with a `request-documents` action that sends a case back to `Uploaded` from any point before a
decision. `Finding` also links back to the `ExtractedField` row(s) it was computed from (a
many-to-many, since a cross-document mismatch cites a field from each document) - needed for
Teammate 3's screening engine and acceptance criterion #1.

`Finding` also has a `Score` (nullable double, Teammate 3's weighted-sum rule score) and uses
`FindingSeverity: Low/Medium/High` - matching the vocabulary Teammates 3 and 4 are building
against, not the `Info/Warning/Critical` this started as.

`GET /api/cases/{caseId}/ai-review-input` assembles extracted fields (across every document on
the case, labeled with their document type) and findings (with severity/score/source-field-ids)
in one call, so Teammate 4's AI reviewer doesn't need to call `/documents`, then
`/extracted-fields` per document, then `/findings`, and stitch the result together itself. Per
the architecture doc, this is deliberately *all* the reviewer gets - no raw document text.

Not done yet: a real S3 storage backend (`Storage:Mode=S3` intentionally throws
`NotImplementedException` for now), and an actual Angular frontend app consuming the client.
`AiReview`'s shape (`modelName`/`modelVersion`/`recommendation`/`rationale`) is also simpler
than Teammate 4's planned output (`summary`, `key_concerns[]` citing finding IDs,
`recommended_next_steps[]`, `draft_case_note`, derived confidence) - expect that entity/contract
to grow when the AI review module is built.

The `Dockerfile` builds and runs correctly, but **the container won't start at all without
`ASPNETCORE_ENVIRONMENT=Development` set explicitly** - it defaults to `Production`, and
`Program.cs` refuses to register the dev-only auth handler there (on purpose, since there's no
real auth yet). Whoever wires up the Kubernetes manifests needs that env var in the
Deployment/ConfigMap for now, until real authentication exists.

## Demo dashboard and test data

`dashboard/` is the compliance review UI (case queue by risk, case detail with document
images, findings, AI review, decision buttons and audit trail). `demo/` holds five synthetic
applicant personas and a script that loads them into a running API. See
`dashboard/README.md` and `demo/README.md`.

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
# extract -> screen -> ai-reviews -> mark-ai-reviewed -> request-decision, then as supervisor:
curl -X POST localhost:5020/api/cases/{id}/decisions -H "X-Dev-User: supervisor" \
  -H "Idempotency-Key: <uuid>" -H "If-Match: <case RowVersion>" \
  -H "Content-Type: application/json" -d '{"outcome":"Approved"}'
```

To switch to Postgres, set `Database:Provider=Postgres` and
`ConnectionStrings:Postgres` (see `appsettings.json`).

## Running in Docker

```bash
docker build -t caseauth-api .
docker run -p 8080:8080 \
  -e ASPNETCORE_URLS=http://+:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  caseauth-api
# -> http://localhost:8080, Swagger UI at /swagger
```

`ASPNETCORE_ENVIRONMENT=Development` is required (see the note above) - without it the
container crashes on startup with `No production authentication handler is configured`. The
container uses an in-container SQLite file, so data doesn't persist across restarts; nothing
else is required to get a working API inside it (migrations apply automatically).
