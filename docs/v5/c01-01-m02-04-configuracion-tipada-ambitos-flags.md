# SOLQARYN V5 FINAL — C01-01.M02.04: diseño de configuración tipada, ámbitos y feature flags seguros

**Microtarea:** `C01-01.M02.04`. **Autor:** PRIMARY DEV 04 — JAVIER, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. **Tipo:** diseño documental de contratos, sin código ni configuración efectiva alterados. **Ámbito:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`.

**Resultado exigido:** definir configuración tipada, ámbitos y feature flags seguros para identidad universal empresa/sucursal/recurso/usuario/membresía/contexto. Esta especificación NO añade valores de appsettings, variables de entorno, grants, secretos, perfiles, planes, suscripciones, entitlements, rutas, tablas ni persistencia; tampoco cambia configuración real de proveedores.

**Fuentes institucionales:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`) y Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. Contratos previos M02.01 (SHA `cbb112f24e162ae914b6a56221c1cbc58bb3f8c0`), M02.02 (`a076e8901d10d04dcc8e47997d4bbe28a56e67d3`), M02.03 (`4100cf526ee1bcaf686a553f843c243438c864f6`) terminaron `LISTO_DEV` documental, NO implementación integral ni promoción.

## 1. Preflight causal y no-alcance

El DAG para M02.04 incluye nueve predecesores `C01-01.M01.01–06` y `C01-01.M02.01–03`, más `C00-01.M06.06`, `C00-03.M01.06` y `C00-03.M06.06`: doce en total. Antes del claim, `PLAN_MAESTRO!H146:H154=LISTO` y H37/H79/H109=LISTO, `PLAN!H155=PENDIENTE/I155` vacía, `DAG_TECNICO!F141=VALIDADO/G141=SI` y `COLA!G155/O155=SI`. Un preflight documental no puede liberar IAM/JWT/RBAC/tenant/DB/M02.06/M03/QA/main/PROD. Toda configuración futura en código requiere microtarea independiente, revisión de amenazas y CI causal.

## 2. Inventario de fuentes dev y garantías limitadas

| Archivo y blob SHA de HEAD verificado | Contrato presente | Límite de la evidencia |
| --- | --- | --- |
| `backend/src/Domain/Entities/ConfigEmpresa.cs` — `664547d28b513d3187622b02c57a7db1f812f7ec` | `ConfigEmpresa` con `EmpresaId` positivo, `Moneda=HNL`, `ZonaHoraria=America/Tegucigalpa`, JSON tributario/emisión, token `Version` y referencia opaca `CorreoSecretoReferencia` | No incluye credencial SMTP en claro; el hecho de que exista referencia no demuestra proveedor de secretos activo |
| `backend/src/Infrastructure/Persistence/Configurations/ConfigEmpresaConfiguration.cs` — `c26a46d4381618c960c93bb71806f802969ddcb8` | EF con FK `EmpresaId` Restrict, índice único `UX_ConfigEmpresas_EmpresaId`, checks de `Moneda` y `Version`, `IsConcurrencyToken`; plantillas con unicidad (EmpresaId,TipoPlantilla) | Mapas de EF no prueban existencia de constraints o reconciliación de versión en DB de proveedor |
| `backend/src/Domain/Entities/FeatureFlagsSaaS.cs` — `ec0062f4d76fd106365c20f3ba50c214369a3de5` | `PlanModulo`, `PoliticaModulosSaaS.Evaluar`, `DecisionModuloSaaS`; deniega tenant inválido/ajeno, suscripción ausente, plan inválido o módulo sin regla explícita | Entitlement del plan NO es permiso RBAC ni prueba de suscripción en vivo |
| `backend/src/API/Filters/TenantPermissionContext.cs` — `df7d49f3e185c538f3b3dafabb36c3a971a7257d` | `X-Empresa-Id` selector, no autoridad; selector ambiguo/no positivo falla; fallback solo a contexto único realmente verificado | Header válido sintácticamente no implica membresía ni autorización |
| `backend/src/Application/Security/VerifiedPlatformSupportTenant.cs` — `710cec66f4859aafa7c82fbc882c0907b3accf28` | Marker candidato de soporte y marker autorizado de lectura con `Scope(SesionId,EmpresaId,Modulo)`, comprueba coincidencia | Marker candidato NO concede grants; requiere autorización posterior del filtro de permisos |
| `docs/adr/ADR-N6.10-FEATURE-FLAGS-PRECEDENCE.md` — `36cf73ac42de92d288fd85158f306cdb86207572` | Precedencia `EXPERIMENT -> TENANT -> PLAN`; no omite autorización/membresía/RBAC; caché por tenant y dimensión funcional | Un ADR describe contrato, no acredita que cada consumidor lo cumpla |

