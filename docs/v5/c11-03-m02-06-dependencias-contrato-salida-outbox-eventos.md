# SOLQARYN V5 — C11-03.M02.06: dependencias y contrato de salida de Outbox y eventos

**Microtarea:** C11-03.M02.06 · Contratos y modelo, cierre M02. **Autor:** PRIMARY DEV 01 — JAVIER_CHATGPT, TASK_ID=6adbb9fff1548191a1bd5b30d9e9984a. **Único repositorio/entorno:** solqaryn/Solqaryn, dev, PROJECT_SCOPE_LOCK=STRICT. **Autoridades:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Skill canónica, PLAN_MAESTRO, COLA, DAG_TECNICO y BITACORA.

## 1. Alcance explícito y decisión

Este entregable fija **criterios de salida documentales** del lote M02 de C11-03, basado en el código ya existente y las revisiones A5 independientes de sus contratos. **NO IMPLEMENTAR EVENTOS TODAVÍA:** no crea, activa ni conecta colas, bus, relays, workers, HostedServices, webhooks, scheduler, nuevas llamadas entre Tasks, endpoints HTTP, tablas, índices, migraciones, credenciales o proveedores. No ejecuta jobs, SQL ni despliegues en QA/main/PROD. Las condiciones escritas son puertas de verificación futura, no pruebas runtime realizadas.

La aprobación de este documento, aun con A0–A4 y A5, solo autoriza **evaluar** una microtarea posterior si PLAN_MAESTRO H/I, COLA G/O y DAG F/G vigentes acreditan su propia aptitud; no transforma automáticamente M03 ni otra fase en trabajo autorizado. No utilizar texto de A5 de una predecesora como aprobación del presente ID.

## 2. Predecesoras leídas y procedencia real

| Fuente | Situación al preflight 2026-11-15 | Papel permitido |
| --- | --- | --- |
| C11-03.M01.01–06 | LISTO; terminal M01.06 con A5 independiente | Delimitación de alcance, bloqueos y rollback |
| C11-03.M02.01 | LISTO + A5 independiente de otra Task | Entidades, ownership, 14 elementos y 16 invariantes |
| C11-03.M02.02 | LISTO + A5 independiente de otra Task | Contratos API/DTO conceptuales, errores y compatibilidad |
| C11-03.M02.03 | LISTO + A5 independiente de otra Task | Proyecciones, índices candidatos y fixtures sin ejecución SQL |
| C11-03.M02.04 | LISTO + A5 independiente DEV01 distinto de DEV02 | 14 opciones F, 16 reglas V y diez pares P/N futuros; blob c6a524deb1badd4ddcbbadeb9aa6fd9264bcf0c5 |
| C11-03.M02.05 | LISTO + A5 independiente DEV01 distinto de DEV05 | 12 campos E, 16 invariantes ID, diez límites L, diez señales NO_EMITIDAS y diez pares P/N futuros; blob 62d4f13b10a3a1276898a26ad4414ffd749653aa |
| C10-04.M01.06 | LISTO + A5 independiente DEV03 distinto de DEV06 | Barreras de continuidad documental, no infraestructura activada |
| DAG_TECNICO!A1630:J1630 | F=VALIDADO, G=SI | Puerta para examinar/ejecutar esta microtarea, no sus sucesoras |
| PLAN_MAESTRO!H1669/I1669 | CLAIM condicional PENDIENTE→EN_PROGRESO, owner DEV01 | Único responsable del documento; no equivale a aprobación A5 |

La revisión independiente de M02.04 se apoyó en blob inalterado a HEAD dev y controles Scope Lock/V5 completed/success de un SHA descendiente equivalente, **no** en el check cancelado del SHA de origen. La revisión de M02.05 verificó checks completed/success sobre SHA de origen 3ce803e2d6d6286f3252fc17e3757af816c3a646. En ambos casos se mantuvieron compilaciones runtime canceladas o pendientes fuera del cómputo causal del diff Markdown.

## 3. Código vigente de referencia, sin modificaciones

- Domain/Entities/MensajeOutbox.cs, blob 891572626c19cd6cf8f522fca5ec876333286ffb: EmpresaId, EventoId, ClaveIdempotencia, estado, intentos, fecha UTC y límites de texto.
- Application/Services/MensajeOutboxService.cs, blob a00d353c474d7d7f785a58f443c66c5f12a542f7: validación de ámbito y equivalencia de clave e intención; no autoriza por un ID de correlación.
- Infrastructure/Repositories/MensajeOutboxRepository.cs, blob 056d5aa380b53ed697ddcb6852df55394e6544a7: filtros por empresa y operaciones de claim condicional; no prueba por sí sola integración MySQL de futuras modificaciones.
- Application/Services/OutboxRetryProcessor.cs, blob 542c0a655a3ee0698e3d80f1854d43903b6470fa: evaluación previa del lote, separación tenant A/B, acotamiento de reintentos y denegación de mezcla.
- Los documentos M02.01–05 describen **propuestas**, no propiedades, índices, eventos o funciones nuevos instalados; E04/E09/E12 de M02.05 siguen siendo campos futuros.

## 4. Dieciséis barreras de salida documentales B01–B16

