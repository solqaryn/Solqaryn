# SOLQARYN V5 FINAL — C14-01.M01.04: riesgos, supuestos falsables y decisiones de aceptación funcional

**Fecha:** 2026-11-14. **Autor:** PRIMARY DEV 06 JAVIER, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Repositorio:** `solqaryn/Solqaryn@dev` con PROJECT_SCOPE_LOCK=STRICT. **Clase:** preparación documental de `C14-01`, sin tests E2E ejecutados, deploy, migraciones, servicio de pago o datos productivos.

**Autoridad:** [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). Preflight: PLAN H=PENDIENTE/I vacío, COLA G/O=SI, DAG F=VALIDADO/G=SI; CAS exclusivo y BITACORA CLAIM. `C14-01.M01.01` [blob `12d813a5b3f2838c2b19b8731efacb0e1d5856c6`] define NR01–NR14; `C14-01.M01.02` [blob `865115f359193d7a601b08139f6ed75e322ca71a`] delimita actores y contratos. `C14-01.M01.03` estaba EN_PROGRESO bajo otro autor al preparar este scope: **no se afirma su inventario completo** ni se certifica lo que aún no se ha observado.

## 1. Riesgos potenciales R01–R14 y evidencia necesaria

Ningún riesgo enumerado es, por sí solo, una vulnerabilidad confirmada: cada fila propone una prueba causal futura para resolverlo.

| ID | Zona | Riesgo potencial | Evidencia que permitiría validarlo o descartarlo |
| --- | --- | --- | --- |
| R01 | Aislamiento tenant | Respuesta HTTP para B visible desde contexto A | Pruebas de pertenencia y datos sintéticos A/B |
| R02 | Autorizacion backend | Un guard de UI se confunde con control RBAC | Casos positivos/negativos HTTP por rol |
| R03 | Sesion JWT | Expiracion o cambio tenant conserva token/estado stale | Ciclo login, logout, cambio tenant y relectura |
| R04 | Storefront | URL pública entrega precio privado o compra ajena | Inspeccionar red y respuesta anonima |
| R05 | Checkout/pago | Doble submit o retry duplica resultado comercial | Pruebas idempotencia y transaccion en sandbox |
| R06 | Inventario | Reserva concurrente produce sobreventa | Prueba carrera controlada y readback persistente |
| R07 | Facturacion | Estado visible y asiento persistente divergen | Prueba integracion MySQL y rollback efimero |
| R08 | Datos financieros | Exportaciones exponen PII o otro tenant | Fixture sintético y escaneo artefactos |
| R09 | Navegacion | Ruta declarada no tiene E2E, guard o fixture | Catalogo rutas + smoke dirigido |
| R10 | Responsive | CTA se oculta en 320 px | Capturas y navegación tactil verificable |
| R11 | Accesibilidad | Error no anunciado/foco perdido | Teclado y aria con capturas de estado |
| R12 | CI | Test CANCELLED/QUEUED se registra PASS | Check-runs exact SHA y evidencia de salida |
| R13 | Fixture de pruebas | Secretos reales o tenant productivo usados en CI | Datos sintéticos redaccion y MySQL efimero |
| R14 | Promocion | LISTO_DEV/A5 documental se usa para deploy | Gate expreso QA/main/PROD independiente |

## 2. Supuestos S01–S10: estado NO_VERIFICADO

| ID | Hipotesis falsable | Estado y prueba pendiente |
| --- | --- | --- |
| S01 | El backend verifica propiedad de cada entidad | NO_VERIFICADO hasta test API/DB |
| S02 | Fixtures A/B cubren roles negativos | NO_VERIFICADO sin evidencias E2E |
| S03 | Rutas enumeradas cubren todas las pantallas | NO_VERIFICADO; inventario exact SHA necesario |
| S04 | Los tests e2e son reproducibles con servicios efimeros | NO_VERIFICADO CI y dependencias |
| S05 | Los proveedores de pago permiten sandbox sin costo | NO_VERIFICADO; no activar nada |
| S06 | Existe revert sin impacto contable | NO_VERIFICADO; valorar datos y reversibilidad |
| S07 | Tiempos de UX cumplen budget | NO_VERIFICADO; medir bundle/INP/LCP |
| S08 | A5 independiente cubre contenido y cambios | NO_VERIFICADO hasta auditoría Task distinta |
| S09 | Permisos cliente siempre reflejan API | NO_VERIFICADO; probar denegaciones |
| S10 | MySQL de CI usa provider real versionado | NO_VERIFICADO sin logs Docker/EF exactos |

No convertir un supuesto en PASS al leer una spec, README, configuración de CI o documentación. Los datos de testing deben ser sintéticos y sin claves reales.

## 3. Decisiones pendientes D01–D10

