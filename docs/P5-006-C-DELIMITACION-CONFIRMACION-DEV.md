# P5-006.C — Delimitación de modal de confirmación institucional (DEV)

- Autoridad: Plan Comercial Inicial V3, P5-006.C, origen P5-006.A. Dependencia P1-016.B LISTO, COLA G798=SI/O798=SI al claim.
- Único archivo de producto inspeccionado: `frontend/src/app/shared/confirm-dialog.component.ts`.
- Blob exacto de producto: `f9b73544cc70beba83a891b2fc89537f0718af8b` (comprobado con `git hash-object` sobre contenido recuperado de `dev`).
- Escenario delimitado: cuadro de confirmación de eliminación; contrato `ConfirmDialogData(title, message)`, componente Angular standalone con Material Dialog/Button, texto interpolado, botón Cancelar devuelve `false`, botón Eliminar devuelve `true` y usa estilo de advertencia.
- Cambio mínimo: prueba estructural de contrato en `scripts/tests/p5-006-c-delimitacion-confirmacion.test.mjs` y esta acta; no modifica el runtime ni representa implementación del sistema completo de cuatro modales.
- Evidencia reproducible en Node 22.16.0: `node --check scripts/tests/p5-006-c-delimitacion-confirmacion.test.mjs && node --test scripts/tests/p5-006-c-delimitacion-confirmacion.test.mjs`; seis subtests: positivo sobre archivo real, negativo cancelar, negativo confirmación destructiva, negativo HTML crudo, negativo pérdida del DTO y negativo módulo ausente/archivo vacío.
- A0 (calidad): módulos built-in Node; no nuevas dependencias, migraciones, API ni configuración. Escanear diff en A0 antes del commit, revisar ausencia de secretos y usos prohibidos.
- A1 (funcional): el alcance requerido es **delimitar un único escenario**, no certificar funcionalidad dinámica de todos los modales. Pruebas de mutación demuestran que regresiones relevantes rompen el contrato estructural.
- A2 (seguridad): interpolaciones Angular y prohibición de `innerHTML` reducen riesgo de HTML inseguro; cancelar nunca se interpreta como afirmación destructiva. Este control de UI no concede permisos ni sustituye autorización del backend.
- A3 (integración proporcional): revisión de importaciones y bindings Material presentes en el componente real; el test estático no prueba compilación Angular completa ni eventos con navegador real.
- Límites: no se ejecutó Angular browser, Playwright ni pruebas de accesibilidad con lector de pantalla; tampoco JWT, RBAC, API, MySQL, multitenancy real ni QA/PROD. Esas superficies no se modifican. No prometer PASS de entorno que no se ensayó.
- Rollback: revert normal y selectivo de test, esta nota y entrada aditiva en `CHANGELOG_AI.md` desde dev, conservando el componente productivo y commits ajenos.
- A4 después de publicar: comparar SHA nuevo y diff final en dev, confirmar resultados de prueba y readback de PLAN/COLA/BITACORA. A5 por Task independiente queda PENDIENTE para promoción.
