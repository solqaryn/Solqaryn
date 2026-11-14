# SOLQARYN V5 — C04-02.M01.05: aceptación funcional y técnica observable de contratos SaaS

**Autor:** SOLQARYN PRIMARY DEV 04 — JAVIER (`6aeacb7820ac8191b7d768a6f33dfa09`). **Fecha:** 2026-11-14. **Repo:** `solqaryn/Solqaryn@dev`. **Scope:** `PROJECT_SCOPE_LOCK=STRICT`. **Entrega M01:** criterios documentales; NO implementación de un motor contractual, pasarela o cambios económicos.

## 1. Propósito, fuente y alcance exacto

La fuente ejecutable única es `STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES` V5 FINAL, Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. `PLAN_MAESTRO!A618` define `C04-02.M01.05`: «Definir aceptación funcional y técnica observable». El resultado final del bloque (no esta M01.05 aislada) requiere alta, renovación, pausa, cancelación, morosidad y cierre contractual coherentes, auditables y reversibles. Se verificó previamente PLAN PENDIENTE libre, COLA G/O=SI y DAG F=VALIDADO/G=SI para el ID, únicamente como habilitación documental.

Se enlazan sin sustitución los contratos M01.01 (objetivo/NR01–NR10), M01.02 (actores y superficies), M01.03 (doce blobs reales) y M01.04 (R01–R12, S01–S08, D01–D08 pendientes). **Ninguna decisión D01–D08 está aprobada aquí.** Definir un oráculo no demuestra resultado PASS ni autoriza una transición económica.

## 2. Línea de base inspeccionada, no certificación de ejecución

| Referencia | Blobs leídos en dev | Garantía inferible limitada |
| --- | --- | --- |
| Domain `SuscripcionesSaaS.cs` | `d9b5dbe19fb728293b00b748f10ac10336debd2d` | Activa/Suspendida/Cancelada/Expirada, métodos de transición local y fechas UTC |
| Application `SuscripcionesSaaSService.cs` | `bb501285f11cd4b32709a58a2c51efa19c3664ea` | `ResolverTenantAsync`, onboard idempotente, conflicto/replay, consulta de vigencia |
| API `SuscripcionesSaaSController.cs` | `cab95db5a33c6a15448e2437481eaaac25ffc4fe` | Rutas onboarding/lectura/límites/entitlement con `[Authorize]` y `RequierePermiso`; no API de mora/pago |
| EF `SuscripcionesSaaSConfiguration.cs` | `16631a0155a449ab71e7479c92e8ba8804303249` | Índices y modelo, no prueba SQL ejecutada |
| Unit tests `SuscripcionesSaaSServiceTests.cs` | `090bf81e7a8f08e86dcdc7ddf5b0c20625ae2839` | Seis tests xUnit/Moq visibles, no integración HTTP/JWT/MySQL en esta entrega |
| Angular `suscripcion-saas.service.ts` | `b3883c10afce06a39bdf4d0dec356a553f8aa32f` | Cliente con tipado de API y header idempotencia, no autoridad contractual o permisos |

## 3. Matriz de aceptación para FUTURA ejecución AC01–AC14

| Criterio | Oráculo positivo observable en fase que implemente el producto | Oráculo negativo y evidencia que exigir |
| --- | --- | --- |
| AC01 — identidad del contrato | Crear contrato vinculado a tenant, versión y actor con aprobación legítima | N01: `empresaId` ajeno o membresía revocada es denegado antes de I/O; test A/B |
| AC02 — RBAC contractual | Permitir únicamente acción respaldada por permiso backend y scope | N02: entitlement contratado no eleva a permiso contractual/admin |
| AC03 — alta única | Una alta autorizada produce único contrato con vigencia UTC | N03: doble request concurrente/mismo key no crea contratos duplicados |
| AC04 — replay distinto | Clave/payload idénticos retornan resultado ganador coherente | N04: mismo idempotency key con payload distinto da conflicto |
| AC05 — estado | Transición Activa→Suspendida guarda motivo, actor y timestamp | N05: suspensión desde Cancelada/Expirada o por usuario sin permiso se rechaza |
| AC06 — reactivación | Suspendida→Activa solo con revalidación y condiciones autorizadas | N06: Cancelada/Expirada, tenant B o permiso revocado no revive contrato |
| AC07 — cancelación | Cancelada conserva identificador, fechas, referencias y trazabilidad | N07: cancelación anterior a inicio o borrado de historia aborta |
| AC08 — renovación | Renovar en periodo/vigencia nuevos con versión económica aprobada | N08: renovación retroactiva o tarifa no aprobada no produce efecto |
| AC09 — morosidad | Una deuda verificable y reconciliada activa política aprobada | N09: timeout, callback tardío o información de otra empresa no genera mora |
| AC10 — integridad DB | Cambio versionado conserva FK, índices y ledger mediante transacción | N10: migración destructiva y concurrencia MySQL fallan en suite efímera |
| AC11 — autorización HTTP | API entrega DTO/errores 400/403/404/409 consistentes con validaciones | N11: falta de JWT/RBAC, key o membresía no responde éxito ni dato B |
| AC12 — UI accesible | Estado, vigencia, causa y errores se presentan con teclado/labels | N12: Angular no puede inventar permiso, estado «pagado» o tarifa |
| AC13 — privacidad/auditoría | Actor, tenant, motivo, estado previo/nuevo y correlación sin secretos | N13: JWT, key privada, tarjeta, PII sensible o dato tenant B en log es fallo |
| AC14 — despliegue seguro | CI causal exact-SHA y rollback documentado antes de cada cambio runtime | N14: check queued, cancelled, skipped, failed o head SHA ajeno no es PASS |

