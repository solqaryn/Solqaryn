# SOLQARYN V5 FINAL — C00-04.M02.06: dependencias técnicas y contrato de salida de M02

**Clase:** especificación documental de cierre de contratos del gobierno de cambios. **Autor:** PRIMARY DEV 05 JAVIER, `TASK_ID=6adc2e55f47c81919f6374e561687076`. **Repositorio:** `solqaryn/Solqaryn`, rama `dev`, `PROJECT_SCOPE_LOCK=STRICT`.

**Resultado exigido por el Standard V5:** fijar dependencias técnicas y el contrato de salida del lote `C00-04.M02`. Este texto **no implementa** el gobierno de cambios, dispatcher, eventos, webhook, worker, outbox, API, DTO ejecutable, servicios, cambios de tenant/JWT/RBAC, base de datos ni migración. No abre por sí solo `M03` y no permite pasar a QA, main ni PROD.

## 1. Fuente, condición inicial y prueba de independencia

- Autoridad ejecutable: `STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES`, documento Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. Estados y elegibilidad en Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`: `PLAN_MAESTRO!H/I`, `COLA!G/O`, `DAG_TECNICO!F/G`, registro histórico append-only en `BITACORA!A:Z`.
- Puerta individual `DAG_TECNICO!A151:J151` únicamente para `C00-04.M02.06`: F=`VALIDADO`; G es **fórmula dinámica**, nunca un permiso fijo. Se exige `C00-04.M01.01–06` y `C00-04.M02.01–05` en LISTO con revisiones A5 independientes; asimismo `C00-01.M06.06` y `C00-02.M06.06` terminales LISTO con A5 vigente. Son 13 antecedentes con dirección hacia este contrato, sin dependencia circular.
- Antes de reclamar se acreditaron `PLAN H110:H120=LISTO` (11/11), `K110:K114` cinco A5 correctivas, `K115:K120` seis A5_PASS, `H37/K37` y `H73/K73` LISTO/A5, la revisión independiente `C00-04.M02.05` por Javier :40 en BITACORA fila 3456 y `COLA G121/O121=SI` con H121 PENDIENTE/I121 vacío. El reclamo usa transición exclusiva registrada y lectura posterior; esta prueba no autoriza otras unidades.
- La inspección de publicación parte del HEAD `96fd1b673a5e3d21e10c523b7fb5daec70fccbf3` (Scope Lock y procedencia V5 success exact-SHA) y los documentos M02.01–05 ya presentes. Después de publicar, comprobar el SHA causal propio, no reutilizar el PASS del padre.

## 2. Mapa de contratos que se entregan a una futura M03

| Contrato precedente | Fuente del proyecto | Requisito a conservar antes de codificar |
| --- | --- | --- |
| Entidades, propiedad, invariantes | `c00-04-m02-01-entidades-ownership-invariantes.md` | La decisión tiene actor y autoridad comprobados; una evidencia no otorga aprobación |
| API, DTO, validación y errores | `c00-04-m02-02-contratos-api-dto-errores-gobierno.md` | Nunca tomar tenant, rol o estado ejecutable de campos aportados por el cliente |
| Persistencia, índices y fixtures | `c00-04-m02-03-persistencia-indices-migraciones-fixtures-gobierno.md` | Un esquema conceptual no crea tablas ni sustituye CAS/transacción real |
| Configuración tipada y flags | `c00-04-m02-04-configuracion-tipada-ambitos-flags-seguros-gobierno.md` | Flags no elevan permisos ni sustituyen JWT/RBAC/Scope Lock; default denegado |
| Eventos e idempotencia | `c00-04-m02-05-event-idempotency-document-only.md` | Diseño no ejecutable; eventos no equivalen a estados, leases ni Tasks |

Todos están bajo `docs/v5/` y describen contratos, no implementaciones ya probadas. Una futura implementación debe derivar casos de uso y pruebas de la arquitectura vigente Domain ← Application ← Infrastructure, API como composición, Angular 22.2.1, .NET 10, MySQL EF Core 10.0.12 y proveedor Oracle 10.0.9. Ninguna dependencia de arquitectura se sustituye por este documento.

## 3. Contrato de salida M02 (condiciones verificables)

La salida documental de M02 está **definida**, pero no es autorización para iniciar código de seguridad o despliegue. Para aceptar el paquete M02 en DEV, la siguiente Task debe comprobar conjuntamente:

1. **Identidad y trazabilidad:** los seis IDs M02.01–06 aparecen una sola vez en PLAN, con sus documentos/versiones, owners reales, evidencia de Git y Scope Lock exact-SHA. Las revisiones A5 de cada autor deben provenir de otro TASK_ID Javier; el A5 de esta unidad se produce **después** y no puede hacerlo su autora.
2. **Cierre de dependencia:** validar contra el DAG vigente, no contra un contador global: M01 terminal, antecedentes C00-01/C00-02 y revisiones causales válidas; el estado VALIDANDO no certifica una fase terminal, DB, IAM, JWT, RBAC ni despliegue.
3. **Contrato de datos y de autorización:** los futuros comandos de decisión no deben aceptar EmpresaId, actor, scope, rol o estado concedidos por URL/header/payload. El backend resuelve identidad y tenant y falla cerrado si falta binding, rol, versión, evidencia, lease o política.
4. **Atomicidad real pendiente:** diseño de IdempotencyKey, revisión de estado y evidencia no prueba CAS en Sheets, transacciones MySQL ni entrega exactly-once. Cualquier persistencia futura necesita implementación explícita y pruebas de concurrencia, rollback y separación tenant A/B.
5. **Publicación y no-regresión:** diff limitado a Markdown y CHANGELOG, sin encabezados huérfanos ni viñetas cruzadas. Comprobar `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` en `completed/success` para el **nuevo** SHA; los jobs no requeridos por la edición Markdown son `NO_APLICA` y nunca PASS inventado.
6. **Autorización de sucesoras:** M03.01–06, M04–M06, cambios funcionales, CI/runtime, QA/main/PROD necesitan DAG individual, pruebas pertinentes, permisos, reservas únicas y alcance aprobado. No se liberan por este documento ni por A5 aislado.

## 4. Matriz de riesgos, fronteras y reversión

| Riesgo | Prevención contractual y prueba futura | Si no se demuestra |
| --- | --- | --- |
| Autorrevisión o Task retirada | Autor y revisor deben ser dos de los seis TASK_ID Javier distintos | A5 denegado; historial antiguo sin autoridad |
| Tenant A intentando leer o decidir para B | Resolver tenant server-side, exigir RBAC real, registrar denegaciones sin PII | 403/denegación; cero filtración y cero side effect |
| Reintento duplicado/conflictivo | Huella canónica y clave de intención por tenant/scope; comparación de revisión | Idempotente o conflicto auditado, sin doble transición |
| Carrera entre dos escritores | Reserva atómica real con un ganador y readback; jamás confiar en get+update como CAS distribuido | NO_GO y conservación del responsable previo |
| Migración irreversible o proveedor incorrecto | Plan de rollback, fixture MySQL efímero y pruebas EF/integración si se autoriza runtime | No migrar ni certificar persistencia |
| Evento/retry infinito o costos | Cuotas, backoff acotado, idempotencia, no worker ni webhook actual | No despachar, comprar ni ejecutar tareas adicionales |
| Check faltante/cancelado/omitido | Requerido = `head_sha` exacto y `completed/success` | Conservar VALIDANDO, sin PASS |
| Intercalado de CHANGELOG | Encabezado seguido inmediatamente por viñetas propias, comparación con padre y check V5 | Corregir en SHA nuevo y revalidar antes de A5 |

**Rollback de esta entrega:** `git revert` ordinario del commit Markdown en `dev`, verificando HEAD y descendientes; no force-push ni reset, no borrar BITACORA ni cambiar propietarios históricos. Ante error de estado institucional, crear evento compensatorio, no editar eventos antiguos.

## 5. Oráculos de aceptación documental (no pruebas ejecutadas)

| Caso | Entrada conceptual | Resultado contractual |
| --- | --- | --- |
| P01 | 13 dependencias LISTO/A5, DAG151 VALIDADO/SI, scope libre | Habilitar exclusivamente documentación M02.06 |
| P02 | Seis contratos M02 y fuentes originales coherentes | Matriz de handoff completa con no-alcances |
| P03 | Tenant A y rol autorizado en futura operación | Backend valida identidad, scope, revisión y evidencia |
| P04 | Reversión del documento sin cambios de runtime | Git revert normal y BITACORA histórica conservada |
| N01 | Tenant B elegido por cliente A | DENY sin filtración ni transición |
| N02 | Actor retirado o que revisa su propia obra | DENY aunque aparezca en auditoría histórica |
| N03 | Prefiltro COLA SI sin DAG validado o sin owner libre | NO_GO al claim |
| N04 | Predecesora VALIDANDO, A5_FAIL o SHA incorrecto | No habilitar contrato terminal ni M03 |
| N05 | Doble escritura o falso CAS por llamadas Sheets separadas | No certificar exclusividad ni completitud |
| N06 | Evento hipotético pretende hacer Task→Task o webhook | No ejecutarlo ni instalar dispatcher |
| N07 | JWT/rol/tenant sin binding, secreto en documento | DENY y bloquear publicación |
| N08 | MySQL/migración solicitada por especificación Markdown | NO_APLICA como ejecución, no tocar DB |
| N09 | CI requerida queued, failed, skipped o cancelled | No PASS ni LISTO por inferencia |
| N10 | Promoción automática a QA/main/PROD por M02 cerrado | DENY; autorización separada |
| N11 | Nuevo flag cambia roles, cuota elimina controles o permite cobros | DENY, no activar ni comprar |
| N12 | Check V5 encuentra encabezado vacío o viñetas ajenas | Corregir procedencia y requerir SHA/check nuevo |

**Todos los P/N son criterios inspeccionables sobre el contrato; no se ejecutaron HTTP, JWT, RBAC, MySQL, Angular, Playwright ni pruebas de transacciones a través de este documento.** Un desarrollador de M03 deberá crear y ejecutar sus propios casos positivos/negativos e integración en GitHub Actions.

## 6. Auditoría y handoff

- **A0:** verificar fuentes institucionales, commit padre, scope y diff exactamente dos archivos Markdown, procedencia del CHANGELOG y ausencia de secretos.
- **A1:** inspección documental P01–P04 y N01–N12; no atribuirles resultados runtime.
- **A2:** matriz de permisos, actor/tenant, aislamiento, idempotencia, corrupción/replay y prohibiciones.
- **A3:** contratos M02.01–05 y 13 dependencias causales LISTO/A5, DAG151, PLAN/COLA/BITACORA.
- **A4:** commit fast-forward a `dev` con `expected_sha` y checks obligatorios exact `head_sha` terminados en éxito; después lectura posterior de documentos/estado.
- **A5:** revisión posterior **independiente** por una de las otras cinco Tasks Javier; no deducir aprobación por el cierre A0–A4 de esta autora.

**Estado de salida esperado:** `LISTO_DEV` sólo después de A0–A4 y readback; `A5_PENDIENTE` hasta dictamen ajeno, sin activar nuevas Tasks, eventos ni ambientes.
