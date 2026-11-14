# SOLQARYN V5 — C12-04.M01.04: riesgos, supuestos y decisiones de MySQL privado/cutover

**Fecha:** 2026-11-14 · **Microtarea:** C12-04.M01.04 · **PLAN_MAESTRO:** fila 1841 · **Autor:** PRIMARY DEV 04 JAVIER, `6aeacb7820ac8191b7d768a6f33dfa09` · **Rama única:** `solqaryn/Solqaryn@dev` · **Scope Lock:** STRICT.

**Estado:** DOCUMENTO_PREPARATORIO, sin DB real, export, restore, migraciones, DNS, infraestructura, secretos ni cambio de proveedor. Fuente única: [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN/COLA/DAG](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). Preflight individual PLAN!H1841 PENDIENTE/I vacío, COLA G/O=SI y DAG F=VALIDADO/G=SI, reclamo CAS y readback propios.

## Contexto causal, sin suponer ejecución

`C12-04.M01.01` definió NR01–NR16 y P01–P10/N01–N10, `C12-04.M01.02` identifica A01–A14, I/O01–12, F01–F10; `C12-04.M01.03` inventarió 16 blobs Git del HEAD `213d08d646bf82e7a7e3648c41b5eb8f254642a8` y G01–G14. Los blobs son **prueba de código versionado**, no de volumen real, certificados ni existencia de MySQL privado. La Skill canónica y `references/architecture.md` describen Angular 22.2.1, ASP.NET Core net10.0, EF Core 10.0.12 y `MySql.EntityFrameworkCore` 10.0.9, JWT/RBAC/tenancy. Hostinger/MySQL privado y Cloudflare son un **objetivo futuro no activado**.

La prioridad es registrar riesgos hipotéticos, supuestos sin acreditar y decisiones con owner. Ninguna fila de este documento autoriza consultar Aiven, crear DB, descargar PII, modificar código, aplicar DDL, cambiar DNS ni ejecutar una prueba sobre producción.

## R01–R14 — riesgos potenciales y controles exigibles

| ID | Riesgo POTENCIAL (no confirmado como incidente) | Gate preventivo futuro |
| --- | --- | --- |
| R01 | Pérdida parcial de filas, FK o secuencias durante export/restore | Snapshot consistente, manifiesto por tabla y tenant, FK y conteos verificados; abortar ante divergencia no autorizada |
| R02 | Exfiltración de PII o secrets en dump, transporte o logs | Privilegios mínimos, cifrado, clasificación, redacción y cadena de custodia |
| R03 | Cruce entre tenants A/B por cambio de contexto o importación | Membresía y RBAC server-side, pruebas cruzadas positivas/negativas, sin confiar en tenant_id de cliente |
| R04 | Reactivación de tokens o permisos revocados tras restore | Prueba de JWT/RBAC y revocación, sincronización del modelo de membresía |
| R05 | DDL paralelo desde API, worker o migrador duplicado | Ejecutor único con lock/admisión y política de cambios compatibles |
| R06 | Incompatibilidad de provider EF/versión MySQL/collation/time zone | Matriz origen/destino y ensayo con schema versionado, sin sustituir proveedor |
| R07 | Estado parcial entre inventario, pagos, facturas y contabilidad | Consistencia transaccional, idempotencia y conciliación por entidad antes/después |
| R08 | Reintentos de carga duplican efectos tras cutover | Outbox/IDs idempotentes y prueba de redelivery al conmutar y revertir |
| R09 | Readiness falso con DB destino caída o latencia extrema | Health/connection checks separados de liveness y pruebas de fallo |
| R10 | Restore de prueba contamina producción o red compartida | Ambiente efímero aislado y pruebas sintéticas, sin credenciales productivas |
| R11 | DNS/TLS/cabeceras proxy crean ruta al tenant equivocado | Host binding y proxies confiables comprobados con doble empresa, sin fiar en x-forwarded-host libre |
| R12 | Roll back pierde deltas posteriores al punto de corte | Freeze, snapshot y reconciliación de escritura con owner y punto de no retorno |
| R13 | Ventana/costo/infraestructura asumidos sin aprobación | Gates humanos de proveedor, recursos, DNS, secrets, horario y presupuesto |
| R14 | Métricas y logs no permiten atribuir una discrepancia | Evidencia exact-SHA, manifiesto, timestamps UTC, correlation IDs saneados y acta |

