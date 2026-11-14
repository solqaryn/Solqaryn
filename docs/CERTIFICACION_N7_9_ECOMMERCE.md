# N7.9 — Ecommerce — Certificación canónica

## Autoridad y alcance

Autoridad operativa única: `docs/VAEP_AUTHORITY.md`.

Esta certificación documenta el cierre técnico de ERP-N7.9 en `Desarrollo`. `N7.9.H` es `DOC_CERT`: no autoriza cambios funcionales nuevos ni amplía alcance a `main`, Producción, deploys, secretos o PR #2.

La dependencia inmediata es `N7.9.G`, cerrada como `LISTO_REAL` mediante `vaep/evidence/receipts/N7.9.G_LISTO_REAL_20260914T230920Z_SUP00.json`.

## Candidate funcional congelado

Candidate funcional final: `5de96492290218639f18c1648668215339c34a1e`.

Los commits posteriores de REVIEW_FIRST, receipts y reconciliación documental deben conservar equivalencia funcional con ese candidate. N7.9.H no agrega lógica de producto ni schema.

## Contrato certificado

La cadena N7.9 certifica, dentro del alcance realmente implementado y probado:

- tienda pública con autoridad server-side sobre disponibilidad/precio y rate limit para tráfico anónimo;
- identidad de variante física exacta preservada desde catálogo/carrito hasta checkout, con recuperación backward-compatible sólo cuando la variante legada es inequívoca;
- checkout fail-closed ante ambigüedad de variante y sin confiar en precio/stock suministrados por cliente;
- persistencia existente reutilizada cuando correspondía, sin migración especulativa en N7.9.C;
- API, frontend y UX alineados con la identidad física de variante;
- controles de seguridad y multitenancy heredados/certificados por N7.9.F, incluyendo redacción de errores y ausencia de exposición de PAN/CVV/secrets;
- regresión backend, MySQL/migraciones, frontend, Docker e higiene de repositorio terminales en verde.

## Evidencia por microtarea

- N7.9.A: preflight `LISTO_REAL` por commit receipt `d16d1f2f487188137a1ef1e505fb87402051e402`.
- N7.9.B: dominio/contratos `LISTO_REAL` por commit receipt `30a4971885436d66f30d6c93c8f344d71f44a695`.
- N7.9.C: persistencia `LISTO_REAL` sin migración especulativa por commit receipt `970e4e4caf2e5111a827addbf37d3c85161d1d8a`.
- N7.9.D: Application/API `LISTO_REAL` por commit receipt `339228c2b3f4f522d0c6ed8baf92cf785d13966b`.
- N7.9.E: frontend/UX `LISTO_REAL` por commit `5a3afda2140cd683ea6db9dca2cc342e68c880c1`.
- N7.9.F: seguridad/RBAC/observabilidad `LISTO_REAL` mediante `vaep/evidence/receipts/N7.9.F_LISTO_REAL_20260914T225045Z_SUP24.json`; P1 de storefront anónimo sin rate limit resuelto same-run.
- N7.9.G: TEST_CI `LISTO_REAL` mediante `vaep/evidence/receipts/N7.9.G_LISTO_REAL_20260914T230920Z_SUP00.json`.

## QA, gates y causalidad

Sobre el functional candidate `5de96492290218639f18c1648668215339c34a1e`, el workflow causal `34905794077` (`Desarrollo - Compilación y pruebas`) terminó con:

- Backend Release y pruebas `104182106669=SUCCESS`, 2245 passed, 0 failed, 0 skipped;
- Docker y aislamiento de entornos `104182106761=SUCCESS`;
- Frontend producción `104182106820=SUCCESS`;
- Higiene del repositorio `104182106850=SUCCESS`;
- Migraciones EF, variantes y cargas masivas en MySQL 8.4 `104182106950=SUCCESS`, incluyendo pruebas de integración MySQL.

Los commits `4879a004...`, `6efeef8c...` y `9a1c1408...` posteriores al candidate agregan exclusivamente REVIEW_FIRST/receipts de F/G; no alteran producto, frontend ni schema. Por ello no se rerunean gates ya terminales sobre el exact functional candidate.

## Operación y rollback

Operación: mantener autoridad de precio/stock/variante en backend, rate limit del storefront anónimo, aislamiento tenant/RBAC donde aplica y flujo de pagos sin exposición de datos de tarjeta ni secretos.

Rollback seguro: revertir únicamente cambios funcionales atribuibles a N7.9 cuando exista regresión causal. N7.9.H no autoriza deploy, modificación de secretos, cambios de Producción o `main`, ni borrado/reescritura de evidencia histórica.

No se requiere ADR/ERD/OpenAPI nuevo para DOC_CERT H: H no introduce arquitectura, schema ni contrato HTTP funcional; consolida evidencia del comportamiento ya implementado y probado.

## DoD de N7.9.H

Para `N7.9.H=LISTO_REAL` deben cumplirse conjuntamente:

1. dependencia N7.9.G releída y válida;
2. fila existente N7.9.H preservada/reconciliada sin append/insert/delete de Sheet;
3. esta certificación persistida y releída;
4. `TASKS.md` y `CHANGELOG_AI.md` reconciliados estrictamente de forma aditiva/history-preserving;
5. prefijo byte-exacto de cada histórico demostrado y tamaño posterior mayor;
6. REVIEW_FIRST fresco con P0=0/P1=0;
7. equivalencia funcional demostrada contra `5de96492290218639f18c1648668215339c34a1e`;
8. receipt H persistido y releído antes de promover `N7.10.A`.

Esta certificación no declara por sí sola `LISTO_REAL`.
