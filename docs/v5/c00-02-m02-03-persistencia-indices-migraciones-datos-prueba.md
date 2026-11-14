# SOLQARYN V5 FINAL — C00-02.M02.03: persistencia, índices, migración reversible y datos de prueba

**Entrega TIPO 1:** especificación de persistencia y evolución del registro institucional de cola/autoría, sin alterar el esquema real, código, proveedores ni ejecutar migraciones. **Autor:** PRIMARY DEV 05 — JAVIER, TASK_ID 6adc2e55f47c81919f6374e561687076. **Proyecto:** solqaryn/Solqaryn rama dev, PROJECT_SCOPE_LOCK=STRICT. **Autoridades:** Standard de Implementación V5 FINAL (Google Doc 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA); Sheet 1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU; gobernanza/arquitectura y docs/AUTOMATION_V13.md del HEAD vivo.

**Resultado exigido de M02:** definir cola, dependencias, autoría, estados y cierre sin duplicados, más evidencia de contrato y ejemplo causal. En este ID se documentan estructuras existentes e índices/migraciones como **diseño futuro NO aplicado**. No introducir tablas MySQL de control ni duplicar autoridad con un backend paralelo. C00-02.M01.06 consta LISTO; COLA G/O=SI y owner vacío al preflight; sin DAG individual, usar gate conservador. No liberar M03 hasta que M02 completo satisfaga sus puertas.

## 1. Persistencia institucional real (lectura comprobable)

| Superficie | Identidad/clave | Operación autorizada | Invariante |
| --- | --- | --- | --- |
| PLAN_MAESTRO (A:M) | A=MICROTAREA_ID, H=ESTADO, I=RESPONSABLE | Actualizar estado/owner y evidencia de ID autorizado | 2.340 IDs y exactamente una fuente para H/I |
| COLA (A:Q) | A=ID, E=estado por fórmula, G/O=puertas | Lectura de gates y coherencia con PLAN | Nunca asignar manualmente estado E ni convertir SI en lock |
| DAG_TECNICO (A:G de contrato) | A=ID, B=prerrequisitos, F=VALIDADO, G=gate | Leer precedentes y evaluación técnica | Sin entrada DAG: gate conservador, no bypass |
| BITACORA (A:Z) | A=EVENT_ID, R=IDEMPOTENCY_KEY | Append de evento auditado con 26 columnas | Inmutabilidad de historial, claves únicas por operación |
| TAREAS_PROGRAMADAS (AA:AN) | C=TASK_ID, AA=clave corrida local | Último reporte por el ejecutor de su fila | No fuente autoritativa de estado/claim |
| Git dev | commit SHA padre + tree SHA + diff | Publicar solo FF sin force-push/PR nuevos | La evidencia Git no sustituye Sheet ni auditoría |

El estado de H se limita a PENDIENTE, EN_PROGRESO, VALIDANDO y LISTO. Los readbacks deben comprobar que COLA E replica H y que el owner I es el mismo antes y después del cierre. BITACORA conserva autor original, revisor distinto y RUN_ID vacío salvo evidencia nativa. El campo AL de monitor no certifica resultados GitHub ni tiempos reales de ejecución del proveedor.

## 2. Claves, relaciones y unicidad

- Clave de microtarea: ID V5 fijo, no número incremental ni un nuevo ID 2.341.
- Propiedad activa: un owner TASK_ID autorizado por ID; lectura+escritura de Sheets no es transacción CAS segura. Para transferencia, exigir mecanismo de exclusión real y ausencia de escritor simultáneo.
- Integridad referencial lógica: cada evento BITACORA N debe apuntar a un ID existente en PLAN A y a un TASK_ID de las seis Javier; nunca borrar un evento anterior con owner histórico retirado.
- Unicidad de evento: EVENT_ID y IDEMPOTENCY_KEY únicos a nivel semántico. Antes de append, buscar la clave; después, leer de nuevo la fila con sus 26 columnas.
- Integridad causal: BASE_HEAD es la versión padre, RESULT_HEAD es el commit efectivo; no confundir blob, tree, HEAD previo ni un job de otro SHA.
- Estado A5: A5_PENDIENTE del autor, revisión A5_PASS/A5_FAIL por TASK_ID distinto; un A5_FAIL histórico no se borra ni se neutraliza con nueva documentación.
- Seguridad: no almacenar JWT, password, emails privados, tokens, secretos de proveedor ni contenido de tenant A/B en eventos; usar identificadores técnicos y huellas no sensibles.

## 3. Índices y persistencia relacional propuestos (NO IMPLEMENTADOS)

Si se aprueba por separado un modelo relacional de control, diseñar como mínimo:

| Relación futura | Clave/índice sugerido | Propósito y límite |
| --- | --- | --- |
| v5_microtasks | PRIMARY KEY(microtask_id), INDEX(estado), INDEX(owner_task_id, estado) | Búsquedas por estado/propietario sin alterar el catálogo fijo |
| v5_dependencies | UNIQUE(microtask_id, predecessor_id), INDEX(predecessor_id) | DAG finito, detectar self-reference y ciclos antes de insertar |
| v5_events | PRIMARY KEY(event_id), UNIQUE(idempotency_key), INDEX(microtask_id, occurred_at_utc) | Eventos append-only y deduplicación de escritura |
| v5_publications | UNIQUE(microtask_id, commit_sha), INDEX(commit_sha) | Relación entre entregas y commits SHA exactos |
| v5_reviews | UNIQUE(microtask_id, commit_sha, reviewer_task_id, verdict_fingerprint) | Trazar A5 por versión sin suplantación |
| v5_run_reports | UNIQUE(task_id, local_run_key) | Telemetría autoinformada separada de la autoridad del catálogo |

