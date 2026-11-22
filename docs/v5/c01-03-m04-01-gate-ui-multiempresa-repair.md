# V24 reparación causal C01-03.M04.01: piloto explícito UI privada

## Defecto reproducido y causa raíz
SHA origen 02b3acac60451b470046b84d0fa5df9e08aabc39, GitHub Actions run45055780568 job137310561117: dos pruebas positivas A/B no encuentran el encabezado de la nueva ruta, tres negativas pasan. SHA bb47a965796de80f32f66342bbdce788f3f14e60, run45056539891/job137312919927: 2 fallos idénticos, 3 PASS. Revisión del código observa permisoGuard.rutaHabilitada(rutaCompleta) en Angular, donde ruta= 'configuracion/dinamica-v5'. El backend CommercialTenantUiCapabilities, en modo multiempresa restringido, no anuncia 'configuracion' ni su hija bajo ningún flag, aunque la ruta API exija [Authorize] y Configuracion:Ver. Por tanto el frontend niega el ingreso correctamente: no es un error para quitar el guard.

## Solución R2 fail closed y seguridad
Extender SOLO la proyección de capacidades de UI, bajo condición triple: administrador autenticado, grant vivo Configuracion:Ver y MultiEmpresa:PrivateReadPilotEnabled=true. Permitir configuración raíz y el hijo exacto 'configuracion/dinamica-v5'. No conceder edición, privilegios backend, JWT ni acceso tenant ajeno; fuera del piloto o sin grant permanece bloqueado. Pruebas xUnit negativas para falta de grant, no administrador, piloto apagado y prohibición de Editar. La API mantiene RequierePermiso y membresía real, no se alteran endpoints, datos ni permisos persistentes.

## Causal CI
Tres workflows UI M04.01/M04.03/M04.05 ahora disparan por cambio en proyección de rutas o pruebas. GitHub-hosted exact-SHA con MySQL8.4, API .NET10, JWT real A/B y Chromium; comprobar logs positivos/negativos y artefactos. Evitar certificar con check SKIPPED/CANCELLED/IN_PROGRESS. Revisión A5 por Task distinta de autor DEV03 y reparador DEV04. Preservar PLAN I236 histórico, H236 VALIDANDO y A5 pendiente; nadie fuerza DAG ni otros scopes.

## Handoff
Si la ruta sigue bloqueada, examinar respuesta real /permisos/mis-permisos de cada usuario y grants Configuracion:Ver/EsAdministrador/PrivateReadPilotEnabled de CI, sin inventar PASS. No cambiar flags de producción. Rollback normal git revert FF/CAS sin reset/force. Scope Lock STRICT, V5, referencias SKREF backend (UI diseño sólo donde cambie).
