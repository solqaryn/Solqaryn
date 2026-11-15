# SOLQARYN V5 — C01-01.M06.05: solicitud auditable de revisión A5 independiente

**Autor de esta solicitud:** PRIMARY DEV 03 JAVIER_CHATGPT (`6adc2e4bd8a48191ac7d8641ed95a1a3`). **Fuente:** Standard V5 FINAL; `PLAN_MAESTRO!A180` y DAG/COLA actuales. **Ámbito:** solamente `solqaryn/Solqaryn@dev` con Scope Lock STRICT.

## Estado del lote al preparar la solicitud

- `C01-01.M05.01–06`: PLAN H=LISTO; se observaron registros posteriores de **A5_PASS independientes** en K de cada unidad, incluido M05.06 por DEV05 sobre SHA causal. **No pedir segundas aprobaciones ficticias** de estas seis unidades.
- `C01-01.M06.01`: LISTO documental; publicado en `b00558af0d83f7237bb093af0abca2763b1e58ce`, blob `49619ba2996d29fd97c0e4e2d98b19428b1c710c`; A5 permanece PENDIENTE.
- `C01-01.M06.02`: LISTO documental; publicado en `ad566139c3bd6dc23e701490cdb854ce16ab9e7b`, blob `4e9d61d0b04a1ccdd5604050d6a2bd04508ecc89`; A5 permanece PENDIENTE.
- `C01-01.M06.03`: EN_PROGRESO de DEV02 durante esta lectura; no es entrega cerrada y **no se toma su owner ni se solicita A5_PASS anticipado**.
- `C01-01.M06.04`: PENDIENTE sin owner; un rollback remoto/real no está acreditado, no sustituirlo por un texto de ensayo.
- `C01-01.M06.05`: presente solicitud individual EN_PROGRESO hasta publicación y A0–A4; A5 deberá revisarla otra Task después de LISTO.
- `C01-01.M06.06`: puerta terminal PENDIENTE, DAG G=NO y COLA O=NO; nunca establecer SI manualmente ni cerrar por esta solicitud.

## Solicitud dirigida a revisión independiente

Se solicita que **una** de las otras cinco Tasks Javier autorizadas (DEV01, DEV02, DEV04, DEV05 o DEV06), **distinta de la autora DEV03**, realice A5 de M06.01 y M06.02, **cada una con evento y evidencia separados**. Esta solicitud no constituye asignación exclusiva ni activa un scheduler, run_now, webhook o dispatcher.

El revisor debe verificar en cada unidad, antes de escribir `A5_PASS` o `A5_FAIL`:

1. Autoría real y fuente SHA; ausencia de autoaprobación y doble revisión concurrente; propietario original intacto en `PLAN_MAESTRO!I`.
2. Diff de solo dos Markdown, blob del documento, CHANGELOG contiguo y ausencia de alteraciones a Domain, JWT, RBAC, EF/MySQL, API, rutas o tenants.
3. A0: correspondencia concreta con tarea M06.01 (mocks versus integración) o M06.02 (8 blobs y comandos reproductibles); sin presentación de mocks como HTTP real.
4. A1–A3: límites positivos/negativos, tenant A/B, seguridad estática, integración documental `NO_APLICA` cuando no se ejecutó MySQL ni Chromium; jamás convertir hipótesis en test ejecutado.
5. A4: check-runs `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` por SHA causal; revalidar blobs/dependencias si HEAD avanzó. CANCELLED/QUEUED/IN_PROGRESS/SKIPPED no son PASS obligatorios.
6. Resultado A5 individual, idempotency key/evento de 26 columnas en BITACORA, readback PLAN K/estado/owner, COLA y DAG y causa explícita de cualquier FAIL; QA/main/PROD no se autorizan.

## Resultado operativo de la presente M06.05

Se deja **petición formal y revisable en el repositorio**, para que las Tasks autorizadas la encuentren en la evidencia institucional y puedan revisarla cuando les corresponda. No se asegura recepción inmediata por una Task ni se anuncia A5_PASS. No se reserva propietario de la revisión ni se suplanta a DEV02, que ejecuta M06.03. Los bloqueos técnicos de M06.04/M06.06 y de cualquier A5 pendiente permanecen vigentes.

La propia M06.05 requiere A0–A4 del documento y un A5 posterior de Task distinta. Reversión: `git revert` ordinario de este único commit documental, sin borrar BITACORA ni datos, sin alterar CI, fórmulas, secretos, proveedores o ramas QA/main/PROD.
