# Solqaryn — Disaster Recovery Runbook (N8.10)

Status: versioned engineering baseline for `Desarrollo`; N8.10.A preflight/model.  
Authority: `docs/VAEP_AUTHORITY.md`.  
Scope guard: this document defines the recovery model only. It does not authorize writes to `main`, Production, provider control planes, deploys, secrets, or PR #2.

## 1. Objective and recovery boundary

Recover Solqaryn after an application, API, database, configuration, DNS/TLS, credential, or storage incident while preserving tenant isolation and auditable evidence. The recovery boundary is the web application, API, MySQL data, versioned configuration, and any external asset references required by the application.

N8.10.A defines the operating model. It does **not** claim that a Production disaster drill, provider fork/restore, DNS cutover, or final RPO/RTO certification has already been executed. Those claims require later N8.10 evidence and REVIEW_FIRST on the exact candidate HEAD.

## 2. Causal baseline already proven in Desarrollo

The current engineering model is grounded in newer causal evidence rather than historical declarations:

- MySQL DEV is Aiven MySQL 8.4.8, service `solqaryn-mysql`, plan `free-1-1gb`, recovery mode `pitr`; the provider backup endpoint, PITR availability, retention policy, restore path `AIVEN_FORK_AND_RESTORE_CONTROL_PLANE`, and effective restore authority were proven read-only in N8.20.
- Provider metadata showed a 24-hour backup interval and three actual backups; no provider restore/fork was executed by that proof.
- N8.21 executed a real logical backup of Desarrollo and restored the **same encrypted artifact** into an isolated disposable MySQL instance. The restore step itself completed successfully in the GitHub Actions job window 14:54:50Z–14:55:17Z (27 seconds). Exact table counts, 104 EF migrations, 240 foreign keys and 542 indexes matched, and the active Desarrollo database was not used as the restore target.
- N8.21 then ran backend sanity against the isolated restore successfully. This proves recovery mechanics in a disposable DEV/CI context; it is not a Production RTO measurement.
- The N8.8/N8.9 historical backup/restore debt is causally superseded by that N8.21 evidence; historical receipts remain append-only.

Canonical references:

- `docs/evidencias/backup-restore/BACKUP_MANIFEST.md`
- `docs/evidencias/backup-restore/RESTORE_VERIFICATION.md`
- `docs/evidencias/backup-restore/N8.8_N8.9_RECONCILIATION.md`
- `vaep/evidence/receipts/N8.20.H_LISTO_REAL_20260916T135053Z_SUP00.json`
- GitHub Actions run `35111491587` (`M11 - Backup operativo de Desarrollo`)

## 3. Recovery objectives

### 3.1 RPO

**Engineering baseline RPO: <= 24 hours for durable MySQL business data.**

Reason: the read-only Aiven control-plane evidence proves a 24-hour managed-backup interval and PITR availability. PITR should normally allow a much smaller loss window, but N8.10 does not claim a tighter contractual RPO until a directed drill measures the recoverable point and records the exact selected restore timestamp.

If PITR is unavailable or cannot produce a recovery point within 24 hours of the incident, the RPO gate is failed and the incident cannot be marked recovered without explicit owner acceptance of data loss.

Code and versioned non-secret configuration have a target RPO of **0 committed changes** because recovery must select an immutable known-good Git SHA. Secret values are outside repository evidence and must be re-bound through the authorized platform secret store without exposing them in logs or receipts.

### 3.2 RTO

**Engineering baseline end-to-end RTO: <= 60 minutes** from formal recovery start until the restored candidate passes database integrity, API readiness/health, tenant-isolation smoke, and the owner-authorized traffic/cutover decision is ready.

Supporting lower-bound evidence: the existing isolated DEV drill restored the logical artifact in 27 seconds, but that number is not treated as the Production RTO because provider service creation/fork time, platform rollback/redeploy, DNS/TLS and owner validation were not included.

Operational sub-targets used for the drill:

- application/API rollback to an immutable known-good deployment: <= 15 minutes;
- database recovery candidate creation + integrity validation: <= 30 minutes;
- post-restore application sanity + business validation + cutover readiness: <= 15 minutes.

Any sub-target breach is recorded; the authoritative gate is the measured end-to-end RTO <= 60 minutes.

## 4. Incident roles and decision ownership

Roles are operational responsibilities, not permission escalation:

- **Incident Commander (IC):** declares recovery start, chooses incident severity, records T0, coordinates the clock, and owns the final recommendation.
- **Database Recovery Operator:** selects the recovery point, creates/restores only an isolated/new recovery target, verifies checksums/topology/migrations/table counts, and never restores over the active source.
- **Application/Platform Operator:** identifies immutable known-good frontend/API revisions and prepares rollback/redeploy steps on the authorized environment.
- **Security/Audit Reviewer:** ensures no secrets are printed, validates tenant-isolation and security controls, and records P0/P1/P2 through REVIEW_FIRST.
- **Business Validator:** validates critical read-only workflows against the recovery candidate and accepts/rejects the measured data-loss window.
- **Communications Owner:** issues incident updates and records the owner decision for cutover or continued containment.

One person may fill multiple roles in a small team, but IC and the final business acceptance must be explicitly attributable in the final drill evidence.

## 5. Trigger and classification

