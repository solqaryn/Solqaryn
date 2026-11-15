# SOLQARYN V5 — C05-04.M03.04: adaptador aislado de evidencia de ligereza UI

**Ámbito:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Autor:** PRIMARY DEV 06 Javier, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Riesgo V15:** R2 por límite tenant/cache. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, `C05-04.M03.04`.

## Implementación y alcance real

Se añade `V5UiLightnessIsolatedSnapshotAdapter` en Infrastructure .NET 10 y nueve casos xUnit. El adaptador captura una vista inmutable de evidencia **ya suministrada por un llamador backend** y ejercita el servicio Application real y sus reglas Domain para prevalidación. Rechaza recurso inválido o distinto, SHA faltante/obsoleto, tenant A→B, snapshot cross-tenant, membresía revocada, revisión obsoleta, bundle fuera de presupuesto y cancelación. No concede acceso y no se registra como proveedor de confianza en DI.

El contrato M02.03 selecciona preferentemente evidencia de CI versionada y anónima, no una tabla nueva. **No crea MySQL/EF, migraciones, tablas, cache real, storage, escritura, API HTTP, Angular, navegador ni mediciones Core Web Vitals.** El adaptador aislado es un componente ejecutable de código, no prueba de existencia de un proveedor productivo. Su snapshot de prueba nunca acredita JWT real ni permisos actuales por sí solo.

## A0–A5, criterios de aceptación

- A0: una microtarea; diff Infrastructure + xUnit + documento + CHANGELOG, dependencia C05-04.M02.01–06 y M03.01–03 con puertas institucionales validadas. Sin cambios arquitectónicos, endpoints ni datos.
- A1: nueve xUnit genuinos capaces de fallar: caso control positivo no autorizante; negativos tenant A/B, ownership del recurso, SHA, snapshot revocado, versión, presupuesto, cancellation y frontera de constructor.
- A2: preflight fail-closed, tenant verificado por `ContextoTenantActual.ExigirEmpresa`; ningún dato de B se lee desde A. `GrantsAccess=false`, `AllowsPersistence=false`, `IsLiveProvider=false`; JWT HTTP real NO_APLICA a este componente aislado, no se proclama PASS real.
- A3: compilación .NET Release + ejecución real de xUnit Application/Domain/Infrastructure vía GitHub Actions. MySQL 8.4 y Playwright/Chromium NO_APLICA al diff sin EF/DB ni UI. Las mediciones CWV siguen pendientes para etapas que cambien interfaz.
- A4: requerir Scope Lock STRICT, procedencia de CHANGELOG, build/tests backend y controles de seguridad/autorización aplicables `completed/success` sobre SHA exacto o descendiente equivalido con blobs; verificar PLAN/COLA/BITACORA.
- A5: exclusivamente otra Task Javier distinta de DEV06 inspeccionará código, nueve tests, riesgos y evidencia CI; puede rechazar.

**Rollback:** `git revert` individual del commit, preservando HEAD concurrente. Sin QA/main/PROD, secretos, proveedores ni migraciones productivas. No interpretar este adaptador como autorización para optimizar cache multitenant ni activar un proveedor real.
