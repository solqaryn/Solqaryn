# SOLQARYN V5 — C05-04.M02.01: modelo conceptual, ownership y no-regresión de rendimiento UI

**ID:** `C05-04.M02.01`, PLAN_MAESTRO fila 836. **Autor/owner:** PRIMARY DEV 01 — JAVIER / `6adbb9fff1548191a1bd5b30d9e9984a`. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Repo:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Tipo de trabajo:** M02.01 contrato DOCUMENTAL; no crea entidades persistidas ni funcionalidades ejecutables.

## Propósito y estado de la realidad

Definir el **modelo conceptual de ownership, relaciones e invariantes** para reducir densidad visual, carga inicial, llamadas duplicadas y ruido de ERP sin mezclar datos de empresas. La plataforma real conserva Angular 22.2.1 standalone + Signals + Material/CDK y Zone.js 0.16.3, ASP.NET Core 10 con Domain/Application/Infrastructure/API, EF Core 10/MySQL Oracle y autorizaciones JWT/RBAC en backend. Estos componentes se respetan; la presente unidad no modifica rutas, DTOs, CSS, consultas, índices, backend, Angular, APIs, datos, métricas ni dependencias de npm.

El plan exige para el lote `C05-04` presupuesto de bundle, objetivos Core Web Vitals y pruebas móviles. **Los valores reales de bundle, LCP, INP, CLS, llamadas HTTP, FPS y tiempos de respuesta no fueron medidos en esta unidad**; ninguna cifra objetivo constituye resultado PASS. La medición será tarea posterior con equipo/entorno, ruta, datos sintéticos y SHA registrados.

## Precedentes del DAG y separación de permisos

En preflight se verificó `PLAN_MAESTRO!H836=PENDIENTE/I836` vacío; `COLA!G836/O836=SI`, DAG de `C05-04.M02.01` `F=VALIDADO/G=SI`, junto con M01.01–06 LISTO y A5 independiente donde correspondía. `C05-04.M01.06` recibió A5_PASS de otra Task Javier antes de quedar habilitados M02.01–05. La habilitación técnica **no** concede permiso de acceso a métricas productivas, modificar control de tenancy ni liberar M02.06/M03.

El frontend puede decidir presentación, estados visuales, lazy loading y caché efímera, pero nunca otorga permisos ni toma un tenant del slug/header como fuente de identidad. El backend valida membresía, autorización, recurso y tenant actual antes de entregar datos, independientemente de si la UI oculta controles.

## Modelo de entidades conceptuales — NO tablas/DTO creados

| ID | Entidad conceptual | Ownership y relaciones exigidas |
| --- | --- | --- |
| E01 | `UiSurfaceBudget` | Clave opaca de ruta/pantalla + versión/commit; no contiene datos personales |
| E02 | `UiLoadingPhase` | Relacionada con E01; skeleton inicial, contenido útil, carga diferida, recuperación |
| E03 | `UiRequestBudget` | E01 + operación HTTP autorizada; evita solicitudes duplicadas por navegación |
| E04 | `UiTelemetrySample` | E01 + entorno/viewport/timestamp/commit; solo mediciones sintéticas redactadas |
| E05 | `UiAccessibilityConstraint` | E01 + operación; teclado/foco/semántica/reduced-motion independientes de permiso |
| E06 | `UiFeatureVisibility` | E01 + capability verificada por backend, sin cambiar ACL ni privilegios |
| E07 | `UiTenantContextVersion` | Token/versión efímera de identidad server-side para invalidar respuestas de otro tenant |
| E08 | `UiRegressionBaseline` | E01 + SHA + pruebas e indicadores, con resultado verificable y rollback |

Las ocho entradas son **conceptos de contrato futuro**, no migraciones, columnas reales, telemetría activa, recursos expuestos al cliente ni API implementada. No se almacenan tokens ni identificadores de personas dentro de estos esquemas.

## Invariantes INV01–INV14 para implementación posterior

