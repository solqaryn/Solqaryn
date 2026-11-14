# SOLQARYN V5 — C04-02.M01.02: actores, entradas, salidas y superficies contractuales

**Task autora:** PRIMARY DEV 04 — JAVIER, `6aeacb7820ac8191b7d768a6f33dfa09`. **Fecha:** 2026-11-14. **Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Clase:** preparación M01 exclusivamente documental, sin ejecución de cobros, cambios de esquema ni nuevos endpoints.

## 1. Fuente y resultado acotado

Autoridad única: `STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES` V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, Plan `C04-02.M01.02` (fila 615): «Identificar entradas, salidas, actores y superficies afectadas». La meta futura del bloque es el ciclo de contratos y suscripciones: alta, renovación, pausa, cancelación, morosidad y cierre. M01.01 define objetivo/no-regresión; esta M01.02 **inventaría los bordes y los actores**, no certifica transiciones nuevas.

Preflight de esta preparación: PLAN fila 615 PENDIENTE sin owner, COLA G/O=SI y DAG_TECNICO ID exacto `C04-02.M01.02`, F=VALIDADO/G=SI antes del claim exclusivo. El DAG preparatorio no sustituye A0–A4 de implementación ni A5 independiente y no abre M01.06/M02.

## 2. Actores y competencias: presente versus futuro

| Actor / frontera | Entrada legítima | Salida esperada | Límite de autoridad |
| --- | --- | --- | --- |
| Usuario tenant autenticado | JWT válido, membresía activa y tenant seleccionado | Lectura de suscripción y límites de su propia empresa | `empresaId` de ruta no prueba titularidad |
| Operador `Configuracion/Crear` | Petición onboarding e `Idempotency-Key` en ruta tenant | Alta cuando las reglas actuales la permiten, DTO de suscripción | No puede aprobar facturación, contratos ajenos ni precios |
| Operador `Configuracion/Ver` | Lecturas de suscripción, límites y entitlement | DTO tenant-scoped sin datos de otra empresa | Un entitlement de plan no sustituye RBAC |
| Administración contractual futura | Solicitud versionada de renovación, pausa por mora o cancelación | Decisión auditada con causa y vigencia | Nuevo rol/permiso requiere política y desarrollo aparte; no se presume existente |
| Application `SuscripcionesSaaSService` | Tenant verificado, plan y fecha UTC | Onboarding idempotente, consulta de vigencia, límites y módulo | Debe negar empresa B antes de I/O; no decidir cobros sin fuente autorizada |
| Domain `Suscripcion` | EmpresaId, PlanId, inicio/fin UTC, preestado | `Activa`, `Suspendida`, `Cancelada`, `Expirada`; validaciones locales | No es un ledger económico ni prueba de morosidad |
| Infrastructure/EF/MySQL | Filtros, índices y persistencia de relaciones | Un ganador transaccional de alta/replay y lecturas de tenant | No deducir aislamiento SQL probado ni migraciones ejecutadas a partir de DTO |
| Angular consumidor | Contexto de empresa elegido y solicitudes API tipadas | Vistas accesibles de estado/errores, sin privilegios locales | No realiza authorization server-side ni cobros |
| Pasarela / webhook / worker futuros | Ninguna entrada autorizada en esta M01 | Ninguna salida ejecutable | Prohibido activar proveedor, cola o Tasks por contrato documental |

## 3. Inventario de entradas/salidas observadas en el código

| Borde existente | Entradas / validaciones visibles | Salidas / riesgos | Evidencia leída |
| --- | --- | --- | --- |
| `POST /saas/tenants/{empresaId}/onboarding` | `PlanCodigo`, `InicioUtc`, header `Idempotency-Key`; `[Authorize]`, `RequierePermiso(Configuracion, Crear)` | `SuscripcionSaaSDto`, error 400 si falta key; una alta no equivale a contrato comercial pagado | `SuscripcionesSaaSController.cs` blob `cab95db5a33c6a15448e2437481eaaac25ffc4fe` |
| `GET /saas/tenants/{empresaId}/suscripcion` | `instanteUtc` opcional; permiso de lectura; `ResolverTenantAsync` | Datos de `Suscripcion` con vigencia; no presentar B a A | Controller + `SuscripcionesSaaSService.cs` blob `bb501285f11cd4b32709a58a2c51efa19c3664ea` |
| `GET /saas/tenants/{empresaId}/limites` | `pagina`, `tamanoPagina`, `clave`; query normalizada | `PaginaSuscripcionSaaSDto`; no confundir cuota con rol | `SuscripcionesSaaSApiContracts.cs` blob `6045a167fb90fe3dea2fa87280d0a29cec57c6be` |
| `GET /saas/tenants/{empresaId}/modulos/{moduloClave}/entitlement` | módulo y timestamp, tenant resuelto | `Habilitado/Motivo` comercial, no permiso RBAC | Controller y servicio anteriores |
| Dominio `Suscripcion` | `Suspender()` desde Activa, `Reactivar()` desde Suspendida, `Cancelar(DateTime)` | Estados locales y fecha fin; cambios sin endpoint no prueban workflow HTTP | `SuscripcionesSaaS.cs` blob `d9b5dbe19fb728293b00b748f10ac10336debd2d` |

