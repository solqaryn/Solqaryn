# SOLQARYN V5 FINAL — C03-01.M01.03: inventario verificable de contratos, código y evidencia de autenticación

**Microtarea:** C03-01.M01.03, M01 preparación DOCUMENTAL. **Autor:** PRIMARY DEV 06 — JAVIER, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Repo:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Fuente V5:** Standard de Implementación `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU` PLAN_MAESTRO/COLA/DAG_TECNICO. DAG VALIDADO: 10/10 antecedentes C02-03.M01.01–05 y C02-04.M01.01–05 LISTO. M01.01–02 anteriores están documentadas, pero este inventario no da por cerrado M01.06 ni M02.

**Objetivo delimitado:** identificar contratos HTTP, negocio, JWT, sesión Angular, contexto empresa y pruebas existentes; clasificar evidencia **real de código**, **test con mocks/EF InMemory** y **capacidad todavía no verificada**. El resultado futuro C03-01 requiere login, refresh, revocación, recuperación y passkeys/MFA si proveedor aprobado, con negativos de sesión revocada, recuperación abusiva y credencial débil. M01.03 **NO** implementa ni certifica dichas funcionalidades.

## 1. Inventario de archivos inspeccionados en dev (SHA de blobs)

| Ref | Ruta | Blob SHA | Hecho delimitado |
| --- | --- | --- | --- |
| F01 | `backend/src/API/Controllers/AuthController.cs` | `af437c869b4800a499c42aeff00347de3a3cc521` | `POST /auth/login` anónimo con `AuthLogin`; `POST /auth/renovar` protegido; auditoría positiva/negativa |
| F02 | `backend/src/Application/Services/AuthService.cs` | `88d18217cd214cc00fca383983d775113fba0c87` | BCrypt Verify, estado de cuenta, rol relacional; `LoginAsync` y `RenovarAsync`; sin recovery acreditado |
| F03 | `backend/src/Application/Interfaces/IAuthService.cs` | `986743c5ba7015c5c6bda564cc50781f3b26cd65` | Contrato solo de login y renovación; no `RefreshTokenAsync` ni `ResetPasswordAsync` |
| F04 | `backend/src/Infrastructure/Services/JwtService.cs` | `9051fe6b4aac23e0ce3ebc684a2830c2cd2e6c9b` | Firma HS256, claims y expiración; JTI no demuestra denylist/revocación centralizada |
| F05 | `backend/src/Application/DTOs/LoginDto.cs` | `6851ae4763fab9bc05bcf14ab75c0c8fd446089e` | LoginRequest: nombre de usuario/contraseña; LoginResponse: token, nombre, rol y expiración |
| F06 | `frontend/src/app/core/auth/auth.service.ts` | `9c9b58c4b289ccc3269b8e99b614a2a9dbaae557` | Angular Signals, POST login/renovar; persistencia de JWT/rol en localStorage; logout limpia valores del navegador |
| F07 | `frontend/src/app/core/auth/tenant-context.service.ts` | `63a711355495d2f80efba35bffb2b8727401154a` | Selector empresa solicita validación server-side y rechaza respuestas tardías; localStorage no es autoridad |
| F08 | `frontend/src/app/core/auth/session-activity.service.ts` | `295299b98204fd38fa542c68c9187c2a2f0faf2c` | Cierre por inactividad de 30 minutos y renovación programada en navegador; no prueba revocación servidor |
| T01 | `backend/tests/Solqaryn.Tests/I8005CAuthTokenAntifuerzaBrutaDelimitacionTests.cs` | `14b4d55992e0f0cc5866d3ef6b6f232722c318fa` | Test de reflexión de atributos AuthLogin y Authorize, NO test HTTP real de 429 |
| T02 | `backend/tests/Solqaryn.Tests/I8009ELoginAuditoriaSinSecretoTests.cs` | `7050551eee56d658b455717848259dfd80d725f6` | Mocks para no filtrar tokens inventados en fallo de login; NO prueba proveedores/logs live |
| T03 | `backend/tests/Solqaryn.Tests/P2013DJwtMembresiaRevocadaTests.cs` | `ee4647f49c5045d217200e1b92c7adc668500f3d` | EF InMemory y membresía A/B; NO prueba revocación universal JWT ni MySQL HTTP real |

**Hallazgo de ausencia acotada:** `frontend/src/app/core/auth/auth.service.spec.ts` no existe bajo ese nombre en el HEAD inspeccionado (fetch dio 404). Esto no prueba ausencia de *toda* la batería de tests Angular; revisar inventario de tests y rutas antes de implementar. No se ha buscado ni activado un proveedor real de recovery o MFA.

## 2. Evaluación de contratos existentes y límites

