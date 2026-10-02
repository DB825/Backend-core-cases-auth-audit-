# Upload test harness

A single dependency-free HTML page for manually exercising the document upload endpoint while
the real Angular workbench (Teammate 5's job) doesn't exist yet. Not meant to ship or be reused
as production frontend code.

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
   cd tools/upload-test-harness
   python3 -m http.server 4200
   ```

3. Open <http://localhost:4200> in a browser.

## What it does

- Lets you pick a seeded dev identity (`analyst1`, `analyst2`, `supervisor`) sent as the
  `X-Dev-User` header on every request.
- Creates a case, or lets you paste an existing case id.
- Uploads a file against `POST /api/cases/{caseId}/documents` with a chosen document type.
- Shows the real API response either way: on success, the stored document's metadata; on
  failure, the HTTP status, the error title, and the correlation id from the `problem+json`
  body - so a rejected file type, an oversized file, or a missing/wrong `X-Dev-User` header
  all show up as the API actually reports them, not a generic "something went wrong."
- Lists the documents recorded against the current case, to confirm the upload actually
  persisted (not just that the request returned 2xx).

## What it isn't

No build step, no framework, no state beyond what's in the DOM. If you need something closer
to the real contract, use the generated client in `clients/angular/` instead - this harness
talks to the API with plain `fetch()`, not the typed client.
