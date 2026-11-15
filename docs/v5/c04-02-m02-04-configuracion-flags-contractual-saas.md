# SOLQARYN V5 — C04-02.M02.04: configuración tipada, ámbitos y flags seguros para contratos SaaS

**Autor:** PRIMARY DEV 06 — JAVIER (TASK_ID 6af16bbad26881918c283abe06e1f32a). **Fecha:** 2026-11-15. **Rama:** dev de solqaryn/Solqaryn. **PROJECT_SCOPE_LOCK=STRICT.** Esta microtarea especifica configuración **futura**; no registra nuevas opciones ejecutables, no activa cobros ni modifica permisos o proveedores.

## 1. Fuentes, preflight e integración con autoridad existente

Fuente única V5: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES, Google Doc 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA, PLAN_MAESTRO fila 623, C04-02.M02.04, «Definir configuración tipada, ámbitos y feature flags seguros». Claim solo después de PLAN H=PENDIENTE, I vacío, COLA G/O=SI, DAG F=VALIDADO/G=SI; se confirmó transición exclusiva a EN_PROGRESO para DEV06.

El baseline real **no** dispone de un motor jurídico/económico aprobado por estas especificaciones. backend/src/Domain/Entities/FeatureFlagsSaaS.cs implementa PoliticaModulosSaaS que comprueba tenant/suscripción/plan y permite denegación explícita; no sustituye RBAC ni autoriza contrato firmado o cargo. backend/src/Domain/Entities/SuscripcionesSaaS.cs expone estado técnico e identidad EmpresaId sin probar precio/impuestos. backend/src/Application/Services/SuscripcionesSaaSService.cs valida scope tenant antes del repository. La configuración de aplicación y C01-02.M02.04 pueden regir módulos existentes, pero el diseño de este bloque es nuevo **en papel**. Las decisiones D01–D08 sobre representante, términos, moneda/impuestos, renovaciones, mora, proveedor, cancelaciones y retención siguen abiertas.

## 2. Precedencia restrictiva y tipado

Ámbitos propuestos: (A) plataforma/configuración de backend administrada por propietario, (B) política comercial aprobada por dominio contractual, (C) empresa autenticada, (D) actor con permiso contractual. La regla de combinación es **intersección de autorizaciones**: una opción de empresa o UI puede deshabilitar, jamás elevar una capacidad que plataforma, política, JWT/RBAC o tenant nieguen. No usar la configuración de sucursal ni un querystring como autoridad contractual. Los valores sensibles son referencias a secretos administrados, no secretos en JSON, Sheets ni Git.

Resolver con valor tipado, fuente y revisión: si está ausente, corrupto, desactualizado, no aprobado o contradictorio, retornar DENY; no convertir null en TRUE. Cache por EmpresaId+revisión, invalidar tras cambio de tenant, revocación, rol o contrato. La UI solo refleja resultado; el backend revalida acción y ownership antes de cualquier I/O.

## 3. Dieciocho opciones conceptuales y sus efectos (todas INACTIVAS)

| Clave lógica propuesta | Tipo y ámbito permitido | Valor por defecto seguro / frontera |
| --- | --- | --- |
| Contracts.Enabled | bool plataforma+política aprobada | false |
| Contracts.DraftReadEnabled | bool plataforma+tenant | false hasta autorización propia |
| Contracts.DraftWriteEnabled | bool plataforma+RBAC | false; no firmas ni cargos |
| Contracts.AcceptanceEnabled | bool política D01/D02+RBAC | false; exige representante y términos aprobados |
| Contracts.RenewalProposalEnabled | bool política D04+RBAC | false |
| Contracts.AutoRenewEnabled | bool política D03/D04/D06 | false; no cobro por flag |
| Contracts.SuspensionEnabled | bool política D05+RBAC | false |
| Contracts.ReactivationEnabled | bool política D05+RBAC | false; no terminal |
| Contracts.CancellationEnabled | bool política D07+RBAC | false; sin refund tácito |
| Contracts.MoraAutomationEnabled | bool política D05+conciliación | false |
| Contracts.PaymentProviderRef | identificador allowlisted D06, backend | null; no credencial ni cobro |
| Contracts.CurrencyPolicyRef | identificador versión D03 | null; no moneda supuesta |
| Contracts.TaxPolicyRef | identificador versión D03 | null |
| Contracts.TermsVersionRef | referencia de términos D02 | null; no consentimiento implícito |
| Contracts.LedgerReconciliationRequired | bool plataforma, no relajable | true |
| Contracts.AuditAppendOnly | bool plataforma, no relajable | true |
| Contracts.IdempotencyRequired | bool plataforma, no relajable | true |
| Contracts.RetentionPolicyRef | identificador aprobado D08 | null; conservar sin borrado automático |

