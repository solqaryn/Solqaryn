# SOLQARYN V5 — C02-01.M02.06: contrato de salida del contexto de identidad y tenancy

**ID institucional:** `C02-01.M02.06` — fila 301 de PLAN_MAESTRO. **Responsable:** PRIMARY DEV 01 / JAVIER_CHATGPT / `6adbb9fff1548191a1bd5b30d9e9984a`. **Fuente ejecutable:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Ubicación:** `solqaryn/Solqaryn@dev`. **PROJECT_SCOPE_LOCK=STRICT**. **Tipo:** contrato de salida M02 exclusivamente documental.

## Alcance y límites de evidencia

Cierra la especificación de **dependencias técnicas, condiciones de liberación y rollback** de la fase de diseño M02 del contexto de identidad y tenant (`C02-01`), sin declarar ningún cambio operativo a JWT, RBAC, EF/MySQL, autorizaciones, middleware, control de sesiones, host, eventos, colas, webhooks o infraestructura. La arquitectura vigente sigue siendo ASP.NET Core 10 (Domain/Application/Infrastructure/API), Angular 22.2.1, MySQL/EF Core proveedor Oracle, con decisiones de seguridad en backend. No se modifica runtime ni se instala despacho de eventos.

Las transiciones de un futuro contrato a código real estarán condicionadas por DAG individual, identidad y permiso, evidencia de pruebas positivas y negativas, Scope Lock en SHA exacto, análisis tenant A/B y revisión A5 independiente. Un documento **no constituye** permiso de ejecución, migración, configuración IAM, QA/main/PROD ni habilitación de tareas sucesoras.

## Evidencia contractual y dependencias efectivas

Antes de reclamar este ID se releyó `PLAN_MAESTRO!H301=PENDIENTE/I301` vacío, `COLA!G301=SI/O301=SI` y `DAG_TECNICO!A297=C02-01.M02.06/F297=VALIDADO/G297=SI`. La reclamación `PENDIENTE → EN_PROGRESO` quedó condicionada y auditada para J00. El DAG declaró: `C02-01.M01.01–06+A5`, `C02-01.M02.01–05+A5` y `C01-01.M01.06+A5`; no hay autorización para inferir prerrequisitos adicionales ni para saltarse A5.

| Antecedente | Contrato revisado | Cautela |
| --- | --- | --- |
| `C02-01.M01.01–06` | Preparación de identidad y tenancy LISTO con revisiones A5 registradas | A5 no sustituye pruebas de autenticación real |
| `C02-01.M02.01` | Modelo de entidades, ownership e invariantes | No certifica persistencia live |
| `C02-01.M02.02` | API/DTO, errores, validaciones y compatibilidad | No acredita endpoints ejecutados |
| `C02-01.M02.03` | Persistencia, índices, migraciones y fixtures sintéticos | No ejecutó DDL/MySQL |
| `C02-01.M02.04` | Flags y configuración tipada, ámbitos y fallo cerrado | No modifica banderas efectivas |
| `C02-01.M02.05` | Idempotencia/correlación, límites y contratos de eventos | Documento SHA `c5cfed9f11f68c32c6ef5224e055340dc52cb08c`, blob `a8d37272d7324180608957789308930c9fa19776`, A5_PASS independiente J00 |
| `C01-01.M01.06` | Gate transversal de núcleo universal | Su A5 es condición documental, no licencia de promoción |

Todos los oráculos P/N documentados por M02.01–05 siguen siendo **FUTUROS NO_EJECUTADOS**; no se atribuye xUnit, HTTP, MySQL, Angular, pentesting, backups ni observabilidad de runtime a esta fase.

## Condiciones de bloqueo BL01–BL16

