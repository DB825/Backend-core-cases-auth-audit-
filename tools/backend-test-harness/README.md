# Backend test harness

A single dependency-free HTML page for manually exercising the whole case lifecycle while the
real Angular workbench (Teammate 5's job) doesn't exist yet. Not meant to ship or be reused as
production frontend code.

## Running it

1. Start the API (from the repo root):

   ```bash
   cd src/CaseAuth.Api
   dotnet run
   # -> http://localhost:5020
   ```

2. Serve this folder on port 4200, so its origin matches the API's CORS allowlist
   (`Program.cs` only allows `http://localhost:4200` - opening `index.html` directly via a
   `file://` URL, or serving it on any other port, will fail with a CORS error):

   ```bash
   cd tools/backend-test-harness
   python3 -m http.server 4200
   ```

3. Open <http://localhost:4200> in a browser.

## What it does

A sticky status bar at the top always shows the current case's id, firm, status and
`rowVersion`. Below it, one section per resource, each able to create/list independently:

- **Connection** - pick a seeded dev identity (`analyst1`/FIRM-A, `analyst2`/FIRM-B,
  `supervisor`/FIRM-A) sent as `X-Dev-User` on every request, and the API base URL.
- **Case** - create a case, or paste an existing id.
- **Pipeline transitions** - buttons for `extract` / `screen` / `mark-ai-reviewed` /
  `request-decision` / `request-documents`, i.e. the state machine in
  `Services/CaseStateMachine.cs`.
- **Upload a document** - `POST /api/cases/{caseId}/documents`, with the document list below it.
- **Extracted fields** - add fields to a document (click "Use" on a document row to target it).
- **AI reviews** - record a review; click "Use" on a row to reference its id from Decision.
- **Findings** - create a finding; click "Cite" on an extracted-field row to append its id to
  `sourceFieldIds`, so you can see a finding actually link back to the field it came from.
- **Decision** - the idempotency-key + `If-Match` flow. Two guided scenarios:
  - **Replay:** submit, then submit again with the *same* Idempotency-Key -> 200 with the same
    decision id, not a new one.
  - **Stale rejection:** submit once (don't click "Sync rowVersion" afterwards), then submit
    again with a *new* Idempotency-Key but the *old* `If-Match` -> 409. (The harness never
    auto-overwrites `If-Match` after an action for exactly this reason - only explicit case
    loads/refreshes and the "Sync rowVersion" button do.)
- **Audit trail** - every event recorded against the case, including the correlation id.

Every result box shows the real API response either way: on success, the resource returned; on
failure, the HTTP status, the error title, and the correlation id from the `problem+json` body -
so a rejected file type, an illegal state transition, or a stale decision all show up as the API
actually reports them, not a generic "something went wrong."

## What it isn't

No build step, no framework, no state beyond what's in the DOM (refreshing the page loses
everything except what's in the URL/inputs). If you need something closer to the real contract,
use the generated client in `clients/angular/` instead - this harness talks to the API with
plain `fetch()`, not the typed client.
