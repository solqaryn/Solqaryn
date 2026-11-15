# C00-04.M04.05 — Verificación real responsive, teclado, foco, labels y contraste

**Autor:** PRIMARY DEV 06 / JAVIER_CHATGPT, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Repositorio:** `solqaryn/Solqaryn@dev`. **Fuente:** Standard V5 FINAL, PLAN/COLA/DAG, Skill canónica y `PROJECT_SCOPE_LOCK=STRICT`.

## Alcance y fronteras

Se agrega una suite real Chromium/Playwright de cinco regresiones en `frontend/e2e/c00-04-m04-05-accessibility.spec.ts` y un job GitHub-hosted dirigido en `.github/workflows/v5-c00-04-m04-05-accessibility.yml`. Verifica la ruta privada ya creada `/plataforma/gobierno-cambios-v5` sin modificar el componente ni `app.routes.ts` de C00-04.M04.01, actualmente de otra Task. El render de Angular ocurre en loopback y las respuestas `/plataforma/sesion` se simulan exclusivamente para medir la experiencia: **no son pruebas de JWT, RBAC, MySQL o aislamiento real entre empresas**.

## Matriz de comprobaciones (CI requerida)

| ID | Evidencia positiva | Negativo/seguridad |
| --- | --- | --- |
| B01 | Componente de gobierno cargado bajo sesión sintética permitida | Anónimo se redirige al login antes de mostrar contenido privado |
| B02 | La suite observa HTTP 403 del mock de sesión | Denegación bloquea pantalla sin renderizar datos restringidos |
| B03 | Viewports 390px y 1280px mantienen cuatro pasos, lectura y ancho contenido | Overflow horizontal o pasos fuera de viewport producen FAIL |
| B04 | Enlace de retorno etiquetado admite foco y activación mediante Enter | No se consulta PLAN, COLA, BITACORA ni se generan acciones de aprobación |
| B05 | Contraste calculado del título >= 4.5:1 | Color CSS ilegible o no interpretable produce FAIL |

Los cinco casos son pruebas **a ejecutar por GitHub Actions del SHA publicado**. Antes de la ejecución son código de prueba, no PASS. El workflow incluye npm ci, lint, build Angular, Chromium local y test dirigido; se guardan trazas/HTML. No usa secretos, workers, dispatcher, producción, conexiones de pago ni un backend real.

## Seguridad, pruebas, salida y reversión

A0: examinar diff de cuatro archivos (spec, job, este documento, CHANGELOG), secretos y compatibilidad del Playwright configurado. A1: ejecutar cinco casos Chromium positivos/negativos. A2: comprobar que la autorización real permanece en plataformaGuard y backend, y que solo se simula la sesión para UI; probar redirección anónima y 403. A3: lint/build Angular, Playwright y ausencia de nuevas rutas/endpoint. A4: commit FF/CAS en dev, SHA, Scope Lock, V5 y job browser propio `completed/success`, leer PLAN/COLA/DAG/BITACORA; **si el job está queued/in_progress/failure, conservar VALIDANDO**. A5: otra Task Javier distinta de DEV06 revisa diff y evidencias; LISTO_DEV no autoriza QA/main/PROD.

Rollback: git revert normal del commit preservando trabajo ajeno; evento institucional aditivo si corresponde. Los cinco casos no certifican accesibilidad universal ni cumplimiento WCAG completo; son una regresión causal de esta superficie.
