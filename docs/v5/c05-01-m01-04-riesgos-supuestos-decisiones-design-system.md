# SOLQARYN V5 — C05-01.M01.04: riesgos, supuestos y decisiones del sistema de diseño

**Alcance:** registro documental de preparación M01.04 del lote **C05-01 — Design tokens y componentes**, autorizado por PLAN_MAESTRO fila 725 y STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`). **Autor:** PRIMARY DEV 03 JAVIER, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Repositorio/rama:** `solqaryn/Solqaryn@dev`; **PROJECT_SCOPE_LOCK=STRICT**; **fecha:** 2026-11-14.

Este registro identifica riesgos **potenciales**, supuestos **por verificar** y decisiones **pendientes de responsable competente**. No constituye hallazgo de vulnerabilidad confirmado, aprobación de política, certificación WCAG, implementación Angular, aceptación de diseño ni permiso para QA/main/PROD. No se ejecutaron pruebas E2E, xUnit ni migraciones para este cambio Markdown.

## Base causal del análisis (A0 y A3 documentales)

- `C05-01.M01.01` (`docs/v5/c05-01-m01-01-objetivo-no-alcance-design-tokens.md`, blob `ea7c5e7d339838f021b3f746fd9a7fe307b58b8d`): doce invariantes NR01–NR12; cinco positivos P01–P05 y doce negativos N01–N12 **futuros**.
- `C05-01.M01.02` (`docs/v5/c05-01-m01-02-entradas-salidas-actores-superficies.md`, blob `bd2976610247668f5e638f6fff38c83c4d54a143`): ocho actores, entradas I01–I09, límites de confianza, cinco positivos y doce negativos como especificaciones.
- `C05-01.M01.03` (`docs/v5/c05-01-m01-03-contratos-codigo-evidencia-design-system.md`, blob `a7f5ed92b80ef68c8957af27e82c1aedbd39ed2a`): manifiesto F01–F09, ocho dimensiones de brechas y oráculos no ejecutados.
- Nueve blobs del inventario F01–F09 fueron **releídos por contenido GitHub** sobre el HEAD previo a esta entrega: `frontend/src/styles.scss` `2eb48562`, `_enterprise-design-system.scss` `6a188590`, `_m10-accessibility.scss` `6140d41b`, `app.routes.ts` `2f560c14`, `confirm-dialog.component.ts` `f9b73544`, `feedback-state.component.ts` `7deb4047`, `app-navigation-menu.component.ts` `b70ef4ff`, `theme-applier.service.ts` `cbccc279` y `theme-applier.service.spec.ts` `99529c4f`. Los nueve hashes **coincidieron** con M01.03; los archivos no fueron modificados.
- Arquitectura: Angular 22.2.1 standalone, Material/CDK, Zone.js activo; .NET 10/API y MySQL/EF como límites de seguridad. Los guards, tokens CSS y branding nunca sustituyen autorización JWT/RBAC/tenant del servidor.

## Registro R01–R12 de riesgos a comprobar (no vulnerabilidades acreditadas)

| ID | Riesgo condicionado / superficie | Señal de prueba futura | Mitigación o NO_GO propuesto |
| --- | --- | --- | --- |
| R01 | Overrides de tokens divergentes entre vistas o componentes (F01/F02) | Mismo estado con valores contradictorios; inventario token→consumidor faltante | Mapear tokens semánticos, justificar excepciones, regresión visual multi-ruta |
| R02 | Contraste insuficiente por tema o estado (F01/F08) | Pares texto/fondo y foco no alcanzan umbral aprobado | Matriz de contrastes por tema/estado, revisión humana y automática antes de publicar |
| R03 | Pérdida de foco visible o tabulación tras overlays (F03/F05) | Tab/Shift+Tab/Escape no devuelven foco al disparador | Pruebas Playwright/teclado; NO_GO para diálogos bloqueantes sin control de foco |
| R04 | Desbordamiento horizontal en 320px fuera de tablas (F01/F02) | Scroll horizontal a nivel documento, controles ocultos | Pruebas 320/768/1280; contenedores fluidos y scroll local a tablas |
| R05 | Estado de error, vacío o carga incomprensible sin texto/ARIA (F06) | Error señalado solo por color o aria-live silencioso | Mensaje textual y recuperación; verificar tecnologías asistivas en M05/M06 |
| R06 | Objetivos táctiles demasiado pequeños o densidad excesiva (F02) | Interacciones por debajo de `--target-min:44px` sin razón y alternativa | Medir componentes por estado, justificar excepciones; no imponer controles ilegibles |
| R07 | Motion obligatorio que dificulta uso (F02) | `prefers-reduced-motion` sin degradación o foco oculto por animación | Reducir motion no esencial y verificar que ningún contenido dependa de efectos |
| R08 | Inyección de CSS/HTML/JS desde tokens de marca no allowlisted (F08) | Valor fuera del patrón hexadecimal/DTO esperado altera estructura o ejecución | Allowlist estricta de claves/valores, rechazo backend y no HTML libre |
| R09 | Fuga visual de marca A al cambiar tenant a B (F08) | Variables antiguas siguen vigentes con respuesta tardía de A | Limpiar tokens antes de aplicar B; prueba A→B y respuestas fuera de orden |
| R10 | Permiso deducido del menú Angular o del tema (F04/F07) | Acceso HTTP a recursos de B al usar ruta/host manipulado | Autorización server-side con JWT/RBAC y pruebas A/B; NO_GO si UI concede autoridad |
| R11 | Cambios globales de estilos rompen formularios, tablas o modales existentes (F01/F05/F06) | Regresión por consumidor sin rollback ni baseline | Diff focalizado, catálogo incremental y CI/UI específica, revert ordinario |
| R12 | Presencia de tests confundida con tests ejecutados (F09) | Reporte PASS sin run, SHA, logs y caso real | Exigir CI pertinente en cambio runtime; registrar NO_EJECUTADO hasta obtener evidencia |

Prioridad propuesta para preflight de implementación: **R08–R10** por límites de confianza y aislamiento; **R02–R05** por accesibilidad y uso básico; **R01/R06/R07/R11/R12** por consistencia y certificación. Es orden de análisis, **no** un gate automático que sustituya DAG ni una autorización de despliegue.

## Supuestos falsables S01–S08 (PENDIENTES, no hechos certificados)

| ID | Supuesto de diseño por confirmar | Evidencia/criterio para confirmarlo |
| --- | --- | --- |
| S01 | Los tokens SCSS existentes pueden servir de fuente canónica | Inventario de consumidores y duplicidades por componente |
| S02 | La escala `--target-min` puede aplicarse sin romper densidad operativa | Medición de objetivos interactivos, tablas y menú en vistas reales |
| S03 | Los colores allowlisted por servicio de tema permiten contraste suficiente | Matriz AA de pares por tema/estado y ajustes de foreground |
| S04 | El layout conserva usabilidad con 320px y zoom de texto | Capturas, medidas y tests E2E en anchos y zoom representativos |
| S05 | MatDialog mantiene foco correcto en cada flujo destructivo | Secuencia Tab/Escape/Confirm/Cancel y devolución de foco |
| S06 | El feedback semántico cubre todos los caminos loading/error/empty | Pruebas de lectura, mensajes y reintento por consumidor |
| S07 | Cambiar tenant limpia el tema anterior incluso con peticiones tardías | Test de carrera A→B, errores HTTP y tokens removidos |
| S08 | Los guards son solo conveniencia UX y el API conserva autorización | Pruebas backend de rol/revocación/tenant B y rutas manipuladas |

Ninguno de estos supuestos puede marcarse `VALIDADO` por la mera existencia de archivos o por el documento M01.

## Decisiones D01–D08 pendientes de autoridad (NO se aprueban aquí)

| ID | Decisión concreta que falta | Responsable funcional/técnico propuesto | Evidencia mínima antes de decidir |
| --- | --- | --- | --- |
| D01 | Qué escala tipográfica y tokens son normativos vs excepciones permitidas | Diseño + frontend | Catálogo token→consumidor y contraste |
| D02 | Umbral de contraste/estados y tratamiento de temas inviables | Diseño accesible + seguridad | Matriz de estado/tema y rechazo fail-closed |
| D03 | Patrón uniforme de tablas grandes, zoom y 320px | Diseño + frontend | Layouts y capturas con scroll local |
| D04 | Contrato de foco para modales/menús complejos | Accesibilidad + frontend | Prueba teclado/AT por tipo de overlay |
| D05 | Allowlist y origen confiable de tokens de tenant | Seguridad + backend | Validación DTO, límites de confianza A/B, pruebas negativas |
| D06 | Estrategia de motion y reduced-motion por componente | Diseño + accesibilidad | Matriz de animaciones y fallback verificable |
| D07 | Qué componentes integran el catálogo de regresión primero | Producto + QA + frontend | Inventario de pantallas críticas y riesgos R01–R12 |
| D08 | Condiciones de rollback/release para cambio visual transversal | Dueño del scope + QA + seguridad | Diff, CI exact SHA, degradación reversible y revisión A5 |

El registro deja todas las decisiones en **PENDIENTE**; ningún aprobador fue suplantado, ni se creó una cola de aprobación paralela.

## Oráculos de seguimiento documental A1/A2 (no pruebas runtime ejecutadas)

**Positivos a instrumentar en fases autorizadas:** P01 token único y excepciones explícitas; P02 contraste y foco en dos temas; P03 vista de 320px con tablas locales y formularios; P04 modal con cancelación y foco restaurado; P05 tenant A→B elimina marca antigua y backend sigue denegando datos B.

**Negativos a instrumentar:** N01 token duplicado; N02 contraste ilegible; N03 foco perdido; N04 modal que deja fondo activo; N05 viewport con overflow; N06 error por solo color; N07 control sin nombre accesible; N08 motion obligatorio; N09 CSS/HTML arbitrario en tema; N10 contaminación de tema entre empresas; N11 ruta/guard como único control RBAC; N12 reporte PASS de spec no ejecutada.

Para M01.04 **A1** se limita a consistencia entre R/S/D, NR01–NR12 y oráculos; **A2** comprueba que el registro no flexibiliza aislamiento tenant, permisos, sanitización de tema ni gestión de secretos; **A3** coteja fuentes F01–F09 y M01.01–03. Estas verificaciones documentales no certifican una implementación real.

## DAG, salida y rollback

Entrada autorizada solo si `PLAN_MAESTRO!H725=PENDIENTE/I725` vacío, `COLA!G725/O725=SI`, `DAG_TECNICO!F686=VALIDADO/G686=SI`, claim CAS único y predecesores de preparación C00-03/C01/C02 M01.01–05. Una vez reclamado, la única entrega aquí es **R01–R12, S01–S08 y D01–D08 documentadas**: M01.05 define aceptación y M01.06 determina cierre de lote separadamente. No liberar M02/M03, IAM, DB, QA, PROD ni secreto alguno.

**A4 de publicación:** commit fast-forward a `dev` con CHANGELOG autocontenido, controles exact SHA `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` en `completed/success`, BITACORA nueva A:Z, lectura PLAN/COLA y evidencia del SHA. Tests Angular, .NET, MySQL y Playwright: **NO_APLICA al diff exclusivamente Markdown**, no declararlos PASS. **A5:** debe revisarla otra de las cinco Tasks Javier distinta del autor. Si CI obligatoria sigue pending o falla, mantener VALIDANDO, sin falsificar LISTO. **Rollback:** `git revert` normal del commit documental; no reset ni force-push. Sin cambios en Angular, API, base de datos, branding real, permisos, cuentas, Tasks, schedules, main/QA/PROD ni servicios de pago.