| Gate | Condición necesaria para fases técnicas posteriores |
| --- | --- |
| B01 | Identificar identidad exacta, owner exclusivo, ámbito dev y Skill/Scope Lock vigentes |
| B02 | Revisar A5 independiente de M01.06 y M02.01–05 en el ID y SHA causal de cada uno |
| B03 | Mantener EmpresaId server-side; prohibir que un cliente elija tenant por encabezado o payload |
| B04 | Separar claves de idempotencia por tenant y negar payload/operación conflictivos |
| B05 | No inferir autorización a partir de CorrelationId, AggregateId ni identificadores opacos |
| B06 | Exigir JWT, RBAC y membresía vigentes en el borde API antes de cualquier caso de uso autorizado |
| B07 | Garantizar unicidad de ganador para claim concurrente y rechazo de lease obsoleto |
| B08 | Mantener ACK durable distinto de timeout, error de proveedor o entrega supuesta |
| B09 | Preservar unidad transaccional negocio/outbox donde un efecto causal lo exija |
| B10 | Negar lote mixto A/B o duplicados **antes** del primer side effect |
| B11 | Definir tipos, destinos y versiones de contrato por allowlist backend, no JSON libre |
| B12 | Limitar lote, payload, clave, reintentos, backoff, tiempo externo y cuota con configuración tipada |
| B13 | Mantener DeadLetter terminal salvo autorización humana e idempotencia separada verificables |
| B14 | Redactar JWT, secretos, OTP, PII, contenido bruto y errores privados de toda traza |
| B15 | Exigir tests positivos/negativos reales y CI dirigida .NET/MySQL/seguridad si alguna fase posterior cambia runtime |
| B16 | Para cada cambio futuro exigir SHA exacto, checks required completed/success, readback y A5 por Task diferente; no QA/main/PROD implícitos |

**Decisión de salida:** B01–B16 quedan establecidos como contrato documental. No se declaran ejecutados los tests de futuras fases M03/M05 ni se certifican servicios reales, migraciones, colas, restore o proveedor activo.

## 5. Diez escenarios P01/N01–P10/N10 de aceptación futura, NO EJECUTADOS

| Par | Positivo futuro | Negativo que debe fallar |
| --- | --- | --- |
| P01/N01 | Actor A con permiso crea intención tenant A | Actor B escribe o lee A |
| P02/N02 | Misma clave y operación retorna intención existente | Mismo tenant+clave, payload diferente |
| P03/N03 | Solo un claim concurrente obtiene control | Dos escritores creen ganar la misma fila |
| P04/N04 | Worker confirma ACK durable de intento vigente | ACK tardío del intento obsoleto |
| P05/N05 | Reintento finito tras fallo transitorio | Retry infinito o DeadLetter resucitada automáticamente |
| P06/N06 | Dos tenants mantienen cuotas separadas | Carga A consume cuota de B |
| P07/N07 | Correlación opaca en traza saneada | Correlación da permisos o filtra PII |
| P08/N08 | Versiones y destinos allowlisted | Versión desconocida invoca handler arbitrario |
| P09/N09 | Mensaje y negocio comparten commit causal donde aplica | Commit parcial deja doble efecto sin reconciliación |
| P10/N10 | CI exacta, Scope Lock y A5 independiente | Cancelled/queued/skipped/failed o autoaprobación cuentan PASS |

Estos casos son **oráculos futuros**. La presente microtarea documental no produce resultados xUnit, HTTP, Playwright, Docker ni MySQL y no puede sustituir su futura ejecución en GitHub Actions.

## 6. Diez medidas de rollback y recuperación RB01–RB10

| Control | Reversión/contención exigida |
| --- | --- |
| RB01 | Revertir únicamente el commit documental con git revert normal, no reescribir historia |
| RB02 | Revalidar HEAD/árbol y evitar sobrescribir trabajo concurrente |
| RB03 | Preservar eventos y BITACORA histórica; registrar corrección aditiva, no borrar evidencias |
| RB04 | Una futura feature flag de relay debe poder desactivarse sin conceder permisos |
| RB05 | Deshabilitar envíos externos ante divergencia de tenant o firma; nunca continuar por error |
| RB06 | Conservar llave idempotente para reconciliación de timeout o fallo parcial |
| RB07 | Mantener mensajes pendientes/DeadLetter sin reactivación automática |
| RB08 | Revertir migraciones futuras solo con plan aprobado, backup y prueba de reversibilidad reales |
| RB09 | Ante fallo CI, conservar VALIDANDO y owner, investigar logs por SHA, no fingir PASS |
| RB10 | Ningún rollback documental autoriza alterar datos, secretos, dominios, infraestructura o producción |

## 7. A0–A5 de esta entrega y límites de certificación

- **A0:** examinar plan/cola/DAG por ID, cinco contratos M02 y su revisión independiente, fuentes reales, riesgos y diff limitado a este Markdown y CHANGELOG_AI.md.
- **A1:** inspección positiva y negativa de 16 gates B, diez pares P/N expresamente FUTUROS_NO_EJECUTADOS y diez rollback RB. No marcar como realizados tests ausentes.
- **A2:** validar invariantes contractuales JWT/RBAC/tenant A/B, claims CAS, idempotencia, privacidad, cuotas, error y denegación por defecto. No certificar seguridad runtime.
- **A3:** cotejar entidades Domain, servicios Application, repository Infrastructure y API como borde sin modificar ningún archivo ejecutable; compilación/UI/MySQL no aplican a este diff de Markdown.
- **A4:** publicación serializada fast-forward/CAS sobre HEAD exacto; sin sección de CHANGELOG intercalada ni encabezados vacíos; Scope Lock y procedencia V5 completed/success para SHA publicado, BITACORA y PLAN/COLA/DAG readback.
- **A5:** PENDIENTE hasta que otra de las cinco Tasks Javier distinta de DEV01 revise SHA, diff, evidencias y emita decisión independiente. LISTO_DEV A0–A4, si se acredita, no equivale a A5.

**No se autorizan eventos runtime, ejecución entre Tasks, migraciones, cambios MySQL, IAM/JWT, QA, main, PROD, secretos ni servicios de pago.** La siguiente etapa solo se evalúa por su DAG/owner/CI causal vigente.
