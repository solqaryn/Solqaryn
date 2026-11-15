# SOLQARYN V5 — C03-02.M02.01: modelo de entidades, ownership e invariantes RBAC/ABAC

**Autor y owner:** PRIMARY DEV 06 JAVIER, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Fecha:** 2026-11-15. **Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Autoridad exclusiva:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL; PLAN_MAESTRO/COLA/DAG_TECNICO; Skill canónica y AGENTS.md.

**Naturaleza de la entrega:** modelo conceptual M02.01. Define actores, objetos, ownership, relaciones y invariantes para implementación posterior. **No introduce entidades EF, tablas, migraciones, controladores, middleware, permisos, configuraciones, workers, eventos ni nuevas rutas HTTP.** No certifica JWT/RBAC/ABAC runtime. Los oráculos son FUTUROS_NO_EJECUTADOS.

## 1. Gate causal de entrada

El claim inicial de `C03-02.M02.01` cumplió PLAN H476=PENDIENTE, I476 sin owner; COLA G476=SI/O476=SI; DAG_TECNICO F447=VALIDADO/G447=SI. Se atribuyó owner exclusivo por CAS del estado y lectura posterior. El predecesor explícito del DAG es el grupo `C03-02.M01.01–06+A5`; se leyeron sus seis estados LISTO y revisiones A5 ajenas vigentes, incluida la revisión independiente de `C03-02.M01.06` completada antes de este claim. Esto habilita **únicamente** este diseño M02, no M03 ni cambios de permisos.

