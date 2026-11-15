# SOLQARYN V17 — C08-03.M01.06 — Bloqueo, rollback y liberación del lote de membresías de gimnasio

**Autor:** PRIMARY DEV 05 — JAVIER; TASK_ID `6adc2e55f47c81919f6374e561687076`. **Fecha:** 2026-11-15. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, C08-03/M01.06. **Riesgo:** R0 — solo diseño documental sin runtime. **Repositorio:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`.

## 1. Propósito, no-alcance y antecedentes

La salida exigida de C08-03 es el diseño futuro de planes comerciales de gimnasio, vigencia, acceso, suspensión y perfil de miembro con **control de acceso, expiración y auditoría**. Esta microtarea establece barreras para M02 y una matriz de reversas; NO implementa membresía, lector, QR, puerta, factura, pago, JWT, endpoint, UI, esquema MySQL ni despliegue. M01.01–05 están LISTO y cuentan con revisiones A5 independientes de otras Tasks Javier, verificadas institucionalmente. C08-03.M01.05 conserva 14 pares de aceptación y ocho NFR como FUTUROS NO_EJECUTADOS. Las predecesoras de fase `C02-04.M01.06+A5` y `C04-01.M01.06+A5`, y documentos C01/C02/C04/C06 se gobiernan por DAG calculado: solo procede escribir si PLAN H=PENDIENTE/I vacía, COLA G/O=SI y DAG F=VALIDADO/G=SI, bajo candado V17. En el CLAIM se verificó `PLAN_MAESTRO!H1231/I1231`, `COLA!G1231/O1231`, `DAG_TECNICO!F1192/G1192`.

La **membresía empresarial de SOLQARYN y su JWT/RBAC** gobierna acceso al SaaS; la **membresía comercial del gimnasio** solo podría conceder un derecho de uso a una persona/sucursal/recurso cuando sea implementada y autorizada. No reutilizar `SuscripcionesSaaS` como plan de gimnasio ni asumir que `Cliente.EmpresaId` opcional acredita pertenencia. El inventario real M01.03 inspeccionó 12 blobs Domain/Application/API/Angular/proyectos net10/EF, pero no certificó infraestructura de acceso físico. Las decisiones D01–D08 de M01.04 siguen PENDIENTES; este documento no las resuelve por inferencia.

## 2. Barreras técnicas y comerciales — B01–B16

| Gate | Evidencia imprescindible para implementar o habilitar | Rechazo fail-closed |
| --- | --- | --- |
| B01 | Repo dev, Skill canónica, Scope Lock STRICT, identidad autorizada | Incompatibilidad de identidad, branch o proveedor |
| B02 | PLAN/COLA/DAG coherentes, owner único, V17 lock y BITACORA causal | Claim sin candado o doble escritor |
| B03 | Cinco predecesores M01.01–05 LISTO+A5 independiente | A5_PENDIENTE/FAIL o revisor autor |
| B04 | Puertas de fase/bloque dinámicas acreditadas por DAG | Cualquier F/G NO o predecesor incompleto |
| B05 | Entidad titular y propiedad tenant/sede verificadas por backend | `Cliente.EmpresaId` nulo se infiere o IDOR A/B |
| B06 | Separación de plan comercial y suscripción SaaS | Un abono de gimnasio concede roles ERP |
| B07 | Semántica UTC e IANA de inicio, fin, DST e instante límite | Reloj cliente o zona ambigua extiende vigencia |
| B08 | Suspensión, baja y revocación invalidadas antes de nuevo uso | Cache/QR antiguo permite entrada residual |
| B09 | RBAC de lectura, alta, suspensión, renovación, acceso y auditoría separados | Rol lectura concede altas o reactivaciones |
| B10 | Tenant/sucursal/recurso derivados de autoridad y roles | Plan A abre recurso B o sede ajena |
| B11 | QR/lector opcional bajo contrato de confianza y TTL | QR replay o lector offline abre por defecto |
| B12 | Check-in atómico, versionado, clave+fingerprint idempotente | Doble acceso concurrente supera cupo |
| B13 | Renovación y pagos solo tras fuente PSP autorizada y verificada | Callback no firmado activa sin pago |
| B14 | Auditoría mínima tenant/actor/acción/motivo/UTC sin PII ni secretos | Registro con JWT, QR permanente o datos de B |
| B15 | xUnit/HTTP/EF MySQL 8.4 efímero/Playwright pertinentes exact-SHA | Build verde aislado o CI CANCELLED como PASS |
| B16 | A0–A4 y A5 independiente, cambios reversibles, dev únicamente | Activar dispositivo/PSP/PROD/QA/main por documento |

**Barrera universal:** la falta de una sola evidencia aplicable conserva el NO_GO para la operación que la necesita; no forzar `DAG!F/G` ni `COLA!G/O`, no inventar cuentas ni decisiones de negocio, no interpretar `VALIDANDO` como LISTO+A5.

## 3. Casos de aceptación futuros — E01–E12 (NO_EJECUTADOS)

| ID | Positivo futuro | Negativo futuro / prueba requerida |
| --- | --- | --- |
| E01 | Persona A autorizada consulta solo su perfil A | Identificador de B o `EmpresaId` falsificado: HTTP 4xx sin fuga |
| E02 | Plan activo concede solo recurso/sede permitido | Plan SaaS no sustituye plan gimnasio; JWT/RBAC negativos |
| E03 | Vigencia cubre instante permitido UTC | En borde de expiración/DST se deniega sin extender por reloj cliente |
| E04 | Suspensión/renuncia invalida acceso inmediato | QR o sesión cacheados después de revocación: deny |
| E05 | Recepción con permiso realiza check-in de sede A | Operador B/rol lectura no registra acceso A |
| E06 | Reintento idéntico produce un solo check-in | Doble request concurrente o clave/payload distinto no duplica en MySQL efímero |
| E07 | Consulta de cupo/uso coincide con DB | Dos accesos simultáneos no superan cuota ni pisan versión |
| E08 | Renovación auténticamente pagada produce una vigencia | Callback PSP no firmado, replay o doble cobro nunca autoriza entrada |
| E09 | Interrupción transaccional conserva integridad | Fallo a mitad de registro revierte o compensa sin mezclar tenants |
| E10 | Acceso de soporte audita mínimo autorizado | Log/URL/export no expone PII, JWT, QR secreto ni usuario B |
| E11 | UI accesible anuncia estado permitido real | Offline/401/403/409/timeout no muestra ingreso concedido ficticio |
| E12 | Rechazo de lector desconectado deja evidencia | QR clonado/expirado, offline o error de zona no abre hardware |

**Ninguno E01–E12 se ejecutó:** son oráculos diseñados. Cambios futuros con DB/EF requieren MySQL 8.4 efímero y pruebas A/B, cambios de UI requieren Angular/Playwright Chromium, y permisos requieren suites JWT/RBAC/revocación/HTTP reales; resultado `in_progress`, `cancelled`, `skipped`, `failed` no es PASS.

## 4. Matriz de reversión — RB01–RB08

| Ruta | Fallo futuro y reversa segura exigida |
| --- | --- |
| RB01 Documento | Divergencia del contrato: corregir aditivamente o `git revert` normal con evento BITACORA; preservar otros autores |
| RB02 Ownership | Titular/tenant no acreditado: denegar y reconciliar identidad sin asignar EmpresaId arbitrario |
| RB03 Vigencia | Ambigüedad UTC/DST: rechazar temporalmente, reparar reglas con test de frontera |
| RB04 Revocación | Cache/QR no invalidado: cerrar acceso y revocar tokens con auditoría, no permitir uso anterior |
| RB05 Concurrencia | Doble check-in: transacción, idempotencia y rollback/compensación MySQL verificables |
| RB06 PSP | Pago no verificado o callback duplicado: no renovar, conciliar autoritativamente sin cargo falso |
| RB07 UX/hardware | Offline o timeout: estado pendiente/denegado, sin abrir puerta ni mostrar éxito ficticio |
| RB08 Gobierno/CI | A5_FAIL, CI REQUIRED FAILED, owner/lock ambiguo: mantener estado real, reparación causal y readback; no forzar DAG |

RB02–RB08 son exigencias **futuras** y no acciones realizadas; hoy solo aplica revert Git normal del documento/CHANGELOG en dev, sin rewrite, con BITACORA aditiva.

## 5. A0–A5 y condición exacta de liberación

**A0:** revisar requisitos y diff exclusivamente `docs/v5` + `CHANGELOG_AI.md`, puertas PLAN/COLA/DAG, propietario, candado V17 y ausencia de secretos; el contrato contiene B01–B16, E01–E12 y RB01–RB08. **A1:** contraste documental de positivos/negativos, fronteras UTC/DST, suspensión y concurrencia; no declarar tests de runtime. **A2:** revisar preservación de JWT/RBAC, tenant A/B, PII, QR, rol revocado y denegación offline como especificaciones; no confundirlo con test de seguridad real. **A3:** conciliar las cinco M01 y el inventario de 12 blobs con arquitectura; para el diff documental xUnit/HTTP/Angular/MySQL/Playwright = **NO_APLICA**, no PASS ficticio. **A4:** publicar en dev por fast-forward/CAS, comprobar check exact-SHA `verify-solqaryn-project-scope` y procedencia V5 `completed/success`, más PLAN/COLA/BITACORA readback y rollback normal. **A5:** un revisor de Task Javier diferente de DEV05 auditará por separado SHA, evidencia y criterios, podrá rechazar defectos; LISTO_DEV de M01.06 no sustituye A5.

**Salida M01.06:** documento publicado, A0–A4 causalmente demostrados y estado LISTO en PLAN. **Liberación M02:** solo al acreditarse las puertas de `C08-03.M01.06+A5` y demás antecedentes exigidos en el **DAG vivo**, junto con PLAN H=PENDIENTE/I vacío y COLA G/O=SI; no habilitar M02 durante VALIDANDO ni por documentación provisional. Las decisiones D01–D08 siguen PENDIENTES antes de codificar accesos, pagos, dispositivos, DB o permisos. No ejecutar QA/main/PROD, compras, secretos, migraciones, nuevas Tasks, webhooks ni hardware.

## 6. Impacto y trazabilidad

M01.01 alcance NR01–NR14/P01–P06/N01–N14; M01.02 actores A01–14 e interfaces I01–14; M01.03 inventario causal de 12 rutas Git; M01.04 R01–14/S01–08/D01–08 todavía pendientes; M01.05 matriz de aceptación y NFR futuras. Ninguna capa Domain, Application, API, Infrastructure, MySQL, Angular, seguridad activa, cuenta o proveedor cambia con este trabajo. Observabilidad futura mínima: actor, tenant, sede, recurso, tiempo UTC, decisión, correlación y motivo, con saneamiento de secretos/PII. Rollback de este documento mediante `git revert` sin force-push, preservando BITACORA; no se demuestra ingreso físico, cobro, check-in ni autorización de otro entorno.
