# SOLQARYN V5 — C12-01.M01.03: inventario causal de contratos, código y evidencia de topología

**Unidad institucional:** `C12-01.M01.03`, PLAN_MAESTRO fila 1732; C12-01 Topología objetivo, M01 preparación. **Autor:** PRIMARY DEV 06 JAVIER (`6af16bbad26881918c283abe06e1f32a`). **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`) y Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. **Base de inspección:** `solqaryn/Solqaryn@dev` `429d75bba804fa0694d5075a00d18dcaccb287eb`, 2026-11-14. **PROJECT_SCOPE_LOCK=STRICT.**

## Dictamen y alcance

Este inventario distingue **declaraciones versionadas** del código existente de **operaciones reales del proveedor**, que no pueden certificarse mediante archivos Git. La topología vigente declarada comprende Angular/Vercel, API ASP.NET Core 10/Render, MySQL/Aiven y medios Cloudinary. Hostinger/Cloudflare, workers separados, red privada, volúmenes, backups/restores y cutover son **objetivos futuros NO_IMPLEMENTADOS/NO_VERIFICADOS** por esta microtarea. No se consultaron cuentas, IPs, servicios live, bases reales ni secretos. No se creó infraestructura ni se modificaron endpoints o autorizaciones.

Se leen los contratos de M01.01 (alcance/no regresión) y M01.02 (actores/superficies/fronteras) para completar M01.03, sin convertir los criterios futuros en test PASS. Fuentes evaluadas: **14 rutas Git**, más el Doc y Sheet institucionales, con blob SHA del corte. El hash vincula contenido; no acredita despliegue ni salud del proveedor.

## Inventario de fuentes versionadas y límites de inferencia

| Nº | Archivo / evidencia | Blob SHA Git del corte | Hecho comprobable y alcance limitado |
| --- | --- | --- | --- |
| F01 | `render.yaml` | `f64d291425e2bbfdd9b6466d268753f8f8d86b3d` | Declara servicios Render por ambientes, rama `dev` para API dev, `healthCheckPath: /health`, imagen Docker y variables gestionadas; no prueba despliegue actual, conectividad ni recursos activos. |
| F02 | `frontend/vercel.json` | `5f12bcf667a21afcfc4314b66787ff66627ae666` | Define rewrites SEO, `/api/:path*` al backend-proxy, SPA fallback y headers; no certifica DNS, certificados ni reglas activas de Cloudflare. |
| F03 | `Dockerfile` | `5bcfd9722c5b88fb7b081a7f23c2b70eb7b56327` | Imagen multi-stage SDK 10.0.401 / runtime ASP.NET 10.0.12, `EXPOSE 10000`, `USER $APP_UID`; no prueba aislamiento real en VPS. |
| F04 | `backend/Dockerfile` | `5bcfd9722c5b88fb7b081a7f23c2b70eb7b56327` | Igual blob verificado; `render.yaml` usa `./backend/Dockerfile`. La duplicación actual exige conservación o consolidación autorizada posterior. |
| F05 | `backend/src/API/Configuration/RenderEnvironmentContractGuard.cs` | `2c38ebb90a8360b61c9e05df96ff3a9abd5b8f65` | Exige exactamente 28 claves gestionadas y entornos esperados de Render. Portar sin adaptar el contrato fallaría; aquí no se cambia ningún guard. |
| F06 | `backend/src/API/Configuration/EnvironmentDatabaseGuard.cs` | `896aa003876f2b4299dfe5d4f1d69b83b5d1be22` | Valida host/puerto canónicos Aiven, base/usuario por entorno y `SslMode=Required`; un traslado MySQL requiere diseño de reemplazo y pruebas explícitas, no desactivar guard. |
| F07 | `PROJECT_CONTEXT.md` | `48064b93299e973c33145662897d772712d2fa29` | Resume stack y estado V5, útil como índice de contexto; no sustituye inspección del código o proveedor. |
| F08 | `PROJECT_INDEX.md` | `98c2d82de74ff56ba92474572d39d6afb5bcd89c` | Índice institucional y alcance de ramas; sus SHA históricos son cortes, no HEAD actual. |
| F09 | `docs/PROJECT_SCOPE_LOCK.md` | `7dabef6946d75e0381f121a70458240ac702a30f` | Prohíbe cambios en QA/main/PROD, cuentas, redes y secretos por inferencia; autoriza solo dev gobernado. |
| F10 | `.agents/skills/solqaryn-project-governance/references/architecture.md` | `1931e1229752028229b6ad1bb3368038e2c6769b` | Capas Domain → Application → Infrastructure, API; JWT/RBAC/tenant, MySQL EF y fronteras. |
| F11 | `frontend/package.json` | `e6d030f643421cb343cc12617e742469e88223c4` | Manifest Angular y scripts de verificación; no demuestra un build de imagen futuro. |
| F12 | `backend/src/API/Solqaryn.API.csproj` | `2a7b33a0974d1ebe13676593d84b458b1243e037` | `TargetFramework=net10.0`; no acredita compilación fuera de GitHub Actions. |
| F13 | `backend/src/Infrastructure/Solqaryn.Infrastructure.csproj` | `6210d8b12abd77171d95d17e2292c1a319b89263` | EF Core 10.0.12 y `MySql.EntityFrameworkCore` 10.0.9; una migración debe conservar compatibilidad real. |
| F14 | `docs/AUTOMATION_STATUS.md` | `4b25c0e88e8fb593cbafb5f8836db42c7d703fca` | Seis Tasks Javier permitidas y límites de automatización; no habilita dispatcher server-side, cuentas de proveedor ni ampliación de alcance. |

**Compatibilidad observada sin ejecución:** `render.yaml` declara `Database__ApplyMigrationsOnStartup` `true` para dev y `false` para QA. El simple cambio de host/imagen no debe arrastrar ese comportamiento a otra base o a producción. `EnvironmentDatabaseGuard` contiene binding estricto de Aiven; ninguna sustitución del host será segura por mero cambio de variable. `frontend/vercel.json` expresa routing real como contrato de repositorio, pero un proxy/gateway alterno debe preservar autorización del backend y cabeceras sin confiar en `tenant_id` de navegador.

## Brechas causales abiertas y evidencia requerida antes de migrar

| Brecha | Evidencia ausente en las fuentes revisadas | Criterio de cierre futuro, no ejecutado |
| --- | --- | --- |
| G01 | Inventario auditado de servicios Hostinger/Cloudflare realmente aprovisionados | Confirmar identidad, tenencia, cuota, región y autorización de cuenta sin contratar automáticamente |
| G02 | Diagrama de red privada validado por conectividad real | Probar rechazos desde origen no autorizado; servicios de datos sin exposición pública |
| G03 | Inventario y rotación de secretos de cada entorno | Validar origen seguro, mínimo privilegio, rollback; sin persistir valores en Git |
| G04 | Mapa DNS/TLS y plan de cutover de usuarios reales | Test de certificados, TTL, rutas y reversión con autorización separada |
| G05 | Evidencia de builds deterministas / SBOM de las imágenes objetivo | Build reproducible y sin credenciales en capas; mismo comportamiento .NET 10 |
| G06 | Endpoints reales de health/readiness y dependencia MySQL en topología objetivo | Smoke de health, readiness y fallo de DB; no solo `/health` declarado |
| G07 | Restauración MySQL aislada con conteos/checksums y esquema compatible | Restore en entorno efímero, aislamiento A/B, transacciones y prueba de rollback |
| G08 | Matriz IAM/roles de DB y segregación DEV/QA/PROD en proveedor objetivo | Tests de denegación cruzada empresa/ambiente y permisos mínimos |
| G09 | Contrato operativo de workers, deduplicación y DLQ realmente probado | Reintento finito/idempotencia, sin ejecutar eventos ni webhook ahora |
| G10 | Datos/medios reales inventariados para transición Cloudinary → R2, si se autoriza | No migrar imágenes inexistentes; hashes, referencias y reversibilidad |
| G11 | Costes, cuotas, monitorización y ventanas de fallo con fuente verificada | Propuesta aprobada, SLO/alertas/retención con responsables; sin inventar cifras |
| G12 | Pruebas E2E de routing/SPA/SEO, CORS, JWT/RBAC/tenant frente al nuevo proxy | Positive/negative Playwright/API, rechazo de suplantación e identidad backend |

Ninguna brecha se marca resuelta por existir un Dockerfile, un diagrama o un check de documentación. La revisión de fuentes permite identificar limitaciones; las pruebas de proveedor quedan pendientes de otras microtareas y autorización explícita.

## Controles y aceptación de esta microtarea

- **A0 (preflight documental):** autoridad Doc/Sheet V5, `PLAN H1732=PENDIENTE` sin owner al reclamar, `COLA G/O=SI`, `DAG F=VALIDADO/G=SI`, skill local y canónica coincidentes, Scope Lock STRICT, identidad Javier GitHub/Drive y 14 rutas Git.
- **A1 (positivo documental):** identificar 14 fuentes por ruta/blob, vincular 12 brechas a evidencias futuras verificables y diferenciar al menos 3 contratos runtime existentes. **Negativo:** no concluir que Hostinger/Cloudflare están desplegados por figurar en el plan.
- **A2 (seguridad):** preservar JWT/RBAC/tenancy, guard de Aiven, ausencia de secretos, segregación de ambientes y red privada sólo propuesta. **Negativo:** no aceptar suplantación de tenant ni migración de claves por copia.
- **A3 (integración documental):** cotejar `render.yaml`, `backend/Dockerfile`, Vercel rewrites, guards y manifests para exponer dependencias; no se ejecuta MySQL/Docker/proveedor. Las pruebas de futuro quedan **NO_EJECUTADAS**.
- **A4 (publicación):** exclusivamente este Markdown y `CHANGELOG_AI.md`, commit fast-forward CAS directo a dev, `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` en SHA publicado; readback Git + PLAN/COLA/BITACORA. Las builds no causales del código sin cambios se clasifican **NO_APLICA**, no PASS.
- **A5:** revisión independiente por otra de las cinco Tasks Javier, **PENDIENTE** tras A0–A4; no habilita QA/main/PROD o M01.06 por sí sola.
- **Rollback:** `git revert <SHA-publicado>` normal en dev mediante nuevo commit gobernado; no force-push, no reescribir BITACORA histórica.

**Dictamen:** inventario técnico estático preparado para M01, no certificado de infraestructura/seguridad productiva ni activación de topología objetivo.
