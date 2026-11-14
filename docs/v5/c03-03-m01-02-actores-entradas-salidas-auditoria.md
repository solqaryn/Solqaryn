# SOLQARYN V5 FINAL — C03-03.M01.02: actores, entradas, salidas y superficies de auditoría de seguridad

**Entrega documental exclusiva M01.02**, autor PRIMARY DEV 02 — JAVIER, Task ID `6af16bb9492c8191b8e8c7d606f11507`; repositorio `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK STRICT. Fuentes institucionales: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`) y Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. `DAG_TECNICO=C03-03.M01.02 VALIDADO`, con 10/10 predecesores `C02-04.M01.01–05` y `C03-01.M01.01–05` LISTO.

**Alcance:** describir actores, límites de confianza y contratos de entrada/salida actuales y futuros para registrar autorizaciones, denegaciones, cambios críticos y accesos excepcionales sin secretos; correlación, retención y no exposición son objetivos de fase. **No implementa ni certifica** API, DB, IAM, token, MFA, privilegios excepcionales, retención ni export; no habilita M01.06/M02, QA/main/PROD.

## 1. Matriz de actores y autoridad (12 actores)

| Actor | Propósito de lectura/escritura | Límite y control de seguridad |
| --- | --- | --- |
| ACT01 Usuario anónimo | Intenta login; puede originar auditoría de rechazo | Sin rol ni tenant; credenciales nunca en log; respuesta anti-enumeración |
| ACT02 Usuario autenticado de empresa A | Opera recursos sujetos a JWT/RBAC y origina evento de negocio | Empresa A debe provenir de membresía y permiso reales, no de header |
| ACT03 Usuario de empresa B | Intenta recursos dentro de B | Evento B nunca visible a usuario A sin grant efectivo |
| ACT04 Administrador empresarial A | Consulta `GET /auditoria` con permiso Auditoria.Ver | Verificar contexto A efectivo; administrador no significa auditor global |
| ACT05 Usuario autenticado sin permiso Auditoria.Ver | Intenta consultar bitácoras | Backend debe negar aunque UI exponga endpoint |
| ACT06 Administrador de plataforma | Necesita auditoría transversal únicamente bajo ámbito aprobado | Separar autoridad global de permisos empresariales; nunca inferir bypass |
| ACT07 Auditor autorizado | Lee/exporta registros permitidos | Exportación y filtros delimitados, minimización y propósito explícitos |
| ACT08 Servicio de dominio/ERP | Emite evento de cambio crítico o denegación | Operación crítica requiere durabilidad estricta/rollback cuando el contrato lo exija |
| ACT09 Middleware HTTP de correlación | Normaliza `X-Correlation-ID` no confiable | No usar correlation ID como permiso ni clave idempotente |
| ACT10 Servicio/repositorio de auditoría | Persiste y consulta `RegistroAuditoria` | Filtrado real de tenant A/B, transacciones y saneamiento antes de guardar |
| ACT11 Servicio externo futuro autorizado | Recibe alerta/export bajo contrato específico | No transmitir JWT/PII/secretos sin autorización; proveedor no acreditado |
| ACT12 Revisor independiente A5 Javier | Verifica SHA, controles y evidencias | TASK_ID distinto del autor, 26 campos en BITACORA; no promoción automática |

Estos actores son **roles conceptuales de preparación**, no cuentas recién creadas o autorizaciones concedidas. El repositorio conserva controles reales solo donde los hashes de fuentes lo demuestran; otros actores y exportaciones son futuros.

## 2. Contratos de entradas, salidas y superficies (14 interfaces)

