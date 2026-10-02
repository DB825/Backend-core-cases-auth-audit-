import type { ReviewFinding, Severity } from "./api";

// The API has no case-level risk score yet, only per-finding rule scores, so the dashboard
// combines them here. Independent findings compound (1 - product of (1 - score)), so two
// medium findings outrank one. Move this server-side once the rules engine owns it.
export function caseRisk(findings: ReviewFinding[]): { score: number; tier: Severity } {
  const score = 1 - findings.reduce((acc, f) => acc * (1 - (f.score ?? 0)), 1);
  const tier: Severity = score >= 0.8 ? "High" : score >= 0.4 ? "Medium" : "Low";
  return { score, tier };
}

export const LOW_CONFIDENCE = 0.8;
