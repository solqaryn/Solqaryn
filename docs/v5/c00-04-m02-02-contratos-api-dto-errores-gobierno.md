# SOLQARYN V5 FINAL — C00-04.M02.02: contrato de API, DTO, errores y compatibilidad del gobierno de cambios

**Naturaleza:** especificación documental, no implementación. **Autor:** PRIMARY DEV 05 — JAVIER, `TASK_ID=6adc2e55f47c81919f6374e561687076`. **Repositorio:** `solqaryn/Solqaryn@dev`. **Proyecto:** SOLQARYN; `PROJECT_SCOPE_LOCK=STRICT`.

**Resultado exigido:** definir los contratos de entrada, salida, validaciones, errores y compatibilidad que una eventual implementación de gobierno de cambios debería cumplir. Este documento no añade endpoints, controladores, DTO en código, servicios, rutas de aprobación, permisos, migraciones, jobs, eventos, webhooks, nuevas Tasks ni capacidad de ejecutar decisiones. Toda operación futura necesita su microtarea propia, autorización y pruebas A0–A5. La autoridad del backlog es el documento V5 FINAL de Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA` y el Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`.

## 1. Preflight y procedencia

El alcance del bloque `C00-04` es distinguir cambios autorizables, reversibles y prohibidos de ejecutar automáticamente; no construir un motor de decisiones. Los siete predecesores exactos son `C00-04.M01.01–06` y `C00-04.M02.01`, todos `LISTO` en `PLAN_MAESTRO!H110:H116`. Las cinco M01.01–05 tienen A5 correctiva independiente; M01.06 tiene A5 independiente de Javier :40; M02.01 tiene A5 de Javier :00 sobre SHA `1c47218bc3dbce4e7734a7258ffa2ae25145a10f`. El contrato M02.01 reside en `docs/v5/c00-04-m02-01-entidades-ownership-invariantes.md`, blob Git `aaf0e385f2d59440b66e923d355f77721f2917f9`.

`DAG_TECNICO!A147:J147` se verificó individualmente, `F147=VALIDADO` y `G147=SI` calculado desde siete cierres y A5, sin utilizar fallback por fase. Al reclamar, `PLAN_MAESTRO!H117=PENDIENTE`, `I117` vacío, `COLA!G117/O117=SI`; la Task actual reclamó el ID con readback. Ninguno de esos hechos libera por sí solo otro ID, ni reemplaza el control de autoría A5 independiente.

## 2. Contratos propuestos de datos — NO EXISTEN como DTO de runtime

| Contrato lógico propuesto | Campos mínimos, derivados exclusivamente de autoridad server-side | Rechazo requerido |
| --- | --- | --- |
| `CambioConsulta` | `idMicrotarea`, versión de contrato, estado `PLAN_MAESTRO!H`, propietario autorizado `I`, resumen de evidencia J:L, fecha M y dependencia DAG | No aceptar un estado o owner enviados por el navegador como autoridad; no incluir secretos ni PII |
| `DecisionPropuesta` | `idMicrotarea`, `idDecision`, alcance, motivo, criticidad, reversibilidad, rol aprobador futuro y evidencia citada | Ausencia de autorización vigente = `PENDIENTE_DECISION`, no `APROBADA`; id de decisión no es permiso |
| `CondicionEjecucion` | scope, SHA causal, controles A0–A4, estado de A5, dependencias, clasificación riesgo y autorización explícita si se requiere | Un `LISTO_DEV` o `A5_PASS` no se transforma en permiso para QA/main/PROD, DB, DNS o pagos |
| `RegistroEvidencia` | `EVENT_ID`, `IDEMPOTENCY_KEY`, timestamp, Task autora, Task revisora (si existe), SHA y resultado | Rechazar EVENT_ID/clave duplicados, eventos truncados o revisor idéntico al autor |
| `CompatibilidadContrato` | versión de esquema, versión cliente compatible, campos obligatorios/opcionales, plan de reversión por cambio | Un consumidor viejo no puede aceptar silenciosamente un nuevo grant o un error desconocido |

Todos los campos son **propuestas de diseño para M02**. Ninguno se materializa en una ruta HTTP ni valida un token real por el hecho de figurar aquí. Los datos institucionales autoritativos siguen en Sheets y la evidencia de código sigue en GitHub; no duplicar un segundo almacén de estados.

