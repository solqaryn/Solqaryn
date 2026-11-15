# SOLQARYN V5 — C03-03.M02.04: configuración tipada, ámbitos y flags de auditoría de seguridad

**Microtarea:** `C03-03.M02.04`, fase documental M02 del módulo de auditoría de seguridad. **Autor:** PRIMARY DEV 06 — JAVIER_CHATGPT, `TASK_ID=6af16bbad26881918c283abe06e1f32a`. **Único repo/rama:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Fuente:** STANDARD V5 FINAL y PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA. **Frontera:** contrato futuro, no cambios de appsettings, variables, código, retención efectiva, permisos, SQL, API, logs reales, workers ni proveedores.

## 1. Precedencia verificada y fuente de código vigente

A0 preflight por ID: PLAN H515=PENDIENTE, owner I515 vacío, COLA G/O=SI, DAG F=VALIDADO/G=SI, terminal `C03-03.M01.06` LISTO con revisión A5 de DEV06 independiente del autor DEV03. Se reclamó mediante CAS de H515 (un único ganador) y readback del owner DEV06.

Código real inspeccionado: `backend/src/API/Controllers/AuditoriaController.cs` blob `82057f2acc571263ed7ed530eae0f81359b2f81a` (`[Authorize]`, permiso de auditoría y guardia administrativa); `backend/src/Application/Services/AuditoriaService.cs` blob `def2c1f7bc58e23f98a1065fcdf0963322f3705c` (registro best-effort distinto del estricto que propaga excepción); `backend/src/Infrastructure/Repositories/AuditoriaRepository.cs` blob `8fc9417b88da91ad1a5777ea77d9e31756230384` (filtros y paginación sin predicado explícito empresa en ese método); `backend/src/API/Middleware/CorrelationIdMiddleware.cs` blob `3b76dca41bb4bc0b44f89c2a4664c79907f9f242` (ID saneado, no autoridad); `backend/src/Domain/Entities/RegistroAuditoria.cs` blob `d669ac397a78ba02a5ba19e2cfb4bc9c7a115462`. Las especificaciones M01.03 (blob `807eab0b4d6aa1a71beeb7d9df82b40e23c06969`), M01.05 (blob `b8ad87e4164135ccbbe1a84e180e55bc07fbecb2`) y M01.06 (blob `274dd951853459e687ea4fc359b03932f254e869`) conservan escenarios AU01–AU16 futuros y siete gates de seguridad. El backend es .NET 10, EF Core 10.0.12 con Oracle MySQL 10.0.9; no se ha certificado aquí su entorno vivo.

La ausencia de filtro `EmpresaId` explícito en el método leído **no demuestra ni descarta** fuga A/B: debe inspeccionarse flujo completo y EF global filters mediante pruebas HTTP/MySQL efímero antes de habilitar consultas sensibles. Este documento no implementa soluciones supuestas.

## 2. F01–F16: catálogo conceptual de opciones tipadas, NO keys instaladas

| ID | Tipo/propuesta | Autoridad prevista | Restricción predeterminada |
| --- | --- | --- | --- |
| F01 | `AuditReadsEnabled: bool` | Plataforma y rol/empresa verificados | false para nueva superficie hasta prueba A/B |
| F02 | `AuditStrictOnCriticalWrites: bool` | Política innegociable servidor | true si cambio crítico la requiere |
| F03 | `AuditTenantScopeMode: enum` | Plataforma seguridad | tenant backend comprobado, jamás header/body |
| F04 | `AuditGlobalVisibilityMode: enum` | Permiso explícito plataforma | cerrado salvo grant global auditado |
| F05 | `AuditAdminRoleRequired: bool` | Autoridad de backend | true para consultas administrativas |
| F06 | `AuditRedactionPolicy: enum` | Plataforma seguridad | allowlist mínima, omitir JWT/PII/DSN |
| F07 | `AuditAllowedFieldSet: catálogo` | Revisión de privacidad | solo campos expresamente aprobados |
| F08 | `AuditCorrelationMaxLength: int` | Middleware servidor | límite seguro, no clave idempotente |
| F09 | `AuditQueryMaxPageSize: int` | Plataforma/backend | positivo, acotado y server-side |
| F10 | `AuditQueryMaxAge: TimeSpan?` | Política de datos aprobada | sin TTL o purga automática inferida |
| F11 | `AuditExportEnabled: bool` | Autoridad plataforma/privacidad | false sin rol, scope, base legal y A5 |
| F12 | `AuditRetentionPolicyId: referencia` | Dueño legal/privacidad | sin configuración: no purgar |
| F13 | `ExceptionalAccessEnabled: bool` | Plataforma seguridad con doble aprobación | false; NO implementar elevación en documento |
| F14 | `ExceptionalAccessTtl: TimeSpan` | Seguridad, sin override tenant | finito si futura capacidad aprobada |
| F15 | `AuditFailureHandling: enum` | Contrato de operación crítica | `strict` cuando durabilidad sea obligatoria |
| F16 | `AuditDiagnosticsSafeMode: bool` | Plataforma | true; sin payload, tokens, mensajes libres |