| ID | Regla futura y prueba negativa necesaria |
| --- | --- |
| INV01 | Densidad/jerarquía de la pantalla conserva tareas clave sin duplicar acciones |
| INV02 | Lazy loading no introduce salto de foco ni bloquea lector de pantalla |
| INV03 | La presentación no depende de un permission check exclusivamente Angular |
| INV04 | Una respuesta asíncrona de tenant A no sobrescribe pantalla tras cambiar a B |
| INV05 | No se reutilizan cachés de A para solicitudes de B o sesión revocada |
| INV06 | Deduplicación HTTP no confunde actor, ruta, filtro, tenant o autorización |
| INV07 | Los datos privados no se precargan por bundle o HTML público |
| INV08 | Desactivar capability no afecta seguridad del backend ni borra datos históricos |
| INV09 | Skeleton/loading tienen fin/error/empty/retry accesibles; sin spinner infinito |
| INV10 | Presupuestos de bundle tienen baseline, límite autorizado y comparación exacta |
| INV11 | LCP, INP y CLS se miden con metodología, viewport, red y SHA comparables |
| INV12 | Menú, formularios y tablas mantienen teclado, contraste, foco y reduced-motion |
| INV13 | Telemetría no registra tokens, PII, contraseñas ni URLs firmadas |
| INV14 | Un fallo de métrica o CI deja scope VALIDANDO, nunca PASS presumido |

## Casos de prueba FUTUROS — NO_EJECUTADOS

| Caso | Verificación esperada |
| --- | --- |
| P01 | Ruta mínima renderiza contenido útil sin bloquear autenticación/permiso |
| P02 | Navegación A→B autorizada invalida consultas/caché del tenant anterior |
| P03 | Misma consulta idempotente deduplicada no realiza llamadas redundantes |
| P04 | Diseño móvil 320px conserva navegación, foco, lectura y scroll adecuado |
| P05 | Budget y CWV medidos en entorno reproducible y conservados con SHA |
| N01 | Actor A consulta recurso B: backend rechaza aun cuando UI lo muestre |
| N02 | Respuesta antigua de A llega después del cambio a B: se descarta |
| N03 | Token caducado o membresía revocada: sin render de datos privados |
| N04 | Un flag desactivado no elimina ni modifica datos de negocio |
| N05 | Imagen o bundle sobredimensionado excede objetivo: CI no acredita éxito |
| N06 | Foco se pierde tras lazy/render; prueba accesibilidad falla |
| N07 | Cache key omite actor/tenant: se rechaza y se aísla |
| N08 | Telemetría incluye PII/token: filtro redacta y falla test de seguridad |
| N09 | API backend error/timeout: UI falla de forma recuperable y accesible |
| N10 | UI falsifica role/capability: backend deniega operación |

No son resultados Playwright/Angular/HTTP ejecutados. El contrato se traducirá a pruebas proporcionales solo al implementar la fase real.

## Auditorías, reversión y salida de M02.01

**A0:** archivo Markdown nuevo y sección CHANGELOG autocontenida, propietario y scope verificados, sin claves, secretos ni código ejecutable. **A1:** E01–E08, INV01–INV14, P01–P05/N01–N10 trazables como futuros. **A2:** tenancy A/B, RBAC backend, revocación, cachés, PII y autorización server-side preservados conceptualmente; no test runtime atribuido. **A3:** coherencia con M01.01–06, DAG, arquitectura y consumidores actuales sin inventar API, tablas ni métricas. **A4:** commit fast-forward/CAS en dev de dos Markdown, checks exact-SHA `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` completed/success, PLAN/COLA/DAG/BITACORA readback. Compilaciones runtime ajenas a estos documentos: NO_APLICA motivado, nunca PASS.

**A5:** revisión por otra Task Javier diferente de la autora, con SHA/blob/diff, pruebas documentales y BITACORA. Hasta ese momento A5_PENDIENTE. **Rollback:** `git revert` normal del commit documental, sin force-push ni pérdida histórica; no QA/main/PROD.
