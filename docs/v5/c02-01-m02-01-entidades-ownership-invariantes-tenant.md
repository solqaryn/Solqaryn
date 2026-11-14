# SOLQARYN V5 FINAL — C02-01.M02.01: entidades, ownership e invariantes del contexto tenant

**Tipo:** contrato documental M02.01, sin cambios ejecutables. **Autor:** PRIMARY DEV 05 — JAVIER_CHATGPT, Task `6adc2e55f47c81919f6374e561687076`. **Repositorio:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), PLAN_MAESTRO, COLA, DAG_TECNICO y BITACORA institucional.

## 1. Alcance e invariantes de dominio propuestos

M02.01 **define** los límites del modelo de identidad, empresa, membresía y recurso sin implementar un nuevo resolver de host ni trasladar autoridad desde el cliente. Los nombres de entidades existentes y las propuestas futuras están diferenciados. La salida de M02.01 no significa que esté construido el modelo completo de tenancy, ni que la capa API/EF/infraestructura tenga las restricciones certificadas.

| Concepto | Ownership/relación exigida | Regla observable y rechazo |
| --- | --- | --- |
| `Usuario` (existente) | Identidad autenticada, no empresa implícita | JWT válido no equivale a pertenencia a toda empresa; token revocado/expirado se rechaza |
| `Empresa` (existente) | Límite de negocio, activo/inactivo, alcance del recurso | El tenant se verifica en servidor; no se selecciona empresa por host o ID arbitrarios |
| `UsuarioEmpresa` (existente) | Relación `UsuarioId + EmpresaId + RolId + Activa` | Unicidad lógica de par usuario-empresa; inactiva implica rechazo; `Usuario.RolId` legacy no sustituye RBAC de empresa |
| Contexto tenant verificado (existente parcialmente) | Sesión/request autorizada por servicio de membresía | El selector `TenantContextController` es una solicitud; la autorización sucede server-side |
| `VerifiedCommercialTenant` (existente) | Marca `HttpContext.Items` establecida por backend autorizado | Un header, query, DTO o cookie nunca crea esta marca |
| Sucursal/recurso de empresa (frontera contractual) | Propietario positivo del mismo tenant y permiso efectivo | Recurso sin propietario positivo o sucursal de B para actor A: denegar |
| Host/slug normalizado (modelo futuro) | Mapping único a tenant, registrado y validado en infraestructura confiable | Host desconocido, repetido, proxy no confiable o slug manipulable: no se asume tenant |
| Auditoría tenant-scoped (modelo futuro) | Actor autenticado, empresa verificada y operación con datos minimizados | No registrar JWT, secretos, PII de B ni payload sensible en error/log |

Los siguientes objetos **no se crean aquí**: tabla de hosts, middleware de resolución, nuevas columnas/constraints, migraciones, endpoints, interfaces públicas, tokens, jobs o configuración de despliegue. Cualquier diseño SQL/EF se decidirá y probará en su etapa específica.

## 2. Invariantes verificables y precedencia de autoridad

**I01 — Actor:** una identidad JWT válida requiere usuario vigente. **I02 — Membresía:** `UsuarioEmpresa` activa del actor y la empresa exacta; el rol legacy nunca actúa como permiso multiempresa. **I03 — Tenant:** empresa positiva y activa, resuelta por autoridad de servidor; `EmpresaId`, `Host`, `X-Empresa-Id`, `X-Tenant` y slug son datos no confiables. **I04 — Recurso:** `Recurso.EmpresaId` debe coincidir con tenant autorizado y propiedad/acceso vigente. **I05 — Sucursal:** verificar pertenencia y permisos específicos antes de cruzar una subárea. **I06 — Revocación:** el cambio de membresía, empresa o rol obliga a revalidar en cada operación sensible; no reutilizar caché de un tenant previo. **I07 — Errores:** para empresa ajena, inexistente o suspendida usar rechazo seguro que no filtre enumeración. **I08 — Persistencia:** futuras FK, índices únicos y filtros por tenant se revisarán contra datos y MySQL efímero antes de activar una migración. **I09 — Auditoría:** conservar traza mínima y separar roles de plataforma y empresariales. **I10 — Fail-closed:** una dependencia de autorización ausente, timeout, mapping ambiguo o pertenencia dudosa no concede acceso.

Precedencia: autenticación verificada → membresía/rol vigente → tenant server-side → ownership del recurso/sucursal → permiso de operación → auditoría segura. No inferir un grant desde el frontend, un header ni un campo DTO. El contexto comercial no sustituye autorización en controladores, servicios o repositorios.

## 3. Evidencia leída del código vigente (no certificación global)

