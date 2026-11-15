# SOLQARYN V5 — C05-01.M02.02: contratos API, DTO, errores y compatibilidad del sistema de diseño

**Autor:** PRIMARY DEV 03 — JAVIER, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (`1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`). **Repositorio:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Riesgo:** R0 documental, 2026-11-15.

## 1. Objeto, dependencias y frontera real

La microtarea `C05-01.M02.02` pide «Definir API, DTO, errores, validaciones y compatibilidad» para tokens de diseño, componentes, tipografía, color, espaciado, estados y personalización segura. En la reclamación dirigida PLAN H729=PENDIENTE e I729 vacía; COLA G729/O729=SI y DAG A690 F=VALIDADO/G=SI; los seis antecedentes M01 de C05-01 están LISTO con A5 independiente. La entrega contigua `C05-01.M02.01`, commit `1c5418f153537bf504924f6cd734978b25f1a6e9`, define DS01–DS14 e INV01–INV18 como **modelo conceptual**, no como tabla o servicio ya instalado. M02.02 define el contrato API/DTO **futuro**, manteniendo las reglas y sus limitaciones.

**No hay endpoints, controller, código Angular, DTO C#, tablas EF, migración MySQL, nueva autoridad de autenticación, configuración activa o despliegue creados por este Markdown.** La app Angular 22.2.1 y .NET 10 operan sin cambios. Todos los casos de aceptación listados son **oráculos FUTUROS_NO_EJECUTADOS**, sin evidencia de HTTP, Chromium, WCAG, xUnit, DB, JWT real o proveedor. Nunca usar un contrato de interfaz como prueba de instalación.

## 2. Operaciones conceptuales API01–API08 (sin ruta ni método HTTP comprometidos)

| Operación | Solicitud y salida conceptual | Autorización, límite y error esperado |
| --- | --- | --- |
| API01 — Consultar catálogo | Filtro tipado, versión, lista de tokens y patrones permitidos | Catálogo público aprobado o recurso restringido según backend; paginado y E03 |
| API02 — Leer tema efectivo | Contexto de sesión backend; `ThemeEffectiveView` saneado | Tenant de sesión, membresía vigente y propiedad A/B; E04/E05 |
| API03 — Previsualizar variante | `ThemePreviewRequest` con versión, preset y overrides allowlist | No modifica DS06 activo, límite de tamaño y sanitización; E06/E07 |
| API04 — Validar accesibilidad | Manifest de contraste/foco/estado para revisión futura | Resultado distingue evidencia browser ejecutada de reglas sólo previstas; E08 |
| API05 — Proponer tema | `ThemeChangeProposal` con revisión esperada e idempotency key | JWT/RBAC server-side y propiedad, creación de intención no publica tema; E02/E09 |
| API06 — Consultar revisión | `ThemeChangeStatus` por intento y sesión autorizada | Sin fuga de tenant, paginación y auditoría saneada; E04/E10 |
| API07 — Solicitar revertir | `ThemeRollbackProposal` con revisión previa comprobada | No concede permisos ni restaura datos; exige revisión y motivo; E09/E11 |
| API08 — Leer historial | `ThemeAuditPage` redactado y filtrado por ownership | Sin JWT, token secreto, URL sensible, correo o datos de B; E12 |

Estas son **operaciones hipotéticas** para el diseño, sin verbos, rutas, controller ni auth middleware implementados. Una publicación futura debe verificar contratos con consumidores existentes y pruebas HTTP/tenant reales separadas.

## 3. DTO conceptuales y política de exposición

| DTO | Campos admitidos (contrato futuro) | Campos rechazados explícitamente |
| --- | --- | --- |
| `TokenCatalogView` | `tokenKey, semanticType, revision, safeValue, status` | CSS arbitrario, evaluador JS, selector global, credencial |
| `ThemeEffectiveView` | `presetId, version, safeTokenOverrides, activatedAt, etag` | DSN, JWT, roleClaims, empresa ajena, objeto secreto |
| `ThemePreviewRequest` | `presetId, expectedVersion, boundedOverrides` | `empresaId` como autoridad, `rawCss`, `rawHtml`, `script` |
| `ThemeChangeProposal` | `presetId, expectedRevision, idempotencyKey, reasonCode` | `force`, `tenantId` privilegiado, `disableRbac`, URL arbitraria |
| `ThemeChangeStatus` | `intentId, state, revision, updatedAt, safeErrorCode` | Identidad o datos de otro tenant, secretos, detalles internos |
| `ThemeRollbackProposal` | `intentId, expectedRevision, priorRevision, reasonCode` | Restauración MySQL, acceso a storage, grant de producción |
| `ThemeAuditPage` | `items, cursor, countBounded, redactedCorrelationId` | PII innecesaria, token, host privado, payload bruto |
| `AccessibilityEvidenceView` | `patternVersion, themeVersion, viewport, sha, executed, outcome` | `passed=true` inferido de documento o tests no corridos |

