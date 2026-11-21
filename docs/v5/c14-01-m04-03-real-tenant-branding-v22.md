# C14-01.M04.03 — V22 proveedor real de identidad tenant

## Defecto verificado
El shell autenticado invocaba `/empresa-configuracion/publica`, que debe responder 503 P0 sin tenant inequívoco. La suite previa simulaba la marca pública, por lo que la prueba visual de tenant A/B no acreditaba lectura real de configuración persistida.

## Reparación en dev
- Tras `TenantContextService.contextoVerificado()`, `AppComponent` consulta `EmpresaConfiguracionService.getTenant(empresaId)` con JWT del usuario; `GET /empresa-configuracion/tenant/{empresaId}` ya aplica `EnsureTenantAccessAsync` y consulta MySQL con la membresía activa. No se confía en `localStorage` ni en hints de host como identidad.
- `EmpresaIdentidadService.cargarTenant` exige ID positivo seguro y respuesta coincidente, conserva únicamente nombre/logo, sanea URL, neutraliza la marca anterior durante el cambio y descarta respuestas asíncronas antiguas. Un HTTP 403/error/ID cruzado falla cerrado a SOLQARYN; no se muestra marca B bajo sesión A.
- Suite Angular adversarial: tenant autorizado, no lecturas duplicadas, ID ajeno y logo inseguro, respuesta A tardía y error HTTP. E2E ejecuta JWT firmado/ASP.NET Core 10 y MySQL 8.4 efímero, deja de simular la marca A/B y verifica proveedor `tenant/1` contra `tenant/2` 403/404; logos de prueba se almacenan en `Empresas` efímeras. Prueba negativa de proveedor público 503 intacta.

## Calidad y límites
R2 por multiempresa/JWT. A0 diff código/frontend, workflow y pruebas revisado; A1-A3 reales y A4 requieren GitHub-hosted CI **completada SUCCESS en SHA exacto**: V21 branding Chromium, unit Angular, tipado, build, multiempresa, Scope Lock y V5. No contar QUEUED/IN_PROGRESS/SKIPPED como PASS. No certificar todavía integración global de todos los módulos M04 ni QA/main/PROD. Ninguna API nueva, autorización ampliada, migración, tenant global o datos productivos; CI usa MySQL efímero. Rollback mediante git revert normal del changeset, preservando changelog y BITACORA histórica. `PLAN_MAESTRO!I` permanece autor DEV01, A5 por otra Task Javier independiente de autor/reparador.

## Reparacion CI P2 en test doble del shell
El SHA 0fbd436cc69f57540048702e636a3d7df21a57e4 ejecuto el nuevo metodo real del shell, pero los jobs GitHub-hosted 136708151680 y 136708151971 fallaron porque la fixture de `app.component.spec.ts` solo simulaba `cargar()` antiguo. Se anade `cargarTenant(id)` al stub y se afirma `id=7` solo en rutas empresariales; la ruta de plataforma sigue borrando branding. Es una reparacion de regression/test-doubles, no un relajamiento de asserts, autorizaciones ni CI. El FAIL previo queda historico y el nuevo SHA debe revalidarse en CI.
