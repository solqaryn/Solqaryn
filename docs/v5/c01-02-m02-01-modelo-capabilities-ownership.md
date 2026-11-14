# C01-02.M02.01 — Entidades, ownership e invariantes de capabilities/RBAC

**Estado del artefacto:** diseño M02 del plan V5 FINAL, no entidad EF ni permiso instalado. **Autor:** PRIMARY DEV 05/JAVIER, TASK_ID `6adc2e55f47c81919f6374e561687076`. **Rama:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`.
**Autoridades:** [Standard V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). La entrada `C01-02.M02.01` está limitada a *definir* entidades, propiedad, relaciones e invariantes; no modifica código ni esquema.

## Contratos actuales observados en HEAD `dev`

| Componente real | Blob Git verificado | Regla que NO debe cambiar |
|---|---|---|
| `backend/src/Domain/Entities/UsuarioEmpresa.cs` | `f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c` | `(UsuarioId,EmpresaId)` membresía activa, RolId efectivo por empresa, nunca `Usuario.RolId` legacy |
| `backend/src/Domain/Entities/Rol.cs` | `a4f913d66f2c6bd2351155d66a93f9d12dc18a09` | Rol empresarial activo y asignable a dueño; `EsAdministrador` no sustituye grant |
| `backend/src/Domain/Entities/Permiso.cs` | `88085d51759403b786b5b384989c78c6c216f04b` | `ModuloSistema`/`AccionPermiso` y ámbito empresarial; catálogo no concede |
| `backend/src/Domain/Security/ContextoTenantActual.cs` | `7377d2d752985cc9baf31d08280f38ad94a6a114` | Contexto solo desde membresía, `ExigirEmpresa` falla cerrado |
| `backend/src/Application/Security/IdentidadUniversalAuthorizationGuard.cs` | código actual inspeccionado | Permiso relacional explícito, tenant y revocación verificados en servidor, sin superusuario implícito |
| `docs/v5/c01-02-m01-06-bloqueo-rollback-liberacion.md` | `a00aaa117f26c567a995a3e3a2e6017d17f09686` | Cierre M01 y A5 independiente: M02 necesita su propio preflight y pruebas |

## Entidades y relaciones PROPUESTAS (contrato, NO esquema creado)

| Tipo conceptual | Identidad natural / dueño | Relaciones y semántica cerrada |
|---|---|---|
| `DefinicionCapacidad` | `CodigoCapacidad` único, inmutable, con versión | Identifica una acción de producto; no es grant RBAC ni token de acceso, su alta requiere catálogo aprobado |
| `ReglaCapacidadPlan` | `(PlanVersionId, CodigoCapacidad, RubroId)` | Expresa límite comercial aplicable para un intervalo vigente; es filtro adicional del permiso, nunca autorización positiva autónoma |
| `AsignacionPlanEmpresa` | `(EmpresaId, PlanVersionId, VigenciaDesde)` | Dueño `EmpresaId` no derivado del cliente; solo una versión efectiva para fecha/criterio de precedencia aprobados; expiración o suspensión DENY |
| `RestriccionCapacidadRecurso` | `(EmpresaId, CodigoCapacidad, TipoRecurso, RecursoId, SucursalId?)` | Requiere recurso realmente owned por empresa y sucursal; sin dueño verificado o sucursal cruzada DENY |
| `DecisionCapacidad` (proyección NO persistida) | `(UsuarioId, EmpresaId, SucursalId?, RecursoId?, Modulo, Accion)` | Resultado de intersección de membresía activa, grant RBAC, restricción comercial y ownership, con causa segura no sensible |

**Autoridad relacional real:** `UsuarioEmpresa(UsuarioId, EmpresaId, RolId)` → `Rol(RolId, EmpresaId/Ambito)` → `RolPermiso(RolId, PermisoId)` → `Permiso(Modulo, Accion, Ambito)`; las restricciones futuras `AsignacionPlanEmpresa` → `ReglaCapacidadPlan` solo pueden *restringir* el resultado. Una definición de capacidad por sí sola nunca crea un grant. No se introduce vinculación real a facturación, suscripciones, cargos ni precios.

## Invariantes verificables de ownership y denegación

1. **TEN-01:** un `EmpresaId` de token, URL, DTO, header, cookie, slug o selector UI no cambia el contexto verificado de la membresía. Se comprueba la empresa del recurso desde persistencia autorizada.
2. **TEN-02:** roles empresariales asignables solo a la empresa de la membresía o plantilla explícita permitida; `RolId` legacy, `EsAdministrador`, rol de plataforma y soporte fuera de allowlist no elevan derechos.
3. **RBAC-01:** una decisión ALLOW exige un `RolPermiso` activo, empresarial, cuyo módulo y acción coinciden, y permiso vigente. Falta, revocación, rol inactivo o estado desconocido = DENY.
4. **CAP-01:** un entitlement de plan/rubro no añade permisos; con plan válido pero sin grant => DENY. Con grant pero plan caducado/inactivo/no aplicable => DENY cuando ese filtro sea exigible.
5. **OWN-01:** consulta o mutación de recurso requiere `Recurso.EmpresaId == Contexto.EmpresaId`; sucursal también debe estar activa y pertenecer a la misma empresa. Identificador adivinado o BOLA/IDOR => DENY sin revelar existencia.
6. **VER-01:** versiones de plan/política y vigencias deben tener precedencia explícita, monotonicidad y revocación seguras. Sin contrato de precedencia/TTL aprobado, no activar evaluación comercial ni cache de ALLOW.
7. **UNI-01:** unicidad conceptual de `(EmpresaId, CodigoCapacidad, TipoRecurso, RecursoId, SucursalId)` y reglas de versión por ventana temporal; índices, FK, normalización, colisiones y transacciones son diseño M02.03, no SQL aplicado aquí.
8. **AUD-01:** evidencias registrarían ID y versión del contrato, actor, empresa, resultado y causal no sensible; nunca incluir JWT, PII, secretos, lista de recursos de otro tenant ni claims crudos.
9. **ERR-01:** entradas inválidas, plan ausente, cuenta suspendida, feature flag incierta, timeout, duplicados o fuente no disponible terminan en DENY seguro, sin fallback al frontend ni a permisos de sistema.
10. **APP-01:** `Application` compone intersección y verificación server-side; `Domain` conserva invariantes; `Infrastructure` implementará repositorios cuando M03/M02.03 lo permitan; API es el borde y Angular solo presentación.

## Oráculos documentales concretos (NO son tests de runtime ejecutados)

| Caso | Escenario | Resultado obligatorio futuro |
|---|---|---|
| P01 | Tenant A con membresía, rol/permiso vigente, plan/rubro válido y recurso A propio | ALLOW únicamente para acción y alcance exactos |
| P02 | Cambio de plan vigente de A sin tocar grants | Ningún grant nuevo; evaluar restricciones y auditoría server-side |
| P03 | Revocación de membresía seguida de consulta | DENY inmediato tras revalidación; ninguna respuesta con datos de A |
| N01 | Tenant A solicita recurso owned por B o sucursal B | DENY sin descubrir existencia de B |
| N02 | Plan activo y capability incluida pero faltan grants RBAC | DENY |
| N03 | Grant activo pero plan/rubro no habilita capability | DENY |
| N04 | Rol legacy/`EsAdministrador` sin grant empresarial | DENY |
| N05 | Capacidad, recurso o plan duplicado, expirado o de versión incierta | DENY, no `first-match` arbitrario |
| N06 | Token, URL, cookie, frontend o claim aportan empresa/rol arbitrarios | DENY y no leer otro tenant |
| N07 | Política sin dueño, sucursal ajena, caché stale o consulta de permisos con error | DENY; no persistir ALLOW |
| N08 | Pretender activar EF, MySQL, endpoint, cobro, worker o IAM por este documento M02 | NO_GO; nueva microtarea y pruebas causales requeridas |

## Pruebas, dependencias, publicación y reversión

- **Preflight:** para este ID `PLAN_MAESTRO H188=PENDIENTE/I188 vacío`, `COLA G/O=SI`, `DAG_TECNICO F=VALIDADO/G=SI` y M01.01–M01.06 LISTO con A5 independiente Javier; `C01-02.M01.05` A5 correctiva por J40 en BITACORA fila 3158. La aptitud es individual, no licencia global para M02.02–06.
- **A0 documental:** revisar diff de solo Markdown+CHANGELOG; sin secretos, sin encabezados ## vacíos, sección del CHANGELOG autocontenida, ocho negativas y fuentes Git trazables.
- **A1 documental:** inspeccionar coherencia P01–P03 y N01–N08; estas filas son **casos diseñados**, no xUnit/HTTP ejecutados.
- **A2 documental:** preservar JWT/RBAC/tenant A/B, ownership, revocación y denegación por defecto; no afirmar aislamiento live ni permisos nuevos.
- **A3 documental:** comparar modelo propuesto con blobs reales y seis preparativos del mismo lote; cambios de persistencia/DI/endpoint requieren evaluación y CI por otra microtarea.
- **A4:** publicar fast-forward en `dev` por CAS exact-HEAD, comprobar `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` para SHA publicado y readbacks PLAN/COLA/BITACORA. Backend/MySQL/Angular/Playwright = NO_APLICA **solamente a este diff documental**; un cancelled/skipped/in_progress nunca se declara PASS.
- **A5:** pendiente de otra de las cinco Tasks Javier, Task distinta de J40, antes de promoción. QA/main/PROD y activaciones comerciales siguen prohibidas.
- **Rollback:** `git revert` normal previa revalidación del HEAD, evento compensatorio aditivo sin modificar BITACORA histórica; no hay DDL/producción/datos alterados en este cambioset.
