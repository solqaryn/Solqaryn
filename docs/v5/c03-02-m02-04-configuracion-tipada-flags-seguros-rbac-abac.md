# SOLQARYN V5 — C03-02.M02.04: configuración tipada, ámbitos y feature flags RBAC/ABAC

**Responsable:** PRIMARY DEV 01 JAVIER (`6adbb9fff1548191a1bd5b30d9e9984a`). **Fecha:** 2026-11-15. **Repositorio:** `solqaryn/Solqaryn@dev` · **Scope Lock:** STRICT. **Autoridad:** STANDARD V5 FINAL, PLAN_MAESTRO/COLA/DAG_TECNICO y Skill canónica.

## Objeto y exclusiones

Especificar **contratos futuros** de configuración tipada y feature flags para acceso seguro RBAC/ABAC, sin modificar JWT, permisos relacionales, políticas, cachés, roles, controllers, DTO ejecutables, Angular, tablas, migraciones ni entornos. Estos objetos **NO EXISTEN COMO CONFIGURACIÓN RUNTIME por esta microtarea**. No activar rutas, soporte global, exports, medios, secretos, jobs ni QA/main/PROD.

La precondición del DAG es `C03-02.M01.01–06+A5` LISTO con revisiones independientes; esta M02.04 tenía PLAN PENDIENTE/I sin responsable, COLA G/O=SI y DAG F=VALIDADO/G=SI. M02.03 de otro owner es independiente por DAG y no se altera. Fuentes del diseño: `docs/v5/c03-02-m02-01-modelo-entidades-ownership-invariantes-rbac-abac.md`, `docs/v5/c03-02-m02-02-contratos-api-dto-errores-compatibilidad-rbac-abac.md`, M01.06 y `docs/PROJECT_SCOPE_LOCK.md`. Los nombres `TenantPermissionContext`, `RequierePermisoAttribute`, `VerifiedCommercialTenant` y `PlatformSupportPolicy` son puntos de código **a verificar antes de M03**, no permisos ya certificados globalmente.

## Precedencia y reglas de seguridad

El orden futuro es `gobierno y Scope Lock` → `identidad JWT verificada` → `membresía/estado/revocación actual` → `tenant y sucursal persistidos` → `RBAC relacional` → `atributos ABAC de recurso/operación` → `capacidad de plan como límite adicional` → `presentación UI`. **DENY domina siempre**: ninguna flag, claim, header, configuración de cliente, entitlement SaaS o usuario soporte transforma denegación en permiso. Error, ausencia, expiración, versión desconocida o excepción provocan denegación auditable y respuesta saneada.

## Dieciséis campos conceptuales CFG01–CFG16 — NO IMPLEMENTADOS

| ID | Clave tipada futura | Default / ámbito / condición |
| --- | --- | --- |
| CFG01 | `authorization.policyVersion` | string versionada obligatoria; ausente = deny |
| CFG02 | `authorization.tenantContextSource` | enum `BACKEND_VERIFIED`; otro valor = deny |
| CFG03 | `authorization.membershipFreshness` | duración limitada server-side; revocación prevalece |
| CFG04 | `authorization.branchScopeMode` | enum `STRICT`; sucursal ajena = deny |
| CFG05 | `authorization.permissionEvaluation` | enum `SERVER_ONLY`; nunca UI |
| CFG06 | `authorization.unknownActionPolicy` | enum `DENY`; no valor permisivo |
| CFG07 | `authorization.ownerNullPolicy` | enum `DENY`; legacy NULL no se asigna |
| CFG08 | `authorization.resourceVersionRequired` | boolean true para mutación |
| CFG09 | `authorization.auditRedaction` | enum `STRICT`; sin JWT/PII |
| CFG10 | `authorization.supportReadOnly` | boolean true; no eleva grants |
| CFG11 | `authorization.supportScopeTtlSeconds` | entero acotado; 0 = deny |
| CFG12 | `authorization.exportMaxRows` | entero acotado, 0 = deny |
| CFG13 | `authorization.mediaUrlTtlSeconds` | entero acotado, 0 = deny |
| CFG14 | `authorization.revocationEnforced` | boolean true invariable |
| CFG15 | `authorization.crossTenantFallback` | boolean false invariable |
| CFG16 | `authorization.writeEffectsEnabled` | boolean false en este contrato |

No colocar dichas claves en `appsettings`, bases, flags de LaunchDarkly u otros servicios mediante esta tarea. Una futura implementación deberá demostrar integridad de firma/versionado, fuente de configuración y pruebas positivas/negativas, sin confiar en valores aportados por navegador o token no validado.

## Doce flags propuestas, todas desactivadas

