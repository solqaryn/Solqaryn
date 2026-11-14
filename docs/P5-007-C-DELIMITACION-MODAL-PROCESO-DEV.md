# P5-007.C — Delimitación de modal de proceso institucional (DEV)

- Autoridad: Plan Comercial Inicial V3, microtarea P5-007.C, origen P5-007.A; dependencia P1-016.B LISTO en PLAN/COLA.
- TARGET_PATH: `frontend/src/app/shared/anular-dialog.component.ts`; SOURCE_BLOB_SHA: `d751503438f963f1deebe371cd14623ad6038e16`. El producto existente no se modifica.
- Escenario único seleccionado: proceso de anulación con causa obligatoria. `AnularDialogComponent` recibe `MAT_DIALOG_DATA` tipado, usa texto interpolado Angular para título/mensaje (sin HTML crudo), muestra `motivo`, deshabilita Anular si sólo hay espacios, devuelve `undefined` en Cancelar y el motivo al confirmar.
- Prueba: `scripts/tests/p5-007-c-delimitacion-modal-proceso.test.mjs`, 5 controles Node sobre el archivo real: uno positivo y cuatro rechazos dirigidos de cancelación que confirme, razón vacía, HTML sin escape y contexto de diálogo perdido.
- Impacto: prueba más nota de delimitación más CHANGELOG; no modifica API/DTO, datos, migraciones, JWT, BCrypt, RBAC, PII, reglas de rama ni UI Angular. No concede permisos ni verifica interacción real en navegador, lector de pantalla o integración de tenant.
- Riesgo no probado: el llamador de este diálogo debe validar autorización y el backend debe imponer permisos y tenant; HTML seguro del template no sustituye sanitización y autorización del servidor. Fases posteriores que modifiquen UI requieren TypeScript, Angular y Playwright reales.
- Evidencia reproducible: Node.js v22.16.0, `node --check scripts/tests/p5-007-c-delimitacion-modal-proceso.test.mjs` y `node --test scripts/tests/p5-007-c-delimitacion-modal-proceso.test.mjs`; sha256 de test `34b37488f05dae7dd68049d892a8436288470a036999f4dfa35324b9ec59332b`. No presentar fixtures sintéticos como integración real.
- Rollback: revert aditivo del commit directo específico, sin force-push y preservando commits ajenos; exigir auditoría A5 independiente para promoción QA/main/PROD.
