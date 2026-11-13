# P5-003.C - Delimitacion del contrato de 4 modales (DEV)

- Autoridad: Plan Comercial Inicial, microtarea P5-003.C (DELIMITAR), origen P5-003.A, antecedente P1-016.B LISTO. No se implementa un nuevo modal ni se certifica el conjunto de cuatro.
- TARGET_PATH unico de producto: `frontend/src/app/shared/alerts/app-alert.service.ts`.
- BASELINE_BLOB_SHA: `3bde601b54bad7d979fca9029025ce4eb72a44db` sobre `dev` al iniciar P5-003.C.
- Frontera comprobada: `AppAlertDialogComponent` y `AppAlertService` son la primitiva existente para alertas, confirmacion booleana (`confirmar`) y entrada textual anulable (`solicitarTexto`); `AppAlertData` admite titulo, mensaje, detalle y entrada opcional. Se usa MatDialog con `restoreFocus: true` y `autoFocus: false`.
- Los cuatro tipos de modal de negocio del alcance institucional (detalle, formulario, confirmacion, proceso) son contratos separados. Este micro-paso solo identifica la superficie reutilizable de confirmacion/alerta; NO afirma que los otros tres tipos esten implementados o auditados. P5-004.C, P5-005.C, P5-006.C y P5-007.C deben verificar sus propias superficies sin convertir un dialogo en permiso de negocio.
- Cambio minimo: test estructural `scripts/tests/p5-003-c-delimitacion-contrato-modales.test.mjs` y esta nota. Ningun archivo Angular productivo, DTO, endpoint, tabla, migracion o secreto es modificado.
- Verificacion dirigida: `node --check scripts/tests/p5-003-c-delimitacion-contrato-modales.test.mjs` y `node --test scripts/tests/p5-003-c-delimitacion-contrato-modales.test.mjs` en checkout exacto. Positivo: interfaz, retorno booleano, cancelacion de texto, foco restaurado, longitud maxima. Negativos: retorno ambiguo, cancelacion tratada como aceptacion, perdida de foco, campo requerido inseguro y sink HTML.
- Riesgos y limites: la prueba estructural no acredita click real, acceso de teclado y lector de pantalla, responsive, autorizacion JWT/RBAC, aislamiento entre empresas, API/DB ni seguridad de la UI completa. La autorizacion queda en backend y su validacion independiente. No alterar QA/main/PROD.
- Rollback: revert selectivo del test, esta nota y la entrada de CHANGELOG_AI.md con un commit normal sobre dev, preservando commits de otros Scopes y sin force-push.
- A5: revision independiente por otro TASK_ID antes de cualquier promocion; la documentacion por si sola no concede A5_PASS.
