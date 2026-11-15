# SOLQARYN V5 — C15-04.M02.02: contratos API, DTO, errores y compatibilidad de auditoría UX

**Autor:** PRIMARY DEV 02 — JAVIER_CHATGPT, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Fecha:** 2026-11-14. **Repositorio:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT. **Clase:** diseño documental de contratos; sin ruta o endpoint nuevo, sin DTO compilado ni autorización de despliegue.

## 1. Autoridad y preflight

Fuente ejecutable única: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive ID `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. Al reclamar `C15-04.M02.02`, `PLAN_MAESTRO!H2277=PENDIENTE`/I vacío, `COLA!G/O2277=SI`, `DAG_TECNICO!A2238=C15-04.M02.02` F=VALIDADO/G=SI. M01.01–06 estaban LISTO con A5 independiente, y el contrato M02.01 estaba LISTO_DEV por esta misma Task (A5 propio pendiente). DAG permite M02.02 por los seis predecesores M01/A5, no se presume aprobación independiente de M02.01.

Las fuentes de código leídas en el HEAD vigente se refieren a:
- `frontend/src/app/app.routes.ts` (blob `2f560c14693455b79ac4546e9333fb74586ef234`): rutas públicas storefront y privadas dashboard, estas últimas con `authGuard`, `permisoGuard`.
- `frontend/src/app/core/guards/auth.guard.ts` (blob `b652a7af6343bdae83a4dc9f3065d9fc1aa120f6`): exige que el contexto tenant sea verificado por backend, no por localStorage.
- `frontend/src/app/core/guards/permiso.guard.ts` (blob `1052bd38041055cc81e84a14d5fe0364e169b1fe`): consulta permisos runtime y evita interpretar como autorización una ruta distinta.
- `backend/src/API/Controllers/DashboardController.cs` (blob `07b2a8a906a5c08c5d8cccfda057f38e456b5304`): clase `[ApiController]`, `[Authorize]`, ruta real `dashboard`, `[HttpGet("resumen")]`, `RequierePermiso`, retorno `ApiResponse<DashboardResumenDto>.Ok`.
- `backend/src/API/Controllers/DashboardKpiConfiguracionController.cs` (blob `e8483f0798bea20baa6097ddc815e7a17c701b10`): API existente `dashboard/kpis/configuracion`, autorización y verificación `ResolverScopeAsync` antes de leer o escribir configuración.
- Contratos documentales M01.01–06 de UX y M02.01 `UX01–UX12`, `INV01–INV16`, `AT01–AT10`. Sus resultados de navegador, accesibilidad y rendimiento siguen NO_EJECUTADOS.

**Regla de compatibilidad:** ninguna especificación aquí se presenta como endpoint implementado. Nombres `Audit*Contract` y formatos de abajo son propuestas de dominio de evidencia a revisar antes de desarrollar M03; no existen necesariamente en el backend.

## 2. Operaciones conceptuales, entradas y salidas

| ID | Operación propuesta (NO_IMPLEMENTADA) | Entrada mínima | Salida contractual | Autorización |
| --- | --- | --- | --- | --- |
| OP01 | Enumerar superficies auditables | módulo, entorno dev, tenant autorizado | refs de rutas, visibilidad, capacidad real vs futura | solo rol autorizado; públicas sin exponer PII |
| OP02 | Crear observación UX | scope ID, SHA, ruta, viewport, estado UI | ID lógico, procedencia, resultado `NO_EJECUTADO` por defecto | autor del scope y tenant verificado |
| OP03 | Adjuntar evidencia accesibilidad | observación, criterio, método, captura depurada | referencia no sensible, dispositivo, modo teclado/lector | no permitir acceso de tenant cruzado |
| OP04 | Adjuntar medición rendimiento | observación, métrica, baseline, red y muestra | valor con unidades y fixture o `NO_MEDIDO` | solo auditor y repositorio de pruebas permitidos |
| OP05 | Reportar hallazgo | criterio, severidad provisional, pasos de reproducción | riesgo, evidencia, estado, clasificación no automática | sin certificación P0/P1 sin reproducción |
| OP06 | Registrar resultado de mitigación | hallazgo, SHA corrección, pruebas dirigidas | transición con fecha, autor, revisor y diff | mismo owner CAS para escribir código del scope |
| OP07 | Revisar A5 independiente | microtarea, autor, SHA, artefacto, checks reales | A5_PASS o A5_FAIL con causalidad | Task Javier distinta del autor |
| OP08 | Consultar trazabilidad | scope, fechas, clave idempotente | PLAN/COLA/DAG/BITACORA readback | lectura autorizada, sin exponer secretos |

Los nombres OP01–OP08 no son verbos HTTP ni rutas existentes; M02.02 **no define** un nuevo recurso HTTP productivo. Para desplazar estos contratos a .NET, una microtarea M03 debe verificar rutas, DTO, autorización server-side, validación, pruebas MySQL y compatibilidad con `ApiResponse` real.

## 3. DTO conceptuales versionables

| Contrato propuesto | Campos mínimos | Reglas |
| --- | --- | --- |
| `AuditScopeContractV1` | `scopeId`, `planItemId`, `commitSha`, `environment`, `ownerTaskId` | `dev` hasta nueva autorización; IDs conocidos; SHA exacto |
| `SurfaceObservationContractV1` | `scopeId`, `surfaceKey`, `visibility`, `viewportPx`, `uiState`, `actorContext` | viewport entero positivo; visibilidad pública/privada explícita |
| `AccessibilityResultContractV1` | `observationId`, `criterionRef`, `method`, `resultStatus`, `artifactRef` | `NOT_RUN` inicial; `PASS` solo con prueba completa y evidencia |
| `PerformanceResultContractV1` | `observationId`, `metric`, `unit`, `value?`, `networkProfile`, `baselineRef`, `sampleCount` | valor nulo si no medido; no inferir objetivos sin baseline |
| `FindingContractV1` | `findingId`, `severityProposed`, `riskRef`, `reproSteps`, `proofRef` | P0/P1 solo tras reproducción y clasificación autorizada |
| `ApprovalContractV1` | `scopeId`, `authorTaskId`, `reviewerTaskId`, `sourceSha`, `gate`, `result` | autor != revisor para A5, `sourceSha` obligatorio |
| `FailureContractV1` | `category`, `publicMessage`, `correlationId`, `retryAllowed` | no incluir stack traces, JWT, email, tenant ajeno o secretos |

**Versionado propuesto:** sufijo V1 solo para este modelo de documento; su incorporación a API requerirá evaluación de contratos actuales y semántica de compatibilidad. No registrar `tenantId` enviado por el navegador como identidad autoritativa; la API deriva el tenant de JWT/claims verificados y controles de membresía vigentes.

## 4. Validaciones y semántica de error propuesta

| Código de contrato | Caso | Comportamiento esperado futuro | Negativo a comprobar |
| --- | --- | --- | --- |
| ERR01 | Falta autenticación para superficie privada | Rechazo 401 si aplica | Mostrar dato privado en respuesta anónima |
| ERR02 | Usuario autenticado sin RBAC | Denegación 403 si aplica | UI concede privilegio porque oculta/expone botones |
| ERR03 | Recurso de otro tenant | Fallo cerrado conforme política API existente | Filtración de existencia, dato o metadata del tenant B |
| ERR04 | Observación inválida | Rechazo 400 con descripción segura y nombres de campo | Error crudo del servidor, PII o stack trace |
| ERR05 | SHA/ID fuera del plan | Rechazo o conflicto conforme control vigente | Aceptar resultados de otro commit o backlog histórico |
| ERR06 | CAS perdido por concurrencia | Conflicto y readback del ganador | Doble owner o sobreescritura de BITACORA |
| ERR07 | Check en progreso/skipped/cancelled | Mantener VALIDANDO y estado de prueba explícito | Reportar éxito requerido |
| ERR08 | Inconsistencia de medición | `NO_MEDIDO`/invalidación con trazabilidad | Publicar valor o sello de conformidad inventado |
| ERR09 | Revisión A5 propia | Rechazo de revisión por independencia | Autor firma A5 como revisor |
| ERR10 | Límite del proveedor o timeout | Reporte causal sin prometer corrida perpetua | Retry infinito o duplicación de eventos |

Los códigos ERR01–ERR10 son identificadores internos de **esta especificación**, no códigos expuestos actualmente por `DashboardController`. Los HTTP 401/403/400 son expectativas normales de seguridad a validar contra acciones concretas y compatibilidad real, no cambios a middleware. Los fallos deben conservar lenguaje humanamente comprensible, estados aria-live cuando proceda y no filtrar credenciales. Para errores de red, el frontend describe fallo y reintento sin convertir el fallo en resultado `PASS`.

## 5. Matriz de compatibilidad y pruebas futuras

| Superficie / cambio propuesto | Compatibilidad que debe verificarse | Prueba positiva futura | Negativa futura |
| --- | --- | --- | --- |
| Storefront público | No exigir login ERP para contenido verdaderamente público | Visitante consulta catálogo permitido | Recurso privado o sesión de otro tenant visible |
| `dashboard` existente | `[Authorize]` + `RequierePermiso` se preservan | Actor habilitado recibe resumen esperado | Sin JWT/permiso no accede a datos |
| `dashboard/kpis/configuracion` | Resolver scope servidor antes de operaciones | Lectura/escritura autorizada permanece compatible | Cliente cambia tenant en body sin permiso |
| `authGuard` Angular | Contexto tenant revalidado por backend | Recarga reconstituye sesión autorizada | localStorage se acepta como autoridad |
| `permisoGuard` Angular | ruta y acción se verifican sin asumir permiso API | Módulo visible según RBAC | Ruta homónima o modulo forzado concede privilegio |
| Contratos `Audit*V1` futuros | No alterar rutas/DTO existentes sin versionar | Consumidor actual permanece funcional | Se reemplaza un schema de API sin migración |
| Estados a11y | Error, loading y foco con textos y semántica | Teclado y lector anuncian recuperación | Color o icono aislado sin nombre |
| Medidas Web | Muestra, baseline, red y SHA | Medición repetible con fixture aprobado | Porcentaje/LCP sin fuente se presenta real |

**Ejecución actual:** esta revisión es estática/documental. Ni Playwright/axe, ni HTTP negativos, ni MySQL ni lecturas de producción fueron ejecutados por M02.02; son casos de prueba futuros. CI compilación de backend/front no es causal a este Markdown y se clasifica `NO_APLICA_DOCUMENTAL`; si está in_progress nunca se nombra PASS.

## 6. Impacto y rollback

- **Componentes tocados:** solo el documento del contrato M02.02 y una nueva sección indivisible de `CHANGELOG_AI.md`. Ningún archivo `.cs`, `.ts`, `.sql`, secreto, endpoint, ruta ni manifest se modifica.
- **Capas y dependencias:** Angular y .NET/MySQL siguen arquitectura canónica; backend autoriza y frontend representa estados. No se crean interfaces API reales, migraciones, tablas o flags. M02.03/M02.04/M02.05/M02.06 tienen sus propios gates de DAG y A5.
- **Pruebas y seguridad:** A0 consistencia de OP01–08, DTO propuestos y ERR01–10; A1 oráculos futuros positivos/negativos y tratamiento de error; A2 revisión de JWT/RBAC y aislamiento conceptual; A3 trazabilidad con los seis antecedentes y código leído, sin runtime; A4 SHA exacto, Scope Lock y procedencia CHANGELOG SUCCESS y readback institucional. A5 independiente pendiente.
- **Rollback:** revert Git ordinario del changeset documental, reparación aditiva de BITACORA y readback; no sobrescribir commits de otros autores, no force-push/reset, no tocar QA/main/PROD ni servicios externos.

**Resultado acotado:** diseño verificable de contratos API/DTO, validaciones, errores y compatibilidad para auditoría UX; no se certifica implementación de los contratos ni calidad empírica UX o de seguridad.
