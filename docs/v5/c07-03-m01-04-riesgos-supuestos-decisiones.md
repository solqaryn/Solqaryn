# SOLQARYN V5 — C07-03.M01.04 — Riesgos, supuestos y decisiones de inventario/compras

**Autor:** PRIMARY DEV 01 — JAVIER, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Fecha:** 2026-11-14. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, C07-03.M01.04. **Naturaleza:** análisis y contrato documental; no modifica inventario, costeo, pedidos, interfaces, seguridad o runtime.

## Fuentes, método y alcance

Se contrastaron los tres contratos ya existentes del bloque, sin atribuirse su autoría: `docs/v5/c07-03-m01-01-alcance-inventario-compras-operaciones.md` (blob `200792a55ed6fced30ae5d5e4cb45104f7210507`, NR01–NR12), `docs/v5/c07-03-m01-02-actores-interfaces-inventario-compras-operaciones.md` (blob `f8ac2a714841adf3804ad24374e03162b0d69a7e`, A01–A12/I01–I12/B01–B10) y `docs/v5/c07-03-m01-03-inventario-codigo-contratos-evidencias.md` (blob `382bbd606993c0103399a22a6056415f84e88a80`, 20 fuentes E01–E20 y cinco archivos de pruebas existentes). La matriz previa aporta referencias Git al código actual y brechas G01–G10. **Un test fuente existente no es un test ejecutado**, y una transacción descrita no equivale a haber probado aislamiento real ni rollback.

Se preservan almacenes, sucursales, compras, reservas, transferencias, conteos, existencias, kardex, costos, reportes y trazabilidad ERP. La autoridad real exige JWT/RBAC y tenant empresarial verificado por servidor, control de sucursal/almacén, estado de documento, consistencia durable y evidencia causal. Ningún ID de navegador es autorización. No se realizarán aquí migraciones, órdenes, ajustes de stock, workers ni operaciones de producción.

## R01–R12 — riesgos potenciales, no incidentes confirmados

| ID | Hipótesis de riesgo | Control, ensayo o evidencia para evaluarlo |
| --- | --- | --- |
| R01 | Usuario de empresa A consulta o actualiza almacén, costo o movimiento B por ID | Pruebas HTTP y repositorio A/B, filtrado por `Almacen.Sucursal.EmpresaId`, permisos negativos |
| R02 | Almacén con sucursal ajena o jerarquía ausente acepta operación | Negativas de parent/child y relaciones EF, rechazo fail-closed |
| R03 | Stock reservado se consume o libera dos veces en carreras concurrentes | Transacciones MySQL efímeras, locks reales, replay y conteos invariantes |
| R04 | Recepción de una misma orden se duplica mediante retry o clave idempotente | Dos POST concurrentes, constraint durable y fingerprint de request |
| R05 | Transferencia descontada en origen se acredita dos veces en destino | Estados aprobación/despacho/recepción, transacción y compensación causal |
| R06 | Conteo con diferencias provoca ajuste sin confirmación o autoridad | Prueba negativa sin permiso; distinguir borrador de ajuste confirmado |
| R07 | Costos/políticas o snapshot histórico se revalorizan retroactivamente | Reconstrucción histórica, invariantes de costo, auditoría y reversión |
| R08 | Existencias de producto/variante `EmpresaId=NULL` legado se vuelven públicas o interempresa | Pruebas A/B y cuarentena explícita de NULL en todas las lecturas |
| R09 | Reporte de reconciliación filtra costos internos a perfil sin Finanzas | Matriz de permisos, redacción condicional, exportación y cache |
| R10 | Error transaccional parcial deja kardex, capa de costo y stock inconciliables | Inyección de fallo en mitad de operación, rollback y conciliación 1:1 |
| R11 | Lotes, números de serie, correlación o referencias cruzan sucursal/empresa | Validar FK, ownership y trazas de `CorrelationId` con payload ajeno |
| R12 | Timeout o operación offline induce UI a confirmar movimiento no persistido | Prueba integración/API+Angular con errores/reintentos y estados verificables |

**Clasificación:** sin pruebas de explotación; R01–R12 son caminos a investigar, no hallazgos de seguridad o defectos comprobados. Las pruebas y servicios preexistentes en M01.03 deben ejecutarse en las microtareas de validación que correspondan, con tests positivos/negativos y logs.

## S01–S08 — supuestos falsables

