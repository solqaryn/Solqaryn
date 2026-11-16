# SOLQARYN V17 — C15-02.M03.05: worker acotado de revisión de arquitectura

Autor: PRIMARY DEV 06 JAVIER; TASK_ID 6af16bbad26881918c283abe06e1f32a; solqaryn/Solqaryn@dev; PROJECT_SCOPE_LOCK=STRICT. Riesgo R2 por revisión de migración/tenancy/proveedores, no R3.

## Entrega ejecutable

V5ArchitectureMigrationBoundedReadWorker revisa secuencialmente de 1 a 12 probes en memoria a través de un puerto IV5ArchitectureMigrationReadReviewPort que compone el adaptador aislado C15-02.M03.04, el preflight Application M03.03 y las reglas Domain M03.01. Antes de consultar cualquier puerto valida identificadores ordinales, tenant, SHA exacto, ausencia de comandos de mutación, scope homogéneo y duplicados. Si cualquier registro falla, descarta el conjunto de resultados para impedir lecturas aparentemente aprobadas parciales.

La respuesta admisible exige evidencia externa autenticada y A5 independiente. No verifica un JWT real, no conecta MySQL, no crea jobs alojados, no despliega, no cambia DNS/R2/Render/Vercel, no lee secretos, no ejecuta backups/restauraciones ni concede permisos. Un Principal sintético en xUnit es una prueba estructural, no una autenticación de cliente.

## Validación adaptativa A0–A5

A0: código Infrastructure, once pruebas xUnit, documento y CHANGELOG autoconclusivo; sin cambios Domain/Application/API/EF, sin PR/branch adicional. A1: positivos homogéneos; negativos tenant B, SHA obsoleto, revocación, duplicados, mezclas, lote nulo/excesivo, comandos live, errores de lectura y cancelación. A2: RBAC, tenant A/B, denegación fail-closed, ningún side-effect. A3: build net10.0 y pruebas xUnit reales vía GitHub-hosted; DB/HTTP/Playwright NO_APLICA porque el diff no conecta servicios reales. A4: verificar Scope Lock, procedencia V5, backend, seguridad y autorización en SHA exacto o equivalencia documentada, readback PLAN/COLA/DAG/BITACORA. A5: revisión independiente por otra Task Javier distinta del autor. Rollback git revert normal; QA/main/PROD fuera de alcance.
