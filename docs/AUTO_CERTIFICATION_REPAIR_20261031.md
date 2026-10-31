# SOLQARYN — Reparación fail-closed del cierre comercial autónomo DEV (31/10/2026)

Estado de la enmienda: PROPUESTA PROTEGIDA HASTA MERGE. No declarar activación sin prueba real `AUTO_CERT=PASS` en un PR comercial posterior.

## Evidencia inicial

- `dev@ffdda57fa42537e2531f41b511c3969b496565e1`: dos workflows `push` reales terminaron SUCCESS; jobs obligatorios Scope Lock, Seguridad multiempresa y Build/backend pasaron sobre ese SHA exacto (runs 41213232294 y 41213232287).
- PR #3804 fusionado el 31/10/2026 (merge `5371292df5473d66cc137c41bcf3d2e2554a1f58`), con cero reviews. Su log previo indicaba `AUTO_CERT_BOOTSTRAP`, no `AUTO_CERT=PASS`. Se conserva esta desviación de gobernanza; no convertirla retroactivamente en aprobación.
- Las tres Tasks nativas de Javier permanecen habilitadas :00/:20/:40 en `America/Tegucigalpa` y sus últimas ejecuciones observadas estaban `completed`. Esto acredita ejecución programada, no cierre comercial autónomo.
- PR #3803: conflicto `mergeable_state=dirty` y cero checks en su SHA actualizado. PR #3807: CI transversal SUCCESS sobre SHA anterior, pero el registro del Scope Lock muestra `PR_BASE_SHA=0cec1a3b...` y `AUTO_CERT_BOOTSTRAP`. PR #3808: DRAFT sin checks sobre HEAD vigente. No certificar ni fusionar usando pruebas heredadas.

## Cambio técnico propuesto

1. El job **obligatorio** `verify-solqaryn-project-scope` rechaza `BASE_DEV_STALE`: compara `github.event.pull_request.base.sha` con el verdadero `refs/heads/dev` consultado por Git. Rechaza también `TRUSTED_BASE_VERIFIER_MISSING`; el bootstrap histórico nunca vuelve a contar como PASS.
2. El verificador se carga desde el commit BASE exacto del PR y comprueba las aprobaciones permitidas, check runs exact-SHA y la prueba dirigida. Rechaza cambios a **cualquier workflow GitHub y a `scripts/security/**`** sin revisión independiente de gobierno; no se apoya en una lista incompleta de dos workflows.
3. Nuevo workflow `solqaryn-commercial-directed.yml`: xUnit dirigido mediante ID comercial del título del PR, checkout de su SHA inmutable, filtro de prueba y TRX leído con cantidad de tests > 0, passed > 0 y cero fallos. El check se identifica como `SOLQARYN - Directed <título del PR>`. Un nombre de check arbitrario o una suite con cero tests no satisfacen `AUTO_CERT`.
4. El gate espera los checks aplicables aún pendientes y falla cerrado si falta cualquiera, el SHA cambia o vence el plazo. `AUTO_CERT=PASS` es certificación técnica de CI, **no** review humana de GitHub.
5. Las Tasks autorizadas pueden trabajar sin pedir intervención humana para PR comerciales ordinarios, pero no deben omitir derechos efectivos, seguridad o requerimientos de revisión protegidos. Si una rama queda obsoleta, se actualiza **sin force** después de comprobar exclusividad. Los conflictos se resuelven con diff dirigido y tests; no se descarta trabajo ajeno ni se falsifica PASS.

## Límite institucional y aceptación pendiente

Esta enmienda no cambia los seis IDs registrados, la allowlist comercial 5/6, la exclusión de Alex :30, los permisos por cuenta, el WIP máximo seis, QA, main, PROD, Aiven, secretos, roles ni datos. El cierre LISTO exige readback auténtico en PLAN_MAESTRO, COLA (fórmulas intactas) y BITACORA después del merge protegido.

**No hay 100 % demostrado** hasta: (a) CI exacto del PR de enmienda con checks requeridos, (b) integración protegida y control independiente de gobierno cuando corresponda, (c) PR comercial actualizado desde HEAD de dev, con test dirigido y `AUTO_CERT=PASS` real, (d) merge autenticado y lectura posterior de Sheets. No permitir auto-revisión disfrazada cambiando credenciales; la independencia es una propiedad de control, no del nombre de la cuenta.

Rollback: revert protegido de esta enmienda, conservando commits, bitácora y evidencias originales.
