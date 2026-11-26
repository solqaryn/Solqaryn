# QA — Registro activo de pendientes visuales y funcionales

Fecha: 2026-11-25. Alcance exclusivo `qa`. Se retira un hallazgo sólo cuando exista evidencia causal de la corrección, prueba en QA y confirmación del propietario cuando corresponda. Los antecedentes quedan en el historial de Git. No se modifican datos productivos.

## Pendientes de integración y rendimiento
- **QA-CI-01** — Optimizar el paquete inicial restante: 743,24 kB frente al aviso de 650 kB. El bloqueo de 992,23 kB está resuelto en 046fbbcf, run 45741260378 SUCCESS, sin subir el máximo de 750 kB. Validar traducción real de paginadores y volver a medir cada SHA.
- **QA-CI-02** — Estilos de storefront-producto: 18,04 kB frente a 16 kB de advertencia. Reducir duplicación sin alterar la apariencia ni el comportamiento.
- **QA-CI-03** — Mantener regresión exact-SHA y aceptación real: 4a556cd6, run 45742232482 SUCCESS, 68/68 Chromium (52 responsive en 13 anchos y 16 adicionales). Cobertura sintética aprobada; ampliar a todas las rutas/estados y comprobar en entorno autenticado.
- **QA-CI-04** — CI y disponibilidad 4a556cd6 SUCCESS (45742232482 y 45742232552). Ambos checks Vercel de vari-app reportan build-rate-limit. El binding canónico identifica QA como prj_BIjoIFQz2oSHCosjj7lcc5blck9E, origen https://solqarynqa-nu.vercel.app; el proyecto histórico solqaryn-qa del equipo SOLQARYN tiene ID distinto y último despliegue cancelado el 25/10/2026. Pendiente identificar IDs de ambos proyectos en vari-app, dominio, rama, variables por nombre y dependencias; consulta directa 403, autorización 404. No eliminar proyectos por nombre, ni cambiar planes. Revisar el ignore-build de IDs desconocidos sin comprometer la compilación canónica. Verificar SHA realmente desplegado y autenticación.

## Pendientes visuales transversales
- **QA-UI-01** — Inventario de todas las rutas, incluidas públicas, autenticadas, listados, creación, detalle, edición, configuración y subrutas. Revisar individualmente todas las pantallas y sus estados vacío, carga, error y datos reales.
- **QA-UI-02** — Fondos, contraste y superficies de tarjetas según tema de empresa; respetar fondoTarjetas (--color-surface), sin introducir otro color; verificar también pie de página, encabezados, tipografía, alineación, iconos y foco accesible. No sobreescribir texto de identidad configurado por empresa sin verificar su origen.
- **QA-UI-03** — Extender cobertura: 52 pruebas de superficies y Costeo aprobadas en 13 anchos CSS (320, 360, 390, 414, 480, 768, 1024, 1280, 1440, 1920, 2048, 2400, 2560) sobre 4a556cd6. Pendientes: rutas/estados restantes, tema, filtros, paginación, tablas y datos reales; revisión visual autenticada.
- **QA-UI-04** — Revisar microcopy estático y dinámico de todas las rutas. Costeo: 13/13 pruebas sintéticas en 4a556cd6 PASS (motivo histórico, texto comercial, motivo de empresa intacto, paginación y detalle). Pendiente aceptación real y redacción de los demás métodos y módulos.

## Pendientes funcionales por módulo
- **QA-FN-01** — Solicitudes de compra duplica tabla/vista móvil: corrección preparada, publicación pendiente de pruebas dirigidas. Órdenes y recepciones: superficies, filtros, creación/edición/detalle y confirmaciones por estado/permiso.
- **QA-FN-02** — Facturas de venta/proveedor, pagos, devoluciones, notas de crédito, cuentas por cobrar y pagar, caja y bancos: acciones legítimas según estado, detalle, editor, cancelación, tablas, paginación y textos.
- **QA-FN-03** — Inventario: existencias, movimientos, ajustes, costeo, reservas, transferencias, conteos, Kardex, trazabilidad, almacenes y ubicaciones: revisar formularios, listados, confirmaciones, estados y datos dinámicos.
- **QA-FN-04** — Centros de costo: filtros fuera de tarjeta y truncamiento silencioso a 100 registros sin paginación. Correcciones preparadas, publicación pendiente de pruebas dirigidas. Revisar asientos, plan de cuentas, períodos, estados financieros y reportes.
- **QA-FN-05** — Administración, dashboard, catálogos, productos y variantes, usuarios, roles, clientes, proveedores, permisos, configuración, auditoría y reportes; cobertura integral por ruta, con conservación de RBAC y aislamiento empresarial.
- **QA-FN-06** — Storefront y cuenta/checkout/carrito: recorrido responsive, texto y estilos; verificar sin afectar la identidad pública de cada empresa.

## Pendientes de rutas
- **QA-RT-01** — Validar en CI la eliminación de las inscripciones duplicadas de Transferencias, Facturas de proveedor y Cuentas por cobrar sin variar primera coincidencia ni guards. Verificar navegación autenticada, y evaluar paridad/consumidores del componente alternativo de Cuentas por cobrar antes de eliminar código huérfano.

## Certificación final pendiente
- Diseño/adaptabilidad; fondos/tarjetas/tema; microcopy/traducciones; funcionamiento/modales/acciones; integración/seguridad/despliegues. Cinco auditorías deben corregir hallazgos, repetir pruebas y conservar evidencia del SHA causal.
- Recorrido autenticado real por rutas y empresas QA con tema configurado; pruebas sintéticas no sustituyen datos, permisos ni operación real. Verificar acciones negativas, cancelación sin mutación, confirmación única, tenant y RBAC, sin modificar API ni semántica contable.
- Cierre del registro: retirar exclusivamente hallazgos verificados y confirmados. Cuando no queden pendientes, registrar «No hay defectos pendientes por resolver».
