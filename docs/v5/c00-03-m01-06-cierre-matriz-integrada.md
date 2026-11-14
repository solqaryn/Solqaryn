# SOLQARYN V5 FINAL — C00-03.M01.06: cierre M01 de matriz integrada de alcance

**Autor:** PRIMARY DEV 01 — JAVIER_CHATGPT (`6adbb9fff1548191a1bd5b30d9e9984a`). **Repo:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT. **Alcance:** documento M01.06; no runtime, migraciones, cambios de cuentas, QA/main/PROD ni despachos automáticos. Fecha: 2026-11-13.

**Fuentes únicas:** [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). El bloque C00-03 exige convertir cada requisito previo en mejora, contrato, prueba y criterio de cierre dentro del V5 FINAL: **sin cola histórica separada**. Esta entrega cierra exclusivamente el diseño M01, no ejecuta esas mejoras.

## Evidencia previa y frontera del gate

En el preflight, C00-03.M01.01–.05 estaban **5/5 LISTO** en PLAN_MAESTRO!H, con A5_PASS independiente de las Tasks autorizadas de Javier, sin borrar A5_FAIL de sus SHA antiguos; C00-01.M06.06 también estaba LISTO y con A5_PASS independiente. DAG_TECNICO F=VALIDADO/G=SI exigía las seis predecesoras; COLA!G79/O79=SI, PLAN!H79=PENDIENTE/I79 vacío antes del claim. Ninguna revisión histórica acredita permisos de otro agente ni libera scopes no verificados.

| Unidad y archivo de fuente Git en dev | Blob SHA comprobable | Aporte verificable al cierre M01 |
| --- | --- | --- |
| [M01.01 alcance](c00-03-m01-01-scope.md) | 32f21417fe9ea6d06a190b513d1a203283313e29 | Objetivo, exclusiones y no-regresión, solo V5 |
| [M01.02 superficies](c00-03-m01-02-surfaces.md) | be8f4b64aab636db428c6b6c112b4a84f921f2a4 | Actores, entradas/salidas y superficies |
| [M01.03 readback](c00-03-m01-03-source-readback.md) | cce7b78b8852c74b2a9e8f71ce972670100264fe | Fuentes verificables y límites de certificación |
| [M01.04 riesgos](c00-03-m01-04-riesgos-supuestos-decisiones.md) | 850e7e7def06f1c036bbd59de1711adff524501b | Supuestos, riesgos, mitigaciones y decisiones |
| [M01.05 aceptación](c00-03-m01-05-acceptance.md) | 40f78cf1b69563f21a3fb8210de3001bac3afbec | Criterios positivos/negativos futuros y cierre documental |

Estos son SHA **de contenido**, no registros de pruebas runtime. A5_PASS corresponde al alcance de cada documento corregido, no a la certificación universal de código. Los riesgos no resueltos permanecen abiertos a futuras M02–M06.

## Matriz integrada: requisito previo → mejora → contrato → prueba → cierre

| Ref documental | Mejora prevista dentro de V5 FINAL | Contrato aceptable | Evidencia requerida en la microtarea ejecutora |
| --- | --- | --- | --- |
| R01 fuente única | Rechazar planes/colas históricos y duplicados | Solo 2.340 IDs, PLAN H autoridad y COLA por fórmula | Validación cardinalidad, claves únicas y lectura previa/posterior |
| R02 identidad | Restringir autoría a Tasks Javier activas y allowlist exacta | TASK_ID/owner/actor autenticado coinciden | Readback identidad y negativo de usuario ajeno |
| R03 concurrencia | Reclamar una sola vez por ID, sin doble escritor | Estado PENDIENTE→EN_PROGRESO con readback | Dos intentos concurrentes: un ganador, ningún overwrite |
| R04 dependencias | Validar DAG, lotes y gates por resultado exigido | COLA G/O SI + DAG VALIDADO + predecesores reales | Prueba positiva todos LISTO; negativa un predecesor omitido |
| R05 stack | Aplicar pruebas proporcionales a artefactos de código | .NET/Angular/MySQL según cambios, no por pipeline global | Check runs SHA exacto y casos positivos/negativos de consumidor |
| R06 A0 | Rechazar secretos, diffs inseguros y ruptura de contratos | A0 en VALIDANDO antes de publicar | Diff, hashes y vulnerabilidades/linters donde apliquen |
| R07 A1/A2 | Pruebas funcionales, JWT/RBAC/tenant y PII según riesgo | Sin PASS cuando faltan negativas o DB real exigida | API/HTTP/DB A/B efímera para cambios relevantes, no mocks solos |
| R08 A3 | Probar interfaces, integración y migraciones por alcance | No SKIPPED=PASS ni CI de otro SHA | Jobs efectivos, logs, TRX/artifacts y resultados causales |
| R09 A4 | Publicar directo fast-forward dev y auditar después | SHA/parent/diff/readback coherentes | GitHub check-runs exact-SHA + PLAN/COLA/BITACORA |
| R10 A5 | Revisión ajena de código y evidencias sin autoaprobación | Revisor de otra Task Javier, autor distinto | Caso positivo+negativo o equivalente, evento A5 por SHA |
| R11 observabilidad | BITACORA append contiguo, 26 campos, idempotente | EVENT_ID e IDEMPOTENCY_KEY únicos; RUN_ID solo real | Readback A:Z y negativos de duplicado/truncamiento |
| R12 rollback | Preservar historia y trabajo concurrente | Sin force-push, sin borrar logs ni reescribir commits | Git revert normal + reconciliación institucional auditada |

