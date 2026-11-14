# SOLQARYN V5 FINAL — C15-01.M01.02: actores, entradas, salidas y fronteras de auditoría de cobertura

Fecha: 2026-11-14. Autor: PRIMARY DEV 04 JAVIER (TASK_ID 6aeacb7820ac8191b7d768a6f33dfa09). Ámbito: solqaryn/Solqaryn@dev. PROJECT_SCOPE_LOCK=STRICT. Preparación documental M01.02, no auditoría ejecutada.

Fuentes únicas: [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), AGENTS.md, docs/PROJECT_SCOPE_LOCK.md, docs/AUTOMATION_STATUS.md, docs/AUTOMATION_AUTHORITY.md, references/architecture.md y [M01.01](./c15-01-m01-01-alcance-auditoria-cobertura-trazabilidad.md). PLAN fila 2163, COLA G/O SI y DAG F VALIDADO/G SI acreditan sólo el preflight para este contrato. Cero huérfanos o duplicados críticos NO_CERTIFICADO.

## 1. Objetivo y contrato de interacción

Definir actores y canales de una futura auditoría que reconcilie requisitos del estándar V5 con los 2340 IDs del plan, fases, microfases, estados, dependencias y evidencia. La cadena será requisito → ID exacto → PLAN/COLA/DAG → commit y pruebas → Task autora/revisora → BITACORA → hallazgos y corrección causal. Cada unión exige claves estables y revisiones explícitas de fuente. Las lecturas separadas de Git y Sheets no son una transacción; si HEAD u owner cambian, refrescar antes de cualquier decisión. La existencia de documentos, tests o workflow no certifica ejecución.

No crear Tasks, cuentas, reservadores, webhooks, eventos, scripts ni nuevas filas. No acceder a QA/main/PROD, DNS, Hostinger, R2, Cloudflare, datos productivos, secretos o migraciones. El allowlist para A5 abarca sólo seis Tasks Javier. Los roles siguientes son conceptuales, no evidencia de cuentas físicas ni privilegios reales.

## 2. Catorce actores (A01–A14)

| ID | Rol | Responsabilidad / límite |
| --- | --- | --- |
| A01 | Propietario | autoriza permisos externos y decisiones |
| A02 | Task autora | entrega A0-A4, sin auto-A5 |
| A03 | Task revisora Javier | A5 con Task_ID distinto |
| A04 | Auditor | matriz de cobertura y discrepancias |
| A05 | Custodio V5 | revisión y hash de fuente |
| A06 | Custodio PLAN | H estado I owner |
| A07 | Custodio COLA | G/O son sólo prefiltro |
| A08 | Custodio DAG | F/G y precedencias reales |
| A09 | Custodio BITACORA | idempotencia y 26 columnas |
| A10 | Integrador GitHub | SHA/tree/diff/checks causales |
| A11 | Especialista seguridad | JWT RBAC tenant A/B |
| A12 | Responsable QA | no promoción por dev |
| A13 | Usuario de negocio | aceptación observable sin duplicación |
| A14 | Task par Javier | no reclama scope activo |

## 3. Doce entradas requeridas (IN01–IN12)

| ID | Fuente | Verificación exigida |
| --- | --- | --- |
| IN01 | Documento V5 | versión/hash vigente |
| IN02 | PLAN A:O | IDs únicos, H/I/G/evidencia |
| IN03 | COLA A:Q | join exacto ID y fórmula G/O |
| IN04 | DAG A:J | predecesores, F VALIDADO/G SI |
| IN05 | BITACORA A:Z | readback e idempotencia |
| IN06 | GitHub dev | HEAD, tree, parent, diff |
| IN07 | CI de commits | completed/success exact SHA requerido |
| IN08 | Evidencia técnica | A0-A4 causal por componente |
| IN09 | Autor y revisor | Task_ID distintos |
| IN10 | Mapa de bloqueos | dependencias actuales |
| IN11 | Seis Tasks Javier | peek nativo/estado |
| IN12 | Correcciones | causa, rollback y nueva auditoría |

Las entradas deben usar datos sintéticos o valores redactados, no copiar JWT, credenciales, PII ni datos entre tenants. Los checks que sean obligatorios sólo son PASS cuando status=completed, conclusion=success y head_sha exacto. queued, in_progress, skipped, cancelled o failed NO son PASS. NO_APLICA necesita causa documentada y no es PASS.

## 4. Doce salidas futuras (OUT01–OUT12)

