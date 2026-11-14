# P0-006.A — Inventario de rutas administrativas y navegación (DEV)

**Proyecto:** SOLQARYN; `PROJECT_SCOPE_LOCK=STRICT`. **Tarea:** P0-006.A, prioridad P0, Plan Comercial Inicial (`STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES`). **Ejecutor:** `SECONDARY_REVIEW_03`, `ALEX_CHATGPT`, Task ID `6adcc98a80a48191b71a5156624c21df`, GitHub `morales35alex`. **Base de lectura inmutable:** [dev@`6de952a43dda3449a9c6756d3c567374d686c5df`](https://github.com/solqaryn/Solqaryn/commit/6de952a43dda3449a9c6756d3c567374d686c5df), 25-10-2026 (America/Tegucigalpa). **Rama de trabajo exclusiva:** `work/solqaryn-p0-006-a`. **Ambiente:** DEV. Sin operaciones de QA/main/PROD, infra, datos ni secretos.

## Método reproducible y límites

Lectura autenticada de `frontend/src/app/app.routes.ts`, `frontend/src/app/shared/navigation/app-navigation-menu.component.ts`, `frontend/src/app/app.config.ts` y diez módulos `*.routes.ts` conectados por `...ROUTES` y `provideRouter`. Extracción estática de declaraciones `path`, `canActivate`, pares `data.modulo/accion`, `loadComponent` y enlaces `routerLink`; la ruta de nueve hijas de `centro-reportes` incorpora prefijo y autenticación efectiva del padre. **Los 92 `path` son exclusivamente declaraciones textuales en `app.routes.ts`, no el número total de URL del producto.** El catálogo se debe actualizar tras cambios de HEAD y no sustituye E2E, autorización backend ni verificación de permisos de usuario.

Comprobaciones dirigidas en las fuentes del SHA base: **92/92 declaraciones raíz; 90 lazies y 2 redirects; 39/39 enlaces del menú; 39/39 destinos resueltos contra raíz y arreglos conectados**. Los módulos contienen **60 declaraciones adicionales** (no representan igual número de registros únicos). El caso negativo de URL privada no se considera satisfecho sólo por mostrar el menú: `authGuard` y `permisoGuard` se verifican en las rutas, mientras RBAC real sigue a cargo del backend.

## Catálogo de las 92 declaraciones de `app.routes.ts`

La columna “guard efectivo” añade `authGuard` del contenedor únicamente a las hijas de `centro-reportes`. “—” identifica rutas públicas, redirecciones o metadatos ausentes y **no certifica** ausencia de validación dentro del componente/API. `plataformaGuard` se conserva separado.

| # | Ruta resultante | `canActivate` local | Guard efectivo de ruta | Capacidad `modulo:accion` | Resolución |
|---:|---|---|---|---|---|
| 1 | `/` | `—` | `—` | `—` | redirect |
| 2 | `/tiendas/:storeKey` | `—` | `—` | `—` | loadComponent |
| 3 | `/tienda/productos` | `—` | `—` | `—` | loadComponent |
| 4 | `/tienda/ofertas` | `—` | `—` | `—` | loadComponent |
| 5 | `/tienda/producto/:slug` | `—` | `—` | `—` | loadComponent |
| 6 | `/tienda/categorias` | `—` | `—` | `—` | loadComponent |
| 7 | `/tienda/categoria/:slug` | `—` | `—` | `—` | loadComponent |
| 8 | `/tienda/carrito` | `—` | `—` | `—` | loadComponent |
| 9 | `/tienda/checkout` | `—` | `—` | `—` | loadComponent |
| 10 | `/tienda/cuenta` | `—` | `—` | `—` | loadComponent |
| 11 | `/tienda/pedido/:id` | `—` | `—` | `—` | loadComponent |
| 12 | `/tienda` | `—` | `—` | `—` | loadComponent |
| 13 | `/login` | `—` | `—` | `—` | loadComponent |
| 14 | `/plataforma` | `plataforma` | `plataforma` | `—` | loadComponent |
| 15 | `/dashboard` | `auth+permiso` | `auth+permiso` | `Dashboard:Ver` | loadComponent |
| 16 | `/productos` | `auth+permiso` | `auth+permiso` | `Productos:Ver` | loadComponent |
| 17 | `/productos/nuevo` | `auth+permiso` | `auth+permiso` | `Productos:Crear` | loadComponent |
| 18 | `/productos/:id/editar` | `auth+permiso` | `auth+permiso` | `Productos:Editar` | loadComponent |
| 19 | `/productos/:id/variantes` | `auth+permiso` | `auth+permiso` | `Productos:Editar` | loadComponent |
| 20 | `/productos/:id` | `auth+permiso` | `auth+permiso` | `Productos:Ver` | loadComponent |
| 21 | `/categorias` | `auth+permiso` | `auth+permiso` | `Categorias:Ver` | loadComponent |
| 22 | `/categorias/nueva` | `auth+permiso` | `auth+permiso` | `Categorias:Crear` | loadComponent |
| 23 | `/categorias/:id/editar` | `auth+permiso` | `auth+permiso` | `Categorias:Editar` | loadComponent |
| 24 | `/sucursales` | `auth+permiso` | `auth+permiso` | `Sucursales:Ver` | loadComponent |
| 25 | `/sucursales/nueva` | `auth+permiso` | `auth+permiso` | `Sucursales:Crear` | loadComponent |
| 26 | `/sucursales/:id/editar` | `auth+permiso` | `auth+permiso` | `Sucursales:Editar` | loadComponent |
| 27 | `/colores` | `auth+permiso` | `auth+permiso` | `Colores:Ver` | loadComponent |
| 28 | `/tallas` | `auth+permiso` | `auth+permiso` | `Tallas:Ver` | loadComponent |
| 29 | `/marcas` | `auth+permiso` | `auth+permiso` | `Marcas:Ver` | loadComponent |
| 30 | `/modelos` | `auth+permiso` | `auth+permiso` | `Modelos:Ver` | loadComponent |
| 31 | `/metodos-pago` | `auth+permiso` | `auth+permiso` | `MetodosPago:Ver` | loadComponent |
| 32 | `/proveedores` | `auth+permiso` | `auth+permiso` | `Proveedores:Ver` | loadComponent |
| 33 | `/proveedores/nuevo` | `auth+permiso` | `auth+permiso` | `Proveedores:Crear` | loadComponent |
| 34 | `/proveedores/:id/editar` | `auth+permiso` | `auth+permiso` | `Proveedores:Editar` | loadComponent |
| 35 | `/clientes` | `auth+permiso` | `auth+permiso` | `Clientes:Ver` | loadComponent |
| 36 | `/clientes/nuevo` | `auth+permiso` | `auth+permiso` | `Clientes:Crear` | loadComponent |
| 37 | `/clientes/:id/editar` | `auth+permiso` | `auth+permiso` | `Clientes:Editar` | loadComponent |
| 38 | `/tipo-clientes` | `auth+permiso` | `auth+permiso` | `TiposClientes:Ver` | loadComponent |
| 39 | `/tipo-clientes/nuevo` | `auth+permiso` | `auth+permiso` | `TiposClientes:Crear` | loadComponent |
| 40 | `/tipo-clientes/:id/editar` | `auth+permiso` | `auth+permiso` | `TiposClientes:Editar` | loadComponent |
| 41 | `/usuarios` | `auth+permiso` | `auth+permiso` | `Usuarios:Ver` | loadComponent |
| 42 | `/usuarios/:id/editar` | `auth+permiso` | `auth+permiso` | `Usuarios:Editar` | loadComponent |
| 43 | `/usuarios/:id` | `auth+permiso` | `auth+permiso` | `Usuarios:Ver` | loadComponent |
| 44 | `/roles` | `auth+permiso` | `auth+permiso` | `Roles:Ver` | loadComponent |
| 45 | `/roles/nuevo` | `auth+permiso` | `auth+permiso` | `Roles:Crear` | loadComponent |
| 46 | `/roles/:id/editar` | `auth+permiso` | `auth+permiso` | `Roles:Editar` | loadComponent |
| 47 | `/permisos` | `auth+permiso` | `auth+permiso` | `Permisos:Administrar` | loadComponent |
| 48 | `/descuentos` | `auth+permiso` | `auth+permiso` | `Descuentos:Ver` | loadComponent |
| 49 | `/descuentos/nuevo` | `auth+permiso` | `auth+permiso` | `Descuentos:Crear` | loadComponent |
| 50 | `/descuentos/:id/editar` | `auth+permiso` | `auth+permiso` | `Descuentos:Editar` | loadComponent |
| 51 | `/impuestos` | `auth+permiso` | `auth+permiso` | `Impuestos:Ver` | loadComponent |
| 52 | `/impuestos/nuevo` | `auth+permiso` | `auth+permiso` | `Impuestos:Crear` | loadComponent |
| 53 | `/impuestos/:id/editar` | `auth+permiso` | `auth+permiso` | `Impuestos:Editar` | loadComponent |
| 54 | `/costos-envio` | `auth+permiso` | `auth+permiso` | `Facturacion:Administrar` | loadComponent |
| 55 | `/cargas-masivas` | `auth+permiso` | `auth+permiso` | `CargasMasivas:Ver` | loadComponent |
| 56 | `/auditoria` | `auth+permiso` | `auth+permiso` | `Auditoria:Ver` | loadComponent |
| 57 | `/solicitudes-compra` | `auth+permiso` | `auth+permiso` | `Compras:Ver` | loadComponent |
| 58 | `/ordenes-compra` | `auth+permiso` | `auth+permiso` | `Compras:Ver` | loadComponent |
| 59 | `/ordenes-compra/nueva` | `auth+permiso` | `auth+permiso` | `Compras:Crear` | loadComponent |
| 60 | `/ordenes-compra/:id/editar` | `auth+permiso` | `auth+permiso` | `Compras:Editar` | loadComponent |
| 61 | `/compras` | `auth+permiso` | `auth+permiso` | `Compras:Ver` | loadComponent |
| 62 | `/compras/nueva` | `auth+permiso` | `auth+permiso` | `Compras:Crear` | loadComponent |
| 63 | `/compras/:id/editar` | `auth+permiso` | `auth+permiso` | `Compras:Editar` | loadComponent |
| 64 | `/compras/:id` | `auth+permiso` | `auth+permiso` | `Compras:Ver` | loadComponent |
| 65 | `/ventas` | `auth+permiso` | `auth+permiso` | `Ventas:Ver` | loadComponent |
| 66 | `/ventas/nueva` | `auth+permiso` | `auth+permiso` | `Ventas:Crear` | loadComponent |
| 67 | `/ventas/:id/editar` | `auth+permiso` | `auth+permiso` | `Ventas:Editar` | loadComponent |
| 68 | `/ventas/:id` | `auth+permiso` | `auth+permiso` | `Ventas:Ver` | loadComponent |
| 69 | `/facturas/:id/pagos` | `auth+permiso` | `auth+permiso` | `Facturacion:Aplicar` | loadComponent |
| 70 | `/facturas/:id` | `auth+permiso` | `auth+permiso` | `Facturacion:Ver` | loadComponent |
| 71 | `/cuentas-por-cobrar` | `auth+permiso` | `auth+permiso` | `Facturacion:Ver` | loadComponent |
| 72 | `/asientos-contables` | `auth+permiso` | `auth+permiso` | `Finanzas:Ver` | loadComponent |
| 73 | `/finanzas` | `auth+permiso` | `auth+permiso` | `Finanzas:Ver` | loadComponent |
| 74 | `/estados-financieros` | `auth+permiso` | `auth+permiso` | `Finanzas:Ver` | loadComponent |
| 75 | `/centro-reportes` | `auth` | `auth` | `—` | loadComponent |
| 76 | `/centro-reportes/financieros` | `permiso` | `auth+permiso` | `Finanzas:Ver` | loadComponent |
| 77 | `/centro-reportes/administrativos` | `permiso` | `auth+permiso` | `ReportesAdministrativos:Ver` | loadComponent |
| 78 | `/centro-reportes/inventario/valorizacion` | `permiso` | `auth+permiso` | `Inventario:Ver` | loadComponent |
| 79 | `/centro-reportes/inventario/kardex` | `permiso` | `auth+permiso` | `MovimientosInventario:ConsultarHistorial` | loadComponent |
| 80 | `/centro-reportes/inventario/stock-health` | `permiso` | `auth+permiso` | `Inventario:Ver` | loadComponent |
| 81 | `/centro-reportes/inventario/reconciliacion` | `permiso` | `auth+permiso` | `Inventario:Ver` | loadComponent |
| 82 | `/centro-reportes/ventas` | `permiso` | `auth+permiso` | `Ventas:Ver` | loadComponent |
| 83 | `/centro-reportes/compras` | `permiso` | `auth+permiso` | `Compras:Ver` | loadComponent |
| 84 | `/centro-reportes/rentabilidad` | `permiso` | `auth+permiso` | `Ventas:Ver` | loadComponent |
| 85 | `/plan-cuentas` | `auth+permiso` | `auth+permiso` | `Finanzas:Ver` | loadComponent |
| 86 | `/cuentas-bancarias` | `auth+permiso` | `auth+permiso` | `Finanzas:Ver` | loadComponent |
| 87 | `/inventario/movimientos` | `auth+permiso` | `auth+permiso` | `MovimientosInventario:Ver` | loadComponent |
| 88 | `/inventario/costeo` | `auth+permiso` | `auth+permiso` | `MovimientosInventario:Ver` | loadComponent |
| 89 | `/configuracion` | `auth+permiso` | `auth+permiso` | `Configuracion:Ver` | loadComponent |
| 90 | `/periodos-contables` | `auth+permiso` | `auth+permiso` | `Configuracion:Ver` | loadComponent |
| 91 | `/perfil` | `auth` | `auth` | `—` | loadComponent |
| 92 | `/**` | `—` | `—` | `—` | redirect |

## Enlaces del menú: 39 referencias

Fuente `frontend/src/app/shared/navigation/app-navigation-menu.component.ts`. La interfaz condiciona grupos/enlaces por `permisosRuntime.puede` y `rutaHabilitada` (y una ruta administrativa requiere `esAdministrador`). Se reporta la capacidad de **la ruta destino**, no se infiere autorización backend de la condición visual. Los destinos dinámicos se compararon con los arreglos importados en el árbol de router.

| # | `routerLink` | Capacidad de ruta | Declaración de origen | Resuelto estáticamente |
|---:|---|---|---|---|
| 1 | `/dashboard` | `Dashboard:Ver` | raíz | SI |
| 2 | `/productos` | `Productos:Ver` | raíz | SI |
| 3 | `/categorias` | `Categorias:Ver` | raíz | SI |
| 4 | `/colores` | `Colores:Ver` | raíz | SI |
| 5 | `/tallas` | `Tallas:Ver` | raíz | SI |
| 6 | `/marcas` | `Marcas:Ver` | raíz | SI |
| 7 | `/modelos` | `Modelos:Ver` | raíz | SI |
| 8 | `/metodos-pago` | `MetodosPago:Ver` | raíz | SI |
| 9 | `/descuentos` | `Descuentos:Ver` | raíz | SI |
| 10 | `/impuestos` | `Impuestos:Ver` | raíz | SI |
| 11 | `/sucursales` | `Sucursales:Ver` | raíz | SI |
| 12 | `/almacenes` | `Almacenes:Ver` | almacenes.routes.ts | SI |
| 13 | `/ubicaciones-almacen` | `UbicacionesAlmacen:Ver` | ubicaciones-almacen.routes.ts | SI |
| 14 | `/proveedores` | `Proveedores:Ver` | raíz | SI |
| 15 | `/clientes` | `Clientes:Ver` | raíz | SI |
| 16 | `/cargas-masivas` | `CargasMasivas:Ver` | raíz | SI |
| 17 | `/solicitudes-compra` | `Compras:Ver` | raíz | SI |
| 18 | `/ordenes-compra` | `Compras:Ver` | raíz | SI |
| 19 | `/recepciones-compra` | `Compras:Ver` | ajustes.routes.ts | SI |
| 20 | `/devoluciones-proveedor` | `Compras:Ver` | facturas-proveedor.routes.ts | SI |
| 21 | `/compras` | `Compras:Ver` | raíz | SI |
| 22 | `/ventas` | `Ventas:Ver` | raíz | SI |
| 23 | `/pedidos-venta` | `Ventas:Ver` | pedidos-venta.routes.ts | SI |
| 24 | `/inventario/movimientos` | `MovimientosInventario:Ver` | raíz | SI |
| 25 | `/inventario/transferencias` | `MovimientosInventario:Ver` | transferencias.routes.ts | SI |
| 26 | `/inventario/conteos` | `MovimientosInventario:Ver` | conteos.routes.ts | SI |
| 27 | `/inventario/reservas` | `MovimientosInventario:Ver` | reservas.routes.ts | SI |
| 28 | `/inventario/costeo` | `MovimientosInventario:Ver` | raíz | SI |
| 29 | `/inventario/ajustes` | `Inventario:Ver` | ajustes.routes.ts | SI |
| 30 | `/finanzas` | `Finanzas:Ver` | raíz | SI |
| 31 | `/plan-cuentas` | `Finanzas:Ver` | raíz | SI |
| 32 | `/estados-financieros` | `Finanzas:Ver` | raíz | SI |
| 33 | `/centro-reportes` | `—` | raíz | SI |
| 34 | `/usuarios` | `Usuarios:Ver` | raíz | SI |
| 35 | `/roles` | `Roles:Ver` | raíz | SI |
| 36 | `/permisos` | `Permisos:Administrar` | raíz | SI |
| 37 | `/auditoria` | `Auditoria:Ver` | raíz | SI |
| 38 | `/configuracion` | `Configuracion:Ver` | raíz | SI |
| 39 | `/periodos-contables` | `Configuracion:Ver` | raíz | SI |

## Registro lazy adicional y precedencia

`app.config.ts` declara `provideRouter([...UBICACIONES_ALMACEN_ROUTES, ...ALMACENES_ROUTES, ...TRANSFERENCIAS_INVENTARIO_ROUTES, ...COTIZACIONES_ROUTES, ...CENTROS_COSTO_ROUTES, ...routes], withPreloading(...))`. La raíz a su vez expande `AJUSTES_INVENTARIO_ROUTES`, `FACTURAS_PROVEEDOR_ROUTES` y `PEDIDOS_VENTA_ROUTES`; AJUSTES expande cuatro grupos anidados. Ningún `loadComponent` debe interpretarse como prueba de carga E2E.

| Archivo/tabla | `path` declarados | Método de incorporación |
|---|---:|---|
| `app.routes.ts` | 92 | Rama `routes`: registro raíz; incluye nueve hijas bajo `centro-reportes` |
| `ajustes.routes.ts` | 11 | `...AJUSTES_INVENTARIO_ROUTES` en raíz; incorpora cuatro arreglos anidados |
| `facturas-proveedor.routes.ts` | 14 | Raíz y nuevamente desde `AJUSTES_INVENTARIO_ROUTES` (registro duplicado) |
| `pedidos-venta.routes.ts` | 12 | `...PEDIDOS_VENTA_ROUTES` en raíz |
| `almacenes.routes.ts` | 3 | `provideRouter` en app.config.ts |
| `ubicaciones-almacen.routes.ts` | 3 | `provideRouter` en app.config.ts |
| `transferencias.routes.ts` | 4 | `provideRouter` y dentro de `AJUSTES_INVENTARIO_ROUTES` (duplicado) |
| `cotizaciones.routes.ts` | 4 | `provideRouter` en app.config.ts |
| `centros-costo.routes.ts` | 1 | `provideRouter` en app.config.ts |
| `conteos.routes.ts` | 4 | Anidado dentro de `AJUSTES_INVENTARIO_ROUTES` |
| `reservas.routes.ts` | 4 | Anidado dentro de `AJUSTES_INVENTARIO_ROUTES` |

**Observación de riesgo sin modificación:** el árbol inserta **dos veces** el arreglo `FACTURAS_PROVEEDOR_ROUTES` (14 declaraciones) y **dos veces** `TRANSFERENCIAS_INVENTARIO_ROUTES` (4). Esto representa **18 registros duplicados por expansión**, no 18 URL faltantes ni una vulnerabilidad confirmada. No eliminar/reordenar rutas en esta microtarea documental; comprobar precedencia y navegación real en la fase certificadora P0-006.B antes de corregir con regresiones.

## Casos dirigidos y negativos

| Caso | Procedimiento reproducible | Resultado estático |
|---|---|---|
| R1 | Enumerar `path` del arreglo principal; distinguir nueve rutas hijas `centro-reportes/*` | **PASS:** 92 registros textuales; 9 hijos con `permisoGuard` y padre con `authGuard` |
| R2 | Verificar lazies y excepciones `redirectTo` | **PASS:** 90 `loadComponent`; redirección `'' -> login` y `'**' -> ''` |
| R3 | Extraer enlaces `routerLink` y cruzarlos con principal + módulos anidados | **PASS:** 39/39 resuelven en las declaraciones conocidas |
| R4 | Probar hipótesis negativa “el matcher público de tienda convierte rutas privadas en públicas” | **NO DEMOSTRADA por inspección:** `tiendas/:storeKey` no es matcher raíz genérico `:slug`; rutas administrativas listadas exigen guard propio o heredado, salvo `perfil` (solo `authGuard`) y `plataforma` (`plataformaGuard`). Verificación E2E/backend queda para P0-006.B. |
| R5 | Contar declaraciones duplicadas al expandir los arreglos del router | **HALLAZGO:** 14 de facturas y 4 de transferencias aparecen en dos rutas de registro; requiere verificación causal, sin afirmar bypass |
| R6 | Confirmar que el comodín `**` está al final de `routes`, y los módulos `provideRouter` se anteponen | **PASS estático** según `app.routes.ts` y `app.config.ts` |

**Límites de aceptación:** este acta es un inventario verificable del código en el SHA indicado. No certifica despliegue real, 92 rutas navegables con sesión, 39 permisos backend ni ausencia total de bypass. Ningún cambio en frontend/backend/API/DTO/migraciones/RBAC. **Rollback:** revert protegido de esta acta y entrada `CHANGELOG_AI.md`; conservar evidencia institucional.

**Gate de cierre:** PR a `dev`, tres checks obligatorios en su SHA final, revisión GitHub independiente, merge protegido, `dev` HEAD verificable, y luego readback de `PLAN_MAESTRO!H12:M12`, `COLA` calculada y `BITACORA`. Hasta completar esos pasos permanece `VALIDANDO`, nunca `LISTO` por crear el acta.
