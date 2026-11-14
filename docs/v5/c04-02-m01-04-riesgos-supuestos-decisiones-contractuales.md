# SOLQARYN V5 — C04-02.M01.04: riesgos, supuestos y decisiones pendientes de contratos SaaS

**Autor:** PRIMARY DEV 04 — JAVIER (`6aeacb7820ac8191b7d768a6f33dfa09`). **Fecha:** 2026-11-14. **Repo/rama:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Clase:** análisis documental M01, sin código, cobros ni modificación de suscripciones.

## 1. Propósito y autoridad

Fuente ejecutable única `STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES` V5 FINAL, Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, fila `PLAN_MAESTRO!617`, microtarea `C04-02.M01.04`: «Registrar riesgos, supuestos y decisiones pendientes». La fase futura exige alta, renovación, pausa, cancelación, morosidad y cierre contractual con máquina de estados auditable. **Esta entrega no resuelve por inferencia las políticas económicas que faltan**.

Fuentes ya inspeccionadas: `c04-02-m01-01-objetivo-no-alcance-contratos-suscripciones.md` (NR01–NR10); `c04-02-m01-02-actores-entradas-salidas-superficies-contractuales.md` (actores y fronteras); `c04-02-m01-03-inventario-contratos-codigo-evidencia.md` (doce blobs verificados). Dominio `SuscripcionesSaaS.cs`, blob `d9b5dbe19fb728293b00b748f10ac10336debd2d`, enumera Activa/Suspendida/Cancelada/Expirada, sin modelar cobros o mora; Application `SuscripcionesSaaSService.cs`, blob `bb501285f11cd4b32709a58a2c51efa19c3664ea`, valida tenant e idempotencia de onboarding. **No se interpretan como autorización comercial ni pruebas de ejecución de este documento**.

Entrada verificada: PLAN H617=PENDIENTE sin owner, COLA G617/O617=SI y DAG F578=VALIDADO/G578=SI antes del claim. Son puertas de preparación documental, no permisos para M01.06/M02 ni para infraestructura.

## 2. Registro de riesgos R01–R12

| Riesgo | Efecto / vector | Control exigible futuro, sin certificar |
| --- | --- | --- |
| R01 — fuga tenant A/B | Usuario A selecciona `empresaId` de B y consulta/alteraría contrato ajeno | Validar `IUsuarioScopeService` antes de IO, test de denegación HTTP/DB |
| R02 — entitlement como RBAC | Plan contratado eleva acción `Configuracion/Crear` sin permiso real | Política backend RBAC separada de plan y membresía |
| R03 — cambio de estado inválido | Se reactiva Cancelada/Expirada o pausa contrato no Activa | Máquina de estados con precondiciones server-side y negatives |
| R04 — renovación retroactiva | Nuevo precio/periodo altera historial de contrato antiguo | Revisión inmutable/versionado y autorización comercial |
| R05 — mora falsa | Timeout del proveedor clasificado como impago sin ledger confiable | Fuente económica autorizada, reconciliación y manejo de incertidumbre |
| R06 — replay financiero | Retry/duplicado provoca dos altas, cargos o transiciones | Idempotencia transaccional, clave por tenant+acción, test de carreras |
| R07 — baja destructiva | Cancelación borra FK, historia o derechos preexistentes | Persistencia histórica, soft change, integridad referencial y rollback |
| R08 — pago no autorizado | Usuario o worker cobra sin consentimiento, precio o fiscalidad aprobada | Gate humano/comercial expreso y permisos antes del runtime |
| R09 — integración insegura | Callback sin firma o de B otorga entitlement a A | Procedencia autenticada, correlación tenant, deduplicación y fail-closed |
| R10 — desalineación UI/API | Angular muestra contrato «activo» pese a rechazo backend | Respuesta backend autoritativa, estados accesibles y test E2E |
| R11 — fuga PII/secretos | Logs exponen JWT, identidad financiera, tokens o facturas | Minimar audit payload, mascarar secretos y prueba de filtración |
| R12 — gates no causales | CI `queued/skipped/cancelled` se declara PASS o snapshot se toma por migración | Check-runs exact-SHA; MySQL efímero/rollback para cambios DB; A5 distinto |

## 3. Supuestos S01–S08 con estado explícito

