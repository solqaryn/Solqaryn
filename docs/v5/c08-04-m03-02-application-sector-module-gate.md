# SOLQARYN V16 — C08-04.M03.02: Application gate del ciclo de vida sectorial

**Autor:** PRIMARY DEV 06 Javier; TASK_ID 6af16bbad26881918c283abe06e1f32a. **Riesgo:** V15 R2, JWT/RBAC/tenant/concurrencia. **Ámbito:** solqaryn/Solqaryn, dev, PROJECT_SCOPE_LOCK=STRICT. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL y PLAN/COLA/DAG.

## A0 Requisitos y alcance

Implementar el caso de uso de prevalidación Application de M03.02 con reglas Domain ya existentes M03.01. Un lector IC0804SectorModuleEvidenceReader DEBE obtener datos nuevos del backend autenticado y persistencia: actor, estado de permisos, propiedad tenant, revisión y auditoría. Esta tarea crea el contrato y un gate fail-closed, NO el lector Infrastructure concreto, API/controller, permisos, esquema, JWT, SQL, UI, proveedor ni activación real. Un resultado positivo requiere todavía un adaptador autenticado y transacción separada.

## A1 Positivos, negativos, frontera y fallo

Catorce pruebas xUnit con asserts de preflight no autorizante, source SHA exacto, overrides cliente, tenant A/B, dueño de módulo, estado de autenticación/membresía/RBAC, auditoría y CAS, revocación, entitlement por plan/rubro, denylist, transiciones legales, idempotencia, lector vacío, cancelación. Son código fuente real; PASS solo después de ejecutar build Release y xUnit sobre SHA causal en Actions.

## A2 Seguridad y aislamiento

Requiere actor confiable actual, tenant solicitado idéntico al verificado y al dueño persistido, permiso RBAC relacional vigente, SHA de evidencia igual, auditoría y fence CAS, y políticas Domain aplicadas. No ejecuta mutaciones. No acepta UI ni headers como autoridad, no otorga acceso BOLA/IDOR. Una posterior etapa debe revalidar JWT/DB en cada petición y demostrar casos A/B reales, idempotencia durable y concurrencia transaccional.

## A3 Integración pertinente y límites

Diff Application/Tests/Docs/CHANGELOG, reusa Domain V5SectorModuleLifecycleDomainRules sin tocarlo. No modifica EF, MySQL, HTTP, Angular, Playwright, Cloudinary, distribución de sector o broker. Estas pruebas reales de integración son NO_APLICA a este diff específico, no equivalen a PASS. No certificar JWT HTTP ni SQL a través de reader simulado en xUnit.

## A4 CI, SHA, readback y rollback

Requiere build Release/xUnit, seguridad multiempresa, Authorization y Scope Lock/V5 completed-success sobre SHA propio o descendiente con identidad de blobs y config. Si CI externa sigue en curso, conservar VALIDANDO/owner; si falla, reparar causal. Registrar SHA, blobs, READBACK PLAN/COLA/DAG/BITACORA por ID. Rollback por git revert de un commit individual sobre dev, sin force-push, datos productivos, permisos, QA/main/PROD.

## A5 Revisión independiente

Solo una Task Javier distinta del autor examina requisitos, código, xUnit ejecutada y negativos, con posibilidad A5_FAIL. LISTO_DEV no da autorización de producción.
