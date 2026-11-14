# SOLQARYN V5 — C05-04.M01.03: inventario de contratos, código y evidencia sobre ligereza

**Autor:** SOLQARYN PRIMARY DEV 01 / JAVIER_CHATGPT, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Fecha:** 2026-11-14. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, documento `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, PLAN_MAESTRO fila 832. **Ámbito:** `solqaryn/Solqaryn@dev` y `PROJECT_SCOPE_LOCK=STRICT`.

## Objeto y alcance causal

La unidad M01.03 **lee** el estado de código/configuración existente, no lo cambia. Distingue patrones ya declarados (lazy routing, preload selectivo, tokens SCSS, estados accesibles) de objetivos de menor carga inicial, menos HTTP duplicado, menor densidad y mejores Core Web Vitals que **todavía requieren medición y pruebas**. Es inventario, no auditoría de rendimiento certificada ni detección causal de fallos de producción. M01.01 definió NR01–NR12 y M01.02 actores, superficies y P01–P06/N01–N12 futuros. Este documento no cierra M01.04–M01.06 ni habilita M02/M03.

## Manifiesto de nueve archivos de fuente inspeccionados

| ID | Ruta en dev | Blob Git leído | Hallazgo limitado |
| --- | --- | --- | --- |
| F01 | `docs/v5/c05-04-m01-01-objetivo-no-alcance-ligereza-percepcion.md` | `a9b7219c7094b8b162f53117a2c67d2c86dd7c21` | Objetivo y NR01–NR12, sin benchmark |
| F02 | `docs/v5/c05-04-m01-02-actores-interfaces-ligereza-percepcion.md` | Revalidar contra HEAD publicado de M01.02 | Actores/entradas futuras; no runtime |
| F03 | `frontend/angular.json` | `8b00f8fee80fdfafe1864e233be7d5f20ad444e3` | Presupuesto initial warning 650 kB/error 750 kB y estilos por componente 16/32 kB; configuración, no peso del bundle medido |
| F04 | `frontend/src/app/app.config.ts` | `cd77384e713014015d83b348955b3d83573348b3` | `provideZoneChangeDetection({ eventCoalescing: true })`, router con estrategia selectiva, interceptor auth y XHR; Zone.js sigue activo |
| F05 | `frontend/src/app/app.routes.ts` | `2f560c14693455b79ac4546e9333fb74586ef234` | `loadComponent`, auth/permiso/plataforma guards; su presencia no demuestra latencia de rutas |
| F06 | `frontend/src/app/core/performance/after-render-selective-preloading.strategy.ts` | `a5083eac7fd882430c9550c037c17f89cb946149` | Solo precarga marcada tras `isStable`, ruta `/tienda` y `timer(250)`; no certificar efecto sin traza |
| F07 | `frontend/src/styles/_enterprise-design-system.scss` | `6a188590af9a958bff2e3e0375c0c46cdb2b14e6` | Tokens de espaciado, geometría, `--target-min:44px`, responsive y movimiento; no cobertura total de vistas |
| F08 | `frontend/src/styles/_m10-accessibility.scss` | `6140d41b8a7e1e98f4fa1e43e0ab72f04b6ae29f` | Foco, grids, truncación/overflow, reduced-motion, forced colors; no prueba visual/teclado |
| F09 | `frontend/src/app/shared/feedback-state/feedback-state.component.ts` | `7deb40475f0c492d09e649e7351b2915bd53d356` | `loading/empty/success/error/warning` y `aria-live`; no asegurar adopción en todas las vistas |
| F10 | `frontend/package.json` | `e6d030f643421cb343cc12617e742469e88223c4` | Scripts de build/lint/test/Playwright; **declarados**, no ejecutados por M01.03 |

Los hashes de F01 y F03–F10 se verificaron de forma estática; F02 corresponde a la entrega propia inmediatamente anterior y debe cotejarse con el SHA `c4cc3d8699282b50eaabe539849dd4c09fb83c9f`. Un source blob no es evidencia de funcionalidad probada, cobertura de producción ni ausencia de incidencias en consumidores no inspeccionados.

## Diferencias entre capacidad existente y evidencia requerida

| Brecha de evidencia | Hecho actual demostrado | Prueba necesaria después |
| --- | --- | --- |
| G01 Bundle emitido | `angular.json` fija presupuestos, no tamaño real | `npm run build:prod` con artefacto, bytes/chunks, SHA y regresión |
| G02 Métricas de percepción | No se leyó traza Lighthouse/Playwright ejecutada en esta unidad | LCP/INP/CLS con red, dispositivo, caché y ruta reproducibles |
| G03 Repetición de HTTP | Interceptor declarado, sin medición de llamadas por flujo | Contar requests por navegación/cambio tenant, detectar cancelación/refetch |
| G04 Aislamiento entre tenants | Guards Angular existen, no sustituyen API | Tests A/B con sesión, respuesta y caché por empresa y cambio A→B |
| G05 Preload selectivo | Estrategia tiene predicados concretos | Verificar orden y bytes de chunks en navegador, sin interrumpir funcionalidad |
| G06 Accesibilidad de pantallas | SCSS/feedback contienen reglas reutilizables | Playwright a 320px, zoom, teclado, foco, reduced-motion y lectores |
| G07 Densidad visual | Tokens y grids reusables declarados | Inventario de pantallas críticas y criterio de reducción sin pérdida de acción |
| G08 Ciclo de vida del tema | Tokens se aplican en frontend; no se auditó cambio A/B end-to-end | Conmutar empresa y probar que tema y datos anteriores no persisten |
| G09 Observabilidad | No se acreditó RUM sin PII y trazas versionadas por ruta | Definir minimización, sampling y protección de datos antes de medir |
| G10 Rollback | Revert de documentación es viable | En fase implementada: revert/flag autorizado y regresión positiva/negativa |

Estos son **pendientes de prueba**, no vulnerabilidades confirmadas. En particular, el presupuesto 650/750 kB configurado no acredita PASS, ni la existencia del interceptor prueba deduplicación. Tampoco se debe inventar un valor de LCP/INP/CLS.

## Contrato de evaluación posterior (oráculos NO_EJECUTADOS)

**Positivos FUTUROS:** P01 build registra bundle y lazy chunks reales; P02 rutas críticas registran LCP/INP/CLS bajo condiciones fijas; P03 conmutación A→B invalida datos y tema de A; P04 320px/zoom/foco/ARIA funcionan; P05 403 y error siguen expresándose; P06 preloading ocurre solo bajo condiciones declaradas y sin filtrar datos privados.

**Negativos FUTUROS:** N01 resultados de bundle no reproducibles; N02 chunk privado precargado desde acceso público; N03 caché A reaparece en B; N04 usuario altera tenant en URL/header; N05 error 403 convertido a vacío; N06 foco atrapado/invisible; N07 scroll horizontal roto a 320px; N08 movimiento no reducido; N09 HTTP duplicado no detectado; N10 se registra JWT/PII en RUM; N11 falso PASS de Playwright/Lighthouse ausente; N12 regresión de control RBAC por optimización. Ninguno se ejecutó al escribir este inventario.

## Fronteras, dependencia y validación proporcional

**Impacto:** dos archivos Markdown, este inventario y sección de CHANGELOG; no cambia interfaz HTTP, código Angular/.NET, modelo de dominio, MySQL, datos, CORS, JWT/RBAC, aislamiento, secretos, proveedores, Docker, CI, ni código de observabilidad. Consumers relevantes: app router, shell Angular, preload, estilos, feedback y API de autorización, **solo para planificación posterior**.

**Preflight:** PLAN_MAESTRO H832=PENDIENTE/I832 vacío; COLA G832/O832=SI; DAG_TECNICO A793=C05-04.M01.03, F793=VALIDADO, G793=SI; CAS a EN_PROGRESO con propietario único y readback. Estos gates autorizan inventario documental, no refactor en masa ni despliegue.

**A0:** diez rutas auditadas, hashes y diff sólo Markdown. **A1:** diez brechas y P06/N12 futuros, sin medir. **A2:** preservación de auth servidor, pertenencia A/B, logs sin PII y accesibilidad. **A3:** coherencia con M01.01/M01.02, Skill canónica y stack Angular 22.2.1 con Zone.js vigente. **A4:** commit fast-forward en dev, `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` sobre exact-SHA, readback PLAN/COLA/BITACORA; builds Angular, pruebas .NET/MySQL/E2E **NO_APLICA** a diff únicamente documental, nunca PASS ficticio. **A5:** exclusivamente otra Task Javier con TASK_ID distinto; no self-review y no QA/main/PROD.

**Rollback:** `git revert` normal sobre commit Markdown, sin force-push, cambios de horarios, tareas, secretos ni migraciones. No se ejecutan eventos, webhooks, workers, R2 ni operaciones de proveedor real.
