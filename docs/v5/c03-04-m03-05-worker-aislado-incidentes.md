# SOLQARYN V17 — C03-04.M03.05: worker aislado de previsualización de incidentes

**Autor:** PRIMARY DEV 03 — JAVIER (`6adc2e4bd8a48191ac7d8641ed95a1a3`). **Repositorio:** `solqaryn/Solqaryn@dev`. **Scope Lock:** STRICT. **Riesgo V15:** R2 (tenancy, incidentes, integridad, recuperación). **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES, C03-04.M03.05, PLAN/COLA/DAG actuales.

## Implementación real y límites

`C0304IsolatedIncidentReadOnlyDispatchWorker` constituye un worker **ejecutable como componente de aplicación en memoria y sin registro hosted** que procesa una tanda finita de hasta 24 tickets. Devuelve resultados por ticket, rechaza claves duplicadas con comparación ordinal, inquilino/sucursal ajenos, SHA de origen no coincidente, ausencia de evidencia sintética, estado revocado, evidencia no saneada, versión negativa, intentos de mutación o restore y cancelación. Reutiliza la proyección de metadatos M03.04 aprobada independientemente; no sustituye la puerta de autorización Application M03.03 ni la decisión Domain M03.01.

Su retorno favorable `AUTHENTICATED_INCIDENT_RUNTIME_ADAPTER_REQUIRED` significa **todavía no autorizado**. No despacha trabajos, no concede lectura de evidencias, no hace mutaciones, no restaura backups, no persiste recibos/idempotencia ni dispone de JWT validado en HTTP, RBAC de proveedor real, MySQL, storage, cola distribuida, leases, credenciales, scheduler, DI o API. `IsolatedServerEvidenceConfirmed` es únicamente entrada sintética para pruebas de preflight y **nunca prueba autenticación**. No usar este componente como autoridad live ni registrar un servicio hosted sin nueva microtarea y pruebas. No hay cambios en datos reales, infraestructura, QA/main/PROD o secretos.

## A0–A4 y seguridad

A0: Application, xUnit, este contrato y CHANGELOG autocontenido en commit individual; prerequisitos reales M02+A5 y M03.01–04+A5; rollback mediante `git revert` normal (sin force-push).
A1: diez métodos xUnit más casos Theory; nominal sin concesión, lote vacío/excesivo, key inválida, duplicado ordinal, SHA obsoleto, versión/estado, cancelación y errores de entrada.
A2: negativos tenant A/B, branch, revocación, provenance no verificada, saneamiento, mutación/recovery prohibidos. Ningún marcador `PreviewReceipt` permite efectos o acceso.
A3: build Release y xUnit reales en GitHub-hosted del SHA; integración de esta composición con proyección Application in-memory. MySQL 8.4 Category=Integration y Playwright NO_APLICA a este diff sin EF/DB/UI. No declarar API HTTP o backup restore real.
A4: validar Scope Lock exact SHA, procedencia V5, Backend Release, suite xUnit, seguridad multiempresa y autorización `completed/success`; CI queued/in_progress/failed/cancelled/skipped no equivale a PASS. Estado pasa a VALIDANDO si la entrega de código se publicó y solo falta CI externa. Registrar BITACORA y leer PLAN/COLA/DAG.

A5: revisor **otra** Task física Javier, independiente del autor, revisa código y resultados positivos/negativos, puede rechazar. Un LISTO_DEV eventual no equivale a QA/main/PROD ni a una restauración certificada.
