# SOLQARYN V5 FINAL — C03-01.M01.04: riesgos, supuestos y decisiones de autenticación/recuperación

**Alcance:** M01.04 DOCUMENTAL de C03-01. **Autor:** PRIMARY DEV 05 — JAVIER, TASK_ID `6adc2e55f47c81919f6374e561687076`. **Rama:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Autoridades:** [Standard V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN_MAESTRO/COLA/DAG_TECNICO](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), Skill canónica y fuentes Git de este documento.

**Resultado exigido:** registrar riesgos, supuestos y decisiones pendientes del ámbito login, renovación, revocación, recuperación y MFA/passkeys **únicamente si un proveedor es aprobado**. Este archivo no crea endpoints, modifica JWT, habilita MFA, ejecuta recuperación ni demuestra pruebas HTTP/MySQL. Los escenarios descritos son **obligaciones futuras**, no capacidades certificadas.

## 1. Fuentes verificadas antes de redactar

| Fuente real en dev | Blob SHA | Observación y límite |
| --- | --- | --- |
| `backend/src/API/Controllers/AuthController.cs` | `af437c869b4800a499c42aeff00347de3a3cc521` | `POST /auth/login` anónimo bajo `AuthLogin`, `POST /auth/renovar` con `[Authorize]`; no endpoint de recuperación observado |
| `backend/src/Application/Services/AuthService.cs` | `88d18217cd214cc00fca383983d775113fba0c87` | Comprueba BCrypt, estado de cuenta y rol activo; `RenovarAsync` obtiene usuario actual; no demuestra denylist global |
| `backend/src/Application/Interfaces/IAuthService.cs` | `986743c5ba7015c5c6bda564cc50781f3b26cd65` | Declara exclusivamente `LoginAsync` y `RenovarAsync`, no refresh token rotativo |
| `backend/src/Infrastructure/Services/JwtService.cs` | `9051fe6b4aac23e0ce3ebc684a2830c2cd2e6c9b` | JWT HMAC-SHA256 con JTI, expiración y claims de rol; JTI no implica revocación inmediata |
| `docs/v5/c03-01-m01-01-objetivo-autenticacion-recuperacion.md` | `a0836252f050d966efc9e803e233b51c4d02b080` | Define objetivos y NR01–NR16 como criterios futuros, NO implementación |

Estos son hashes de blobs de código/documento inspeccionados, **no resultados de tests nuevos**. Un cambio posterior obliga a releer código y a revalidar esta matriz.

## 2. Registro de riesgos abierto (no certificados como mitigados)

| Riesgo | Superficie | Condición de peligro | Control y prueba exigibles posteriormente | Estado |
| --- | --- | --- | --- | --- |
| R01 | `/auth/login` | Enumeración por diferencias en errores, tiempos o auditoría | HTTP positivo/negativo y revisión de respuesta/log sin secretos | ABIERTO |
| R02 | Login/rate limit | Ataque distribuido elude `AuthLogin` o agota almacenamiento | Pruebas de 429 real, cuotas, límites por IP/identidad y no bloqueo masivo | ABIERTO |
| R03 | JWT/JTI | Se presupone revocación inmediata por incluir JTI | Probar token emitido antes/después de cierre de sesión y revocación server-side | ABIERTO |
| R04 | `/auth/renovar` | Renovación se confunde con refresh token rotativo y reutilizable | Contrato diferenciado, autenticación, expiración y revalidación de cuenta/rol | ABIERTO |
| R05 | Tenant A/B | Claims de `EmpresaId`, host, header o rol del cliente se aceptan como autoridad | API negación BOLA/IDOR sin membresía en B, persistencia intacta | ABIERTO |
| R06 | Cuenta/rol | Desactivación/bloqueo no invalida todas las sesiones o permisos en consumidores | Prueba positiva de usuario vigente y negativa tras bloqueo/rol inactivo | ABIERTO |
| R07 | Recuperación futura | Respuestas o links revelan existencia de cuenta | Respuesta indistinguible, expiración, un solo uso, rate limit y auditoría | NO_IMPLEMENTADO |
| R08 | Recuperación futura | Token reutilizado o cambio de contraseña concurrente deja sesión vieja válida | Prueba de replay, CAS/rotación, invalidación y rollback seguro en DB efímera | NO_IMPLEMENTADO |
| R09 | MFA/passkey futura | Proveedor fallido habilita bypass/fallback débil o desafío replay | Autorización expresa, factor vinculado a identidad/operación, fail-closed | SIN_PROVEEDOR_APROBADO |
| R10 | Credenciales | Contraseña débil, filtrada o reutilizada supera políticas futuras | Validación server-side, evaluación contra política aprobada sin loguear secretos | ABIERTO |
| R11 | Logs/PII | Auditoría o excepción expone contraseñas, tokens, OTP o datos excesivos | Capturas/logs redactados y prueba negativa con marcadores de secreto | ABIERTO |
| R12 | Fallo parcial | Falla de auditoría, MySQL, SMTP o proveedor se anuncia éxito | Estado seguro, compensación aprobada y prueba dirigida de fallo aislado | ABIERTO |
| R13 | Frontend/estado | Sesión de B mostrada en A por cambio de empresa, caché o respuesta tardía | Negativas A/B de API + navegación y aislamiento de caché/cliente | ABIERTO |
| R14 | Gobernanza | Documentación se utiliza como autorización para QA/main/PROD o A5 autoaprobado | Control de TASK_ID distinto, 26 campos A:Z, Scope Lock y readback | BLOQUEADO_POR_GATES |

