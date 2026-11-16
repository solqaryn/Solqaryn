# SOLQARYN V17 — C15-03.M04.01: superficie privada de auditoría de aislamiento

**Autor:** PRIMARY DEV 03 JAVIER / `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Riesgo R2:** frontera de autorización de UI privada. **Fuentes:** V5 FINAL STANDARD, PLAN_MAESTRO fila 2252, COLA fila 2252, DAG_TECNICO fila 2213; `PROJECT_SCOPE_LOCK=STRICT`. **Candado V17:** `SQV17_C15_03_M04_01` / 2000002252.

## A0 — resultado verificable
Nueva ruta lazy `/plataforma/auditoria-tenancy-v5` protegida por `plataformaGuard`, sin alterar otra ruta. Componente Angular 22.2.1 read-only: presenta cuatro frentes de revisión (tenant/sucursal, propiedad/membresía, archivos/jobs, control plane/secretos) **sólo cuando la señal `PlataformaSesionService.sesion()` confirma plataforma administradora**. Cambios externos de sesión revocan inmediatamente la vista. Ninguna operación, formulario, exportación, pentest, archivo, backup, restore, API, base de datos o proveedor se invoca. No se atribuyen mediciones ni auditorías A/B reales. Los riesgos preexistentes de `TenantQueryBypassGuard` continúan independientes y NO se certifican como resueltos por una pantalla.

## A1 — pruebas positivas, negativas, frontera
Vitest con cinco casos: ruta exactamente una vez con guard + lazy load; acceso anónimo denegado; acceso sintético válido muestra cuatro frentes sin botones ni formularios; objeto con `ambito=Empresa` denegado; revocación de señal elimina el contenido. Playwright Chromium con tres casos: ruta anónima a login, 403 sintético con token ficticio a login-denegado, acceso simulado responsive que sólo muestra guía y no llama rutas operativas. Todas las pruebas son ejecutables sobre código REAL en GitHub-hosted; no se adelanta PASS antes de logs.

## A2/A3 — alcance de seguridad
El guard revalida `/plataforma/sesion` contra backend; la vista nunca infiere grant de token ni `EmpresaId`. Mocks de navegador **no acreditan JWT, RBAC, tenant A/B, autorización HTTP o base de datos real**. A2 exige denegación y revocación visible en UI. A3 requiere build/lint Node/Angular, Vitest y Chromium real con sesión simulada. MySQL 8.4/EF, pruebas de integración y proveedor externo: `NO_APLICA` a este diff de sólo frontend; pentest real y auditoría API permanecen `NO_MEDIDO`.

## A4/A5 — gates y reversión
Un commit individual fast-forward con SHA esperado, ruta, componente, pruebas, workflow, Markdown y CHANGELOG autocontenido. Exigir Scope Lock y procedencia V5 exact SHA, CI UI dirigida `v17-c15-03-m04-01-auditoria-tenancy.yml` completed/success con logs 0 FAIL, readbacks institucionales. Mientras queued/in_progress: `VALIDANDO`, owner sin mutación y RUN/check real. LISTO sólo tras A0–A4. A5 exige revisor Javier Task ID distinto del autor. Sin QA/main/PROD, secretos, cambios de permisos, compras ni datos reales. Rollback por git revert normal, nunca force-push.
