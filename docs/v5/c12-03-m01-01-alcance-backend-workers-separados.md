# SOLQARYN V5 — C12-03.M01.01: backend y workers separados, alcance/no-regresión

**Fecha:** 2026-11-14. **ID:** C12-03.M01.01, PLAN_MAESTRO fila 1802. **Autor:** PRIMARY DEV 02 JAVIER, `6af16bb9492c8191b8e8c7d606f11507`. **Rama exclusiva:** `solqaryn/Solqaryn@dev`. **Tipo:** preparación documental M01; no código ni despliegue.

## Autoridad, objetivo y evidencia
El [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) define para C12-03 backend, workers, jobs, migraciones, health, colas y escalamiento; el [Sheet institucional](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit) confirma la habilitación técnica de DOCUMENTACIÓN M01. Al reclamar: PLAN H1802=PENDIENTE, I vacía; COLA G/O=SI, DAG F=VALIDADO/G=SI, CAS de estado y owner único con readback. Esto **no habilita** M03, API live ni servicios de terceros.

Objetivo: establecer reglas verificables de separación futura de API y procesamiento asíncrono sin cambiar el funcionamiento actual. Preparación puede adelantarse, pero no certifica idempotencia, recuperación, seguridad en runtime, infraestructura privada ni migración.

Fuentes versionadas comprobadas: `backend/src/API/Solqaryn.API.csproj` define ASP.NET Core `net10.0` + JwtBearer `10.0.12`; `backend/src/Infrastructure/Solqaryn.Infrastructure.csproj` define EF Core `10.0.12`, MySql.EntityFrameworkCore `10.0.9`, Cloudinary, MailKit y QuestPDF. `ARCHITECTURE.md`, `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, `docs/AUTOMATION_STATUS.md` y `.agents/skills/solqaryn-project-governance/references/architecture.md` preservan capas Domain <- Application <- Infrastructure y API como composition root, JWT/RBAC/tenant A/B, logs seguros y CI exact-SHA. Las menciones de Hostinger/Cloudflare son arquitectura **futura**, no despliegue actual auditado.

## Impacto y no-alcance
- API/controllers/DTO: solo definición; **ningún contrato HTTP cambiado**.
- Application/Domain/Infrastructure: solo definición; **ningún servicio o interfaz nueva**.
- EF/MySQL: documento de exclusividad/rollback; **sin SQL, DDL, datos ni migraciones**.
- Workers/outbox/colas: condiciones futuras de entrega, reintentos y recuperación; **sin dispatcher, webhook o runtime**.
- Seguridad: cero cambios de secretos, JWT, RBAC, PII, membresías o identidades tenant.
- Operación: sin compras, DNS, TLS, VPS, Cloudflare, Hostinger, despliegue, QA/main/PROD ni nuevas Tasks.

## Invariantes exigibles
- **NR01:** API ASP.NET Core 10 sigue siendo superficie HTTP; jobs no conceden autorizaciones adicionales.
- **NR02:** Domain no referencia Infrastructure; Application coordina contratos, Infrastructure implementa adaptadores.
- **NR03:** Un JWT inválido, vencido o no firmado falla cerrado antes de procesar trabajo.
- **NR04:** Identidad tenant proviene de identidad+membresía server-side, no del slug/header/parametro del cliente.
- **NR05:** RBAC/membresía se revalidan al ejecutar el trabajo, incluidos reintentos asíncronos.
- **NR06:** Tenant A nunca puede leer, escribir ni relacionar datos, adjuntos o auditorías de B.
- **NR07:** Secrets JWT/MySQL/SMTP/Cloudinary no se almacenan en repo ni se imprimen en logs.
- **NR08:** EF Core 10.0.12 y MySql.EntityFrameworkCore 10.0.9 permanecen anclados hasta decisión técnica explícita.
- **NR09:** Una migración MySQL tiene ejecutor único y plan de rollback; jamás compiten API y worker.
- **NR10:** Los jobs con efecto externo deben ser idempotentes frente a redelivery y reintentos.
- **NR11:** Ni cola, ni outbox, ni dispatcher se declaran operativos por esta definición documental.
- **NR12:** Readiness de API/worker no se confunde con liveness cuando DB u otra dependencia falla.
- **NR13:** No se añaden endpoints, puertos, DNS, TLS, dominios, proveedores ni servicios por esta fase M01.
- **NR14:** Los logs correlacionan el trabajo sin PII, tokens JWT, password ni datos de otros tenants.
- **NR15:** El apagado/reinicio requiere prueba de no pérdida y no duplicación antes de activar workers.
- **NR16:** QA/main/PROD, Hostinger, Cloudflare, datos productivos y cobros permanecen fuera del alcance.

## Matriz futura de aceptación positiva/negativa (NO_EJECUTADA)
Estas son especificaciones para CI posterior, no PASS funcional de esta microtarea. Cada escenario exigirá suite, comando, evidencia, SHA, tenant A/B sintéticos y rollback:

| Par | Positivo previsto | Negativo previsto |
|---|---|---|
| P01/N01 | JWT valido y usuario autorizado | JWT vencido/ausente se rechaza |
| P02/N02 | tenant A accede a datos A | tenant A no accede a B |
| P03/N03 | RBAC habilita accion autorizada | rol sin permiso se deniega |
| P04/N04 | mensaje unico produce efecto unico | redelivery no duplica efecto |
| P05/N05 | readiness saludable tras dependencias | DB caida no se anuncia sana |
| P06/N06 | arranque con secrets correctos | secret ausente no permite iniciar vulnerable |
| P07/N07 | migracion ejecutada por un solo proceso | API y worker no corren DDL en carrera |
| P08/N08 | restart continua estado duradero | kill a mitad no provoca commit parcial |
| P09/N09 | telemetria correlaciona por evento | logs no emiten contrasenas ni JWT |
| P10/N10 | revert de codigo mantiene integridad | rollback no elimina filas productivas |

## Puertas de implementación y cierre
- **M01:** coherencia con Plan/Sheet/DAG y fuentes Git, análisis estático, CHANGELOG autocontenido, Scope Lock + procedencia V5 del SHA exacto; A5 requiere otra Task Javier. Compilación general no pertinente se clasifica NO_APLICA con motivo y no PASS.
- **M02:** especificar contratos de jobs, permisos, idempotencia, política retry/dead-letter, dueño único de migraciones y compatibilidad HTTP.
- **M03:** código .NET real, pruebas positivas y negativas GitHub Actions, tenant A/B, JWT/RBAC y MySQL efímero; quedarse VALIDANDO si CI requerida está en curso.
- **M04–M05:** escenarios de reinicio, kill, DB caída, secretos ausentes, carga, trazabilidad y reversión reproducible.
- **M06:** evidencias A0–A4, A5 independiente y gates del Plan; no promover a QA/main/PROD por terminar docs.

**Rollback de este changeset:** git revert normal de solo los dos Markdown publicados, con readback; no editar BITACORA histórica. **Resultado actual:** alcance documentado y no-regresión definidos, sin implementación, pruebas live, servicio o infraestructura nueva. A5 queda pendiente de otra de las cinco Tasks Javier.
