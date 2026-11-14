# SOLQARYN V5 FINAL — C03-03.M01.01: objetivo, exclusiones y no-regresión de auditoría de seguridad

**Tipo:** preparación DOCUMENTAL M01.01; no agrega runtime, base de datos, migraciones, permisos o telemetría productiva. **Autor:** PRIMARY DEV 04 — JAVIER, TASK_ID \`6aeacb7820ac8191b7d768a6f33dfa09\`. **Ámbito:** \`solqaryn/Solqaryn@dev\`, PROJECT_SCOPE_LOCK=STRICT. **Fecha:** 2026-11-13.

**Fuentes de autoridad:** [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN_MAESTRO / COLA / DAG_TECNICO / BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), Skill canónica y Scope Lock. El DAG clasifica este ID \`PREPARACION_DOC_CONDICIONAL\`, \`VALIDADO\`, gate \`SI\`; diez antecedentes de \`C02-04.M01.01–05\` y \`C03-01.M01.01–05\` estaban LISTO antes del claim. La preparación NO libera M01.06, M02, IAM, JWT/RBAC de producción, qa/main/PROD ni cambios de infraestructura.

## 1. Objetivo verificable de la futura capacidad

Definir el contrato de una auditoría de seguridad **server-side y por ámbito autorizado** que registre intentos permitidos y denegados, cambios críticos y accesos excepcionales, con evento estructurado (quién, qué, resultado, cuándo, recurso, tenant verificado, correlación saneada, motivo permitido), sin copiar credenciales, JWT, claves, secretos de recuperación ni PII no indispensable. Permitir a un administrador autorizado consultar exclusivamente su alcance empresarial; ningún usuario de A debe observar eventos, identidades, IP, valores anteriores/nuevos o correlaciones privadas de B.

La implementación futura debe distinguir **registro de mejor esfuerzo** y **registro obligatorio en transacción**. Donde una decisión legal o de seguridad requiera evidencia durable, fallar cerrado o revertir si no se puede acreditar la auditoría; una excepción silenciada no es una garantía. El identificador de correlación facilita trazabilidad, pero NO representa autorización, idempotencia ni identidad auténtica. La retención, control de borrado/exportación, acceso excepcional y alertas requieren política explícita y pruebas antes de ser declarados efectivos.

**Resultado de ESTA microtarea:** objetivos, no-alcances, fronteras de confianza y matriz falsable de no-regresión. No se acreditan nuevas capacidades ni se cambia comportamiento real de auditoría.

## 2. Código existente cotejado — evidencia estática, NO ejecución

| Archivo y blob Git inspeccionado | Observación acotada | Brecha a verificar después |
| --- | --- | --- |
| \`backend/src/API/Controllers/AuditoriaController.cs\` — \`82057f2acc571263ed7ed530eae0f81359b2f81a\` | \`[Authorize]\`, permiso \`Auditoria.Ver\`, \`EsAdministrador\`, GET de auditoría | Aislamiento A/B por HTTP y pertenencia de resultados |
| \`backend/src/Application/Services/AuditoriaService.cs\` — \`def2c1f7bc58e23f98a1065fcdf0963322f3705c\` | \`RegistrarAsync\` captura errores; \`RegistrarEstrictoAsync\` los propaga; \`EmpresaId\` desde \`VerifiedCommercialTenant\` u otras marcas de servidor | Durabilidad selectiva, datos serializados/redacción universal |
| \`backend/src/Domain/Entities/RegistroAuditoria.cs\` — \`d669ac397a78ba02a5ba19e2cfb4bc9c7a115462\` | Modelo con actor, empresa nullable, antes/después, motivo, IP, user-agent y correlación | TTL, cifrado, privacidad y query scoping no se infieren del modelo |
| \`backend/src/Infrastructure/Repositories/AuditoriaRepository.cs\` — \`8fc9417b88da91ad1a5777ea77d9e31756230384\` | Filtros visibles de usuario, módulo, fechas, resultado y texto, con paginación | No muestra predicado explícito EmpresaId: comprobar filtros EF globales y acceso real, sin asumir fuga |
| \`backend/src/API/Middleware/CorrelationIdMiddleware.cs\` — \`3b76dca41bb4bc0b44f89c2a4664c79907f9f242\` | Normaliza cabecera \`X-Correlation-ID\` a máximo 64 caracteres ASCII seguros o GUID | No demuestra propagación íntegra entre servicios ni evita duplicados por sí sola |

Estos blobs describen archivos observados, no son evidencia de HTTP, MySQL 8.4, proveedor, retención temporal real ni análisis de secretos dinámico. Antes de implementar se deben refrescar HEAD, blobs, contratos, amenazas y resultados CI del SHA candidato.

## 3. No-alcance y límites de confianza

- No crear tablas, migraciones EF, políticas de retención, logs productivos, dashboards, exportadores, índices, roles, endpoints, JWT, secretos, emails, webhooks, permisos o acceso excepcional.
- No registrar tokens, contraseñas, valores de credencial, payloads de recuperación, cookies, tokens SMTP, cadenas de conexión ni \`Authorization\` en descripciones, motivos, valores serializados, errores o logs de excepciones. No asumir que la implementación actual ya los redacta todos.
- No tratar \`X-Empresa-Id\`, IP, user-agent, correlation ID, claims manipulados o una vista Angular como grant. La resolución de tenant y permiso se valida en backend y con membresía vigente.
- No hacer auditoría global accesible a administradores de una sola empresa por inferencia. La situación \`EmpresaId=null\` y accesos de plataforma requiere ámbito explícito y mínimo privilegio, nunca un fallback de acceso.
- No certificar funcionamiento o conformidad regulatoria por inspección estática. No probar contra datos reales, PROD, qa/main, VPS, DNS o servicios con coste.

## 4. Matriz de no-regresión falsable para implementación posterior (NO ejecutada aquí)

| ID | Signo | Estímulo / actor | Oráculo verificable exigido a futuro |
| --- | --- | --- | --- |
| AR01 | + | Administrador A con \`Auditoria.Ver\` y membresía vigente consulta registros de A | HTTP 200, solo filas A; filtros, paginación y auditoría causal |
| AR02 | - | Miembro no administrador intenta \`GET /auditoria\` | 403, ningún evento o metadato sensible |
| AR03 | - | Administrador A fuerza identificador, filtro o header de tenant B | No devuelve datos, PII ni correlaciones B; HTTP + MySQL A/B |
| AR04 | + | Cambio crítico autorizado sobre recurso A | Evento con actor/ámbito/recurso/resultado/tiempo y correlación válida en DB |
| AR05 | - | JWT inválido o membresía revocada realiza cambio crítico | Denegación antes del efecto, sin filtración ni auditoría que otorgue permisos |
| AR06 | - | Header correlación contiene CRLF, control o longitud mayor a 64 | Generar ID saneado; no inyección de log, header de respuesta seguro |
| AR07 | + | Correlation ID válido en HTTP y evento duradero | Id congruente y verificable, sin confundirlo con clave idempotente |
| AR08 | - | Login fallido o payload de cambio con contraseña/JWT/secreto | No aparece en old/new/error/motivo, respuesta, logs ni export |
| AR09 | - | Falla MySQL en flujo que exige auditoría obligatoria | Rollback o denegación, sin declarar éxito durable ni efecto parcial |
| AR10 | + | Registro crítico con variante estricta y DB disponible | Commit auditable comprobado con pruebas causales transaccionales |
| AR11 | - | Reintento concurrente de la misma operación lógica | Semántica de idempotencia explícita; no duplicar efecto ni confundir correlación |
| AR12 | - | Export/consulta masiva no autorizada | RBAC + ámbito tenant + paginación/límites, no filas ajenas |
| AR13 | - | Intento de acceso excepcional sin autorización, TTL o evidencia de reautenticación | Rechazo por defecto; no bypass por headers/rol inventado |
| AR14 | + | Política de retención aprobada y aplicada | Registro verificable de plazo, protección, borrado/archivo seguro y lectura, sin presuponer política actual |

Cada futura prueba deberá precisar precondiciones A/B, actor y rol, estímulo, request/DB, resultado positivo/negativo, SHA exacto, logs/artefactos desensibilizados y rollback. Los escenarios AR03, AR09, AR10 y AR14 necesitan entorno real apropiado; ni EF InMemory ni un documento constituyen sustitutos.

## 5. Aceptación documental propia (A0–A3) y cierre futuro

- **A0 estática:** el diff de esta entrega queda limitado a este Markdown y CHANGELOG aditivo; revisar enlaces, fuentes, interpolaciones, credenciales accidentales y no modificación de Skill/Scope Lock. No se instala nada.
- **A1 documental positiva:** existe objetivo, resultado, no-alcance y 14 casos falsables (AR01–AR14). **Negativa:** no se afirma que pruebas HTTP/MySQL/retención se hayan ejecutado; la observación de archivo nunca equivale a éxito de runtime.
- **A2 documental seguridad:** distinguir JWT/RBAC/tenant verificado, denegación, correlación no confiable, minimización PII, secretos y fallos de persistencia. **Negativa:** no habilitar auditoría global, privilegios excepcionales ni lectura B desde A.
- **A3 documental integración:** contrastar V5, DAG validado, las 10 predecesoras, interfaces de código y contratos M01.03/M01.05. Para ESTA entrega: .NET, Angular/Node build, MySQL, Docker y E2E = \`NO_APLICA\` motivado, pues no hay código ejecutable. Scope Lock sí aplica al commit documental.
- **A4 (solo tras publicar):** readback de blobs y diff del SHA exacto en dev; check-run \`verify-solqaryn-project-scope\` \`completed/success\` y \`head_sha\` coincidente; PLAN/COLA/BITACORA A:Z y rollback \`git revert\` normal, sin reescribir historia.
- **A5:** evento \`A5_PENDIENTE\` para otra de las cinco Tasks JAVIER del allowlist; nunca auto-A5_PASS ni promoción automática a qa/main/PROD.

**Fronteras de salida:** M01.01 termina con especificación verificable, NO con auditoría técnica desplegada. M01.06/M02, permisos IAM, retention policy real y producción siguen sujetos a sus propios gates y permisos.
