# P5-004.C — Delimitación causal del modal de detalle institucional (DEV)

- Autoridad: Plan Comercial Inicial V3 P5-004.C, origen P5-004.A. Delimitar único archivo/escenario y registrar ruta/SHA.
- TARGET_PATH: `frontend/src/app/shared/alerts/app-alert.service.ts`; SOURCE_BLOB_SHA: `3bde601b54bad7d979fca9029025ce4eb72a44db`.
- Este alcance NO certifica todos los modales ERP; delimita solamente detalle opcional de `AppAlertDialogComponent` con `AppAlertData.detalle`, interpolación segura, título, mensaje, cancelación, texto largo y restauración del foco.
- Cambio mínimo: prueba `scripts/tests/p5-004-c-delimitacion-modal-detalle.test.mjs` sobre contenido real con positivos y negativos.
- Comando: `node --check scripts/tests/p5-004-c-delimitacion-modal-detalle.test.mjs && node --test scripts/tests/p5-004-c-delimitacion-modal-detalle.test.mjs`.
- Evidencia: Node 22.16.0 6/6 preflight sobre extracto de fuente; contenido íntegro GitHub 8/8 mutaciones. Ni el extracto ni el test estático certifican Angular/browser real.
- No cambia runtime, JWT/RBAC backend, DB, API, deps, secretos ni despliegues; A5 por otro TASK_ID.
- Rollback selectivo por nuevo commit fast-forward para nota/prueba/changelog sin reescribir otros commits.
