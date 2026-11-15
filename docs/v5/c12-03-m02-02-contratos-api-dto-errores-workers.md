# SOLQARYN V5 — C12-03.M02.02: contratos API, DTO, errores y compatibilidad de backend/workers

**Microtarea:** C12-03.M02.02. **Autor:** PRIMARY DEV 02, TASK_ID 6af16bb9492c8191b8e8c7d606f11507, JAVIER_CHATGPT. **Rama:** solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, PLAN_MAESTRO, COLA, DAG_TECNICO, A0–A5. Este alcance es **documental**: no crea endpoint, worker, cron, dispatcher, migración, DNS, proveedor, entorno ni Task.

## 1. Fuentes reales y frontera de ejecución

- API ASP.NET Core 10, backend/src/API/Program.cs blob 321fc0a3c29d5cbc53f5e30373f82939e907d6f0: JWT Bearer, Authorization, CORS allowlist y rate limiting. La existencia de HostedService **de otra finalidad** no implica un scheduler general de outbox.
- backend/src/Application/Services/OutboxRetryProcessor.cs blob 542c0a655a3ee0698e3d80f1854d43903b6470fa: interfaz IOutboxEffectDispatcher, OutboxRetryProcessor, options tipadas con Validar(), y ProcesarLoteAsync(int empresaId, DateTime ahoraUtc, dispatcher, cancellationToken). No incorpora un scheduler propio.
- backend/src/Application/Interfaces/IMensajeOutboxRepository.cs blob a7dc258df9b1123dfe54ea85ba6d4dcc2a1eb478: AddAsync sin commit, GetByEventoIdAsync, GetByClaveIdempotenciaAsync, ExisteClaveIdempotenciaAsync, ClaimDisponiblesAsync, RecuperarProcesandoStaleAsync, MarcarEntregadoAsync y RegistrarFalloAsync.
- backend/src/Domain/Entities/MensajeOutbox.cs blob 891572626c19cd6cf8f522fca5ec876333286ffb: EmpresaId, EventoId, ClaveIdempotencia, estado, intentos y UTC. La entidad no autoriza acceso al tenant por sí sola.
- docs/v5/c12-03-m01-06-gates-rollback-backend-workers.md blob 3771d1eb465cdb165a3402dbfc673c325b23f78b: puertas B01–B16, contención R01–R10 y escenarios futuros P/N. La fase M01 exige no confundir documentación con autorización de Hostinger/Cloudflare, DNS o producción.
- Arquitectura actual Domain <- Application <- Infrastructure, API como borde HTTP y JWT/RBAC comprobado en backend. Worker sin HttpContext debe obtener identidad, permisos, recursos y tenant mediante autoridad confiable, no de payload, header o nombre de cola.

La topología con procesos separados es **futura**. Ninguna ruta HTTP, esquema de respuesta o servicio de ejecución nuevo descrito aquí está registrado en producción por este cambio.

## 2. Operaciones de contrato OP01–OP10

Las operaciones son candidatas para contratos internos de Application o API administrativa futura; **no son endpoints existentes** y deben separarse de la superficie pública. Las operaciones existentes del repositorio/procesador se citan como fuente, no como permisos expuestos.

| ID | Operación conceptual | Precondiciones/resultado exigidos |
| --- | --- | --- |
| OP01 | Consultar readiness de un proceso autorizado | Reportar dependencia por código seguro; sin exponer DSN, secreto ni datos tenant |
| OP02 | Registrar intención durable de efecto | Actor, permiso, tenant y idempotencia verificados; commit de negocio + outbox en unidad transaccional |
| OP03 | Consultar mensaje por EventoId | Empresa obtenida del contexto del servidor, no identificador libre del cliente |
| OP04 | Consultar resultado por clave idempotente | Repetición equivalente reutiliza resultado; conflicto de payload se rechaza |
| OP05 | Reclamar lote disponible | Hora UTC, máximo validado, tenant único, CAS/lease real y único ganador |
| OP06 | Recuperar claims stale | Requiere corte UTC y confirmación condicional de que no existe claim vigente |
| OP07 | Confirmar entrega | ACK durable y mismo tenant, mensaje e intento; falsa confirmación denegada |
| OP08 | Registrar fallo o DeadLetter | Estado y política de retry finita, error redactado y siguiente disponibilidad |
| OP09 | Consultar estado operativo del worker | Solo métricas agregadas permitidas, sin payload, PII ni cross-tenant |
| OP10 | Deshabilitar y contener un productor/consumidor | Solo operador autorizado; no es autorización para reactivar ni desplegar |