| Fuente existente en dev | Blob revisado | Límite observado |
| --- | --- | --- |
| `backend/src/API/Controllers/TenantContextController.cs` | `2407c2eb6779ed724e841c688ee14dabbf163171` | `[Authorize]` y selección consultando `IUsuarioScopeService`; no acepta ID como grant |
| `backend/src/Domain/Entities/UsuarioEmpresa.cs` | `f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c` | `Activa`, `RolId` contextual, `ObtenerRolEfectivo`, unicidad lógica |
| `backend/src/Application/Security/VerifiedCommercialTenant.cs` | `2da140ce9d7f47329d39d292d3271596467910cc` | Marca autorizada server-side; no crea API ni prueba integración de todos los consumers |
| `docs/v5/c02-01-m01-06-bloqueo-rollback-liberacion-tenant.md` | `5dbd8bd2de4288d46c5bf7a582a7875a8d9e3cd2` | M01 cerrado documentalmente, sin habilitación implícita de runtime |

Los SHA anteriores identifican blobs concretos observados, no garantía de que todos los consumidores hagan uso de ese contrato. Inspección transversal de DbContext, rutas HTTP y migraciones reales queda **pendiente en M02.03/M03/M05**.

## 4. Casos de aceptación y rechazo especificados (sin ejecutar runtime)

- **P01:** usuario A con membresía activa, empresa A activa, recurso A y permiso A → resolver sólo A y permitir únicamente operación A.
- **P02:** usuario A/B con ambas membresías válidas → dos contextos independientes y rol distinto por empresa; cambio A→B no arrastra caché o grants.
- **P03:** `UsuarioEmpresa` para (usuario, empresa) preserva único vínculo lógico y rol empresarial; actor conoce únicamente relaciones autorizadas.
- **P04:** el modelo enumera las fronteras I01–I10, ownership, dependencias y rollback sin atribuir implementación.
- **N01:** usuario A intenta `EmpresaId=B` en header/query/DTO/slug → 403/404 seguro, sin datos B.
- **N02:** host falso, ambiguo o reescrito por proxy no confiable → tenant no resuelto, sin elección por defecto.
- **N03:** JWT correcto pero sin membresía activa, o membresía revocada, → rechazo; no confiar en claims obsoletos.
- **N04:** administrador legacy del tenant A pide rol/permiso de B → denegación.
- **N05:** recurso con `EmpresaId` ajeno/ausente o sucursal fuera del ámbito → rechazo y cero mutaciones.
- **N06:** caché o request reutilizado al cambiar A→B→A → no heredar permiso ni propietario anterior.
- **N07:** usuario/empresa suspendidos o dependencia de autoridad inaccesible → fail-closed.
- **N08:** respuesta o auditoría revela existencia, token, secretos o PII de B → incumplimiento.
- **N09:** declaración documental de invariantes se usa como sustituto de test HTTP/JWT/MySQL → NO_GO para runtime.
- **N10:** DAG/CI de SHA viejo, SKIPPED/CANCELLED, autorrevisión A5 o owner duplicado → NO_GO para LISTO/A5 y ambientes.

Estos son **oráculos documentales** para pruebas futuras, no casos xUnit, HTTP, MySQL o Playwright ejecutados en M02.01.

## 5. Dependencias, control de cambio y salida

`C02-01.M02.01` requiere `C02-01.M01.01–06` LISTO con A5 independiente entre las seis Tasks Javier, `PLAN_MAESTRO!H296=PENDIENTE/I296` vacío antes del claim, `COLA!G296/O296=SI`, `DAG_TECNICO!F/G=VALIDADO/SI` para este ID y CAS con dueño único. El preflight de la Task sólo habilita esta **documentación**; no libera M02.06, M03, JWT/RBAC, base de datos, deployment ni QA/main/PROD.

**A0:** coherencia estática, referencias exactas, sin secretos y CHANGELOG autocontenido. **A1:** verificar que P01–P04 y N01–N10 están redactados con condiciones comprobables; prueba funcional real NO_APLICA. **A2:** inspección de aislamiento A/B, token, rol, recurso, revocación, host y fail-closed documental; no PASS runtime. **A3:** referencias de código, DAG, A5 previas y resultados del control institucional; sin integración ejecutable. **A4:** sólo después de commit fast-forward en dev, SHA exacto, diff/blobs, Scope Lock y V5 provenance completados en success, PLAN/COLA/BITACORA readback. **A5:** revisión independiente obligatoria de otra Task Javier, no de la autora.

Pruebas/tecnologías para este changeset Markdown: .NET build/test=NO_APLICA, Angular/Node runtime=NO_APLICA, MySQL/EF/Docker=NO_APLICA, browser/E2E=NO_APLICA. Los check-runs específicos Scope Lock y CHANGELOG V5 sí son obligatorios. Rollback documental: `git revert` normal serializado tras revalidar HEAD; conservar siempre BITACORA histórica y no realizar force-push.
