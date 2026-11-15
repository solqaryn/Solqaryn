# SOLQARYN V5 FINAL — C15-01.M02.04: configuracion tipada, ambitos y flags seguros de auditoria de cobertura

**Autor:** PRIMARY DEV 06 JAVIER_CHATGPT, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Ambito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. Este es un **contrato documental no implementado**. No agrega configuracion runtime, parametros, nuevos secretos, permisos, proveedor, Tasks, eventos ni migraciones.

## 1. Autoridad y modelo previo

Fuentes: [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [Sheet institucional](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), [M01.06](c15-01-m01-06-bloqueos-rollback-liberacion-cobertura.md), [M02.01](c15-01-m02-01-modelo-entidades-ownership-trazabilidad-cobertura.md), [M02.02](c15-01-m02-02-contratos-api-dto-errores-compatibilidad-cobertura.md) y [M02.03](c15-01-m02-03-persistencia-indices-migraciones-fixtures-cobertura.md). El modelo CT01–CT16, OP01–OP08 y PD01–PD12 es conceptual y los tests son FUTUROS_NO_EJECUTADOS.

En preflight PLAN H2171 PENDIENTE I vacia; COLA G2171/O2171=SI; DAG A2132=C15-01.M02.04, F=VALIDADO/G=SI. Owner se adquiere por CAS y readback, no por prompt ni celda de telemetria. Gobierno real actual en AGENTS.md, docs/PROJECT_SCOPE_LOCK.md y docs/AUTOMATION_STATUS.md; ninguna clave CFG listada aqui se encuentra autorizada como flag runtime sin posterior implementacion valida.

## 2. Ambitos jerarquicos y fuentes de configuracion

| Ambito | Autoridad | Lectura | Prohibiciones |
| --- | --- | --- | --- |
| PLATAFORMA | Skill canónica, AGENTS.md, scope lock, codigo | Reglas inmutables para este contrato | No alterar allowlist ni versiones por UI |
| PROYECTO/dev | HEAD Git exacto y gobierno autentico | Version, restricciones de dev, rollback | No cambiar main, QA ni PROD |
| AUDITORIA_C15 | STANDARD V5 y snapshot del plan | Modelo, revision de fuentes y universo | No inventar cobertura ni reemplazar fuente |
| MICROTAREA | PLAN H/I/K, COLA G/N/O, DAG F/G | Estado, owner, gate, A5 | Nunca escribir G/O/F/G para lograr SI |
| EJECUCION | Automations.peek, BITACORA, fila AA:AN | Estado nativo frente a ultimo autorreporte | No tratar clave local como RUN_ID |
| TENANT/SUCURSAL | Backend validado JWT/RBAC/tenant | Solo datos permitidos por autoridad real | Client-side selector no concede acceso |

Regla de precedencia: ningun ajuste de nivel inferior relaja las restricciones superiores; la UI no determina autorizacion. Los cambios futuros son tipados, allowlisted, con default fail-closed, versionados y observables. Una configuracion textual de este documento no cambia el runtime.

## 3. Catalogo prospectivo de configuracion

| Codigo | Clave propuesta | Tipo/default | Ambito | Condicion negativa |
| --- | --- | --- | --- | --- |
| CFG01 | AuditEnabled | boolean=false | proyecto/dev | Desactivado; ningun hook ni tarea nueva |
| CFG02 | SourceRevision | string immutable / requerido en snapshot | corte auditoria | Rechazar revisions mezcladas |
| CFG03 | DevSha | SHA40 vinculado a corte | repositorio dev | No aceptar desde frontend |
| CFG04 | AuditUniverseSize | integer 2340 como referencia documental | fuente V5 | Conteo no implica cobertura real |
| CFG05 | MaxPageSize | integer 1..100 / default 25 | lectura | Capar paginacion, no enumerar |
| CFG06 | ScanBudget | integer >=0 acotado por proveedor | corrida manual | 0 = no ejecutar, nunca infinito |
| CFG07 | AllowedTaskIds | set exacto de seis task IDs Javier | gobierno canónico | No agregar Task por input |
| CFG08 | TenantIsolationMode | enum STRICT_FIXED | backend | No permitir deshabilitar RBAC |
| CFG09 | EvidenceRedaction | enum MINIMIZED_ONLY | audit report | No guardar secretos ni PII |
| CFG10 | A5ReviewPolicy | enum DIFFERENT_AUTHORIZED_TASK | microtarea | No autoaprobacion |
| CFG11 | DagGateMode | enum FORMULA_READ_ONLY | Sheet | No forzar G/O=SI |
| CFG12 | WriteMode | enum DRY_RUN_ONLY | auditoria propuesta | Sin mutaciones externas |
| CFG13 | FailureMode | enum FAIL_CLOSED | errores externos | NO_APLICA no es PASS |
| CFG14 | TelemetryMode | enum LAST_REPORT_ONLY | TAREAS_PROGRAMADAS AA:AN | No afirmar heartbeat nativo |
| CFG15 | FutureWebhookEnabled | boolean=false | autorizacion eventual | Nunca activar ahora |
| CFG16 | DeploymentScope | enum DEV_ONLY | entorno | QA/main/PROD no autorizados |

**CFG01–CFG16 son nombres PROPUESTOS_NO_IMPLEMENTADOS**, no claves agregadas a .NET appsettings, Angular environments, CONFIG! ni base de datos. La fuente de six Tasks Javier es el allowlist canónico, no una variable libre. Ningun campo de cliente permite alterar tenant, rol, scope o evidencia de CI. Configuracion de proveedores externos y secretos no se agrega ni se lee a traves de esta interfaz conceptual.

## 4. Casos positivos/negativos futuros

| Codigo | Positivo contractual | Negativo que debe rechazarse |
| --- | --- | --- |
| VAL01 | Snapshot SHA40 y revision deben ser coherentes | SHA inexistente/revision obsoleta -> rechazo |
| VAL02 | Solo seis IDs Javier autenticados | Task externa no se agrega por lista textual |
| VAL03 | MaxPageSize dentro de rango | 0, -1 o millones se rechazan |
| VAL04 | ScanBudget limita llamadas reales | Sin presupuesto no se dispara ciclo |
| VAL05 | AuditEnabled=false por defecto | Ninguna Task se inicia por configurar |
| VAL06 | JWT/RBAC/tenant no configurables desde web | Header EmpresaId no cambia fuente |
| VAL07 | Gate no se escribe manualmente | G/O calculado NO sigue NO |
| VAL08 | A5 debe tener autor distinto | Autoaprobacion falla |
| VAL09 | Release Scope DEV_ONLY | QA/main/PROD requiere orden nueva |
| VAL10 | PII/redaction minima por defecto | Token en evidencia es error |
| VAL11 | Errors conservan unknown/failure | CANCELLED no equivale PASS |
| VAL12 | Feature flag futuro sigue disabled | Ningun webhook ni eventos activos |

**VAL01–VAL12 permanecen FUTUROS_NO_EJECUTADOS.** La capacidad descrita no constituye servicio, dashboard nuevo, cambio en PLAN/COLA/DAG, proteccion contra carreras ni prueba de SLO. No se declara cero huerfanos, cero duplicados criticos, ni cero fallos P0/P1.

## 5. Controles de cambio, dependencias y rollback

1. Diseño de lectura: cualquier futura implementacion debe pedir autorizacion explicita antes de crear flags reales, demostrar tipado, resolver fallback seguro y contar con tests .NET/Angular/MySQL segun diff causal.
2. Reglas por ID: PLAN H/I/K solo se cambia mediante transiciones autorizadas, claim CAS, evidencia A0–A4 y A5 distinto; DAG y COLA se recalculan y se leen tras cada transicion, sin forzar valores.
3. Identidad y pertenencia: JWT y RBAC verificados por backend; el usuario no decide EmpresaId o rol por parametros. Logs, reportes y snapshots eliminan secretos y minimizan PII.
4. Fallos: provider unavailable, CI queued/in_progress, cancelled o failed se conservan como indeterminados/error; nunca PASS ni desbloqueo terminal. No se lanzan procesos externos ni run_now o webhooks no instalados.
5. Promocion: incluso LISTO_DEV y A5 independiente solo acreditan desarrollo y revision de su alcance; QA/main/PROD, Cloudflare/Hostinger, DNS, secrets y MySQL productiva quedan fuera de autorizacion.
6. Rollback documental: git revert normal de changeset, Scope Lock y V5 nuevos; BITACORA historica append-only. Revertir configuracion runtime requeriria contrato posterior y ensayos propios.

## 6. Auditorias A0–A5

- **A0:** verificar 16 CFG, 12 VAL, las seis Tasks Javier, documentos fuente y exclusiones; diff solo de Markdown y CHANGELOG.
- **A1:** consistencia positiva/negativa del contrato, sin pruebas de parser/backend ejecutadas; VAL futuras. **A2:** tenant/JWT/RBAC/PII sin cambio runtime, NO_APLICA HTTP para Markdown, nunca PASS inventado. **A3:** referencia documental al gobierno dev y M02.01–03, sin mutar CONFIG, formulas, API ni servicios.
- **A4:** publicacion fast-forward CAS en dev, checks `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` completed/success exact SHA y readback PLAN/COLA/DAG/BITACORA. Jobs de backend/frontend no causales pueden seguir en curso, no se cuentan PASS.
- **A5:** revision por TASK_ID Javier diferente de DEV06 sobre diff, blob y SHA; pendiente de otro revisor. Rollback git revert. El contrato no autoriza trabajo de runtime, QA/main/PROD ni event dispatcher.