| ID | Bloqueo hasta que una fase futura lo acredite con pruebas reales |
| --- | --- |
| BL01 | No existe evidencia server-side de identidad, membresía activa y tenant autorizado por petición |
| BL02 | Se confía en EmpresaId, tenant_id, slug, URL, hostname, header o cookie provistos por cliente como permiso |
| BL03 | Se permite operación sin JWT válido donde se requiere sesión autenticada o se tolera token expirado |
| BL04 | Se conceden permisos RBAC por UI, claims obsoletos o rol no verificado en backend |
| BL05 | No se rechaza acceso objeto B desde actor/tenant A, incluso por cache, jobs o respuestas tardías |
| BL06 | Las relaciones de recurso, sucursal, ownership, plan/capability no son verificadas en contexto actual |
| BL07 | Flags y configuración no están tipados, allowlisted y validados por ámbito/versión |
| BL08 | No hay rechazo explícito para permisos revocados, cuentas suspendidas o membresía deshabilitada |
| BL09 | Falta control de replay/idempotency key ligada a actor, tenant, operación, payload y plazo |
| BL10 | No existe transacción/outbox verificable antes de declarar efectos o mensajes publicados |
| BL11 | CorrelationId expone PII/secretos, admite CRLF o fuga de tenant cruzado en logs |
| BL12 | No se verifica integración API/DTO, respuesta segura, rate limiting y contratos legacy |
| BL13 | Persistencia/índices/migraciones no tienen pruebas aisladas y rollback verificable |
| BL14 | No se demuestra respuesta segura ante duplicados, carreras CAS, fallos de proveedor o timeout |
| BL15 | CI causal y exact SHA de código/seguridad/tenant A/B no concluye `completed/success` |
| BL16 | Falta revisión A5 independiente, Scope Lock, permiso específico o condiciones individuales del DAG |

Un BL abierto impide únicamente la operación pertinente. Nunca se transforma `VALIDANDO`, un documento ni un check `CANCELLED/SKIPPED` en PASS.

## Recuperación y reversión RB01–RB10

| ID | Escenario negativo futuro | Acción segura exigida |
| --- | --- | --- |
| RB01 | Credencial o membresía revocada | Invalidar autoridad en servidor, denegar nuevas operaciones y auditar |
| RB02 | Actor A intenta recurso B | Rechazar sin enumerar datos y limpiar respuestas/caches cruzadas |
| RB03 | Tenant de ruta/host no coincide | Fallo cerrado, no cambiar contexto por parámetro de cliente |
| RB04 | Replay con payload distinto | Emitir conflicto, ningún segundo efecto, auditar sin PII |
| RB05 | Idempotencia vencida o concurrente | Serializar/denegar conforme contrato, sin duplicar transacciones |
| RB06 | Fallo después de escribir pero antes de evento | Recuperar mediante outbox transaccional futuro, sin éxito ficticio |
| RB07 | Migración o índice altera relación de recursos | Restaurar solo desde respaldo probado en sandbox y plan aprobado |
| RB08 | Configuración tipada inválida | Recuperar último valor confiable, conservar rechazo fail-closed |
| RB09 | Consumidor/API incompatible | Revertir el cambio con commit normal y pruebas de regresión |
| RB10 | CI Scope Lock/seguridad falla | Conservar VALIDANDO, corregir causa y revalidar SHA antes de LISTO |

Son **estrategias de recuperación futuras, NO_EJECUTADAS**. Rollback de esta entrega Markdown: `git revert` normal del commit con su sección de CHANGELOG, sin reescribir Git ni BITACORA y sin tocar datos productivos.

## Auditorías A0–A4 y A5 requeridas

**A0 estática:** documento y CHANGELOG únicamente, identidad J00, alcance M02.06, ausencia de secretos y no alteración de código, API, schemas o seguridad existente.

**A1 funcional documental:** BL01–BL16 y RB01–RB10 con oráculos claros; cualquier prueba de JWT/API o MySQL futura marcada NO_EJECUTADA.

**A2 seguridad:** mantener tenant A/B, autorización en backend, sesión/membresía fresca, RBAC, rate limiting, configuración tipada, confidencialidad de logs e idempotencia. No afirmar pentest ni corrección de vulnerabilidad sin evidencia.

**A3 integración:** coherencia con M01.01–06, M02.01–05, C01-01.M01.06 y DAG exacto; no crear tareas, webhooks, eventos, DB/worker o permiso de producción.

**A4 publicación:** commit fast-forward/CAS exclusivamente `dev`, diff limitado a este Markdown y sección autocontenida de `CHANGELOG_AI.md`, check-runs `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` en SHA exacto, BITACORA y readback de PLAN/COLA/DAG. CI .NET/Angular/MySQL/Playwright no causal para dos Markdown = NO_APLICA motivado; jamás PASS por un check omitido.

**A5:** otra Task Javier **distinta** del TASK_ID autor examina documento, blobs, diff, CI causal y trazabilidad. Hasta A5 independiente queda `A5_PENDIENTE`. Este cierre no libera M03 código real sin gate propio ni QA/main/PROD.
