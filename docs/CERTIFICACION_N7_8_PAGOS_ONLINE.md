# N7.8 — Pagos online — Certificación canónica

## Autoridad y alcance

Autoridad operativa única: `docs/VAEP_AUTHORITY.md`.

Esta certificación documenta el cierre técnico de ERP-N7.8 en `Desarrollo`. `N7.8.H` es `DOC_CERT`: no autoriza cambios funcionales nuevos ni amplía alcance a `main`, Producción, deploys, secretos o PR #2.

La dependencia inmediata es `N7.8.G`, cerrada como `LISTO_REAL` mediante `vaep/evidence/receipts/N7.8.G_LISTO_REAL_20260914T212600Z_SUP48.json`.

## Candidate funcional congelado

Candidate funcional final: `b217cc00bfa9bfc452674f0bdacbef52e50186a7`.

Los commits posteriores de REVIEW_FIRST, receipts y reconciliación documental deben conservar equivalencia funcional con ese candidate. N7.8.H no agrega lógica de producto ni schema.

## Contrato certificado

La cadena N7.8 certifica, dentro del alcance realmente implementado y probado:

- modelo de dominio provider-agnostic para pagos online, sin almacenar PAN/CVV/card number;
- persistencia y migración MySQL tenant-scoped para intentos/estado de pago;
- servicio Application/API con idempotencia durable, validación de factura/saldo/moneda y errores controlados;
- proveedores desacoplados mediante `IPagoOnlineProvider` y referencias de pago externas;
- URL de checkout devuelta por proveedor obligatoriamente absoluta HTTPS; URLs HTTP fallan cerrado;
- redacción de errores externos mediante código seguro `PROVIDER_INIT_FAILED` sin filtrar detalle sensible;
- boundary autenticado con RBAC/tenant isolation para operaciones de pago y callback externo aislado según su contrato;
- frontend/UX de pagos online certificado antes del hardening final de backend;
- regresión backend, migración/integración MySQL, contratos/build frontend, seguridad/multitenancy y auditoría de dependencias en verde.

## Evidencia por microtarea

- N7.8.A: preflight/cierre canónico recuperado mediante `vaep/evidence/receipts/N7.8.A_LISTO_REAL_20260914T191100Z_RECOVERY_SUP29.json`.
- N7.8.B: modelo de dominio certificado por commit receipt `1902ff298e0fac1e79ee8d154e19020d0631fb9a`.
- N7.8.C: `vaep/evidence/receipts/N7.8.C_LISTO_REAL_20260914T195300Z_SUP24.json`.
- N7.8.D: `vaep/evidence/receipts/N7.8.D_LISTO_REAL_20260914T201200Z_SUP24.json`.
- N7.8.E: `vaep/evidence/receipts/N7.8.E_LISTO_REAL_20260914T2043Z_SUP24.json`.
- N7.8.F: `vaep/evidence/receipts/N7.8.F_LISTO_REAL_20260914T210900Z_SUP48.json`; resolvió same-run el P1 `INSECURE_PROVIDER_CHECKOUT_URL_ALLOWED` y dejó REVIEW_FIRST P0=0/P1=0.
- N7.8.G: `vaep/evidence/receipts/N7.8.G_LISTO_REAL_20260914T212600Z_SUP48.json`; TEST_CI final con P0=0/P1=0.

## QA, gates y causalidad

Sobre el candidate `b217cc00bfa9bfc452674f0bdacbef52e50186a7` quedaron terminales en PASS los gates aplicables:

- `34896693855` — `Desarrollo - Compilación y pruebas`: SUCCESS, incluyendo Release tests y MySQL migration/integration;
- `34896693692` — `Priority 4 - Interfaces architecture and quality`: SUCCESS;
- `34896693717` — `Priority 3 - Security, Compliance, and Multi-tenancy`: SUCCESS;
- `34896693820` — `Fase 2 - Auditoría de configuración y dependencias`: SUCCESS.

El run de aceptación integral `34896693703` terminó failure por suites legacy/global de responsive/navigation: recursos 404 genéricos y overflow de `/configuracion`. El compare causal de N7.8 posterior al receipt UI cambia exclusivamente `backend/src/Application/Services/PagoOnlineService.cs` y `backend/tests/Solqaryn.Tests/N78DPagoOnlineServiceTests.cs`; no modifica frontend, `/configuracion`, assets, rutas ni responsive. Conforme a `docs/VAEP_AUTHORITY.md`, ese fallo no relacionado no bloquea N7.8 y tampoco se usa como PASS.

No existe criterio de aceptación performance-specific ni gate dedicado atribuible a N7.8; performance se clasifica N/A, no como PASS fabricado.

## Operación y rollback

Operación: mantener tenant/RBAC en operaciones autenticadas, claves de idempotencia no reversibles, referencias de proveedor externas y checkout HTTPS-only. No registrar ni persistir datos de tarjeta; cualquier integración que requiera captura de tarjeta queda fuera de alcance sin cumplimiento PCI específico.

Rollback seguro: revertir únicamente cambios funcionales atribuibles a N7.8 cuando exista regresión causal. N7.8.H no autoriza deploy, modificación de secretos, cambios de Producción o `main`, ni borrado/reescritura de evidencia histórica.

No se requiere ADR/ERD nuevo para DOC_CERT H: la decisión de desacoplamiento provider-agnostic y el modelo/persistencia están materializados y certificados en B-C; H no introduce arquitectura ni schema. No se modifica OpenAPI porque H no cambia el contrato HTTP funcional.

## DoD de N7.8.H

Para `N7.8.H=LISTO_REAL` deben cumplirse conjuntamente:

1. dependencia N7.8.G releída y válida;
2. SHEET_SCHEMA_GUARD sobre la fila existente de N7.8.H;
3. esta certificación persistida y releída;
4. `TASKS.md` y `CHANGELOG_AI.md` reconciliados de forma estrictamente aditiva/history-preserving porque el punto cambia de estado;
5. prefijo byte-exacto de cada histórico demostrado y tamaño posterior mayor;
6. REVIEW_FIRST fresco con P0=0/P1=0;
7. equivalencia funcional demostrada contra `b217cc00bfa9bfc452674f0bdacbef52e50186a7`;
8. receipt H persistido y releído antes de promover el sucesor dependency-valid.

Esta certificación no declara por sí sola `LISTO_REAL`.
