# SOLQARYN V5 FINAL — C03-01.M01.02: actores, entradas, salidas y superficies de autenticación

**Entrega:** análisis DOCUMENTAL de M01.02 del bloque C03-01 (autenticación y recuperación). **Autor:** PRIMARY DEV 06 — JAVIER, TASK_ID `6af16bbad26881918c283abe06e1f32a`; `solqaryn/Solqaryn@dev`, Scope Lock STRICT. **Fuente única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (`1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), Sheet PLAN_MAESTRO/COLA/DAG_TECNICO (`1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`). DAG VALIDADO con 10/10 antecedentes C02-03.M01.01–05 y C02-04.M01.01–05 LISTO. La autorización alcanza solo documentación M01.01–05, sin habilitar M01.06/M02 ni alterar código.

**Salida futura de fase:** login, renovación, revocación y recuperación segura, con passkeys/MFA únicamente si un proveedor y contrato posteriores son autorizados. Evidencia mínima FUTURA: sesiones revocadas, intentos abusivos de recuperación y credenciales débiles. Esta M01.02 clasifica actores e interfaces, no implementa ni certifica esas funciones.

## 1. Inventario de actores y límite de confianza

| Actor | Operación/entrada posible | Riesgo y regla futura |
| --- | --- | --- |
| Usuario anónimo | Credenciales en `POST /auth/login` | Aceptar solo autenticación válida, rate limit y error uniforme; nunca otorgar rol/tenant por input |
| Usuario autenticado | JWT Bearer en `POST /auth/renovar` | Revalidar estado y rol, sin ampliar scope ni asumir refresh rotativo |
| Cuenta bloqueada/inactiva | Intento login/renovación | Denegación y auditoría segura, nunca restaurar sesión con token anterior |
| Operador empresarial A | Selección de tenant y uso de recursos A | Membresía y permisos server-side; no acceso a B por claim/header |
| Usuario con membresías A/B | Cambia contexto entre empresas | Reevaluar autorización efectiva en cada empresa; no reusar permisos entre tenants |
| Administrador de plataforma | Acceso extraordinario si hay política explícita | Rol global no nace de rol empresarial; MFA/reauth y motivo FUTUROS requieren aprobación |
| Auditor autorizado | Revisión de log de acceso/denegación | Lectura limitada; PII y credenciales fuera de registros y exports |
| Servicio interno autorizado | Verifica estado de identidad/membresía | Servicio con ámbito explícito, trazabilidad y secretos fuera del repo |
| Solicitante de recuperación FUTURA | Identificador de cuenta o token de recovery | No enumerar cuenta, tokens de un solo uso/TTL; sin endpoint acreditado hoy |
| Proveedor MFA/passkey FUTURO | Resultado de factor adicional | Firma, binding, anti-replay y fallo cerrado solo si integración se aprueba |

Ningún actor externo recibe permiso por declarar ser plataforma, administrador, otro tenant o usuario interno. La Task autora tampoco se atribuye A5 independiente.

## 2. Matriz de entradas, salidas y superficies

| ID | Superficie | Entrada | Salida / efecto y límite actualmente verificable |
| --- | --- | --- | --- |
| IO01 | `AuthController.Login`, `POST /auth/login` | `LoginRequestDto` (usuario, contraseña), anónimo | `200` con `LoginResponseDto` tras servicio; `401` genérico al devolver null; auditoría de éxito/rechazo |
| IO02 | `AuthController.Renovar`, `POST /auth/renovar` | JWT autenticado con claim `id` numérico | `200` token renovado o `401` si identidad/resultado inválido; no se verificó refresh token rotativo |
| IO03 | `AuthService.LoginAsync` | NombreUsuario/Password | Busca usuario no eliminado, verifica BCrypt, estado activo/no bloqueado, rol relacional válido |
| IO04 | `AuthService.RenovarAsync` | Identificador extraído del JWT | Relee usuario y estado/rol; no acredita revocación criptográfica universal |
| IO05 | `JwtService.GenerarToken` | Usuario y configuración Jwt | JWT HS256 con `id`, rol, JTI y expiración acotada; JTI no equivale a denylist |
| IO06 | `AuthLogin` rate limiting | Solicitud HTTP de login | Política declarada por atributo; se debe probar 429/abuso en runtime para certificar defensa |
| IO07 | Auditoría login | Resultado y nombre de usuario | Registro saneado; no escribir contraseñas, tokens, enlaces de recovery ni claims sensibles |
| IO08 | Autorización de recurso tenant | JWT y selector de empresa solicitado | A/B y membresía verificados en filtros/servicios específicos; un JWT no es grant global |
| IO09 | Recuperación FUTURA | Identificador, enlace/código y nuevo secreto | Por definir: respuesta indistinguible, TTL, uso único, rotación segura, auditoría; NO existe ruta acreditada en este inventario |
| IO10 | Revocación central de sesión FUTURA | Sesión/token/JTI y causa autorizada | Por definir: invalidación verificable y denegación de token emitido, no inferible de `renovar` |
| IO11 | MFA/passkeys FUTUROS | Desafío, factor, dispositivo, firma y proveedor | Ninguna integración se activa o certifica mediante este documento |
| IO12 | Observabilidad y proveedores | Correlation ID/resultado/error sin secretos | Eventos seguros, sin PII excesiva; ante falla crítica, fail-closed según política aprobada |
| IO13 | Pruebas y CI posterior | SHA, fixtures A/B, casos de sesión y contratos | .NET/MySQL/E2E solamente cuando implementación afectada los exija, no como prueba de este documento |
| IO14 | Gobierno de estados V5 | PLAN H/I, COLA E/G/O y BITACORA A:Z | 26 columnas, EVENT_ID/IDEMPOTENCY_KEY; A5 solo por otra Task Javier distinta del autor |

## 3. Evidencia estática por rutas y hashes de blob

| Archivo real leído de dev | Blob SHA | Alcance y límite de inferencia |
| --- | --- | --- |
| `backend/src/API/Controllers/AuthController.cs` | `af437c869b4800a499c42aeff00347de3a3cc521` | Rutas, atributos, respuestas 200/401 y auditoría |
| `backend/src/Application/Services/AuthService.cs` | `88d18217cd214cc00fca383983d775113fba0c87` | BCrypt, usuario activo, rol relacional; login/renovar |
| `backend/src/Application/Interfaces/IAuthService.cs` | `986743c5ba7015c5c6bda564cc50781f3b26cd65` | Dos métodos existentes; no contrato completo de recovery |
| `backend/src/Infrastructure/Services/JwtService.cs` | `9051fe6b4aac23e0ce3ebc684a2830c2cd2e6c9b` | Claims, firma y TTL; no session store acreditado |
| `backend/tests/Solqaryn.Tests/I8005CAuthTokenAntifuerzaBrutaDelimitacionTests.cs` | `14b4d55992e0f0cc5866d3ef6b6f232722c318fa` | Reflexión sobre atributos, no HTTP 429 real |
| `backend/tests/Solqaryn.Tests/I8009ELoginAuditoriaSinSecretoTests.cs` | `7050551eee56d658b455717848259dfd80d725f6` | Mocks no filtración en respuesta/auditoría, no proveedor live |
| `backend/tests/Solqaryn.Tests/P2013DJwtMembresiaRevocadaTests.cs` | `ee4647f49c5045d217200e1b92c7adc668500f3d` | EF InMemory y empresa A/B; no DB MySQL ni revocación total de JWT |

La evidencia precedente proviene de archivos reales y distingue código existente de capacidad FUTURA. No afirmar que passkeys, MFA, refresh rotativo, logout central o recuperación ya funcionen. La existencia de tests no prueba que se hayan ejecutado en esta entrega.

## 4. Casos de aceptación futura y amenazas que deben cruzar la matriz

| Caso | Tipo | Precondición y resultado observable requerido en fase posterior |
| --- | --- | --- |
| AC01 | + | Identidad válida y activa: login 200, JWT y auditoría sin secreto |
| AC02 | + | Renovación desde sesión autorizada: status y rol vigente, sin elevación |
| AC03 | - | Credencial errónea o usuario inexistente: 401 uniforme y sin enumeración |
| AC04 | - | Cuenta bloqueada/rol inactivo: ninguna credencial nueva |
| AC05 | - | JWT vencido/falsificado o tenant B no autorizado: 401/403 y cero datos B |
| AC06 | - | Intentos repetidos o distribuidos: rate limit 429 real y sin abuso |
| AC07 | - | Token de recuperación vencido/replay: ningún cambio de contraseña |
| AC08 | - | Recuperación de cuenta inexistente: respuesta no enumeradora, sin correo de secreto |
| AC09 | - | Contraseña débil o comprometida: rechazo conforme contrato aprobado |
| AC10 | - | Revocación de membresía con JWT anterior: recursos B/A denegados según ámbito |
| AC11 | - | Falla MFA/proveedor o factor distinto: ningún bypass, si feature FUTURA autorizada |
| AC12 | - | Fallo de auditoría/DB o repetición concurrente: efecto único o rollback consistente |
| AC13 | - | Rol empresa A o claim `esAdministrador` invoca plataforma B: 403 sin grant |
| AC14 | control | Evento de revisión incompleto/autorevisión: fail-closed y BITACORA 26 campos |

AC01–AC14 son **oráculos futuros**, no pruebas ejecutadas en M01.02. La microtarea no genera usuarios, tokens ni sesiones, ni modifica JWT, RBAC, datos reales, CI o proveedores.

## 5. Clasificación proporcional de herramientas y cierre

`requiere_dotnet=NO_APLICA`, `requiere_node_angular=NO_APLICA`, `requiere_mysql=NO_APLICA`, `requiere_docker=NO_APLICA`, `requiere_e2e=NO_APLICA`: el artefacto es solo Markdown. `requiere_security=SI_DOCUMENTAL`: lectura de fronteras de JWT/BCrypt/RBAC/tenant, abuso y recuperación sin inventar PASS de runtime. `requiere_scope_lock=SI`: check `verify-solqaryn-project-scope` SUCCESS del SHA publicado.

A0: diff Markdown, estructura, enlaces internos, referencias SHA, exposición de secretos. A1: actores/IO positivos y negativos, salida futura explícita. A2: denegaciones, no enumeración, rate limiting, A/B, privacidad y privilegios definidos como contratos, no runtime. A3: siete archivos/consumidores revisados de modo proporcional; no se modifica integración. A4: commit directo serializado fast-forward en `dev`, SHA exacto y Scope Lock, readback PLAN/COLA/BITACORA A:Z, rollback `git revert`. A5: revisión técnica independiente pendiente de otra de cinco Tasks Javier; nunca QA/main/PROD automático.
