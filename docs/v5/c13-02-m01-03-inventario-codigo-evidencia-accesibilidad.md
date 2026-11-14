# SOLQARYN V5 FINAL — C13-02.M01.03: inventario técnico y brechas verificables de accesibilidad

**Fecha:** 2026-11-14. **Autor:** PRIMARY DEV 03 JAVIER, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Ámbito:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT. **Clase:** lectura y análisis documental sin cambios Angular/SCSS/Playwright, API, JWT/RBAC, MySQL, migraciones, proveedores o producción.

**Fuentes de autoridad:** [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). M01.01 define NR01–NR14 (Git blob `7625e8638e3b228f8773ffe11541b0299228fda9`), M01.02 delimita A01–A14, I01–I12, S01–S10 y F01–F08 (blob `fdf905569b0d289006d86f28b59bc70b26a648e7`). Estos contratos no acreditan conformidad WCAG ni cobertura de todas las pantallas.

## 1. Fuentes reales inspeccionadas en GitHub (blob de contenido, no test PASS)

| Ref | Ruta exacta | Blob | Lo que el archivo demuestra y NO demuestra |
| --- | --- | --- | --- |
| F01 | `frontend/package.json` | `e6d030f643421cb343cc12617e742469e88223c4` | Scripts Angular lint/build y `test:responsive`; no prueba que Playwright se ejecutó con éxito |
| F02 | `frontend/src/app/app.routes.ts` | `2f560c14693455b79ac4546e9333fb74586ef234` | Rutas públicas, lazy y guards; no inventario de nombres accesibles por página |
| F03 | `frontend/playwright.config.ts` | `87946080e7fe87f5fa9e9c8d4c8da8a36f49d5da` | Fixtures y settings E2E, cookies vacías sintéticas; no auditoría con lector de pantalla ni autenticación live |
| F04 | `frontend/src/app/core/guards/auth.guard.ts` | `b652a7af6343bdae83a4dc9f3065d9fc1aa120f6` | Exige tenant context verificado o redirige; no reemplaza RBAC ni test cross-tenant real |
| F05 | `frontend/e2e/c00-03-m04-05-accessibility.spec.ts` | `e16a8ea375e12723a65786930dd3c4f73f69e63a` | Contiene test Playwright ruta privada/403 simulado y scroll teclado a 390/1280; no cobertura global ni backend real |
| F06 | `frontend/src/app/features/centros-costo/centros-costo.a11y.spec.ts` | `5eeecfba3c039765c0d0f088e5f2db73abef408c` | Vitest/TestBed de search, labels, errores; mocks de servicio/permisos, no certificación con lector real |
| F07 | `frontend/src/app/features/estados-financieros/estados-financieros.accessibility.spec.ts` | `f4edd5996a0c816b6fb8d2c2804149f4ac3263f1` | Tests de `aria-labelledby`, etiquetas, `aria-live`, table caption; no prueba todos los flujos financieros |
| F08 | `frontend/src/styles/_m10-accessibility.scss` | `6140d41b8a7e1e98f4fa1e43e0ab72f04b6ae29f` | CSS scroll-margin, focus, forced-colors, reduced-motion; no resultado de contraste computado por tema |
| F09 | `docs/v5/c00-02-m04-05-a11y-responsive.md` | `4f59f87620f79ea60cab78555da997ffcf97268a` | Contrato de tests por viewport; un documento no ejecuta Playwright |
| F10 | `docs/v5/c05-01-m01-05-criterios-aceptacion-design-system.md` | `875e6929d0f166e70192f1b6a441997f7bcefb6c` | Oráculos design system AC/N, no PASS de WCAG |
| F11 | `docs/v5/c12-02-m01-05-criterios-aceptacion-frontend-separado.md` | `0758f037ae4910c73d6617f63d5bafc753acfb8d` | Gates futuros de frontend separado, no auditoría a11y del sitio actual |
| F12 | `docs/N4.10_ESTADOS_FINANCIEROS_A11Y_MATRIX.md` | `1f61347d54ef4617bdaa293b4e827ec1701d4fa1` | Matriz histórica que advierte ausencia de cobertura total; no autoridad de ejecución V5 |

Se leyeron efectivamente las fuentes desde el árbol de desarrollo en el preflight; sus SHA de **blob** no son SHA de workflow, build o test. Tampoco se realizó en esta microtarea un barrido exhaustivo de cada componente Angular, una evaluación humana con lector de pantalla, una captura de contraste por browser o validación de permisos API/MySQL. M01.04/M01.05 deben conservar estas limitaciones.

## 2. Brechas de evidencia B01–B14 y fuente de verificación

