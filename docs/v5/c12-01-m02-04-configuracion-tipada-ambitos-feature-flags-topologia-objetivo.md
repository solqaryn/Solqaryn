# SOLQARYN V17 — C12-01.M02.04: contrato de configuración tipada, ámbitos y feature flags de topología objetivo

**Microtarea:** C12-01.M02.04 · **PLAN_MAESTRO:** fila 1739 · **Autor:** PRIMARY DEV 04 JAVIER (TASK_ID 6aeacb7820ac8191b7d768a6f33dfa09) · **Riesgo:** R0 documental, sin ejecución ni configuración real · **Repositorio:** solqaryn/Solqaryn@dev · **PROJECT_SCOPE_LOCK:** STRICT.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL y PLAN_MAESTRO/COLA/DAG_TECNICO institucionales. **Entradas:** C12-01.M01.01–06 con revisión A5 y gates G01–G16; modelo E01–E16/I01–I20 de M02.01, y operaciones conceptuales OP01–OP08, DTO D01–D12, errores ER01–ER12 y validaciones V01–V16 de M02.02. La presente entrega **no cambia la configuración ejecutada**.

## A0 — Propósito, alcance y arquitectura

Definir contratos futuros de configuración **tipada, por ambiente y por frontera de confianza**, necesarios para evaluar propuestas de red, contenedores, frontend, API, workers, MySQL, secretos, backup, observabilidad y reversión. Mantener las referencias de secretos opacas, los flags seguros por defecto y la separación tenant A/B y DEV/QA/PROD. Los contratos son **propuestas inactivas**: no crean clases .NET, tipos Angular, variables reales, secretos, toggles, endpoints, contenedores, DNS, bases, VPS, tickets ni recursos de proveedores. La topología vigente sigue usando Vercel, Render, Aiven y Cloudinary; Hostinger/Cloudflare son opciones sin autorización o evidencia de ambiente operativo.

**Capas efectivas modificadas:** ninguna. **Consumidores futuros:** plataforma/arquitectura, custodia IAM, backend, frontend, operaciones, DBA, revisión de continuidad y QA autorizado aparte. **Datos/EF/SQL:** ninguna migración ni sentencia. **JWT/RBAC/tenant:** reglas conceptuales que no suplantan autorización backend. **Integraciones:** sin llamadas. **Observabilidad:** solo criterios de redacción. **Rollback de esta entrega:** revertir aditivamente documento y CHANGELOG tras comprobar HEAD, sin borrar BITACORA.

## CFG01–CFG16 — Inventario de configuración propuesta y ámbito tipado

| ID | Clave conceptual y tipo futuro | Ámbito / dueño | Validación obligatoria y defecto seguro |
| --- | --- | --- | --- |
| CFG01 | topology.revisionId: identificador inmutable | E01 / propietario | Desconocido o versión sin firma de revisión => NO_GO |
| CFG02 | topology.environment: enum DEV, QA, PROD | E02 / operaciones | Ausente o distinto del ambiente autenticado => NO_GO; sin inferir de URL |
| CFG03 | topology.workloadKey: identificador registrado | E03 / arquitectura | No registrado o sin owner => rechazar |
| CFG04 | topology.placementRef: referencia opaca versionada | E04 / infraestructura | Sin zone/ambiente compatibles => rechazar |
| CFG05 | topology.networkZoneRef: referencia privada | E05 / red | Exposición pública de DB o worker => rechazar |
| CFG06 | topology.ingressPolicyRef: referencia TLS/CORS | E06 / red y seguridad | Ausencia de evidencia TLS, CORS y proxy => no activar |
| CFG07 | topology.serviceContractRef: identificador de API/worker | E07 / aplicación | Sin scope y ownership => rechazar |
| CFG08 | topology.authPolicyRef: referencia RBAC/JWT/revocación | E08 / seguridad | Nunca default-allow ni usar guard Angular como permiso |
| CFG09 | topology.secretRef: referencia **opaca**, no valor | E09 / custodio IAM | Password/token/DSN en claro => rechazar y no registrar |
| CFG10 | topology.databasePlanRef: referencia de compatibilidad | E10 / datos | No certificar MySQL 8.4/EF Core 10 sin integración real |
| CFG11 | topology.volumePolicyRef: referencia de retención | E11 / operaciones | Cuota/retención no aprobada => NO_MEDIDO y NO_GO para activar |
| CFG12 | topology.backupContractRef: referencia de evidencia | E12 / continuidad | Sin hash/ensayo de restore => no declarar restaurable |
| CFG13 | topology.mediaPolicyRef: referencia de ACL/tenant | E13 / medios | Cross-tenant o firma ausente => rechazar |
| CFG14 | topology.observabilityPolicyRef: ref de filtrado | E14 / operación | PII, JWT o secreto sin redacción => rechazar emisión |
| CFG15 | topology.migrationWaveRef: referencia de plan | E15 / propietario del cambio | Sin ventana, permisos, rollback y G01–G16 => inactivo |
| CFG16 | topology.rollbackCheckpointRef: referencia de evidencia | E16 / continuidad | Sin prueba, checksum o owner => NO_GO |