**Severidad:** R03, R05, R08, R09, R11 y R12 son candidatos críticos de privacidad o acceso; el texto no asigna PASS ni admite compensación mediante una revisión documental. El equipo que implemente deberá confirmar severidades y amenazas con evidencia runtime y control de cambios.

## 3. Supuestos que deben validarse, no verdades acreditadas

| ID | Supuesto de diseño | Cómo verificar o qué decisión bloquea |
| --- | --- | --- |
| S01 | El login usa rol relacional, cuenta activa y BCrypt como se observó en los blobs indicados | Releer código y tests del SHA de implementación; no extenderlo automáticamente a todos los consumidores |
| S02 | Un JWT con JTI no cuenta todavía con revocación criptográfica inmediata acreditada | Inventariar middleware, token store/denylist y pruebas de revocación antes de afirmar cobertura |
| S03 | `RenovarAsync` no equivale a refresh token de un solo uso | Documentar expiración, rotación y reuso antes de diseñar refresh |
| S04 | Recuperación de contraseña, correo, MFA y passkeys requieren decisión de proveedor | Ningún enrolamiento, coste, correo, secreto o servicio externo sin autorización posterior |
| S05 | Las membresías y roles pueden cambiar entre emisión del JWT y acceso | Verificar revalidación actual por API/servicio/repository y prueba cross-tenant |
| S06 | Los logs actuales pueden cumplir sanitización parcial, no universal | Validar trazas de controlador, excepciones, reverse proxy y CI sin producir PII |
| S07 | Las pruebas existentes de reflexión/InMemory son insuficientes para HTTP/MySQL real | Planificar suite dirigida .NET/Playwright/MySQL cuando el alcance ejecute código |
| S08 | Se necesita preservar datos/retención y jamás usar producción como fixture | Usar servicio efímero y procedimiento rollback seguro, sin tocar Aiven real |

## 4. Decisiones pendientes y responsables de aprobación

