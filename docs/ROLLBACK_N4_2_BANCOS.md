# N4.2 — Bancos — Rollback y recuperación

## Propósito

Definir una recuperación segura y verificable para incidentes del módulo N4.2 Bancos sin ocultar fallos ni modificar `main`, Producción, secretos o infraestructura. Un rollback técnico no equivale a una reversión contable: los movimientos de negocio deben corregirse mediante operaciones compensatorias o flujos explícitamente soportados por el dominio, nunca borrando o editando historial de forma manual.

## Principios fail-closed

1. Detener nuevas mutaciones sobre la cuenta bancaria afectada antes de iniciar recuperación.
2. Preservar auditoría, logs y evidencia del SHA causal; no borrar evidencia para “dejar verde” un gate.
3. No editar directamente Bancos, CuentaBancaria, BancoMovimiento, Conciliacion o ConciliacionDetalle en persistencia salvo un procedimiento de migración/recuperación previamente validado y ejecutado exclusivamente en un entorno autorizado de desarrollo/pruebas.
4. No forzar estados incompatibles ni reutilizar conciliaciones cerradas.
5. No aplicar rollback en Producción bajo este runbook.

## Clasificación del incidente

### A. Fallo de aplicación sin persistencia confirmada

- Confirmar que la transacción no produjo cambios parciales.
- Revalidar estado de la cuenta bancaria mediante el contrato de lectura vigente.
- Corregir la causa en `Desarrollo`, ejecutar pruebas causales y repetir solo después de demostrar estado consistente.

### B. Movimiento de negocio persistido que debe corregirse

- No eliminar ni sobrescribir el movimiento histórico.
- Determinar si el dominio admite una operación compensatoria/reversa explícita.
- Si no existe una operación soportada, registrar el incidente y bloquear cualquier corrección manual hasta contar con decisión funcional/técnica autorizada.
- Conservar relación entre movimiento original, acción correctiva y evidencia de auditoría, incluidas claves de idempotencia.

### C. Proceso de conciliación en estado inconsistente

- Bloquear nuevas operaciones sobre la conciliación activa.
- Confirmar saldos libro/banco, partidas conciliadas, partidas en tránsito y diferencias observadas.
- No forzar flags/estados en base de datos para reabrir o cerrar una conciliación.
- Resolver únicamente mediante servicio/API o corrección versionada, probada y revisada en `Desarrollo`.

### D. Cambio de esquema/migración defectuoso

- Reproducir el fallo en una base de pruebas aislada.
- Validar historial de migraciones y snapshot EF antes de cualquier `Down` o restauración.
- Preferir migración correctiva forward cuando retirar una migración pueda perder o reinterpretar datos financieros.
- Si se ensaya rollback de migración y `Down` lo permite, respaldar la base de pruebas y validar después esquema, datos históricos y capacidad de reaplicar el forward path.
- Nunca ejecutar este procedimiento contra Producción desde VAEP.

## Recuperación técnica en entorno autorizado

1. Identificar el SHA funcional exacto que introdujo o expone el fallo.
2. Capturar error causal, estado de CI y evidencia relevante sin secretos.
3. Preparar la corrección mínima en `Desarrollo`.
4. Ejecutar build y pruebas proporcionales:

```bash
cd backend
dotnet build Solqaryn.sln --configuration Release
dotnet test Solqaryn.sln --configuration Release --no-build
```

5. Cuando el cambio afecte persistencia/migraciones de Bancos, ejecutar además gates MySQL/migración aplicables en entorno de pruebas autorizado.
6. Verificar que la recuperación no rompa RBAC, auditoría, saldos, movimientos, referencialidad ni conciliaciones.
7. Mantener `P0=0` y `P1=0` como condición de cierre.

## Validación post-recuperación

- [ ] Bancos y Cuentas Bancarias conservan identidad/estado esperado.
- [ ] La conciliación afectada conserva transiciones válidas.
- [ ] Los movimientos históricos no fueron eliminados ni reescritos destructivamente.
- [ ] Saldos/diferencias observables son consistentes con movimientos registrados y conciliados.
- [ ] Permisos específicos de N4.2 continúan fail-closed.
- [ ] Auditoría/correlación e idempotencia permanecen trazables.
- [ ] Build y pruebas causales aplicables pasan sobre el SHA funcional nuevo.
- [ ] Migraciones/snapshot/upgrade pasan cuando el cambio toca persistencia.
- [ ] No se tocaron `main`, Producción, secretos ni deploy.

## Escalamiento

Si no existe compensación segura, el estado persistido no puede reconciliarse sin intervención destructiva o una recuperación Jules falla en R2, transferir a ChatGPT/VAEP QA takeover. No crear R3 Jules.

## Criterio de cierre

Si M11 es aplicable, el cierre requiere evidencia explícita y actual del respaldo cifrado operacional exacto, junto con `.sha256` y `.meta.json` coincidentes, y una restauración/drill causalmente correlacionada con ese mismo artifact. Evidencia histórica o de otro backup no satisface el criterio.

Preservar `NO_LISTO` hasta demostrar DoD, P0=0/P1=0 y gates aplicables terminales sobre el SHA exacto.

Este documento no concede autorización para merge, Producción o despliegue.
