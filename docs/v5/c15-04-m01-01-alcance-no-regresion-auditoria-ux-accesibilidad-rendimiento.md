# SOLQARYN V5 — C15-04.M01.01: alcance y no-regresión de UX, accesibilidad y rendimiento

**Autor:** PRIMARY DEV 03 JAVIER; TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Fecha:** 2026-11-14, hora local de Honduras. **Ámbito:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT. **Clasificación:** preparación documental M01.01, no auditoría ejecutada.

## Objetivo y resultado acotado

Fijar el perímetro verificable de la auditoría transversal de **ligereza visual, diseño responsive, accesibilidad, navegación, landing pública, portal empresarial, superficies privadas tipo ERP y builder futuro**. El éxito de una auditoría posterior exige inventario de flujos representativos, pruebas de escritorio y móvil, teclado/lector asistivo cuando corresponda, métricas de carga, defectos reproducibles, correcciones y retesteo documentado.

**Salida concreta de M01.01:** objetivos, exclusiones, catorce invariantes de no-regresión, ocho pares de oráculos de aceptación y mapa de evidencia pendiente. Este artefacto no demuestra cumplimiento WCAG ni metas de Core Web Vitals y no declara que el builder, el portal completo o la landing universal estén implementados.

Fuente ejecutable: STANDARD V5 FINAL de Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, PLAN_MAESTRO fila 2270; COLA G/O=SI y DAG_TECNICO F=VALIDADO/G=SI en el preflight. El gate del DAG permite **documentación de M01**, nunca bypass de M02–M06, despliegues o QA/main/PROD.

## Línea base inspeccionada en el HEAD anterior (evidencia estática, NO runtime)

| Fuente | Blob verificado | Qué acredita y qué NO |
| --- | --- | --- |
| `frontend/package.json` | Contrato de Angular 22.2.1 / Material/CDK y scripts | Versión declarada; no acredita build ni experiencia completa |
| `frontend/src/styles.scss` | `2eb48562fd77c758662af6b9683649b4f9698a64` | Tokens, colores y uso de hojas de estilos; no valida contraste real |
| `frontend/src/styles/_enterprise-design-system.scss` | `6a188590af9a958bff2e3e0375c0c46cdb2b14e6` | Escalas de tipografía, espaciado y componentes; no cubre todas las vistas |
| `frontend/src/styles/_m10-accessibility.scss` | `6140d41b8a7e1e98f4fa1e43e0ab72f04b6ae29f` | Skip links y geometría de foco declarados; no garantiza navegación de teclado |
| `frontend/src/app/app.routes.ts` | `2f560c14693455b79ac4546e9333fb74586ef234` | Rutas lazy y guards Angular; no acredita que UI reemplace RBAC en API |
| `frontend/playwright.config.ts` | `87946080e7fe87f5fa9e9c8d4c8da8a36f49d5da` | Configuración de Playwright; no certifica tests ejecutados |
| `docs/v5/c13-02-m01-01-alcance-no-regresion-accesibilidad.md` | `7625e8638e3b228f8773ffe11541b0299228fda9` | Contrato previo de accesibilidad, sin auditoría WCAG real |
| `docs/v5/c05-01-m01-01-objetivo-no-alcance-design-tokens.md` | `ea7c5e7d339838f021b3f746fd9a7fe307b58b8d` | Contrato de design tokens, no mapa de cumplimiento por pantalla |

La procedencia de un archivo estático no equivale a una captura, medición de rendimiento, sesión de lector de pantalla o prueba E2E. Angular 22.2.1 usa Zone.js 0.16.3; no se cambia a zoneless. Seguridad tenant/JWT/RBAC continúa en .NET 10 backend, sin excepción visual.

## Dentro del perímetro futuro y exclusiones explícitas

**Dentro de futuras fases (por auditar):** landing institucional, storefront público, login/onboarding, navegación autenticada, formularios, tablas, filtros, carrito/checkout cuando existan, portales, flujos de error/loading/empty, patrón responsive, componentes de marca tenant con allowlist, contraste, teclado, foco, tecnología asistiva, motion reducido, payloads y respuesta percibida con red y dispositivos de prueba.

**Fuera de M01.01:** editar Angular, SCSS, HTML, Angular Material, API .NET, permisos, base de datos MySQL, JWT/RBAC, migraciones, worker, builder ejecutable, contenido de marcas reales, DNS/CDN, Aiven/Vercel/Render productivo, Cloudinary/R2, eventos/webhooks, Playwright real, cumplimiento legal, métricas inventadas o pagos. Ni M01.01 ni una revisión A5 abren QA/main/PROD.

## Catorce invariantes de no-regresión (contratos pendientes de ejecución)

