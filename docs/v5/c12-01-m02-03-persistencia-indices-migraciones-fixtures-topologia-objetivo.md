# SOLQARYN V17 — C12-01.M02.03: persistencia, indices, migraciones y datos de prueba de topologia objetivo

**ID:** C12-01.M02.03 · **PLAN_MAESTRO:** fila 1738 · **Autor:** PRIMARY DEV 02 JAVIER (TASK_ID 6af16bb9492c8191b8e8c7d606f11507) · **Riesgo:** R0 documental · **Repositorio:** solqaryn/Solqaryn, rama dev, PROJECT_SCOPE_LOCK=STRICT.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, PLAN_MAESTRO/COLA/DAG_TECNICO vivos; antecedentes C12-01.M01.01–06, M02.01 (E01–E16/I01–I20) y M02.02 (OP01–08, D01–12, ER01–12, V01–16). Este documento diseña **opciones futuras**, no tablas, DDL, migraciones EF, bases, VPS, redes, DNS, despliegues o proveedores activos. El sistema vigente conserva Angular/Vercel, ASP.NET Core 10/Render, MySQL 8.4/Aiven y Cloudinary; cualquier alternativa Hostinger requiere decisión y autorización separadas. No añade endpoints, clases .NET, credenciales ni acceso a secretos.

## A0. Alcance, impacto y criterio de aceptación

**Resultado exigido del ID:** definir persistencia conceptual, claves/indices, evolución reversible y fixtures sintéticos aptos para una implementación futura del plan de topología; enlazar todas las entidades E01–E16 sin inventar un modelo ejecutado. El contrato se considera documentalmente coherente si cubre ownership, separación de entornos, aislamiento tenant A/B, control de versiones, idempotencia, retención, rechazo por defecto y rollback, y reconoce evidencias pendientes como NO_MEDIDO/NO_PROBADO.

**Capas/rutas efectivas:** solo este Markdown y una sección indivisible de CHANGELOG_AI.md. **Consumidores futuros:** arquitectura, seguridad, equipo de datos, continuidad, responsables de proveedores y pruebas de integración. **Interfaces/DTO efectivos:** ninguno; D01–D12 son tipos conceptuales. **Persistencia/transacciones efectivas:** ninguna. **JWT/RBAC/PII:** invariantes estáticas, no nueva autorización. **Observabilidad:** propuestas de referencias y digest redacted, jamás valores sensibles. **Pruebas runtime:** NO_APLICA por R0 sin código; deben ejecutarse en un scope posterior cuando exista implementación.

## K01–K16. Claves e indices propuestos, no existentes en MySQL

| ID | Propuesta futura | Propiedad y criterio de integridad |
| --- | --- | --- |
| K01 | E01 TopologyRevision(revisionId) unique | Un identificador inmutable; autor/versión obligatorios; estados borrador y aprobado separados |
| K02 | E02 EnvironmentBoundary(revisionId, environmentKey) unique | DEV/QA/PROD no comparten contextos ni secretos |
| K03 | E03 WorkloadDefinition(revisionId, workloadKey) unique | Clave canónica, ámbito de revisión y owner |
| K04 | E04 PlacementProposal(revisionId, placementId) unique | FK conceptual a E03/E02 de la **misma revisión** |
| K05 | E05 PrivateNetworkZone(revisionId, zoneId) unique | Zona por ambiente; no habilita exposición de MySQL |
| K06 | E06 IngressRoutePlan(revisionId, ingressId) unique | Ruta declarativa, jamás DNS efectivo; normalización estricta |
| K07 | E07 ServiceEndpointContract(revisionId, serviceKey) unique | E03/E05 relacionados por ambiente, TLS futuro obligatorio |
| K08 | E08 AccessPolicyReference(revisionId, policyRef) unique | Actor, rol, tenant y revocación referenciados, sin secretos |
| K09 | E09 SecretReference(revisionId, opaqueRef) unique | Referencia opaca; prohibido valor secreto en columna, doc o log |
| K10 | E10 DatabasePlacementPlan(revisionId, databasePlanId) unique | E02/E05/E08 coherentes; nunca DSN o recurso activo |
| K11 | E11 VolumeRetentionPlan(revisionId, volumePlanId) unique | Owner, retención y cifrado futuros; sin datos reales |
| K12 | E12 BackupRestoreContract(revisionId, backupPlanId) unique | FK conceptual a E10/E11; restauración no se presume ejecutada |
| K13 | E13 MediaBindingPlan(revisionId, mediaBindingId) unique | Medio/tenant scoped; no crear URL firmada real |
| K14 | E14 ObservabilityContract(revisionId, observabilityId) unique | Digest/redacción y retención aprobada, sin PII |
| K15 | E15 MigrationWaveProposal(revisionId, waveId) unique | Wave propuesta inactiva hasta autorizaciones y gates |
| K16 | E16 RollbackCheckpoint(revisionId, checkpointId) unique | Referencia a wave y backup, con hashes futuros obligatorios |