**Regla de contexto:** cliente puede pedir un recurso y su revisión, pero NO elegir tenant efectivo, almacenamiento, credenciales ni permisos. El backend obtiene la identidad, rol vigente y EmpresaId autorizado por su propia sesión/almacén. DTO omitido no concede un default permisivo. `safeTokenOverrides` es un mapa tipado con allowlist de propiedades y límites de longitud/número; rechazar cualquier valor dinámico no verificado.

## 4. Errores seguros E01–E12

| Código | Causa que obliga a denegar | Respuesta esperada |
| --- | --- | --- |
| E01 AUTH_REQUIRED | JWT no autenticado o verificación backend ausente | 401 conceptual, sin recurso |
| E02 ROLE_DENIED | Permiso/rol actual insuficiente o revocado | 403 conceptual, auditado |
| E03 CATALOG_UNKNOWN | Token/componente/versión no registrado | 404/422 saneado, sin internals |
| E04 TENANT_OWNERSHIP | Empresa B intenta leer/modificar tema A | 404/403 no enumerativo |
| E05 SESSION_EPOCH_STALE | Cambio A→B y respuesta antigua de A | Rechazo, limpiar estado anterior |
| E06 OVERRIDE_NOT_ALLOWED | CSS/URL/HTML/JS o propiedad fuera de allowlist | 422; no aplicar DOM |
| E07 PAYLOAD_TOO_LARGE | Overrides exceden cardinalidad/longitud | 413/422, no cambios parciales |
| E08 ACCESSIBILITY_NOT_VERIFIED | Evidencia de browser/contraste sin ejecución real | `NOT_VERIFIED`, jamás PASS |
| E09 VERSION_CONFLICT | expectedRevision/Etag o idempotencia no coinciden | 409, ninguna activación |
| E10 INTENT_NOT_FOUND | Intento fuera del tenant o inexistente | 404 no enumerativo |
| E11 ROLLBACK_NOT_AUTHORIZED | Revisión anterior inválida o falta de gate | NO_GO, sin restauración ni escritura |
| E12 AUDIT_ACCESS_DENIED | Actor sin permiso de historial o alcance paginado | 403, valores redactados |

HTTP 401/403/404/409/413/422 son **códigos propuestos** que deben validarse con los patrones actuales en implementación; este documento NO crea rutas ni devuelve respuestas.

## 5. Validaciones y compatibilidad V01–V16

1. **V01** identidad y JWT verificados exclusivamente en API backend; frontend es defensa de UX, no fuente de autoridad.
2. **V02** revalidar membresía, rol, suspensión y revocación por operación; denegación por defecto.
3. **V03** verificar ownership tenant A/B en lectura, preview, intento, estado, rollback y auditoría.
4. **V04** en cambio A→B, descartar respuesta antigua por epoch/abort y limpiar tema/caché anterior.
5. **V05** sólo nombres de token `DS01` registrados, semánticos y con revisión válida.
6. **V06** mapa override con tipos permitidos, tamaño acotado y desinfección de URL/asset.
7. **V07** rechazar scripts, `style` libre, selectores DOM, `url()` arbitrario y tokens de otro tenant.
8. **V08** resolver alias `DS04` sin ciclos; denegar referencia ausente antes de mostrar tema.
9. **V09** validar contraste, foco, teclado, zoom y responsive con evidencia real futura antes de afirmar accesibilidad.
10. **V10** preview produce datos inertes sin activar `DS06` ni otorgar permisos.
11. **V11** propuesta vinculada a actor, tenant, versión, SHA y clave idempotente; no reintentos con efectos duplicados.
12. **V12** clave repetida con tenant, payload o revisión diferente retorna conflicto y no revela datos.
13. **V13** publicar/revertir exige revisión monotónica y gate de rollback, CAS futuro y auditoría append-only.
14. **V14** errores y registros usan correlationId opaco, redacción y paginación; jamás tokens/DSN/PII extra.
15. **V15** compatibilidad: firmas existentes no se cambian; clientes antiguos mantienen tema neutral y ruta vigente hasta evolución aprobada, no fallback de permisos.
16. **V16** A4 exige checks reales `completed/success` sobre SHA causal y A5 de otra Task Javier; ningún `skipped/cancelled/in_progress` desbloquea implementación.

