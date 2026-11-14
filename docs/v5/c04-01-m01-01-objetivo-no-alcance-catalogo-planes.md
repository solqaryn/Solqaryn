# SOLQARYN V5 — C04-01.M01.01: objetivo, no-alcance y no-regresión del catálogo de planes

**Autor de esta entrega:** PRIMARY DEV 02 — JAVIER, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Rama:** `solqaryn/Solqaryn@dev`. **Límite:** `PROJECT_SCOPE_LOCK=STRICT`. **Fecha:** 2026-11-14. **Naturaleza:** preparación M01 exclusivamente documental; no habilita M02, implementación de pagos, QA, main ni producción.

## 1. Autoridad, alcance y resultado comprobable

Autoridad ejecutable única: `STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES`, Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, sección **C04-01 — Catálogo de planes**, y PLAN_MAESTRO `C04-01.M01.01` (fila 578). La condición preliminar de entrada observada en `COLA!G578/O578=SI` y `DAG_TECNICO!F539=VALIDADO/G539=SI` autoriza revisar este documento, **no** facturar, alterar suscripciones ni liberar un gate posterior.

**Objetivo de producto para el futuro lote C04-01:** definir un catálogo SaaS versionado de planes, capacidades y límites, con precio, moneda, periodicidad y política de cambio de plan explícitos y trazables; las decisiones de acceso y límite de un tenant deben imponerse en backend a partir de una suscripción vigente, sin aceptar atribuciones de autoridad desde la interfaz. La evidencia final de esa meta exigiría plan versionado y límites server-side, pero **esta M01.01 solo establece la frontera y el criterio de no-regresión**.

**Entrega de M01.01:** este contrato de objetivo/no-alcance, mapa de invariantes y criterios para detectar regresiones. El documento **no demuestra que el catálogo versionado ni los precios estén implementados**; son requisitos para las etapas posteriores M02/M03 y sus pruebas reales.

## 2. Línea de base verificable en dev (lectura, no certificación nueva)

| Fuente concreta leída | Identidad de blob al preflight | Hecho verificable de código | Límite de la inferencia |
| --- | --- | --- | --- |
| `backend/src/Domain/Entities/SuscripcionesSaaS.cs` | `d9b5dbe19fb728293b00b748f10ac10336debd2d` | `Plan`, `PlanLimite`, `Suscripcion`; límites normalizados por clave, positivos o null; asignación a EmpresaId por suscripción | No prueba versionado de tarifas, cobros ni ejecución de un motor de planes comercial |
| `backend/src/Application/Services/SuscripcionesSaaSService.cs` | `bb501285f11cd4b32709a58a2c51efa19c3664ea` | `ResolverTenantAsync` precede casos de uso; onboarding exige clave idempotente y resuelve replay/concurrencia | No autoriza consumir EmpresaId del cliente como permiso ni acredita pasarela de pagos |
| `backend/src/API/Controllers/SuscripcionesSaaSController.cs` | `cab95db5a33c6a15448e2437481eaaac25ffc4fe` | `[Authorize]` y `[RequierePermiso]` para rutas de onboarding, lectura, límites y entitlement | HTTP, claims JWT y aislamiento A/B requieren CI de runtime dirigida cuando cambien |
| `backend/src/Application/DTOs/SuscripcionesSaaSApiContracts.cs` | `6045a167fb90fe3dea2fa87280d0a29cec57c6be` | DTO de plan/suscripción, paginación acotada y códigos de error explícitos | DTO existente no implica nuevo API de precios o facturación |
| `frontend/src/app/services/suscripcion-saas.service.ts` | `b3883c10afce06a39bdf4d0dec356a553f8aa32f` | Cliente Angular usa rutas SaaS, idempotency key y validación local de IDs | Las validaciones UI no reemplazan autorización backend |

La arquitectura vigente es Angular 22.2.1, .NET 10, Domain <- Application <- Infrastructure, MySQL/EF Core. Los proveedores, tarifas, módulos vendibles, monedas y periodicidades definitivas **no se inventan** a partir de estas fuentes.

## 3. Dentro y fuera del alcance

**Incluido en la definición futura del producto (no implementado aquí):** catálogo de planes con identificador y versión inmutable de reglas aplicadas; capacidades allowlisted; cuotas con unidad, semántica de ilimitado y horizonte de medición claros; precio, moneda, periodicidad y reglas comerciales expresas que exigen aprobación; upgrade/downgrade y prorrateo sujetos a decisión documentada; política de convivencia de versiones para una suscripción; trazabilidad de decisiones de entitlement; consulta de uso de tenant sin fuga transversal; rollback funcional sin alterar historia comercial legítima.

