# C01-04.M03.05 — Reparación P2 de límite y enumeración del timeline

**Repositorio:** solqaryn/Solqaryn@dev. **Scope Lock:** STRICT. **PLAN:** fila 270, LISTO histórico bajo owner original DEV06 (6af16bbad26881918c283abe06e1f32a). **Reparación técnica asistida:** DEV03 (6adc2e4bd8a48191ac7d8641ed95a1a3). **Riesgo:** R2 por límites, aislamiento e integridad de evidencias. Sin autorización productiva.

## Defecto A5_FAIL P2 y remediación

Una auditoría independiente detectó que `V5UniversalCaseFileBoundedTimelineWorker.Project` solo comprobaba `request.Events.Count <= 64` antes de enumerar. Una colección `IReadOnlyCollection<TimelineEvent>` malformada puede reportar Count=1, producir 65 o más eventos válidos y retornar proyección superior al límite. `GetEnumerator/MoveNext` también puede lanzar excepciones ordinarias fuera de captura; es un defecto del límite y del oráculo de sanitización, no evidencia de explotación HTTP.

La corrección limita el recuento observado a 64, rechaza discrepancia entre `Count` y los elementos enumerados y captura fallos normales de la fuente con `CASEFILE_TIMELINE_SOURCE_UNAVAILABLE` sin devolver proyección parcial. Conserva coincidencia estricta de tenant/caso, versiones, UTC, identidades y claves, ordenamiento determinista, resultado inmutable y cancelación propagada.

## Matriz A0–A5 y reversión

- A0: diff acotado al worker en Application, xUnit de la misma unidad, este documento y CHANGELOG. Ninguna interfaz, API, tabla, servicio ni migración se modifica.
- A1: tres xUnit nuevos, un proveedor con Count falso que emite 65 eventos (debe denegar), mismatch Count/iteración (debe denegar) y enumerador que falla después del primer elemento (debe sanitizar). Los xUnit positivos/negativos previos siguen siendo regresión obligatoria.
- A2: conservar rechazo tenant B, case ID ajeno y prueba de no exponer timeline parcial, sin conceder lectura; seguridad HTTP/JWT NO_APLICA al diff, no se afirma haber probado servidores reales.
- A3: capa Application in-process, MySQL/EF/Playwright NO_APLICA para este cambio; no se prueba persistencia comercial ni proveedores.
- A4: sólo acreditar en GitHub-hosted .NET Release/xUnit, autorización y seguridad pertinentes, Scope Lock y V5 en SHA exacto `completed/success`; NO interpretar `queued/in_progress/skipped` como PASS. Registrar SHA y readback BITACORA.
- A5: la revisión A5_FAIL histórica sigue intacta y bloquea DAG donde corresponda. Revisión correctiva independiente debe proceder de una Task Javier distinta del autor DEV06 y de DEV03 reparador; ningún autopase.
- Rollback: `git revert` normal del commit después de revisar HEAD y escritor; jamás fuerza, reset, QA/main/PROD, secretos o datos productivos.
