# C07-01.M04.02 — Estados de catálogo universal V22

**PLAN_MAESTRO!D1029:** implementar estados carga/vacío/error/éxito/permisos, unidad aislada de M04. Riesgo R2 por superficie multitenant, sin cambios backend ni autoridad de acceso. Referencias leídas: SOLQARYN Governance, SKREF-03 Impeccable pin 2149fcce, SKREF-10 Angular Developer pin 68089669, origen original. Escritor Javier DEV06 Task ID `6af16bbad26881918c283abe06e1f32a`, proyecto SOLQARYN dev con Scope Lock STRICT.

## A0 alcance, dependencia y rollback
- Estado de carga con mensaje y spinner accesibles; fallo HTTP oculta datos previos, datos privados y texto ambiguo de vacío, muestra reintento sin efectos mutantes.
- Contrato HTTP 200 paginado malformado (items array, totalCount entero no negativo) se rechaza fail-closed, sin inventar cero resultados.
- Éxito y vacío se distinguen de error; modo consulta indica permisos provenientes de `PermisosRuntimeService`, no concede derechos ni altera backend.
- Diseño: tokens CSS existentes, móvil 320 px y escritorio, foco y mensajes ARIA. Sin nuevas dependencias/rutas/secrets/producción.
- Rollback: revert normal FF del commit propio bajo V17 sin cambiar otros scopes.

## A1–A3 pruebas reales y límites
Playwright con Angular+Chromium verdaderos en GitHub-hosted, cuatro pruebas adversariales: demora HTTP con spinner; 503 sin detalles privados y reintento; HTTP 200 paginación malformada fail-closed; read-only vs tenant denegado y cero operaciones POST/PUT/PATCH/DELETE. Fixtures de tenant/JWT/RBAC son **sintéticos**, no constituyen certificación de seguridad backend, JWT firmado o MySQL; A2 para el diff verifica que la superficie no concede derechos adicionales y el guard existente sigue activo. No tocar billing ni tareas ajenas.

## A4, A5 y cierre
Requeridos `completed/success` para workflow dirigido exact-SHA, TypeScript/Angular, Scope Lock, V5 y checks pertinentes. Releer PLAN/COLA/BITACORA antes de LISTO, A5 revisión genuina independiente por otra Task Javier. Una prueba `queued`, `in_progress`, `skipped`, `cancelled` o `failed` no es PASS. Evidencia mínima se limita a PLAN D1029 y no certifica toda M04. CHANGELOG_AI.md histórico no se reconstruye desde fetch parcial.
