# SOLQARYN V5 FINAL — C01-03.M02.04: configuración tipada, ámbitos y feature flags seguros

**Microtarea:** `C01-03.M02.04`. **Responsable:** PRIMARY DEV 04 — JAVIER, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. **Repositorio/alcance:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Fuente contractual:** Standard de Implementación de Automatizaciones V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), Plan/COLA/DAG institucional. **Naturaleza:** especificación M02, nunca un cambio operativo de configuración.

## 1. Objetivo y separación de autoridades

Definir un catálogo futuro **tipado, allowlisted, versionado y limitado por ámbito**; ningún dato editable puede representar código, establecer autorización de rol, modificar secretos o desactivar seguridad. Esta unidad DOCUMENTAL fija esquemas, precedencia y reglas de flags; no añade feature flag ni despliega gestor de settings. Las rutas, persistencia, pruebas reales y UI pertenecen a microtareas ejecutables distintas. M02.01–03 establecieron conceptos de owner, DTO/errores y plan físico, sin autorizar por sí solos M03.

Autoridad efectiva de cualquier lectura/escritura futura:

```text
actor autenticado -> membresía vigente -> empresa/sucursal/recurso verificados en servidor
  -> RBAC vigente para la operación -> catálogo permite clave y ámbito
  -> entitlement comercial si esa capacidad requiere plan -> valor tipado seguro
```

**El orden es una conjunción de restricciones**: el entitlement de plan no es un grant; el flag no es RBAC; la UI no autoriza; `EmpresaId`, `tenant_id`, URL, cookie, header y slug del cliente no verifican pertenencia. Un usuario de empresa A no puede resolver ni modificar configuración privada de B. Los valores de plataforma requieren rol global diferenciado y control reforzado, no un administrador ERP normal.

## 2. Fuentes ejecutables actuales inspeccionadas

| Fuente de código en dev | Blob y observación | Límite |
| --- | --- | --- |
| `backend/src/Domain/Entities/ConfigEmpresa.cs` | `664547d28b513d3187622b02c57a7db1f812f7ec`; `Moneda`, `ZonaHoraria`, JSON operativos, versión y referencia SMTP opaca | No ampliar JSON a expresiones ejecutables ni publicar referencia de secreto |
| `backend/src/Domain/Entities/FeatureFlagsSaaS.cs` | `ec0062f4d76fd106365c20f3ba50c214369a3de5`; `PlanModulo` y `PoliticaModulosSaaS` fail-closed verifican plan, suscripción, tenant y regla explícita | Esta política no comprueba por sí sola RBAC para editar un valor; un plan habilitado NO eleva permisos |
| `frontend/src/app/services/theme-applier.service.ts` | `cbccc2795cb1b010f74ce5b2b4dc1fb20f0b41f3`; `COLOR_HEX_SEGURO` acepta sólo hex `#RGB` / `#RRGGBB`, limpia tokens al cambiar contexto, descarta respuestas tardías | Este componente no demuestra existencia de un catálogo universal de flags; no admitir CSS libre |
| `backend/src/Application/Services/EmpresaConfiguracionService.cs` | `bfe65f93aa00e2ce7cc805b553c7f1b34014b98b`; servicio de config existente con ruta legacy y tenant-first | No asumir que sus consumidores se pueden reemplazar sin migración gradual y contrato API |
| `backend/src/Application/DTOs/EmpresaConfiguracionDto.cs` | `203ec5ceaeb99ba2afdd14846ef6c373668fa0ff`; DTO con `Version` tenant y valores operativos | No exponer campos ocultos ni usar DTO global como autoridad de empresa |
| `docs/v5/c01-03-m02-01-entidades-ownership-invariantes.md` | `8820c3b28b56a9134c6edc6fe8139c27a21ca340` | Referencia de owner y 12 invariantes |
| `docs/v5/c01-03-m02-02-contratos-api-dto-errores-compatibilidad.md` | `04de8c729ed45769d39f8edaff9af5940e02588b` | Errores y versionado futuros |
| `docs/v5/c01-03-m02-03-persistencia-indices-migraciones-fixtures.md` | `81ac4ffae031be5f8ed30bc95e587994c7478010` | Índices y rollback futuros, no DDL ejecutado |

## 3. Esquema tipado propuesto de catálogo (no nuevo código/tabla)

