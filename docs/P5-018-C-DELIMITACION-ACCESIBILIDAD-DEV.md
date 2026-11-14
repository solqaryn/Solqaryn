# P5-018.C — Delimitación reproducible: accesibilidad visual transversal

- Autoridad: Plan Comercial Inicial V3, P5-018.A; predecesora P1-016.B = LISTO en PLAN_MAESTRO/COLA.
- Único archivo de producto elegido (`TARGET_PATH`): `frontend/src/styles/_m10-accessibility.scss`.
- `BASELINE_BLOB_SHA`: `6140d41b8a7e1e98f4fa1e43e0ab72f04b6ae29f` (Git blob de DEV leído antes del claim). El SHA de commit es distinto del SHA del blob; ver estado del HEAD en cada ejecución.
- Escenario delimitado: CSS de foco programático de `#main-content`, indicador visible con `forced-colors`, y eliminación de transformaciones/transiciones de `.skip-link`/`.app-surface-card--interactive` en `prefers-reduced-motion`.
- Evidencia causal: `frontend/src/styles.scss` importa `./styles/m10-accessibility`. El SCSS seleccionado incluye reglas `@media (prefers-reduced-motion: reduce)` y `@media (forced-colors: active)` y está versionado. Comprobar con `node --test scripts/tests/p5-018-c-delimitacion-accessibility.test.mjs`.
- Resultado de esta microtarea **C = delimitación**, no la implementación de mejoras P5-018.D/E, ni certificación WCAG 2.2 AA, contraste numérico, zoom 400%, lector de pantalla, navegador real o auditoría de accesibilidad de todas las vistas.
- Criterio positivo: el único SCSS seleccionado conserva las ramas de accesibilidad con controles asociados de foco, reduced motion y forced colors; no se toca runtime de componentes ni seguridad RBAC/tenancy.
- Negativos: retirar la rama reduced-motion o forced-colors debe hacer fallar la prueba; un archivo no coincidente o una declaración de segundo target se rechaza. Esta prueba estática no reemplaza la prueba visual/e2e futura.
- Siguiente fase P5-018.D: evaluar/ajustar el único escenario con CSS y evidencia de contraste, zoom, teclado y lector según impacto; en P5-018.E realizar pruebas reales antes de declarar conformidad.
- Impacto: documento y test Node sobre stylesheet existente; sin modificación de Angular runtime, API/DTO, JWT, BCrypt, RBAC, EmpresaId A/B, datos, migraciones, dependencias, credenciales, infraestructura o QA/PROD.
- Rollback: revert selectivo de esta nota, test y entrada de CHANGELOG de P5-018.C mediante nuevo commit normal en dev; nunca force-push. La hoja oficial conserva el historial de eventos.
