# SOLQARYN V5 — C05-03.M01.05: aceptación observable del builder gobernado

**Autor:** PRIMARY DEV 06 JAVIER; TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Fuente ejecutable:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, PLAN_MAESTRO fila 798. **Repositorio/rama:** `solqaryn/Solqaryn@dev`. **PROJECT_SCOPE_LOCK=STRICT**. **Fecha:** 2026-11-14.

## Alcance: contrato de aceptación documental, NO implementación

Esta unidad M01.05 fija criterios **observables y falsables** para el futuro builder de páginas y secciones **allowlisted**; no crea editor, renderer, API, permisos, migraciones, publicación, preview funcional o backend sanitizador. Las pruebas y resultados indicados abajo son **requisitos futuros no ejecutados**; no se certifica Angular, .NET, MySQL, JWT, RBAC, Playwright ni resiliencia por escribirlos.

Se complementan M01.01 (alcance y NR01–NR12), M01.02 (12 actores y 12 contratos futuros), M01.03 (12 blobs causales) y M01.04 (R01–R12, S01–S08, D01–D08, **decisiones pendientes**). M01.06 y M02–M06 no se liberan automáticamente por este documento.

## Criterios positivos AC01–AC14 — para pruebas FUTURAS

Cada criterio requiere una prueba dirigida reproducible, registro de SHA/datos efímeros y prueba negativa complementaria antes de contar PASS de implementación.

| ID | Capacidad | Dado / cuando / entonces esperado | Evidencia futura exigida | Estado |
| --- | --- | --- | --- | --- |
| AC01 | Catálogo permitido | Dado catálogo aprobado y componente registrado, al solicitar render por clave/version válida, se obtiene únicamente el componente Angular precompilado esperado | Contrato M02 + unitarias catálogo | **FUTURO / NO EJECUTADO** |
| AC02 | Campos tipados | Dado esquema firmado, al recibir texto, número, booleano y enum válidos, backend devuelve DTO estricto sin propiedades extra | xUnit validación y Angular component tests | **FUTURO / NO EJECUTADO** |
| AC03 | Texto como dato | Dado texto inocuo con palabras HTML/SQL, al mostrarlo se escapa como contenido sin interpretarlo | Browser/DOM assertion + xUnit | **FUTURO / NO EJECUTADO** |
| AC04 | Preview tenant | Dado usuario A con membresía y permiso de preview, al abrir borrador A ve solo datos A, sin persistencia ni eventos | API JWT/RBAC + Playwright A/B | **FUTURO / NO EJECUTADO** |
| AC05 | Guardar borrador | Dado editor autorizado y versión vigente, al guardar configuración válida se crea revisión trazable del tenant propio | API/EF/MySQL efímera + concurrencia | **FUTURO / NO EJECUTADO** |
| AC06 | Publicar separado | Dado publicador autorizado con revisión aprobada, al publicar se crea versión pública sin cambiar borradores ajenos | API security + auditoría + MySQL efímera | **FUTURO / NO EJECUTADO** |
| AC07 | Revertir sin borrar historia | Dado publicación previa íntegra, al revertir autorizado nace nueva revisión basada en la anterior, manteniendo trazabilidad | API integration + historial | **FUTURO / NO EJECUTADO** |
| AC08 | Público mínimo | Dado host/slug vinculados por servidor, al consultar anónimamente se proyecta exclusivamente lo publicado | API tests y caché segmentada | **FUTURO / NO EJECUTADO** |
| AC09 | URLs seguras | Dado URL con esquema y destino aprobados por arquitectura/seguridad, al validar se acepta sin hacer fetch arbitrario | Validator tests y SSRF negatives | **FUTURO / NO EJECUTADO** |
| AC10 | Límites medidos | Dadas cotas aprobadas de bytes/secciones/profundidad, al enviar payload dentro de límites se acepta con latencia objetivo definida | xUnit + integración/load test | **FUTURO / NO EJECUTADO** |
| AC11 | Accesibilidad básica | Dado viewport 320px y teclado sin mouse, al navegar editor/preview futuro hay foco visible, orden lógico y sin scroll horizontal roto | Playwright Chromium y auditoría a11y | **FUTURO / NO EJECUTADO** |
| AC12 | Preferencia de movimiento | Dado reduced-motion, las transiciones no impiden interacción ni obligan animación | Playwright emulación preferencia | **FUTURO / NO EJECUTADO** |
| AC13 | Error seguro | Dada solicitud inválida, se devuelve error estructurado estable sin exponer token, stacktrace o recurso B | API contrato negativo + log redaction | **FUTURO / NO EJECUTADO** |
| AC14 | Rollback operativo | Dado cambios desplegados con feature flag y autorización, al desactivar se mantiene la UI/tema previo sin perder datos | Pruebas regresión y ensayo rollback aprobado | **FUTURO / NO EJECUTADO** |

