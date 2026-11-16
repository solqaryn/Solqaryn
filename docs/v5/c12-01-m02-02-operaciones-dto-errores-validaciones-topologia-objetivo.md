# SOLQARYN V17 — C12-01.M02.02: operaciones, DTO, errores, validaciones y compatibilidad de topología objetivo

**ID:** C12-01.M02.02 · **PLAN_MAESTRO:** fila 1737 · **Autor:** PRIMARY DEV 03 JAVIER, TASK_ID 6adc2e4bd8a48191ac7d8641ed95a1a3 · **Riesgo:** R0 documental · **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL y PLAN/COLA/DAG. **Repositorio:** solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT.

## A0. Alcance y fronteras

Definir interfaces **conceptuales** de revisión de la topología objetivo C12-01, no endpoints, clases .NET, DTO compilados, esquemas de base, servidores, secretos, proveedores, DNS ni modificaciones de red. Antecedentes: C12-01.M01.01–06 (dieciséis gates G01–G16, riesgos abiertos, rollback futuro) y C12-01.M02.01 (E01–E16, I01–I20). Son diseños sujetos a autorización futura. Vercel/Render/Aiven/Cloudinary constituyen el despliegue vigente; Hostinger y otras alternativas siguen NO_VERIFICADAS/NO_AUTORIZADAS. La sola documentación no levanta bloqueos del M01.06 ni permite QA/main/PROD.

Componentes consumidores **futuros**: arquitectura, plataforma, seguridad, datos y operación. Capas/DTO/API efectivos modificados: ninguno. MySQL 8.4/EF Core 10, JWT/RBAC, separación tenant y secretos existentes: invariantes preservadas, sin cambios de runtime. Rollback de este documento: git revert normal sobre HEAD verificado, sin borrar BITACORA.

## OP01–OP08: operaciones de diseño FUTURAS_NO_IMPLEMENTADAS

| ID | Operación conceptual | Entrada y decisión futura | Negativa obligatoria |
| --- | --- | --- | --- |
| OP01 | DraftTopologyRevision | D01+D02 -> D03 (borrador inactivo) | Sin owner o versión válida |
| OP02 | EvaluateEnvironmentBoundaries | D03+D04 -> D05 | Cruce DEV/QA/PROD |
| OP03 | ReviewWorkloadPlacement | D03+D06 -> D07 | Red, puerto o privilegio ambiguo |
| OP04 | ReviewIngressAndServiceContract | D03+D08 -> D09 | TLS, ruta, CORS o proxy sin prueba |
| OP05 | ReviewSecretAndAccessReferences | D03+D10 -> D05 | Secreto expuesto, revocación o ref ajena |
| OP06 | EvaluateDataAndRestoreProposal | D03+D11 -> D05 | MySQL 8.4 o backup sin evidencia futura |
| OP07 | CompareMigrationProposal | Dos D03+D12 -> D05 | Diferencia destructiva no autorizada |
| OP08 | EvaluateRollbackReadiness | D03+D11+D12 -> D05 | Sin hash, checkpoint o ventana autorizada |


Estas son operaciones de evaluación de un **documento inactivo**. OP01 no crea ni almacena un recurso efectivo; OP02–OP08 no mutan proveedores, tráfico, DNS, MySQL, IAM ni secretos. No se asignan URLs o verbos HTTP a operaciones todavía inexistentes. Un futuro endpoint requerirá scope propio, implementación real en backend, tests negativos y revisión R2/R3 según riesgo.

## D01–D12: DTO propuestos, no clases ni payloads reales

| ID | Tipo conceptual | Campos de diseño permitidos |
| --- | --- | --- |
| D01 | DraftRequest | topologyRevisionId, requestVersion, actorRef, purpose |
| D02 | TopologyScope | environmentKey, ownerRef, tenantBoundaryPolicyRef |
| D03 | TopologyProposal | E01 revision, E02 environments, E03 workloads, proposalState |
| D04 | BoundaryReview | environmentKey, privateZoneRefs, identityPolicyRefs |
| D05 | ReviewDecision | decisionCode, ruleIds, evidenceRefs, gaps, isBlocked |
| D06 | PlacementReview | workloadKey, placementRef, privateNetworkZoneRef |
| D07 | PlacementDecision | acceptability, deniedRules, missingEvidence |
| D08 | EndpointReview | serviceKey, ingressRef, tlsPolicyRef, corsPolicyRef |
| D09 | EndpointDecision | allowedDesign, deniedRules, proposedChecks |
| D10 | SecurityRefReview | policyRef, secretRef, roleScope, revocationPolicyRef |
| D11 | DataContinuityReview | databasePlanRef, backupPlanRef, restoreEvidenceRef |
| D12 | MigrationRollbackReview | waveRef, checkpointRef, diffSummary, ownerApprovalRef |


Todos los campos son propuestas versionadas. Nunca transportar JWT, contraseña, DSN, API key, secretos, datos personales o autoridad de tenant suministrada por cliente. Las referencias a E01–E16 pertenecen a la misma TopologyRevision y frontera E02, salvo relación expresamente autorizada y comprobada. Campos opcionales desconocidos no confieren permisos. Ningún DTO prueba que Hostinger, DNS o MySQL destino existan.

## ER01–ER12: errores tipados de diseño, sin status HTTP implementado

