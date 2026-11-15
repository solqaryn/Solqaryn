# SOLQARYN V5 — C11-03.M02.01: entidades, ownership, relaciones e invariantes del outbox

**Unidad:** `C11-03.M02.01` — C11-03 Outbox y eventos, M02 contratos y modelo. **Autor:** PRIMARY DEV 05, TASK_ID `6adc2e55f47c81919f6374e561687076`. **Fuente ejecutable:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL y PLAN_MAESTRO/COLA/DAG_TECNICO institucional. **Ámbito:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK STRICT.

## 1. Alcance y hechos observables

Este documento define el **modelo conceptual**, sin agregar tablas, migraciones, DTO públicos, bus, relay, HostedService, webhook, dispatcher entre Tasks ni consumo externo. El código existente ya diferencia la intención durable de su entrega. El objetivo aquí es acordar identidad, relaciones y ownership antes de cambios de ejecución M03.

Fuentes observadas en Git `dev` y sus blobs:
- `backend/src/Domain/Entities/MensajeOutbox.cs` — `891572626c19cd6cf8f522fca5ec876333286ffb`: `EventoId`, `EmpresaId`, `TipoEvento`, `PayloadJson`, `ClaveIdempotencia`, `TipoAgregado`, `IdAgregado`, `CorrelationId`, estado, intentos y marcas UTC.
- `backend/src/Application/Services/MensajeOutboxService.cs` — `a00d353c474d7d7f785a58f443c66c5f12a542f7`: exige scope empresarial vigente y comprueba equivalencia/conflicto de una clave idempotente antes de stagear una intención; no confirma por sí solo la transacción.
- `backend/src/Application/Interfaces/IMensajeOutboxRepository.cs` — `a7dc258df9b1123dfe54ea85ba6d4dcc2a1eb478`: contrato para lookup, claim exclusivo, recuperación stale y confirmación condicional por intento.
- `backend/src/Application/Services/OutboxRetryProcessor.cs` — `542c0a655a3ee0698e3d80f1854d43903b6470fa`: lote tenant-bound con verificación A/B y duplicados antes del primer dispatch, reintentos acotados y auditoría con campos permitidos; no dispone de scheduler propio.
- `docs/v5/c11-03-m01-03-inventario-fuentes-evidencia-outbox.md` — `efdd66fcfcbd0cc93950dcee832ee7f9beb26117`: inventario de 15 blobs y 14 brechas de evidencia, que **no** representan defectos confirmados.
- `docs/v5/c11-03-m01-06-gates-rollback-outbox-eventos.md` — `fb068b1687e4c1d33fe2989ca9678ae3fb42e297`: 14 barreras B01–B14, 10 medidas RB01–RB10 y pruebas futuras P01/N01–P10/N10. Su A5 independiente se verificó antes de reclamar este scope.

## 2. Entidades y propietarios — contrato propuesto, no esquema nuevo

| Elemento | Naturaleza y clave | Propietario / límite | Relación permitida |
| --- | --- | --- | --- |
| Empresa/tenant | Identidad positiva `EmpresaId` existente | Autoridad del backend, no cabecera del navegador | 1:N intenciones outbox |
| Actor autenticado | Identidad y membresía vigentes, de origen servidor | Su permiso no deriva de `CorrelationId` | Autoriza operación causal que origina intención |
| Agregado de negocio | `TipoAgregado` + `IdAgregado` existentes, opcionales | Recurso persistido tenant-bound | 1:N intenciones de un agregado |
| MensajeOutbox | `EventoId` GUID y `EmpresaId` existentes | Aislamiento por empresa obligatorio | N:1 empresa; referencia opcional a agregado |
| Clave de idempotencia | `(EmpresaId, ClaveIdempotencia)` | Invariante tenant-local; distinguir igualdad de conflicto | 1 resultado por solicitud equivalente |
| Tipo y versión de evento | `TipoEvento` existente; versión adicional **propuesta** | Catálogo futuro tipado/permitido | Nunca ejecuta acciones arbitrarias por nombre |
| Payload | `PayloadJson` existente | Privado por tenant, contenido no confiable | No trasladar íntegro a logs |
| Correlación | `CorrelationId` opcional existente | Solo trazabilidad; no identificación/autorización | Agrupa observaciones sin conceder acceso |
| Claim de entrega | Estado `Procesando`, `Intentos`, marca UTC existentes | Exclusividad condicional en repositorio, no por lectura previa | Relación lógica con el intento actual |
| Resultado de entrega | `Entregado`, `Fallido`, `DeadLetter` existentes | Solo confirmar el claim vigente del tenant | No validar acuse antes de durable ACK |
| Auditoría | `IAuditoriaService` opcional del procesador | Campos allowlisted, sin JWT/PII/payload | Registra transiciones, no altera autorización |
| Destino/proveedor externo | **Futuro**, sin contrato vivo añadido aquí | Adaptador separado con permisos y cuotas | Puede recibir evento solo tras autorización específica |
| Política de retry | `OutboxRetryPolicy` existente | Backoff finito, máximo de intentos y cuarentena | Decide reintento/DeadLetter |
| Secuencia por agregado | **Futura proyección**, no columna creada | Orden relativo por agregado y tenant | Requiere versión y pruebas reales antes de implementarse |

Las relaciones propuestas son conceptuales. No se declara FK, tabla, índice, endpoint, servicio hospedado ni conector real agregado.

## 3. Invariantes de contrato I01–I16