**Reglas de indexación:** cualquier FK compuesta debe retener revisionId y environmentKey pertinentes; si hay recursos multiempresa, tenantId confiable debe formar parte de las claves de unicidad y búsquedas, no inferirse de un header, subdominio ni input libre. Toda clave desconocida o relación cruzada requiere rechazo, nunca fallback al primer tenant. No crear índices globales que permitan lectura entre tenants. Usar nombres canónicos case-sensitive/ordinal donde una colisión de mayúsculas altere autoridad. Estos son **dieciséis diseños**, no dieciséis índices presentes ni una migración validada.

## MX01–MX08. Secuencia futura de migración reversible

| Paso | Condición previa exigible | Evidencia futura y rollback |
| --- | --- | --- |
| MX01 | Aprobación de alcance R2/R3, propietarios y autorización separada | Plan versionado y snapshot reproducible, sin escribir producción |
| MX02 | Aislar laboratorio MySQL 8.4 efímero; confirmar versión de proveedor EF/Oracle | Registro de versión y hashes de fixtures |
| MX03 | Crear esquema aditivo y claves compuestas con controles tenant/ambiente | SQL/EF migration de laboratorio; rollback de objetos nuevos |
| MX04 | Sembrar exclusivamente fixtures sintéticos A/B y DEV/QA/PROD | Conteos y checksums por ambiente; limpiar sandbox |
| MX05 | Verificar FKs, unicidad, collations, índices y carga negativa | Integration tests Category=Integration realmente ejecutados; cero fails |
| MX06 | Ensayar upgrade y downgrade sin pérdida de datos y transacciones parciales | Checkpoint/backup restaurable y diff verificable |
| MX07 | Evaluar coste, tiempo, RTO/RPO y capacidad con métricas reales | Si no medido: NO_MEDIDO, sin aprobar go-live |
| MX08 | Solicitar revisión independiente, plan de abortar y permiso explícito | Mantener infraestructura actual hasta PASS y autorización |

No ejecutar ni autorizar migraciones destructivas con este documento. Si una etapa falla: detener cambios, mantener entorno actual, conservar logs sanitizados, regresar al checkpoint probado y verificar hashes. Un build verde, una tabla simulada o una propuesta de migración nunca prueban compatibilidad MySQL/EF, consistencia ni restauración.

## F01–F12. Fixtures **sintéticos** para futura integración

| ID | Fixture a construir en el futuro | Riesgo que debe revelar |
| --- | --- | --- |
| F01 | Dos revisiones de topología con versiones distintas | Mezcla de relaciones entre revisiones |
| F02 | DEV y QA con identidades totalmente separadas | Reutilización inadvertida de secretRef |
| F03 | PROD sin posibilidad de activar desde un draft | Promoción por simple valor booleano |
| F04 | Tenant A con placement y política exclusivos | Lectura transversal por tenant B |
| F05 | Tenant B con mismo nombre visible pero clave distinta | Colisión por normalización o collation |
| F06 | Actor revocado y rol insuficiente | Bypass de RBAC y revocación |
| F07 | Referencia opaca de secreto inexistente | Fallback a valor de otro entorno |
| F08 | Plan DB privado y propuesta de endpoint público prohibida | MySQL expuesto en red |
| F09 | Backup sintético íntegro con checksum | Certificación sin restauración |
| F10 | Backup corrupto y volumen sin owner | Restore falso y retención indefinida |
| F11 | Operación duplicada con idempotencyKey de mismo tenant | Doble inserción o cambio concurrente |
| F12 | Migración interrumpida en mitad de transacción | Estado parcial no revertible |

