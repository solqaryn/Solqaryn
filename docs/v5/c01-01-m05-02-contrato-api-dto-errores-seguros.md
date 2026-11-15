# SOLQARYN V5 FINAL — C01-01.M05.02: contrato API/DTO y errores seguros de contexto tenant

**Implementación real:** pruebas xUnit .NET 10 sobre `TenantContextController` del repositorio `solqaryn/Solqaryn` en `dev`, bajo `PROJECT_SCOPE_LOCK=STRICT`. Autor PRIMARY DEV 03, `6adc2e4bd8a48191ac7d8641ed95a1a3`. PLAN_MAESTRO fila 171, COLA G/O=SI, DAG F=VALIDADO/G=SI y claim exclusivo con readback. Esta microtarea no edita ningún controlador ni registra endpoints nuevos.

## Casos unitarios del controlador real

Cinco casos xUnit nuevos (dos casos parametrizados + tres Fact), sin simular compilación o test hasta disponer de CI:

| Escenario | Resultado esperado |
| --- | --- |
| Obtener(0) / Obtener(-1) | BadRequest con `ApiResponse<object>` sin datos ni errores privados; **cero llamadas** a la capa de ámbito |
| Obtener(22) sin membresía válida | `ForbidResult` sin revelar si tenant B existe o no, una llamada a fuente de autoridad |
| Obtener(11) con scope servidor verificado | Ok, `ApiResponse<UsuarioTenantScopeActual>` con UsuarioId 7 / EmpresaId 11 / RolId 19 |
| ListarEmpresas con únicamente A | Respuesta tipada y lista exacta de una empresa; no infiere permisos a B |

`ScopeStub` suministra entradas deterministas únicamente para aislar contrato del controlador; el Test **NO** acredita que el backend resuelva realmente membresías activas, autorización JWT, respuesta HTTP via TestServer, ni aislamiento MySQL/EF end-to-end. El chequeo de pertenencia real está cubierto por reglas M05.01 y pruebas de integración posteriores.

## Evaluación A0–A5

A0: Skill/canon, identidad Javier, HEAD dev, Scope Lock, PLAN H/I, COLA G/O, DAG F/G, CAS owner y diff de tres artefactos (test, documento y CHANGELOG). A1: comprobar cada resultado positivo y negativo en GitHub-hosted xUnit con build .NET 10. A2: errores neutros, rechazo de empresa inválida antes del servicio y distinción entre principal y scope servidor; no se declara PASS de JWT real. A3: integración unitaria Controller + ApiResponse + IUsuarioScopeService stub, CI de pruebas backend; no TestServer/MySQL/Angular ni claims no acreditados. A4: commit fast-forward CAS exact SHA, `verify-solqaryn-project-scope`, `V5 - Sin encabezados vacios ni atribucion cruzada`, `SOLQARYN - Build y pruebas backend` y seguridad relevante completed/success (no skipped/cancelled/in_progress), readback PLAN/COLA/BITACORA. A5: Task Javier distinta de DEV03 debe realizar revisión técnica independiente antes de promover otros entornos.

Se conserva el contrato existente de API; no se introducen permisos, JWT, headers, rutas, excepciones, cambios EF, credenciales, deployment, producer de eventos, dispatcher o webhook. Rollback de test/doc mediante `git revert` normal sin sobrescribir otros autores; BITACORA inmutable salvo eventos append-only.
