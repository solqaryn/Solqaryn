# SOLQARYN V22 — C11-03.M03.01: reglas Domain de eventos/outbox

**Task:** PRIMARY DEV 04 — JAVIER. **Rama:** `solqaryn/Solqaryn@dev`. **Scope Lock:** STRICT. **Riesgo:** R2 por idempotencia, tenant, auditoría y retry; **impacto ejecutable:** solo código Domain nuevo sin consumidor vivo.

## A0 — Salida atómica y dependencias

PLAN_MAESTRO D1670 exige **implementar reglas de dominio y validaciones server-side**. La meta acumulativa E1670 ("Completa M03") no atribuye a M03.01 las implementaciones posteriores M03.02 servicio de aplicación, M03.03 autorización, M03.04 repositorio y MySQL, M03.05 worker/proveedor ni M03.06 integración/cierre. Documentos base C11-03.M02.05/M02.06 establecen tenant-scope, replay, no efectos duplicados, ACK durable, límites y protección de datos.

Se añade una clase Domain estática de validaciones puras sobre snapshots de **procedencia backend confiable**, y una batería de xUnit adversariales .NET 10. No modifica `MensajeOutbox`, API, Application, EF, migraciones, Jobs, JWT, secretos, BBDD, sesiones, auditoría real ni ningún sistema productivo. Todos los resultados de preflight tienen `AuthorizesDispatch=false`, `AuthorizesPersistence=false` y `CertifiesJwtOrMySqlIntegration=false`.

## A1 y A2 — Oráculos que deben fallar ante defectos

El test dirigido verifica positivo domain-only, falta de contexto trusted, membresía/grant revocados, empresa B contra A, tipo fuera del catálogo, llave/tipo/sha corruptos, timestamp no UTC/futuro, versión inválida, retries agotados, correlación manipulada sin autoridad, replay exacto sin segunda entrega, payload divergente con misma llave, intento de otro tenant, DeadLetter terminal, ACK obsoleto y timeout sin acuse durable. No se consideran pruebas de JWT firmado, RBAC HTTP, propiedad contra MySQL ni aislamiento real de una ruta.

## A3 — Límites de integración

**NO_APLICA a este diff atómico:** ninguna ruta HTTP, persistencia ni interfaz UI existente cambia; las pruebas xUnit Domain prueban solo el código Domain nuevo. La autoridad JWT/RBAC real debe acreditarse al implementar M03.03, y la persistencia MySQL 8.4 con unicidad transaccional/CAS y rollback al implementar M03.04; los ACK durables y el relay dependen M03.05/06. Declarar estas capacidades globales como PASS sería incorrecto. No habilitar pedidos ni pagos reales.

## A4 — Procedencia, CI y rollback

La workflow GitHub-hosted `.github/workflows/v22-c11-03-m03-01-outbox-domain.yml` se dispara en un push a dev del código/test, compila .NET 10 Release, ejecuta los casos xUnit filtrados y exige ausencia de FAIL y SKIP. Registrar tras publicación commit SHA y run/job reales; `verify-solqaryn-project-scope` y V5 deben ser SUCCESS exact-SHA. Antes de esos checks, mantener PLAN H=VALIDANDO, no LISTO. Rollback si se requiere: `git revert` normal únicamente de los archivos de esta entrega, después de revalidar el HEAD actual y la ausencia de otro escritor; jamás reset/force-push.

## A5 pendiente

Tras A0–A4 exitosas, LISTO_DEV por V17/CAS y revisión A5 genuina de otro TASK_ID Javier distinto de DEV04. Preservar owner original, BITACORA histórica, DAG/COLA derivados y límites QA/main/PROD.