Son **nombres de diseño futuros**, no propiedades C#, columnas EF, opciones en DI o variables nuevas. No editar configuraciones reales ni reclamar que estén habilitadas. F10/F12 no establecen obligación legal ni plazo de retención; requieren decisión específica del propietario y área responsable antes de implementación.

## 3. Orden de precedencia, ámbito y permisos

1. **Gobierno/seguridad transversal:** identidad, JWT/RBAC, tenant A/B, privacidad, Scope Lock y autorización de auditoría jamás se relajan por un flag.
2. **Plataforma:** fija fuentes y claves permitidas, tipos, límites de consulta, modo estricto, política redactora y alcance global. Un operador autorizado debe aportar evidencia y revalidación; no existe activación automática.
3. **Empresa:** sólo puede restringir acceso/visibilidad dentro de su ámbito previamente habilitado; no elegir empresa ajena ni habilitar exportación, global scope, retención destructiva o acceso excepcional.
4. **Usuario:** requiere membresía activa y permiso backend sobre el recurso; rol de UI o parámetro de cliente no concede acceso.
5. **Cliente/browser:** filtros, timestamps, `X-Correlation-ID` e IP son datos no autoritativos; nunca selector de tenant ni facultad de purga.
6. **Fallo seguro:** ausencia de política aprobada, secreto seguro, actor/scope verificado o precondición de pruebas => NO_GO para funcionalidad afectada. Mantener otras tareas elegibles.

## 4. V01–V18: validaciones preventivas fail-closed

| ID | Oráculo futuro obligatorio |
| --- | --- |
| V01 | Rol administrativo sin permiso explícito de auditoría no consulta |
| V02 | Admin de A no ve B incluso manipulando query/header o ID |
| V03 | Permiso plataforma explícito no se infiere de rol empresarial |
| V04 | Cuenta, membresía o JWT revocados invalidan la operación |
| V05 | Consulta sin predicado/EF tenant verificable se bloquea, no se supone segura |
| V06 | Campos old/new sólo pueden contener allowlist redactada |
| V07 | JWT, OTP, correo, DSN, SQL, contraseñas y PII quedan fuera de logs/exports |
| V08 | `CorrelationId` inválido, CRLF o excesivo se sanea o reemplaza |
| V09 | `CorrelationId` no es permiso ni una clave de idempotencia |
| V10 | Registro best-effort jamás certifica durabilidad de cambio crítico |
| V11 | Auditoría estricta falla con rollback cuando política exige atomía |
| V12 | Paginación/filtros/fechas están acotados por servidor, no por UI |
| V13 | Exportación desactivada no se habilita con parámetro de cliente |
| V14 | Retención/purga no corre sin política aprobada, idempotencia y ensayo |
| V15 | Acceso excepcional no se activa sin motivo, grant, TTL y revisión independiente |
| V16 | Datos auditados no atraviesan fronteras tenant por background jobs |
| V17 | CI pertinente `completed/success` exact SHA; `queued/cancelled/skipped/failed` no PASS |
| V18 | A5 revisor distinto del autor y permiso explícito antes de QA/main/PROD |

