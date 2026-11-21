# C06-01.M06.05 — Solicitud verificable de revisión A5 independiente del lote

**Autoridad:** SOLQARYN V5 FINAL, PLAN_MAESTRO!D900 «Solicitar revisión independiente del lote cuando sea un gate». PLAN!E900 expresa una meta acumulativa de M06, no una autorización para declarar M06 terminado. Repositorio exclusivo solqaryn/Solqaryn, rama dev, PROJECT_SCOPE_LOCK=STRICT. Esta entrega R0 **crea una solicitud auditable**, no realiza ni suplanta ninguna aprobación A5. El único cambio de Git de esta microtarea es documental; sin runtime, datos, auth, API, UI, migraciones, tareas físicas ni despliegues.

## Destinatarios y alcance de la solicitud

Se solicita **revisión técnica genuina e independiente** a cualquiera de las seis Tasks Javier autorizadas cuyo TASK_ID sea diferente del **autor y reparador** de cada entrega. El revisor no se preasigna ni se simula que esté activo; debe verificar con Automations.peek y namedRange V17 que no exista escritor actual para ese ID, leer el diff de GitHub original, las pruebas de CI requeridas y PLAN/BITACORA, adquirir candado propio y registrar decisión A5_PASS o A5_FAIL idempotente con readback. El autor no puede autoaprobarse ni borrar FAIL histórico. No se crean PR, ramas, issues, Tasks ni aprobaciones humanas.

### Matriz de la etapa C06-01.M06 (estado al solicitar)

| ID | PLAN H / autor histórico | SHA de entrega | Auditoría independiente requerida |
| --- | --- | --- | --- |
| C06-01.M06.01 | LISTO / DEV04 | ae236880f2b22770f728845fe349b13021562870 | A5_PENDIENTE: matriz de pruebas reales frente a fixtures/mocks; no certificar landing completa |
| C06-01.M06.02 | LISTO / DEV04 | 8673f483cb7100f91ec3abd6c9cb743938afa81d | A5_PENDIENTE: manifiesto SHA/diff/artefactos reproducibles; verificar referencias y límites |
| C06-01.M06.03 | LISTO / DEV04 | 91845b3a8d08411ab32e0aecd45f4ce91a4e0e7d | A5_PASS: DEV05, revisión real independiente por TASK_ID distinto y evento en BITACORA |
| C06-01.M06.04 | LISTO / DEV05 | a78f87fc51afe3ca5204abf97651f34e53f15a01 | A5_PENDIENTE: reproducir Git revert **real** aislado de 5 archivos, comprobar 5 negativos de refs, no afectación de datos; DEV05 no puede revisar |
| C06-01.M06.05 | EN_PROGRESO / DEV05 al momento de esta solicitud | Será SHA del commit documental de esta solicitud | A5_PENDIENTE: otro Javier verifica que la solicitud no se contabilice como PASS o gate liberado |
| C06-01.M06.06 | PENDIENTE / sin autor | Sin SHA de entrega | DAG gate=NO mientras falte alguna A5 real de .01–.05, además de predecesores técnicos; no reclamar ni forzar fórmula |

**Señal de revisión:** este documento y los eventos de BITACORA de C06-01.M06.05 son el handoff solicitado. La petición **no** equivale a la ejecución de ninguna A5. Verificar la matriz contra PLAN_MAESTRO H/I/K y DAG F/G antes de cambiar cualquier estado; si otra Task ya actuó, respetar sus eventos y actualizar diagnóstico en vez de duplicar eventos.

## Evidencia concreta para los revisores

- M06.01: [diff/auditoría](https://github.com/solqaryn/Solqaryn/commit/ae236880f2b22770f728845fe349b13021562870); distingue browser Chromium real de fixture HTTP local y conserva riesgos no comprobados.
- M06.02: [manifiesto documental](https://github.com/solqaryn/Solqaryn/commit/8673f483cb7100f91ec3abd6c9cb743938afa81d); comprobar cada SHA/URL de pruebas y límites.
- M06.03: [documento original](https://github.com/solqaryn/Solqaryn/commit/91845b3a8d08411ab32e0aecd45f4ce91a4e0e7d); A5 independiente por DEV05, evento SQV23-J40-103C8276-C0601M0603-A5PASS-91845B3A-20261121-R11.
- M06.04: [código real del ensayo de reversión](https://github.com/solqaryn/Solqaryn/commit/a78f87fc51afe3ca5204abf97651f34e53f15a01) y [GitHub-hosted 137012089670](https://github.com/solqaryn/Solqaryn/actions/runs/44958581146/job/137012089670), completed/success: Git real, detached worktree, revert --no-commit del commit auténtico 9b90aa0d, cinco rutas revertidas, cinco entradas adversariales denegadas, SHA-256 de datos sintéticos preservado, 0 escrituras remoto/DB/PROD. El fixture sintético **no** es prueba de rollback de MySQL de clientes.
- M05.01–.06: PLAN H=LISTO y K=A5 genuino registrado; sus verificaciones son independientes y **no** certifican automáticamente M06, JWT tenant A/B de backend, SEO/dominios live o UX premium global.

## Riesgos abiertos que deben sobrevivir a la revisión

**AUDIT_UI_BRECHAS/UI-03 P1** permanece PARCIAL: la raíz pública aún redirige a /login; la ruta /conoce-solqaryn y su CTA no prueban homepage pública, pricing, onboarding, legales ni SEO. Capturas de Playwright/Chromium en cinco anchuras se limitan a laboratorio; no prueban dispositivo físico, WCAG completa ni Core Web Vitals de campo. Los tests Angular usan HTTP local sintético en varios casos; la API .NET/Oracle EF/MySQL 8.4 real fue comprobada únicamente en pruebas pertinentes y aisladas de M05.02/.03, sin otorgar certificación JWT/RBAC firmada de recursos privados, Aiven live, datos comerciales o despliegue. No cerrar estas brechas fuera de su alcance ni fingir P0/P1=0.

## Calidad y autorización

**A0:** revisar diff exclusivamente documental, código/CI fuente y la matriz frente a PLAN D900/P:S. **A1:** comprobar de manera adversarial que no se declara A5 artificial, que autor≠revisor en cada fila y que M06.06 continúa gate=NO mientras falte A5, y referencias SHA reales. **A2:** NO_APLICA a nuevas pruebas JWT/RBAC/tenant por diff sin runtime; no inferir autorización. **A3:** NO_APLICA a nuevas integraciones HTTP/MySQL/Chromium por diff sin runtime; citar evidencia auténtica anterior sólo con alcance limitado. **A4:** comprobar Scope Lock exact-SHA del commit documental, V5/procedencia pertinente y readback PLAN/COLA/DAG/BITACORA; SKIPPED/IN_PROGRESS nunca reemplazan PASS requerido. **A5 de esta solicitud:** revisión independiente por Task Javier **distinta de DEV05**.

**V23:** referencia SKREF-06 Humanizer leída del origen canónico blader/humanizer@9862685f575c65a8247f90369951df1b3416e3d6, ruta SKILL.md, aplicada para claridad sin inventar resultados. SKREF-03/05/10 **NO_APLICA** por ausencia de cambios UI/Angular; SKREF-04 sin motion, SKREF-11 sin modelo nuevo de amenazas, SKREF-12 sin perf medida. Ninguna referencia externa fue instalada. Si esta entrega documental falla, corregir bajo V17 y nuevo SHA, no modificar el backlog ajeno. Rollback por git revert normal del commit documental, preservando BITACORA e historia; nunca force-push. No QA/main/PROD.
