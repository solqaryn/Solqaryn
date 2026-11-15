# SOLQARYN V5 — C08-04.M02.02: contratos API/DTO, errores, validación y compatibilidad de módulos sectoriales

**Autor:** PRIMARY DEV 06 — JAVIER_CHATGPT, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Fecha:** 2026-11-15. **Fuente única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, PLAN/COLA/DAG del Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. **Repositorio:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT.

**Clase:** contrato documental M02.02, NO implementación de HTTP/DTO compilado. No altera API, Angular, roles, JWT, MySQL, flags, workers, eventos, webhooks, DNS, QA/main/PROD ni datos. Futuros tests NO_EJECUTADOS; A5 requiere revisor de otra Task Javier.

## 1. Antecedentes y compatibilidad

`C08-04.M01.01` (blob `41dd2a10d667124c021791278d43ceab3dc02c35`) define NR01–NR12; `M01.03` (blob `f34c6b59328d9d5a9e106e55eedeb250d50fb900`) documenta once fuentes de código y brechas; `M01.05` (blob `9ad425e66e7839d38a2416cf97615b20e0e129a2`) fija AC01–AC14/N01–N14 FUTUROS; `M01.06` (blob `009a4190314f89ec1516d0a8c4ddc7fe08cb4a96`) conserva BL01–BL16. Contrato conceptual hermano M02.01 propone `CapabilityDefinition`, `TenantCapabilityAssignment`, vínculos a Empresa/Sucursal y auditoría; se mantiene independiente y con A5 ajena.

La API vigente es ASP.NET Core 10 en capas Domain/Application/Infrastructure/API, seguridad JWT/RBAC y MySQL/EF Core; frontend Angular 22.2.1 expone guards de experiencia. Cualquier endpoint futuro debe seguir `ApiResponse`, controladores y middleware existentes sin crear autenticación paralela. El servidor debe validar membresía, Empresa efectiva, sucursal, propietario de objeto y permiso por acción, NO `X-Empresa-Id`, slug, Host o flag del navegador como autoridad.

## 2. Operaciones propuestas (rutas hipotéticas NO REGISTRADAS)

| ID | Intención de caso futuro | Entrada mínima y autoridad | Respuesta segura o veto |
| --- | --- | --- | --- |
| OP01 | Listar capabilities visibles | Contexto efectivo + permiso lectura | Lista allowlisted para Empresa, sin claves de B |
| OP02 | Consultar estado de capability | Clave versionada + Empresa servidor | Estado explícito; no inferir permiso desde enabled |
| OP03 | Proponer activación | Clave + expectedVersion + motivo + rol | Sólo autorización backend; sin grant si faltan pruebas |
| OP04 | Suspender o desactivar | Identidad operador + expectedVersion | Niega altas nuevas sin borrar historial |
| OP05 | Consultar vínculo de recurso | resourceKind/id + permiso y tenant | 404/403 seguro sin enumerar otros tenants |
| OP06 | Consultar compatibilidad por rubro | Catálogo allowlisted y versión | Deniega rubro/schema desconocido |
| OP07 | Recuperar snapshot de auditoría | Scope, paginación y rol auditor | Resultado depurado sin tokens ni PII |
| OP08 | Verificar reversión | SHA, snapshot de versión y autorización | Sólo evidencia/plan; no ejecutar rollback automático |

Los nombres OP01–OP08 son casos de uso, **no rutas HTTP activas** ni nuevos permisos concedidos. Semántica de POST/PUT/PATCH y URL concreta requieren decisión M03 y revisión de consumidores reales.

## 3. DTO conceptuales y validación de entradas

| Contrato futuro | Campos tipados propuestos | Regla positiva y negativa |
| --- | --- | --- |
| `SectorCapabilityQueryV1` | capabilityKey, schemaVersion, pagingToken? | Key allowlisted; rechazo de campos arbitrarios |
| `SectorCapabilityStatusV1` | capabilityKey, state, effectiveVersion, editable | No incluir identificadores o permisos del tenant B |
| `SectorCapabilityMutationV1` | capabilityKey, expectedVersion, reason, idempotencyKey | Versión y clave vinculadas a Empresa servidor; falla CAS/replay |
| `SectorResourceLinkV1` | resourceKind, opaqueResourceRef, expectedVersion | Enlace a recurso verificado, no ID cruzado ni ownership NULL global |
| `SectorPolicyProjectionV1` | capabilityKey, allowedActions, policyVersion | Proyección UI no otorga permiso API |
| `SectorCapabilityAuditV1` | safeActorRef, operationRef, stateFrom, stateTo, evidenceSha | Depurado, append-only y sin JWT/PII |

