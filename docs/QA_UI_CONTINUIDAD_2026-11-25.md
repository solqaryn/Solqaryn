# QA — Registro activo de pendientes visuales y funcionales

Fecha: 2026-11-25. Alcance exclusivo `qa`. Se retira un hallazgo sólo cuando exista evidencia causal de la corrección, prueba en QA y confirmación del propietario cuando corresponda. Los antecedentes quedan en el historial de Git. No se modifican datos productivos.

## Pendientes de integración y rendimiento
- **QA-CI-01** — Optimizar el paquete inicial restante: 743,24 kB frente al aviso de 650 kB. El bloqueo de 992,23 kB está resuelto en 046fbbcf, run 45741260378 SUCCESS, sin subir el máximo de 750 kB. Validar traducción real de paginadores y volver a medir cada SHA.
- **QA-CI-02** — Estilos de storefront-producto: 18,04 kB frente a 16 kB de advertencia. Reducir duplicación sin alterar la apariencia ni el comportamiento.
- **QA-CI-03** — Validar ampliación de regresión a 13 anchos, incluidas superficies y Costeo. Run 45741260378 sobre 046fbbcf: 25/25 PASS, incluidas las dos confirmaciones de Transferencias y los listados incorporados. Conservar pruebas y verificar en nuevo SHA, más aceptación visual autenticada.
- **QA-CI-04** — CI completo y certificación viva de disponibilidad/aislamiento en 046fbbcf: SUCCESS (45741260378 y 45741260357). Vercel solqaryn-qa: SUCCESS; solqaryn_qa: FAILURE por build-rate-limit. Verificación directa de equipo pendiente (403). Verificar nuevo SHA y ambos despliegues; disponibilidad no acredita autenticación ni la revisión integral. No comprar ni cambiar planes, dominios o configuración.

## Pendientes visuales transversales
- **QA-UI-01** — Inventario de todas las rutas, incluidas públicas, autenticadas, listados, creación, detalle, edición, configuración y subrutas. Revisar individualmente todas las pantallas y sus estados vacío, carga, error y datos reales.
- **QA-UI-02** — Fondos, contraste y superficies de tarjetas según tema de empresa; respetar fondoTarjetas (--color-surface), sin introducir otro color; verificar también pie de página, encabezados, tipografía, alineación, iconos y foco accesible. No sobreescribir texto de identidad configurado por empresa sin verificar su origen.
- **QA-UI-03** — Validar las nuevas 52 pruebas de superficies y Costeo sobre 13 anchos CSS (320, 360, 390, 414, 480, 768, 1024, 1280, 1440, 1920, 2048, 2400, 2560). Conservadas las pruebas de modales y storefront; ampliar después a las rutas/estados restantes, comprobar paginación, filtros, tablas con scroll y ausencia de desbordes con tema y datos reales.
- **QA-UI-04** — Revisar microcopy estático y dinámico de todas las rutas. Costeo: validar la presentación comercial del motivo exacto de la migración ERP-N1.10, la descripción de promedio ponderado y las fechas, sin modificar registros contables ni motivos de empresas. Validar motivo de usuario intacto, detalle y paginación. Pendiente validación de datos reales y extensión de redacción al resto de módulos.

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
