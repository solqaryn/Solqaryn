# SOLQARYN V5 FINAL — C02-01.M02.04: configuración tipada, ámbitos y feature flags del contexto tenant

**Naturaleza:** contrato DOCUMENTAL M02.04; no cambios ejecutables ni nuevos grants. **Autor:** PRIMARY DEV 06 — JAVIER_CHATGPT, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Scope:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA` y PLAN/COLA/DAG/BITACORA del Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`.

## 1. Objetivo y frontera de esta microtarea

Definir las **opciones tipadas** que condicionan el contexto multitenant, los ámbitos permitidos, su precedencia y flags seguros, sin crear un mecanismo que seleccione arbitrariamente empresa, rol o tenant por parámetro de navegador. Esta entrega fija un contrato para implementaciones futuras de tenant control plane; NO agrega tablas, migraciones, middleware, dominios, proxies, JWT claims, endpoints, DTOs, variables, flags activados, código de frontend, workers, webhooks o eventos. No cambia `main`, QA, PROD, Aiven, Vercel, Render, DNS o configuración de secretos.

Se complementan los contratos existentes del mismo lote: M02.01 entidades/ownership, M02.02 API/DTO/errores y M02.03 persistencia y fixtures. El siguiente M02.05 es de eventos/correlación, todavía distinto; M03 debe crear y demostrar comportamientos reales en el runtime por DAG propio. **El diseño documental no equivale a pruebas HTTP/MySQL/JWT ni a un sistema implementado.**

## 2. Fuentes actuales y límites contrastados

| Fuente vigente | Evidencia observada en contratos M02 anteriores | Restricción que conserva M02.04 |
| --- | --- | --- |
| `backend/src/Domain/Entities/UsuarioEmpresa.cs` | `UsuarioId`, `EmpresaId`, `RolId`, `Activa`; blob `f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c` | La membresía/rol exactos determinan acceso; un flag nunca los amplía |
| `backend/src/Application/Security/VerifiedCommercialTenant.cs` | Marca de servidor; blob `2da140ce9d7f47329d39d292d3271596467910cc` | Ningún header, query, cookie o host arbitrario puede crear la marca |
| `backend/src/API/Controllers/TenantContextController.cs` | Lista selectiva y GET contextual bajo autorización; blob `2407c2eb6779ed724e841c688ee14dabbf163171` | No se agregan rutas ni se presume cobertura universal de endpoints |
| `backend/src/Infrastructure/Persistence/Configurations/UsuarioEmpresaConfiguration.cs` | FK e índice único usuario/empresa; blob `adb6f7db929ecafcfb101031b4da23595477388b` | Índice relacional no demuestra permiso de lectura/escritura ni cache tenant segura |
| `backend/src/Infrastructure/Persistence/Configurations/EmpresaConfiguration.cs` | Índices de `Empresa` y `Activa`; blob `7000321f5e4914e92c7a08fb526c83c622e4c82f` | No existe por inferencia un mapping host->tenant universal |
| `docs/v5/c02-01-m02-01-entidades-ownership-invariantes-tenant.md` | Modelo conceptual e invariantes I01–I10 | El contrato de flags se subordina a IAM, pertenencia y propiedad real |
| `docs/v5/c02-01-m02-02-api-dto-errores-compatibilidad-tenant.md` | Errores seguros, rutas vigentes vs futuras | La UI no crea permisos; mantener compatibilidad de `/tenant-context/*` |
| `docs/v5/c02-01-m02-03-persistencia-indices-migraciones-fixtures-tenant.md` | Índices existentes y posibles migraciones condicionadas | No crear tabla/DDL ni certificar host binding en esta fase |

Estas fuentes describen **código existente inspeccionado en M02.01–03**; sus SHA sirven como trazabilidad de los contratos, no certifican cobertura transversal de todas las rutas o datos físicos de producción. Si un componente real discrepa al implementar, prevalece el código + A0–A4 exact-SHA y se corrige en scope posterior con rollback.

