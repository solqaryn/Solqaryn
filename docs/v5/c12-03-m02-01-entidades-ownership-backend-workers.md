# SOLQARYN V5 — C12-03.M02.01: entidades, ownership y límites de backend/workers

**Microtarea:** `C12-03.M02.01` (Backend y workers separados; contrato M02). **Autor:** PRIMARY DEV 05 — JAVIER_CHATGPT, TASK_ID `6adc2e55f47c81919f6374e561687076`. **Rama exclusiva:** `solqaryn/Solqaryn@dev`. **Scope Lock:** STRICT. Fuente institucional: STANDARD V5 FINAL, PLAN_MAESTRO/COLA/DAG_TECNICO y BITACORA.

## 1. Alcance contractual, no despliegue

El objetivo consiste en modelar ownership y fronteras de confianza para una posible separación posterior de backend y procesos de trabajo, **sin implementar ni iniciar un worker, cron, scheduler, relay, mensaje entre Tasks, webhook, migración, hosting ni servicio nuevo**. Que un componente Application pueda procesar un lote no demuestra que esté hospedado, que exista un daemon ni que tenga licencia para operar. Ningún modelo aquí autoriza Hostinger, Aiven/Render/Vercel, DNS, Cloudflare, R2, secretos o QA/main/PROD.

Fuentes reales de Git, verificadas contra el inventario terminal C12-03.M01.06:
- `backend/src/API/Program.cs` — blob `01425cff56eb13e12022b1b81a2f322d17fd1fff`: composición y servicios API; no implica que un worker aislado herede HttpContext.
- `backend/src/API/Solqaryn.API.csproj` — blob `2a7b33a0974d1ebe13676593d84b458b1243e037`: `net10.0`, autenticación JwtBearer.
- `backend/src/Infrastructure/Solqaryn.Infrastructure.csproj` — blob `6210d8b12abd77171d95d17e2292c1a319b89263`: `net10.0`, EF 10.0.12 y proveedor `MySql.EntityFrameworkCore` 10.0.9.
- `backend/src/Infrastructure/Persistence/AppDbContext.cs` — blob inventariado `c4b32f6637f1b4426655bb0cd12af8dd9d229866`; su presencia no revela por sí misma el estado del esquema desplegado.
- `backend/src/Application/Services/OutboxRetryProcessor.cs` — blob `542c0a655a3ee0698e3d80f1854d43903b6470fa`: lógica de batch tenant-bound y retry finito, **sin HostedService**.
- `backend/src/Application/Interfaces/IMensajeOutboxRepository.cs` — blob `a7dc258df9b1123dfe54ea85ba6d4dcc2a1eb478`: contrato de claim y confirmación condicionada.
- `docs/v5/c12-03-m01-06-gates-rollback-backend-workers.md` — blob `3771d1eb465cdb165a3402dbfc673c325b23f78b`: 16 barreras B01–B16, 10 contenciones R01–R10, 8 pares P/N futuros, **A5 independiente acreditada** de autor distinto.
- `docs/v5/c11-03-m02-01-entidades-ownership-invariantes-outbox.md` — contrato de eventos únicamente conceptual, sin activación ni autorización de ejecutar un relay.

## 2. Doce componentes y owners conceptuales

| ID | Componente / entidad lógica | Ownership y aislamiento requerido |
| --- | --- | --- |
| C01 | API ASP.NET Core | Actor autenticado backend, identidad JWT validada y policy RBAC por recurso |
| C02 | Operación de negocio | Tenant real del objeto persistido, no identificador elegido por caller |
| C03 | Intent de trabajo durable | `EmpresaId`, `EventoId`, llave idempotente y estado verificados |
| C04 | Scheduler/trigger futuro | **NO IMPLEMENTADO**; ninguna Task ChatGPT sustituye ejecución server-side |
| C05 | Proceso worker futuro | Identidad de servicio separada; sin sesión HTTP ni permisos implícitos |
| C06 | Contexto tenant de job | Capacidad efímera obtenida de repositorio/autorizador y revalidada |
| C07 | Repositorio EF/MySQL | Filtro por tenant, transacción y concurrencia condicional por claim |
| C08 | Migrador de esquema | Autoridad única y bloqueo transaccional antes de cualquier DDL futuro |
| C09 | Dispatcher externo futuro | Adaptador deshabilitado por defecto, permiso y cuotas por proveedor |
| C10 | Lease/claim y versión | Exclusividad por CAS/estado e intento, distinta de mero timestamp |
| C11 | Auditoría operacional | Logs con campos allowlisted; nunca tokens, payload o PII |
| C12 | Health/readiness y rollback | Identidad por proceso, dependencia real, reversión y separación de liveness |

Los C04/C05/C09 son **diseño futuro**: no se agrega registro DI, servicio hospedado, cola, módulo de infraestructura ni worker real a esta entrega. C01/C02/C03/C07/C10 tienen anclajes parciales existentes, sin certificar una topología desplegada.

## 3. Dieciséis invariantes I01–I16 de seguridad y continuidad

