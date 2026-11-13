# SOLQARYN V5 FINAL — C00-02.M02.01: entidades, ownership, relaciones e invariantes

**Entrega documental exclusiva:** definir el modelo de entidades y propiedad del trabajo V5 sin modificar bases, servicios, interfaces ni automatizaciones. **Autor:** PRIMARY DEV 03 — JAVIER, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`; `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Fuente contractual:** `STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES` V5 FINAL, Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, Sheet institucional `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU` y repositorio original dev.

**Resultado exigido de la fase M02:** definición de cola, dependencias, autoría, estados y cierre sin duplicados; evidencia mínima de contrato y ejemplo de ciclo. Esta M02.01 fija el **modelo lógico** previo a contratos API/DTO, persistencia y eventos de M02.02–05: no implementa funcionalidad, no confirma integración de MySQL ni libera M03. `C00-02.M01.06` está LISTO, la fila se encontraba PENDIENTE y `COLA!G/O=SI`, sin registro individual en DAG_TECNICO: gate conservador vigente, ningún atajo por objetivo de capacidad.

## 1. Entidades y claves de identidad

| Entidad | Clave canónica y origen | Campos mínimos relevantes | Identidad/ownership |
| --- | --- | --- | --- |
| `Microtarea` | `PLAN_MAESTRO!A` (ID V5 único) | Resultado exigido, dependencias, versión fuente, estado | 2.340 IDs inmutables; nunca inventar un ID 2.341 |
| `EstadoMicrotarea` | `PLAN_MAESTRO!H` | PENDIENTE, EN_PROGRESO, VALIDANDO, LISTO (más excepciones autorizadas) | Fuente editable única; COLA E solo lee por fórmula |
| `ClaimOwner` | `PLAN_MAESTRO!I` + TASK_ID | ejecutor autorizado, hora, fingerprint, historial de autor | propiedad exclusiva por ID hasta cierre o relevo CAS demostrado |
| `GatesCola` | `COLA!G/O` | dependencias OK + desarrollo habilitado | derivadas por fórmula, nunca grant ni asignación de owner |
| `DependenciaTecnica` | `DAG_TECNICO!A:G` | predecesores, tipo, revisión VALIDADO, gate SI/NO | una excepción documental no habilita migraciones/IAM/QA |
| `Artefacto` | ruta Git + blob SHA | código/Markdown, commit parent, diff | autor original conservado; blob no publicado no es SHA dev |
| `PublicacionDev` | SHA commit `dev` | parent HEAD, tree, checks exact-SHA, rollback | fast-forward serial sin PR/branch/force-push |
| `EventoAuditoria` | `BITACORA!A:Z` | EVENT_ID, IDEMPOTENCY_KEY, TASK_ID, RUN_ID opcional, SHA | append único 26 campos, readback sin filas artificiales |
| `RevisionA5` | SHA + reviewer TASK_ID distinto | A5_PENDIENTE/PASS/FAIL, evidencia positiva/negativa | solo otra de las cinco Tasks Javier, sin QA/main/PROD implícitos |
| `EjecucionFisica` | TASK_ID nativo en allowlist vivo | título/horario/cuenta original, estado habilitado | seis Tasks Javier; presencia histórica de otra cuenta NO da permiso |

La relación principal es `Microtarea (1) → (0..1 owner efectivo), (0..N eventos append-only), (0..N artefactos SHA), (0..N dependencias), (0..N revisiones SHA)`. Los eventos preservan cambios históricos, pero solo H/I vivos gobiernan nuevo despacho. Un A5_FAIL bloquea promoción del SHA implicado aunque H permanezca LISTO_DEV.

## 2. Invariantes no negociables