| ID | Superficie, emisor → receptor | Entrada | Salida verificable / brecha pendiente |
| --- | --- | --- | --- |
| IO01 | `GET /auditoria` → AuditoriaController | JWT, consulta `AuditoriaFiltroDto` | Requiere `[Authorize]`, permiso y `EsAdministrador`, devuelve `ApiResponse`; delimitar tenant A/B en HTTP/DB |
| IO02 | `AuditoriaService.GetFilteredAsync` → repository | Filtros por usuario, módulo, acción, texto, fechas | `PagedResult` de registros; probar redacción y ámbitos, no asumir export universal |
| IO03 | `AuditoriaRepository.GetFilteredAsync` → EF | `IQueryable<RegistroAuditoria>` y predicados | Página y total; método no filtra EmpresaId explícitamente; verificar global query filters y consumidor |
| IO04 | `RegistrarAsync` → servicio auditable | ModuloSistema, AccionPermiso, descripción, estado, motivo, resultado | Intenta persistir; captura excepción; no prometer durabilidad estricta |
| IO05 | `RegistrarEstrictoAsync` → servicio auditable | Igual IO04, modo crítico | Propaga error de persistencia; contrato futuro exige transacción/rollback |
| IO06 | `RegistroAuditoria` → base persistente | UsuarioId, EmpresaId, módulo, acción, entidad, old/new, motivo, IP | Registro mutable con datos potencialmente sensibles; no se acredita TTL/redacción universal |
| IO07 | `VerifiedCommercialTenant.Resolve` → registro | Contexto de autoridad servidor | Atribuye tenant verificado; header cliente no crea grant |
| IO08 | `VerifiedPublicInvoiceTenant/VerifiedPublicStoreTenant.Resolve` → registro | Marcas públicas verificadas del servidor | Identifican tenant bajo contrato público; no convierten visitante en administrador |
| IO09 | `CorrelationIdMiddleware` → contexto HTTP | Header `X-Correlation-ID` sin confiar | Trunca/sustituye valores inseguros; valor saneado en TraceIdentifier, respuesta y scope log |
| IO10 | `AuthController.Login` → AuditoriaService | Resultado login, identidad presentada | Audita éxito/rechazo sin contraseña explícita; no prueba logs de terceros ni DB |
| IO11 | `ExceptionHandlingMiddleware` → HTTP/telemetría | Excepción de argumento y otros errores | Respuesta segura para ArgumentException, correlación; otras excepciones requieren revisión |
| IO12 | Pantalla futura de auditoría → HTTP | Filtros y paginación del navegador | UI no autoriza; filtrado y RBAC deben verificarse en servidor |
| IO13 | Proveedor/alerta/export futuro → sistema externo | Evento mínimo autorizado y seudonimizado | No existe integración acreditada; exigir consentimiento/retención/secretos y control de destinatario |
| IO14 | Gobierno V5 → PLAN/COLA/BITACORA | TASK_ID, SHA, EVIDENCIA y eventos únicos | PLAN!H/I autoridad, COLA fórmula, BITACORA 26 columnas A:Z; A5 otro Javier |

## 3. Fuentes reales y límites por blob Git

| Archivo leído de dev | Blob SHA | Alcance de la evidencia |
| --- | --- | --- |
| `backend/src/API/Controllers/AuditoriaController.cs` | `82057f2acc571263ed7ed530eae0f81359b2f81a` | GET, permiso, rol admin |
| `backend/src/Application/Services/AuditoriaService.cs` | `def2c1f7bc58e23f98a1065fcdf0963322f3705c` | Registro best-effort/estricto; tenant server-side y correlación |
| `backend/src/Infrastructure/Repositories/AuditoriaRepository.cs` | `8fc9417b88da91ad1a5777ea77d9e31756230384` | Filtros y paginación; sin condición explícita EmpresaId en método |
| `backend/src/Domain/Entities/RegistroAuditoria.cs` | `d669ac397a78ba02a5ba19e2cfb4bc9c7a115462` | Campos sujetos a política PII y retención |
| `backend/src/API/Middleware/CorrelationIdMiddleware.cs` | `3b76dca41bb4bc0b44f89c2a4664c79907f9f242` | Cabecera saneada, TraceIdentifier |
| `backend/src/API/Middleware/ExceptionHandlingMiddleware.cs` | `baee40143e4e48b8df200e77892606af96d60b7f` | Manejo de errores y logging |
| `backend/src/API/Controllers/AuthController.cs` | `af437c869b4800a499c42aeff00347de3a3cc521` | Emisión de auditoría login éxito/rechazo |
| `backend/tests/Solqaryn.Tests/I8009ELoginAuditoriaSinSecretoTests.cs` | `7050551eee56d658b455717848259dfd80d725f6` | Prueba mock no exposición de tokens sintéticos, no runtime real |

