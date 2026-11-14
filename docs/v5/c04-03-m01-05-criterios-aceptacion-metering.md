# SOLQARYN V5 FINAL — C04-03.M01.05: aceptación funcional y técnica observable de metering

**Entrega:** criterios de aceptación **documentales y futuros**, no implementación o certificación de medición. **Autor:** PRIMARY DEV 03 / JAVIER_CHATGPT / `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Fecha:** 2026-11-14. **Ámbito exclusivo:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`.

**Fuentes institucionales:** [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). El preflight del ID `C04-03.M01.05` comprobó `PLAN H654=PENDIENTE/I654=vacío`, `COLA G654/O654=SI/SI`, `DAG F615/G615=VALIDADO/SI`, con 60 preparaciones antecesoras C01/C02/C03 M01.01–05 LISTO. La fase C04-03 persigue futura medición verificable de usuarios, almacenamiento, operaciones y archivos por tenant, eventos idempotentes, límites y conciliación. Este documento no culmina M01.06 ni autoriza M02/M03 o despliegue.

## 1. Evidencia actual y grado de certeza

| Entrega/source | Blob real en dev | Lo acreditado | No inferir |
| --- | --- | --- | --- |
| [M01.01 frontera](c04-03-m01-01-alcance-metering-no-regresion.md) | `9f84047a310a558f1266052eeab545fd6a44088f` | NR01–14 de alcance y no regresión | Tests runtime |
| [M01.02 interfaces](c04-03-m01-02-actores-entradas-salidas-metering.md) | `d9702754c2094255a3d564841688a9546fdf690f` | 10 actores y 14 fronteras futuras | Endpoints implementados |
| [M01.03 inventario](c04-03-m01-03-inventario-evidencia-metering.md) | `cb8a8349719d709f6d81bba3e9b620a946a06b9d` | Once archivos inspeccionados y seis brechas de metering | Ledger consumo funcional |
| [M01.04 riesgos](c04-03-m01-04-riesgos-decisiones-metering.md) | `60c340707b7f419e9ffdb8287779183dcefab1e4` | R01–12, S01–08, D01–08 con roles propuestos y estado PENDIENTE | Aprobaciones comerciales otorgadas |
| `backend/src/Domain/Entities/SuscripcionesSaaS.cs` | `d9b5dbe19fb728293b00b748f10ac10336debd2d` | PlanLimite, Suscripcion tenant y estado | Uso real medido |
| `backend/src/Infrastructure/Repositories/SuscripcionesSaaSRepository.cs` | `f3340ca687867d6dc2d99dfff5452bbe5c9bbb79` | Idempotencia de onboarding SaaS | Idempotencia del consumo |

Los hashes son de blobs de archivos, no de trabajos CI, entornos MySQL ni autorizaciones comerciales. Sin validación dinámica de JWT/RBAC/tenant A/B, retención o pruebas de replay de consumo, no se acepta una capacidad de metering runtime.

## 2. Matriz falsable para futuras implementaciones (14 pares de prueba positivos y negativos, NO ejecutados aquí)

| ID | Estímulo positivo y resultado esperado | Estímulo negativo y denegación/observación | Evidencia exigible en microtarea runtime |
| --- | --- | --- | --- |
| AC01 | Usuario autorizado tenant A ejecuta operación medible y suma solo a A | N01: cabecera/body fuerza EmpresaId B, rechazo sin mutación | .NET+HTTP+MySQL efímero A/B |
| AC02 | Un mismo EventID con misma clave se contabiliza exactamente una vez | N02: dos reintentos/concurrencia duplican contador, debe fallar | Integración DB simultánea y readback |
| AC03 | Consumo de almacenamiento reporta bytes y ventana tipados | N03: cantidad o unidad arbitraria cliente, rechazar | Adaptador confiable + pruebas de unidades |
| AC04 | Conteo de usuarios usa criterio de activo/intervalo aprobado | N04: revocado o tenant externo contado sin política, rechazar | Fixtures de membresía/estado |
| AC05 | Conteo de archivos distingue objeto/versionado según regla aprobada | N05: dos vistas/copias duplican mismo objeto, rechazo | Fuente real autorizada + DB |
| AC06 | Métrica operaciones se deriva del hecho confirmado del servidor | N06: evento frontend sin operación backend, no contar | Test HTTP/API negativo |
| AC07 | PlanLimite se interpreta con clave normalizada y semántica aprobada | N07: null/0/negativo malinterpretado como grant, fallo | Domain+Application + alcance comercial |
| AC08 | Lectura de uso/limites de A devuelve solo proyección autorizada A | N08: usuario A ve registros o PII de B, denegación | HTTP+MySQL A/B + RBAC |
| AC09 | API de uso exige JWT y permiso servidor reales | N09: rol insuficiente obtiene agregado por ruta, 403/404 | Security integration |
| AC10 | Reloj UTC servidor y policy version fijan el corte de periodo | N10: cliente cambia fecha para alterar saldo o costo, rechazo | Pruebas de fecha/periodos |
| AC11 | Conciliación detecta faltante y registra compensación con rastro | N11: reconciliador edita/borrar historial para cuadrar, fallo | Eventos inmutables y readback |
| AC12 | Fallo al guardar evento crítico revierte o bloquea según política | N12: operación crítica confirma sin persistencia requerida, fallo | MySQL error injection + rollback |
| AC13 | Logs y export cumplen minimización/autorización/retención aprobadas | N13: JWT, contraseña, token, PII o export A→B se expone, fallo | Secret-sentinel tests y privacidad |
| AC14 | UI representa límites/uso con loading/error/empty y acceso permitido | N14: Angular decide seguridad, cuota o cobro sin servidor, rechazo | Angular/Playwright + backend |

