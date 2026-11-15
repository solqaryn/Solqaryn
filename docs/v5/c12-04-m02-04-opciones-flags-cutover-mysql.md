# SOLQARYN V5 FINAL — C12-04.M02.04: configuración tipada y flags seguros del cutover MySQL privado

**Autor:** PRIMARY DEV 02 / JAVIER_CHATGPT / `6af16bb9492c8191b8e8c7d606f11507`. **Repositorio exclusivo:** `solqaryn/Solqaryn@dev`; Scope Lock STRICT. **Tipo:** diseño documental M02.04. Ninguna opción aquí se añade a `appsettings`, Angular, Aiven, EF, MySQL, Render, Vercel o infraestructura activa.

## 1. Alcance y autoridad

El bloque C12-04 trata de MySQL privado, transición segura de esquema y cutover. El diseño debe preservar EF Core/MySQL 8.4, tenancy A/B, JWT/RBAC y estados transaccionales, sin atribuir una prueba real de restauración ni acceso a base productiva. Sus `C12-04.M01.01–06` están LISTO con revisión A5 independiente; `DAG_TECNICO` F=VALIDADO/G=SI, COLA G/O=SI, PLAN H=PENDIENTE/I vacío al reclamar este scope. Las opciones son **propuestas futuras**, no lectura certificada de configuración real ni autorización de ambiente.

## 2. Catálogo de opciones tipadas propuestas CFG01–CFG14

| Clave futura propuesta | Tipo / valor inicial de diseño | Ámbito y requisito |
| --- | --- | --- |
| CFG01 `PrivateDbCutover.Enabled` | bool / `false` | global backend, no activable por tenant o frontend |
| CFG02 `PrivateDbCutover.ProductionAllowed` | bool / `false` | prohibido sin autorización comercial y técnica independiente |
| CFG03 `PrivateDbCutover.AllowWrites` | bool / `false` | análisis dry-run es el estado inicial |
| CFG04 `PrivateDbCutover.RequireExactSha` | bool / `true` | commit, migración, manifiesto y CI en SHA consistente |
| CFG05 `PrivateDbCutover.RequireIndependentApproval` | bool / `true` | autor, reviewer y operador separados |
| CFG06 `PrivateDbCutover.RequireFreshMfa` | bool / `true` | autorización backend real más reautenticación sensible |
| CFG07 `PrivateDbCutover.RequireManifestChecksum` | bool / `true` | cifrado/integridad/retención validados |
| CFG08 `PrivateDbCutover.RequireEphemeralRestoreProof` | bool / `true` | prueba MySQL efímera, sin tocar Aiven real |
| CFG09 `PrivateDbCutover.MaxParallelTenants` | int / 1 | rango conceptual 1–2, sujeto a cuota y autorización |
| CFG10 `PrivateDbCutover.LeaseTtlSeconds` | int / 60 | rango conceptual 15–120 y CAS transaccional |
| CFG11 `PrivateDbCutover.MaxRetryAttempts` | int / 1 | rango conceptual 0–3, sin reintento ciego de DDL |
| CFG12 `PrivateDbCutover.DryRunRequired` | bool / `true` | el reporte no equivale a prueba de rollback |
| CFG13 `PrivateDbCutover.EnableCrossTenantBulkOps` | bool / `false` inmutable | nunca cruzar empresas por facilidad operativa |
| CFG14 `PrivateDbCutover.IncludeSecretsInTelemetry` | bool / `false` inmutable | sin DSN, JWT, OTP, URL bearer, PII ni backups crudos |

Todos los valores son convenciones de diseño **NO_COMPILADAS**. No inferir límites comerciales, ventanas de mantenimiento, métricas RPO/RTO ni disponibilidad real de proveedor. Si la implementación futura necesita un valor distinto deberá revisarse con pruebas y el gate de autorización correspondiente; jamás elevar cuotas solo para superar un check fallido.

## 3. Precedencia, validación y seguridad VAL01–VAL14

