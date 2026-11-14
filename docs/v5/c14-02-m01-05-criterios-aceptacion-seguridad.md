# SOLQARYN V5 FINAL — C14-02.M01.05: criterios observables de aceptación funcional y técnica de seguridad

**Fecha:** 2026-11-14. **Autor:** PRIMARY DEV 01 JAVIER, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Rama:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Tipo:** criterios futuros documentales, **NO_EJECUTADOS**; no se han ejecutado pentests, requests, MySQL, navegador, backups ni pruebas de producción por esta microtarea.

**Fuentes:** [Standard V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [Sheet PLAN/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), [M01.01](./c14-02-m01-01-alcance-no-regresion-aceptacion-seguridad.md), [M01.02](./c14-02-m01-02-actores-interfaces-fronteras-seguridad.md), [M01.03](./c14-02-m01-03-inventario-codigo-evidencia-aceptacion-seguridad.md), [M01.04](./c14-02-m01-04-riesgos-supuestos-decisiones-aceptacion-seguridad.md). Claim PLAN fila 2058 PENDIENTE sin owner, COLA G/O SI y DAG fila 2019 F VALIDADO/G SI comprobados; la aptitud es para este documento M01.05, no para ejecución de release.

## 1. Contrato mínimo de evidencia cuando existan pruebas reales

Toda aceptación futura deberá incluir por caso: `CASE_ID`, requisito NR/R asociado, actor, rol efectivo, permisos vigentes, tenant A/B y sucursal, recurso own/foreign, request y payload redactados, entorno efímero/autorizado, fixture, condición previa, resultado esperado y observado, API/Angular/DB SHA, ejecución CI y `head_sha` exacto, `status=completed`, `conclusion=success` para cada prueba requerida, aserciones positivas y negativas, fallos reproducibles, regresiones, observador independiente y evidencia sin secrets ni PII. Una suite guardada en el repo **no demuestra** un PASS actual.

El umbral institucional final del bloque es **cero hallazgos P0/P1 sin mitigación aprobada**. No se atribuye aquí un conteo cero, ni se crea un resultado `P0/P1=0` por una lista de supuestos. Todo hallazgo confirmado debe tener criticidad, impacto, owner, reproducción y mitigación/retest independiente antes del gate.

## 2. Matriz de aceptación P01–P16 y N01–N16 — FUTUROS_NO_EJECUTADOS

| ID | Positivo observable a reproducir | Negativo que debe bloquear aprobación |
| --- | --- | --- |
| P01/N01 | A autenticado lee recurso propio con autorización | A consulta ID de B: respuesta deniega sin payload/datos de B |
| P02/N02 | Permiso RBAC vigente permite acción delimitada | Rol de lectura/revocado ejecuta mutación: bloqueado |
| P03/N03 | Contexto tenant deriva de membresía vigente | `X-Empresa-Id`/body/host manipulado no convierte B en A |
| P04/N04 | Sucursal autorizada opera recursos asignados | Sucursal ajena no puede leer ni mutar |
| P05/N05 | JWT válido firmado y vigente se valida | JWT alterado, expirado o sin vínculo actual falla |
| P06/N06 | Endpoint backend aplica política y propiedad propia | Invocar HTTP sin UI/guard con ID ajeno no evade autorización |
| P07/N07 | EF consulta propia con filtro/ownership | Query con `IgnoreQueryFilters` sin scope no devuelve B |
| P08/N08 | Venta/inventario propio persiste con auditoría | Escribir FK de otra empresa falla y revierte sin estado parcial |
| P09/N09 | Host/StoreKey configurado exacto resuelve tenant esperado | Host de sufijo parecido, wildcard o duplicado se rechaza |
| P10/N10 | Archivo propio con URL/token válido y TTL se obtiene | URL ajena/caducada/replay no se acepta |
| P11/N11 | PDF/exportación contiene sólo filas permitidas | Export cross-tenant no incluye datos B ni PII indebida |
| P12/N12 | Job autorizado conserva tenant e idempotencia | Payload/replay con B o rol retirado no duplica efectos |
| P13/N13 | Caché A se consume sólo por sesión/rol A válidos | Cambio A→B/revocación no reutiliza respuesta A |
| P14/N14 | Backup/restore sandbox conserva owner e integridad | Restore cruzado o secret leak se bloquea |
| P15/N15 | Error controlado produce auditoría sanitizada | Excepción no devuelve JWT, stack interno ni datos personales |
| P16/N16 | CI dirigida del SHA exacto y A5 independiente acreditan scope | `queued`/`in_progress`/`skipped`/`cancelled`/`failure` no es PASS requerido ni promoción |

**Estado actual de los 32 oráculos:** `FUTUROS_NO_EJECUTADOS`. El inventario M01.03 documenta que existen filtros/middleware/test xUnit, no un resultado de cada caso. Las pruebas P12/P13/P14 son condicionales a identificar implementaciones autorizadas: su ausencia no autoriza inventar colas, eventos, cachés o backups.

## 3. Requisitos no funcionales NF01–NF12, sujetos a medición

| ID | Métrica/evidencia futura | Resultado actual |
| --- | --- | --- |
| NF01 | Rechazos 401/403/404 sin exposición de IDs ni datos ajenos | NO_MEDIDO |
| NF02 | Cada prueba reporta SHA de commit y job exacto | NO_MEDIDO |
| NF03 | Fixtures de dos tenants separados, sintéticos y limpiados | NO_MEDIDO |
| NF04 | Prueba de revocación rol/JWT no depende de caché obsoleta | NO_MEDIDO |
| NF05 | Registros de auditoría minimizan PII y conservan correlación | NO_MEDIDO |
| NF06 | Archivo temporal y link firmado respetan caducidad y scope | NO_MEDIDO |
| NF07 | Reintentos no duplican transacciones ni permisos | NO_MEDIDO |
| NF08 | Crash/timeout no degrada políticas fail-closed | NO_MEDIDO |
| NF09 | Cache y backup/restore no mezclan tenants | NO_MEDIDO |
| NF10 | CI MySQL/EF usa servicios efímeros autorizados y rollback | NO_MEDIDO |
| NF11 | Reporte de hallazgos P0/P1 contiene resolución aprobada/pendiente | NO_MEDIDO |
| NF12 | Decisión de release y QA/PROD se documenta fuera de este scope | NO_APROBADO |

No se inventan umbrales numéricos de latencia, retención, TTL o tasa de éxito. M01.04 D01–D10 decidirá propietarios y magnitudes de pruebas antes de automatizarlas, con control de seguridad y sin servicios de pago.

## 4. Puertas de salida y criterios de bloqueo

- Un `PASS_DOCUMENTAL` de M01.05 sólo acredita la definición verificable de escenarios, no cero P0/P1 ni protección runtime.
- La ausencia de prueba del endpoint, empresa B, revocación, archivo, restauración o entorno válido significa `NO_VERIFICADO`, no `PASS`.
- Si hay fallo de una prueba **pertinente** en SHA exacto, se mantiene la microtarea de implementación respectiva VALIDANDO, con owner original, o A5_FAIL causal según contrato. No se sobreescriben eventos históricos.
- Un test runtime de MySQL requiere MySQL efímero en GitHub Actions, no Aiven live. Angular requiere build/tests y Playwright donde causal. No consultar ni alterar producción, QA o main sin autorización.
- `M01.06` no queda habilitada automáticamente por publicar este documento: requiere sus propios predecesores, revisión independiente y DAG actual. `M02–M06` también requieren gates propios.

## 5. A0–A4 de la entrega documental, independencia A5 y rollback

**A0:** validar 16 pares positivos y negativos P01–P16/N01–N16, NF01–NF12, no secretos y no cabeceras vacías en CHANGELOG. **A1:** revisar resultados esperados observables y casos adversariales, **sin ejecutarlos**. **A2:** correlacionar tenant A/B, JWT/RBAC, DB, archivos, errores y el mecanismo fail-closed sin proclamar auditoría runtime. **A3:** trazar M01.01–M01.04, fuentes Git y Standard V5, sin asumir cobertura no inspeccionada. **A4:** commit fast-forward/CAS de únicamente este Markdown y `CHANGELOG_AI.md`, blob y diff verificados, exact-SHA Scope Lock y check V5 `completed/success`, readback PLAN/COLA/BITACORA; pruebas backend/frontend/security sin cambios de runtime pueden marcarse `NO_APLICA` con motivo y no constituyen PASS de funcionalidad.

**A5_PENDIENTE:** revisión de Task Javier diferente de PRIMARY DEV 01, no autoaprobación ni QA/main/PROD. No se implementan workers, webhooks, dispatcher, endpoints, MySQL, migraciones, cambios de JWT/RBAC, credenciales, proveedores R2, host, DNS o compras. **Rollback:** `git revert` normal, tras revalidar HEAD y conflictos, sin force-push ni reescritura de BITACORA.
