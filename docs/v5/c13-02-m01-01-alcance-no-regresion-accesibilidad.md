# SOLQARYN V5 FINAL — C13-02.M01.01: alcance y no-regresión de accesibilidad

**Microtarea:** C13-02.M01.01 — fijar objetivo, no-alcance y criterio de no-regresión. **Autor:** SOLQARYN PRIMARY DEV 04 JAVIER, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. **Corte de lectura:** `solqaryn/Solqaryn@dev`, HEAD inicial `88c05d3a6d6e5914f3c97134f23ff7b44354d042`, 2026-11-14. **Alcance:** preparación documental M01; `PROJECT_SCOPE_LOCK=STRICT`.

**Autoridades:** [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN_MAESTRO / COLA / DAG_TECNICO / BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). La fila PLAN_MAESTRO 1910 y DAG_TECNICO 1871 delimitan esta tarea. WCAG aplicable y cualquier nivel de conformidad deberán acordarse y demostrarse con auditoría y pruebas posteriores: **este documento no certifica cumplimiento WCAG, accesibilidad de la aplicación ni pruebas de navegador ejecutadas**.

## 1. Resultado buscado, evidencia y límites

Definir una ruta verificable de accesibilidad para las superficies públicas y privadas de SOLQARYN: teclado, orden de foco, componentes semánticos, etiquetas y descripciones, contraste, mensajes de error, navegación, lectura por tecnología asistiva, estados de carga y responsive. Identificar en fases posteriores componentes, flujos y usuarios representativos, medir criterios seleccionados de WCAG según aplicabilidad y registrar defectos reproducibles con severidad, corrección, prueba y rollback.

**Resultado exigible en M01:** contrato de objetivos y no-regresión; matriz de oráculos futuros y evidencia esperada. **No incluye:** cambios Angular/SCSS, tokens, nuevos componentes, modificaciones API/DB, WCAG audit real, Playwright ejecutado, pruebas con lector de pantalla real, auditoría jurídica, ajustes en producción, modificación de cookies, endpoints, permisos JWT/RBAC/tenant, workers, servicios de pago, CI o programaciones.

**Dependencias:** M01.02 identifica actores, entradas y salidas; M01.03 inventaría fuentes concretas; M01.04 enumera riesgos y decisiones; M01.05 fija aceptación; M01.06 sólo termina con todos los gates pertinentes. DAG VALIDADO/SI permite únicamente preparación de esta microtarea, no libera fases M03/QA/PROD.

## 2. Fuentes técnicas inspeccionadas y límites de inferencia

| Archivo o contrato de dev | Blob Git leído | Observación verificable / límite |
| --- | --- | --- |
| `frontend/package.json` | `e6d030f643421cb343cc12617e742469e88223c4` | Angular y scripts de build/lint, `test:responsive` Playwright; existencia de script no prueba ejecución ni accesibilidad |
| `frontend/src/app/app.routes.ts` | `2f560c14693455b79ac4546e9333fb74586ef234` | Rutas lazy, privadas y públicas; guards cliente no sustituyen autorización API |
| `frontend/playwright.config.ts` | `87946080e7fe87f5fa9e9c8d4c8da8a36f49d5da` | Configuración E2E, no resultado de tests ni cobertura de lector real |
| `docs/v5/c05-01-m01-05-criterios-aceptacion-design-system.md` | `875e6929d0f166e70192f1b6a441997f7bcefb6c` | AC/N de design system como oráculos documentados, no PASS runtime |
| `docs/v5/c12-02-m01-05-criterios-aceptacion-frontend-separado.md` | `0758f037ae4910c73d6617f63d5bafc753acfb8d` | Oráculos para SPA, build y frontend; no es auditoría WCAG |

**Límite de lectura:** no se ha inventariado toda la UI ni ejecutado dispositivo, browser, red, capturas, axe, NVDA, VoiceOver o teclado sobre flujos reales. No afirmar que el sistema sea accesible o inaccesible sin evidencia específica. El código vigente permanece sin cambios.

## 3. Invariantes de no-regresión NR01–NR14 (contrato futuro)

| ID | Invariante que se deberá probar | Síntoma de regresión |
| --- | --- | --- |
| NR01 | Todo control interactivo importante operable sólo con teclado | Acción inaccesible sin mouse |
| NR02 | Tab/Shift+Tab mantienen secuencia comprensible y foco visible | Foco invisible, ciclo involuntario o bloqueo |
| NR03 | Diálogos gestionan entrada, salida y restauración del foco | Foco atrapado o perdido tras cerrar |
| NR04 | Formulario tiene label, nombre accesible y error vinculado al campo | Error sólo cromático o input sin nombre |
| NR05 | Mensajes async se anuncian sin desplazar abruptamente el foco | Cambios importantes silenciosos para lector |
| NR06 | La información no depende exclusivamente de color o icono | Estado indescifrable con contraste reducido |
| NR07 | Temas y estados interactivos mantienen contraste medible según criterio aprobado | Texto o foco ilegible en modo alterno |
| NR08 | Reflow móvil/zoom no oculta funciones esenciales ni fuerza scroll dual innecesario | Acción vital cortada u oculta |
| NR09 | Navegación semántica distingue títulos, regiones y propósito de enlaces | Estructura ambigua en lector |
| NR10 | Imágenes informativas tienen alternativa adecuada; decorativas no duplican voz | Imagen significativa no descrita o ruido lector |
| NR11 | Animaciones no esenciales respetan preferencia de movimiento reducido | Parpadeo o transición indispensable |
| NR12 | Carga, error, vacío y permiso denegado resultan comprensibles y recuperables | Spinner eterno o mensaje ambiguo |
| NR13 | Guard Angular no concede derechos: JWT, RBAC, pertenencia y tenant A/B se verifican en API | Usuario A logra recurso B manipulando UI/URL |
| NR14 | Documentar criterios no equivale a PASS ni habilita QA/main/PROD, CI omitida o cambios de stack | M01 se usa como certificación o despliegue |

