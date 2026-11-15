# SOLQARYN V5 FINAL — C14-01.M02.01: entidades, ownership e invariantes de aceptación funcional

**Alcance:** diseño contractual documental de `C14-01 | Aceptación funcional | M02`. **Autor:** PRIMARY DEV 06, Task `6af16bbad26881918c283abe06e1f32a`. **Fecha:** 2026-11-15. **Repositorio:** `solqaryn/Solqaryn@dev`. **PROJECT_SCOPE_LOCK:** STRICT.

## Autoridad de la microtarea

Fuentes ejecutables exclusivas: [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN_MAESTRO/COLA/DAG_TECNICO](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). Dependencia técnica: `C14-01.M01.01–06` en LISTO y con A5 independiente; `C14-01.M01.06` se revisó por Task distinta sobre SHA `19bc00901492c173e40af6eeb47968eb7653b54d` con 16 barreras, 10 reversos y 12 oráculos futuros. PLAN fila 2024 PENDIENTE sin owner, COLA G/O=SI y DAG F=VALIDADO/G=SI al claim. No se autoriza ejecución de pruebas de aceptación por documentar estas entidades.

Este contrato propone objetos de trazabilidad y no introduce tablas, índices, DTO ejecutables ni API. Debe adaptarse al dominio real de cada rubro cuando los contratos de M03/M04 se implementen con evidencia. Las relaciones son lógicas, no migraciones aprobadas.

## Modelo propuesto E01–E16 y relación de propiedad

| ID | Entidad lógica | Clave conceptual | Fuente de autoridad / ownership | Restricción |
| --- | --- | --- | --- | --- |
| E01 | Actor | actor_key | identidad autenticada o visitante público | No asumir permisos por el nombre del rol |
| E02 | IdentidadSesion | session_fingerprint | servicio de autenticación backend | No registrar JWT ni refresh tokens |
| E03 | Tenant | tenant_id | autoridad de membresía verificada | No seleccionar empresa por un parámetro del cliente |
| E04 | Membresia | tenant_id + user_id | servidor y base autorizada | Vigencia y revocación comprobadas |
| E05 | Sucursal | tenant_id + branch_id | tenant propietario | No cruzar sucursales sin grant |
| E06 | PermisoEfectivo | subject + resource + action | RBAC backend | El guard Angular no autoriza |
| E07 | EscenarioAceptacion | scenario_key + version | plan de prueba versionado | Oráculo positivo y negativo definidos |
| E08 | PasoEscenario | scenario_key + sequence | escenario padre | Entrada, acción y salida esperadas tipadas |
| E09 | Precondicion | scenario_key + predicate | contrato de prueba | No fabricar estado listo cuando falta fixture |
| E10 | RecursoPrueba | tenant_id + fixture_key | fixture sintético autorizado | No usar PII ni tokens reales |
| E11 | Observacion | scenario_key + run_sha | corrida identificable | Distinguir resultado observado de futuro |
| E12 | EvidenciaSanitizada | sha + evidence_key | ámbito de ejecución | Sin URLs internas, secretos ni adjuntos de cliente |
| E13 | ContratoNegativo | scenario_key + denial_code | política de seguridad | Toda denegación observable por backend |
| E14 | AccionComercial | tenant_id + order_ref | propiedad persistida | Idempotencia y versión antes de cobro/factura |
| E15 | EstadoUI | route + viewport + state | contexto visual | Error, foco, vacío y responsive no equivalen a WCAG |
| E16 | PlanReversion | scenario_key + baseline_sha | artefacto y autor de prueba | Rollback aislado y evidencia histórica conservada |

Relaciones: un `Tenant` tiene `Membresia` y `Sucursal` con aislamiento por tenant; `Actor` posee sesión verificada por backend; `EscenarioAceptacion` agrupa `PasoEscenario`, `Precondicion`, `ContratoNegativo` y `PlanReversion`; `Observacion` apunta a `EvidenciaSanitizada` con SHA de ejecución. `AccionComercial` sólo se observa mediante identificador de documento y ownership persistido, nunca se invoca por el contrato documental.

## Invariantes I01–I16, positivas y de fallo cerrado