**Fuente contractual de M02.01–03:** los documentos anteriores delimitan entidad/ownership, DTO/API y persistencia. Esta M02.04 solo define reglas configurables y de flags, sin cambiar la autoridad ni conferir nuevos permisos.

## 3. Esquema lógico tipado de configuración propuesto (sin nuevo DTO)

| Clave conceptual | Tipo, valor seguro y dueño | Validación y condición de no-regresión |
| --- | --- | --- |
| `EmpresaId` | Entero positivo, owner único del objeto de configuración | Se deriva del tenant server-side; jamás usar EmpresaId de header/cliente como autoridad |
| `Moneda` | Código ISO de longitud 3; actual default HNL | Normalizar y comprobar valor; no reutilizar configuración de empresa B |
| `ZonaHoraria` | Nombre de zona válido, default America/Tegucigalpa | No aceptar valor vacío o zona sin verificación; no cambia timezone físico de Tasks |
| `ImpuestosJson` | Parámetros operativos por empresa, `{}` inicial | Esquema tipado futuro, límites de tamaño, rechazo de campos inesperados; no sustituye catálogo tributario |
| `EmisionJson` | Parámetros no secretos por empresa, `{}` inicial | No persistir JWT, contraseñas, tokens SMTP ni secuencias fuera de su fuente |
| `CorreoSecretoReferencia` | Referencia opaca a almacén externo controlado | Nunca devolver referencia en DTO público ni usar el texto como credencial; proveedor/configuración no creado aquí |
| `Version` | Entero largo monotónico positivo | Cambios futuros usarán CAS/concurrencia y no borrarán actualización ajena |
| `ModuloClave` | String normalizado trim+upper, reglas explícitas por plan y tenant | Módulo vacío/plan inválido/suscripción vencida ⇒ denegado; no derivar módulo habilitado de límite cuantitativo |

**Semántica de ámbitos:** `GLOBAL_PLATAFORMA` para configuración técnica privilegiada gestionada únicamente bajo autoridad de plataforma real; `EMPRESA` para configuración operativa y entitlement; `SUCURSAL` para futura política explícita y sin herencia de grants; `USUARIO` para preferencias sin capacidad de modificar seguridad. Estas etiquetas son un **modelo de diseño**, no enumeraciones introducidas en C# ni feature flags existentes. Los datos `EmpresaId` y `Version` deben viajar junto con la comprobación del owner, nunca separarse en caché global.

## 4. Orden de decisión de flags y matriz de seguridad

La ADR existente describe **EXPERIMENT → TENANT → PLAN**, siempre **dentro** de sesión autenticada, membresía de empresa activa y permiso RBAC ya aprobado. Un experimento puede especializar comportamiento dentro del tenant y módulo, pero nunca habilitar una operación prohibida por RBAC ni cambiar `EmpresaId`. Plan y suscripción vigentes son condición base, y la ausencia de `PlanModulo` explícito **niega** la función. La lectura de configuración de B a partir de sesión A debe rechazarse incluso si ambos planes comparten nombre de módulo.

**Concurrencia/caché:** toda llave efectiva futura incluye empresa autorizada, plan, módulo, experimento y versión cuando aplique; al revocar membresía, suscripción o permiso se invalida la decisión. Caché global sin partición por empresa, fallback `true` ante fallo de proveedor, selector localStorage sin validación o `X-Empresa-Id` arbitrario no pueden conceder permiso.

