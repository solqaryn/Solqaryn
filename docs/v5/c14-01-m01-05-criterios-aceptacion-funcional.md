# SOLQARYN V5 FINAL — C14-01.M01.05: criterios observables de aceptación funcional

**Fecha:** 2026-11-14. **Autor:** PRIMARY DEV 02 JAVIER, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Repositorio:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Tipo:** preparación documental M01.05, sin ejecución E2E ni autorización de release.

**Fuentes:** [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN_MAESTRO/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), [M01.01](./c14-01-m01-01-alcance-no-regresion-aceptacion-funcional.md), [M01.02](./c14-01-m01-02-actores-contratos-aceptacion-funcional.md), [M01.03](./c14-01-m01-03-inventario-codigo-evidencia-aceptacion-funcional.md) y [M01.04](./c14-01-m01-04-riesgos-supuestos-decisiones-aceptacion-funcional.md). El claim exigió PLAN H=PENDIENTE/I vacío, COLA G/O=SI, DAG F=VALIDADO/G=SI y actualización condicional H con lectura posterior; ninguna dependencia documental habilita por sí sola M01.06, M02, QA, main ni PROD.

## 1. Contrato de aceptación y estados

Esta unidad **define** oráculos verificables para el programa de aceptación futura de flujos críticos públicos, privados, administrativos, tenant y sectoriales. Los criterios se alinean con NR01–NR14 de M01.01, actores A01–A12 e interfaces de M01.02, inventario de fuentes de M01.03 y riesgos R01–R14 de M01.04. Son especificaciones de prueba: todos los escenarios a continuación tienen estado `FUTURO_NO_EJECUTADO`. Ninguno prueba hoy ausencia de defectos ni constituye un PASS de runtime.

Para cada caso futuro, exigir `CASE_ID`, actor/rol y permisos, contexto empresa/sucursal/recurso, fixture sintético A/B, precondición, petición o acción, resultado esperado y observado, respuesta/status y trazas redactadas, SHA de aplicación, suite CI, fecha, artefactos, revisor y plan de reversión. Un `spec` presente sin ejecución, check `queued/in_progress/cancelled/skipped`, fixture mock o snapshot no causal **no es** PASS requerido.

## 2. Matriz de casos positivos y negativos P01–P16 / N01–N16

| Caso | Superficie y riesgo | Aceptación positiva FUTURA | Rechazo negativo FUTURO |
| --- | --- | --- | --- |
| P01/N01 | Tienda pública / NR01 | Anónimo ve solo producto publicado y campos públicos | Anónimo no obtiene detalle de pedido, precio privado o datos de cliente |
| P02/N02 | Sesión / NR02 | Login permitido crea sesión válida, logout la invalida | JWT expirado, alterado o revocado no permite acceder a recurso |
| P03/N03 | Roles / NR03 | Usuario con permiso efectivo opera acción autorizada | Ocultar botón en Angular no habilita POST directo con rol insuficiente |
| P04/N04 | Tenant A/B / NR04 | Tenant A consulta recursos propios con filtro server-side | Identificador de tenant B en ruta, query, body o header no revela ni modifica B |
| P05/N05 | Administración / NR05 | Operador plataforma autorizado deja rastro auditado | Administrador de empresa no obtiene poderes globales cambiando rol o header |
| P06/N06 | Búsqueda y catálogo / NR06 | Filtros y paginación devuelven resultados propios coherentes | Cache/autocomplete no incorpora resultados de otra empresa o sesión |
| P07/N07 | Carrito y pedidos / NR07 | Pedido fixture persiste transición y clave idempotente | Doble clic o retry no factura ni confirma dos veces la misma operación |
| P08/N08 | Contabilidad / NR08 | Documento comercial conserva saldo y referencia transaccional | Error parcial no deja factura confirmada sin contrapartida persistente |
| P09/N09 | Inventario / NR09 | Reserva concurrente válida preserva disponibilidad | Dos reservas simultáneas no generan sobreventa ni saldo negativo |
| P10/N10 | Responsive / NR10 | Viewports 320, 390, 768 y 1280 permiten completar flujo esencial | Control o confirmación no queda invisible, tapado o sin desplazamiento accesible |
| P11/N11 | Accesibilidad / NR11 | Formularios exponen etiquetas, foco visible, error anunciado y teclado | Trampa de foco, error solo por color o control sin nombre accesible bloquean |
| P12/N12 | Evidencia CI / NR12 | Suite pertinente termina success sobre exact SHA y fixture inequívoco | Test fuente, resultado antiguo, skipped/cancelled o SHA distinto no acredita éxito |
| P13/N13 | Fixture y privacidad / NR12–13 | Datos A/B sintéticos permiten repetir una prueba sin secretos | Credenciales reales, PII y productos de producción no se copian a CI |
| P14/N14 | Rollback / NR13 | Revert aditivo restaura estado de código sin perder historial legítimo | Rollback destructivo o borrado de evidencias y transacciones impide aceptación |
| P15/N15 | Promoción / NR14 | LISTO DEV y A5 distinto autor se registran con SHA y motivo | A5 propio, suite pendiente o DEV se presentan como permiso QA/main/PROD |
| P16/N16 | Operación sectorial / R03–R08 | Cada módulo habilitado aplica capability, rol, ownership y sucursal | Activar otra capability/tenant no habilita rutas ni exportaciones ajenas |