Estos son requisitos de aceptación futura, no controles runtime añadidos en M02.04.

## 5. P01/N01–P10/N10: prueba positiva/negativa FUTUROS_NO_EJECUTADOS

| Par | Positivo futuro verificable | Negativo futuro que debe denegarse |
| --- | --- | --- |
| P01/N01 | Admin A con permiso consulta solo eventos A | Admin A obtiene eventos B o actor no admin consulta |
| P02/N02 | Revocación de permiso se aplica al próximo request | JWT antiguo mantiene consulta |
| P03/N03 | Registro estricto de cambio crítico persiste/rollback atómico | Falla DB pero cambio crítico queda aprobado |
| P04/N04 | Correlación saneada y limitada en request/registro | Header CRLF/largo inyecta log |
| P05/N05 | Logs en allowlist sin PII ni secretos | Old/new muestran JWT, contraseña, DSN |
| P06/N06 | Paginación/fechas acotadas por server | Consulta masiva/export bypass sin límite |
| P07/N07 | Grant plataforma explícito permite reporte autorizado | Rol empresa se eleva a plataforma |
| P08/N08 | Retención aprobada, backup y borrado autorizado se prueban | Purga automática sin política legal o sin rollback |
| P09/N09 | Acceso excepcional concedido con TTL y doble autoridad | Tenant activa elevación por flag propio |
| P10/N10 | CI exact SHA y A5 independiente acreditan gates | Cancelled/skipped o autor aprueba su A5 |

**Todos son escenarios FUTUROS_NO_EJECUTADOS en M02.04.** Un cambio posterior en código necesita pruebas reales HTTP/EF/MySQL efímero y xUnit de tenant A/B, revocación, fallo DB, redacción y autorización; Angular y Playwright cuando se cambien componentes de consulta/exportación. Este Markdown no certifica tales pruebas ni cambios de política.

## 6. Impacto, adopción futura, rollback y A0–A5

**Impacto previsto:** únicamente especificación de `docs/v5` y nota independiente de `CHANGELOG_AI.md`; sin cambios en servicios, DTO, configuraciones, runtime, migraciones, DB, tokens, permisos, procesos, Tasks, cuentas, proveedores o entornos.

**Antes de implementar M03 futura:** propietario de cada dato y base legal de retención; aprobación de A5 documental y DAG; designar actor y contrato de consulta, inspeccionar EF filtros globales y pruebas tenant A/B, distinguir best-effort/estricto y revert; definir índices y cuotas después de medición en MySQL efímero. No habilitar excepcionalidad ni exportación con documentación solamente.

**A0:** Skill canónica/arquitectura/Scope Lock, identidad Javier, PLAN PENDIENTE sin owner, COLA G/O SI, DAG F=VALIDADO G=SI y CAS único de H515/I515. **A1:** catálogo F01–F16, reglas V01–V18 y diez pares futuros cotejados con M01. **A2:** tenant A/B, revocación, permisos, protección de secretos/PII, auditoría estricta y retención sin privilegios nuevos. **A3:** API, Application, Domain, Repository y CorrelationIdMiddleware reales inspeccionados; `dotnet/npm/MySQL/HTTP` NO_APLICA a diff exclusivo Markdown, jamás PASS. **A4:** Git fast-forward/CAS dev sobre HEAD actual para `docs/v5/c03-03-m02-04-configuracion-tipado-flags-auditoria-seguridad.md` y sección propia contigua de CHANGELOG; comprobar Scope Lock + V5 `completed/success` SHA exacto y readbacks institucionales. **A5:** una de DEV01–DEV05 (distinta de DEV06) revisa evidencia; jamás autoaprobación.

**Rollback del documento:** `git revert` normal del commit tras releer HEAD y evento compensatorio aditivo si hace falta; sin datos/servidores/configuración runtime alterados. No QA/main/PROD, trabajadores, eventos, webhooks, migraciones productivas ni pagos.