| ID | Decision abierta | Limite/estado |
| --- | --- | --- |
| D01 | Definir listado mínimo de rutas por sector y rol | ABIERTA; precisar con fuente y producto |
| D02 | Exigir orden de pruebas HTTP/DB antes de UI para autorización | ABIERTA; no mover controles |
| D03 | Establecer fixture A/B y limpieza transaccional | ABIERTA; sandbox y seguridad |
| D04 | Seleccionar reportes contables críticos para no regresión | ABIERTA; sin datos reales |
| D05 | Definir qué flujos de pago tienen sandbox autorizado | ABIERTA; sin gastos |
| D06 | Definir matriz dispositivos/zooms/accesibilidad | ABIERTA; datos objetivos |
| D07 | Relacionar cada test a su SHA y revisión A5 | ABIERTA; formato de evidencia |
| D08 | Reconciliar umbrales de rendimiento con medición | ABIERTA; no inventar SLO |
| D09 | Definir rollback reversible según tipo de transacción | ABIERTA; proteger historia |
| D10 | Separar aprobación DEV y autorización QA/main/PROD | ABIERTA; owner explícito |

Ninguna decisión aquí modifica proveedores, migraciones, tenants, secretos, presupuestos, schedules, API o gates del plan. Una decisión arquitectónica futura debe tramitarse con análisis separado y autorización correspondiente.

## 4. Pruebas propuestas FUTURAS_NO_EJECUTADAS

| ID | Positivo a demostrar | Negativo a impedir |
| --- | --- | --- |
| P01/N01 | Tenant A autorizado / B denegado | Filtración de entidad B |
| P02/N02 | JWT vigente y RBAC correcto / token expirado | Acceso con token invalido |
| P03/N03 | Checkout idempotente / doble submit | Cargo o factura duplicados |
| P04/N04 | Reserva consistente / carrera | Sobreventa en dos sesiones |
| P05/N05 | Error seguro / exception | Stacktrace o PII |
| P06/N06 | Reflow a 320/768 / viewport roto | CTA inaccesible |
| P07/N07 | E2E reproducible en SHA / test fuente | SKIPPED o CANCELLED contado PASS |
| P08/N08 | Rollback documentado / salida no autorizada | Deploy QA/PROD por doc A5 |

**Fuentes reales para diseñar futuras pruebas:** `frontend/e2e/phase7-user-isolation.spec.ts` (blob `5e207298ea4f8cccb6e88b5ec46e20a7163437a4`), `frontend/e2e/v5-plataforma-browser-guards.spec.ts` (`72e10a1cc76141bc6d84618e532fbbf2148d2b9e`), `frontend/e2e/storefront-fase12.spec.ts` (`0dfe0dcd25ee1c49299ecf4b93301d66e334fd7b`), `frontend/e2e/responsive-global.spec.ts` (`aef296d8d2766f652b1a9f47800e19411867fb07`), `backend/tests/Solqaryn.Tests/API/C00M0306SafeErrorsTests.cs` (`f5ad59a34743ceb205400e841fbe4872c040f1cf`). Se han identificado **archivos Git**, no runs, logs ni tests PASS. Para test HTTP/MySQL requerido posteriormente, usar CI real con servicio efímero y SHA exacto.

## 5. Seguridad, alcance y método A0–A5

JWT/RBAC/tenant/propiedad siguen bajo autorización backend; no se introduce ningún permiso desde HTML o menú. El aislamiento A/B exige respuestas HTTP, datos persistidos y estados de cache, no solo que una ruta se esconda. Pagos, inventario y facturas requieren transacciones e idempotencia; no ejecutar servicios externos por un documento. No se publican PII, cuentas de clientes, secretos ni datos productivos.

- **A0:** diff solo dos Markdown, 14 riesgos, 10 supuestos, 10 decisiones, encabezado y viñetas de CHANGELOG juntos, sin secretos.
- **A1 documental:** cada riesgo tiene criterio de falsación y los ocho P/N son **FUTUROS_NO_EJECUTADOS**.
- **A2 documental:** límites de tenant A/B, JWT/RBAC, pagos, PII, saldos, rollback y denegaciones futuras.
- **A3 documental:** correspondencia con M01.01, M01.02 y blobs source reales; M01.03 ajena en progreso, no inventar resultado.
- **A4:** Git fast-forward CAS exact-HEAD, blob/diff, `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` completed/success exact SHA; tests .NET/Angular/MySQL no causales para Markdown son NO_APLICA y nunca PASS ficticio.
- **A5:** revisor de otra Task Javier, distinto de `6af16bbad26881918c283abe06e1f32a`; no autoaprobar ni habilitar terminal M01.06, QA/main/PROD.
- **Rollback:** `git revert` no destructivo del commit exacto, preservando cambios concurrentes y registros BITACORA inmutables.