**Matriz propuesta, no corrida de pruebas.** Los IDs P/N identifican expectativas futuras, no resultados capturados. Cuando una superficie todavía no tenga implementación, debe quedar `NO_IMPLEMENTADO` y no transformarse artificialmente en PASS o FAIL de ejecución.

## 3. Evidencias mínimas y gates de aceptación futura

| Dominio | Artefacto requerido para un PASS causal posterior | Condición de bloqueo |
| --- | --- | --- |
| Frontend Angular 22.2.1 | Build, test dirigido y Playwright real con viewport, traza/captura y SHA | Fallo de navegación, foco, formulario o aislamiento en navegador |
| API .NET 10 / JWT-RBAC | Prueba HTTP positiva y negativa del endpoint real, permiso y propietario | 401/403/404 ambiguo sin política documentada; acceso no autorizado |
| MySQL / consistencia comercial | MySQL efímero y test de transacción, concurrencia, idempotencia, rollback | Duplicado, pérdida o contaminación cross-tenant |
| Integración y sector | Contrato versionado, fixtures sintéticos y consumer test si aplica | Proveedor no configurado, endpoints simulados tratados como reales |
| CI / seguridad | Check-runs por exact HEAD SHA, logs pertinentes, sin secretos y A5 independiente | check requerido queued, in_progress, failed, skipped o cancelled |
| Trazabilidad institucional | PLAN/COLA/BITACORA readback y A5 con Task Javier diferente del autor | IDs duplicados, dependencia NO, doble owner o promoción implícita |

La aceptación final del lote C14-01 y cualquier promoción siguen separadas de este cierre documental. No se ejecutarán pagos, DNS, migraciones, datos productivos, nuevos workers ni servicios de pago.

## 4. Controles A0–A5 de ESTA unidad documental y reversión

- **A0:** revisar cobertura estática de NR01–NR14, P01–P16/N01–N16 y trazabilidad a M01.01–04; no afirmar una capacidad ausente.
- **A1:** comprobar coherencia interna positiva/negativa del contrato escrito; casos HTTP/browser permanecen `FUTURO_NO_EJECUTADO`.
- **A2:** mantener separación JWT/RBAC/tenant A/B, sucursal, recurso y PII como criterio negativo, nunca como prueba de aislamiento live.
- **A3:** cotejar fuentes y predecesores documentales; builds y tests funcionales ajenos al cambio exclusivamente Markdown son `NO_APLICA` con motivo.
- **A4:** una vez publicado en dev, exigir diff solo documento + CHANGELOG, Scope Lock y procedencia V5 `completed/success` del SHA exacto, BITACORA y lectura posterior.
- **A5:** revisión independiente por otra Task Javier autorizada; queda `A5_PENDIENTE` tras LISTO_DEV propio. Sin autoaprobación ni autorización QA/main/PROD.

**Rollback:** revert normal y revisado del commit en dev, con actualización aditiva de evidencia; nunca reset, force-push ni edición retroactiva de BITACORA. No se modifican API, DTO, tablas, migraciones, secretos ni runtime mediante este documento.
