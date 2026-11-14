# SOLQARYN V5 FINAL — C04-03.M01.01: objetivo, exclusiones y no-regresión del metering SaaS

**Entrega:** preparación documental exclusivamente de `C04-03.M01.01`. **Autor:** PRIMARY DEV 03, JAVIER_CHATGPT, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Fecha:** 2026-11-14. **Repositorio y alcance:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. No se crea ni activa una medición real, una cuenta de cobro ni un servicio.

**Fuente única:** [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit) y Skill/Scope Lock canónicos. Resultado de fase exigido: futura medición verificable de usuarios, almacenamiento, operaciones, archivos y uso por tenant, sustentada en eventos idempotentes, límites y conciliación. Esta entrega M01.01 define únicamente el objetivo, no-alcance y no-regresión. No equivale a M01 completa, M02, M03, auditoría runtime ni autorización comercial.

## 1. Objetivo del producto futuro y éxito verificable

El futuro metering debe medir **consumos atribuibles a un tenant verificado en el servidor**, con unidad, intervalo, origen, operación, correlación y clave idempotente por evento, y permitir una conciliación determinista entre una fuente autorizada de uso y una proyección de consumo. Los límites deben expresarse por métrica con unidades y reglas de conteo inequívocas; entitlement y RBAC son controles distintos. Los datos de medición deben ser mínimos, trazables, reversibles cuando exista rectificación autorizada y no legibles por tenants ajenos. La disponibilidad de un `PlanLimite` **no prueba** que existan contadores, eventos, cobros, cuotas ejecutadas o retención aprobada.

El éxito de una etapa ejecutable posterior exigiría pruebas de duplicado/replay, concurrencia, fecha de corte, cambio de plan, suspensión/cancelación, tenant A/B, fuente de almacenamiento, reconcile y falla de persistencia en CI dirigida del SHA exacto; sin esos tests no afirmar consumo real. Una nueva política comercial o de retención requiere decisión de autoridad correspondiente, no inferencia a partir de la app.

## 2. Baseline visible en dev: hechos, no certificación

| Fuente del proyecto | Blob inspeccionado | Observación acotada | Falta acreditar |
| --- | --- | --- | --- |
| `backend/src/Domain/Entities/SuscripcionesSaaS.cs` | `d9b5dbe19fb728293b00b748f10ac10336debd2d` | `Plan`, `PlanLimite`, `Suscripcion`; límite nullable, plan ligado por suscripción a EmpresaId, estados Activa/Suspendida/Cancelada/Expirada | Eventos medidos, persistencia de contadores, tiempo efectivo, política de límite |
| `backend/src/Application/Services/SuscripcionesSaaSService.cs` | `bb501285f11cd4b32709a58a2c51efa19c3664ea` | Flujo de suscripciones con resolución de tenant servidor y replay/idempotencia para onboarding | Que esta idempotencia cubra consumo, conciliación o cobros; no se infiere |
| `backend/src/Application/DTOs/SuscripcionesSaaSApiContracts.cs` | `6045a167fb90fe3dea2fa87280d0a29cec57c6be` | Contratos `LimiteSuscripcionSaaSDto` y `EntitlementModuloSaaSDto` para lectura y reglas existentes | DTO de evento de metering, facturación o lectura de consumo |
| `docs/v5/c04-01-m01-03-inventario-codigo-contratos-evidencia.md` | `1d68214ba00a23842d88a9c9549cecb25ab69e11` | Inventario documental del catálogo SaaS y límites; señala carencias de versiones y tarifas comerciales | Catálogo versionado aprobado, engine de eventos, precios o conciliación ejecutada |

Cada SHA de la tabla identifica un **blob de fuente**, no un resultado de integración/CI ni una prueba de operación. La arquitectura vigente utiliza .NET 10, Angular 22.2.1, EF Core/MySQL, JWT y RBAC con aislamiento tenant. El metering posterior debe preservar las capas Domain/Application/Infrastructure/API y no aceptar autoridad delegada desde Angular.

## 3. Alcance M01.01 y exclusiones expresas

**Sí entrega:** descripción de quién medirá, qué se considerará consumo, unidades y dimensión temporal por definir, datos mínimos de evidencias, precondiciones de autorización, indicadores de error, matriz falsable de no-regresión y límites de futuras pruebas.

**No entrega ni autoriza:** implementación de tabla ledger, cambios MySQL/EF, endpoints, jobs, workers, webhooks, triggers, eventos dispatch entre Tasks, cotizaciones, tarifas, cobros, facturas, modificación de suscripciones, credenciales, modificación de tenants, suspensión de cuentas, acceso privilegiado, QA/main/PROD, DNS, cloud, infraestructura real o uso de datos productivos. Tampoco se eligen políticas definitivas de retención, moneda, impuestos, facturación o prorrateo. Los requerimientos relacionados con datos personales, cuotas, billetes o exportación necesitarán permisos y evaluación independientes.

## 4. Invariantes de no-regresión

