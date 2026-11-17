# SOLQARYN V19 — C09-04.M02.03: persistencia, índices, migraciones y datos de prueba multicanal

**ID institucional:** C09-04.M02.03, PLAN_MAESTRO fila 1414. **Autor:** PRIMARY DEV 06 JAVIER, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Repositorio:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`.

**Riesgo del dominio:** R2 (persistencia, secretos, idempotencia, tenant, consentimiento y proveedores). **Riesgo de este changeset:** R0 documental; NO ejecuta SQL, migraciones, pruebas MySQL, envíos, HTTP, SMTP, WhatsApp ni jobs, y NO modifica modelos EF, permisos, índices, configuración o datos. Esta especificación NO equivale a una migración aplicada ni a transacciones comprobadas. Su sucesor M03 exige pruebas reales dirigidas.

## A0 — Evidencia existente verificable en dev

| Fuente actual y blob Git | Hecho comprobado por código, no por MySQL en ejecución |
| --- | --- |
| `backend/src/Domain/Entities/MensajeOutbox.cs` — `891572626c19cd6cf8f522fca5ec876333286ffb` | EventoId, EmpresaId, ClaveIdempotencia, Estado, Intentos, ventanas UTC, referencias y límites declarados; el comentario separa el claim persistente del modelo |
| `backend/src/Infrastructure/Persistence/Configurations/MensajeOutboxConfiguration.cs` — `194911aa2c5a662a6df658e076eb918fe7496576` | Tabla `MensajesOutbox`, índice único por EventoId y por (EmpresaId, ClaveIdempotencia), índices para claim y lectura tenant, FK Empresa RESTRICT, checks positivos/no vacíos |
| `backend/src/Infrastructure/Migrations/20260913050800_N71COutboxPersistence.cs` — `3a46036eb4061a8a27d0f1253abf974970a79d49` | `Up` declara la tabla y esos cuatro índices; `Down` elimina la tabla. Es archivo de migración, NO prueba de su despliegue en un servidor |
| `backend/src/Infrastructure/Repositories/MensajeOutboxRepository.cs` — `056d5aa380b53ed697ddcb6852df55394e6544a7` | `AddAsync` stagea sin SaveChanges; consultas filtran por EmpresaId; `ClaimDisponiblesAsync` usa ExecuteUpdate con predicados condicionales; confirmación/fallo valida intento esperado; no asegura entrega exactamente una vez |
| `backend/src/Application/Services/MensajeOutboxService.cs` — `a00d353c474d7d7f785a58f443c66c5f12a542f7` | Resuelve ámbito de empresa, coteja clave y payload y hace stage; la consulta previa sola NO resuelve una carrera entre procesos, por eso importa el UNIQUE DB |
| `backend/src/Infrastructure/Repositories/EmailEmpresarialRepository.cs` — `14b776398184153e87bd1a9458b1e918c675211f` | Lecturas/lookup incluyen EmpresaId; listados tienen filtros y límite de página, sin declarar envíos ni consentimiento comercial |
| `backend/src/Infrastructure/Migrations/20260914114200_N76CWhatsAppProviderPersistence.cs` — `d192de9881faf1c374e9e54a9a61f112e7fb8032` | ConfiguracionesWhatsAppEmpresa tiene UNIQUE EmpresaId, Version, referencias opacas de secretos y FK RESTRICT; no acredita HMAC real ni integración de proveedor |

Los documentos predecesores `c09-04-m02-01-entidades-ownership-invariantes-mensajeria.md` y `c09-04-m02-02-api-dto-errores-compatibilidad-mensajeria.md` establecen E01–E16, INV01–INV20, I01–I12 y denegaciones por rol/tenant. Las entidades E06–E16 y nuevas interfaces son `PROPUESTA_NO_IMPLEMENTADA`; no reetiquetarlas como tablas actuales.

## Mapa de persistencia actual (sin aplicar cambios)

- **P01 Outbox real:** `MensajesOutbox` guarda una intención por empresa. `UX_MensajesOutbox_EventoId` es único global por identificador; `UX_MensajesOutbox_EmpresaId_ClaveIdempotencia` limita colisiones por tenant. Reusar clave con payload contradictorio se debe rechazar; `CorrelationId` NO es prueba de identidad ni clave de autorización.
- **P02 Selección real:** `IX_MensajesOutbox_Claim(Estado,DisponibleDesdeUtc,Id)` y `IX_MensajesOutbox_EmpresaId_Estado_Disponible(EmpresaId,Estado,DisponibleDesdeUtc)` apoyan selección elegible; un índice no es por sí mismo certificado de aislamiento, prueba transaccional ni ausencia de duplicados.
- **P03 Filtros reales:** el repositorio exige EmpresaId positivo en lookup, claim, retry y confirmación. El update de entrega exige `Estado=Procesando`, `Intentos=intentoEsperado` y tenant coincidente; el proceso externo requiere reconciliación ante timeout.
- **P04 Restricciones reales:** FK `MensajesOutbox.EmpresaId -> Empresas.Id` restringe borrado; check constraints exigen tenant positivo, intentos no negativos, tipo/clave/payload no vacíos y estado dentro de 0..4. La base en Aiven NO se consultó ni modificó.
- **P05 WhatsApp actual:** migración N76C modela *referencias opacas* de secreto, número E.164 y versión. Nunca guardar tokens, cuerpo confidencial ni secreto HMAC en este documento o fixtures.
- **P06 Email actual:** el repositorio incluye tenant en la consulta e idempotencia; esta lectura no certifica su migración en MySQL, envío SMTP, bounced webhook ni un proveedor activo.

## Diseño aditivo para M03 posterior — PROPUESTO, NO IMPLEMENTADO

| ID | Objeto futuro | Restricción/índice que deberá justificarse y probarse antes de una migración |
| --- | --- | --- |
| D01 | IntencionComunicacion | PK técnica, FK tenant+actor válidos, `UNIQUE(EmpresaId,Canal,Finalidad,ClaveIdempotente)` con huella de payload/versionado; colisión distinta = conflicto |
| D02 | ConsentimientoCanalFinalidad y EvidenciaConsentimiento | FK empresa+destinatario; índice por `(EmpresaId,DestinatarioId,Canal,Finalidad,Estado,Version)`; revocación preservada y evaluada al enviar |
| D03 | PlantillaVersionada | `UNIQUE(EmpresaId,Codigo,Version)`, variables permitidas, ninguna lectura de plantilla B desde empresa A |
| D04 | IntentoEntrega/AcuseProveedor | FK a intención y tenant; `UNIQUE(EmpresaId,Proveedor,ReferenciaExterna)` cuando la semántica del proveedor lo permita; acuse no implica entrega final |
| D05 | AntiReplayWebhook | `UNIQUE(Proveedor,EmpresaId,Nonce)` acotado a ventana y hash; HMAC y timestamp verificados server-side antes de persistir; no almacenar payload secreto |
| D06 | RegistroSupresion/AuditoriaComunicacion | Índice por empresa+sujeto+canal+finalidad y retención conforme a política aprobada; logs opacos sin PII |

**No ejecutar D01–D06 en este changeset.** La forma exacta de FKs, cascadas, partición, collation de clave, TTL, política de consentimiento y DDL queda `POR_VALIDAR` en diseño y prueba posterior MySQL 8.4. No asumir `exactly once` por UNIQUE de outbox cuando el proveedor externo puede confirmar tarde.

## Secuencia segura de migración futura (NO EJECUTADA)

1. Inventariar `__EFMigrationsHistory`, modelo EF Oracle y esquema de ambiente efímero MySQL 8.4; comparar snapshot y DDL con nombres/índices; no tocar Aiven ni datos reales.
2. Diseñar migración **aditiva** con columnas/tabla e índices versionados, FK tenant y constraints compatibles; decidir regla de `NULL`, `utf8mb4`, tamaño/collation y bloqueo de índices bajo concurrencia; producir script idempotente y plan de reversión antes de ejecutarlo.
3. Migrar **sólo fixtures sintéticas** en MySQL 8.4 efímero durante GitHub Actions, con tests Category=Integration ejecutados realmente, logs de versión, count, constraints y checksum; `SUCCESS` de un build o job que excluye integración NO acredita esta fase.
4. Exigir transacción/rollback, dos tenants A/B, dos escritores y conflicto de clave, stale lease, replay, opt-out y borrado restringido. Verificar 0 fallos y aislamiento; bloquear paso si aparece P0/P1 o fuga de PII.
5. Revisar plan de expansión/backfill, ventanas de despliegue y reversibilidad con permiso específico antes de cualquier schema real; `Down` destructivo o truncado de historia **NO autorizado**.

## Fixtures sintéticas y matriz de pruebas para M03–M05 (FUTUROS_NO_EJECUTADOS)

Fixtures **ficticias**, sin personas ni secretos reales: `EmpresaA=1101`, `EmpresaB=2202`, `ActorA=8101`, `ActorB=8201`, `Clave=A-test-001`, `Evento=A-evt-001`, `Canal=correo_sintetico`, `Consentimiento=OPT_IN_SINTETICO`, `payload={\"fixture\":true}`. Todas las pruebas deben reinicializar la BD efímera y comprobar conteos por empresa.

- **T01 positivo:** A+clave+payload estable genera exactamente una intención en la transacción autorizada.
- **T02 positivo:** reenvío mismo scope y payload obtiene referencia existente sin segunda intención ni efecto externo.
- **T03 positivo:** claim + entrega condicional del intento activo actualiza sólo A; B permanece intacta.
- **T04 negativo:** A intenta consultar/confirmar mensaje de B, incluso con ID correcto: 0 filas afectadas/no datos B.
- **T05 negativo:** misma clave con distinto payload en A rechaza sin nueva fila; clave igual en B no mezcla tenants.
- **T06 negativo:** dos escritores paralelos sobre A y misma clave deben producir máximo una intención confirmada; demostrar UNIQUE real y manejo del error.
- **T07 negativo:** consumidor stale con `intentoEsperado` previo no confirma tras recuperación/reclaim posterior.
- **T08 negativo:** consentimiento revocado, contacto sin empresa o finalidad distinta bloquean nueva mensajería comercial.
- **T09 negativo:** webhook nonce repetido, HMAC incorrecto, timestamp vencido o proveedor deshabilitado no crea acuse nuevo.
- **T10 negativo:** secreto, JWT, PII y cuerpo de proveedor no aparecen en API, logs, errores ni auditoría.

**Resultados:** `NO_EJECUTADOS` 0/10; sin numerador/denominador de cobertura runtime, `NO_MEDIDO`. Ningún T01–T10 certifica DB, HTTP o proveedores en esta microtarea. Los asserts futuros deberán fallar ante mutaciones adversariales, no aceptar mocks como MySQL real.

## A0–A5, DAG y rollback

- **A0:** diff permitido: este markdown y una sección autocontenida de CHANGELOG_AI.md; sin código ni schema. C09-04.M01.01–06 precede a M02 y requiere A5 independiente según DAG dinámico; el gate se verifica en PLAN/COLA/DAG en cada transición.
- **A1:** revisión estática de P01–P06, D01–D06 y T01–T10 con fuentes y distinción explícita REAL vs FUTURO; T01–T10 aún NO_EJECUTADOS.
- **A2:** límites documentales para tenant A/B, roles/revocación, opt-out, idempotencia, HMAC, PII y secretos; **NO_APLICA certificación HTTP/JWT/RBAC live** por no tocar runtime.
- **A3:** ejecución .NET, Angular, MySQL 8.4, EF migrations, Playwright, SMTP y WhatsApp **NO_APLICA a este changeset R0**, no a trabajos posteriores que alteren esos sistemas.
- **A4:** commit directo fast-forward en dev, blob verificado y CI exact-SHA `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success`; PLAN H/I, COLA G/O, DAG F/G y BITACORA EVENT_ID/IDEMPOTENCY_KEY con readback. `queued/in_progress/skipped/cancelled` no son PASS.
- **A5:** revisión independiente obligatoria por otra de las cinco Tasks Javier, distinta del autor. LISTO_DEV sin A5 no habilita QA/main/PROD.
- **Rollback:** `git revert` normal del changeset documental tras revisar HEAD; preservar eventos BITACORA y trabajo concurrente. No ejecutar migración ni borrar datos como rollback de documento.
