# SOLQARYN V5 FINAL — C15-01.M01.01: alcance, exclusiones y no-regresión de auditoría de cobertura

**Fecha:** 2026-11-14. **Autor:** PRIMARY DEV 04 JAVIER (`6aeacb7820ac8191b7d768a6f33dfa09`). **Repositorio/rama:** `solqaryn/Solqaryn@dev`. **Scope Lock:** `STRICT`. **Clase:** preparación documental M01, no auditoría integral ejecutada, ni certificación de cero omisiones.

**Fuente de verdad:** [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN_MAESTRO / COLA / DAG_TECNICO / BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, `docs/AUTOMATION_STATUS.md`, `docs/AUTOMATION_AUTHORITY.md` y Skill canónica. Fila institucional de la microtarea: PLAN 2162; la elegibilidad inicial exige H=PENDIENTE, I vacío, COLA G/O SI, DAG F VALIDADO/G SI. El prefiltro autoriza preparar este contrato, no asumir que se auditaron los 2.340 IDs.

## 1. Propósito, resultado y unidad mínima de análisis

El bloque C15-01 aspira a reconciliar cada requisito de entrada con mejoras, programa, bloque, fase, microfase, Task, artefacto y evidencia para identificar **requisitos huérfanos, duplicados críticos, contradicciones y dependencias falsas**, y someter correcciones a nueva auditoría independiente. El resultado exigido por el plan —cero huérfanos o duplicados críticos— es un **gate futuro** que requerirá cotejo exhaustivo, cobertura calculada y evidencia, y **no** está demostrado por redactar este M01.01. Son fuentes ejecutables únicas el documento V5 FINAL y los IDs vigentes del Sheet; la historia no añade nuevos requisitos, cuentas ni trabajos.

Cada `ID_MICROTAREA` debe representar una unidad del plan con objetivo, resultado, dependencia, estado, responsable, versión, evidencia causal y rollback. Para pruebas de integridad del conjunto se exige correspondencia por ID exacto, no orden de fila, prefijo parcial o heurística de texto. Un cambio concurrente puede alterar fórmulas y HEAD; revalidar el snapshot antes de cada decisión real, sin reclamar dos IDs simultáneamente.

## 2. Alcance y límites de la futura auditoría

| Área | Comparación que deberá realizarse | Resultado de evidencia todavía pendiente |
| --- | --- | --- |
| Requisitos de origen V5 | Cada requisito con capítulo, mejora, ID de microtarea y criterio de aceptación | Matriz bidireccional completa, con conteos y hash del documento |
| PLAN_MAESTRO | Los 2.340 IDs de plan, estado H, responsable I, dependencia G y evidencia L | Censo actualizado, unicidad y justificación de cada excepción |
| COLA | Coincidencia de IDs, estado E, fórmula G y habilitación O | Diferencias agrupadas por causa sin autorizar gates falsos |
| DAG_TECNICO | Referencias A, estado de revisión F y gate G, predecesores y dependencias | Detección de ausencia, autorreferencia, ciclo o gate inconsistente |
| BITACORA A:Z | Eventos nuevos únicos, idempotencia y trazas por Task y SHA | Readback append-only y correlación con transición real |
| GitHub dev | `head_sha`, cambio publicado, diff, pruebas pertinentes y autoría | Evidencia real por microfase, sin tratar ausencia de CI como éxito |
| Seguridad transversal | JWT/RBAC, tenant A/B, PII, secretos, roles, datos y superficie pública | Negativos causales y permisos por entorno, según etapa |
| Automatizaciones | Sólo seis Tasks Javier autorizadas, RRULE/identidad/estado físico verificados | Lectura nativa de proveedor, sin reactivación de terceros |
| Auditoría y corrección | Hallazgo → responsable → corrección → repetición de pruebas | Registro no duplicado y cierre independiente cuando corresponda |

Esta fase M01 no crea scanners, bots, servidores, pipelines nuevos, reservadores, eventos, filas de plan, migraciones, cuentas ni cambios funcionales. No emplea QA/main/PROD ni despliega Hostinger, Cloudflare, R2, DNS, bases de datos reales o servicios de pago. No consulta Tasks fuera de las seis Javier. No se remueve A5_FAIL histórico sin reparación causal y nuevo revisor distinto.

## 3. Dieciséis invariantes de no-regresión (NR01–NR16)

| ID | Invariante verificable en futura auditoría | Contraejemplo que impide certificar |
| --- | --- | --- |
| NR01 | El conjunto de IDs del PLAN corresponde íntegramente al estándar V5 vigente | Se usa backlog archivado o se agrega ID no autorizado |
| NR02 | Los IDs son únicos y las uniones PLAN/COLA/DAG usan clave exacta | Fila huérfana, duplicada o coincidencia aproximada |
| NR03 | Dependencia y gate técnico se validan por fuente y prerrequisito real | COLA O=SI se convierte automáticamente en aprobación DAG |
| NR04 | Una dependencia inexistente, cíclica o NO permanece bloqueante | Se declara SI sin predecesores satisfechos |
| NR05 | PLAN H y responsable I gobiernan el estado de la unidad | Se interpreta BITACORA histórica como nueva autorización |
| NR06 | Un CLAIM usa exclusividad comprobada y CAS con readback | Dos Tasks modifican simultáneamente el mismo ID |
| NR07 | Cada evento BITACORA incorpora 26 campos A:Z y llave idempotente única | Evento repetido o historia sobrescrita |
| NR08 | Evidencia de commit y checks responde al SHA publicado exacto | PASS heredado de otro commit o `skipped` contado PASS |
| NR09 | A0–A4 demuestran lo pertinente al diff real | M03 runtime se cierra con documentación inventada |
| NR10 | A5 exige un TASK_ID distinto del autor, entre seis Javier | Autoaprobación o aprobación de identidad retirada |
| NR11 | A5 DEV no concede acceso o autorización QA/main/PROD | Certificación de publicación por estar LISTO_DEV |
| NR12 | JWT/RBAC y tenant A/B se validan en backend cuando haya código causal | Guard UI o texto suplanta denegación HTTP |
| NR13 | MySQL/EF preserva integridad e historial empresarial en operaciones reales | Migración o restore sin copia/reversión autorizadas |
| NR14 | La matriz de cobertura distingue `NO_VERIFICADO` de `FALLO_VERIFICADO` | Falta de test se inventa como defecto o PASS |
| NR15 | La corrección de un hallazgo requiere reauditar la misma evidencia causal | Se elimina bloqueo por cambio cosmético o conteo agregado |
| NR16 | Cambios concurrentes en dev preservan HEAD y CHANGELOG sin intercalado | Force-push, sección huérfana o atribución cruzada |

## 4. Ocho pares de aceptación futura (P/N) — NO_EJECUTADOS

| Caso | Positivo exigible cuando se autorice la auditoría | Negativo bloqueante |
| --- | --- | --- |
| P01/N01 | Matriz bidireccional V5 ↔ PLAN muestra cada requisito y destino único | Requisito sin mapeo o duplicado crítico se reporta cubierto |
| P02/N02 | Conteo único de IDs y comparación con COLA y DAG reconciliados | ID ausente o duplicado pasa sin explicación |
| P03/N03 | Dependencias no cíclicas, predecesores y A5 causalmente comprobados | DAG SI libera scope con prerrequisito NO |
| P04/N04 | Estado/owner y BITACORA reflejan la misma transición readback | Evento inventado adelanta estado o lease |
| P05/N05 | Commit SHA, diff y checks causales concluidos correctamente | `queued`, `skipped`, `cancelled` o SHA distinto se toma por éxito |
| P06/N06 | Matriz tenant A/B/JWT/RBAC por scope con test negativo causal | Declaración textual reemplaza autorización backend |
| P07/N07 | Auditor independiente revisa corrección sin autoaprobación | Se elimina A5_FAIL por la Task autora |
| P08/N08 | Informe de hallazgos tiene severidad fundada, evidencia y prueba repetida | Se declara cero huérfanos o cero P0/P1 sin inventario completo |

**Estado de P01–P08 y N01–N08: `FUTUROS_NO_EJECUTADOS`.** No se ejecutó un censo exhaustivo de requisitos o de 2.340 IDs, auditoría real de dependencias, SQL, HTTP, Playwright ni seguridad operativa por este documento. El objetivo 'cero huérfanos/duplicados críticos' permanece `NO_CERTIFICADO`. Ni las fórmulas ni una muestra permiten certificar el universo.

## 5. Entregables futuros y reconciliación obligatoria

La auditoría de cobertura real deberá producir: (1) instantánea con revisiones V5, Sheet y HEAD exacto; (2) índice de requisitos con claves estables y hash; (3) unión completa PLAN/COLA/DAG/BITACORA por ID; (4) reporte de huérfanos, colisiones, ciclos, divergencias y elementos fuera del alcance; (5) trazabilidad requisito→implementación→test→evidencia→A5; (6) clasificación P0/P1 justificada, sin convertir incertidumbre en defecto; (7) fichas de corrección, dueño y rollback; (8) nueva ejecución de la prueba afectada y readback final. El reporte debe explicitar filas no procesadas, límites de permisos, CI pendiente y conflictos de concurrencia.

La revisión posterior debe incluir deliberadamente casos negativos sintéticos: duplicado, ID inexistente, dependencia faltante, cambio de SHA, Task ajena, owner concurrente y bitácora duplicada. Ningún resultado se presume PASS hasta observarlo. El motor de pruebas y el orden de ejecución se decidirán por dependencias y recursos reales, sin ampliar el allowlist.

## 6. Impacto técnico, A0–A5 y reversión

**Cambio propuesto:** únicamente este Markdown y sección aditiva íntegra de `CHANGELOG_AI.md`. **Sin cambios** en Angular 22.2.1, .NET 10, API/DTO, Domain/Application/Infrastructure, MySQL/EF, JWT/RBAC, tenant, archivos, Cloudinary, observabilidad, secretos, arquitectura, conectores, Task física, calendario o ambientes. **Consumidores futuros:** C15-01.M01.02–05 y M01.06 solo tras DAG/independencia; otros ciclos de auditoría sólo conforme al plan.

- **A0:** contrato, NR01–16, pares P/N01–08, no-alcance y ausencia de encabezados vacíos.
- **A1:** criterios documentales positivos/negativos falsables, marcados FUTUROS_NO_EJECUTADOS.
- **A2:** preservación de seguridad JWT/RBAC/tenant A/B y confidencialidad; ninguna prueba runtime atribuida.
- **A3:** lectura cruzada de estándar, plan, gobernanza y arquitectura; .NET/Angular/MySQL/Playwright `NO_APLICA` para dos Markdown, no PASS.
- **A4:** commit directo dev fast-forward con CAS sobre HEAD vigente, diff limitado, checks Scope Lock y V5 exact-SHA `completed/success`, readback PLAN/COLA/BITACORA.
- **A5:** revisión independiente pendiente por otra Task Javier; jamás autoaprobación ni permiso QA/main/PROD.

**Rollback:** `git revert` normal del commit documentario después de verificar HEAD y cambios concurrentes; nunca fuerza, borrado/rewrite de BITACORA ni migración.