**Ausencias de contrato que requieren etapa futura:** identificador/versionado de acuerdo, autoridad de aceptación contractual, periodicidad, fecha de próxima renovación, cálculo de mora, titular de la deuda, aviso y plazos, prorrateos, ajustes fiscales, ledger de cargos, conciliación/reembolso, endpoint de cancelación y desactivación de pagos. Esta M01.02 no les inventa formatos, reglas ni permisos.

## 4. Límites de confianza y superficies de implementación futura

1. **API:** autenticación JWT y RBAC de acción; `empresaId` expresa solicitud, no autorización. Un identificador manipulado, un token inválido y una membresía revocada deben negar antes de operaciones de escritura.
2. **Application:** `IUsuarioScopeService` resuelve la empresa permitida; un cambio contractual requiere autorización específica adicional y una transición legal; no basta el `PlanCodigo` enviado.
3. **Domain:** las invariantes existentes evitan `Reactivar` desde estado distinto de Suspendida y `CambiarPlan` tras Cancelada/Expirada. Morosidad y renovación futuras no deben saltarse estos guardas.
4. **MySQL/EF:** futuros cambios económicos necesitan transacciones, índices e historia versionada; `Idempotency-Key` no es por sí sola comprobación de cargos. Probar con MySQL efímero y rollback no destructivo.
5. **Frontend:** mostrar contexto, estado, timestamps, causas y error accesible; una transición no puede parecer exitosa sin confirmación backend.
6. **Auditoría:** registrar tenant, actor, acción, estado anterior/nuevo, momento y correlación sin JWT, PII excesiva ni secretos. No cambiar logs/runtime en esta etapa.
7. **Proveedores externos:** no hay disparador autorizado para webhook, pago o worker; futuros eventos exigen firma, idempotencia, procedencia tenant y rechazo de duplicados.

## 5. Oráculos futuros, NO ejecutados como pruebas runtime

**Positivos P01–P05:** usuario autorizado lee su contrato; onboarding con clave estable no duplica alta; suspensión desde estado Activa conserva historia; reactivación legítima de Suspendida preserva tenant; cancelación registra fin UTC y no borra los registros previos.

**Negativos N01–N10:** usuario sin JWT; RBAC insuficiente; selector de tenant B con token A; membresía revocada; key ausente o reusada con otro payload; reactivación de Cancelada; cancelación anterior al inicio; renovación con precio/versión no aprobada; supuesto pago duplicado o sin autorización; fallo/timeout de pasarela que conceda acceso. Cada caso debe tener aserciones de rechazo y evidencia CI cuando se implemente.

Estos P/N son **casos documentados para etapas posteriores**, nunca pruebas de HTTP, MySQL, xUnit o Playwright ejecutadas por este Markdown. M01.02 no altera Domain, Application, Infrastructure, Controller, contratos DTO, UI ni secretos.

## 6. Trazabilidad, no-regresión y salida

No se deben degradar NR01–NR10 de `c04-02-m01-01-objetivo-no-alcance-contratos-suscripciones.md`: propiedad tenant, RBAC separado de entitlement, transición con historia, cierre no reversible por replay, preestado válido, vigencia, mora con evidencia, idempotencia, fallo cerrado y rollback de datos legítimos.

**A0:** código fuente real, Plan y alcance inspeccionados. **A1–A3 documentales:** mapas de actores y entradas/salidas, cinco escenarios positivos y diez negativos escritos, límites de tenant/RBAC y no-regresión coherentes con fuentes existentes. **A4:** diff de Markdown/changelog, commit fast-forward en dev, Scope Lock y verificación V5 de CHANGELOG `completed/success` sobre SHA exacto, BITACORA A:Z y readback PLAN/COLA. Las compilaciones ajenas al cambio de Markdown son `NO_APLICA`, no PASS atribuido. **A5:** otra Task Javier; no autoaprobación.

**Rollback de esta entrega:** `git revert` ordinario sobre los dos archivos documentales del commit, sin force-push ni borrar historial. Ninguna parte de este contrato autoriza cobros, morosidad ejecutable, pagos, migraciones, JWT/RBAC nuevos, QA/main/PROD ni cierre anticipado M01.06/M02.