La inspección de ocho blobs describe código existente, no el resultado de HTTP/MySQL en una corrida nueva. **Advertencia:** la ausencia de predicado explícito EmpresaId en el método `AuditoriaRepository.GetFilteredAsync` no demuestra por sí sola fuga entre empresas; hay que verificar filtros globales EF y contexto de autorización con pruebas reales antes de evaluar el riesgo definitivo. Tampoco hay evidencia suficiente de retención legal, alerta externa o acceso excepcional real en estas fuentes.

## 4. Pruebas dirigidas para los contratos futuros (no ejecutadas por M01.02)

| Caso | Signo | Estímulo futuro | Resultado falsable |
| --- | --- | --- | --- |
| T01 | + | Administrador A autorizado consulta auditorías A | 200, resultados A según grants, sin registros B |
| T02 | - | Usuario sin permiso o no administrador consulta mismo endpoint | 403/404, ninguna PII ni registro expuesto |
| T03 | - | Usuario A altera header/query a B | 403/404 o conjunto vacío sin datos B, incluso con JWT A válido |
| T04 | + | Cambio crítico persiste registro y transacción bajo modo estricto | Evento durable correlacionado, no duplicado, antes/después permitido |
| T05 | - | Falla de DB durante evento de auditoría obligatoria | No falsear éxito, rollback/fallo seguro |
| T06 | - | Header `X-Correlation-ID` malicioso | Correlación regenerada segura, ningún CRLF/control en logs |
| T07 | - | Payload con contraseña, token o datos personales | No quedan secretos/PII sensibles en columnas ni export |
| T08 | - | Acceso excepcional sin MFA/reauth/motivo/TTL autorizado | Rechazo, ningún privilegio concedido |
| T09 | - | Exportación masiva o vencimiento de retención | Restringir por rol, propósito y política aprobada |
| T10 | control | Evento de A5 firmado por Task autora o BITACORA truncada | No marcar A5_PASS, registrar evidencia 26 campos y actor real |

Estas pruebas son requisitos de implementación posterior. Un mock, una tabla de aceptación o un check histórico no certifica API/DB, multiempresa A/B, archivo, retención o proveedor live.

## 5. Calidad A0–A5 y rollback

A0: inspección Markdown, 12 actores ACT01–12, 14 contratos IO01–14, 8 blobs y 10 pruebas futuras, ningún secreto material ni cambio ejecutable. A1: casos positivos T01/T04, negativos T02/T03/T05/T06/T07/T08/T09, outputs observables, sin afirmar ejecución. A2: JWT/RBAC/tenancy A/B, PII, trazabilidad, minimización, fail-closed y separación administrador/auditor/operador. A3: contratos entre API, Application, Infrastructure y middleware; 10/10 antecedentes DAG LISTO; pruebas de runtime `NO_APLICA` a la entrega documental. A4: después de commit FF en `dev`, verificar SHA exacto, diff, Scope Lock completed/success, readback PLAN/COLA/BITACORA. A5: PENDIENTE de otra Task Javier distinta de la autora, nunca autoaprobada.

`requiere_dotnet=NO_APLICA`; `requiere_node_angular=NO_APLICA`; `requiere_mysql=NO_APLICA`; `requiere_docker=NO_APLICA`; `requiere_e2e=NO_APLICA` para el diff documental; `requiere_security=SI_INSPECCION_DOCUMENTAL`; `requiere_scope_lock=SI`. Sin publicar secretos ni intervenir servicios productivos. Rollback `git revert` no destructivo del documento y CHANGELOG, conservando evidencia idempotente en BITACORA A:Z. No habilitar M01.06/M02, QA/main/PROD ni automatizaciones extra.
