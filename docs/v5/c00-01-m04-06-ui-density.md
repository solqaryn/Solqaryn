# V5 C00-01.M04.06 — densidad y carga progresiva

Microtarea: verificar densidad, carga progresiva y percepción de ligereza en la tienda pública de SOLQARYN, sin atribuir certificación de latencia no medida.

## Evidencia causal
- Ruta pública Angular /tienda en storefront.component.html.
- Hero y CTAs antes de diferir secciones con @defer(on idle); categorías y destacados tienen estados loading/error/empty/success.
- Prueba Chromium real Playwright en GitHub N8.2, viewport 390 y 1440: hero, título visible, CTAs >=44px, separación >=7px, sin overflow, títulos de secciones progresivas y telemetría de ruta.
- Datos de configuración de empresa son fixture de CI, no readback productivo. Navegador, CSS y código Angular sí son reales.
- Sin cambios de API, esquema, identidad, RBAC, Cloudinary ni configuración productiva. No se inventan SLA/Core Web Vitals.
- Se exigen Node/Angular lint/build, N8.2 y Scope Lock del SHA publicado; A5 independiente antes de QA/PROD.

Rollback con git revert del SHA publicado sobre HEAD vigente, sin force-push.
