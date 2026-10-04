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
