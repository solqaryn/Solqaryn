# C00-02.M04.01 — superficie Angular privada del ciclo V5 (solo lectura)

**Alcance:** ruta `/plataforma/flujo-v5`, protegida por el guard existente `plataformaGuard`, que consulta sesión global verificada por backend. Se reutiliza `PlataformaComponent` y sus componentes Material; no se crea un `*.component.ts` fuera del inventario Angular canónico.

## Criterio de entrega y fuente

Fuente: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, microtarea `C00-02.M04.01` (M04 Superficies y experiencia): «Implementar ruta, pantalla o superficie pública/privada correspondiente». DAG_TECNICO VALIDADO G=SI, seis predecesoras C00-02.M03.01–06 LISTO; estado real se lee exclusivamente de PLAN_MAESTRO H/I y COLA G/O. El frontend no escribe en ellas.

Se presenta una **guía contractual estática** de PENDIENTE, EN_PROGRESO, VALIDANDO y LISTO_DEV. La pantalla indica inequívocamente que no consulta ni certifica estados reales, no reclama microtareas, no escribe BITACORA, no dispone de botón de ejecución y no presenta controles de QA/main/PROD. El acceso HTTP sigue validándose en `/plataforma/sesion` por el backend. Una identidad de empresa no adquiere autoridad de plataforma. El frontend no sustituye JWT, RBAC ni separación A/B.

## Alcance técnico

- Archivos: `frontend/src/app/app.routes.ts`, `frontend/src/app/features/plataforma/plataforma.component.ts` y `frontend/src/app/app.routes.v5.spec.ts`; cambio aditivo de CHANGELOG_AI.md.
- Dependencia: `plataformaGuard`, `PlataformaSesionService`, componentes Angular Material ya instalados. Se evita una petición de directorio para la vista informativa.
- Sin endpoint API nuevo, migración, MySQL, provider, worker, secretos, webhooks, eventos entre Tasks, acceso a datos reales o cambios del token.
- UX: jerarquía clara, etiquetas, pasos numerados, `role=note`, diseño adaptable a móvil, sin animación obligatoria.
- Clasificación: requiere_dotnet=NO; requiere_node_angular=SI; requiere_mysql=NO; requiere_docker=NO; requiere_e2e=POR_EVALUAR según evidencia de navegación real; requiere_security=SI (guard existente, no exposición); requiere_scope_lock=SI.

## Pruebas y cierre

A0: diff de rutas, template, estilos, secretos y provenance. A1: Vitest de ruta, caso permitido y denegaciones del `plataforma.guard.spec.ts`; ningún mock autoriza acceso real. A2: misma barrera de sesión global, sin grants locales ni datos empresariales. A3: lint, tipado, build Angular y regresiones del SHA exacto en GitHub Actions. Para A4 se exige commit directo fast-forward a dev, Scope Lock SUCCESS y checks técnicos pertinentes, diff, rollback mediante `git revert` normal y readback PLAN/COLA/BITACORA. La navegación visual/e2e no se declara PASS si no se ejecuta realmente cuando sea exigible. A5 independiente solo otra Task Javier autorizada antes de promoción, nunca autorrevisión.

**Límite:** este entregable es una superficie informativa y no una consola operativa ni evidencia de backend V5 expuesto al público.
