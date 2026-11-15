# SOLQARYN V5 FINAL — C02-02.M02.04: configuración tipada y feature flags seguras

**Autor:** PRIMARY DEV 04 JAVIER, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. **Repositorio:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Fecha:** 2026-11-15.

**Fuentes:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (`1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), PLAN/COLA/DAG por ID, Skill canónica, M01.06 y contratos C02-02.M02.01–03. No se modifica configuración real, secretos, proveedor de MySQL, routing tenant, feature toggles activos ni despliegues.

## 1. Contrato hipotético de configuración

El servicio debe obtener políticas de fuente confiable de backend con firma, revisión y control RBAC; Angular nunca será autoridad. Los siguientes dieciséis campos constituyen **esquema conceptual**, sin crear claves de configuración en `appsettings`, Variables, Aiven o MySQL:

| Campo propuesto | Tipo/límite | Default seguro |
| --- | --- | --- |
| CFG01 `SchemaVersion` | integer positivo | Versión actual, sin migración automática |
| CFG02 `EnvironmentScope` | enum DEV/QA/PROD | DEV para este plan; otros prohibidos |
| CFG03 `PlacementMode` | enum SHARED/DEDICATED | SHARED según baseline verificado, no selección cliente |
| CFG04 `TenantReference` | identificador interno autenticado | NONE hasta resolver contexto |
| CFG05 `BindingAlias` | token opaco server-only | NONE; nunca DSN |
| CFG06 `AssignmentRevision` | entero monotónico | Sin cambio |
| CFG07 `FenceEpoch` | entero monotónico | No permite cutover |
| CFG08 `PolicyRevisionSha` | 40 hex exactos | NO_GO si ausente |
| CFG09 `ApprovedActorRef` | referencia auditada | NONE |
| CFG10 `AuthGrantExpiryUtc` | UTC con caducidad | Expirado |
| CFG11 `RestoreProofRef` | referencia opaca | NONE |
| CFG12 `IntegrityDigestRef` | referencia opaca | NONE |
| CFG13 `IdempotencyNamespace` | prefijo interno acotado | NONE |
| CFG14 `RoutingChangeWindow` | intervalo autorizado | CERRADA |
| CFG15 `AuditRetentionDays` | entero acotado por política | Política existente |
| CFG16 `RollbackTargetRef` | referencia opaca verificada | NONE |

Un valor de cliente, cabecera, cookie, URL o Sheet no puede activar `PlacementMode`, `BindingAlias`, `AuthGrantExpiryUtc` ni `RoutingChangeWindow`. La selección de proveedor y la conexión efectiva permanecen bajo configuración existente hasta implementación, pruebas y autorización separadas.

## 2. Flags FUTURAS APAGADAS (FL01–FL12)

| Flag conceptual | Propósito futuro | Default |
| --- | --- | --- |
| FL01 DedicatedPlacementEnabled | Evaluar destino dedicado | false |
| FL02 PlacementReviewEnabled | Proponer revisión | false |
| FL03 MigrationPreviewEnabled | Simular sin mutación | false |
| FL04 MigrationApplyEnabled | Aplicar solo con autorización futura | false |
| FL05 VerifiedSnapshotRequired | Exigir backup restaurable, nunca desactivar seguridad | true |
| FL06 CutoverEnabled | Permitir corte bajo fencing | false |
| FL07 DualReadValidationEnabled | Comparación aislada de pruebas | false |
| FL08 DualWriteEnabled | Escritura simultánea — alto riesgo | false |
| FL09 RollbackRequestEnabled | Solicitar reversión controlada | false |
| FL10 DedicatedRoutingEnabled | Routing server-side | false |
| FL11 TenantMigrationAuditEnabled | Auditoría adicional sin PII | false hasta integración verificada |
| FL12 ProviderSwitchEnabled | Cambio de proveedor | false |

Los flags de seguridad **no deben poder apagarse** para eludir tenant, JWT/RBAC, integridad, restore, Scope Lock o A5. La intención de FL05 es obligación mínima, no habilitación de migración ni su activación en runtime.

## 3. Precedencia y alcance (ORD01–ORD08)

1. ORD01 Negación de Scope Lock, permisos o falta de evidencia siempre prevalece.
2. ORD02 Identidad, membresía y tenant de backend prevalecen frente a petición.
3. ORD03 Prohibiciones legales/de seguridad prevalecen frente a flags.
4. ORD04 Una política específica de tenant puede restringir, nunca ampliar privilegio global.
5. ORD05 Cambios por ambiente tienen gates independientes y aprobación separada.
6. ORD06 Un flag ausente se trata como apagado; nunca asumir true.
7. ORD07 Una revisión obsoleta frente a HEAD/gate provoca NO_GO, sin fallback.
8. ORD08 Un rollback sólo puede restituir versión comprobada y sin secretos en log.

## 4. Validaciones de seguridad (VAL01–VAL12)

- VAL01 Tipo y rango de los dieciséis campos, sin parseo ambiguo.
- VAL02 Revisión de política y SHA exacto con firma/autoridad futura.
- VAL03 Tenant/actor se obtienen de contexto backend autenticado, no del cliente.
- VAL04 JWT, RBAC y membresía vigentes por operación.
- VAL05 Empresa A no puede consultar binding o estado de B.
- VAL06 Nunca almacenar ni imprimir DSN, passwords, credenciales, tokens ni PII.
- VAL07 Versionado CAS/fence evita actualización concurrente de placement.
- VAL08 Intent y clave idempotente son específicos de empresa/SHA.
- VAL09 Restore y reconciliación de hashes deben acreditarse de forma nativa antes de cutover.
- VAL10 Flags de migración, proveedor, dual-write y dedicated routing permanecen apagadas aquí.
- VAL11 CI requerida `queued/in_progress/cancelled/skipped/failed` no es PASS.
- VAL12 Ningún flag o estado M02 autoriza QA/main/PROD ni cambio de pagos/servicios.

## 5. Pares P/N futuros (sin ejecutar)

| Par | Positivo futuro | Negación exigida |
| --- | --- | --- |
| P01/N01 | Snapshot tipado con revisión válida | String malformada cambia modo por coerción |
| P02/N02 | Tenant A carga policy server-only | Tenant B selecciona binding A |
| P03/N03 | Actualización de versión correcta | Fence obsoleto cambia destino |
| P04/N04 | Flag ausente conserva operación actual | Ausencia activa dedicated por defecto |
| P05/N05 | Restore comprobado habilita preflight | Sin restore se omite gate |
| P06/N06 | Grant vigente y RBAC autorizan evaluación | Flag concede permiso sin JWT |
| P07/N07 | Correlación auditable sin secretos | DSN aparece en logs/API |
| P08/N08 | Dev con A5 y check SHA exacto | A5 del autor o CI cancelada se acepta |

Los ocho pares son **FUTUROS_NO_EJECUTADOS**; no hay test runtime .NET/Angular, HTTP, MySQL ni Playwright en este cambio.

## 6. A0–A5 y recuperación

**A0:** revisar doc+CHANGELOG autocontenido; sin código, migraciones ni secretos. **A1:** validar coherencia de CFG01–16, FL01–12, ORD01–08 y VAL01–12; oráculos futuros NO EJECUTADOS. **A2:** fail-closed, separación de ambientes, RBAC/JWT y aislamiento A/B. **A3:** respetar M01.06, entidades M02.01, contratos M02.02 y persistencia conceptual M02.03, sin confundir diseños con runtime. **A4:** commit en `dev` fast-forward con HEAD esperado, blobs exactos, Scope Lock y procedencia V5 en `completed/success` exact-SHA, PLAN/COLA/BITACORA readback; CI .NET/Angular/MySQL `NO_APLICA` al diff Markdown-only, nunca PASS. **A5:** otra Task Javier, distinta de la autora, acredita revisión con evidencia y readback.

**Rollback aplicable:** `git revert` normal del documento y CHANGELOG sin force-push. Este cambio no altera QA, main, PROD, DNS, proveedores, MySQL, variables de entorno, secretos, flags reales ni autorizaciones.