Start DR when normal rollback cannot safely recover service, or when there is suspected data corruption/loss, failed destructive migration, unrecoverable deployment state, provider outage requiring recovery, or credential/security impact that invalidates the active environment.

Classify the incident before action:

1. app/frontend-only;
2. API/service-only;
3. database/data-integrity;
4. configuration/secret binding;
5. DNS/TLS/provider control plane;
6. external storage/assets;
7. multi-component/full-system.

## 6. Recovery procedure

### Step 0 — Freeze and evidence

Record UTC incident time, declaration time `T0`, affected environment, current immutable revisions, observed symptoms, last known-good transaction/revision, and scope. Stop speculative changes. Do not expose secrets in evidence.

### Step 1 — Choose rollback vs restore

Use application rollback when data integrity is intact and the defect is code/configuration only. Use database recovery when data corruption, destructive migration, or unacceptable data state exists. A schema incident must not be 'fixed' by silently restoring over the active database.

### Step 2 — Application/API rollback preparation

Identify the exact known-good Git/deployment revisions for web and API. Prepare the provider-native rollback/redeploy path, but any Production execution requires the appropriate owner-authorized run; this Desarrollo-only runbook does not perform it.

### Step 3 — Database recovery candidate

Preferred path when data recovery is required:

1. select a recovery point consistent with the RPO target;
2. create/fork/restore into a **new isolated recovery target** using the authorized Aiven control-plane path or the validated encrypted logical-backup path applicable to the incident;
3. never use the active source database as the restore target;
4. verify checksum/manifest when using a logical artifact;
5. verify schema/migration history, all critical table counts, foreign-key/index topology, and database connectivity;
6. record actual selected recovery timestamp and calculate observed data-loss duration.

### Step 4 — Application sanity against recovery candidate

Run the backend against the recovery candidate with outbound business side effects disabled. Required minimum checks: build/startup, readiness/health, DB connected, migration compatibility, critical read-only queries, tenant isolation, and no real email/WhatsApp/payment/business transaction emission.

### Step 5 — Frontend and business validation

Validate the smallest critical read-only flows needed to prove service usability. If the incident has no frontend surface, document causal N/A. Business Validator records expected vs actual and PASS/FAIL.

### Step 6 — Security review

Run REVIEW_FIRST. `P0=0` and `P1=0` are mandatory before any recovery candidate can be recommended for cutover. Do not suppress failures. Secret values must not appear in logs, artifacts, screenshots, receipts, or Sheet evidence.

### Step 7 — Cutover decision

The IC presents: selected recovery point, observed RPO, measured RTO, validation result, residual risks, and rollback-of-the-rollback option. Actual Production traffic/configuration changes are outside this run and require explicit authorized execution.

### Step 8 — Post-recovery verification

After an authorized cutover, verify health/readiness, migration compatibility, tenant isolation, critical business workflows, queues/integrations, and audit trail. Preserve immutable evidence and close only through VAEP receipt/readback.

## 7. Rollback-of-recovery

If the recovery candidate fails validation, do not promote it. Preserve the current active environment, discard/contain the failed candidate, select an earlier known-good application revision or different database recovery point, and restart the recovery clock as a new attempt while preserving the previous attempt evidence.

If a cutover has already happened and new P0/P1 appears, use the pre-cutover environment only if its integrity is still proven; otherwise create another isolated recovery candidate. Never destructively overwrite the only known-good copy.

## 8. External storage/assets

Database backups contain asset references, not an invented guarantee that every external blob provider/version is recoverable. A storage incident must use the provider/versioning/retention evidence certified for that provider at execution time. If that evidence is absent, storage recovery remains an explicit blocker rather than a guessed PASS.

## 9. Communication cadence

- At declaration: incident scope, impact, `T0`, IC, next checkpoint.
- During recovery: only material state changes, measured blocker, or owner decision needed; avoid heartbeat noise.
- Before cutover: recovery point, observed RPO, measured RTO, validation, residual risk, go/no-go owner decision.
- After recovery: final status, exact revisions/recovery point, data-loss measurement, total RTO, follow-up actions.

No communication may include passwords, connection strings, tokens, secret values, or customer-sensitive payloads.

## 10. Required evidence for N8.10 closure

N8.10 cannot close from this document alone. Later stages must produce, on the same causal chain:

- directed recovery drill logs/steps and exact timestamps;
- a recovery candidate distinct from the active source;
- measured selected recovery point and observed data-loss duration (`observed_RPO <= 24h`);
- measured end-to-end recovery duration (`observed_RTO <= 60m`);
- database integrity evidence;
- application sanity and tenant-isolation evidence;
- security REVIEW_FIRST with `P0=0/P1=0`;
- attributable business/owner acceptance of the measured RPO/RTO result;
- final receipt + readback under VAEP authority.

Expected canonical closure artifacts include `docs/evidencias/backup-restore-evidence.md` and `docs/evidencias/rpo-rto-evidence.md` or an explicitly reconciled stronger successor.

## 11. N8.10.A acceptance result

For the PRE/model scope only:

- recovery scope and out-of-scope boundaries: defined;
- RPO/RTO engineering targets: defined;
- rollback and restore procedure: defined;
- roles/responsibilities: defined;
- communication path: defined;
- dependencies on N8.20/N8.21 evidence: explicit;
- later measurement/sign-off requirements: explicit;
- large implementation changes: none;
- Production/main/deploy/secrets/PR #2 touched by this model: no.
