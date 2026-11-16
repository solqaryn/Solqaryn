# SOLQARYN V17 — C11-04.M04.05: accesibilidad y responsive de observabilidad privada

**ID:** C11-04.M04.05 · **PLAN_MAESTRO:** 1716 · **autor:** PRIMARY DEV 06, TASK_ID 6af16bbad26881918c283abe06e1f32a · **riesgo:** R1 Angular · **rama:** dev · **PROJECT_SCOPE_LOCK:** STRICT.

## A0 — alcance y consumidores

La microtarea verifica el componente existente de observabilidad privada para anchos 320, 768 y 1280 px, navegación real mediante Tab, foco visible, labels accesibles, targets táctiles de 44 px y contraste de identidad visual. Se conserva la ruta privada protegida /plataforma/observabilidad-v5, el backend como autoridad exclusiva y la interfaz sin métricas ni exporters. Cambios limitados al CSS de targets táctiles, tests Playwright Chromium dirigidos, este documento y CHANGELOG. Sin API, DTO, Angular routing, JWT/RBAC backend, persistencia EF/MySQL, proveedores, datos productivos, secrets ni nuevas dependencias.

## A1 — casos que deben detectar defectos

- Positivo: Tab físico alcanza enlace y botón con :focus-visible y outline de al menos 2 px.
- Negativo: simular foco con HTMLElement.focus no acredita navegación; la prueba sólo usa keyboard.press(Tab).
- Frontera: 320/768/1280 px sin scroll horizontal; rejilla 1/2 columnas y ambos controles completamente visibles.
- Accesibilidad de interacción: ambos targets miden al menos 44 px y no se salen del viewport; contraste calculado sobre CSS renderizado del distintivo >=4.5.
- Error/seguridad: retener los casos existentes de respuesta 403/503, revocación, no doble GET y ocultación de contenido al fallar sesión. Los mocks de sesión son sintéticos y no demuestran autenticación HTTP real.

## A2/A3 — segregación, integración y límites

Conservar tenant/JWT/RBAC y revocación existentes, sin usar estilo, cliente o marca como autorización. La UI oculta panel privado durante revalidación y errores; ninguna prueba E2E sintética reemplaza controles reales del backend. Playwright Chromium y compilación Angular deben ejecutarse en GitHub-hosted para el SHA publicado. MySQL 8.4, EF, integraciones HTTP de proveedor y despliegue: NO_APLICA en esta modificación local de interfaz. Cualquier falla P0/P1 o evidencia insuficiente bloquea certificación.

## A4/A5 — publicación y revisión

Publicación individual fast-forward/CAS en dev con comprobación del HEAD exacto, blobs, Scope Lock, procedencia V5, tipado/build Angular, Vitest y los escenarios Chromium dirigidos. Ningún QUEUED/IN_PROGRESS/FAILED/SKIPPED es PASS; una CI global fallida debe analizarse causalmente y no ocultarse. PLAN/COLA/DAG y BITACORA requieren readback por transición. A5 queda pendiente de otra Task Javier distinta del autor, con poder de rechazar código y pruebas. Rollback: git revert normal del changeset con validación de HEAD; nunca force-push, QA/main/PROD, ni borrar BITACORA.
