# SOLQARYN V5 FINAL — C14-01.M01.01: objetivo, exclusiones y no-regresion de aceptación funcional

**Fecha:** 2026-11-14. **Autor:** PRIMARY DEV 06 JAVIER, `6af16bbad26881918c283abe06e1f32a`. **Repositorio:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Clase:** preparación documental M01 de `C14-01 — Aceptación funcional`, no una aceptación final, ejecución E2E, release ni certificado de seguridad.

**Autoridad:** [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), Skill canonica y Scope Lock. Para este ID: PLAN H=PENDIENTE/I vacio, COLA G/O=SI, DAG F=VALIDADO/G=SI y CAS de owner auditado. Sus dependencias de fase/bloque aparecen en PLAN, pero la habilitacion `DOC_M01_PREP` **no autoriza M01.06, suite E2E ejecutada, migracion, QA/main/PROD o dinero real**.

## 1. Objetivo observable y limites

Preparar una matriz reproducible de aceptacion funcional para flujos publicos, autenticados, administrativos, multitenant y sectoriales; vincular requisito, ruta, actor, tenant, datos sinteticos, estado, resultado esperado, prueba positiva y negativa, Git SHA y evidencia de rollback. El criterio de exito futuro exige pruebas reales sobre endpoints, frontend y persistencia cuando el cambio lo requiera, y revision independiente A5 cuando corresponda.

**Fuera de alcance de esta microtarea:** compilar, cambiar o desplegar .NET/Angular/MySQL; registrar resultados como PASS, operar pagos, migrar datos, alterar JWT/RBAC, hacer seed en produccion, crear fixtures sensibles, implementar eventos/dispatcher, desactivar controles, abrir PR/branches, autorizar release, QA/main/PROD, DNS o infraestructura de pago. Documento y CHANGELOG son los unicos cambios esperados.

La aceptacion posterior debe preservar integridad de referencias comerciales y financieras, autorizacion server-side, identidad tenant/empresa/sucursal, accesibilidad y respuesta segura ante fallos. Una pagina visible o spec existente no certifica ninguno de estos hechos.

## 2. Invariantes NR01–NR14 para aceptación futura

| ID | Dominio / frontera | Positivo que debe conservarse | Negativo bloqueante |
| --- | --- | --- | --- |
| NR01 | Rutas publicas | Checkout y producto nunca filtran informacion empresarial privada | Vista anonima con pedidos privados o sesion no validada |
| NR02 | Login y sesion | Error seguro, foco y expiracion controlados | JWT invalido reutilizado o datos previos visibles |
| NR03 | Permisos | Cada accion privada exige RBAC en backend | Menu oculto tomado como autorizacion |
| NR04 | Tenancy | Tenant A/B segregado en API, cache, fixtures y reportes | Datos, precios privados o ID de A consultables desde B |
| NR05 | Admin plataforma | Acciones globales auditadas y autorizadas separadamente | Rol tenant escala a privilegios plataforma |
| NR06 | Catalogos | Listas paginadas preservan filtros y autorizacion | Resultado cruzado por cache, autocomplete o query |
| NR07 | Pedidos y pagos | Transicion de estado idempotente y coherente | Pago duplicado o confirmacion sin estado persistente |
| NR08 | Facturas/finanzas | Referencias y saldos trazables con consistencia | Documento confirmado sin transaccion correlativa |
| NR09 | Inventario | Stock y reservas sin sobreventa concurrente | Existencia negativa o doble asignacion |
| NR10 | Responsive | 320/390/768/1280 mantienen interaccion principal | CTA invisible o tabla inaccesible en viewport |
| NR11 | Accesibilidad | Estados de foco, etiqueta, error y teclado verificables | Foco atrapado, error cromatico o control sin nombre |
| NR12 | Reproducibilidad | CI por SHA y fixture sintetico con metadata | Un test fuente, skip o cancelacion contado PASS |
| NR13 | Rollback | Reversion no destructiva preserva historia comercial | Rollback elimina evidencia, secretos o datos ajenos |
| NR14 | Promocion | Aceptacion DEV separada de QA, main y PROD | A5 o doc equivale a autorizacion de despliegue |

## 3. Inventario de fuentes verificadas en Git

Se indican SHA de **blob Git** del código fuente, no checks aprobados; la existencia de una spec no demuestra que se ejecutó. Base inspeccionada de `dev`: archivo verificable mediante ruta y blob, con posteriores cambios a revalidar antes de pruebas.