La matriz R01–R12 enumera **referencias de cobertura documental**, NO genera 12 nuevas microtareas ni altera el catálogo V5. Las entregas M02–M06 existentes serán las únicas que podrán implementar cambios. Cada prueba se asigna causalmente al resultado de su ID y a su propio SHA, sin prometer pruebas que todavía no se ejecutaron.

## Casos de aceptación del cierre de la matriz (documentales)

| Caso | Tipo | Resultado del control sobre fuentes y reglas |
| --- | --- | --- |
| P01 | Positivo documental | Cinco predecesoras M01.01–.05 LISTO + A5 causal y C00-01.M06.06 LISTO+A5, DAG VALIDADO, PLAN owner vacío, COLA G/O SI autorizan reclamar M01.06 |
| P02 | Positivo documental | Cada R01–R12 tiene mejora, contrato, prueba y evidencia identificables sin crear otra cola ni reordenar V5 |
| N01 | Negativo documental | Falta cualquiera de los seis predecesores o A5 causal: denegar claim/cierre M01.06 |
| N02 | Negativo documental | Un agente/Task ajena al censo intenta escribir o revisar: no aceptar identidad ni A5 |
| N03 | Negativo documental | PR histórico/FIFO/plan V3 intenta reintroducir ID o fase: rechazar |
| N04 | Negativo documental | COLA fórmula diverge de PLAN H: detectar y conciliar antes de actuar, no sobrescribir E manualmente |
| N05 | Negativo documental | Dos Tasks trabajan mismo ID/archivo: no doble claim ni pérdida de SHA del otro |
| N06 | Negativo documental | Sólo tests sintéticos o mocks para seguridad tenancy real: no certificar A1–A3 runtime |
| N07 | Negativo documental | Check SKIPPED/CANCELLED/IN_PROGRESS sobre SHA requerido: no A4 PASS |
| N08 | Negativo documental | A5 del autor o de cuenta externa: no A5_PASS institucional |
| N09 | Negativo documental | BITACORA evento con 21 columnas, RUN_ID inventado o clave duplicada: rechazar append |
| N10 | Negativo documental | M01 LISTO se usa para habilitar arbitrariamente M02 o QA/main/PROD: denegar |
| N11 | Negativo documental | Un estado VALIDANDO se confunde con cierre: conservar owner y exigir auditorías |
| N12 | Negativo documental | Revert o corrección sobrescribe auditoría histórica o commits ajenos: rechazar |

P01/P02 y N01–N12 se validan documentalmente contra reglas/fuentes: **no son catorce pruebas ejecutadas de software** ni equivalen a la implementación de la matriz integrada.

## Gates finales, auditorías y rollback

La salida `M01.06 → LISTO_DEV` solo se permite con A0–A4 causales de este documento y A5_PENDIENTE a otra Task Javier. M02 no recibe permiso de despacho por el mero texto: cada ID exige nueva lectura de PLAN H/I, COLA G/O, DAG y pruebas proporcionadas; M02 ejecutable no usa PASS heredado de documentos. No crear tareas ni tocar QA/main/PROD.

Clasificación: `requiere_dotnet=NO_APLICA`, `requiere_node_angular=NO_APLICA`, `requiere_mysql=NO_APLICA`, `requiere_docker=NO_APLICA`, `requiere_e2e=NO_APLICA`; `requiere_security=SI` (autoría, permisos, independencia, tenant A/B y secretos), `requiere_scope_lock=SI` (commit documental). A0 revisa cinco blobs y diff/secretos; A1 matriz + positivos y negativos documentales; A2 fail-closed y no cambio de seguridad; A3 coherencia DAG/PLAN/COLA/consumidores; A4 SHA/parent/diff/checks/readback; A5 pendiente de otro TASK_ID Javier antes de cualquier promoción.

Rollback: `git revert` normal del commit documental tras revalidar HEAD y conciliación institucional aditiva sin borrar BITACORA. Cero PR, ramas, credenciales, servicios pagos o datos productivos.
