# C03-01.M02.05 — Contrato de eventos, idempotencia, correlación y límites UX_LOGIN_V1

**SOLQARYN V5 — PRIMARY DEV05 JAVIER**, TASK_ID `6adc2e55f47c81919f6374e561687076`, `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. Autoridad: STANDARD DE IMPLEMENTACIÓN V5 FINAL, PLAN_MAESTRO!D444 y anexo UX_LOGIN_INTELIGENTE_V1; M01.06 y M02.01–03 definen fronteras 0/1/N, entidades, API/DTO y preferencia futura. PLAN E444 es meta acumulativa del lote, NO significa que este documento implemente sesión, backend/Angular ni proveedor. **Riesgo de dominio R2, diff documental.**

## Fuentes reales y límites explícitos

- `backend/src/API/Controllers/AuthController.cs` tiene `POST /auth/login` [AllowAnonymous] con `EnableRateLimiting("AuthLogin")`; la contraseña errónea produce 401. `POST /auth/renovar` requiere [Authorize] y actor `id`; el controlador efectúa auditoría actual de login, pero **el éxito incluye NombreUsuario en mensaje**, por lo que la minimización de PII queda **pendiente de microtarea runtime**. No afirmar auditoría libre de PII.
- `frontend/src/app/core/auth/auth.service.ts` hace pre-sonda GET antes del POST y observa un `http.post` por suscripción de login. **No hay prueba aquí de que UI evite dos suscripciones o doble clic**; prevenir duplicados end-to-end queda para implementación y pruebas adversariales.
- `backend/src/API/Controllers/TenantContextController.cs` ofrece `GET /tenant-context/empresas` y `GET /tenant-context/{empresaId:int}`, JWT requerido; pertenencia ausente -> 403. Estos GET no persisten última empresa, no confirman cambios transaccionales ni reemplazan checks server-side de recurso.
- `backend/src/Infrastructure/Services/UsuarioScopeService.cs` verifica Usuario, Empresa, UsuarioEmpresa y Rol actuales; la preferencia nunca puede conceder privilegios. `Usuario.RolId` legacy, JWT/host/localStorage y `Rol.EsAdministrador` empresarial no permiten autoridad global ni cross-tenant.

## Eventos propuestos: nombres y datos permitidos, NO emisiones implementadas

| Código conceptual | Disparador / resultado | Campos permitidos mínimos | Prohibidos |
| --- | --- | --- | --- |
| `AUTH_ATTEMPT` | Intento de login, aceptado/rechazado o rate-limit | Código fijo, timestamp UTC, correlación opaca, categoría resultado | Password, nombreUsuario, JWT, IP literal, body |
| `SESSION_REJECTED` | JWT inválido, vencido, revocación o actor inactivo | Código de motivo de allowlist, estado 401/403, correlación | Token/claims, stack trace, PII |
| `TENANT_CONTEXT_EVALUATED` | 0/1/N membresías vigentes, sin elección por host | Conteo **en banda 0/1/2+**, código de ruta/result, correlación | Listado de empresas, roles, nombres, IDs sensibles en log |
| `TENANT_SELECTION_REJECTED` | Empresa ajena, inactiva o rol inválido | Código genérico `TENANT_DENIED`, correlación, 403 | Existencia o nombre de tenant ajeno, IDOR |
| `TENANT_SWITCH_COMPLETED` | Cambio validado A→B, revision actual | Correlación opaca, resultado, versión monotónica no sensible | Jwt, tenant ID en analytics públicos, snapshot anterior |
| `PREFERENCE_WRITE_REJECTED` | CAS viejo, revocación, actor incorrecto | Motivo clasificado y resultado, sin datos de preferencia | UsuarioId/EmpresaId ajenos, payload, roles |
| `AUTH_RETRY_GUARD` | POST bloqueado por doble clic o repetición | Código fijo y contador limitado, correlación | Credenciales, respuesta de auth o token |

Estos son **contratos futuros** para implementación R2 posterior; no se crean tablas, eventos de BITACORA comercial, emisores, proveedores externos ni observabilidad real ahora. La BITACORA institucional de automatización es distinta del audit log de sesión.

## Idempotencia, orden causal y correlación

1. **Login POST único:** UI debe usar bloqueo singleflight/disabled apropiado durante solicitud; no emitir un segundo POST por doble clic/Enter o por una sonda GET que se reintenta. Un fallo de red ambiguo **no** dispara retry automático del POST de credenciales. Rate limit backend `AuthLogin` sigue siendo obligatorio, no sustituye singleflight.
2. **GET seguros:** consultas de `tenant-context` pueden repetirse de forma acotada ante fallo transitorio si JWT/scope siguen vigentes; no reintentar ciegamente tras 401/403. `429` respeta `Retry-After` cuando exista; 503/timeout devuelve salida fail-closed, sin transformar 503 en falso PASS de aislamiento 403.
3. **Correlación opaca por intento:** generar identificador aleatorio sin incluir usuario/empresa/token, formato/longitud máximos, rotación por nueva acción y uso sólo para unir pasos permitidos. No usar correlation ID como clave de autorización.
4. **Preferencia futura:** identidad real del actor + clave idempotente por intención + versión CAS; revalidar membresía/rol actual dentro de transacción, nunca confiar en `EmpresaId` del cliente para ownership. Dos peticiones con misma clave producen un efecto o conflicto seguro, no dos preferencias.
5. **Cambio de tenant:** generación creciente de contexto, cancelación de peticiones anteriores y rechazo de callbacks tardíos; incluso con respuesta 200 A después del cambio a B, no se repinta A bajo B. Revalidación en el recurso final, no sólo en selector.
6. **Revocación:** si B pierde membresía entre consulta, selección y guardado, denegar y no reactivar preferencia; una idempotency key vieja no mantiene permiso. Cuenta bloqueada, rol inactivo, JWT expirado o tenant inactivo fallan cerrados.
7. **Límites:** tamaños fijos de identificadores/headers, mensajes de error opacos, presupuestos bounded de intentos y backoff, saturación/429 sin fuga, cero logs de secretos; evitar retención indefinida de correlaciones o eventos. No hay autorización de SMTP, passkeys/MFA, pagos, cloud o telemetría externa.

## Pruebas adversariales **definidas, no ejecutadas** en esta entrega

| Caso | Positivo / frontera | Negativo o fallo que requiere bloqueo |
| --- | --- | --- |
| E01 | Un clic/Enter => un POST login | Doble clic simultáneo => no segundo POST |
| E02 | GET health retry seguro; POST sólo una vez | HTTP timeout POST no reintenta sin usuario |
| E03 | Credencial válida => sesión según contrato actual | Contraseña inválida -> 401 sin revelar actor |
| E04 | Dos membresías A/B válidas => elección acotada | Host/header/localStorage B sin grant no concede B |
| E05 | 0 membresías => deny sin emisor sensible | `Usuario.RolId` legacy o rol Plataforma no concede empresa |
| E06 | JWT/rol/empresa vigentes => GET autorizado | JWT expirado/forjado o rol revocado -> 401/403 |
| E07 | Preferencia de U1 aislada de U2 | Reusar browser/correlation ID no cruza propietario |
| E08 | Una escritura CAS de preferencia con versión correcta | Dos writers obsoletos no escriben dos efectos |
| E09 | Cambio A→B válido descarta respuestas A viejas | Callback A tardío no repinta datos A |
| E10 | 429 espera acotada, código genérico | Reintento infinito o enumeración por error no aceptado |
| E11 | Evento permitido usa motivo fijo y correlación opaca | JWT, password, UsuarioId/EmpresaId o stack en log: FAIL |
| E12 | Revocación durante persistencia falla cerrado | Retry/idempotency key antigua restablece tenant: FAIL |

Oráculos futuros requieren verdaderas pruebas de integración HTTP/JWT/RBAC, aislamiento A/B, Oracle MySQL 8.4 efímero y Chromium cuando cambien esos componentes. **Este diff sólo documenta**, por lo que no atribuir PASS de runtime a las especificaciones E01–E12.

## A0–A5, impacto, rollback

- **A0:** cotejar D444, contrato UX_LOGIN_V1 y rutas/servicios actuales; diff sólo `docs/v5/` y CHANGELOG, sin nuevos endpoints, migraciones, DI, secretos o escrituras productivas.
- **A1:** doce casos falsables positivos, negativos, fronteras/errores y concurrencia **definidos**; no confundir diseño con ejecución.
- **A2:** denegación por defecto JWT/RBAC A/B, protección PII/secretos, idempotencia, revocación, TOCTOU y autoridad Plataforma separada; conservar riesgos legacy sin maquillar.
- **A3:** `NO_APLICA` a HTTP/MySQL/Chromium en esta microtarea documental; CI real requerida cuando M03/M04/M05 implementen estos contratos.
- **A4:** publicar fast-forward/CAS `dev`, comprobar `verify-solqaryn-project-scope` + V5 provenance `completed/success` del SHA causal y readback PLAN/COLA/BITACORA. Nunca tratar `SKIPPED/CANCELLED/FAIL/IN_PROGRESS` requerido como PASS.
- **A5:** otra Task Javier distinta del autor DEV05 y de reparador revisa código/documento/criterios con independencia real, no autoaprobación.
- **Rollback:** `git revert` del commit documental sobre dev con HEAD/candado revalidados, BITACORA histórica intacta y fórmulas DAG/COLA inalteradas. Fuera de alcance QA/main/PROD, secretos, DNS, proveedores y migración real.

**Salida atómica C03-01.M02.05:** eventos mínimos, límites, correlación opaca, idempotencia y doce oráculos definidos; **no** funcionamiento de login, preferencia ni auditoría live certificado.