## 3. Catálogo tipado y ámbito autorizado (propuesto, no implementado)

Cada clave del futuro control plane debe estar definida en allowlist del servidor con `KeyCanonical`, `ValueKind`, `AllowedScopes`, `DefaultSafeValue`, `ReaderPermission`, `WriterPermission`, `MaxLength/Range`, `Sensitivity`, `SchemaVersion` y `OwnerResolutionPolicy`. Claves desconocidas o valores inválidos: **denegar**, nunca ejecutar contenido como SQL, HTML, scripts, plantillas o políticas RBAC.

| Clave conceptual (no activada) | Tipo, ámbito y default seguro | Límite de uso |
| --- | --- | --- |
| `tenant.contextSelectionEnabled` | Boolean, `PLATFORM_POLICY`, falso para flujos nuevos no certificados | No habilita a usuarios ajenos ni elude membresía activa |
| `tenant.allowedHostBinding` | Registro estructurado administrado por plataforma, NO string editable por tenant | Host exacto sólo de proxy confiable y mapping único aprobado; nunca wildcard dinámico |
| `tenant.strictResourceOwnership` | Boolean de seguridad reservado, servidor; default **true** | No editable por roles de empresa, no admite false para relajar filtros |
| `tenant.contextCacheMaxSeconds` | Entero acotado, política de servidor; default conservador | TTL no supera revocación; revalidación obligatoria en operación sensible |
| `tenant.publicBrandVisibility` | Enum cerrado `HIDDEN | PUBLIC_ALLOWLISTED`, `TENANT`, default HIDDEN | Sólo campos de branding explícitamente públicos y sin PII/secretos |
| `tenant.navigationDensity` | Enum cerrado `COMPACT | COMFORTABLE`, `TENANT/USER_PREFERENCE`, default COMFORTABLE | Preferencia UI, no autenticación, sin CSS/HTML libre |
| `tenant.optionalModuleVisible` | Boolean `TENANT`, default false | Afecta navegación, nunca derecho de ejecutar endpoint ni suscripción |

Los nombres precedentes son **diseño futuro**. No existen nuevos flags efectivos por redactarlos. El esquema físico y las cuotas finales deben validarse en M03/M05 y pasar revisión de seguridad, sin exponer el catálogo reservado a `GET` público.

## 4. Resolución de contexto y precedencia fail-closed

1. Validar autenticación (JWT vigente y usuario activo) y resolver membresías activas desde servidor. Para rutas públicas, servir sólo proyección allowlisted sin sesión de tenant privado.
2. Resolver el tenant solicitado como **selección no autoritativa** y verificar empresa activa, membresía del actor, rol empresarial efectivo y autorización de recurso/sucursal. Para host/slug, exigir mapping registrado, único y proxy confiable antes de resolver; si no existe, no elegir empresa por defecto.
3. Evaluar el catálogo de claves tipadas con `AllowedScopes` explícitos: `PLATFORM_POLICY` (sólo administración de plataforma autorizada), `TENANT` (empresa validada), `BRANCH` (sucursal propia) y `USER_PREFERENCE` (actor autenticado ligado a tenant validado). El ámbito global no se degrada a tenant editable.
4. Aplicar restricciones de seguridad inmutables, RBAC y pertenencia **antes y después** de valores de presentación. Los defaults niegan capacidades sensibles. Un entitlement comercial habilita como máximo una capacidad adicional ya autorizada; jamás concede rol ni exime de JWT.
5. Ante pérdida de membresía, cambio tenant, cambio rol, logout, caché obsoleta, proxy ambiguo o respuesta tardía de UI: invalidar selección/caché y revalidar antes de consulta/mutación. Nunca renderizar ni devolver datos del tenant A tras pasar a B.
6. Versionar cualquier valor por scope y exigir revisión esperada (CAS); un cambio de preferencia o flag no modifica roles ni audit logs. Mantener resultados y logs minimizados y sin secretos.

