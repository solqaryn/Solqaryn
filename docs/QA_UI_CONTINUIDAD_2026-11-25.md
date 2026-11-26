# QA — Registro activo de pendientes visuales y funcionales

Fecha: 2026-11-25. Alcance exclusivo `qa`. Se retira un hallazgo sólo cuando exista evidencia causal de la corrección, prueba en QA y confirmación del propietario cuando corresponda. Los antecedentes quedan en el historial de Git. No se modifican datos productivos.

## Pendientes de integración y rendimiento
- **QA-CI-01** — Confirmar en CI exact-SHA que la carga diferida de los defaults de Material Dialog reduce el paquete inicial a 650 kB o menos (última medición verificada 742,95 kB). Validar equivalencia de opciones de apertura, traducción real de paginación y regresión de modales; si persiste el aviso, analizar las estadísticas de contribuciones y eliminar dependencias eager innecesarias. Mantener máximo de 750 kB.
- **QA-CI-02** — Medir el SCSS compilado de storefront-producto tras compartir pie de página y selector de fuente con catálogo y eliminar repetición responsive (última medición verificada 18,04 kB; objetivo máximo de aviso 16 kB). Validar identidad, enlaces, foco, galería, compra y presentación sin regresiones. Si persiste el aviso, continuar reducción causal sin subir el presupuesto.
- **QA-CI-04** — Acreditar despliegue del HEAD vigente en el único proyecto QA autorizado prj_BIjoIFQz2oSHCosjj7lcc5blck9E (https://solqarynqa-nu.vercel.app), confirmar fin del límite de compilaciones y que sólo recibe builds de qa; verificar configuración de variables por nombre mediante acceso autorizado (403). Validar nuevo control ignore-build de IDs explícitos desconocidos en CI y que no se genere otro check de proyecto redundante. Comprobar el SHA publicado y recorrido autenticado real, sin modificar otros entornos.

## Pendientes visuales transversales
- **QA-UI-01** — Inventario de todas las rutas, incluidas públicas, autenticadas, listados, creación, detalle, edición, configuración y subrutas. Revisar individualmente todas las pantallas y sus estados vacío, carga, error y datos reales.
- **QA-UI-02** — Fondos, contraste y superficies de tarjetas según tema de empresa; respetar fondoTarjetas (--color-surface), sin introducir otro color; verificar también pie de página, encabezados, tipografía, alineación, iconos y foco accesible. No sobreescribir texto de identidad configurado por empresa sin verificar su origen.
- **QA-UI-03** — Ampliar regresión exact-SHA a todas las rutas y estados, con anchos CSS 320, 360, 390, 414, 480, 768, 1024, 1280, 1440, 1920, 2048, 2400 y 2560, preservando las 68 pruebas existentes. Verificar tema, filtros, paginación, tablas, pie de página y datos reales; repetir CI y aceptación visual autenticada de cada nuevo SHA.
- **QA-UI-04** — Revisar microcopy estático y dinámico de todas las rutas. Costeo: 13/13 pruebas sintéticas en 4a556cd6 PASS (motivo histórico, texto comercial, motivo de empresa intacto, paginación y detalle). Pendiente aceptación real y redacción de los demás métodos y módulos.

## Pendientes funcionales por módulo
- **QA-FN-01** — Solicitudes de compra: ejecutar regresión visual de navegador con registros y datos reales, editor y detalle, acciones según estado/permiso. Órdenes y recepciones: revisar superficies, filtros, formularios y confirmaciones.
- **QA-FN-02** — Facturas de venta/proveedor, pagos, devoluciones, notas de crédito, cuentas por cobrar y pagar, caja y bancos: acciones legítimas según estado, detalle, editor, cancelación, tablas, paginación y textos.
- **QA-FN-03** — Inventario: existencias, movimientos, ajustes, costeo, reservas, transferencias, conteos, Kardex, trazabilidad, almacenes y ubicaciones: revisar formularios, listados, confirmaciones, estados y datos dinámicos.
- **QA-FN-04** — Centros de costo: verificar en navegador y API QA real total, paginación, búsqueda/reinicio, filtros, selector de sucursal, edición y acciones. Revisar asientos, plan de cuentas, períodos, estados financieros y reportes.
- **QA-FN-05** — Administración, dashboard, catálogos, productos y variantes, usuarios, roles, clientes, proveedores, permisos, configuración, auditoría y reportes; cobertura integral por ruta, con conservación de RBAC y aislamiento empresarial.
- **QA-FN-06** — Storefront y cuenta/checkout/carrito: recorrido responsive, texto y estilos; verificar sin afectar la identidad pública de cada empresa.

## Pendientes de rutas
- **QA-RT-01** — Verificar navegación autenticada y paridad/consumidores del componente alternativo de Cuentas por cobrar antes de retirar código huérfano.

## Certificación final pendiente
- Diseño/adaptabilidad; fondos/tarjetas/tema; microcopy/traducciones; funcionamiento/modales/acciones; integración/seguridad/despliegues. Cinco auditorías deben corregir hallazgos, repetir pruebas y conservar evidencia del SHA causal.
- Recorrido autenticado real por rutas y empresas QA con tema configurado; pruebas sintéticas no sustituyen datos, permisos ni operación real. Verificar acciones negativas, cancelación sin mutación, confirmación única, tenant y RBAC, sin modificar API ni semántica contable.
- Cierre del registro: retirar exclusivamente hallazgos verificados y confirmados. Cuando no queden pendientes, registrar «No hay defectos pendientes por resolver».