| ID | Entregable futuro | Evidencia causal |
| --- | --- | --- |
| OUT01 | Matriz requisito↔ID | denominador completo |
| OUT02 | Censo PLAN/COLA/DAG | diferencias por clave |
| OUT03 | Huérfanos/duplicados | referencia y clasificación |
| OUT04 | Dependencias | ciclos, ausentes y estado gate |
| OUT05 | Estados/owner | coincidencia BITACORA |
| OUT06 | Artefactos Git | rutas, SHA, blobs y diffs |
| OUT07 | CI y PN | pruebas pertinentes del SHA |
| OUT08 | Seguridad | JWT/RBAC/tenant A/B negativos |
| OUT09 | A5 | revisora distinta, SHA original |
| OUT10 | Hallazgos | confirmados versus no acreditados |
| OUT11 | Corrección | retest y reversión |
| OUT12 | Acta final | riesgos y permisos explícitos |

OUT01–OUT12 son FUTUROS_NO_PRODUCIDOS. No se ha ejecutado en este documento el censo íntegro del estándar, ni se ha demostrado cero requisitos huérfanos/duplicados críticos. Un informe real debe exponer universo, denominador, elementos no leídos, tolerancias, errores y causa de cada excepción, con SHA y fecha.

## 5. Diez fronteras (B01–B10)

| ID | Frontera | Negativo que bloquea |
| --- | --- | --- |
| B01 | V5 → PLAN | requisito archivado aparece válido |
| B02 | PLAN → COLA | fila sustituye join de ID |
| B03 | COLA → DAG | O SI libera predecesor NO |
| B04 | DAG → código | ciclo habilita M03 runtime |
| B05 | Task → owner | escritura concurrente sobre ID |
| B06 | SHA → CI | skipped/cancelled se cuenta PASS |
| B07 | Autor → A5 | autor se autoaprueba |
| B08 | Tenant → API | usuario A lee datos de B |
| B09 | BITACORA → informe | histórico falso se usa como autoridad |
| B10 | LISTO DEV → PROD | promoción sin permiso |

La autorización real JWT/RBAC/tenant A/B corresponde al backend ASP.NET Core, no a un guard Angular o una fila de Sheets. Nunca se retiran gates A5_FAIL sin corrección, test dirigido y revisión independiente. BITACORA es append-only y los eventos históricos no dan autoridad operativa.

## 6. Diez pares futuros PN01–PN10, NO_EJECUTADOS

| Caso | Positivo observable | Negativo bloqueante |
| --- | --- | --- |
| PN01 | Requisitos V5 mapeados completos | huérfano se declara cubierto |
| PN02 | IDs exactos reconciliados | duplicado se omite |
| PN03 | Predecesores reales validados | DAG SI sobre dependencia NO |
| PN04 | Estado/owner causal readback | doble reclamación admitida |
| PN05 | CI completed/success SHA exacto | cancelled/skipped se declara PASS |
| PN06 | A5 por Task distinta | autorrevisión permitida |
| PN07 | JWT/RBAC tenant A/B causal | documento reemplaza test HTTP |
| PN08 | Hallazgo con prueba reproducible | sospecha se convierte en P0 |
| PN09 | Corrección y reauditoría causal | hallazgo cerrado sin retest |
| PN10 | Entorno con permiso explícito | dev LISTO despliega QA/PROD |

PN01–PN10 = FUTUROS_NO_EJECUTADOS. Esta unidad no prueba HTTP, MySQL, Playwright, integraciones vivas, seguridad funcional ni las seis automatizaciones por sí misma. Un caso de riesgo no acreditado NO es vulnerabilidad demostrada. El censo exhaustivo de los 2340 IDs queda para la auditoría autorizada de las etapas correspondientes.

## 7. Evidencia, impacto, A0–A5 y reversión

Cada prueba futura exigirá CASE_ID, requisito e ID de microtarea, revisión del estándar y Sheet, HEAD exacto, rol/tenant sintético, esperado y observado, job/status/conclusion/head_sha, fecha UTC, log sin secretos, autor/revisor distintos, incidencia y rollback. Una corrección requiere repetir la auditoría afectada.

Impacto de este cambio: sólo este Markdown y una sección autocontenida CHANGELOG_AI.md. No modifica API, DTO, Domain, Application, Infrastructure, Angular 22.2.1, .NET 10, MySQL/EF, JWT/RBAC, tenant, CI, cuentas o infraestructura. Consumidores: M01.03–05, M01.06 sólo con DAG y A5.

- A0: catorce actores, doce IN, doce OUT, diez fronteras, diez pares PN y encabezados sin intercalado.
- A1: oráculos documentales futuros y negativos falsables; no se atribuye PASS runtime.
- A2: se preservan límites JWT/RBAC/tenant A/B, privacidad y secretos al no tocar código.
- A3: cotejo de estándar, M01.01 y arquitectura; builds .NET/Angular, MySQL y Playwright NO_APLICA para sólo dos Markdown.
- A4: fast-forward/CAS directo dev, Scope Lock y V5 provenance completed/success SHA exacto, readback PLAN/COLA/BITACORA.
- A5: independiente por Task Javier distinta, sin autorización QA/main/PROD.

Rollback: git revert normal de este changeset documental después de releer HEAD; nunca force-push, reset, reescritura de BITACORA o cambios productivos.
