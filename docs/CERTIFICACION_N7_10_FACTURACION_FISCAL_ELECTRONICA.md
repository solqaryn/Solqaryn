# N7.10 — Facturación fiscal/electrónica — Certificación canónica

## Autoridad y alcance

Autoridad operativa única: `docs/VAEP_AUTHORITY.md`.

Esta certificación documenta el cierre técnico de ERP-N7.10 en `Desarrollo`. `N7.10.H` es `DOC_CERT`: no autoriza cambios funcionales nuevos ni amplía alcance a `main`, Producción, deploys, secretos o PR #2.

La dependencia inmediata es `N7.10.G`, cerrada como `LISTO_REAL` mediante `vaep/evidence/receipts/N7.10.G_LISTO_REAL_20260915T012330Z_SUP00.json`.

## Candidate funcional congelado

Candidate funcional final: `903901c6b30a4a2d70b4a52dc440ef8fc61adc5b`.

Los commits posteriores de REVIEW_FIRST, receipts y reconciliación documental deben conservar equivalencia funcional con ese candidate. N7.10.H no agrega lógica de producto ni schema.

## Contrato certificado

La cadena N7.10 certifica, dentro del alcance realmente implementado y probado:

- contratos fiscales dependientes de jurisdicción/proveedor y sin hardcodear una legislación internacional como universal;
- persistencia de emisión fiscal separada del snapshot comercial, con identidad tenant/sucursal y semántica durable de idempotencia/reintento;
- frontera HTTP autenticada y protegida por permiso de Facturación/Crear;
- comprobación server-side de membresía tenant, empresa activa con identidad fiscal verificable, sucursal del mismo tenant y coincidencia entre el snapshot fiscal de la factura y la identidad legal del tenant;
- proveedor fiscal seleccionado de forma explícita y fail-closed ante proveedor inexistente o ambiguo;
- frontend de emisión fiscal provider-neutral, con control fail-closed de permisos y sin exponer secretos;
- auditoría de resultados fiscales con metadatos operativos sanitizados, sin persistir claves de idempotencia, hash de snapshot, payloads del proveedor, referencias externas o excepciones crudas;
- correlation id, health checks y observabilidad heredados del pipeline global;
- regresión backend/frontend/Docker/MySQL/migraciones y controles de seguridad terminales en verde.

No se introdujo webhook fiscal en N7.10; por tanto autenticación de webhook no forma parte de la superficie implementada y no se certifica como comportamiento inexistente.

## Evidencia por microtarea

- N7.10.A — preflight `LISTO_REAL`: receipt commit `62c5b0f4def6305b40b798bd5152ec1e43eeb8c6`.
- N7.10.B — dominio/contratos `LISTO_REAL`: receipt commit `dfe3b21a996d8bb1c41175a6f310a2e8bb136624`.
- N7.10.C — persistencia/migración/datos `LISTO_REAL`: receipt commit `06db2f4f2b015b6ad352e05cf0ae02f121f90606`.
- N7.10.D — Application/API `LISTO_REAL`: receipt commit `52c8a602e78fa6c67e302251d609211b2aed4cdf`.
- N7.10.E — frontend/UX `LISTO_REAL`: receipt commit `9db85a9bc1b3a83c6a8a0ddd1dd75bed4ae7d737`.
- N7.10.F — seguridad/RBAC/auditoría/observabilidad `LISTO_REAL`: `vaep/evidence/receipts/N7.10.F_LISTO_REAL_20260915T011651Z_SUP00.json`.
- N7.10.G — QA/regresión/CI `LISTO_REAL`: `vaep/evidence/receipts/N7.10.G_LISTO_REAL_20260915T012330Z_SUP00.json`.

## QA, gates y causalidad

Sobre el functional candidate `903901c6b30a4a2d70b4a52dc440ef8fc61adc5b`, el workflow causal `34916355954` (`Desarrollo - Compilación y pruebas`) terminó `SUCCESS` con:

- Higiene del repositorio `104214765638=SUCCESS`;
- Docker y aislamiento de entornos `104214765818=SUCCESS`;
- Migraciones EF, variantes y cargas masivas en MySQL 8.4 `104214765855=SUCCESS`, incluyendo aplicación de migraciones actuales y pruebas de integración MySQL;
- Backend Release y pruebas `104214765858=SUCCESS`, con 2263 passed, 0 failed, 0 skipped y build Release con 0 warnings/0 errors;
- Frontend producción `104214765911=SUCCESS`.

El gate suplementario `34916355934` (`Priority 3 - ERP security and isolated restore`) terminó `SUCCESS` sobre el mismo candidate, incluyendo restore aislado de MySQL y controles de autorización/tenant/secrets.

Los commits posteriores al candidate hasta el receipt G agregan exclusivamente REVIEW_FIRST/receipts de F/G; no alteran producto, frontend ni schema.

## Operación y rollback

Operación: mantener la configuración fiscal por jurisdicción/proveedor explícita, el aislamiento tenant/sucursal, la idempotencia durable y la auditoría sanitizada. La ausencia o ambigüedad de configuración/proveedor debe fallar cerrada; nunca inferir aceptación de autoridad fiscal ni fabricar estados externos.

Rollback seguro: revertir únicamente cambios funcionales atribuibles a N7.10 cuando exista regresión causal. Deshabilitar o retirar un adaptador fiscal no debe reescribir facturas históricas ni convertir reglas de una jurisdicción en reglas universales. Preservar el camino comercial existente y los registros fiscales ya persistidos.

No se requiere ADR/ERD/OpenAPI nuevo en N7.10.H porque H no introduce arquitectura, schema ni contrato HTTP adicional; consolida y certifica lo ya implementado en B-G.

## DoD de N7.10.H

Para `N7.10.H=LISTO_REAL` deben cumplirse conjuntamente:

1. dependencia N7.10.G releída y válida;
2. fila existente N7.10.H preservada/reconciliada sin append/insert/delete de Sheet;
3. esta certificación persistida y releída;
4. `TASKS.md` y `CHANGELOG_AI.md` reconciliados de forma estrictamente aditiva/history-preserving;
5. prefijo byte-exacto de cada histórico demostrado y tamaño posterior mayor;
6. REVIEW_FIRST fresco con P0=0/P1=0;
7. equivalencia funcional demostrada contra `903901c6b30a4a2d70b4a52dc440ef8fc61adc5b`;
8. receipt H persistido y releído antes de promover `GATE-N7`.

Esta certificación no declara por sí sola `LISTO_REAL`.
