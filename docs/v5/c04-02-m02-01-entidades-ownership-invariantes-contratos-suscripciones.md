# SOLQARYN V5 — C04-02.M02.01: modelo conceptual, ownership e invariantes contractuales

**Autor:** PRIMARY DEV 06 — JAVIER (TASK_ID 6af16bbad26881918c283abe06e1f32a). **Fecha:** 2026-11-15. **Scope:** solqaryn/Solqaryn, rama dev, PROJECT_SCOPE_LOCK=STRICT. **Clase:** diseño M02 exclusivamente documental; no se agregan entidades persistentes, interfaces públicas, migraciones ni procesos de cobro.

## 1. Fuente, alcance y distinción crucial

La autoridad del backlog es STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES, Google Doc 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA; la microtarea exacta es PLAN_MAESTRO fila 620, C04-02.M02.01, «Definir entidades, ownership, relaciones y reglas de invariantes». El resultado de producto previsto es gestión de altas, renovación, suspensión, cancelación, morosidad y cierre contractual, con transiciones inválidas rechazadas. **Eso es un objetivo futuro, no una capacidad implementada por este documento.**

Preflight previo al claim: PLAN H620=PENDIENTE/I620 vacío, COLA G620=SI/O620=SI, DAG_TECNICO F581=VALIDADO/G581=SI; posterior claim puntual de PRIMARY DEV 06 fue verificado H620=EN_PROGRESO e I620=su TASK_ID. El predecesor C04-02.M01.06 está LISTO y recibió A5 independiente de DEV06 sobre SHA 7a2559cf302d7929e762a4432d813756ff90f05c. Ello habilita este **diseño** conforme al DAG, no decisiones financieras ni operaciones live.

El sistema actual ya dispone de Plan, PlanLimite y Suscripcion en backend/src/Domain/Entities/SuscripcionesSaaS.cs (blob d9b5dbe19fb728293b00b748f10ac10336debd2d). Suscripcion tiene EmpresaId, PlanId, estados Activa/Suspendida/Cancelada/Expirada y métodos técnicos Suspender, Reactivar y Cancelar; no equivale a un contrato económico firmado. La capa Application SuscripcionesSaaSService.cs (blob bb501285f11cd4b32709a58a2c51efa19c3664ea) resuelve tenant server-side e idempotencia de **onboarding**, no pagos, renovaciones facturadas ni consentimientos contractuales. C04-02.M01.03, inventario en docs/v5, documenta 12 archivos y sus límites. Nunca duplicar la entidad existente ni asignarle facultades contractuales implícitas.

## 2. Diccionario de 16 objetos **propuestos** — no tablas creadas

| Objeto futuro lógico | Dueño/identidad y referencias | Invariante principal |
| --- | --- | --- |
| ContratoComercialSaaS | EmpresaId obligatorio, ContratoId único por tenant, SuscripcionId opcional | El contrato legal no se deduce de PlanId ni de estado técnico Activa |
| VersionContrato | ContratoId, VersionId monotónico, huella de contenido | Una versión aceptada es inmutable y no se edita retroactivamente |
| ParteContratante | ContratoId, identificador de parte con ámbito EmpresaId | Sin acceso interempresa; las partes externas no son usuarios privilegiados |
| RepresentacionAutorizada | ParteId, evidencia de representación, vigencia UTC | Representación válida requiere autorización real D01, no inferencia de rol |
| TerminosAplicables | VersionId, identificador de términos, idioma/jurisdicción aprobados | Nunca inventar moneda, impuesto o jurisdicción |
| AceptacionEvidenciada | VersionId, ParteId, evidencia de consentimiento, UTC | Consentimiento explícito, verificable y no modificable tras aceptación |
| PeriodoContractual | ContratoId, InicioUtc/FinUtc o vigencia abierta legítima | UTC y orden temporal; no renovación retroactiva |
| PropuestaRenovacion | ContratoId, versión futura, periodo propuesto | No renueva ni factura hasta aprobación expresa y precondiciones verificadas |
| PoliticaRenovacionReferencia | VersionId, aprobación D04, fuente estable | La ausencia de política impide autorrenovación |
| ReferenciaEconomica | VersionId, referencia externa aprobada, no datos de tarjeta | No almacenar secretos ni inventar tarifas/moneda/impuestos |
| ConciliacionReferencia | ContratoId, evidencia inmutable del proveedor aprobado | Callback o timeout no prueban pago ni mora |
| ExpedienteMorosidad | ContratoId, evento causal y política D05 | Morosidad requiere ledger conciliado y política aprobada |
| SuspensionContractual | ContratoId, motivo, autoridad, instante UTC | No suspender por error de red ni por deuda de otro tenant |
| SolicitudCancelacion | ContratoId, actor habilitado, alcance, UTC | No hacer reembolso ni borrado implícito |
| HistorialEstadoContrato | ContratoId, EstadoPrevio, EstadoNuevo, Causa y UTC | Trazabilidad append-only, rechazo de transiciones inválidas |
| EvidenciaAuditoriaContractual | ContratoId, correlación e identificador de evento | No guardar PII/secretos innecesarios; retención requiere D08 aprobada |

