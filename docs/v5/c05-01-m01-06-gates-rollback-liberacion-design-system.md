# SOLQARYN V5 — C05-01.M01.06: bloqueo, reversión y liberación condicionada del sistema de diseño

**Autor:** SOLQARYN PRIMARY DEV 05 — JAVIER (`6adc2e55f47c81919f6374e561687076`). **Fecha:** 2026-11-15. **Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Clase:** documento terminal de preparación M01; no implementa tokens, componentes, pruebas E2E ni certifica accesibilidad.

## 1. Alcance autorizado y procedencia de la reclamación

La fuente única de implementación es el documento `STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL` (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`). `PLAN_MAESTRO!A727` define `C05-01.M01.06` como «Definir bloqueo, rollback y condición de liberación del lote». El objetivo final de fase C05-01 es un catálogo coherente de tipografía, color, espaciado, estados, formularios, tablas, modales y navegación, con contraste y diseño responsive acreditados mediante pruebas específicas de runtime en fases posteriores. Esta entrega M01.06 sólo fija el contrato de esa verificación, no la realiza.

El claim se acreditó con `PLAN H727=PENDIENTE`, `I727` vacío, `COLA G727/O727=SI` y `DAG_TECNICO A688=C05-01.M01.06, F688=VALIDADO/G688=SI`; luego H pasó a `EN_PROGRESO` por CAS único e I a esta Task Javier, con readback. El DAG exige `C05-01.M01.01–.05` LISTO con A5 independiente y `C00-03.M01.06` LISTO/A5. Las revisiones A5 pendientes de `C05-01.M01.04` y `.05` se completaron en esta misma corrida por Task distinta de su autora; las restantes ya estaban acreditadas. A5 permite **reclamar este documento**, nunca certificar UI real ni autorizar QA/main/PROD.

## 2. Fuentes históricas, código existente y huecos por verificar

| Fuente verificada | Evidencia documental identificable | Límite de atribución |
| --- | --- | --- |
| `C05-01.M01.01` | `docs/v5/c05-01-m01-01-objetivo-no-alcance-design-tokens.md`, blob `ea7c5e7d339838f021b3f746fd9a7fe307b58b8d`; NR01–NR12 | No prueba contraste por ejecución |
| `C05-01.M01.02` | `docs/v5/c05-01-m01-02-entradas-salidas-actores-superficies.md`, blob `bd2976610247668f5e638f6fff38c83c4d54a143`; 8 actores, 9 entradas | No valida UX en dispositivo real |
| `C05-01.M01.03` | `docs/v5/c05-01-m01-03-contratos-codigo-evidencia-design-system.md`, blob `a7f5ed92b80ef68c8957af27e82c1aedbd39ed2a`; F01–F09 | Nueve rutas inspeccionadas no equivalen a nueve tests ejecutados |
| `C05-01.M01.04` | `docs/v5/c05-01-m01-04-riesgos-supuestos-decisiones-design-system.md`, blob `5f566142acb7a41d7dfc3b9a8e5dbf7e41975ddb` | R01–R12 son riesgos potenciales, S01–S08 falsables y D01–D08 **pendientes** |
| `C05-01.M01.05` | `docs/v5/c05-01-m01-05-criterios-aceptacion-design-system.md`, blob `875e6929d0f166e70192f1b6a441997f7bcefb6c` | AC01–AC14/N01–N14 son oráculos FUTUROS_NO_EJECUTADOS |
| `C00-03.M01.06` | Terminal M01 anterior LISTO/A5 distinto; ámbito documental de gobierno | No permite omitir Scope Lock ni dependencias específicas |

Las capas existentes identificadas en F01–F09 son `frontend/src/styles.scss`, parciales SCSS empresariales/accesibilidad, `app.routes.ts`, diálogo de confirmación, componente de feedback, menú, `theme-applier.service.ts` y su spec. Angular 22.2.1 usa Material/CDK, Signals/guards y Zone.js; la autoridad para sesiones, JWT/RBAC y tenant sigue en ASP.NET Core 10 del backend. El tema visual **no otorga permiso backend**. Los blobs citados se deben releer si cambia el código o sus dependencias antes de probar o aprobar una futura fase M02–M06.

## 3. Dieciséis barreras BL01–BL16 por ámbito y prueba causal

| Bloqueo | Condición específica necesaria en cambio de runtime futuro |
| --- | --- |
| BL01 | Catálogo versionado token semántico→consumidor sin overrides invisibles ni doble definición contradictoria |
| BL02 | Contraste de texto/fondo/foco medido por estado, tema y variante; no asumir WCAG AA por observar CSS |
| BL03 | Teclado Tab/Shift+Tab/Escape conserva orden y foco visible, incluido restore de diálogo |
| BL04 | Formularios con label, ayuda, error textual, aria-busy/live y recuperación; no depender de color únicamente |
| BL05 | 320px sin overflow global del documento, con scroll local a tablas; ensayar 768px/1280px y zoom |
| BL06 | Modal mantiene foco, bloquea fondo cuando corresponde y ofrece cancelar/revertir |
| BL07 | Objetivos táctiles medidos contra criterio adoptado, excepciones justificadas sin empeorar legibilidad |
| BL08 | `prefers-reduced-motion` elimina motion cosmético obligatorio y conserva estado/acción accesibles |
| BL09 | Tokens de marca vienen de DTO/allowlist estricta; CSS/HTML/JS arbitrario de tenant rechazado |
| BL10 | Cambio tenant A→B limpia identidad/estilos de A, incluso bajo respuestas HTTP fuera de orden |
| BL11 | API valida JWT, RBAC y tenant A/B sin confiar en menú, guard, theme, slug u Host |
| BL12 | Errores de autorización, conexión, carga y estado vacío no revelan información de B ni inventan éxito |
| BL13 | Regresión de componentes críticos y catálogo inicial de pruebas aprobados por responsables según D07 |
| BL14 | D01–D08 quedan decisiones abiertas hasta autoridad real: tokens, contrastes, patrones, foco, sanitización, motion, prioridades y rollback |
| BL15 | Para código Angular: build, tipado, tests de estado/tema y Playwright cuando corresponda, logs reales en SHA causal |
| BL16 | Scope Lock STRICT, CI required exact-SHA SUCCESS, BITACORA/PLAN/COLA, A0–A4 y A5 independiente; nuevas autorizaciones para QA/main/PROD |

**NO_GO específico:** fallar BL02–BL12 impide declarar esa interfaz segura/accesible; una fase M01.06 documental **no** afirma que los 16 BL aprobaron en runtime. Un dashboard verde o spec estático no sustituye test ejecutado, y `queued/in_progress/skipped/cancelled/failed` no es PASS requerido.

## 4. Matriz de aceptación/denegación FUTURA por consumidor

| Componente | Positivo a ejecutar posteriormente | Caso negativo a exigir |
| --- | --- | --- |
| Tipografía/espacio | Token semántico, zoom y jerarquía sin truncamientos | Tamaño fijo causa pérdida de acciones o doble token divergente |
| Color/tema | Contraste medido por dos temas y estados | Texto o foco ilegible, color libre fuera de allowlist |
| Feedback de carga | Mensaje semántico accesible, reintento y estado estable | Spinner infinito, error sólo por color o `aria-live` incorrecto |
| Diálogo destructivo | Cancelar conserva datos y restaura foco; confirmar exige permiso real | Escape imposible, foco en fondo o acción sin autorización |
| Formulario | Label/ayuda/error vinculado y validación no intrusiva | Campo sin nombre, error no anunciado o validación permisiva |
| Tabla | 320/768/1280px, scroll horizontal local, teclado | Scroll del body o controles inaccesibles por overflow |
| Menú/routing | Navegación accesible con `aria` y roles correctos | Guard visual concede acceso HTTP no autorizado |
| Branding multiempresa | A→B elimina tokens de A, cancela respuestas obsoletas | CSS de A permanece en B o server devuelve datos B |
| Motion | Reduced-motion sin pérdida de información | Animación requerida para comprender/focalizar una acción |
| Publicación segura | Pruebas dirigidas, Scope Lock y A5 independiente en SHA | Confundir documento, test skipped o CI cancelada con PASS |

Estos son **ejemplos de diseño de test, no Playwright/Vitest/xUnit ejecutados** durante esta preparación.

## 5. Ocho acciones de reversión REV01–REV08

| Trigger futuro | Acción correctiva condicionada |
| --- | --- |
| REV01 — pérdida de contraste/accesibilidad | Detener promoción y revertir CSS mínimo con evidencia y pruebas |
| REV02 — cambio de tenant contamina tema | Limpiar tokens, invalidar respuestas tardías, preservar aislamiento backend |
| REV03 — token no tipado produce inyección | Rechazar antes de aplicación, bloquear clave y corregir allowlist |
| REV04 — foco modal bloqueado o perdido | Revertir control focal y revalidar teclado/cancelación |
| REV05 — 320px/zoom pierde funcionalidad | Limitar overflow al contenedor afectado, no ocultar controles |
| REV06 — guard UI aparenta permiso | Corregir backend/denegación; nunca emitir grants desde front |
| REV07 — CI real o regresión visual falla | Mantener VALIDANDO, leer logs exact-SHA, reparar y reintentar sólo con nueva hipótesis |
| REV08 — dependencia, Scope Lock o A5 falla | NO_GO al sucesor afectado, conservar owner/estado y no manipular fórmulas G/O |

El **rollback de este documento únicamente** es `git revert` ordinario del commit Markdown+sección completa `CHANGELOG_AI.md` en `dev`, respetando cambios concurrentes; no borrar BITACORA ni hacer force-push. No se cambia UI ejecutable, JWT/RBAC, datos, proveedores, Tasks físicas, QA/main/PROD o infraestructura por la reversión hipotética.

## 6. A0–A5 causal y contrato de salida

**A0:** verificar exactamente una nueva nota Markdown y su sección CHANGELOG autocontenida, fuentes NR/I/F/R/S/D/AC y PLAN/COLA/DAG del ID. **A1:** comprobar consistencia documental entre 16 barreras, diez pares positivos/negativos y ocho reversas futuras; ningún resultado real Angular/Playwright se acredita. **A2:** documento conserva aislamiento A/B, sanitización CSS, RBAC backend, no regresión, privacidad y accesibilidad como condiciones fail-closed. **A3:** conexión conceptual a Angular/Material/SCSS, Domain/API .NET y M01.01–05 sin modificar interfaces o migraciones; builds no pertinentes `NO_APLICA` con causa de diff sólo Markdown. **A4:** publicación fast-forward/CAS directa en `dev`, commit/blobs exactos, `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` sobre SHA publicado, readbacks PLAN/COLA/BITACORA; nunca atribuir PASS a CI general cancelada. **A5_PENDIENTE:** revisión real por otra Task Javier distinta de la autora antes de habilitar dependencias terminales que la requieran.

**Condición de avance:** `LISTO_DEV` documental sólo completa contrato de preparación. `C05-01.M02.01` y demás sucesoras se reclaman únicamente cuando el DAG/PLAN/COLA vivientes acrediten todas sus puertas, incluido A5 que corresponda. Ninguna nueva autorización de despliegue, políticas de identidad, tenant, cambios de base de datos o QA/PROD nace de este Markdown.
