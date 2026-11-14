# SOLQARYN V5 FINAL — C04-03.M01.03: inventario de código, contratos y evidencia de metering

**Resultado exigido:** lectura de contratos, código y evidencias sin asumir certificación. **Responsable:** PRIMARY DEV 03 / JAVIER_CHATGPT / `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Ámbito:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT. **Fecha:** 2026-11-14. **Clase:** análisis documental, no código de medición ni pruebas runtime nuevas.

**Autoridades:** [Documento V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). Preflight: `PLAN!H652=PENDIENTE/I652=vacío`, `COLA!G/O652=SI/SI` y `DAG!F613/G613=VALIDADO/SI` al reclamar, con los sesenta antecedentes de preparación C01/C02/C03 M01.01–05 LISTO; las entregas previas M01.01 y M01.02 de C04-03 ya LISTO_DEV (A5 pendiente). La preparación no autoriza M01.06 ni M02, nuevas tablas, auditoría de consumo, migraciones o QA.

## 1. Inventario de fuentes inspeccionadas en dev, con evidencia limitada

| ID | Archivo Git | Blob verificado | Evidencia de lectura (no de funcionamiento live) | Brecha específica |
| --- | --- | --- | --- | --- |
| INV01 | `backend/src/Domain/Entities/SuscripcionesSaaS.cs` | `d9b5dbe19fb728293b00b748f10ac10336debd2d` | Entidades `Plan`, `PlanLimite`, `Suscripcion`; `DefinirLimite` rechaza cero/negativo y usa null para ilimitado; estado suscripción | No contiene por sí solo medidor de uso ni eventos por periodo |
| INV02 | `backend/src/Application/Services/SuscripcionesSaaSService.cs` | `bb501285f11cd4b32709a58a2c51efa19c3664ea` | Verifica tenant en Application, onboarding con clave idempotente/replay y operaciones de límites | Idempotencia de onboarding NO equivale a ledger de metering |
| INV03 | `backend/src/Application/DTOs/SuscripcionesSaaSApiContracts.cs` | `6045a167fb90fe3dea2fa87280d0a29cec57c6be` | DTOs de onboarding/suscripción/límite/entitlement y consulta normalizada | No expone contrato versionado de contador/consumo |
| INV04 | `backend/src/API/Controllers/SuscripcionesSaaSController.cs` | `cab95db5a33c6a15448e2437481eaaac25ffc4fe` | `[Authorize]` y rutas `onboarding`, `suscripcion`, `limites`; cabecera Idempotency-Key para onboarding | No prueba autorización HTTP tenant A/B del futuro metering |
| INV05 | `backend/src/Infrastructure/Repositories/SuscripcionesSaaSRepository.cs` | `f3340ca687867d6dc2d99dfff5452bbe5c9bbb79` | Filtro de suscripción vigente por EmpresaId; ledger de idempotencia durable ligado a onboarding de suscripción | NO es evidencia de deduplicación de consumo, bytes o archivos |
| INV06 | `backend/src/Infrastructure/Persistence/Configurations/SuscripcionesSaaSConfiguration.cs` | `16631a0155a449ab71e7479c92e8ba8804303249` | EF mapea Planes/PlanLimites/Suscripciones, índices/ownership de claves y relación EmpresaId | Mapeo no acredita migración aplicada, almacenaje de eventos ni MySQL productivo |
| INV07 | `backend/tests/Solqaryn.Tests/SuscripcionesSaaSServiceTests.cs` | `090bf81e7a8f08e86dcdc7ddf5b0c20625ae2839` | Tests xUnit con Moq definidos para tenant inválido, replay de onboarding y conflicto de payload | Leer tests no es ejecutarlos ni cubrir contadores/idempotencia futura |
| INV08 | `frontend/src/app/services/suscripcion-saas.service.ts` | `b3883c10afce06a39bdf4d0dec356a553f8aa32f` | HttpClient consulta suscripción/límites y manda Idempotency-Key para onboarding | Validación de UI no es permiso ni evento medido |
| INV09 | `frontend/src/app/features/configuracion/suscripcion-saas-card.component.ts` | `7909433c414089b89b87ebff20017c2704a7f085` | Presenta plan, estado, límites, loading/error y `valorMaximo ?? 'Sin límite'` | Mostrar límite no representa uso real ni prueba Playwright |
| INV10 | [C04-03.M01.01 alcance](c04-03-m01-01-alcance-metering-no-regresion.md) | `9f84047a310a558f1266052eeab545fd6a44088f` | Especifica objetivo de metering, NR01–NR14, P01–P05 y N01–N12 | Son criterios NO ejecutados en runtime |
| INV11 | [C04-03.M01.02 interfaces](c04-03-m01-02-actores-entradas-salidas-metering.md) | `d9702754c2094255a3d564841688a9546fdf690f` | Documenta 10 actores, 14 fronteras y oráculos futuros | No crea adaptador, API ni servicio |

Cada SHA es el hash de un **blob real en dev**, no el SHA de CI o confirmación de migración. Este inventario acota sus afirmaciones a las once fuentes enumeradas: no prueba la ausencia global de otro código ni declara una vulnerabilidad basada exclusivamente en inspección parcial.

## 2. Matriz de cobertura y test pendientes (positivo/negativo)

| Caso | Signo | Evidencia observada | Prueba causal que todavía se exige |
| --- | --- | --- | --- |
| P01 | + | `PlanLimite` normaliza clave y guarda límite positivo o null | Test medidor: unidad definida y periodo/tenant correctos |
| P02 | + | `SuscripcionesSaaSService` resuelve tenant servidor antes de onboarding | Test HTTP+MySQL tenant A válido para medidor futuro |
| P03 | + | Repository tiene ledger de idempotencia de onboarding | Test de evento de **consumo** idempotente, distinto del onboarding |
| P04 | + | UI tipada muestra límites con estado de carga | Test de nueva UI de **uso** con datos reales/cero/sin permiso |
| P05 | + | EF mapea Suscripciones por empresa | Test MySQL efímero y conciliación de conteos por tenant |
| N01 | - | `EmpresaId` de suscripción se vincula en dominio/repo | Intento A→B con body/header/ruta manipulado: rechazo |
| N02 | - | Idempotency-Key solo se valida para onboarding observado | Dos eventos de consumo idénticos concurrentes: una cuenta, sin duplicados |
| N03 | - | UI Angular muestra `Sin límite` al ver null | El cliente NO concede permisos adicionales por `null` o clave vacía |
| N04 | - | Rutas con `[Authorize]` disponibles | Sin RBAC/tenant suficiente, API nueva rechaza 403/404 |
| N05 | - | Suscripción conoce Activa/Suspendida/Cancelada/Expirada | Estado no activo no se trata como ilimitado sin regla aprobada |
| N06 | - | Lectura de mapping EF y repository | No afirmar MySQL real/migración aplicada basándose en blobs |
| N07 | - | Tests xUnit con mocks inspeccionados | No atribuir PASS runtime, aislamiento A/B o proveedor sin ejecutar CI pertinente |
| N08 | - | Fuentes de plan y límites sin ledger de metering acreditado | No generar factura, precio, cargo, worker ni cron desde esta documentación |
| N09 | - | Interfaces futuras no expuestas en controladores inspeccionados | Idempotencia de onboarding no se usa para falsificar ledger de consumo |
| N10 | - | Code review documental, sin evento ejecutado | Tokens, secretos y PII no se almacenan al emitir evento futuro |
| N11 | - | Cambio a Markdown únicamente | No confundir revisión del documento con prueba de .NET, MySQL o Playwright |
| N12 | - | PR/Task históricos fuera de fuente ejecutable V5 | No crear backlog nuevo, permisos QA/main/PROD o bypass de Scope Lock |

Estas P01–P05 y N01–N12 son escenarios de diseño y **no se ejecutaron**. No se verifica dentro de M01.03 que una autorización HTTP real o una deduplicación MySQL de metering funcionen.

## 3. Brechas técnicas priorizables sin inventar la solución

**BRECHA-01:** aún no se acreditó ledger/evento de metering por tenant, fuente y periodo (fronteras: Domain/Application/Infrastructure/MySQL). **BRECHA-02:** sin prueba dirigida de replay/concurrencia de **consumo**, aunque el onboarding tenga idempotencia propia. **BRECHA-03:** no se aprobó un catálogo de métricas, unidades y ventanas (usuarios, operaciones, bytes, archivos) ni reglas de reinicio de periodo. **BRECHA-04:** no se acreditan tests A/B de lectura de consumo, errores y redacción de PII de esa superficie futura. **BRECHA-05:** PlanLimite y UI muestran límites, no una proyección/conciliación entre uso y cuota. **BRECHA-06:** políticas de retención, tratamiento de datos, precios y pagos no se aprueban aquí.

La consecuencia de cada brecha es requerir una microtarea y evidencia independientes en la fase correcta, con tests de .NET/EF/MySQL/seguridad/Angular cuando el alcance realmente introduzca código. No añadir tablas/eventos/Endpoints por inferencia.

## 4. A0–A4, A5 y rollback

A0: comparar 11 blobs, chequear diff documental, no secretos y procedencia CHANGELOG. A1: 5 escenarios P y 12 negativos como contrato falsable, no runtime; A2: preservar tenant server-side, JWT/RBAC, privacidad y secretos; A3: coherencia entre M01.01, M01.02, fuentes y DAG/PLAN/COLA sin alterar consumidores; A4: commit fast-forward en dev, checks Scope Lock y procedencia V5 completed/success exact-SHA, readback institucional PLAN/COLA/BITACORA A:Z. A5 únicamente otra de las cinco Tasks Javier distinta del autor.

No se modifica runtime: `dotnet/npm/MySQL/Playwright=NO_APLICA` para **esta entrega** y nunca se contabilizan PASS no ejecutados. Un cierre documental no habilita M01.06/M02, cobro, IAM, QA/main/PROD, nueva infraestructura ni cambio de proveedores. Rollback `git revert <SHA>` ordinario y evento compensatorio nuevo, sin borrar historial/force-push.
