# SOLQARYN — C02-01.M05.05, negativos R2 sobre tenant y login inteligente

Autor: DEV01 Javier, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. Repositorio único `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. Autoridad: STANDARD V5 FINAL y PLAN_MAESTRO!D318. El objetivo de PLAN!E318 es acumulativo de M05 y no equivale a una certificación de la fase completa.

## A0 / impacto y límites
Se agrega exclusivamente prueba adversarial .NET 10 xUnit y workflow para MySQL 8.4 efímero GitHub-hosted. No cambia controladores, usuarios de producción, migraciones, DI productiva, Angular, rutas HTTP reales desplegadas ni secretos. La prueba acopla JWT Bearer HMAC firmado y middleware real ASP.NET Core TestServer, TenantContextController, UsuarioScopeService real, Oracle EF Core y UsuarioEmpresa/Rol/Empresa efectivos en MySQL 8.4 temporal. No es un mock de SQL. Rollback seguro: revertir el changeset Git con un nuevo commit en dev, sin force-push.

## A1/A2/A3: regresiones que deben fallar ante defectos
- 401 si anónimo o firma JWT falsificada; un claim `rolId=999999` no concede rol.
- Host `X-Forwarded-Host` y header `X-Empresa-Id` de empresa B no cambian la autorización de empresa A, cuya identidad/rol se vuelven a leer de base.
- JWT firmado de otro usuario sin membresía no obtiene el scope de A.
- Membresía de empresa B revocada físicamente se deniega HTTP 403 y desaparece del selector.
- Un rol relacional B asignado indebidamente a la membresía A deniega A.
- Preferencia de otro actor se rechaza en la política existente `V17IntelligentTenantLoginAuthorization` con instantánea sintética: **NO certifica almacenamiento SQL live de preferencias**, que sigue sin cablear. No se prueba Chromium ni hardware y no se proclama validación UI.
- Workflow exige contadores TRX reales `total=1,passed=1,failed=0,notExecuted=0`, versión MySQL 8.4 física, Scope Lock y salida fail-closed.

## A4 / condiciones de cierre
Comprobar SHA causal exacto, job dirigido real COMPLETED/SUCCESS, Backend Release, multiempresa seguridad, Scope Lock y V5 procedencia pertinentes; falta de prueba/CI obliga mantener VALIDANDO. La suite actual no implementa ni certifica persistencia per-user de la última empresa, reemisión de JWT, conmutación automática efectiva en UI, ni todos los módulos de la fase. A5 independiente requiere Task Javier distinta de DEV01. No tocar QA/main/PROD. El archivo CHANGELOG_AI.md permanece **intacto**: el conector fetch_file devuelve contenido vacío por >1 MB; no reemplazarlo, ni inventar su actualización, y dejar su evidencia de procedencia explícitamente pendiente si el gate exige una entrada nueva.