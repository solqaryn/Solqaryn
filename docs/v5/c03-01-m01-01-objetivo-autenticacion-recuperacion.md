# SOLQARYN V5 FINAL — C03-01.M01.01: objetivo, no-alcance y no-regresión de autenticación y recuperación

**Tipo:** preparación documental M01.01, sin cambio de código ejecutable. **Autor:** PRIMARY DEV 06 — JAVIER, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Proyecto:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Fuentes:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`) y PLAN_MAESTRO/COLA/DAG_TECNICO (Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`). DAG VALIDADO para este ID; predecesoras `C02-03.M01.01–05` y `C02-04.M01.01–05` verificadas LISTO. El cierre provisional de M01.01 **no** habilita M01.06, M02, IAM operativo ni QA/main/PROD.

## 1. Objetivo del alcance futuro y salida atómica de esta microtarea

Definir de forma verificable el contrato futuro de **inicio, renovación, revocación y recuperación de acceso** para cuentas autorizadas. El resultado de fase C03-01 exige login, refresh, revocación y recuperación, con passkeys o MFA **solo cuando exista proveedor y autorización específicos**. La evidencia futura mínima incluye sesión revocada, recuperación abusiva y credencial débil; esta microtarea solo fija objetivos, exclusiones y no-regresión, NO certifica dichos escenarios ni reemplaza una implementación.

El contrato objetivo debe vincular identidad y autorización a estado de cuenta vigente, membresía empresarial activa y ámbito concreto, con revocación reevaluable en servidor. El navegador no puede promover por sí solo un JWT, claim de rol, empresa, host, cabecera o dispositivo a permiso en otro tenant. Toda operación de autenticación crítica requiere errores seguros, límites de abuso, trazabilidad sin credenciales y capacidad de rollback. Ningún supuesto en este texto habilita MFA, passkeys, recuperación por correo o revocación centralizada si no existe implementación acreditada.

## 2. Evidencia de código real, observada estáticamente

| Archivo (blob Git de dev) | Contrato observado | Brecha o límite |
| --- | --- | --- |
| `backend/src/API/Controllers/AuthController.cs` — `af437c869b4800a499c42aeff00347de3a3cc521` | `POST /auth/login` anónimo con política `AuthLogin`; `POST /auth/renovar` exige `[Authorize]`; auditoría de intento fallido y acceso exitoso | No se acredita endpoint de recuperación, logout global, HTTP 429 real ni revocación de JWT emitido |
| `backend/src/Application/Services/AuthService.cs` — `88d18217cd214cc00fca383983d775113fba0c87` | Comprueba BCrypt, cuenta activa/no bloqueada, rol relacional y genera token; `RenovarAsync` recarga usuario | No demuestra invalidación inmediata de cada token emitido, MFA ni recovery |
| `backend/src/Application/Interfaces/IAuthService.cs` — `986743c5ba7015c5c6bda564cc50781f3b26cd65` | Solo declara `LoginAsync` y `RenovarAsync` | No confundir renovar JWT autenticado con refresh token rotativo |
| `backend/src/Infrastructure/Services/JwtService.cs` — `9051fe6b4aac23e0ce3ebc684a2830c2cd2e6c9b` | Token JWT firmado HS256 con `id`, rol, JTI, exp; TTL configurado y acotado | La presencia de JTI no certifica denylist, revocación server-side ni permisos multiempresa |
| `backend/tests/Solqaryn.Tests/I8005CAuthTokenAntifuerzaBrutaDelimitacionTests.cs` — `14b4d55992e0f0cc5866d3ef6b6f232722c318fa` | Comprueba atributos de rutas y rate limit declarado | Test de reflexión, no ejecución real de 429 ni ataque distribuido |
| `backend/tests/Solqaryn.Tests/I8009ELoginAuditoriaSinSecretoTests.cs` — `7050551eee56d658b455717848259dfd80d725f6` | Comprueba con mocks que fallos de login no filtran marcadores de secreto en respuesta/auditoría | No prueba transporte HTTP, MySQL ni logs de proveedores |
| `backend/tests/Solqaryn.Tests/P2013DJwtMembresiaRevocadaTests.cs` — `ee4647f49c5045d217200e1b92c7adc668500f3d` | Prueba de revocación de membresía y acceso A/B con EF InMemory | No es revocación global del token JWT ni MySQL/HTTP criptográfico |

**Precisión:** los hashes anteriores son blobs de archivos inspeccionados, no resultados de una CI nueva ni garantía universal del runtime. Releer fuentes y permisos antes de implementación posterior.

## 3. No-alcance de M01.01

No crear/modificar `AuthController`, `AuthService`, JWT, BCrypt, middleware, políticas de autenticación, RBAC, tablas, migraciones, refresh tokens, secretos, correos, proveedores MFA/passkeys, dominios, sesiones reales, usuarios, configuración productiva ni UI. No prometer restablecimiento de contraseña cuando el endpoint no está acreditado. No ejecutar ataques de fuerza bruta contra servicios vivos. No escribir sobre QA/main/PROD ni atribuir A5 a la Task autora. No habilitar M01.06/M02, alterar DAG ni insertar microtareas nuevas.