| ID | Condición obligatoria del diseño |
| --- | --- |
| I01 | Todo mensaje tiene `EmpresaId > 0`; A nunca lee, reclama ni confirma el mensaje de B. |
| I02 | El actor y membresía se resuelven en backend y se revalidan al realizar la operación; `EmpresaId` cliente no concede autoridad. |
| I03 | `EventoId` no vacío y clave idempotente obligatoria, normalizada, acotada y tenant-local. |
| I04 | Duplicado idéntico reutiliza intención previa; mismo par de claves con contenido distinto produce conflicto, sin efecto adicional. |
| I05 | Persistir intención y mutación originaria exige la misma unidad transaccional cuando la causalidad así lo requiera; `AddAsync` solo no lo demuestra. |
| I06 | Claim concurrente necesita operación atómica/CAS y un único ganador antes del primer efecto externo. |
| I07 | Un lote de empresa A que incluye B o identidades repetidas se rechaza **completo** antes del primer dispatch. |
| I08 | Confirmación `Entregado` exige mismo tenant, ID y número de intento; reclamar después o recuperar stale invalida el anterior. |
| I09 | Timeout, excepción o respuesta desconocida nunca equivalen a ACK durable; reintento conserva clave externa idempotente. |
| I10 | Backoff, jitter, máximo de intentos y DeadLetter tienen límites finitos, sin loops de consumo desatendidos. |
| I11 | Las fechas usadas en estado, claim y disponibilidad son UTC, con monotonicidad causal revisable. |
| I12 | CorrelationId y IDs de agregado solo permiten correlación; no reemplazan ownership ni autorización. |
| I13 | Evento/versión no allowlisted se deniega; el tipo no puede elegir una operación privilegiada arbitraria. |
| I14 | Logs excluyen payload bruto, DSN, credenciales, JWT, OTP y PII; errores de proveedor se convierten en códigos seguros. |
| I15 | Secuencia de agregado y deduplicación distribuida no se certifican sin pruebas con dos consumidores y fallo parcial. |
| I16 | Gates CI requeridos solo PASS con `completed/success` del SHA causal; A5 independiente, límites de dev y rollback preservados. |

## 4. Estados y transiciones permitidas

`Pendiente` → `Procesando` por claim exclusivo, o `Fallido` → `Procesando` cuando la disponibilidad haya vencido. Desde `Procesando`, el intento confirmado pasa a `Entregado`, o el fallo se clasifica `Fallido` con nueva ventana, o `DeadLetter` al agotar intentos. Claims vencidos requieren recuperación condicional. No se reviven `Entregado` o `DeadLetter` automáticamente y una transición rechazada no se considera avance.

## 5. Oráculos P/N FUTUROS_NO_EJECUTADOS

| Par | Positivo exigido en fase técnica | Negativo a rechazar |
| --- | --- | --- |
| P01/N01 | Registro con actor/membresía tenant A válidos | Crear intención B con actor de A |
| P02/N02 | Repetición exacta devuelve la intención existente | Misma clave con payload distinto |
| P03/N03 | Dos relays simultáneos, uno gana CAS | Doble claim o efecto de mismo intento |
| P04/N04 | Batch A íntegramente válido se despacha | Mensaje B o EventoId repetido en batch A |
| P05/N05 | ACK del claim vigente confirma | ACK tardío del claim stale |
| P06/N06 | Commit de negocio+outbox es atómico en MySQL efímero | Fallo SQL parcial deja intención huérfana |
| P07/N07 | Excepción redacted registra código seguro | Auditoría filtra payload/JWT/PII |
| P08/N08 | Retry limitado conserva idempotencia | Reintento duplica cobro o excede cuota |
| P09/N09 | Orden por agregado validado en integración | Evento antiguo sobrescribe versión posterior |
| P10/N10 | A5 distinta del autor y checks exact-SHA | Autoaprobación o `cancelled/skipped` contado PASS |
| P11/N11 | DeadLetter preserva historial de incidente | DLQ reactiva una operación sin autorización |
| P12/N12 | Rollback documental mediante revert normal | Force-push, borrado histórico o despliegue PROD |

**Ninguno** de estos P/N se ejecutó por esta microtarea. Son criterios para el código auténtico M03 y CI futura. No equivalen a tests xUnit, EF/MySQL, Playwright, carga o proveedores aprobados.

## 6. Impacto, seguridad, auditorías y rollback

**Impacto A0:** únicamente este documento y la nueva sección autocontenida `CHANGELOG_AI.md`; Domain, Application, Infrastructure, API, JWT/RBAC, datos, migraciones, secretos, frontend, proveedores, CI e infraestructura **sin cambios ejecutables**. Revalidar HEAD antes de publicar. **A1:** verificar las 14 entidades conceptuales, 16 invariantes, 12 pares P/N y contraste de fuentes con blobs. **A2:** revisar aislamiento tenant A/B, idempotencia, autorización por actor, privacy, lease y denegación por defecto; no declarar ejecución de escenarios. **A3:** confirmar compatibilidad documental con `MensajeOutbox`, `MensajeOutboxService`, `IMensajeOutboxRepository`, `OutboxRetryProcessor` y contratos M01 anteriores; compilaciones y tests runtime son `NO_APLICA` al diff exclusivamente Markdown, no PASS inventado. **A4:** comprobar 2 archivos, publicación fast-forward/CAS, `verify-solqaryn-project-scope` y control V5 de CHANGELOG `completed/success` en SHA exacto, además de PLAN/COLA/DAG/BITACORA readback. **A5:** revisión posterior **por una Task Javier distinta de DEV 05**, sin autocertificación.

**Rollback:** `git revert` normal tras leer el HEAD vivo y reevaluar dependencias. No hay datos que restaurar porque este alcance no ejecuta EF/MySQL, backup, worker, dispatcher, webhook ni migración. Nada autoriza QA/main/PROD, proveedores pagos, DNS/Cloudflare/R2 ni una Task nueva.