Estos nombres e índices son una **propuesta de modelo**, no tablas existentes ni autorización para instalar MySQL, EF migrations, triggers, constraints o worker. El versionado de pruebas .NET/MySQL solo procede si otra microtarea implementa un cambio ejecutable. Si se adopta una réplica futura, la fuente institucional permanece Sheet hasta un corte formal aprobado y reversible; nunca doble escritura silenciosa.

## 4. Plan de migración futura, con gates (no ejecutado)

1. Tomar snapshot autorizado de encabezados y límites reales, fórmulas COLA, 2.340 IDs, DAG validado, BITACORA A:Z y duplicados EVENT_ID/IDEMPOTENCY_KEY; registrar SHA de fuentes y fecha. No exportar secretos/PII.
2. Validar transformaciones de estado/owner y relaciones, incluidos registros históricos con actores retirados; ningún owner histórico adquiere permiso actual.
3. Dry-run en instancia MySQL efímera aislada con datos sintéticos: crear claves e índices propuestos, ensayar duplicados, conflictos de dueño, cascadas peligrosas, orden y límites. Sin afirmar que esta prueba se ejecutó por redactar este documento.
4. Comparar conteos 2.340, IDs, orden y semántica; simular reintento idempotente, rechazo de DAG circular y readback. Errores o pérdida de filas = NO_GO sin cutover.
5. Solo con autorización separada, desplegar adaptador controlado y conmutación reversible, monitoreo de doble escritura y rollback. No cambiar Vercel/Render/Aiven, QA/main/PROD, dominios ni despliegues desde esta tarea.
6. Reversión: desactivar adaptador por mecanismo aprobado y volver al Sheet canónico sin borrar eventos, permisos, estados ni historia. El rollback de **esta entrega documental** es git revert del commit en dev.

## 5. Fixture de diseño y ejemplos positivos/negativos (NO datos reales)

El fixture exclusivamente ilustrativo usa un **ID ya incluido** en V5 y variables genéricas, no filas nuevas ni cuentas creadas:

- ID observado C00-02.M02.03; owner de ejemplo TASK_JAVIER_AUTORIZADA; evento de ejemplo EVENT_ID_DE_PRUEBA; clave IDEMPOTENCY_KEY_DE_PRUEBA; commit ficticio indicado como SHA_DE_EJEMPLO_NO_REAL. Los placeholders no se escriben en BITACORA.
- Ciclo lógico: PLAN H=PENDIENTE/I vacío + G/O SI + M01.06 LISTO → EN_PROGRESO y owner real → candidato VALIDANDO → A0–A3 pertinentes → commit FF dev → A4 checks exact SHA y readback → A5_PENDIENTE → LISTO. Otro TASK_ID distinto podrá revisar A5.

| Caso | Signo | Oráculo contractual |
| --- | --- | --- |
| P01 | + | Catálogo de 2.340 IDs distintos → ningún duplicado de clave |
| P02 | + | PLAN H=LISTO y fórmula COLA E vinculada a H → mismo estado |
| P03 | + | Repetir lectura de un EVENT_ID existente → no agregar segundo evento |
| P04 | + | Snapshot de un evento completo A:Z con 26 campos → preservar orden físico y evidencia |
| N01 | - | Insertar 2.341 ID, alterar ID histórico o inventar estado → rechazar |
| N02 | - | Dos claims sobre el mismo ID sin exclusión CAS → no afirmar propiedad doble ni promoción |
| N03 | - | Duplicar EVENT_ID o IDEMPOTENCY_KEY → rechazar append repetido |
| N04 | - | Recortar BITACORA A:Z a A:U → fallo de integridad de esquema |
| N05 | - | DAG circular, dependencia sin LISTO real o gate G/O NO → no liberar tarea |
| N06 | - | Un job de SHA anterior se interpreta como PASS del actual → rechazar A4 |
| N07 | - | Revisor A5 coincide con autor o no está en allowlist → rechazar |
| N08 | - | Registro contiene JWT, claves DB o secretos de tenant → rechazar por seguridad |
| N09 | - | Se intenta crear tabla real, migrar o cambiar QA/PROD solo por documento → denegar |

Los casos anteriores son **pruebas de consistencia del contrato documental** y *no* representan ejecución MySQL, rollback real, asserts HTTP, pruebas .NET, validación Playwright ni transacciones CAS nativas. Los nombres de índices futuros no son evidencia de migraciones existentes.

## 6. Evaluación de impacto y A0–A5 proporcional

**Capas:** documentación de gobernanza V5 y consumidores de Sheet/GitHub; sin cambios en Domain/Application/Infrastructure/API, frontend, migraciones EF, datos tenant, workers, proveedores ni secretos. **API/DTO afectados:** ninguno ejecutable; sólo modelo de persistencia propuesto. **Seguridad:** revisar rechazo de owner no autorizado, 26 campos, datos sensibles, idempotencia y aislamiento A/B. **Pruebas:** A0 inspección diff y coherencia de fuentes; A1 casos P01–P04/N01–N09 como oráculos documentales; A2 modelo de permisos y no exposición de secretos; A3 compatibilidad lógica con M02.01, M02.02, M02.04 y M02.05, sin pruebas runtime porque no cambia código. .NET, Angular/Node, MySQL, Docker y E2E son subpruebas NO_APLICA por alcance. **A4:** diff, commit FF exacto y check Scope Lock SUCCESS del SHA nuevo, readback PLAN/COLA/BITACORA. **A5:** otro ejecutor Javier realiza revisión independiente; no autopromoción QA/main/PROD.

La matriz SQL es únicamente diseño y no una migración implementada. Si se detecta discrepancia de integridad o fail-closed, mantener VALIDANDO, corregir y volver a auditar; nunca falsificar pruebas de runner ni alterar datos productivos.