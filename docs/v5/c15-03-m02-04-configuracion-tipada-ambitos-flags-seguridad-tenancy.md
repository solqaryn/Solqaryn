# SOLQARYN V5 FINAL — C15-03.M02.04: configuración tipada y ámbitos seguros de auditoría multiempresa

**Autor:** PRIMARY DEV 04 — JAVIER_CHATGPT (`6aeacb7820ac8191b7d768a6f33dfa09`). **Repos:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Clase:** contrato DOCUMENTAL M02.04; no valores de secretos, configuración runtime, feature flags activos, permisos nuevos ni cambios de despliegue.

## 1. Fuentes, precondiciones y límites

Fuente única: [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), [M01.06](c15-03-m01-06-bloqueos-rollback-liberacion-auditoria-seguridad-tenancy.md), [M02.01](c15-03-m02-01-modelo-entidades-ownership-auditoria-seguridad-tenancy.md) y [M02.02](c15-03-m02-02-contratos-api-dto-errores-validaciones-seguridad-tenancy.md). SEC01–SEC16 y operaciones/DTO anteriores son diseños no implementados. Relevar el HEAD vivo y no convertir pruebas futuras en permisos.

Preflight del ID: PLAN H2243=PENDIENTE/I vacía; COLA G2243/O2243=SI; DAG F2204=VALIDADO/G2204=SI con M01.06 y A5 independiente. M02.03 puede estar ocupado por otra Task; no comparte archivo ni owner con este documento. Las lecturas no garantizan CAS atómico multi-sistema, por ello escribir sólo tras readback y HEAD esperado.

Las configuraciones comerciales actuales del backend .NET 10/EF/MySQL 8.4 deben mantenerse separadas de estas opciones hipotéticas de auditoría. Las identidades `EmpresaId`, `tenant_id`, host y headers de Angular nunca conceden autoridad. No incluir valores de conexión, API keys, cookies, bearer JWT ni rutas de proveedores en el repositorio.

## 2. Orden de ámbitos y propiedad de valores

| Ámbito | Origen de autoridad | Regla de precedencia | Denegación segura |
| --- | --- | --- | --- |
| SYS — sistema | Configuración validada desde backend autorizado | Máximo nivel para guardas globales | Usuario comercial no puede sobreescribir |
| ENV — ambiente | Dev/QA/PROD autorizado explícitamente | Dev no concede producción | Sin permiso ambiental: NO_GO |
| TEN — tenant | Empresa verificada en sesión y DB | Sólo puede restringir, nunca ampliar SYS | No usar cabecera o query EmpresaId |
| PRN — principal | JWT + RBAC y membresía real | Nunca supera la empresa ni SYS | Rol UI sin backend no habilita |
| RUN — corrida | Task Javier autenticada/estado nativo | Observación temporal, no autoridad persistente | Self-report no constituye lease |
| DOC — artefacto | Git SHA y documento validado | Evidencia de contrato, no ejecución | No leer Markdown como permiso |

Regla de decisión propuesta: `habilitado = SYS && ENV && TEN && PRN && gate_efectivo`. La ausencia de un dato requerido equivale a `DENY`; ningún flag de presentación supera JWT/RBAC y el contexto tenant establecido en backend.

## 3. Catálogo tipado futuro, no publicado

| Clave conceptual | Tipo esperado | Restricción inicial | Condición negativa |
| --- | --- | --- | --- |
| CFG01 `SecurityAuditEnabled` | bool | false hasta autorización real | Sin permiso global no se activa |
| CFG02 `StrictTenantBoundary` | bool | true, no relajable por tenant | false rechazado |
| CFG03 `RequireBackendRbac` | bool | true | Rol de UI no sustituye servidor |
| CFG04 `RequireMembershipRefresh` | bool | true | Membresía revocada bloquea |
| CFG05 `EvidenceSourceSha` | SHA40 | commit verificable | SHA obsoleto = error 409 |
| CFG06 `AuditCutRevision` | cadena no vacía | versión del STANDARD actual | Cortes mezclados rechazados |
| CFG07 `MaxFindingsPageSize` | entero | rango futuro 1..100 | Paginación ilimitada rechazada |
| CFG08 `IncludeSensitivePayloads` | bool | false inalterable | PII/tokens jamás emitidos |
| CFG09 `RequireIndependentA5` | bool | true | El autor no aprueba |
| CFG10 `FailClosedOnCIFailure` | bool | true | SKIPPED/CANCELLED no PASS |
| CFG11 `AuditRetentionPolicy` | enum | política autorizada posterior | Nunca purga BITACORA |
| CFG12 `AllowedReviewerTaskIds` | conjunto | seis Javier actuales | No agrega Tasks retiradas |
| CFG13 `EnableProdMutations` | bool | false y fuera de alcance | Ningún documento lo cambia |
| CFG14 `TraceRedactionMode` | enum | estricto/seguro | Logs con secretos denegados |
| CFG15 `MaxEvidenceAge` | duración | valor autorizado por operación | Timestamps inventados no aceptados |
| CFG16 `SecurityAuditFeatureVersion` | semver | versión aprobada futura | No activar API incompatible |

