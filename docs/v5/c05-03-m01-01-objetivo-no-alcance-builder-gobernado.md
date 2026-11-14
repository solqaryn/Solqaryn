# SOLQARYN V5 — C05-03.M01.01: objetivo y no-regresión del builder gobernado

**Autor:** PRIMARY DEV 03 JAVIER — `TASK_ID=6adc2e4bd8a48191ac7d8641ed95a1a3`. **Fuente ejecutable:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), PLAN_MAESTRO fila 794, unidad `C05-03 | Builder gobernado | M01 | Alcance y preparación`. **Repositorio:** `solqaryn/Solqaryn`; **rama:** `dev`; **PROJECT_SCOPE_LOCK=STRICT**; fecha 2026-11-14.

Esta es únicamente una delimitación **documental M01.01**. No se implementa builder, editor, renderer, API, esquema MySQL, tabla de páginas, sistema de plantillas, publicación ni migración. Tampoco se afirma que el builder exista. Lo observable actualmente se separa del diseño futuro, sin permitir ejecutar código arbitrario por introducir una interfaz configurable.

## Objetivo medible y límites del lote

Definir un futuro constructor de **páginas y secciones configurables exclusivamente por un catálogo versionado de componentes y campos permitidos**, con validación redundante en backend, tenant y permisos. La evidencia final del lote deberá mostrar rechazo de **HTML, JavaScript, SQL, URLs peligrosas, referencias a tenants ajenos y payloads excesivos**, además de navegación con teclado, foco, responsive y rollback. Toda página publicada deberá ser renderizada por componentes Angular precompilados: el contenido guardado será datos tipados, nunca plantillas ejecutables.

**Dentro del lote C05-03 futuro, sujeto a autorizaciones M02–M06:** componentes preconstruidos identificados por claves canónicas (no por clases arbitrarias), campos con tipos/tamaño/rango, recursos con URL segura, límites de profundidad y cantidad de secciones, orden estable, preview sin efectos, versión/estado borrador y publicación con autorización server-side, tenant propietario, auditoría, rollback de versión y pruebas negativas A/B.

**Fuera de esta unidad M01.01:** aceptar HTML/JS/CSS libre, SQL, ejecución de expresiones, widgets de origen externo, scripts de terceros, uploads, migraciones, activación de proveedores pagos, guardar y publicar layouts de verdad, instalar dependencias, crear endpoints, editar código Angular/.NET, reglas IAM/JWT/RBAC/tenant, secrets, Task, QA/main/PROD. No se decide aquí catálogo definitivo, formatos, máximos ni roles autorizados: esas decisiones requieren dueño competente y contratos específicos posteriores.

## Línea base inspeccionada (evidencia de código, no existencia del builder)

| Archivo existente en dev | Blob observado | Hecho delimitado |
| --- | --- | --- |
| `frontend/src/app/core/models/tema-visual.model.ts` | `9d99a1da70c9c6da408d9a564e91512150b4aedd` | Define `TemaVisual` y una lista `CAMPOS_TEMA` tipada para colores; no permite inferir editor de páginas |
| `frontend/src/app/services/theme-applier.service.ts` | `cbccc2795cb1b010f74ce5b2b4dc1fb20f0b41f3` | Expresa regex de color hexadecimal y limpieza antes de nuevo tenant; no es un sanitizer general HTML/URL |
| `frontend/src/app/services/tema-visual.service.ts` | `588aa46fc4c10f8d6be85628e919a7bd89f366ac` | GET/PUT/restaurar del tema, no API de publicación de páginas |
| `backend/src/API/Controllers/TemaVisualController.cs` | `a33991fbf941c1fc3048dd30425f6342a0b0c6a4` | GET público de colores; PUT protegido por autorización y permiso Configuración.Editar; no es ACL universal para nuevos recursos |
| `frontend/src/app/app.routes.ts` | `2f560c14693455b79ac4546e9333fb74586ef234` | Rutas/guards existentes, no autorización de recursos ni builder |
| `frontend/src/styles/_enterprise-design-system.scss` | `6a188590af9a958bff2e3e0375c0c46cdb2b14e6` | Tokens semánticos y responsive reutilizables, no catálogo de componentes gobernados |

Estos seis blobs fueron leídos en GitHub sobre HEAD dev previo a esta entrega. El ejemplo de allowlist `CAMPOS_TEMA` es un patrón acotado **para colores**, no evidencia de que el catálogo de builder o validación server-side de layouts exista. Los archivos no se modifican.

## Doce invariantes de no-regresión propuestos (NR01–NR12)

