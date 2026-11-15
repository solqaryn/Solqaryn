# SOLQARYN V17 — C14-01.M03.03: autorización y tenancy fail-closed de aceptación funcional

**Autor:** PRIMARY DEV 06 — JAVIER, TASK_ID \`6af16bbad26881918c283abe06e1f32a\`. **Riesgo:** R2, autorización/tenant; única rama \`dev\`; PROJECT_SCOPE_LOCK=STRICT. **Fuentes:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V17, PLAN_MAESTRO 2032, DAG, COLA, BITACORA, AGENTS y Skill canónica.

## A0 — Entrega técnica y límites

Se agrega \`C1401FunctionalAcceptanceAuthorizationGate\` en Application y su lector \`IC1401FunctionalAcceptanceAuthorityReader\`. El contrato obliga a leer del **backend confiable** el recurso persistido, SHA exacto, identidad actual autenticada, membresía viva, grant RBAC relacional vigente, tenant y propiedad comprobados, además de catálogos de casos/acciones emitidos por servidor. Deniega cualquier privilegio, pertenencia o permiso alegado desde el cliente, bypass productivo, dueño ajeno, evidencia incompleta y dependencia caida.

Compone \`V5FunctionalAcceptanceApplicationService\` de C14-01.M03.02 y \`V5FunctionalAcceptanceDomainRules\` de C14-01.M03.01, sin dependencias Domain→Infrastructure. Es **código ejecutable de prevalidación R2**, no un endpoint, interceptor JWT real, DI/API live, transacción, migración EF/MySQL, PSP, autorización de recursos, ensayo browser, QA ni publicación PROD. El \`VerifiedContext\` debe provenir en futuro de un adaptador confiable; jamás de la solicitud. Un preflight favorable devuelve \`AUTHENTICATED_FUNCTIONAL_ACCEPTANCE_ADAPTER_REQUIRED\` y todos los indicadores de concesión y certificación permanecen **false**.

## A1 — Pruebas reales dirigidas .NET

\`C1401FunctionalAcceptanceAuthorizationGateTests\` cubre camino válido limitado, recurso/tenant/SHA malformados, escalación alegada por cliente, fuente no disponible o divergente, sesión sin JWT verificado, membresía revocada, falta de RBAC, ownership ausente, recurso de tenant B, catálogo nulo, acción falsa, SHA de evidencia distinto, lote vacío/excesivo, PII, escenario negativo ausente, rollback ausente, excepción del lector y cancelación. Las pruebas reales de CI Release y seguridad son obligatorias en el SHA publicado; cero pruebas se computan aprobadas antes de la evidencia.

## A2 — Seguridad

El gate deniega antes de evaluar Domain si faltan JWT actual, membresía, RBAC, propiedad o no-revocación. Rechaza assertion tenant B y propiedad B aunque el token de A fuera formalmente válido. El scope de cliente no puede generar un grant. Los errores de proveedor devuelven código opaco, sin propagar PII. El resultado favorable **nunca autoriza HTTP, consulta DB, acciones comerciales, QA o PROD**.

## A3 — Integración y NO_APLICA

Application→Domain es la integración ejecutable real compilada/probada por xUnit. Sin cambios en infraestructura, controlador, EF Core, MySQL, Angular o navegador: pruebas efímeras MySQL, HTTP y Playwright = NO_APLICA a este changeset concreto. Un test unitario no certifica integración productiva. Rollback: \`git revert\` normal del commit de dev tras HEAD y pruebas, sin force-push.

## A4/A5 — Handoff verificable

Exigir Scope Lock y procedencia V5, Backend Release, backend build, seguridad multiempresa y autorización cuando resulte pertinente; status QUEUED/IN_PROGRESS/CANCELLED/SKIPPED nunca PASS. Registrar SHA de publicación, blobs, CI, EVENT_ID idempotente y readback PLAN/COLA/BITACORA. Mover a VALIDANDO antes de evaluar A1–A3 causalmente. LISTO_DEV solo cuando A0–A4 correspondan a checks SUCCESS reales. A5 debe realizarla otra Task Javier independiente, jamás DEV06. Sin cambiar QA/main/PROD, secretos, schedules ni fórmulas.