CFG01–CFG16 son identificadores de diseño, **no** keys reales añadidas a `appsettings`, secretos ni base de datos. Los valores ilustrativos no son un catálogo desplegado. Para implementación se necesita un nuevo alcance M03, opciones validadas al startup, pruebas de resolución, flags con rollback, rate limits y autorización por empresa.

## 4. Invariantes y precedencia negativa

| ID | Control | Infracción a rechazar |
| --- | --- | --- |
| IV01 | Todo valor efectivo tiene dueño y origen auditables | Flag llega por header de cliente |
| IV02 | Política estricta prevalece sobre override inferior | Tenant desactiva guard global |
| IV03 | Revocación prevalece sobre caché | Principal retirado conserva acceso |
| IV04 | SHA de evidencia y corte no se mezclan | Check de otro commit se da por PASS |
| IV05 | Sin secreto en logs/config públicos | ConnectionString en Markdown |
| IV06 | Paginación y cuotas acotadas | Petición sin límite agota servicio |
| IV07 | Error externo se expresa INDETERMINADO | Falta fuente significa cero P0/P1 |
| IV08 | A5 Task distinta de autor | Self-review se cuenta válido |
| IV09 | Gates PLAN/COLA/DAG calculados | Forzar G/O manualmente |
| IV10 | Tenant se resuelve del backend | Header de empresa B autoriza lectura |
| IV11 | Nunca se instala evento por documento | Webhook sin permiso empieza trabajo |
| IV12 | QA/main/PROD fuera del ámbito de dev | LISTO_DEV acciona despliegue |

## 5. Diez escenarios positivos y negativos futuros

| T | Positivo exigible en eventual runtime | Negativo |
| --- | --- | --- |
| T01 | SYS exige guard tenant siempre | Un tenant lo desactiva |
| T02 | Feature apagada no emite observación sensible | Se usa sin autorización |
| T03 | JWT + membresía verificadas | Sesión revocada lee |
| T04 | Usuario con permiso de auditoría y recurso propio | Header B permite enumerar B |
| T05 | Cursor de paginación respeta rango | Página de tamaño ilimitado |
| T06 | Evidencia SHA y corte coinciden | CI cancelada se considera exitosa |
| T07 | Logs redacted preservan correlación | Se imprimen credenciales |
| T08 | A5 distinto y permitido | El autor se aprueba |
| T09 | Formatos tipados inválidos fallan en arranque | Opción malformed toma default permisivo |
| T10 | Rollback reduce superficie sin borrar datos | Se alteran schedules o QA/PROD |

**T01–T10 FUTUROS_NO_EJECUTADOS**, no resultados de integración, pentest o Playwright. Las opciones conceptualizadas no presuponen implementación, métricas efectivas o eliminación de hallazgos.

## 6. Auditoría por causa y rollback

- **A0:** fuentes M01/M02, CFG01–CFG16, IV01–IV12, T01–T10 y Markdown íntegro sin secretos ni encabezados vacíos.
- **A1:** revisar que cada contrato tiene positivo/negativo medible; las pruebas runtime no han sido ejecutadas.
- **A2:** conservar JWT, RBAC, boundaries tenant A/B, PII y protección de claves; la revisión es estática y no modifica aplicación.
- **A3:** referencias a contratos M02.01/02, PLAN/COLA/DAG y fuentes autorizadas; no crear configuración efectiva.
- **A4:** commit fast-forward/CAS sobre HEAD, exclusivamente este documento+CHANGELOG, checks Scope Lock y procedencia V5 `completed/success` del SHA y readback institucional.
- **A5:** revisión independiente posterior por otra Task Javier distinta del autor actual; sin autoaprobación.

Consumidores: M02.05 evento conceptual, M02.06 gate terminal y futuro M03 verdadero código/pruebas. Sin permiso para DDL, secretos, AWS/Cloudflare, cuotas de pago, QA/main/PROD, DNS ni mutaciones de datos. **Rollback** mediante nuevo `git revert` en dev, checks exact-SHA y BITACORA append-only.
