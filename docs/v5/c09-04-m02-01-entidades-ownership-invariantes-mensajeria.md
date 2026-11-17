# SOLQARYN V19 — C09-04.M02.01: entidades, ownership e invariantes de mensajería multicanal

**Autor:** PRIMARY DEV05 JAVIER (`6adc2e55f47c81919f6374e561687076`). **Plan:** C09-04.M02.01, fila PLAN_MAESTRO 1412; contrato M02, `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`). **Riesgo del dominio:** R2 por tenant/consentimiento/privacidad. **Riesgo del changeset:** R0, exclusivamente documental.

## A0 — Alcance y fuentes existentes, leídas en dev

Esta especificación define propiedad e invariantes del futuro modelo de mensajería comercial; **no crea código, tablas, DTO, endpoints, workers, eventos, webhooks, proveedores, flujos de envío, secretos, migraciones ni configuración de producción**. Continúa M01.01–M01.06: NR01–NR14 de no-regresión, A01–A14 actores, I01–I12 interfaces, F01–F09 fronteras, P01–P16/N01–N16 aceptación futura y nueve gates G01–G09. M01.06 alcanzó LISTO y A5 independiente. El avance documental jamás autoriza un envío.

| Fuente existente en `backend/src/` | Blob leído | Límite del contrato real |
| --- | --- | --- |
| `Domain/Entities/MensajeOutbox.cs` | `891572626c19cd6cf8f522fca5ec876333286ffb` | EmpresaId, EventoId, clave idempotente, estados, intentos; **no** claim atómico/dispatcher certificado |
| `Domain/Entities/EmailEmpresarial.cs` | `718421df52d525712ee9512d0c164ffebfcf80a5` | Destinatario, plantilla, estados y referencia proveedor por empresa; **no** consentimiento de marketing |
| `Domain/Entities/ConfiguracionWhatsAppEmpresa.cs` | `e861f83314287f59b00290f579cae0247d95a780` | Empresa, número E.164, referencias secretas opacas, bandera y versión; **no** proveedor conectado |
| `Application/Services/MensajeOutboxService.cs` | `a00d353c474d7d7f785a58f443c66c5f12a542f7` | Valida scope de empresa antes de stage y coteja idempotencia; **no** demuestra unicidad concurrente DB |
| `Application/Services/OutboxRetryPolicy.cs` | `a9bcacda24a7ba031a271b527d8ca2043a8510b9` | Retries bounded y jitter reproducible; **no** garantiza exactly-once |

Estos cinco archivos y las entidades `Empresa` y `UsuarioEmpresa` ya se han identificado en el inventario M01, pero sólo las capacidades expresas anteriores se acreditan por esta lectura. `Cliente.EmpresaId=NULL` legado no adquiere propiedad por defecto. No se ejecutó HTTP, SMTP, WhatsApp, JWT ni MySQL.

## Catálogo conceptual de propiedad E01–E16

| ID | Objeto | Dueño / vínculo y estado |
| --- | --- | --- |
| E01 | Empresa | Raíz de tenant verificado; existente |
| E02 | UsuarioEmpresa | Membresía y rol vigente del actor; existente |
| E03 | MensajeOutbox | Intención durable con EmpresaId; existente |
| E04 | EmailEmpresarial | Mensaje de correo por empresa; existente |
| E05 | ConfiguracionWhatsAppEmpresa | Configuración por empresa con secretos opacos; existente |
| E06 | IntencionComunicacion | Empresa, actor, canal y finalidad; **PROPUESTA_NO_IMPLEMENTADA** |
| E07 | DestinatarioVerificado | Contacto, owner y relación tenant; **PROPUESTA_NO_IMPLEMENTADA** |
| E08 | ConsentimientoCanalFinalidad | Opt-in revocable por empresa/sujeto/canal/finalidad; **PROPUESTA_NO_IMPLEMENTADA** |
| E09 | EvidenciaConsentimiento | Origen, versión, UTC y revocación; **PROPUESTA_NO_IMPLEMENTADA** |
| E10 | PlantillaVersionada | Tenant y variables allowlisted; **PROPUESTA_NO_IMPLEMENTADA** |
| E11 | ClaveIdempotenteScoped | Scope de tenant/canal/destino/intención/payload; **PROPUESTA_NO_IMPLEMENTADA** |
| E12 | IntentoEntrega | Intentos acotados, estado y correlación; **PROPUESTA_NO_IMPLEMENTADA** |
| E13 | AcuseProveedor | Referencia opaca y estado normalizado; **PROPUESTA_NO_IMPLEMENTADA** |
| E14 | AntiReplayWebhook | Firma, nonce, ventana y tenant/proveedor; **PROPUESTA_NO_IMPLEMENTADA** |
| E15 | RegistroSupresion | Opt-out por sujeto/canal/finalidad; **PROPUESTA_NO_IMPLEMENTADA** |
| E16 | EventoAuditoriaComunicacion | Actor, empresa, resultado y correlación sin cuerpo sensible; **PROPUESTA_NO_IMPLEMENTADA** |