| ID | Invariante para futuras fases | Condición de NO_GO |
| --- | --- | --- |
| NR01 | Claves de componente permitidas, versionadas y reconocidas en servidor | Un string libre elige código o markup |
| NR02 | Cada campo de sección tiene tipo y esquema válido | Valores extra se interpretan como HTML, JS o CSS |
| NR03 | Contenido renderizado se escapa y no ejecuta código | Inyección de script/event handler/expresión |
| NR04 | URLs permitidas por protocolo y dominio/regla aprobada | `javascript:`, `data:` inseguro o URL de red interna |
| NR05 | Cotas de tamaño, cantidad y profundidad se validan antes de persistir | Payload enorme, recursión o bloqueo de render |
| NR06 | Ownership de página, sección y recurso se verifica por tenant y membresía | A puede leer/alterar página o asset de B |
| NR07 | Backend JWT/RBAC decide preview, guardar y publicar | Guard de Angular, CSS o rol del cliente actúa como autorización |
| NR08 | Preview es aislada y no publica ni invoca efectos colaterales | Vista previa dispara workflows/handlers externos |
| NR09 | Borradores/publicaciones tienen versión trazable y rollback | Editar versión vieja sobrescribe una nueva sin CAS |
| NR10 | Render mantiene accesibilidad, responsive 320px y reduced-motion | Control inaccesible o scroll del documento roto |
| NR11 | Errores presentan rechazo comprensible sin revelar secretos/tenant ajeno | Stacktrace, token, dato B o ruta interna expuesta |
| NR12 | Se preservan temas actuales y rutas sin migración lateral improvisada | Cambio de builder altera IAM, BD o producción sin prueba |

## Cinco positivos y doce negativos: oráculos FUTUROS, NO ejecutados

**Positivos P01–P05:** P01 seleccionar clave válida produce componente fijo; P02 configuración tipada válida se muestra sin script; P03 tenant A puede previsualizar solo su borrador bajo permiso real; P04 320px/teclado/foco mantiene navegación; P05 edición versionada publica solo tras autorización y permite revertir a versión válida.

**Negativos N01–N12:** N01 etiqueta `<script>` rechazada; N02 evento `onerror` y HTML inyectado rechazados; N03 cadena SQL no se interpreta ni ejecuta; N04 `javascript:`/URL interna rechazada; N05 payload de tamaño excesivo rechazado; N06 profundidad excesiva rechazada; N07 componente no allowlisted rechazado; N08 campo extra de objeto rechazado; N09 tenant A intenta editar B y recibe denegación; N10 usuario sin permiso intenta publicar y recibe denegación; N11 versión obsoleta no pisa la vigente; N12 un guard Angular oculto no sustituye autorización de API.

Estos escenarios son **aceptación a diseñar y automatizar** en las fases técnicas correspondientes; no son pruebas de seguridad ya ejecutadas ni defectos observados. Distinguir también texto seguro que contiene palabras `SQL` de una orden SQL: el backend debe tratarlo como dato, no ejecutar; el criterio N03 prohíbe interpretación, no censura de contenido inocuo.

## Dependencias, pruebas y reversibilidad

`PLAN_MAESTRO!H794/I794`, `COLA!G794/O794` y `DAG_TECNICO!F755/G755` autorizan únicamente reclamar esta preparación documental por CAS; la fase C05.01/C05.02 es referencia preparatoria y no equivale a liberación de M01.06 ni M02–M06. El catálogo actual de 2.340 IDs se conserva.

**A0:** diff únicamente documento y CHANGELOG, revisión de fuentes y no secretos. **A1:** verificar objetivo, exclusiones, NR01–NR12, P01–P05 y N01–N12 como criterios documentales, sin runtime. **A2:** no alterar ni relajar JWT/RBAC/membresía/tenant, validación server-side, SSRF/URL o sanitización. **A3:** coherencia con los seis blobs citados, fuente estándar y preparaciones C05-01/C05-02; datos falsables claramente separados de código existente. **A4:** commit fast-forward serializado a `dev`, checks pertinentes `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` completed/success exact SHA, PLAN/COLA/BITACORA readback. CI Angular/.NET/MySQL/Playwright de runtime es **NO_APLICA** al diff solo Markdown, nunca PASS por defecto. **A5:** revisión independiente por TASK_ID Javier distinto del autor; no autoaprobación ni promoción.

**Rollback:** revertir el changeset documental mediante `git revert` normal, sin reescribir historia. No autoriza servicios externos, worker/eventos, nueva Task, QA, main ni PROD.