| Regla | Condición exigida |
| --- | --- |
| I01 | Un sujeto sin JWT válido sólo puede ejecutar escenarios públicos sin datos privados |
| I02 | La autoridad de tenant/sucursal proviene de backend, no de URL, cookie, cabecera ni DTO |
| I03 | Los recursos de tenant B nunca son visibles para la membresía de tenant A |
| I04 | Membresía revocada equivale a denegación incluso con token todavía firmado |
| I05 | El ownership legado NULL se trata como ambiguo y se deniega hasta corrección autorizada |
| I06 | Cada escenario distingue precondición, acción y resultado positivo/negativo observable |
| I07 | Un caso de aceptación documentado no se contabiliza como prueba ejecutada |
| I08 | Los fixtures de CI son sintéticos, reproducibles y se eliminan sin tocar datos reales |
| I09 | El backend vuelve a verificar RBAC; el guard Angular sólo orienta navegación |
| I10 | Idempotencia evita doble pedido/pago/factura bajo reintento concurrente |
| I11 | Versión obsoleta de pedido o reserva produce conflicto, no mutación silenciosa |
| I12 | Toda evidencia persistente liga escenario, SHA exacto, estado CI y origen autorizado |
| I13 | Un job CANCELLED, SKIPPED, queued o in_progress no demuestra PASS requerido |
| I14 | Se comprueba foco, semántica, viewport y mensajes de error con pruebas futuras concretas |
| I15 | El rollback documental no revierte transacciones comerciales ni restaura base de datos |
| I16 | A5 independiente y autorización separada son requisitos antes de QA/main/PROD |

Las invariantes son condiciones de aceptación **futuras**: no constituyen PASS runtime, pentest, WCAG, prueba de restauración o comprobación de cero P0/P1. Cualquier cambio en JWT, RBAC o tenant debe conservar denegación verificable en backend con datos sintéticos A/B.

## Casos positivos/negativos propuestos, NO EJECUTADOS

| Positivo | Oráculo futuro | Negativo | Oráculo de denegación futuro |
| --- | --- | --- | --- |
| P01 | Visitante anónimo accede a catálogo público permitido | N01 | El visitante solicita un pedido privado por URL y recibe denegación |
| P02 | Usuario tenant A lee sólo su sucursal autorizada | N02 | Un parámetro intenta consultar tenant B y backend deniega |
| P03 | Reintento del mismo pedido usa contrato idempotente | N03 | Dos solicitudes concurrentes no deben duplicar factura |
| P04 | Interfaz responsive mantiene CTA y foco visible | N04 | Estado error no debe ocultar acción ni atrapar foco |

Otras negativas independientes que deben instrumentarse en M03–M05:

- **N05:** JWT expirado o revocado no alcanza recurso privado.
- **N06:** Membresía sin permiso carece de mutación aunque Angular muestre CTA.
- **N07:** Recurso con propietario NULL no hereda tenant de la solicitud.
- **N08:** Reserva concurrente no admite sobreventa ni stock negativo.
- **N09:** Importe o moneda discrepante no genera pago.
- **N10:** Evidencia con email/token/PII se rechaza antes de guardarse.
- **N11:** Check CI cancelado no se transforma en PASS por repetir resumen.
- **N12:** Rollback de documento no modifica bitácora histórica ni pagos.

Un resultado aceptado necesitará entorno de prueba, rol, tenant/sucursal, ruta/backend, fixture, viewport, HTTP o evento observable, SHA exacto y estado de CI. No confundir escenarios escritos con suites xUnit/Playwright ejecutadas.

## Auditoría del artefacto y rollback

**A0:** comprobación de dos archivos Markdown, ausencia de credenciales, capas/domain no modificadas, CHANGELOG con encabezado y viñetas contiguos. **A1:** recuento E01–E16, I01–I16 y cuatro pares P/N definidos con oráculos. **A2:** doce negativas N01–N12, autoridad del servidor, A/B, token revocado, propiedad NULL y pruebas fail-closed futuras; sin atribuir PASS runtime. **A3:** correlación de cinco M01.01–05 y barrera M01.06 LISTO+A5; sólo documento conceptual, sin MySQL/HTTP/Angular ejecutado. **A4:** tras publicar por fast-forward/CAS, `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` deben terminar `completed/success` en SHA exacto o equivalencia de blobs acreditada. Si la CI está pendiente, mantener VALIDANDO con el propietario. **A5:** revisión independiente por Task Javier distinta, exigible y registrada antes de liberar dependencias que la requieran.

Rollback: `git revert` normal del commit documental, sin force-push, sin borrar BITACORA y sin transacciones de datos. No crear tablas, API, servicios, workers, pagos, migraciones, webhook, QA/main/PROD ni alterar dominios/secretos.
