# C14-04.M05.01 — Regresiones unitarias de reglas de readiness DEV

**Alcance:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT. Riesgo R2 preventivo por checks, tenant y revisión de liberación. No se realiza ninguna acción productiva R3.

## Incremento verificable
Nueva suite xUnit real de `V5ReleaseReadinessDomainRules` sin editar la regla de negocio ni instalar endpoints. Verifica paquete de evidencia sintética correcto que **solo** devuelve `READY_FOR_EXTERNAL_EVIDENCE_REVIEW`, frontera de origen confiable 512/513, restricción a entorno dev, separación autor/revisor, aislamiento A/B, SHA y manifiesto exactos, CI queued/in_progress/cancelled/skipped/failure, checks duplicados o ausentes, casos de aceptación no ejecutados/duplicados/con evidencia inválida y exigencia de rollback/stop condition.

## Gates
- A0: xUnit aditivo, este documento y CHANGELOG autocontenido; Domain/Application/API/Infra sin alteraciones.
- A1: tests positivos, negativos, frontera y errores compilados y ejecutados mediante GitHub Actions .NET 10 en SHA publicado.
- A2: aserciones adversariales tenant A/B, rol/revisor y estado de CI; no atribuir verificación real de JWT, grants o entornos.
- A3: ejecución real in-process xUnit .NET10. MySQL 8.4/EF, HTTP, Playwright, DNS, R2, proveedores y PROD NO_APLICA por diff sin adaptador ni interacción.
- A4: Scope Lock STRICT, procedencia V5, build/test Release y seguridad requeridos deben finalizar SUCCESS exact-SHA. Mientras la CI esté QUEUED/IN_PROGRESS, mantener VALIDANDO.
- A5: exclusivamente otra Task Javier distinta del autor, con revisión independiente del código y pruebas. No autoaprobación ni liberación de QA/main/PROD.

**Rollback:** `git revert` normal después de verificar HEAD, sin borrar historia ni fórmulas. Este paquete no demuestra smoke de producción, monitoreo real, alertas, DNS, backups ni aceptación comercial.
