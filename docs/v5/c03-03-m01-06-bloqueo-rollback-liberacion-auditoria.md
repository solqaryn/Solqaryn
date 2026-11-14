# SOLQARYN V5 FINAL — C03-03.M01.06: bloqueo, rollback y liberación documental de auditoría de seguridad

**Ámbito:** solqaryn/Solqaryn, rama dev, PROJECT_SCOPE_LOCK=STRICT. **Autor:** PRIMARY DEV 03 — JAVIER_CHATGPT, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Fecha:** 2026-11-14. **Entrega:** contrato documental de salida de la preparación M01 para C03-03. No implementa auditoría HTTP, DB, retención, IAM, exports, accesos excepcionales o migraciones.

**Autoridades:** [Standard V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), Skill canónica y Scope Lock de dev. El resultado futuro de C03-03 es auditar autorizaciones, denegaciones, cambios críticos y accesos excepcionales con trazabilidad, correlación segura, retención autorizada y datos minimizados. **Este documento solo define condiciones, no acredita esas capacidades.**

## Prerrequisitos comprobados antes del claim

El preflight institucional observó `PLAN_MAESTRO!H511=PENDIENTE, I511=vacío`, `COLA!G511/O511=SI/SI`, y `DAG_TECNICO!F477/G477=VALIDADO/SI`. Las cinco M01.01–M01.05 del propio lote estaban en LISTO y con revisión A5 independiente Javier; `C02-04.M01.06` (PLAN fila 403) también estaba LISTO con A5 independiente J30 sobre el commit `dd67dc4acd45b3fca70777f5cb3d0719bce256fe`. Esto autoriza **solo el reclamo y la definición documental M01.06**, no el desbloqueo automático de M02.

| Entrada causal (archivo en dev) | Blob verificado | Condición de salida que conserva |
| --- | --- | --- |
| [M01.01 objetivo](c03-03-m01-01-objetivo-no-regresion-auditoria.md) | `59a1e138fccf59c674bca3f6cc45ed1733ffa5bc` | No registrar secretos ni confundir correlación con autorización |
| [M01.02 interfaces](c03-03-m01-02-actores-entradas-salidas-auditoria.md) | `0b67a6b49d6612be4cef2f9251c481fd8953323a` | Separación de actores A/B, administrador empresarial y plataforma |
| [M01.03 inventario](c03-03-m01-03-inventario-auditoria-seguridad.md) | `807eab0b4d6aa1a71beeb7d9df82b40e23c06969` | Hechos de código y brechas sin afirmar una fuga no probada |
| [M01.04 riesgos](c03-03-m01-04-riesgos-supuestos-decisiones-auditoria.md) | `16d53b6311163dd8c071bb5a92f887cec03d0bd3` | R01–R14 sin eliminación administrativa de riesgos pendientes |
| [M01.05 aceptación](c03-03-m01-05-criterios-auditoria-seguridad.md) | `b8ad87e4164135ccbbe1a84e180e55bc07fbecb2` | AU01–AU16: oráculos falsables futuros, NO tests HTTP/MySQL realizados |

Estos identificadores son SHA de **blobs**, no SHA de CI. A5 documental no sustituye pruebas runtime futuras.

## Puertas de bloqueo (fail-closed) y autoridad

| Gate | Precondición | NO_GO y evidencia requerida |
| --- | --- | --- |
| G01 — identidad y exclusividad | Solo una de seis Tasks Javier, PLAN H=PENDIENTE, I vacío, CAS y readback | Otro owner, estado distinto o lease concurrente: no escribir el mismo ID |
| G02 — dependencia individual | DAG F=VALIDADO/G=SI, COLA G/O=SI y todas las M01.01–05 y C02-04.M01.06 LISTO con A5 causal | Cualquier A5 pendiente/fallida o precedente no LISTO: no cerrar |
| G03 — publicación | Diff únicamente del alcance documental, HEAD de dev releído y commit fast-forward con CAS | Cambio de HEAD: rebase lógico sin force-push o detener solo este ID |
| G04 — calidad A0–A4 | A0 estática, A1 oráculos P/N, A2 límites de seguridad, A3 consistencia documental/DAG, A4 Scope Lock y regresión V5 SUCCESS del SHA publicado | Job obligatorio en queued/in_progress/failed/skipped: conservar VALIDANDO |
| G05 — revisión A5 | Revisor de otra Task Javier, distinta del autor, con evidencia del SHA y BITACORA 26 columnas | Autoevaluación prohibida; LISTO_DEV no es permiso QA/main/PROD |
| G06 — seguridad real posterior | Para certificar auditoría, tests HTTP + MySQL/EF efímero, A/B y errores transaccionales | Preparación M01, mocks o descripción de escenarios no prueban aislamiento real |
| G07 — privacidad y controles | Política de retención, mínima exposición, permiso y tenant resueltos en backend | Sin política aprobada o con secretos/token/PII accidental: NO_GO para implementación |

