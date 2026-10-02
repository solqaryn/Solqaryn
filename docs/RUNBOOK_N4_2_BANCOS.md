# N4.2 — Bancos — Runbook operativo

## Alcance

Este runbook cubre el módulo N4.2 Bancos en `Desarrollo`: ciclo de vida CRUD de CuentaBancaria y operaciones de depósito, retiro, transferencia, comisión, interés y conciliación. No autoriza cambios en `main`, Producción, secretos, merge ni despliegues.

## Precondiciones

- Trabajar únicamente sobre `Desarrollo` y un SHA exacto conocido.
- Confirmar P0=0 y P1=0 antes de certificar cierre.
- Mantener RBAC fail-closed: lectura `Finanzas/Ver`; creación `Finanzas/Crear`; edición `Finanzas/Editar`; activación `Finanzas/Activar`; desactivación `Finanzas/Desactivar`.
- Mantener auditoría sanitizada y correlación mediante `X-Correlation-ID` en los flujos aplicables.
- Las operaciones mutables que soportan idempotencia deben conservar y validar su `IdempotencyKey`; reutilizar una key con payload incompatible debe ser conflicto, no una segunda mutación silenciosa.

## Verificación funcional

1. Validar alta y lectura de CuentaBancaria.
2. Validar edición sin alterar identidad, banco, número de cuenta, moneda, saldo o estado fuera del contrato permitido.
3. Validar activar/desactivar con permisos específicos.
4. Validar depósito y retiro con montos válidos y rechazo de cuenta inactiva o monto inválido.
5. Validar transferencia con cuenta origen/destino distintas, activas, misma moneda y persistencia atómica de egreso/ingreso.
6. Validar comisión e interés conforme a las reglas de negocio vigentes.
7. Validar conciliación sin reescribir historial financiero.
8. Verificar replay idempotente equivalente y conflicto para la misma key con payload materialmente diferente.
9. Confirmar auditoría y correlación sin secretos ni datos sensibles innecesarios.

## Validación técnica proporcional

```bash
cd backend
dotnet build Solqaryn.sln --configuration Release
dotnet test Solqaryn.sln --configuration Release --no-build
```

Para cambios frontend, ejecutar además build/lint/tests aplicables del workspace `frontend` y los E2E de CuentaBancaria cuando corresponda.

## Criterios fail-closed

- Un workflow, manifiesto, Issue o sesión Jules por sí solo no certifica el parent.
- No declarar `LISTO_REAL` con CI/gates aplicables pendientes, P0/P1 abiertos o evidencia no revisada.
- Un R2 Jules agotado pasa a QA_TAKEOVER; nunca R3.
- No corregir movimientos financieros borrando o editando historial manualmente.
- No usar Producción para validar este runbook.

## Diagnóstico de incidentes

### Error de validación o regla de negocio

Capturar el error causal y el `X-Correlation-ID`, comprobar estado actual de la cuenta, corregir únicamente la causa en `Desarrollo` y ejecutar la prueba causal antes de repetir la operación.

### Reintento/idempotencia

Comprobar que un replay equivalente no duplica la mutación. Si la misma key llega con payload diferente, debe rechazarse como conflicto. No regenerar keys para ocultar un defecto de replay.

### Conciliación o saldo inconsistente

Detener mutaciones sobre la cuenta afectada, preservar auditoría y movimientos históricos y usar operaciones compensatorias/flujo soportado por dominio. No editar directamente la base para “cuadrar” saldos.

## Cierre

El parent solo es cerrable cuando DoD, P0=0/P1=0, review de evidencia y CI/gates aplicables del SHA exacto sean terminales y correctos. Si existe requisito M11 de backup/restore, la evidencia debe corresponder al artifact operacional exacto y su validación correlacionada.

Este documento no concede autorización para merge, Producción o despliegue.
