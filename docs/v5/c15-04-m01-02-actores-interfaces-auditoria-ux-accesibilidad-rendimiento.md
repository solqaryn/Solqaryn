# SOLQARYN V5 — C15-04.M01.02: actores, entradas, salidas y fronteras de UX/accesibilidad/rendimiento

**Autor:** PRIMARY DEV 03 JAVIER; TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Scope:** `solqaryn/Solqaryn@dev`; PROJECT_SCOPE_LOCK=STRICT. **Unidad:** M01.02 de preparación documental; no implementación, auditoría WCAG ni medición runtime.

## Autoridad y contrato antecedente

Fuente ejecutable: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. PLAN_MAESTRO fila 2271, COLA G/O=SI, DAG_TECNICO F=VALIDADO/G=SI. El documento `docs/v5/c15-04-m01-01-alcance-no-regresion-auditoria-ux-accesibilidad-rendimiento.md` fue publicado en SHA `eb4cf6f9263aad255b23388c54b41d44d1b3928d` y fija NR01–NR14, P01–P08/N01–N08 como oráculos FUTUROS NO_EJECUTADOS. Este contrato complementa, no sustituye, M01.01.

Fuentes estáticas cotejadas en dev: `frontend/src/app/app.routes.ts` (blob `2f560c14693455b79ac4546e9333fb74586ef234`), `frontend/src/styles.scss` (`2eb48562fd77c758662af6b9683649b4f9698a64`), `frontend/src/styles/_enterprise-design-system.scss` (`6a188590af9a958bff2e3e0375c0c46cdb2b14e6`), `frontend/src/styles/_m10-accessibility.scss` (`6140d41b8a7e1e98f4fa1e43e0ab72f04b6ae29f`) y `frontend/playwright.config.ts` (`87946080e7fe87f5fa9e9c8d4c8da8a36f49d5da`). Ninguna ruta, import, token o configuración por sí solo prueba la experiencia en un navegador.

## Doce actores/consumidores y autoridad

| Actor | Superficie o entrada | Límite explícito |
| --- | --- | --- |
| A01 Visitante anónimo | Landing / sitio público habilitado | Sólo páginas públicas existentes, sin sesión privada |
| A02 Cliente del storefront | Navegación / catálogo y checkout donde existan | No inferir checkout implementado por la ruta |
| A03 Usuario empresarial | Login, navegación ERP y tareas autorizadas | Guard Angular mejora UX; API .NET valida JWT/RBAC |
| A04 Administrador empresarial | Configuración, usuarios y personalización autorizada | Tema de A no debe alterar permisos ni datos de B |
| A05 Operador móvil | Formularios/tablas con viewport reducido | No perder acciones ni visibilidad de errores |
| A06 Usuario de teclado | Menús, diálogos, formulario y atajos | Operabilidad sin mouse, secuencia y foco verificables |
| A07 Usuario de lector asistivo | Nombres, roles, estados, anuncios y semántica | Auditoría manual pendiente; CSS no demuestra compatibilidad |
| A08 Usuario con movimiento reducido | Animaciones, loading y transiciones | Respetar preferencias sin ocultar información |
| A09 Usuario con conexión limitada | Carga progresiva, paginación y errores de red | Umbral de rendimiento por acordar; no inventar SLO |
| A10 Equipo de diseño/frontend | Tokens, componentes Angular/Material y rutas | No introducir CSS o framework por escribir esta unidad |
| A11 Auditor técnico de accesibilidad/rendimiento | Playwright, navegador, medidas y capturas sintéticas | Registrar SHA, fixture, viewport, dispositivo y resultado real |
| A12 Revisor A5 independiente | Evidencia GitHub/PLAN/COLA/BITACORA | TASK_ID distinto del autor y sólo dev; no QA/PROD |

## Doce interfaces de entrada y salida — oráculos futuros

