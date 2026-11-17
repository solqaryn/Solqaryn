# C15-04.M05.04 - Auditoria UX global R2, incremento 1 (NO COMPLETO)

## A0: alcance y cambios
Se implementa un incremento REAL de aceptacion Playwright/Chromium en localhost Angular: login publico, panel privado informativo de cinco superficies (landing/portal/ERP/builder/global) y guard negativo ERP. Cero cambios runtime, JWT, RBAC, EF/MySQL, secretos o produccion. Prueba en cuatro anchos, labels, headings, Tab real, foco visible, contraste calculado de un titulo, 403 fail-closed y cero HTTP mutaciones. La prueba ERP corre en un contexto browser aislado. La sesion esta SIMULADA con intercept HTTP, NO demuestra servidor.

## Calidad, limites y continuidad
A1-A3: cinco pruebas nuevas, pendientes de CI causal exact-SHA. A2: 403/guard simulado, no autorizacion backend real. A3: Chromium localhost real, NO DB ni proveedor externo. A4: no declarar PASS antes de Scope Lock, V5, Angular y pruebas dirigidas completed/success y readback institucional.
Pendiente obligatorio para LISTO: inventario COMPLETO de rutas y pantallas, incluso lazy y detalle/edicion, cobertura de landing, portal, ERP, builder y consola global con roles/tenant A/B autorizados, teclado, foco, contraste, responsive, formularios, WCAG aplicable, rendimiento medido o NO_MEDIDO, P0/P1 corregidos. Los cinco dominios enumerados en UI NO equivalen a cobertura de todas sus pantallas. Reportar rutas inaccesibles como SIN_VERIFICAR; sin JWT firmado ni MySQL reales.
EN_PROGRESO reanudable por DEV02; A5 requiere otra Task Javier distinta, no autoprobacion. Git revert normal para rollback. Solo dev, sin QA/main/PROD.

## A0.1 Inventario estático rastreable de rutas (NO equivale a prueba de interfaz)

Fuente: `app.routes.ts` y ocho arrays de rutas importados recursivamente desde la raíz (9 archivos únicos). El recuento es de declaraciones `path`, no de pantallas navegables únicas ni de URLs probadas, y puede incluir `''`, `**`, patrones repetidos y rutas que requieran roles, datos o sesiones no disponibles. No confundir con auditoría UX global completada.

- Declaraciones de ruta extraídas: **160**; rutas parametrizadas: **47**; fuentes: **9**.
- Cobertura Chromium causal de este incremento: **cinco escenarios**, concentrados en login, la vista de auditoría privada y denegación de dashboard; no equivale a 160 pantallas probadas ni a WCAG global.
- Rutas que requieran parámetros `:id`, credenciales empresariales, permisos, tenant A/B, API, datos existentes o configuración dinámica permanecen `SIN_VERIFICAR` hasta prueba por ruta real y evidencia de autorización. No inventar porcentajes de cobertura.

| Fuente de rutas | Declaraciones `path` | Parametrizadas | Blob exacto en dev |
| --- | ---: | ---: | --- |
| `frontend/src/app/app.routes.ts` | 109 | 24 | `f4c64f4e538de54af999a7cdecdc4b4e08648b8c` |
| `frontend/src/app/features/inventario/ajustes.routes.ts` | 11 | 5 | `e4f356e5eeffd30621f5a29e5b447ae4e9c19479` |
| `frontend/src/app/features/compras/facturas-proveedor/facturas-proveedor.routes.ts` | 14 | 7 | `614c47f0778e50dcac183d5051086b965b61f244` |
| `frontend/src/app/features/pedidos-venta/pedidos-venta.routes.ts` | 12 | 5 | `996eb747aa1336bb43c1cc7526f7985df4f0b1e2` |
| `frontend/src/app/features/inventario/transferencias.routes.ts` | 4 | 2 | `54aaacfe3e84890a7fd25fcb48046275c3293a6b` |
| `frontend/src/app/features/inventario/conteos.routes.ts` | 4 | 2 | `41764e42ea96952e41ce2eb8a163588a1277d568` |
| `frontend/src/app/features/inventario/reservas.routes.ts` | 4 | 2 | `29e28c754da4260648de33f13f49353b6898c016` |
| `frontend/src/app/features/finanzas/cuentas-por-cobrar/cuentas-por-cobrar.routes.ts` | 1 | 0 | `bad3da7fe9b3c034eab42a47e91aadc14ddc9a56` |
| `frontend/src/app/features/finanzas/cuentas-por-pagar/cuentas-por-pagar.routes.ts` | 1 | 0 | `5611e80c3ccd90f3112569fe75a28d76d10093d6` |