| ID | Condición a preservar | Oráculo negativo que deberá detectarla |
| --- | --- | --- |
| NR01 | Navegación primaria y CTA tienen jerarquía reconocible en móvil y escritorio | CTA perdido, superposición o estado confuso |
| NR02 | Texto, botones y formularios reacomodan contenido sin corte a 320 px y anchuras mayores definidas | Scroll horizontal de página y contenido inaccesible |
| NR03 | Controles interactivos funcionan con teclado, Tab/Shift+Tab y Enter/Espacio | Bloqueo de foco, trampa fuera de modal o acción sólo mouse |
| NR04 | Foco tiene contraste/estilo visible y regresa al origen al cerrar diálogo | Foco invisible/perdido tras overlay |
| NR05 | Formulario muestra label, instrucciones, error textual y estado no sólo cromático | Error no anunciado o campo sin nombre accesible |
| NR06 | Elementos e imágenes útiles poseen semántica y alternativas adecuadas | Icono único ambiguo o imagen crítica sin descripción |
| NR07 | Motion reducido desactiva animaciones no esenciales sin ocultar estados | Movimiento intrusivo pese a preferencia del usuario |
| NR08 | Cargas, errores y estados vacíos muestran contexto y posibilidad de recuperación | Spinner indefinido, error silencioso o CTA muerto |
| NR09 | Tipografía/espaciado/tokens preservan consistencia en vistas públicas y privadas | Personalización rompe legibilidad o densidad |
| NR10 | Tablas y listas amplias usan scroll de contenedor, filtros accesibles y límites de render | Tabla desborda toda la pantalla o bloquea interacción |
| NR11 | Navegación por rutas preserva control de acceso real en backend | UI client-side aparenta conceder recurso de tenant B |
| NR12 | Tema/branding de tenant A no expone datos o recursos privados de B | Reutilización de caché/estado muestra marca o PII de B |
| NR13 | Páginas priorizan carga progresiva y evitan dependencias bloqueantes evitables | Experiencia queda inutilizable con red degradada acordada |
| NR14 | Versiones, diseños y métricas se evalúan sobre commit y fixture reproducibles | Captura, prueba o cifra pertenece a otro SHA o sesión |

Todos NR01–NR14 tienen **ESTADO FUTURO_NO_VERIFICADO**. No deben contarse como pruebas ejecutadas por el simple hecho de estar escritos.

## Ocho pares de escenarios observables futuros — NO_EJECUTADOS

| Caso | Positivo a medir en fixtures | Negativo bloqueante |
| --- | --- | --- |
| P01/N01 | Menú, foco y CTA funcionan a 320 px y escritorio | Contenido importante no entra en viewport |
| P02/N02 | Modal y formulario se completan por teclado | Escape/foco atrapado o validación sin texto |
| P03/N03 | Mensaje de error y recuperación se anuncian | Error sólo por color, animación o consola |
| P04/N04 | Tabla usa scroll interno y mantiene filtros utilizables | Scroll horizontal del documento bloquea navegación |
| P05/N05 | Preferencia reduced-motion se respeta | Transición esencial oculta contexto al desactivarla |
| P06/N06 | Carga progresiva se evalúa con red y dispositivo fijados | Métrica sin baseline, muestra o ventana de medición |
| P07/N07 | Tenant A ve contenido público/privado delimitado por autorización | Tenant B se revela por tema, ruta, caché o datos |
| P08/N08 | Ruta pública existente se distingue de builder futuro | Se etiqueta capacidad no implementada como PASS |

Las futuras métricas (tiempo de carga, tamaños, errores, contraste y estabilidad visual) deben definir **umbral, dispositivo, viewport, red, fixture y fecha** antes de medirse. No se asignan umbrales cuantitativos sin fuente aprobada, ni se certifica WCAG sin auditoría.

## Secuencia de auditoría, puertas y rollback

M01.02 identifica actores e interfaces; M01.03 inventaría pantallas, componentes y evidencia; M01.04 fija riesgos y decisiones; M01.05 define los criterios medibles; M01.06 sólo cierra con sus dependencias, A0–A4 y A5 independiente exigible. No suponer elegibilidad nueva ni acceso productivo por fase documental.

Ejecución futura: matriz ruta→viewport→dispositivo→criterio→resultado, captura y traza con datos sintéticos, prueba positiva/negativa Playwright/teclado y revisión humana donde la herramienta no basta; registrar regresiones, responsable, corrección, SHA, logs y rollback. Las pruebas deben preservar aislamiento tenant A/B y autorización server-side.

**Cambios efectivamente cubiertos por esta unidad:** este Markdown y sección autocontenida de `CHANGELOG_AI.md`. A0–A3 documentales revisan alcance, coherencia, seguridad y trazabilidad de fuentes; A4 exige checks Scope Lock y procedencia V5 exact-SHA. Tests Angular, Playwright, MySQL y HTTP no se ejecutan por este diff, por lo que sólo procede `NO_APLICA_RUNTIME_DOCUMENTAL`, nunca un PASS inventado. A5 requiere otra Task Javier. Rollback: `git revert` ordinario sin reescritura ni QA/main/PROD.
