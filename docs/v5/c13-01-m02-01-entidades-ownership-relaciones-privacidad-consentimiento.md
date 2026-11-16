# SOLQARYN V17 — C13-01.M02.01: entidades, ownership, relaciones e invariantes de privacidad y consentimiento

**Unidad:** C13-01.M02.01 · PLAN_MAESTRO fila 1880 · DAG_TECNICO fila 1841. **Autor:** PRIMARY DEV 06 JAVIER, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Riesgo:** R0 documental. **Ámbito:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, PLAN/COLA/DAG/BITACORA, política V15 y lock V17.

## A0. Resultado exigido y límites

Definir un **modelo conceptual** de privacidad, avisos, cookies, términos, elecciones, revocación, retención y derechos. Los nombres E01–E16 son **propuestas de contrato**, no entidades de EF Core, tablas MySQL, DTO compilados, endpoints, textos legales publicados, cookies efectivas ni un motor de consentimiento. Las jurisdicciones, finalidades, bases jurídicas, plazos, titulares, procesadores y autorizaciones están pendientes de determinación real y revisión competente. Ningún registro se crea ni elimina con este documento.

**Fuentes antecedentes cotejadas en dev:** M01.01 blob `79523b25247b3a2db4bc79ca723ba5adc698d7c3` (NR01–16); M01.02 `635fc95dbe1a32c628c72fd860af0a0d8e2e2dab` (actores y fronteras); M01.03 `3fa9e8bc3e6a72d17f9bb0912bc03fd5acb9b959` (brechas B01–B12); M01.04 `82d7238a73dba3275921da8349b291ceb0d46a55` (riesgos R01–14 y decisiones abiertas D01–10); M01.05 `703d0aad6cdfc93a2a39153d9fc5cbd3924dc160` (AC01–16 futuros); M01.06 `fa4c6eb61796367d8ede9f4e50b94806e929191f` (gates G01–16, rollback R01–08). Su M01.06 obtuvo LISTO y A5 independiente en PLAN/BITACORA; el gate del DAG de esta microtarea se revalida antes del claim.

## E01–E16: entidades conceptuales, owner y relación futura

| ID | Contrato futuro | Campos conceptuales mínimos | Autoridad/ownership | Relación y condición |
| --- | --- | --- | --- | --- |
| E01 | PrivacyPolicyDraft | policyDraftId, scope, locale, revision, approvalState | Propietario institucional designado | Versión E02; rechazo mientras G01/G04 abiertos |
| E02 | PolicyVersionReference | policyRef, revisionDigest, effectiveFrom? | Responsable aprobado; no autor implícito | E01 1:N E02; versiones históricas no mutables |
| E03 | ProcessingPurposeDraft | purposeKey, category, legalBasisReviewState | Responsable de finalidad por ámbito | E01 1:N E03; base jurídica no inventada |
| E04 | DataCategoryInventory | categoryKey, sensitivityReviewState, retentionClassRef? | Custodio de datos identificado | E03 N:M E04; clasificación pendiente G02 |
| E05 | TenantBoundaryReference | tenantId, membershipRevision | Empresa A/B desde backend | Claves de ámbito E06/E08/E09, jamás cabecera |
| E06 | SubjectReference | subjectRef, subjectType, identityProofState | Sujeto identificado; operador no lo suplanta | E05 1:N E06; ninguna PII en identificador compartido |
| E07 | PermissionContextReference | actorRef, tenantRef, roleRevision, revokedAt? | Servicio de autorización server-side | Obligatorio para E08/E10/E12; no otorgar grants |
| E08 | PreferenceDecisionDraft | decisionRef, purposeKey, policyVersionRef, decisionState | Sujeto E06 con autoridad E07 | E05+E06+E03+E02; nunca consentimiento implícito |
| E09 | ConsentEvidenceEnvelope | evidenceRef, opaqueCorrelation, digest, occurredAtUtc | Custodio auditable por E05 | Una decisión E08 a N evidencias; mínima PII |
| E10 | RevocationIntent | revocationRef, priorDecisionRef, reasonCode, requestedAtUtc | Sujeto autenticado y E07 vigente | No revoca B; precede usos futuros del propósito |
| E11 | PreferenceSnapshotReference | snapshotRef, lastRevision, evaluatedAtUtc | Servicio backend, no caché UI | Proyección derivada, no nueva fuente de consentimiento |
| E12 | PrivacyRightsCaseDraft | caseRef, caseType, verificationState | Solicitante E06 y revisor competente futuro | Sin plazos o derechos presuntos; ninguna exportación aquí |
| E13 | RetentionRuleProposal | ruleRef, categoryRef, legalReviewState, effectiveVersion | Autoridad jurídica competente pendiente | E04 por clase; sin DELETE/TTL automático |
| E14 | HoldAndExceptionReference | holdRef, reasonClass, approvedScope, validUntil? | Custodio autorizado futuro | E13 condicionado; protege registros y auditoría |
| E15 | ProcessorDestinationInventory | processorRef, transferReviewState, purposeRef | Responsable de integración designado | E03 con destino verificado; sin llamar proveedor |
| E16 | AuditReadbackReference | eventRef, commitDigest, actorScope, timestampUtc | Operación/auditoría con mínimo privilegio | E08/E10/E12; no BITACORA comercial de clientes |

