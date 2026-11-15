# SOLQARYN V5 — C03-02.M02.02: contratos API, DTO, errores y compatibilidad RBAC/ABAC

**Autor/owner:** PRIMARY DEV 06 JAVIER `6af16bbad26881918c283abe06e1f32a`. **Fecha:** 2026-11-15. **Ámbito:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT. **Fuentes:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, PLAN_MAESTRO/COLA/DAG_TECNICO, Skill canónica y los seis contratos preparatorios C03-02.M01.01–06.

**Entregable documental exclusivamente.** Este texto propone interfaces y errores; no crea ni modifica controllers, rutas HTTP, DTO compilados, tests, JWT, grants, tablas, políticas, colas, webhooks, workers ni datos. No certifica seguridad live ni confiere autorización a QA/main/PROD. Todo test aquí descrito es FUTUROS_NO_EJECUTADOS.

## 1. Preflight de elegibilidad y alcance

Antes del claim se leyó `PLAN_MAESTRO!H477=PENDIENTE`, `I477` vacío; `COLA!G477=SI/O477=SI` y `DAG_TECNICO!F448=VALIDADO/G448=SI`. La dependencia explícita es `C03-02.M01.01–06+A5`; el conjunto estaba LISTO con revisiones A5 independientes, incluida la terminal M01.06. Se registró owner único por CAS en PLAN, seguido de readback. El desarrollo paralelo de C03-02.M02.01 es un contrato relacionado, **no prerequisito obligatorio del DAG de esta unidad**; no se trata su A5 pendiente como un pase.

La arquitectura vigente se conserva: Domain ← Application ← Infrastructure, API Web ASP.NET Core 10, MySQL 8.4 con EF Core 10.0.12 y proveedor Oracle MySql.EntityFrameworkCore 10.0.9, Angular 22.2.1 solo experiencia/guards, JWT firmado, RBAC relacional, tenant y sucursal verificados en servidor. Los puntos de extensión del código existente se revalidarán en M03 antes de tocar rutas.

## 2. Superficies de API propuestas, NO IMPLEMENTADAS

| Contrato | Entrada futura propuesta | Resultado y control servidor |
| --- | --- | --- |
| API01 EvaluatePermission | módulo, acción, resourceRef opaca | Veredicto interno solo tras JWT, membresía, grant, propiedad y condición ABAC; nunca devuelve roles amplios |
| API02 GetOwnResource | resourceRef, versión opcional | Un recurso propio o 403/404 seguro; no revela existencia ajena |
| API03 UpdateOwnResource | resourceRef, versión esperada y comando allowlisted | Control de versión, grant de escritura y transacción; nunca cross-tenant |
| API04 GetOwnBranchResources | cursor opaco, filtro de sucursal autorizado | Paginación filtrada desde servidor, máximo acotado y orden estable |
| API05 RequestDataExport | alcance permitido, formato allowlisted | Job futuro idempotente solo si grant específico y filtro por fila/campo validado |
| API06 ReadExportStatus | solicitud opaca con actor/empresa | Estado seguro; nunca URL ni conteos ajenos |
| API07 RequestMediaAccess | resourceRef, propósito, caducidad corta | URL temporal emitida solo después de revalidar ownership; no devolver secreto del proveedor |
| API08 GetAccessAudit | paginación y tiempo UTC acotado | Registros redactados, permiso explícito, sin PII/secretos de otro tenant |
| API09 RevokeEffectiveGrant | membership/grant y versión autorizados | Revocación transaccional y auditoría; nunca override cliente |
| API10 SupportReadScoped | empresa, módulo, resourceRef y motivo aprobados | GET solamente, allowlist, expiración, auditoría y cero mutación |
| API11 RotateTenantContext | empresa destino solicitada | Resolver membresía y grants nuevos en servidor; invalidar contexto previo |
| API12 GetCurrentCapabilities | módulo y versión | Resumen mínimo de capacidades efectivas, sin convertir entitlement SaaS en RBAC |

Estos son **nombres de operaciones conceptuales**, no endpoints existentes ni URI comprometidas. Los contratos HTTP, verbos, versionado y consumidores específicos se determinarán en implementación M03 después de comparar el repositorio vivo. Una ruta de lectura del cliente no debe convertirse en endpoint de permisos globales ni conceder grants por ver una tarjeta de UI.

## 3. DTO propuestos y validación de fronteras