**Compatibilidad funcional:** campos nuevos opcionales llevan defaults de denegación; valores de tokens versionados, serialización estable, límites de pagina y TTL deben ser pruebas futuras de consumidores. Una migración de esquema, cambio de API ya consumida o router server-side necesita evaluación separada y CI específica, no es una consecuencia automática de M02.02.

## 6. Aceptación futura P01–P10 / N01–N10 (NO EJECUTADA)

| Par | Positivo propuesto | Negativo que debe detectar prueba futura |
| --- | --- | --- |
| P01/N01 | Catálogo devuelve tokens semánticos versionados | Selector CSS inesperado o alias circular pasa al cliente |
| P02/N02 | Tenant A recibe sólo su tema saneado | Tenant B obtiene branding, assets o información de A |
| P03/N03 | Cambio A→B invalida respuestas anteriores | Respuesta lenta restaura CSS/imagen de A durante sesión B |
| P04/N04 | Preview válido no modifica tema activo | Preview hace mutación de producción sin aprobación |
| P05/N05 | Override seguro pasa validación tipada | HTML/JS/URL externa o variable arbitraria llega al DOM |
| P06/N06 | Reintento idéntico mantiene un único intento | Replay cross-tenant o payload nuevo se acepta con misma clave |
| P07/N07 | expectedRevision actual permite propuesta | Dos clientes compiten y revisión vieja gana |
| P08/N08 | Rol activo propone revisión autorizada | Rol revocado o JWT inválido cambia tema |
| P09/N09 | Evidencia browser real vincula SHA y tema | Documento, mock o test cancelado produce resultado `PASS` |
| P10/N10 | Rollback con target y aprobación válidos se propone | `git revert` documental se atribuye a restauración de datos |

Esta matriz es **diseño de pruebas**: no se ejecutaron HTTP, MySQL, xUnit, Playwright, Chromium, contrastes medidos ni pruebas de seguridad en este contrato.

## 7. Auditorías A0–A5, decisión y rollback

**A0:** cotejar fuente V5, PLAN/COLA/DAG exactos y `C05-01.M01.06` + modelo conceptual `C05-01.M02.01`; diff de solo este Markdown y una sección CHANGELOG autocontenida. **A1:** inspeccionar coherencia de API01–08, 8 DTO, E01–12, V01–16 y P/N01–10 futuros; no falsificar tests. **A2:** revisar contrato fail-closed de JWT/RBAC, revocación, tenant A/B, CSS seguro e idempotencia. **A3:** documentación conceptual alineada con Angular22/.NET10 y sin runtime; .NET/Angular/DB/Playwright `NO_APLICA` **por diff exclusivamente Markdown**, no PASS. **A4:** commit en `dev` con fast-forward y CAS HEAD, verificar `verify-solqaryn-project-scope` y procedencia V5 `completed/success` sobre SHA exacto, y readback PLAN/COLA/DAG/BITACORA. **A5:** revisor de otra Task Javier, distinto de autor, con evaluación propia y opción de A5_FAIL; no autoprobación.

**Salida:** contrato documental de API/DTO/errores, NO implementación. Gate dinámico de cada siguiente M02, M03, M01.06 o M06.06 se reevalúa según DAG/owner; ni LISTO_DEV ni un A5 documental equivalen a JWT/tenant/HTML seguro en runtime. **Rollback:** `git revert` normal del changeset Markdown; no force-push, no borrar BITACORA ni recuperar datos productivos desde Git. No QA/main/PROD, servidores, cuentas, dominios, secrets o pagos.
