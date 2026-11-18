# C14-01.M03.05 — Proveedor de evidencias CI de aceptación funcional

**Riesgo:** R2, integración GitHub Checks API read-only. **Alcance:** scripts/quality y workflow GitHub-hosted; NO es lector de identidad JWT ni reemplaza `IC1401TrustedAcceptanceSnapshotSource`. Autor DEV05, revisión A5 por otra Task Javier.

## A0 — Decisión técnica
Para aceptación funcional, una evidencia externa verificable es la conclusión nativa del proveedor GitHub Checks sobre SHA exacto. Se implementa un lector GET de sólo lectura contra el repositorio autorizado `solqaryn/Solqaryn`, con token efímero de runner y permisos mínimos `checks:read` / `contents:read`. Rechaza repositorios no previstos, SHA inválido, respuestas parciales o no JSON, resultados mezclados, nombres duplicados, errores y redirects. No existe worker persistente, cola, webhook, migración, DI de aplicación, productor de eventos ni cambio de API. Los eventos EV01–EV14 de M02.05 permanecen futuros sin ejecución.

## A1/A2 — Tests y seguridad
La suite Node:test exige ocho casos, incluidos positivos, negativos, frontera, fallos HTTP/JSON, no divulgación de token, SHA y repositorio exactos, duplicados, estados QUEUED/IN_PROGRESS/SKIPPED/CANCELLED/FAIL (nunca PASS). La lectura positiva NO concede acceso, tenant, recurso, QA o producción; `authorizesAccess` y `authorizesRelease` siempre false. Fallos externos retornan errores opacos.

## A3 — Integración REAL
El octavo caso consulta HTTPS real a la API GitHub Checks del SHA del runner usando GITHUB_TOKEN efímero; acredita integridad de respuesta y head_sha. **No presupone que el check requerido ya terminó:** si está pendiente, requiredPassed=false, sin inventar PASS. Los otros siete casos son adversariales de contrato con transport inyectado. No se toca MySQL/EF, JWT HTTP, Cloudinary ni Angular, por lo que esos runtimes NO_APLICAN a este changeset preciso, no se acreditan.

## A4 / A5 / rollback
Exigir CI `C14-01.M03.05 - real GitHub GET and adversarial tests` completed/success, Scope Lock y V5 exact-SHA y readback PLAN/COLA/BITACORA antes de LISTO. A5 independiente obligatoria, sin autoaprobación. Revert seguro con commit `git revert` sobre dev tras verificar HEAD. Sin QA, main, PROD, secretos ni cuentas de pago.

## V19 — Corrección A5_FAIL P2 de procedencia de GitHub Checks (2026-11-17)
La auditoría independiente DEV06 detectó que el check `completed/success` con nombre y SHA exactos podía proceder de una App no autorizada. El proveedor ahora exige también `app.id=15368` y `app.slug=github-actions` (identidad real observada en la API GitHub de SOLQARYN) y una URL de job bajo `github.com/solqaryn/Solqaryn/actions/runs/{id}/job/{id}`. Ante ausencia de metadatos, emisor ajeno, identidad falsificada o URL externa, `sourceValid=false` y `requiredPassed=false`; jamás autoriza despliegue o acceso. Se agregan dos pruebas adversariales para suplantación del emisor y origen externo, más la lectura HTTPS real vigente. Mantener el histórico A5_FAIL. Exigir CI Node dirigido, Scope Lock, V5 exact-SHA y una nueva A5 por otra Task Javier distinta de DEV05. No tocar token en logs ni QA/main/PROD.
