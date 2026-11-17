# SOLQARYN V19 — C09-04.M03.02: frontera de idempotencia del caso de uso

Autor DEV02: 6af16bb9492c8191b8e8c7d606f11507. Repositorio solqaryn/Solqaryn, dev, PROJECT_SCOPE_LOCK=STRICT; PLAN fila 1419, candado V17 2000001419. Riesgo R2. Fuente: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5.

## A0 — Necesidad real y alcance

MensajesOutboxService.RegistrarAsync ya valida membresía y replay tenant-bound antes de AddAsync. Se detectó una brecha: aplicaba Trim a Idempotency-Key antes de invocar la nueva validación Domain, borrando CR/LF/TAB en bordes y permitiendo normalizar una clave adversarial hacia una clave existente. Se valida la entrada ORIGINAL con char.IsControl ANTES de Trim y ANTES de consultas al repositorio, respetando el orden previo de autorización de membresía empresarial. Se conservan claves imprimibles, normalización de espacios, idempotencia por empresa y contrato público. Solo cambian Application/Services/MensajeOutboxService.cs, pruebas xUnit existentes, esta nota y CHANGELOG. No cambian API/DTO, EF, migración, tenant real, DI, Angular, SMTP/WhatsApp, secretos o datos productivos. Rollback: git revert normal contra HEAD revalidado.

## A1 — Regresiones que detectan el defecto

Cinco entradas adversariales CR/LF/TAB/NUL/U+001F, positivas con Unicode y JSON multilínea, fronteras 200/201, negativa empresa B sin membresía y correlación con controles. Las pruebas N74D previas continúan comprobando replay igual/sin segundo AddAsync, conflicto de payload y normalización. CI GitHub-hosted .NET 10 Release por SHA exacto requerida; tests publicados no equivalen a PASS.

## A2 — Seguridad

Rechazo fail-closed sin exponer claves en logs. Se conserva autorización server-side por empresa y no se despachan efectos. Los dobles de pruebas no certifican JWT/RBAC HTTP real, HMAC de webhooks ni aislamiento MySQL. El cambio no otorga capacidades adicionales.

## A3 — Integración

Prueba dirigida in-process Domain+Application+xUnit existente. MySQL 8.4, EF, Playwright, HTTP autenticado, SMTP y WhatsApp real: NO_APLICA para este diff, porque no se modifican esas capas. No se declara antireplay distribuido ni entrega real.

## A4 y A5

Requiere build/test real Release, controles causales de seguridad, Scope Lock y V5/CHANGELOG SUCCESS por SHA o equivalencia documentada; SKIPPED/QUEUED/IN_PROGRESS/CANCELLED/FAIL no son PASS requerido. Leer PLAN/COLA/DAG/BITACORA tras transición V17, conservar propietario. A5 solo otra Task Javier; no QA/main/PROD. Métricas no verificadas: NO_MEDIDO.
