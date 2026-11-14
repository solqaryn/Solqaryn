# SOLQARYN V5 — C09-04.M01.05: criterios de aceptación observable de mensajería multicanal

**Microtarea:** C09-04.M01.05 · PLAN_MAESTRO fila 1410 · **Autor:** PRIMARY DEV 06 JAVIER, TASK_ID 6af16bbad26881918c283abe06e1f32a · **Fecha:** 2026-11-14 · `solqaryn/Solqaryn@dev` · PROJECT_SCOPE_LOCK=STRICT.

**Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Resultado de fase futura:** adaptadores email, WhatsApp y proveedores adicionales con protección de webhook, replay, privacidad y no duplicación; evidencia runtime y permisos. Esta M01.05 **define aceptación para fases futuras**, no ejecuta tests ni implementa, invoca o habilita sistemas de mensajería, dispatcher, workers o eventos.

## Procedencia y prerequisitos

- `C09-04.M01.01`: `docs/v5/c09-04-m01-01-alcance-mensajeria-multicanal.md`, blob `dbbed9aa74fa10f4b5e44dadf62edd2b7983ab24`, define NR01–NR14 y no-alcance.
- `C09-04.M01.02`: `docs/v5/c09-04-m01-02-actores-interfaces-mensajeria-multicanal.md`, blob `ab286319a4903bc0eb861eee76873a259455f362`, define A01–A14/I01–I12/F01–F09.
- `C09-04.M01.03`: `docs/v5/c09-04-m01-03-inventario-codigo-brechas-mensajeria.md`, blob `3626d69599d4c96f87c69464558dac31a9fa9ba8`, inventaría 14 archivos base y G01–G12 como interrogantes, no vulnerabilidades confirmadas.
- `C09-04.M01.04`: `docs/v5/c09-04-m01-04-riesgos-supuestos-decisiones-mensajeria.md`, blob `09bae123a9c31eb5b3726c8f72b1f09c2adc5dd5`, registra R01–R14, S01–S08 y D01–D08 abiertos.
- `DAG_TECNICO!F1371/G1371=VALIDADO/SI` permite **solo preparación documental** cuando PLAN!H1410=PENDIENTE e I libre y COLA!G1410/O1410=SI; este gate no autoriza la ejecución de capacidades futuras.

## Matriz de aceptación positiva P01–P16

| Caso futuro | Preparación necesaria | Oráculo observable de aprobación |
| --- | --- | --- |
| P01 Identidad tenant | JWT válido, membresía empresarial vigente | El servidor asocia sólo empresa del usuario |
| P02 Rol y permiso | Actor autorizado para tipo de mensaje | Intención aceptada en el ámbito permitido |
| P03 Sucursal | Sucursal perteneciente a empresa | Mensaje referencia sucursal válida |
| P04 Intención durable | Request bien formado y clave idempotente | Identificador persistido una sola vez |
| P05 Retry idempotente | Misma clave y mismo contenido | Repetición responde mismo resultado sin efecto extra |
| P06 Claim concurrente | Dos workers efímeros sobre la misma intención | Sólo uno reclama el efecto |
| P07 Dead-letter | Proveedor ficticio falla reiteradamente | Límite de intentos y estado terminal auditable |
| P08 Consentimiento | Opt-in real por tenant, finalidad y canal | Canal promocional permitido sólo mientras vigente |
| P09 Revocación | Opt-out confirmado previo a dispatch | Estado nuevo bloquea cualquier envío futuro |
| P10 Plantilla segura | Variables autorizadas de tenant A | Render no expone datos de tenant B |
| P11 Email transaccional | Intención comercial válida y configuración segura | Envío de prueba mediante adaptador simulado |
| P12 WhatsApp autorizado | Referencias opacas de secreto y canal permitido | Acuse simulado correlacionado, nunca token claro |
| P13 Callback firmado futuro | HMAC válida, nonce vigente, tenant conocido | Acuse idempotente procesado una sola vez |
| P14 Traza mínima | CorrelationId y actor verificados | Auditoría sin cuerpos, tokens ni PII innecesaria |
| P15 Recuperación | Caída simulada tras persistir intención | Reanudación segura sin duplicar transacción comercial |
| P16 Desactivación | Flag/canal revocado con política autorizada | Ningún envío nuevo y estado conservado para auditoría |

Estos criterios demandan pruebas automatizadas positivas reales de .NET/EF Core/MySQL efímero/API o test de contrato con proveedor falso; no basta inspeccionar Markdown, obtener build SUCCESS o que exista la clase `OutboxRetryProcessor`.

## Matriz de aceptación negativa N01–N16