OP05–OP08 se corresponden conceptualmente con métodos del repositorio ya existente. Su uso en un futuro proceso separado exigirá seguridad de identidad de servicio, DI, integración MySQL efímera y pruebas de concurrencia; no se autoriza una API pública para reclamar mensajes.

## 3. DTO y sobre de compatibilidad D01–D14 (propuestos)

Los campos definen tipos y responsabilidades **futuras**, no nuevas clases C# ni columnas EF.

| ID | Campo/tipo lógico | Seguridad y compatibilidad |
| --- | --- | --- |
| D01 | ContractVersion: versión semántica cerrada | Negociar versión conocida; no aceptar deserialización arbitraria |
| D02 | EventId: GUID no vacío | No sirve como permiso ni tenant derivado |
| D03 | TenantBinding: identidad positiva de empresa | Se asigna desde autoridad del backend, nunca del consumidor |
| D04 | IdempotencyKey: cadena normalizada limitada | Clave compuesta tenant + clave; replay conflictivo denegado |
| D05 | EventType: identificador allowlisted | No selecciona código dinámico ni proveedor por texto libre |
| D06 | AggregateRef: identificador opaco opcional | Se valida ownership en servidor, sin buscar globalmente |
| D07 | CorrelationRef: traza opaca limitada | No contiene JWT, PII ni confianza implícita |
| D08 | CreatedUtc: instante UTC | No aceptar fecha local o futuro no justificable |
| D09 | AvailableUtc: instante UTC | Nunca adelantar intento para eludir backoff |
| D10 | Attempt: entero positivo | Coincidir con claim al confirmar; evita ACK tardío |
| D11 | LeaseRef: token de claim opaco | Debe vincular mensaje + tenant + intento, nunca servir como credencial cliente |
| D12 | ResultState: enum cerrado | Pendiente, Procesando, Entregado, Fallido, DeadLetter, según dominio vigente |
| D13 | SafeErrorCode: código de catálogo | Excluye excepciones, SQL, headers y payload bruto |
| D14 | ObservationUtc: UTC y versión de observación | No certifica readiness ni efecto sin resultado causal |

**Compatibilidad:** mantener consumo de miembros existentes de MensajeOutbox e IMensajeOutboxRepository; los campos propuestos no deben convertirse en nuevas columnas/DTO para clientes sin implementación y migración separadas. El consumidor desconocedor de ContractVersion debe fallar cerrado. Reintentos conservan clave idempotente externa, incluso tras reconexión.

## 4. Matriz de errores E01–E16

| Error | Causa que debe rechazarse con código seguro |
| --- | --- |
| E01 UNAUTHENTICATED | Ausencia o invalidez de identidad de servicio |
| E02 FORBIDDEN | Rol o permiso servidor insuficiente |
| E03 TENANT_SCOPE_MISSING | Empresa no acreditada por backend |
| E04 TENANT_MISMATCH | Mensaje del tenant B en operación A |
| E05 INVALID_EVENT | EventId vacío, tipo desconocido o esquema inválido |
| E06 IDEMPOTENCY_CONFLICT | Misma clave con operación diferente |
| E07 STALE_CLAIM | Intento supersedido o lease inválido |
| E08 INVALID_UTC | Timestamp no UTC o ventana incompatible |
| E09 RETRY_BUDGET_EXHAUSTED | Máximo de intentos agotado |
| E10 RATE_LIMITED | Cuota de lote/proveedor excedida |
| E11 DEPENDENCY_UNAVAILABLE | DB o dependencia obligatoria no disponible |
| E12 SECRET_UNAVAILABLE | Secret missing o inválido, sin volcarlo |
| E13 PROVIDER_TIMEOUT | Resultado desconocido, NO ACK |
| E14 VERSION_UNSUPPORTED | Versión/contrato de consumidor incompatibles |
| E15 INTEGRITY_CONFLICT | Duplicado de EventoId, estado o commit discordante |
| E16 CANCELLED_OR_FAILED_CHECK | Prueba obligatoria no completó exitosamente en SHA causal |

