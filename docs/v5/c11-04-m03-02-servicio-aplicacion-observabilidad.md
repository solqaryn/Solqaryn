# SOLQARYN V17 — C11-04.M03.02 servicio de prevalidación Application para observabilidad privada

**Microtarea:** C11-04.M03.02. **Riesgo V15:** R2 (JWT/RBAC, tenant A/B y evidencias de observabilidad). **Scope:** sólo dev, PROJECT_SCOPE_LOCK=STRICT. **Autor:** PRIMARY DEV 06, Task 6af16bbad26881918c283abe06e1f32a.

## A0 — Requisitos, impacto y límites
La autoridad es el STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES, PLAN_MAESTRO fila 1707, predecesores C11-04.M02.01–06 y M03.01 con DAG VALIDADO/SI. Implementa una envolvente Application **real y compilable** sobre `C1104ObservabilitySignalDomainRules` ya existente. Dos archivos .NET 10 (servicio + xUnit) y este documento, más CHANGELOG, sin cambios estructurales ni configuración/DB/HTTP/Angular/MySQL/EF.

`IC1104ObservabilityEvidenceReader` es interfaz para un adaptador backend futuro, NO un proveedor activado. El lector suministrará origen SHA, evidencia JWT/RBAC/membresía vigente, tenant derivado del servidor, propiedad persistida, aggregate y fence de auditoría; ningún valor se considera probado en vivo hasta que exista integración autenticada. `Request` sólo selecciona señal y correlación; rechaza overrides de tenant, datos crudos y certificación SLO/exportación.

## A1 — Pruebas funcionales reproducibles
`backend/tests/Solqaryn.Tests/Application/C1104ObservabilityApplicationServiceTests.cs` contiene 12 pruebas xUnit ejecutables: flujo válido con adaptador aún requerido, entradas nulas/SHA inválido, overrides antes de I/O, tenant extranjero, membresía/RBAC revocada, JWT/auditoría, SHA/señal/correlación desalineados, señal y etiqueta prohibidas, denominador cero => NO_MEDIDO, ventana y conteos frontera, lector sin datos y cancelación. Exigir resultado CI .NET real en SHA publicado; texto solo no constituye PASS.

## A2 — Seguridad/aislamiento
La validación es fail-closed; exige evidencia de fuente confiable para tenant/owner, JWT vigente, membresía/RBAC relacional actual y auditoría. No concede autorización de lectura, no emite tokens, no exporta logs ni modifica métricas, y no certifica ningún SLO. Un `CorrelationId` no es una credencial. Los nombres de señal/etiquetas son allowlists cerradas del dominio: sin tenant, usuario, PII ni credenciales como dimensiones libres. Revisar revalidación y revocación también en futuro adaptador real.

## A3 — Integración y superficies no implementadas
Se comprueba composición Domain<-Application mediante xUnit .NET 10. MySQL 8.4, EF/migraciones, API HTTP, Angular, Playwright, collectors, exporters, alertas, costos y SLO medidos son **NO_APLICA** para el diff de esta microtarea, nunca PASS implícito. Los indicadores reales SLI/SLO y costos permanecen **NO_MEDIDO** por falta de numerador/denominador observados de infraestructura real.

## A4 — CI causal, readbacks y rollback
Requerir `SOLQARYN - Build y pruebas backend`, `Backend Release y pruebas`, `SOLQARYN - Seguridad multiempresa`, `Authorization, files, secrets and tenant gate`, `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` en `completed/success` exact-SHA o descendiente con blobs y dependencias probadas idénticas. CANCELLED/QUEUED/IN_PROGRESS/SKIPPED/FAILED nunca PASS requerido. Registrar PLAN/COLA/DAG, BITACORA de 26 columnas y candado V17 id 2000001707 con lecturas posteriores. Rollback: `git revert` normal coordinado, preservando BITACORA, sin force-push ni QA/main/PROD.

## A5 — Separación de deberes
Solo otra Task Javier, distinta de DEV06, revisa el diff, pruebas adversariales, correspondencia de SHA y evidencias antes de A5_PASS. No constituye autorización para desplegar ni conectar proveedores. No se activa telemetry runtime ni webhook.
