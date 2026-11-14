# SOLQARYN V5 — C11-02.M01.04: riesgos, supuestos y decisiones pendientes de API modular

**Unidad:** `C11-02.M01.04`, PLAN_MAESTRO fila 1625 (preparación documental M01, C11-02/API modular). **Autor:** PRIMARY DEV 01 / JAVIER_CHATGPT / TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Fuente institucional:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Ámbito:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT. **Fecha:** 2026-11-14.

## Alcance, método y restricciones

Registrar riesgos **POTENCIALES**, supuestos **NO_VERIFICADOS** y decisiones **PENDIENTES** respecto de contratos HTTP, DTOs, RBAC, tenant A/B, separación de plataforma, persistencia, compatibilidad y errores. Las microtareas C11-02.M01.01–03 anteriores documentaron alcance, actores I/O/F y 16 blobs originales, pero NO certificaron toda la API ni demostraron exploits. El DAG F=VALIDADO/G=SI y COLA G/O=SI de esta M01.04 autorizan preparación documental, **no cambios JWT/RBAC, endpoints, persistencia, SQL, workers, migraciones, QA/main/PROD, secretos o infraestructura**.

Cada riesgo requiere una comprobación positiva/negativa futura dirigida; registrar incertidumbre no equivale a crear bloqueo de seguridad ya confirmado. El plan exige puertos, casos de uso, DTOs y validaciones correctas antes de cualquier migración de controladores; el presente análisis **no implementa ni autoriza** esa evolución. No se ejecuta MySQL, API HTTP, .NET Tests, Angular, Playwright ni tareas programadas adicionales.

## R01–R16: riesgos potenciales, NO hallazgos certificados

| ID | Riesgo por analizar | Criterio de bloqueo para cambio futuro |
| --- | --- | --- |
| R01 | Un endpoint confía en `EmpresaId` del cliente en vez de membresía verificada | Denegar tenant A→B, probar IDOR y membresía revocada |
| R02 | La autoridad de plataforma se reutiliza como rol de empresa | Exigir separación expresa de ámbito |
| R03 | Soporte read-only alcanza rutas mutables por permisos cruzados | Probar POST/PUT/PATCH/DELETE y EF/raw SQL en soporte |
| R04 | JWT válido pero usuario o rol revocado continúa obteniendo recursos | Consultar estado efectivo y probar revocación |
| R05 | Refactor rompe códigos 400/401/403/404/409 o forma `ApiResponse` | Contratos HTTP y consumidores versionados |
| R06 | DTO permite mass assignment o campos de privilegios | Allowlist explícita, validación backend y negativos |
| R07 | Errores serializan SQL, stack, tokens o PII | Respuesta segura, logger con scrubbing y correlación |
| R08 | Login sin límite por actor/endpoint o con respuesta diferenciada | Pruebas antiabuso y no enumeración de usuarios |
| R09 | Consultas no acotan filtros/orden/paginación | Límites y allowlist de query validada |
| R10 | Operación comercial parcial pierde consistencia | Pruebas transaccionales e idempotencia |
| R11 | Migración EF modifica FK/unicidad de forma destructiva | MySQL efímero, respaldos, compatibilidad, rollback |
| R12 | Frontend guard usado como única autorización | Seguridad server-side en cada ruta protegida |
| R13 | Storefront/host público accede a DTOs privados | Separar DTO públicos y validar tenant binding |
| R14 | Ausencia de auditoría en decisión sensible | Registro seguro, sin secretos, con correlación |
| R15 | Middleware/DI de soporte cambia de efecto según ruta | Inspección de orden y pruebas de control cruzado |
| R16 | Check queued/cancelled/skipped se presenta PASS | Requerir SUCCESS exact-SHA de pruebas pertinentes |

**Estado de R01–R16:** POTENCIAL / NO_VERIFICADO. Ninguno se declara vulnerabilidad explotable; no se modifican gates de terceros ni se abren tareas fuera del plan.

## S01–S10: supuestos a falsar, actualmente NO_VERIFICADOS

| ID | Supuesto de trabajo (no hecho certificado) | Evidencia requerida |
| --- | --- | --- |
| S01 | Los controladores existentes conservan compatibilidad de shape y status | Snapshot de rutas/DTO y pruebas |
| S02 | Todas las rutas empresariales validan scope backend | Auditoría de cada endpoint + pruebas A/B |
| S03 | Revocación de membresía se observa antes de acceso sensible | Prueba bajo cambios concurrentes |
| S04 | El soporte solo lectura no tiene camino lateral de escritura | Pruebas interceptor, EF tracking y raw SQL |
| S05 | Las excepciones jamás exponen secretos/PII | Negativos de middleware/logs |
| S06 | Login mantiene protección contra abuso en despliegue | Config real + prueba de límites |
| S07 | Los DTOs públicos no reutilizan campos de DTO internos | Inventario serialización y consumidor |
| S08 | Los consumidores Angular toleran los contratos sin regresión | Build/Playwright/HTTP causal |
| S09 | MySQL efímero reproduce el proveedor EF declarado | Prueba con MySql.EntityFrameworkCore 10.0.9 |
| S10 | Revertir un refactor deja datos y contratos coherentes | Rollback probado y matriz de compatibilidad |

