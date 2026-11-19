# C07-04.M06.01 — Auditoría de pruebas reales frente a mocks (V22)

PLATFORM=SOLQARYN
REPOSITORY=solqaryn/Solqaryn
BRANCH=dev
PROJECT_SCOPE_LOCK=STRICT
MICROTAREA_ID=C07-04.M06.01
AUTHOR_TASK_ID=6aeacb7820ac8191b7d768a6f33dfa09
RISK=R0_DOCUMENTAL;A5=REVISOR_JAVIER_INDEPENDIENTE_PENDIENTE

## Objeto y límites del incremento

La acción **PLAN_MAESTRO!D1148** pide revisar que las pruebas C07-04.M05.01–06 correspondan al alcance real y no se hagan pasar mocks por integración. Esta unidad exclusivamente **audita evidencia existente**; no ejecuta nuevos pagos, modifica dominio/API/DTO, altera Angular/MySQL, activa proveedores, cambia permisos o habilita QA/main/PROD. La columna PLAN E (“Completa M06”) es objetivo **acumulativo**, no un certificado de este documento. Se conserva BITACORA, la secuencia del DAG y la responsabilidad independiente A5. El área C07-04 incluye pedidos, facturas, caja, pagos, anulaciones y exportaciones; por eso el riesgo del negocio es R2/R3, aunque **este diff de documentación es R0**.

## Matriz exacta de pruebas M05.01–06

