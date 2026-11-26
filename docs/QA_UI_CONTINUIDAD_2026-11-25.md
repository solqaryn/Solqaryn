# QA — Registro activo de pendientes visuales y funcionales

Fecha: 2026-11-25. Alcance exclusivo `qa`. Se retira un hallazgo sólo cuando exista evidencia causal de la corrección, prueba en QA y confirmación del propietario cuando corresponda. Los antecedentes quedan en el historial de Git. No se modifican datos productivos.

## Pendientes de integración y rendimiento
- **QA-CI-04** — Pendiente únicamente recorrido autenticado real en QA y comprobación de disponibilidad del SHA desplegado tras cada cambio funcional pertinente. Acceso personal de Vercel recuperado: proyecto `prj_BIjoIFQz2oSHCosjj7lcc5blck9E`, alias canónico servido por `464d0e02908a3205e3191b82e7a7871e9f4026df` READY, rama qa; consulta de variables exitosa, 0 personalizadas y 0 ocultas según API. El binding canónico usa `VERCEL_PROJECT_ID`, no requiere overrides manuales. Run 45748241886 SUCCESS (disponibilidad, proxy QA, noindex, Storefront y DB QA); autenticación de aplicación no acreditada. No modificar otros entornos.

## Pendientes visuales transversales
- **QA-UI-01** — Inventario efectivo de Router.config acreditado en SHA 464d0e02, run 45748241922: 151 inscripciones únicas, 149 pantallas y 2 redirecciones; metadatos y guardas RBAC verificados para las rutas con contrato declarado. Completar inventario de estados internos, query params, modales y subrutas; revisar todas las pantallas individualmente en estados vacío, carga, error y datos reales.
- **QA-UI-02** — Fondos, contraste y superficies de tarjetas según tema de empresa; respetar fondoTarjetas (--color-surface), sin introducir otro color; verificar también pie de página, encabezados, tipografía, alineación, iconos y foco accesible. No sobreescribir texto de identidad configurado por empresa sin verificar su origen.
- **QA-UI-03** — Ampliar regresión exact-SHA a todas las rutas y estados, con anchos CSS 320, 360, 390, 414, 480, 768, 1024, 1280, 1440, 1920, 2048, 2400 y 2560, preservando las 69 pruebas aprobadas en 464d0e02. Aviso del detalle Storefront verificado en los 13 anchos (región viva persistente, límites horizontales, separación de barra de compra móvil). Falta cobertura integral de tema, filtros, paginación, tablas, pie de página y datos reales; repetir CI y aceptación visual autenticada.
- **QA-UI-04** — Revisar redacción estática y dinámica de todas las rutas. Verificar nuevas descripciones de los tres métodos de Costeo (promedio ponderado, FIFO y estándar), ayuda contextual del cambio y conservación del motivo original con datos reales. Extender revisión al resto de módulos y respuestas de la API.

## Pendientes funcionales por módulo
- **QA-FN-01** — Solicitudes de compra: ejecutar regresión visual de navegador con registros y datos reales, editor y detalle, acciones según estado/permiso. Órdenes y recepciones: revisar superficies, filtros, formularios y confirmaciones.
- **QA-FN-02** — Cuentas por cobrar: validar saldos separados por moneda, enlaces de venta y pagos según permisos, saldo positivo y errores 401/403, con la API real y distintos perfiles. Revisar filtros, paginación, detalle y edición en facturas de venta/proveedor, pagos, devoluciones, notas de crédito, cuentas por cobrar/pagar, caja y bancos; acciones legítimas según estado.
- **QA-FN-03** — Inventario: existencias, movimientos, ajustes, costeo, reservas, transferencias, conteos, Kardex, trazabilidad, almacenes y ubicaciones: revisar formularios, listados, confirmaciones, estados y datos dinámicos.
- **QA-FN-04** — Centros de costo: verificar en navegador y API QA real total, paginación, búsqueda/reinicio, filtros, selector de sucursal, edición y acciones. Revisar asientos, plan de cuentas, períodos, estados financieros y reportes.
- **QA-FN-05** — Administración, dashboard, catálogos, productos y variantes, usuarios, roles, clientes, proveedores, permisos, configuración, auditoría y reportes; cobertura integral por ruta, con conservación de RBAC y aislamiento empresarial.
- **QA-FN-06** — Storefront y cuenta/checkout/carrito: recorrido responsive, texto y estilos; verificar sin afectar la identidad pública de cada empresa.

## Pendientes de rutas
- **QA-RT-01** — Retirada autorizada por el propietario de los dos archivos alternativos de Cuentas por cobrar; se mantiene la única ruta efectiva `/cuentas-por-cobrar` con Facturacion/Ver, el componente activo, FacturaService y el acceso a facturas/ventas/pagos. El componente alternativo no estaba registrado en la configuración efectiva; el gate de higiene comprueba ausencia de archivos y referencias residuales en frontend. Pendiente resultado exact-SHA del nuevo commit (higiene, build y regresiones de ruta/consulta). La navegación real autenticada permanece en QA-CI-04/QA-FN-02.

## Certificación final pendiente
- Diseño/adaptabilidad; fondos/tarjetas/tema; microcopy/traducciones; funcionamiento/modales/acciones; integración/seguridad/despliegues. Cinco auditorías deben corregir hallazgos, repetir pruebas y conservar evidencia del SHA causal.
- Recorrido autenticado real por rutas y empresas QA con tema configurado; pruebas sintéticas no sustituyen datos, permisos ni operación real. Verificar acciones negativas, cancelación sin mutación, confirmación única, tenant y RBAC, sin modificar API ni semántica contable.
- Cierre del registro: retirar exclusivamente hallazgos verificados y confirmados. Cuando no queden pendientes, registrar «No hay defectos pendientes por resolver».
