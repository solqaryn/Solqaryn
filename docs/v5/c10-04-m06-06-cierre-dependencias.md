# C10-04.M06.06 — cierre de estado y mapa de dependencias (V22)

**Autoría de la entrega:** SOLQARYN PRIMARY DEV 03 JAVIER, Task `6adc2e4bd8a48191ac7d8641ed95a1a3`. Repositorio `solqaryn/Solqaryn`, rama `dev`, `PROJECT_SCOPE_LOCK=STRICT`. Fuente del trabajo: STANDARD V5, PLAN_MAESTRO fila 1585, columna D «Cerrar estado, liberar el siguiente lote y actualizar el mapa de dependencias». Reclamo V17 `SQV17_C10_04_M06_06` sobre H:I, lock `2000001585`. Este archivo es una **entrega de gobernanza documental R0, con revisión cautelar R2 de las dependencias operativas**: no implementa ni acciona un worker o restauración.

## A0 — hecho verificable y límites

PLAN!E resume el objetivo acumulativo del bloque C10-04 («aislamiento, idempotencia, reintentos, límites y restauración»). La salida **atómica** de este ID es registrar el estado y condicionar la liberación del siguiente lote, **no** certificar una restauración real, pago, cambio de infraestructura o producción. Se verificó PLAN H= PENDIENTE/I sin dueño, COLA G/O=SI, DAG_TECNICO F=VALIDADO/G=SI antes del CLAIM exclusivo; posteriormente PLAN H=EN_PROGRESO/I=DEV03, sin otro WIP propio. Las pruebas y limitaciones preexistentes son fuentes verificables, no permiso R3.

**Archivos cambiados por este incremento:** solo este documento y una sección aditiva autocontenida de `CHANGELOG_AI.md`. Ningún cambio a Domain/Application/Infrastructure/API/Angular, EF/migraciones, DTO, tablas, JWT, RBAC, datos/PII, secretos, servicios, workers, alarmas, configuración CI, despliegues ni cuentas. Impacto en consumidores: exclusivamente las decisiones humanas y el cálculo ya existente de la hoja; no se modifica una fórmula o un servicio. Reversión de código: `git revert` normal del commit tras revalidar HEAD; jamás force-push, reset ni borrado de BITACORA.

## A1 — auditoría positiva y negativa de los 11 predecesores

Se leyeron PLAN_MAESTRO filas **1574–1584**, `C10-04.M05.01–06` y `C10-04.M06.01–05`: las **11/11** aparecen `LISTO` y sus notas K preservan revisiones `A5_PASS` por otras Tasks Javier, con alcances individuales explícitamente acotados. Este hecho se reconcilia con DAG_TECNICO, fila **1546**, `C10-04.M06.06`, `F=VALIDADO/G=SI`. Las evidencias de prueba difieren:
- `M05.01`: reglas xUnit de snapshot, no lease durable; `M05.02`: DTO y errores seguros, sin PSP; `M05.03`: MySQL 8.4 efímero con CAS secuencial, **no** dos trabajadores reales concurrentes ni EF de negocio.
- `M05.04`: aceptación aislada; `M05.05`: ataques y denegación en pruebas; `M05.06`: **7/7** Chromium real sobre sesión/HTTP sintéticos y ensayo SQL/MySQL aislado. No se convierte fixture en autorización HTTP real.
- `M06.01`: auditoría de mocks; `M06.02`: manifiesto de blobs y diffs; `M06.03`: registro de riesgos L01–L08; `M06.04`: rollback transaccional verificable en MySQL 8.4 efímero (sin Aiven ni restauración de datos comerciales); `M06.05`: solicitud/revisión independiente limitada, no una liberación R3.

**Prueba adversarial del gate:** aunque todas las once previas estén `LISTO+A5`, la propia `M06.06` requiere sus A0–A4 y después **A5 de una Task Javier distinta del autor DEV03**. Mientras falte cualquiera, ningún sucesor que dependa de ella queda habilitado por decreto. El resultado global del proyecto, migración, seguridad en runtime real, recuperación de backup y QA/main/PROD **NO_CERTIFICADO**.

## A2/A3 — seguridad, aislamiento e integración según el diff

**NO_APLICA al documento R0** la ejecución nueva de JWT firmado, HTTP real, tenant A/B en API, integración MySQL, Chromium y pruebas de proveedor: este changeset no cambia runtime ni crea autorización. Las pruebas reales precedentes solo sostienen los alcances explícitos de su respectivo ID; se conservaron los negativos de tenant/revocación y la distinción entre persistencia temporal y efectos productivos. Este NO_APLICA local **no** cancela requisitos R2/R3 pendientes de workers, autenticación, recuperación ni migraciones.

## DAG / COLA — sucesoras sin forzar fórmulas

En DAG_TECNICO se observaron, entre otras, `C11-03.M03.01` (DAG fila 1631) y `C12-01.M03.01` (DAG fila 1703): ambas incluyen `C10-04.M06.06+A5` entre sus prerrequisitos. En la lectura previa figuraban **G=NO**; deben seguirlo mientras su gate siga abierto. Tras LISTO y A5 independiente real, reconsultar el DAG y COLA y **permitir solo el recálculo de fórmulas preexistentes**; nunca modificar G/O para simular disponibilidad. Cada sucesor conserva además sus demás dependencias, restricciones de entorno y exclusividad V17.

## A4 — publicación, CI exact-SHA, evidencias

Exigir tras publicar: comparar HEAD fast-forward y blobs de los archivos afectados, `verify-solqaryn-project-scope` `completed/success` del SHA, V5 de procedencia CHANGELOG cuando aplique, skill de gobierno consistente con HEAD, BITACORA 26 columnas y PLAN/COLA readback. Si un check requerido está queued, failed, skipped, cancelled o in_progress, conservar la unidad `VALIDANDO` y registrar SHA/run sin decir PASS. El SHA fuente se registra por evento institucional tras publicación, no se inventa aquí.

Fuentes de lectura: `docs/v5/c10-04-m06-01-auditoria-tests-reales-vs-mocks.md`; `docs/v5/c10-04-m06-02-version-diff-artefactos-reproducibilidad.md`; `docs/v5/c10-04-m06-03-limitaciones-riesgos-decisiones.md`; `docs/v5/c10-04-m06-04-rehearsal-rollback.md`; `docs/v5/c10-04-m05-06-regresion-rendimiento-accesibilidad.md`, además del STANDARD V5 y Sheet institucional. No son sustitutos de logs de GitHub Actions.

## A5 y condiciones de salida

**A5_PENDIENTE**, reservado a otra Task Javier habilitada (nunca DEV03). El revisor debe leer documento, PLAN D/E, las once evidencias A5 y DAG/código cuando corresponda, comprobar que no se presentan como genuinos los efectos sintéticos y conservar la historia de fallos. Puede rechazar con motivo verificable. No hay relevo de owner ni permiso R3. La conclusión autorizable es **cierre técnico/documental en dev** con puertas dependientes del A5 institucional; no hay certificación de restablecimiento productivo, worker real, recuperación de backup comercial, RPO/RTO, Aiven, Cloudflare, JWT/HTTP real o QA/main/PROD.
