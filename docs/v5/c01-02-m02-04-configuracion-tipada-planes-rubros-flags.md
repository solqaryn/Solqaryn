# C01-02.M02.04 — Configuración tipada, ámbitos y feature flags de capabilities

**Entrega:** contrato documental V5 FINAL de capacidades por plan, rubro, tenant, sucursal y recurso. **Autor:** PRIMARY DEV 06 — JAVIER, TASK_ID `6af16bbad26881918c283abe06e1f32a`. Sólo `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Fuentes:** [Standard V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN/COLA/DAG](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit).

**Naturaleza:** M02 define configuración y reglas de **diseño**; no crea feature flag, endpoint, tabla, worker, migración ni permiso en ejecución. Una condición comercial jamás reemplaza el permiso RBAC empresarial y el ownership verificado en backend. Los escenarios P/N siguientes son FUTUROS_NO_EJECUTADOS.

## 1. Código real y contratos previos inspeccionados

| Fuente exacta de dev | Blob Git | Afirmación verificable y límite |
|---|---|---|
| `backend/src/Domain/Entities/FeatureFlagsSaaS.cs` | `ec0062f4d76fd106365c20f3ba50c214369a3de5` | `PlanModulo` y `PoliticaModulosSaaS.Evaluar` deniegan tenant diferente, suscripción no vigente, plan inexistente/inactivo o incoherente, clave de módulo inválida y ausencia de regla explícita. **No** validan por sí solos RBAC, sucursal ni dueño del recurso. |
| `backend/src/Domain/Entities/ConfigEmpresa.cs` | `664547d28b513d3187622b02c57a7db1f812f7ec` | `ConfigEmpresa` tenant-scoped con Moneda, ZonaHoraria, JSON operativos, referencia opaca de secreto y token `Version`; no convertir su JSON en expresiones ejecutables. |
| `backend/src/Infrastructure/Persistence/Configurations/ConfigEmpresaConfiguration.cs` | `c26a46d4381618c960c93bb71806f802969ddcb8` | `EmpresaId` único, `Version` concurrency token, controles de Moneda; la configuración operativa no es fuente de grants. |
| `backend/src/Application/Services/EmpresaConfiguracionService.cs` | `bfe65f93aa00e2ce7cc805b553c7f1b34014b98b` | Servicio legacy y tenant-first coexistentes; no introducir una ruta paralela para habilitar módulos. |
| `backend/src/Infrastructure/Persistence/Configurations/EmpresaConfiguracionConfiguration.cs` | `f238687672981621506175db2cc9d6ec6d02d323` | Configuración de presentación heredada y activa única; no confundirla con catálogo de autorizaciones por tenant. |
| `backend/src/Application/DTOs/EmpresaConfiguracionDto.cs` | `203ec5ceaeb99ba2afdd14846ef6c373668fa0ff` | DTO tenant-first devuelve versión y valores no secretos; `CorreoSecretoReferencia` no se expone. |
| `docs/v5/c01-03-m02-04-configuracion-tipada-ambitos-flags.md` | `0532022ee7f43a69e10bf5b051f098281cf8242b` | Contrato GENERAL de configuración dinámica, reutilizable sin crear otro registro global; este M02.04 aplica el límite especializado a capabilities comerciales. |
| `docs/v5/c01-02-m02-01-modelo-capabilities-ownership.md` | `85344880fe45907bd9777dfdeb29dce1f07db3fb` | `DefinicionCapacidad`, `ReglaCapacidadPlan`, `AsignacionPlanEmpresa` y `RestriccionCapacidadRecurso` aún conceptuales. |
| `docs/v5/c01-02-m02-02-contratos-api-dto-capabilities.md` | `126802a61e74816785242c0fc815e36a70398139` | Rutas/DTO solo propuestos; no hay API de capabilities creada. |
| `docs/v5/c01-02-m02-03-persistencia-indices-migraciones-fixtures-capabilities.md` | `32fbba1c338acc45d78a8bb66d727c8a05480132` | Diseño anterior de índices, vigencias y fixtures: no DDL real, sin equivalencia a RBAC. |

La separación crítica es entre `PoliticaModulosSaaS.Evaluar` (filtro comercial ya existente) y `PermisoService`/guard de autorización (permiso real). **ALLOW final es la conjunción** de identidad verificada, membresía activa, grant de módulo/acción, pertenencia del recurso, política comercial aplicable y restricción de sucursal. Los permisos no se heredan de flags ni de DTOs.

## 2. Tipos y ámbitos propuestos, sin activación

| Campo del catálogo especializado futuro | Tipo y restricción | Regla de denegación |
|---|---|---|
| `CodigoCapacidad` | Identificador normalizado allowlisted, de longitud acotada, sin confusables | Código desconocido, vacío o malformado = DENY |
| `TipoValor` | `Boolean`, `BoundedInt`, `ClosedEnum`; `SafeString` solo donde haya justificación y escape | SQL/JS/HTML/CSS arbitrario, expresiones y URIs inseguras = RECHAZO |
| `AmbitoPermitido` | Conjunto explícito `PLATFORM`, `PLAN_VERSION`, `RUBRO`, `TENANT`, `BRANCH`, `RESOURCE` | Ningún wildcard implícito; claves de plataforma no editables por ERP |
| `DefaultFailClosed` | Boolean false, límite 0 o estado DENY conforme semántica | Ausencia, timeout, versión inválida o conflicto nunca habilitan |
| `VigenciaDesdeUTC/VigenciaHastaUTC` | UTC, intervalo bien formado, sin solapes efectivos | Filtro vencido o futuro no permite acción |
| `PolicyVersion` | Versión monotónica para lectura/escritura condicional | Cambio con versión obsoleta => 409 o rechazo; cache ALLOW se invalida |
| `ReadPermission/WritePermission` | Grant RBAC server-side explícito más ownership de tenant | Rol administrativo legacy, soporte o plan activo NO son permiso |
| `Sensitivity` | PrivateTenant por defecto, Public solo allowlist; Secret nunca en esta tabla | No exponer JWT, PII, claves, cadena de conexión ni referencia secreta en DTO público |

No se añade el registro `ConfigEmpresa` como dependencia para la decisión RBAC: es configuración operativa vigente y cualquier nuevo catálogo se integrará de manera reversible bajo M03 autorizado. El plan/rubro restringe una acción; no modifica la matriz `RolPermiso`. Una opción `ui.compactMode` es presentación, no prerrogativa de lectura del recurso privado.

## 3. Precedencia y cálculo de alcance (contrato futuro)

1. Resolver usuario y membresía activa desde backend, empresa efectiva y rol empresarial verificable. El tenant no procede de URL, cookie, header, claim sin validar ni parámetro del navegador.
2. Comprobar grant de `ModuloSistema/AccionPermiso` vigente y la propiedad real de recurso/sucursal; denegar antes de consultar decisiones comerciales ajenas.
3. Validar `PoliticaModulosSaaS.Evaluar` para empresa propia, suscripción vigente, plan efectivo y regla explícita del módulo. Un `DecisionModuloSaaS.Habilitado` es **necesario cuando aplica** pero nunca suficiente para ALLOW.
4. Evaluar intersección del catálogo allowlisted con rubro/plan, tenant, sucursal y recurso; una restricción más específica **solo limita**, jamás puede revertir un DENY superior. Si hay políticas duplicadas/conflictivas o datos faltantes, DENY.
5. Revalidar versión y vigencia en servidor para cada operación protegida; registrar una decisión no sensible (actor opaco, empresa autorizada, versión, resultado). No persistir ni cachear ALLOW indefinidamente.
6. La UI Angular solo podrá mostrar una proyección segura de features; no enviará configuración para autoconceder permisos ni sustituirá API/guarda. El soporte de plataforma se mantiene en su allowlist de lectura.

La precedencia de capacidades **no** es “plataforma ALLOW vence tenant DENY”. Es un filtro de restricciones acumulativas. Un admin de empresa A no edita políticas de B. Cambiar el plan no altera grants históricos. Revocar grant/plan/membresía invalida autorizaciones anteriores aunque el cliente conserve estado visual.

## 4. Escenarios documentales positivos y negativos (NO ejecutados)

| Caso | Signo | Oráculo de implementación futura |
|---|---|---|
| P01 | + | Miembro A + grant + plan vigente + recurso owned A -> ALLOW limitado a A |
| P02 | + | Plan cambia con grants iguales -> varía filtro comercial, sin grant nuevo |
| P03 | + | Deshabilitar flag de tenant A -> DENY en A, no modifica B |
| P04 | + | Comparar versionado estable en dos lecturas -> decisión igual si inputs idénticos |
| N01 | - | Tenant A envía recurso B por ID -> DENY sin revelar datos B |
| N02 | - | Plan y flag habilitados, grant ausente -> DENY |
| N03 | - | Grant existe, plan vencido o ausente -> DENY |
| N04 | - | `EsAdministrador` o rol legacy sin grant -> DENY |
| N05 | - | Sucursal A1 solicita recurso de A2 sin autorización -> DENY |
| N06 | - | Flags duplicados, versiones solapadas o ambiguas -> DENY |
| N07 | - | DTO/header/claim pide EmpresaId=B -> ignorar o rechazar; nunca elevar |
| N08 | - | Suscripción revocada mientras UI muestra ALLOW -> backend DENY |
| N09 | - | Política, MySQL o fuente de permisos falla/timeouts -> DENY, sin mutación |
| N10 | - | Payload incluye SQL/JS/HTML o clave desconocida -> 400/422 seguro |
| N11 | - | Versión obsoleta o cache ALLOW tras revocación -> 409/DENY sin bypass |
| N12 | - | Configuración/errores/logs exponen token o política B -> sanitizar, no revelar |
| N13 | - | Operador de soporte edita capability o plan fuera de allowlist -> DENY |
| N14 | - | Se intenta instalar endpoint, worker/evento, DB o despliegue solo con M02.04 -> NO_GO |

La tabla contiene **4 positivos y 14 negativos de diseño**. No son resultados de xUnit, HTTP, MySQL, Playwright o pruebas de seguridad runtime. Futuras M03 deberán implementarlos con CI pertinente del SHA exacto y MySQL efímero si afectan persistencia.

## 5. A0–A5, dependencias, impacto y rollback

**Ámbito del changeset:** únicamente `docs/v5` y `CHANGELOG_AI.md`. Sin cambios a Angular, Domain, Application, Infrastructure, API, EF/MySQL, JWT, roles, permisos, secretos, jobs, providers ni QA/main/PROD. Consumidores futuros: M02.05 correlación, M02.06 contrato de salida, M03 policy enforcement y tests. M01.01–06 y A5 previos condicionan M02; no se liberan dependencias por este diseño.

- A0: inspección de diez fuentes/blobs reales, diff Markdown, no secretos, sin encabezados vacíos en CHANGELOG, sección con viñetas propias.
- A1: inspección de P01–P04/N01–N14 como oráculos escritos; no afirmar pruebas runtime ejecutadas.
- A2: verificar riesgos de JWT, RBAC, tenants A/B, recurso y sucursal, SQL/JS injection, revocación y errores seguros.
- A3: compatibilidad documental con `FeatureFlagsSaaS`, `ConfigEmpresa`, `PermisoService`, M02.01–03 y C01-03.M02.04; runtime no se modifica.
- A4: publicación por CAS/fast-forward en `dev`; comprobar Git HEAD exacto y los checks obligatorios Scope Lock y “V5 - Sin encabezados vacios ni atribucion cruzada” `completed/success`, además de readback PLAN/COLA/BITACORA. Builds .NET/Angular, MySQL y Playwright = **NO_APLICA sólo por diff Markdown**; no contarlos como PASS.
- A5: requiere otra de las cinco Tasks JAVIER distinta de la autora; no certifica promoción a QA/main/PROD.

**Rollback de esta entrega:** `git revert` normal del commit en dev y registro compensatorio aditivo tras revalidar HEAD; nunca borrar historia BITACORA, forzar push o manipular datos tenant. Sin activar eventos/webhooks ni comprar proveedor.
