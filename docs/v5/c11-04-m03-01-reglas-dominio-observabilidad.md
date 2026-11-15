# C11-04.M03.01 — Reglas de dominio para agregados privados de observabilidad

## Clasificación V15 y alcance
R2: autorización de lectura y aislamiento tenant; \`Solqaryn.Domain.AutomationV5.C1104ObservabilitySignalDomainRules\` proporciona solamente una validación pura y aislada. No integra HTTP, API, Identity, middleware, EF/MySQL, telemetría real, collector, exporter, servicio, alertas, SLO, cobros o datos productivos. El controlador futuro deberá derivar identidad, membresía, permisos y tenant del backend confiable: parámetros de este validador NO sustituyen JWT/RBAC real.

## A0 — Requisitos del plan
ID C11-04.M03.01, dependencias COLA/DAG confirmadas. M02.01–06 define correlación opaca, cardinalidad limitada, acceso privado A/B, ventanas reproducibles, denominadores y ausencia de datos sensibles. Este changeset agrega dos archivos .NET 10 (Domain y xUnit) más este contrato y CHANGELOG autocontenido.

## A1 — Casos xUnit ejecutables
Valida nombres de señal allowlisted, correlationId <=64 sin CRLF, tenant A/B fail-closed, membresía revocada separada de permiso de lectura, ventanas UTC/orden/duración, numerador/denominador, NO_MEDIDO con denominador cero y etiquetas de baja cardinalidad con valores enumerados. Incluye positivos, negativos, frontera, errores y entradas nulas. No afirmar resultados antes de CI.

## A2 — Seguridad
La API debe autenticar antes de usar estas reglas. \`IdentityTenant\` y \`ResourceTenant\` se presuponen procedentes de contexto backend confiable; un atacante no puede conferir permisos aportando strings. La validación rechaza el uso de tenant ID/usuario/PII como etiqueta, acceso tenant B, membresía revocada y permisos ausentes. Ninguna evaluación genera tokens, privilegios o evidencias institucionales.

## A3 y A4
A3 MySQL, EF Core, proveedor, red y navegador: NO_APLICA porque este changeset no toca persistencia ni interfaz. A3 funcional se cubre por xUnit reales cuando GitHub Actions complete. A4 exige \`SOLQARYN - Build y pruebas backend\`, \`Backend Release y pruebas\`, \`SOLQARYN - Seguridad multiempresa\`, autorizaciones pertinentes, Scope Lock y procedencia V5 completed/success en SOURCE_SHA o equivalencia probada. Mientras CI esté QUEUED/IN_PROGRESS se mantiene VALIDANDO sin PASS. A5 de otra Task Javier distinto del autor es obligatorio.

Revertir con \`git revert\` ordinario en dev tras verificar HEAD y ausencia de escritor. No hacer cambios de QA/main/PROD o datos reales.
