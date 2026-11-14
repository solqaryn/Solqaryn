# Auditoría forense de N8.6, N8.7 y N8.8

- **Fecha de corte:** 2026-09-15T21:34:00Z
- **Rama / HEAD auditado:** `Desarrollo` / `c9693ace419cee195945eee0aecc51a8635e9317`
- **HEAD reconciliado antes de publicar:** `81af92dbfb8f33de8bd6091c6f23647b3183f5f7`
- **Autoridad:** `docs/VAEP_AUTHORITY.md`
- **Producción:** no tocada.
- **Secretos:** no leídos ni expuestos.

Esta auditoría contrasta los receipts con los commits, diffs, revisiones, filas de
`COLA`, el runtime de Desarrollo y pruebas dirigidas. No trata un receipt como
prueba suficiente por sí solo.

## Fuentes comprobadas

1. `COLA`, `PLAN_MAESTRO`, `CONFIG` y `CONTROL_TOWER` del Plan Maestro.
2. Commits y artefactos de revisión/receipt en `origin/Desarrollo`.
3. Diff funcional de WhatsApp en `7d736f4b` y prueba actual:
   `npm exec vitest run src/app/core/services/factura-whatsapp-handoff.contract.spec.ts`
   — 4/4 PASS.
4. Runtime de Desarrollo: `GET /health` y `GET /health/ready` respondieron HTTP
   200; el segundo confirmó `database=connected`.
5. Render identificó el servicio `solqaryn-api-desarrollo`, rama `Desarrollo`,
   plan Free, último deployment Live y variables presentes sin revelar valores.
6. M11 histórico: GitHub Actions run `33504690547`, de 2026-09-01, ejecutó con
   éxito el backup cifrado real de Desarrollo y el restore/drill del mismo
   artefacto en MySQL descartable. No sustituye una nueva certificación de
   proveedor ni de backups automáticos actuales.

## Resultado por microtarea

| Task | Estado previo | Resultado forense | Evidencia material | Línea de tiempo | Acción |
| --- | --- | --- | --- | --- | --- |
| N8.6.A | LISTO | CONFIRMED_LISTO_REAL | preflight/review y dependencia de N8.6 | coherente | conservar |
| N8.6.B | LISTO | N_A_JUSTIFIED | no era necesario un contrato de dominio para el handoff de factura | coherente | conservar |
| N8.6.C | LISTO | N_A_JUSTIFIED | no se requirió migración ni persistencia nueva | coherente | conservar |
| N8.6.D | LISTO | N_A_JUSTIFIED | el API de auditoría ya existía; el fix fue de orquestación UI | coherente | conservar |
| N8.6.E | LISTO | N_A_JUSTIFIED | no hubo cambio UX independiente del fix final en G | coherente | conservar |
| N8.6.F | LISTO | N_A_JUSTIFIED | no hubo delta RBAC/auditoría adicional; el contrato preserva enmascaramiento | coherente | conservar |
| N8.6.G | LISTO | CONFIRMED_LISTO_REAL | `7d736f4b`: `aperturaAceptada` separa fallback de auditoría; test dirigido 4/4 PASS | receipt final 21:01Z, commit funcional anterior verificable | conservar |
| N8.6.H | LISTO | CONFIRMED_LISTO_REAL | receipt final `59d94ef7`, README y contrato final con P0/P1=0 | coherente | conservar |
| N8.7.A | LISTO | CONFIRMED_LISTO_REAL | preflight define que la carga controlada pertenece a G | coherente | conservar |
| N8.7.B | LISTO | N_A_JUSTIFIED | una prueba de rendimiento no exigía cambio de dominio | coherente | conservar |
| N8.7.C | LISTO | N_A_JUSTIFIED | no se autorizó un índice especulativo sin profiling | coherente | conservar |
| N8.7.D | LISTO | N_A_JUSTIFIED | no apareció una causa API antes de ejecutar la carga real | coherente | conservar |
| N8.7.E | LISTO | STALE_CONTROL_ONLY | no se probó un gap frontend; la carga real sigue en G | corregida: review `21:03:50Z`, receipt `21:04:05Z` | mantener N/A y corregir fila |
| N8.7.F | LISTO | N_A_JUSTIFIED | no apareció requerimiento de seguridad/observabilidad adicional antes de G | coherente tras la corrección | conservar |
| N8.7.G | BLOQUEADO | BLOCKER_CONFIRMED | runners `k6`, Locust, JMeter y Artillery ausentes; no hay identidad/tenant de rendimiento ni sesión web utilizable; salud DEV sí está disponible | blocker revalidado | requiere workload autenticado no productivo |
| N8.7.H | PENDIENTE | REOPEN_REQUIRED | no existe receipt H; depende causalmente de G | dependency-gated | cerrar tras G |
| N8.8.A | LISTO | CONFIRMED_LISTO_REAL | preflight separa provider/restore hacia G | coherente | conservar |
| N8.8.B | LISTO | N_A_JUSTIFIED | backup no exigió cambio de dominio | coherente | conservar |
| N8.8.C | LISTO | N_A_JUSTIFIED | no había gap de migración que pudiera sustituir la verificación de restore | coherente | conservar |
| N8.8.D | LISTO | N_A_JUSTIFIED | el control de backup pertenece al proveedor y al workflow M11, no a un endpoint nuevo | coherente | conservar |
| N8.8.E | LISTO | N_A_JUSTIFIED | no era necesario UI para verificar backup | coherente | conservar |
| N8.8.F | LISTO | N_A_JUSTIFIED | la seguridad relevante está en cifrado/least exposure ya cubiertos por M11; G debe validar la instancia real | coherente | conservar |
| N8.8.G | BLOQUEADO | REOPEN_REQUIRED | existe ruta segura M11 para backup real + restore aislado; falta aún certificación fresca de motor/proveedor y backup automático | no cerrada | ejecutar Paso 1 y Paso 2 |
| N8.8.H | PENDIENTE | REOPEN_REQUIRED | no existe receipt H; depende de la evidencia nueva de G | dependency-gated | cerrar tras G |
| N8.9.A | PENDIENTE | REOPEN_REQUIRED | la fila de restore sigue abierta y no puede heredar el drill histórico | sin ejecución actual | ejecutar después de backup real |

