# C03-01.M02.02 — Contrato API/DTO, errores y compatibilidad UX_LOGIN_V1

**SOLQARYN V5, DEV05 JAVIER** — TASK_ID `6adc2e55f47c81919f6374e561687076`, `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. Fuente: STANDARD V5 FINAL, PLAN_MAESTRO D441 y anexo `UX_LOGIN_INTELIGENTE_V1`. La **salida atómica** de D441 es definir contratos API, DTO, errores, validaciones y compatibilidad para login, selección 0/1/N, preferencia de usuario y confirmación tenant. PLAN E441 expresa meta **acumulada de fase**; esta unidad NO implementa endpoints, JWT, migraciones, UI ni MFA. Riesgo de diseño R2; diff sólo documental.

## Contrato existente verificado en `dev` (no inventar rutas)

| Ruta real | Autenticación, datos y respuesta actuales | Fallo real / límite |
| --- | --- | --- |
| `POST /auth/login` | `AuthController.Login` anónimo con `AuthLogin` rate limit, `LoginRequestDto { NombreUsuario, Password }` a `IAuthService.LoginAsync`; `ApiResponse<LoginResponseDto>` con `Token,NombreUsuario,NombreCompleto,Rol,FotoPerfilUrl,ExpiraEn` | Credenciales inválidas -> `401 ApiResponse.Fail`. **No existe selector automático 0/1/N en esta respuesta actual** |
| `POST /auth/renovar` | `[Authorize]` y claim `id`, `IAuthService.RenovarAsync`, devuelve LoginResponseDto | Claim sin ID o sesión inválida -> `401`; no prueba revocación completa por sí mismo |
| `GET /tenant-context/empresas` | `[Authorize]`; lista `EmpresaSeleccionable(EmpresaId,Nombre)` sólo desde `IUsuarioScopeService.ListarEmpresasSeleccionablesAsync` | No expone un catálogo global; sin JWT -> política middleware de autenticación |
| `GET /tenant-context/{empresaId:int}` | `[Authorize]`; devuelve `UsuarioTenantScopeActual(UsuarioId,EmpresaId,RolId,RolNombre,EsAdministrador)` validado por servidor | `empresaId<=0` -> `400`; empresa inexistente, inactiva o ajena -> `403` indistinguible para evitar enumeración |

**Fuentes:** `backend/src/API/Controllers/{AuthController,TenantContextController}.cs`, `backend/src/Application/DTOs/LoginDto.cs`, `backend/src/Application/Interfaces/{IAuthService,IUsuarioScopeService}.cs`, `backend/src/Infrastructure/Services/UsuarioScopeService.cs`, `frontend/src/app/core/models/auth.model.ts` y `frontend/src/app/core/auth/auth.service.ts`.

**Compatibilidad vigente:** los DTO frontend `LoginRequest(nombreUsuario,password)` y `LoginResponse(token,nombreUsuario,nombreCompleto,rol,fotoPerfilUrl?,expiraEn)` reflejan el flujo actual. No añadir campos obligatorios de empresa, rol, selector, sucursal ni tenant al POST de credenciales. El `AuthService` frontend **actualmente conserva token y datos de usuario en localStorage**; es superficie de riesgo heredada y no se modifica ni se declara segura por este contrato. El `AuthController` registra detalles de auditoría tras éxito; minimizar PII cuando una unidad de runtime toque esos registros, no asumir cobertura actual de sanitización.

## Protocolo UX_LOGIN_V1 propuesto para futuras unidades, sin alterar el login existente

1. Usuario envía **un único** `POST /auth/login` con nombre y contraseña. La sonda de disponibilidad GET no debe reintentar el POST. Ante 401, error estable y genérico sin confirmar si el usuario existe.
2. La sesión firmada identificada **no autoriza** una empresa por sí sola. Consumidor consulta `GET /tenant-context/empresas` desde el usuario autenticado; el backend calcula empresas actuales, no recibe actor suministrado por UI.
3. **0 empresas:** denegar contexto empresarial; ingreso a consola global sólo con guard de rol **Plataforma real** distinto, sin convertir `rol: Administrador` del LoginResponse en poder global.
4. **1 empresa A:** usar `GET /tenant-context/A` para verificar y entrar directamente al panel de A, **sin selector ni segundo POST de autenticación**. Si A se revocó, 403 con sesión empresarial bloqueada.
5. **N empresas:** usar hint de preferencia **del mismo usuario** únicamente si A permanece en lista autorizada. Si no hay hint válido, UI presenta elección accesible de `EmpresaSeleccionable`; antes de usar B debe obtener `GET /tenant-context/B` actual. Un ID arbitrario o pertenencia revocada produce 403.
6. **Cambio A→B:** nueva consulta al servidor, invalidación de cachés y generación de solicitudes para descartar respuestas A fuera de época; 401/403/timeout no deben preservar contexto privilegiado anterior.
7. **Preferencia persistente futura:** cualquier API de preferencia será definida por M02.03/M03 en especificación separada, por actor autentificado, con idempotencia, versión y validación real; **NO existe ruta nueva certificada aquí**. No escribir preferencias desde parámetros de tenant sin comprobación de membresía.

## DTO y compatibilidad propuestos, no publicados

| Operación funcional | Datos mínimos confiables | Validación y compatibilidad |
| --- | --- | --- |
| Login existente | `NombreUsuario` y `Password` | Ambos obligatorios, longitud/estructura según validador vigente; sin empresa en credenciales; misma `ApiResponse` y estado 401 genérico |
| Lista vigente | Token de actor en middleware; `EmpresaSeleccionable[]` | Orden determinista y no enumeración global; 0/1/N válidos, no incluir roles/permisos de empresas ajenas |
| Confirmación existente | `empresaId` positivo de ruta + identidad servidor | 400 si ID inválido, 403 si no autorizado; payload efectivo `UsuarioTenantScopeActual` de servidor, **no** aceptar `RolId` del cliente |
| Pista última empresa futura | Identidad server-side + `EmpresaId` sugerida + versión de concurrencia | Nunca token/rol/grant; validación al leer y al confirmar, scoped por usuario; rechazar otra identidad/cambio TOCTOU |
| Errores | `ApiResponse.Fail` cuando corresponda, 401/403/400 actuales | 401 sesión/credenciales; 403 membresía ausente o revocada; 400 ID formalmente inválido; 429/5xx manejar sin fuga. No convertir 503 de guard temporal en 403 probado |

**No ampliar silenciosamente la semántica de endpoints existentes.** Un GET autenticado de contexto no materializa persistencia de preferencia, no prueba recuperación de contraseña ni concede nueva sesión. Cualquier DTO futuro debe ser compatible con clientes existentes y validarse en contrato HTTP real cuando M03/M05 lo implemente.

## Matriz de verificación positiva, negativa, borde y fallo (oráculos futuros)

| Caso | Positivo | Negativo / error exigido |
| --- | --- | --- |
| C01 | POST válido -> 200 con respuesta compatible | Contraseña mala -> 401, sin enumerar usuario ni PII |
| C02 | Sólo A vigente -> GET empresas [A] y GET A 200 | GET B, aun si enviado por header/caché, 403 |
| C03 | Cero empresas -> lista vacía y sin panel empresarial | `Rol` legacy o hint local no habilita contexto |
| C04 | N empresas -> [A,B] y selección desde ellas | Preferencia de usuario distinto o C externa -> 403 |
| C05 | Revocación entre lista/confirmación -> 403 | Snapshot cliente anterior no concede acceso |
| C06 | `empresaId=0/-1` -> 400 o rechazo de ruta seguro | IDs mal formados, ambigüedad y overflow no hacen fallback |
| C07 | Token firmado vigente + rol A -> acceso A autorizado | JWT expirado/falsificado/rol B o sin issuer -> 401/403 |
| C08 | Cambio A->B invalida contexto A previo | Respuesta tardía de A no restaura datos/SEO/permisos |
| C09 | Un clic de credenciales = un POST | Reintento/clic múltiple no duplica sesión ni registra secretos |
| C10 | Renovar sólo con sesión vigente, `id` válido | Sin ID, actor inactivo o revocado -> 401 |
| C11 | Error opaco consistente sin PII en 400/401/403/429/5xx | Stack trace, JWT, nombres de recursos ajenos no expuestos |
| C12 | Rol de Plataforma verificado -> consola global independiente | Admin de empresa no obtiene consola global |

Estos oráculos **NO** se ejecutaron por este cambio documental; M03/M05 requerirá backend .NET 10, JWT/RBAC real, tenant A/B, MySQL 8.4 efímero y Chromium Playwright si la superficie lo modifica.

## Evaluación y rollback V15/V17

- **A0:** diff documental, contraste de firmas/DTO con archivos reales; no alterar `AuthController`, tenant routes, DI, DB, frontend ni secretos.
- **A1:** doce oráculos verificables positivos/negativos, frontera 0/1/N, expiración, cancelación/error, compatibilidad; diseño **no PASS runtime**.
- **A2:** separación JWT/tenant/rol Plataforma, revocación, IDOR y privacidad; no certificar JWT real por haber escrito la matriz. Riesgos heredados localStorage y auditoría con nombre siguen abiertos, sin introducir nuevos.
- **A3:** `NO_APLICA` a .NET HTTP/MySQL/Chromium en este diff Markdown; integración auténtica queda obligada al desarrollar los endpoints/selector.
- **A4:** commit FF/CAS en `dev`, `verify-solqaryn-project-scope` + V5 provenance `completed/success` exact-SHA, PLAN/COLA/BITACORA readback, sin aceptar checks requeridos `SKIPPED/CANCELLED/FAIL/IN_PROGRESS`.
- **A5:** otro Javier Task ID distinto de autor DEV05 y de reparador; sin autocertificación.
- **Rollback:** revert commit documental normal tras releer HEAD, sin force-push, sin borrar BITACORA, sin cambiar DAG/COLA ni QA/main/PROD. Runtime futuro exige rollback seguro probado y revocación efectiva.

**Salida atómica C03-01.M02.02:** contrato de API, DTO, errores y compatibilidad firmado como especificación, **no** endpoint nuevo ni autenticación empresarial certificada.
