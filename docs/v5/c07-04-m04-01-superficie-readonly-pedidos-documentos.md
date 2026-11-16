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

## Defecto QA P2 y reparación causal (16 noviembre 2026)

GitHub Actions SHA original `8820ca5908f0d9de34e925a602adac84bdc49601`, job
`133456946733` del run `43912242323`: Vitest 6/6 y lint/build aprobaron,
pero Chromium 2/3 y 1 FAIL por selector Playwright ambiguo: el panel contiene
dos párrafos válidos con `NO_MEDIDO`. La aserción `.getByText('NO_MEDIDO')`
violó strict mode; no se observó fuga ni cambio de negocio. P2 es calidad
de la prueba; no indica vulnerabilidad del runtime. La reparación restringe
el assert al párrafo único `p[role="note"]`, mantiene verificación del texto,
cuatro tarjetas, negativas de permiso, responsive y ausencia de controles
transaccionales. Se exige CI nueva en SHA causal; nunca contar el fallo como
PASS ni cerrar LISTO antes de que todos los checks requeridos finalicen.

## Diagnóstico adicional de responsive (SHA reparado 6399072d)

La nueva CI `43913325430` dejó la prueba de accesibilidad con 2 PASS y
1 FAIL real: a 320 px el documento excede su viewport 75 px. La primera
reparación del selector fue correcta y las autorizaciones sintéticas
aprobaron. Se instrumenta únicamente geometría (etiquetas, clases y cajas)
para identificar el elemento causante, **sin volcar datos**, manteniendo
el assert global de overflow como prueba obligatoria; no declarar PASS
ni culpar al nuevo panel hasta aislar causalmente el elemento. R2 UI.

## Reparación responsive dirigida después de diagnóstico Chromium (16 noviembre 2026)

GitHub-hosted run `43914301315` sobre `febfe19ce5052216f1855c845c82ae616c9a6b3e`
confirmó seis Vitest y lint/build exitosos, pero 2/3 Chromium: a 320 px,
`document.documentElement.scrollWidth` excedía 75 px. El diagnóstico estructural
identificó la tabla de pedidos (no las cuatro tarjetas read-only), sin mostrar
datos de la empresa. Se acotan `.page`, `mat-card`, `mat-card-content` y `.table`
con min-width:0/max-width:100%, manteniendo scroll horizontal interno y todas
las columnas visibles mediante desplazamiento dentro del contenedor.
El assert GLOBAL de overflow permanece, junto con los rechazos de permiso,
revocación y cuatro criterios read-only. Los resultados de CI anteriores siguen
FAIL; exigir nueva suite GitHub-hosted exact-SHA, frontend, seguridad,
Scope Lock y V5 antes de LISTO; A5 debe revisarla otra Task Javier.
Rollback normal `git revert` sobre HEAD verificado; sólo dev, sin QA/main/PROD,
API, transacciones, permisos ni migraciones.