**Fuera de esta microtarea:** editar código, API, schemas o migraciones; precios reales, impuestos y cobros; contratar/activar pagos; crear pasarelas, cron, workers, dispatcher, webhook o Task; abrir permisos globales; cambiar tenant/JWT/RBAC; usar datos de producción; modificar `qa`, `main`, DNS, infraestructura o producción. La preparación **no abre M01.06/M02 por sí sola** ni omite revisión A5.

## 4. Invariantes de no-regresión (NO_GO si se vulneran)

| ID | Condición invariante | Señal de regresión y control exigible en fase ejecutable |
| --- | --- | --- |
| NR01 | `Plan` define capacidades; la suscripción asigna el plan a EmpresaId y su vigencia | Plan tratado como autorización sin membresía tenant server-side: negar |
| NR02 | El tenant efectivo se verifica en backend antes del acceso a repositorio | EmpresaId, plan o rol enviados por navegador como autoridad: negar |
| NR03 | RBAC de lectura/administración sigue siendo independiente del entitlement comercial | Entitlement habilitado convertido en permiso privilegiado: negar |
| NR04 | El límite por clave debe ser consistente: mayor que cero o null definido como ilimitado; no negativos | Clave vacía, duplicación o valor no válido: rechazo determinista |
| NR05 | Un replay idempotente no duplica altas ni acepta payload diferente con la misma clave | Doble cobro/alta o cambio silencioso: fallo y auditoría |
| NR06 | Suscripciones canceladas/expiradas no admiten cambios que reescriban historia | Resurrección o pérdida de histórico comercial: bloqueo |
| NR07 | Precios, monedas, periodicidades y revisiones se aplican por versión aprobada | Reprecio retroactivo o cobro no autorizado: NO_GO |
| NR08 | A/B conservan sus límites incluso con administradores de plataforma | Mostrar/modificar datos de otro tenant: NO_GO |
| NR09 | Modo fallo de servicios externos no concede entitlements ni efectos económicos | Error, timeout o webhook repetido que activa permisos o cobro: negar |
| NR10 | Desactivación y rollback preservan integridad de suscripciones ya registradas | Eliminar datos comercialmente relevantes o ejecutar revert destructivo: prohibido |

## 5. Contratos de aceptación FUTUROS, expresamente no ejecutados

- **P01:** un plan activo y aprobado puede consultarse con versión y unidades coherentes, sin efectos de cobro.
- **P02:** límites positivos y null se interpretan de forma estable en backend según la misma clave normalizada.
- **P03:** la suscripción del tenant A expone exclusivamente su plan, sin registros del tenant B.
- **P04:** un replay idéntico de onboarding produce el resultado ganador sin duplicar transacción.
- **P05:** la desactivación de una versión impide nuevas altas sin alterar historia previa.
- **N01:** tenant inexistente o sin membresía: denegación.
- **N02:** cliente suplanta EmpresaId B en ruta o cuerpo: denegación server-side.
- **N03:** permiso RBAC insuficiente aunque haya un plan habilitado: denegación.
- **N04:** plan inexistente/inactivo o sin versión vigente: rechazo sin mutación.
- **N05:** moneda, tarifa, impuesto o periodicidad no aprobados: ningún cobro ni activación.
- **N06:** límites 0/negativos, clave vacía o no incluida: validación fallida.
- **N07:** misma clave idempotente con carga diferente: conflicto auditable.
- **N08:** cambio sobre suscripción cancelada/expirada o downgrade destructivo: bloqueo.
- **N09:** caída del proveedor de facturación/timeout: sin aumento de cuota ni acceso.
- **N10:** evento duplicado, tardío o proveniente de tenant B: sin mutación ni filtración.

Estos son **oráculos documentales**, no resultados PASS de .NET, SQL, Angular o Playwright. Sus pruebas positivas, negativas, de concurrencia y aislamiento se ejecutarán únicamente en los scopes de implementación/integración con CI exact-SHA.

## 6. Condición de salida y reversibilidad

La M01.01 puede cerrarse en LISTO DEV **solo** al verificar el documento y alcance, integridad de markdown, diff acotado, Scope Lock y control V5 CHANGELOG `completed/success` sobre SHA publicado, BITACORA A:Z, PLAN/COLA readback y `A5_PENDIENTE` para otro TASK_ID Javier. No atribuir PASS de CI ajena, skipped o en progreso. El rollback de **esta entrega documental solamente** es un `git revert` normal sobre el commit de dev, nunca force-push. Para revertir futuras reglas comerciales hacen falta migraciones y criterios separados.

**NO se declara aprobada ninguna tarifa, límite productivo, esquema fiscal, pago, autorización adicional, pasarela, evento ejecutable ni liberación de lote.**