### Matriz de patrones declarados (no verificados visualmente)

**frontend/src/app/app.routes.ts**

`(vacía)` · `tiendas/:storeKey` · `tienda/productos` · `tienda/ofertas` · `tienda/producto/:slug` · `tienda/categorias` · `tienda/categoria/:slug` · `tienda/carrito` · `tienda/checkout` · `tienda/cuenta` · `tienda/pedido/:id` · `tienda` · `login` · `extensiones-sectoriales` · `plataforma/cobertura-v5` · `plataforma/flujo-v5` · `plataforma/auditoria-operativa-v5` · `plataforma/continuidad-operativa-v5` · `plataforma/mysql-cutover-readonly-v5` · `plataforma/placement-v5` · `plataforma/consola-global-v5` · `plataforma/auditoria-tenancy-v5` · `plataforma/gobierno-cambios-v5` · `plataforma/respuesta-incidentes-v5` · `plataforma/revision-amenazas-v5` · `plataforma/experiencia-visual-v5` · `plataforma/trazabilidad-cobertura-v5` · `plataforma/observabilidad-v5` · `plataforma/inventario-rutas-v19` · `plataforma/auditoria-ux-v5` · `plataforma` · `dashboard` · `productos` · `productos/nuevo` · `productos/:id/editar` · `productos/:id/variantes` · `productos/:id` · `categorias` · `categorias/nueva` · `categorias/:id/editar` · `sucursales` · `sucursales/nueva` · `sucursales/:id/editar` · `colores` · `tallas` · `marcas` · `modelos` · `metodos-pago` · `proveedores` · `proveedores/nuevo` · `proveedores/:id/editar` · `clientes` · `clientes/nuevo` · `clientes/:id/editar` · `tipo-clientes` · `tipo-clientes/nuevo` · `tipo-clientes/:id/editar` · `usuarios` · `usuarios/:id/editar` · `usuarios/:id` · `roles` · `roles/nuevo` · `roles/:id/editar` · `permisos` · `descuentos` · `descuentos/nuevo` · `descuentos/:id/editar` · `impuestos` · `impuestos/nuevo` · `impuestos/:id/editar` · `costos-envio` · `cargas-masivas` · `auditoria` · `solicitudes-compra` · `ordenes-compra` · `ordenes-compra/nueva` · `ordenes-compra/:id/editar` · `compras` · `compras/nueva` · `compras/:id/editar` · `compras/:id` · `ventas` · `ventas/nueva` · `ventas/:id/editar` · `ventas/:id` · `facturas/:id/pagos` · `facturas/:id` · `cuentas-por-cobrar` · `asientos-contables` · `finanzas` · `estados-financieros` · `centro-reportes` · `financieros` · `administrativos` · `inventario/valorizacion` · `inventario/kardex` · `inventario/stock-health` · `inventario/reconciliacion` · `ventas` · `compras` · `rentabilidad` · `plan-cuentas` · `cuentas-bancarias` · `inventario/movimientos` · `inventario/costeo` · `configuracion` · `periodos-contables` · `perfil` · `**`

**frontend/src/app/features/inventario/ajustes.routes.ts**

`inventario/existencias` · `inventario/existencias/nueva` · `inventario/existencias/:id/editar` · `inventario/trazabilidad/:varianteId` · `inventario/ajustes` · `inventario/ajustes/nuevo` · `inventario/ajustes/:id/editar` · `inventario/ajustes/:id` · `recepciones-compra/nueva` · `recepciones-compra/:id` · `recepciones-compra`

**frontend/src/app/features/compras/facturas-proveedor/facturas-proveedor.routes.ts**

`compras/:id/three-way-match` · `facturas-proveedor/nueva` · `facturas-proveedor/:id/editar` · `facturas-proveedor/:id` · `facturas-proveedor` · `devoluciones-proveedor/nueva` · `devoluciones-proveedor/:id/editar` · `devoluciones-proveedor/:id` · `devoluciones-proveedor` · `notas-credito-proveedor/nueva` · `notas-credito-proveedor/:id/editar` · `notas-credito-proveedor/:id` · `notas-credito-proveedor` · `evaluaciones-proveedor`