| DTO conceptual | Campos mínimos | Validación de entrada / salida |
| --- | --- | --- |
| D01 EffectiveAccessContext | actorRef opaco, tenantRef servidor, branchRef autorizado, grantVersion | Solo se genera en backend, nunca desde `EmpresaId` del cuerpo |
| D02 ResourceReference | opaqueId, expectedVersion, purpose | Formato/longitud allowlist; buscar recurso siempre filtrado por tenant |
| D03 ActionRequest | module, action, resourceRef, clientRequestId | Module/action validados; clientRequestId correlación, no autoridad |
| D04 ScopedPageRequest | cursor firmado/opaco, tamaño acotado, branchRef | Cursor ligado al tenant; no aceptar offsets de otro contexto |
| D05 DataExportRequest | format enum, filters tipados, purpose | Prohibir selección SQL libre, columnas sensibles y scope global |
| D06 MediaAccessRequest | resourceRef, purpose, ttlSeconds acotado | Ninguna ruta física, URL arbitraria o clave de proveedor |
| D07 PermissionDecision | allow/deny interno, policyVersion, reasonCode | No exportar al navegador grants internos o reglas sensibles |
| D08 SafeApiError | safeCode, correlationId, retryable flag, utc | Sin PII, claims, stack, descripción de recurso ajeno o secretos |

**Precedencia fail-closed:** identidad verificada → membresía activa → ámbito de sucursal → RolPermiso/grant → condición ABAC de recurso → versión/estado → operación. Los campos del cliente solo especifican una petición; no suministran la decisión. Un plan SaaS no genera permiso RBAC. Para una revocación se debe invalidar la autorización posterior aun con JWT previo todavía vigente.

## 4. Catálogo conceptual de 14 errores seguros

| Código propuesto | Condición y respuesta futura |
| --- | --- |
| ERR01 UNAUTHENTICATED | 401 sin revelar si el recurso existe |
| ERR02 INVALID_JWT | 401 con respuesta genérica, sin token ni claims |
| ERR03 MEMBERSHIP_MISSING | 403/404 seguro, sin empresa ajena |
| ERR04 MEMBERSHIP_REVOKED | 403/404 seguro y política de invalidación |
| ERR05 GRANT_MISSING | 403 sin fallback por plan o rol legacy |
| ERR06 TENANT_MISMATCH | 403/404 sin confirmación de otro tenant |
| ERR07 BRANCH_MISMATCH | 403/404 para sucursal no autorizada |
| ERR08 RESOURCE_NOT_FOUND_OR_DENIED | 404 uniforme sin filtración de existencia |
| ERR09 VERSION_CONFLICT | 409 con versión opaca segura |
| ERR10 INVALID_CURSOR | 400 sin reutilizar paginación de otro tenant |
| ERR11 EXPORT_SCOPE_DENIED | 403 sin producir archivo o filas |
| ERR12 MEDIA_ACCESS_DENIED | 403 sin URL ni ruta de proveedor |
| ERR13 SUPPORT_SCOPE_DENIED | 403/405 según contrato, sin mutación |
| ERR14 DEPENDENCY_OR_AUDIT_FAILURE | 503/409 según operación; DENY y reversión transaccional |

Los códigos son un contrato propuesto sujeto a compatibilidad con la API actual. La elección precisa de 403 frente a 404 se documentará para no filtrar existencia, junto con semántica actual de `ApiResponse`, rate limiting, CORS explícito y el manejo de errores. No afirmar que exista hoy un handler por cada nombre anterior.

## 5. Dieciséis invariantes transversales

1. Una sesión JWT legítima no sustituye membresía ni grant vigente.
2. `X-Empresa-Id`, slug, body, ruta, claim manipulado o cliente Angular no confieren tenant efectivo.
3. Cada recurso y objeto de medios se filtra por propietario empresarial y sucursal antes de proyectarlo.
4. Un permiso `read` no implica `export`, `upload`, `delete` ni `admin`.
5. Entitlement, catálogo y feature flag solo restringen, nunca elevan permisos.
6. El cursor queda ligado a actor, tenant, filtro y caducidad para impedir paginación cruzada.
7. Las exportaciones aplican filtros de fila/campo antes de crear CSV, PDF o XLSX.
8. Las URLs firmadas se emiten tras revalidar grant y caducan; no filtran claves.
9. Soporte es GET allowlisted por empresa, recurso y tiempo, nunca administrador global.
10. La revocación se verifica en cada operación protegida; un JWT antiguo no restituye permiso.
11. Un error 403/404 no devuelve ID, nombre, conteo o dato de un tenant ajeno.
12. Correlación y auditoría ocultan JWT, PII, secretos, hashes sensibles y paths internos.
13. Un fallo DB/EF, timeout o auditoría deniega y no publica mutación parcial.
14. Reintentos de una operación de escritura futura son idempotentes por tenant/actor/acción.
15. La UI no implementa ACL sustituta de controles del backend.
16. El cierre de M02 requiere A0–A4 causales, A5 de otra Task y nuevo DAG; nunca autoriza QA/main/PROD.