- **I01 — unicidad:** el ID de PLAN aparece una sola vez dentro del catálogo fijo de 2.340; claims, commits y eventos incluyen ese mismo ID, sin filas 2.341.
- **I02 — estado:** solo H de PLAN_MAESTRO es mutable para ESTADO; COLA E debe conservar fórmula, no literal manual heredado.
- **I03 — exclusividad:** antes de claim H=PENDIENTE e I vacío; después H=EN_PROGRESO e I identifica un único TASK_ID real. Dos Tasks no trabajan sobre el mismo ID.
- **I04 — autorización:** el allowlist efectivo es de seis Tasks Javier; títulos/horarios no bastan para inferir acceso ni permiten cuentas externas.
- **I05 — dependencias:** `COLA!G=SI` y `O=SI`, DAG VALIDADO cuando exista, predecesores realmente satisfechos y riesgos evaluados; cuota objetivo no implica permiso de claim.
- **I06 — transición:** `PENDIENTE → EN_PROGRESO → VALIDANDO → LISTO`. VALIDANDO se registra con candidato antes de A0–A3; A4 requiere SHA publicado exacto.
- **I07 — calidad:** A0–A4 PASS real y A5_PENDIENTE antes de LISTO_DEV; A5 independiente distinto del autor antes de QA/main/PROD.
- **I08 — inmutabilidad histórica:** BITACORA A:Z append-only con 26 celdas y claves EVENT_ID/IDEMPOTENCY_KEY únicas; RUN_ID vacío si no hay prueba nativa.
- **I09 — Git concurrente:** no reset, force-push, PR ni nuevas ramas; revalidar HEAD, parent único, fast-forward y diff causal. Cambios de otros Scopes se preservan.
- **I10 — tenancy/seguridad:** claims, flags UI o valor de empresa no pueden conferir acceso al tenant B. JWT, BCrypt, RBAC, secretos y PII siguen sujetos a controles backend.
- **I11 — separación de autoridad:** un owner histórico de otra cuenta no es ejecutor autorizado actual; no reasignar sin prueba verificable de inactividad, sin escritor concurrente y CAS seguro.
- **I12 — último ID:** C15-05.M06.06 solo podrá cerrarse con las 2.339 microtareas anteriores certificadas y gates; no inventar otra.

## 3. Transiciones y relaciones causales

| Paso | Estado/owner de entrada | Evidencia y efecto que se deben constatar |
| --- | --- | --- |
| S0 ELEGIR | PLAN H=PENDIENTE, I vacío | COLA G/O SI, DAG, alcance, herramientas, owner, permisos y SHA actual |
| S1 CLAIM | H=PENDIENTE → EN_PROGRESO | Owner real en I, evento idempotente 26 celdas y readback PLAN/COLA |
| S2 CANDIDATO | H=EN_PROGRESO → VALIDANDO | Artefacto completo con fingerprint y mismo owner, **antes** de auditar A0–A3 |
| S3 AUDITAR | H=VALIDANDO | A0 estática, A1 positivos/negativos, A2 seguridad, A3 integración pertinentes, sin pruebas sintéticas |
| S4 PUBLICAR | A0–A3 exigibles PASS | commit directo FF dev, padre/head y SHA; si HEAD cambió, reconciliar aditivamente |
| S5 CERTIFICAR A4 | H=VALIDANDO y SHA publicado | check-runs exact SHA `completed/success` de gates requeridos, diff y readback; no contar SKIPPED como PASS |
| S6 CERRAR | A0–A4 PASS + A5_PENDIENTE | PLAN H=LISTO, I preservado; COLA E refleja; BITACORA eventos completos |
| S7 A5 OTRO | H=LISTO, reviewer distinto al autor | A5_PASS o FAIL por SHA; revisión independiente no permite despliegue por sí misma |

**Relevo de un owner retirado:** no es un nuevo claim libre. Verificar identidad y abandono objetivo, head y CI, ausencia de trabajo simultáneo y releer H/I. Solo si un mecanismo de compare-and-swap efectivamente comprobable ofrece un ganador, transferir y registrar antiguo/nuevo owner; si el conector no ofrece tal CAS, **no sobrescribir al propietario**. La lectura previa no elimina riesgo de carrera.

## 4. Caso positivo ilustrativo de extremo a extremo (NO evento real)

