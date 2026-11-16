# SOLQARYN V17 — C14-04.M03.06 auditoría estructurada segura de release readiness

Autor: PRIMARY DEV 02 JAVIER, TASK_ID 6af16bb9492c8191b8e8c7d606f11507. `solqaryn/Solqaryn@dev` y PROJECT_SCOPE_LOCK STRICT. PLAN fila 2143; candado V17 SQV17_C14_04_M03_06 id 2000002143. Riesgo R2 por datos de tenant, autoría, autorización y auditoría.

## A0 — alcance, límites y reversión
- Se agrega `V5ReleaseReadinessSafeAuditSink` en Infrastructure .NET 10 con `IV5ReleaseReadinessSafeAuditAuthority` de fuente backend confiable fresca: identidad, tenant A/B, rol, membresía, permiso, revocación, audit fence, revisión de autor independiente, sha canónico y ambiente dev.
- Recibe exclusivamente `V5ReleaseReadinessBoundedReviewWorker.Outcome` del predecesor M03.05 con vocabulario fijo de siete códigos y contador acotado 0..16. Rechaza códigos desconocidos antes de leer la autoridad.
- Con `ILogger` existente escribe solo código fijo, severidad y EventId 14040306; nunca actor, tenant, SHA, PII, excepción, token, datos de proveedor o identificadores. Una recepción de log no es A5, autorización QA/main/PROD, despliegue, restauración, auditoría durable ni grant.
- Sin DI registrada, cola externa, worker hospedado, endpoint HTTP, migración EF, transacción MySQL, temporizador ni secretos. Reversión git revert normal en dev; BITACORA histórica inmutable.

## A1–A3 — pruebas y seguridad
- Dieciséis xUnit positivos, negativos y adversariales: caso nominal, cinco códigos de error estructurados, código/contador no permitido, tenant B, revocación, rol, permisos, autor=self, audit fence, identidad, QA/PROD, SHA stale/malformado, autoridad nula/exception, logger inactivo/exception, cancelación y constructor fail-closed. Requieren CI .NET real para ser PASS.
- A2: dos tenants no pueden cruzarse ni crear autor/revisor falso; la autoridad proviene de backend, y el payload no se serializa a logger. No sustituye JWT/RBAC HTTP real.
- A3: integración real en proceso del `ILogger<T>` y contratos .NET 10; MySQL 8.4, EF, navegador Chromium, almacenamiento durable y proveedores reales NO_APLICA al diff.

## A4/A5 — evidencia requerida
- Un solo commit fast-forward/CAS con implementación, tests, documento y CHANGELOG autocontenido. Exigir .NET Release xUnit, build, seguridad multiempresa, autorización, Scope Lock y procedencia V5 en SHA exacto o descendiente con blobs idénticos comprobados. No contar queued/cancelled/skipped/fail como PASS.
- A5 obligatoria por otra Task Javier, distinta del autor y con revisión real independiente; no habilitar QA/main/PROD ni despacho de releases por esta entrega.
