# P5-005.C — Delimitacion: modal formulario institucional

- Autoridad: Plan Comercial Inicial V3, microtarea P5-005.C (DELIMITAR), origen P5-005.A; predecesora P1-016.B LISTO. Esto es **identificacion de superficie y escenario**, no implementacion de un sistema de formularios institucional universal.
- Archivo unico de producto (`TARGET_PATH`): `frontend/src/app/shared/anular-dialog.component.ts`.
- `BASELINE_BLOB_SHA`: `d751503438f963f1deebe371cd14623ad6038e16`, obtenido de `solqaryn/Solqaryn@dev` al seleccionar la microtarea; codigo productivo intacto.
- Escenario concreto: modal existente de motivo de anulacion. `AnularDialogComponent` standalone utiliza `MatDialogModule`, `FormsModule`, `MatFormFieldModule`, `MatInputModule`, `matInput` y `[(ngModel)]="motivo"`; cancelar retorna `undefined`, confirmar devuelve `motivo` y mantiene deshabilitada la accion para motivos en blanco o solo espacios con `!motivo.trim()`. Los textos `data.title` y `data.message` usan interpolacion Angular, no HTML crudo.
- Positivo estructural reproducible: archivo real presenta controles Material, enlace del motivo, estado local y semantica explicita de cancelar/confirmar.
- Negativos reproducibles: romper two-way binding, convertir cancelar en confirmar, aceptar entrada compuesta de espacios, introducir HTML sin saneamiento o eliminar FormsModule debe hacer FALLAR `node --test scripts/tests/p5-005-c-delimitacion-modal-formulario.test.mjs`.
- Cambio minimo: solo esta nota y un test Node estatico, con una entrada aditiva de `CHANGELOG_AI.md`. No altera la implementacion Angular del dialogo, ni permisos, roles, APIs, DTO, MySQL, migraciones, secretos, workflows o infraestructura.
- Limite: la prueba estructural de fuente real **NO prueba interaccion de navegador**, teclado/foco real, envio de formulario HTTP, JWT, RBAC backend, persistencia MySQL, integracion con datos comerciales, ni conformidad WCAG. Un futuro ajuste del formulario requiere Angular/browser y pruebas de seguridad dirigidas, sin tratar este DELIMITAR como certificacion E2E.
- Riesgo A/B: no se tocan limites de tenant, controles JWT/RBAC ni endpoints; el servidor sigue siendo la autoridad. Ningun dato sensible de usuario o empresa se registra en los casos.
- Rollback: nuevo commit selectivo que revierta unicamente test, esta nota y la entrada asociada de CHANGELOG, sin force-push ni cambios a otros Scopes.
- A5: requiere revisor TASK_ID independiente sobre el SHA publicado antes de QA/main/PROD; no atribuir A5_PASS por este documento.
