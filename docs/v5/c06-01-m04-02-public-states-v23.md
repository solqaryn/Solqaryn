# C06-01.M04.02 — Estados públicos reales y recuperación (V23)

SOLQARYN dev, PROJECT_SCOPE_LOCK=STRICT. PLAN_MAESTRO!D885 exige carga, vacío, error, éxito y permisos. PLAN E885 es resultado acumulado de M04, no certificación total de esa fase. Riesgo R1 UI/HTTP público, sin contrato privado; no modifica API, RBAC, JWT, MySQL, pagos, migraciones ni secretos.

## Entrega y límites

Componente standalone Angular22 `C0601PublicStatusComponent` integrado en la página informativa `/conoce-solqaryn`. Su único consumidor es el usuario que solicita explícitamente GET `/health` público: cero peticiones antes de interacción, credenciales omitidas, sin token/tenant ni caché persistente. Respuesta JSON pública esperada `{status:"ok"}`; 204 o cuerpo vacío muestra vacío; HTTP 401/403 denegación de health, 404 ruta no disponible, 429 limitación de frecuencia, 5xx/red/timeout error recuperable, éxito 200. No se renderiza texto no confiable del servidor, ni se afirma uptime global, permisos de usuario, tenants A/B, disponibilidad legal de compra ni status privado.

UI premium conserva identidad editorial y tokens institucional/contraste existentes; panel adaptable con contenedor propio y botón de 44px, focus visible, texto aria-live, teclado, doble clic deshabilitado durante carga. CTA no hace fetch implícito. Abortar al destruir componente; tiempo máximo 8 s. Sin nuevo framework, dependencias o motion.

## Referencias originales pinned ACTIVE

- SKREF-03 Impeccable: `pbakaus/impeccable@2149fcce39a90bb409df5f16515f316a76dc6199`, `.agents/skills/impeccable/SKILL.md` (jerarquía, responsive, estados accesibles).
- SKREF-05 Taste: `Leonxlnx/taste-skill@ccbc15639c97057cbfcf32ecebc38ef716e4bb37`, `skills/taste-skill/SKILL.md` (landing B2B editorial sin plantilla genérica).
- SKREF-06 Humanizer: `blader/humanizer@9862685f575c65a8247f90369951df1b3416e3d6`, `SKILL.md` (copy preciso y sin afirmaciones inventadas).
- SKREF-10 Angular Developer: `angular/angular@6808966993bf2b2d455d9b6245e960a59a01b9a9`, `skills/dev-skills/angular-developer/SKILL.md` (Signals, standalone, a11y).
- NO_APLICA: SKREF-04 sin motion, SKREF-11 sin frontera JWT/RBAC/tenant nueva, SKREF-12 sin medición de rendimiento .NET. No copiar/instalar skills ni introducir otro stack.

## Pruebas y certificaciones condicionadas

A0: diff limitado a componente, route shell, runner HTTP localhost efímero y Playwright; rollback `git revert` normal. A1: probar HTTP GET real sobre servidor loopback Node sin `page.route`, 200/204/403/404/429/500, estado loading, retry a success, sin doble petición concurrente. A2: comprobar en Chromium ausencia de petición antes de clic, de Cookie, Authorization, X-Empresa-Id, POST privados y de exposición de payload; NO_APLICA certificar permisos reales de backend desde esta UI pública. A3: Angular 22 build + test Chromium en GitHub-hosted con 320/390/768/1280/1440 capturas PNG verdaderas antes/después y overflow, escala Chromium 200%, teclado. Fixture es servidor HTTP real efímero para UI; **no** es producción ni prueba de JWT, MySQL ni disponibilidad real de Render/Aiven. A4: checks pertinentes SUCCESS en exact SHA, Scope Lock/V5, artefactos PNG readback, BITACORA y PLAN/COLA readback. A5: revisar de forma independiente por una Task Javier diferente a DEV04, autor y reparador; no autopass.

Un workflow QUEUED, IN_PROGRESS, SKIPPED, CANCELLED o FAIL necesario mantiene VALIDANDO; no se declara LISTO sin A0–A4. Los errores de CORS o proxy real no se confunden con 403 de permisos privados. No QA/main/PROD.

## Reparación real del validador P0-007.A tras ejecución de Chromium

La primera ejecución GitHub-hosted sobre `4ccbac2fedd3bce8eb673bb33e90350edb45c57a` verificó cinco pruebas Chromium reales y persistió diez PNG de ancho 320/390/768/1280/1440 (artefacto 14723434965, 3,409,875 bytes). Frontend build, V5/procedencia y Scope Lock completaron SUCCESS; el chequeo P0-007.A falló porque el nuevo archivo Angular no estaba en el catálogo fuente de componentes. Se corrigió de manera aditiva el manifiesto P0-007.A (170 archivos, 162 feature y 8 shared) y sus contadores del validador; se mantiene la prueba negativa de deriva. Esta reparación necesita un nuevo SHA y todos los checks pertinentes SUCCESS antes del cierre, sin convertir el antiguo fallo en PASS. No hay cambio de auth backend ni dependencia externa.
