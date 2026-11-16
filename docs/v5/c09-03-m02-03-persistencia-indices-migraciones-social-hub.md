# SOLQARYN V17 — C09-03.M02.03: persistencia, índices, migraciones y fixtures del Social Hub

**Riesgo:** R0 documental sin runtime. **Autor:** PRIMARY DEV06 Javier, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Ámbito:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL; PLAN_MAESTRO!A1378, COLA!A1378, DAG_TECNICO!A1339 (VALIDADO/SI al CLAIM). Dependencias conceptuales: `C09-03.M01.01–06`, entidades `C09-03.M02.01`, contratos `C09-03.M02.02`; no activa flags `M02.04`.

## A0. Diseño persistente futuro, NO schema ejecutado

Los nombres siguientes son modelos lógicos propuestos, **no tablas existentes, código EF Core, migraciones, DDL o índices creados**. Futuras capas respetarán Domain <- Application <- Infrastructure y API como frontera autenticada. `EmpresaId` siempre deriva de identidad backend verificable, nunca de DTO, parámetro cliente, cookie o UI; toda FK a entidades de tenant incorpora empresa, sin NULL legacy ni fallback. MySQL no recibe por este documento autorización para escribir, usar datos reales, programar envíos o activar proveedores. La seguridad real exige RBAC, consentimiento vigente, opt-out, revocación, ownership y pruebas separadas.

| Entidad conceptual E01–E16 | Clave compuesta futura | Integridad/relación propuesta |
| --- | --- | --- |
| E01 Campaña | EmpresaId+CampaignId | root, Revision y estado |
| E02 CanalCampaña | EmpresaId+CampaignId+ChannelId | FK a E01, canal allowlist |
| E03 PiezaContenido | EmpresaId+ContentId | FK a E01, versión y medio propio E10 |
| E04 ConsentimientoCanal | EmpresaId+ConsentId | sujeto opaco, propósito, canal, revocación |
| E05 PruebaConsentimiento | EmpresaId+ProofId | FK compuesta a E04, fuente verificable |
| E06 SegmentoDefinido | EmpresaId+SegmentId | FK a E01, filtros tipados |
| E07 InstantáneaSegmento | EmpresaId+SnapshotId | FK a E06, revisión y UTC |
| E08 MiembroSegmento | EmpresaId+SnapshotId+SubjectRefHash | FK a E07, opt-out en tiempo de uso |
| E09 ExclusiónOptOut | EmpresaId+SubjectRefHash+ChannelId+Purpose | revocación prevalente y UTC |
| E10 MedioPropio | EmpresaId+MediaId | FK E03, propiedad y URL segura |
| E11 CalendarioCampaña | EmpresaId+ScheduleId | FK E01, UTC, expiración y cancelación |
| E12 IntenciónPublicación | EmpresaId+IntentId | FK E01/E02/E03/E11, idempotencia y consentimiento |
| E13 IntentoPublicación | EmpresaId+IntentId+AttemptId | FK E12, estado y CAS |
| E14 AcuseProveedor | EmpresaId+ReceiptId | FK E13, ACK opaco no autorizante |
| E15 MétricaAgregada | EmpresaId+CampaignId+WindowStartUtc+MetricKind | FK E01, cohorte mínima |
| E16 EventoAuditoría | EmpresaId+AuditId | append-only sin secretos ni PII directa |

**Reglas de consistencia:** un hijo B no puede referenciar padre A; opt-out efectivo prevalece sobre caché, snapshot, calendario y reintento. E13 y E14 no implican efecto exactamente una vez en proveedor externo sin reconciliación causal. Las auditorías E16 son append-only y minimizadas. El esquema deberá validar longitudes/tipos, compatibilidad real de FK compuestas, normalización y transacciones con Oracle MySql.EntityFrameworkCore 10.0.9 en MySQL 8.4 **únicamente en la futura unidad de migración autorizada**.

## Doce restricciones/índices propuestos (no creados)

| ID | Diseño de índice lógico | Defecto que previene |
| --- | --- | --- |
| IX01 | E01 (EmpresaId,Status,UpdatedAtUtc) | listados tenant aislados |
| IX02 | E02 UNIQUE(EmpresaId,CampaignId,ChannelId) | canal duplicado |
| IX03 | E03 (EmpresaId,CampaignId,ContentId,Revision) | contenido de B/versión vieja |
| IX04 | E04 (EmpresaId,SubjectRefHash,Purpose,ChannelId,Revision) | consentimiento cruzado |
| IX05 | E05 (EmpresaId,ConsentId,CreatedAtUtc) | prueba huérfana |
| IX06 | E07 (EmpresaId,SegmentId,SnapshotRevision) | snapshot obsoleto |
| IX07 | E08 UNIQUE(EmpresaId,SnapshotId,SubjectRefHash) | miembro repetido |
| IX08 | E09 (EmpresaId,SubjectRefHash,ChannelId,Purpose,EffectiveUtc) | opt-out antiguo |
| IX09 | E11 (EmpresaId,DueAtUtc,Status,Revision) | agenda caducada |
| IX10 | E12 UNIQUE(EmpresaId,CampaignId,ChannelId,DestinationRefHash,IdempotencyKey) | intención duplicada |
| IX11 | E13/E14 UNIQUE(EmpresaId,IntentId,AttemptId) | reintento/acuse repetido |
| IX12 | E15/E16 (EmpresaId,WindowStartUtc,MetricKind) y (EmpresaId,AuditId) | fuga agregada o auditoría |