Fuentes relevantes: `docs/v5/c03-02-m01-06-bloqueo-rollback-liberacion-rbac-abac.md`, los cinco documentos M01.01–05, `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, `.agents/skills/solqaryn-project-governance/references/architecture.md` y el documento institucional V5. Las referencias a `TenantPermissionContext`, `RequierePermisoAttribute`, `VerifiedCommercialTenant` y `PlatformSupportPolicy` describen código existente localizado por M01; **no** prueban una cobertura transversal de todos los endpoints.

## 2. Entidades lógicas propuestas y autoridad de origen

| ID | Objeto de contrato | Atributos mínimos y autoridad | Relación e invariante |
| --- | --- | --- | --- |
| E01 | AuthenticatedPrincipal | identidad de usuario, sesión, issuer y vencimiento verificados por backend | Ningún claim de cliente crea rol o empresa efectiva |
| E02 | TenantMembership | usuario, empresa, vigencia, estado y fuente del servidor | Una membresía revocada no sigue autorizando con JWT vigente |
| E03 | EnterpriseRole | rol empresarial, empresa, estado, versión | Un rol global o legacy no crea automáticamente grants |
| E04 | ActionGrant | rol, módulo, acción, condición y vigencia | Denegar toda acción no expresamente concedida al contexto |
| E05 | AuthorizedTenantContext | empresa efectiva, membresía validada y request correlator | `X-Empresa-Id` sólo selecciona contexto, jamás confiere autoridad |
| E06 | BranchBinding | sucursal, empresa, pertenencia, estado | La sucursal efectiva debe pertenecer al tenant autorizado |
| E07 | ResourceOwnership | recurso opaco, empresa, sucursal opcional, titular y versión | BOLA/IDOR: un ID ajeno no permite leer ni escribir su recurso |
| E08 | ResourceOperation | recurso, acción, actor, versión esperada | Propiedad + grant + condición ABAC deben resolverse juntos |
| E09 | DataExportPermit | sujeto, empresa, módulos, alcance, caducidad | Export CSV/XLSX/PDF filtra filas y campos antes de serializar |
| E10 | MediaAccessPermit | propietario de objeto, tipo, expiración y permiso | No revelar URL firmada ni metadatos sin revalidación |
| E11 | SupportReadGrant | actor soporte, empresa exacta, método GET, objeto allowlist y vencimiento | Soporte no adquiere POST/PUT/DELETE ni acceso global por fallback |
| E12 | AuthorizationDecisionAudit | actor opaco, scope, recurso hash, política, motivo y UTC | Append-only, sin JWT, PII, secretos ni detalles de tenant ajeno |
| E13 | PolicyVersion | módulo, versión, vigencia y precedencia restrictiva | Política desconocida o conflictiva produce DENY |
| E14 | RevocationState | usuario, membresía, grant, versión y timestamp | Invalidación inmediata en evaluación posterior, sin caché permisiva |
| E15 | ScopeLease | request/tenant context aislado, id efímero y caducidad | Ninguna variable singleton mezcla autoridad A y B |
| E16 | SafeErrorContract | código 401/403/404, correlación opaca y redacción | No confirmar existencia de recurso B ni exponer stacktrace |

**No son tablas existentes, clases compiladas o DTO ejecutables**. Son contratos de propiedad para refinar en M02.02/M02.03; ningún campo se usará como permiso por ser presentado desde navegador, URL, header, cookie o configuración local.

## 3. Relaciones, composición y reglas fail-closed

- **R01:** `AuthenticatedPrincipal → TenantMembership → AuthorizedTenantContext` exige identidad y membresía activa en cada solicitud protegida.
- **R02:** `TenantMembership → EnterpriseRole → ActionGrant` exige empresa y grants coincidentes; el plan o entitlement SaaS puede **restringir** operación, nunca elevar un permiso denegado.
- **R03:** `AuthorizedTenantContext → BranchBinding → ResourceOwnership` exige empresa y sucursal de ambos extremos; un recurso con propietario ausente es NO_GO.
- **R04:** `ResourceOperation` se permite sólo tras comprobar identidad, grant, recurso, sucursal, versión y condición ABAC de la misma operación; no se reutiliza un veredicto para otra solicitud.
- **R05:** `DataExportPermit`/ `MediaAccessPermit` revalida ownership antes de extraer bytes o generar enlaces; filtros se aplican antes de proyectar datos.
- **R06:** `SupportReadGrant` restringe identidad, empresa, módulo y GET, impide escritura y caduca; la plataforma no es un superusuario por omisión.
- **R07:** `PolicyVersion` y `RevocationState` prevalecen sobre caché o claims obsoletos; política ausente, conflictiva o expirada deniega.
- **R08:** `AuthorizationDecisionAudit` y `SafeErrorContract` registran decisiones minimizadas; no exponer resource ID ajeno, permisos privados o credenciales.
- **R09:** el resultado de autorización queda asociado a un solo request y tenant, nunca a estado mutable compartido, SPA o variable estática.
- **R10:** el error de DB, timeout o fallo de auditoría no produce grant ni mutación parcial; la implementación exigirá transacción y aislamiento verificables.
- **R11:** JWT firmado y vigente es necesario, pero insuficiente sin membresía, RBAC y propiedad actuales.
- **R12:** Scope Lock, A0–A4 exact-SHA y A5 independiente son gates del desarrollo; las señales y bitácoras no sustituyen pruebas reales.

Las capas conservan `Domain ← Application ← Infrastructure`: Domain expresa invariantes sin EF/HTTP; Application orquesta autorización; Infrastructure resuelve persistencia MySQL 8.4 y referencias a medios; API aplica middleware y devuelve códigos seguros. Angular 22.2.1 sólo presenta UX/guards, no es autoridad. Cualquier implementación futura revisará el provider MySql.EntityFrameworkCore 10.0.9 y EF Core 10.0.12 antes de migrar, sin tocar producción.

## 4. Oráculos positivos y negativos FUTUROS_NO_EJECUTADOS

| Caso | Resultado positivo futuro | Negativo que debe denegarse |
| --- | --- | --- |
| P01/N01 | Usuario A con membresía, rol, grant y recurso A lee metadatos permitidos | JWT A accede ID recurso B (BOLA/IDOR) |
| P02/N02 | Sucursal A1 autorizada puede actualizar sólo recurso A1 con versión vigente | Sucursal A2 accede recurso A1 sin grant |
| P03/N03 | Revocación de rol bloquea la segunda solicitud protegida | JWT válido conserva privilegio después de revocación |
| P04/N04 | Exportación autorizada contiene sólo filas A y campos permitidos | CSV/PDF de A incluye filas B por filtro delegado a UI |
| P05/N05 | URL de medio A se firma tras permisos actuales y caduca | Usuario B obtiene URL de bytes A por identificador |
| P06/N06 | Soporte autorizado realiza GET allowlisted y audita el acceso | Soporte ejecuta PUT o consulta otro módulo |
| P07/N07 | Dos requests A/B en paralelo mantienen aislamiento de contexto | Caché compartida devuelve grants o datos cruzados |
| P08/N08 | Error de recurso inexistente/denegado es opaco y auditado | Respuesta revela existencia o nombre de recurso B |
| P09/N09 | Grant explícito limita a una acción/módulo/empresa | Plan premium o flag activa permiso RBAC ausente |
| P10/N10 | Token legítimo + membresía validada + versión vigente permite evaluación | Claim forjado o `X-Empresa-Id=B` confiere autoridad |
| P11/N11 | Regla desconocida deniega con razón saneada y correlation ID | Falta de política concede `allow` por defecto |
| P12/N12 | Candidato M02 conserva source SHA, CI y A5 independientes | Documento se presenta como prueba HTTP, MySQL o QA ejecutada |

La tabla anterior es **diseño de escenarios**, no resultados de xUnit, HTTP, JWT real, MySQL, Playwright o navegador. M03 exigirá código y suites dirigidas con negativos entre empresas A/B, BOLA/IDOR, exportación, revocación y rollback. No hay autorización para nuevas políticas, servicios, migraciones, QA, main, PROD ni Cloudflare.

## 5. Integración, riesgos y rollback

**Dependencias y consumidores futuros:** los contratos E01–E16 deben revisarse en M02.02 para DTO, errores y compatibilidad; M02.03 para ownership FK, índices y fixtures; M02.04 para configuración tipada; M02.05 para correlación y límites de eventos **hipotéticos**, sin implementarlos. M02.06 solo puede cerrarse si su DAG y A5 reales lo permiten. No inferir habilitación de fases M03–M06 ni alterar COLA/DAG a mano.

**Riesgos abiertos:** token o membresía caducada, claim falsificado, permisos globales de soporte, recurso sin empresa, sucursal cruzada, exportación filtrada tras serializar, enlaces de medios reutilizables, caché cross-tenant, logging de PII y migraciones sin rollback. La implementación exigirá tests positivos/negativos causales, aislamiento y una estrategia de revert/restore sobre servicios efímeros.

**A0:** diff limitado a este Markdown y sección autocontenida de `CHANGELOG_AI.md`, sin cambios de runtime, secretos o arquitectura. **A1:** revisar las 16 entidades, 12 invariantes y 12 pares positivos/negativos como oráculos futuros, no tests ejecutados. **A2:** verificar diseño fail-closed de JWT/RBAC/ABAC, empresa, sucursal, archivos, soporte y PII. **A3:** contrastar M01.01–06 LISTO+A5, stack y referencias existentes; compilación .NET/Angular, MySQL y Playwright = NO_APLICA a diff Markdown, nunca PASS inventado. **A4:** publicar fast-forward/CAS en `dev`, verificar SHA/blob/diff, Scope Lock y procedencia V5 `completed/success` exact-SHA, eventos BITACORA y readbacks PLAN/COLA/DAG. **A5:** requerir revisión independiente por otra Task Javier distinta del autor; no autoaprobar.

**Rollback documental:** `git revert` normal preservando cambios concurrentes y BITACORA histórica, con evento compensatorio si corresponde. No hay transacciones, SQL, pagos, archivos externos, webhooks, workers, DNS, despliegues o datos productivos que revertir en esta entrega.