Son campos **propuestos, no variables de entorno introducidas**. Ningún ID de esta tabla corresponde automáticamente a una tabla SQL, campo del servidor, recurso Hostinger o contrato HTTP ya implementado. Cualquier campo desconocido conserva su valor de permiso **denegado** y nunca habilita un módulo por mera presencia.

## F01–F12 — Flags propuestos y reglas de aislamiento

| ID | Feature flag conceptual FUTURO | Defecto obligatorio | Condición verificable de habilitación posterior |
| --- | --- | --- | --- |
| F01 | topology.proposalReviewEnabled | false | Revisión E01 aprobada y propietario conocido |
| F02 | topology.privatePlacementPreview | false | E02/E04/E05 consistentes, solo simulación autorizada |
| F03 | topology.ingressComparisonPreview | false | E06/E07 con pruebas TLS/CORS futuras |
| F04 | topology.secretReferenceReview | false | Referencias E09 opacas, IAM y revocación aprobadas |
| F05 | topology.databaseCompatibilityProbe | false | MySQL 8.4 aislado autorizado y prueba real EF/Oracle |
| F06 | topology.syntheticBackupPreview | false | Datos sintéticos y plan de integridad verificable |
| F07 | topology.restoreDryRunPreview | false | Ambiente aislado, hash y rollback aprobado |
| F08 | topology.tenantIsolationProbe | false | Identidades/roles A/B y negativos reales planificados |
| F09 | topology.workerIdempotencyProbe | false | Correlación, reintentos limitados y dead-letter futura |
| F10 | topology.mediaAclProbe | false | Permisos por tenant y ausencia de fugas |
| F11 | topology.observabilityComparison | false | Redacción, cuota, retención y métricas autorizadas |
| F12 | topology.cutoverEnabled | **false bloqueado** | R3: nueva autorización expresa + gates G01–G16, pruebas y ventana |

**Reglas transversales:** un flag no sustituye controles de backend ni seguridad; no se habilita por UI, header del cliente, query string, nombre de host, tenant slug o un documento. Cada futuro valor requiere tipo validado, issuer/owner autenticado, versión, ambiente E02, política E08, expiración/revocación y traza sin PII. El tenant objetivo se resuelve por el servidor con pertenencia vigente, no por configuración inyectada por cliente. Un flag habilitado para tenant A jamás se hereda a B. Los flags de ensayo no autorizan efectos productivos.

## Resolución, precedencia, atomicidad y rechazo

1. **Fuente de autoridad futura:** contrato versionado y firmado/aprobado por el propietario del alcance. Las preferencias del cliente no son fuente para ambiente, tenant, rol, DSN, secretos o autorización.
2. **Ámbito:** primero ambiente E02 y servicio E03; después referencia de policy del tenant validado por backend. Prohibida caída silenciosa de QA/PROD a DEV o de tenant B a A.
3. **Precedencia tipada:** negar explícitamente tiene precedencia sobre permiso heredado; valores desconocidos, caducados, ambiguos o con tipo incompatible => NO_GO. No aplicar coerción truthy de cadenas como "false", "0" o "off".
4. **Cambio futuro:** exigir revisión/versión esperada (CAS/idempotencia), auditoría de autor, diff sin valores sensibles, ruta de reversión y publicación atómica; un commit concurrente no debe pisar políticas de otro escritor.
5. **Integración segura:** un provider/secretRef no se resuelve desde documentación ni se copia a logs; solo el runtime futuro autorizado podría resolverlo desde gestor de secretos segregado.
6. **Telemetría:** registrar ruleId, correlationId y decisión redactada; no registrar JWT, cookies, password, DSN, payload fiscal, PII o identificadores de otro tenant. Cuotas y SLO sin muestra real => NO_MEDIDO.
7. **Compatibilidad/rollout:** nuevas claves son opt-in no disruptivas y no alteran RenderEnvironmentContractGuard, EnvironmentDatabaseGuard, Vercel, Render, Aiven ni Cloudinary vivos. Sin suite dirigida y aprobación futura no activar F01–F12.

## ER-CFG01–ER-CFG08 — Rechazos tipados FUTUROS