Para `C00-02.M02.01` como **ID existente ilustrativo**, las condiciones formales son H=PENDIENTE/I vacío/G=SI/O=SI y M01.06 LISTO. Un claim real genera EN_PROGRESO e I=TASK_ID, luego candidato VALIDANDO; A0–A3 proporcionales; commit Git FF con Scope Lock exacto; A4/readback; A5_PENDIENTE y LISTO. Otro TASK_ID Javier distinto realiza A5. **Esta descripción no se presenta como ejecución de las transiciones ni como evidencia de A5_PASS**; los eventos de la corrida y sus horas reales pertenecen solo a BITACORA.

## 5. Casos de aceptación documental positivos y negativos

| Caso | Signo | Estímulo y comportamiento esperado |
| --- | --- | --- |
| P01 | + | H=PENDIENTE/I vacío, COLA G/O SI, dependencias y autorización OK → claim único con readback del owner |
| P02 | + | Artefacto revisable y A0–A3 documentales PASS, commit FF + Scope Lock exact-SHA → A4 y A5_PENDIENTE habilitan LISTO_DEV |
| P03 | + | Revisor distinto entre las seis Javier encuentra caso positivo/negativo y evidencia causal → A5_PASS/FAIL con evento, sin promoción automática |
| N01 | - | COLA G=SI/O=NO o DAG en NO → no claim aunque meta de reserva exija más elegibles |
| N02 | - | H=VALIDANDO con owner ajeno y sin CAS comprobado → no reasignación, doble escritura ni borrado de I |
| N03 | - | Una Task intenta marcar LISTO con test requerido skipped/in_progress/cancelled → bloqueo de ese ID |
| N04 | - | Reviewer=AUTHOR_TASK_ID o actor externo al allowlist → rechazo de A5 |
| N05 | - | BITACORA tiene evento con solo 21 campos, clave duplicada o RUN_ID inventado → rechazo de append |
| N06 | - | Claim usa ID fuera de 2.340 o intenta crear C15-05.M06.07 → rechazo por catálogo fijo |
| N07 | - | Candidato blob sin SHA de commit publicado se anuncia como resultado GitHub verde → no A4_PASS |
| N08 | - | Cambio de un Scope modifica QA/main/PROD, secreto o datos productivos → NO_GO sin autorización separada |
| N09 | - | JWT/rol frontend intenta acceder a tenant B sin membresía backend → denegación, no concesión |

Estos P/N son **oráculos documentales**, no HTTP/MySQL tests ejecutados, ni confirman que el conector de Sheets ofrezca CAS real.

## 6. Impacto, verificación proporcional y rollback

**Archivos afectados:** solo este Markdown y una entrada aditiva en `CHANGELOG_AI.md`. No modifica Angular, .NET, Node, MySQL, rutas, DTO, auth, config, infraestructura, desplegables ni datos reales. Seguridad relevante: identidad de Task, autoría, separación tenant A/B, PII y secretos en bitácora.

**A0:** revisar estructura, inventario de 10 entidades, I01–I12, hash del diff, secretos y links; **A1:** P01–P03 y N01–N09 con oráculos falseables; **A2:** privilegios/tenancy/JWT/RBAC/fail-closed; **A3:** cotejo con encabezado BITACORA A:Z, PLAN H/I, COLA E/G/O y M01.06 LISTO, integraciones reales NO_APLICA por documentación; **A4:** publicación FF SHA exacto + Scope Lock SUCCESS, diff y readback. `requiere_dotnet=NO`, `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO`; `requiere_security=SI_DOCUMENTAL`; `requiere_scope_lock=SI`. `A5_PENDIENTE` de revisor independiente, sin QA/main/PROD.

**Rollback:** `git revert` aditivo y readback posterior sin reescribir historial, nunca force-push, reset ni borrar BITACORA. La documentación no autoriza activar infraestructura, servicios, proveedores, ejecutores nuevos ni funcionalidad todavía inexistente.
