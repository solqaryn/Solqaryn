# SOLQARYN V5 — C11-03.M01.02: actores, entradas/salidas y fronteras de Outbox/eventos

**Autor:** PRIMARY DEV 02 JAVIER; Task `6af16bb9492c8191b8e8c7d606f11507`. **Unidad:** C11-03.M01.02, PLAN_MAESTRO fila 1659, fase documental M01, bloque Outbox y eventos. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Repo:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`; 2026-11-14.

## Propósito, preparación y restricciones

Identificar participantes y contratos de entrada/salida que una futura plataforma de outbox y eventos necesitaría, sin crear ni activar nada. Continuidad de `C11-03.M01.01` (blob `a058779633c30a155ac8df5dacb8159e979876df`) que fijó NR01–NR16; se conserva su distinción entre capacidad existente y propuesta. Al claim, PLAN H1659=PENDIENTE/I vacío, COLA G/O=SI, DAG A1620=C11-03.M01.02 F=VALIDADO/G=SI; no son permisos para runtime.

**No implementar eventos todavía:** no crear worker, dispatcher entre Tasks, schedule, webhook, SQL, colas, migración, endpoint, proveedor, secretos ni alterar roles. C10.04 es antecedente documental, no autorización de restauración, Cloudflare, Hostinger ni QA/main/PROD. Conservar compatibilidad .NET 10, Angular 22.2.1, MySQL EF Core 10, JWT/RBAC y tenant A/B.

## Actores A01–A14: identidad, capacidad y frontera

| Actor | Función esperada, no necesariamente ya operativa | Límite de confianza |
| --- | --- | --- |
| A01 | Usuario de empresa A autenticado | Permisos y pertenencia activa derivados en servidor, sin acceso a B |
| A02 | Usuario de empresa B autenticado | Mismo control aislado, sin leer eventos de A |
| A03 | Administrador empresa | RBAC empresarial, nunca potestad global implícita |
| A04 | Operador plataforma | Contexto global no equivale a membresía de una empresa |
| A05 | Operador de soporte read-only | Nunca muta estado ni reenvía eventos |
| A06 | Cliente Angular/storefront | Datos, host y headers no confiables; UI no autoriza |
| A07 | API Controller | Autentica entrada y valida contratos, no confía en EmpresaId proporcionado |
| A08 | Application Service | Coordina caso de uso e intención transaccional tenant-bound |
| A09 | Entidad MensajeOutbox | Mantiene identidad durable, tipo, clave idempotente y correlación |
| A10 | Unidad de trabajo/EF | Commit atómico de mutación e intención; no probado en esta unidad |
| A11 | IMensajeOutboxRepository | Contrato de persistencia y claim; no representa un job activo |
| A12 | OutboxRetryProcessor | Procesa lotes tenant-bound mediante dispatcher inyectado; sin scheduler |
| A13 | Proveedor/consumidor externo futuro | Sólo adaptador autorizado, con idempotencia verificable y límites |
| A14 | Auditoría/observabilidad | Registra resultado/correlación mínima sin JWT, PII ni payload sensible |

A01–A14 mezclan superficies existentes y futuros consumidores: **la tabla NO prueba que todos estén desplegados**. En particular A13 es FUTURO_NO_IMPLEMENTADO y A10/A11 necesitan ensayos de integración antes de cualquier certificación.

## Matriz de doce interfaces I/O01–I/O12

| Contrato | Entrada de riesgo y origen | Salida esperada | Rechazo obligatorio futuro |
| --- | --- | --- | --- |
| I/O01 comando de negocio | DTO/tenant de solicitud HTTP | resultado del caso de uso | identidad y capacidad no válidas |
| I/O02 creación de intención outbox | empresa validada, tipo, clave, payload mínimo | mensaje durable en UoW | EmpresaId de B o clave vacía |
| I/O03 consulta por EventoId | empresa validada, identificador UUID | mensaje de la misma empresa o no encontrado | acceso a recurso de B |
| I/O04 consulta por idempotencia | empresa y clave | intención anterior o conflicto controlado | mismo key + payload distinto |
| I/O05 claim de pendientes | empresa, UTC, batchSize | lote exclusivo | dos winners del mismo intento |
| I/O06 recuperación stale | empresa, umbral UTC | reclamos recuperados | reset de otro tenant o versión obsoleta |
| I/O07 dispatch futuro | MensajeOutbox previamente validado | efecto externo/ack idempotente | falta de permiso/provider activo |
| I/O08 confirmación de entrega | empresa, ID e intento esperado | transición confirmada o supersedida | confirmar claim ajeno |
| I/O09 registro de fallo | tipo de error allow-listed y retry policy | reprogramación o dead-letter | guardar excepción/token/PII |
| I/O10 correlación HTTP | X-Correlation-ID no fiable | identificador sanitizado | usar header como autenticación |
| I/O11 auditoría de reintento | tenant, evento y transición | registro mínimo observable | filtrar payload o datos de proveedor |
| I/O12 conciliación/replay futuro | ID, huella e historial autorizados | resultado idempotente, no duplicado | duplicar pago/inventario |

I/O02–I/O06 se apoyan en contratos presentes del repositorio, pero requieren pruebas de MySQL/EF; I/O07 y I/O12 expresan una futura integración, **no una capacidad invocada**. La semántica de entrega por red no se certifica como *exactly-once*.

## Fronteras de confianza F01–F12

1. **F01 Browser → API:** body, query, cookie, host y header son no fiables; JWT/RBAC se revisan server-side.
2. **F02 JWT → membresía vigente:** claims no sustituyen verificación de empresa, rol revocado, suspensión ni estado actual.
3. **F03 Empresa A → B:** prohibido recuperar, confirmar o reintentar evento ajeno; un lote contaminado falla antes de despacho.
4. **F04 Plataforma → tenant:** autoridad global y empresa comercial se controlan por separado.
5. **F05 Soporte → mutación:** soporte read-only no publica, reenvía ni cambia mensajes.
6. **F06 Controller → Application:** validación, cancellation token, DTOs acotados y errores seguros.
7. **F07 Application → Domain:** tipos y claves idempotentes mantienen sus invariantes; correlación no es permiso.
8. **F08 Application → Infra/EF:** persistencia con tenant, UoW transaccional y reclamación exclusiva.
9. **F09 Repository → OutboxRetryProcessor:** reconfirmar el lote íntegro antes del primer efecto de red.
10. **F10 Dispatcher → proveedor:** auth específica y efecto idempotente; no activar un adaptador por diseño documental.
11. **F11 Eventos → logs:** redacción de claves, excepciones, secretos, JWT y PII; usar identificadores mínimos.
12. **F12 Automatización ChatGPT → runtime:** no hay dispatcher server-side de Tasks; ningún mensaje/chat dispara un bucle perpetuo.

## Fuentes de implementación inspeccionadas (sin pruebas ejecutadas)

| Archivo | SHA de blob Git | Hecho verificable |
| --- | --- | --- |
| `backend/src/Domain/Entities/MensajeOutbox.cs` | `891572626c19cd6cf8f522fca5ec876333286ffb` | Invariantes de identidad empresarial, evento, clave y longitudes |
| `backend/src/Application/Services/MensajeOutboxService.cs` | `a00d353c474d7d7f785a58f443c66c5f12a542f7` | Valida scope de empresa antes de stagear intención |
| `backend/src/Application/Interfaces/IMensajeOutboxRepository.cs` | `a7dc258df9b1123dfe54ea85ba6d4dcc2a1eb478` | Define claim, consultas, AddAsync sin commit autónomo |
| `backend/src/Application/Services/OutboxRetryProcessor.cs` | `542c0a655a3ee0698e3d80f1854d43903b6470fa` | Defensa de lote tenant-bound y reintentos; sin scheduler |
| `backend/src/API/Middleware/CorrelationIdMiddleware.cs` | `3b76dca41bb4bc0b44f89c2a4664c79907f9f242` | Cabecera sanitizada sin permisos |
| `docs/v5/c11-03-m01-01-alcance-no-regresion-outbox-eventos.md` | `a058779633c30a155ac8df5dacb8159e979876df` | Objetivo y 16 invariantes, escenarios futuros no ejecutados |

Todos los blobs acreditan contenido versionado, no ejecución ni despliegue. Ninguna ruta inspeccionada autoriza modificar esquema, emplear credenciales o integrar proveedor de pago.

## Escenarios y auditoría de preparación, NO_EJECUTADOS

**P01–P08 propuestos:** intención durable tenant A; B independiente; Idempotency-Key repetida devuelve mismo resultado; claim único; recuperación stale; error con dead-letter; correlación segura; soporte no modifica estado.

**N01–N14 propuestos:** N01 evento B en lote A; N02 EmpresaId falso; N03 claim duplicado; N04 intención con clave vacía; N05 fingerprint distinto; N06 UoW incompleta; N07 caída tras efecto antes del ack; N08 replay duplica inventario; N09 soporte intenta dispatch; N10 JWT revocado; N11 token en log; N12 proveedor fuera del allowlist; N13 SKIPPED/IN_PROGRESS como PASS; N14 nueva Task llamada como dispatcher remoto.

**A0:** revisar Skill canónica, HEAD/Scope Lock, fuente V5 y elegibilidad DAG. **A1:** matriz A01–A14, I/O01–I/O12, F01–F12 y oráculos futuros. **A2:** fronteras empresariales, JWT/RBAC, soporte y PII. **A3:** trazabilidad Git de seis rutas; pruebas HTTP/MySQL/dispatch NO_APLICA al cambio documental. **A4:** diff de este Markdown+CHANGELOG, publicación serializada por expected SHA, checks Scope Lock y procedencia V5 exact SHA `completed/success`, readback institucional. **A5:** revisión por Task Javier distinta, aún pendiente.

**Impacto y rollback:** sólo `docs/v5/` y `CHANGELOG_AI.md`; Domain/Application/Infrastructure/API/UI/SQL/Secrets/Observabilidad productiva intactos. Revertir vía commit normal después de conciliar HEAD; ninguna edición de BITACORA histórica, force-push, QA/main/PROD ni activación de eventos.
