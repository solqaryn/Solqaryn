# SOLQARYN V5 FINAL — C14-01.M01.02: actores, superficies y contratos de aceptación funcional

**Fecha:** 2026-11-14. **Autor:** PRIMARY DEV 06 JAVIER, `6af16bbad26881918c283abe06e1f32a`. **Entorno:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT. **Tipo:** preparación documental M01; no es E2E ejecutado, autorización de release ni implementación funcional.

**Fuentes oficiales:** [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). El preflight acreditó H=PENDIENTE/I vacío, COLA G/O=SI, DAG F=VALIDADO/G=SI; CAS de owner único. Documento M01.01 [blob `12d813a5b3f2838c2b19b8731efacb0e1d5856c6`] define 14 invariantes y 8 futuros P/N, sin declarar pruebas terminadas. El plan pide aceptación de flujos críticos públicos, privados, administrativos, tenant y sectoriales; M01 documental no libera dependencias terminales ni QA/main/PROD.

## 1. Actores, entradas, salidas y limites de confianza

Los perfiles son **roles de análisis** y no nuevas identidades o permisos RBAC. El permiso efectivo siempre lo verifica la API.

| Actor | Perfil | Entrada/superficie | Salida esperada futura | Restriccion |
| --- | --- | --- | --- | --- |
| A01 | Visitante anonimo | Ruta de tienda/producto publico | Catalogo permitido o error inocuo | No ve compra privada ni perfil ajeno |
| A02 | Comprador autenticado | Carrito, pedido y sesion | Estado de checkout permitido | No lee pedidos de otro cliente |
| A03 | Empleado de caja | Venta, medios de pago y factura | Confirmacion autorizada y trazable | Sin cobros reales fuera del entorno aprobado |
| A04 | Operador de inventario | Reserva, traslados y existencias | Movimientos consistentes | No sobreventa ni sucursal ajena |
| A05 | Administrador de sucursal | Operacion y reportes delegados | Filtros con scope sucursal | Sin privilegio plataforma |
| A06 | Administrador tenant | Usuarios, capacidades y branding | Configuracion solo para su empresa | No alterar tenant B |
| A07 | Administrador plataforma | Gestion multiempresa | Accion global auditada | Requiere auth global del backend |
| A08 | Contabilidad | Asientos, facturas e impuestos | Lectura de reportes autorizados | Sin romper historial referenciado |
| A09 | Soporte tecnico | Diagnostico de error sintético | Evidencia sanitizada | Sin credenciales o PII en logs |
| A10 | Persona con teclado/lector | Formularios y estados | Nombre, foco y anuncio correcto | Sin trampas ni mensajes solo color |
| A11 | QA automatizado | Suites E2E/API, fixture sintético | Resultado pos/neg en exact-SHA | Test .spec no es ejecucion |
| A12 | Revisor A5 Javier | Blobs, CI y auditorias | Revision independiente con evidencia | No autoaprobar, no liberar QA/PROD |

## 2. Contratos de interfaz que deberá cubrir la aceptación

Las rutas se contrastaron contra el árbol Git actual como archivos fuente. **Un .spec existente no equivale a ejecución ni cobertura certificada.** Esta unidad no cambia contratos API ni schemas.

| ID | Interfaz | Ruta o fuente real | Entrada | Salida/resultado requerido futuro |
| --- | --- | --- | --- | --- |
| I01 | Rutas publicas | `frontend/src/app/app.routes.ts` | URL/slug permitido | Pagina publica/error |
| I02 | Guard de permisos | `frontend/src/app/core/guards/permiso.guard.ts` | Rol/accion/ruta | Acceso o bloqueo en cliente; servidor decide |
| I03 | Tenant test helper | `frontend/e2e/tenant-aware-test.ts` | Fixture/empresa | Contexto sintetico para test futuro |
| I04 | Aislamiento E2E | `frontend/e2e/phase7-user-isolation.spec.ts` | Usuarios/tenants fixture | Assertions de aislamiento a ejecutar |
| I05 | Browser guard E2E | `frontend/e2e/v5-plataforma-browser-guards.spec.ts` | Sesion y rutas | Assertions de rutas a ejecutar |
| I06 | Storefront E2E | `frontend/e2e/storefront-fase12.spec.ts` | Producto/carrito/checkout fixture | Escenarios fuente no certificados |
| I07 | Inventario E2E | `frontend/e2e/reservas-inventario-overselling.spec.ts` | Reservas concurrentes | Aserciones de existencia futura |
| I08 | Ajuste responsive | `frontend/e2e/responsive-global.spec.ts` | Viewport/ruta | Cobertura responsive a medir |
| I09 | Accesibilidad E2E | `frontend/e2e/m10-ui-ux-accesibilidad.spec.ts` | Teclado/foco | Hallazgos browser a recopilar |
| I10 | Errores API | `backend/tests/Solqaryn.Tests/API/C00M0306SafeErrorsTests.cs` | Caso de error | Respuesta de prueba al ejecutar |
| I11 | Proyecto CI | `scripts/verify-project-scope.mjs` | Git SHA y alcance | Check exact-SHA para cambios |
| I12 | Oraculo institucional | `PLAN_MAESTRO / COLA / DAG_TECNICO` | ID/estado/owner/gate | Elegibilidad documental, no PASS runtime |