La asociación conceptual es Empresa 1:N Contrato; Contrato 1:N VersionContrato, Periodo, HistorialEstado y Evidencia; VersionContrato 1:N Terminos/Consentimientos y 0:N Referencias de política aprobada. Una Suscripcion técnica puede ser referencia cruzada 0..1, nunca relación de autoridad ni prueba de aceptación legal. Cada entidad hija hereda la frontera EmpresaId del contrato de forma verificable; una clave global sin tenant no es autorización.

## 3. Invariantes obligatorias del **futuro** modelo (INV01–INV18)

- **INV01:** Crear y leer únicamente con identidad empresa extraída del contexto autenticado del servidor; ruta/body EmpresaId no conceden permisos.
- **INV02:** Revalidar miembro activo y RBAC de acción en servidor antes de I/O sensible; entitlement SaaS no equivale a permiso contractual.
- **INV03:** Toda referencia cruzada (contrato, versión, parte, ledger, auditoría) debe pertenecer al mismo EmpresaId; negar A→B y B→A.
- **INV04:** No publicar contrato legal sin representación/consentimiento aprobados (D01 y D02).
- **INV05:** Versionar y conservar huella de términos aceptados; una actualización crea versión nueva, nunca sobrescribe la firmada.
- **INV06:** No inferir precio, tasa, impuesto, moneda, proveedor o jurisdicción: D03 y D06 siguen sin aprobar.
- **INV07:** Inicio y fin contractuales UTC, periodo final posterior al inicio; nunca renovación en fecha pasada.
- **INV08:** Solo una transición contractual coherente por estado, tenant, versión y concurrencia comprobada; jamás aceptar doble alta vigente accidental.
- **INV09:** De Activa a Suspendida requiere causa y rol aprobados; de Suspendida a Activa requiere políticas y conciliación vigentes; Cancelada/Expirada son terminales.
- **INV10:** Morosidad y grace period requieren D05 aprobado y reconciliación confiable; timeout/red/callback no firmado son negativos.
- **INV11:** Cancelación/prorrateo/reembolso no se ejecutan sin D07 y autorización económica separada.
- **INV12:** Aceptación, estado anterior, causa y UTC permanecen en historial append-only; prohibido borrado físico en cascada de historia.
- **INV13:** Idempotencia proyectada por EmpresaId+Acción+Clave+hash de payload; mismo replay equivalente devuelve el resultado anterior, payload distinto da conflicto.
- **INV14:** Carreras deben resolverse por constraints y transacciones MySQL 8.4 EF Core realmente probadas con servicio efímero antes de implementación; no confiar en doble lectura.
- **INV15:** Ninguna propiedad Angular, DTO o rol UI altera estado contractual sin autorización backend y auditoría.
- **INV16:** Identificadores y errores HTTP no filtran tenant B ni datos financieros; límites, redacción, CORS, rate limit y JWT continúan vigentes.
- **INV17:** No se activan eventos, workers, webhooks, cron ni cambio de proveedor por este diseño; D08 y mecanismos de idempotencia requerirán autorización propia.
- **INV18:** Errores de Scope Lock, CI, pruebas negativas o A5 mantienen el gate correspondiente en NO_GO; no alterar fórmulas de DAG/COLA para compensarlos.

