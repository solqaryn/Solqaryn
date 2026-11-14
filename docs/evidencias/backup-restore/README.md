# N8.21 — Backup real y restore aislado de Desarrollo

Esta carpeta contiene la certificación causal de N8.21 para `jmejia31/Solqaryn`, exclusivamente sobre la rama `Desarrollo`.

## Resultado certificado

- Backup automático administrado de la instancia DEV: **PROVEN** por la evidencia causal de N8.20 sobre Aiven `solqaryn-mysql`, con recovery mode `pitr`, política de retención demostrada y ruta de restore autorizada.
- Backup lógico temporal real de la base de Desarrollo: **PASS**.
- Restore aislado del mismo artefacto lógico cifrado: **PASS**.
- Verificación de checksums externos e internos: **PASS**.
- Conteos de todas las tablas: **PASS**.
- Topología validada: **132 tablas**, **104 migraciones EF**, **240 foreign keys** y **542 índices**.
- Smoke de backend contra el restore aislado: **PASS**.
- Base activa de Desarrollo modificada por el restore: **NO**.
- Producción tocada: **NO**.
- Secretos expuestos: **0**.
- Artefacto temporal con el backup lógico completo retenido tras la validación: **NO**.
- Candidatos `REMOVE_SAFE_DB_AFTER_BACKUP` pendientes: **0**.

## Cadena causal

1. `N8.20.H` certificó mediante control-plane/runtime evidence el proveedor Aiven, el servicio real, plan, backup/PITR/retención y capacidad/ruta de restore sin ejecutar acciones pagadas ni tocar Producción.
2. `N8.21.C` ejecutó el backup lógico temporal real y el restore aislado en el run `35111491587`, complementando la prueba de capacidad administrada con un recovery drill material y verificable.
3. `N8.21.D` ejecutó smoke real del backend contra el restore aislado en el run `35113349678`.
4. `N8.21.E` demostró que no existe superficie frontend de producto para esta operación.
5. `N8.21.F` detectó y corrigió same-run la retención innecesaria del artefacto lógico completo; el run de cleanup `35120379562` terminó `success` y el readback confirmó ausencia del artefacto `10452911215`.
6. `N8.21.G` reconcilió la cadena backup → restore → sanity, cerró formalmente `ARCH-02` sin borrado especulativo y preparó la reconciliación causal de N8.8/N8.9.

## Evidencia canónica

- `BACKUP_MANIFEST.md`
- `RESTORE_VERIFICATION.md`
- `CLEANUP.md`
- `N8.21.G_TEST_CI_RECONCILIATION.json`
- `N8.8_N8.9_RECONCILIATION.md`
- `vaep/evidence/receipts/N8.20.H_LISTO_REAL_20260916T135053Z_SUP00.json`
- Receipts y REVIEW_FIRST de N8.21 bajo `vaep/evidence/`.

La evidencia histórica no se reescribe. Las conclusiones previas quedan supersedidas únicamente cuando existe evidencia causal más nueva y explícitamente referenciada.