| Caso futuro | Estímulo adverso | Oráculo mínimo exigido |
| --- | --- | --- |
| N01 Tenant cruzado | Empresa A intenta envío de B | Denegación sin persistencia ni envío |
| N02 Rol insuficiente | JWT válido sin permiso del canal | Denegación server-side |
| N03 Sucursal ajena | Referencia sucursal de otra empresa | Denegación sin revelar existencia |
| N04 Tenant legado NULL | Contacto sin pertenencia reconciliada | Fail-closed, sin atribución tácita |
| N05 Consentimiento revocado | Marketing tras opt-out | No se envía y queda evidencia no sensible |
| N06 Finalidad distinta | Opt-in transaccional reutilizado para campaña | Rechazo por cambio de finalidad |
| N07 Idempotencia conflictiva | Misma clave, payload distinto | Conflicto sin nuevo efecto |
| N08 Doble worker | Dos claims concurrentes, mismo ID | No doble dispatch |
| N09 HMAC falsa | Callback con firma incorrecta | Rechazo sin mutar estado |
| N10 Replay vencido | Callback duplicado fuera de ventana | Denegación sin nuevo efecto |
| N11 Secreto expuesto | Token en JSON/log/error | Prueba falla y salida queda redacted |
| N12 PII cruzada | Plantilla de A contiene dato de B | Denegación y no exposición |
| N13 Cuota superada | Más intentos que el máximo | Dead-letter y cero retries adicionales |
| N14 Fallback implícito | WhatsApp no disponible; email no consentido | No envío alternativo |
| N15 Proveedor indisponible | Timeouts/reintentos no recuperables | Se conserva transacción comercial, sin pérdida de control |
| N16 Exportación ajena | Usuario A busca trazas de B | Respuesta tenant-safe y sin filas cruzadas |

Todos los escenarios P01–P16/N01–N16 tienen etiqueta **`FUTUROS_NO_EJECUTADOS`**. Los códigos HTTP definitivos dependen del endpoint realmente autorizado; no se inventa 200/403 para rutas que no existen en la unidad documental. Una implementación futura debe documentar los códigos efectivos, demostrar el rechazo y registrar los artifacts reales del CI con SHA exacto.

## Requisitos no funcionales RNF01–RNF10

| Requisito | Criterio observacional futuro |
| --- | --- |
| RNF01 Aislamiento | Test tenant A/B para toda consulta, lectura y escritura |
| RNF02 Idempotencia | Simulación concurrente y persistencia sin duplicación |
| RNF03 Privacidad | Secret scanner, redacción logs y respuestas sin PII cruzada |
| RNF04 Disponibilidad | Timeout y degradación por proveedor, sin afectar contabilidad |
| RNF05 Resiliencia | Reintentos limitados, dead-letter y rollback |
| RNF06 Observabilidad | Estado/correlación por intención, sin cuerpo sensible |
| RNF07 Accesibilidad UI | Errores y estados comprensibles con teclado y lector de pantalla |
| RNF08 Rendimiento | Perfil de colas y UI bajo carga representativa definida |
| RNF09 Control de cuotas | Límites por tenant/canal sin bucles infinitos |
| RNF10 Reversibilidad | Apagar canal/worker de forma segura y auditable |

No se establecen números de throughput/latencia arbitrarios; los presupuestos se fijarán a partir de carga y recursos realmente disponibles y se validarán en pruebas pertinentes de código.

## Matriz de trazabilidad y puertas posteriores

Los criterios P/N cubren NR01–NR14, I01–I12, F01–F09 y riesgos R01–R14 de las cuatro unidades anteriores. El plan de ejecución técnica posterior exige: A0 diff/arquitectura/seguridad, A1 positivos y negativos, A2 tenant/JWT/RBAC/PII/secretos, A3 xUnit + MySQL efímero/HTTP/Playwright cuando corresponda, A4 GitHub Actions exact SHA con logs y readback, A5 independiente en una de las cinco otras Tasks Javier. Un `queued`, `skipped`, `cancelled`, `in_progress` o FAIL nunca equivale a PASS de prueba causal.

**No autorización adicional:** este documento no habilita activar eventos, webhooks, dispatcher, SMTP real, WhatsApp real, migraciones, proveedores comerciales, cuentas de pago, DNS, secrets, QA/main/PROD ni cualquier disparador de Tasks. Cualquier decisión D01–D08 aún abierta requiere una fase con gate concreto.

## Cierre de esta unidad documental y reversibilidad

Para el cierre de C09-04.M01.05 se requiere corroborar el documento de aceptación, los blobs, el diff de sólo `docs/v5` + `CHANGELOG_AI.md`, el HEAD fast-forward, y el éxito `completed/success` de `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` en SHA exacto. Clasificar suites runtime ajenas como `NO_APLICA` para este diff, no como PASS. Registrar BITACORA de 26 campos, PLAN/COLA readback, A5_PENDIENTE y mantener autor original.

Rollback reversible con `git revert` normal de este commit únicamente en dev, preservando historia de BITACORA. Cero force-push, no QA/main/PROD.

**Resultado:** matriz observable de 16 casos positivos, 16 negativos y 10 requisitos no funcionales, todos para ejecución futura y sujetos a pruebas causales reales; cero implementaciones nuevas.
