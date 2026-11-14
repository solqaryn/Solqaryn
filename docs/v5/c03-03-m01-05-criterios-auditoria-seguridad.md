# SOLQARYN V5 FINAL — C03-03.M01.05: aceptación observable de auditoría de seguridad

**Entrega:** preparación documental M01.05; **autor** PRIMARY DEV 02 — JAVIER, Task `6af16bb9492c8191b8e8c7d606f11507`. **Repo:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT. **Autoridad:** Standard V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU` (PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA). **Fecha:** 2026-11-13.

**Salida de esta microtarea:** definir criterios funcionales y técnicos observables para la auditoría FUTURA de autorización, denegación, cambios críticos y accesos excepcionales sin secretos, con logs estructurados, correlación, retención y no exposición. **No implementa** un nuevo sistema de auditoría, migraciones, permisos, política de retención ni acceso excepcional. El DAG `C03-03.M01.05` está VALIDADO para preparación M01 con los diez predecesores `C02-04.M01.01–05` y `C03-01.M01.01–05` en LISTO. Nunca libera M01.06/M02, privilegios IAM ni QA/main/PROD.

## 1. Niveles de prueba: sin equiparar diseño y ejecución

- **Observación actual:** lectura de código, atributos, SHA de blobs, contratos y pruebas existentes. No certifica HTTP, MySQL, SMTP, producción, alertas, retención temporal real ni aislamiento global.
- **Aceptación futura:** cada caso debe incluir precondiciones A/B y roles, estímulo/identificador autorizado, resultado o denegación, auditoría correlacionada y datos no divulgados; artefactos de ejecución CI o proveedor relevante en SHA exacto, rollback y verificación negativa.
- **Límite de confianza:** un header `X-Empresa-Id`, dirección IP, user-agent o JTI no otorgan permisos; la consulta de auditorías exige autorización backend por permiso, rol y ámbito de tenant efectivamente verificado. Un administrador empresarial no se convierte por inferencia en auditor global.
- **Auditoría crítica:** donde el flujo exija persistencia obligatoria, fallar de modo cerrado o transaccional y demostrarlo; diferenciar explícitamente `RegistrarAsync` (captura excepciones y continúa) de `RegistrarEstrictoAsync` (propaga fallos). No llamar durabilidad a un log best-effort.

## 2. Matriz falsable de aceptación posterior (16 casos; NO ejecutados por esta entrega)

| ID | Signo | Estímulo, actor y alcance | Oráculo observable y evidencia exigida |
| --- | --- | --- | --- |
| AU01 | + | Administrador autorizado de empresa A con permiso `Auditoria.Ver` consulta eventos A | Solo registros del ámbito autorizado A, paginación estable, acceso registrado y ninguna exposición de B; HTTP+MySQL |
| AU02 | - | Usuario autenticado sin permiso o no administrador llama `GET /auditoria` | 403/404 sin registros; backend deniega aunque frontend muestre botón; HTTP negativo |
| AU03 | - | Auditor de A manipula tenant/header/filtro para consultar eventos B | Denegación o resultados vacíos, sin correlaciones, PII ni registros de B; prueba multiempresa HTTP+DB |
| AU04 | + | Acción crítica autorizada modifica un recurso dentro de A | Evento durable con usuario, tenant verificado, operación, fecha, resultado y correlación saneada; prueba integración |
| AU05 | - | Acción crítica denegada por RBAC/membresía revocada | Operación no sucede; evidencia de rechazo cuando política lo exija, sin datos sensibles ni aumento de privilegios |
| AU06 | - | Solicitud con `X-Correlation-ID` con CRLF, longitud excesiva o caracteres de control | Reemplazo por valor seguro, ninguna inyección de log, correlación coherente; middleware/HTTP |
| AU07 | + | Correlación segura y válida acompaña flujo HTTP y persistencia | CorrelationId consistente en respuesta, contexto y registro; no se confunde ID de trazabilidad con autorización |
| AU08 | - | Payload contiene contraseña, JWT, refresh/recovery token, credencial SMTP o PII sensible | Redacción/minimización antes de serializar old/new/motivo/error; inspección de evento almacenado y logs |
| AU09 | - | `RegistrarAsync` falla en persistencia durante operación que requiere auditoría estricta | No declarar auditoría garantizada; operación crítica usa variante estricta/rollback, prueba inyectando fallo |
| AU10 | + | `RegistrarEstrictoAsync` registra cambio crítico con DB disponible | Evento persistido y transacción causal demostrada, sin duplicado por retry |
| AU11 | - | Dos reintentos paralelos del mismo evento crítico con clave/correlación lógica | Un efecto auditable según contrato de idempotencia aprobado; no confundir correlation ID con llave única |
| AU12 | - | Usuario solicita exportación masiva o filtra por fechas/usuario ajenos | Autorización de scope, límites/paginación, respuesta 4xx o conjunto restringido, sin PII ajena |
| AU13 | - | Actor intenta activar acceso excepcional sin MFA, reautenticación, motivo, TTL o aprobación | Rechazo por defecto; sin privilegio concedido ni ruta alternativa por header; FUTURO, no implementado aquí |
| AU14 | + | Acceso excepcional FUTURO aprobado, limitado y vigente | Grant acotado por identidad, propósito, recurso, expiración y evento auditado; control técnico con caducidad real |
| AU15 | - | Retención expira o llega solicitud válida de minimización/borrado | Política legal aprobada, integridad y minimización verificables, sin purga destructiva de registros de cumplimiento; FUTURO |
| AU16 | control | Check Scope Lock SKIPPED/pendiente, A5 autor propio o BITACORA de menos de 26 campos | No certificar cierre indebido, preservar evento/corrección y bloquear promoción; verificación institucional |

Los AU01–AU16 son **oráculos de aceptación para entregas futuras**, no pruebas de runtime realizadas en M01.05. Un resultado `NO_IMPLEMENTADO`, `NO_PROBADO` o `RIESGO_ABIERTO` no se convierte en PASS por documentación.

## 3. Contratos reales observados y riesgos técnicos delimitados

| Fuente real leída en dev | Blob SHA | Evidencia limitada |
| --- | --- | --- |
| `backend/src/API/Controllers/AuditoriaController.cs` | `82057f2acc571263ed7ed530eae0f81359b2f81a` | `[Authorize]`, permiso Auditoria.Ver, GET con guardia adicional `EsAdministrador`; requiere verificar scope A/B en ejecución |
| `backend/src/Application/Services/AuditoriaService.cs` | `def2c1f7bc58e23f98a1065fcdf0963322f3705c` | `RegistrarAsync` absorbe error y loguea; `RegistrarEstrictoAsync` propaga; atributos serializados; empresa desde marcas server-side |
| `backend/src/Domain/Entities/RegistroAuditoria.cs` | `d669ac397a78ba02a5ba19e2cfb4bc9c7a115462` | Modelo contiene empresa nullable, identidad, valores anteriores/nuevos, motivo, IP, user agent, correlación, error |
| `backend/src/Infrastructure/Repositories/AuditoriaRepository.cs` | `8fc9417b88da91ad1a5777ea77d9e31756230384` | Query EF con filtros y paginación, sin filtro explícito por EmpresaId en este método; revisar filtros globales/consumidor antes de afirmar fuga |
| `backend/src/API/Middleware/CorrelationIdMiddleware.cs` | `3b76dca41bb4bc0b44f89c2a4664c79907f9f242` | Valida caracteres ASCII y máximo 64; sustituye ID inválido; establece TraceIdentifier; no garantiza correlación durable |
| `backend/src/API/Middleware/ExceptionHandlingMiddleware.cs` | `baee40143e4e48b8df200e77892606af96d60b7f` | Errores genéricos y ArgumentException saneados; otras rutas requieren revisión dirigida por datos sensibles |
| `backend/src/API/Controllers/AuthController.cs` | `af437c869b4800a499c42aeff00347de3a3cc521` | Registra login éxito y rechazo mediante IAuditoriaService; no acredita retención o protección contra fallo transaccional |
| `backend/tests/Solqaryn.Tests/I8009ELoginAuditoriaSinSecretoTests.cs` | `7050551eee56d658b455717848259dfd80d725f6` | Mock/controlador no expone marcador sintético; no MySQL ni proveedor externo |

**Riesgos abiertos que se deberán comprobar:** (R1) aislamiento de consulta A/B efectivo, pues el método del repository mostrado no filtra `EmpresaId` explícitamente; no afirmar filtración sin inspeccionar los query filters de AppDbContext y el contexto de autorización. (R2) alta de auditoría best-effort puede perder evento; revisar qué operaciones críticas requieren `RegistrarEstrictoAsync` y atomicidad. (R3) strings serializados `ValoresAnteriores`, `ValoresNuevos`, `Motivo`, `Error` requieren minimización y redacción verificables. (R4) retención, caducidad de excepciones y exportaciones no fueron acreditadas mediante este inventario. (R5) la correlación no equivale a idempotencia ni permisos. No modificar ni consultar servicios productivos para probar estos riesgos.

## 4. Clasificación CI, A0–A5 de esta entrega y rollback

| Control | Validación realmente exigible para M01.05 documental |
| --- | --- |
| A0 | Diff Markdown, 16 AU únicos, consistencia de 8 hashes Git, ausencia de credenciales y clasificación de riesgos |
| A1 | Contratos positivos y negativos con precondición, estímulo, oracle y futuro mecanismo de prueba; no afirmar que esos tests se ejecutaron |
| A2 | Delimitar JWT/RBAC/tenant A/B, autorización server-side, PII, logs, fail-closed, MFA futuro y ausencia de bypass |
| A3 | Verificar 10 prerequisitos DAG LISTO, ocho fuentes concretas y consumidores; runtime .NET/Node/Angular/MySQL/Docker/E2E NO_APLICA a este documento |
| A4 | Tras commit directo FF en `dev`, revisar parent, diff 2 archivos documentales, SHA exacto, check `verify-solqaryn-project-scope` `completed/success`, PLAN/COLA/BITACORA |
| A5 | Pendiente de otra Task Javier (distinta de DEV02) sobre SHA publicado; revisar un AU positivo y negativo documental o prueba equivalente |

`requiere_dotnet=NO_APLICA`, `requiere_node_angular=NO_APLICA`, `requiere_mysql=NO_APLICA`, `requiere_docker=NO_APLICA`, `requiere_e2e=NO_APLICA`; **sí** `requiere_security=INSPECCION_DOCUMENTAL` y `requiere_scope_lock=SI`. Un pipeline ajeno a un diff documental no sustituye la inspección de contratos ni debe bloquear un cierre que cumpla todos los gates realmente exigibles.

**Rollback no destructivo:** `git revert` del commit documental, preservando historia de 26 columnas BITACORA y trazabilidad; sin force-push, secretos, cambios API/DB/permiso, IAM real, QA, main, PROD, ramas ni PR. A5 independiente no queda certificado por la autoría de este documento.