| IO | Entrada a auditar | Salida observable exigible |
| --- | --- | --- |
| IO01 | Navegador visitante → ruta pública | Página disponible sólo si esa capacidad existe; semántica y CTA visibles |
| IO02 | Navegador autenticado → ruta privada | Navegación coherente y backend deniega permisos ausentes |
| IO03 | Tab/Shift+Tab/Enter/Espacio | Foco visible, orden lógico y activación por teclado |
| IO04 | Abrir/cerrar modal o menú | Gestión/restauración de foco y anuncio de cambios |
| IO05 | Input con validación y submit | Nombre accesible, error asociado y recuperación |
| IO06 | Cambio de tamaño/zoom de pantalla | Contenido fluye sin corte, scroll de tabla confinado |
| IO07 | Preferencia reduced-motion | Efectos decorativos reducidos sin perder contexto |
| IO08 | Token de color/typography/theme | Estado legible y coherente en variantes, sin depender de color |
| IO09 | Búsqueda, filtro o lista grande | Estado loading/empty/error y resultados accesibles |
| IO10 | Red lenta o pérdida temporal de conexión | Retroalimentación y recuperación sin spinner perpetuo |
| IO11 | Marca/contenido de tenant A/B | Sólo contexto autorizado; UI no autoriza ni cachea información ajena |
| IO12 | Fixture y ejecución de pruebas | Evidencia SHA, navegador, viewport, caso, salida y capturas sin datos reales |

**IO01–IO12 se definen para una auditoría futura, no para certificar ejecución, E2E ni cumplimiento.** Donde la función no exista, documentar `NO_APLICA_RUNTIME_NO_IMPLEMENTADO` y mantener su gate independiente.

## Diez fronteras de confianza y pares negativos

| Frontera | Positivo futuro | Negativo futuro |
| --- | --- | --- |
| F01 Público / autenticado | CTA público navega sin datos privados | Página pública revela estado o identidad privada |
| F02 Browser Angular / API .NET | UI oculta acción y servidor comprueba permiso | Angular autoriza a tenant B por guard o tema |
| F03 Teclado / evento | Control acepta foco y acción | Mouse-only o trampa de foco |
| F04 Formulario / estado | Error textual junto al control | Sólo color o error no anunciado |
| F05 Overlay / documento | Modal contiene/restaura foco | Overlay esconde contexto o pierde posición |
| F06 SCSS / componentes | Contraste y token medidos en estados | Color de branding deja texto ilegible |
| F07 Layout / viewport | Responsive conserva acciones | Overflow global y corte desde 320px |
| F08 Movimiento / preferencia | Reduced-motion preserva comprensión | Animación intrusiva o información perdida |
| F09 Caché / tenancy | Cambiar tenant refresca estado de A/B | Vista o branding de B reutilizado por A |
| F10 CI / auditoría | Captura/test vinculados a SHA exacto | Test skipped, sin ejecución, SHA ajeno o auto-A5 se declara PASS |

Cada positivo y negativo de F01–F10 es **FUTURO_NO_EJECUTADO**. La categoría de actor no implica conocimiento de ninguna condición personal de usuarios reales.

## Próximas unidades y reglas de ejecución verificable

- M01.03 debe identificar rutas/components existentes con blob SHA y brechas concretas por superficie; no inferir el inventario completo de todo Angular.
- M01.04 debe distinguir riesgos potenciales, supuestos y decisiones sin métricas inventadas.
- M01.05 debe fijar umbrales y oráculos sólo cuando exista baseline, equipo, dispositivo y alcance aprobado.
- M01.06 debe verificar dependencias, A0–A4, A5 independiente y ausencia de blockers antes de habilitar M02; una entrada DAG no basta.

Futuros tests causales: Matriz actor × ruta × dispositivo × viewport × modo teclado/lector × perfil de red × tenant A/B; cada caso requiere resultado esperado/observado, screenshot/traza sintética, severidad de defecto, SHA, URL de CI y rollback. La autorización server-side nunca se valida mediante screenshot de un botón deshabilitado.

**Cambios efectivos:** este Markdown y `CHANGELOG_AI.md` exclusivamente. A0 inspección de actores/fuentes, A1 coherencia de IO positivos/negativos, A2 límites de JWT/RBAC/tenant A/B, A3 vínculo con M01.01 y rutas Git, A4 Scope Lock/V5 SHA exacto y readback. Angular/Playwright/.NET/MySQL ejecutables son `NO_APLICA_RUNTIME_DOCUMENTAL` al diff, no PASS. A5 debe ser efectuado por otra Task Javier. Rollback por `git revert` sin reescritura ni QA/main/PROD.
