// Loads the synthetic personas into a running CaseAuth API and walks each case through the
// pipeline up to AwaitingDecision, so the dashboard opens on a full queue ready for a
// supervisor to decide.
//
//   node demo/seed.mjs                      # API at http://localhost:5020
//   API_BASE=http://localhost:8080 node demo/seed.mjs
//
// Needs Node 18+ (built-in fetch/FormData). Run demo/make_specimens.py first so the
// document images exist. Every run creates new cases; delete the SQLite file to start over.
//
// The AI reviews written here are canned stand-ins for Teammate 4's Bedrock output, stored
// as JSON in the review's rationale until the AiReview contract grows those fields.
import { readFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const here = path.dirname(fileURLToPath(import.meta.url));
const base = (process.env.API_BASE ?? "http://localhost:5020").replace(/\/$/, "");
const analyst = process.env.SEED_USER ?? "analyst1";

async function call(method, url, body, { form } = {}) {
  const headers = { "X-Dev-User": analyst };
  let payload;
  if (form) {
    payload = form;
  } else if (body !== undefined) {
    headers["Content-Type"] = "application/json";
    payload = JSON.stringify(body);
  }
  const res = await fetch(base + url, { method, headers, body: payload });
  const text = await res.text();
  if (!res.ok) {
    throw new Error(`${method} ${url} -> ${res.status}: ${text}`);
  }
  return text ? JSON.parse(text) : null;
}

async function seedPersona(p) {
  const c = await call("POST", "/api/cases", {
    applicantFullName: p.applicant.fullName,
    applicantDateOfBirth: p.applicant.dateOfBirth ?? null,
    applicantEmail: p.applicant.email ?? null,
    applicantPhone: p.applicant.phone ?? null,
  });

  // "docKey.FIELD_NAME" -> extracted field id, so findings can cite their source fields.
  const fieldIds = {};
  for (const doc of p.documents) {
    const bytes = await readFile(path.join(here, "specimens", doc.file));
    const form = new FormData();
    form.append("documentType", doc.type);
    // Upload under the document's title so the dashboard tabs read "Form W-9", not a file slug.
    form.append("file", new Blob([bytes], { type: "image/png" }), `${doc.title}.png`);
    const uploaded = await call("POST", `/api/cases/${c.id}/documents`, undefined, { form });

    const fields = await call("POST", `/api/documents/${uploaded.id}/extracted-fields`, {
      fields: doc.fields.map((f) => ({ fieldName: f.name, fieldValue: f.value, confidence: f.confidence })),
    });
    for (const f of fields) fieldIds[`${doc.key}.${f.fieldName}`] = f.id;
  }
  await call("POST", `/api/cases/${c.id}/extract`);

  for (const f of p.findings) {
    const sourceFieldIds = f.fields.map((k) => {
      if (!fieldIds[k]) throw new Error(`${p.key}: finding ${f.code} cites unknown field ${k}`);
      return fieldIds[k];
    });
    await call("POST", `/api/cases/${c.id}/findings`, {
      severity: f.severity, source: "Ai", code: f.code, message: f.message, score: f.score, sourceFieldIds,
    });
  }
  await call("POST", `/api/cases/${c.id}/screen`);

  const r = p.aiReview;
  await call("POST", `/api/cases/${c.id}/ai-reviews`, {
    modelName: "demo-reviewer (seeded)",
    modelVersion: "personas-1",
    recommendation: r.recommendation,
    rationale: JSON.stringify({
      summary: r.summary, keyConcerns: r.keyConcerns, nextSteps: r.nextSteps, draftCaseNote: r.draftCaseNote,
    }),
  });
  await call("POST", `/api/cases/${c.id}/mark-ai-reviewed`);
  await call("POST", `/api/cases/${c.id}/request-decision`);
  return c;
}

const { personas } = JSON.parse(await readFile(path.join(here, "personas.json"), "utf8"));
for (const p of personas) {
  const c = await seedPersona(p);
  console.log(`seeded ${p.key.padEnd(10)} ${c.id}  ${p.applicant.fullName}`);
}
console.log(`\n${personas.length} cases are AwaitingDecision for ${analyst}'s firm.`);