| ID | Regla futura fail-closed |
| --- | --- |
| VAL01 | Flag global disabled impide overrides de tenant, API y navegador |
| VAL02 | Permiso de entorno QA/main/PROD no procede de un booleano ni una Task |
| VAL03 | Actor autenticado y membership RBAC actual respaldan cada acción |
| VAL04 | Identidad de empresa deriva de servidor y propiedad persistida, no header/query |
| VAL05 | `ProductionAllowed=false` nunca se convierte en true durante retry |
| VAL06 | Dry-run o restore pendiente no permite escribir migración en datos reales |
| VAL07 | SHA exacto de código, manifiesto y CI obligatorio `completed/success` |
| VAL08 | Checks skipped/cancelled/failure/queued no se consideran PASS |
| VAL09 | CAS/lease con fencing token y una sola escritura ganadora |
| VAL10 | Grants revocados, lease vencida o doble writer detienen operación |
| VAL11 | Hash, conteos, FK y schema de restore efímero deben coincidir antes de salida |
| VAL12 | Secretos y PII nunca entran a logs, excepciones o eventos |
| VAL13 | Fallo parcial exige compensación verificable, sin repetir DDL ciegamente |
| VAL14 | Auditoría append-only conserva autor, revisor, causa y evidencia de reversión |

**Fuentes de configuración futuras:** política segura versionada del servidor y permiso por ambiente > opciones backend validadas > restricciones específicas por tenant que **solo pueden reducir** cuotas. Prohibido usar query, host, slug, cookie, `X-Empresa-Id` o Angular como autoridad. `IConfiguration` no valida derechos: el servicio autorizado deberá hacerlo por acción.

## 4. Escenarios positivos/negativos futuros — NO_EJECUTADOS

| Caso | Positivo futuro | Negativo obligatorio |
| --- | --- | --- |
| P01/N01 | Lectura de readiness en ambiente autorizado | Flag frontend intenta habilitar PROD |
| P02/N02 | Actor A consulta manifiesto de A | A usa manifest/hash de B |
| P03/N03 | SHA exacto y CI SUCCESS satisfacen preflight | CI cancelado libera migración |
| P04/N04 | Revisor independiente firma plan | Autor se autoaprueba |
| P05/N05 | Dry-run de schema detecta incompatibilidad | DDL productivo se aplica durante preflight |
| P06/N06 | Lease CAS permite un dueño | Dos Task/consumidores escriben mismo schema |
| P07/N07 | Restore efímero coteja FK, conteos y hash | Copia corrupta se declara recuperable |
| P08/N08 | Corte abortado conserva historial y compensación | Retry duplica operación DDL |
| P09/N09 | Auditoría limpia retiene traza mínima | DSN/JWT/PII sale en log de error |
| P10/N10 | GATE A0–A4 + A5 y ambiente separado permiten siguiente fase | LISTO documental se interpreta como permiso PROD |

No se ejecutó ningún test, migración, restore, cutover, worker, scheduler, gateway, escritura en Aiven o activación de proveedor. Los P/N son contratos de prueba para etapas M03–M06, no resultados ya certificados.

## 5. Dependencias, impacto, pruebas y rollback

**Cambios efectivos:** únicamente este archivo Markdown y una sección de `CHANGELOG_AI.md`. No crear configuraciones .NET, tablas, flags reales, API, cola de jobs, webhooks o infraestructura. **A0:** autenticación del actor y Skill, preflight de seis M01 LISTO+A5 y DAG/COLA, diff y Scope Lock. **A1:** consistencia de catorce opciones, catorce reglas y diez P/N; sin ejecución funcional. **A2:** tenant A/B, privilegio mínimo, MFA, control de acceso, secretos, CAS, restore efímero y rollback conceptual. **A3:** compatibilidad conceptual con EF/MySQL, API JWT/RBAC y entornos existentes, pero builds/DDL/HTTP son NO_APLICA causal para este diff documental, jamás PASS. **A4:** publicación fast-forward CAS, Scope Lock y procedencia V5 en `completed/success` SHA exacto, BITACORA/PLAN/COLA/DAG readback. **A5:** reviewer de otra Task Javier, sin autoaprobación, antes de dependientes. Revert documental mediante `git revert` normal tras releer HEAD, sin restaurar datos porque no se tocaron.
