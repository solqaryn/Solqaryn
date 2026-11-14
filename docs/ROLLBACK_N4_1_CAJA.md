# N4.1 — Caja — Rollback y recuperación

## Propósito

Definir una recuperación segura y verificable para incidentes del módulo N4.1 Caja sin ocultar fallos ni modificar `main`, Producción, secretos o infraestructura. Un rollback técnico no equivale a una reversión contable: los movimientos de negocio deben corregirse mediante operaciones compensatorias o flujos explícitamente soportados por el dominio, nunca borrando o editando historial de forma manual.

## Principios fail-closed

1. Detener nuevas mutaciones sobre la caja/sesión afectada antes de iniciar recuperación.
2. Preservar auditoría, logs y evidencia del SHA causal; no borrar evidencia para “dejar verde” un gate.
3. No editar directamente Caja, CajaSesion o CajaMovimiento en persistencia salvo un procedimiento de migración/recuperación previamente validado y ejecutado exclusivamente en un entorno autorizado de desarrollo/pruebas.
4. No reutilizar una sesión cerrada ni forzar estados incompatibles.
5. No aplicar rollback en Producción bajo este runbook.

## Clasificación del incidente

### A. Fallo de aplicación sin persistencia confirmada

- Confirmar que la transacción no produjo cambios parciales.
- Revalidar estado de caja y sesión mediante el contrato de lectura vigente.
- Corregir la causa en `Desarrollo`, ejecutar las pruebas causales y repetir la operación solo después de demostrar estado consistente.

### B. Movimiento de negocio persistido que debe corregirse

- No eliminar ni sobrescribir el movimiento histórico.
- Determinar si el dominio admite una operación compensatoria/reversa explícita.
- Si no existe una operación soportada, registrar el incidente y bloquear cualquier corrección manual hasta contar con una decisión funcional/técnica autorizada.
- Conservar la relación entre movimiento original, acción correctiva y evidencia de auditoría.

### C. Sesión en estado inconsistente

- Bloquear nuevas operaciones sobre la sesión.
- Confirmar caja, operador, estado, movimientos, arqueo y cierre observados.
- No forzar flags/estados en base de datos para reabrir o cerrar una sesión.
- Resolver únicamente mediante servicio/API o mediante una corrección versionada, probada y revisada en `Desarrollo`.

### D. Cambio de esquema/migración defectuoso

- Reproducir el fallo en una base de pruebas aislada.
- Validar historial de migraciones y snapshot EF antes de ejecutar cualquier `Down` o restauración.
- Preferir una migración correctiva forward cuando retirar la migración pueda perder o reinterpretar datos.
- Si se ensaya rollback de migración, respaldar la base de pruebas y validar después esquema, datos históricos y capacidad de volver a aplicar el forward path.
- Nunca ejecutar este procedimiento contra Producción desde VAEP.

## Recuperación técnica en entorno autorizado

1. Identificar el SHA funcional exacto que introdujo o expone el fallo.
2. Capturar el error causal, estado de CI y evidencia relevante sin secretos.
3. Preparar la corrección mínima en `Desarrollo`.
4. Ejecutar build y pruebas proporcionales:

```bash
cd backend
dotnet build Solqaryn.sln --configuration Release
dotnet test Solqaryn.sln --configuration Release --no-build
```

5. Cuando el cambio afecte persistencia/migraciones, ejecutar además los gates MySQL/migración aplicables en el entorno de pruebas autorizado.
6. Verificar que la recuperación no haya roto RBAC, auditoría, apertura, movimientos, arqueo ni cierre.
7. Mantener `P0=0` y `P1=0` como condición para declarar recuperado/cerrable el parent.

## Validación post-recuperación

- [ ] Caja existe y conserva su identidad/estado esperado.
- [ ] La sesión afectada conserva transiciones de estado válidas.
- [ ] Los movimientos históricos no fueron eliminados ni reescritos de forma destructiva.
- [ ] Saldos/diferencias observables son consistentes con los movimientos registrados.
- [ ] Permisos específicos continúan fail-closed.
- [ ] Auditoría/correlación permanece trazable.
- [ ] Build y pruebas causales aplicables pasan sobre el SHA funcional nuevo.
- [ ] Migraciones/snapshot/upgrade pasan cuando el cambio toca persistencia.
- [ ] No se tocaron `main`, Producción, secretos ni deploy.

## Escalamiento

Si no existe una operación de compensación segura, el estado persistido no puede reconciliarse sin intervención destructiva, o una recuperación falla dos veces dentro del protocolo Jules, detener el reintento automático y transferir el diagnóstico a ChatGPT/VAEP QA takeover. No crear R3 Jules.

## Criterio de cierre (Closure Semantics)

1. El incidente solo se considera recuperado y la certificación N4.1.H alcanzada cuando el estado funcional queda consistente, la evidencia histórica permanece intacta y todos los gates causales aplicables están verdes (DoD/P0=0/P1=0) o documentados N/A.
2. **Requisito estricto (M11)**: el cierre fail-closed / rollback requiere **evidencia explícita, actual y real** del respaldo cifrado M11 en `Desarrollo`, identificando el artifact operacional exacto junto con su archivo `.sha256` coincidente y su archivo `.meta.json` coincidente.
3. **Validación de restauración**: requiere una restauración/drill aplicable y correlacionada causalmente con **ese mismo artifact operacional exacto**, validando su `.sha256` y `.meta.json` coincidentes antes de certificar N4.1.H. Evidencia histórica, planificada, definida únicamente por workflow o producida por un backup desechable distinto no satisface este criterio.
4. Preservar estado `NO_LISTO` hasta demostrar el cumplimiento íntegro sin excepciones. No declarar evidencia M11 actual inexistente, no marcar `LISTO` y no promover N4.2 por preparación.

Este documento no concede autorización para merge, Producción o despliegue.
