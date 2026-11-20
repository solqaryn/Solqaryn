# C02-01.M05.03 — integración causal de persistencia tenant A/B

2026-11-20 · SOLQARYN `dev` · PROJECT_SCOPE_LOCK=STRICT · autora Javier DEV06 (6af16bbad26881918c283abe06e1f32a).

**A0 / entrega del ID:** PLAN_MAESTRO!D316 pide ejecutar integración real de persistencia/proveedor. No implementa endpoints, decisiones de host, JWT, roles, migraciones o cambios productivos. PLAN E es meta acumulada de M05. Archivos: xUnit Integration + workflow GitHub-hosted + este documento + CHANGELOG, sin cambios a runtime. Riesgo R2 por comprobación tenant/MySQL.

**A1 / aserciones:** contra `UsuarioScopeService` verdadero (no sustituto de consulta SQL), comprobar dos membresías A/B con selección única denegada, selector filtrado, acceso individual A/B, ID inválido, revocación B persistida, rol cruzado B aplicado a A denegado, usuario bloqueado y anónimo rechazado. Aserciones operan después de `ChangeTracker.Clear()`, en base efímera distinta por GUID. Un fallo o prueba cero detiene el job; no se acepta SUCCESS de build aislado.

**A2 / seguridad:** identidad del actor simulado por `ICurrentUserService`; no se certifica JWT firmado, autorización HTTP o proxy/host, ni control plane completo. Las consultas SQL contra roles y membresías reales del repositorio son evidencia R2 parcial. No consultar ni alterar Aiven ni datos comerciales reales.

**A3 / integración REAL:** `mysql:8.4` efímero en GitHub-hosted, EF Core 10 + proveedor Oracle `MySql.EntityFrameworkCore`, `AppDbContext` con esquema inicializado localmente, sin servidor externo. Confirmar `SELECT VERSION()` 8.4.*, filtro xUnit `Category=Integration` y TRX exactamente 1 total/1 pass/0 fail/0 omitidas, más logs. `EnsureDeletedAsync` sólo sobre base aleatoria `v22_c0201_m0503_*`. Sin certificación de backend HTTP/JWT o integración universal de fase.

**A4 / estado:** commit FF/CAS en `dev` y readback del HEAD, diff/blobs + `verify-solqaryn-project-scope` y V5, build .NET y job dirigido exact-SHA `completed/success`. Mientras exista check requerido `queued/in_progress/failure/cancelled/skipped`, mantener PLAN H316=VALIDANDO, I316=DEV06 y registrar SHA/RUN y siguiente acción. Sin atribuir PASS por CI no finalizada. **A5:** sólo otra Task Javier distinta de DEV06 después de revisión independiente. Rollback técnico `git revert` sin force-push; conservar BITACORA histórica. No QA/main/PROD, ni cambios financieros.
