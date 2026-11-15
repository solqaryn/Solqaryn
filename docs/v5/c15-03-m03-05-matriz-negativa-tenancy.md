# SOLQARYN V17/V15 — C15-03.M03.05: matriz negativa de seguridad y tenancy

**Unidad:** C15-03.M03.05; **fila:** PLAN_MAESTRO 2250; **rama:** solqaryn/Solqaryn@dev; **PROJECT_SCOPE_LOCK:** STRICT; **riesgo:** R2 por seguridad, JWT/RBAC y tenant. Autor de implementación: PRIMARY DEV 01 JAVIER (6adbb9fff1548191a1bd5b30d9e9984a). A5 únicamente mediante otra Task Javier distinta. Candado V17 de fila: SQV17_C15_03_M03_05 / 2000002250.

## Cambio real

La nueva suite ejecutable .NET 10 xUnit en backend/tests/Solqaryn.Tests/Infrastructure/V5TenancySecurityCrossLayerNegativeMatrixTests.cs compone reglas Domain C15-03.M03.01, servicio Application C15-03.M03.02, gate C15-03.M03.03 y adaptador aislado Infrastructure C15-03.M03.04. Incluye 14 métodos xUnit de regresión con varias aserciones por caso y matrices negativas. No registra endpoints, servicios en DI, proveedores ni permisos.

Cobertura: tenant A/B y tenant padre, sucursal ajena, actor/recurso cruzados, ownership persistido ausente, SHA obsoleto, membresía/rol/permiso revocados, fuente backend no confiable, archivo caducado/revocado, job sin grant auditado ni clave idempotente segura, control plane sin autoridad, enumeración, bypass de query filters, mutación institucional, superficie desconocida y cancelación. Los casos nominales deben devolver **solo prevalidación no autorizante**, sin concesión de lectura, descarga, job, administración o escritura. El servicio Application demuestra derivación de tenant desde fuente backend y rechaza bypass sin ejecutar lectores de autoridad/recursos.

## Puertas A0–A4 y límites

**A0:** diff con una suite xUnit nueva, este documento y una sección autocontenida de CHANGELOG_AI.md. Sin cambios de EF, SQL, migraciones, JWT middleware, API/HTTP, Angular, MySQL, permisos persistidos o despliegue.

**A1:** ejecutar realmente .NET 10/Release xUnit en GitHub Actions y exigir 0 fallos. Los asserts deben detectar aceptación incorrecta en positivos/negativos, frontera y errores; la presencia de test sin ejecución NO es PASS.

**A2:** controles pertinentes de seguridad multiempresa y Authorization completed/success del SHA publicado, además de comprobaciones internas de revocación, A/B, roles y bypass. Los lectores fixture NO prueban autenticación HTTP ni JWT firmado.

**A3:** integración de código **en proceso** Domain → Application → Infrastructure probada por la suite; NO_APLICA MySQL 8.4 efímero, EF, Playwright, HTTP y proveedores externos por no modificar ni invocar dichas fronteras. Esta exención no certifica la plataforma global ni el guard de queries productivo.

**A4:** exigir verify-solqaryn-project-scope, procedencia V5, Backend Release y seguridad/autorización required completed/success por exact-SHA o descendiente con hashes y dependencias equivalentes; readback PLAN, COLA/DAG y evento BITACORA idempotente. Si queda CI externa pendiente, mantener VALIDANDO con dueño, RUN y SHA y sin fingir LISTO.

**A5:** revisión técnica genuina por distinta Task Javier, con derecho a rechazo y evidencia del cambio, pruebas y blobs.

## Riesgo residual y rollback

Esta matriz usa fuentes in-memory y no demuestra filtros SQL reales, sesión JWT de HTTP, aislamiento real en MySQL 8.4, descarga efectiva, despacho de jobs ni control-plane productivo. El riesgo documentado de TenantQueryBypassGuard no se modifica con esta microtarea y exige validación separada contra MySQL aislado. No ejecutar datos reales, QA, main ni PROD. Rollback por revert Git ordinario y compensación institucional idempotente cuando proceda, sin force-push ni borrar BITACORA.
