# SOLQARYN V5 — C04-02.M01.03: inventario de contratos, código y evidencia disponible

**Autor:** SOLQARYN PRIMARY DEV 04 — JAVIER (`6aeacb7820ac8191b7d768a6f33dfa09`). **Fecha:** 2026-11-14. **Alcance:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Naturaleza:** análisis documental M01.03; no implementa una función ni certifica una operación financiera.

## 1. Autoridad, propósito y corte verificable

Documento único autorizado: `STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES`, Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. `PLAN_MAESTRO!A616`: `C04-02.M01.03`, «Leer contratos, código y evidencia disponibles sin asumir certificación». Bloque `C04-02 — Contratos y suscripciones`: objetivo futuro de alta, renovación, pausa, cancelación, morosidad y cierre contractual. El inventario no infiere que una suscripción SaaS actual equivalga a un contrato pagado.

Antes de reclamar este ID: PLAN H616=PENDIENTE/I616 vacía, COLA G616/O616=SI, DAG F577=VALIDADO/G577=SI y Scope Lock exacto previo `9020c778618837c11b5f0911a769092b399950f1` completed/success. Después del CAS, la fuente de verdad de estado sigue siendo PLAN; este documento solo inventaría artefactos de código. Los documentos `c04-02-m01-01-objetivo-no-alcance-contratos-suscripciones.md` y `c04-02-m01-02-actores-entradas-salidas-superficies-contractuales.md` establecen fronteras, no activaciones de cobro o nuevas rutas.

## 2. Inventario autenticado por blob: doce archivos leídos en dev

| Capa | Archivo | Blob Git inspeccionado | Contrato o comportamiento visible |
| --- | --- | --- | --- |
| Domain | `backend/src/Domain/Entities/SuscripcionesSaaS.cs` | `d9b5dbe19fb728293b00b748f10ac10336debd2d` | `Plan`, `PlanLimite`, `Suscripcion`; estados Activa/Suspendida/Cancelada/Expirada; `Suspender`, `Reactivar`, `Cancelar`, `EsVigenteEn` |
| Application | `backend/src/Application/Services/SuscripcionesSaaSService.cs` | `bb501285f11cd4b32709a58a2c51efa19c3664ea` | Verificación `ResolverTenantAsync` antes de consulta; onboarding idempotente; manejo de colisión y replay; DTO de vigencia/entitlements |
| Contracts | `backend/src/Application/DTOs/SuscripcionesSaaSApiContracts.cs` | `6045a167fb90fe3dea2fa87280d0a29cec57c6be` | Request `PlanCodigo` y `InicioUtc`; DTO de estado y fechas; error codes; límites paginados |
| Application ports | `backend/src/Application/Interfaces/ISuscripcionesSaaSService.cs` | `ee320815a588ff28a6c60bf3d9634991a050e6a1` | Interfaces service/repository con `EmpresaId` que debe estar verificado server-side |
| HTTP API | `backend/src/API/Controllers/SuscripcionesSaaSController.cs` | `cab95db5a33c6a15448e2437481eaaac25ffc4fe` | `[Authorize]`, `[RequierePermiso]`; onboarding, suscripcion, limites y entitlement; no ruta de renovaciones/cobros |
| Repository | `backend/src/Infrastructure/Repositories/SuscripcionesSaaSRepository.cs` | `f3340ca687867d6dc2d99dfff5452bbe5c9bbb79` | EF/repository con filtrado `EmpresaId` y ledger de idempotencia y persistencia transaccional |
| EF mapping | `backend/src/Infrastructure/Persistence/Configurations/SuscripcionesSaaSConfiguration.cs` | `16631a0155a449ab71e7479c92e8ba8804303249` | Entidades e índices de código y suscripción por tenant/estado/fechas; no autorización a nuevas migraciones |
| EF snapshot overlay | `backend/src/Infrastructure/Migrations/AppDbContextModelSnapshot.N69C.cs` | `81d843b215fcce36d60ec2e446eb5576f3800b31` | Modelo relacional versionado presente en código, no verificación de DB/restore real |
| Unit tests | `backend/tests/Solqaryn.Tests/SuscripcionesSaaSServiceTests.cs` | `090bf81e7a8f08e86dcdc7ddf5b0c20625ae2839` | Seis `[Fact]` xUnit/Moq para tenant denegado, replay, payload conflicto, alta verificada y colisiones concurrentes |
| Angular service | `frontend/src/app/services/suscripcion-saas.service.ts` | `b3883c10afce06a39bdf4d0dec356a553f8aa32f` | Cliente `HttpClient` para operaciones SaaS y header idempotente; no control backend desde UI |
| Angular models | `frontend/src/app/core/models/suscripcion-saas.model.ts` | `efbd1c24ffd12e2475645d21553cc70451dd92cc` | Interfaces TS de suscripción, límites, entitlement y onboarding; no precio/moneda/periodicidad |
| Angular UI | `frontend/src/app/features/configuracion/suscripcion-saas-card.component.ts` | `7909433c414089b89b87ebff20017c2704a7f085` | Component con Signals, formularios y Material para suscripción; lectura no certifica accesibilidad E2E |

Los blob SHA son identidad del **archivo leído**, no prueba de que el código compiló, corrió o fue desplegado durante esta microtarea. Si algún blob cambia, los consumidores deben releer el HEAD y la evidencia causal. Estos archivos pertenecen al repo original, no a mirrors ni planes archivados.

## 3. Capacidades verificables por lectura y límites exactos