| Decisión | Pregunta concreta | Responsable de aprobación futura (rol) | Dictamen técnico previo (función por designar) | Criterio de aceptación de la decisión |
| --- | --- | --- | --- | --- |
| D01 | ¿Revocación por token, sesión o versión de credenciales, con qué almacenamiento? | Propietario SOLQARYN, autorización expresa pendiente | Seguridad y Backend/API | TTL, consistencia, fail-closed y pruebas de token previamente emitido |
| D02 | ¿Refresh token rotativo y almacenado seguro o solo renovación JWT autenticada? | Propietario SOLQARYN, autorización expresa pendiente | Backend/API y Frontend | Contrato, expiración, replay y compatibilidad API/frontend explícitos |
| D03 | ¿Canal y proveedor aprobado para recuperación? | Propietario SOLQARYN, aprobación expresa de proveedor y coste pendiente | Seguridad y Operaciones | Protección anti-enumeración, enlace de un uso, auditoría, abuso y coste autorizado |
| D04 | ¿MFA/passkeys para qué actores y qué proveedor? | Propietario SOLQARYN, aprobación expresa de alcance y proveedor pendiente | Seguridad y UX/accesibilidad | Política de enrolamiento/recuperación, fallback seguro, amenaza phishing/replay y presupuesto |
| D05 | ¿Política de credenciales y límites de abuso por identidad/IP/dispositivo? | Propietario SOLQARYN, aprobación expresa de política pendiente | Seguridad, Backend y UX/accesibilidad | Pruebas de bloqueo legítimo, límites distribuidos y accesibilidad |
| D06 | ¿Qué eventos de identidad son auditables y cuánto PII retener? | Propietario SOLQARYN, aprobación expresa de retención pendiente | Seguridad/privacidad y Operaciones | Esquema mínimo, redacción, retención, cifrado y respuesta ante falla de auditoría |
| D07 | ¿Revalidar membresía/rol por solicitud o versión/cache invalidable? | Propietario SOLQARYN, aprobación expresa de modelo pendiente | Seguridad, Backend y Persistencia | Denegación inmediata entre tenants A/B y después de revocación, bajo carga |
| D08 | ¿Rollback y compatibilidad de tokens activos ante cambio futuro? | Propietario SOLQARYN, autorización expresa de plan de cambio pendiente | Backend, Seguridad y Operaciones | Plan aditivo, contingencia, no comprometer datos ni mantener grants antiguos |

**Autoridades por rol, no personas nombradas:** el propietario de SOLQARYN es la autoridad de aprobación futura propuesta para D01–D08; las funciones de dictamen técnico se indican para asignación posterior, no son aprobadores efectivos ni permisos concedidos. Cada decisión requiere constancia expresa del propietario, alcance y evidencia causal antes de ejecutar. **Ninguna decisión aquí está aprobada ni implementada.** La autorización para introducir cualquier componente nuevo o proveedor será otro acto específico con pruebas y arquitectura, no una inferencia de este M01.04.

## 5. Aceptación documental y gates

**Verificación positiva documental:** las 5 fuentes están referenciadas con blob real, 14 riesgos con control verificable, 8 supuestos con criterio de verificación y 8 decisiones con responsable de aprobación futura. **Verificación negativa documental:** no se presenta endpoint inexistente como presente, JTI no se equipara a revocación, MFA/recuperación no autorizados, tenant B no se concede por claim, BITACORA A:Z y A5 independiente son obligatorios. Estos son oráculos textuales, no tests HTTP.

**DAG:** `C03-01.M01.04` aparece `VALIDADO`, gate `SI`; prerequisitos `C02-03.M01.01–05` + `C02-04.M01.01–05` en `LISTO`. `PLAN_MAESTRO!H437` y `COLA!E/G/O` son autoridades de estado/elegibilidad; la preparación documental no libera `C03-01.M01.06`, M02, IAM o producción.

**Clasificación:** `requiere_dotnet=NO`, `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO`, porque el diff propuesto contiene únicamente Markdown de análisis y CHANGELOG; `requiere_security=SI_DOCUMENTAL` por JWT/RBAC/membresía A/B, abuso, secretos y revocación; `requiere_scope_lock=SI` para commit documental a dev.

- **A0:** tras estado VALIDANDO, inspeccionar diff documental, hashes, formato/tabla, referencias, duplicados y ausencia de secretos; sin compilación irrelevante.
- **A1:** validar matriz R01–R14, S01–S08 y D01–D08 con casos documentales positivos/negativos; no declarar ejecución HTTP.
- **A2:** verificar explícitamente A/B, JWT/RBAC/BCrypt, recuperación, revocación y PII sin reducir amenazas.
- **A3:** verificar encadenamiento a Standard V5, doc M01.01, 10 prerequisitos del DAG y contratos fuente; runtime compilación NO_APLICA.
- **A4:** solo tras commit fast-forward dev, comprobar diff/SHA, check `verify-solqaryn-project-scope` SUCCESS exact-SHA y readback PLAN/COLA/BITACORA. No contar auxiliar SKIPPED como PASS.
- **A5:** evento `A5_PENDIENTE` para una **Task Javier** distinta de la autora; no promover a QA/main/PROD.

**Rollback:** git revert aditivo normal tras verificar HEAD y colisión, preservando otros cambios y BITACORA; nunca force-push/reset. Ningún secreto, usuario, proveedor, migración o imagen histórica fue alterado.
