# @caseauth/angular-client

Generated Angular HttpClient services and models for the CaseAuth API.

**Do not hand-edit anything in `src/`.** It's regenerated from the backend's OpenAPI spec
(`../../openapi/openapi.json`) by [`ng-openapi-gen`](https://github.com/cyclosproject/ng-openapi-gen).
Any change belongs in the backend's controllers/contracts, not here.

## Regenerating

From the repo root, after changing any controller or request/response contract:

```bash
./scripts/generate-client.sh
```

This builds `CaseAuth.Api`, runs it briefly against a throwaway SQLite db to capture its live
OpenAPI spec into `openapi/openapi.json`, then regenerates this client from that spec. Commit
both the updated spec and the regenerated `src/` so consumers don't need .NET installed just to
pick up a new client.

## Using it

This package ships raw, uncompiled TypeScript (no build step) - it's meant to be consumed as a
local path dependency and compiled by the consuming Angular app's own build, e.g. from the
frontend repo: `npm install ../Backend-core-cases-auth-audit-/clients/angular`.

```ts
import { provideHttpClient } from '@angular/common/http';
import { ApiConfiguration } from '@caseauth/angular-client/src/api-configuration';
import { CaseAuthApi } from '@caseauth/angular-client/src/case-auth-api';
import { casesCreate } from '@caseauth/angular-client/src/fn/cases/cases-create';

// app bootstrap:
providers: [
  provideHttpClient(),
  { provide: ApiConfiguration, useValue: { rootUrl: 'http://localhost:5020' } },
],
```

```ts
// in a component/service:
private api = inject(CaseAuthApi);

createCase() {
  this.api.invoke(casesCreate, { body: { applicantFullName: 'Jane Doe' } })
    .subscribe(caseResponse => { ... });
}
```

Every call needs the dev-only `X-Dev-User` header (see the root README) - add it via an
`HttpInterceptor` in the consuming app rather than passing it per-call; this client has no
knowledge of auth beyond what the OpenAPI spec documents.

The `Idempotency-Key` and `If-Match` headers on `decisionsCreate` are real, typed parameters
(not something you have to remember to bolt on) - `If-Match` must be the case's current
`rowVersion` from the last `CaseResponse` you fetched.
