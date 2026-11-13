# SOLQARYN V5 FINAL — C03-03.M01.04: riesgos, supuestos y decisiones de auditoría de seguridad

**Objeto:** documentar riesgos, supuestos y decisiones pendientes de la futura auditoría de autorizaciones, denegaciones, cambios críticos y accesos excepcionales, con correlación, retención y no exposición de secretos. **Autor:** PRIMARY DEV 02 — JAVIER, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. Solo `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK STRICT. Fuente de ejecución: Standard V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`) y Sheet institucional `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. `DAG_TECNICO` registra M01.04 VALIDADO con predecesores `C02-04.M01.01–05` y `C03-01.M01.01–05`, diez LISTO.

**Entrega exclusivamente DOCUMENTAL.** Ningún riesgo es una vulnerabilidad confirmada sin prueba causal, ninguna decisión aquí concede permisos, cambia JWT, persiste datos ni autoriza migración o acceso extraordinario. No habilitar M01.06, M02, QA/main/PROD, ramas o PR.

## 1. Registro de riesgos con condición de aceptación y respuesta (14 riesgos)

| Riesgo | Superficie y síntoma potencial | Impacto si ocurre | Verificación dirigida y tratamiento propuesto |
| --- | --- | --- | --- |
| R01 — Lectura A/B | `AuditoriaRepository.GetFilteredAsync` no muestra predicado `EmpresaId` explícito | Consulta de empresa B desde actor A | Verificar filtros globales de EF, ámbito del usuario y HTTP/MySQL con A/B; **no atribuir fuga antes del resultado** |
| R02 — Permiso insuficiente | Un rol no admin intenta usar `GET /auditoria` | Exposición de registros sensibles | Caso 403/404 real con `[Authorize]`, `RequierePermiso` y `EsAdministrador` |
| R03 — Auditoría best-effort | `RegistrarAsync` captura excepción de almacenamiento y continúa | Registro crítico perdido, éxito aparente | Identificar flujos críticos y exigir `RegistrarEstrictoAsync`/rollback cuando corresponda, con falla DB inyectada |
| R04 — Datos viejos/nuevos | Serialización libre de objetos `ValoresAnteriores`/`ValoresNuevos` | Contraseña, token o PII en almacenamiento | Pruebas de redacción/minimización antes de serializar y exportar |
| R05 — Logs de error | Mensajes de proveedor/entidad/exception y `Error` pasan a registro | Filtración de identificador/secretos | Tests de sentinelas privadas en log, respuesta y registro persistido |
| R06 — Correlation header | Cliente controla `X-Correlation-ID` | Log injection o correlación falsa | HTTP negativo con CRLF, Unicode de control, longitud 65; GUID seguro generado |
| R07 — Confundir ID con grant | CorrelationId o header se considera autorización | Escalamiento de tenant/rol | Controles server-side de membresía y deny A→B |
| R08 — Retención legal | Política de conservación sin TTL/propósito aprobado | Exceso o pérdida indebida de registros | Diseñar política legal aprobada, borrado/preservación idempotente, test de retención |
| R09 — Exportación masiva | Usuario auditor descarga registros sin minimización | Exfiltración de PII | Límite de filtros/paginación, rol y tenant, trazabilidad export, API negativa |
| R10 — Acceso excepcional | Elevación sin MFA/reauth/TTL/motivo | Privilegios permanentes no autorizados | No implementar hasta orden y proveedor; negativos fail-closed y expiración |
| R11 — Evento duplicado | Retry concurrente de escritura crítica | Doble bitácora/efecto | Contrato de idempotencia y test de concurrencia; correlation ID no es llave única |
| R12 — Actor anónimo | Rechazo login guarda username arbitrario con token embebido | Inyección y exposición en auditoría | Pruebas de marker sintético en AuthController/servicio y proveedor real posteriormente |
| R13 — Empresa nullable | `RegistroAuditoria.EmpresaId` permite null para eventos de plataforma/legacy | Ambigüedad de visibilidad global | Clasificar ámbito de evento con marca verificada y filtro explícito por actor, sin auto-grant |
| R14 — Logs no verificables | Solo mocks o registros de diseño usados como certificación | Pérdida de evidencia A0–A4 / A5 falsa | Evidencia SHA+job+logs reales, positivo y negativo, 26 campos BITACORA, revisión independiente |

No asumir que R01 o R04 ocurren en despliegue. Su estado inicial es `POR_VERIFICAR` y cualquier cierre futuro exige evidencia observada del mismo SHA. Criticidad y prioridad se determinan cuando el resultado técnico y la política aplicable sean comprobados.

## 2. Supuestos de preparación que NO constituyen certificación (8 supuestos)

| Supuesto | Fundamento inspeccionado | Condición que lo puede invalidar |
| --- | --- | --- |
| S01 — GET protegido | `AuditoriaController` contiene `[Authorize]`, permiso y admin | Ruta alternativa, global filters, bypass o proxy no inspeccionados |
| S02 — Tenant verificado al registrar | `AuditoriaService` usa marcas VerifiedCommercial/Public y no header bruto | Consumidor distinto o marca mal construida |
| S03 — Persistencia invocada | Service llama AddAsync/SaveChangesAsync | Falla DB, transacción ausente, excepción absorbida |
| S04 — Modelo trazable | RegistroAuditoria tiene usuario, fecha, acción, resultado y correlación | Campos nulos/incorrectos, falta de retención o política |
| S05 — Correlación saneada | Middleware valida longitud/caracteres del header | Omisión del middleware o consumidores que relean header bruto |
| S06 — Rechazo login no expone token sintético | Test mock `I8009ELoginAuditoriaSinSecretoTests` | No cubre HTTP/proveedor real ni DB |
| S07 — Lectura EF tenant-safe por otro control | Aún no se revisó todo AppDbContext + infraestructura | Falta de query filter; nunca suponer cumplimiento |
| S08 — M01 documentación admisible | DAG `PREPARACION_DOC_CONDICIONAL` y diez predecesores LISTO | Cambios de HEAD, permisos, claim/owner o Scope Lock en el futuro |

## 3. Decisiones pendientes de responsable autorizado (8 decisiones)

| Decisión | Opciones a evaluar | Restricción actual y prueba que la habilitaría |
| --- | --- | --- |
| D01 — Alcance de consulta por tenant | Filtro explícito EmpresaId, query filter global o combinación | Requerir prueba A/B y permiso server-side, no cambiar sin impacto revisado |
| D02 — Auditoría obligatoria | Estricta transaccional vs best-effort para evento por criticidad | Definir operaciones obligatorias con rollback y failure injection |
| D03 — Esquema de redacción | Allowlists por evento/campo, hashing o minimización | Nunca serializar secreto/PII sin regla y prueba |
| D04 — Políticas de retención | TTL por categoría, archivo inmutable, borrado legal | Necesita autorización jurídica y controles de integridad |
| D05 — Modelo de acceso excepcional | MFA/reauth/TTL/motivo/grants temporales | Solo implementación futura expresamente aprobada, nunca bypass de tenancy |
| D06 — Correlación e idempotencia | Dos IDs separados y persistidos según evento | Probar reintentos concurrencia; no usar cabecera libre como idempotency key |
| D07 — Exportación segura | Límite, firma, auditoría del export, redacción | Backend autoriza usuario/tenant y retiene evidencia de acceso |
| D08 — Estrategia de pruebas | HTTP multi-tenant, MySQL efímero, fallas, redacción y logs | CI causal SHA exacto; `SKIPPED` de job requerido no equivale a PASS |

Ninguna D01–D08 constituye aprobación del propietario ni obliga a modificar infraestructura; son asuntos abiertos para fases posteriores, sin creación de proveedor ni ampliación de autorizaciones.

## 4. Trazabilidad de código real leído

| Archivo dev | Blob SHA verificado | Evidencia observada |
| --- | --- | --- |
| `backend/src/API/Controllers/AuditoriaController.cs` | `82057f2acc571263ed7ed530eae0f81359b2f81a` | Guardias `Authorize`, permiso y administrador |
| `backend/src/Application/Services/AuditoriaService.cs` | `def2c1f7bc58e23f98a1065fcdf0963322f3705c` | Registro best-effort/estricto y marcas verificadas |
| `backend/src/Infrastructure/Repositories/AuditoriaRepository.cs` | `8fc9417b88da91ad1a5777ea77d9e31756230384` | Filtros/paginación, EmpresaId sin predicado local explícito |
| `backend/src/Domain/Entities/RegistroAuditoria.cs` | `d669ac397a78ba02a5ba19e2cfb4bc9c7a115462` | EmpresaId nullable, old/new, IP, UserAgent, CorrelationId |
| `backend/src/API/Middleware/CorrelationIdMiddleware.cs` | `3b76dca41bb4bc0b44f89c2a4664c79907f9f242` | Límite ASCII/64 y TraceIdentifier |
| `backend/src/API/Middleware/ExceptionHandlingMiddleware.cs` | `baee40143e4e48b8df200e77892606af96d60b7f` | Excepción argumento saneada y log estructurado |
| `backend/src/API/Controllers/AuthController.cs` | `af437c869b4800a499c42aeff00347de3a3cc521` | Auditoría de login éxito/rechazo |
| `backend/tests/Solqaryn.Tests/I8009ELoginAuditoriaSinSecretoTests.cs` | `7050551eee56d658b455717848259dfd80d725f6` | Prueba mock de no filtración sintética |

## 5. A0–A5, clasificación de pruebas y rollback

A0: 14 riesgos R01–14, 8 supuestos S01–08, 8 decisiones D01–08 únicos; Markdown, no secretos, hashes de 8 fuentes. A1: escenarios positivos (admin A autorizado consulta A; evento crítico durable futuro) y negativos (A→B, PII, fallas DB, log injection), con verificación pendiente real. A2: JWT, RBAC, tenant A/B, credenciales, PII, minimización, no elevación y fail-closed delimitados. A3: ocho contratos reales y 10 predecesores DAG LISTO; solo documento, no cambia API/consumidores. A4: verificar commit FF `dev`, SHA exacto, diff y Scope Lock `completed/success`, PLAN/COLA/BITACORA readback. A5: PENDIENTE de otra Task Javier distinta de DEV02.

`requiere_dotnet=NO_APLICA`, `requiere_node_angular=NO_APLICA`, `requiere_mysql=NO_APLICA`, `requiere_docker=NO_APLICA`, `requiere_e2e=NO_APLICA` **para este diff documental**; `requiere_security=SI_INSPECCION`; `requiere_scope_lock=SI`. Ningún riesgo implica pruebas ejecutadas en entorno real aquí. Rollback documental `git revert` normal y registro idempotente BITACORA A:Z de 26 campos; preservar autor, no QA/main/PROD.