**Cardinalidad propuesta:** cada E05 delimita sujetos E06 y decisiones E08; un E08 referencia una versión E02 y una finalidad E03, con 0..N comprobantes E09. Una E10 hace referencia a una decisión anterior sin sobrescribirla, y la proyección E11 se invalida por revisión o revocación. Una E12 sólo puede vincular sujeto/contexto verificados; E13/E14 afectan retención únicamente tras validación jurídica, nunca por su mera creación. E15 es catálogo de destinos por finalidad sin credenciales; E16 modela readback mínimo, no reemplaza BITACORA institucional ni almacena datos reales.

**Clave propuesta de aislamiento:** toda relación privada debería incorporar `tenantRef` y `subjectRef` verificados server-side; una clave global opaca sola no basta. Para anonimato público y casos legales especiales se necesita análisis adicional: no se inventa un tenant o sujeto autenticado. Mantener revisión UTC/digest, no copiar JWT ni material sensible.

## I01–I20: invariantes exigibles a futuras implementaciones

| ID | Invariante de diseño (no prueba runtime) |
| --- | --- |
| I01 | E05 tenant A/B y E06 sujeto son compuestos; no puede existir decisión E08 sin ambos |
| I02 | Identidad, membresía efectiva, rol y pertenencia se revalidan en backend antes de cualquier lectura o escritura futura |
| I03 | JWT firmado o UI guard no sustituye RBAC vigente, ownership ni verificación de revocación |
| I04 | Un actor A no lee, escribe ni deduce existencia de registros E08/E09/E10/E12 de B |
| I05 | E02 conserva versión y digest íntegros; edición no reinterpreta una aceptación anterior como nueva |
| I06 | E03 propósito no reutiliza consentimiento de otro canal/categoría/versión por coincidencia parcial |
| I07 | E08 ausencia, silencio, timeout o fallo equivale a ausencia de decisión afirmativa; no crear aceptación fantasma |
| I08 | E09 no almacena JWT, cookies privadas, credenciales, documentos de identidad ni payload libre con PII |
| I09 | E10 revocación pertinente bloquea usos FUTUROS y no elimina evidencias ni derechos no relacionados |
| I10 | Duplicación/idempotencia se delimita por tenant+sujeto+propósito+versión+operación, no ID global aislado |
| I11 | Eventos tardíos o replay no sobrescriben revisión de decisión más reciente ni anulan revocación |
| I12 | E11 snapshot de caché nunca es autoridad final; revocación invalida lecturas y procesamiento futuros |
| I13 | E12 caso de derechos exige identidad y autorización; exportación no incluye recursos ajenos ni enumera sujetos |
| I14 | E13 retención se mantiene NO_APROBADA sin jurisdicción, fundamento, categoría, custodio y excepciones |
| I15 | E14 legal hold impide eliminación destructiva por regla automática o por una interpretación documental |
| I16 | Backups, logs, caches, archivos y bases tienen estados de retención separados; NO_MEDIDO permanece NO_MEDIDO |
| I17 | E15 requiere contrato/región/destino real validados; fallo del tercero nunca concede un permiso |
| I18 | Cambio de tenant, rol o sesión invalida permisos derivados y requiere nueva comprobación de ownership |
| I19 | El acceso a evidencias por soporte/admin global es excepcional, autorizable, acotado y auditado, nunca automático |
| I20 | El diseño M02 es sólo R0; no publica políticas ni crea entidades EF, APIs, cookies, migraciones, proveedores o nuevas Tasks |

**Matriz de impacto:** frontend Angular 22.2.1 sólo muestra avisos y elecciones accesibles; API ASP.NET Core 10 deberá verificar sesión/membresía/JWT/RBAC/tenant y ownership; Application define reglas; Infrastructure Oracle EF Core/MySQL 8.4 requeriría migraciones aditivas, constraints compuestas y transacciones aisladas **en otra microtarea habilitada**. No se modifican interfaces, clases, proveedores, bases, datos, integraciones, logging, flags, rutas ni políticas; no se crea un administrador global omnipotente.

