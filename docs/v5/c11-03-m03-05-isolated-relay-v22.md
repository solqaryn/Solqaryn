# C11-03.M03.05 — Outbox relay aislado y recuperable (SOLQARYN V22)

**Ámbito:** solo solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT, PLAN_MAESTRO!D1674. Riesgo R2 por transacciones, colas, concurrencia y aislamiento tenant. Autor DEV05 (6adc2e55f47c81919f6374e561687076), candado V17 2000001674.

## A0 — Salida atómica y contrato

Unidad opt-in de worker/relay sobre el repositorio outbox **ya existente**. Recibe authorizedTenantId de una frontera servidora externa (esta clase NO autoriza identidades), usa SQL CAS ClaimDisponiblesAsync, confirma intento esperado y registra fallo con código seguro fijo, retry acotado y DeadLetter. No añade HostedService, DI, API pública, transporte real, proveedor externo, secretos, migraciones ni scheduler. La clase no se conecta a producción ni prueba delivery real. Un proveedor eventual deberá garantizar idempotencia por tenant/clave.

## A1–A3 — Prueba adversarial REAL pertinente

xUnit Category=Integration usa AppDbContext EF proveedor Oracle y servicio MySQL 8.4 efímero en GitHub-hosted. Empresas A/B comparten infraestructura; A procesa una entrega exitosa y un fallo transitorio, sin reclamar B; fallo persistido OUTBOX_TRANSPORT_FAILURE sin mensaje externo/PII, reintento no prematuro, reintento tras ventana, denegación de ACK stale y otra empresa, DeadLetter con máximo de intentos y cancelación antes del claim. Transporte es doble solo para simular éxito/fallo del proveedor: NO es prueba de integración de proveedor externo. Persistencia y exclusividad se prueban contra SQL físico.

## A4, A5 y rollback

Requerir commit fast-forward/CAS y GitHub Actions exact SHA completed/success: .NET Release, test dirigido TRX 1/1, Scope Lock, seguridad multiempresa, V5 changelog y checks obligatorios según alcance. SKIPPED/CANCELLED/IN_PROGRESS no son PASS requeridos. Con CI pendiente conservar VALIDANDO hasta A0–A4; A5 por otra Task Javier distinta del autor. No QA/main/PROD. Revertir aditivamente con git revert tras revisar HEAD, sin force-push.
