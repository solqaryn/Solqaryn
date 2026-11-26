# QA — Registro activo de pendientes visuales y funcionales

Fecha: 2026-11-25. Alcance exclusivo `qa`. Se retira un hallazgo sólo cuando exista evidencia causal de la corrección, prueba en QA y confirmación del propietario cuando corresponda. Los antecedentes quedan en el historial de Git. No se modifican datos productivos.

## Pendientes de integración y rendimiento
- **QA-CI-02** — Confirmar tamaño compilado del detalle de producto de 16 kB o menos tras compartir avisos accesibles de Inicio/Catálogo/Detalle; última medición 16,32 kB (exceso 324 bytes). Validar aviso visible, región viva persistente, espacio sobre compra móvil, galería e identidad con el gate de presupuestos y la regresión de navegador. No aumentar budgets.
- **QA-CI-04** — Verificar SHA efectivamente servido por https://solqarynqa-nu.vercel.app y nombres de variables del único proyecto autorizado prj_BIjoIFQz2oSHCosjj7lcc5blck9E mediante acceso autorizado (403; solicitud 404). Acreditar recorrido real autenticado y repetir comprobación de disponibilidad en el SHA desplegado. No modificar otros entornos.
- **QA-CI-05** — Verificar diagnóstico de contribuciones por archivo del build: Angular no produjo metadatos localizables en a787c64c. Se reutiliza el analizador existente y se admiten stats.json/browser-stats.json; el control de presupuestos usa el log causal y el baseline lee límites vigentes. Falta confirmar que se genera y conserva el desglose real, sin inventar estadísticas.

## Pendientes visuales transversales
- **QA-UI-01** — Validar el inventario efectivo emitido por la prueba de Router.config (151 inscripciones esperadas, 149 pantallas y 2 redirecciones), incluidos parámetros y permisos. Completar inventario de estados internos, query params, modales y subrutas; revisar todas las pantallas individualmente en estados vacío, carga, error y datos reales.
- **QA-UI-02** — Fondos, contraste y superficies de tarjetas según tema de empresa; respetar fondoTarjetas (--color-surface), sin introducir otro color; verificar también pie de página, encabezados, tipografía, alineación, iconos y foco accesible. No sobreescribir texto de identidad configurado por empresa sin verificar su origen.
- **QA-UI-03** — Ampliar regresión exact-SHA a todas las rutas y estados, con anchos CSS 320, 360, 390, 414, 480, 768, 1024, 1280, 1440, 1920, 2048, 2400 y 2560, preservando las 68 pruebas existentes y verificando el nuevo aviso de Storefront en los 13 anchos. Verificar tema, filtros, paginación, tablas, pie de página y datos reales; repetir CI y aceptación visual autenticada.
- **QA-UI-04** — Revisar redacción estática y dinámica de todas las rutas. Verificar nuevas descripciones de los tres métodos de Costeo (promedio ponderado, FIFO y estándar), ayuda contextual del cambio y conservación del motivo original con datos reales. Extender revisión al resto de módulos y respuestas de la API.

## Pendientes funcionales por módulo
- **QA-FN-01** — Solicitudes de compra: ejecutar regresión visual de navegador con registros y datos reales, editor y detalle, acciones según estado/permiso. Órdenes y recepciones: revisar superficies, filtros, formularios y confirmaciones.
- **QA-FN-02** — Cuentas por cobrar: validar saldos separados por moneda, enlaces de venta y pagos según permisos, saldo positivo y errores 401/403, con la API real y distintos perfiles. Revisar filtros, paginación, detalle y edición en facturas de venta/proveedor, pagos, devoluciones, notas de crédito, cuentas por cobrar/pagar, caja y bancos; acciones legítimas según estado.
- **QA-FN-03** — Inventario: existencias, movimientos, ajustes, costeo, reservas, transferencias, conteos, Kardex, trazabilidad, almacenes y ubicaciones: revisar formularios, listados, confirmaciones, estados y datos dinámicos.
- **QA-FN-04** — Centros de costo: verificar en navegador y API QA real total, paginación, búsqueda/reinicio, filtros, selector de sucursal, edición y acciones. Revisar asientos, plan de cuentas, períodos, estados financieros y reportes.
- **QA-FN-05** — Administración, dashboard, catálogos, productos y variantes, usuarios, roles, clientes, proveedores, permisos, configuración, auditoría y reportes; cobertura integral por ruta, con conservación de RBAC y aislamiento empresarial.
- **QA-FN-06** — Storefront y cuenta/checkout/carrito: recorrido responsive, texto y estilos; verificar sin afectar la identidad pública de cada empresa.

## Pendientes de rutas
- **QA-RT-01** — Comparada la vista activa de Cuentas por cobrar (FacturaService, resumen, factura, venta y pagos) con la alternativa (mismo endpoint, consulta modal). Validar paridad de consulta, navegación autenticada y ausencia de consumidores de la alternativa antes de retirar código huérfano.

## Certificación final pendiente
- Diseño/adaptabilidad; fondos/tarjetas/tema; microcopy/traducciones; funcionamiento/modales/acciones; integración/seguridad/despliegues. Cinco auditorías deben corregir hallazgos, repetir pruebas y conservar evidencia del SHA causal.
- Recorrido autenticado real por rutas y empresas QA con tema configurado; pruebas sintéticas no sustituyen datos, permisos ni operación real. Verificar acciones negativas, cancelación sin mutación, confirmación única, tenant y RBAC, sin modificar API ni semántica contable.
- Cierre del registro: retirar exclusivamente hallazgos verificados y confirmados. Cuando no queden pendientes, registrar «No hay defectos pendientes por resolver».
