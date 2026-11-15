# SOLQARYN V16 — C02-01.M03.01: reglas Domain de resolucion server-side de tenant

**Riesgo:** R2 (identidad, JWT, RBAC, tenant A/B). **Ambito:** solqaryn/Solqaryn, rama dev, PROJECT_SCOPE_LOCK=STRICT. **Actor:** PRIMARY DEV 02 Javier, TASK_ID 6af16bb9492c8191b8e8c7d606f11507. **Fuente:** STANDARD DE IMPLEMENTACION DE AUTOMATIZACIONES V5 FINAL, C02-01.M02.01–06 y PLAN/COLA/DAG actual.

## Entrega implementada

- Codigo ejecutable puro en \`backend/src/Domain/AutomationV5/V5VerifiedTenantResolutionDomainRules.cs\`: precondiciones server-side de identidad JWT autenticada, usuario y tenant vigentes, membresia y rol frescos leidos del store, rechazo de revocaciones y fallos del proveedor de autoridad.
- Dos modos delimitados: seleccion de tenant por sesion verificada y binding de host registrado por autoridad backend. Un host desconocido/ambiguo/caducado, forwarded host sin proxy confiable o mapping tenant B no valida contexto A.
- El tenant en ruta/DTO solo expresa un objetivo; nunca es un grant. Rechaza overrides del cliente, rol global legado, fallback predeterminado y ownership de recurso/sucursal cruzado A/B. Devuelve solo codigos seguros no enumerativos.
- Un resultado de preflight positivo se llama \`TRUSTED_BACKEND_ENFORCEMENT_REQUIRED\`; nunca autoriza acceso HTTP, crea JWT, cambia \`HttpContext\`, consulta MySQL, modifica membresias, registra un host o omite filtros EF. API/Application deben aportar evidencia real y aplicar JWT/RBAC/ownership/auditoria. Integracion de servicio/middleware es competencia de etapas M03.02–06 y sus pruebas propias.

## Evidencia funcional de esta unidad

\`backend/tests/Solqaryn.Tests/Domain/V5VerifiedTenantResolutionDomainRulesTests.cs\` incluye doce casos xUnit, positivos, negativos, fronteras y errores: membresia fresca A, switch A→B con pertenencia B independiente, host fiable, host suplantado, tenant equivocado, pertenencia y rol revocados, JWT ausente, tenant suspendido, store no disponible, solicitud sin autoridad, sucursal ajena y superficie desconocida. Las pruebas son ejecutables, **no se consideran PASS hasta resultado real de GitHub Actions sobre SHA causal**.

## A0–A5 y limites

- **A0:** diff exacto de un archivo Domain, un test xUnit, este documento y seccion autocontenida del CHANGELOG; sin cambios a infraestructura, API, EF, SQL, Angular, JWT real, secretos ni deployment.
- **A1:** xUnit ejecutado por GitHub Actions \`SOLQARYN - Build y pruebas backend\` y \`Backend Release y pruebas\`; resultados positivos y negativos, cero FAIL en SHA aplicable.
- **A2:** seguridad multiempresa, gate de autorizacion/secretos y pruebas dirigidas A/B, revocacion, host y RBAC pertinentes; un mock no se interpreta como JWT HTTP real.
- **A3:** coherencia de contratos M02 y arquitectura Domain → Application, sin MySQL/EF/HTTP cambiado. MySQL efimero, Playwright, Angular y proveedores externos = NO_APLICA para este diff Domain aislado.
- **A4:** verificacion Scope Lock y procedencia V5, checks obligatorios completed/success en exact SHA o descendiente probado equivalente, BITACORA A:Z, PLAN/COLA/DAG y blob readback. CI queued/in_progress/skipped/cancelled/failed NO son PASS.
- **A5:** revision independiente posterior de una de las otras cinco Tasks Javier distinta de la autora; no autoriza QA/main/PROD.

**Rollback:** git revert normal del commit tras HEAD/CAS, reejecutar checks sin force-push ni borrar BITACORA. Cualquier falta de evidencia mantiene VALIDANDO con owner exclusivo.