| ID | Condición fail-closed para implementación posterior |
| --- | --- |
| I01 | Un job sin `HttpContext` no obtiene `EmpresaId` ni permiso heredado de la API. |
| I02 | Identidad de servicio y alcance activo se acreditan en backend por grant específico, no por payload. |
| I03 | El tenant del trabajo y del recurso persistido deben coincidir antes de reclamar, leer o despachar. |
| I04 | Las pertenencias y roles caducados/revocados se comprueban también al consumir, no solo al encolar. |
| I05 | Un job nunca ejecuta una acción que el actor humano o grant de servicio no pudiera autorizar. |
| I06 | Claim concurrente emplea CAS/lock real y un ganador; stale lease no prueba abandono por antigüedad. |
| I07 | Confirmación de resultado exige `EmpresaId`, ID y versión/intento esperado; ACK viejo es NO_OP. |
| I08 | Encolado y mutación de negocio comparten transacción cuando existe causalidad de efecto. |
| I09 | Retry es finito, idempotente y con backoff; timeout externo no se convierte en éxito. |
| I10 | Worker futuro no ejecuta DDL paralelo a API; migrador único requiere ownership comprobable. |
| I11 | Falta de secreto, configuración o base sana hace readiness negativo; liveness no equivale a capacidad de trabajar. |
| I12 | Payload, JWT, OTP, DSN, PII y errores de proveedor no se escriben en logs ni trazas. |
| I13 | CorrelationId identifica una traza, no autoridad ni pertenencia tenant. |
| I14 | No hay conexión entre Tasks, webhooks, dispatcher o runtime infinito autorizada por documentación. |
| I15 | `cancelled/skipped/in_progress/failed` no son PASS CI, ni un reviewer puede aprobar su propia obra. |
| I16 | QA/main/PROD, hosting, DNS, credenciales y datos reales requieren autorización de entorno separada. |

## 4. Relaciones conceptuales y estados

La API puede crear una intención durable solo dentro de su transacción verificada; un eventual proceso de consumo deberá obtener autoridad de servicio independiente y revalidar tenant y recurso. El outbox ya conserva estados `Pendiente`, `Procesando`, `Entregado`, `Fallido` y `DeadLetter`, y la transición está mediada por claim condicional del repositorio. **La existencia de esos tipos no prueba que haya un worker activo ni que dos nodos hayan sido ensayados**.

Relación propuesta: tenant 1:N recurso/operación; operación 1:N intent; intent 0:1 claim vigente; claim 0:1 confirmación/fallo por intento; proceso futuro N:1 identidad de servicio; identidad y scope se revalidan por cada operación y no se guardan como permiso implícito en el mensaje.

## 5. Ocho contratos de falla F01–F08

| Falla potencial | Denegación, contención y prueba requerida |
| --- | --- |
| F01 Tenant A/B | Ninguna consulta a B durante trabajo A; test negativo y auditoría redacted |
| F02 Actor revocado | Job no ejecuta bajo permiso desactualizado |
| F03 Doble claim | CAS único, segundo consumidor recibe 0 |
| F04 Crash entre dispatch y confirmación | Reentrega con misma idempotency key, sin doble efecto |
| F05 Migrador doble | Solo un dueño DDL, otro rechaza sin desactivar controles |
| F06 Secreto ausente | Readiness NO_GO, error seguro sin imprimir secretos |
| F07 Evento tipo incompatible | Rechazar versión antes de mutación; no activar dispatch |
| F08 CI incompleta | Conservar VALIDANDO y exigir success causal del SHA |

## 6. Diez oráculos P01/N01–P10/N10 FUTUROS_NO_EJECUTADOS

| Par | Positivo futuro | Negativo futuro |
| --- | --- | --- |
| P01/N01 | API A con JWT/RBAC actual crea intención A | Caller B intenta operar recurso de A |
| P02/N02 | Worker A con grant vigente consume A | Job sin contexto intenta usar EmpresaId de payload |
| P03/N03 | Dos claims compiten, uno gana | Ambos reciben mismo mensaje |
| P04/N04 | Replay idéntico preserva un efecto | Misma clave con payload distinto duplica pago |
| P05/N05 | Revocación detiene siguientes consumos | Permiso obsoleto sigue consumiendo |
| P06/N06 | DB efímera transacción+outbox hace commit | Crash deja efecto sin mensaje o viceversa |
| P07/N07 | Migrador exclusivo conserva esquema | API y worker ejecutan DDL simultáneo |
| P08/N08 | Health señala dependencia caída | Secreto/DB ausente reporta ready |
| P09/N09 | Auditoría solo registra código seguro | Filtra JWT/DSN/PII o payload |
| P10/N10 | A5 independiente y CI SHA exacto permiten revisión | Autor aprueba A5 o cancelled equivale a PASS |

**Todos son FUTUROS_NO_EJECUTADOS.** Este Markdown no ejecuta .NET, Angular, MySQL efímero, Docker, Hostinger, nube ni Playwright; definir pruebas no equivale a realizarlas.

## 7. A0–A5, impactos y reversión

**A0:** preflight de Skill, identidad, HEAD y Scope Lock, PLAN PENDIENTE/I vacío, COLA G/O=SI y DAG VALIDADO/SI antes del CAS. **A1:** cotejar 12 componentes, 16 invariantes, 8 fallas y 10 pares de pruebas futuras. **A2:** JWT/RBAC, tenant A/B sin HttpContext, lease/CAS, idempotencia, privacidad, migrador único y denial. **A3:** contrato concordante con API net10, Application/Infrastructure, outbox y M01; no interpretar builds/MySQL/HTTP no pertinentes al diff documental como PASS. **A4:** publicar solo este doc y CHANGELOG_AI.md en `dev`, fast-forward CAS, encabezado unido a viñetas, Scope Lock y V5 exact-SHA `completed/success`, más readbacks PLAN/COLA/DAG/BITACORA. **A5:** revisión por una de las otras Tasks Javier distinta de DEV05 antes de cualquier gate de fase; no autoaprobación.

**Rollback:** `git revert` normal tras leer HEAD; no hay datos, procesos, versiones de software ni migraciones que revertir: el changeset es documental. No autoriza instalación de workers ni cambios en dominios, secretos, proveedores, PROD/QA/main o infraestructura.
