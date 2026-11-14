# SOLQARYN V5 FINAL — C01-01.M02.06: dependencias técnicas y contrato de salida M02 de identidad universal

**Clase:** contrato terminal documental de M02. **Autor:** PRIMARY DEV 02 — JAVIER; TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Repositorio:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`.

**Resultado exigido:** fijar las dependencias técnicas y las condiciones verificables de cierre de M02 para tenant, empresa, sucursal, usuario, membresía, rol, recurso y contexto. **Esta microtarea NO implementa entidades, DTO, endpoints, cambios de JWT/RBAC, migraciones SQL/EF, configuración efectiva, workers, dispatcher, webhooks, eventos ni nuevas Tasks.** Su cierre documental no equivale a entrega funcional de identidad universal ni habilita automáticamente M03/M06/QA/main/PROD.

**Autoridad única:** [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [Sheet institucional](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit) (PLAN_MAESTRO, COLA, DAG_TECNICO, BITACORA). No se admiten PR, ramas nuevas, cuentas retiradas ni cola histórica.

## 1. Preflight de M02.06 y separación entre elegibilidad y liberación

La evaluación individual comprueba **14 predecesores `LISTO`** en PLAN_MAESTRO: `C01-01.M01.01–06` (seis), `C01-01.M02.01–05` (cinco), `C00-01.M06.06`, `C00-03.M01.06` y `C00-03.M06.06` (tres). Antes del claim de esta unidad, `PLAN!H157=PENDIENTE` e `I157` libre; `COLA!G157/O157=SI`. El nuevo gate documental específico en `DAG_TECNICO!A143:J143` contiene los 14 IDs, `F143=VALIDADO/G143=SI`, alcance `DOC_TERMINAL_M02_DEPENDENCY_AND_EXIT_CONTRACT_ONLY` y readback de diez campos.

El gate habilita solamente el **claim exclusivo para redactar este contrato**. No convierte el fallback `COLA!Q` en permiso para otras 96 candidatas sin DAG individual, ni reemplaza A0–A4, revisión A5 independiente o pruebas en código. El claim modifica exclusivamente el ID `C01-01.M02.06` y preserva propietarios de otras microtareas.

## 2. Inventario causal de cinco contratos M02 predecesores

| ID, artefacto y blob Git | Contrato documental existente | Qué falta certificar al implementar |
| --- | --- | --- |
| M02.01 `docs/v5/c01-01-m02-01-contratos-entidades-ownership-invariantes.md` — `6545f81125d0115b9bfdd0e24daee6d0edbe490a` | Empresa, sucursal, usuario, `UsuarioEmpresa`, rol tenant, ownership y 12 invariantes | Ejecución real del dominio, denegación de recursos A/B y persistencia |
| M02.02 `docs/v5/c01-01-m02-02-contratos-api-dto-errores-compatibilidad.md` — `54f7570b606829b41bfb9363157f31a3fc0ba8c0` | Rutas actuales, contratos API/DTO y respuestas 200/400/401/403/404/409/429/503 | Pruebas HTTP/TestServer, claims JWT, compatibilidad de consumidores |
| M02.03 `docs/v5/c01-01-m02-03-persistencia-indices-migraciones-fixtures.md` — `b3945fb9329034210027cd456c7aaff604c4e4cd` | Índice único `UsuarioId,EmpresaId`, FK restrictivas, `Sucursal.EmpresaId` legacy nullable, fixtures simbólicos y riesgos de `Down` | Prueba SQL/EF MySQL 8.4 efímero, migración y rollback no destructivo; ninguna conexión productiva |
| M02.04 `docs/v5/c01-01-m02-04-configuracion-tipada-ambitos-flags.md` — `aa1b2b1c999de60f9bf7fef07f877bc023a048ac` | `ConfigEmpresa`, referencias opacas de secretos, flags fail-closed, precedencia experimento/tenant/plan | Pruebas de configuración efectiva, caché por tenant y que un entitlement no sustituye RBAC |
| M02.05 `docs/v5/c01-01-m02-05-contrato-eventos-idempotencia-correlacion.md` — `40895ff417b172f862f671138d88424ea28024fb` | CorrelationId separado de clave idempotente y de EmpresaId; outbox y límites de retry como contrato futuro | Integración real, ack, deduplicación durable, race/transaction; **no autoriza implementar dispatcher** |

El contrato M01.06 es `docs/v5/c01-01-m01-06-gates-bloqueo-rollback-identidad.md`, blob `3cc98bf22a0ef0bf90e51f3d128462299546b33d`. Distingue la **posibilidad de evaluar** M02 de la autorización para modificar código. En BITACORA existen revisiones A5 documentales independientes válidas de los cinco M02.01–05, filas **3355, 3357, 3358, 3365, 3370**; sus autores originales permanecen registrados. Una revisión de cuenta no autorizada no reemplaza una A5 de Javier.

## 3. Matriz de consumidores, conflictos y pruebas futuras

| Futuro consumidor o área | Entrada y dependencia que debe revalidar | Gate exigible para código |
| --- | --- | --- |
| Domain/Application .NET 10 | Entidades, roles por membresía activa, invariantes M02.01; usuario global nunca otorga permiso tenant | build/test .NET exact-SHA; casos positivos A y negativos B, revocación, rol inactivo y propiedad de sucursal/recurso |
| API HTTP, filtros y servicios | M02.02; `EmpresaId` de header/URL es selector, no autoridad; JWT auténtico + permiso de acción y recurso | TestServer/API real y seguridad RBAC/tenant; 401/403, 400/409 y no fuga de PII; Scope Lock |
| EF Core 10.0.12, MySql.EntityFrameworkCore 10.0.9, MySQL 8.4 | M02.03; esquema real, FK, unique, migración `Down` potencialmente destructiva | integración MySQL efímera sobre SHA, revisión transaccional y rollback no destructivo; NUNCA Aiven/PROD por este contrato |
| Angular 22.2.1 | Contratos de contexto/DTO M02.02 y configuración M02.04; solicitud de empresa no es grant | npm/build/lint/test y E2E/accesibilidad según UI; invalidar respuesta tardía de tenant anterior |
| Flags/plan y configuraciones | M02.04; entitlements, precedencia, cache, version y referencias secretas | tests de decisiones fail-closed y aislamiento por empresa; no valores secretos en repo |
| Eventos/observabilidad, **solo diseño futuro** | M02.05; idempotencia por `EmpresaId`, ACK y límites de reintento | ningún evento real ni mecanismo Task→Task en esta microtarea; exigir una autorización ejecutable distinta si algún día procede |

**Riesgos abiertos:** usuario legacy sin membresía, sucursal sin owner, rol cruzado A/B, respuestas tardías en Angular, excepción con PII, cache/flags compartidos entre empresas, race de clave idempotente y rollback SQL destructivo. Ninguno se declara resuelto por la existencia de documentación.

## 4. Contrato de salida y condiciones que NO libera

La salida **documental M02** se puede certificar solo cuando los seis IDs M02.01–06 figuren realmente `LISTO`, cada uno con evidencia A0–A4 proporcional, commit causal cuando aplique, Scope Lock exact-SHA, BITACORA A:Z idempotente y revisión A5 por distinta Task Javier antes de cualquier promoción. M02.06 no produce un `A5_PASS` propio; se registra `A5_PENDIENTE` al cerrar DEV.

Cada ID posterior M03/M04/M05/M06 requiere un **nuevo DAG individual `F=VALIDADO/G=SI`**, PLAN H=PENDIENTE/I libre, COLA G/O=SI, análisis de cambios reales y exclusión de escritores; no usar el fallback de fase ni la meta de capacidad `MAX(24,3*N)` como habilitación. El cierre de contrato **no autoriza** IAM/JWT/RBAC activos, cambios tenant, servicios de integración, SQL/migraciones, despliegues ni QA/main/PROD. Se mantienen bloqueos A5_FAIL de otros scopes hasta nueva corrección y revisión causal.

## 5. Oráculos de aceptación documental — no son pruebas ejecutadas de runtime

| Caso | Tipo | Verificación del contrato |
| --- | --- | --- |
| P01 | positivo | Los 14 predecesores están LISTO, la fila DAG143 contiene sus IDs y F=VALIDADO/G=SI |
| P02 | positivo | Los cinco M02 tienen rutas y blobs verificables, autor distinto de reviewer A5 |
| P03 | positivo | Una petición de usuario con membresía activa A y rol/ownership A debe tener un camino de autorización **en implementación futura**, no por afirmar un header |
| P04 | positivo | Cambios solo Markdown requieren diff/árbol, integridad de fuentes, Scope Lock y CHANGELOG exact-SHA con readback |
| N01 | negativo | Un M03 sin DAG individual se mantiene no elegible aunque COLA Q/G/O muestre SI por fallback |
| N02 | negativo | Un token A, header de B, rol global o frontend no concede permiso a empresa B |
| N03 | negativo | `Sucursal.EmpresaId=null` o propietario ausente obliga a denegar operación tenant |
| N04 | negativo | A5 de la autora o una cuenta fuera del allowlist no es revisión independiente |
| N05 | negativo | Migración `Down` que elimina `UsuarioEmpresas` no es rollback seguro sin análisis de pérdida de datos |
| N06 | negativo | Test sintético, contrato escrito o job SKIPPED no demuestra MySQL, JWT, HTTP ni eventos en vivo |
| N07 | negativo | Callback, webhook, dispatcher o Task→Task recurrente no puede activarse por el documento M02.05/06 |
| N08 | negativo | Git HEAD obsoleto, dos owners, BITACORA truncada/duplicada o check required pendiente no justifican LISTO |

Estos oráculos son **inspecciones de texto y fuentes**; no se presenta ejecución de solicitudes HTTP, MySQL, suites .NET, Angular, Docker, Playwright, JWT real ni producción.

## 6. A0–A5 y rollback

- **A0:** inspección del diff Markdown, blobs de cinco fuentes + M01.06, referencias exactas, exclusión de secretos, procedencia de CHANGELOG y encabezados no vacíos.
- **A1:** comprobar los contratos positivos/negativos P01–P04/N01–N08 y correspondencia con RESULTADO_EXIGIDO; no simular ejecución runtime.
- **A2:** revisión estática de límites A/B/JWT/RBAC/PII, secretos por referencia, flags como no-permisos y fail-closed; no se altera ninguna autorización activa.
- **A3:** comprobar 14/14 predecesores `LISTO`, DAG143 validado, cinco contratos en blobs y consumidores; .NET/Angular/MySQL/Docker/E2E `NO_APLICA` para el diff estrictamente documental.
- **A4:** publicar commit fast-forward directo `dev` sin PR/rama, comprobar SHA/blob/diff, checks nativos requeridos `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` con head_sha exacto, y readback PLAN/COLA/BITACORA; `SKIPPED/IN_PROGRESS/FAIL` no es PASS requerido.
- **A5:** `A5_PENDIENTE` después de A0–A4, un revisor entre las **otras cinco** Tasks Javier; no autopromoción.
- **Rollback:** `git revert` normal del commit documental tras revalidar HEAD y evento compensatorio append-only; jamás force-push, reset, borrado de BITACORA, alteración de permisos/datos productivos o activación de un worker.

**Resultado de esta unidad:** mapa de dependencias y condiciones de salida **solamente documentales**. Toda implementación posterior debe justificarse y probarse por su microtarea independiente.
