# SOLQARYN V5 FINAL — C01-01.M02.02: contratos API, DTO, errores y compatibilidad de identidad universal

**Unidad:** `C01-01.M02.02`; **autor:** PRIMARY DEV 04 — JAVIER, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. **Tipo:** diseño documental de contratos, `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Fuente ejecutable:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`) y PLAN_MAESTRO/COLA/DAG_TECNICO (Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`).

**Resultado exigido:** definir API, DTO, errores, validaciones y compatibilidad para la fase C01-01 (empresa, sucursal, usuario, membresía, rol y contexto). No agregar rutas, clases ejecutables, permisos o respuestas al sistema actual; esta M02.02 distingue los contratos observados de futuras mejoras de implementación en M03. Esta especificación por sí sola no certifica autenticación, aislamiento multiempresa HTTP/MySQL, fiabilidad del frontend ni JWT live.

## 1. Predecesores y dependencias exactas

El contrato M02.01 de entidades/ownership fue publicado como diseño con A0–A4 documentales en `cbb112f24e162ae914b6a56221c1cbc58bb3f8c0`, doc blob `6545f81125d0115b9bfdd0e24daee6d0edbe490a`, A5_PENDIENTE de otro Javier (no es aprobación de QA/PROD). Las seis C01-01.M01.01–06, C00-01.M06.06, C00-03.M01.06 y C00-03.M06.06 están LISTO, y el DAG individual de M02.02 incluye los diez requisitos. Un gate COLA G/O=SI no permite saltar el DAG o modificar otra microtarea. La fase M02.03 (persistencia) y M02.06 (gate de salida) requieren evaluación nueva; no se liberan con esta entrega.

## 2. Inventario de contratos que existen en código dev

| Archivo y blob verificado | Superficie real | Conclusión delimitada |
| --- | --- | --- |
| `backend/src/API/Controllers/TenantContextController.cs` — `2407c2eb6779ed724e841c688ee14dabbf163171` | `[Authorize]` y rutas GET `tenant-context/empresas`, GET `tenant-context/{empresaId:int}` | La selección requiere membresía server-side; catálogo lista solo empresas de membresías; entrada inválida 400, contexto ausente `Forbid()`. No demuestra cobertura de todos los controladores |
| `backend/src/Application/Interfaces/IUsuarioScopeService.cs` — `5345ee2de1b1e942bff8eded658f5dc27c0a6745` | `EmpresaSeleccionable`, `UsuarioTenantScopeActual`, `ObtenerActualAsync(empresaId)` | DTO de contexto incluye usuario, empresa, rol y nombre/atributo admin informativo; default sin implementación falla cerrado null/lista vacía |
| `backend/src/Application/Interfaces/IContextoTenantService.cs` — `97d0a05456c66d069a56b9743bbc286af07502b1` | `ResolverActualAsync(empresaIdSolicitada)` | Solicitud no es autoridad; sin membresía/usuario/rol inequívocos devolver null |
| `backend/src/Infrastructure/Services/UsuarioScopeService.cs` — `14f4acbc5e1dc6a3ed1cf3485d54c7f1121b0e6f` | Predicado EF de membresía, usuario, empresa y rol; listado filtrado | Revisa activos, bloqueo, ámbito Rol Empresa y tenant antes de devolver DTO; servicio legacy sin EmpresaId tiene frontera que debe revisarse en consumidores |
| `backend/src/API/Filters/RequiereMembresiaEmpresaAttribute.cs` — `17c7d191e3e1b34f56c3d27ba4992b512526542b` | Filtro de acción con `RequireEmpresaIdAsync`, scope `ObtenerActualAsync`, marca de autorización | Header selecciona tenant pero no concede acceso. Si no hay sesión/membresía, excepción de acceso denegado; no equivale a RBAC de acción |
| `backend/src/API/Filters/RequierePermisoAttribute.cs` — `b25a75b33d9cf809e2bbbf92b8e7f35463d3eea4` | Filtro de módulo+acción `IPermisoService.VerificarPermisoAsync`; soporte verificado GET/Ver limitado | Permiso granular y tenant server-side; soporte no implica permisos de escritura ni acceso ajeno |
| `backend/src/API/Middleware/ExceptionHandlingMiddleware.cs` — `baee40143e4e48b8df200e77892606af96d60b7f` | Mapeo de excepción a 400, 403, 404, 409, 503, 500; `ApiResponse`/ProblemDetails | Contrato actual presenta formatos diferentes por tipo de error; mensajes dinámicos y trazas requieren prueba de no fuga antes de afirmar seguridad integral |
| `backend/src/API/Controllers/AuthController.cs` — `af437c869b4800a499c42aeff00347de3a3cc521` | POST `auth/login` con `LoginRequestDto` y `ApiResponse<LoginResponseDto>`; rate limit; 401 credenciales erróneas | Login acredita identidad, no membresía o permiso de empresa; M02.02 no altera tokens, login ni refresh |
| `frontend/src/app/core/auth/tenant-context.service.ts` — `63a711355495d2f80efba35bffb2b8727401154a` | Angular señales `empresaSolicitadaId`, `contextoVerificado`, GET lista/contexto, versión de selección | localStorage conserva solo solicitud, no grant; contexto verificado solo después de respuesta del backend; protege contra respuesta tardía y seleccion duplicada |

**Alcance del inventario:** lectura de los blobs anteriores y contratos compilables ya presentes. No se afirma haber ejecutado una petición HTTP o una prueba Angular/.NET/E2E para esta entrega documental. Si se modifica posteriormente código, el contrato y sus casos se vuelven prueba causal real.

## 3. Tabla de rutas y DTO existentes (no nuevas rutas propuestas)

| Operación existente | Entrada | Positivo esperable | Negativo y frontera de autorización |
| --- | --- | --- | --- |
| `GET /tenant-context/empresas` | JWT autenticado, sin EmpresaId de cliente como grant | HTTP 200 `ApiResponse<IReadOnlyList<EmpresaSeleccionable>>` solo del usuario autenticado | Sesión ausente 401 (autorización middleware); empresas no vinculadas nunca en la lista; 0 membresías devuelve lista vacía, no catálogo global |
| `GET /tenant-context/{empresaId:int}` | JWT y entero positivo solicitado; empresa seleccionada no autoritativa | HTTP 200 `ApiResponse<UsuarioTenantScopeActual>` si membership+usuario+empresa+rol vigentes | Entero ≤0 produce 400; membresía inactiva/ajena o empresa inválida `Forbid()` → 403; JWT ausente 401; nunca revelar si B existe |
| `POST /auth/login` | `LoginRequestDto`, sujeto a rate limiting | 200 `ApiResponse<LoginResponseDto>` cuando el backend autentica | Credenciales erróneas 401 con mensaje genérico; no autorizar tenant por login solo |
| Rutas marcadas con `RequiereMembresiaEmpresa` | Selección tenant + sesión | Continúan solo si `ObtenerActualAsync(empresaId)` devuelve contexto coincidente | Sin membresía, empresa ajena o header manipulado → denegación |
| Rutas marcadas `RequierePermiso` | Tenant verificado, módulo y acción | Solo si `IPermisoService` autoriza; soporte verificado limitado a consulta GET | Rol/acción sin grant, tenant ajeno, soporte POST u otra acción → denegación |

**Reglas de DTO:** `EmpresaSeleccionable` expone solo `EmpresaId` y `Nombre` de empresa autorizada; `UsuarioTenantScopeActual` solo datos de contexto necesarios para UX, nunca PasswordHash, JWT, Rtn/PII sin necesidad, conexiones o tokens. `EsAdministrador` dentro de un contexto es dato informativo; el backend debe consultar autorización granular por acción. No ampliar la respuesta por id de empresa suministrado por el cliente. Los contratos C# y los objetos TypeScript se validan uno frente a otro en tarea de integración posterior.

## 4. Validaciones, compatibilidad y errores seguros

| Código/estado previsto | Supuesto y contrato | No-regresión requerida |
| --- | --- | --- |
| 200 | Contexto autorizado o lista filtrada; envelope tipado con `success/data` | Un usuario A no recibe datos de empresas B; al alternar A→B la UI invalida respuesta de A |
| 400 | EmpresaId no positivo o parámetros inválidos | Error estable sin existencia de empresa B ni mensaje de excepción arbitraria |
| 401 | Sesión ausente/credencial inválida; auth requerido | Sin fallback a rol legacy ni emision de contexto válido |
| 403 | Sesión autenticada sin membresía, permiso o tenant correcto | No revelar si recurso ajeno existe; no permitir cruces por `EmpresaId` |
| 404 | Recurso ausente en endpoints que correspondan | Evitar usar 404/403 como oráculo de pertenencia de otro tenant |
| 409 | Conflicto de unicidad o concurrencia | No repetir escritura automáticamente ni exponer SQL/identificadores de datos |
| 429 | Rate limit si se aplica en ruta de login/protección | Política real debe verificarse en pipeline, nunca omitirse por UX |
| 503/500 | Persistencia indisponible o excepción inesperada | Cuerpo genérico con correlación, sin detalles MySQL, ruta interna, hash o JWT |

**Compatibilidad:** `UsuarioScopeActual` legacy sin EmpresaId no es equivalente a tenant real; no cambiar firmas ni desactivar rutas hoy. Migrar consumidores solo tras inventario exacto, pruebas de llamadas de ámbito, denegación a B, versión DTO y rollback por endpoint. Angular localStorage y signals no sustituyen la validación server-side. No se proponen nuevas tablas, GUID o claims globales, porque M02.03 y M03 deben definir la implementación con evidencia real.

**Frontera de excepción:** el middleware actual incluye varios `ex.Message` en errores 403/404/409; la seguridad del contenido depende de qué excepciones produzcan los servicios. Registrar una prueba negativa futura de no fuga (token, PII, identificadores de B, SQL) en API; el hecho de que exista middleware no equivale a aprobarla.

## 5. Oráculos de aceptación positivos y negativos para pruebas posteriores

| Caso | Camino positivo de especificación | Negativo que debe fallar cerrado |
| --- | --- | --- |
| API01 | JWT U1 con membresía A activa lista A únicamente | U1 sin B intenta GET contexto B → 403, no datos B |
| API02 | Empresa A, rol empresarial RA y scope coincidente → DTO correcto | EmpresaId ≤0 → 400; U1 con membresía suspendida → 403 |
| API03 | POST login válido → autenticación, sin grant tenant automático | Credenciales incorrectas → 401; bypass de rate-limit → rechazar |
| API04 | Filtro de membresía autoriza recurso del owner A | Header B con actor A o sucursal sin empresa → DENY |
| API05 | Filtro RBAC permite módulo/acción con grant efectivo | Rol legacy, flag admin o soporte verificado POST → DENY |
| API06 | Angular verifica respuesta coincidente con solicitud vigente | Selección A→B y respuesta tardía A no debe reactivar A ni sobrescribir B |
| API07 | Error 409/500/503 estable contiene solo referencia y mensaje seguro | Excepción/log con PasswordHash, JWT, SQL o PII ajena → no aceptable |
| API08 | Contrato C# y contrato TypeScript deben conservar forma bajo versión compatible | DTO desconocido, campos no validados, respuesta malformada → no establecer contexto |

Estos son **oráculos para implementar/probar** y comprobaciones documentales; no se reportan como suites HTTP, EF, Angular, navegador o integración ejecutadas en M02.02. Caso de seguridad se comprueba de forma real cuando alguna microtarea altere el comportamiento ejecutable.

## 6. Impacto técnico, control A0–A5 y rollback

**Archivos de esta microtarea:** este documento y sección agrupada de `CHANGELOG_AI.md`; sin cambios de API/controllers, Application, Domain, Infrastructure/EF, Angular, tokens, roles, persistencia, workers, permisos, migraciones, secrets, middleware, servicios externos o CI. **Consumidores posteriores:** M02.03–06 y M03, UI, API, controles IAM de tenant; revalidar por archivo afectado antes de ejecutar.

- Clasificación: `requiere_dotnet=NO`, `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO`, `requiere_security=SI_DOCUMENTAL`, `requiere_scope_lock=SI`.
- **A0:** inspección estática del diff, nueve blobs fuente, secretos, atribución de CHANGELOG y hashes exactos.
- **A1:** comparar las cinco rutas/filtros documentados y los ocho oráculos API01–API08, con casos positivos/negativos y rechazo de implementación ficticia.
- **A2:** matriz JWT/membresía/roles/tenant A/B, datos prohibidos, CORS/rate-limit preservados; no convertir `EsAdministrador` ni localStorage en autorización.
- **A3:** diez predecesores, DAG para ID exacto, PLAN/COLA, compatibilidad conceptual de DTOs sin afirmar compilación nueva o MySQL.
- **A4:** commit directo fast-forward a dev, blobs y diff, checks exact-SHA `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success`, readback institucional.
- **A5:** otro TASK_ID Javier distinto; registrar A5_PENDIENTE antes de LISTO_DEV, sin autopromoción QA/main/PROD.
- **Rollback:** `git revert` normal del changeset documental y anotación correctiva aditiva si cambia el contrato; nunca force-push, reset destructivo, duplicar microtareas, modificar datos productivos ni borrar eventos históricos.

**Salida:** especificación verificable que permite preparar los contratos futuros sin conceder permiso de implementación, migración o despliegue. Cada etapa posterior exige un claim independiente, pruebas proporcionales y gates de seguridad causal.