Flujo positivo futuro: usuario pide ruta -> frontend muestra estado -> servidor valida JWT/rol/tenant/propiedad -> Application e Infrastructure realizan operación idempotente/transaccional si aplica -> navegador presenta confirmación recuperable -> artefacto CI del SHA demuestra positivo/negativo. Flujo negativo futuro: rol o tenant incorrecto -> API deniega -> UI no revela entidad, cache, excepción interna ni histórico de otro actor. Ambos **FUTUROS_NO_EJECUTADOS** en esta microtarea.

## 3. Fronteras de seguridad y datos

| ID | Cruce | Invariante |
| --- | --- | --- |
| F01 | Visitante -> storefront | No conceder datos privados por URL publica |
| F02 | Browser -> API | JWT/RBAC/tenant propiedad validados en backend |
| F03 | Tenant A -> B | Sin cache, datos, precios o permisos cruzados |
| F04 | Usuario -> sucursal | Scope delegado no escala a admin plataforma |
| F05 | Compra -> inventario | Transaccion consistente, sin sobreventa |
| F06 | Pago -> contabilidad | Integridad e idempotencia, ninguna operacion real en M01 |
| F07 | Prueba -> fixture | Entorno efimero y datos sintéticos; no PII |
| F08 | A5 -> promocion | Revision tecnica DEV no autoriza QA/main/PROD |

El frontend jamás crea membresía/propiedad. Los fixtures de usuario/empresa/sucursal deben ser efímeros; no se leen ni alteran bases productivas. Datos comerciales, pagos, inventario, facturas e impuestos requieren aislamiento, integridad y rollback proporcional. No se envía PII a artefactos ni se registran secretos.

## 4. Casos positivos y negativos FUTUROS_NO_EJECUTADOS

| Caso | Superficie | Positivo propuesto | Negativo propuesto |
| --- | --- | --- | --- |
| P01/N01 | Publico | Producto de tienda visible | Pedido privado invisible |
| P02/N02 | Sesion | Login autorizado y expiracion segura | Credencial invalida no se reutiliza |
| P03/N03 | RBAC | Accion permitida por backend | Accion oculta en UI no crea permiso |
| P04/N04 | Tenant | A no ve B en API/cache | Intento de B desde A responde denegado |
| P05/N05 | Inventario | Reservas preservan stock | Concurrencia no causa doble reserva |
| P06/N06 | UI | Reflow y teclado conservan accion | Error solamente cromatico o CTA oculto |
| P07/N07 | Errores | Respuesta segura y recuperable | No stacktrace, tokens ni PII |
| P08/N08 | Evidencia | CI exact-SHA y negativa reproducible | SKIPPED/CANCELLED/QUEUED no cuentan PASS |

Para certificación real, la matriz futura debe guardar: caso, SHA, fixture sintético, navegador y viewport, rol/membresía/tenant, API esperada, aserción positiva, aserción negativa, resultado efectivo, checks con `head_sha` exacto, tiempos/dispositivo si pertinente, salida sanitizada, rollback y revisor A5. Esta entrega no ejecuta Playwright, .NET, MySQL, pruebas reales, cobros o despliegues.

## 5. Auditorías proporcionales y cierre

- **A0 estática:** validar dos archivos Markdown, 12 actores, 12 interfaces, ocho fronteras y ocho P/N, sin encabezados vacíos, secretos o nuevas dependencias.
- **A1 documental:** cada actor tiene entrada/salida y restricción; los oráculos del plan y M01.01 son comprobables a posteriori, no PASS funcional.
- **A2 documental:** negar elevación frontend, aislamiento A/B, factura/inventario, PII, manejo seguro de errores.
- **A3 documental:** contratos de tests existentes coherentes con fuentes; la presencia de `.spec` no prueba ejecución.
- **A4:** Git fast-forward/CAS HEAD, readback de blob/diff, Scope Lock y V5 CHANGELOG `completed/success` exact SHA; CI runtime no causal a este diff Markdown se marca NO_APLICA, nunca PASS inventado.
- **A5:** revisión por otra Task Javier distinta de `6af16bbad26881918c283abe06e1f32a`; no autoaprobación. Sin autorización de QA/main/PROD, pagos, workers, migraciones ni cambios de infra.
- **Rollback:** `git revert` del commit exacto sin forzar historia, tras releer HEAD y estados del Sheet; BITACORA A:Z sólo append.
