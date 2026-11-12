# P2-012.A — Catálogo de jobs y Outbox sin contexto HTTP (DEV)

**Autoridad:** Plan Comercial Inicial V3, P2-012.A; `solqaryn/Solqaryn`, base `dev@0ef1e736a4538c2d48c66bbd12fafb77259400aa`. Solo evidencia y regresión sintética en rama `work/solqaryn-p2-012-a`. No hay ejecución en QA/PROD.

## Inventario causal por componente

| Componente | Punto de entrada / consumidor | Fuente de tenant sin HTTP | Límite comprobable |
| --- | --- | --- | --- |
| `Application/Services/OutboxRetryProcessor.cs` | `ProcesarLoteAsync(empresaId, ahoraUtc, dispatcher)` invocado por caller externo | `empresaId > 0` obligatorio; `ahoraUtc` UTC; repositorio recibe el mismo `empresaId`; se rechaza lote contaminado antes de despachar | No implementa scheduler, hosted worker ni obtiene HttpContext. El cliente del dispatcher debe preservar idempotencia |
| `IMensajeOutboxRepository` / `MensajeOutboxRepository` | Stale recovery, claim, confirmar, reprogramar | Métodos reciben `empresaId` explícito y estado/intento esperado | Acreditar transacción/aislamiento MySQL en CI de impacto; no se demuestra por mocks |
| `MensajeOutbox` | Intención durable con `EmpresaId`, `EventoId` y clave de idempotencia | `MensajeOutbox.Crear` exige `empresaId > 0` | No asumir envío externo por simple registro |
| `MensajeOutboxService` / `MensajesOutboxController` | Registro desde API, servicio valida `IUsuarioScopeService` | En API se verifica autorización; en Application se valida pertenencia a empresa antes de persistir | NO tomar cabecera HTTP como identidad de un job background |
| `DevCloudinaryLegacyNormalizationHostedService` | HostedService de normalización legacy DEV, **separado del Outbox** | Requiere Development+Render+flag explícito+host DB+Cloudinary y un único tenant verificable desde DB | Opt-in y controles de entorno; no activar ni asumir disponibilidad de infraestructura |

## Reutilización de regresiones presentes en dev

- `P2012COutboxSinHttpTenantTests`: despacho explícito A/B con clave de idempotencia común; tenant inválido bloqueado.
- `P2012DOutboxLoteCruzadoTests` y `N72DOutboxRetryProcessorTests`: lote mezclado A/B se rechaza antes de cualquier efecto externo.
- `P2012EOutboxFalloTenantSinHttpTests`: fallo externo en A no reprograma/confirma B.
- **Nueva prueba dirigida:** `P2012AOutboxSinHttpArranqueTests`, cuatro casos sintéticos; valida UTC antes de todo claim/dispatch y asegura que stale/claim reciben el tenant explícito sin depender de HttpContext. El test de reloj no UTC exige 0 consultas y 0 efectos.

## Riesgos, límites y decisión

No se agrega ningún scheduler ni se cambian servicios de producción. `IOutboxEffectDispatcher` es interfaz, NO prueba despacho real; tampoco se prueban aquí redistribución de jobs, cadenas SMTP/Cloudinary, carreras MySQL, límites de RBAC/JWT ni llamadas HTTP. `dotnet test` local no ejecutado en esta sesión: el PASS depende de GitHub Actions exact-HEAD. La prueba valida únicamente los contratos citados, no un certificado E2E.

**Integración:** revisión triple independiente por TASK_ID ajeno a autora/contribuyentes; Scope Lock, Seguridad multiempresa, Backend y Directed cuando aplique, en SUCCESS exact SHA; `COLA!G234=SI` por fórmula, FIFO global, merge protegido, posmerge 3/3 y readback institucional. No confundir un PR en VALIDANDO con LISTO.

**Rollback:** revert protegido y selectivo de este documento, el test y la nota del `CHANGELOG_AI.md` sin reescribir historia ni afectar workers/consumidores ajenos.
