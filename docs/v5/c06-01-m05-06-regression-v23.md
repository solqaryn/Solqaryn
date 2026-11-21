# C06-01.M05.06 — Regresión, rendimiento y accesibilidad públicos (V23)

PROJECT_SCOPE_LOCK=STRICT; repositorio solqaryn/Solqaryn, rama dev. Alcance atómico PLAN_MAESTRO!D895: ejecutar regresión, rendimiento y accesibilidad proporcionales para la landing institucional Angular existente en /conoce-solqaryn. PLAN!E895 es una meta acumulativa M05, no certificación de toda la fase.

## Análisis de impacto, criterio y reversión

R1, prueba de navegador real: se agregan **únicamente** una suite Playwright, configuración específica y workflow GitHub Actions. No cambia componente, ruta, CSS, DTO, arquitectura, credenciales, persistencia, migraciones, autenticación, JWT, tenancy, pagos ni datos de producción. Dependencias: Angular 22.2.1 ya instalado, Playwright/Chromium, servicio aislado localhost /health. Consumidores del contenido público: visitantes anónimos; los datos empresariales nunca son consultados. Reversión: git revert normal del commit sobre dev, sin force-push. No se altera la historia CHANGELOG_AI.md por su tamaño y el riesgo documentado de lectura vacía/truncamiento; la procedencia se conserva aquí y en BITACORA.

## Evidencia requerida, aún por ejecutar en GitHub-hosted

A0: comparar estos cuatro archivos nuevos y criterios D895, ruta pública y V17/Scope Lock. A1: cinco casos de navegador con 320/390/768/1280/1440, 10 PNG reales antes/después, un único GET /health tras acción explícita, estados 204/403/404/429/500, cancelación/prevención de duplicidad, enlace a /login y parámetros hostiles no reflejados. A2: demostrar ausencia de solicitudes privadas/headers tenant y fuga de token en ruta anónima; **no** equiparar ello a JWT/RBAC tenant A/B auténticos. A3: Angular compilado y Chromium real; tiempos de navegación y nodos medidos **solo en laboratorio CI** (no field CWV, ninguna afirmación de latencia productiva ni presupuesto de red). A4: exigir exact-SHA SUCCESS de job específico, ver 5/5 PASS 0 FAIL 0 SKIP, 10 PNG no vacíos, Scope Lock, self-test de Skill, procedencia V5 y readback PLAN/COLA/BITACORA antes de certificar. Queued/in_progress/failed/skipped NO son PASS. A5 independiente de autor DEV03 por otra Task Javier tras revisión efectiva del diff/pruebas y SHA.

La landing no posee tabla, modal ni sidebar; scroll local, MySQL, JWT autenticado, suite .NET privada, host real de tienda, SEO productivo y medición RUM son **NO_APLICA** al diff de prueba exclusivamente pública, sin certificar las metas acumulativas de la fase M05. Si se halla defecto funcional o P0/P1 no pasar LISTO, registrar QA_DEFECT_FOUND y reparar de modo causal.

## Referencias condicionales consultadas en origen/pin

- SKREF-03 Impeccable: pbakaus/impeccable@2149fcce39a90bb409df5f16515f316a76dc6199, .agents/skills/impeccable/SKILL.md. Jerarquía, densidad, responsive y estados visuales; sin instalar lanzador externo.
- SKREF-05 Taste: Leonxlnx/taste-skill@ccbc15639c97057cbfcf32ecebc38ef716e4bb37, skills/taste-skill/SKILL.md. Enfoque de la landing pública; sin React ni nuevo diseño.
- SKREF-06 Humanizer: blader/humanizer@9862685f575c65a8247f90369951df1b3416e3d6, SKILL.md. Terminología honesta para estado público y límites de las métricas.
- SKREF-10 Angular Developer: angular/angular@6808966993bf2b2d455d9b6245e960a59a01b9a9, skills/dev-skills/angular-developer/SKILL.md. Angular standalone, Playwright y foco accesible.
- SKREF-04 NO_APLICA: no se crea ni modifica motion. SKREF-11 NO_APLICA: no cambia frontera de confianza ni se abre threat model. SKREF-12 NO_APLICA: ninguna optimización de .NET ni perfilado de backend.

Solamente autoriza QA dev por CI; sin QA/main/PROD, contratación, pagos o publicación de frontend productivo.