## D01–D10: decisiones de diseño PENDIENTES, no aprobadas

| ID | Decisión futura | Criterio previo |
| --- | --- | --- |
| D01 | Granularidad de puertos por caso de uso | Arquitectura vigente Domain/Application/Infrastructure |
| D02 | Estrategia de versionado de DTO/rutas | Mapa de consumidores y compatibilidad |
| D03 | Standard de errores (mantener ApiResponse/ProblemDetails adaptado) | No romper clientes ni exponer PII |
| D04 | Fuente de contexto empresarial en la nueva API | Servidor verifica membresía, no input cliente |
| D05 | Política de revocación en endpoints críticos | Evidencia de coherencia y amenaza |
| D06 | Contratos de soporte sólo lectura | Auditoría de DI, SQL y endpoints |
| D07 | Paging/filter/sort y límites de tamaño | Métricas/casos reales |
| D08 | Idempotencia y transacción por acción con efectos laterales | Rollback y consistencia de dominio |
| D09 | Estrategia de pruebas HTTP/EF/MySQL/A-B/Playwright | CI causal exact-SHA |
| D10 | Rollout y reversión versionada | Reglas operativas, A5 independiente y permiso nuevo |

## Evidencia concreta, alcance temporal

- `AuthController.cs` blob `af437c869b4800a499c42aeff00347de3a3cc521`: login/renovación con endpoints y tratamiento de errores explícitos; no acredita cobertura de todos los controladores.
- `TenantContextController.cs` blob `2407c2eb6779ed724e841c688ee14dabbf163171`: ID de ruta no autoritativo, membresía consultada en servidor.
- `PlataformaSesionController.cs` blob `965a05321a8b8c3f90011addb84e14bcc5978775`: autoridad global separada, no-store y auditoría.
- `IUsuarioScopeService.cs` blob `5345ee2de1b1e942bff8eded658f5dc27c0a6745`: contexto legacy distinto del scope empresarial confiable.
- `ApiResponse.cs` blob `aba995eae50ad756712961c9c394f5038a31a142`: formato de respuesta actual; no cambiarlo sin plan de consumidores.
- `ExceptionHandlingMiddleware.cs` blob `baee40143e4e48b8df200e77892606af96d60b7f` y `FluentValidationActionFilter.cs` blob `fec754eeb2c7e41599155f25a24e8adbb53298bb`: superficies candidatas a pruebas de validación/error, no cobertura demostrada.
- `SupportReadOnlyCommandInterceptor.cs` blob `3b80b59899522b9b41681ed75b8269f8720fe297`: barrera descrita en código, no certificación de todos los accesos.
- `Solqaryn.API.csproj` blob `2a7b33a0974d1ebe13676593d84b458b1243e037`: net10.0; `Solqaryn.Infrastructure.csproj` blob `6210d8b12abd77171d95d17e2292c1a319b89263`: EF 10.0.12/MySQL 10.0.9.

No se leyó una base productiva ni se alteraron servicios y configuración. La lista de fuentes es muestra de la M01.03, no inventario exhaustivo. Debe revalidarse frente a HEAD al materializar trabajo de código.

## Pruebas P01–P08 y N01–N12 — FUTUROS_NO_EJECUTADOS

**Positivas:** P01 login vigente y límite; P02 renovación autorizada; P03 listar empresas del usuario correcto; P04 lectura tenant A verificada; P05 acceso global plataforma independiente; P06 respuesta DTO/status compatible; P07 lectura soporte auditada y sin cambios; P08 transacción correcta bajo falla simulada.

**Negativas:** N01 token inválido/expirado; N02 tenant A solicita ID B; N03 membre­sía revocada; N04 rol plataforma se usa como empresarial; N05 soporte intenta escribir; N06 request manipula EmpresaId/rol desde DTO; N07 carga malformada/masiva; N08 error contiene PII/SQL/token; N09 no aplica rate limit; N10 storefront expone DTO privado; N11 EF/MySQL consulta sin filtro tenant; N12 transacción falla dejando estado parcial.

Todos los P/N son contratos para ejecución posterior con CI dirigida; ninguno es PASS de runtime del presente documento.

## Gates A0–A5, rollback

**A0:** fuente V5, Skill canónica, HEAD vivo, PLAN/COLA/DAG y exclusión de archivos. **A1:** documento distingue 16 R potenciales, 10 S no verificados y 10 D sin decidir. **A2:** revisar tenant A/B, JWT/RBAC, PII, soporte y fronteras, sin modificar permisos. **A3:** correspondencia documental de API→Application→Infrastructure y fuentes Git; compilación/HTTP/DB no se requieren causalmente para Markdown. **A4:** publicación fast-forward CAS sobre dev, diff únicamente este documento + CHANGELOG y blobs exactos; exigir Scope Lock y check V5 de CHANGELOG `completed/success` exact-SHA, readback de PLAN/COLA/BITACORA. Suites runtime ajenas se clasifican NO_APLICA, jamás PASS. **A5:** independiente por otra Task Javier distinta, antes de cualquier QA/main/PROD.

**Rollback:** `git revert` normal de commit documental, preservando otros commits y BITACORA histórica. Sin force-push, rebase destructivo ni cambios API/EF/DB/servidores, workers, eventos, webhooks, cuentas o tareas programadas.