**Principio de no ampliación:** la combinación de `featureFlag=true`, plan activo, header `X-Tenant` y `EmpresaId` ajeno produce **denegación** si falta pertenencia/RBAC real. Invariantes de seguridad no admiten override en configuración editable.

## 5. Modelo de aceptación/rechazo (simbólico, sin ejecutar tests)

| ID | Tipo | Escenario y aserción futura |
| --- | --- | --- |
| P01 | POS | Usuario A con membresía A válida consulta preferencia UI A: sólo datos A, tipo validado |
| P02 | POS | Usuario A/B cambia contexto con membresías ambas vigentes: nueva evaluación de permisos, sin reutilizar A |
| P03 | POS | Selector `navigationDensity=COMPACT` válido: cambia sólo experiencia visual, nunca permiso |
| P04 | POS | Lectura de marca pública explícitamente allowlisted: sin secretos, sin scope privado |
| N01 | NEG | `X-Tenant=B` o ruta B para miembro exclusivo A: denegar sin revelar B |
| N02 | NEG | Flag de UI habilitado + JWT inválido: acceso a datos privados denegado |
| N03 | NEG | Suscripción activa pero rol RBAC sin permiso: denegar operación |
| N04 | NEG | Membresía revocada o usuario desactivado con caché anterior: denegar y limpiar selección |
| N05 | NEG | Host desconocido, wildcard, forwarded host falsificado o mapping ambiguo: fail-closed |
| N06 | NEG | Valor contiene JavaScript/CSS/SQL arbitrario, clave no allowlisted o enum inválido: rechazar |
| N07 | NEG | `strictResourceOwnership=false` desde rol empresarial: rechazar cambio reservado |
| N08 | NEG | `contextCacheMaxSeconds` negativo, demasiado grande o NaN: rechazar sin fallback permisivo |
| N09 | NEG | Respuesta tardía de A llega al cliente en contexto B: descartar, sin mostrar información A |
| N10 | NEG | `expectedVersion` obsoleta o scope/owner incorrecto: conflicto sin escritura |

Estas filas son **oráculos documentales P01–P04/N01–N10**, no resultados de xUnit, HTTP, Playwright ni MySQL. La implementación M03 requiere casos de prueba de pertenencia revocada, A/B, rol, host malicioso, cache invalidada y versiones, además de regresión CI del SHA.

## 6. Riesgos, dependencia, evidencia y rollback

- **R01 — Spoofing de host/header:** ningún valor remitido por cliente/forwarded no confiable es fuente de tenant. **R02 — RBAC implícito:** flag y plan nunca otorgan permisos. **R03 — Caché entre empresas:** invalidación al cambiar contexto y revalidación obligatoria. **R04 — Datos legacy sin owner:** no migrar/duplicar sin reconciliación, autorización y respaldo. **R05 — Configuración inyectable:** catálogo tipado cerrado, límites estrictos y secretos fuera de valores.
- **Gate de esta unidad:** C02-01.M01.01–06 en LISTO con A5 independiente; `PLAN_MAESTRO!H299=PENDIENTE`, `I299` vacío, `COLA!G299/O299=SI` y DAG exacto `F=VALIDADO/G=SI` fueron comprobados **antes** de la reclamación CAS exclusiva. Otros lotes y fases conservan su DAG; este documento no los libera.
- **Verificación A0–A4 documental:** diff delimitado a este Markdown y CHANGELOG, contrato tipado sin secretos, matriz simbólica 4/10 inspeccionada, consistencia con M02.01–03, Scope Lock/V5 provenance exact-SHA y readback PLAN/COLA/BITACORA. Pruebas de backend/frontend/MySQL que no guardan causalidad con Markdown: `NO_APLICA` justificado, no PASS inventado.
- **Salida:** estado LISTO_DEV tras las evidencias propias, A5_PENDIENTE por Task Javier distinta; ningún pase a QA/main/PROD, ninguna autorización de desplegar hosts, JWT o migración. **Rollback de este documento:** `git revert` normal y nuevo readback, sin borrar la BITACORA histórica. Los cambios futuros requieren reversión técnica específica.
