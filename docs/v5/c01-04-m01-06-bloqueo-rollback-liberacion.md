# SOLQARYN V5 FINAL — C01-04.M01.06: bloqueo, rollback y salida documental del expediente universal

**Identificador exclusivo:** `C01-04.M01.06`. **Autor:** PRIMARY DEV 03 — JAVIER, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Rama y seguridad:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Fecha:** 2026-11-14. **Tipo:** contrato de salida M01, no implementación de expediente.

**Autoridad:** [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN_MAESTRO, COLA, DAG_TECNICO y BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). Los estados, autores y dependencias se verifican por ID exacto. No se modifica otra microtarea, no se agrega nueva fila ni se reabre backlog archivado.

## 1. Límite de alcance y resultado del lote

El resultado **futuro** de C01-04 consiste en un expediente y seguimiento universal que correlacione cronología, decisiones, observaciones, adjuntos y versiones para un recurso de negocio, aislado por empresa, sucursal y autorización efectiva. Debe mantener la fuente transaccional separada de la proyección de historial, preservar el autor de cada hecho y soportar consulta ordenada y paginada sin mezclar tenants.

**Esta M01.06 únicamente define condiciones de bloqueo, rollback y liberación documental del lote M01.** No crea APIs, DTOs, tablas, vínculos de storage, interfaces Angular, eventos, worker, outbox, dispatcher, aprobadores, migraciones, permisos ni reglas productivas. No certifica consultas HTTP/JWT, MySQL, Playwright ni funcionamiento del expediente universal. Las decisiones formales, contratos y pruebas de M02+ tienen validación autónoma.

## 2. Trazabilidad específica de dependencia y propiedad

Al preflight, los cinco preparativos `C01-04.M01.01–05` figuran LISTO y poseen revisión A5 independiente de una Task Javier habilitada, manteniendo sus autores. Los contratos adicionales `C01-01.M01.06`, `C01-02.M01.01–05` y `C01-03.M01.01–05` se comprobaron LISTO; los A5 históricos de cuentas retiradas **no son autoridad**, y constan revisiones correctivas de Javier para las unidades pertinentes. La entrada DAG propia dice `F=VALIDADO`, `G=SI`, con estos prerrequisitos explícitos. El gate habilita evaluar el ID, no omitir auditorías ni liberar etapas automáticamente.

| Predecesor | Artefacto fuente o evidencia consultada | SHA de blob comprobado en dev |
| --- | --- | --- |
| C01-04.M01.01 | `docs/v5/c01-04-m01-01-objetivo-expediente-timeline.md` | `c6a8f326569e1afe780b2afd79548ad9dab4f53d` |
| C01-04.M01.02 | `docs/v5/c01-04-m01-02-actores-entradas-salidas.md` | `29184fdc66a5ccec8998c85851f1c505aa02ca80` |
| C01-04.M01.03 | `docs/v5/c01-04-m01-03-inventario-contratos-codigo-evidencia.md` | `1488cb861ceef6f852bb1d9059585239d10cd0e9` |
| C01-04.M01.04 | `docs/v5/c01-04-m01-04-riesgos-supuestos-decisiones-expediente.md` | `d3db4eac1419e7540ca601084bfc935b746988f7` |
| C01-04.M01.05 | `docs/v5/c01-04-m01-05-aceptacion-expediente-timeline.md` | `1cad256ee30742ccc0384fe00eaff586a97add11` |
| C01-01.M01.06 | Contrato de contexto, identidad y membresía de la plataforma | PLAN LISTO y A5 Javier independiente (SHA `009d412176429266eee6574ac632bca333f3b0b2`) |
| C01-02.M01.01–05 | Contratos de capabilities y permisos sin grants automáticos | PLAN LISTO y A5 Javier por ID (M01.05 corregida J40) |
| C01-03.M01.01–05 | Contratos de configuración tipada, sin código/HTML/SQL inyectable | PLAN LISTO y A5 Javier por ID; no depender del cierre M01.06 de otra Task en curso |

**Lectura transaccional de esta Task:** `PLAN_MAESTRO!H259=PENDIENTE/I259` vacío y `COLA!G259/O259=SI` antes del claim; el único claim CAS de Javier :20 cambia a `H259=EN_PROGRESO`, `I259=TASK_ID`, `COLA!O259=NO` por fórmula. Para VALIDANDO/LISTO: A0–A4 propios y BITACORA A:Z con EVENT_ID/IDEMPOTENCY_KEY únicos, readback final, A5 pendiente de otro TASK_ID permitido.

## 3. Bloqueos y reversibilidad por superficie

