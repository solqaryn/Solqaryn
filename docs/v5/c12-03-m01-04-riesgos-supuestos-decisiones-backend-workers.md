# SOLQARYN V5 — C12-03.M01.04: riesgos, supuestos y decisiones backend/worker

**Fecha:** 2026-11-14. **ID:** C12-03.M01.04. **PLAN fila:** 1805. **Autor:** PRIMARY DEV 03 JAVIER / `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Destino:** solo `solqaryn/Solqaryn@dev`. **Fase:** preparación documental M01; no implementación.

## Fuentes y límite de autoridad

El [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y el [Sheet PLAN/COLA/DAG](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit) gobiernan la microtarea. Se usan los tres antecedentes del mismo lote: [alcance e invariantes NR01–NR16](c12-03-m01-01-alcance-backend-workers-separados.md), [actores A01–A14 y fronteras F01–F12](c12-03-m01-02-actores-superficies-backend-workers-separados.md), [15 fuentes Git y brechas G01–G12](c12-03-m01-03-inventario-codigo-evidencia-backend-workers.md). Se contrastaron `AGENTS.md`, `ARCHITECTURE.md`, Scope Lock y la Skill canónica.

Stack declarado: ASP.NET Core net10.0, Domain ← Application ← Infrastructure, API como composición; EF Core 10.0.12, MySql.EntityFrameworkCore 10.0.9 y JWT/RBAC. `AppDbContext` depende de marcas de tenant Verified* para contexto HTTP; un futuro worker sin `HttpContext` **debe negar por defecto** si no dispone de identidad/membresía server-side comprobadas. El inventario no acredita worker, cola, migración, despliegue ni servicio externo nuevo.

## R01–R14: registro de riesgos y controles de bloqueo

| ID | Riesgo causal | Contención obligatoria antes de activar código | Evidencia pendiente |
|---|---|---|---|
| R01 | Mensaje con tenant tomado de payload/header/slug | Binding tenant firmado/verificado, denegación A→B | G03, NR04/06 |
| R02 | Rol o membresía revocado tras encolar | Reautorización en el momento de consumir | G04, NR05 |
| R03 | Redelivery después de efecto externo | Deduplicación atómica y clave de idempotencia | G02/G05 |
| R04 | Ack antes del COMMIT | Confirmación únicamente de estado duradero; kill-before-commit | G02/G07 |
| R05 | API y worker migran esquema simultáneamente | Migrador único y prueba de carrera EF/MySQL | G06, NR09 |
| R06 | Readiness confunde proceso vivo con DB sana | Separar liveness/readiness y simular fallo MySQL | G09, NR12 |
| R07 | Secret no inyectado o impreso en logs | Arranque fail-closed y escaneo de secretos | G08, NR07 |
| R08 | Correlation ID o trazas revelan JWT/PII/otro tenant | Logs saneados y prueba A/B negativa | G10, NR14 |
| R09 | DTO o contrato job evoluciona sin compatibilidad | Versionado y pruebas consumidor anterior/nuevo | G11 |
| R10 | Crash durante retry con resultado ambiguo | Transacción durable, replay seguro, rollback | G05/G07, NR15 |
| R11 | Retries ilimitados causan tormenta y costes | Backoff, cuota, dead-letter y observabilidad | G02 |
| R12 | Timeout de Cloudinary/SMTP produce efecto repetido | Contrato explícito de idempotencia, sin asumir éxito | G05/G12 |
| R13 | Interpretar documento M01 como evidencia de runtime | CI del SHA de código y A0–A4 antes de LISTO técnico | G01/G12 |
| R14 | Activar proveedor/DNS/DDL por DAG documental | Nueva autorización diferenciada; no QA/main/PROD | NR13/16 |

Se priorizan R01/R02/R07 (aislamiento y autorización), R03–R05/R10 (integridad), R06/R08/R11/R12 (operabilidad) y R09/R13/R14 (compatibilidad y gobierno). No se asignan probabilidades ni umbrales inventados.

## S01–S10: supuestos NO_VERIFICADOS

| ID | Supuesto de diseño | Prueba para decidirlo |
|---|---|---|
| S01 | La API actual mantiene net10.0 y guardas vigentes | csproj + tests exact-SHA de implementación |
| S02 | Un worker separado no está aprobado por estas fuentes | Inventario actualizado antes de M03 |
| S03 | La cola duradera no tiene contrato probado | Diseñar persistencia/redelivery M02 |
| S04 | Se puede vincular job a tenant server-side sin HttpContext | Test A/B, ausencia de contexto NO_GO |
| S05 | RBAC/membresía puede revalidarse al consumo | Revocación durante retardo, tests efímeros |
| S06 | MySQL permitirá deduplicación transaccional | Migración aprobada y pruebas concurrentes |
| S07 | Migraciones tendrán un ejecutor único | Lock + CI con API/worker simultáneos |
| S08 | Proveedor externo ofrece semántica idempotente adecuada | Contract test de timeout/retry |
| S09 | Secrets pueden aislarse por proceso | Test secret ausente y logs sin valores |
| S10 | Rollback de código preservará escrituras de usuarios | Restore y compatibilidad de schema/versiones |

Cada supuesto permanece pendiente, no se considera PASS ni habilita M03.

## D01–D10: decisiones abiertas, responsable y condición

| ID | Decisión pendiente | Responsable del gate y evidencia |
|---|---|---|
| D01 | Outbox/tabla durable/proveedor de cola | Arquitectura + propietario: durabilidad, costes y redelivery |
| D02 | Identidad de job y binding tenant verificable | Application/Seguridad: denegación A/B y sin HttpContext |
| D03 | Clave idempotente y ámbito de unicidad | DB/Application: replay concurrente sin doble efecto |
| D04 | Retry/backoff/dead-letter y cuotas | Operación: pruebas de saturación |
| D05 | Owner único de DDL, esquema de migración | DB/Infraestructura: competencia controlada |
| D06 | Contrato liveness/readiness por proceso | API/Operación: caída DB/secret |
| D07 | Campos, saneamiento y retención de observabilidad | Seguridad: sin JWT/PII en trazas |
| D08 | Versionado de jobs/DTO y reversibilidad | API/Application: consumidor versión anterior |
| D09 | Concurrencia y backpressure ante dependencia caída | Operación: test con cola y límites medidos |
| D10 | Topología, proveedor y autorización de despliegue | Propietario: permiso específico fuera de M01 |

Las decisiones son **abiertas**; no se selecciona RabbitMQ, un webhook, un dispatcher ni una base nueva por inferencia.

## Ocho pares P/N de aceptación FUTUROS_NO_EJECUTADOS

| Par | Positivo a automatizar en M03–M05 | Negativo obligatorio |
|---|---|---|
| P01/N01 | Job A autorizado persiste A | Token ausente o payload B denegado |
| P02/N02 | Rol vigente permite consumo | Rol revocado no ejecuta |
| P03/N03 | Retry mantiene un solo efecto | Doble entrega nunca duplica |
| P04/N04 | Readiness correcto con DB sana | DB caída/secret ausente es not-ready |
| P05/N05 | Migrador con lock único | API y worker no hacen DDL simultáneo |
| P06/N06 | Restart recupera estado | Kill no confirma transacción parcial |
| P07/N07 | Log opaco correlacionable | JWT, PII, secretos no aparecen |
| P08/N08 | Reversión conserva datos | Versión anterior no borra registros |

Estos escenarios solo especifican futuras pruebas, **no fueron ejecutados** y no cuentan como CI PASS.

## Impacto, auditorías y rollback

Cambio limitado a este Markdown y una sección autocontenida de `CHANGELOG_AI.md`. Sin modificaciones de API/DTO/Domain/Application/Infrastructure, UI, MySQL, EF migrations, workers, colas, secrets, hosting, IAM, QA, main ni PROD; no se crean Tasks o eventos.

A0: análisis de fuentes, dependencias y alcance. A1: trazabilidad documental R/S/D y ocho pares futuros (no prueba runtime). A2: revisión estática de tenant A/B, JWT/RBAC, secretos y denegación. A3: referencias cruzadas a G01–G12/NR01–NR16, **NO_APLICA integración de worker/MySQL**. A4: exigir diff, fast-forward, `verify-solqaryn-project-scope` y check V5 de procedencia completados SUCCESS sobre SHA exacto, más PLAN/COLA/BITACORA readback. A5: pendiente de otra Task Javier. Rollback permitido: git revert normal, sin modificar BITACORA histórica.
