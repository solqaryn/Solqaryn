# SOLQARYN V22 — C04-02.M03.02 Application preflight contractual

**PLAN D627:** «Implementar caso de uso, servicio o aplicación con límites claros». **Task autora:** Javier DEV03, `6adc2e4bd8a48191ac7d8641ed95a1a3`; repo solqaryn/Solqaryn, rama dev; PROJECT_SCOPE_LOCK=STRICT. Riesgo R2 (contratos, tenant, políticas comerciales).

## A0 — entrega específica sin certificar M03 completa

`C0402ContractApplicationService.PrepareAsync` conecta la máquina de estados `C0402ContractDomainRules` de M03.01 con dos contratos para futuros adaptadores de servidor: `IC0402VerifiedContractActorSource` y `IC0402VerifiedContractSnapshotSource`. El `TenantHint` no otorga autoridad y se contrasta exclusivamente contra la empresa autenticada; la consulta de contrato se invoca solo con tenant del actor verificado. El contrato versionado debe ser propiedad de ese tenant y su política comercial venir de fuente verificada. Se niegan credenciales, pertenencia, RBAC, revocación, versión caduca y autoridad inventada por cliente.

El único resultado afirmativo es `REAL_AUTHORIZED_ATOMIC_TRANSACTION_AND_POLICY_STILL_REQUIRED`, **candidato de preflight** sin side effect: `AuthorizesContractMutation/PaymentOrRefund/PrivateRead/CertifiesJwtHttpMysql/PersistsData` son siempre false. No se registra DI, API, adaptador Oracle EF, migración, firma, cobranza, cancelación, reembolso, renovación ni worker.

## A1 — prueba xUnit en GitHub-hosted .NET10

La clase `C0402M0302ContractApplicationServiceTests` verifica preflight positivo inerte y los rechazos: parámetros inválidos, operación desconocida, texto con control characters, cambios de tenant A/B, escritura/pago/roles falsificados, JWT no validado, roles revocados, fuentes caídas, dueño de contrato distinto, versión caduca, política no acreditada, transición inválida y cancelación antes/entre fuentes. Los dobles son **solo tests de aplicación** y no autentican JWT real ni consultan MySQL.

## A2/A3 — límites

Esta microtarea no altera runtime HTTP, JWT, RBAC relacional, base SQL/EF ni UI: `NO_APLICA` para integración runtime a este diff, no PASS de componentes no probados. Sigue pendiente una implementación de fuentes reales contra JWT firmado, RBAC/MySQL 8.4, aprobación legal D01–D08 y CAS transaccional de contrato; esas son entregas separadas M03.03–06. No confundir `CommercialEvidence` sintética con políticas aprobadas.

## A4/A5, seguimiento y rollback

Verificar exact-SHA Scope Lock, V5, Backend Release/xUnit y seguridad pertinentes. Mientras CI sea QUEUED/IN_PROGRESS registrar VALIDANDO/owner/sha/run, liberar candado V17 tras readback y no acreditar PASS. Una Task Javier distinta de DEV03 realizará A5 independiente tras LISTO A0–A4. BITACORA append-only, DAG/COLA por fórmulas; no QA/main/PROD, secretos, PSP ni pagos. Rollback `git revert` normal en dev con CI, sin force-push.