## Negativos N01–N14 — para pruebas FUTURAS

| ID | Abuso/error | Resultado negativo obligatorio | Estado |
| --- | --- | --- | --- |
| N01 | Script/handler inyectado | Se rechaza cualquier valor que pretenda convertirse en script, handler o HTML ejecutable | **FUTURO / NO EJECUTADO** |
| N02 | CSS, expresiones y markup libres | Rechazar estilos/scripts/expresiones no incluidos en catálogo; no eval, Function ni innerHTML arbitrario | **FUTURO / NO EJECUTADO** |
| N03 | SQL como ejecución | Nunca interpolar ni ejecutar SQL derivado de configuración; texto 'SELECT' inocuo es dato | **FUTURO / NO EJECUTADO** |
| N04 | Esquemas peligrosos/SSRF | Rechazar javascript:, data: no permitido, file:, localhost, IP privada y redireccionamientos a redes internas según política aprobada | **FUTURO / NO EJECUTADO** |
| N05 | Payload sobredimensionado | Rechazar bytes y número de secciones que excedan límites aprobados sin persistir efectos | **FUTURO / NO EJECUTADO** |
| N06 | Profundidad/recursión extrema | Rechazar ciclo o árbol profundo para evitar stack overflow o consumo no acotado | **FUTURO / NO EJECUTADO** |
| N07 | Componente no allowlisted | Clave desconocida o versión obsoleta nunca se transforma en componente dinámico arbitrario | **FUTURO / NO EJECUTADO** |
| N08 | Campo/tipo extra | Rechazar propiedad adicional, enum inválido y tipo incorrecto en DTO y servidor | **FUTURO / NO EJECUTADO** |
| N09 | Acceso tenant A a B | API deniega GET/preview de recurso de B, incluso con ID o slug conocido por A | **FUTURO / NO EJECUTADO** |
| N10 | Edición o publicación sin rol | API deniega a usuario autenticado sin permiso; UI oculta no se considera autorización | **FUTURO / NO EJECUTADO** |
| N11 | Versión caducada | Dos editores con revisiones concurrentes: el segundo recibe conflicto, no pisa versión nueva | **FUTURO / NO EJECUTADO** |
| N12 | Preview con efectos | Preview no invoca webhook, worker, envío, persistencia ni publicación | **FUTURO / NO EJECUTADO** |
| N13 | Error con información ajena | Respuestas y logs no revelan JWT, secretos, PII, tenant B, query SQL ni ruta interna | **FUTURO / NO EJECUTADO** |
| N14 | Publicación pública cruzada | Host mal vinculado o caché cruzada no devuelve página/asset de otro tenant | **FUTURO / NO EJECUTADO** |

Las entradas HTML, SQL y URL descritas son **payloads de prueba propuestos**, no exploits observados. Rechazar ejecución de SQL no significa censurar texto inocuo que contenga palabras SQL. El navegador **no** puede ser única barrera: deben existir validaciones de servidor y aislamiento por tenant, incluidos casos A/B cruzados.

## Criterios de suficiencia por frontera técnica

