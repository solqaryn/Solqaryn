# C15-03.M05.01 — Matriz xUnit de tenancy y autoridad

Rama dev, PROJECT_SCOPE_LOCK=STRICT, PLAN_MAESTRO fila 2258. Autor DEV01 (6adbb9fff1548191a1bd5b30d9e9984a). Riesgo R2 por authorization, tenant y propiedad.

## A0 — alcance e impacto

Solo se agregan un test xUnit, workflow dirigido .NET 10, documento y sección autocontenida de CHANGELOG. Se ejercitan las implementaciones reales existentes V5TenancySecurityAuthorizationGate (Application) y V5TenancySecurityDomainRules (Domain) sin modificar API/DTO, datos, EF, seguridad runtime ni servicios externos. Se comprueban cinco superficies BusinessRead, BranchRead, FileDownload, BackgroundJob y ControlPlane.

## A1 — criterios falsables

Seis Fact con más de cien verificaciones contra código real: snapshot positivo nunca concede capacidad, matriz tenant/actor/recurso/parent/ownership A-B, revocación y membresía, sucursal ajena, archivo revocado o expirado, job sin clave o grant, control-plane sin grant, filtros client-side forzados, SHA malformado y cancelación. El lector falso contabiliza llamadas para comprobar que solicitudes rechazadas no consultan evidencia. Ninguna expectativa se presume PASS antes del log real GitHub Actions del SHA.

## A2 y A3 — seguridad e integración pertinente

A2: es prueba de reglas reales con **evidencia sintética**; no se comprueban JWT firmado, endpoint HTTP, rol backend completo, EF query filters sobre MySQL, proveedores externos ni enforcement de datos A/B en producción. Una decisión positiva de preflight requiere siempre un adaptador autenticado adicional; no equivale a autorización de API. A3: ejecutable .NET 10 real, dotnet restore/build Release y xUnit dirigido en GitHub-hosted; MySQL/Angular/Chromium/Cloudinary/SMTP no aplican a este diff test-only.

## A4, A5 y rollback

Al publicar, verificar SHA exacto, seis pruebas xUnit completed/success, Scope Lock y V5, logs, resultado H/I, BITACORA y ausencia de P0/P1. Si CI queda queued/in_progress conservar VALIDANDO y RUN/JOB, liberar namedRange después de readback y continuar otra independiente. A5 por Task Javier diferente de autor DEV01, nunca autoaprobación. Rollback git revert normal sobre dev; sin QA/main/PROD, secretos, datos productivos, branches adicionales o force-push.
