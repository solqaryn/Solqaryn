# SOLQARYN V5 — C13-04.M02.06: dependencias, gates y salida de experiencia visual

**Autor:** PRIMARY DEV 05 — JAVIER_CHATGPT (`6adc2e55f47c81919f6374e561687076`). **Fecha:** 2026-11-15. **Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Naturaleza:** documento M02 de contrato técnico, no implementación funcional.

## 1. Autoridad y alcance

La autoridad ejecutable es STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, y las pestañas PLAN_MAESTRO, COLA, DAG_TECNICO y BITACORA del Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. Se aplican la Skill canónica, `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md` y `docs/AUTOMATION_STATUS.md` vigentes.

La tarea define el handoff de contratos de experiencia visual, estados loading/empty/error/success, navegación, responsive, accesibilidad, temas tenant y evidencias futuras. **No** crea ni activa Angular/CSS, rutas, endpoints, DTO C#, JWT/RBAC, MySQL/EF, migraciones, telemetría, workers, webhooks, servicios ni QA/main/PROD. Los oráculos definidos abajo son FUTUROS_NO_EJECUTADOS: no acreditan WCAG, Chromium, HTTP, pruebas A/B runtime ni rendimiento medido.

## 2. Predecesores de la fórmula DAG y procedencia

Al reclamar la fila PLAN 1993: H=PENDIENTE, I vacío; COLA G/O=SI; DAG_TECNICO fila 1954 F=VALIDADO y G=SI. Se efectuó cambio condicional de estado, owner único con readback y BITACORA. No se modificaron fórmulas. Se revalidará la elegibilidad real antes de cualquier transición.

| ID | Fuente original y blob Git preservado | Requisito independiente |
| --- | --- | --- |
| M02.01 | [Entidades y ownership](./c13-04-m02-01-entidades-ownership-experiencia-visual.md), `578a88d57ca8db3d3d97de11059687a7e9c17c40` | 14 entidades e invariantes; A5 |
| M02.02 | [API/DTO y errores conceptuales](./c13-04-m02-02-contratos-api-dto-errores-experiencia-visual.md), `839352a81234eabc5f257e85301b3909fa528048` | 12 DTO/validaciones futuras; A5 |
| M02.03 | [Evidencia, índices y fixtures](./c13-04-m02-03-persistencia-indices-fixtures-experiencia-visual.md), `5ddfa422e375c32fd197ce0ce2ad290508754d16` | Sin MySQL; A5 |
| M02.04 | [Configuración tipada](./c13-04-m02-04-configuracion-tipada-ambitos-flags-experiencia-visual.md), `5dc42dbdf50486082524bf0102ac4008327353ed` | Fail-closed y sin flags activadas; A5 |
| M02.05 | [Eventos conceptuales](./c13-04-m02-05-eventos-idempotencia-correlacion-limites-experiencia-visual.md), `cb20f140f47252ba8a6e7883e64153df90077621` | Sin dispatcher; A5 |

El contrato preparatorio [M01.06](./c13-04-m01-06-bloqueos-rollback-liberacion-experiencia-unificada.md), blob `d097224cf4ef0ed17c6941b1338a6a4c1c218f55`, consolida M01.01–05. Los seis M01 y cinco M02 anteriores constan LISTO con A5 independiente en PLAN K. La fórmula del DAG, no este texto, determina el gate. En caso de nueva evidencia adversa, A5_FAIL, owner rival o cambio de SHA relevante, detener sucesoras hasta resolver la causa.

## 3. Dieciséis gates de aceptación y liberación

