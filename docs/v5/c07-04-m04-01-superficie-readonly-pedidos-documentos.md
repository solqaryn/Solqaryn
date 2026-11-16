# C07-04.M04.01 — superficie de criterios privados para pedidos y documentos

## Alcance y riesgo
R2 por contexto de pedidos, pagos y documentación fiscal. Se incorpora exclusivamente una
superficie read-only en el listado ERP existente `/pedidos-venta`, que ya usa
`authGuard` + `permisoGuard` y exige `Ventas:Ver` con tenant verificado en backend.
El nuevo panel comprueba además ambas condiciones y la ruta habilitada. No modifica
backend, modelos fiscales, importes, facturas, pagos, devoluciones, permisos ni bases de datos.

## Criterios verificables
- A0: ruta original y lista no mutativas; cuatro frentes: pedidos/cotizaciones,
  facturas/notas de crédito, caja/pagos, anulaciones/exportaciones.
- A1: pruebas de lectura positiva, sin permiso, sin contexto, ruta deshabilitada,
  revocación y frontera móvil; asserts deben fallar si se reabre el panel indebidamente.
- A2: guards de tenant y permisos efectivos por servidor, sin depender de localStorage
  como autoridad; escenarios de denegación sintéticos. Tenant A/B y JWT HTTP reales:
  **NO_MEDIDO**, no equivalen a aceptación de aislamiento backend.
- A3: Vitest y Playwright/Chromium real en GitHub-hosted con API simulada.
  MySQL 8.4, EF, servicios de cobro, emisión fiscal e integraciones reales:
  **NO_APLICA a este diff**; certificación financiera real: **NO_MEDIDO**.
- A4: commit fast-forward sobre dev, catálogo P0-007.A sin nuevas rutas ni
  archivos `.component.ts`, checks necesarios completos SUCCESS exact-SHA,
  Scope Lock/V5, readback PLAN_MAESTRO/COLA/DAG/BITACORA.
- A5: una Task Javier distinta del autor revisa fuente y pruebas independientemente.
  Nunca inferir A5_PASS de LISTO_DEV.

## Límites y rollback
El panel no inicia requests, crea órdenes, ejecuta POST, concilia caja ni asigna
permisos. Textos SIN_VERIFICAR/NO_MEDIDO no representan pruebas fiscales,
SLO, auditoría durable ni producción. Revertir cambios mediante `git revert`
ordinario después de verificar HEAD y efectos, sin force-push; solo dev,
sin modificaciones a QA/main/PROD ni datos productivos.
