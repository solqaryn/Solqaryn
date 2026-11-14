# C01-02.M02.02 — Contratos API/DTO, validación, errores y compatibilidad

**Unidad:** C01-02.M02.02 de SOLQARYN V5 FINAL; artefacto de **diseño**, no endpoint, DTO .NET ni permiso operativo nuevo. Autor: PRIMARY DEV 05 JAVIER (`6adc2e55f47c81919f6374e561687076`). Destino `solqaryn/Solqaryn@dev`, Scope Lock STRICT.
**Fuentes únicas:** [Standard V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [Sheet PLAN/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). M02.01 define entidades conceptuales y la intersección segura, sin creación real en EF/MySQL.

## 1. Compatibilidad con contratos realmente existentes

| Código inspeccionado | Blob | Contrato vigente y límite |
|---|---|---|
| `backend/src/API/Controllers/PermisosController.cs` | `f04f71475872a5692ab51732aabb167f43f03b50` | `GET/PUT permisos/matriz/{rolId}`, `GET permisos/mis-permisos`, `GET permisos/mis-permisos/empresa/{empresaId}`, `GET permisos/catalogo`; `[Authorize]`, filtros RBAC; no son endpoints de capabilities comerciales |
| `backend/src/Application/Services/PermisoService.cs` | `278a8fa90dec43609d20157208a143f5794dcc35` | Roles y grants relacionales activos, empresa propietaria, rechazo de privilegios de plataforma y de plantillas de sistema; ninguna capability por plan otorga permiso |
| `backend/src/Application/Security/IdentidadUniversalAuthorizationGuard.cs` | lógica tenant/revalidación actual | Exige acceso server-side por empresa, membresía, módulo, acción y rol; denegación antes de fuente ajena |
| `backend/src/Application/Security/PlatformSupportPolicy.cs` | `b103f0b0ec7b9169c060661f6a4456f8863152cf` | Soporte DEV solo lectura en cinco acciones de Productos/Categorías permitidas; no incluir capabilities o modificación de políticas |
| `backend/src/Domain/Security/ContextoTenantActual.cs` | `7377d2d752985cc9baf31d08280f38ad94a6a114` | Tenant no se construye desde DTO/claim arbitrario; `ExigirEmpresa` obligatorio |
| `docs/v5/c01-02-m02-01-modelo-capabilities-ownership.md` | `85344880fe45907bd9777dfdeb29dce1f07db3fb` | Modelo M02.01 propuesto: catálogo, versión, asignación y filtro de alcance, no entidades/persistencia real |

**Regla de compatibilidad:** conservar nombres, autorización, JSON de respuesta y contratos de clientes de `PermisosController`. Una futura API de capabilities se versionará **por separado** y se integrará en `Application` con guard servidor, sin redefinir `GetMisPermisos` como autoridad comercial ni usar `CommercialTenantUiCapabilities.Apply` como permiso backend. No se añade ruta ni filtro en esta unidad.

## 2. Contratos DTO conceptuales propuestos (NO clases creadas)

| Contrato futuro | Campos de entrada/salida diseñados | Validaciones fail-closed |
|---|---|---|
| `EvaluarCapacidadRequestV1` | `codigoCapacidad` (catálogo conocido), `tipoRecurso`, `recursoId`, `sucursalId?`, `versionPoliticaEsperada?` | Códigos no vacíos / tamaño acotado, IDs positivos, no aceptar `usuarioId`, `rolId`, `permisoId`, `empresaId` como autoridad |
| `EvaluarCapacidadResponseV1` | `permitida` boolean, `codigoEstado` DENEGADA/HABILITADA, `versionEvaluada` y `correlationId` opaco | No incluir claims, secretos, membresías, plan de otro tenant, razones sensibles ni listado de recursos ajenos; error seguro aun para 404 |
| `DefinicionCapacidadDtoV1` | `codigoCapacidad`, descripción pública y `versionCatalogo` | Solo catálogo permitido; enumeración no habilita grant |
| `ReglaCapacidadPlanDtoV1` | `codigoCapacidad`, `planVersionId`, `rubroId`, `vigenciaDesde`, `vigenciaHasta?`, `limites` | Intervalo coherente UTC, plan propio en servidor, sin elevación por superposición de intervalos |
| `AsignacionPlanTenantDtoV1` (lectura administrativa) | `planVersionId`, `vigencia`, `estado`, `versionConcurrencia` | Empresa derivada del contexto + objeto owned; no acepta asignaciones por IDs elegidos por cliente |

Los tipos anteriores son exclusivamente contratos propuestos para discusión M02; la persistencia, nombres definitivos, rutas y serialización deben aprobarse antes de la implementación. Nunca permitir `permitida=true` si no se puede validar grant relacional, plan y recurso *en el mismo ámbito de tenant*.

## 3. Superficie API FUTURA — no registrada ni invocable

| Ruta conceptual (pendiente de decisión y versión) | Método | Seguridad/resultado previsto |
|---|---|---|
| `/api/v1/capacidades/evaluar` | POST sólo consulta, idempotente sin efectos | JWT válido, membresía actual, owner recurso, RBAC explícito + restricción rubro/plan; nunca usa evaluación frontend para permitir la operación real |
| `/api/v1/capacidades/catalogo` | GET | RBAC de lectura administrativa y respuesta filtrada sin acceso a tenants extraños |
| `/api/v1/capacidades/planes/{planVersionId}` | GET administrativo | Owner/plano de administración separado, no público; el contrato exacto se decide en M02.04 |

**Prohibido habilitar estas rutas con este documento.** No se crea controller, DI, middleware, DTO, migration, worker, evento, webhook, dispatch ni flag. La autoridad de `GET/PUT permisos/matriz` actual no cambia; futuras rutas requerirán revisión de compatibilidad, contratos de OpenAPI y pruebas API/backend A/B dedicadas.

## 4. Códigos de error y rechazo de revelación

| Condición | Comportamiento futuro |
|---|---|
| Ausencia de JWT o token expirado/inválido | `401` sin detalles de identidad ni lista de recursos |
| Membresía revocada, grant faltante, plan/rubro deshabilitado o sucursal ajena | `403` de rechazo genérico; no dar pistas sobre permisos o configuración de otra empresa |
| Recurso o código no visible para el actor | `404` no enumerable o `403` indistinguible según contrato final; escoger política uniforme y probar BOLA/IDOR |
| Payload inválido, ID ≤0, longitud/código fuera de catálogo | `400`/`422` genérico, sin echo de token o datos sensibles |
| Versión de política no coincide / concurrencia | `409` y relectura solo del tenant propio; nunca aceptar versión stale |
| Fuente empresarial/plan/permiso no disponible, timeout o inconsistencia | DENY, sin fallback a ALLOW; `503` seguro sin enumerar datos internos |
| Operación prohibida de modificación desde soporte/rol sistema | `403` + cero mutaciones, independientemente de flags frontend o permisos legacy |

**Idempotencia y trazabilidad futura:** `correlationId` externo solo opaco; la API no reutiliza event ID de BITACORA ni lo usa para conceder privilegios. No incluir token JWT, PII, secretos o conexión SQL en errores/logs. Cache `private,no-store` en respuestas con permisos, igual o más estricto que el `GET mis-permisos/empresa` ya existente.

## 5. Matriz de pruebas para implementación futura (no ejecutadas aquí)

| Caso | Oráculo |
|---|---|
| P01 | Tenant A con membresía+grant+plan habilitado+recurso A: consulta coherente y sin mutaciones |
| P02 | Dos evaluaciones iguales de lectura producen misma decisión bajo política y versión estables |
| P03 | Cliente legacy `PermisosController` permanece compatible sin nuevos campos obligatorios |
| N01 | A solicita recurso de B por ID adivinado: DENY sin lectura/PII B |
| N02 | JWT inválido/anónimo: 401, sin evaluación de plan |
| N03 | Plan habilitado pero grant ausente: 403, NO ALLOW |
| N04 | Grant vigente pero plan vencido / feature flag no disponible: DENY |
| N05 | Rol `EsAdministrador` o soporte GET sin grant explícito: DENY |
| N06 | Tenant/sucursal/usuario en body intentan cambiar autoridad: IGNORAR o 400; NUNCA elevar |
| N07 | Política antigua, concurrente o revocada: 409/DENY |
| N08 | Fuente MySQL caída o timeout: 503/DENY, 0 mutación |
| N09 | Payload manipulado, codigoCapacidad LF, IDs cero/negativos o malformados: error seguro sin stacktrace |
| N10 | Intento de usar DTO o UI como permiso real / activar endpoint por M02: NO_GO |

## 6. A0–A5, dependencias y rollback

- Preflight de `C01-02.M02.02`: PLAN `H189=PENDIENTE`, `I189` vacío, COLA G/O=SI y DAG fila 203 F=VALIDADO/G=SI; seis preparativos `C01-02.M01.01–06` LISTO con A5 de Task Javier autorizada. La autorización permite **diseño M02.02**, no M03 ni cambio runtime.
- A0: artefacto y CHANGELOG únicos, referencias Git con blobs reales, sin secretos, sin `##` vacíos, sección autocontenida.
- A1: revisar P01–P03/N01–N10 como especificación, no afirmar que se ejecutaron dotnet, HTTP ni pruebas de seguridad.
- A2: comprobar por inspección las fronteras JWT/RBAC/tenant A/B, revocación, BOLA/IDOR, datos seguros y compatibilidad; sin certificar seguridad live.
- A3: confrontar `PermisosController`, `PermisoService`, guard actual y M02.01; implementación real requerirá pruebas dirigidas en GitHub Actions, MySQL efímero donde aplique.
- A4: publicación fast-forward exclusiva en dev con exact SHA, controles `verify-solqaryn-project-scope` y V5 CHANGELOG `completed/success`, readback PLAN/COLA/BITACORA. Compilaciones backend/Angular/MySQL/Playwright son NO_APLICA para este diff Markdown, pero `cancelled/skipped/in_progress` jamás cuentan PASS.
- A5: pendiente a otra Task JAVIER distinta del autor. QA/main/PROD, API, IAM, claves, migraciones, webhooks y compras requieren autorización aparte.
- Rollback: `git revert` normal, conservar eventos históricos en BITACORA, revalidar HEAD/consumidores; este documento no modifica esquema ni datos.