| Código conceptual | Causa | Efecto futuro exigido |
| --- | --- | --- |
| ER-CFG01 | UNKNOWN_ENVIRONMENT | Denegar y conservar servicio actual |
| ER-CFG02 | VERSION_CONFLICT | No aplicar cambios parciales |
| ER-CFG03 | TENANT_SCOPE_MISMATCH | Denegar sin exponer existencia ajena |
| ER-CFG04 | ROLE_OR_REVOCATION_FAILED | Denegar y auditar de forma redactada |
| ER-CFG05 | UNSAFE_SECRET_REFERENCE | Denegar y no persistir valor secreto |
| ER-CFG06 | FLAG_VALUE_INVALID | Denegar; no coerción booleana |
| ER-CFG07 | PROVIDER_OR_INTEGRATION_UNVERIFIED | NO_GO para transición |
| ER-CFG08 | CUTOVER_NOT_AUTHORIZED | F12=false y no realizar despliegue |

Errores, códigos y flags no son clases, respuestas HTTP ni pruebas realizadas.

## P01/N01–P12/N12 — Pares de aceptación FUTUROS_NO_EJECUTADOS

| Par | Positivo futuro en ambiente expresamente autorizado | Negativo que debe producir rechazo |
| --- | --- | --- |
| P01/N01 | CFG01 versión y owner aprobados | Revisión desconocida activa sin aprobación |
| P02/N02 | CFG02 DEV aislado | Credenciales DEV usadas en PROD |
| P03/N03 | CFG03/04 workload y placement coherentes | Ubicación sin owner o zona |
| P04/N04 | CFG05/06 red privada y TLS comprobados | MySQL público o TLS desactivado |
| P05/N05 | CFG08 rol, JWT, membresía vigentes | Rol revocado o tenant ajeno aceptado |
| P06/N06 | CFG09 secretRef opaca validada | Password o token literal en configuración |
| P07/N07 | F05 ensayo EF/MySQL 8.4 sintético validado | Build confundido con integración real |
| P08/N08 | F07 restore compara hash y conteos | Backup corrupto marcado correcto |
| P09/N09 | F08 tenant A no accede a B | Flag heredado a B desde A o header manipulado |
| P10/N10 | F09 job idempotente con correlationId | Replay genera segunda operación |
| P11/N11 | F11 emisión sin PII y dentro de cuota | JWT o PII exfiltrados en log |
| P12/N12 | F12 requiere orden R3, gates y rollback | Documento M02 tomado como permiso de cutover |

**Resultado real de C12-01.M02.04:** 0/12 pares ejecutados; ninguna prueba HTTP, MySQL, .NET, Angular, navegador, VPS, Docker, DNS, secrets o proveedor. A1 consiste exclusivamente en revisión documental de 16 campos tipados, 12 flags, 8 errores, 12 pares P/N, compatibilidad con E01–E16 y G01–G16; A2 es evaluación estática de tenant A/B, roles, revocación, segregación de ambientes y referencias opacas. A3 = NO_APLICA por no tocar código ni base. No afirmar que futuros escenarios han aprobado.

## Gates, entrega A4, A5 y rollback

- **Entrada:** PLAN H=PENDIENTE/I vacío, COLA G/O=SI, DAG F=VALIDADO/G=SI, candado SQV17_C12_01_M02_04 con ID 2000001739 y CAS exclusivo, un solo EN_PROGRESO propio. Predecesor M01.01–06 LISTO con A5; M02.01–02 son referencias documentales, no permisos de producción.
- **Salida A0–A4:** publicar este documento y una sección indivisible de CHANGELOG_AI.md mediante commit fast-forward exclusivo en dev; comprobar SHA exacto Scope Lock/V5 completed/success, PLAN/COLA/BITACORA readback. Una CI externa queued/in_progress mantiene VALIDANDO sin PASS.
- **A5:** revisión independiente de Task Javier diferente de DEV04, con libertad de rechazar y registrar defecto. M02.06 y M03 no se habilitan sin DAG real, LISTO+A5 de cada antecedente requerido y permisos concretos.
- **G01–G16 de M01.06:** continúan sin acreditarse para despliegue. F12 cortado por defecto. No activar QA/main/PROD, VPS/Hostinger, Cloudflare, DNS, puertos, MySQL real, secrets, workers ni compras.
- **Rollback documental:** git revert normal tras revalidar SHA/HEAD, preservando autoría, changelog de terceros y BITACORA. Ante defecto, A5_FAIL/reapertura con regresión; nunca force-push ni desactivar tests.

**Handoff:** usar este contrato solo para las siguientes definiciones documentales y diseño de pruebas futuras. Los resultados de runtime son NO_MEDIDO/NO_EJECUTADO, no SUCCESS.