R01/R07/R12 afectan datos financieros y documentales; la reversión no consiste solo en revertir un deploy. Los controles de consistencia deben contemplar escritura concurrente, usuarios activos, inventario, secuencias, fiscalidad y auditoría. R03/R04 requieren negativos de tenant A/B y JWT/RBAC con proveedor destino, sin atribuir capacidades globales a un proceso sin HttpContext. R11 exige distinguir `Host` confiable de `x-forwarded-host` controlado por cliente. Todos estos controles están **PENDIENTES_DE_PRUEBA**.

## S01–S10 — supuestos abiertos, nunca hechos certificados

| ID | Supuesto a comprobar | Estado y método de verificación |
| --- | --- | --- |
| S01 | Volumen real por tenant y tabla | NO_VERIFICADO; requerirá inventario autorizado, no extraer datos en M01 |
| S02 | RPO/RTO aceptables y duración de freeze | NO_VERIFICADO; requiere medición y decisión del propietario |
| S03 | Compatibilidad MySQL destino con esquema/EF actuales | NO_VERIFICADO; probar con servicio efímero en CI |
| S04 | Disponibilidad de export transaccional sin bloquear negocio | NO_VERIFICADO; ensayar según modelo real autorizado |
| S05 | Topología de redes privadas, TLS y Hostinger | NO_VERIFICADO; no provisionado por este documento |
| S06 | Existencia/retención de backups Aiven actuales | NO_VERIFICADO en esta corrida; no asumir existencia ni cero |
| S07 | Inventario de medios históricos externos | NO_VERIFICADO; Git solo prueba referencias de código |
| S08 | Seguridad de secretos y rotación en destino | NO_VERIFICADO; no duplicar connection strings |
| S09 | Capacidad de revertir sin pérdida de escrituras | NO_VERIFICADO; diseño y ensayo posterior exigidos |
| S10 | Owners técnicos y aprobaciones de cutover | NO_VERIFICADO; una ejecución programada no puede otorgarlas |

La ausencia de inventario real no significa que no existan clientes, imágenes o respaldos. `NO_VERIFICADO` significa falta de evidencia causal de la fuente correspondiente; **nunca se sustituye con un valor estimado ni se elimina el riesgo**. La disponibilidad de una cuenta conectada no autoriza operaciones sobre datos reales de clientes.

## D01–D10 — decisiones no tomadas y autoridad de cierre

| ID | Decisión requerida ANTES de una fase operativa | Responsable/entregable requerido |
| --- | --- | --- |
| D01 | Autoridad para iniciar export real, source y scope | Propietario/DBA aprobado; registro firmado y segregación de tenant |
| D02 | Dataset sintético, ambiente efímero y protocolo de restore | Equipo DB/seguridad; hash y conteos demostrados |
| D03 | Tecnología y versión definitiva de MySQL privado | Arquitectura/DBA; compatibilidad y tests EF |
| D04 | Estrategia de DDL único y compatibilidad expand/contract | Responsable backend/DB; lock y rollback |
| D05 | RPO/RTO, tolerancias y umbrales de divergencia | Propietario/operaciones tras medición; no inventar SLO |
| D06 | Modelo de identidad tenant/RBAC para procesos sin HTTP | Arquitectura y seguridad; pruebas A/B negativas |
| D07 | Freeze, sincronización de deltas y política de idempotencia | Negocio/DB; no facturas/pagos duplicados |
| D08 | Ventana de mantenimiento, corte DNS/TLS, rollback y punto de no retorno | Owner + operaciones tras ensayo verificable |
| D09 | Acceso mínimo, gestión de secretos, cifrado y retención | Seguridad/DBA y aprobación del propietario |
| D10 | Checklist de aceptación e independencia A5/QA/producción | Revisor distinto y autorizaciones diferenciadas; nunca inferidas de M01 |

