# SOLQARYN V5 — C14-03.M02.06: contrato terminal de dependencias y salida M02 de promoción controlada

**Responsable:** PRIMARY DEV 06 JAVIER (TASK_ID \`6af16bbad26881918c283abe06e1f32a\`). **Fecha:** 2026-11-15. **Ámbito:** \`solqaryn/Solqaryn@dev\`, \`PROJECT_SCOPE_LOCK=STRICT\`. **Fuente ejecutable:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL y PLAN_MAESTRO/COLA/DAG_TECNICO institucional.

**Naturaleza:** entrega exclusivamente documental para definir el cierre del *diseño M02*. No demuestra paridad, autorización ni despliegue en QA, \`main\` o PROD; no instala API, base de datos, workers, eventos, webhook, credenciales, servicios ni automatizaciones. A5 independiente es necesaria después de A0–A4 y no sustituye permiso explícito de otro entorno.

## 1. Preflight causal y límites del alcance

Al reclamar este ID se leyó \`PLAN_MAESTRO!H2101=PENDIENTE\`, owner \`I2101\` vacío, \`COLA!G2101=SI/O2101=SI\` y \`DAG_TECNICO!F2062=VALIDADO/G2062=SI\`. Se confirmó exclusividad de PRIMARY DEV 06 con actualización condicional del estado y lectura posterior de owner. El DAG exige \`C14-03.M01.01–06+A5\`, \`C14-03.M02.01–05+A5\`, \`C14-01.M01.06+A5\` y \`C14-02.M01.06+A5\`. Los estados institucionales de esos antecedentes se verificaron al entrar. Si un A5_FAIL real posterior altera el DAG, se revoca la aptitud de las sucesoras, sin cambiar fórmulas manualmente.

Fuentes técnicas y de contrato examinadas: \`docs/v5/c14-03-m01-06-bloqueos-rollback-promocion-controlada.md\`; \`docs/v5/c14-03-m02-01-modelo-entidades-ownership-promocion-controlada.md\`; \`docs/v5/c14-03-m02-02-api-dto-errores-compatibilidad-promocion-controlada.md\`; \`docs/v5/c14-03-m02-03-persistencia-indices-migraciones-fixtures-promocion-controlada.md\`; \`docs/v5/c14-03-m02-04-configuracion-tipada-flags-promocion-controlada.md\`; \`AGENTS.md\`, Scope Lock, Skill canónica y el documento V5.

**Arquitectura que no se altera:** Angular 22.2.1 con Signals; API ASP.NET Core net10.0; Domain ← Application ← Infrastructure, API como composition root; EF Core 10.0.12 / MySql.EntityFrameworkCore 10.0.9 y MySQL 8.4; JWT, RBAC, aislamiento de tenants A/B, auditoría y secretos fuera del repositorio. Estas versiones describen el baseline, no un entorno promocionado.

## 2. Matriz de salida del contrato M02 (diseño, no pruebas ejecutadas)

| Gate | Evidencia exigida para el diseño M02 | Lo que no certifica |
| --- | --- | --- |
| OUT01 Alcance | Declarar repo único, rama \`dev\`, source SHA de 40 hex, árbol y diff exactos | Escritura en QA, main o PROD |
| OUT02 Dependencias | PLAN/COLA/DAG por ID exacto, M01/M02 y predecesores externos con A5 real | Liberación por un estado VALIDANDO provisional |
| OUT03 Separación | Solicitud, candidato, target y aprobación conceptual separados | Credencial, permiso o conexión a target |
| OUT04 Autoridad | JWT y membresía verificadas por backend, RBAC por acción, tenant A/B fail-closed | Autorización basada en cabecera o payload |
| OUT05 Evidencia nativa | Check-run \`completed/success\` y \`head_sha\` exactos; ignorar cancelado, omitido, en curso o fallido | Certificación por texto de UI o BITACORA sin lectura nativa |
| OUT06 Artefactos | Candidato, tree, digest e identidad de build inmutables, validación de integridad y procedencia | Build o paquete de despliegue producido aquí |
| OUT07 Configuración | Esquema tipado, defaults restrictivos, flags futuros apagados y target NONE | Activación de flags, nuevas variables o secretos |
| OUT08 Separación de funciones | Autor DEV, A5 de otra Task Javier y aprobación de ambiente distinta | Autoaprobación o promoción por A5 |
| OUT09 Reintentos | Idempotencia por actor, target y SHA; rechazo de outcomes duplicados | Worker, dispatcher, webhook o evento instalado |
| OUT10 Seguridad de datos | No registrar PII, tokens, rutas sensibles ni credenciales; auditoría redactada | Datos productivos inspeccionados o migrados |
| OUT11 Reversión | Contrato de rollback basado en artefacto previo, restore efímero y health/readback posterior | Restore, migración o rollback ejecutados |
| OUT12 Regresión | Casos positivos/negativos de auth, tenant, SHA, tests y fallos de target | Suites de integración o Playwright ejecutadas |
| OUT13 Observabilidad | Correlación saneada, causa de NO_GO, versión de reglas y timestamp comprobado | Sistema de alertas o telemetría en tiempo real |
| OUT14 Continuidad | Propietario único, CAS, BITACORA A:Z append-only, readback por transición y recálculo DAG/COLA | Scheduler server-side continuo |
| OUT15 Despliegue | Gate futuro separado para QA/main/PROD con autorización explícita y smoke/rollback real | Permiso comercial, cambio de rama o DNS |
| OUT16 Handoff | Documentar A0–A4, SHA, blobs, resultado de CI causal, A5 pendiente y reversión Git | Certificación final de M03–M06 |

La comprobación de OUT01–OUT16 en este archivo es **coherencia documental**, nunca un pase implícito de release ni una ejecución de pruebas de infraestructura.

## 3. Contratos de datos e interfaces futuras, con denegación por defecto

- **Domain:** el candidato de release es inmutable por repo, branch, commit SHA, tree SHA y digest; ninguna regla de dominio puede depender de GitHub, EF, Cloudflare, Angular o permisos del navegador. La aprobación se asocia a target, actor y caducidad, y jamás equivale a A5 propia.
- **Application:** sólo casos de uso futuros autorizados podrían solicitar preflight, lectura de evidencias, revisión, denegación y propuesta de rollback. El permiso efectivo deriva del servidor y debe comprobarse de nuevo ante cambios de membresía, revocación o tenant.
- **Infrastructure:** posibles repositorios, índices, registros de auditoría e idempotencia requerirán migración aditiva, integración MySQL 8.4 efímera y pruebas de revert/restore antes de cualquier activación. El diseño M02.03 no autoriza ejecutar SQL.
- **API / Angular:** los DTO propuestos y estados UI solo informan; no pueden transformar \`LISTO_DEV\`, \`A5_PASS\` o cualquier flag en autorización de QA/PROD. Los errores no revelan secretos, IDs de otro tenant o rutas internas.
- **CI:** cada M03 de código deberá demostrar compilación .NET/Angular, pruebas causales por SHA y regresiones multiempresa relevantes; las pruebas canceladas, omitidas o pendientes nunca son PASS.
- **Eventos:** señales y contratos del M02.05 son hipotéticos. Ningún webhook, dispatcher, worker, cola ni invocación entre Tasks se instala en este cierre. Los mensajes no otorgan autoridad.

## 4. Oráculos positivos y negativos FUTUROS_NO_EJECUTADOS

| ID | Caso positivo futuro | Denegación o fallo negativo exigido |
| --- | --- | --- |
| P01/N01 | Candidato DEV con repo, rama, SHA y tree verificados | Repo externo, SHA divergente o rama \`main\` tomada por alias |
| P02/N02 | Predecesores LISTO+A5 registrados por ID y DAG SI | Predecesor VALIDANDO o A5_FAIL se interpreta como SI |
| P03/N03 | A5 de Task Javier independiente del autor | Autor firma A5 de su propio SHA |
| P04/N04 | Check nativo terminado con éxito para SHA pertinente | \`queued\`, \`cancelled\` o \`skipped\` cuenta como PASS |
| P05/N05 | Backend permite recurso de tenant A al rol A autorizado | Tenant B lee o modifica evidencia de A |
| P06/N06 | Aprobación de target concreta, vigente y con permisos | Consentimiento DEV se reutiliza para PROD |
| P07/N07 | Digest real coincide con artefacto registrado | Digest provisto por cliente valida bytes distintos |
| P08/N08 | Dos intentos idénticos preservan outcome único | Reintento con misma clave produce doble promoción |
| P09/N09 | Configuración ausente mantiene flags apagados | Header o UI activa aprobación o release |
| P10/N10 | Log redactado registra causa y correlación | JWT, clave o PII aparece en observabilidad |
| P11/N11 | Health/readback nativo confirma versión observada | Texto \`deployed\` de UI suple evidencia |
| P12/N12 | Restore efímero recupera conteos y hashes esperados | Rollback nominal sin backup verificado |
| P13/N13 | Error de proveedor produce estado NO_GO auditado | Falla parcial crea una promoción silenciosa |
| P14/N14 | Cambio de HEAD exige revalidación y nuevo diff | Check de SHA antiguo se reutiliza sin equivalencia |
| P15/N15 | Permisos expirados deniegan la operación futura | Aprobación caducada permanece válida |
| P16/N16 | El final de M02 queda como contrato documental | Se confunde M02 LISTO con M03 probado o PROD autorizado |

**Ningún P01–P16 o N01–N16 se ejecutó contra HTTP, MySQL, navegador, servicios de nube o datos reales en esta microtarea.** Son criterios de aceptación y diseño posteriores; las CI de Scope Lock y procedencia sí deben comprobarse sobre el commit documental publicado.

## 5. Riesgos, handoff, auditorías y reversión

Riesgos que permanecen abiertos hasta sus fases: mezcla accidental de entornos; revocación de permisos sin revalidación; digest y artefacto divergentes; cancelaciones de CI confundidas con PASS; contratos de restore sin ensayo real; carreras de propietarios, cambios Git y señales de UI que aparentan autoridad. Cada uno exige fallo cerrado, evidencia causal y rollback antes de una futura operación autorizada.

**A0:** verificar diff limitado a este documento Markdown más encabezado y viñetas contiguos de \`CHANGELOG_AI.md\`; sin código, secretos ni políticas de ambiente.

**A1:** inspección de OUT01–OUT16 y P01/N01–P16/N16 como obligaciones documentales; no emitir PASS funcional para oráculos futuros.

**A2:** revisar JWT/RBAC, tenant A/B, independencia, SHA e inmutabilidad, permisos por ambiente, fail-closed y la ausencia de credenciales o efectos externos.

**A3:** corroborar M01.01–06, M02.01–05 y C14-01/C14-02.M01.06 en LISTO+A5 antes del claim; verificar coherencia de contratos. .NET/Angular, MySQL, Playwright, infraestructura y despliegue = NO_APLICA al diff exclusivamente Markdown, con explicación.

**A4:** publicar mediante fast-forward/CAS sobre HEAD vigente en \`dev\`; leer commit, diff/blobs, Scope Lock y control de procedencia V5 \`completed/success\` sobre el SHA **publicado**, además de PLAN/COLA/DAG y BITACORA por ID.

**A5:** registrar \`A5_PENDIENTE\` y exigir revisión técnica independiente por DEV01–DEV05, con SHA/diff/evidencia y actualización auditable de PLAN!K antes de liberar sucesoras que la exijan.

**Rollback documental:** \`git revert\` normal, adaptado al HEAD concurrente sin fuerza ni reescritura. No hay DDL, datos, dominios, DNS, despliegues o servicios externos que revertir en este changeset. QA, \`main\` y PROD permanecen explícitamente fuera de alcance.
