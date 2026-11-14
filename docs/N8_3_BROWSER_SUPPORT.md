# N8.3 — Contrato de navegadores soportados

Autoridad operativa: `docs/VAEP_AUTHORITY.md`.

## Contrato

Solqaryn `Desarrollo` usa Angular `20.3.27`. Mientras no exista una excepción explícita y versionada en este documento, el frontend soporta el conjunto de navegadores que Angular 20 declara compatible mediante su política Baseline. Para Angular 20, la fecha Baseline publicada es `2025-04-30` y las familias principales son Chrome, Edge, Firefox y Safari.

Este contrato **no amplía** compatibilidad hacia navegadores legacy o versiones fuera de la ventana soportada por Angular 20. Tampoco promete capacidades específicas del shell/vendor que no formen parte de la plataforma web usada por Solqaryn.

## Evidencia automatizable

La cobertura causal de aplicación se ejecutará con Playwright sobre tres motores web:

- Chromium, como proxy de compatibilidad de la familia Chromium usada por Chrome y Edge.
- Firefox.
- WebKit, como proxy de compatibilidad del motor de Safari.

Los motores Playwright validan compatibilidad web de la aplicación; no sustituyen una certificación de integraciones propietarias específicas de una distribución concreta del navegador. Si una funcionalidad futura depende de una API exclusiva de Chrome, Edge o Safari, deberá añadir un gate vendor-specific antes de declararse soportada.

## Alcance técnico

N8.3 cubre renderizado, navegación, interacción y regresión E2E del frontend en navegadores soportados. No introduce cambios de dominio de negocio, backend, API, persistencia, migraciones, RBAC, tenant isolation, secretos, deploy ni Producción.

N8.2 conserva la autoridad sobre perfiles de dispositivo/responsive; N8.3 verifica el eje navegador/motor sin duplicar la matriz de dispositivos.

## Rollback

Si este contrato o la automatización N8.3 causa una regresión, revertir únicamente los archivos de contrato, pruebas/configuración Playwright y workflow causal de N8.3. No existe rollback de datos asociado porque este punto no modifica persistencia.

## Definition of Done del contrato

- El conjunto soportado queda explícito y no es más amplio que Angular 20.
- Existe estrategia causal Chromium + Firefox + WebKit.
- Cualquier incompatibilidad P0/P1 detectada se corrige antes del cierre de N8.3.
- El cierre final depende de REVIEW_FIRST, gate causal terminal y receipt `LISTO_REAL`; este documento por sí solo no certifica N8.3.