| Ref | Ruta real | Blob SHA | Alcance y límite |
| --- | --- | --- | --- |
| F01 | `frontend/e2e/storefront-fase12.spec.ts` | `0dfe0dcd25ee1c49299ecf4b93301d66e334fd7b` | Fuente de flujos storefront, no certificacion de pagos |
| F02 | `frontend/e2e/phase7-user-isolation.spec.ts` | `5e207298ea4f8cccb6e88b5ec46e20a7163437a4` | Fuente de escenarios aislamiento de usuario, no evidencia E2E ejecutada |
| F03 | `frontend/e2e/v5-plataforma-browser-guards.spec.ts` | `72e10a1cc76141bc6d84618e532fbbf2148d2b9e` | Escenarios de guard/browser, no sustituto auth server |
| F04 | `frontend/e2e/responsive-global.spec.ts` | `aef296d8d2766f652b1a9f47800e19411867fb07` | Lista de rutas responsive, no captura real ni cobertura completa |
| F05 | `frontend/e2e/m10-ui-ux-accesibilidad.spec.ts` | `ce3d50c53b5de8447fa9eba09208160657415fa3` | Pruebas fuente de accesibilidad, WCAG no certificado |
| F06 | `frontend/e2e/tenant-aware-test.ts` | `9c196b0591253b247c2a6a126e98cfa4a7abfb20` | Helper tenant sintético, no autorizacion real |
| F07 | `frontend/src/app/app.routes.ts` | `2f560c14693455b79ac4546e9333fb74586ef234` | Rutas publicas/privadas declaradas, no ejecucion |
| F08 | `frontend/src/app/core/guards/permiso.guard.ts` | `1052bd38041055cc81e84a14d5fe0364e169b1fe` | Guard de cliente, backend conserva RBAC |
| F09 | `backend/tests/Solqaryn.Tests/API/C00M0306SafeErrorsTests.cs` | `f5ad59a34743ceb205400e841fbe4872c040f1cf` | Test fuente de errores seguros, no resultado de run |
| F10 | `scripts/verify-project-scope.mjs` | `5d51fe92566cfd0e51b7265b844af5432bac57c3` | Script de Scope Lock, no autoriza promoción QA/main/PROD |

## 4. Muestras positivas y negativas FUTURAS_NO_EJECUTADAS

| ID | Actor/superficie | Positivo futuro | Negativo futuro | Oraculo inicial (no PASS) |
| --- | --- | --- | --- | --- |
| P01/N01 | Anonimo / tienda | Entrar a storefront, listar productos permitidos | No acceder a cuentas/pedidos de otra persona | `frontend/e2e/storefront-fase12.spec.ts` |
| P02/N02 | Autenticado / empresa | Login y cambio autorizado A->B sin residuos | B no ve datos ni marca A al cambiar contexto | `frontend/e2e/phase7-user-isolation.spec.ts` |
| P03/N03 | Admin / permisos | Accion permitida corresponde a rol servidor | UI expone ruta protegida pero API debe denegar | `frontend/e2e/v5-plataforma-browser-guards.spec.ts` |
| P04/N04 | Inventario | Reserva y liberacion preservan stock | Carrera de reservas no produce sobreventa | `frontend/e2e/reservas-inventario-overselling.spec.ts` |
| P05/N05 | UI / reflow | Accion visible y foco estable en 320/768 | Viewport recorta CTA o error desaparece | `frontend/e2e/responsive-global.spec.ts` |
| P06/N06 | Accesibilidad | Teclado, aria y estados legibles | Foco perdido al cerrar o error solo color | `frontend/e2e/m10-ui-ux-accesibilidad.spec.ts` |
| P07/N07 | API / validacion | Error seguro con estado apropiado | Stacktrace o dato interno en respuesta | `backend/tests/Solqaryn.Tests/API/C00M0306SafeErrorsTests.cs` |
| P08/N08 | Gobernanza CI | Commit exact-SHA y tests relevantes completados con artefactos | SKIPPED/CANCELLED/QUEUED/FAIL contado aprobado | `scripts/verify-project-scope.mjs` |

Una aceptacion real deberá correlacionar cada caso con el servicio backend, version de datos, permisos JWT/RBAC, tenant A/B, navegador, red, viewport y rollback, no solo UI verde. Pruebas negativas de otro tenant nunca deberán compartir secretos/PII reales en videos o artifacts. Los escenarios aquí no incluyen comandos ejecutados ni resultados medidos.

## 5. Criterio de trazabilidad antes de declarar PASS en fases posteriores

Matriz futura `ID -> modulo/ruta/API -> owner -> prerequisito -> caso positivo -> caso negativo -> fixture sintetico -> SHA -> test real -> evidencia -> limitacion -> rollback -> revisor`. Pruebas `queued/in_progress/skipped/cancelled/failed` requeridas no cumplen PASS; un test de backend que no corresponde a un doc no tiene que bloquearlo, pero debe marcarse NO_APLICA y nunca convertido a aprobado. Configuracion por tenant no sustituye membresia/propiedad verificada del servidor.

El cierre M01.06 es un gate terminal separado que requiere prerequisitos y evidencia; no se libera porque este inventario esta en LISTO_DEV. M03 de implementacion sigue siendo codigo, no texto. Promoción a QA/main/PROD requiere autorizacion expresa distinta.

## 6. Auditar esta entrega y rollback

- **Impacto:** solo nuevo Markdown `docs/v5/c14-01-m01-01-alcance-no-regresion-aceptacion-funcional.md` y seccion autocontenida `CHANGELOG_AI.md`. Cero API/DTO, entidades, migraciones, UI, runtime, roles o secretos modificados.
- **A0:** contar 14 NR y fuentes Git 10, sin encabezados vacios, sin credenciales; controlar diff exacto.
- **A1 documental:** verificar objetivo/exclusiones, trazabilidad y ocho pares P/N como contratos, no resultados.
- **A2 documental:** revisar fronteras JWT/RBAC/tenant A/B, PII, pagos y reversibilidad, sin probar seguridad runtime.
- **A3 documental:** cotejar rutas, pruebas fuente y contrato de pruebas futuras, sin atribuir PASS a .spec.
- **A4:** publicar fast-forward con CAS del HEAD actual y verificar `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` exact-SHA; otras suites no causales de Markdown = NO_APLICA con motivo.
- **A5:** otra Task Javier distinta de `6af16bbad26881918c283abe06e1f32a`; no autoaprobar ni autorizar despliegue.
- **Rollback:** `git revert` del commit exacto en coordinación con HEAD concurrente, sin force-push ni reescribir BITACORA.
