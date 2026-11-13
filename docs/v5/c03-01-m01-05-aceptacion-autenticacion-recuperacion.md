# SOLQARYN V5 FINAL — C03-01.M01.05: criterios de aceptación observables de autenticación y recuperación

**Tipo:** preparación documental (M01.05). **Autor:** PRIMARY DEV 02 — JAVIER, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Repositorio:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Fecha:** 2026-11-13. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`) y Sheet institucional `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`.

**Salida atómica de M01.05:** fijar aceptación funcional y técnica **observable y falsable** para un alcance futuro de login, renovación, revocación y recuperación; MFA/passkeys solo con proveedor y orden aprobados. **No implementa ni acredita** esas funcionalidades futuras. El DAG técnico registra M01.05 `VALIDADO`, condicionado a los diez documentos `C02-03.M01.01–05` y `C02-04.M01.01–05`, comprobados `LISTO`. Este avance documental **no** habilita M01.06/M02 ni cambia seguridad, grants, QA, main, PROD o proveedores.

## 1. Regla de aceptación por niveles de evidencia

1. **Contrato actual comprobable por inspección:** rutas, servicios, JWT, atributos y pruebas unitarias existentes, vinculados a blobs inmutables. Su inspección **no equivale** a HTTP en ejecución, MySQL o proveedor real.
2. **Evidencia para implementación posterior:** caso positivo y negativo reproducibles, actor A/B, precondiciones, request/resultados observados, SHA exacto, job/log/test report y rollback. Si una superficie no existe todavía, resultado es `NO_IMPLEMENTADO`, nunca `PASS` por descripción.
3. **Seguridad no negociable:** denegación por defecto de identidades inválidas, permisos server-side basados en membresía vigente, anti-enumeración, ausencia de secretos/PII en respuestas y auditorías, revocación efectiva del permiso y cero escalamiento por `X-Empresa-Id` o claims autocontrolados.
4. **Gates de publicación:** A0–A4 pertinentes y Scope Lock del SHA exacto. A5 lo ejecuta una Task distinta de entre las otras cinco Javier autorizadas, con un positivo y un negativo (o equivalentes) y evento `BITACORA!A:Z` de 26 campos. LISTO DEV documental no autoriza promoción ni certifica runtime.

## 2. Matriz de aceptación futura (NO representa pruebas ejecutadas por M01.05)

| ID | Signo | Preparación y estímulo | Resultado observable exigido | Nivel de prueba requerido |
| --- | --- | --- | --- | --- |
| AC01 | + | Usuario vigente, BCrypt correcto y rol relacional activo: `POST /auth/login` | HTTP 200, respuesta y expiración válidas; auditoría sin secreto | HTTP API + backend .NET |
| AC02 | - | Usuario inexistente o contraseña inválida, con selector de empresa falsificado | HTTP 401 homogéneo, sin username, contraseña, token ni tenant en body/log | HTTP negativo + auditoría |
| AC03 | - | Cuenta bloqueada/desactivada, rol eliminado o inactivo | Login/renovación denegados; ninguna credencial nueva usable | Backend con repositorio/HTTP |
| AC04 | - | JWT adulterado, expirado, issuer/audience equivocados o firma inválida | Middleware rechaza acceso protegido; sin bypass de headers | HTTP/JWT firma y expiración |
| AC05 | + | JWT vigente, identidad existente y estado/rol aún válidos: `POST /auth/renovar` | HTTP 200, nueva expiración y permisos actuales, sin conceder otra empresa | Backend/HTTP con control de rol |
| AC06 | - | Renovación con claim `id` ausente, no numérico, desbordado o usuario desaparecido | HTTP 401; no se llama servicio con ID inválido | Controlador + HTTP negativo |
| AC07 | - | JWT emitido antes de desactivar membresía en A; intento posterior en A y B | Acceso a A revocado; B siempre denegado sin membresía | HTTP + MySQL efímero A/B |
| AC08 | - | Usuario A envía `X-Empresa-Id=B` o rol/claim manipulado | Denegación 403/404, sin lectura ni escritura en B | API/DB tenant + RBAC |
| AC09 | - | Reintentos masivos a `/auth/login` desde identidad de cliente controlada | Se observa 429 y límite real, sin confiabilidad ciega en IP/proxy manipulable | Rate limiter HTTP/E2E dirigido |
| AC10 | - | Username contiene marcador tipo secreto, enlace de reset o token | Auditoría y respuesta no reflejan marcador; sin PII excesiva | Test positivo/negativo de logs + proveedor cuando aplique |
| AC11 | - | Solicitud futura de recuperación para usuario existente/inexistente | Respuesta indistinguible, límites antiabuso, sin enumeración | FUTURO: HTTP + SMTP/servicio autorizado |
| AC12 | - | Token de recuperación futuro vencido, reutilizado, para otra cuenta o en carrera | Cero cambio de contraseña no autorizado; TTL y un solo uso transaccional | FUTURO: MySQL/HTTP y concurrencia |
| AC13 | - | Contraseña débil, reutilizada o fuera de política aprobada | Rechazo uniforme, no exposición ni log de contraseña | FUTURO: backend + reglas aprobadas |
| AC14 | - | MFA/passkey futuro con desafío inválido, factor ajeno, replay o proveedor caído | Denegación sin bypass; ningún factor se activa sin autorización | FUTURO: proveedor y seguridad |
| AC15 | control | Falla auditora/transaccional o rollback del cambio futuro | Sin éxito ficticio, datos legítimos intactos, revert seguro y observabilidad | Integración y reversión proporcional |
| AC16 | control | A5 con mismo TASK_ID autor; evento BITACORA incompleto; check SKIPPED presentado como PASS | Rechazar cierre/promoción indebida y conservar historia de 26 campos | Auditoría institucional por SHA |

**Criterio de aprobación del lote FUTURO:** todos los AC aplicables ejecutados con evidencia causal; los AC de funcionalidad aún no implementada permanecen `NO_IMPLEMENTADO` hasta el entregable que los incorpora, no se satisfacen por esta planificación. Nunca usar fixtures InMemory como sustituto de MySQL/HTTP cuando el AC lo requiere.

## 3. Evidencia técnica verificada en el código existente (solo lectura, no certificación nueva)

| Fuente en `dev` | Blob SHA observado | Afirmación limitada |
| --- | --- | --- |
| `backend/src/API/Controllers/AuthController.cs` | `af437c869b4800a499c42aeff00347de3a3cc521` | Login anónimo con `AuthLogin` y renovar protegido; 200/401/auditoría |
| `backend/src/Application/Services/AuthService.cs` | `88d18217cd214cc00fca383983d775113fba0c87` | BCrypt, cuenta activa/no bloqueada, rol activo en login/renovación |
| `backend/src/Application/Interfaces/IAuthService.cs` | `986743c5ba7015c5c6bda564cc50781f3b26cd65` | Solo `LoginAsync` y `RenovarAsync`; no se acredita API de recovery |
| `backend/src/Infrastructure/Services/JwtService.cs` | `9051fe6b4aac23e0ce3ebc684a2830c2cd2e6c9b` | HS256, JTI, roles, TTL 35–720 min; JTI no demuestra denylist |
| `backend/src/API/Program.cs` | `01425cff56eb13e12022b1b81a2f322d17fd1fff` | JWT valida issuer/audience/lifetime/signature; política `AuthLogin` define 429; no prueba 429 emitido |
| `backend/tests/Solqaryn.Tests/I8005CAuthTokenAntifuerzaBrutaDelimitacionTests.cs` | `14b4d55992e0f0cc5866d3ef6b6f232722c318fa` | Refleja atributos, no tráfico real |
| `backend/tests/Solqaryn.Tests/I8002DRenovacionSinIdentidadValidaTests.cs` | `305439b19ea6e03cca9476ff4ee00843ae454d1a` | Mock/controlador niega `id` inválido; no middleware JWT |
| `backend/tests/Solqaryn.Tests/I8002ERecuperacionSinEnumeracionTests.cs` | `0dfbb4601f0754ea03c1abbe479a95d416ca9589` | Mock de login no enumera usuarios/tenant, no endpoint de recovery |
| `backend/tests/Solqaryn.Tests/I8009ELoginAuditoriaSinSecretoTests.cs` | `7050551eee56d658b455717848259dfd80d725f6` | Marcadores sintéticos no filtrados en mock/auditoría |
| `backend/tests/Solqaryn.Tests/P2013DJwtMembresiaRevocadaTests.cs` | `ee4647f49c5045d217200e1b92c7adc668500f3d` | Revocación A/B con EF InMemory; no prueba criptográfica HTTP ni MySQL |

**Hallazgos y dependencias abiertas:** la renovación observada usa JWT autenticado, **no** acredita refresh token rotativo; el JTI no acredita invalidación central inmediata; la inspección de `AuthController`/`IAuthService` no demuestra recuperación/reset; la mera configuración 429 no sustituye prueba real; cobertura InMemory no sustituye aislamiento transaccional MySQL. No se modifican endpoints, JWT, tablas, secretos ni proveedores para producir este documento.

## 4. Validación de esta entrega DOCUMENTAL M01.05 y rollback

| Puerta | Alcance exigible a este documento |
| --- | --- |
| A0 | Revisar diff, Markdown, 16 AC únicos, rutas y hashes, secretos/PII literales, consistencia V5, ausencia de cambios ejecutables |
| A1 | Casos positivos AC01/AC05 y negativos AC02/AC06/AC07/AC08/AC11–14 con estímulo/oráculo precisos; no atribuir ejecución |
| A2 | Revisar fail-closed, JWT/RBAC/tenant A/B, BCrypt, recuperación, abuso, PII, no secretos; distinguir propuesta de sistema real |
| A3 | Trazabilidad hacia 10 blobs reales y las diez dependencias DAG LISTO, sin cambiar consumidores; compilaciones `NO_APLICA` |
| A4 | Tras commit FF en dev, comprobar SHA, diff, Scope Lock `completed/success` exacto y readback PLAN/COLA/BITACORA |
| A5 | `PENDIENTE` de otro TASK_ID Javier distinto del autor; verificar al menos caso positivo y negativo documental |

`requiere_dotnet=NO_APLICA`; `requiere_node_angular=NO_APLICA`; `requiere_mysql=NO_APLICA`; `requiere_docker=NO_APLICA`; `requiere_e2e=NO_APLICA`, porque sólo se añade este artefacto documental. `requiere_security=SI_INSPECCION_DOCUMENTAL`; `requiere_scope_lock=SI`. Un check técnico ajeno no es validación de funcionalidad presente ni se espera como gate de este archivo si la gobernanza no lo exige.

**Rollback:** `git revert` normal del commit documental, sin force-push ni cambios a datos. Preservar eventos BITACORA y actualizar nueva evidencia si se revierte. No declarar A5_PASS por autoría propia; no habilitar M01.06, M02, IAM productivo ni promoción a QA/main/PROD.
