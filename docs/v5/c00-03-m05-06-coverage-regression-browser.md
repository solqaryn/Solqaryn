# SOLQARYN V5 — C00-03.M05.06: regresion, rendimiento y accesibilidad

Resultado exigido: ejecutar regresion, rendimiento y accesibilidad proporcionales a la matriz V5. Se amplian los tests Playwright Chromium existentes de la ruta real /plataforma/cobertura-v5: caso positivo con carga de la cabecera visible antes de 25 s y DOMContentLoaded <25 s, accesibilidad de tabla y region de scroll por teclado, viewports 390/1280 px sin desbordamiento exterior y ausencia de llamadas a PLAN/COLA/BITACORA/claims. Caso negativo: sin token, redireccion login y tabla privada ausente.

Clasificacion: require_dotnet=NO; require_node_angular=SI; require_mysql=NO; require_docker=NO; require_e2e=SI (Chromium real); require_security=SI (denegacion UI y no acceso operativo); require_scope_lock=SI. CI requerida del SHA publicado: V5 - Chromium real, rutas, denegaciones y responsive (incluye npm lint, build, Angular local y Playwright), Scope Lock y verificador V5 CHANGELOG. Para esta modificacion de tests/UI no se exige dotnet o MySQL sin gate de rama causal.

LIMITES: la sesion de la prueba esta simulada, no verifica JWT/RBAC live, backend MySQL ni permisos contra servidor productivo. Las metricas son presupuesto de navegador dev en runner GitHub, no un SLA de produccion. Sin cambio a Angular productivo, API ni datos. Rollback: git revert ordinario sin force-push.