No agregar estas clases a producción por este documento. Si se implementan, usar DTO separados del dominio y validar schema estricto, tamaños máximos basados en riesgo/medición futura, tiempo UTC auténtico, autorización y transacciones en la capa correspondiente.

## 4. Errores, denegaciones y compatibilidad futura

| Código conceptual | Causa | Respuesta requerida en prueba posterior |
| --- | --- | --- |
| E01 | JWT ausente/inválido/caducado | 401 seguro; no datos |
| E02 | RBAC insuficiente o revocado | 403, sin bypass por flag |
| E03 | Empresa, recurso o sede ajena | Denegación sin enumerar B |
| E04 | Capability no allowlisted | 400/404 según contrato real, sin activar |
| E05 | Versión del catálogo no soportada | Conflicto/versionado explícito |
| E06 | expectedVersion obsoleta | 409, sin cambios parciales |
| E07 | idempotencyKey duplicada con payload distinto | Conflicto y rollback transaccional futuro |
| E08 | Empresa inactiva, capability disabled | No permitir nuevas operaciones |
| E09 | NULL legacy ownership ambiguo | Fail closed y seguimiento de migración futura |
| E10 | Host/slug/cabecera cliente manipulados | No conceder rol, tenant ni sesión |
| E11 | Timeout/caché/servicio no disponible | Error recuperable sin éxito silencioso |
| E12 | Datos sensibles en log/DTO | Rechazar y sanear, no exponer JWT/PII |

Compatibilidad: contratos serían aditivos y versionados con consumidores actuales, sin modificar `Empresa`, `UsuarioEmpresa`, `Producto`, `Sucursal`, `Cliente` ni `PublicStoreBinding` sin un scope de código M03 autorizado. Membership comercial no equivale al rol empresarial ni al entitlement SaaS. Una fila `EmpresaId=NULL` histórica no habilita consultas globales.

## 5. Oráculos P/N para pruebas futuras, NO_EJECUTADOS

P01/N01: consulta A con autorización retorna sólo A; ID de B no filtra siquiera metadatos. P02/N02: cambio válido y versionado produce una sola transición; concurrencia/replay genera 409 y ninguna duplicación. P03/N03: suspendido no registra nuevas operaciones, pero su historia permanece accesible sólo al actor permitido. P04/N04: configuración tipada se proyecta a Angular; flag/UI manipulados no eluden API. P05/N05: host público normalizado muestra catálogo mínimo; host inventado no otorga permiso privado. P06/N06: errores y auditoría están saneados; JWT/PII en input o salida se rechazan. P07/N07: vínculo de sucursal A cumple permiso; sede ajena o ownership ambiguo se bloquea. P08/N08: reversión exige preflight, datos de backup y pruebas; estado de fallo jamás se certifica PASS.

## 6. A0–A5, trazabilidad y rollback

- **A0:** verificar sólo archivo Markdown y sección autocontenida del CHANGELOG, sin cambio de runtime, secreto ni permiso.
- **A1:** cotejar OP01–OP08, contratos y E01–E12 más pares P/N escritos; no afirmar xUnit, HTTP ni E2E.
- **A2:** revisar límites JWT/RBAC/tenant A/B, idempotencia, revocación, host y PII como diseño de seguridad, sin declarar runtime seguro.
- **A3:** concordancia conceptual con M01 y M02.01, ninguna integración API/EF/MySQL ejecutada (NO_APLICA motivado).
- **A4:** publicación dev fast-forward/CAS, diff/blobs, checks `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` del SHA publicado; PLAN/COLA/BITACORA con readback.
- **A5:** revisión real independiente por una de las otras cinco Tasks Javier, sin autoaprobación.

Rollback de documento con `git revert` normal y conservación de cambios concurrentes; sin migración, evento, webhook, proveedor externo ni QA/main/PROD. M02.06 sólo cuando todas sus predecesoras figuren LISTO+A5 y el DAG/COLA calcule SI, no por esta especificación.
