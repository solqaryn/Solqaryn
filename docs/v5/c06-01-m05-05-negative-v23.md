# C06-01.M05.05 — V23 rechazo seguro en la landing pública (DEV)

## Alcance / A0

Microtarea PLAN_MAESTRO!A894:S894: «Ejecutar caso negativo de seguridad, aislamiento o abuso». Una única entrega dirigida de pruebas negativas contra la ruta Angular real \`/conoce-solqaryn\` y su petición pública de salud \`GET /health\`. **No se cambia código runtime, autorización del servidor, MySQL, dominios, pagos ni producción**. El resultado de fase consignado en PLAN E es acumulativo y NO se certifica mediante esta sola microtarea.

Arquitectura: Angular 22.2.1 (standalone, Signals) y Playwright/Chromium en GitHub-hosted; el backend real y los controles JWT/RBAC/tenant son autoridad aparte. El fixture local HTTP \`frontend/scripts/c0601-health-fixture.mjs\` simula únicamente las respuestas de salud para verificar manejo seguro; no sustituye API ASP.NET real, proveedor Oracle EF ni MySQL 8.4.

## A1 / A2: casos adversariales implementados

- Query manipulada \`empresaId\`, \`redirect\` y \`token\` no causa solicitudes privadas ni refleja una cadena de prueba en el DOM.
- Estado local de tenant y token falsos no debe iniciar carga privada ni adjuntar \`Authorization\`, \`X-Empresa-Id\` o \`Cookie\` a \`GET /health\`.
- \`403\`, \`429\` y \`500\` devuelven mensajes genéricos: no se revelan detalles del proveedor, direcciones internas ni trazas.
- HTTP público lento mantiene botón deshabilitado y una única petición hasta resolver.
- Chromium real registra diez capturas antes/después del estado denegado en anchos 320, 390, 768, 1280, 1440; sin overflow horizontal; teclado, \`aria-live\` y movimiento reducido conservados.

**Limitaciones explícitas:** pruebas sobre superficie pública real y *fixture de salud*, no sobre tokens JWT firmados, tenant A/B en MySQL, sucursales, propiedad de recursos, IDOR/BOLA de API privada, archivos, jobs o replay de operaciones mutantes. Una denegación visual o respuesta simulada no acredita autorización backend. P0/P1 previos y los controles comerciales siguen intactos; no liberan fase técnica ni QA/main/PROD. Para A2/A3 completo si se requiere por PLAN D o su alcance acumulativo, agregar pruebas adversariales con API .NET 10 real, JWT firmado, Oracle EF/MySQL 8.4 efímero y permisos servidor.

## A3 / A4 pendientes de evidencia causal

Workflow \`.github/workflows/v23-c06-01-m05-05-negative.yml\` en GitHub-hosted, comprobaciones exact-SHA Scope Lock/V5, Angular tipado/build, Playwright 7 casos adversariales y 10 PNG. **No registrar PASS hasta que el job termine \`completed/success\`**, con 7/7 aprobados y 0 fallos/omitidos. Registrar SHA, run, fuentes y readback en PLAN/BITACORA. \`SKIPPED\`, \`CANCELLED\`, \`IN_PROGRESS\`, \`FAILURE\` no son PASS cuando el check es exigible.

## V23 referencias y impacto

Gobierno obligatorio \`solqaryn-project-governance\` canónico contrastado con \`dev\`. Leídas de origen+pin oficiales: SKREF-03 Impeccable \`pbakaus/impeccable@2149fcce39a90bb409df5f16515f316a76dc6199\`, SKREF-05 Taste \`Leonxlnx/taste-skill@ccbc15639c97057cbfcf32ecebc38ef716e4bb37\`, SKREF-10 Angular \`angular/angular@6808966993bf2b2d455d9b6245e960a59a01b9a9\`. Aplicadas sólo pautas compatibles para pruebas en superficie pública. **NO_APLICA** SKREF-04 (sin motion nuevo), SKREF-06 (sin copy de producto nuevo), SKREF-11 (sin modelado formal de amenazas; es prueba puntual adversarial), SKREF-12 (sin perfilado .NET medido). No se instala ninguna skill ni se agrega dependencia.

Impacto de diff: tres archivos de test/config/workflow y este documento. No cambios de Domain/Application/Infrastructure/API, DTO/DB/migraciones/JWT, permisos, contratos productivos o secretos. Rollback: \`git revert\` ordinario del único commit, no reescribir historia. A5 siempre posterior e independiente por otra Task Javier distinta del autor DEV06 y de cualquier reparador. La ausencia de actualización \`CHANGELOG_AI.md\` debe declararse como limitación si GitHub no entrega su contenido completo; no sobrescribir su historia.
