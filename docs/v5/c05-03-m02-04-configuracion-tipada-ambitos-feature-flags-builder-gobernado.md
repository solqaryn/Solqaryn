# SOLQARYN V16 — C05-03.M02.04: configuración tipada, ámbitos y flags seguros del builder gobernado

**Microtarea:** C05-03.M02.04, PLAN_MAESTRO fila 803; owner PRIMARY DEV 05 JAVIER, TASK_ID `6adc2e55f47c81919f6374e561687076`; `solqaryn/Solqaryn@dev`; PROJECT_SCOPE_LOCK=STRICT; V15 **R0 DOCUMENTAL**, 2026-11-15.

**Autoridad única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; Sheet institucional PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA. Resultado exigido: **configuración tipada, ámbitos y feature flags seguros**. No se activan flags, endpoints, configuración, API, Angular, EF, MySQL, secretos, proveedores, DNS o QA/main/PROD.

## 1. Precedentes, no-alcance y seguridad

`C05-03.M01.06` conserva bloqueos BL01–BL16, rollback RB01–RB10, gates LG01–LG12 y decisiones D01–D08 PENDIENTES. `M02.01` propone E01–E12/INV01–INV16; `M02.02` API/DTO hipotéticos; `M02.03` persistencia, índices, migraciones y fixtures **futuros, no ejecutados**. Al claim PLAN H803=PENDIENTE/I803 vacío, COLA G803/O803=SI y DAG F764=VALIDADO/G764=SI. Esta elegibilidad no libera M03 de código real.

Toda autorización real está en backend ASP.NET Core 10, JWT/RBAC/membresía y propiedad del recurso verificados, nunca en Angular 22.2.1 ni en input editable del cliente. EmpresaId resuelto server-side, cache segmentada por tenant y host, ningún dato privado B visible a A. La ausencia de configuración, cadena inválida, flag desconocido o autorización revocada equivale a **DENY**, sin fallback permisivo.

## 2. Dieciséis campos tipados CFG01–CFG16 (NO CONFIGURADOS)

| ID | Campo y tipo propuesto | Ámbito y condición |
| --- | --- | --- |
| CFG01 | BuilderEnabled: bool | Plataforma, false por default |
| CFG02 | TenantBuilderEntitlement: bool | Tenant, false sin concesión explícita y RBAC backend |
| CFG03 | AllowedComponentVersions: lista cerrada | Plataforma, ComponentKey+Version exactos, no scripts |
| CFG04 | AllowedPropertySchemas: mapa tipado | Plataforma, unknown = deny |
| CFG05 | AllowedDesignPresets: enum | Plataforma, solo presets precompilados y CSP |
| CFG06 | AllowedPublicHosts: hosts verificados | Tenant/host, ownership server-side |
| CFG07 | MaxPageSections: entero acotado | Plataforma, exceso sin escritura |
| CFG08 | MaxSectionDepth: entero acotado | Plataforma, recursión bloqueada |
| CFG09 | MaxDraftBytes: entero acotado | Plataforma, payload grande rechazado |
| CFG10 | MaxPropertyBytes: entero acotado | Plataforma, no eval HTML/JS/SQL |
| CFG11 | PreviewLeaseSeconds: duración limitada | Tenant/actor, expira o se revoca |
| CFG12 | PublicationApprovalRequired: bool | Plataforma, true hasta nueva autoridad |
| CFG13 | RevisionCasRequired: bool | Plataforma, true y 409 si ETag obsoleto |
| CFG14 | CacheTenantHostIsolation: bool | Plataforma, true; sin mezcla A/B |
| CFG15 | AuditRetentionPolicy: enum/plazo | Plataforma, metadatos mínimos sin PII |
| CFG16 | SafeUrlPolicy: lista esquema/destino | Plataforma, HTTPS aprobado; file/javascript/SSRF deny |

Los umbrales y listas **no constituyen valores aprobados**: siguen sujetos a D01–D08, análisis de uso y pruebas futuras. Ninguna fila autoriza cambiar appsettings, CONFIG, environment, servidor ni BD.

## 3. Doce feature flags FF01–FF12 propuestos, todos OFF