| ID | Flag hipotética | Default y frontera |
| --- | --- | --- |
| F01 | `ShowCurrentPermissionsSummary` | false; solo alcance redactado |
| F02 | `ShowResourcePolicyHints` | false; nunca ocultar denegación backend |
| F03 | `EnableAuthorizationDryRun` | false; lectura sintética, sin efectos |
| F04 | `ShowDeniedActionReason` | false; error opaco sin existencia ajena |
| F05 | `EnableSyntheticTenantABPreview` | false; no datos reales |
| F06 | `ShowBranchBindingPreview` | false; no asignar sucursal |
| F07 | `ShowGrantRevocationStatus` | false; leer estado saneado |
| F08 | `EnableExportPolicyPreview` | false; no exportar filas |
| F09 | `ShowMediaGrantPreview` | false; no generar URL firmada |
| F10 | `EnableSupportReadOnlyPreview` | false; sin acceso global |
| F11 | `ShowPolicyVersion` | false; metadatos no secretos |
| F12 | `ShowA5AuthorizationAuditTrail` | false; sin aprobar revisión propia |

Las doce banderas son documentación de diseño: no se instalaron, registraron, ejecutaron ni cambiaron en ningún entorno. Todas requieren permiso backend anterior a visibilidad; ninguna equivale a un grant, ni a aprobación QA/PROD.

## Doce pares positivos/negativos FUTUROS_NO_EJECUTADOS

| Caso | Positivo futuro | Negativo obligatorio |
| --- | --- | --- |
| PN01 | JWT backend válido permite evaluar una acción propia | JWT expirado o no firmado recibe 401 |
| PN02 | Membresía A vigente permite acción A con grant | Revocación A impide acceso aun con token válido |
| PN03 | Recurso A con owner confirmado se devuelve al actor A | Actor B conoce ID A y obtiene denegación sin fuga |
| PN04 | Sucursal A válida pertenece al tenant A | Sucursal B en header no confiere acceso |
| PN05 | Permiso relacional y condición ABAC coinciden | Solo rol UI o entitlement SaaS intenta dar grant |
| PN06 | Contexto soporte GET allowlisted y vigente | Soporte intenta escribir o expandir empresa |
| PN07 | Export propio aplica filtro antes de serializar | Export con tenant ajeno/columna PII se bloquea |
| PN08 | Media propia entrega enlace temporal autorizado | Recurso B o TTL no válido no revela URL |
| PN09 | Política tipada válida conserva defaults estrictos | Flag desconocida intenta habilitar escritura |
| PN10 | Registro de denegación conserva correlación saneada | Token/correo/PII aparece en log o payload |
| PN11 | Concurrencia usa versión actual y auditoría | Versión obsoleta/owner NULL produce rechazo |
| PN12 | CI completed/success exact SHA avala prueba real | Cancelled/skipped/in_progress se atribuye PASS |

Estos pares no se ejecutaron. En M03/M05 se deben preparar fixtures sintéticos A/B, roles, revocación y sucursales, request/response real de backend, resultados de xUnit/HTTP con SHA y logs saneados. No confundir documento con pentesting, control automático de autorización o revisión WCAG.

## Contrato de evolución, comprobación y reversión

1. Identificar tipos/rutas reales y consumidores; no asumir un servicio nuevo a partir de una tabla de diseño.
2. Resolver configuración **en servidor** por tenant autenticado, permiso relacional y recurso persistido, sin inputs elevadores.
3. Validar negativas de aislamiento, revocación, sucursal, soporte, export, medios y política desconocida, con CI dirigida.
4. Bloquear migraciones/flags runtime hasta autorización y DAG M03 válidos; sin edición de secretos ni proveedor.
5. Aplicar un cambio futuro aditivo y reversible con métricas y logs redactados; no alterar auditabilidad histórica.
6. Ante FAIL: conservar owner y VALIDANDO, revisar SHA/logs, corregir causa sin apagar analizadores ni forzar SI; rollback normal de Git para esta documentación.

## A0–A5 y salida

**A0:** dos Markdown (este artefacto y sección autocontenida CHANGELOG), no código ni datos, revisión de alcance y origen. **A1:** comprobar 16 campos CFG, 12 flags F y 12 pares PN escritos; los resultados runtime continúan NO_EJECUTADOS. **A2:** revisar JWT/RBAC, tenant A/B, recursos, sucursales, ABAC, privacidad y denegación por defecto en diseño, no afirmar pruebas de seguridad. **A3:** M01.01–06 LISTO+A5 y contratos M02.01/02 coherentes; implementación .NET/MySQL/frontend = NO_APLICA a este diff documental. **A4:** Git fast-forward/CAS en `dev`, blobs exactos y checks Scope Lock más V5 provenance completed/success sobre SHA aplicable y readback PLAN/COLA/DAG/BITACORA. **A5:** requiere otro TASK_ID Javier distinto de autor y evidencia original; no equivale a permiso QA/main/PROD.

**Rollback de esta entrega:** `git revert` normal preservando historial. No hay base/flags/runtime que restaurar por esta microtarea.
