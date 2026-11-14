# N8.8 — Backup — Certificación current-standard

Fecha de revalidación: 2026-09-17  
Rama: `Desarrollo`  
Autoridad única: `docs/VAEP_AUTHORITY.md`  
Producción/main/merge PR #2/secrets/DNS/certificados: **fuera de alcance y no modificados**.

## Alcance certificado

N8.8 comprueba el mecanismo de backup automático y restauración verificable de Solqaryn en Desarrollo. La revalidación current-standard preserva el diseño M11 existente y añade evidencia fresca del proveedor Aiven sin ejecutar restore, fork, upgrade ni acción pagada.

## Evidencia causal vigente

- `M11 - Backup operativo de Desarrollo`, run `35223693841`, attempt 1: `SUCCESS` sobre `ab7b5a35cdc91312254b8a96b13ac24c53e28f44`.
- `M11 - Backup y restauración en Desarrollo`, run `35223693868`, attempt 1: `SUCCESS` sobre el mismo tested head. El drill crea backup cifrado, verifica checksum/retención, restaura en MySQL descartable, valida integridad y arranca la API restaurada con health checks.
- `N8.20.F - Aiven DEV Capabilities Read-only Proof`, run `35223698590`, attempt 4: `SUCCESS`. Evidencia sanitizada persistida en `6aee547a5133d77ec65f1c674ac08af87dd456e1`.

La prueba de Aiven confirmó servicio MySQL `RUNNING`, endpoint de backups accesible, tres backups observados, PITR disponible, política de retención leída y capacidad/autorización de restore identificada. La ejecución fue exclusivamente read-only: no se creó fork/restore, no se cambió plan y no se expusieron credenciales.

## Equivalencia causal de la superficie de backup

Entre el tested head de los gates M11 y el candidato revisado, las piezas ejecutables de backup/restore permanecen byte-identical:

- `.github/workflows/m11-backup-restore-desarrollo.yml`: blob `5cba214846ace7c45984d23b3e0db013404c3f01`.
- `scripts/m11_backup_desarrollo.sh`: blob `c160f588f465bc4b69f447225825eccef79ce6dd`.
- `scripts/m11_restore_desarrollo.sh`: blob `8dad281093d69eb2c3e55722516ac63f35bf6968`.

Esta equivalencia se limita estrictamente a la superficie N8.8 de backup/restore; no se usa para afirmar equivalencia global de cambios ajenos al punto.

## Controles de seguridad verificados

- backup limitado a Desarrollo/CI y fail-closed para nombres/entornos de Producción;
- cifrado OpenPGP AES-256;
- checksum SHA-256 externo y manifest interno;
- secretos sólo por variables de entorno y fuera de metadata/artifacts;
- restore permitido únicamente en CI/Desarrollo descartable, host local y nombre de base explícitamente descartable;
- `ALLOW_DESTRUCTIVE_RESTORE=YES_M11` obligatorio;
- integridad post-restore por tablas, migraciones y conteos;
- Producción no tocada y ningún restore contra Aiven ejecutado durante esta revalidación.

## Estado por microtarea

- N8.8.A — PRE: `LISTO` current-standard.
- N8.8.B — DOMAIN: `LISTO` current-standard / N/A grounded.
- N8.8.C — DB_MIG: `LISTO` current-standard / N/A grounded según alcance operacional.
- N8.8.D — BACKEND_API: `LISTO` current-standard / sin delta de API requerido; smoke de API restaurada PASS.
- N8.8.E — FRONTEND_UX: `LISTO` current-standard / N/A grounded.
- N8.8.F — SEC_AUDIT: `LISTO` current-standard.
- N8.8.G — TEST_CI: `LISTO` current-standard mediante receipt `vaep/evidence/receipts/N8.8.G_REVALIDATED_CURRENT_STANDARD_LISTO_20260917T154700Z_SUP36.json`.
- N8.8.H — DOC_CERT: en revalidación hasta completar REVIEW_FIRST documental, actualización history-preserving de `TASKS.md` y `CHANGELOG_AI.md`, receipt y readback final.

## Dictamen

La evidencia técnica material de N8.8.A-G satisface el estándar current-standard con P0=0/P1=0 para el alcance funcional/técnico ya certificado. N8.8.H **no debe declararse LISTO** hasta que el cierre documental quede reconciliado de forma aditiva en `TASKS.md` y `CHANGELOG_AI.md` y exista receipt H con write/readback.
