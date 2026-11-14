# SOLQARYN V5 — C05-04.M01.02: actores, entradas, salidas y superficies de ligereza

**Autor:** SOLQARYN PRIMARY DEV 01 / JAVIER_CHATGPT, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Fecha:** 2026-11-14. **Fuente institucional:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, documento `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, PLAN_MAESTRO fila 831. **Repositorio:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`.

## Objeto verificable de M01.02

Identificar actores, entradas, salidas y fronteras de confianza para la **futura** mejora de percepción del ERP: carga inicial, duplicación de solicitudes, densidad visual, estados y accesibilidad móvil. Este archivo es un contrato de **preparación documental**, no un benchmark, cambio de Angular, optimización implementada, servicio nuevo ni autorización de publicar. Da continuidad al objetivo y NR01–NR12 de `C05-04.M01.01`, sin anticipar M01.03–06.

## Actores y responsabilidades

| Actor o consumidor | Entrada/intención | Salida esperada futura | Frontera que no debe cambiar |
| --- | --- | --- | --- |
| U01 Operador tenant | Abrir dashboard y flujo principal | Contenido útil sin esperas espurias | Permisos y pertenencia resueltos por API |
| U02 Administrador tenant | Cambiar empresa/sucursal | Navegación y datos del nuevo contexto | Invalidar estado privado anterior y negar mezcla A/B |
| U03 Usuario de teclado/lector | Tabular, enfocar, leer feedback | Foco visible, etiquetas y mensajes accesibles | Sin ocultar estados de error/403 |
| U04 Usuario móvil 320 px | Navegar menús, filtros y formularios | Layout legible sin cortes ni scroll roto | Controles accionables y zoom preservados |
| U05 Usuario con reduced-motion | Solicitar reducción de movimiento | Transiciones opcionales, sin perder información | Respetar preferencias del sistema |
| U06 Visitante storefront | Navegar rutas públicas `/tienda` | Lazy carga y preloading selectivo | Rutas públicas no revelan datos privados |
| U07 Router Angular | Resolver rutas, guards y chunks | Lazy import y preload condicionado | El guard cliente no sustituye RBAC servidor |
| U08 Capa de datos HTTP | Solicitar y cancelar lecturas | Respuestas asociadas a sesión/tenant actual | No cachear por clave global entre empresas |
| U09 Diseño/feedback Angular | Presentar loading/vacío/error/success | Estados accesibles con `aria-live` | Optimizar sin silenciar errores |
| U10 Backend .NET | Resolver auth, RBAC y tenant | Solo datos autorizados al consumidor | No reducir validaciones por latencia |
| U11 QA/performance | Medir escenarios repetibles | Baseline/delta reproducibles | No calificar pruebas que no corrieron |
| U12 Auditor de seguridad | Revisar A/B, token y registros | Evidencia sin PII/secretos | Logs y métricas no exponen contenido privado |

Estos roles describen consumidores y escenarios, **no** nuevos roles RBAC creados ni autorizaciones activas.

## Entradas, salidas y límites FUTUROS (no ejecutados)

| ID | Entrada observacional o futura | Salida/criterio exigido | Rechazo o protección |
| --- | --- | --- | --- |
| I01 | Ruta y sesión con contexto servidor | Primer contenido útil medible | Evitar flash de datos empresa anterior |
| I02 | Evento de navegación consecutiva | Una carga eficaz y estado consistente | Evitar HTTP duplicado/no cancelado |
| I03 | Chunk Angular marcado para preload | Diferir a condición explícita | No precargar ruta privada por heurística pública |
| I04 | Presupuesto de build versionado | Evidencia del bundle emitido | No confundir 650/750 kB declarados con PASS |
| I05 | Viewport 320 px y zoom | Navegación y acciones utilizables | No cortar textos, foco ni formularios |
| I06 | Preferencia reduced-motion | Animación ajustada sin salto semántico | No ignorar preferencia del sistema |
| I07 | Feedback loading/error/vacío | Estado ARIA comprensible | No convertir 403/500 en vacío |
| I08 | Tema y empresa seleccionados | Colores y datos correctos por contexto | No reutilizar tokens/estado del tenant A en B |
| I09 | Solicitud HTTP con JWT | Respuesta autorizada por API | No derivar tenant desde URL/header del cliente |
| I10 | Trazas Core Web Vitals por ruta | Métrica con dispositivo/red/versión | No declarar LCP/INP/CLS sin navegador |
| I11 | Tabla densa/diálogo | Densidad adaptativa y foco restaurado | No esconder acciones críticas |
| I12 | Rollback documentado | Restauración del comportamiento previo | No degradar seguridad ni reescribir historia |

