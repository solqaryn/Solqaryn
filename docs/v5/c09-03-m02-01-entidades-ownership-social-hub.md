# SOLQARYN V17 — C09-03.M02.01: entidades, ownership y relaciones del Social Hub
**Autor y owner:** PRIMARY DEV03 Javier, `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Rama:** `solqaryn/Solqaryn@dev`. **Riesgo:** R0 documental; este documento no crea tablas, proveedores, workers, APIs ni activación comercial.
**Fuente institucional:** STANDARD V5 FINAL, PLAN_MAESTRO fila 1376, COLA fila 1376, DAG_TECNICO fila 1337, dependencia C09-03.M01.06 LISTO y A5_PASS independiente. Antes de crear contenido: H1376 PENDIENTE, I vacío, COLA G/O=SI, DAG F=VALIDADO/G=SI, candado SQV17_C09_03_M02_01 id 2000001376 y CAS H occurrencesChanged=1, owner DEV03, readback. Baseline dev al crear: `61cfaa028a8b2585607c6ddf02b8f39d6eeba78a`.

## Alcance, fronteras y claves conceptuales (A0)
Este contrato prepara entidades y asociaciones del Social Hub y campañas conforme a C09-03.M01.01–06: consentimiento por propósito/canal, aislamiento de empresa, opt-out, calendario, métricas agregadas, reglas de idempotencia y recuperación. Son propuestas, NO migraciones. La identidad autorizante siempre provendrá del backend y la pertenencia tenant actual; no confiar en IDs de payload, cookies, claims sin verificar o controles de UI. El principio actual es denegar si falta información o aprobación técnica.

| Entidad lógica propuesta | Clave y ownership exigido antes de implementar | Relaciones conceptuales |
| --- | --- | --- |
| E01 Campaña | CampaignId, EmpresaId y versión | Propietaria de E02, E03, E06, E11, E12 y E16 |
| E02 CanalCampaña | ChannelId, CampaignId, EmpresaId, proveedor autorizado opcional | Canal permitido por consentimiento E04 y política |
| E03 PiezaContenido | ContentId, CampaignId, EmpresaId, propietario y versión | Pertenencia a E01 y archivos propios E10 |
| E04 ConsentimientoCanal | ConsentId, EmpresaId, SubjectRef opaco, propósito, canal, estado, revisión | Vincula sujeto y canal; exige evidencia E05 |
| E05 PruebaConsentimiento | ProofId, ConsentId, EmpresaId, fecha/fuente verificadas | No puede existir sin E04 de la misma empresa |
| E06 SegmentoDefinido | SegmentId, CampaignId, EmpresaId, versión de criterios allowlist | Produce E07 sin SQL/JS de usuario |
| E07 InstantáneaSegmento | SnapshotId, SegmentId, EmpresaId, revisión y fecha UTC | Contiene referencias opacas E08; no PII expuesta |
| E08 MiembroSegmento | SnapshotId, SubjectRef y EmpresaId compuestos | Debe cumplir consentimiento E04 vigente al uso |
| E09 ExclusiónOptOut | ExclusionId, SubjectRef, EmpresaId, canal y fecha efectiva | Prevalece sobre snapshots, calendarios y reintentos |
| E10 MedioPropio | MediaId, ContentId, EmpresaId, owner verificable, URL segura | Asociado a E03, prohibido consumir medios de B |
| E11 CalendarioCampaña | ScheduleId, CampaignId, EmpresaId, UTC, cancelación y versión | Genera solo intenciones E12 tras reautorización |
| E12 IntenciónPublicación | IntentId, CampaignId, EmpresaId, canal, clave idempotente | Vincula E02, E03, E07, E11, E09 y E13 |
| E13 IntentoPublicación | AttemptId, IntentId, EmpresaId, estado/versiones | No provoca duplicado al reintentar; E14 opcional |
| E14 AcuseProveedor | ReceiptId, AttemptId, EmpresaId, ref opaca y estado | Sólo si proveedor/contrato habilitado y permitido |
| E15 MétricaAgregada | MetricId, CampaignId, EmpresaId, intervalo UTC | Agrega sin PII, cardinalidad mínima y RBAC |
| E16 EventoAuditoría | AuditId, CampaignId, EmpresaId, actorRef opaca y resultado | Append-only, contenido minimizado, no secretos |

**Límites del modelo:** claves PK/FK compuestas deben incorporar EmpresaId o verificar ownership antes de cualquier acceso; no admitir NULL legado sin reconciliación. Una relación entre E01–E16 nunca puede unir empresas A/B. No se selecciona motor de mensajería, proveedor, tablas ni costos en esta unidad.

## Dieciocho invariantes vinculantes para diseño futuro (A1/A2)
| ID | Invariante cuya falla debe causar rechazo |
| --- | --- |
| I01 | Toda lectura/escritura exige empresa autenticada y pertenencia backend actual |
| I02 | Propietario y recurso comparten EmpresaId, incluida toda FK |
| I03 | Ausencia/NULL legado de ownership deniega sin interpolar empresa por defecto |
| I04 | Campaña, canal, pieza y medio existen y están activos en misma empresa |
| I05 | RBAC separa borrador, aprobación, publicación, cancelación y exportación |
| I06 | JWT inválido, vencido, revocado o rol perdido deniega aun con UI habilitada |
| I07 | Consentimiento explícito aplica por finalidad, canal y destinatario |
| I08 | Opt-out y revocación prevalecen sobre versiones o snapshots antiguos |
| I09 | Revalidar E04/E09 y RBAC inmediatamente antes del efecto externo |
| I10 | Segmentación usa filtros tipados, no SQL/JS ni URLs externas arbitrarias |
| I11 | IdempotencyKey y scope empresa+campaña+canal+destino son únicos |
| I12 | Concurrencia/reintentos/outbox no pueden producir dos publicaciones |
| I13 | Calendario UTC con versión, expiración y cancelación persistente |
| I14 | Contenido/medios deben sanear XSS/SSRF y verificar propiedad de archivos |
| I15 | Tokens, secretos, DSN, contactos y URLs sensibles no aparecen en logs |
| I16 | Métricas agregadas limitan cardinalidad y niegan reidentificación |
| I17 | Cuotas/rate limit/timeout y proveedor deshabilitado bloquean el envío |
| I18 | Rollback detiene nuevos efectos, preserva auditoría y consentimiento |

## Matriz de diez pruebas positivas/negativas futuras, NO EJECUTADAS
| Par | Positivo a probar al implementar | Negativo adversarial que debe fallar |
| --- | --- | --- |
| P01/N01 | Admin A lista campañas A | usuario B intenta consultar CampaignId A |
| P02/N02 | Editor crea borrador propio | editor sin rol publica sin aprobación |
| P03/N03 | Consentimiento activo canal/purpose | contacto sin opt-in o con baja intenta recibir |
| P04/N04 | Snapshot de A usa solo miembros A | segmento incluye sujeto B o ownership NULL |
| P05/N05 | Pieza y medio propios saneados | XSS/SSRF o mediaId de B |
| P06/N06 | Calendario UTC vigente dispara intención | revocación posterior cancela la emisión |
| P07/N07 | Reintento idempotente devuelve mismo resultado | carreras o replay duplican envío |
| P08/N08 | Proveedor autorizado ACK correctamente | proveedor sin permiso/429/timeouts produce fallo seguro |
| P09/N09 | Métrica A agregada por cardinalidad válida | exportación PII o cohortes pequeñas |
| P10/N10 | Rollback conserva eventos sin publicar | rollback borra consentimiento o reenvía campaña |

Resultados runtime: **0/10 pares ejecutados**. No se infiere xUnit, Playwright, MySQL, HTTP JWT, proveedor ni conformidad legal. Los escenarios son criterios de aceptación futura y deben ejecutarse en sus microtareas de código, con A0–A5 causales.

## Integración, seguridad, calidad y rollback (A3–A5)
- A0: cambios únicamente a este Markdown y encabezado+viñetas autocontenidos de CHANGELOG; sin cambios de Domain/Application/Infrastructure/API, Angular, esquema ni autorizaciones.
- A1: revisión estructural de E01–E16, I01–I18 y P01/N01–P10/N10; todos sintéticos.
- A2: tenancy A/B, ownership, RBAC, JWT, consentimiento y revocación especificados como obligaciones; no afirmar pruebas vivas.
- A3: build .NET/MySQL 8.4/Playwright/proveedor NO_APLICA **a este diff R0**; sí serán obligatorios al introducir runtime.
- A4: publicación fast-forward sin force-push a dev; Scope Lock exact SHA, procedencia CHANGELOG V5, PLAN/COLA/DAG/BITACORA con readback y un solo candado V17. No inventar SUCCESS de checks queued/in_progress.
- A5: revisor Task Javier **distinta de DEV03** examina estas propuestas y puede emitir FAIL; sin A5 la sucesora técnica no se libera artificialmente.
- Rollback: `git revert` normal del commit documental sólo con HEAD CAS, nueva CI/readbacks, BITACORA histórica intacta; no borrar consentimientos ni datos reales. Decisiones futuras de proveedor, costos, índices/PK reales, tenant nullable y retención siguen **pendientes**. Ninguna tarea autoriza QA/main/PROD.