## T01–T12: pares positivo/negativo FUTUROS_NO_EJECUTADOS

| Caso | Positivo futuro comprobable | Negativo adversarial que debe fallar |
| --- | --- | --- |
| T01 | Tenant A documenta propósito y política bajo versión revisada | Tenant B consulta evidencia de A por ID |
| T02 | Decisión affirmative válida se vincula a versión y sujeto correctos | Silencio o fallo UI produce aceptación |
| T03 | Revocación válida preserva auditoría y niega usos futuros pertinentes | Evento atrasado reabre consentimiento revocado |
| T04 | Cambio de versión exige evaluación explícita según base aprobada | Aceptación de versión vieja se hereda sin revisión |
| T05 | Lectura de derechos solo entrega elementos del sujeto autorizado | IDOR/BOLA devuelve contenido o existencia ajena |
| T06 | Caché se separa por tenant+sujeto+versión/revocación | Snapshot obsoleto reautoriza tras revocación |
| T07 | Autorización con rol vigente acepta únicamente recursos propios | Token revocado o rol removido permite acceso |
| T08 | Auditoría registra digest/correlación sin PII sensible | JWT, email completo o cookie sensible aparecen en evento |
| T09 | Retención evaluada con regla y excepción aprobadas | Plazo inventado borra factura, evidencia o backup |
| T10 | Proveedor revisado respeta finalidad y destino permitidos | Timeout/SDK no aprobado ejecuta tracking opcional |
| T11 | Interfaz futura accesible acepta y rechaza con teclado | Rechazo inalcanzable por foco o contraste |
| T12 | Rollback de texto preserva revocaciones y versiones | Revertir habilita scripts o descarta trazas históricas |

**Ejecución:** 0/12 pares ejecutados; todos son oráculos diseñados para fases futuras. No se simula un navegador, API, cookie o MySQL ni se declara cumplimiento jurídico. El diseño evita que un PASS documental se confunda con una certificación de consentimiento o privacidad real.

## Dependencias, salidas y rollback

- **Entrada:** PLAN H=PENDIENTE/I vacío, COLA G/O=SI y DAG F=VALIDADO/G=SI, V17 namedRange exclusivo `SQV17_C13_01_M02_01` e identidad física de las seis Tasks. M01.01–06 documentadas y A5 del cierre M01.06 contrastado. No reescribir COLA/DAG ni tomar otra EN_PROGRESO.
- **Salida documental:** E01–E16 y I01–I20 coherentes, T01–T12 con negativo explícito, ownership y relaciones trazables; documentar NO_APLICA en runtime. Exigir A0–A4 causal y revisión A5 de otra Task Javier distinta del autor.
- **Puertas posteriores:** M02.02–05 dependen de gates actuales; M02.06 no se habilita antes de M02.01–05 LISTO+A5, y M03/QA/main/PROD no se liberan por este documento. Los G01–G16 de M01.06 siguen abiertos para despliegue, cookies y tratamiento real.
- **Rollback:** nuevo commit `git revert` tras inspeccionar HEAD, sin force-push, sin borrar BITACORA histórica ni políticas/datos reales; cualquier futura migración exige rollback y pruebas de integración separados.

## A0–A5 y handoff

**A0:** requisitos PLAN/COLA/DAG y M01.01–06, relaciones y diff **solo este Markdown + sección íntegra de CHANGELOG_AI.md**. **A1:** inspección estática de 16 entidades, 20 invariantes y 12 pares P/N capaces de exponer riesgos futuros, no tests reales. **A2:** revisión estática de tenant A/B, JWT/RBAC, revocación, minimización PII, cross-tenant e IDOR; no se ejecutó validación HTTP. **A3:** MySQL, EF, Playwright, browser, proveedores y política legal: **NO_APLICA al diff R0** porque no hay código, despliegue ni datos modificados; quedan como gates futuros no satisfechos. **A4:** commit fast-forward/CAS a dev, SHA causal, Scope Lock y V5 `completed/success`, PLAN/COLA/BITACORA readback y evento único; checks requeridos pendientes, canceled, failed o skipped **no son PASS**. **A5:** **PENDIENTE** para otro TASK_ID Javier, con revisión independiente y potestad de rechazo.

Esta unidad **no concede autorización** para ejecutar cambios de cookies, textos legales, usuarios, base, facturación, marketing, brokers, QA/main/PROD o proveedores.