| ID | Invariante exigida en la implementación futura | Señal negativa a bloquear |
| --- | --- | --- |
| NR01 | Tenant efectivo proveniente de identidad/membresía verificada en backend | `EmpresaId` de header, URL, JSON, cliente o slug como grant |
| NR02 | Cada evento identifica métrica allowlisted y unidad no ambigua | Cliente crea nombre/unidad arbitraria para modificar cuota |
| NR03 | Idempotencia de evento se valida dentro del ámbito real tenant+fuente+operación | Replay o concurrencia duplican conteo |
| NR04 | Separar evento de consumo de correlación y de autorización | `X-Correlation-ID` asumido como permiso o llave única |
| NR05 | Contar consumo medido con fuente confiable y corte de periodo definido | Timestamps cliente alteran periodo o saldo |
| NR06 | Una rectificación/compensación conserva historial inmutable y justificación | Se borra ledger histórico o se aplican negativos clandestinos |
| NR07 | Denegación cross-tenant en lectura, conciliación, export y cálculo de límites | Empresa A ve consumo, identificadores, archivos o PII de B |
| NR08 | El plan y estado de suscripción se validan al aplicar reglas; RBAC sigue separado | Entitlement reemplaza autorización de usuario |
| NR09 | Cambio de plan preserva periodos de medición y reglas vigentes con versión | Un downgrade altera consumo ya registrado o recalcula facturas anteriores sin autorización |
| NR10 | Falla de almacenamiento en cuota crítica es fail-closed o reversible según política aprobada | Se confirma consumo/cobro sin persistencia necesaria |
| NR11 | Emisión de logs sin secretos, contraseñas, tokens ni identificadores personales innecesarios | Payload privado se copia en evento/export/log |
| NR12 | Unidades y límites permiten distinguir 0, ilimitado y no configurado de forma tipada | `null` usado automáticamente como tarifa libre/permiso ilimitado |
| NR13 | UI Angular indica estados de carga/error sin decidir seguridad o validez de consumo | Medición de frontend se usa como fuente exclusiva de facturación |
| NR14 | Cambios de costos y proveedores requieren autorización comercial/seguridad nueva | Documentación M01 desencadena cobro, activación de servicio o migración |

## 5. Casos verificables posteriores (oráculos documentales, NO pruebas runtime)

| Caso | Signo | Estímulo y resultado exigido en fase ejecutable |
| --- | --- | --- |
| P01 | + | Usuario A autorizado causa acción medible: servidor atribuye el evento únicamente a A y unidad aprobada |
| P02 | + | Mismo evento y clave reintentados dentro de A: proyección contabiliza una sola vez y conserva rastreo |
| P03 | + | Conciliación sobre ventana autorizada reproduce suma de eventos con versión/periodo correctos |
| P04 | + | Evento válido dentro de límite permite acción tras validar suscripción y RBAC independientes |
| P05 | + | Rectificación autorizada crea compensación auditada, nunca reescribe evento original |
| N01 | - | Tenant A altera `EmpresaId` para afectar cuota B: denegación sin contabilización ni fuga |
| N02 | - | Cliente cambia unidad/clave/métrica no autorizada: rechazo |
| N03 | - | Dos reintentos concurrentes de consumo: nunca doble efecto |
| N04 | - | Cabecera de correlación maliciosa pretende autorizar o generar idempotencia: no afecta grants |
| N05 | - | Timestamp o periodo controlado por el cliente mueve uso a otro ciclo: no aceptar |
| N06 | - | Consumo en estado Suspendida/Cancelada/Expirada sin política aprobada: no asumir autorización |
| N07 | - | Lectura/export A incluye eventos, PII o recursos de B: falla seguridad |
| N08 | - | Fallo del almacén durante consumo crítico, pero operación se confirma como medida: falla integridad |
| N09 | - | Cambio de plan edita/borra consumo histórico o aplica precios retroactivos sin decisión: rechazar |
| N10 | - | Registro contiene JWT/contraseña/secretos o PII ajena: redacción y minimización obligatorias |
| N11 | - | Angular calcula y comunica un saldo para elevar permisos o facturar: backend no confía |
| N12 | - | M01.01 habilita M01.06/M02, migraciones, webhooks, precios o QA/PROD por sí sola: NO_GO |

Estos oráculos son una **especificación de pruebas para el futuro**. El presente cambio Markdown no ejecuta ni acredita pruebas HTTP, EF/MySQL, Playwright, idempotencia distribuida, reconciliación ni topes productivos.

## 6. Dependencias, seguridad, verificación documental y rollback

Al preflight, `PLAN_MAESTRO!H650=PENDIENTE, I650=vacío`, `COLA!G650/O650=SI/SI`, `DAG_TECNICO!F611/G611=VALIDADO/SI` y las 60 preparaciones `C01-01..04/C02-01..04/C03-01..04.M01.01–05` en LISTO. Estas referencias de preparación **no** habilitan automáticamente implementación de C04-03, precios, JWT/RBAC/tenant, base de datos, cobros ni promoción. Claim exclusivo, sin escritores duplicados y con readback antes de publicar.

A0: revisar que los blobs sean realmente legibles en HEAD, matriz NR01–NR14, P01–P05/N01–N12, alcance, secretos y procedencia del CHANGELOG. A1: validar estructuralmente casos positivos/negativos y no declarar pruebas futuras ejecutadas. A2: revisar restricciones de tenant/RBAC/JWT, privacidad, cronología y fail-closed sin tocar runtime. A3: comprobar fuentes y coherencia de PLAN/COLA/DAG y ausencia de cambios a consumidores. A4: commit fast-forward de Markdown con Scope Lock y procedencia V5 SUCCESS del SHA exacto, BITACORA A:Z y PLAN/COLA readback. A5: obligatoria revisión independiente por otro TASK_ID Javier, distinta del autor.

Para **este changeset documental**: `dotnet/Angular/MySQL/Docker/Playwright=NO_APLICA` porque no cambia código, no por atribuirse sus PASS. Todos serán pertinentes cuando M02/M03/funciones runtime los modifiquen. Reversión: `git revert <SHA>` normal sobre HEAD revalidado y evento compensatorio append-only, sin borrar BITACORA histórica ni force-push. M01.01 no cambia el gate de fases posteriores.