## 3. Semántica eventual de operaciones, validaciones y respuestas

No se reserva ningún path REST concreto ni se declara una API existente de gobierno de cambios. Una futura tarea de implementación podrá elegir rutas y DTO sólo después de inventario de API, control de tenancy y una decisión arquitectónica propia. Este contrato fija comportamientos exigibles, no una interfaz ya publicada:

| Operación lógica futura | Resultado positivo especificado | Negativo que debe denegar |
| --- | --- | --- |
| Consultar una microtarea del plan | Solo estado/evidencia de alcance que la identidad autenticada puede consultar | ID inexistente, cuenta fuera del allowlist, tenant ajeno, recurso restringido o falta de JWT → no revelar datos |
| Proponer una decisión | Registro tipado de propuesta con motivo, riesgo, rol aprobador y condición `PENDIENTE` | Solicitud client-side con `aprobado=true`, rol legacy, `EmpresaId` ajeno o código/SQL/HTML arbitrario → rechazo |
| Solicitar evaluación de un cambio | Evaluación sin efectos, contra HEAD, DAG, PLAN y permisos actuales | CI obligatoria pendiente/failed/skipped, scope inexistente, doble owner, A5 del autor o dependencia sin cumplir → `NO_GO` |
| Registrar evidencia | Evento append-only de 26 columnas A:Z, identidad y clave únicas | EVENT_ID/IDEMPOTENCY_KEY duplicados, RUN_ID inventado, edición de BITACORA histórica o evento incompleto → rechazo |
| Consultar compatibilidad | Esquema versionado que no amplía permisos ni convierte datos legacy en autoridad | Campos desconocidos sensibles, discrepancia de versión, error no reconocido o cliente sin autorización → fail-closed |

**Validaciones contractuales:** ID del catálogo fijo de 2.340, tipo y longitud limitados, versión soportada, fuente server-side verificable, ownership vigente, DAG exacto `VALIDADO/SI`, event key única, fecha UTC explícita, restricción por tenant y prohibición de acceso a secretos. La elegibilidad `COLA!G/O` es filtro calculado, no un endpoint, permiso ni mecanismo de scheduling. Una fuente indisponible devuelve resultado de denegación estable y registrable, no aprobación por defecto.

**Tabla de errores para una implementación FUTURA (no comportamiento probado):**

| Clase | Código HTTP orientativo si existiera API | Contenido permitido |
| --- | --- | --- |
| Solicitud inválida o versión no soportada | 400 / 422 | Código genérico, correlación, campos públicos y documentación de versión |
| Ausencia de autenticación válida | 401 | Mensaje genérico, sin datos de usuario o tenant |
| No autorizado / pertenencia ajena | 403 (o 404 uniforme de privacidad) | No revelar si existe tenant, decisión o recurso B |
| ID realmente inexistente | 404 | Mensaje no enumerable; no detalles de otro tenant |
| Conflicto de owner, versión o idempotencia | 409 | Referencia opaca de conflicto, nunca contenido de otra petición |
| Demasiadas solicitudes | 429 | Reintento acotado cuando la política real así lo permita |
| Dependencias incompletas o controles requeridos no aprobados | 409 / 423 según contrato futuro | `NO_GO` verificable, sin falsificar checks |
| Error de persistencia/servicio | 503 / 500 | CorrelationId, mensaje seguro sin JWT, SQL, rutas locales ni PII |

La tabla es orientativa y requiere reconciliación con `ExceptionHandlingMiddleware` y contratos HTTP existentes en una microtarea de implementación. No se atribuyen estas respuestas al runtime actual.

## 4. Compatibilidad, seguridad y límites irreversibles

El contrato tiene versión explícita; cambios aditivos opcionales son preferibles a cambios de significado. Clientes anteriores ignoran campos no críticos pero no pueden interpretar campos ausentes como aprobación, rol efectivo, tenant verificado o A5_PASS. Cualquier cambio incompatible requiere prueba positiva/negativa de DTO y rollback por consumidor; el contrato documental no cambia clientes Angular ni servidores ASP.NET Core.

