# SOLQARYN V20 — C15-03.M05.02 contrato API/DTO de sesión privada

PROJECT_SCOPE_LOCK=STRICT; repo solqaryn/Solqaryn@dev; PLAN_MAESTRO fila 2259. OWNER DEV01, clasificación R2 autorización y tenancy. Incremento **exclusivamente de pruebas, workflow y documentos**, sin runtime, JWT, DB ni API modificadas.

## A0 requisitos e impacto

Verificar controller existente `PlataformaSesionController.ObtenerActual`, DTO público `PlataformaSesionDto`, ruta `plataforma/sesion`, atributos Authorize/ApiController/HttpGet/ResponseCache NoStore y ausencia de tenant ID o passwordHash en respuesta. Un usuario con rol global persistido obtiene payload con solo autoridad de plataforma y audita sin EmpresaId; administrador exclusivamente empresarial o identidad no autenticada se rechaza; revocación debe aplicar a la petición siguiente y generar auditoría negativa, sin reflejar encabezados X-Empresa-Id/X-Role.

## A1–A3 causales y límites

Seis hechos xUnit reales contra Controller, `PlataformaScopeService` y EF InMemory, con asserts positivos, negativos, revocación, wire DTO, caché y auditoría. Se inspeccionan valores JSON y propiedades exactas, con fixtures independientes. Se reutilizan las clases de backend auténticas. La identidad ClaimsIdentity de prueba **no está firmada como JWT**, no hay HTTP TestHost/JwtBearer middleware ni MySQL 8.4, por lo que esta suite **NO certifica** JWT real, BOLA/IDOR HTTP, propagación a tenancies A/B, persistencia SQL, rol financiero, facturación o producción; estas son evidencias pendientes para aceptación global R2. A3 valida integración unitario-componente EF InMemory; la integración MySQL real no se presenta como realizada.

## A4/A5/rollback

GitHub Actions sobre SHA exacto ejecuta .NET 10 restore, build Release y seis xUnit. Requiere Scope Lock, V5, prueba dirigida y checks pertinentes SUCCESS, logs causales, readback PLAN/COLA/BITACORA. Al publicar y con CI externa QUEUED/IN_PROGRESS, conservar VALIDANDO, owner histórico DEV01, liberar candado V17 tras readback. LISTO exige completar A0–A4 reales; A5 por Task Javier distinta de DEV01. Rollback git revert normal, sin force-push, QA/main/PROD, secretos ni datos live.