| Contrato | Estado comprobable estáticamente | No se debe inferir |
| --- | --- | --- |
| Login básico | Controller, DTO, BCrypt, rol y JWT presentes | Protección 429 distribuida probada, recuperación de acceso, MFA |
| Renovación | Ruta protegida con `Authorize`, servicio busca usuario por ID | Refresh token rotativo, revocación central de todo JWT, sesión en servidor |
| Auditoría de login | Registro en Controller y test de mocks sin tokens ficticios | Redacción completa de PII en toda la pila o fiabilidad de almacenamiento real |
| Logout Angular | Limpieza de token y campos en localStorage | Invalidación del JWT ya emitido si una copia sigue activa |
| Inactividad Angular | Límite local con actividad y renovación | Forzado central de todas las sesiones/dispositivos |
| Rol Angular | `esAdministrador` deriva de nombre de rol local | Grant server-side de plataforma o multiempresa por texto del rol |
| Contexto tenant | Selección requiere validación backend y evita respuesta tardía | Autorización global de todo endpoint por el valor cacheado |
| Seguridad de token | JWT firmado con JTI y TTL acotado | Denylist/rotación o revocación por JTI confirmada |
| Persistencia del JWT web | Se almacena token en localStorage según código actual | No constituye auditoría XSS/CSP, ni aprobación de ese diseño futuro |
| Recovery/passkeys/MFA | Necesidad y criterios de V5 documentados | Endpoint, proveedor, MFA enrolado o rollout real |

**Riesgos pendientes documentados, sin cambios en runtime:** almacenamiento de token en navegador requiere threat model XSS y controles; políticas de recuperación deben resistir enumeración/replay; logout local no equivale a revocación server-side; el controlador de renovación no es un endpoint de refresh rotativo; permisos de empresa se revalidan en backend y no en la signal `esAdministrador` de Angular.

## 3. Matriz de evidencias y pruebas requeridas en fases POSTERIORES

| Control | Evidencia observada ahora | Gap y prueba futura proporcional |
| --- | --- | --- |
| C01 login + | Controller/Service/DTO revisados | Test API HTTP+JWT positivo con usuario activo y auditoría real |
| C02 login - | Test de mocks T02 | HTTP 401 sin enumerar usuarios ni filtrar secretos/PII |
| C03 abuso | Test por reflexión T01 | HTTP 429 real y concurrencia distribuida conforme política |
| C04 rol inactivo | Validación de AuthService observada | Unit/integration con cambio del estado de usuario/rol |
| C05 tenant A/B | T03 EF InMemory | MySQL efímero y HTTP para acceso entre empresas |
| C06 JWT manipulado | Firma JTI/TTL en código | Pruebas de expirado/alterado/issuer/audience sin permiso |
| C07 revocación sesión | No se acreditó store/denylist | Persistencia/revocación real de token emitido; impedir replay |
| C08 rotación refresh | `/auth/renovar` autenticado; no refresh rotativo | Contrato futuro con expiración, one-time-use, aislamiento y error |
| C09 recovery abusiva | Endpoint no acreditado en fuente inspeccionada | Token único, TTL, no enumeración, repeticiones e intentos débiles |
| C10 MFA/passkeys | Proveedor no aprobado/identificado | Pruebas de challenge, replay, binding, error de proveedor y denegación |
| C11 sesión browser | AuthService y SessionActivityService | Playwright cierre/renovación, navegación sin token, multi-tab y pérdida de contexto |
| C12 privacidad/logs | Auditoría controller + mocks T02 | Correlación, retención y no filtración en logs reales y artifacts |
| C13 estabilidad | No hay prueba causal de este inventario | Rollback sin borrar datos, fallos intermedios y idempotencia por caso |

Los T01–T03 son **archivos de tests inspeccionados, no tests ejecutados ahora**. No atribuirles `PASS` de API, MySQL ni browser. Para cualquier cambio ejecutable posterior se deben correlacionar logs, jobs, SHA exacto y A0–A5, con un runner efímero autorizado.

## 4. Clasificación y no-regresión de esta entrega

- `requiere_dotnet=NO_APLICA`: no se altera C#, DLL, DTO ni tests.
- `requiere_node_angular=NO_APLICA`: se lee Angular, no se cambia JS/TS, UI ni routing.
- `requiere_mysql=NO_APLICA`: no se añade migración ni acceso DB.
- `requiere_docker=NO_APLICA`: no cambia build de imagen.
- `requiere_e2e=NO_APLICA`: no hay comportamiento de navegación modificado.
- `requiere_security=SI_DOCUMENTAL`: verificar límites JWT/BCrypt, tenant y pruebas negativas, sin declarar cobertura runtime.
- `requiere_scope_lock=SI`: verificar check `verify-solqaryn-project-scope` real SUCCESS en SHA del nuevo commit.
- **A0:** lectura de diff del inventario, fuentes SHA reales, no incluir secretos o datos personales. **A1:** cobertura explícita de contrato de login, renovación, revocación y recovery con positivos/negativos futuros. **A2:** matriz de amenazas JWT, abuso, tenant A/B, privacidad, roles y recuperación; no cambio de permisos. **A3:** once blobs revisados y discrepancias identificadas, no compilación requerida por documento. **A4:** publicación fast-forward a `dev`, SHA exacto, diff, Scope Lock, readback PLAN/COLA/BITACORA A:Z y rollback por `git revert` normal. **A5:** otra Task Javier distinta del autor; no QA/main/PROD por esta entrega.

**Restricciones:** no crear ramas/PR, Tasks, providers, endpoints, usuarios, tokens, secretos ni migraciones; no cambiar QA/main/PROD; no convertir esta lectura en `LISTO` de C03-01 completo ni habilitar M01.06/M02. La implementación y los negativos exigibles siguen pendientes en su fase autorizada.