**Fronteras de confianza:** Browser→API (JWT/RBAC, permisos, tenancy); router/guard→carga (experiencia, no autoridad); caché/HTTP→sesión/empresa (separar A/B); estado UI→accesibilidad (mensajes y foco); bundle/configuración→métrica medida (configuración no es resultado); telemetría→privacidad (sin tokens, nombres ni datos comerciales).

## Evidencia leída del árbol dev (estática, sin benchmarking)

| Ruta | Blob SHA observado | Qué acredita y qué no |
| --- | --- | --- |
| `docs/v5/c05-04-m01-01-objetivo-no-alcance-ligereza-percepcion.md` | `a9b7219c7094b8b162f53117a2c67d2c86dd7c21` | NR01–NR12 y oráculos FUTUROS; no mejora medida |
| `frontend/angular.json` | `8b00f8fee80fdfafe1864e233be7d5f20ad444e3` | Budget inicial 650/750 kB configurado, no build ejecutado |
| `frontend/src/app/app.routes.ts` | `2f560c14693455b79ac4546e9333fb74586ef234` | Lazy routes y guards declarados, no perfilado |
| `frontend/src/app/core/performance/after-render-selective-preloading.strategy.ts` | `a5083eac7fd882430c9550c037c17f89cb946149` | Preload condicional /tienda estable + 250 ms, no medición |
| `frontend/src/app/shared/feedback-state/feedback-state.component.ts` | `7deb40475f0c492d09e649e7351b2915bd53d356` | Feedback/aria-live, no prueba browser |

Los hashes son fuentes del árbol revisado; para fases posteriores deben cotejarse de nuevo contra HEAD. No se certifica exhaustividad de rutas ni fallos existentes por enumerar riesgos.

## Oráculos observables FUTUROS (NO_EJECUTADOS)

| Positivo | Condición medible cuando exista fase de pruebas |
| --- | --- |
| P01 | Medición de ruta crítica registra baseline LCP, INP, CLS, build SHA, red y dispositivo |
| P02 | Navegar a `/tienda` no precarga chunk marcado antes de estabilidad y política explícita |
| P03 | Cambiar A→B limpia datos en memoria/caché sin mostrar recursos de A |
| P04 | Pantalla a 320 px mantiene foco, scroll y controles utilizables |
| P05 | Error HTTP se comunica sin perder semántica `role=alert` o `aria-live` |
| P06 | Rollback revierte solo optimización y conserva permisos/tenancy |

| Negativo | Resultado futuro de prueba exigido |
| --- | --- |
| N01 | HTTP duplicado al navegar se detecta, no se declara mejora |
| N02 | Preload de chunk privado por ruta pública se rechaza |
| N03 | Caché de tenant A consultada en B se invalida |
| N04 | Eliminación de feedback de error falla accesibilidad |
| N05 | 403 convertido a empty no supera aceptación |
| N06 | Foco invisible/atrapado falla prueba teclado |
| N07 | Layout con corte a 320 px falla inspección móvil |
| N08 | Animación obligatoria viola reduced-motion |
| N09 | Script/chunk adicional rompe budget medido |
| N10 | TenantId de URL/header intenta conceder acceso y API deniega |
| N11 | Log de rendimiento incluye JWT/PII y se rechaza |
| N12 | Declaración de PASS sin medición de navegador y SHA es inválida |

Los P/N documentan **expectativas**, no pruebas Angular/Playwright/.NET/MySQL realmente ejecutadas aquí.

## Impacto, validaciones y reversibilidad

**Impacto M01.02:** solo este Markdown y sección nueva autocontenida de `CHANGELOG_AI.md`; cero cambios en frontend, Domain/Application/Infrastructure/API, rutas activas, DTO, SQL, migraciones, permisos, tenant, observabilidad real, servicios, endpoints, jobs, secretos o configuración física de Tasks. Dependencias: M01.01 de C05-04 y referencias documentales de C01/C02/C05; DAG individual `A792` F=VALIDADO/G=SI y PLAN H831/I831/COLA G/O=SI fueron preflight.

**A0:** diff Markdown + procedencia de fuentes, sin secretos. **A1:** comprobar 12 actores, 12 interfaces, P01–P06/N01–N12 documentales; no asignar PASS runtime. **A2:** preservación de JWT/RBAC/tenant A/B, ARIA, datos/PII y caching seguro. **A3:** coherencia con M01.01 y arquitectura del HEAD, sin afirmar CWV. **A4:** publicación serial fast-forward/CAS en `dev`, controles `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` completados/success en SHA exacto; PLAN/COLA/BITACORA readback. CI backend/frontend/browser/MySQL **NO_APLICA** a este diff exclusivamente Markdown. **A5:** pendiente por otra Task del allowlist Javier, TASK_ID distinto. **Rollback:** `git revert` normal; ningún acceso a QA/main/PROD ni activación de eventos, proveedores o migraciones.