| Gate | Evidencia exigible | Comportamiento si falta |
| --- | --- | --- |
| G01 | Repo original, rama dev, Skill canónica y Scope Lock STRICT | No escribir ni certificar |
| G02 | PLAN H/I, COLA G/O, DAG F/G y BITACORA concordantes | No forzar SI ni reclamar |
| G03 | M01.01–06 y M02.01–05 LISTO y A5 de autor distinto | Mantener M02.06 bloqueada |
| G04 | Inventario ruta/pantalla/acción/rol/tenant/estado/viewport/SHA | No inferir cobertura UI |
| G05 | Autorización servidor JWT, membresía, RBAC y ownership real | UI y guard no conceden permisos |
| G06 | Tenant A/B y sucursal aislados con fixtures sintéticos | No cruzar caché, datos ni tema |
| G07 | Loading, empty, error, success y 403 con feedback textual recuperable | No declarar aceptación UX |
| G08 | Responsive 320/390/768/1280 conserva CTA, tablas y modales | No asumir reflow medido |
| G09 | Teclado, lector, foco, contraste y reduced motion con test posterior | No declarar WCAG |
| G10 | Flags y tokens de tema en allowlist, default seguro y revocación | No activar personalización |
| G11 | Evidencia futura con caseKey, rol, viewport, source SHA, artefacto depurado | No mezclar resultados SHA |
| G12 | Eventos documentales no lanzan Tasks, reintentos ni transiciones | No crear webhook ni dispatcher |
| G13 | CI causal por cambio: Angular/Vitest/Playwright, .NET/seguridad o MySQL aislado | Queued/cancelled/skipped/failed no PASS |
| G14 | A0–A4, diff documental, CHANGELOG, Scope Lock y V5 completed/success exact SHA | Seguir VALIDANDO |
| G15 | A5 real de Task Javier diferente de DEV05, con diff, blobs, pruebas y evento | No autorizar M03/QA/PROD |
| G16 | M03.01 sólo tras LISTO+A5 de M02.01–06 y DAG/PLAN/COLA SI; M03 exige código real | No sustituir código por documentos |

## 4. Matriz de oráculos positivos y negativos FUTUROS_NO_EJECUTADOS

| Caso | Positivo futuro | Negativo que debe impedir PASS |
| --- | --- | --- |
| P01/N01 | Formulario y login anuncian foco y error | Label ausente, foco perdido o mensaje sólo cromático |
| P02/N02 | Tenant A ve únicamente su marca y datos | Tema o caché A aparece en B |
| P03/N03 | Menú refleja permiso backend | Flag/guard cliente eleva RBAC |
| P04/N04 | Loading/empty/error/success recuperables | Spinner indefinido o fallo silencioso |
| P05/N05 | CTA visible en 320/390/768/1280 | Overflow tapa la acción esencial |
| P06/N06 | Modal permite Escape y retorno de foco | Trampa de teclado o foco perdido |
| P07/N07 | Reduced motion conserva información | Animación obligatoria para comprender |
| P08/N08 | Error 403/404 no filtra datos sensibles | JWT, PII o traza expuesta |
| P09/N09 | CI completed/success con evidencia exact SHA | Cancelled/skipped/no-run declarado PASS |
| P10/N10 | Evento futuro idempotente y dentro de cuota | Duplicado altera resultado o tareas |
| P11/N11 | DTO versionado conserva consumidor | Cambio incompatible sin pruebas contractuales |
| P12/N12 | Sesión A→B invalida contexto dependiente | Resultado o sucursal previa permanece |

La ejecución posterior requiere pruebas reales en GitHub-hosted: build/test de componentes Angular, browser Playwright/Chromium con viewport y teclado; API .NET si se tocan permisos; MySQL efímero si se tocan migraciones. Guardar escenarios positivos/negativos, datos A/B sintéticos, cambios, SHA, logs y regresiones. Aquí se ha definido la expectativa, no ejecutado esos tests.

## 5. Recuperación, rollback y cierre

| Riesgo | Respuesta mínima |
| --- | --- |
| RB01 HEAD avanza | Releer HEAD y CHANGELOG, reconstruir changeset, CAS fast-forward, nunca force-push |
| RB02 falla Scope Lock/V5 | Diagnóstico de logs y reparación mínima, sin desactivar control |
| RB03 CI pendiente/cancelada | Preservar VALIDANDO, owner y evidencia; no inventar PASS |
| RB04 A5 faltante o FAIL | No liberar M03 ni cambiar fórmulas; revisión distinta y causal |
| RB05 falta aislamiento o accesibilidad | Mantener negativos pendientes, no certificar UI |
| RB06 evidencia con PII/secretos | Redactar y bloquear, probar la falla de manera sintética |
| RB07 concurrencia o evento repetido | CAS/readback y compensación aditiva, nunca alterar BITACORA histórica |
| RB08 documento erróneo | Git revert normal con nuevo SHA y verificación posterior |

**A0:** análisis de alcance, archivos y precedentes. **A1:** revisión de doce pares P/N documentados, no ejecutados. **A2:** revisión estática de tenant A/B, JWT/RBAC, privacidad y denegación. **A3:** trazabilidad de M01/M02, NO_APLICA a HTTP, DB y browser por diff sólo Markdown. **A4:** publicar fast-forward/CAS y verificar Scope Lock/V5 en SHA exacto, PLAN/COLA/BITACORA y diff. **A5:** queda PENDIENTE de otra Task Javier después de LISTO_DEV. Ninguna transición habilita QA/main/PROD.