| ID | Supuesto no certificado | Evidencia necesaria |
| --- | --- | --- |
| S01 | Todas las rutas de almacén resuelven sucursal y empresa autorizadas | Mapa completo API–Application–Repository y prueba A/B |
| S02 | Stock físico, disponible y reservado se reconcilian en toda operación | Reconciliación por variante/almacén antes/después de transacciones |
| S03 | Ordenes y recepciones tienen idempotencia durable para reintentos simultáneos | Índices/unique keys, MySQL efímero, fingerprint y double submit |
| S04 | Estado de transferencia impide pasos fuera de orden y reversión indebida | Pruebas negativas en cada transición, roles y transacción |
| S05 | Conteos sólo producen ajustes confirmados explícitamente | Verificar flujo borrador→validar→confirmar y auditoría |
| S06 | Las capas de costo y los saldos históricos se mantienen tras errores | Suite de rollback y consistencia con snapshots fechados |
| S07 | Reportes ocultan costo/PII a roles ajenos, también en exportación | Pruebas API, permisos y cache/redacción con usuario sin Finanzas |
| S08 | UI de inventario conserva accesibilidad y estado tras fallos de red | Playwright responsive, teclado, foco y duplicado de peticiones |

## D01–D08 — decisiones técnicas pendientes

| ID | Decisión no tomada | Condición mínima para decidir |
| --- | --- | --- |
| D01 | Autoridad de tenant/sucursal/almacén y herencia de permisos | Especificación de rechazo A/B, NULL y FK |
| D02 | Lock y transacción de reserva/consumo/expiración | Carrera simultánea reproducible y consistencia del stock |
| D03 | Idempotencia de orden y recepción entre API/DB | Clave, fingerprint, constraint y respuesta de replay verificables |
| D04 | Máquina de estados de transferencias, recepciones y ajustes | Pruebas de transición inválida y reversión con auditoría |
| D05 | Política histórica de costeo, redondeos y snapshots | Reglas contables con no-regresión y rollback |
| D06 | Alcance de reconciliación y censura de datos de costo | Matriz RBAC y pruebas export/consulta bajo múltiples roles |
| D07 | Telemetría/auditoría de correlación, PII y errores | Logging redacted, cardinalidad y retención proporcional |
| D08 | Criterios de aceptación, fallo e interrupción segura en móvil | Contratos observables, CI dirigida y rollback sin mutar PROD |

Todas las decisiones están **PENDIENTES**. Este documento no autoriza nuevas políticas, ajustes, schemas, despliegues ni pagos.

## Oráculos P01–P08 y N01–N14 — FUTUROS NO_EJECUTADOS

**P01:** almacén propio por sucursal; **P02:** reservar stock disponible; **P03:** consumir exactamente una vez; **P04:** crear orden de compra propia idempotente; **P05:** recibir contra saldo de orden; **P06:** transferir origen→destino con saldos reconciliados; **P07:** conteo crea borrador y confirma con autoridad; **P08:** reporte de costo bajo permiso válido.

**N01:** empresa B por ID en A; **N02:** almacén de otra sucursal; **N03:** producto o variante NULL legado; **N04:** reserva mayor a disponible; **N05:** doble consumo; **N06:** orden mismo key y payload diferente; **N07:** recepción duplicada concurrente; **N08:** recibir transferencia sin despacho; **N09:** volver a acreditar transferencia recibida; **N10:** ajuste sin confirmar conteo; **N11:** revalorizar costo histórico sin autoridad; **N12:** reporte con costo a rol sin Finanzas; **N13:** fallo transaccional deja movimiento sin kardex; **N14:** expiración o timeout provoca movimiento duplicado. Todos requieren implementación/ejecución en CI causal posterior, no son pruebas PASS.

## Impacto, dependencias, auditoría y rollback

- Dependencias oficiales de preparación C01-01–04, C02-01–04, C05-01–04 y C07-01 M01.01–05 (13 familias, **65/65** terminales según preflight); `DAG_TECNICO!A1046:J1046` F=VALIDADO/G=SI; `COLA!G1085/O1085=SI` y `PLAN_MAESTRO!H1085=PENDIENTE, I1085 vacío` antes del claim. No se autoriza M01.06/M02 ni cambios de seguridad, base de datos, instalaciones o producción.
- **A0:** revisión de contratos M01.01–03 y 20 fuentes/5 archivos de pruebas referenciados, con diferenciación de observación y prueba. **A1:** consistencia de riesgos, supuestos, decisiones y futuros P/N; ninguna prueba funcional ejecutada. **A2:** matriz de riesgos tenant/RBAC/NULL/PII, negativa de acceso como hipótesis sin validación runtime. **A3:** `NO_APLICA` .NET, Angular, EF/MySQL y Playwright a la publicación exclusivamente Markdown; no son PASS. **A4:** commits serializados en dev, checks obligatorios Scope Lock y procedencia V5 `completed/success` sobre SHA exacto, readback de PLAN/COLA/BITACORA.
- **A5:** revisión independiente por Task Javier distinta de la autora; no autoaprobación ni promoción.
- **Rollback:** `git revert` convencional y evento compensatorio nuevo; no borrar BITACORA histórica.
- **Impacto:** solo este `docs/v5` y sección autocontenida `CHANGELOG_AI.md`. Sin API, DTO, JWT, RBAC, migraciones, inventario físico, costo, secret, workers, Cloudflare R2, QA/main/PROD.