| Frontera | PASS futuro necesario | NO_GO inequívoco |
| --- | --- | --- |
| Catálogo/schema | Versiones y claves reconocidas por servidor; DTO estricto | Cualquier clave produce código, CSS o markup libre |
| Autenticación/roles | JWT válido, membresía y RBAC por recurso; separación preview/guardar/publicar | Cliente decide permisos o A puede acceder a B |
| Sanitización/enlaces | Datos tratados como texto, esquemas/hosts permitidos y SSRF controlado | Script, data URL riesgosa o fetch interno |
| Datos/versionado | Casos de carrera con CAS, historial auditable y rollback | Lost update o reversión destructiva |
| UX/accessibility | 320px, teclado, foco, reduced-motion, estados error | UI bloqueada o no navegable |
| Efectos/observabilidad | Preview sin side effects y errores/logs mínimos | Worker, webhook, publicación o fuga de PII desde preview |

**Dependencias decisorias:** D01 catálogo, D02 roles, D03 cuotas, D04 URL/CSP, D05 CAS/rollback, D06 preview, D07 host/caché y D08 accesibilidad continúan **PENDIENTE** en M01.04. Este M01.05 no fija máximos, permisos ni acuerdos por sí mismo. Las pruebas concretas se elaborarán solo al aprobar esas definiciones y reclamar fases técnicas correspondientes.

## Trazabilidad de código y evidencia existente

Se reusan como **línea base inspeccionada** los doce blobs de M01.03: `docs/v5/c05-03-m01-03-inventario-fuentes-builder-gobernado.md`, blob `5251f2cc0a3931f322e96adebd5f507c9e4a4daa`. Fuentes clave: `tema-visual.model.ts` `9d99a1da70c9c6da408d9a564e91512150b4aedd` (allowlist de colores), `theme-applier.service.ts` `cbccc2795cb1b010f74ce5b2b4dc1fb20f0b41f3` (hex, no sanitizador HTML), `TemaVisualController.cs` `a33991fbf941c1fc3048dd30425f6342a0b0c6a4` (GET público de tema y PUT/restore con permiso), `tenant-context.service.ts` `63a711355495d2f80efba35bffb2b8727401154a` (contexto validado por backend) y `_enterprise-design-system.scss` `6a188590af9a958bff2e3e0375c0c46cdb2b14e6` (tokens, no builder).

M01.04: `docs/v5/c05-03-m01-04-riesgos-supuestos-decisiones-builder.md`, blob `f6a9b0de4e971c610087f002f85d0e39c365b7b0`. Riesgos y decisiones de ese documento son hipotéticos/pedientes; no se reinterpretan como vulnerabilidades confirmadas o autorizaciones.

## Impacto, verificación A0–A5 y reversibilidad

Preflight por ID: `PLAN_MAESTRO!H798=PENDIENTE/I798` vacío, `COLA!G798/O798=SI` y `DAG_TECNICO!A759=C05-03.M01.05/F759=VALIDADO/G759=SI`; solo permite **claim documental exclusivo**, no M01.06, programación, deploy ni trabajo en otro tenant. La columna J sigue registrando incidencias reales; no editar BITACORA histórica.

**Impacto:** exclusivamente nuevo archivo `docs/v5` y sección autocontenida en `CHANGELOG_AI.md`. No se modifican Domain, Application, API, Infrastructure, Angular, MySQL, JWT/RBAC, tenant, permisos, archivos, logging ni integración externa. **A0:** diff dos Markdown, criterios contados AC14/N14 y sin secretos; **A1:** matriz dada-cuando-entonces + evidencia futura; **A2:** no relajar backend/JWT/RBAC/tenant/SSRF/XSS; **A3:** coherencia con fuente V5 y M01.01–M01.04, sin afirmar tests ejecutados; **A4:** commit fast-forward serializado, Scope Lock y procedencia CHANGELOG `completed/success` en SHA exacto y readback PLAN/COLA/BITACORA; las suites runtime ajenas al diff Markdown son `NO_APLICA` motivado, nunca PASS; **A5:** revisor de otra Task Javier distinta de la autora, sin aprobación propia ni autorización QA/main/PROD.

**Rollback:** revertir el commit documental con `git revert` ordinario; sin force-push, reescritura de historia ni activación de worker/event dispatcher/webhook.