**AC01–AC14 / N01–N14 son pruebas a construir y ejecutar posteriormente**, con datos de test anonimizados. Los métodos observados no incluyen aún un calendario comercial aprobado de renovación, moneda, impuesto, mora, prorrateo o reembolso. Esas decisiones D01–D08 siguen abiertas y no se inventan para dar por pasado un AC.

## 4. Evidencia mínima por capa en la fase correcta

- **Domain/Application .NET 10:** xUnit de transiciones positivas y negativas, preestados, tenant A/B, RBAC y replay/concurrencia. No aceptar solo snapshot textual o Moq cuando se modifica persistencia.
- **EF Core/MySQL:** servicio efímero CI, claves únicas, transacciones, datos A/B, reversión aditiva y preservación de historia con prueba de rollback. No tocar Aiven/prod por este documento.
- **HTTP/JWT/RBAC:** autenticación y autorización server-side con 401/403, membership revocada, conflictos 409 y no revelación de tenant B. UI no reemplaza el gate.
- **Angular 22.2.1:** estados accesibles, mensajes sin precio ficticio, tests de servicio y Playwright cuando exista UI runtime nueva. Contraste en viewport reducido/teclado.
- **Economía/cobros:** precio, moneda, periodicidad, mora y reembolso sólo después de decisión comercial aprobada, proveedor autorizado y fuente ledger auditable; ningún webhook/worker se activa por documento.
- **GitHub Actions y gobierno:** `verify-solqaryn-project-scope` y checks pertinentes completed/success sobre HEAD exacto; revisar logs y SHA. A5 por Task distinta, sin promoción a QA/main/PROD.

## 5. Condiciones de cierre por etapa, evitando gates falsos

**M01.05 documental:** existe este contrato y referencias causales, catorce oráculos positivos/negativos y límites; diff sólo Markdown/CHANGELOG, Scope Lock y V5 provenance exact-SHA, BITACORA A:Z y readback de PLAN/COLA. Esto no convierte las AC futuras en tests ejecutados.

**M01.06:** requiere su propia comprobación de dependencias y A5 independiente de predecesores; si DAG G=NO se conserva NO_GO. M02 precisa decisiones de modelo comercial, sin inventar permisos o tarifas. M03 exige código real .NET 10, MySQL EF y controles de tenant/JWT/RBAC con CI causal; M04–M06/UI/QA dependen de ese trabajo y de autorizaciones adicionales.

**A0:** revisión estática de alcance y fuentes. **A1:** cobertura documental de 14 casos positivos futuros. **A2:** 14 negativos y separación RBAC/tenant/seguridad documentada. **A3:** coherencia con evidencias existentes y decisiones abiertas, NO_APLICA de runtime para este Markdown. **A4:** SHA y diff exactos, Scope Lock/V5 provenance successful, PLAN/COLA/BITACORA verificadas. Backend/frontend/security cancelados, skipped o in_progress no se cuentan como PASS aunque sean irrelevantes al documento.

**A5_PENDIENTE**: otra Task Javier, no la autora; posterior a A0–A4. **Rollback de la entrega:** `git revert` normal del commit de los dos archivos documentales sin editar BITACORA histórica. No se autorizan pagos, nuevas APIs, morosidad runtime, migraciones, workers, events, QA, main ni producción.
