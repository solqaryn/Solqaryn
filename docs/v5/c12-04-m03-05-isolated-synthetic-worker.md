# SOLQARYN V18 — C12-04.M03.05: worker aislado de revisión sintética

**Riesgo R2.** Autor DEV03 Javier, TASK_ID 6adc2e4bd8a48191ac7d8641ed95a1a3. Rama dev, PROJECT_SCOPE_LOCK STRICT; V17 lock SQV17_C12_04_M03_05 / 2000001854, PLAN fila 1854. Reutiliza M03.04 y los contratos C1204VerifiedActor de Application.

## A0: implementación real, pero no cutover
Implementa `C1204SyntheticManifestReviewWorker` y `C1204SyntheticManifestReadPort` en Infrastructure. El worker realmente lee manifiestos previamente preparados **en memoria** por `C1204IsolatedManifestAdapter`, con un máximo de ocho solicitudes por revisión, snapshot antes de cualquier await, SHA 40 caracteres lowercase, revisión 1..1.000.000, claves únicas y aislamiento estricto de tenant. Compara por clave, tenant, SHA y revisión sin copiar datos de manifests al resultado. Todo resultado deja falsos CertifiesMysqlBackupOrRestore, AuthorizesSqlOrCutover, GrantsTenantAccess, ExecutesHostedWorker, WritesPersistentData y AuthorizesQaOrProduction.

No está registrado como HostedService, DI, API, endpoint, SQL, migrador, restaurador, replicador, cola, scheduler, proveedor externo ni operador de datos reales. No modifica EF/DbContext, schema, DSN, grants, JWT ni archivos de producción. Un fixture de `C1204VerifiedActor` no es JWT HTTP real ni autorización end-to-end. Una lectura sintética exitosa nunca es evidencia MySQL restaurado.

## A1–A3: regresiones negativas en .NET 10
xUnit con adaptador **real in-process** (no respuesta de mock para el positivo), dos manifests staged y luego leídos, verificando conteo y no autoridad. Pruebas adversariales: tenant B cruza A, membresía/JWT/rol/MFA revocados, SHA falsificado, clave duplicada o insegura, límite 0/9, revisión desfasada, respuesta adulterada del puerto, error con secreto en excepción y cancelación. Ensayo en GitHub Actions exact-SHA, Suite dirigida + Release y seguridad. MySQL 8.4/EF externo: **NO_APLICA al diff actual**, porque ningún DB/EF, red, migración ni restore se invoca; los gates G01–G16 se conservan para una implementación real futura. No declarar backup/restore probado, SLO/telemetría, CI de MySQL, sistema desplegado ni P0/P1 corregidos por simples pruebas in-process.

## A4–A5 y rollback
A0–A4 requieren CI GitHub-hosted directed .NET xUnit y build/security/autorización/Scope Lock/V5 `completed/success`, SHA o equivalencia blobs auditada; RUN queued/in_progress no PASS. Estado VALIDANDO con owner real hasta cierre. A5 requiere revisor otra Task Javier diferente, sin self-approval. Deshacer solamente este changeset mediante git revert fast-forward, preservar BITACORA; sin QA/main/PROD.