| Brecha | Verificación faltante para cierre futuro | Riesgo de calidad/seguridad |
| --- | --- | --- |
| B01 Cobertura por ruta | Inventario de vistas públicas, ERP, checkout, administración y fallos; muestreo justificable | Rutas no auditadas se asumen accesibles |
| B02 Teclado y foco real | Playwright con Tab/Shift+Tab/Enter/Escape, pruebas manuales de tabulación | Trampas de foco o acciones sólo mouse |
| B03 Overlay/modal | Inspección real de MatDialog, Escape, entrada/salida/retorno foco | Foco cae en fondo o se pierde |
| B04 Formularios | Labels, instrucciones, validaciones, `aria-describedby`, errores live | Error imposible de comprender sin color |
| B05 Tablas y scroll | Column headers, regiones de scroll, paginación/teclado a zoom | Resultados inaccesibles o recorte móvil |
| B06 Contraste real | Medidas computadas por estado/tema y forced-colors | CSS aparente oculta contraste insuficiente |
| B07 Reflow y zoom | Matriz 320/390/768/1280 y zoom definido según norma aplicable | Overflow, doble scroll o botón esencial perdido |
| B08 Tecnologías asistivas | Prueba humana y automática combinadas (lector/OS/browser) | Falsa certificación por scanner |
| B09 Movimiento/medios | Test reduced-motion, flashes, imágenes y alternativas | Efectos molestos, contenido silencioso |
| B10 Internacionalización | Idioma, títulos, anuncios, orden de lectura al traducir | Lector incorrecto/label inconsistente |
| B11 Estados async y permisos | Loading/empty/error/denied con foco y anuncios no intrusivos | Error opaco o filtración PII de otro tenant |
| B12 Auth y A/B | HTTP JWT/RBAC/tenant A/B y membresía revocada en backend real | Confundir guard UI o fixture con autorización |
| B13 Evidencia reproducible | CI exact SHA, artefactos browser, a11y tree, contraste, versión de lector | Un archivo .spec.ts se reporta PASS sin ejecución |
| B14 Gate normativo | Matriz de WCAG aplicable, versión/nivel y aprobación competente | Declarar conformidad general sin evaluación |

Las B01–B14 son **brechas de evidencia**, no defectos confirmados del producto, no reportes de exploit ni decisión normativa. Una suite existente puede cubrir un caso y dejar superficies fuera.

## 3. Modelo de prueba positiva/negativa (FUTUROS_NO_EJECUTADOS)

| Caso | Futuro positivo verificable | Futuro negativo que debe fallar |
| --- | --- | --- |
| P01/N01 | Tab/Shift+Tab recorren acción esencial con foco visible | Control interactivo imposible sin mouse |
| P02/N02 | Modal restaurado al invocador tras Escape | Foco queda en `body` o trampa |
| P03/N03 | Formulario asocia label, error y ayuda | Campo sin nombre, mensaje sólo rojo |
| P04/N04 | Tabla y región scrolleable operables a 390 px | Scroll horizontal del body oculta acción |
| P05/N05 | Comprobación de contraste con tema efectivo | Estado dark/forced-colors no alcanza criterio acordado |
| P06/N06 | Estado carga/error anunciado al lector | Cambio async silencioso o foco robado |
| P07/N07 | Reduced-motion evita efecto no esencial | Animación ineludible o intermitente |
| P08/N08 | Ruta autorizada funciona sin exponer otras empresas | JWT de A con empresa B filtra datos o backend permite acción |

En M03–M05, un resultado necesita **SHA exacto, comando/workflow, runner, URL, rol/tenant, viewport, OS/browser/lector, pasos reproducibles, resultado positivo/negativo, capturas/árbol accesible y rollback**, con datos sintéticos y sin exportar tokens/PII. La prueba HTTP/MySQL sólo será exigible donde un cambio real toque seguridad/datos; no se inventa cumplimiento a partir de un fixture. Todas estas pruebas están **pendientes**, no se ejecutaron al escribir este documento.

## 4. Clasificación técnica y decisiones que siguen abiertas

- **Frontend:** Angular 22.2.1 standalone/Signals, Material, Zone.js vigente. El código actual admite tests a11y focalizados; no concluir accesibilidad global ni cambiar a zoneless por inferencia.
- **Backend:** ASP.NET Core 10, JWT/RBAC y tenant verificado residen en API; la UI no concede permisos ni elimina necesidad de tests negativos A/B.
- **Persistencia:** MySQL con EF Core 10.0.12 y proveedor declarado; ninguna nueva entidad/migración se introduce en M01.
- **Proveedores e infraestructura:** sin nuevas dependencias, SDK de auditoría, contrataciones, CI, env vars, DNS, QA/main/PROD ni servicios externos.
- **Decisiones abiertas:** población de usuarios/lectores, versión y nivel WCAG aplicables, páginas prioritarias, umbrales de contraste y zoom, tratamientos de tablas complejas, contenido traducido y criterios por tema deben ser definidos y aprobados con evidencia; no inventar nivel AA certificado aquí.

## 5. Matriz A0–A5, dependencias y rollback

- **A0 estática:** diff limitado a este documento y sección autocontenida CHANGELOG; verificar ausencia de secretos, encabezados ## vacíos, atribución cruzada, 12 blobs y números B01–B14.
- **A1 documental:** cotejar 8 P/N futuros y 14 brechas contra contratos M01.01/.02, con resultado "oráculos definidos", **nunca pruebas pasadas**.
- **A2 documental:** verificar límites JWT/RBAC/tenant A/B y PII, interfaz sin autoridad de API, no introducir permisos ni datos.
- **A3 documental:** comparar rutas/código, specs y CSS citados; compilación Angular, Playwright, dotnet, MySQL/Docker **NO_APLICA por diff sólo Markdown**, sin confundir ausencia de prueba con PASS.
- **A4:** publicar por CAS fast-forward en dev y exigir check `verify-solqaryn-project-scope` + `V5 - Sin encabezados vacios ni atribucion cruzada` completed/success sobre SHA publicado, diff y readback Git/PLAN/COLA/BITACORA.
- **A5:** queda PENDIENTE de revisión por Task Javier diferente de la autora; no habilitar QA, main, PROD ni declaratoria WCAG.
- **Rollback:** nuevo commit git revert normal con HEAD revalidado y evento compensatorio BITACORA append-only. La unidad no altera usuario, datos, UI ni runtime. M01.06 y M02 sólo con DAG/A5 causal y evidencias propias.
