// Thin fetch wrapper over the CaseAuth API. Types mirror openapi/openapi.json; regenerate or
// update them by hand if the contract changes.

export type CaseStatus =
  | "Uploaded" | "Extracted" | "Screened" | "AiReviewed" | "AwaitingDecision"
  | "Approved" | "Rejected" | "Escalated";
export type Severity = "Low" | "Medium" | "High";
export type DecisionOutcome = "Approved" | "Rejected" | "Escalated";

export interface Me { userId: string; username: string; firmId: string; role: string }
export interface CaseResponse {
  id: string; firmId: string; status: CaseStatus; applicantFullName: string;
  createdByUserId: string; createdAt: string; updatedAt: string; rowVersion: string;
  // Server-computed from the findings' rule scores; absent on older API builds.
  riskScore?: number; riskTier?: Severity;
}
export interface DocumentResponse {
  id: string; fileName: string; contentType: string; sizeBytes: number;
  documentType: string; uploadedByUserId: string; uploadedAt: string;
}
export interface ReviewField {
  id: string; documentId: string; documentType: string; fieldName: string;
  fieldValue: string; confidence: number | null;
}
export interface ReviewFinding {
  id: string; code: string; severity: Severity; score: number | null; message: string;
  sourceFieldIds: string[];
}
export interface AiReviewInput { caseId: string; fields: ReviewField[]; findings: ReviewFinding[] }
export interface AiReviewResponse {
  id: string; version: number; modelName: string; modelVersion: string;
  recommendation: "Approve" | "Reject" | "Escalate"; rationale: string; createdAt: string;
}
export interface DecisionResponse {
  id: string; caseId: string; outcome: DecisionOutcome; aiReviewId: string | null;
  decidedByUserId: string; decidedAt: string;
}
export interface AuditEventResponse {
  id: string; caseId: string | null; actorUsername: string; action: string;
  outcome: "Success" | "Rejected" | "Failure"; timestamp: string; correlationId: string;
  aiReviewVersion: number | null;
}

export class ApiError extends Error {
  constructor(public status: number, message: string) { super(message); }
}

async function request<T>(user: string, method: string, url: string, body?: unknown, headers: Record<string, string> = {}): Promise<T> {
  const res = await fetch(url, {
    method,
    headers: {
      "X-Dev-User": user,
      ...(body !== undefined ? { "Content-Type": "application/json" } : {}),
      ...headers,
    },
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });
  if (!res.ok) {
    let message = `${res.status} ${res.statusText}`;
    try {
      const problem = await res.json();
      message = problem.detail ?? problem.title ?? message;
    } catch { /* not JSON */ }
    throw new ApiError(res.status, message);
  }
  return res.status === 204 ? (undefined as T) : res.json();
}

export const api = {
  me: (u: string) => request<Me>(u, "GET", "/api/me"),
  cases: (u: string) => request<CaseResponse[]>(u, "GET", "/api/cases"),
  case: (u: string, id: string) => request<CaseResponse>(u, "GET", `/api/cases/${id}`),
  documents: (u: string, id: string) => request<DocumentResponse[]>(u, "GET", `/api/cases/${id}/documents`),
  reviewInput: (u: string, id: string) => request<AiReviewInput>(u, "GET", `/api/cases/${id}/ai-review-input`),
  aiReviews: (u: string, id: string) => request<AiReviewResponse[]>(u, "GET", `/api/cases/${id}/ai-reviews`),
  decisions: (u: string, id: string) => request<DecisionResponse[]>(u, "GET", `/api/cases/${id}/decisions`),
  audit: (u: string, id: string) => request<AuditEventResponse[]>(u, "GET", `/api/cases/${id}/audit-events`),
  decide: (u: string, c: CaseResponse, outcome: DecisionOutcome, aiReviewId: string | null, idempotencyKey: string) =>
    request<DecisionResponse>(u, "POST", `/api/cases/${c.id}/decisions`, { outcome, aiReviewId },
      { "Idempotency-Key": idempotencyKey, "If-Match": c.rowVersion }),
  resetDemo: (u: string) => request<{ casesCreated: number }>(u, "POST", "/api/demo/reset"),
  // Returns null if the file can't be served (missing on disk, or an API that predates the endpoint).
  documentBlobUrl: async (u: string, caseId: string, docId: string): Promise<string | null> => {
    const res = await fetch(`/api/cases/${caseId}/documents/${docId}/content`, { headers: { "X-Dev-User": u } });
    return res.ok ? URL.createObjectURL(await res.blob()) : null;
  },
};

// Teammate 4's planned output is richer than AiReview's current columns, so the seeded reviews
// carry it as JSON in `rationale`. Plain-text rationales still render as a summary.
export interface StructuredReview {
  summary: string;
  keyConcerns: { text: string; findingCodes: string[] }[];
  nextSteps: string[];
  draftCaseNote: string;
}
export function parseRationale(rationale: string): StructuredReview {
  try {
    const r = JSON.parse(rationale);
    if (r && typeof r.summary === "string") {
      return { summary: r.summary, keyConcerns: r.keyConcerns ?? [], nextSteps: r.nextSteps ?? [], draftCaseNote: r.draftCaseNote ?? "" };
    }
  } catch { /* plain text */ }
  return { summary: rationale, keyConcerns: [], nextSteps: [], draftCaseNote: "" };
}
