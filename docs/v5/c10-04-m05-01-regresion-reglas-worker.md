# C10-04.M05.01 — Tests xUnit de reglas en worker aislado

PROJECT_SCOPE_LOCK=STRICT; repo solqaryn/Solqaryn; branch dev; Task DEV01 6adbb9fff1548191a1bd5b30d9e9984a. Riesgo R2 por tenancy, concurrencia, reintentos e idempotencia. M05.01 verifica reglas reproducibles; NO certifica restauración total M05 ni proveedores.

## A0: alcance, impacto y rollback
DAG y COLA confirmaron predecesores de M04.01–06 con revisión A5; lock exclusivo V17 fila 1574. Solo se agregan suite xUnit, workflow propio .NET 10, esta evidencia y CHANGELOG. Código existente Domain/Application/Infrastructure, API, EF, DB, JWT, UI y proveedores permanecen intactos. Rollback por git revert normal (sin force-push).

## A1: positivo, negativo, fronteras y error
Ocho nuevos Fact xUnit: treinta y dos claves distintas aceptadas en el orden original, treinta y dos duplicadas (un preflight/no-op para las restantes), treinta y tres rechazadas sin revisión, tenant B rechazado sin consumir clave A, workId externo rechazado, Request nula no bloquea revisión legítima, membresía revocada deniega ambos intentos, duplicación entre llamadas separadas evidencia falta de persistencia. Se vuelven a ejecutar las doce regresiones existentes de límites, digest distinto, actor, retry, deadline, política, cancelación y batch. Todas las salidas del worker están diseñadas como no ejecutantes.

## A2: seguridad y no-autoridad
Los tipos y la ejecución de Domain/Application/Infrastructure son reales, pero el snapshot es sintético. No se prueba JWT firmado ni RBAC/tenant de API o MySQL real. Los asserts exigen no encolar fuera del proceso, adquirir lease, restaurar datos, exponer archivo privado o conceder acceso entre empresas. Aprobación de pruebas nunca da permiso de producción o restauración.

## A3: integración pertinente
Compilación Release y xUnit de clases reales en GitHub-hosted .NET 10; logs del job exact-SHA. No hay cambio de EF, DB, controller ni provider; MySQL 8.4 real, Cloudflare R2, persistencia de idempotencia, worker distribuido y restauraciones son NO_APLICA a este diff de tests, pero siguen PENDIENTES para la fase M05 y su futuro R3.

## A4 y A5
Exigir commit FF dev, Scope Lock/V5/seguridad completed/success en SHA, 8/8 nuevos tests y 12/12 existentes con cero fallos, PLAN/COLA/DAG/BITACORA readback. CI QUEUED/IN_PROGRESS/CANCELLED/SKIPPED/FAILED nunca son PASS. Mientras CI está ejecutándose conservar VALIDANDO/owner. A5 requiere Task Javier distinta, sin autoaprobación. No QA/main/PROD, ni secretos o fórmulas modificados.
