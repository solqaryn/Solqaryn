# V22 - C02-03.M03.03 - rechazo de alias numericos de tenant

Microtarea: C02-03.M03.03, propiedad historica DEV05, repo solqaryn/Solqaryn rama dev. Riesgo R2: autorizacion, fronteras tenant/JWT y MySQL; PROJECT_SCOPE_LOCK=STRICT. Solo cambios Application de preflight read-only y pruebas, sin cambiar API productiva, RBAC, tablas, migraciones, secretos, entornos ni la fuente de autoridad institucional.

## Hipotesis causal y A0

La validacion SafeId previa aceptaba `01`, `001` y `000123` porque permitia cualquier ID de longitud mayor que uno si era ASCII. MySQL convierte la representacion decimal `01` al mismo INT que `1`. En la integracion existente, el lector de evidencia usa `int.TryParse`, filtra SQL por ese INT y conserva la representacion textual recibida como `VerifiedTenantId`; ambos valores podian coincidir y producir preflight positivo con un alias no canonico. Se conserva la posibilidad de un SQL ID legitimo de un solo digito positivo y de IDs alfanumericos opacos, pero se rechazan cadenas exclusivamente numericas que empiezan por cero antes de consultar el reader.

## A1 / A2 / A3

- xUnit de Application: `01`, `001`, `00`, `000123` deben devolver `LIFECYCLE_ACCESS_DENIED` sin llamar al lector; persisten los tests positivos `1`, `7`, `9`, los rechazos de tenant B, revocacion, JWT no acreditado y comando stale.
- Integracion GitHub-hosted dirigida: JWT HMAC firmado de pruebas con TestServer HTTP y MySQL 8.4 efimero. Con fila SQL real de tenant A, un GET protegido que contiene el alias `0{tenantA}` debe devolver HTTP 403. El mismo token con tenant A canonico debe seguir obteniendo solo `VERIFIED_READONLY_PREFLIGHT_ONLY`; JWT falso o ausente 401, tenant B/revocacion/operacion alterada 403, rollback sin fila 403.
- No se certifica EF/UsuarioEmpresa de produccion, proveedor PSP, CAS durable de ciclo de vida, receipt idempotente, revocacion de policy revision ni full tenant lifecycle. El lector y ruta pertenecen exclusivamente al harness de TestServer, no a una ruta API comercial desplegada.

## A4 / A5 y continuidad

Exigir CI real del nuevo SHA: workflow C02-03 JWT HTTP MySQL8.4 con ambos tests y TRX sin failures/skips, backend Release, seguridad, Scope Lock y procedencia V5 exitosos. CANCELLED/SKIPPED/IN_PROGRESS no son PASS. Mantener H=VALIDANDO, owner DEV05 y A5 independiente por otra Task Javier despues de A0-A4 integrales. El PLAN D376 aun exige implementar autorizacion y tenancy durables, relectura atomica/CAS, receipts y rollback por cada operacion; este parche NO cierra ese alcance. Registro BITACORA idempotente y readback. Rollback normal git revert, nunca force-push ni QA/main/PROD.
