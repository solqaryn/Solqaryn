# P5-017.C - Delimitacion del layout responsive de modales (DEV)

- Fuente: Plan Comercial Inicial, P5-017.C, tarea atomica DELIMITAR; origen P5-017.A y dependencia P1-016.B cerrada. No es certificacion visual de todos los modales.
- TARGET_PATH unico: `frontend/src/app/shared/alerts/app-alert.service.ts`. Blob de origen `3bde601b54bad7d979fca9029025ce4eb72a44db` en dev. Se conserva el componente de Angular intacto.
- Escenario unico: dialogo de alerta/confirmacion de `AppAlertService`, implementado con MatDialog en `confirmar` y `solicitarTexto`.
- Contrato observable por fuente: ambas aperturas declaran `width: 'min(94vw, 540px)'`, `maxWidth: '94vw'` y `restoreFocus: true`. A 480px de ancho o menos, el CSS apila las acciones con `flex-direction: column-reverse` y da `width: 100%` a los botones. Esos limites se prueban sobre el archivo de producto exacto, sin transformar el codigo de runtime.
- Negativos estructurales: ancho de dialogo no relativo al viewport, breakpoint perdido, acciones sin stacking/full width, foco no restaurado y sink HTML inseguro. Test Node `scripts/tests/p5-017-c-delimitacion-layout-responsive.test.mjs`, `node --check` y `node --test` (5 casos).
- No inferir cumplimiento WCAG, zoom 200%, lector de pantalla, gestos, teclado real ni resultado visual en navegadores o telefonos. No certificar los cuatro modales institucionales por este micro-paso. Validacion responsiva visual completa corresponde a un alcance posterior de prueba.
- Sin edicion de Angular runtime, DTO, backend, JWT, RBAC, tenant, bases de datos, scripts de migracion, entornos QA/main/PROD o secretos. Un contrato de presentacion no concede autorizacion de negocio.
- A5 independiente por otro TASK_ID antes de cualquier promocion; no genera autorizacion para desplegar.
- Rollback: revert selectivo, normal, de prueba, nota y entrada aditiva CHANGELOG en dev; sin force push y sin revertir commits ajenos.
