# C14-01.M04.02 — Estados reales de la vista privada de QA (V19)

## A0 — Alcance y arquitectura
Unidad C14-01.M04.02, R1 de presentación Angular 22.2.1. La ruta privada ya existente /plataforma/qa-global-v19 preserva el guard y el grant de plataforma. Carga de manera asincrona un inventario LOCAL sin llamadas HTTP a proveedores y muestra estados de carga, vacio, fallo opaco, catalogo listo y permiso denegado. El estado *listo* solo significa que se renderizo el inventario local: los cinco controles siguen NO_VERIFICADO. No es una fuente de CI ni permiso de tenant.

## A1 — Casos positivos, negativos, limites y error
Vitest dirigidos: sin grant no ejecuta loader; carga pendiente aria-busy; vacio; rechazo opaco sin mostrar el mensaje ni secreto; catalogo positivo sin PASS; IDs duplicados error; respuesta tardia descartada tras revocacion de grant. Regresion unitaria existente M04.01 y Playwright real con denegacion anonima/403, cinco categorias, sin POST/PUT, foco/teclado y cuatro viewports via suite M04.01.

## A2 — Seguridad
La autorizacion de backend en /plataforma/sesion y demas endpoints sigue siendo obligatoria. El frontend oculta inventario si el grant se revoca y no expone texto de errores ni datos de otras empresas. No se cambia JWT, RBAC, ni permisos de ERP. Suite sintetica NO certifica JWT firmado ni tenant A/B real.

## A3 — Integracion
Angular dinámico y Chromium sobre localhost efimero de GitHub Actions. Carga de modulo JavaScript real y errores capturados, no API de GitHub/HTTP ni MySQL. No atribuir SUCCESS de CI por un estado UI, no usar secretos productivos.

## A4/A5 y rollback
Git commit de dev fast-forward y control de Scope Lock/V5/seguridad exact SHA; job dirigido npm test x2, lint/build y 5 Chromium reales. Si QUEUED/IN_PROGRESS o FAIL, permanece VALIDANDO, con SHA+RUN+owner y A5 pendiente. Revisar COLA, DAG, PLAN y BITACORA en cada transicion. Revisor A5 de otra Task Javier, nunca autor. Rollback mediante git revert normal de la unidad, sin reescribir historia, manteniendo seguridad y pruebas.

## CI_REPAIR — regresion asincrona de M04.01
La primera CI de M04.02 en el SHA b40cbc8d encontro un fallo causal: el test heredado M04.01 comprobaba las cinco tarjetas antes de que terminara el import dinamico tras el grant (0 de 5). Las 7 pruebas nuevas de estado fueron PASS; la regresion heredada 1/2 FAILED y el workflow se detuvo antes de Chromium. Reparacion minima: esperar acotadamente con vi.waitFor el estado real listo y cinco tarjetas, manteniendo todas las aserciones originales. Reejecutar GitHub-hosted exact-SHA; no declarar pruebas browser ni A4 aprobadas hasta confirmacion.