| Necesidad del bloque C04-02 | Evidencia presente | Brecha abierta, sin inferencias |
| --- | --- | --- |
| Alta tenant-aware | Controller protegida, `ResolverTenantAsync`, contrato `OnboardingSuscripcionSaaSRequest` y repository | Un onboarding SaaS no prueba contrato legal firmado, cobro, moneda ni periodicidad |
| Estado y vigencia | `EstadoSuscripcion`, constructor UTC y métodos del agregado Domain | No existe en estos artefactos una máquina comercial de morosidad/renovación ni un ledger contractual completo |
| Suspensión/reactivación | Métodos Domain con preestados `Activa` y `Suspendida` | No se observó API contractual específica, rol aprobado ni política de mora |
| Cancelación | `Cancelar(canceladaUtc)` guarda fin UTC y controla inicio | No demuestra reembolso, terminación legal, cierre económico ni notificación al cliente |
| Idempotencia | Header y ledger de repository; seis tests Moq definen casuística | Test mock ≠ transacciones/colisión comprobadas con MySQL efímero en esta M01 |
| Seguridad A/B | Servicio valida membresía server-side; API exige autorización y permisos | Código leído ≠ JWT/claims y aislamiento multiempresa extremo a extremo probado en esta corrida |
| Datos contractuales | EF configuración, índices y snapshot | Esquema presente en código ≠ migración aplicada, rollback SQL ejecutado o contabilidad consistente |
| Experiencia | Angular service/model/card de suscripción | No hay en los DTO leídos precio, impuesto, renovación o morosidad; a11y/E2E no certificados |

## 4. Hallazgos, riesgos y decisiones que NO pueden convertirse en PASS

- **H01, alcance:** `Suscripcion` conserva estados técnicos; falta un contrato explícito versionado para transiciones económicas de renovación/mora, condiciones legales y aceptación contractual. No se concluye inexistencia en todo el repositorio, solo ausencia de prueba en esta muestra.
- **H02, autorización:** un `Plan` y entitlement no conceden RBAC. El `empresaId` de la ruta es intención, no autoridad. Futuras transiciones exigirán privilegio de acción y membresía vigente.
- **H03, concurrencia:** el ledger existente promete replay para onboarding, pero el comportamiento de una futura renovación o doble cobro debe diseñarse y probarse con MySQL efímero y transacciones propias.
- **H04, ciclo económico:** no hay definición comercial aprobada de precio, moneda, impuesto, vencimiento, días de gracia, mora, cálculo de prorrateo o cancelación con reembolso; prohibido inventar decisiones.
- **H05, compatibilidad:** cualquier nuevo estado/DTO de contrato debe contemplar clientes Angular existentes y preservar suscripciones históricas sin reescritura destructiva.
- **H06, pruebas:** ver métodos o leer seis xUnit/Moq no significa que esos tests se ejecutaron para este Markdown; cualquier cambio de runtime futuro requiere GitHub Actions exact-SHA causal y, según la capa, MySQL, seguridad A/B y E2E.

## 5. Contrastes documentales positivos y negativos (no tests runtime)

**P01:** verificar que `Suscripcion` mantiene `EmpresaId`, `PlanId`, fecha UTC y estado. **P02:** localizar `Suspender`/ `Reactivar` con preestado. **P03:** identificar `Cancelar` y la condición `FinUtc` válida. **P04:** comprobar `ResolverTenantAsync` antes de I/O, API auth y permisos. **P05:** localizar tests xUnit de replay y su límite mock. **P06:** localizar índices EF y tipos Angular existentes.

**N01:** no inferir autorización de `empresaId` UI. **N02:** no confundir entitlement con rol administrativo. **N03:** no considerar vigencia `Activa` como pago acreditado. **N04:** no inventar precio, moneda, mora ni periodicidad. **N05:** no tratar los mocks como prueba real de MySQL A/B. **N06:** no tratar snapshot EF como migración desplegada. **N07:** no afirmar nueva API de cancelación/renovación observando métodos Domain. **N08:** no considerar renders Angular como prueba Playwright. **N09:** no desbloquear M01.06/M02 con un documento preliminar. **N10:** no promover QA/main/PROD ni cargar datos productivos por un resultado de CI documental.

Estos P/N son un checklist de coherencia **documental** y nunca se registran como ejecución de HTTP, .NET, SQL o Playwright. Su uso futuro exige nuevos tests y gates causalmente pertinentes.

## 6. Impacto, cierre, trazabilidad y rollback

Se modifica exclusivamente este Markdown y una sección íntegra de `CHANGELOG_AI.md`. No se toca infraestructura, entidades, servicios, repositorios, frontend, API, secrets, tareas, base de datos o datos. Las consumidoras futuras M01.04–.06/M02–M06 deben releer cada código SHA, confirmar pruebas y no sustituir su ingeniería por este inventario.

**A0:** fuente y doce blobs leídos. **A1:** seis contrastes positivos del inventario documentados. **A2:** diez negativos sobre tenant/JWT/RBAC, estados, idempotencia y no-regresión listados, sin conceder permisos. **A3:** consistencia con fuente comercial V5, M01.01/02 y arquitectura actual. **A4:** GitHub commit fast-forward y diff sólo Markdown/changelog, Scope Lock y procedencia V5 `completed/success` del SHA exacto, BITACORA 26 columnas y PLAN/COLA readback. CI .NET, MySQL, Angular/E2E no aplica al diff Markdown y no se cuenta como PASS.

**A5:** revisión por otra Task Javier distinta del autor; nunca autoaprobación. **Rollback:** `git revert` normal de este commit documental; sin force-push ni reescribir BITACORA. No se certifican renovaciones, cobros, morosidad, eventos, migraciones, QA/main/PROD ni condiciones financieras.