Para futuras interfaces HTTP, no inferir que todos los errores internos deban exponerse al cliente; aplicar catálogo seguro, códigos de respuesta coherentes y no filtrar PII/secretos. Un worker sin HTTP utiliza estados internos tipados. Los E01–E16 aquí **no son excepciones lanzadas actualmente por la API**.

## 5. Escenarios positivos/negativos para fase técnica futura

| Par | Positivo por acreditar | Negativo obligatorio |
| --- | --- | --- |
| P01/N01 | Actor de servicio A con permiso procesa A | Payload B/ID B con actor A denegado sin efecto |
| P02/N02 | Intento durable enlazado a commit de negocio | Fallo entre negocio y outbox no deja efecto huérfano |
| P03/N03 | Repetir misma IdempotencyKey devuelve intención | Mismo key con carga distinta devuelve conflicto |
| P04/N04 | Dos consumidores concurren: uno reclama | Doble claim o doble efecto del mismo intento falla |
| P05/N05 | Confirmación con intento vigente después de ACK | ACK stale, timeout o crash antes del commit nunca PASS |
| P06/N06 | Retry y DeadLetter tienen cuota y backoff | Reintento infinito o entrega de DeadLetter sin autorización |
| P07/N07 | Readiness refleja dependencia saludable | Secret/DB ausente produce NOT_READY sin fuga |
| P08/N08 | Contrato de versión anterior sigue compatible | Version desconocida o enum nuevo no aceptado sin gate |
| P09/N09 | Revocación detiene futuras entregas | Tenant activa flag privilegiado o reclama sin RBAC |
| P10/N10 | CI del SHA y A5 de autor diferente acreditados | Cancelled/skipped/failed/in_progress o auto-A5 son NO_PASS |

Todos los P/N son **FUTUROS_NO_EJECUTADOS**. Esta unidad no ejecutó HTTP, .NET, SQL, MySQL efímero, Docker, Playwright, Cloudinary ni SMTP. Los límites de operación y métricas exigibles se fijarán con pruebas reales en M03 y permisos separados.

## 6. Auditoría A0–A4, siguiente gate y rollback

**A0**: diff únicamente de este Markdown y encabezado autocontenido con viñetas en CHANGELOG_AI.md; no modificar código, DAOs, endpoints, Domain, datos, CI, secrets o infraestructura. Relevar HEAD y tareas concurrentes.

**A1**: contar diez operaciones OP, catorce campos D, dieciséis errores E y diez pares P/N, confirmar que son contratos FUTUROS y no se atribuyen pruebas ejecutadas. **A2**: examinar JWT/RBAC real de API, tenant A/B, identidad sin HttpContext, revalidación del servicio, CAS, cuotas, idempotencia, privacidad y política de secretos. **A3**: cotejar OP05–OP08 con interfaces reales, verificar no alterar contratos binarios ni schema EF/MySQL; CI de compilación no causal a una adición Markdown y se consigna NO_APLICA, nunca PASS ficticio.

**A4**: fast-forward expected HEAD con CAS; commit debe contener solo dos Markdown. Verificar blob postpublicación, procedencia de CHANGELOG sin encabezados vacíos, Scope Lock y V5 completed/success en el SHA exacto (o equivalencia demostrada del árbol/descendiente). Un job requerido cancelled/skipped/queued/in_progress nunca se llama PASS.

**A5**: requiere otra Task Javier distinta del autor de esta microtarea, revisión de SHA, diff y pruebas causales. No se libera M03 por una fase conceptual VALIDANDO ni se autoriza QA, main o PROD.

**Rollback**: git revert normal del commit documental (preservando BITACORA histórica). Cualquier posterior cambio ejecutable deberá aportar rollback propio, parada controlada de worker, revalidación de tenant y pruebas en MySQL efímero; no usar force-push.
