# Documentation (MANDATORY)
This short document provides concise, professional answers to the required topics. It is intended for reviewers, maintainers, and implementers who need to understand design decisions, constraints, and testing strategy.

---

## 1. Purpose & Scope 
- Purpose: Describe why the system enforces strict attendance governance, how core data must be handled, and what non-functional requirements are mandatory for a production-quality solution.
- Scope: Backend APIs, data model and migration approach, state machine and governance actions, auditability, role-based access, and operational concerns (monitoring, backups, data retention).

## 2. Audience 
- Primary: Developers, architects, and QA engineers.
- Secondary: Product owners and school administrators for acceptance criteria and audit/operations teams for compliance.

## 3. Domain Understanding 
- Risk summary: Weak attendance controls enable inaccurate reporting, safety lapses, reconciliation disputes, and intentional tampering. This can cause regulatory non-compliance and loss of trust.
- Real-world failure modes: Missed incident detection, retroactive unauthorized edits, disputed records with no demonstrable history, and lack of reliable audit evidence for investigations.

## 4. Data Model Explanation 
- Entities and responsibilities:
  - Student: canonical identity (immutable identifiers such as school-id). Changes should create a new version or be captured via audit logs rather than mutating historical identity fields.
  - Course: contextual info for grouping AttendanceRecords.
  - AttendanceRecord: event capturing presence/absence with timestamp, actor, and status. Core facts are immutable; corrections create new version records linked via parent/child.
  - AuditLog: append-only, records who changed what, when, and why (actor, timestamp, operation, before/after snapshots).
  - ReopenRequest: governance artifact to request and record manual review and corrections.
- Immutability vs versioning: Core event facts are immutable. Corrections produce versioned records (new rows with links to originals) and audit entries marking why a correction occurred.
- History preservation: Parent/child chains, append-only audit logs, and retained snapshots allow deterministic reconstruction of any past state for compliance or investigations.

## 5. State Machine & Roles 
- States (with examples): Draft → Submitted → Approved → Published → Locked. Corrections generate a new Draft that references the Corrected record. ReopenRequested transitions may be initiated by teachers and resolved by coordinators.
- Actor mappings:
  - Teacher: create Draft, submit, request reopen
  - Academic Coordinator: review, approve, publish, apply corrections, lock
  - Leadership/Auditor: read-only, audit queries
- Transition controls: Enforced by API and domain validation to prevent unauthorized jumps (e.g., no direct Draft → Published without approval). Time-window checks and role checks are enforced server-side.

## 6. API Design Rationale & Examples 
- Design principles: Commands for state-changing actions (POST /attendance/{id}/submit), queries for read-only access (GET /attendance?studentId=), and separate admin/audit endpoints (GET /audit/attendance/{id}).
- Example endpoints & intent:
  - POST /attendance - create Draft
  - POST /attendance/{id}/submit - attempt to move Draft → Submitted
  - POST /attendance/{id}/request-reopen - request reopen (creates ReopenRequest)
  - POST /attendance/{id}/correct - apply correction (creates new Draft linked to original)
  - GET /attendance/{id}/history - returns all versions and audit events
  - GET /audit/attendance - full audit queries (filter by actor, date, operation)
- Error handling: Clear, machine-readable errors (RFC 7807 Problem Details). Include business error codes for state validation (e.g., ATT_STATE_INVALID).
- Sample request/response (brief):
  - POST /attendance (body: studentId, courseId, date, status, actorId) → 201 Created with location and Draft id
  - GET /attendance/{id}/history → 200 OK with chronological list of versions + AuditLog entries

## 7. Security & Authorization 
- Role-based access control: Enforce least privilege; map API operations to roles and validate on every request.
- Tamper resistance: Append-only audit logs, immutability of core facts, and cryptographic hash chain for audit entries (optional enhancement) for additional non-repudiation.
- Data protection: Encrypt sensitive fields at rest if required; use TLS in transit; follow principle of minimal data exposure in queries.

## 8. Non-Functional Requirements (NFRs) 
- Availability: Target 99.9% for the API during school hours. Design for retries and idempotency in commands.
- Performance: Typical APIs respond <300ms under normal load; heavy audit queries may be async or paged.
- Scalability: Partition by school/region for large deployments; use read-replicas for reporting workloads.
- Retention & compliance: Retain audit logs and corrected records per policy (e.g., 7 years) in append-only store; provide tools for export.

## 9. Data Validation & Integrity 
- Server-side validation for all business invariants (no future-dated attendance, required fields present, role checks).
- Idempotency for create/correct endpoints (client-supplied idempotency key) to avoid duplicate records on retries.

## 10. Testing & QA Strategy 
- Unit tests: Cover state machine logic and validation rules.
- Integration tests: Exercise end-to-end flows (create → submit → approve → publish → correct → history) including authorization boundaries.
- Contract tests: API contracts and Problem Details shapes.
- Acceptance tests: Scenarios based on real school workflows and edge cases (rapid consecutive edits, reopen after publish).

## 11. Observability & Operations 
- Logging: Structured logs with correlation ids for requests; do not log PII unless redaction/compliance is in place.
- Metrics: Track counts of state transitions, correction rates, reopen requests, and audit query latencies.
- Alerts: High error rates for command endpoints, unexpected state transition attempts, and failed background jobs (e.g., retention cleanup).

## 12. Deployment, Migration & Backups 
- Migration notes: Versioned schema changes with forward/backward compatible migrations; preserve historical snapshots during migration windows.
- Backups: Regular DB backups with tested restores; ensure audit logs are included and verified.
- Release strategy: Canary or feature-flagged rollout for risky changes (state machine updates).

## 13. Trade-offs & Decisions 
- Deferred features: Full SSO, complex UI, distributed event-sourcing, and blockchain anchoring for audit records were deferred to keep scope focused on governance and traceability.
- Rationale: Prioritize a minimal, verifiable backend that meets audit and safety requirements within the project timeline.

## 14. Open Questions & Future Work 
- Should we add cryptographic anchoring of audit logs for extra tamper-proof guarantees?
- What is the formal retention policy required by regulators (school-by-school variation)?
- Should heavy audit queries be async with pre-computed snapshots to reduce load on primary DB?

## 15. Glossary & References 
- AuditLog: Append-only record of who did what and why, with timestamp and before/after snapshots.
- ReopenRequest: Request that triggers manual review and possible correction workflow.
- References: RFC 7807 Problem Details, OWASP API Security Top 10, internal architecture and compliance docs.