**Los nombres son especificación de tipado, no claves realmente registradas.** El futuro validador debe rechazar nombres no allowlisted, coerciones desde string arbitraria, desbordes y referencias de versiones no existentes. No habilitar por medio de valores publicados en un documento, Sheet o frontend.

## 4. Doce reglas de configuración (CFG01–CFG12)

- CFG01: plataforma deshabilitada bloquea todas las opciones descendientes.
- CFG02: ausencia de política D01–D08 requerida bloquea exactamente la operación sensible.
- CFG03: permisos JWT/RBAC y membresía válida backend nunca se infieren de feature flag.
- CFG04: respuesta cacheada de empresa A nunca se usa para B, ni al alternar sesión.
- CFG05: flags de UI no habilitan rutas, roles, autenticación ni datos.
- CFG06: ningún valor cliente puede fijar EmpresaId confiable o principal autenticado.
- CFG07: referencias de proveedor y credencial permanecen privadas; null significa sin proveedor.
- CFG08: la conciliación y la idempotencia transaccional son obligatorias y no relajables.
- CFG09: consentimiento, tarifas, moneda, impuestos, prorrateo y vencimiento exigen política versionada aprobada.
- CFG10: estado contractual terminal no puede reactivarse por toggles.
- CFG11: cada actualización futura de opción requiere versión esperada/CAS, actor autenticado, auditoría y rollback.
- CFG12: Scope Lock FAIL, CI obligatoria no aprobada o A5 pendiente impide cualquier promoción; flags no la sustituyen.

## 5. Pares de pruebas futuras (no ejecutadas)

| Contraste positivo futuro | Rechazo negativo futuro |
| --- | --- |
| P01 Plataforma y política habilitadas con RBAC | N01 plataforma OFF y empresa intenta activar |
| P02 Referencia a término aprobado D02 | N02 término inexistente o D02 no aprobado |
| P03 Lectura A con revisión A | N03 reutilizar cache A en B |
| P04 Revocación invalidada antes de operación | N04 cliente con permiso revocado usa cache |
| P05 Valor booleano tipado validado | N05 "true" no confiable o clave desconocida |
| P06 Modo borrador permitido con scope | N06 borrador concede aceptación contractual |
| P07 Renovación propuesta con D04 | N07 toggle ejecuta cargo automático |
| P08 Concilia ledger verificado | N08 timeout provoca mora/suspensión |
| P09 Cancelación con D07 | N09 cancelación dispara reembolso sin autorización |
| P10 AuditAppendOnly activo | N10 usuario intenta desactivar auditoría |
| P11 ProviderRef allowlisted y aprobado | N11 URI/token aportado por frontend |
| P12 Config versionada con CAS | N12 sobrescribir revisión concurrente obsoleta |

P01–P12 y N01–N12 **son futuros oráculos documentales**. No se implementaron options classes, flags, endpoints, migraciones, suites .NET/MySQL ni decisiones económicas.

## 6. Compatibilidad, rollback y A0–A5

No cambiar contratos públicos SaaS existentes: API onboarding, limits, entitlement, RBAC de Configuración y tipos Angular actuales siguen operando según su autorización previa. El bloque de contrato legal sería otro caso de uso sujeto a nueva decisión y a pruebas positivas/negativas del SHA funcional. Migraciones, JWT, EF, auditores y secreto no se tocan en este scope.

**A0:** fuente V5, claim H/I, DAG/COLA, archivo Domain FeatureFlagsSaaS y baseline suscripción leídos; solo dos Markdown en diff. **A1:** 18 claves, doce reglas y doce pares P/N revisables textualmente; NO son pruebas ejecutadas. **A2:** JWT/RBAC/tenant A/B, autorización, privacidad y rollback fail-closed sin modificar sistema. **A3:** diseños compatibles con componentes actuales; .NET, Angular, MySQL y HTTP runtime NO_APLICA para estos dos Markdown, nunca PASS. **A4:** publicación serial en dev, Scope Lock y procedencia V5 completed/success exact SHA, PLAN/COLA/BITACORA readback. **A5:** exclusivamente otra Task Javier diferente del autor.

Rollback del changeset Markdown mediante git revert ordinario con HEAD actualizado; nunca cambiar datos, cobrar, aplicar SQL, configurar Cloudflare, QA/main/PROD ni desactivar checks.