**frontend/src/app/features/pedidos-venta/pedidos-venta.routes.ts**

`pedidos-venta` · `pedidos-venta/nuevo` · `pedidos-venta/:id/editar` · `pedidos-venta/:pedidoVentaId/preparacion` · `preparaciones-pedido-venta` · `pedidos-venta/:id` · `devoluciones-clientes` · `devoluciones-clientes/nueva` · `devoluciones-clientes/:id` · `notas-credito-cliente` · `notas-credito-cliente/nueva` · `notas-credito-cliente/:id`

**frontend/src/app/features/inventario/transferencias.routes.ts**

`inventario/transferencias` · `inventario/transferencias/nueva` · `inventario/transferencias/:id/editar` · `inventario/transferencias/:id`

**frontend/src/app/features/inventario/conteos.routes.ts**

`inventario/conteos` · `inventario/conteos/nuevo` · `inventario/conteos/:id/editar` · `inventario/conteos/:id`

**frontend/src/app/features/inventario/reservas.routes.ts**

`inventario/reservas` · `inventario/reservas/nueva` · `inventario/reservas/:id/editar` · `inventario/reservas/:id`

**frontend/src/app/features/finanzas/cuentas-por-cobrar/cuentas-por-cobrar.routes.ts**

`cuentas-por-cobrar`

**frontend/src/app/features/finanzas/cuentas-por-pagar/cuentas-por-pagar.routes.ts**

`cuentas-por-pagar`

Acción posterior: ejecutar navegación por rol/caso, guard, viewport, teclado, foco, contraste, error y efectos secundarios para cada ruta realmente accesible; distinguir explícitamente accesibilidad no comprobable y fuentes que cambien después de los blobs listados. En este incremento ninguna ruta parametrizada se certifica por el solo hecho de aparecer aquí.

## A0.2 Incremento de matriz de rutas privadas (pendiente de CI)
Prueba nueva de 20 rutas exactas del router y arrays importados: **40 navegaciones negativas** sin credenciales a 375/1280 px, con redireccion a login, formulario visible, sin desbordamiento horizontal ni mutaciones HTTP. Adicionalmente, mostrar/ocultar contrasena sin envio. **7 casos Chromium** totales esperados en CI sobre SHA exacto; no acreditar PASS antes de consultar logs.

Rutas de la muestra: /plataforma/auditoria-ux-v5 · /plataforma/inventario-rutas-v19 · /plataforma/mensajeria-multicanal-v19 · /plataforma/observabilidad-v5 · /plataforma/consola-global-v5 · /dashboard · /productos · /productos/1/editar · /clientes/nuevo · /usuarios/1 · /roles/1/editar · /inventario/ajustes/1 · /inventario/transferencias/1/editar · /inventario/conteos/1 · /compras/1/three-way-match · /facturas-proveedor/1/editar · /pedidos-venta/1/editar · /cuentas-por-cobrar · /cuentas-por-pagar · /configuracion.

**Limites:** la redireccion anonima Angular no prueba JWT firmado, roles autorizados, revocacion, aislamiento tenant A/B, MySQL, acceso a contenido autenticado ni formularios privados. Esta muestra no verifica las 160 declaraciones de rutas, WCAG global ni Web Vitals. A0-A4 globales permanecen incompletas, estado EN_PROGRESO de DEV02, candado V17 y A5 independiente. Rollback por git revert ordinario.

## A0.3 Validaciones del login (CI pendiente, incremento R2 parcial)
Se agregan dos casos Chromium de UX para el formulario de login: fronteras de longitud de usuario 50 y contrasena 128 caracteres, bloqueo de envio invalido, y recorrido real de Tab desde el inicio con activacion de mostrar contrasena por Espacio y estado aria-pressed. No ejecutan submit ni invocan un proveedor real. El workflow exige nueve pruebas en navegador; no declarar PASS sin logs de CI del SHA exacto. Las credenciales son sinteticas y no se envian. No certifica pantallas privadas con sesion autorizada, servidor JWT/RBAC, tenant A/B, MySQL, WCAG global ni Web Vitals. Mantener EN_PROGRESO propia y lock V17 hasta A0-A4 completos; A5 distinto.

