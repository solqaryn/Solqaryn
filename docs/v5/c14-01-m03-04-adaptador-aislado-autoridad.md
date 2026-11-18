# C14-01.M03.04 — Adaptador aislado de autoridad confiable (V19)

**Ámbito:** C14-01.M03.04, capa Application, riesgo R2. Implementación de la alternativa **adaptador aislado** de la microtarea; no ejecuta migraciones, no modifica tablas ni registra DI de producción.

## A0 — Alcance, interfaces y riesgo
El contrato de lector `IC1401FunctionalAcceptanceAuthorityReader` existe en Application. El nuevo adaptador `C1401IsolatedFunctionalAcceptanceAuthorityAdapter` sólo acepta un proveedor explícito `IC1401TrustedAcceptanceSnapshotSource` de instantáneas verificadas del servidor. No confía en actor, tenant, permisos o catálogos enviados por el cliente. No crea endpoints, sesiones, escrituras, workers ni acceso productivo. La fuente concreta persistida/HTTP está deliberadamente fuera de esta entrega; requiere nueva microtarea con su propia A3.

## A1/A2 — Pruebas causales
Pruebas xUnit .NET 10 de preflight positivo **sin concesión de autoridad**; claves y acciones con comparación ordinal pese a un conjunto case-insensitive; copia defensiva del catálogo mutable; rechazo tenant B, sesión revocada, JWT/membresía/RBAC/propiedad inválidos, SHA o recurso incorrectos, catálogo ausente, errores del proveedor, rutas inválidas y cancelación. Una prueba que use un proveedor simulado **no certifica MySQL, JWT firmado ni autorización HTTP**. En el fallo del proveedor el adaptador retorna null y el gate existente deniega.

## A3 — Integración pertinente
Se valida composición **real en .NET Release** con `C1401FunctionalAcceptanceAuthorizationGate`, sin alterar DI ni persistencia. MySQL 8.4/EF/migraciones, servicio HTTP, navegador y proveedor externo: **NO_APLICA para el código cambiado**; su integración futura requerirá CI aislada con MySQL/HTTP real antes de cualquier certificación de esos límites.

## A4/A5 — Publicación y revisión
Exigir CI xUnit Release, seguridad multiempresa, Scope Lock exact-SHA, procedencia V5 y readback del estado/owner y BITACORA. No aceptar queued/in_progress/skipped/cancelled como PASS. A5 sólo por Task Javier distinta de DEV04, sin autopase; asegurar P0/P1=0. CHANGELOG aditivo, commit fast-forward a dev. Rollback mediante `git revert` normal tras revalidar HEAD. No QA/main/PROD ni secretos ni datos productivos.