| Campo obligatorio de definición | Restricción | Ejemplo conceptual no activado |
| --- | --- | --- |
| `KeyCanonical` | Identificador único allowlisted, longitud limitada y normalización estable, sin traversal ni confusables | `ui.compactMode` |
| `ValueKind` | `Boolean`, `BoundedInt`, `Enum`, `SafeString`, `SafeColorHex` (si el caso lo exige) | `Boolean` |
| `AllowedScopes` | Conjunto explícito `PLATFORM`, `TENANT`, `BRANCH`, `RESOURCE`, nunca wildcard implícito | `TENANT` |
| `DefaultSafeValue` | Valor válido para el tipo, seguro ante ausencia o error; no concede permisos | `false` |
| `Constraints` | `min/max`, longitud, enum cerrado, regex estricta sin ejecución, cota de carga | `0..100` |
| `ReadPermission`, `WritePermission` | Permiso server-side explícito, tenant y rol activo; escritura de plataforma separada | `Configuracion.Ver/Editar` |
| `Sensitivity` | `Public` sólo si se publica expresamente, `PrivateTenant` por defecto; `Secret` no editable desde catálogo general | `PrivateTenant` |
| `SchemaVersion`, `ValueVersion` | Semántica de cambios de contrato y CAS; validación al leer/escribir | revisión monotónica positiva |
| `AuditPolicy` | Evento de mutación/denegación con actor, scope, key, resultado, sin valor secreto | audit on write |

Los nombres son **propuesta de diseño**, no DTO/tabla/servicio ya implementados. El `valueType` cliente nunca puede cambiar el tipo permitido. Para `SafeColorHex`, mantener el patrón hexadecimal estricto y contraste accesible del componente Angular existente, sin aceptar `var()`, `url()`, `expression()`, HTML o CSS arbitrario. Un campo tipo `SafeString` no se interpreta como código/HTML/SQL y requiere longitud y contexto de escape.

## 4. Precedencia y resolución de ámbito

Política futura segura: catálogo y default gobiernan; sólo se aplica override explícito del ámbito autorizado más específico según una jerarquía **definida por cada clave**, no una cascada universal. Si una clave sólo admite `TENANT`, se ignoran overrides de `BRANCH`/`RESOURCE`. Para claves con varios scopes, la definición enumera precedencia y un `OwnerId` vinculado al tenant realmente autenticado. Nunca se reutiliza override de empresa A al evaluar empresa B ni se permite global editable por membresía local.

- **Lectura positiva:** miembro activo de A con permiso solicita `ui.compactMode` de A; retorna override de A si permitido, o default seguro. Respuesta incluye scope efectivo, versión y procedencia no sensible.
- **Lectura negativa:** miembro B con URL `empresaId=A`, sucursal A o recurso huérfano: rechazo opaco sin indicar si existe una clave/config de A.
- **Concurrencia:** `expectedVersion` válido avanza revisión; obsoleto recibe 409 y no sobrescribe.
- **Fallo de proveedor/ambigüedad:** default restrictivo seguro o denegación según sensibilidad, nunca un valor del último tenant en caché.

Al cambiar tenant en Angular, limpiar tema antes de nueva respuesta y descartar payload tardío como el patrón inspeccionado. **Ese patrón frontend no reemplaza autorización del backend.**

## 5. Reglas de flags y excepciones prohibidas

Los flags controlan características de **experiencia o presentación no críticas** y funcionalidades aprobadas que ya respetan autorizaciones. La evaluación de `PlanModulo` exige suscripción vigente y regla de plan; aun cuando habilita un módulo, sigue siendo imprescindible permiso RBAC, membresía y ownership. Un flag no activa módulo comercial no autorizado ni permite acceso a otra empresa.

**Claves expresamente fuera del catálogo editable:** `disable_auth`, `disable_rbac`, `disable_tenant_isolation`, `disable_audit`, `disable_scope_lock`, `bypass_mfa`, `enable_sql_eval`, `secrets.smtpPassword`, `database.connectionString`, `worker.runForever`, variables que disparen Tasks, pagos, webhooks o despliegues. Cualquier intento se rechaza y se registra sin exponer credenciales. No se crean tales flags ni un dispatcher por redactar ejemplos.

## 6. Oráculos documentales positivos/negativos