N8.9.B–H no existen todavía en `COLA`; no se inventaron IDs ni estados.

## Corrección de tiempo N8.7.E

El valor histórico de `COLA` mostraba inicio `2026-09-15T21:06:20Z` y fin
`2026-09-15T21:04:05Z`. El receipt
`N8.7_TIMESTAMP_CORRECTION_20260915T210607Z_SUP48.json` preserva el historial
y establece como tiempo canónico la revisión en `21:03:50Z` (commit
`0bcac92b`) y el receipt en `21:04:05Z` (commit `a748378d`).

Como corrección append-only del control plane, se actualizó exclusivamente la
fila `COLA!N676:O676` a esos dos instantes canónicos. Se mantuvieron estado,
evidencia y demás columnas sin cambios.

## Revalidación N8.7.G

La revalidación no convirtió el bloqueo en PASS:

- El host puede ejecutar `curl`, Node, .NET, Docker y el cliente MySQL, pero no
  tiene `k6`, Locust, JMeter ni Artillery.
- No se encontró credencial, ruta de seed, tenant sintético ni identidad de
  rendimiento autorizada en las fuentes locales. La sesión de la app expuesta
  a esta herramienta redirige a `/login` por expiración.
- Render confirma que la API de Desarrollo está viva; su Shell está bloqueada
  para el plan Free y requiere una actualización pagada, que no se solicitó.
- No se envió carga anónima, no se intentó login por fuerza bruta y no se
  crearon ventas ni transacciones financieras.

Por tanto, N8.7.G conserva `BLOCKER_CONFIRMED`: hace falta una identidad/tenant
no productivo y un runner de carga autenticado o una autorización equivalente.

## Siguiente hard gate

La auditoría permite iniciar solamente el **Paso 1**: certificar el motor y el
proveedor de la base de Desarrollo mediante evidencia segura. No autoriza
declarar `AUTOMATIC_BACKUP=PASS`, `RESTORE_ISOLATED=PASS` ni
`STAGING_EQUIVALENT=PASS`.