Estas decisiones se mantienen **ABIERTAS**. El documento no concede una orden de compra, acceso a MySQL productivo, VPS, DNS, R2, Cloudflare, Hostinger, secrets, QA, main o PROD. Una futura ventana con controles no evita la necesidad de autorización humana del ámbito preciso, antes de ejecutarla.

## P01/N01–P08/N08 — pruebas futuras, TODAS NO_EJECUTADAS

| Caso | Positivo que demostrar posteriormente | Negativo de seguridad/consistencia |
| --- | --- | --- |
| P01/N01 | Manifest de snapshot sintético con hash, recuentos y origen | Hash inválido o tabla ausente causa NO_GO |
| P02/N02 | Restore efímero reproduce FKs, índices y secuencias | FK rota/collation distinta bloquea pase |
| P03/N03 | Tenant A opera solo sobre A con JWT/permiso correcto | B o tenant_id inyectado nunca accede a A |
| P04/N04 | Migraciones EF únicas versionadas y reversibles | DDL concurrente o versión incompatible falla |
| P05/N05 | Transacciones conservan pagos, inventario y auditoría | Doble retry no duplica cargo ni documento |
| P06/N06 | Readiness consulta dependencia DB efectiva | DB inaccesible no se anuncia disponible |
| P07/N07 | Freeze/rollback reconcilia deltas posteriores | Delta perdido o restore mezclado impide cortar |
| P08/N08 | Logs seguros correlacionan sin secretos ni datos sensibles | JWT, connection string o PII registrados causan FAIL |

No se ejecutaron esas pruebas aquí. El diseño de pruebas no equivale a A1/A2 runtime aprobado, ni a checksums o restores efectivos. Una futura implementación exige pruebas dirigidas .NET y MySQL efímero, tests negativos A/B, readback de migraciones, rollback demostrado y CI exact-SHA, con permisos nuevos cuando apliquen.

## Gate de cierre, impacto y rollback documental

- **A0:** contrastar STANDARD, PLAN/COLA/DAG, identidad, Skill, HEAD y Scope Lock; registrar riesgos sin cambios técnicos.
- **A1 documental:** los R14, S10, D10 y 8 pares P/N pueden revisarse por completitud y coherencia; ejecución positiva/negativa de migración = `FUTUROS_NO_EJECUTADOS`.
- **A2 documental:** revisar fail-closed JWT/RBAC, tenants A/B, secrets, mínimo privilegio, transacciones y exposición de PII; seguridad del nuevo destino NO_CERTIFICADA.
- **A3 documental:** relacionar G01–G14, contratos M01.01/.02 y fuentes M01.03, sin integración real MySQL/Hostinger.
- **A4:** publicar únicamente este Markdown y sección autocontenida de `CHANGELOG_AI.md` en `dev`, fast-forward CAS; checks Scope Lock y V5 `completed/success` en SHA exacto; readback de PLAN/COLA/BITACORA.
- **A5:** Task Javier revisora diferente del autor debe evaluar evidencia; propia entrega en `A5_PENDIENTE`.
- **Rollback:** revertir el changeset documental mediante commit normal sobre HEAD reevaluado; nunca force-push, borrar BITACORA ni sobrescribir autores ajenos.

**Impacto presente:** sin alteración Domain/Application/Infrastructure/API/DTO/MySQL/Angular, auth, provider, secret, CI, observabilidad o datos reales. M01.06, M03, QA/main/PROD y cutover siguen condicionados a pruebas, dependencias y autorizaciones adicionales.
