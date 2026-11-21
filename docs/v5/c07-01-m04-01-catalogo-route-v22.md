# C07-01.M04.01 - entrada canonica privada al catalogo universal (V22)

- Microtarea PLAN D: implementar ruta, pantalla o superficie publica/privada correspondiente.
- Resultado atomico: la URL `/catalogo-universal-v22` redirige a la superficie ERP existente y probada `/productos`. No se crea una segunda autoridad de inventario, precios ni un servicio HTTP paralelo.
- Riesgo R1 para este diff de routing Angular; A2 exige redireccion anonima, rechazo de tenant revocado y denegacion de `Productos:Ver`.
- Seguridad: el destino sigue protegido por `authGuard` y `permisoGuard`, que revalidan contexto tenant backend y ruta habilitada `productos`; el alias no contiene datos ni altera JWT, roles, API, SQL, taxes o DI.
- Matriz de ownership: Productos/variantes/precios -> `/productos` + rutas `/productos/:id/variantes` autorizadas; Categorias -> `/categorias`; marcas/modelos -> `/marcas` y `/modelos`; Impuestos -> `/impuestos`. Cada superficie conserva permisos propios y servicio ERP existente. Este alias solo entra por el catalogo de Productos, no concede acceso automatico a otras rutas.
- A0: diff de `app.routes.ts`, contrato D de PLAN 1028, DAG validado y readback en Sheets; compatibilidad con funciones preexistentes sin migraciones ni nuevas DTO.
- A1/A2/A3: pruebas Angular/Chromium de anónimo -> login, tenant A, tenant B, revocación y falta de permiso. HTTP y JWT de estas pruebas son *sintéticos*, no sustituyen integración firmada ni MySQL real. No se declara cobertura de todo M04 ni etapa .06.
- A4: esperar CI dirigida GitHub-hosted exact SHA con typecheck, build, Chromium, Scope Lock y V5/procedencia pertinentes COMPLETED/SUCCESS; QUEUED, SKIPPED, CANCELLED, FAILED e IN_PROGRESS no son PASS.
- Rollback: `git revert` normal del commit si se detecta regresión; preservar historia, BITACORA y rutas existentes.
- A5 independiente después de LISTO por otra Task Javier distinta del autor; no hay autorizacion QA/main/PROD ni pago real.