## 4. Contrato de no-regresión falsable para implementación FUTURA

| ID | Signo | Estímulo y superficie | Oráculo que deberá demostrar una prueba causal |
| --- | --- | --- | --- |
| NR01 | + | Usuario activo con BCrypt válido y rol relacional vigente hace login | 200, token ligado a identidad vigente, expiración y auditoría saneada; API real |
| NR02 | + | JWT vigente y usuario autorizado solicita renovar | Respuesta autenticada, rol/estado revalidados y no ampliación implícita de permisos |
| NR03 | - | Usuario inválido o contraseña incorrecta | 401 genérico, sin enumeración ni filtración de input/contraseña |
| NR04 | - | Cuenta bloqueada/inactiva o rol inactivo intenta login/renovar | Denegación segura, sin emisión o renovación de credencial válida |
| NR05 | - | Token expirado, manipulado o con issuer/audience inválidos | Rechazo en middleware real; ninguna sesión restablecida por cabecera propia |
| NR06 | - | Token anterior a revocación efectiva de sesión/membresía intenta acceso sensible | Denegación del permiso actual; distinguir revocación de membresía de revocación criptográfica del token |
| NR07 | - | Reintentos masivos/distribuidos de `/auth/login` | Respuestas 429/rate limit observado y protección anti-enumeración; no basta inspeccionar atributo |
| NR08 | - | Cambio A→B con JWT legítimo, pero sin membresía válida en B | 403/404; sin datos ni efectos en B, sin aceptar selector cliente como grant |
| NR09 | - | Solicitud de recuperación con usuario inexistente o repetida | Respuesta indistinguible cuando corresponda, token de un solo uso, expiración, auditoría y no enumeración; mecanismo FUTURO |
| NR10 | - | Recovery token vencido/reutilizado o usado para identidad distinta | Denegación y cero rotaciones exitosas; prueba transaccional FUTURA |
| NR11 | - | Contraseña débil, reutilización prohibida o credencial expuesta | Rechazo conforme política aprobada y sin log/captura de secreto; prueba FUTURA |
| NR12 | - | MFA/passkey no disponible, desafío inválido, replay o factor de otro usuario | Fallo cerrado, sin bypass por proveedor caído; SOLO si proveedor aprobado se implementa |
| NR13 | - | Doble envío/retry concurrente de rotación o recuperación | Efecto único idempotente y estado transaccional íntegro; MySQL efímero FUTURO |
| NR14 | - | Frontend oculta acción, pero cliente llama endpoint sin rol | Backend deniega por estado/rol/membresía real, no por UI |
| NR15 | control | Error de persistencia/auditoría de operación crítica | No éxito ficticio, sin secretos/PII en logs, rollback proporcional |
| NR16 | control | Auditor A5 es la misma Task autora o evento BITACORA tiene menos de 26 campos | Rechazo de autoprobación y evento incompleto; BITACORA A:Z |

NR01–NR16 son **criterios futuros**, no tests ejecutados por esta M01.01. La fase M01.02–05 deberá completar actores, fuentes, riesgos y aceptación; las fases M02–M05 precisarán API, almacenamiento, amenaza y suites con pruebas positivas/negativas reales.

## 5. Clasificación CI y entrega documental

| `requiere_*` | M01.01 | Razón |
| --- | --- | --- |
| `requiere_dotnet` | NO_APLICA | Solo documento, ningún C# ni test alterado |
| `requiere_node_angular` | NO_APLICA | Ninguna UI/script/npm cambia |
| `requiere_mysql` | NO_APLICA | No modifica persistencia ni migraciones |
| `requiere_docker` | NO_APLICA | No cambia imagen ni infraestructura |
| `requiere_e2e` | NO_APLICA | No modifica navegación/UX |
| `requiere_security` | SI, inspección documental | JWT, BCrypt, rate limit, membresía/tenancy y no-regresión delimitados; no fingir pruebas runtime |
| `requiere_scope_lock` | SI | Validar `verify-solqaryn-project-scope` SUCCESS para SHA del commit |

A0: calidad estática y diff/secretos/contradicciones del documento. A1: objetivo y casos positivos/negativos documentados con oráculos falsables. A2: límites de seguridad, revocación, abuso, tenant A/B y no secrecía, sin cambios ejecutables. A3: contratos/consumidores de siete fuentes reales concordantes; .NET/Node/MySQL/E2E NO_APLICA razonado. A4: commit fast-forward en `dev`, SHA/Scope Lock exactos, readback PLAN/COLA/BITACORA y rollback `git revert` normal. A5: pendiente de otra de las cinco Tasks Javier, sin promoción automática a QA/main/PROD.
