# SOLQARYN V5 FINAL — C02-02.M02.02: contratos API/DTO y compatibilidad de placement

**Responsable:** PRIMARY DEV 04 / JAVIER_CHATGPT, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. **Ámbito:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT. **Fecha:** 2026-11-15.

**Autoridad ejecutable:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; PLAN/COLA/DAG por ID; Skill canónica, `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`. Predecesor `C02-02.M01.06` LISTO y modelo `C02-02.M02.01` documentado. No se convierte un artefacto M02 en permiso de runtime o de QA/PROD.

## 1. Frontera y semántica

**Entrega:** contrato de interfaces conceptuales y DTO futuros para colocar almacenamiento shared/dedicated sin exponer el destino. No agrega controlador ni ruta HTTP, servicio Application, tabla, contexto EF, migración, DNS, secreto, job, webhook, selector de cadena, worker, despliegue ni escritura institucional desde el código de la app.

La autoridad de empresa, usuario y permiso debe derivarse del servidor con JWT/RBAC y membresía verificada. Los campos cliente nunca deciden DSN, servidor, credencial, instancia de MySQL, empresa propietaria, modo de bypass o selección de tenant. El backend actual sigue siendo fuente efectiva para `AppDbContext` y `StorageTenantContext`; este diseño no certifica router dedicado.

## 2. Operaciones contractuales hipotéticas (API01–API08)

| ID | Caso futuro y frontera | Resultado/denegación exigida |
| --- | --- | --- |
| API01 | Consultar placement lógico autorizado de empresa actual | Enum de modo, versión, estado y timestamp; nunca DSN |
| API02 | Verificar factibilidad sin efectos de cambio | Diagnóstico redactado con gates y prerequisitos, sin grant |
| API03 | Proponer cambio de modo por operador autorizado | Intent idempotente en DRAFT sujeto a doble validación |
| API04 | Solicitar evaluación de backup/restore aislado | Estado de evidencia referencial, nunca datos o ruta interna |
| API05 | Consultar progreso de intento de migración propio | Checkpoints paginados por tenant, sin filas de otro tenant |
| API06 | Proponer cutover con versión y fence esperados | NO_GO hasta restore/checksums y permiso diferenciado |
| API07 | Proponer rollback ante fallo corroborado | Registro condicionado a artefacto anterior y prueba |
| API08 | Leer auditoría de placement por actor habilitado | Eventos redactados, paginados y sin PII/credenciales |

Se trata de operaciones de diseño, **no endpoints instalados**, sin rutas HTTP ni verbos comprometidos. Su adopción requerirá análisis Domain/Application/Infrastructure/API y pruebas reales posteriores.

## 3. DTO propuestos y compatibilidad

| DTO conceptual | Campos permisibles de ejemplo | Campos expresamente prohibidos |
| --- | --- | --- |
| `PlacementPublicView` | `mode`, `revision`, `state`, `verifiedAtUtc`, `canRequestReview` | `connectionString`, DSN, host, token, secretos |
| `PlacementReviewRequest` | `intentId`, `expectedRevision`, `requestIdempotencyKey`, `reasonCode` | `tenantId` como autoridad, `targetDsn`, `force` |
| `MigrationReadinessView` | `intentId`, `gateCodes`, `sourceCommitSha`, `evidenceStatus` | Backup plaintext, credenciales, datos empresariales |
| `PlacementCheckpointView` | `intentId`, `checkpointNumber`, `recordedAtUtc`, `progressState` | Payload de filas, tablas privadas, ubicación del servidor |
| `PlacementAuditView` | `eventId`, `resultCode`, `redactedActorRef`, `timestampUtc` | JWT, email personal, DSN, PII no necesaria |

**Compatibilidad:** no modificar rutas vigentes ni formatos usados por clientes; una evolución futura deberá usar versión/adición, conservar firmas previas durante ventana explícita y documentar reversión. Los campos ausentes no habilitan permisos. Defaults fail-closed: modo actual permanece, intent NO_EJECUTADO, destino NONE.

## 4. Errores seguros (ERR01–ERR12)

| Código conceptual | Condición | Tratamiento |
| --- | --- | --- |
| ERR01 AUTH_REQUIRED | Ausencia de JWT validado backend | Rechazar sin devolver datos de placement |
| ERR02 MEMBERSHIP_INVALID | No existe membresía vigente | Denegar incluso lectura |
| ERR03 RBAC_DENIED | Rol carece de permiso | Denegación auditada sin información externa |
| ERR04 TENANT_SCOPE_MISMATCH | Solicitud de empresa B con contexto A | Denegar sin confirmar existencia de recurso |
| ERR05 STORAGE_BINDING_MISSING | Mapping server-only ausente | Fallo cerrado, nunca fallback a otro tenant |
| ERR06 REVISION_CONFLICT | expectedRevision obsoleta | Rechazo de operación y relectura del estado |
| ERR07 RETRY_CONFLICT | Clave idempotente ligada a otro SHA | No duplicar intento, bloquear |
| ERR08 RESTORE_NOT_VERIFIED | Snapshot no restaurado | No permitir cutover |
| ERR09 INTEGRITY_MISMATCH | Conteos/FK/hash divergentes | Abortar y conciliar |
| ERR10 PERMISSION_EXPIRED | Aprobación revocada o caducada | Revalidar y denegar |
| ERR11 CI_OR_SHA_UNVERIFIED | Check en curso, cancelado, fail o SHA distinto | NO_GO |
| ERR12 ENVIRONMENT_FORBIDDEN | QA/main/PROD solicitado sin orden | NO_GO sin emitir cambio |

