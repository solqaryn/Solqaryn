# N8.10 — RPO/RTO drill evidence

Authority: `docs/VAEP_AUTHORITY.md`  
Branch: `Desarrollo`  
Scope: N8.10.G TEST_CI  
Production/main/deploy/secret values/PR #2: not touched.

## Recovery objectives under test

- Engineering RPO baseline: **<= 24 hours** for durable MySQL business data.
- Engineering end-to-end RTO baseline: **<= 60 minutes** from recovery start until the isolated recovery candidate has passed database integrity and backend sanity and is ready for an owner-authorized cutover decision.

These objectives are defined in `docs/evidencias/dr-runbook.md`.

## Material drill reused causally

N8.10 does not re-run the backup workflow because the already-certified N8.21 chain is a stronger material recovery drill and the diff from the N8.21 TEST_CI certification head `11c705ea830c2b10036e5d44c9327ac77b954c3f` to the N8.10.F head contains only documentation/evidence/receipt files. There is no application, backend, frontend, migration, recovery-script, or workflow delta that invalidates that material drill.

### Backup + isolated restore

GitHub Actions run `35111491587`, `M11 - Backup operativo de Desarrollo`:

- run started: `2026-09-16T14:52:24Z`;
- run completed: `2026-09-16T14:55:22Z`;
- real encrypted logical backup from Desarrollo: PASS;
- same encrypted artifact restored into disposable isolated MySQL: PASS;
- restore step: `14:54:50Z` → `14:55:17Z` = **27 seconds**;
- checksums: PASS;
- base tables: `132/132`;
- EF migrations: `104/104`;
- foreign keys: `240` verified;
- indexes: `542` verified;
- all table row counts: exact equivalence against the captured source snapshot;
- active Desarrollo DB used as restore target: no;
- Production touched: no.

### Backend sanity against isolated recovery candidate

GitHub Actions run `35113349678`, `N8.21.D - Backend smoke against isolated restore`:

- run started: `2026-09-16T15:08:53Z`;
- run completed: `2026-09-16T15:13:08Z`;
- backend build/start: PASS;
- health/readiness: PASS;
- DB connection: connected;
- migration compatibility: PASS;
- critical read-only checks: PASS;
- directed tenant isolation: PASS;
- real email/WhatsApp/business transaction emission: no.

### Security cleanup

Run `35120379562` removed the retained full logical backup artifact after validation. N8.21.F certified `sensitive_backup_retained=false`, `P0=0`, `P1=0`, and zero exposed secrets.

## Measured RTO

For a conservative replay of the existing drill chain, T0 is the material backup/restore run start and T1 is the completion of backend sanity against the isolated recovery candidate:

- T0 = `2026-09-16T14:52:24Z`
- T1 = `2026-09-16T15:13:08Z`
- observed end-to-end drill duration = **20 minutes 44 seconds** (`1244` seconds)
- target = `<= 60 minutes`
- result = **PASS**

This measurement intentionally includes the orchestration gap between the two runs, making it more conservative than summing only active job execution. It does not claim Production DNS/traffic cutover timing.

## Measured RPO / data loss

The selected drill recovery point is the source snapshot embodied by the encrypted backup artifact. The isolated restored candidate reproduced that selected point with exact table-count equivalence across all `132` base tables and matching migration/topology checks. No row-count loss was observed relative to the selected recovery point.

- selected recovery point: same-artifact source snapshot from run `35111491587`;
- observed loss relative to that selected recovery point: **0 detected row-count divergence**;
- time objective under test: `<= 24 hours`;
- result = **PASS for the simulated drill recovery point**.

This is not represented as a live Production-incident PITR measurement. A real incident must record its incident timestamp, chosen recovery timestamp, and the resulting time delta. If that delta exceeds 24 hours, the RPO gate fails unless the owner explicitly accepts the data loss.

## No-functional-delta validation

GitHub compare `11c705ea830c2b10036e5d44c9327ac77b954c3f...50df0ad9d98e2850e9c7cc3a17c993a77a707930` is ahead only by documentation/evidence/review/receipt files. No application/backend/frontend/migration/recovery script/workflow file appears in the compare set. Therefore the N8.21 material recovery test remains causally applicable to the N8.10 procedural DR contract.

## TEST_CI gate result

- backup → restore same artifact: PASS;
- database integrity/topology: PASS;
- backend sanity: PASS;
- tenant isolation: PASS;
- sensitive backup retained after cleanup: no;
- P0: `0`;
- P1: `0`;
- engineering RPO drill gate: PASS;
- engineering RTO drill gate: PASS (`20m44s <= 60m`);
- new functional delta requiring another destructive/backup drill: none.

N8.10.G may close LISTO_REAL on this evidence. Final N8.10 documentation/certification must still preserve the distinction between engineering drill evidence and any business-owner sign-off required by the Plan Maestro.