| Gate | Precondición antes de implementación futura | Respuesta segura/rollback |
| --- | --- | --- |
| Identidad y tenant | Servidor obtiene membresía activa, empresa/sucursal y ownership real del recurso; permiso del rol vigente | Petición A→B, usuario revocado, recurso con `EmpresaId=null`, URL manipulada o sucursal ajena: DENY sin filtrar PII |
| Línea de tiempo | Orden total y paginación deterministas incluso con igual timestamp; trazabilidad de actor, UTC, recurso, versión y correlación | Historial duplicado, reordenado o sin propietario: bloquear liberación; no sobrescribir hechos históricos |
| Decisiones y observaciones | Observación ≠ aprobación formal; decisión con rol, motivo, estado y autorización explícita | Rol sin grant, decisión autoaprobada, soporte fuera de allowlist o replay: DENY y ninguna mutación |
| Adjuntos y versiones | Ownership y control de lectura server-side, checks de tipo/tamaño/URL y política de retención | Blob externo no validado, enlace de B, path traversal o borrado destructivo: no cargar ni borrar |
| Reuso de auditoría existente | Revisar filtros de `AuditoriaRepository.GetFilteredAsync` y globales EF antes de derivar una API de expediente | El inventario observó falta de predicado tenant explícito en el método: **riesgo a investigar**, no vulnerabilidad demostrada; NO_GO de exposición hasta prueba A/B |
| Concurrencia | PLAN H/I + CAS único, relectura de HEAD y exclusión de archivos, sin force-push | Otro owner o HEAD cambiado: no sobrescribir; reconciliar con auditoría y revert normal si corresponde |
| Quality gates | A0 documental, A1 positivos/negativos diseñados, A2 seguridad fail-closed, A3 fuentes y dependencias, A4 commit fast-forward y Scope Lock/V5 exact-SHA con readback | Un check obligatorio CANCELLED/SKIPPED/IN_PROGRESS/FAIL no es PASS; CI de runtime no relacionada con solo Markdown = NO_APLICA |
| Revisión y promoción | A5 independiente por Task Javier distinta; luego autorización expresa separada de QA/main/PROD | Autorevisión o evidencia ajena a SHA no conceden promoción ni estado productivo |

**Rollback de este changeset:** `git revert` normal del commit documental tras comparar HEAD y dependencias; no modificar el histórico A:Z, preservar dueño y dejar evento compensatorio si fuera necesario. Este rollback no revierte MySQL o datos productivos: esta entrega **no los cambia**. Una implementación futura deberá documentar rollback transaccional/migratorio propio.

## 4. Pruebas de aceptación documentales y rechazos causales

| Caso | Tipo | Oráculo verificable en el documento |
| --- | --- | --- |
| P01 | + | Los cinco M01 propios y antecedentes técnicos explícitos figuran LISTO con A5 Javier válida; DAG F=VALIDADO/G=SI, claim CAS único |
| P02 | + | Timeline, adjuntos, decisiones, observaciones y versiones se relacionan con tenant, ownership, actor, UTC y rollback sin afirmar runtime |
| P03 | + | Solo Markdown y CHANGELOG; commit FF, blobs exactos, Scope Lock y procedencia V5 SUCCESS del mismo SHA; BITACORA/PLAN/COLA readback y A5_PENDIENTE |
| N01 | − | Falta un antecedente, A5 correctiva, gate DAG o propiedad única: NO_GO |
| N02 | − | Tenant A consulta evento/adjunto de B, rol revocado o resource null-owner: DENY |
| N03 | − | Timeline con empate de timestamp sin ID determinista, paginación inconsistente o historia alterada: bloquear implementación |
| N04 | − | Observación presentada como aprobación, decisión de rol no autorizado o soporte con permiso global implícito: DENY |
| N05 | − | Subida/lectura de adjunto con URL/path inseguro, secrets/PII en log, payload HTML/script inyectable: DENY |
| N06 | − | CI de otro SHA, SKIPPED como PASS, evento BITACORA incompleto, A5 de autor o Task retirada: NO_GO |
| N07 | − | M01 documental se atribuye endpoints, query tenant segura ya probada, MySQL, E2E, workers/eventos o QA/PROD: NO_GO de certificación |

**Clasificación de este diff**: solo `.md` y CHANGELOG, sin código ni arquitectura nueva; `.NET/API=NO_APLICA`, `Angular/Node runtime=NO_APLICA`, `EF/MySQL/Docker=NO_APLICA`, `Playwright=NO_APLICA`. Se exige revisión estática de JWT/RBAC/tenant/PII y los checks ligeros GitHub obligatorios del SHA. P01–P03/N01–N07 son **oráculos de aceptación documental** que no se etiquetan como pruebas runtime ejecutadas.

## 5. Condiciones de salida y continuidad segura

El resultado cerrado de M01 permite **evaluar por separado** cada unidad de M02, nunca reclamar en cascada: releer PLAN estado/owner, COLA G/O, DAG F/G, dependencias, A5 causal, herramientas y exclusión de archivos. No crea eventos inter-Task, no habilita dispatcher, no concede permiso a usuarios comerciales y no libera C01-04.M06.06, IAM/JWT/RBAC, base de datos, QA/main/PROD o la terminación global V5. Una brecha de seguridad o A5_FAIL histórica conserva gate hasta reparación y verificación independiente del SHA.
