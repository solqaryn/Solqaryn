# SOLQARYN V5 — C05-01.M01.02: entradas, salidas, actores y superficies del sistema de diseño

**Task autora:** PRIMARY DEV 06 — JAVIER, `6af16bbad26881918c283abe06e1f32a`. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`) y `PLAN_MAESTRO!A723:O723`. **Repositorio:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Clase de entrega:** M01.02 documental, sin implementar UI ni liberar M01.06.

## Objetivo atómico

Identificar **quién** solicita o consume diseño, **qué** entradas pueden afectar la UI, **qué** salidas se esperan y **dónde** están los límites de confianza. Complementa el perímetro M01.01 y se limita a inventario de interfaces; no sustituye un catálogo visual, auditoría completa de accesibilidad, contratos M02, implementación M03–M04, suites M05 ni pruebas de aceptación M06.

## Fuentes de lectura verificadas (sin asumir pruebas runtime)

| Referencia en `dev` | Blob observado | Superficie comprobada / límite |
| --- | --- | --- |
| `frontend/src/styles.scss` | `2eb48562fd77c758662af6b9683649b4f9698a64` | Tokens de color/superficie/texto, focus-visible, reglas de ancho y estilo global; no garantiza todas las vistas |
| `frontend/src/styles/_enterprise-design-system.scss` | `6a188590af9a958bff2e3e0375c0c46cdb2b14e6` | Escalas de tipografía, espaciado, sombras, movimiento, breakpoints y objetivo táctil `--target-min` |
| `frontend/src/styles/_m10-accessibility.scss` | `6140d41b8a7e1e98f4fa1e43e0ab72f04b6ae29f` | Helpers de sección, scroll y accesibilidad; no significa certificación WCAG automática |
| `frontend/src/app/app.routes.ts` | `2f560c14693455b79ac4546e9333fb74586ef234` | Rutas y guards existentes; no conceden autorización backend |
| `frontend/src/app/shared/confirm-dialog.component.ts` | `f9b73544cc70beba83a891b2fc89537f0718af8b` | Modal Angular Material `MatDialog` con título, mensaje y acciones cancelar/confirmar; no certifica navegación por teclado en todo el producto |
| `frontend/src/app/shared/feedback-state/feedback-state.component.ts` | ruta observada en árbol `dev`, hash no inspeccionado | Componente compartido de feedback; su comportamiento debe verificarse cuando se modifique |
| `frontend/src/app/shared/navigation/app-navigation-menu.component.ts` | ruta observada en árbol `dev`, hash no inspeccionado | Superficie de navegación; no se infiere estado de permisos ni focus management de su contenido |

Las versiones y arquitectura siguen siendo Angular 22.2.1 standalone + Material/CDK, Zone.js activo y backend .NET 10/MySQL/EF, JWT/RBAC. Inventariar rutas y estilos no prueba que una empresa esté autorizada a leer los recursos que muestra.

## Actores y responsabilidades (no concede accesos)

| Actor | Entrada permitida | Salida / frontera obligatoria |
| --- | --- | --- |
| Diseñador/a de producto | Requerimiento de lenguaje visual, estados y patrón | Propuesta revisable de tokens, contraste y componentes; no cambios en permisos |
| Desarrollador/a Angular | Diseño aprobado, contratos de componentes y estado server-side | Implementación futura con pruebas tipadas, lint/build y componentes accesibles |
| Revisor/a accesibilidad / QA | Flujos de teclado, contraste y responsive esperados | Evidencia negativa/positiva y defectos reproducibles; no auto-certificación |
| Responsable de seguridad | Threat boundary de tema por empresa y superficies privadas | Criterios fail-closed para CSS/HTML inyectado, datos y accesos A/B |
| Administrador/a tenant autorizado | Preferencias de marca permitidas por servidor | Tema limitado a claves allowlisted; no puede crear roles, flags de acceso ni grants |
| Usuario final mouse/touch | Navegación, formularios y diálogos | Interacciones entendibles, objetivos táctiles, error/reintento y estado de carga |
| Usuario final teclado/lector | Tab, Enter, Escape, landmarks, labels y anuncios | Foco visible, secuencia lógica y contenido semántico; pruebas reales posteriores |
| Backend/API de permisos | Identity/membership/RBAC desde servidor | Decisión de autorización independiente de color, host, ruta o tema |

El actor `tenant` nunca equivale a una identidad autenticada. Los datos visuales pueden ser públicos o privados; la clasificación y el acceso a datos no se determinan por la clase CSS.

## Entradas, transformaciones y salidas (contratos, no implementación)

| ID | Entrada | Procesamiento futuro seguro | Salida observable |
| --- | --- | --- | --- |
| I01 | Tokens canónicos de color y tipografía en SCSS | Mantener nombres semánticos, contraste por combinación | Texto, botones, feedback y superficies legibles |
| I02 | Escala responsive y ancho de viewport | Adaptar layout/grid; tablas con scroll interno | Página sin overflow horizontal a 320px |
| I03 | Interacción foco/teclado | Tab/Escape/restauración de foco según patrón | Navegación comprobable, foco visible |
| I04 | Estados de campo y validación | Propagar error desde servidor de forma segura, vincular texto/label | Error textual contextual y aria asociadas |
| I05 | Contenido de tablas, listas y carga | Contenedores con min-width 0, feedback de progreso | Datos sin desbordar, carga/vacío/error distinguibles |
| I06 | Solicitud de diálogo destructivo | Exigir confirmación y acción de backend autorizada | Cancelación segura, confirmación solo con permiso real |
| I07 | Marca/tema permitido por tenant | Sanitizar valor, aplicar allowlist y contraste | Solo presentación; JWT/RBAC idénticos antes/después |
| I08 | Preferencia `prefers-reduced-motion` | Reducir transiciones cosméticas | Sin animación obligatoria ni pérdida de información |
| I09 | Navegación de app y guard local | Mostrar rutas según contrato; servidor revalida | 403/404 seguros, sin filtración de datos de tenant B |

Ninguna transformación puede aceptar JavaScript/HTML arbitrario como token ni convertir cliente/host/header en verdad de autorización.

## Fronteras y escenarios verificables futuros

**P01**: token de color y estilo importado sin duplicidad conflictiva. **P02**: formulario con label/ayuda/error textual y foco. **P03**: modal cancela por Escape y restaura foco, si el patrón lo permite. **P04**: 320px, tabla con scroll dentro y teclado operativo. **P05**: tema A permitido no modifica rol ni datos de B.

**N01**: se pierde focus-visible; **N02**: orden Tab salta controles; **N03**: modal deja activo el fondo; **N04**: se comunica error solo por rojo; **N05**: botón bajo objetivo táctil sin excepción; **N06**: overflow horizontal en viewport; **N07**: entrada de tema con HTML/JS; **N08**: tema/host usado para saltar RBAC; **N09**: dato B expuesto en A; **N10**: ruta Angular tomada como autoridad; **N11**: spinner infinito sin estado de error; **N12**: animación obligatoria bajo reduced-motion. Son **oráculos futuros**, NO tests Playwright/Angular o seguridad ejecutados en esta microtarea.

## Dependencias, validación y cierre

`DAG_TECNICO!A684:G684` contiene los predecesores documentales `C00-03.M01.01–05`, `C01-01..04.M01.01–05`, `C02-01..04.M01.01–05`, con `F=VALIDADO/G=SI` al claim. `PLAN_MAESTRO!H723=PENDIENTE/I723` vacío, `COLA!G723/O723=SI` y ownership mediante CAS autorizan **solo este inventario**. El inventario no sustituye validación causal de gates posteriores ni habilita cambios productivos.

**A0:** revisar árbol/fuentes y diff documental; **A1:** comparar actores, I01–I09, P01–P05 y N01–N12 como especificaciones, no resultados runtime; **A2:** preservar tenant A/B, autorización server-side y no inyección de tokens; **A3:** coherencia con M01.01, rutas/SCSS y fuente V5; **A4:** publicar fast-forward con SHA exacto, comprobar `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` en completed/success y registrar PLAN/COLA/BITACORA. CI .NET/Angular/MySQL de ejecución no necesaria para documento puro: `NO_APLICA` motivado, nunca PASS. **A5:** revisión por otra Task Javier; el autor no se revisa.

Reversión: `git revert` normal del commit documental sobre `dev`; nunca force-push ni cambio en QA/main/PROD. No autoriza despliegue, eventos, webhooks, nuevas Tasks, código ni migraciones.