| ID | Error conceptual | Comportamiento de rechazo |
| --- | --- | --- |
| ER01 | OWNER_REQUIRED | Rechazar propuesta sin custodia |
| ER02 | UNKNOWN_REVISION | Rechazar versión desconocida |
| ER03 | ENVIRONMENT_BOUNDARY_VIOLATION | No mezclar DEV/QA/PROD |
| ER04 | TENANT_SCOPE_MISMATCH | No aceptar recurso del otro tenant |
| ER05 | NETWORK_POLICY_UNVERIFIED | Exigir prueba de red privada |
| ER06 | TLS_OR_INGRESS_UNVERIFIED | Exigir prueba de TLS/routing |
| ER07 | SECRET_REFERENCE_UNSAFE | Rechazar valor secreto o referencia ajena |
| ER08 | ROLE_OR_REVOCATION_UNVERIFIED | No inferir IAM/RBAC efectivo |
| ER09 | DATABASE_COMPATIBILITY_UNVERIFIED | No acreditar EF/MySQL sin integración |
| ER10 | RESTORE_EVIDENCE_MISSING | No inferir respaldo restaurable |
| ER11 | DESTRUCTIVE_DIFF_BLOCKED | Nueva autorización y rollback |
| ER12 | CUTOVER_NOT_AUTHORIZED | No activar infraestructura ni PROD |


No inventar códigos HTTP ejecutados. Una futura API distinguirá autenticación, autorización, conflicto de versión, validación y servicio no disponible sin filtrar recursos ajenos. Los errores no habilitan fallback de permiso ni secreto.

## V01–V16: validaciones de seguridad y compatibilidad FUTURAS

| ID | Regla que deberá demostrar una implementación posterior |
| --- | --- |
| V01 | Backend autentica actor y owner; no confiar solo en UI |
| V02 | Versión e ID de revisión vinculados al ámbito correcto |
| V03 | Referencias E01–E16 consistentes y no huérfanas |
| V04 | DEV/QA/PROD sin identidades, secretos ni datos compartidos |
| V05 | Tenant A/B no derivado de headers cliente o hostname |
| V06 | Placement E03-E04-E05 sin MySQL expuesto públicamente |
| V07 | Ingress E06 requiere TLS, CORS y pruebas de proxy |
| V08 | E08 valida RBAC, membresía y revocación real |
| V09 | E09 referencia opaca sin secretos en claro |
| V10 | E10 no declara EF/MySQL 8.4 compatible sin integración real aislada |
| V11 | E11/E12 requieren backup y restauración con hashes |
| V12 | E13 mantiene ACL/firmas de medios por tenant |
| V13 | E14 redacta PII, JWT y credenciales en logs |
| V14 | E15 bloquea migración destructiva y falta de ventana |
| V15 | E16 pide evidencia causal de rollback efectivo |
| V16 | Ausencia de datos, medidas o versión conocida implica NO_GO |


La compatibilidad futura exige versionado de contractos, datos canónicos y rechazo de cambios de semántica de autorización. Sin medidas aprobadas, RTO/RPO, costes, rendimiento, cuotas y capacidad se reportan NO_MEDIDO. Ninguna prueba real de proxy, host, tenant A/B, red privada, EF, SQL o restore se deriva de este documento.

## P01/N01–P12/N12: casos futuros, cero ejecutados aquí

| Par | Positivo de laboratorio futuro | Negativo adversarial |
| --- | --- | --- |
| P01/N01 | Draft con owner, ámbito y versión válidos | Draft sin owner aprobado |
| P02/N02 | DEV con identidad segregada | Credencial de DEV reutilizada en PROD |
| P03/N03 | Placement en red privada | Puerto MySQL público aceptado |
| P04/N04 | Ingreso TLS/CORS verificado | Ruta sin TLS aprobada |
| P05/N05 | Rol y revocación evaluados | Actor revocado conserva autorización |
| P06/N06 | Referencias de secretos opacas | Contraseña en DTO o registro |
| P07/N07 | Tenant A solo enlaza recursos A | Ref de B aceptada en A |
| P08/N08 | MySQL 8.4 efímero migrado y probado | Build tomado como prueba de base |
| P09/N09 | Restore sintético con checksum | Backup corrupto declarado correcto |
| P10/N10 | Medio con ACL scoped comprobada | URL de A entrega objeto B |
| P11/N11 | Cambio aditivo con reversión ensayada | Diff destructivo sin permiso |
| P12/N12 | Cutover solo con orden separada | M02 documental tomado como permiso PROD |


**Resultado de esta microtarea R0:** 0 de 12 pares ejecutados, cero endpoints creados y cero validaciones HTTP, MySQL, VPS, DNS, Playwright o proveedor realizadas. A1 presente es revisión estática de OP01–08, D01–12, ER01–12, V01–16, los doce pares y las relaciones E01–E16. A2: inspección documental de segregación DEV/QA/PROD, tenant A/B, roles, revocación, redes y referencias de secretos; NO afirma seguridad de un runtime inexistente. A3: NO_APLICA a integración por ausencia de cambio ejecutable.

## A4, dependencias y A5

Entrada: PLAN_MAESTRO H=PENDIENTE/I vacío, COLA G/O=SI, DAG F=VALIDADO/G=SI y candado atómico V17 antes de CAS. M01.01–06 LISTO+A5 son dependencias reales; M02.01 es fuente documental, no dependencia inventada. Al entregar, usar commit fast-forward CAS en dev con dos archivos Markdown (este contrato y sección indivisible de CHANGELOG_AI.md); comprobar Scope Lock y procedencia V5 completed/success de SHA causal o equivalencia estrictamente probada y readback PLAN/COLA/BITACORA. A5 por una Task Javier distinta del autor, que puede rechazar. Una sucesora técnica no se libera sin sus gates y A5 correspondientes.

**Rollback y límite de autoridad:** ante fallos de CI mantener VALIDANDO/owner/sha; CANCELLED, SKIPPED, QUEUED, IN_PROGRESS nunca sustituyen un PASS requerido. No se modifica cronograma, reglas de ramas, QA/main/PROD, secretos, datos, proveedor ni infraestructura. Revertir de forma aditiva y conservar evidencia y autoría.