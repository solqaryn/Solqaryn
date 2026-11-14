# Certificación N8.10 — Disaster Recovery

Autoridad: `docs/VAEP_AUTHORITY.md`  
Rama: `Desarrollo`  
Alcance: ERP-N8 / N8.10 Disaster Recovery

## Resultado

N8.10 define y prueba un modelo operativo de recuperación ante desastres para Solqaryn sin alterar `main`, Producción, despliegues, secretos ni PR #2.

El contrato operativo canónico está en `docs/evidencias/dr-runbook.md` y define:

- RPO de ingeniería: `<= 24 horas` para datos MySQL durables;
- RTO de ingeniería end-to-end: `<= 60 minutos` hasta disponer de un candidato de recuperación aislado, validado y listo para una decisión de cutover autorizada;
- procedimiento de rollback/restore;
- recuperación siempre sobre target nuevo/aislado, nunca sobre la base fuente activa;
- responsables operativos;
- comunicación de incidente;
- validación de integridad, salud/readiness, compatibilidad de migraciones, lecturas críticas y aislamiento de tenant;
- REVIEW_FIRST con P0=0/P1=0 antes de recomendar cutover;
- exclusión de secretos de toda evidencia.

## Evidencia material

La cadena causal reutilizada es más fuerte que una simulación documental aislada:

- proveedor/backup/PITR/restore path certificado en N8.20;
- backup real de Desarrollo + restore del mismo artefacto cifrado a MySQL desechable aislado en GitHub Actions run `35111491587`;
- checksum, 132 tablas, 104 migraciones EF, 240 foreign keys, 542 índices y conteos exactos verificados;
- backend smoke contra el restore aislado en run `35113349678`;
- tenant isolation PASS y side effects reales deshabilitados;
- cleanup del backup completo retenido en run `35120379562`;
- `sensitive_backup_retained=false`.

Reconciliación canónica: `docs/evidencias/backup-restore-evidence.md`.

## RPO/RTO medidos

Evidencia: `docs/evidencias/rpo-rto-evidence.md`.

- inicio conservador del drill: `2026-09-16T14:52:24Z`;
- fin del backend sanity: `2026-09-16T15:13:08Z`;
- RTO observado: `1244 s` = `20m44s`;
- objetivo RTO: `<= 3600 s`;
- resultado RTO: PASS.

Para el recovery point seleccionado en el drill, el restore reprodujo el snapshot del artefacto con equivalencia exacta de conteos en las 132 tablas verificadas y sin divergencia detectada respecto al punto seleccionado. El gate de RPO de ingeniería `<=24h` queda PASS para este drill. Esto no se presenta como una medición de pérdida de datos de un incidente productivo real; un incidente real debe medir su propio delta temporal entre incidente y recovery point.

## Cobertura A–G

- `N8.10.A PRE`: modelo DR, alcance, RPO/RTO, procedimiento, responsables y comunicación — LISTO_REAL.
- `N8.10.B DOMAIN`: contrato operacional; sin nueva entidad/DTO/invariante de producto requerida — LISTO_REAL.
- `N8.10.C DB_MIG`: sin nueva migración; contrato de restore/integridad validado con evidencia real — LISTO_REAL.
- `N8.10.D BACKEND_API`: sin delta API; sanity real contra restore aislado — LISTO_REAL.
- `N8.10.E FRONTEND_UX`: N/A causal; no se introduce nueva superficie UI — LISTO_REAL.
- `N8.10.F SEC_AUDIT`: controles de seguridad/retención/tenant isolation y no-secret boundary — LISTO_REAL.
- `N8.10.G TEST_CI`: RPO/RTO medidos, recuperación material reutilizada causalmente, REVIEW_FIRST P0=0/P1=0/P2=0 — LISTO_REAL.

## Rollout y rollback

N8.10 no ejecuta rollout a Producción. Ante incidente real, el Incident Commander decide entre rollback de aplicación o recuperación de datos conforme al runbook. Todo restore de datos se materializa primero en target aislado; un candidato que falle validación no se promueve. El cutover productivo, si alguna vez corresponde, queda fuera de este alcance y exige autorización explícita bajo la autoridad vigente.

## Documentación histórica

`TASKS.md` y `CHANGELOG_AI.md` no son autoridad de estado machine-readable según `docs/VAEP_AUTHORITY.md`. Este cierre es de procedimiento/evidencia y no introduce delta funcional de producto. La trazabilidad autoritativa del estado queda en receipts VAEP + CONFIG/COLA/CONTROL_TOWER; la historia técnica del punto queda preservada por los commits y artefactos canónicos creados en esta cadena. No se reescribe historia previa para fabricar un cierre.

## Criterio de cierre

El Plan Maestro vigente define N8.10 como: RPO, RTO, procedimiento, responsables y comunicación, cerrable cuando sus microtareas estén LISTO y se cumpla el DoD global. La Fuente Rectora no exige una firma humana adicional para N8.10. La certificación técnica se materializa mediante REVIEW_FIRST, receipt verificable y readback del exact-head.

Estado previo al receipt final H: candidato documental completo, P0=0/P1=0/P2=0 conocidos, listo para REVIEW_FIRST y certificación VAEP.