El objetivo exige preservar navegación ligera sin eliminar protecciones. La evaluación manual y automatizada será complementaria: ningún scanner aislado certifica todos los requisitos.

## 4. Matriz inicial de pruebas futuras (TODAS FUTUROS_NO_EJECUTADOS)

| Caso | Estímulo futuro | Evidencia mínima exigida |
| --- | --- | --- |
| P01 | Navegar ruta representativa sólo con Tab, Shift+Tab, Enter y Escape | Video/traza y secuencia de foco por SHA |
| P02 | Enviar formulario incompleto y corregir campos | Nombre/label/error/estado anunciados y reenvío válido |
| P03 | Abrir/cerrar modal en viewport móvil y con zoom | Foco inicial y restaurado, sin elemento cortado |
| P04 | Cambiar tema, idioma y preferencia reduced-motion donde aplique | Contrastes medidos, semántica conservada |
| N01 | Botón sólo `onclick` con ratón | Detección y rechazo |
| N02 | `tabindex` introduce trampa de foco | Caso falla y se documenta |
| N03 | Input sin label asociada | Detección por árbol de accesibilidad |
| N04 | Error señalado sólo en rojo | Rechazar ausencia de descripción textual |
| N05 | Diálogo cierra y devuelve foco al `body` | Fallo documentado |
| N06 | Texto y botón indistinguibles por bajo contraste | Valor medido y regresión |
| N07 | Contenido se corta a 320px o con zoom según caso aprobado | Captura reproducible y fallo |
| N08 | Región async cambia sin anuncio accesible | Evidencia de ausencia de anuncio |
| N09 | Imagen informativa carece de texto alternativo útil | Fallo semántico documentado |
| N10 | Animación esencial ignora `prefers-reduced-motion` | Rechazo y corrección |
| N11 | UI permite ver datos del tenant B con JWT de A | Rechazo de backend exigido con HTTP y fixture A/B |
| N12 | Una prueba SKIPPED/CANCELLED/IN_PROGRESS se registra como PASS | Bloquear cierre de ese gate causal |

**Estos 16 oráculos no se ejecutaron**. La evidencia futura requiere URL/ruta, rol/tenant, dispositivo, viewport, pasos, estado inicial, resultado, artefactos, SHA del build, versión del lector si aplica, fallo, fix y rollback. Cualquier obligación normativa se ratificará por responsables competentes; no se inventa un nivel de certificación.

## 5. Impacto, auditorías, A5 y rollback

- **Frontend:** únicamente lectura de Angular/Playwright y contrato de diseño. No cambian componentes, rutas, traducciones ni estilos; consumidores existentes permanecen intactos.
- **Backend/DB:** ningún contrato API, DTO, tabla, migración, transacción o dato se modifica.
- **Seguridad:** UI no reemplaza autorización server-side. Deben probarse escenarios A/B, JWT revocado, roles insuficientes, cache y PII en futuras fases ejecutables.
- **Integraciones/observabilidad:** no se agregan SDK, workers, scripts, trackers, logs ni métricas activas. Futuras pruebas no almacenarán tokens/PII en artefactos.
- **A0:** verificar diff de dos Markdown, encabezados completos, ausencia de secretos y procedencia del CHANGELOG.
- **A1:** auditar documentalmente NR01–14, P01–04, N01–12 y criterios observables; no afirmar tests runtime.
- **A2:** revisar fail-closed tenant/JWT/RBAC/PII y negaciones, sin alterar el backend.
- **A3:** cotejar blobs Git y contratos C05-01/C12-02, marcar builds Angular/Playwright y .NET como NO_APLICA al diff solo Markdown.
- **A4:** publicar por fast-forward/CAS sobre HEAD dev; exigir Scope Lock y check V5 `completed/success` del SHA exacto; readback PLAN/COLA y BITACORA A:Z.
- **A5:** queda PENDIENTE de revisión técnica por otro TASK_ID Javier del allowlist; no autoaprobar ni promover a QA/main/PROD.
- **Rollback:** `git revert` del commit documental como nuevo commit no destructivo, con readback y evento compensatorio, sin reescribir historia ni tocar datos reales.

**Estado del contrato:** PREPARACIÓN_M01; aceptación y conformidad runtime NO_VERIFICADAS.
