# SOLQARYN V22 — C04-01.M03.01: reglas de dominio para catálogo de planes SaaS

**Microtarea:** C04-01.M03.01, PLAN_MAESTRO!D590 = "Implementar reglas de dominio y validaciones server side". **Autor:** PRIMARY DEV 04, Task \`6aeacb7820ac8191b7d768a6f33dfa09\`. **Repo/branch:** solqaryn/Solqaryn@dev. **Scope:** PROJECT_SCOPE_LOCK=STRICT. **Riesgo:** R2 por autorización SaaS/tenancy/cuotas, aunque el diff es deliberadamente Domain puro.

## A0. Impacto y fronteras

El código real nuevo \`backend/src/Domain/AutomationV5/C0401PlanDomainRules.cs\` recibe **solo snapshots declarados verificados en servidor**. No reescribe \`Plan\`, \`Suscripcion\`, \`PlanLimite\`, \`PoliticaModulosSaaS\`, \`RolPermiso\`, Application/API, el esquema EF ni la configuración de MySQL. Consumidores futuros de Application deben resolver actor/tenant/rol/grants con JWT firmado y DB, comprobar propiedad, volver a leer versiones y consumir cuotas en una transacción. Ninguna decisión de Domain otorga un permiso, derecho de escritura, cuota reservada, cobro, actualización ni certificación de HTTP/JWT/MySQL.

**Fuentes:** STANDARD V5 FINAL institucional, PLAN_MAESTRO D590 y E590 (E es meta acumulativa, no certificación completa de M03), COLA fila590, DAG_TECNICO fila551 (M02.01–06 y C01-02.M06.06 con A5 independientes), skill canónica, docs/PROJECT_SCOPE_LOCK.md, docs/QUALITY_RISK_AUDIT_V15.md, \`SuscripcionesSaaS.cs\`, \`FeatureFlagsSaaS.cs\`, \`C0102CapabilityDomainRules.cs\` y contrato documental M02.06.

## A1/A2. Pruebas adversariales y autoridad

\`backend/tests/Solqaryn.Tests/Domain/C0401M0301PlanDomainRulesTests.cs\` aporta xUnit para la precondición positiva sin autorizar el uso, evidencia nula, tenant A/B, RBAC/rol/membresía y revocación, sucursal, plan activo/código canónico, versión/revisión obsoleta, expiración UTC, prohibición de cambios de tarifa/impuesto/prorrateo/cobro, intersección explícita de cuatro fuentes de capacidad, \`null\` ilimitado **solo explícito**, cuota insuficiente, límites y overflow long.

La precondición exitosa devuelve \`PRECONDICIONES_DOMINIO_SOLAMENTE_GATE_BACKEND_REQUERIDO\`, con propiedades de autoridad todas \`false\`: no es una autorización ni PASS HTTP. El rechazo por caducidad y revocación es fail-closed. Los tests deben ejecutarse por **GitHub-hosted .NET10 xUnit real**, no darse por PASS por mera existencia. Logs o jobs \`FAILED/SKIPPED/CANCELLED/IN_PROGRESS\` requeridos no equivalen a PASS.

## A3/A4. Integración y límites de microtarea

**A3 MySQL/HTTP/JWT/Chromium NO_APLICA a este diff específico** porque M03.01 no modifica API/HTTP, auth runtime, EF, migración, proveedor ni UI, y PLAN D exige **reglas de Domain**, no integraciones. Es una LIMITACIÓN DE FASE: M03.02–06/M05 deberán acreditar integración real según su D específico; este resultado NO garantiza que el enforcement de cuotas funcione por HTTP o MySQL, y nunca se extrapola a precios comerciales. Si se detecta cambio runtime o una exigencia propia de D, dejar VALIDANDO hasta probarlo.

**A4 pendiente hasta pruebas observadas:** certificar solo si \`verify-solqaryn-project-scope\`, \`V5 - Sin encabezados vacios ni atribucion cruzada\`, build Release/xUnit de reglas y checks causalmente aplicables terminan \`completed/success\` en el SHA de publicación; comparar diff, blobs, PLAN/COLA/BITACORA y el candado V17. El estado inicial EN_PROGRESO precede publicación, y VALIDANDO precede la auditoría A1–A3; no convertir a LISTO por este documento.

## A5, rollback y R3

A5 independiente requiere revisión genuina de otra Task Javier, no autor DEV04, incluso tras LISTO. Permanecen NO_GO: precios, impuestos/tarifas reales, upgrade/downgrade monetario, PSP, transacciones contra Aiven, dominios, secretos, QA/main/PROD, cambios de reglas de rama o claims simultáneos. Rollback exclusivamente \`git revert\` normal del commit en dev, más transición institucional auditada; nunca force-push ni borrar BITACORA.
