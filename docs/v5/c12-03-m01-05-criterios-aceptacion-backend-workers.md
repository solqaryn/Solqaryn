# SOLQARYN V5 — C12-03.M01.05: aceptación observable backend y workers separados

**Fecha:** 2026-11-14. **Microtarea:** C12-03.M01.05. **PLAN:** fila 1806. **Autor:** PRIMARY DEV 03 JAVIER / `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Rama:** `solqaryn/Solqaryn@dev`. **Tipo:** solo contrato documental de preparación M01.

## Autoridad, fuentes, alcance y dependencias

[STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN_MAESTRO/COLA/DAG](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit) definen el resultado esperado: criterios funcionales/técnicos observables para **API, jobs, migraciones, health, cola, escalamiento**. Se comprobó autorización solo de esta preparación documental (PLAN PENDIENTE sin owner, COLA G/O SI y DAG VALIDADO/SI) antes de reclamar.

Antecedentes verificables: [M01.01, NR01–NR16](c12-03-m01-01-alcance-backend-workers-separados.md); [M01.02, A01–A14 y F01–F12](c12-03-m01-02-actores-superficies-backend-workers-separados.md); [M01.03, inventario SRC01–SRC15 y gaps G01–G12](c12-03-m01-03-inventario-codigo-evidencia-backend-workers.md); [M01.04, riesgos R01–R14, supuestos S01–S10 y decisiones D01–D10](c12-03-m01-04-riesgos-supuestos-decisiones-backend-workers.md). Stack de referencia vigente: ASP.NET Core net10.0, EF Core 10.0.12 con MySql.EntityFrameworkCore 10.0.9, Domain ← Application ← Infrastructure, API composition root, JWT/RBAC/tenant verificado.

**Límites:** los siguientes son **contratos para futuras pruebas** y **NO_EJECUTADOS**. No existe aquí evidencia de worker dedicado, cola duradera, outbox, reentrega, migración, despliegue, servicio de terceros o tráfico productivo. La condición `HttpContext` ausente **no** concede tenant global ni autorización. No se implementan eventos, webhooks, dispatcher ni Tasks.

## Matriz de criterios positivos (AC01–AC16) y rechazos (N01–N16)

Cada par es un criterio **futuro** con observación esperada, no un resultado certificado. La evidencia requerida en M03–M05 incluirá SHA exacto, comando CI, logs saneados, dataset ficticio A/B, caso negativo, causa y rollback.

| ID | Aceptación positiva futura (AC) | Negativo obligatorio (N) | Evidencia y vínculo |
|---|---|---|---|
| AC01/N01 | API net10.0 compila, arranca y aplica DI/guardas | JWT faltante o inválido nunca crea job | build/test GitHub exact-SHA; NR03/G01 |
| AC02/N02 | Usuario autorizado del tenant A solicita trabajo sobre A | Parámetro/header/slug B no permite leer/escribir B | HTTP+Application A/B, NR04/06/R01 |
| AC03/N03 | Membresía y RBAC vigentes permiten ejecutar | Revocación entre creación y consumo deniega | test de revocación diferida, R02/G04 |
| AC04/N04 | Mensaje durable se recupera tras retry | Redelivery no duplica documento/efecto externo | test de cola y deduplicación, G02/G05 |
| AC05/N05 | Identidad job se revalida antes de la transacción | Worker sin HttpContext ni binding tenant falla cerrado | test ausencia contexto, R01/S04 |
| AC06/N06 | Transaction commit y ack mantienen consistencia | Crash/kill antes de commit nunca confirma ack | MySQL efímero con restart, G07/R04 |
| AC07/N07 | Un único dueño aplica migraciones versionadas | API+worker concurrentes no ejecutan DDL dos veces | CI MySQL dos procesos, R05/D05 |
| AC08/N08 | Health/readiness indica dependencia realmente sana | MySQL caído o configuración crítica ausente da not-ready | prueba negativa de readiness, G09/R06 |
| AC09/N09 | Secrets son externos por proceso | Ausencia de JWT/DB secret bloquea operación; logs no revelan secreto | CI redacted logs, G08/R07 |
| AC10/N10 | Correlación job/tenant utiliza ID opaco auditable | No se registra JWT, PII, credenciales ni datos B | inspección logs y redaction test, G10 |
| AC11/N11 | Retry usa backoff, cuota y dead-letter explícitos | Saturación no genera loop infinito ni llamada repetida sin límite | prueba de contador/quota, R11/D04 |
| AC12/N12 | Timeout proveedor se clasifica/reintenta de forma idempotente | Resultado ambiguo no se registra como éxito | contrato Cloudinary/SMTP simulado, R12 |
| AC13/N13 | Versiones anterior/nueva de payload conviven según política | Consumidor previo rechaza payload incompatible de modo seguro | contract test DTO/job, G11/D08 |
| AC14/N14 | Apagado controlado respeta deadlines y estado persistente | Kill durante ejecución no deja operación parcial como exitosa | replay/restart test, NR15/R10 |
| AC15/N15 | A0–A4 tienen evidencia del SHA publicado | cancelled/skipped/queued/in_progress no son PASS requerido | check-runs GitHub exact-SHA, R13 |
| AC16/N16 | Rollback restaura comportamiento sin pérdida de datos | Revert de código no borra filas reales ni cambia tenant | plan de restore en CI efímera, R14/S10 |

**Regla de aceptación:** ningún AC positivo implica que su N negativo pasó. Un test marcado SKIPPED/CANCELLED/FAIL/IN_PROGRESS no satisface un gate requerido. Si una prueba necesita MySQL/Docker/Node/.NET real, utilizar GitHub Actions en SHA exacto; no sustituir con lectura textual. El productor de M01 no certifica los AC de runtime.

## NF01–NF10: condiciones no funcionales medibles, sin inventar SLO

| ID | Métrica/gate a establecer | Observabilidad o criterio de bloqueo |
|---|---|---|
| NF01 | Latencia API p50/p95/p99 por ruta y tenant ficticio | Registrar baseline; umbral futuro con autorización técnica |
| NF02 | Edad de trabajo en cola y duración por etapa | Histograma/percentiles sin payload ni PII |
| NF03 | Duplicados de entrega vs duplicados de efecto | Debe acreditarse **cero efecto adicional** en prueba determinista |
| NF04 | Recovery ante kill/restart | Tiempo a recuperación medido y transacción consistente |
| NF05 | Backpressure y máximos de intentos | Cuota y límite por cliente/proveedor aprobados antes de activar |
| NF06 | Disponibilidad de MySQL y dependencia esencial | Readiness cambia a negativa bajo simulación, sin falso verde |
| NF07 | Error/authz denegaciones tenant A/B | Casos negativos documentados; no volcar credenciales en métricas |
| NF08 | Compatibilidad de contratos durante rollback | Tests del cliente previo y esquema compatible |
| NF09 | Exclusión de secretos en fuentes, artefactos y logs | Scan dirigido y prueba de ausencia |
| NF10 | Reproducibilidad de CI y trazabilidad exact-SHA | Mismo commit, hash, suite, logs, fallos y reversión verificable |

Los umbrales (ms, tasa, tamaño de cola, TTL, reintentos, RTO/RPO, capacidad) **no se fijan artificialmente**. Dependen de carga real, arquitectura aprobada y autorización futura. Cualquier ausencia de baseline se registra como brecha, no como éxito.

## Evidencia exigida por fase, y criterio de no-promoción

- **M01 (actual):** fuente Git/doc, matriz AC/N/NF, revisión estática de amenazas JWT/RBAC/tenant/PII, CHANGELOG sección autocontenida, check de Scope Lock y procedencia V5 exact-SHA, readback institucional; pruebas runtime **NO_APLICA** por cambios Markdown.
- **M02:** aprobar contrato formal de mensajes, tenant binding fuera de HTTP, idempotencia, transacciones, retry/TTL/dead-letter, compatibilidad de payload y dueño DDL; R/S/D abiertos deben tener decisión o NO_GO.
- **M03:** implementar código real exclusivamente en microtarea habilitada; CI de .NET10/EF/MySQL servicio efímero, positivo/negativo A/B, JWT/RBAC/revocación, integración y rollback; no inventar PASS por documentación.
- **M04/M05:** pruebas de consumo, reentrega, kill/restart, dependencia caída, observabilidad, carga y fallback proporcionadas al cambio real. Playwright solo si hay UI afectada.
- **M06:** A0–A4, A5 por Task Javier diferente, prerequisitos LISTO y CI causal del SHA; **no** es permiso QA/main/PROD por sí mismo.

## A0–A4 documental, seguridad, impacto y reversión

A0: método, dependencia y autoridad contrastados con DAG/Plan y Skill. A1: consistencia de 16 AC, 16 N y 10 NF verificable **como contrato**, no ejecución funcional. A2: revisión documental de fail-closed sin contexto tenant, RBAC/membresía revocada, secretos y PII; no se alteran controles actuales. A3: matriz trazada a R01–R14, S01–S10, D01–D10, G01–G12; **NO_APLICA** integración runtime por ausencia de diff de código/DB/UI. A4: exige publicación fast-forward, diff de dos Markdown, checks de Scope Lock y V5 procedencia completados SUCCESS del SHA exacto y readback PLAN/COLA/BITACORA. A5 independiente de otro Task Javier sigue pendiente.

No hay cambios de API, controlador, DTO, Domain, Application, Infrastructure, MySQL, migración, JWT, RBAC, tenants, UI, worker, proveedor, DNS, dominio, Cloudinary/SMTP, secretos o producción. Rollback por **git revert normal** del commit documental, manteniendo BITACORA histórica intacta, sin reset/force-push. La publicación en `dev` no habilita QA/main/PROD.
