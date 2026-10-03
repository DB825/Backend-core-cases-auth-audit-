import type { CaseResponse, ReviewFinding, Severity } from "./api";

// Prefers the API's own case risk (CaseResponse.riskScore). Older API builds only have
// per-finding rule scores, so this falls back to combining them the same way the server does. Independent findings compound (1 - product of (1 - score)), so two
// medium findings outrank one.
export function caseRisk(findings: ReviewFinding[]): { score: number; tier: Severity } {
  const score = 1 - findings.reduce((acc, f) => acc * (1 - (f.score ?? 0)), 1);
  const tier: Severity = score >= 0.8 ? "High" : score >= 0.4 ? "Medium" : "Low";
  return { score, tier };
}

export const LOW_CONFIDENCE = 0.8;

export function riskFor(c: CaseResponse, findings: ReviewFinding[]): { score: number; tier: Severity } {
  return c.riskScore !== undefined && c.riskTier ? { score: c.riskScore, tier: c.riskTier } : caseRisk(findings);
}
