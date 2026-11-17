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
