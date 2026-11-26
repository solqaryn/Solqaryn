# QA — Registro activo de pendientes visuales y funcionales

Fecha: 2026-11-25. Alcance exclusivo `qa`. El registro contiene únicamente trabajo restante; la evidencia de cierres parciales y completos se conserva en `CHANGELOG_AI.md` y en el historial Git. Se retira un hallazgo sólo cuando exista evidencia causal de la corrección, prueba en QA y confirmación del propietario cuando corresponda. Los antecedentes quedan en el historial de Git. No se modifican datos productivos.

## Pendientes de integración y rendimiento
- **QA-CI-04** — Resolver compilaciones cruzadas y confirmar ajuste de previews del proyecto QA (solicitud previa aceptada, readback no acreditado); desplegar SHA funcional validado y verificar alias/SHA, navegación autenticada, roles y aislamiento empresarial. Último alias observado: c4a01a21 READY, 70fa2b23 con límite de builds. No modificar DEV/MAIN ni contratar planes.

## Pendientes visuales transversales
- **QA-UI-01** — Auditar estados internos, query params, modales y subrutas de las 149 pantallas, en vacío, carga, error y datos representativos. La matriz de rutas de CI aislado registra PASS/FAIL/NOT_RUN por pantalla y resolución; completar sus hallazgos y los estados no cubiertos.
- **QA-UI-02** — Extender fondos, contraste, pie de página, encabezados, tipografía, alineación, iconos y foco a las superficies y temas no verificados, estados poblados y cambios de contexto. Nuevo barrido verifica token de superficies visibles. Se excluyen las comprobaciones de fondo y botón Pagos ya acreditadas a 390/1440.
- **QA-UI-03** — Ejecutar matriz de 149 pantallas y raíz en 16 anchos con API/MySQL desechables; resolver cada FAIL y NOT_RUN, completar fixtures de IDs y estados internos, tablas, filtros y paginación. No certificar por redirección o por ausencia de datos. Nuevo workflow qa-responsive-audit.yml, pendiente evidencia causal.
- **QA-UI-04** — Revisar texto estático/dinámico y traducciones de pantallas y respuestas restantes; resolver codificación defectuosa detectada por barrido y evaluar claridad contextual. Estabilizar la selección de Costeo a 390 px (panel de opciones desmontado en primer intento del run 45755575602); nuevo control del ciclo de overlay y gate fail-on-flaky-tests. La redacción de sucursal ya pasó suite determinista; revisar selector pendiente en Contabilidad.

## Pendientes funcionales por módulo
- **QA-FN-01** — Completar orden de compra y recepción: validar nuevas protecciones de confirmación/permiso y saldo asociado a la orden, rechazo de guardado, datos poblados y flujos completos de aprobación, recepción y anulación. Revisar operaciones no cubiertas de solicitudes, órdenes y recepciones con API real.
- **QA-FN-02** — Completar flujos de facturas de venta/proveedor, pagos, devoluciones, notas de crédito, cuentas por pagar, caja y bancos; revisar filtros, paginación, detalle, edición, permisos y estados. CxC: recorrido real con perfiles y API real.
- **QA-FN-03** — Revisar estados poblados, formularios, permisos y operaciones de existencias, movimientos, ajustes, costeo, reservas, transferencias, conteos, Kardex, trazabilidad, almacenes y ubicaciones. Confirmar selección de Costeo sin reintentos y cambio autorizado con entorno de pruebas desechable.
- **QA-FN-04** — Centros de costo: selector de sucursal con autorización existente, filtros y edición con registros. Períodos: validar nuevas regresiones de listados, alta y cierre rechazados; confirmar flujos de alta/cierre real aislado. Completar asientos, plan de cuentas y estados financieros/reportes.
- **QA-FN-05** — Completar administración, dashboard, catálogos, productos/variantes, usuarios, roles, clientes, proveedores, permisos, configuración, auditoría y reportes en los estados no cubiertos. Revisar datos y acciones con RBAC/tenant.
- **QA-FN-06** — Validar entrega de cuenta y checkout: respuestas tardías de sesión, limpieza del panel, invalidación de enlace preparado tras modificar datos y bloqueo de duplicación. Completar recorrido responsive y flujos/estados pendientes de catálogo, cuenta, carrito, checkout y pedidos, sin pagos ni mensajes externos reales.

## Certificación final pendiente
- Diseño/adaptabilidad; fondos/tarjetas/tema; microcopy/traducciones; funcionamiento/modales/acciones; integración/seguridad/despliegues. Cinco auditorías deben corregir hallazgos, repetir pruebas y conservar evidencia del SHA causal.
- Recorrido autenticado real por rutas y empresas QA con tema configurado; pruebas sintéticas no sustituyen datos, permisos ni operación real. Verificar acciones negativas, cancelación sin mutación, confirmación única, tenant y RBAC, sin modificar API ni semántica contable.
- Cierre del registro: retirar exclusivamente hallazgos verificados y confirmados. Cuando no queden pendientes, registrar «No hay defectos pendientes por resolver».
