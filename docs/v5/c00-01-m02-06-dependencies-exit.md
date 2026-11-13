# C00-01.M02.06 - Dependencias tecnicas y contrato de salida del lote M02

**Fuente:** V5 FINAL, microtarea `C00-01.M02.06`, documento institucional `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; seguimiento `PLAN_MAESTRO!H`, `COLA!E/G/O`, `BITACORA!A:U`. **Repositorio:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Actor:** `PRIMARY DEV 01 - JAVIER`, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Revision de origen:** `f898f130ef133ca9a42969f6bbfccaaa3031e811`.

## Alcance estricto

Esta unidad **define** dependencias tecnicas y criterios verificables de salida de M02. No ejecuta ni declara finalizado M02, M01, una migracion, un endpoint o una revision A5. No modifica entidades, repositorios, configuracion de produccion, DNS, secretos, JWT, RBAC, EmpresaId, tablas MySQL ni permisos. Las microtareas se despachan solo desde V5 FINAL y la hoja viva, no desde filas historicas V3/V13. Los controles A0-A5 de `docs/AUTOMATION_V13.md` siguen vigentes.

## Mapa del lote y evidencias que cada productor debe aportar

| ID | Contrato tecnico | Evidencia de salida exigible | Riesgo si falta |
| --- | --- | --- | --- |
| `C00-01.M02.01` | Entidades, ownership y relaciones; `docs/v5/c00-01-m02-01-domain-ownership.md` | Trazabilidad entre Domain, membresia de empresa e invariantes; diff y pruebas documentales | Autoridad tenant inferida desde datos opcionales |
| `C00-01.M02.02` | API, DTO, validadores y errores; `docs/v5/c00-01-m02-02-api-contracts.md` | Mapa API/Application, codigos de error y compatibilidad verificables | DTO usado como autorizacion o error que filtra PII |
| `C00-01.M02.03` | Persistencia, indices, migraciones y datos de prueba | Matriz EF/MySQL, reversibilidad y separacion A/B comprobables segun alcance | Perdida de datos, migracion no reversible o indice tenant incorrecto |
| `C00-01.M02.04` | Configuracion tipada y feature flags | Origen y validacion de opciones, defaults seguros y limites de secretos | Flag que evita permisos o secreto en codigo |
| `C00-01.M02.05` | Eventos, idempotencia, correlacion y limites | IDs reproducibles, rechazo de duplicados y fronteras de reintento | Doble efecto transaccional y perdida de auditoria |
| `C00-01.M02.06` | Dependencias tecnicas y esta salida de lote | Este documento, prueba Node 22 dirigida, diff, commit FF, readback y A5_PENDIENTE | Avance prematuro de lote sin evidencia completa |

La dependencia declarada de M02 es `LOTE ANTERIOR: C00-01.M01; FASE: Ninguna; BLOQUE: Ninguna`. La seleccion individual usa `PLAN_MAESTRO!H=PENDIENTE`, `COLA!G=SI`, `COLA!O=SI` y responsable libre; `COLA!E` es formula que refleja H. La seleccion no equivale a certificacion. Las columnas `COLA!P:Q` son un tablero calculado de fases y **no** sustituyen la puerta G/O del ID ni autorizan una escritura en PLAN.

## Contrato verificable de salida M02

1. Consultar **en vivo** `PLAN_MAESTRO!A:H` y `COLA!A:Q` por ID, no por numero de fila historico. El lote M02 se anuncia completo solo cuando sus seis IDs `C00-01.M02.01` a `C00-01.M02.06` tienen estado `LISTO` o estado final expresamente permitido por el plan, con evidencia de A0-A4 y SHA dev. Una tarea en `VALIDANDO` NO certifica el cierre del lote. No asumir que un `COLA!G=SI` implica cierre colectivo.
2. Confirmar que el lote anterior M01 y sus dependencias expresas no tengan faltantes para cualquier puerta de salida que los requiera; si hay una tarea M01 en `VALIDANDO` o `EN_PROGRESO`, registrar `DEPENDENCY_NOT_FINAL` para esa salida, sin adulterar formulas ni trabajo concurrente. El dispatch de otro ID independiente sigue sus puertas propias G/O.
3. Para cada ID M02 conservar dueño/TASK_ID, evidencia de requisito, diferencia de codigo o contrato, caso positivo y negativo relevante, A0 estatica, A1 funcional, A2 seguridad, A3 integracion, A4 posdev sobre SHA y `A5_PENDIENTE` autentico para un Scope ajeno. `A5_PASS` es indispensable antes de QA/main/PROD; LISTO_DEV no autoriza promocion.
4. Reconstruir mapa de dependencias por ID exacto y verificar que no hay dos claims del mismo ID. El calculo de fase no es autorizacion para ignorar RBAC ni tenant. Los datos `EmpresaId` de navegador, header, slug o JWT no validado NO otorgan acceso; se requiere membresia backend verificada. Ninguna imagen historica inexistente justifica migracion: `NO_APLICA`.
5. Un CI `PENDING`, `FAILURE`, `CANCELLED` o `SKIPPED` no es PASS. Verificar el SHA publicado y checks aplicables a ese SHA; distinguir retry auxiliar SKIPPED. No inventar .NET/MySQL/HTTP/browser live cuando solo existe prueba Node de documento.
6. Documentar rollback no destructivo mediante `git revert` del commit de esta unidad tras comparar HEAD; nunca force-push, reset ni borrar BITACORA. `main`, `qa`, `PROD`, Hostinger, Cloudflare y bases reales quedan fuera del alcance actual.

## Aceptacion de esta microtarea

**Positivo documental:** las seis unidades aparecen en la matriz, el algoritmo de salida exige seis estados finales con evidencia y el mapping a las fuentes V5 es inequivo. **Negativos:** no liberar lote con un M02 VALIDANDO; no promover sin A5_PASS; no inferir privilegios tenant desde EmpresaId proporcionado por el cliente; no considerar un check SKIPPED aprobado; no certificar MySQL/HTTP sin prueba real; no publicar si HEAD cambia sin conciliacion. Estas aserciones se prueban de forma dirigida con `node --check` y `node --test` sobre el documento. Son pruebas documentales, NO certificacion runtime.

La prueba y este contrato se entregan como una unidad aditiva con `CHANGELOG_AI.md`. Para LISTO de **esta** microtarea se necesita `VALIDANDO` previo a A0-A3, commit directo fast-forward `dev`, A4 sobre SHA real, evento idempotente BITACORA y readback PLAN/COLA; registrar `A5_PENDIENTE` para otro TASK_ID. `git revert` posterior protege el rollback. El contrato de salida M02 permanece separado de la certificacion individual de M02.06.
