# SOLQARYN V5 — C15-01.M02.06: contrato de salida y dependencias de auditoría de cobertura

**Microtarea:** `C15-01.M02.06` · **Responsable:** PRIMARY DEV 04 — JAVIER_CHATGPT, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09` · **Fecha:** 2026-11-14 (America/Tegucigalpa) · **Repositorio:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`.

**Resultado delimitado:** consolidación **documental** de la trazabilidad, precondiciones y rollback del lote M02 de cobertura de requisitos. No se ejecuta la auditoría exhaustiva, no se certifican cero requisitos huérfanos, cero duplicados críticos, cero P0/P1 ni seguridad multiempresa runtime. No se crean endpoints, workers, eventos, webhooks, tablas, flags ejecutables, Tasks o despliegues QA/main/PROD. Todos los escenarios dinámicos son `FUTUROS_NO_EJECUTADOS`.

## 1. Autoridad, preflight y evidencia de predecesores

Fuente autorizada: [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN_MAESTRO, COLA, DAG_TECNICO, BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), Skill canónica, `AGENTS.md`, Scope Lock y arquitectura del HEAD `dev`. El preflight para esta unidad acreditó PLAN H2173=PENDIENTE/I2173 vacío, COLA G/O2173=SI, DAG registro ID exacto `C15-01.M02.06` F=VALIDADO/G=SI. El gate exige `C15-01.M01.01–06+A5`, `C15-01.M02.01–05+A5` y `C00-03.M01.06+A5`. El claim posterior debe conservar un solo owner, evento idempotente y readback; una escritura sobre Sheets no constituye CAS distribuido demostrable sin primitiva transaccional.

| Antecedente | Fuente Git y evidencia | Alcance reconocido |
| --- | --- | --- |
| `C15-01.M01.01–05` | Documento M01.06 con revisión de cinco SHA y A5 independientes | Alcance, actores, brechas, riesgos y oráculos previstos; no cobertura real |
| `C15-01.M01.06` | `docs/v5/c15-01-m01-06-bloqueos-rollback-liberacion-cobertura.md`, blob `c158ad18ca8de807731e6a87f5fe74870be32dcb` | B01–B12; LISTO+A5 independiente |
| `C00-03.M01.06` | SHA `89a9f0c681d163a8ca617523075249af9fb8a2c6`, PLAN LISTO+A5 independiente | Gate transversal documental, no permiso de ejecución |
| `C15-01.M02.01` | `54aa6cdc914d835b118c89578d21faa308ee6a13`, SHA `22f3fca891f46173ae671bbd75f7713a82b40a0f` | CT01–CT16/CV01–CV12, A5 independiente J30 |
| `C15-01.M02.02` | `6a939a016f347dac0764dc8168a90c362f0b9fc2`, SHA `71c36b469d932492e277d59f64d36f70977c0907` | OP01–08, errores, T01–12, A5 independiente J30 |
| `C15-01.M02.03` | `4acf13d2ab6a1bae2077c91358022f87cd577daa` | PD01–12/IX01–06 y A5 independiente J20, sin MySQL |
| `C15-01.M02.04` | `07dd50a24ac3ae907947c68cbbf7b680d488b840` | CFG01–16/VAL01–12, A5 independiente J20, sin runtime flags |
| `C15-01.M02.05` | `bea81da1694b9ba65aa9e5d94b92831f2c728049`, SHA `b9e21edefd7d4d8b39c0e212c95e2e575805125f` | EV01–10/ID01–08/LM01–12/T01–12, A5 independiente J30 |

Los tres A5 nuevos J30 para M02.01, M02.02 y M02.05 fueron registrados con SHA/blob/autoría distinta en PLAN K y BITACORA A:Z antes de reclamar esta terminal. El A5 de M02.03–04 estaba previamente acreditado por J20 y el A5 C00-03.M01.06 por Javier distinto. Ninguna revisión de **predecesor** autoaprueba el A5 de este documento. El paso posterior M03 exige su propia autorización, CI y evidencia real.

## 2. Matriz de salida B01–B16

| Gate | Condición para aceptar una transición | Si falta evidencia |
| --- | --- | --- |
| B01 | Identidad Javier Task permitida y rama `dev` con Skill/HEAD canónicos | NO_GO al claim |
| B02 | Universo 2.340 IDs en fuente V5 versionada, sin mezclar backlog archivado | Abrir reconciliación, no completar por suposición |
| B03 | PLAN H/I de cada predecesor y owner real por ID exacto | No usar texto del prompt como estado |
| B04 | DAG F=VALIDADO/G=SI y COLA G/O=SI calculados, no forzados | No liberar M02.06 ni M03 |
| B05 | A5 independiente de C15-01.M01.01–06 y C00-03.M01.06 | Mantener bloqueo del terminal |
| B06 | A5 independiente de M02.01–05 con diff y SHA original | Mantener gate NO aunque LISTO exista |
| B07 | Claim de owner único, ausencia de escritor rival, readback | No sobrescribir una Task activa |
| B08 | BITACORA append-only A:Z, idempotencia `EVENT_ID` y key única | Rectificar solo con nuevo evento |
| B09 | Git HEAD y parent, diff y blobs exactos; fast-forward esperado | Reconstruir en HEAD nuevo, sin force-push |
| B10 | Scope Lock y procedencia V5 completed/success exact-SHA | VALIDANDO hasta reparar |
| B11 | Si se cambia código, tests .NET/Angular/MySQL/seguridad dirigidos reales | CI en progreso/cancelada no satisface PASS |
| B12 | JWT/RBAC/tenancy A/B no degradados y sin secretos/PII | Detener superficie afectada |
| B13 | No afirmar cero P0/P1, huérfanos, duplicados o carreras sin auditoría completa | Mantener `NO_CERTIFICADO` |
| B14 | Telemetría propia es autorreporte, no lease ni run nativo | Validar `Automations.peek` según caso |
| B15 | A5 del terminal por distinta Task Javier sobre su SHA | A5_PENDIENTE, no despliegue |
| B16 | M03 implementa código real con su DAG/CI y autorización específica | Documento M02 no habilita runtime |

