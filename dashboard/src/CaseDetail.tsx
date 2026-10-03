import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  api, parseRationale, type AiReviewInput, type AiReviewResponse, type AuditEventResponse, type CaseResponse,
  type DecisionOutcome, type DecisionResponse, type DocumentResponse, type Me, type Severity,
} from "./api";
import { LOW_CONFIDENCE, riskFor } from "./risk";
import { RecommendationTag, RiskBadge, SeverityTag, StatusTag } from "./Badges";

interface Data {
  c: CaseResponse;
  docs: DocumentResponse[];
  input: AiReviewInput;
  review: AiReviewResponse | null;
  decisions: DecisionResponse[];
  audit: AuditEventResponse[];
}

const SEV_RANK: Record<Severity, number> = { Low: 1, Medium: 2, High: 3 };
const pretty = (s: string) => s.replace(/_/g, " ").toLowerCase();
const DOC_TYPE_LABEL: Record<string, string> = { GovernmentId: "Government ID", ProofOfAddress: "Proof of address", Financial: "Financial", Other: "Other" };

export default function CaseDetail({ me, caseId }: { me: Me; caseId: string }) {
  const [data, setData] = useState<Data | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [docId, setDocId] = useState<string | null>(null);
  const [activeFinding, setActiveFinding] = useState<string | null>(null);

  const load = useCallback(async () => {
    const u = me.username;
    const [c, docs, input, reviews, decisions, audit] = await Promise.all([
      api.case(u, caseId), api.documents(u, caseId), api.reviewInput(u, caseId),
      api.aiReviews(u, caseId), api.decisions(u, caseId), api.audit(u, caseId),
    ]);
    const review = reviews.reduce<AiReviewResponse | null>((a, r) => (!a || r.version > a.version ? r : a), null);
    setData({ c, docs, input, review, decisions, audit });
    setDocId((cur) => cur ?? docs[0]?.id ?? null);
  }, [me.username, caseId]);

  useEffect(() => {
    load().catch((e) => setError(e.status === 404 ? `This case isn't visible to ${me.username} (${me.firmId}).` : e.message));
  }, [load, me]);

  // Worst severity of any finding that cites each extracted field.
  const fieldSeverity = useMemo(() => {
    const m = new Map<string, Severity>();
    for (const f of data?.input.findings ?? []) {
      for (const id of f.sourceFieldIds) {
        const cur = m.get(id);
        if (!cur || SEV_RANK[f.severity] > SEV_RANK[cur]) m.set(id, f.severity);
      }
    }
    return m;
  }, [data]);

  if (error) return <><a className="back" href="#/">← Back to queue</a><div className="banner error">{error}</div></>;
  if (!data) return <div className="loading">Loading case…</div>;

  const { c, docs, input, review, decisions, audit } = data;
  const risk = riskFor(c, input.findings);
  const activeFieldIds = new Set(input.findings.find((f) => f.id === activeFinding)?.sourceFieldIds ?? []);
  const docFields = input.fields.filter((f) => f.documentId === docId);
  const fieldsByDoc = (id: string) => input.fields.filter((f) => f.documentId === id);
  const flaggedCount = (id: string) => fieldsByDoc(id).filter((f) => fieldSeverity.has(f.id)).length;

  const focusFinding = (findingId: string) => {
    setActiveFinding((cur) => (cur === findingId ? null : findingId));
    const first = input.findings.find((f) => f.id === findingId)?.sourceFieldIds[0];
    const doc = input.fields.find((f) => f.id === first)?.documentId;
    if (doc) setDocId(doc);
  };

  return (
    <>
      <a className="back" href="#/">← Back to queue</a>
      <div className="case-head">
        <div>
          <h1>{c.applicantFullName}</h1>
          <p className="muted">Opened {new Date(c.createdAt).toLocaleString()} by {c.createdByUserId.replace(/^u-/, "")} · {c.firmId}</p>
        </div>
        <div className="case-head-right">
          <StatusTag status={c.status} />
          <RiskBadge {...risk} />
        </div>
      </div>

      <section className="panel docs-panel">
        <div className="doc-tabs">
          {docs.map((d) => (
            <button key={d.id} className={d.id === docId ? "doc-tab active" : "doc-tab"} onClick={() => setDocId(d.id)}>
              {d.fileName.replace(/\.[a-z]+$/i, "")}
              <span className="doc-file">{DOC_TYPE_LABEL[d.documentType] ?? d.documentType}</span>
              {flaggedCount(d.id) > 0 && <span className="doc-flag">{flaggedCount(d.id)} flagged</span>}
            </button>
          ))}
        </div>
        <div className="doc-grid">
          <DocumentImage user={me.username} caseId={c.id} doc={docs.find((d) => d.id === docId) ?? null} />
          <div>
            <h3>Extracted fields</h3>
            <table className="fields">
              <tbody>
                {docFields.map((f) => {
                  const sev = fieldSeverity.get(f.id);
                  const low = f.confidence !== null && f.confidence < LOW_CONFIDENCE;
                  const cls = ["field", sev ? `flag-${sev.toLowerCase()}` : "", activeFieldIds.has(f.id) ? "active" : ""].join(" ");
                  return (
                    <tr key={f.id} className={cls}>
                      <th>{pretty(f.fieldName)}</th>
                      <td className="field-value">{f.fieldValue}</td>
                      <td className={low ? "conf low" : "conf"} title="Extraction confidence">
                        {f.confidence === null ? "–" : `${Math.round(f.confidence * 100)}%`}
                        {low && <span className="conf-note">check by eye</span>}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>
      </section>

      <div className="two-col">
        <section className="panel">
          <h2>Rule findings <span className="count">{input.findings.length}</span></h2>
          <p className="muted small">Deterministic checks. Select one to highlight the fields it cites.</p>
          <ul className="findings">
            {[...input.findings].sort((a, b) => (b.score ?? 0) - (a.score ?? 0)).map((f) => (
              <li key={f.id}>
                <button className={activeFinding === f.id ? "finding active" : "finding"} onClick={() => focusFinding(f.id)}>
                  <div className="finding-top">
                    <SeverityTag severity={f.severity} />
                    <span className="finding-code">{f.code}</span>
                    {f.score !== null && <span className="finding-score">score {f.score.toFixed(2)}</span>}
                  </div>
                  <p>{f.message}</p>
                </button>
              </li>
            ))}
          </ul>
        </section>

        <AiPanel review={review} />
      </div>

      <DecisionPanel me={me} c={c} review={review} decisions={decisions} onDecided={load} />

      <section className="panel">
        <h2>Audit trail <span className="count">{audit.length}</span></h2>
        <p className="muted small">Every action on this case, written in the same transaction as the change it records.</p>
        <ol className="audit">
          {[...audit].sort((a, b) => a.timestamp.localeCompare(b.timestamp)).map((e) => (
            <li key={e.id} className={`audit-${e.outcome.toLowerCase()}`}>
              <span className="audit-time">{new Date(e.timestamp).toLocaleTimeString()}</span>
              <span className="audit-action">{e.action}</span>
              <span className="audit-actor">{e.actorUsername}</span>
              <span className={`audit-outcome ${e.outcome.toLowerCase()}`}>{e.outcome}</span>
              {e.aiReviewVersion !== null && <span className="audit-ai">AI review v{e.aiReviewVersion}</span>}
              <code className="audit-corr" title="Correlation ID">{e.correlationId.slice(0, 8)}</code>
            </li>
          ))}
        </ol>
      </section>
    </>
  );
}

function DocumentImage({ user, caseId, doc }: { user: string; caseId: string; doc: DocumentResponse | null }) {
  const [url, setUrl] = useState<string | null | undefined>(undefined);
  useEffect(() => {
    if (!doc) return;
    let revoked: string | null = null;
    setUrl(undefined);
    api.documentBlobUrl(user, caseId, doc.id).then((u) => { revoked = u; setUrl(u); }, () => setUrl(null));
    return () => { if (revoked) URL.revokeObjectURL(revoked); };
  }, [user, caseId, doc]);

  if (!doc) return <div className="doc-image empty">No documents uploaded.</div>;
  if (url === undefined) return <div className="doc-image empty">Loading image…</div>;
  if (url === null) return <div className="doc-image empty">The API can't serve this file yet (needs the document content endpoint).</div>;
  return doc.contentType === "application/pdf"
    ? <iframe className="doc-image" src={url} title={doc.fileName} />
    : <img className="doc-image" src={url} alt={`${doc.documentType} document, ${doc.fileName}`} />;
}

function AiPanel({ review }: { review: AiReviewResponse | null }) {
  const parsed = review ? parseRationale(review.rationale) : null;
  const [note, setNote] = useState(parsed?.draftCaseNote ?? "");
  const [copied, setCopied] = useState(false);
  useEffect(() => setNote(parsed?.draftCaseNote ?? ""), [review?.id]); // eslint-disable-line react-hooks/exhaustive-deps

  if (!review || !parsed) {
    return <section className="panel ai-panel"><h2>AI review</h2><p className="muted">No AI review yet.</p></section>;
  }
  return (
    <section className="panel ai-panel">
      <div className="ai-head">
        <h2>AI review</h2>
        <span className="advisory">Advisory only</span>
      </div>
      <div className="ai-rec">Recommends <RecommendationTag rec={review.recommendation} /></div>
      <p className="ai-summary">{parsed.summary}</p>

      {parsed.keyConcerns.length > 0 && <>
        <h3>Key concerns</h3>
        <ul className="concerns">
          {parsed.keyConcerns.map((k, i) => (
            <li key={i}>{k.text} {k.findingCodes.map((code) => <code key={code}>{code}</code>)}</li>
          ))}
        </ul>
      </>}

      {parsed.nextSteps.length > 0 && <>
        <h3>Suggested next steps</h3>
        <ol className="steps">{parsed.nextSteps.map((s, i) => <li key={i}>{s}</li>)}</ol>
      </>}

      {parsed.draftCaseNote && <>
        <h3>Draft case note <span className="muted small">(edit before filing)</span></h3>
        <textarea className="note" value={note} onChange={(e) => setNote(e.target.value)} rows={4} />
        <button className="btn ghost small" onClick={() => navigator.clipboard.writeText(note).then(() => { setCopied(true); setTimeout(() => setCopied(false), 1500); })}>
          {copied ? "Copied" : "Copy note"}
        </button>
      </>}
      <p className="ai-meta muted small">{review.modelName} · {review.modelVersion} · review v{review.version}</p>
    </section>
  );
}

const ACTIONS: { outcome: DecisionOutcome; label: string; cls: string }[] = [
  { outcome: "Approved", label: "Approve", cls: "approve" },
  { outcome: "Escalated", label: "Escalate", cls: "escalate" },
  { outcome: "Rejected", label: "Reject", cls: "reject" },
];

function DecisionPanel({ me, c, review, decisions, onDecided }: {
  me: Me; c: CaseResponse; review: AiReviewResponse | null; decisions: DecisionResponse[]; onDecided: () => Promise<void>;
}) {
  const [pending, setPending] = useState<DecisionOutcome | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  // One key per page load: a double-submit replays the first decision instead of making a second.
  const idempotencyKey = useRef(crypto.randomUUID());

  const decision = decisions[decisions.length - 1];
  if (decision) {
    return (
      <section className={`panel decision decided decided-${decision.outcome.toLowerCase()}`}>
        <h2>Decision</h2>
        <p><strong>{decision.outcome}</strong> by {decision.decidedByUserId.replace(/^u-/, "")} on {new Date(decision.decidedAt).toLocaleString()}
          {review && decision.aiReviewId === review.id && <> after reading AI review v{review.version} (recommended {review.recommendation.toLowerCase()})</>}.</p>
      </section>
    );
  }

  const canDecide = me.role === "Supervisor" && c.status === "AwaitingDecision";
  const submit = async (outcome: DecisionOutcome) => {
    setBusy(true);
    setError(null);
    try {
      await api.decide(me.username, c, outcome, review?.id ?? null, idempotencyKey.current);
      await onDecided();
    } catch (e) {
      setError((e as Error).message);
      setBusy(false);
      setPending(null);
    }
  };

  return (
    <section className="panel decision">
      <h2>Decision</h2>
      {!canDecide && (
        <p className="muted">
          {c.status !== "AwaitingDecision"
            ? `The case is ${c.status}; it can be decided once it reaches Awaiting decision.`
            : `Only a supervisor can decide. ${me.username} is an ${me.role.toLowerCase()}, so switch to supervisor at the top right.`}
        </p>
      )}
      {error && <div className="banner error">{error}</div>}
      <div className="decision-actions">
        {pending ? (
          <>
            <span>Record <strong>{pending.toLowerCase()}</strong> as {me.username}?</span>
            <button className={`btn ${ACTIONS.find((a) => a.outcome === pending)!.cls}`} disabled={busy} onClick={() => submit(pending)}>
              {busy ? "Recording…" : "Confirm"}
            </button>
            <button className="btn ghost" disabled={busy} onClick={() => setPending(null)}>Cancel</button>
          </>
        ) : ACTIONS.map((a) => (
          <button key={a.outcome} className={`btn ${a.cls}`} disabled={!canDecide} onClick={() => setPending(a.outcome)}>{a.label}</button>
        ))}
      </div>
    </section>
  );
}