## A0.4 Rutas publicas storefront (CI pendiente, incremento R1 parcial)
Tres rutas publicas reales de Angular (inicio, productos y categorias) se prueban en Chromium a 375 y 1280px, con seis navegaciones, main visible, enlace de salto que apunta al main correcto, exactamente un titulo H1 y control de overflow horizontal. Se comprueba ausencia de metodos HTTP mutantes, sin realizar pedidos ni cobros. No se suplanta el backend, no se inventa disponibilidad de productos ni funcionamiento de checkout. El total esperado del workflow es de 10 casos, sujeto a CI exact-SHA antes de PASS. A0-A4 de la auditoria UX global siguen incompletos; tenant A/B, auth/JWT real, rutas parametrizadas restantes, WCAG y Web Vitals SIN_VERIFICAR.

## A0.5 Teclado de tienda y menu movil (incremento causal parcial)
Dos escenarios Playwright: Tab real inicial y Enter con fragmento de ancla en /tienda, /tienda/productos y /tienda/categorias; modal movil a 375 px con foco inicial, Escape y retorno al disparador, aria-expanded y cero mutaciones HTTP. Sin .focus() fabricado. Se esperan 12 pruebas Chromium, solo contar PASS tras check-runs/jobs/logs exact-SHA. Las 160 declaraciones no son pantallas certificadas. Sin JWT real/tenant A-B/roles privados, WCAG 2.2 AA global o Web Vitals; EN_PROGRESO con candado V17 y A5 independiente.

## A0.6 QA_DEFECT_FOUND P2 y correccion causal del salto de contenido
GitHub-hosted RUN 44112044539 JOB 134139969460 sobre SHA 39b2e65d63f5d7e8f376ff53f9e9c006ab033768: 11/12 Playwright PASS, 1 FAIL. Tras Tab+Enter el enlace de salto con href solo fragmento resolvia desde HTML base a /#contenido-principal en vez de permanecer en /tienda; desaparecia main objetivo. Es un defecto UX real P2 de ruta y accesibilidad, NO un problema aislado de fixture. Reparacion minima: usar Angular RouterLink relativo con fragment para conservar /tienda, /tienda/productos y /tienda/categorias. Ajustar expectativas de ambas pruebas y comprobar location.pathname en regresion. CI exact-SHA del nuevo commit es obligatoria, no declarar PASS hasta ver log y check-runs. Sin cambios de backend/tenant/MySQL ni certificacion global WCAG/Web Vitals; mantener EN_PROGRESO y candado V17.

## A0.7 Reparacion de validador estatico fase 2 (sin desactivar seguridad)
CI correctivo SHA 71a32b6f4e28900e3753bc4f680ac25a8eabef4f RUN 44112967642 JOB 134143250818 fallo en lint fase 2: el validador exigia exactamente [href]="destinoSaltar", contrato que causaba la salida real hacia / al pulsar Enter en tienda bajo base href. El validador se actualiza para EXIGIR de forma mas fuerte [routerLink]="[]" Y [fragment]="destinoSaltar.slice(1)". No se elimina control: preserva destino real y deteccion de regresiones. La regresion Chromium 12/12 en SHA descendiente continua obligatoria, sin PASS prematuro ni cierre global UX.

## A0.8 Regresiones heredadas fase 2 actualizadas sin reducir cobertura
El SHA a93d3ab4e90c9cf463e052dd027c40f4a9f7737a provocó fallos reales de contratos Playwright heredados en fase2, propagados a CI fase3/4/7/8/9/12: dos asserts comparaban href con solo #contenido-categorias o #contenido-categoria. En Angular RouterLink corregido el href real esperado incorpora pathname+fragmento. Se actualizan exactamente esos dos asserts a URLs canonicas completas. Las pruebas siguen verificando el destino y no se omiten, silencian ni relajan. Se fuerza rerun de la prueba incremental C15 al cambiar su spec. Aún se requiere PASS GitHub-hosted exact-SHA de todas las regresiones pertinentes antes de A4 y estado global R2 permanece EN_PROGRESO.