Los AC/N prueban **contratos de aceptación futura**, no hechos comprobados por esta M01.05. Los planes que requieran factura, moneda, precio o proveedor necesitan aprobación D01–D08 según ámbito y no se inventa un criterio fiscal.

## 3. Criterios técnicos de salida y exclusiones no negociables

La entrega runtime futura solo podrá aceptar una métrica cuando consten unidad tipada, tenant verificado, identidad/fuente, OperationId y clave idempotente independiente de CorrelationId, periodo UTC, persistencia y posibilidad de reconstruir un agregado. Las APIs deben negar claims de usuario sin pertenencia, RBAC insuficiente, estado de suscripción inaplicable y consulta de otro tenant, aunque la UI muestre datos. Un balance de consumo no es un balance monetario, y PlanLimite no representa eventos de uso.

No habilitar cobros, precios, impuestos, prorrateos, retención legal, MFA, JWT/roles nuevos, IAM, workers, webhooks, eventos entre Tasks, EF migration, infraestructura, dominios ni QA/main/PROD por esta documentación. No borrar historia ni suponer que filtros de tenant funcionaron por observar código o mocks.

## 4. Tabla de nivel de prueba por clase

| Tipo de cambio posterior | Evidencia obligatoria antes de LISTO_DEV | Lo que este documento NO acredita |
| --- | --- | --- |
| Domain / Application (.NET 10) | CI build/xUnit + positivos y negativos del scope | Unit test ejecutado |
| API / JWT / RBAC / tenant | CI de autorización HTTP y aislamiento A/B sobre SHA | Permiso o sesión real |
| EF / MySQL / outbox/ledger | MySQL efímero, transacciones y replay concurrente | DB/retención productiva |
| Angular UI 22.2.1 | npm/build y Playwright cuando sea pertinente | Componente de uso existente |
| Seguridad / privacidad | Escenarios de secretos, export y lectura cruzada | Política legal aprobada |
| Solo Markdown M01.05 | A0–A3 documental y Scope Lock+provenance V5 exact-SHA | Certificación runtime de cualquiera anterior |

Un CHECK `skipped`, `cancelled`, `queued`, `in_progress` o `failed` nunca equivale a PASS de una prueba requerida; CI no pertinente se marca NO_APLICA con motivo.

## 5. A0–A5, dependencias, rollback

A0: cotejar blobs de M01.01–04, 14 pares AC/N, ausencia de secretos, sección autocontenida CHANGELOG. A1: verificar formalmente AC01–AC14 y N01–N14 distintos/observable y que ningún escenario se declara ejecutado. A2: revisar que tenant, JWT/RBAC y privacidad no se amplían por el documento. A3: verificar PLAN/COLA/DAG, 60 predecesores LISTO y continuidad secuencial M01, sin liberar M01.06. A4: publicar diff limitado a Markdown fast-forward/CAS en `dev`, comprobar `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` completados SUCCESS en SHA exacto, BITACORA A:Z y PLAN/COLA readback. A5 requiere otra Task Javier distinta del autor antes de QA/main/PROD.

Rollback: `git revert <SHA>` ordinario en dev (después de releer HEAD) y evento compensatorio nuevo; no force-push ni borrar BITACORA. Esta M01.05 no cambia la cola por fuera de su propio estado institucional.
