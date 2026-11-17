# SOLQARYN V19 — C09-04.M02.05: contratos de evento, idempotencia, correlación y límites

**ID:** C09-04.M02.05 · PLAN_MAESTRO 1416 · **Autor:** PRIMARY DEV05 JAVIER (`6adc2e55f47c81919f6374e561687076`) · `solqaryn/Solqaryn@dev` · `PROJECT_SCOPE_LOCK=STRICT` · **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`.

**Riesgo funcional:** R2 idempotencia, concurrencia, tenant A/B, secretos y callbacks. **Cambio real:** R0 exclusivamente documental. La orden V5 no permite implementar eventos/dispatchers ahora; este documento **NO CREA, HABILITA NI EJECUTA** eventos, workers, colas, webhooks, jobs, endpoints, tablas, migraciones, proveedores, envíos SMTP/WhatsApp, feature flags, secretos ni datos de terceros. Todos los modelos futuros están expresamente pendientes de código y pruebas.

## A0 — Fuentes existentes, estados e interfaces

| Fuente leída `backend/src/` | Blob fuente | Realidad comprobable / límite |
| --- | --- | --- |
| `Domain/Entities/MensajeOutbox.cs` | `891572626c19cd6cf8f522fca5ec876333286ffb` | `EmpresaId`, `EventoId` Guid, `ClaveIdempotencia`, `TipoEvento`, `PayloadJson`, `CorrelationId`, UTC, estado Pendiente/Procesando/Fallido/Entregado/DeadLetter, intentos y transiciones; claim durable externo no demostrado |
| `Application/Services/MensajeOutboxService.cs` | `a00d353c474d7d7f785a58f443c66c5f12a542f7` | Revalida membresía tenant y clave, compara solicitud antes de stage; no demuestra ejecución exactamente una vez ni duplicidad entre procesos |
| `Application/Services/OutboxRetryPolicy.cs` | `a9bcacda24a7ba031a271b527d8ca2043a8510b9` | Policy bounded/jitter determinista, máximos configurables, decisión dead-letter; no prueba envío ni transporte |
| `API/Controllers/MensajesOutboxController.cs` | `b6d3fa67c9ecdb35072fa59dad1e71c1e351be81` | POST `api/outbox/tenants/{empresaId}` + `Idempotency-Key`, verifica empresa autorizada, ProblemDetails 400/403/409 en ramas tratadas; no endpoint de campañas |
| `Domain/Entities/EmailEmpresarial.cs` | `718421df52d525712ee9512d0c164ffebfcf80a5` | Correo tenant-bound y estados distintos aceptado-proveedor/entregado/rebotado; no opt-in comercial demostrado |
| `API/Controllers/WhatsAppController.cs` | `efc1e79625e62925efe7d1e47040eae91fb12dc6` | Webhook actual compara token de verificación; **NO** implementar ni atribuir HMAC o replay a ese token |

Las etapas M01.01–06 y M02.01/.02/.04 fijan 20 invariantes de owner, 12 interfaces lógicas, flags propuestos y seguridad. Ningún campo conceptual nuevo constituye propiedad de código existente.

## Modelo futuro de envoltura de eventos (PROPUESTA_NO_IMPLEMENTADA)

| ID | Campo lógico futuro | Regla de validez |
| --- | --- | --- |
| EV01 | `EventId` | Identificador único no reutilizable; NO equivale a autorización |
| EV02 | `EmpresaId` | Tenant confirmado server-side y consistente en toda referencia |
| EV03 | `ActorRef` | Actor autenticado/membresía vigente, no impersonación |
| EV04 | `TipoEvento` | Allowlist/versionado, nunca nombre de clase arbitraria o código ejecutable |
| EV05 | `SchemaVersion` | Versión positiva, rechazar incompatibilidad desconocida |
| EV06 | `AggregateRef` | Referencia del mismo tenant, existencia y ownership comprobados |
| EV07 | `CorrelationId` | Trazabilidad opaca, no acceso/permiso |
| EV08 | `CausationId` | Relación de causalidad a un evento previo autorizado; propuesta |
| EV09 | `IdempotencyKey` | Identidad durable por empresa+canal+operación+destino+payload |
| EV10 | `PayloadDigest` | Huella canónica sin revelar contenido sensible; propuesta |
| EV11 | `OccurredUtc` | UTC validado y orden lógico, no timestamp del cliente como autoridad |
| EV12 | `DispatchPolicyRef` | Política permitida de cuota/retry; no endpoint habilitado |
| EV13 | `ConsentEvidenceRef` | Consentimiento por finalidad/canal/destinatario, revocable |
| EV14 | `DeliveryState` | Estado tipado/monótono, distinguir intención, acuse y entrega |
| EV15 | `ProviderRef` | Referencia opaca tenant-bound, sin token/DSN en payload |
| EV16 | `AttemptNumber` | Entero acotado y transición idempotente con CAS durable |

**EV01–EV16 son nombres descriptivos de contrato futuro, no propiedades de `MensajeOutbox` existentes.** El modelo actual conserva sus propios campos documentados en el código; `PayloadJson` existe hoy, pero este documento no supone que actualmente esté redacted/cifrado ni autoriza persistir PII comercial. La protección concreta de payload requiere diseño, implementación y test propios.

## Invariantes de evento y límites EI01–EI20

1. **EI01:** tenant y recursos se verifican server-side en cada registro, claim, consulta y acuse.
2. **EI02:** empresa de ruta/cabecera/body no equivale al scope autorizado.
3. **EI03:** revocación de membresía, permiso o consentimiento comercial impide efectos nuevos.
4. **EI04:** mismo key, tenant y payload nunca debe duplicar intención ni entrega efectiva.
5. **EI05:** mismo key con otro payload debe rechazarse, sin filtrar la respuesta original.
6. **EI06:** distinta empresa no comparte idempotency-key, estado ni caches.
7. **EI07:** dos trabajadores simultáneos requieren claim durable exclusivo, no mera lectura+escritura.
8. **EI08:** estado de intento y transición se validan; fallo no produce `Entregado`.
9. **EI09:** accepted-provider y delivered-recipient son estados semánticamente diferentes.
10. **EI10:** un evento terminal/DeadLetter no vuelve a procesarse sin reapertura formal autorizada.
11. **EI11:** retries y backoff son finitos; jitter reproducible no altera tenant/privilegios.
12. **EI12:** cancelación/timeouts preservan evidencia previa sin double-send posterior.
13. **EI13:** CorrelationId, CausationId y EventId no son credenciales ni grants.
14. **EI14:** payload, template, PII y secretos no se registran indiscriminadamente en logs.
15. **EI15:** callback futuro requiere firma HMAC válida, ventana temporal, proveedor y nonce único; **no existe aquí**.
16. **EI16:** replay con nonce duplicado o firma incorrecta no altera estado ni dispara envío.
17. **EI17:** fallo del proveedor no habilita fallback entre SMTP y WhatsApp sin consentimiento específico.
18. **EI18:** no se infiere tenant a partir de contacto legado NULL o a partir de URL externa.
19. **EI19:** auditoría mínima registra identidad, empresa, resultado, tiempo UTC y correlation id sin cuerpo sensible.
20. **EI20:** ningún resultado de documentación, compilación, DAG/SI o A5 aislada autoriza producción, dispatch ni migración.

## Contratos de concurrencia y límites propuestos

**Identidad lógica:** `(empresa, canal, operación, destinatario, idempotency-key)` más huella canónica del contenido; normalización exacta, índice único, aislamiento transaccional y CAS se diseñarán y acreditarán en una microfase de persistencia posterior. No afirmar que existe hoy un índice suficiente para todos los canales. **Retry:** el `OutboxRetryPolicy` actual tiene límite permitido y jitter bounded; la política de canal no se implementa por este documento. **Causalidad:** propagar un identificador opaco no privilegia al usuario; exigir predecesor de la misma empresa y tipos allowlisted. **Webhook:** un token de verificación header existente no es validación criptográfica del payload; futuro HMAC/replay requiere almacenamiento efímero real y pruebas. **Control de carga:** cuotas por tenant/canal, saturación, tamaño máximo de payload/plantilla y circuit-breaker requerirán límites medidos, no cifras inventadas.

## Casos futuros de prueba, explícitamente NO_EJECUTADOS

**P01–P10 positivos futuros:** registrar intención tenant A autorizada, replay idéntico sin doble efecto, idempotencia entre dos callers, estado terminal, retry con jitter acotado, opt-in válido, acuse idempotente, auditoría opaca, respuesta bajo cuota permitida, desactivación sin borrar evidencia. **N01–N14 negativos futuros:** A→B, rol revocado, opt-out, payload distinto misma clave, doble claim, contacto NULL, secreto en log, replay de nonce, HMAC inválida, provider timeout, retry fuera de cuota, fallback no consentido, evento de esquema desconocido, exportación de trazas de B. Todos **FUTUROS_NO_EJECUTADOS**. Exigir .NET/xUnit, HTTP JWT/RBAC tenant A/B, MySQL 8.4 efímero y proveedor simulado con fallos cuando sea causal a nuevas implementaciones; no fingir resultados o números de tests.

## Calidad A0–A5, DAG y reversión

- **A0:** únicamente esta especificación Markdown y una sección completa de `CHANGELOG_AI.md`, sin nuevas colas/eventos/scripts/workflows ni código.
- **A1:** trazabilidad estática EV01–16, EI01–20, diez P/catorce N propuestos, y cinco fuentes existentes sin atribuir pruebas ejecutadas.
- **A2:** protecciones tenant, JWT, RBAC, consentimiento, idempotencia, replay y secretos son contratos futuros fail-closed; NO_APLICA verificación de una API nueva por cambios solo documentales.
- **A3:** MySQL/HTTP/SMTP/WhatsApp/Angular/Chromium reales **NO_APLICA al changeset documental**. Los propios checks de compilación ajenos no certifican envíos.
- **A4:** publicar exclusivamente en dev por fast-forward CAS, verificar `verify-solqaryn-project-scope` y V5 de encabezados/procedencia `completed/success` **del SHA exacto**, revisar blobs y readback Sheets/BITACORA por ID; `skipped`, `cancelled` e `in_progress` no pasan.
- **A5:** otro TASK_ID de las cinco Tasks Javier realiza revisión independiente, nunca el autor; no promover QA/main/PROD.
- **Dependencia:** M02.05 LISTO+A5 no libera por sí sola código de M03 o entrega real. Respetar M02.06 y fórmulas DAG/COLA, sin forzar G/O.
- **Rollback:** `git revert` normal del changeset documental tras revisar HEAD y otros escritores. Conservar BITACORA histórica, estado de proveedores, mensajes ya entregados y efectos de otros autores; prohibido reset/force-push.
