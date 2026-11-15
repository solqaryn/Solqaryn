# SOLQARYN V5 FINAL — C13-04.M02.02: API, DTO, errores y compatibilidad visual

**Autor:** PRIMARY DEV 06 (`6af16bbad26881918c283abe06e1f32a`), **fecha:** 2026-11-15, **repo:** `solqaryn/Solqaryn@dev`, **PROJECT_SCOPE_LOCK=STRICT**. Este archivo es exclusivamente un **contrato de diseño**; no implementa rutas, DTO, controllers, CSS, permisos o servicios.

## Gate, fuentes y ámbito

Referencias institucionales: [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN/COLA/DAG](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). M01.01–06 LISTO + A5 independiente incluyendo M01.06 SHA `c707bb8edfba59a6a5c90b45671ffd3a2df20bf6`. PLAN fila 1989 PENDIENTE sin owner, COLA G/O SI y DAG F VALIDADO/G SI antes de claim CAS. M02.01 y M02.02 son desarrollos documentales independientes sin dependencia DAG entre ellos.

Los nombres de rutas y DTO que siguen son **hipótesis de contrato**; la implementación futura debe comprobar el backend real y la autorización antes de adoptarlos. El frontend Angular no gobierna permisos efectivos, pertenencia a tenant, factura, pagos ni miembros de empresa.

## Matriz D01–D12 de API/DTO candidatos — NO DESPLEGADOS

| ID | Método | Ruta propuesta | DTO conceptual | Requisito futuro backend | Respuestas propuestas | Límite |
| --- | --- | --- | --- | --- | --- | --- |
| D01 | GET | /public/experience | PublicExperienceDto | Anónimo | 200/404 | Sólo superficie pública visible |
| D02 | GET | /private/navigation | AllowedNavigationDto | JWT+nav.read | 200/401/403 | Lista derivada de backend, no flags cliente |
| D03 | GET | /private/tenant/theme | TenantThemeDto | JWT+tenant.read | 200/403/404 | Tema vinculado al tenant autenticado |
| D04 | GET | /private/forms/{id}/state | FormStateDto | JWT+forms.read | 200/403/404 | Recurso con propiedad persistida |
| D05 | GET | /private/dashboard/summary | DashboardSummaryDto | JWT+dashboard.read | 200/403/404 | Datos agregados sin fuga A/B |
| D06 | POST | /private/actions/{id}/validate | ActionPreflightDto | JWT+action.verify | 200/403/409 | No ejecuta mutación por preflight |
| D07 | GET | /private/alerts | AlertViewDto | JWT+alerts.read | 200/403/404 | Evidencia saneada |
| D08 | GET | /private/locale/tokens | LocalizedTokenDto | JWT+theme.read | 200/403/404 | No filtrar datos de otra sucursal |
| D09 | GET | /private/components/permissions | UiPermissionViewDto | JWT+permission.read | 200/403 | El DTO no crea grants |
| D10 | GET | /admin/ux/acceptance/{id} | UxEvidenceDto | JWT+ux.audit | 200/403/404 | Sólo referencias de CI saneadas |
| D11 | POST | /admin/ux/acceptance/request | UxTestRequestDto | JWT+ux.request | 202/403/409 | No lanza Task/CI por documento |
| D12 | GET | /public/accessibility/help | A11yHelpDto | Anónimo | 200/404 | Texto público sin contexto privado |

Estas rutas no deben declararse HTTP accesibles ni configurarse en Angular sin implementar y probar contratos. `AllowedNavigationDto` representa permisos resueltos en servidor, no una lista que pueda enviar el navegador para otorgarse acceso. El endpoint de aceptación futuro tampoco ejecuta una Task, webhook ni promoción sin autorización explícita.

## Errores estructurados ER01–ER12 y validaciones V01–V12