Ningún objeto propuesto implica una tabla EF, endpoint, índice, campo, migración, API o permiso autorizado. Política de consentimiento transaccional frente al comercial queda abierta; ausencia de opt-in impide mensajería promocional.

## Relaciones de ownership y invariantes INV01–INV20

1. **INV01:** toda intención lleva un único tenant positivo comprobado server-side.
2. **INV02:** EmpresaId proporcionado por browser, ruta o cabecera es una afirmación no confiable.
3. **INV03:** membresía/rol/permisos se revalidan con revocación antes de cada efecto.
4. **INV04:** contacto, sucursal, template, destinatario y emisor deben concordar con tenant autorizado.
5. **INV05:** contacto con EmpresaId nulo o ambiguo se bloquea; no adquiere dueño por inferencia.
6. **INV06:** marketing exige consentimiento vigente por canal, destinatario, finalidad y tenant.
7. **INV07:** opt-out bloquea nuevas entregas comerciales y conserva auditoría legítima.
8. **INV08:** evidencia de consentimiento tiene fuente, versión, timestamp UTC y revocación.
9. **INV09:** idempotencia se acota por empresa, operación, canal, destino y huella de payload.
10. **INV10:** dos productores/consumidores no pueden efectuar doble dispatch; requiere exclusión durable futura, no demostrada hoy.
11. **INV11:** igual key y distinto payload debe devolver conflicto sin emitir ni revelar estado ajeno.
12. **INV12:** acuse de aceptación proveedor no equivale a entrega al destinatario.
13. **INV13:** estado terminal y dead-letter no se reabren por reintento implícito.
14. **INV14:** fallback de canal necesita consentimiento específico y autorización del canal alterno.
15. **INV15:** secretos de SMTP/WhatsApp/HMAC nunca aparecen en respuesta, HTML, log o BITACORA.
16. **INV16:** callback futuro verifica HMAC, timestamp y nonce una sola vez en scope correcto.
17. **INV17:** timeout/error/cancelación no elevan privilegios ni borran eventos existentes.
18. **INV18:** CorrelationId identifica diagnóstico, nunca autenticación, rol o permiso.
19. **INV19:** auditoría y consulta permanecen tenant-scoped y minimizan PII y retención.
20. **INV20:** COLA/DAG=SI, documentación o build verde no autorizan envíos, workers ni producción.

**Relaciones explícitas:** Empresa→intención; UsuarioEmpresa→actor; Empresa→contacto/destino; intención→consentimiento/canal/finalidad; intención→template/versionado; intención→clave idempotente; intención→intento y acuse; proveedor+empresa→webhook; sujeto→supresión; intención→auditoría. Ninguna arista puede cruzar empresas sin vínculo verificado y permiso server-side.

## Matriz adversarial para fases posteriores — FUTUROS_NO_EJECUTADOS

**Positivos P01–P08:** intención tenant A/rol vigente; consentimiento canal/finalidad; replay devuelve estado previo sin duplicación; plantilla A sin PII de B; supresión efectiva tras opt-out; aceptación proveedor distinta de entrega; retry bounded con dead-letter; auditoría tenant-scoped sin PII.

**Negativos N01–N12:** A intenta enviar por B; rol revocado; contacto legado nulo; opt-out; finalidad distinta; misma clave con payload distinto; dos workers concurrentes; HMAC inválida; replay vencido; secreto en respuesta/log; fallback sin consentimiento; lectura de auditoría de B desde A. **No son tests ejecutados.** M03–M05 deberán incorporar xUnit real, HTTP/JWT/RBAC A/B, MySQL 8.4 efímero y proveedores autorizados según cambio. No inventar HTTP status ni garantías exactly-once.

## A0–A5, dependencias y reversibilidad

**A0:** sólo documento en `docs/v5/` y sección autocontenida de `CHANGELOG_AI.md`; sin cambio de arquitectura/runtime. **A1:** coherencia comprobable de E01–E16 e INV01–INV20 frente a fuentes, positivos/negativos expresamente futuros. **A2:** seguridad y privacidad son gates documentados, no endpoints ni pruebas reales. **A3:** builds .NET/Angular, MySQL, HTTP, SMTP, WhatsApp, Playwright **NO_APLICA para un diff sólo documental**. **A4:** publicación fast-forward en dev, SHA/blobs, `verify-solqaryn-project-scope` y control V5 de procedencia completed/success en SHA exacto, PLAN/COLA/DAG/BITACORA readback. No contar skipped/cancelled/in_progress como PASS. **A5:** otra Task Javier con ID diferente al autor deberá revisar autónomamente, sin autopase. No aprobar QA, main, PROD ni secretos.

**Gates pendientes para diseño posterior:** granularidad/retención de consentimiento, vínculo cliente-sucursal, índices y unicidad durable, contratos DTO, claims de outbox, HMAC del proveedor, cuotas, rollback no destructivo. El avance sólo es válido si PLAN H/I y gates calculados del DAG lo permiten; nunca editar manualmente G/O. **Rollback de este cambio:** `git revert` normal de commit documental en dev, tras revisar HEAD y trabajos concurrentes, preservando BITACORA y los envíos históricos de sistemas existentes.
