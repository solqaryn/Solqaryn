# Modernización — Fase 5 Tooling frontend — 2026-10-04

## Alcance

- Repositorio: `solqaryn/Solqaryn`.
- Rama: `dev`.
- Entorno autorizado: DEV exclusivamente.
- QA, `main`, PROD, datos, secretos e infraestructura productiva: fuera de alcance y no modificados.

## Precondición

- Fase 4 Angular 22.2.1 certificada exact-head.
- Angular/TypeScript/RxJS/tslib/Zone.js permanecen sin cambios.

## Matriz aplicada

- Playwright Test: `1.63.0`.
- Vitest: `5.0.3`.
- jsdom: `30.0.1`.
- html5-qrcode: `2.3.8` (sin cambio).

Se aplica `jsdom@30.0.1` exactamente según el alcance autorizado de Fase 5; no se amplía la fase a releases posteriores sin autorización.

## Política html5-qrcode

`html5-qrcode@2.3.8` continúa siendo la última versión publicada y se mantiene sin cambios funcionales. Su antigüedad se registra como deuda técnica no bloqueante para evaluación futura de mantenimiento, alternativas, compatibilidad de navegador/cámara, seguridad y costo de migración.

## Gates pre-publicación

- scope SOLQARYN;
- reinstalación limpia con `npm ci`;
- matriz exacta de versiones;
- audit completo high+;
- audit productivo high+;
- 58/58 archivos + 217/217 unit tests;
- lint/contratos;
- build production-mode sin deploy.

## Cierre exact-head

El certifier permanente de Fase 5 exige aceptación Playwright integral DEV 100/100 sobre el mismo HEAD y emite `FASE_5_TOOLING_FRONTEND=PASS`.

## Hardening quirúrgico de aceptación exact-head

- El harness canónico DEV valida explícitamente antes de Chromium que `@playwright/test`, `playwright-core` y `npx playwright --version` resuelvan exactamente `1.63.0`.
- Esto elimina la dependencia implícita en el lockfile como única prueba de versión runtime y hace causal la evidencia de los 100 tests E2E.
- `jsdom@30.0.1` se mantiene por alcance exacto de Fase 5 aunque exista una release posterior; no se amplía el scope sin autorización.
- QA, `main` y PROD permanecen fuera de alcance.

## Limpieza de warnings propios detectados en Vitest 5

- La recertificación detectó warnings Angular de Reactive Forms provocados por `[disabled]` sobre controles con `formControlName` en filtros de reportes de compras.
- Se movió el estado disabled al `FormGroup` mediante `disable()/enable()` con `emitEvent:false`, preservando el contrato funcional y eliminando el warning de Angular.
- Se añade prueba dirigida para verificar que los inputs `disabled` y `cargandoSelectores` gobiernan el estado del formulario.
- Reactive Forms sin disabled nativo: PASS esperado en la recertificación exact-head.