**Resultado de este contrato:** B01, B03–B10 pueden verificarse documentalmente con las fuentes institucionales de la corrida cuando apliquen. B02, B11, B13 y pruebas de plataforma no quedan certificados por un artefacto Markdown. La evaluación de cobertura final exige numerador/denominador exactos, análisis semántico de duplicados y retest de hallazgos.

## 3. Plan de recuperación y rollback RB01–RB10

| Escenario | Recuperación dirigida sin falsificar estado |
| --- | --- |
| RB01 — Lock FAIL | Mantener scope VALIDANDO, leer logs y arreglar causa mínima sin bypass |
| RB02 — Carrera Git | Releer HEAD, reconstruir cambios, publicar por fast-forward expected SHA |
| RB03 — Owner disputado | Abort claim/relevo si otra corrida escribe o no existe CAS efectivo |
| RB04 — CI pendiente | VALIDANDO en ese ID, otro desarrollo independiente si es apto |
| RB05 — Check cancelado | No PASS; clasificar causalidad y equivalencia documental solo probada |
| RB06 — A5 propio | Rechazo; preservar autor y solicitar revisión de otra Task Javier |
| RB07 — DAG inconsistente | Revisar fórmulas/predecesores, jamás cambiar G/O manualmente |
| RB08 — Hallazgo de seguridad | Bloquear exposición, registrar reproducción saneada y test negativo |
| RB09 — Error de evento | Conciliar dedupe y añadir compensación, nunca borrar BITACORA |
| RB10 — Documento defectuoso | `git revert` normal, comprobaciones GitHub exact-SHA y evento aditivo |

Cualquier remedio que necesite proveedor externo, permisos de producción o migración no se ejecuta dentro de una corrección documental; se conserva el diagnóstico real y se escala solo autorización humana imprescindible.

## 4. Casos de aceptación futura AT01–AT12 (positivos y negativos NO_EJECUTADOS)

| ID | Esperado positivo futuro | Contraprueba obligatoria |
| --- | --- | --- |
| AT01 | Requisito mapea fuente y microtarea exacta con evidencia | Requisito huérfano se da por cubierto |
| AT02 | Duplicado semántico tiene clasificación y dueño | Dos requisitos críticos se cuentan como uno sin justificación |
| AT03 | Mapa de 2340 IDs conserva cardinalidad y trazas | ID inexistente o cambiado se acepta |
| AT04 | Gate DAG surge de LISTO+A5 completos | VALIDANDO o A5_FAIL libera técnico |
| AT05 | Owner exclusivo responde a cambio condicional | Dos Tasks obtienen mismo ID en carrera |
| AT06 | CI completed/success por SHA pertinente acredita paso | Cancelled/skipped se declara PASS |
| AT07 | A5 distinto revisa código y artefacto original | Autor firma su propia aprobación |
| AT08 | JWT/RBAC y tenant A/B preservan acceso restringido | Header A revela evidencia B |
| AT09 | Evento se deduplica por key/payload y contexto | Mismo evento con payload distinto sobrescribe |
| AT10 | Telemetría distingue run nativo y clave local | Reporte AA aparece como estado en tiempo real |
| AT11 | Rollback normal conserva commits de otros | Force-push o borrar BITACORA suplanta recuperación |
| AT12 | Defectos P0/P1 sin retest siguen abiertos y bloquean release | Documento concluye cero fallos o promueve PROD |

**AT01–AT12 son FUTUROS_NO_EJECUTADOS**. No hay nuevo reporte de auditoría completa, pruebas Playwright, HTTP/EF/MySQL, concurrencia bajo carga, simulación de incidentes, pentest ni certificación de cobertura. `NO_EVALUADO` nunca es `PASS`.

## 5. Auditorías de este cambioset, continuidad y reversión

**A0:** revisar alcance limitado a dos Markdown, los antecedentes de M01/M02/C00-03, B01–B16, RB01–RB10, AT01–AT12 y sección CHANGELOG autocontenida sin secretos. **A1:** escenarios positivos/negativos como diseño reproducible, nunca ejecución efectiva. **A2:** preservación conceptual de JWT, RBAC, tenant A/B, PII y denial by default, sin prueba runtime. **A3:** PLAN/COLA/DAG/BITACORA y fuentes Git verificadas, sin declarar ejecutados .NET/Angular/MySQL/Playwright por cambio documental. **A4:** publicación de documento y CHANGELOG a `dev` por fast-forward/expected HEAD; comprobar SHA, diff y blobs, CI Scope Lock y V5 completados/success en SHA publicado, readbacks institucionales. **A5:** obligatorio un revisor distinto de PRIMARY DEV 04; hasta entonces `A5_PENDIENTE` aunque PLAN diga LISTO.

El gate para `C15-01.M03.*` se vuelve a calcular desde el DAG actual; algunos sucesores además dependen de cierres de C00-03 no certificados. No existe habilitación de QA/main/PROD, eventos, secretos, APIs ni migraciones. **Rollback:** `git revert` ordinario del commit documental, CI y registro compensatorio append-only; no force-push, no reset, no edición del histórico.
