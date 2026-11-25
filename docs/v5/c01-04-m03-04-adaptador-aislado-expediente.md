# SOLQARYN V5 — C01-04.M03.04: adaptador aislado de lectura tenant-scoped

**Autor:** PRIMARY DEV 05 JAVIER_CHATGPT, TASK_ID `6adc2e55f47c81919f6374e561687076`. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL; PLAN_MAESTRO fila 269, COLA G/O=SI, DAG F=VALIDADO/G=SI, previa M02 y M03.01–03. **Entorno:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT.

## Entrega ejecutable y frontera de confianza

Se introduce código real .NET 10 en Infrastructure `backend/src/Infrastructure/AutomationV5/V5UniversalCaseFileIsolatedReadAdapter.cs`. `V5UniversalCaseFileIsolatedReadAdapter` implementa `IV5UniversalCaseFileVerifiedEvidenceReader` existente en Application (M03.03) mediante **dos lectores inyectables y separados**: `IV5CaseFileTrustedActorSnapshotSource` para identidad/membresía/permisos verificados en backend y `IV5CaseFileTenantScopedSnapshotSource` para la lectura `(tenantId,caseFileId)` desde una futura fuente persistente aislada. El tenant usado en el segundo lookup procede sólo del lector de identidad servidor, nunca de una cabecera/URL/campo del cliente. El adaptador rechaza sin evidencia identificadores inválidos, actor anónimo, membresía/permiso caducados, revocación, SHA no completo o desalineado, archivo sin ownership persistido, tenant A→B, expediente distinto, rama ajena, legacy ambiguo o versión inválida.

**No implementa tabla, entidad EF, DbSet, migración MySQL, consulta SQL, transaction, DI productiva, controller, worker, API ni permiso de acceso.** Los dos lectores son interfaces que únicamente una integración posterior con autoridades de servidor puede implementar. Las flags de las fuentes no se aceptan desde solicitudes cliente y esta clase no se registra como endpoint ni como proveedor real. La salida `VerifiedEvidence` se pasa al gate Application M03.03, que siempre devuelve un preflight `TRUSTED_CASEFILE_EXECUTION_REQUIRED` sin conceder `GrantsTimelineRead`, `GrantsCaseWrite`, `GrantsAttachment`, `AllowsTenantFilterBypass` ni `GrantsInstitutionalWrite`. Por tanto, **NO** es autenticación JWT/HTTP ni certificación de propiedad de una fila MySQL real.

## Validaciones y rollback

La suite xUnit `backend/tests/Solqaryn.Tests/Infrastructure/V5UniversalCaseFileIsolatedReadAdapterTests.cs` introduce pruebas de caso positivo limitado, sin autorización efectiva, y múltiples negativas: id inválido sin I/O, actor anónimo, membresía/rol revocados, SHA inválido, tenant cruzado, recurso equivocado, versión inválida, legacy sin atribución, prueba de persistencia ausente, rama cruzada, caso ausente, cancelación y constructor sin dependencias. Las aserciones son reales y deben ejecutarse en GitHub Actions del SHA publicado; **no se declaran PASS hasta concluir** Backend Release, seguridad multiempresa, autorización pertinente, Scope Lock y procedencia V5. `Category=Integration` MySQL es NO_APLICA a este componente sin proveedor/consulta EF; no representar mocks como SQL ni certificación HTTP.

- **A0:** diff aislado Infrastructure + tests + este documento + CHANGELOG; preserva Domain/Application API y config sin cambiar DI ni migraciones.
- **A1:** xUnit positivos/negativos de lookup compuesto `tenantId,caseFileId` y gate sin grants.
- **A2:** fail-closed tenant A/B, revocación, source SHA, propiedad/legacy/branch y cancelación, sin secretos, PII ni datos reales.
- **A3:** compilación y ejecución .NET 10/CI proporcional, MySQL NO_APLICA explícito.
- **A4:** publicación fast-forward/CAS, blobs, check-runs `completed/success` sobre SHA causal o equivalencia rigurosa y readback institucional.
- **A5:** requiere Task Javier distinta del autor DEV05; LISTO_DEV no autoriza QA/main/PROD.

**Rollback:** `git revert` ordinario del commit individual. Al no registrar el adaptador en DI ni modificar esquema o datos, la reversión no necesita operaciones de bases; revalidar CI tras revertir. Ninguna futura implementación persistente queda autorizada ni simulada por este documento.

## Corrección P2 de revisión A5 — 2026-11-15

La revisión independiente A5_FAIL identificó que las banderas `MembershipReadFromTrustedBackend` y `PermissionReadFromTrustedBackend` establecían únicamente procedencia, no el resultado afirmativo de la autorización. `ActorSnapshot` ahora exige `MembershipActive` y `ExplicitPermissionGranted` como booleanos explícitos sin valores por defecto. Antes de consultar expedientes, `ReadAsync` rechaza pertenencia inactiva o permiso denegado aunque los lectores de servidor hayan respondido correctamente; `ServerAuthority` recibe las decisiones verificadas y no un `true` fabricado. Se añaden pruebas xUnit con tres combinaciones negativas, además de aserciones positivas de pertenencia/permiso en el preflight.

No se incorporan endpoints, registros DI, MySQL, migraciones, acceso a archivos ni cambios de JWT. La corrección no convierte el adaptador aislado en permiso de lectura/escritura. Los controles .NET 10, xUnit, seguridad multiempresa, autorización, Scope Lock y procedencia V5 deben aprobarse en el SHA causal. El A5_FAIL anterior **permanece vigente** hasta nueva revisión independiente por una Task Javier diferente de DEV 05; no desbloquear el DAG ni afirmar cierre por la sola publicación. Reversión mediante git revert ordinario, sin force-push ni tocar QA/main/PROD.


## R11: vinculación de evidencia a operación y expediente (2026-11-25)

El contrato aislado distingue selector de autoridad: el lector por acción recibe `caseFileId` y `CaseAction`, y la evidencia debe acreditar esa acción exacta. El adaptador requiere que su fuente de actor confirme recurso y operación antes del I/O. Los proveedores anteriores que no implementan el nuevo contrato deniegan por defecto. No se crean grants, ni se deduce permiso de un rol global, del JWT o de los parámetros del cliente. Las pruebas xUnit de contrato no acreditan integración JWT/RBAC/MySQL; sigue pendiente la fuente auténtica de permisos por operación y revalidación post-I/O. R11, foco C01-04.M03.06; se conserva la certificación histórica aislada y no se otorga nueva A5. Rollback: revert ordinario del commit en dev y CI causal.