| ID | Capacidad futura | Condición antes de permitir |
| --- | --- | --- |
| FF01 | CatalogRead | Allowlist versionada y rechazo de unknown |
| FF02 | DraftRead | JWT/RBAC/tenant y lectura A/B negativa |
| FF03 | DraftWrite | Propiedad, CAS, cuota, auditoría |
| FF04 | SectionEdit | Schema validado, orden y profundidad acotados |
| FF05 | DesignPreset | Tokens permitidos, sin CSS libre |
| FF06 | PrivatePreview | Read-only, lease limitado, revocación |
| FF07 | ApprovalWorkflow | Reviewer autorizado y revisión vigente |
| FF08 | ControlledPublish | Approval, host verificado, atomicidad |
| FF09 | PublicProjection | Solo proyección mínima segmentada |
| FF10 | HostBinding | Prueba server-side, no confiar en Host header |
| FF11 | RevisionRollback | Historial aditivo, permiso explícito |
| FF12 | AuditExport | Actor autorizado, redacción PII y límites |

Cada flag queda conceptualmente en false: no existe modificación de código, Sheet CONFIG o proveedor. **La conjunción de flag, entitlement y rol es condición necesaria, nunca suficiente para omitir autorización sobre recurso, tenant, revisión, host o revocación.**

## 4. Ocho precedencias restrictivas PREC01–PREC08

| ID | Regla fail-closed futura |
| --- | --- |
| PREC01 | Scope Lock/NO PROD prevalece sobre todas las banderas |
| PREC02 | Política de seguridad/plataforma prevalece sobre personalización tenant |
| PREC03 | Default faltante/indeterminado = denegar sin fallback |
| PREC04 | Entitlement tenant solo puede restringir policy plataforma |
| PREC05 | JWT/RBAC y propiedad por recurso prevalecen sobre visibilidad UI |
| PREC06 | Revocación de actor invalida lease/caché/permiso previo |
| PREC07 | CAS/ETag de revisión vigente prevalece sobre valor obsoleto |
| PREC08 | Error de deserialización, versión/host desconocido = deny y auditoría mínima |

## 5. Doce escenarios positivos/negativos T01–T12 FUTUROS_NO_EJECUTADOS

| ID | Positivo futuro | Negativo/limite futuro |
| --- | --- | --- |
| T01 | Plataforma y tenant autorizados ven editor | Tenant no habilitado no edita |
| T02 | Componente allowlisted usa props tipadas | Versión desconocida se rechaza |
| T03 | Texto legítimo se escapa | HTML/JS handler nunca se ejecuta |
| T04 | URL pública aprobada es dato | file/javascript/IP privada/SSRF denegado |
| T05 | Borrador propio de A se puede leer | A no lee draft del tenant B |
| T06 | Payload dentro de cuotas se acepta | Exceso bytes/profundidad no escribe |
| T07 | Preview lease vigente read-only | Lease expirado/revocado deniega |
| T08 | Rol y aprobación vigentes publican | Rol revocado/ETag stale fallan |
| T09 | Host propio muestra mínimo público | Host/cache B no filtran A |
| T10 | Rollback autorizado preserva historia | Sin rol o hard-delete se bloquea |
| T11 | Flag ausente deshabilita builder | Flag true del cliente no autoriza |
| T12 | Auditoría conserva correlación | No registra PII, secretos ni IDs ajenos |

**12 pares descritos, 0 ejecutados**; no confundir especificación con pruebas backend, browser o MySQL. La implementación futura R2 deberá acreditar positivos, negativos, bordes, error, revocación, A/B, xUnit, Category=Integration MySQL 8.4 efímero si hay EF/DB y Playwright/Chromium si hay UI.

## 6. A0–A5, efectos, pruebas y rollback

- **A0:** alcance M02.04, CFG01–16, FF01–12, PREC01–08, T01–12; diff restringido a este Markdown y CHANGELOG_AI.md con encabezado/viñetas autocontenidos.
- **A1:** cotejo documental de tipados, defaults deny, límites y escenarios P/N; sin tests ejecutados.
- **A2:** JWT/RBAC, tenancy A/B, revocación, XSS/SSRF, CAS/IDOR, hosts, PII definidos como controles futuros. NO_APLICA test runtime al diff R0.
- **A3:** trazabilidad M01.06/M02.01–03; API, .NET/Angular, MySQL, Playwright y proveedor NO_APLICA por ausencia de runtime.
- **A4:** commit fast-forward/CAS `dev`, `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` completed/success SHA causal, readback PLAN/COLA/DAG/BITACORA A:Z.
- **A5:** revisor Javier de TASK_ID distinto del autor; no autoprobación ni liberación de M03/QA/main/PROD.

**Reversa documental:** `git revert` normal tras verificar HEAD y concurrencia, BITACORA intacta, sin force-push. BL/LG y D01–D08 siguen NO RESUELTOS.