## A0.9 Reparacion de regresiones storefront fases 3 y 7 (contratos estrictos)
RUN 44114180606 fase3 fue FAIL de un assert de href previo (#catalogo-productos), cuando el href canonical correcto es /tienda/productos#catalogo-productos; otras siete pruebas del job aprobaron. Se corrige solo ese assert, preservando la navegacion, paginacion y tests reales. Fase7 conserva la prueba de skip-link en home y ahora espera URL completa /tienda#contenido-principal. No se ignoran tests ni se relajan permisos o seguridad. Exigir CI exact-SHA de las suites afectadas y doce Chromium C15 antes de declarar regresiones aprobadas; global UX sigue EN_PROGRESO sin A5.

## A0.10 Reparacion de aislamiento de foco real entre rutas
RUN 44114180266 JOB 134147579140 sobre SHA b7c50a35: 11/12 Chromium PASS, test Tab+Enter FAIL porque la tercera navegacion en la misma pestana tenia foco inicial residual y el primer Tab no llego al skip-link. Sin focus() sintetico: cada ruta usa una pestana Playwright nueva desde el mismo contexto y Tab real desde carga, se cierra la pestana en finally. La regresion sigue exigiendo foco skip-link, Enter, href ruta+fragmento, pathname y target visible. Pendiente CI exact-SHA, no certificar global UX.

## A0.11 Expansión de rutas públicas reales (CI PENDIENTE, no auditoría global)
Se agrega un escenario Playwright para /tienda/ofertas, /tienda/carrito y /tienda/cuenta con pestaña nueva por ruta y viewport 375/1280 (seis navegaciones adicionales). Se verifica título H1 propio, ausencia de overflow horizontal y ausencia de mutaciones HTTP. En cuenta anónima se prueba el estado inicial del tab login, la inhibición del submit vacío, cambio de tab a registro y la inhibición del submit vacío. No se realizan compras, registro, login ni cambios de datos. Total esperado: **13 casos Chromium**; NO contabilizar PASS sin check-runs, job y logs exact-SHA tras publicar. Continuidad R2: rutas restantes, roles/tenant A/B reales, WCAG y WebVitals sin certificar; EN_PROGRESO DEV02, mismo candado V17, A5 exclusivamente de otra Task.

## A0.12 Corrección real ARIA Tabs cuenta pública — R2, CI pendiente
La cuenta pública tenía role=tab sin flechas/Home/End, roving tabindex ni relaciones explícitas tab-panel. Se corrige el patrón en Angular para alternar panel con teclado real, conservando foco visible y el estado del formulario sin peticiones de escritura. Nueva prueba Chromium a 375 y 1280 comprueba Tab real, ArrowRight/Left, End/Home, aria-selected/tabindex, panel rotulado, ausencia de overflow y cero POST. No implica JWT real, RBAC, tenant A/B, WCAG global ni WebVitals. Exigir 14/14 Chromium en GitHub-hosted, Scope Lock, V5 y frontend exact-SHA antes de dar PASS. El scope continúa EN_PROGRESO DEV02 con lock V17 2000002297 y A5 por otra Task. Rollback: commit revert normal con HEAD verificado.

## A0.13 QA P2: enlace de salto de cuenta con destino ausente (CI pendiente)
La página pública /tienda/cuenta heredaba en el header el destino #catalogo, pero el template no contenía dicho id. Se declara explícitamente destinoSaltar="#cuenta-principal" y etiquetaSaltar de cuenta, añadiendo id y tabindex=-1 al main. Playwright/Chromium comprueba Tab y Enter REALES desde una pestaña nueva para 375/1280 px: href con pathname de cuenta, hash #cuenta-principal, target visible, sin overflow ni mutaciones HTTP. P2 de accesibilidad corregido en código, no reportar PASS hasta verificar logs y check-runs exact SHA. Es una regresión puntual, NO WCAG total ni JWT/RBAC/tenant A/B real; C15-04.M05.04 sigue EN_PROGRESO con lock 2000002297, A5 sólo por otra Task. Rollback: git revert normal con HEAD verificado.

## A0.14 - rutas publicas ofertas/carrito: destino de teclado (incremento R2 parcial, CI pendiente)
Se agrega tabindex=-1 al main catalogo-productos de la plantilla comun de productos/ofertas para convertirlo en destino programaticamente enfocable. Un nuevo caso Playwright utiliza Tab y Enter reales en pestanas nuevas, a 375 y 1280 px, para /tienda/ofertas y /tienda/carrito: verifica href/hash con pathname conservado, main visible con tabindex, ausencia de overflow y ninguna solicitud HTTP mutadora. El workflow espera 16 casos Chromium acumulados. NO afirmar PASS hasta comprobar logs y check-runs del SHA exacto. Evidencia aislada de teclado de rutas publicas; no certifica WCAG global, JWT/RBAC, tenant A/B ni Web Vitals. C15-04.M05.04 continua EN_PROGRESO DEV02 con candado V17 2000002297 y A5 por otra Task. Rollback git revert ordinario.

## A0.15 - muestra de tiempos reales de navegador y Web Vitals (R2 parcial, CI pendiente)
Se agrega un escenario Chromium para cuatro navegaciones de muestra: /tienda y /login a 375 y 1280 px. Usa Navigation Timing (TTFB), Paint Timing (FCP), PerformanceObserver buffered (LCP) y desplazamientos de layout (CLS), imprimiendo valores numericos reales o NO_MEDIDO donde el navegador no emita entrada. La prueba no fija un SLA artificial, pero rechaza TTFB negativo/no finito. Exigir 17/17 casos y logs exact-SHA para acreditar la muestra; todavia faltan umbrales y cobertura Web Vitals integral de rutas/roles, WCAG y tenant A/B. C15-04.M05.04 sigue EN_PROGRESO DEV02, lock V17 2000002297, A5 distinta Task. Rollback git revert normal.

## A0.16 - reparacion causal P2 de selector de test y censo de rutas
GitHub run 44147705676, job 134267234348, sobre 2f0b1710585e723516826de40b9486c83690df0c termino FAILURE: 16 PASS y 1 FAIL. El test Web Vitals buscaba h1 general despues de navegar a /login, pero el titulo real del formulario es h2#login-title. No es un fallo de producto ni de rendimiento. Se cambia la asercion de login al nodo semantico visible #login-title manteniendo controles de tienda, TTFB, FCP/LCP/CLS y noOverflow. CI de reparacion exact-SHA requerida; no aceptar checks SKIPPED.

Recuento estatico revalidado en HEAD dev: 161 declaraciones path en nueve archivos, 47 parametrizadas. app.routes.ts contiene 110, blob f55a8b707770118f39e9bcda8ff794361a52ba5c (anterior 109 y blob f4c64f4e538de54af999a7cdecdc4b4e08648b8c). Los otros ocho archivos conservan 11/14/12/4/4/4/1/1. La tabla A0.1 es snapshot historico; el nuevo recuento de declaraciones NO certifica 161 pantallas. Alcance R2 roles/tenants A-B, rutas dinamicas, WCAG/WebVitals sigue EN_PROGRESO con owner DEV02 y lock V17 2000002297. A5 es de otra Task.

## A0.17 - landmarks y teclado real en seis rutas y cuatro viewports (CI PENDIENTE)
Se amplian las regresiones Playwright con 24 navegaciones reales en /tienda, /tienda/productos, /tienda/categorias, /tienda/ofertas, /tienda/carrito y /tienda/cuenta a 320/375/768/1280 px. Cada ruta debe exponer su landmark main visible con destino de skip-link canonico, un H1, cero overflow horizontal y cero metodos HTTP mutantes. En 320/768 tambien exige Tab y Enter reales desde pestana nueva, conservacion de pathname y destino de fragmento. El workflow exige 18 tests acumulados y CI causal del SHA exacto; antes del resultado se declara NO_ACREDITADO, no PASS. Sin JWT real, tenant A/B, roles privados, auditoria de todas las pantallas, WCAG global ni Web Vitals global. C15-04.M05.04 sigue EN_PROGRESO con owner DEV02 y candado V17 2000002297; A5 exclusivamente otra Task Javier. Reversion: git revert normal sobre el commit validado.

## A0.18 - formularios de cuenta cliente: casos negativos y frontera (CI PENDIENTE)
Un nuevo caso Playwright/Chromium comprueba en dos anchos reales (375/1280) la invalidez y bloqueo del envio de login con correo malformado, contrasena bajo longitud minima y estado de habilitacion local al cumplir los requisitos; verifica etiquetas autocomplete. En el registro, el nombre ausente y clave de nueve caracteres bloquean el boton, y una clave de diez con los demas campos validos habilita el formulario sin enviarlo. Comprueba la ayuda aria-describedby, no overflow y cero POST/PUT/PATCH/DELETE. No ejecuta autenticacion o registro ni certifica JWT, RBAC, empresa A/B, MySQL o WCAG global. El workflow requiere diecinueve tests; antes de PASS exact-SHA en GitHub-hosted no certificar. EN_PROGRESO DEV02 con candado V17 2000002297, A5 exclusivamente otra Task. Rollback git revert normal.