| Supuesto | Estado / verificación pendiente |
| --- | --- |
| S01 — existe un rol contractual global aprobado | **NO_ACREDITADO**: API actual usa permisos `Configuracion`, no autoridad global de precios |
| S02 — hay moneda y tarifa del plan vigente aprobadas | **NO_ACREDITADO**: DTO leído no incluye importe, impuesto o moneda |
| S03 — periodicidad y días de gracia son uniformes | **NO_ACREDITADO**: no asumir reglas fiscales/comerciales por región |
| S04 — mora puede inferirse de respuesta del proveedor | **RECHAZADO COMO AUTORIDAD**: requiere ledger/reconciliación |
| S05 — estados Domain actuales cubren cierre legal | **NO_ACREDITADO**: son estados técnicos SaaS, falta contrato jurídico/económico |
| S06 — tests unitarios Moq certifican MySQL real | **RECHAZADO COMO EVIDENCIA**: leer tests no equivale a ejecutarlos |
| S07 — la interfaz Angular puede aplicar políticas | **RECHAZADO COMO AUTORIDAD**: backend y membresía deben validar |
| S08 — se permite modificar producción al documentar | **RECHAZADO**: dev-only, nueva orden explícita para otros entornos |

## 4. Decisiones D01–D08 pendientes (ninguna aprobada)

| Decisión futura | Pregunta necesaria para implementación | Estado |
| --- | --- | --- |
| D01 | ¿Quién es autoridad de aceptación contractual y cuál es su RBAC? | PENDIENTE; no crear rol ficticio |
| D02 | ¿Cuál es la versión inmutable del contrato y la fecha de vigencia económica? | PENDIENTE; preservar historial |
| D03 | ¿Qué periodicidad, moneda, impuestos y reglas de precio son autorizadas? | PENDIENTE; no inventar importes |
| D04 | ¿Cuándo se renueva y qué ocurre ante fecha/plan vencido? | PENDIENTE; exigir transición verificable |
| D05 | ¿Cómo se define mora, gracia y pausa sin perjudicar datos legítimos? | PENDIENTE; no inferir por timeout |
| D06 | ¿Qué método de pago/proveedor y consentimiento estarán aprobados? | PENDIENTE; no activar pasarelas |
| D07 | ¿Qué política de cancelación, prorrateo o reembolso aplica? | PENDIENTE; sin cargo/reembolso ahora |
| D08 | ¿Qué eventos, auditoría, retención, recuperación y rollback serán autorizados? | PENDIENTE; sin webhooks ni dispatcher |

Cada decisión requiere evidencia de aprobación conforme al alcance comercial vigente antes de introducir lógica, permiso, contrato de API, migración o efecto financiero. No se convierte la falta de política en una razón para conceder acceso por defecto.

## 5. Oráculos documentales positivos y negativos, NO ejecutados

**P01–P05 (contraste de fuentes):** identificar estados Domain actuales; confirmar validación `ResolverTenantAsync`; localizar atributo de permiso en Controller; localizar ledger de onboarding; encontrar índices EF y sus límites. Son verificaciones de documentación, no pruebas de rendimiento ni integración.

**N01–N10 (futuros casos de denegación):** A accede contrato B; plan suplanta RBAC; usuario revocado cancela; reactivar Cancelada; mora por timeout; replay distinto mismo key; contrato con precio no aprobado; doble cargo por callback duplicado; rollback que borra historia; publicación con Scope Lock FAIL/CANCELLED/SKIPPED/IN_PROGRESS. Los futuros tests deben probar rechazo explícito, no convertir estos escenarios en PASS por mencionarlos.

## 6. Impacto y salida sin sobrecertificación

La entrega crea este único documento y una sección íntegra de `CHANGELOG_AI.md`. No cambia Domain/Application/Infrastructure/API/Angular, datos, migraciones, credenciales, pagos, proveedores, workers, eventos ni automatizaciones; no toca QA/main/PROD. Dependencias de salida: M01.05 (criterios de aceptación) y M01.06 (gate terminal propio), que no se liberan simplemente por registrar riesgos.

**A0:** autoridad/código/diff revisados. **A1:** cinco positivos documentales de contraste con las fuentes. **A2:** diez negativos futuros, tenant/JWT/RBAC/PII, fallos cerrados y riesgo económico descritos. **A3:** coherencia con M01.01–03 y plan vigente. **A4:** commit fast-forward, diff únicamente Markdown y CHANGELOG, Scope Lock y V5 provenance `completed/success` del SHA exacto y readback PLAN/COLA/BITACORA. Builds generales `in_progress` son NO_APLICA al Markdown y nunca se adjudican como PASS.

**A5:** Task Javier distinta de la autora, con registro A:Z e idempotency key; no autopase ni autorización de QA/main/PROD. **Rollback solo documental:** `git revert` ordinario del commit, sin forzar ni reescribir BITACORA. **Resultado correcto:** riesgos, supuestos y decisiones abiertos y trazados; cero reglas económicas nuevas implementadas.