| Error | HTTP futuro | Código | Comportamiento seguro |
| --- | --- | --- | --- |
| ER01 | 401 | AUTHENTICATION_REQUIRED | JWT ausente o inválido |
| ER02 | 403 | MEMBERSHIP_REVOKED | Membresía inactiva |
| ER03 | 403 | RBAC_DENIED | Permiso faltante |
| ER04 | 404 | RESOURCE_NOT_FOUND | Recurso no visible o ajeno |
| ER05 | 409 | STALE_UI_CONTEXT | Estado de formulario obsoleto |
| ER06 | 409 | IDEMPOTENCY_CONFLICT | Replay incompatible |
| ER07 | 422 | TENANT_SCOPE_INVALID | Tenant/sucursal incompatible |
| ER08 | 422 | INVALID_VIEWPORT_CONTRACT | Contexto no permitido |
| ER09 | 422 | MISSING_ACCESSIBILITY_LABEL | Contrato semántico ausente |
| ER10 | 429 | REQUEST_LIMIT | Cuota backend excedida |
| ER11 | 503 | DEPENDENCY_UNAVAILABLE | Backend no disponible |
| ER12 | 412 | SOURCE_SHA_REQUIRED | SHA/test causal no acreditado |

Un futuro contrato de error devolverá `code`, mensaje seguro y correlationId opaco generado/validado en backend; nunca stack traces ni información de tenant B. Diferenciar 403/404 sin filtrar existencia.

| Validación | Control backend obligatorio |
| --- | --- |
| V01 | JWT y membresía verificados backend |
| V02 | RBAC por operación y recurso |
| V03 | Tenant/Sucursal desde autoridad persistida |
| V04 | Revocación efectiva y NULL legado denegados |
| V05 | DTO allowlist sin roles/flags autoritativos |
| V06 | Compatibilidad aditiva o versión explícita |
| V07 | IDs y referencias saneados sin PII |
| V08 | Respuesta de error no revela recursos ajenos |
| V09 | If-Match para mutaciones futuras |
| V10 | Idempotencia durable en endpoints futuros con efectos |
| V11 | Viewport/casos de foco no interpretados como permiso |
| V12 | Ningún DTO habilita QA/main/PROD |

Compatibilidad: sólo modificaciones aditivas versionadas o migración explícita de consumidores; no eliminar DTO ni cambiar semántica de error sin pruebas contractuales. No usar URL, cabeceras o body como autoridad de tenant, role o flags.

## Oráculos P01–P08 / N01–N08 FUTUROS_NO_EJECUTADOS

| Positivo | Resultado esperado futuro | Negativo | Denegación futura |
| --- | --- | --- | --- |
| P01 | Catálogo público sin datos privados | N01 | Petición anónima a navegación privada denegada |
| P02 | JWT A lee menú permitido por backend | N02 | Tenant B no hereda menú ni datos A |
| P03 | Formulario comunica error con label/foco | N03 | Error sólo cromático no aceptado |
| P04 | Acción visual consulta preflight sin mutar | N04 | UI no puede elevar permiso |
| P05 | Theme de tenant autorizado aislado | N05 | Header o DTO no selecciona tenant ajeno |
| P06 | Evidence view expone referencia saneada | N06 | JWT/correo/PII nunca retornan |
| P07 | Contrato versionado convive con consumidor antiguo | N07 | Cambio rompe cliente sin migración |
| P08 | CI exact-SHA respalda prueba futura | N08 | Skips/cancelled no equivalen PASS |

No se ejecutó HTTP/Playwright, no se midió WCAG ni se probaron endpoints reales. La fase técnica futura debe usar JWT/membresía autorizada, tenant A/B sintético, fixture, request/response, actor, SHA exacto, resultado CI, casos de revocación y rollback.

## Pruebas A0–A5, impacto y rollback

**A0:** Markdown y CHANGELOG, doce rutas y DTO conceptuales, doce errores y doce validaciones sin código. **A1:** ocho pares P/N definidos, no resultados. **A2:** JWT/RBAC/tenant A/B, limpieza de PII y permisos backend obligatorios; sin falso PASS runtime. **A3:** referencias M01.01–06 LISTO+A5, arquitectura Angular22/.NET10/MySQL8.4 intacta, compatibilidad futura. **A4:** publicación fast-forward/CAS con checks Scope Lock/V5 `completed/success` del SHA exacto o equivalencia de blobs validada, eventos institucionales y readback. **A5:** revisión de otra Task Javier, no autoprobación.

Rollback documental: revert Git normal sin force-push y evento BITACORA compensatorio; no cambiar QA/main/PROD, secretos, servicios, permisos ni scripts de DB.