JWT identifica al actor; el backend resuelve membresía y permisos RBAC/acción/recurso en su estado actual. `EmpresaId` de URL, header o formulario es selector, nunca autoridad; los entitlements o flags no reemplazan autorización. Registros de auditoría deben excluir secretos, tokens, PasswordHash y PII innecesaria. Un revisor A5 diferente del autor entre las otras cinco Tasks Javier no equivale a un aprobador comercial o de producción. Ningún paso de este documento autoriza manipular Aiven, MySQL, DNS, Cloudflare, Hostinger, Render, Vercel, dominios, pagos o `qa/main/PROD`.

El componente propuesto afectaría en el futuro a consumidores de gobierno institucional, controles de autorización, observabilidad y UX, pero no introduce cambios hoy en Domain/Application/Infrastructure/API/Angular/DB. No implementar dispatcher, webhook ni llamadas Task→Task continuas.

## 5. Oráculos de aceptación documental positivos y negativos

- **P01:** siete predecesores `LISTO`, A5 de otra Javier para sus contratos y DAG147 `VALIDADO/SI` permiten evaluar exclusivamente C00-04.M02.02, sin delegar autoridad a fases.
- **P02:** `CambioConsulta`, `DecisionPropuesta`, `CondicionEjecucion`, `RegistroEvidencia` y `CompatibilidadContrato` tienen entradas y denegaciones explícitas.
- **P03:** una consulta autorizada solo mostraría sus datos permitidos; una propuesta no activa aprobación ni worker.
- **P04:** documento más CHANGELOG autocontenido publicados en `dev` mediante fast-forward, checks Scope Lock y V5 de SHA exacto aprobados y readback institucional permiten `LISTO_DEV` exclusivamente documental.
- **N01:** `COLA G/O=SI` sin DAG individual, predecesor no LISTO o doble owner → no claim.
- **N02:** cliente afirma `aprobado=true`, permiso por header, rol legacy o contexto de tenant B → no autorización.
- **N03:** A5 por autor, cuenta excluida, evento con menos de 26 campos o clave duplicada → no acreditación.
- **N04:** check necesario `SKIPPED/CANCELLED/IN_PROGRESS/FAIL` o resultado de SHA diferente → no PASS.
- **N05:** error que expone token, SQL, dato de B, correo o PasswordHash → no aceptación.
- **N06:** intento de API real, alteración JWT/RBAC/tenant, migración, webhook, séptima Task, QA/main/PROD o pago desde esta microtarea → NO_GO.
- **N07:** RUN_ID no nativo, aprobación implícita, fase liberada sin DAG o changelog con encabezado vacío/intercalado → no cierre.

Los casos P/N son pruebas de **coherencia del diseño por inspección documental**; no se ha ejecutado una solicitud HTTP, .NET, MySQL, Angular, Docker o Playwright para demostrar esos comportamientos.

## 6. Auditorías aplicables, evidencia y rollback

- **A0 (estática):** correspondencia con M02.01 y M01.06, integridad Markdown/tablas, 7 dependencias, SHA de blob de M02.01, ausencia de secretos y sección CHANGELOG con viñetas inmediatamente bajo su encabezado.
- **A1 (funcional documental):** P01–P04 y N01–N07 comprobables contra el texto; no se presenta especificación como código.
- **A2 (seguridad):** tenant A/B, JWT, RBAC, roles y privilegios, PII, denegación y segregación autor/revisor; alcance exclusivamente documental.
- **A3 (integración documental):** contrato conceptual compatible con fuentes institucionales PLAN/COLA/DAG/BITACORA, 7 predecesores y consumidores futuros; `.NET`, Angular, MySQL, Docker y E2E `NO_APLICA` por diff sin runtime.
- **A4 (publicación):** SHA publicado en `dev`, padre fast-forward, diff dos Markdown, hashes, checks `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` sobre SHA exacto, readback `PLAN_MAESTRO/COLA/BITACORA`; si obligatorio queda en progreso, conservar `VALIDANDO`.
- **A5 (independencia):** `A5_PENDIENTE` después de LISTO_DEV; revisión de otra Task Javier con ID distinto, sin autopromoción.

**Rollback:** `git revert` normal del commit documental tras verificar HEAD y consumidores, registro compensatorio append-only si corresponde, nunca force-push, reset, edición de eventos históricos ni cambio de datos productivos. M02.03–06, M03 y otros bloques requieren sus propios DAG y pruebas antes de reclamarse.