La consulta existente `GET /auditoria` tiene protecciones declaradas, pero `AuditoriaRepository.GetFilteredAsync` no exhibe por sí solo un predicado explícito por empresa; pueden existir filtros EF. Esto **obliga a probar el conjunto completo**, no a declarar una filtración ni a darla por resuelta. `RegistrarAsync` es best-effort y `RegistrarEstrictoAsync` propaga fallos: no confundir ambos con durabilidad transaccional universal. `X-Correlation-ID` es una señal saneada, nunca grant ni llave de idempotencia.

## Oráculos positivos y negativos de este contrato (documentales, no runtime)

| Caso | Signo | Condición y resultado esperado |
| --- | --- | --- |
| P01 | + | M01.01–05 y C02-04.M01.06 LISTO/A5, DAG y COLA SI, owner vacío: CLAIM M01.06 permitido una vez |
| P02 | + | Documento contiene puertas, riesgos, rollback y negativa de secret leakage: VALIDANDO verificable antes de A1–A3 |
| P03 | + | Git dev publica por fast-forward, Scope Lock y V5 SUCCESS exact-SHA, PLAN/COLA y BITACORA readback: LISTO_DEV documental posible |
| P04 | + | Revisor independiente distinto del autor evalúa SHA, fuentes y pruebas: A5 separado y sin promoción |
| N01 | - | Antecesor M01 pendiente, VALIDANDO o A5 causal ausente: M01.06 NO_GO |
| N02 | - | COLA O=SI sin DAG F VALIDADO/G SI: no reclamar |
| N03 | - | PLAN I ocupado o dos escritores: no transferir ni duplicar evento |
| N04 | - | Scope Lock FAILURE o check pertinente CANCELLED/SKIPPED/IN_PROGRESS: no PASS |
| N05 | - | Revisión A5 por el propio autor o fuera de seis Javier: rechazar |
| N06 | - | Header, filtro o correlation ID del cliente pretende ampliar tenant o RBAC: denegar |
| N07 | - | Consulta de auditor A devuelve evento/usuario/PII de B: falla futura A2 HTTP+MySQL, no aprobar |
| N08 | - | Fallo al guardar auditoría estricta que permite un cambio crítico sin rollback: falla futura A3 |
| N09 | - | Contraseña, JWT, token de recuperación o PII en old/new, motivos, excepción o export: falla futura A2 |
| N10 | - | Reintento con misma clave produce evento crítico duplicado: falla futura idempotencia |
| N11 | - | Retención sin política legal y alcance aprobado, purga destructiva o export irrestricto: NO_GO |
| N12 | - | LISTO documental usado para activar M02, MFA, IAM, JWT, DB, worker, webhook, billing o QA/PROD: prohibido |

P01–P04 y N01–N12 son criterios lógicos cotejables contra documentos/estado actual; **no se ejecutaron pruebas HTTP, EF, MySQL, Chromium ni retención real**. Las futuras pruebas AU01–AU16 conservan su estado no certificado.

## Rollback y condición de salida

El rollback del documento es `git revert <SHA>` normal y una transición compensatoria institucional con nuevo EVENT_ID y readback; jamás borrar BITACORA histórica, reescribir Git, desactivar Scope Lock ni revertir ejecuciones de otros owners. Nunca modificar producción.

A0: comprobar fuentes, encabezado/viñetas contiguos de CHANGELOG, alcance y ausencia de secretos. A1: validar P01–P04, N01–N12 como matriz estructurada (no prometer runtime). A2: revisar A/B, permisos, errores y minimización sin modificar código. A3: verificar identidad de seis fuentes, PLAN/COLA/DAG y dependencias en el preflight. A4: comprobar commit publicado, Scope Lock y V5 exact-SHA, readback institucional y evento A5_PENDIENTE. Para este documento `dotnet/npm/MySQL/Playwright=NO_APLICA` solo por no haber runtime alterado, NO por considerar aprobadas sus pruebas.

La salida M01.06 **no cambia ningún estado M02**. Cada M02 exige nuevos preflight, autoría exclusiva y CI correspondiente. A5 queda PENDIENTE hasta revisión independiente de otra Task Javier.
