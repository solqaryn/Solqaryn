# N7.5 — Webhooks — Certificación canónica

## Autoridad y alcance

Autoridad operativa única: `docs/VAEP_AUTHORITY.md`.

Esta certificación documenta el cierre técnico de ERP-N7.5 para recepción segura de webhooks externos en `Desarrollo`, sin ampliar el alcance a `main`, Producción, deploys, secretos ni PR #2. `N7.5.H` es exclusivamente `DOC_CERT`: no autoriza cambios funcionales nuevos ni inventa una interfaz de administración que no exista en el contrato certificado.

La dependencia inmediata es `N7.5.G`, cerrada como `LISTO_REAL` mediante `vaep/evidence/receipts/N7.5.G_LISTO_REAL_20260914T030250Z_SUP36.json`.

## Candidate funcional congelado

Candidate funcional exacto: `dbd515909d98893f4924bb73d0a48f74fe9b97c3`.

El commit de receipt `51af3d136ac4eeade0b38dab710f99f4c793d8a6` está exactamente un commit por delante y agrega únicamente `vaep/evidence/receipts/N7.5.G_LISTO_REAL_20260914T030250Z_SUP36.json` (`+67/-0`). Por tanto, el cierre documental H debe conservar equivalencia funcional con `dbd515909d98893f4924bb73d0a48f74fe9b97c3`.

## Contrato certificado

La cadena N7.5 certifica, dentro del alcance verificado:

- validación HMAC antes de persistir un webhook entrante;
- resolución de secreto acotada por tenant/empresa y proveedor;
- persistencia durable del evento entrante con identidad externa tenant-scoped;
- replay del mismo evento con payload equivalente idempotente y sin segundo efecto;
- mismo identificador externo con payload distinto rechazado como conflicto, fail-closed;
- clasificación segura de carreras de unicidad mediante relectura del registro durable;
- ausencia de persistencia del secreto, firma o material de autenticación sensible;
- respuestas de error sin detalle interno de proveedor o base de datos;
- auditoría/correlación con identificador saneado y logging estructurado sin payload crudo, firma, secreto ni identificador externo sensible;
- rechazo de firma inválida, timestamp stale y tenant mismatch;
- lectura del body acotada también para requests chunked/sin `Content-Length`, evitando bypass del límite por streaming;
- ausencia intencional de una UI de administración de secretos/webhooks cuando no existe contrato browser-facing que la soporte.

## Persistencia y migración

La persistencia de `WebhookEntrante` fue materializada antes del cierre de Application/API. La migración N7.5.C crea la relación con `EmpresaId` bajo `Restrict`, protege la identidad mediante unicidad `(EmpresaId, Proveedor, EventoExternoId)` y añade el índice operativo `(EmpresaId, Estado, RecibidoEnUtc, Id)`.

N7.5.H no agrega schema, backfill ni una segunda autoridad de eventos. Cualquier cambio futuro de persistencia exige su propia microtarea DB_MIG y gates causales.

## Application/API, frontend y seguridad

N7.5.D certificó la recepción de webhook con firma HMAC, scope tenant/proveedor, persistencia, replay idempotente, conflicto de payload y manejo fail-closed. Su receipt canónico es `vaep/evidence/receipts/N7.5.D_LISTO_REAL_20260914T020500Z_SUP36.json`.

N7.5.E quedó `LISTO_REAL / FRONTEND_NA_GROUNDED`: el endpoint es machine-to-machine y el contrato activo no expone administración browser-facing de secretos ni eventos entrantes. Crear UI habría sido filler y ampliación de scope. Receipt: `vaep/evidence/receipts/N7.5.E_LISTO_REAL_20260914T022530Z_SUP36.json`.

N7.5.F reforzó observabilidad/auditoría segura: correlación saneada mediante `HttpContext.TraceIdentifier`, outcomes estructurados y ausencia de secreto, firma, payload crudo o event id externo en logs. Receipt: `vaep/evidence/receipts/N7.5.F_LISTO_REAL_20260914T023917Z_SUP24.json`.

N7.5.G completó la regresión de seguridad, incluyendo firma inválida, timestamp stale, tenant mismatch, replay duplicate/conflict, body oversized con y sin `Content-Length` y no filtración sensible. REVIEW_FIRST terminó P0=0/P1=0.

## Gates causales y equivalencia

El candidate funcional `dbd515909d98893f4924bb73d0a48f74fe9b97c3` quedó certificado con gates terminales exact-head:

- `VAEP engine` run `34800735379`, job `103842737649`: `success`.
- `Static contracts, tests and production build` run `34800735411`, job `103842739664`: `success`; backend restore/build/test=`success` y frontend lint/build=`success`.

N7.5.H no debe rerunear esos gates costosos mientras su delta permanezca estrictamente documental/evidencia y la equivalencia funcional se demuestre. Sus gates causales propios son la integridad del paquete documental, la preservación aditiva de históricos, REVIEW_FIRST final P0=0/P1=0 y readback del receipt.

## Operación y rollback

Operación: cada webhook debe autenticarse y acotarse al tenant/proveedor antes de persistencia; el replay equivalente debe converger al resultado durable previo y un replay conflictivo debe fallar cerrado. Los límites de body y freshness son parte de la superficie de seguridad y no deben relajarse sin un cambio explícito y sus pruebas causales.

Rollback seguro: revertir únicamente cambios funcionales atribuibles a N7.5 si una regresión causal lo exige, preservando eventos históricos y sin tocar Producción desde esta tarea. Las evidencias y rollups documentales son historia; no autorizan borrado de datos, deploy, rotación de secretos ni cambios de `main`.

## DoD de N7.5.H

Para `N7.5.H=LISTO_REAL` deben cumplirse conjuntamente:

1. dependencia N7.5.G releída y válida;
2. SHEET_SCHEMA_GUARD sobre la fila existente de N7.5.H;
3. esta certificación persistida y releída;
4. `TASKS.md` y `CHANGELOG_AI.md` reconciliados de forma estrictamente aditiva/history-preserving cuando corresponda;
5. compare de cada histórico con `additions>0` y `deletions=0`;
6. REVIEW_FIRST fresco con P0=0/P1=0;
7. equivalencia funcional demostrada contra `dbd515909d98893f4924bb73d0a48f74fe9b97c3`;
8. receipt H persistido y releído antes de promover `N7.6.A`.

Esta certificación no declara por sí sola `LISTO_REAL`.
