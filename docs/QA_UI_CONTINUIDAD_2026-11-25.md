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