| Microtarea, acción propia | SHA de entrega (GitHub dev) | Evidencia de CI histórica | Valor probado y límite de aceptación |
| --- | --- | --- | --- |
| M05.01: regla unitaria | `859b6575f584c416a8074b2ca140b15f1776e78c` | Vitest 5/5, Angular service y respuestas GET controladas; revisión A5 independiente | Test real de regla; credenciales, empresa y HTTP externos simulados; no JWT firmado, reserva de stock ni DB |
| M05.02: contrato API/DTO y errores seguros | `ac6e42ac5b25ef86514fe5bc818d1778a0a3f35a` | xUnit .NET 10 14/14, controlador con Moq Strict, respuestas 400 al faltar idempotency header; A5 independiente | Prueba real de controlador/DTO, **no** llamada HTTP autenticada ni transacción MySQL |
| M05.03: persistencia/proveedor | `4e0ee17ae7a48da368f21e801e378ddb83e6e2c9` | 4/4 Category=Integration .NET 10, **MySQL 8.4.13 efímero de GitHub Actions**, claves repetidas A/B, rollback y rechazo de destino erróneo; A5 independiente | Integración SQL genuina pero **no** Aiven productivo, JWT, PSP, impuestos, conciliación ni transacción entre módulos |
| M05.04: aceptación positiva | `c9d6ad8275f9a6217c50fa3017ffae420a73797d` | Chromium dirigido 4/4, navegación de listado/detalle, responsive y permiso mock; A5 independiente | Navegador real con **sesiones/ventas simuladas**, no aceptación comercial end-to-end |
| M05.05: caso negativo seguridad/aislamiento | `d976f5d8f603dae350dbfcd35abf7d233aeb2256` | xUnit .NET 10 9/9 de filtro tenant/permiso e idempotencia; A5 independiente | Código de filtro probado, `IPermisoService` de fixture, no membresía viva ni JWT HTTP y recursos ajenos reales |
| M05.06: regresión/rendimiento/accesibilidad | `0f78a8f783c6afcf47e9a92d21fc67d178673683` | Chromium GitHub-hosted 15/15 en [run 44607415955](https://github.com/solqaryn/Solqaryn/actions/runs/44607415955), Angular lint/build, V5 y Scope Lock; A5 independiente | Pruebas de navegador genuinas, HTTP/session synthetic; no datos fiscales, pagos ni Core Web Vitals de campo |

Los conteos M05.01–05 se tomaron del readback real PLAN_MAESTRO K/L, sus SHA y ejecuciones referenciadas; **esta auditoría no ha reejecutado sus jobs ni atribuye un PASS nuevo**. En M05.06, el manifiesto [M06.02](c07-04-m06-02-version-diff-artefactos-reproducibilidad.md) coteja cuatro rutas/blobs del commit y documenta CI dirigida. El [registro M06.03](c07-04-m06-03-limitaciones-riesgos.md) conserva brechas R2. Los seis M05 aparecen en PLAN H=LISTO, pero LISTO atomizado no certifica toda la fase M05/M06 ni efectos financieros. Las A5 de M05.01–06 están registradas como independientes en PLAN K/BITACORA, con autores distintos.

## Falsos positivos que deben rechazarse

1. **Mock de permisos versus autorización auténtica:** un `IPermisoService` simulado, respuesta 403 o `localStorage` en Chromium no prueban consulta de membresía server-side, JWT firmado, revocación o denegación BOLA/IDOR A/B.
2. **MySQL efímero versus operación de negocio completa:** 4 tests de persistencia son reales sobre MySQL aislado, pero no aseguran reservas/stock, factura, pago PSP, conciliación, webhook firmado, reintento distribuido ni rollback multicomponente.
3. **Navegador real versus cobro real:** pasar 15 casos Chromium solo certifica las interacciones UI bajo fixtures. No afirmar autorización financiera, emisión fiscal, acceso del tenant B o tiempos de campo.
4. **CI dirigida versus salud global:** el manifiesto M06.02 registra que el check `Módulos, cargas, sesión, catálogos, variantes, accesibilidad, facturación y correo` fue `completed/failure` en el SHA fuente de M05.06. **No se oculta ni convierte a PASS**. La reparación global requiere cambios y checks causales en su scope, no documentación retrospectiva.
5. **A5 de un test versus liberación de fase:** la aprobación independiente para M05.* revisa cada entrega; no sustituye evidencias de JWT HTTP, RBAC real, PSP, impuestos, integridad contable o A5 de los M06 siguientes.
6. **SHA desalineado versus resultado válido:** `queued`, `skipped`, `cancelled`, `in_progress`, `failure` nunca equivalen a PASS de un gate obligatorio. Un SHA descendiente requiere blobs propios y dependencias/pruebas equivalentes antes de reutilizar resultados.

## Riesgos y siguiente evidencia sin falsificar estado

| Superficie futura | Riesgo abierto | Prueba causal exigible cuando corresponda |
| --- | --- | --- |
| Identidad / empresa / rol | Lectura o mutación ajena de C07 A/B, revocación no efectiva | HTTP con JWT firmado, membresía real, 401/403, rotación y casos adversariales sobre MySQL aislado |
| Inventario / pedido | Doble reserva o reintento concurrente genera inconsistencia | Transacción durable, unicidad/idempotencia, rollback verificable, carrera con dos escritores |
| Factura / caja / PSP | Cobro o anulación no auditados, callback falso | Contratos del proveedor con permisos efectivos, negativos de webhook/reversión; R3 no permitido |
| Experiencia / accesibilidad | Mock UI no representa una sesión real de usuario | E2E autorizados con datos sintéticos, teclado/escenarios 0/1/N y trazas métricas apropiadas |
| Integridad/auditoría | Changelog, BITACORA o fuente SHA pierden procedencia | Encabezados autocontenidos, CAS, commit FF, pruebas por SHA, readback institucional, rollback normal |

**Decisión operativa:** mantener C07-04.M06.01 como una auditoría estrictamente documental R0. Los riesgos pendientes permanecen abiertos y se asignan únicamente a microtareas técnicas cuyo PLAN D/DIFF los alcance, nunca se transforman a PASS por este reporte. Sus sucesoras M06.02–05 y el cierre M06.06 siguen sujetos a DAG/A5 verdaderos; no modificar manualmente COLA G/O ni DAG F/G.

## A0–A5 y reversión

- **A0:** se coteja PLAN D1148, seis SHA, códigos/fixtures, fronteras Domain/Application/API/Angular/Infrastructure y diffs de los entregables reales; este commit agrega solo este MD y CHANGELOG aditivo.
- **A1:** se verifican la matriz de seis fuentes, negativos que distinguen simulación de integración y evidencia de riesgo; un criterio de prueba futuro no se afirma ejecutado.
- **A2:** NO_APLICA ejecutar JWT/RBAC/tenant nuevo porque el changeset no altera runtime. Quedan explícitos como **no certificados en el negocio**.
- **A3:** NO_APLICA nueva MySQL/Chromium/HTTP/PSP al documento, pero se reconoce que MySQL/Chromium de M05 sí fueron auténticos en un entorno GitHub-hosted aislado, con mocks en entradas y sesiones.
- **A4:** exigir GitHub `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` del SHA exacto, diff limitado, PLAN/BUITACORA/COLA readback, V17 CAS y mantener historia. Los otros jobs solo cuentan donde son causales.
- **A5:** revisión crítica genuina por una Task JAVIER distinta de DEV04; no autoaprobación ni promoción.

Rollback: `git revert` normal del commit documental en `dev` tras revalidar HEAD, preservando BITACORA y trabajo concurrente. No force-push/reset, QA/main/PROD, proveedores de cobro ni secretos.
