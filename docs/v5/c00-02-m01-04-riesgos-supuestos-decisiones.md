# C00-02.M01.04 — Riesgos, supuestos y decisiones pendientes

**Origen:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (`1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), hoja operativa `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`.
**Proyecto:** `solqaryn/Solqaryn`, rama `dev`. **Fase:** C00-02.M01, modelo de trabajo para automatizaciones. La unidad M01.04 registra riesgos y decisiones; no cambia runtime, estados de otros IDs, seguridades, Tasks o topología.

## Riesgos verificables

| Clave | Riesgo o condición observable | Impacto | Detección causal y tratamiento | Responsable real |
| --- | --- | --- | --- | --- |
| R01 | Dos ejecutores reclaman la misma PENDIENTE | Doble trabajo, pérdida de autoría | Antes de escribir releer `PLAN_MAESTRO!H/I` y `COLA!G/O`. Si custodio activo, `NO_GO`; no suplantar ni editar scope ajeno | Task que intenta claim |
| R02 | Fórmula `COLA!E/G/O` perdida o reemplazada por texto | Estado o dependencia falsa | Inspeccionar `userEnteredValue.formulaValue` antes y después; reparar sólo desde homóloga verificada con permiso, nunca inventar valor calculado | Administrador institucional autorizado |
| R03 | `BITACORA` contiene EVENT_ID repetido o evento incompleto | Auditoría irrecuperable | Verificar unicidad, 26 campos (`BITACORA!A:Z`), atribución real y readback; no reinsertar eventos existentes ni alterar historia | Actor del evento / administrador autorizado |
| R04 | SHA de `dev` cambia durante preparación | Sobrescritura de cambios concurrentes | Comparar HEAD antes de publicación, construir commit hijo del vivo y avanzar sólo fast-forward; fallos de ref no autorizan force-push | Task desarrolladora |
| R05 | Skill instalada difiere de Skill canónica | Ejecución con gobierno desactualizado | Comparar contenido exacto y arquitectura en HEAD, confirmar Scope Lock; `NO_GO` para escrituras si difiere | Task ejecutora / propietario de la Skill |
| R06 | Job CI obligatorio ausente, `SKIPPED`, `FAILURE` o de SHA anterior | LISTO injustificado | Clasificar runtime y checks nativos realmente aplicables; inspeccionar Check Runs y logs; dejar `VALIDANDO` con bloqueo concreto hasta éxito | Task ejecutora / dueño legítimo del cambio |
| R07 | Separación de tenant, JWT/RBAC o secretos comprometida | Fuga multiempresa y PII | Revisar fronteras backend, denegaciones y tests A/B proporcionales; no compensar falla de seguridad con éxito de build | Autor del código y revisor independiente |
| R08 | Se promociona una unidad DEV sin A5 o sin autorización separada | Alteración no aprobada de QA/main/PROD | LISTO_DEV sólo acredita A0–A4; A5 exige otro TASK_ID, y ni A5 concede acceso al entorno por sí mismo | Titular con autorización explícita |
| R09 | Una Task pausada aparenta estar operativa | Pérdida de continuidad o ejecución simulada | Verificar `is_enabled` nativo, título/ID/horario/owner; una pausa no equivale a run cancelado. Recuperar sólo otro ID permitido de la misma cuenta y releer | Propietario de las Tasks |
| R10 | Suites de frontend/.NET/MySQL ejecutadas sin impacto o se omiten cuando sí hay runtime | Uso erróneo del tiempo o regresión | Clasificar componente y `RESULTADO_EXIGIDO`; para documentación exigir auditoría directa y Scope Lock nativo pertinente; para runtime prueba causal pertinente exact-SHA | Task desarrolladora |

## Supuestos contrastables

- **S1:** La única cola de implementación proviene de V5 FINAL, 2.340 IDs, no de planes archivados. *Verificación:* documento y `PLAN_MAESTRO!A` coinciden; IDs no se inventan.
- **S2:** `PLAN_MAESTRO!H` es el estado editable y `COLA!E` lo refleja por fórmula, sin necesidad de actualizaciones manuales sobre COLA. *Verificación:* readback de `effectiveValue` y `formulaValue`.
- **S3:** La Task sólo puede trabajar en un scope sin dueño o en uno previamente reclamado por su TASK_ID. *Verificación:* `PLAN_MAESTRO!I`, BITACORA y GitHub antes del claim/commit.
- **S4:** La CI del SHA exacto está accesible cuando la prueba depende del proveedor; la falta de evidencia no se sustituye con inferencias. *Verificación:* Check Runs y logs.
- **S5:** Cualquier aprobación o revisión A5 es verdaderamente ajena a la autoría de la unidad. *Verificación:* TASK_ID original, historia de contribuciones y evidencia del SHA revisado.
- **S6:** Las credenciales, datos productivos y ambientes QA/main/PROD no entran en esta fase documental. *Verificación:* diff limitado a `docs/v5/` y `CHANGELOG_AI.md`.

Si un supuesto no puede comprobarse, dejar constancia `NO_GO` para la operación que depende de él; no inferir autorización.

## Decisiones de diseño y puntos no resueltos

| Decisión | Estado | Razón y evidencia |
| --- | --- | --- |
| Único backlog V5 + seguimiento PLAN/COLA/BITACORA | Vigente | Doc y Sheet oficiales; no crear cola paralela |
| Publicación en `dev` sin PR ni rama, sólo fast-forward | Vigente | Contrato V13 Direct DEV; leer HEAD y preservar concurrentes |
| A0–A4 antes de LISTO_DEV; A5 ajeno antes de promoción | Vigente | `docs/AUTOMATION_V13.md`; no tomar un PASS de CI ajena |
| CI condicional por componente sin degradar seguridad | Vigente | Documentos sin runtime no requieren E2E/.NET/MySQL; seguridad/Scope Lock nativos siguen |
| Coedición ajena sin traspaso confirmado | Prohibida | La lectura o revisión no concede custodia |
| Crear nuevos permisos/Tasks o promover a QA/main/PROD | No autorizado por esta unidad | Requiere orden explícita separada del titular |
| Fecha/volumen máximo de LISTO por ejecución | Sin cuota rígida | Continuidad sujeta a elegibilidad, auditoría real y recursos del proveedor |
| Resolver fallos CI de terceros mediante otra identidad | Prohibido | Diagnóstico causal y regreso al autor autorizado, no bypass |

## Criterios de aceptación de M01.04

**Positivo:** un operador puede explicar qué comprobación bloquea cada riesgo, qué actor puede remediarlo y qué evidencia se requiere para retomar sin romper historia.

**Negativo 1:** si otro TASK_ID es custodio, un agente no escribe `PLAN_MAESTRO` ni BITACORA para ese ID y no crea ramas auxiliares.

**Negativo 2:** un Check Run requerido `SKIPPED` o de SHA antiguo no pasa A3/A4; permanecer `VALIDANDO`.

**Negativo 3:** no se puede considerar una aprobación del propio autor como A5 ni inferir derechos de QA/PROD.

La entrega es un registro de riesgo documental. A0 = coherencia estructural, diff, referencias y ausencia de secretos; A1 = escenarios positivos/negativos y responsables; A2 = restricciones de tenancy/identidad/PII; A3 = consistencia de fuentes V5/V13, sin runtime (Node/Angular/.NET/MySQL/Playwright = NO_APLICA). A4 = verificación del commit `dev`, Scope Lock exact-SHA y readback institucional. Rollback: `git revert` normal del commit de documentación, sin forzar refs ni borrar eventos.

> **Corrección R03 por A5_FAIL (2026-11-13):** el encabezado institucional `BITACORA!A1:Z1` contiene 26 campos (`EVENT_ID` a `TIMESTAMP_UTC_ORIGINAL`). No reescribir, completar ficticiamente ni eliminar eventos previos de 21 columnas; para nuevos registros usar el esquema vivo, comprobar validaciones, unicidad, atribución y readback. Requiere A5 independiente sobre el nuevo SHA para levantar el fallo documental de revisión; sin autorización a QA/main/PROD.