**Soporte plataforma:** `VerifiedPlatformSupportTenant` tiene candidate y authorized distintos; un candidate no es permiso. El filtro de permiso actual limita consultas a GET/Ver y módulo/empresa precisos; una flag no debe ampliar la sesión temporal a POST o rol de B.

## 5. Casos documentales positivos/negativos que deberán probarse

| ID | Aceptación positiva esperada | Caso negativo obligatorio |
| --- | --- | --- |
| CFG01 | Contexto A validado carga solo ConfigEmpresa A | Cliente cambia header a B sin membresía: DENY |
| CFG02 | Configuración de A tiene `Moneda`/zona válidas y version positiva | Moneda inválida, timezone vacío, version ≤0: rechazar |
| CFG03 | `CorreoSecretoReferencia` se maneja solo server-side | DTO/log presenta credencial/secret reference privada: FAIL |
| CFG04 | Versionado permite actualización con estado actual | Escrituras concurrentes con versión obsoleta: CONFLICT, no overwrite |
| CFG05 | Suscripción y plan explícitos vigentes permiten solo módulo habilitado | Plan no disponible o módulo sin regla: DENY |
| CFG06 | Empresa A con rol RA y plan correcto accede a A si RBAC permite | Token/rol RB de B, experimento o plan intenta saltar RBAC A: DENY |
| CFG07 | Tenant A y B con igual plan/módulo tienen cachés independientes | Reutilizar cache de A en B o tras revocación: DENY/INVALIDATE |
| CFG08 | `X-Empresa-Id` único y miembro válido se procesa como selección | Header duplicado, cero, inválido o B ajena: DENY |
| CFG09 | Soporte plataforma verificado consulta GET del módulo/empresa autorizado | Marker candidate solo, POST o módulo diferente: DENY |
| CFG10 | Flag no afecta identidad global ni datos privados | Feature toggle habilita nueva ruta JWT/tenant/API/DB sin tests: NO_GO |

Estos son **oráculos para futuras pruebas**; NO se ejecutaron HTTP, .NET, Angular, browser ni MySQL por redactarlos. La fuente de verdad sigue siendo el código implementado, su CI del SHA causal y el gate de seguridad real.

## 6. Clasificación, verificaciones A0–A5 y rollback

**Diff permitido:** únicamente este Markdown y nueva sección autocontenida de `CHANGELOG_AI.md`; cero appsettings, secretos, Provider, flag habilitado, migration, tabla, endpoint, plantilla real o cambio de rol.

- `requiere_dotnet=NO`; `requiere_node_angular=NO`; `requiere_mysql=NO`; `requiere_docker=NO`; `requiere_e2e=NO`; `requiere_security=SI_DOCUMENTAL`; `requiere_scope_lock=SI`.
- **A0:** diff de dos archivos, seis fuentes con hash, ausencia de secretos/PII, encabezados de CHANGELOG y no duplicación de sección.
- **A1:** comprobar diez oráculos CFG01–CFG10 con resultado positivo/negativo conceptual sin confundir con ejecución.
- **A2:** aislamiento A/B, RBAC, JWT, revocación, soporte limitado, secreto de correo, caché y fallback fail-closed.
- **A3:** doce predecesores (M01/M02.01–03 y tres C00), DAG individual VALIDADO, coincidencia de fuentes/tipos y no regresión de consumidor.
- **A4:** commit directo FF a `dev`, diff/blob/SHA exacto, checks `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success`, readback PLAN/COLA/BITACORA. Compilaciones ajenas a Markdown no se exigen, pero no se etiquetan PASS si están SKIPPED/IN_PROGRESS.
- **A5:** revisión independiente de otra Task Javier distinta del autor; A5_PENDIENTE no autoriza QA/main/PROD.
- **Rollback:** `git revert` ordinario de documentación y evento compensatorio append-only, no revertir flags ni recursos reales sin procedimiento aprobado; prohibidos force-push, cambios de RRULE, compras y despliegues.

**Dependencias siguientes:** M02.05 y M02.06 requieren su propio gate. M03 futuro con código/configuración ejecutable exige .NET/Angular/MySQL/seguridad conforme a archivos y checks realmente afectados. Cualquier autorización/entitlement real conserva A0–A5 y seguridad fail-closed.
