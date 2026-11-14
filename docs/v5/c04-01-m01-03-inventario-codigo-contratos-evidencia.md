# SOLQARYN V5 — C04-01.M01.03: inventario de código, contratos y evidencia SaaS

**Microtarea:** `C04-01.M01.03` (PLAN_MAESTRO fila 580). **Responsable:** PRIMARY DEV 02 — JAVIER, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Fecha:** 2026-11-14. **Repositorio:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Tipo:** lectura e inventario documental, sin implementar cambios runtime ni certificar funciones nuevas.

## 1. Fuente autorizada y diferencia entre existencia y certificación

Fuente ejecutable: `STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES` V5 FINAL, Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`, `PLAN_MAESTRO/COLA/DAG_TECNICO`. Para M01.03, preflight comprobó `COLA!G580/O580=SI`, `DAG!F541=VALIDADO/G541=SI` y owner vacío antes de claim CAS.

**Alcance concreto:** leer contratos, código y evidencia disponibles para el futuro catálogo de planes C04-01 y señalar las brechas de versionado/precios/upgrade; ninguna lectura certifica el ambiente ni sustituye pruebas CI del commit de implementación. Cualquier documento de backlog histórico (por ejemplo un preflight de N6.9 para otro repo/rama) es evidencia contextual antigua, **nunca autorización ni inventario actual**.

## 2. Inventario autenticado de archivos vigentes y blobs

| Capa | Archivo inspeccionado en dev | Blob Git del preflight | Evidencia limitada derivada de la lectura |
| --- | --- | --- | --- |
| Domain | `backend/src/Domain/Entities/SuscripcionesSaaS.cs` | `d9b5dbe19fb728293b00b748f10ac10336debd2d` | `Plan`, `PlanLimite`, `Suscripcion` y reglas de estados/UTC; `Plan` global, asignación por `Suscripcion.EmpresaId` |
| Application | `backend/src/Application/Services/SuscripcionesSaaSService.cs` | `bb501285f11cd4b32709a58a2c51efa19c3664ea` | `ResolverTenantAsync` antes de operaciones; onboard con replay idempotente y conflicto explícito |
| Contratos | `backend/src/Application/DTOs/SuscripcionesSaaSApiContracts.cs` | `6045a167fb90fe3dea2fa87280d0a29cec57c6be` | DTO para onboarding, suscripción, límites, entitlement; error codes y paginación 1–100 |
| Puertos | `backend/src/Application/Interfaces/ISuscripcionesSaaSService.cs` | `ee320815a588ff28a6c60bf3d9634991a050e6a1` | Interfaces de casos y repositorio; parámetros de empresa se someten a scope server-side |
| API | `backend/src/API/Controllers/SuscripcionesSaaSController.cs` | `cab95db5a33c6a15448e2437481eaaac25ffc4fe` | `[Authorize]`, filtros `[RequierePermiso]`, rutas `saas/tenants/{empresaId}`; no permiso global de catálogo |
| Infrastructure | `backend/src/Infrastructure/Repositories/SuscripcionesSaaSRepository.cs` | `f3340ca687867d6dc2d99dfff5452bbe5c9bbb79` | Filtros `EmpresaId`, búsqueda de planes activos, ledger durable de idempotencia en operación transaccional |
| EF mapping | `backend/src/Infrastructure/Persistence/Configurations/SuscripcionesSaaSConfiguration.cs` | `16631a0155a449ab71e7479c92e8ba8804303249` | `Planes` con clave de código única, `PlanLimites`, índices de `Suscripciones` por empresa, restricción de borrado |
| EF snapshot | `backend/src/Infrastructure/Migrations/AppDbContextModelSnapshot.N69C.cs` | `81d843b215fcce36d60ec2e446eb5576f3800b31` | Overlay de snapshot de Plan/Suscripcion/PlanModulo; lectura **no prueba migración aplicada ni conectividad MySQL** |
| Tests | `backend/tests/Solqaryn.Tests/SuscripcionesSaaSServiceTests.cs` | `090bf81e7a8f08e86dcdc7ddf5b0c20625ae2839` | Se observan casos xUnit con Moq para tenant no verificado, replay idempotente y payload conflictivo; **no se ejecutaron aquí** |
| Angular service | `frontend/src/app/services/suscripcion-saas.service.ts` | `b3883c10afce06a39bdf4d0dec356a553f8aa32f` | Cliente tipado: lectura, límites, entitlement y onboarding con header; validación local no es permiso |
| Angular model | `frontend/src/app/core/models/suscripcion-saas.model.ts` | `efbd1c24ffd12e2475645d21553cc70451dd92cc` | Interfaces de datos SaaS; **no incluyen contrato de precio/moneda/versionado comercial** |
| Angular UI | `frontend/src/app/features/configuracion/suscripcion-saas-card.component.ts` | `7909433c414089b89b87ebff20017c2704a7f085` | Card de plan, estado y módulos; a11y/loading/error observables en plantilla, sin certificación Playwright |

Este inventario es un **corte de blobs**, no una garantía de que cada archivo permanecerá igual en commits futuros. Toda implementación posterior debe contrastar HEAD y diffs propios y no suponer que tests históricos siguen pasando.

## 3. Capacidades existentes y su grado de evidencia

| Función | Nivel de evidencia en este alcance | Qué permanece sin demostrar |
| --- | --- | --- |
| Catálogo básico de plan y límites | Declaraciones Domain y configuración EF visibles | Precios/monedas/periodicidad/versiones comerciales; efectividad en DB productiva |
| Alta de suscripción SaaS | Código HTTP/Application e interfaz inspeccionados | Test HTTP real concurrente y autorización JWT end-to-end en esta M01 |
| Entitlement por módulo | DTO y ruta protegida observados | No sustituir RBAC; no se certifica un catálogo nuevo por este inventario |
| Límite multiempresa | Resolver tenant, filtros y casos unitarios vistos | Integración real A/B con MySQL/claims en entorno vivo; revisión de futuros cambios |
| Idempotencia | Clave obligatoria en API, ledger en repository, casos de replay | Resistencia bajo concurrencia real de MySQL/EF en este corte |
| UX de suscripción | Angular service/model/card observables | Tests E2E, accesibilidad completa y criterios de visualización de precios futuros |
| Migraciones/versiones | Configuraciones y snapshot parciales presentes | Migración ejecutada; rollback ensayado; compatibilidad comercial de tarifa versionada |

**Hallazgo H01:** el código inspeccionado demuestra un catálogo inicial (Plan/PlanLimite) y una suscripción tenant-aware, pero el contrato de negocio futuro de **precio/versionado/periodicidad/prorrateo** no está acreditado por esos DTO y entidades. No inferir ausencia absoluta en todo el repositorio a partir de esta muestra; registrar la brecha para M02.

**Hallazgo H02:** EF mappings y snapshot son evidencia de diseño del modelo, no del estado MySQL real. Cualquier migración C04-01 requiere suite CI efímera y revisión reversible antes de ejecutarse.

**Hallazgo H03:** xUnit con Moq es valioso para reglas Application, pero no demuestra por sí solo que un rol JWT, middleware API, MySQL, Playwright o aislamiento de datos productivos haya sido probado en esta corrida.

**Hallazgo H04:** el cliente Angular no puede actuar como fuente de verdad para precio, entitlement ni membership: backend debe volver a validar identidad, RBAC, límites y vigencia.

**Hallazgo H05:** no se verificó una definición fiscal/comercial aprobada de moneda, precio, impuesto o pasarela. Ninguna puede añadirse por conjetura.

## 4. Contraste con requisitos V5 y decisiones no autorizadas

| Requisito futuro C04-01 | Artefacto observado reutilizable | Puerta pendiente |
| --- | --- | --- |
| Plan versionado, reglas y capacidades | `Plan`, `PlanLimite` | Definir modelo y semántica de versión en M02, revisión de consumidores |
| Precio, moneda, periodicidad | No probados por los contratos observados | Decisión comercial aprobada; prohibido inventar importes |
| Upgrade/downgrade | Estados y cambio de plan en `Suscripcion` | Semántica de vigencia, prorrateo, autoría, reversibilidad y tests |
| Límites tenant server-side | Service y repo existentes | Pruebas dirigida A/B, revocación, límites y conflictos de replay |
| Experiencia UX accesible | Card y service Angular | UX de monedas/estados y pruebas de accesibilidad reales |
| Observabilidad y auditoría | `IAuditoriaService` usado en Application | Reglas anti-PII, idempotencia, retención y pruebas posteriores |

## 5. Escenarios de contraste DOCUMENTAL, no tests ejecutados

- **P01:** encontrar Domain `Plan` y límite por clave; registrar si su semántica conserva null/positivo.
- **P02:** verificar que Application resuelve tenant antes de I/O y Repository usa `EmpresaId` para suscripciones.
- **P03:** identificar endpoint protegido y DTO de lectura/paginación sin autorización implícita del cliente.
- **P04:** identificar test xUnit que reclama rechazo por tenant no verificado y replay sin nueva escritura.
- **P05:** distinguir mapping EF y snapshot de migración ejecutada real.
- **N01:** ausencia de permiso/tenant no puede convertirse en acceso por entitlement UI.
- **N02:** reusar una idempotency key con distinto payload debe ser conflicto, no éxito silencioso.
- **N03:** tarifas, impuestos, periodos y precios no publicados no se suponen válidos.
- **N04:** código leído ≠ tests PASS del SHA actual.
- **N05:** snapshot EF leído ≠ DB migrada ni rollback funcional.
- **N06:** test mock leído ≠ integración MySQL A/B.
- **N07:** componente Angular leído ≠ accesibilidad E2E verificada.
- **N08:** documento histórico de preflight ≠ fuente operativa actual.
- **N09:** precio contratado no concede permiso de administración.
- **N10:** un nuevo gate de fase no se libera solamente por documento M01.03.

Los puntos P/N son **comprobaciones de coherencia del inventario**; no declarar tests ejecutables PASSED de una función comercial que no se modificó.

## 6. Impacto, rollback y condición de cierre

**Impacto de este changeset:** solo `docs/v5/c04-01-m01-03-inventario-codigo-contratos-evidencia.md` y sección íntegra de `CHANGELOG_AI.md`; ninguna API, DB, DTO, clase, worker, secreto, configuración, automatización, dominio o estado productivo cambia. Consumidores posteriores de este inventario son las microtareas M02/M03 de C04-01, que deberán revalidar SHA/diff y alcance real.

**A0–A4 documental:** identidad y diferencias verificadas; coherencia P/N e invariantes JWT/RBAC/tenant; trazabilidad a blobs y Plan V5; publicación fast-forward bajo CAS y check-runs obligatorios `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` completados en SUCCESS sobre SHA publicado. Compilaciones o pruebas generales aún en progreso son `NO_APLICA` para Markdown y jamás se anotan como PASS.

**A5:** independiente por otro TASK_ID Javier; conserva autoría original. **Rollback de esta entrega:** git revert ordinario del commit documental. No revertir datos comerciales ni usar force-push. No habilitar M01.06, M02, JWT global, migración, cobro, nuevas Tasks, QA/main/PROD o producción.
