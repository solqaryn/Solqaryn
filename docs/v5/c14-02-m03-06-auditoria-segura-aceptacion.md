# SOLQARYN V17/V15 — C14-02.M03.06 auditoría y errores seguros de aceptación

**ID:** C14-02.M03.06, PLAN fila 2071. Autor PRIMARY DEV06 JAVIER, TASK_ID 6af16bbad26881918c283abe06e1f32a. Branch dev, PROJECT_SCOPE_LOCK STRICT, clasificación R2 (seguridad/JWT/RBAC/tenancy y auditoría).

## A0 — contrato de implementación
Implementación real de C1402SecurityAcceptanceStructuredAuditSink en Infrastructure .NET 10, mediante ILogger y los tipos del gate Application C1402SecurityAcceptanceAuthorizationGate. Se consume contexto observado de backend: coincidencia ordinal de recurso, actor y tenant positivo, observación tenant A/B sin cruce, SHA completo hexadecimal minúscula y UUID de correlación. El tipo de evento estructurado está fijo: 140206. Se emiten exclusivamente códigos normalizados PREFLIGHT_ONLY_NO_PERMISSION, DENIED_NO_PERMISSION, REJECTED_UNTRUSTED_AUDIT, UUID solo con contexto verificado y un indicador booleano. NO se serializan resource keys, tenant ID, actor ID, SHA, decisión cruda, JWT, claves, encabezados, texto de error, PII, DSN o secretos. Una evidencia no confiable produce evento genérico redactado; cuando ILogger no está habilitado o falla, se devuelve AUDIT_LOGGER_UNAVAILABLE, nunca certificado de auditoría durable.

Este sink no autentica ni verifica por sí solo JWT firmado, membership o permisos reales; BackendEvidenceVerified es un contrato de fuente futura, no una prueba de proveedor conectado. Aunque el preflight fuese nominal, la respuesta jamás concede acceso, descarga, job, escritura, restauración, QA ni despliegue. No se registra DI/hosted worker, migración, SQL, HTTP, flujo en vivo o proveedor externo. Rollback git revert normal con HEAD reconciliado.

## A1/A2 — pruebas reales y denegaciones
Se añaden 11 xUnit de resultado nominal no autorizante, redacción de motivos y datos arbitrarios, tenant A/B, contexto revocado, recurso o SHA falsificado, bypass/restore/rol del cliente, clave o SHA inseguro, superficie desconocida, logger deshabilitado, excepción opaca, cancelación y constructor nulo. La suite real .NET 10 Release, seguridad multiempresa y Authorization se acreditarán únicamente cuando GitHub Actions del SHA publicado estén completed/success.

## A3 — límites de integración
Se prueba compilación Infrastructure→Application y API ILogger con fake específico; NO_APLICA a MySQL 8.4, EF, migraciones, Playwright/Chromium, JWT HTTP, proveedor de auditoría persistente y PROD porque ninguna de esas superficies se modifica ni se invoca. No equivale a pentest, evidencia de operación real o bitácora durable.

## A4/A5 — cierre y trazabilidad
Un commit fast-forward/CAS sobre dev con clase, 11 xUnit, documento y sección autocontenida de CHANGELOG_AI.md. Tras publicación cambiar primero a VALIDANDO con event BITACORA, run/SHA y readback, liberar candado V17 una sola vez. No marcar LISTO hasta Backend Release, build, seguridad multiempresa, Authorization, verify-solqaryn-project-scope y V5 causal completados con SUCCESS. A5 revisión independiente solo de otra de las cinco Tasks Javier, no autor. No tocar QA/main/PROD, secretos, datos productivos ni schedules.