## 6. Casos positivos y negativos de aceptación FUTUROS_NO_EJECUTADOS

| Caso | Positivo futuro | Negativo que debe fallar |
| --- | --- | --- |
| P01/N01 | A autorizado lee recurso A | JWT A solicita recurso B por ID (BOLA/IDOR) |
| P02/N02 | Permiso A de sucursal A1 escribe recurso A1 con versión válida | A1 intenta recurso de sucursal A2 |
| P03/N03 | Grant de lectura retorna metadatos mínimos | Grant de lectura intenta DELETE/EXPORT |
| P04/N04 | Paginación de A retorna solo filas A | Cursor de B reutilizado desde A |
| P05/N05 | Export A con grant específico retorna campos allowlisted | Export A contiene filas o PII B |
| P06/N06 | Media A autorizado obtiene URL corta | Usuario B obtiene URL a medio A |
| P07/N07 | Soporte con alcance GET lee objeto permitido | Soporte hace PUT/DELETE o cambia empresa |
| P08/N08 | Revocación corta permiso en segunda solicitud | JWT vigente omite consultar revocación |
| P09/N09 | Refresh de contexto A→B revalida membresía B | Guard Angular conserva datos/roles A |
| P10/N10 | PolicyVersion conocida permite un permiso explícito | Política desconocida se vuelve ALLOW por defecto |
| P11/N11 | Error opaco informa código y correlación | Error expone JWT, stack o existencia B |
| P12/N12 | Check CI exact-SHA terminado con éxito es acreditable | CI `cancelled`, `skipped` o `in_progress` contada PASS |

Ninguno de los 12 pares se ejecutó contra HTTP, JWT, MySQL, Playwright o datos reales. La implementación M03 requerirá código real .NET y suites positivas/negativas de permisos, recuperación de errores y aislación tenant A/B, con integración MySQL 8.4 efímera cuando exista persistencia.

## 7. Riesgos, evidencia, A0–A5 y rollback

Los riesgos causales que deben permanecer visibles son BOLA/IDOR, claims alterados, roles legacy con grants implícitos, privilegios de soporte excesivos, cursor de otro tenant, exportación cruzada, URL persistente, revocación tardía, caché global de decisiones y logs con PII. Ningún archivo documental certifica la remediación de estos riesgos en runtime.

**A0:** comprobar diff de dos Markdown (documento actual y sección CHANGELOG autocontenida), sin código, secretos, dependencias ni configuración. **A1:** verificar doce operaciones conceptuales, ocho DTO, catorce errores, dieciséis invariantes y doce pares positivos/negativos FUTUROS_NO_EJECUTADOS. **A2:** verificar diseño JWT/RBAC/ABAC/tenant A/B/sucursal/propiedad/medios, fail-closed. **A3:** verificar M01.01–06 LISTO+A5, el modelo M02.01 como referencia no obligatoria y arquitectura; compilación .NET/Angular, HTTP/MySQL/E2E = NO_APLICA a diff Markdown, sin atribuir PASS. **A4:** publicar fast-forward/CAS al HEAD exacto, verificar blob/diff, Scope Lock y V5 provenance `completed/success` sobre SHA publicado, PLAN/COLA/DAG/BITACORA y readback. **A5:** pendiente de revisión técnica por otra Task Javier diferente a DEV06 antes de sucesoras que la requieran.

**Rollback:** `git revert` no destructivo del commit documental tras revalidación HEAD, evento compensatorio aditivo, conservación de autores y estados históricos. No se ejecutan DB, permisos, eventos, webhooks, planes de pago, QA/main/PROD, DNS ni cambios externos en esta microtarea.