## 4. Máquina de estados propuesta y pruebas que deberán implementarse

**Transiciones propuestas:** Borrador→PendienteAceptacion solo con versión y términos aprobados; PendienteAceptacion→Activa con consentimiento y verificación contractual; Activa→Suspendida únicamente por causa autorizada; Suspendida→Activa únicamente tras resolución y conciliación; Activa/Suspendida→Cancelada con razón, alcance y preservación de historial; Activa→Expirada cuando periodo vence según política aprobada. Renovación propone una nueva versión/periodo; no es una mutación libre del estado histórico. **El catálogo existente de EstadoSuscripcion no se migra ni se modifica en esta tarea.**

| Escenario positivo FUTURO | Escenario negativo FUTURO |
| --- | --- |
| P01 Alta firmada por representante válido de empresa A | N01 miembro B o representación ausente |
| P02 Versión posterior con aceptación explícita | N02 mutar términos previamente aceptados |
| P03 Periodo nuevo posterior a inicio | N03 renovación retroactiva o intervalo invertido |
| P04 Suspensión con autoridad y motivo válidos | N04 suspensión por timeout sin conciliación |
| P05 Reactivación desde Suspendida con permiso real | N05 reactivar Cancelada/Expirada |
| P06 Cancelación trazable sin pérdida histórica | N06 devolución automática sin aprobación D07 |
| P07 Replay idéntico de solicitud idempotente | N07 misma clave con payload diferente |
| P08 Resolución de colisiones en DB efímera | N08 doble contrato activo por carrera concurrente |
| P09 Consulta y trazas limitadas al tenant A | N09 acceso A→B por id/DTO manipulados |
| P10 Historial enlazado a versión y aceptación | N10 borrar historial o permitir mutación de evento |
| P11 Ausencia de cargos sin proveedor habilitado | N11 disparar cobro, webhook o worker por cambiar estado |
| P12 API/backend autorizado sobre SHA real de producto | N12 confiar en guard Angular sin RBAC backend |

**P01–P12/N01–N12 son oráculos documentales futuros, NO xUnit/HTTP/MySQL/Playwright ejecutados.** La CI documental se evalúa únicamente por sus checks causales.

## 5. Impacto, límites, aceptación A0–A5 y retorno

Impacto actual: exclusivamente este archivo docs/v5 y una sección completa en CHANGELOG_AI.md. Ninguna API/DTO, contrato TS, Domain, Application, Infrastructure, SQL, servicio, identidad, secreto, worker, billing, entorno ni datos cambia. Los consumidores M02.02–.06 y M03 futuro deberán confirmar versiones de código, decisiones comerciales D01–D08, CI xUnit/seguridad, tests de integración MySQL 8.4 y A5 pertinentes antes de habilitar efectos.

**A0:** inventario vivo Domain y Application con blobs, autoridad V5, gates de COLA/DAG y diff acotado. **A1:** coherencia entre 16 objetos, 18 invariantes y 12 pares P/N documentales; no son pruebas ejecutadas. **A2:** JWT/RBAC, membresía, tenant A/B, propiedad de hijos, PII y auditoría descritos fail-closed sin habilitación. **A3:** modelo conceptual coherente con las capas existentes y C04-02.M01.06; runtime, API, MySQL, Playwright y cobros NO_APLICA para diff solo Markdown, nunca PASS artificial. **A4:** publicación en dev con SHA CAS/fast-forward, dos Markdown solamente, Scope Lock y procedencia V5 completed/success exact SHA y readback institucional. **A5:** revisión independiente por otra de las cinco Tasks Javier, sin autoaprobación, previa a cualquier promoción adicional y no sustituta de autorización QA/main/PROD.

**Rollback limitado:** git revert ordinario del changeset documental tras releer HEAD concurrente. Nunca revertir datos contractuales existentes, migrar, cobrar o alterar BITACORA histórica como efecto de esta especificación.