Los fixtures no contienen PII ni secretos reales y **no se han generado ni ejecutado**. Una implementación R2 deberá usar datos artificiales, contraseñas de prueba y servicio efímero sin acceso a Aiven productivo.

## P01/N01–P12/N12. Matriz futura positiva/negativa, cero ejecución

| Par | Positivo a comprobar | Negativo que debe fallar |
| --- | --- | --- |
| P01/N01 | K01 unicidad por versión | Duplicado de revisionId aceptado |
| P02/N02 | K02 separa entornos | FK DEV apunta a secreto PROD |
| P03/N03 | K04 enlaza workload en mismo contexto | Placement enlaza workload de revisión ajena |
| P04/N04 | Tenant A lee solo A con clave compuesta | Tenant B consulta o escribe filas de A |
| P05/N05 | Comparación ordinal conserva referencia canónica | Colisión de case-folding expande autoridad |
| P06/N06 | RBAC y revocación se evalúan en backend | Actor revocado persiste en consulta |
| P07/N07 | SecretReference opaca jamás revela valor | Texto secreto, DSN o JWT en log/fixture |
| P08/N08 | Migración aditiva en MySQL 8.4 aislado | Índice/columna irreversible sin rollback |
| P09/N09 | Restore de checksum íntegro | Backup corrupto aprobado |
| P10/N10 | Idempotencia de F11 mantiene un resultado | Reintento duplica wave o transacción |
| P11/N11 | F12 rollback y limpieza transaccional | Commit parcial queda sin compensación |
| P12/N12 | Solo aprobación separada permite una wave efectiva | Propuesta documental habilita host/PROD/DNS |

**Resultado observable hoy:** 0/12 pares ejecutados, 0/12 fixtures materializados, 0/8 migraciones ejecutadas y 0/16 índices creados. Es evidencia de criterios documentados, **no** de su cumplimiento en runtime. RTO, RPO, latencia, coste, éxito de restore y aislamiento de un host alterno: NO_MEDIDO/NO_VERIFICADO. Los asserts futuros deben fallar frente a defectos, sin convertir un mock en prueba de proveedor.

## A1–A4, DAG y trazabilidad

- **A1 R0:** comprobación documental de 16 diseños K, ocho etapas MX, doce fixtures F y doce pares P/N; correspondencia con E01–E16, OP01–08, D01–12, ER01–12 y V01–16, escenarios de error y rollback. Los tests descritos siguen FUTUROS_NO_EJECUTADOS.
- **A2 R0:** revisión estática de fronteras tenant A/B, separaciones DEV/QA/PROD, RBAC, revocación, red privada, DNS/TLS y referencias opacas. Nunca certificar aislamiento HTTP, base o infraestructura inexistente.
- **A3 R0:** NO_APLICA build .NET, npm, Playwright, API, Docker, MySQL, restauración y terceros; el changeset modifica solamente Markdown.
- **A4 obligatorio:** Scope Lock STRICT y procedencia V5 completed/success sobre SHA publicado o equivalencia demostrada de blobs, PLAN H/I y COLA/DAG readback, evento idempotente BITACORA. En caso de CI fallida conservar VALIDANDO, reparar causa real y nunca convertir CANCELLED/SKIPPED/IN_PROGRESS en PASS.
- **A5:** revisor de otra Task Javier distinta de la autora verifica documento, diff, evidencia, criterios y regresiones; puede rechazar. Solo LISTO+A5 y puertas reales habilitan sucesoras técnicas, nunca forzar G/O.
- **Rollback:** revert normal del commit documental y CHANGELOG tras revalidar HEAD, preservar autoría e historial BITACORA; sin reset ni force-push.

**Dependencias institucionales:** al reclamar, PLAN_MAESTRO fila 1738 estaba PENDIENTE sin owner, COLA G/O=SI, DAG F=VALIDADO/G=SI, candado V17 de ID 2000001738 y CAS H occurrencesChanged=1 con owner único. El cierre documental M02.03 no cambia el estado de M02.04–06 ni autoriza Hostinger, VPS, DNS, QA/main/PROD, datos o credenciales reales.