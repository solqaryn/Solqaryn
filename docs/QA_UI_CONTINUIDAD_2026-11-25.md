# QA — Registro activo de pendientes visuales y funcionales

Fecha: 2026-11-25. Alcance exclusivo `qa`. Se retira un hallazgo sólo cuando exista evidencia causal de la corrección, prueba en QA y confirmación del propietario cuando corresponda. Los antecedentes quedan en el historial de Git. No se modifican datos productivos.

## Pendientes de integración y rendimiento
- **QA-CI-01** — Run 45738243072, SHA 2bfdf3bc: compilación inicial 992,23 kB frente a 750 kB máximo (650 kB advertencia). Base anterior 35f3acbf: 743,23 kB. Se localiza el proveedor de paginación en sus consumidores; queda pendiente verificar el nuevo build exact-SHA, tamaño real y traducciones. Conservar budgets, routing y regresiones; optimizar también hasta el umbral de advertencia si es viable.
- **QA-CI-02** — Estilos de storefront-producto: 18,04 kB frente a 16 kB de advertencia. Reducir duplicación sin alterar la apariencia ni el comportamiento.
- **QA-CI-03** — Repetir las dos confirmaciones de Transferencias que fallaron en 1280/390 y los escenarios de superficies nuevos. No retirar cancelación, verificación POST, escritura única, estado ni permisos.
- **QA-CI-04** — Verificar CI completo (higiene, frontend, backend), certificación viva y despliegues Vercel solqaryn-qa y solqaryn_qa en el SHA causal. No certificar por build o deploy aislado.

## Pendientes visuales transversales
- **QA-UI-01** — Inventario de todas las rutas, incluidas públicas, autenticadas, listados, creación, detalle, edición, configuración y subrutas. Revisar individualmente todas las pantallas y sus estados vacío, carga, error y datos reales.
- **QA-UI-02** — Fondos, contraste y superficies de tarjetas según tema de empresa; respetar fondoTarjetas (--color-surface), sin introducir otro color; verificar también pie de página, encabezados, tipografía, alineación, iconos y foco accesible. No sobreescribir texto de identidad configurado por empresa sin verificar su origen.
- **QA-UI-03** — Filtros, tablas con scroll interno, paginación en español y geometría sin desbordes en anchos CSS 320, 360, 390, 414, 480, 768, 1024, 1280, 1440, 1920, 2048, 2400 y 2560. Ampliar pruebas existentes, no sustituirlas.
- **QA-UI-04** — Microcopy estático y dinámico en todos los módulos: títulos, explicaciones, etiquetas, ayuda, estados vacíos, errores, confirmaciones, fechas y traducciones. En Costeo se visualiza el motivo técnico histórico de la migración ERP-N1.10; presentar una descripción comercial fiel y preservar el motivo original para trazabilidad, sin cambiar registros contables ni motivos escritos por usuarios.

## Pendientes funcionales por módulo
- **QA-FN-01** — Solicitudes, órdenes y recepciones de compra: superficies y filtros, creación/edición/detalle, modales y confirmaciones según estado y permiso.
- **QA-FN-02** — Facturas de venta/proveedor, pagos, devoluciones, notas de crédito, cuentas por cobrar y pagar, caja y bancos: acciones legítimas según estado, detalle, editor, cancelación, tablas, paginación y textos.
- **QA-FN-03** — Inventario: existencias, movimientos, ajustes, costeo, reservas, transferencias, conteos, Kardex, trazabilidad, almacenes y ubicaciones: revisar formularios, listados, confirmaciones, estados y datos dinámicos.
- **QA-FN-04** — Finanzas: centros de costo, asientos, plan de cuentas, períodos, estados financieros y reportes; superficies, filtros, edición, detalle y acciones legítimas.
- **QA-FN-05** — Administración, dashboard, catálogos, productos y variantes, usuarios, roles, clientes, proveedores, permisos, configuración, auditoría y reportes; cobertura integral por ruta, con conservación de RBAC y aislamiento empresarial.
- **QA-FN-06** — Storefront y cuenta/checkout/carrito: recorrido responsive, texto y estilos; verificar sin afectar la identidad pública de cada empresa.

## Certificación final pendiente
- Diseño/adaptabilidad; fondos/tarjetas/tema; microcopy/traducciones; funcionamiento/modales/acciones; integración/seguridad/despliegues. Cinco auditorías deben corregir hallazgos, repetir pruebas y conservar evidencia del SHA causal.
- Recorrido autenticado real por rutas y empresas QA con tema configurado; pruebas sintéticas no sustituyen datos, permisos ni operación real. Verificar acciones negativas, cancelación sin mutación, confirmación única, tenant y RBAC, sin modificar API ni semántica contable.
- Cierre del registro: retirar exclusivamente hallazgos verificados y confirmados. Cuando no queden pendientes, registrar «No hay defectos pendientes por resolver».
