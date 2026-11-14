# SOLQARYN V5 FINAL — C00-01.M06.05: solicitud de revisión técnica independiente del lote

**Solicitud institucional, NO dictamen A5.** Responsable de esta solicitud: PRIMARY DEV 01 — JAVIER, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`, GitHub `jmejia31`. La atribución de cuenta y commit no es firma criptográfica ni reemplaza una revisión por otro TASK_ID. Fecha de preparación: 2026-11-13 (America/Tegucigalpa).

**Fuente ejecutable exclusiva:** [Standard V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN_MAESTRO/COLA/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). `PLAN_MAESTRO!H` es el estado canónico, `COLA!E` lo refleja por fórmula y `G/O` expresa elegibilidad; las puertas por fase y el DAG se deben comprobar de forma independiente. El universo institucional es 2.340 IDs, sin historial V13 como backlog.

## Identidad, precedencia y clasificación previa

Al claim de M06.05: las seis M05 (filas 26–31) constaban LISTO; M06.01–M06.04 constaban LISTO; M06.05 estaba PENDIENTE, sin responsable, `COLA!G36=SI`, `O36=SI`; M06.06 continuaba PENDIENTE. M06.05 **no** figura en `DAG_TECNICO` como excepción VALIDADO; por ello se utiliza exclusivamente el gate conservador existente para su fila, sin anticipar la liberación de M06.06. HEAD de referencia inicial `6b3eccd44a322c205bcb5db13e8348ed740e041c`, sujeto a revalidación antes de cualquier publicación/revisión.

**Tipo 1, solicitud documental sin cambio ejecutable:** requiere_dotnet=NO; requiere_node_angular=NO; requiere_mysql=NO; requiere_docker=NO; requiere_e2e=NO; requiere_security=SI (independencia A5, autorización, RBAC/tenant, secretos y riesgos documentados); requiere_scope_lock=SI (commit documental exact-SHA). Esta clasificación describe **esta solicitud**, no exime a los revisores de las pruebas reales pertinentes a los SHA que evalúen.

**Alcance autorizado:** registrar matriz reproducible, solicitar A5 por otro TASK_ID, conservar FAIL/PENDIENTE históricos y rollback seguro. No emitir A5_PASS por esta solicitud, no cambiar propiedad de otra Task, no implementar correcciones ajenas, no tocar QA/main/PROD ni infraestructura o datos productivos.

## Manifiesto de fuentes y hash de revisión

| Fuente o unidad | Identificador y huella verificable | Estado / solicitud |
| --- | --- | --- |
| Gobierno y arquitectura | Skill `304e400d5c0f89fcfdcc65caece86c34276734b5`; arquitectura `1931e1229752028229b6ad1bb3368038e2c6769b` | Scope Lock STRICT; revalidar contra HEAD vivo |
| M06.01 correspondencia pruebas/mocks | commit `44065130c13be001370130bfe11c683636bd4227`; documento blob `e8622341597558c84310a91d132f8b75367e91e7` | LISTO_DEV, A5_PENDIENTE al snapshot: requerir revisor distinto del autor J50 |
| M06.02 manifiesto de artefactos | commit `6b3eccd44a322c205bcb5db13e8348ed740e041c`; documento blob `4bc3e1b3de77b8544114904023aaf692bdfd50d4` | LISTO_DEV, A5_PENDIENTE al snapshot: reproducir SHA/blobs y pruebas negativas |
| M06.03 riesgos/contradicciones | commit correctivo `a849ad2965f96cff7b3247cbe1703ec6a39b6096`; documento blob `2af0c3abe67d0ac0853fedff099d5eafebadb352` | LISTO_DEV; A5_PASS documental por TASK_ID ajeno, FAIL del SHA anterior preservado |
| M06.04 rollback | SHA de ensayo `92695cf9` (consultar commit completo y check-runs antes de certificar); resultados de MySQL descartable y guard shell | LISTO_DEV; A5_PASS independiente reportado; no equivalencia con recuperación PROD |
| Branding M04.03 (riesgo transversal) | SHA original `fb20c55ed4f9a9cb09f18c9b81d3d97d84b08605` | A5_FAIL por callback de tema/SEO tardío; requiere corrección y revisión nueva exact-SHA, no convertirla en PASS por ascendencia |
| BITACORA institucional | `BITACORA!A1:Z1` = **26 campos**, EVENT_ID A e IDEMPOTENCY_KEY R únicos | RUN_ID vacío si no hay ejecución nativa comprobable; rechazar truncamiento 21–25 |

El identificador de cada commit es una huella Git verificable; el estado de Sheet es un snapshot cambiante, no una certificación de que todos sus controles permanecen completos. Una aprobación previa de un SHA distinto no valida un candidato nuevo.

## Solicitud técnica dirigida a un revisor independiente

Un agente autorizado **con TASK_ID distinto del autor de cada SHA** deberá:

1. Confirmar identidad/cuenta, Skill canónica, Scope Lock del **SHA de cada unidad** y autorización V5; comprobar PLAN H/I, COLA E/G/O, DAG y BITACORA A:Z sin reclamar propiedad ajena.
2. Para M06.01, verificar el caso **positivo** de API/HTTP/MySQL realmente invocado en [M10 run 43463854111](https://github.com/solqaryn/Solqaryn/actions/runs/43463854111) y un **negativo** que distinga `route.fulfill` sintético de una respuesta real: un mock de dashboard **no** demuestra backend ni aislamiento A/B. Revisar también los límites de la suite seguridad [run 43463853912](https://github.com/solqaryn/Solqaryn/actions/runs/43463853912).
3. Para M06.02, reproducir un **positivo** de hash/rutas, árbol y [Scope Lock run 43468890474](https://github.com/solqaryn/Solqaryn/actions/runs/43468890474) sobre su SHA exacto; como **negativos**, rechazar un run de SHA anterior, job SKIPPED tomado como PASS y eventos BITACORA incompletos.
4. Para M06.03, verificar la corrección positiva A:Z/26 del documento y la conservación del A5_FAIL original; comprobar los **negativos** de EVENT_ID duplicado, idempotencia duplicada, RUN_ID ficticio, columnas 21–25 y autoaprobación del autor.
5. Para M06.04, revisar el ensayo positivo MySQL 8.4 descartable y la prueba negativa del guard shell, distinguiendo este último de fault injection o restore real productivo. Si falta evidencia causal del job, registrar A5_FAIL/PENDIENTE, no inventarla.
6. Para M04.03, **conservar** el A5_FAIL por falta de caso negativo de navegación `/tienda` → `/login`/otro tenant antes del retorno async. Una corrección del código necesita nuevo SHA, CI Angular/E2E pertinente y A5 independiente nueva.
7. Para esta M06.05, revisar el **SHA de publicación del presente documento**, referencias positivas y rechazo negativo de autoría falsa, QA prematuro, evidencia de otra versión o permisos cruzados. La presente Task **no** puede aprobar su propia solicitud.
8. Registrar por SHA un único A5_PASS o A5_FAIL sustentado en prueba positiva+negativa real/equivalente, identidad de revisor, enlaces a job/run/log, riesgos y rollback; BITACORA con **26 campos A:Z** y EVENT_ID/IDEMPOTENCY_KEY únicos. Sin segundo revisor efectivo, mantener A5_PENDIENTE.

## Matriz de contradicciones y decisión conservadora

| Contradicción potencial | Rechazo / control verificable |
| --- | --- |
| `LISTO_DEV` frente a `A5_PENDIENTE` | A0–A4 permiten cierre DEV, **no** certifican revisión independiente o promoción |
| `Scope Lock SUCCESS` frente a CI parcial | A4 exige checks pertinentes de cada alcance; no falsear PASS global ni exigir jobs ajenos a docs |
| Documento de mocks frente a prueba HTTP real | Marcar cobertura concreta, no elevar route.fulfill a backend seguro |
| A5_FAIL previo frente a commit posterior | Preservar FAIL histórico, exigir una revisión nueva del SHA corregido |
| Lote M06 con M06.06 aún PENDIENTE | Esta solicitud **no libera** el siguiente lote ni cambia fórmulas/gates |
| BITACORA A:U / 21 frente al esquema A:Z / 26 | Usar encabezado vivo, rechazar registros truncados y no crear filas auxiliares |
| Firma declarada por texto frente a identidad real | Esta solicitud sólo atribuye responsable; A5 exige otro TASK_ID autenticado |

## Verificaciones A0–A5 y condición de salida

**A0:** diff Markdown+CHANGELOG, fuentes, enlaces, integridad, ausencia de secretos, cambios de runtime y duplicación. **A1:** solicitud completa con matriz positiva/negativa y revisores elegibles, sin simular su aceptación. **A2:** separación de identidades/tenants, JWT/RBAC/PII, FAIL vigentes y prohibición QA/main/PROD. **A3:** consistencia del inventario con Doc V5, Git blobs, PLAN/COLA/DAG y BITACORA física; .NET/Angular/MySQL/Docker/E2E nuevos **NO_APLICA** a este registro documental. **A4:** después de publicar, comprobar commit FF, parent/tree/blobs, Scope Lock `completed/success` exact-SHA y readback PLAN/COLA/BITACORA. **A5:** PENDIENTE de otro TASK_ID, sin autopromoción.

**Entrega atómica de M06.05:** solicitud y matriz publicadas, auditorías documentales aplicables completas, evento institucional A5_PENDIENTE y LISTO_DEV sólo si A4 aprueba. **No** declarar que una revisión independiente del lote terminó sólo porque se solicitó. Para M06.06 deberá releerse el estado y los A5_FAIL de toda la fase, resolver lo que sea gate efectivo, y evitar liberación ficticia.

**Rollback:** `git revert <SHA-publicado-de-M06.05>` normal, con HEAD revalidado; reconciliar evidencias de Sheets mediante eventos nuevos, sin borrar historial ni reescribir commits de terceros.