| ID | Caso | Respuesta prevista en futura implementación |
| --- | --- | --- |
| P01 (+) | Admin A activo con permiso válido edita `ui.compactMode` Boolean autorizado en A | Nuevo valor tipado A, versión+auditoría; B no cambia |
| P02 (+) | Lector A consulta clave sin override | Default seguro tipado y scope correcto |
| P03 (+) | Plan activo + suscripción válida + grant RBAC, clave de función permitida | Se considera habilitada únicamente la capacidad permitida, sin escalada |
| P04 (+) | `SafeColorHex` `#112233` autorizado para branding | Valor aceptado, contraste/a11y resueltos por UI existente cuando aplique |
| N01 (-) | Empresa A intenta leer/editar owner B por `EmpresaId`, URL o header | DENY y sin información B |
| N02 (-) | Usuario sin membresía, revocado o sin permiso pero plan habilitado | DENY, cero efecto |
| N03 (-) | Cliente fija `type=Boolean` para clave de otro tipo | Rechazo por esquema servidor |
| N04 (-) | Valor fuera de rango, enum desconocido, null, tamaño excesivo | Error seguro, sin mutación |
| N05 (-) | `<script>`, `javascript:`, `var(--x)`, SQL literal o CSS libre | Rechazo/escape contextual, sin ejecución |
| N06 (-) | Versión obsoleta en una actualización concurrente | 409 y un solo ganador |
| N07 (-) | Solicitud de `disable_rbac`, `bypass_mfa` o `worker.runForever` | Clave reservada: DENY |
| N08 (-) | Un rol de administrador de empresa intenta modificar scope PLATFORM | DENY por rol global ausente |
| N09 (-) | Cambiar tenant mientras llega respuesta asíncrona antigua | No se aplica tema ni valor del tenant anterior |
| N10 (-) | Fallo de DB o auditoría durante update futuro | No escritura parcial, respuesta sin secreto |
| N11 (-) | Error/ausencia de plan, suscripción o módulo sin regla | Fail-closed; no activa funcionalidad |
| N12 (-) | Flag busca cambiar JWT, Scope Lock, secretos, CI o despliegue | DENY incondicional, sin autoridad nueva |

Estas pruebas son una **matriz de diseño escrita**, no xUnit/Playwright/HTTP/MySQL real. No atribuir PASS de runtime sin ejecutar jobs pertinentes del SHA de una futura implementación.

## 7. Impacto, dependencias, auditorías y reversión

- **Preflight:** `C01-03.M01.01–06` LISTO con A5 Javier, `DAG_TECNICO!A235:J235` F=VALIDADO/G=SI, `PLAN_MAESTRO!H227/I227` PENDIENTE/vacío y `COLA!G227/O227` SI antes de CAS; M02.01–03 están LISTO documental y sus A5 son independientes, no usados para habilitar gates sucesores.
- **Archivos intencionales:** este Markdown y sección autocontenida de `CHANGELOG_AI.md`; ni una línea de TS, C#, JSON runtime, SQL, EF migration, YAML, secret, pipeline o DB cambia.
- **A0:** cotejo de fuentes y blobs, diff exactamente documental, procedencia del CHANGELOG y ausencia de encabezados vacíos o secretos.
- **A1:** verificar esquema/precedencia y P01–P04/N01–N12 en texto, no ejecución de código.
- **A2:** revisar amenazas A/B, RBAC/tenant, secrets, inyección, revocación, cache/response tardía y flags reservados.
- **A3:** compatibilidad con `ThemeApplierService`, `PlanModulo`, `ConfigEmpresa` legacy y contratos M02.01–03, con separación explícita entre diseño y runtime.
- **A4:** commit directo FF/CAS en dev, `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` sobre SHA exacto y readbacks PLAN/COLA/BITACORA. Compilación backend/frontend, seguridad ejecutable, Docker/MySQL/E2E `NO_APLICA` al diff exclusivamente .md; jamás clasificarlos como PASS requeridos si siguen ejecutándose.
- **A5:** revisor de una **Task Javier distinta** a DEV04 debe certificar el SHA y límites, sin autoaprobación. QA/main/PROD requieren autorización separada.
- **Rollback:** `git revert` ordinario del documento en HEAD vigente tras verificar concurrencia, sin borrar BITACORA ni hacer reset/force-push.

**Condición de cierre:** definición contractual tipada/ámbitos/flags seguros escrita con evidencias y límites. M03 implementará sólo cuando su DAG lo permita y con CI real; aquí no se habilitaron workers, nuevas Tasks o servicios.
