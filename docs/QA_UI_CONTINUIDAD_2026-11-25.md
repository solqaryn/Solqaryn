# QA — Registro de continuidad visual y funcional
Fecha: 2026-11-25. Base causal: 9702bcf158e9e1a020e71a6c31d0b6f6e353ef8f. Alcance exclusivo QA, sin datos productivos.
## Evidencia anterior
- Run 45734701200: higiene y backend SUCCESS, frontend FAILURE, 11 Playwright PASS, 5 FAIL (transferencias 1280/390, reservas 1280/390, conteos 1280). La evidencia dice que el botón se desmontó al intentar confirmar de nuevo; no demuestra una falla del backend.
## Revisión por áreas (no equivale a certificación A5)
1. Filtros y geometría: regresión Productos localizada; se aplanan filtros y se corrige el selector global. Prueba nueva pendiente de resultado.
2. Superficies: cuatro listados de inventario en tarjeta; se reutiliza fondoTarjetas (--color-surface). Sin quinto color, sin migración de tema. Falta revisar todos los demás listados y estados vacíos.
3. Microcopy: se retiran referencias internas ERP-N1 de Almacenes y Ubicaciones y se acortan descripciones de Productos/Kardex/Costeo/Transferencias/Reservas/Conteos/Ajustes. Falta revisión exhaustiva de textos dinámicos, errores, ayuda y traducciones (paginador incluido).
4. Acciones/confirmaciones: bloqueo de reentrada visible mientras el modal cierra; se conservan permiso, tenant, estado y pruebas negativas. Falta comprobar todos los formularios restantes, especialmente ajustes (modal manual), recepciones, órdenes, solicitudes, facturas, finanzas, caja, bancos, contabilidad, configuración, permisos, auditoría y reportes. No se añaden acciones contrarias al dominio.
5. CI/seguridad: nuevas pruebas integradas en gate existente sin deshabilitar pruebas previas. Resultado del nuevo SHA pendiente; inspección autenticada pendiente. No declarar PASS global antes de ambos.
## Criterios de aceptación
- Todos los listados con superficie visible, filtros adaptables, estados vacío/error/carga, scroll propio de tabla, acciones según permiso, etiquetas legibles y copy español breve y factual.
- Cancelar no muta, confirmar una sola vez, tenant/RBAC y estados revalidados; sin cambio de API ni semántica contable.
- CI exact-SHA, capturas escritorio/móvil y recorrido autenticado de todas las rutas. Si queda un fallo, registrar causa y corregir; no certificar por compilación.

## Segunda revisión — evidencia y continuidad
- SHA 35f3acbfdd5625cce59634717b694557a2354df7: run 45737518387, 20/22 pruebas de navegador aprobadas (incluidas las seis nuevas de Productos y tarjetas a 1440/390/320 px); higiene/backend correctos. Certificación viva 45737518375 y ambos despliegues Vercel correctos. Dos fallos restantes de Transferencias en 1280/390 px: la aserción de texto coincidía con el encabezado de tabla «Solicitada» antes de que se ejecutara la petición POST. Se exige respuesta POST exitosa y estado del resumen, sin retirar verificación de cancelación, escritura única, permisos o actualización del listado.
- Se migran superficies de Órdenes, Recepciones, Existencias, Períodos y Cuentas por cobrar; se adapta scroll de Cuentas bancarias, Asientos y Estados financieros. Plan de cuentas, Solicitudes, Finanzas, Configuración y detalle de Transferencias reciben correcciones puntuales de textos. Paginador Material centralizado en español. No se altera lógica de negocio.
- Se añaden tres escenarios sintéticos de cinco nuevos listados vacíos en 1440/390/320 px. Pendientes de ejecución y validación visual autenticada. No equivale a certificación integral.
### Cinco revisiones técnicas realizadas en esta intervención
1. Layout: composición de filtros, tamaños mínimos, scroll local y revisión de las nuevas tarjetas; seis pruebas previas aprobaron.
2. Superficies y tema: uso del token existente, estados vacíos, separación del fondo; nuevas rutas pendientes de pruebas.
3. Textos: eliminación de jerga de fases, copy descriptivo en módulos intervenidos, traducción global del paginador. Pendiente revisar textos de estados, errores y ayuda dinámica de todas las pantallas, incluidas traducciones de fecha y vocabulario de dominio.
4. Acciones: reapertura segura de confirmaciones y aserción de escritura real; falta verificar los dos casos corregidos y migrar flujos restantes. Conservados permisos, guardas y contratos.
5. Integración: se revisan resultados y diferencias de cambios; verificar nuevo SHA, capturas y sesión autenticada. No declarar cierre por build o despliegue solamente.
### Pendientes concretos (orden de continuación)
- Validar confirmaciones de Transferencias y las cinco superficies nuevas con CI exact-SHA; revisar trazas si falla cualquier escenario.
- Solicitudes, órdenes y recepciones: creación, edición, detalle y confirmaciones uniformes; evitar duplicación entre vista inline y modal. 
- Facturas de venta/proveedor, pagos, devoluciones, notas de crédito, cuentas por pagar, caja y bancos: respetar acciones de estado y permisos; no introducir eliminación ajena al dominio.
- Inventario: existencias y movimientos, detalle/creación de ajustes y costeo; revisar formularios, estados cargando/error y acciones restantes.
- Finanzas: centros de costo (filtro fuera de tarjeta, editor inline), asientos, plan de cuentas, períodos, reportes; completar acciones y revisión de tablas de cada estado.
- Administración: usuarios, roles, clientes, proveedores, permisos, configuración y auditoría (tabla/vista móvil duplicadas); revisar cobertura integral de modales/confirmaciones/tablas y textos por ruta.
- Inspección visual autenticada en QA, con tema real por empresa, en escritorio y móvil; no sustituirla por mocks. Mantener las cinco auditorías completas finales abiertas hasta obtener esa evidencia.