Todos los errores deberán tener mensajes redactados, `correlationId` opaco generado por backend, ningún secret y auditoría suficiente para investigarlos.

## 5. Reglas de validación (VAL01–VAL14)

1. VAL01 Verificar identidad JWT en API backend y permiso por acción, no en Angular.
2. VAL02 Resolver empresa de contexto servidor; rechazar cabeceras, URL o cuerpo como autoridad.
3. VAL03 Asegurar ownership empresa A/B de intentos, bindings, evidencias y lecturas.
4. VAL04 Validar formato y vigencia de `requestIdempotencyKey`; no usar valor como permiso.
5. VAL05 Verificar revisión, fence y expectativa antes de cambio.
6. VAL06 Exigir estado previo legal según contrato M02.01.
7. VAL07 Denegar DSN, ruta SQL o secreto suministrado por cliente.
8. VAL08 Bloquear ante ausencia de binding, y nunca usar conexión de otra empresa.
9. VAL09 Requerir backup y restore EFÍMERO realmente comprobados para cutover futuro.
10. VAL10 Comparar conteos/FKs/checksums aislados, sin exponer registros.
11. VAL11 Diferenciar evidencia de CI por SHA exacto y `completed/success`; no aceptar skipped.
12. VAL12 Exigir A5 independiente y permisos por entorno cuando corresponda.
13. VAL13 Retener logs redactados e idempotencia de eventos.
14. VAL14 Paginación y límites en vistas de progreso/auditoría, sin exportación masiva arbitraria.

## 6. Oráculos P/N FUTUROS_NO_EJECUTADOS

| Par | Comportamiento permitido futuro | Negativa obligatoria |
| --- | --- | --- |
| P01/N01 | Empresa A lee únicamente su `PlacementPublicView` | Empresa B infiere DSN o existencia de A |
| P02/N02 | Backend valida membresía+RBAC para propuesta | Cabecera/cookie spoofing concede permiso |
| P03/N03 | Conflicto de versión retorna error seguro | Dos cutovers consumen misma revisión |
| P04/N04 | Reintento idempotente conserva outcome | Segundo intento duplica datos o eventos |
| P05/N05 | Snapshot verificado habilita evaluación | Restore no probado se trata como PASS |
| P06/N06 | Error de binding da NO_GO | Fallback conecta almacenamiento ajeno |
| P07/N07 | Auditoría autorizada redacta valores | Token, host, PII o DSN aparece en respuesta |
| P08/N08 | Revisión A5 y SHA/CI realmente acreditados | Autor se autoaprueba o CI cancelada habilita |
| P09/N09 | Rollback sólo con target previo verificado | Git revert de Markdown se confunde con restauración |
| P10/N10 | QA/main/PROD requerirían orden independiente | Estado LISTO_DEV autoriza desplegar |

Estos diez pares están **escritos para futuras pruebas**, no se ejecutaron con HTTP, JWT real, MySQL, Docker, Playwright ni navegador.

## 7. Auditorías y salida

**A0:** diff exclusivo de este Markdown y sección contigua de CHANGELOG; ningún API/DTO .NET real modificado. **A1:** coherencia de API01–08, DTO y ERR01–12, VAL01–14 y pares positivos/negativos futuros (no ejecutados). **A2:** aislamiento A/B, JWT/RBAC, DSN server-only, control de reintentos y mensajes sin información sensible. **A3:** alineación con contrato de entidades de C02-02.M02.01 y bloqueos de M01.06; `APP`/`Infrastructure` actuales no reciben router dedicado. **A4:** publicar en dev con fast-forward y expected SHA, revisar diff/blobs y checks Scope Lock y V5 `completed/success` sobre SHA exacto, estado PLAN/COLA/DAG y BITACORA. Las compilaciones runtime ajenas al diff Markdown son `NO_APLICA`, nunca PASS. **A5:** requiere reviewer distinto de TASK autora entre las otras cinco Javier, antes de habilitar sucesora dependiente de A5.

**Rollback:** `git revert` normal del documento en dev preservando la historia. No se inició SQL, migración, despliegue, rotación de claves, pago ni operación externa. Ningún contrato M02 autoriza tocar QA, main o PROD.