**Índices no equivalen a autorización.** E04 preserva historia de consentimiento y revocaciones; E09 es prioritaria a cualquier consentimiento anterior. IdempotencyKey de E12 requiere digest de payload y scope tenant/campaña/canal/destino; misma clave con digest distinto es conflicto y no publicación. Antes de implementar, medir selectividad, plan EXPLAIN, costo de índices, política de retención y coherencia de cascadas; no asumir cobertura real hoy.

## Plan de migración futura y rollback — solo diseño

1. **Expand reversible:** definir nuevas tablas deshabilitadas, FKs tenant compuestas, índices estables y versiones de esquema; ensayar contra MySQL 8.4 efímero aislado con datos sintéticos.
2. **Backfill/validación:** verificar claves EmpresaId no nulas, FK de misma empresa, consentimiento y opt-out persistentes, idempotencia y estado de intentos sin efectos externos. Ante legacy sin ownership, denegar/reconciliar; no adivinar tenant.
3. **Compatibilidad/CAS:** ensayar versiones monotónicas, If-Match y migración aditiva sin dual-write a proveedor real; cada lectura/escritura requiere identidad, roles y revocación vigentes en servidor.
4. **Fallas y carreras:** simular interrupción entre intención/intento/acuse, rollback de transacción, replay, cancelación y dos escritores; comprobar no duplicar envíos y conservar eventos/auditoría.
5. **Contraction futura separada:** no DROP, renombrar claves ni eliminar historial hasta backup verificable, plan de reversión y revisión independiente. Rollback bloquea efectos nuevos, retiene opt-out/consentimiento, y privilegia roll-forward correctivo si ya existe dato histórico.

**No hay SQL ejecutado ni migración creada.** No existe autorización para bases Aiven productivas, schedulers, pagos, proveedores o secretos. El trabajo de EF/MySQL deberá probar `Category=Integration` **real** en MySQL 8.4 efímero, con logs y cero fallos, cuando exista cambio de esquema.

## Doce fixtures sintéticos y pares P/N futuros — 0/12 ejecutados

| Caso | Positivo a probar más adelante | Negativo/frontera adversarial |
| --- | --- | --- |
| F01 | Campaña A con canal y contenido propios | FK a contenido/canal B |
| F02 | Consentimiento activo A por propósito/canal | consentimiento B/otro propósito |
| F03 | Prueba E05 ligada a E04 A | ProofId huérfano o empresa B |
| F04 | Segmento/snapshot A vigente | miembro B o snapshot obsoleto |
| F05 | Opt-out vigente cancela intención | replay de consentimiento antiguo |
| F06 | Medio propio saneado | URL SSRF o MediaId B |
| F07 | DueAtUtc válido y versión actual | DST ambiguo, expirado, cancelación |
| F08 | Una intención idempotente | mismo key con digest diferente |
| F09 | Reintento sin segundo ACK | carrera de dos escritores |
| F10 | Métrica con umbral privacidad | cohorte pequeña o PII |
| F11 | Revalidación pertenencia/rol | NULL ownership o JWT revocado |
| F12 | Rollback conserva bajas y bitácora | reenvío o borrado de auditoría |

La evidencia anterior es contrato de pruebas futuras, **no resultados de xUnit, SQL, HTTP o Playwright**. Usar sujetos opacos sintéticos y tenant A/B; no registrar secretos, contactos ni payloads. Las pruebas reales futuras deben fallar si se viola ownership, consentimiento, opt-out, RBAC, UTC, idempotencia o privacidad, no limitarse a consultar índices.

## Calidad V15 adaptativa A0–A5

- **A0:** se conserva el modelo M02.01 (E01–E16), los contratos M02.02, doce índices y doce casos; el diff afecta únicamente este Markdown y una sección autocontenida de CHANGELOG.
- **A1:** inspección documental de 16 claves, 12 propuestas de índice y 12 pares P/N; runtime `0/12`.
- **A2:** diseño de aislamiento tenant A/B, FK compuestas, RBAC/revocación, consent/opt-out y minimización de PII; sin alegar pruebas de seguridad ejecutables.
- **A3:** `.NET`, MySQL 8.4, EF, navegador, JWT HTTP, proveedores y migraciones `NO_APLICA_R0` porque ninguna fuente ejecutable se modifica.
- **A4:** publicar en `dev`, readback del archivo y CHANGELOG por blob, Scope Lock + procedencia V5 `completed/success` sobre SHA exacto antes de acreditar LISTO, y PLAN/COLA/DAG/BITACORA readback.
- **A5:** revisión separada de otra Task Javier (TASK_ID distinto del autor). Puede rechazar, sin promoción QA/main/PROD.

**Rollback:** revertir el commit por fast-forward normal; conservar bitácora y sus evidencias. El diseño no modifica el modelo productivo, permisos reales o decisiones comerciales.
