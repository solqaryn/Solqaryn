# QA — Registro activo de pendientes visuales y funcionales

Fecha: 2026-11-25. Alcance exclusivo `qa`. El registro contiene únicamente trabajo restante; la evidencia de cierres parciales y completos se conserva en `CHANGELOG_AI.md` y en el historial Git. Se retira un hallazgo sólo cuando exista evidencia causal de la corrección, prueba en QA y confirmación del propietario cuando corresponda. Los antecedentes quedan en el historial de Git. No se modifican datos productivos.

## Pendientes de integración y rendimiento
- **QA-CI-04** — Resolver las compilaciones cruzadas del proyecto QA originadas por el contrato desactualizado de la rama dev (requiere autorización expresa para modificar dev); respetar la cuota de Vercel, sin contratar planes. El alias QA sigue sirviendo `23dc28d1` READY, mientras `f8ea35a3` recibe FAILURE por límite de builds. Desplegar el SHA funcional validado o su sucesor y acreditar alias/SHA, navegación autenticada, roles y aislamiento empresarial.

## Pendientes visuales transversales
- **QA-UI-01** — Completar el inventario de estados internos, query params, modales y subrutas; auditar individualmente las 149 pantallas en estados vacío, carga, error y datos representativos.
- **QA-UI-02** — Extender la comprobación de fondos y contraste al resto de pantallas y temas, incluyendo pie de página, encabezados, tipografía, alineación, iconos, foco accesible, tarjetas Material y botón de pagos con registros. Verificar estados poblados, errores y cambios de contexto; preservar fondoTarjetas y la identidad empresarial.
- **QA-UI-03** — Ejecutar efectivamente `e2e/responsive-global.spec.ts` para las 149 pantallas y raíz en los 16 anchos del catálogo compartido; completar estados internos, filtros, paginación, tablas, pie de página y datos representativos.
- **QA-UI-04** — Revisar textos estáticos y dinámicos no cubiertos de todas las rutas y respuestas API. Verificar interacción de selección y ayuda contextual del cambio de Costeo, así como presentación con datos reales y conservación del motivo original. Comprobar específicamente el nuevo texto y validación de sucursal en el caso de tipo Sucursal.

## Pendientes funcionales por módulo
- **QA-FN-01** — Solicitudes de compra: ampliar pruebas dirigidas de respuestas rechazadas en consulta de detalle, guardado y acciones, editor y detalle con registros, permisos y estados. Órdenes y recepciones: revisar datos poblados, filtros, formularios y confirmaciones no cubiertas. Completar recorrido con API real.
- **QA-FN-02** — Cuentas por cobrar: cubrir 401, saldo cero/negativo, color del botón de pagos con registros, perfiles y API real. Revisar filtros, paginación, detalle y edición en facturas de venta/proveedor, pagos, devoluciones, notas de crédito, cuentas por cobrar/pagar, caja y bancos; acciones legítimas según estado.
- **QA-FN-03** — Inventario: revisar estados poblados y dinámicos, formularios, permisos y acciones de existencias, movimientos, ajustes, costeo, reservas, transferencias, conteos, Kardex, trazabilidad, almacenes y ubicaciones; ampliar la cobertura más allá de las superficies vacías, modales y presentación de Costeo ya probados.
- **QA-FN-04** — Centros de costo: comprobar selector de sucursal, tipo Sucursal, filtros adicionales, edición/acciones y recorrido de navegador/API real con registros. Revisar asientos, plan de cuentas, períodos con datos y estados financieros/reportes. Ejecutar también `centros-costo.component.spec.ts` para comprobar el cambio de redacción.
- **QA-FN-05** — Administración, dashboard, catálogos, productos y variantes, usuarios, roles, clientes, proveedores, permisos, configuración, auditoría y reportes: completar flujos y estados no cubiertos por pruebas dirigidas de tallas, tabla de clientes, variantes, permisos, filtros de productos y modales; preservar RBAC y aislamiento empresarial.
- **QA-FN-06** — Storefront y cuenta/checkout/carrito: completar recorrido responsive, textos, operaciones y estados no cubiertos por las pruebas dirigidas de detalle, galería, destacados y feedback, con identidad de cada empresa preservada.

## Certificación final pendiente
- Diseño/adaptabilidad; fondos/tarjetas/tema; microcopy/traducciones; funcionamiento/modales/acciones; integración/seguridad/despliegues. Cinco auditorías deben corregir hallazgos, repetir pruebas y conservar evidencia del SHA causal.
- Recorrido autenticado real por rutas y empresas QA con tema configurado; pruebas sintéticas no sustituyen datos, permisos ni operación real. Verificar acciones negativas, cancelación sin mutación, confirmación única, tenant y RBAC, sin modificar API ni semántica contable.
- Cierre del registro: retirar exclusivamente hallazgos verificados y confirmados. Cuando no queden pendientes, registrar «No hay defectos pendientes por resolver».
