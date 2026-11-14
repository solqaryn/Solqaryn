# N8.10 — Backup/restore evidence reconciliation

Authority: `docs/VAEP_AUTHORITY.md`  
Branch: `Desarrollo`  
Purpose: canonical, non-secret reconciliation of the material backup/restore evidence reused by Disaster Recovery.

## Proven material chain

N8.10 reuses the stronger causal recovery chain already certified by N8.20/N8.21 instead of fabricating a second backup or restore solely for documentation.

### Provider recovery capability

N8.20 established read-only provider facts for the Desarrollo MySQL service:

- engine/version: MySQL 8.4.8;
- provider: Aiven;
- service: `solqaryn-mysql`;
- plan: `free-1-1gb`;
- recovery mode: PITR;
- provider backup interval observed: 24 hours;
- actual backups observed: 3;
- restore authority/path: Aiven fork-and-restore control plane;
- no provider restore or Production write was executed by that evidence collection.

Canonical receipt: `vaep/evidence/receipts/N8.20.H_LISTO_REAL_20260916T135053Z_SUP00.json`.

### Real logical backup + isolated restore

GitHub Actions run `35111491587` (`M11 - Backup operativo de Desarrollo`) executed a real backup of Desarrollo and restored the same encrypted artifact into a disposable isolated MySQL target.

Observed results:

- run start: `2026-09-16T14:52:24Z`;
- run completion: `2026-09-16T14:55:22Z`;
- restore step: `2026-09-16T14:54:50Z` to `2026-09-16T14:55:17Z` = 27 seconds;
- checksum/manifest validation: PASS;
- base tables: 132/132;
- EF migrations: 104/104;
- foreign keys: 240 verified;
- indexes: 542 verified;
- row counts: exact equivalence for all captured base tables;
- active Desarrollo database used as restore target: no;
- Production touched: no.

Canonical details remain in:

- `docs/evidencias/backup-restore/BACKUP_MANIFEST.md`
- `docs/evidencias/backup-restore/RESTORE_VERIFICATION.md`
- `docs/evidencias/backup-restore/N8.8_N8.9_RECONCILIATION.md`

### Backend sanity against restored candidate

GitHub Actions run `35113349678` (`N8.21.D - Backend smoke against isolated restore`) completed `2026-09-16T15:13:08Z` with:

- backend build/start: PASS;
- readiness/health: PASS;
- database connection: connected;
- migration compatibility: PASS;
- critical read-only queries: PASS;
- tenant-isolation directed check: PASS;
- real email/WhatsApp/business transaction emission: none.

### Sensitive artifact cleanup

Run `35120379562` removed the retained full logical backup artifact after validation. N8.21.F certified:

- `sensitive_backup_retained=false`;
- P0=0;
- P1=0;
- secret values exposed=0.

## Disaster Recovery applicability

The material recovery drill above is causally applicable to N8.10 because the compare from the N8.21 TEST_CI certified head `11c705ea830c2b10036e5d44c9327ac77b954c3f` through the N8.10 DR documentation chain contains documentation/evidence/receipt changes only; no application, frontend, backend, migration, recovery-script, or workflow delta invalidates the recovery mechanics.

The measured Disaster Recovery timing and recovery-point interpretation are recorded separately in `docs/evidencias/rpo-rto-evidence.md`.

## Boundary

This reconciliation is evidence, not authorization to touch Production. It neither reads nor stores secret values and does not change `main`, Production, deployment targets, domains/certificates, database data, or PR #2.
