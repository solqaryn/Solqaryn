# N7.4 — Idempotencia — Certificación canónica

## Autoridad y alcance

Autoridad operativa única: `docs/VAEP_AUTHORITY.md`.

Esta certificación documenta el cierre técnico de ERP-N7.4 para evitar efectos duplicados en integraciones externas mediante una identidad idempotente durable y tenant-scoped, sin ampliar el alcance a `main`, Producción, deploys, secretos ni PR #2.

`N7.4.A` a `N7.4.G` tienen receipts `LISTO_REAL` persistidos en `Desarrollo`. La dependencia inmediata de este DOC_CERT es `N7.4.G`, receipt `vaep/evidence/receipts/N7.4.G_LISTO_REAL_20260913T220100Z_SUP48.json`.

## Candidate funcional congelado

Candidate funcional: `1c366562a90c590cc2925a153298fd1b758e8dab`.

Después de ese candidate, los cambios de REVIEW_FIRST/certificación son exclusivamente control/evidencia; no existe delta de producto que invalide la equivalencia funcional certificada por N7.4.F/G.

## Contrato certificado

La implementación certificada garantiza, dentro del scope material verificado:

- búsqueda de idempotencia acotada por `EmpresaId`/tenant;
- normalización de la clave de idempotencia antes de resolver el intento;
- misma empresa + misma clave + mismo request devuelve el resultado existente sin segundo efecto;
- misma empresa + misma clave + request diferente falla con conflicto y sin segundo efecto;
- claves nuevas conservan una única intención efectiva;
- replay cross-tenant falla cerrado;
- no existe endpoint expuesto de replay/requeue que amplíe la superficie de ataque;
- no se exponen claves de idempotencia, payloads sensibles ni errores de proveedor en la evidencia/auditoría certificada;
- la persistencia durable existente conserva identidad/uniqueness por tenant y event id sin requerir nueva migración N7.4.C.

## Persistencia y migraciones

N7.4.C certificó que no era necesario un delta de esquema. La persistencia durable reutilizada se encuentra en `backend/src/Infrastructure/Persistence/Configurations/MensajeOutboxConfiguration.cs` y la migración base en `backend/src/Infrastructure/Migrations/20260913050800_N71COutboxPersistence.cs`.

La decisión `NOT_APPLICABLE_EXISTING_PERSISTENCE_SUFFICIENT` preserva historia y evita una migración artificial: N7.4 no introduce un segundo ledger ni una segunda autoridad de idempotencia.

## API, seguridad y regresión

N7.4.D corrigió y certificó el contrato de replay idempotente sobre el candidate funcional `1c366562a90c590cc2925a153298fd1b758e8dab`, con REVIEW_FIRST P0=0/P1=0.

N7.4.F revalidó tenant isolation, fail-closed cross-tenant, no segundo efecto y ausencia de leakage sensible. N7.4.G cerró la matriz de regresión sin nuevo patch funcional.

Gates causales terminales reutilizados por equivalencia funcional:

- `Desarrollo - Compilación y pruebas` run `34782712890`, job `103792518381`: Backend Release y pruebas = `success`, `2189 passed, 0 failed, 0 skipped`.
- mismo run, job `103792518352`: migraciones EF + integración MySQL 8.4 = `success`.
- mismo run, job `103792518396`: frontend lint + producción = `success`.
- `Priority 3 - ERP security and isolated restore` run `34782712884`, job `103792518243`: authorization/files/secrets/tenant = `success`.
- mismo run, job `103792518046`: MySQL 8.4 isolated backup/restore = `success`.

No se vuelve a ejecutar CI costoso por este DOC_CERT mientras el delta posterior permanezca estrictamente documental/evidencia y se demuestre equivalencia con el candidate funcional congelado.

## Operación y rollback

Operación: el contrato idempotente debe mantenerse tenant-scoped; una clave repetida sólo puede reutilizar el resultado previo cuando la intención/request sea equivalente. Una intención distinta con la misma clave debe permanecer fail-closed y no producir un segundo efecto.

Rollback seguro del scope N7.4: revertir únicamente los commits funcionales atribuibles a N7.4.D si una regresión causal lo exige, preservando la persistencia histórica existente y sin alterar datos de Producción desde esta tarea. Cualquier cambio de schema futuro exige su propia microtarea DB_MIG, preflight/postcheck y gates causales; este DOC_CERT no autoriza deploy ni migración productiva.

## DoD de N7.4.H

Para `N7.4.H=LISTO_REAL` deben cumplirse conjuntamente:

1. dependencia N7.4.G releída y válida;
2. SHEET_SCHEMA_GUARD sobre la fila existente de N7.4.H;
3. esta certificación persistida y releída;
4. reconciliación documental aditiva/history-preserving requerida por el estado (`TASKS.md` y `CHANGELOG_AI.md` cuando corresponda);
5. REVIEW_FIRST fresco con P0=0/P1=0;
6. equivalencia funcional demostrada contra `1c366562a90c590cc2925a153298fd1b758e8dab`;
7. receipt H persistido y releído antes de promover N7.5.A.

Esta certificación no declara por sí sola `LISTO_REAL`.