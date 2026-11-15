# SOLQARYN V5 FINAL — C14-04.M02.02: contratos de API, DTO, errores y compatibilidad de puesta en marcha

**Autor:** PRIMARY DEV 03 JAVIER_CHATGPT (`6adc2e4bd8a48191ac7d8641ed95a1a3`). **Repo:** `solqaryn/Solqaryn@dev`. **Scope Lock:** STRICT. **Entrega:** diseño DOCUMENTAL M02.02; **ningún endpoint nuevo se implementa aquí**.

## 1. Autoridad, fuentes y secuencia

Autoridad exclusiva: [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit) y gobierno `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, `docs/AUTOMATION_STATUS.md`, skill y arquitectura canónicas. Este diseño depende de `C14-04.M01.01–06` LISTO+A5; `M02.01` estaba LISTO A0–A4, pero A5 independiente permanece pendiente y no constituye autorización de implementación runtime. El DAG propio M02.02 fue VALIDADO/SI, COLA G/O=SI y PLAN H=PENDIENTE/I vacío antes del claim exclusivo.

**Superficies existentes observadas en Git:** `backend/src/API/Controllers/PlataformaSoporteController.cs` declara `[ApiController]`, `[Authorize]`, ruta `plataforma/soporte/sesiones`, POST para crear y DELETE para revocar sesión, `ResponseCache(NoStore=true)`; request incluye `EmpresaId`, `Modulo`, `Motivo` de 10 a 500 caracteres y `DuracionMinutos` 1–30. `backend/src/API/Controllers/AuditoriaController.cs` declara GET `auditoria` con `[Authorize]`, `[RequierePermiso(Auditoria, Ver)]` y un segundo `Forbid()` explícito para no administradores. Estas rutas **no son** endpoints de control de release, backup, restore o alertas. No se amplían ni se certifican por este contrato.

## 2. Operaciones conceptuales para evaluar en una implementación autorizada

| OP | Operación futura propuesta, NO_IMPLEMENTADA | Regla de control |
| --- | --- | --- |
| OP01 | Consultar expediente de release por identificador | JWT/RBAC backend y scope explícito |
| OP02 | Consultar manifest y checks exact-SHA | Sólo lectura, evidencia inmutable |
| OP03 | Consultar autorización de ventana/entorno | Ausencia de grant => DENEGADO |
| OP04 | Registrar caso de aceptación observado | Identidad de ejecutor, contexto y hash |
| OP05 | Consultar readiness y alertas correlacionadas | Medición real con timestamp, sin SLO supuesto |
| OP06 | Abrir incidente operacional | Owner, severidad, deduplicación y PII redactada |
| OP07 | Actualizar escalamiento de incidente | Política de rol, no cambio de tenant |
| OP08 | Consultar prueba aislada de restore | No ejecutar backup/restore al consultar |
| OP09 | Consultar plan de rollback aprobado | No activar reversión desde GET |
| OP10 | Solicitar transición de release | Requiere autorización de entorno separada y gates probados |

No se reservan URLs, no se define controller nuevo, no se envían webhooks ni eventos. Antes de implementar, deberá decidirse qué operaciones corresponden a API privada, consola interna, adaptador o archivo auditable y cómo quedan versionadas.

## 3. DTO y campos de contrato propuestos

| Campo | Tipo lógico | Restricción y semántica |
| --- | --- | --- |
| `releaseId` | opaque identifier | No predecible; no confiere permisos |
| `commitSha` | Git SHA | Validar formato y presencia en manifest |
| `artifactDigest` | digest tipado | Hash verificado, no texto arbitrario |
| `environment` | enum allowlisted | dev/entorno explícitamente autorizado; ningún default PROD |
| `scopeId` | identificador servidor | Resuelto desde autoridad backend |
| `tenantKey` | contexto servidor | No confiar en ID/slug/host del cliente |
| `caseId` | identificador de prueba | Único en evidencia verificada |
| `observedAtUtc` | timestamp UTC | Observación y no hora inventada |
| `correlationId` | identificador opaco | Sin PII o secretos |
| `checkName` | nombre allowlisted | Pareja con head SHA y conclusión |
| `checkConclusion` | enum | `success` sólo cuando `completed` y causal |
| `rollbackRef` | versioned reference | Plan aprobado, no ejecución automática |

**Modelo de respuesta FUTURO:** estado explícito `NO_ACREDITADO | VALIDANDO | BLOQUEADO | VERIFICADO`, `code`, `correlationId`, `observedAtUtc` y `evidenceRef` redactada. No se copian tokens de soporte ni datos de tenants en errores o logs. Los clientes existentes no deberían romperse: contrato aditivo, versionado y documentado, evitando cambios a DTO o rutas ya publicados.

## 4. Matriz de fallos y denegación por defecto

| Código lógico | Situación | Comportamiento requerido futuro |
| --- | --- | --- |
| ER01 | Credencial ausente o inválida | 401 sin información sensible |
| ER02 | Rol, permiso o grant faltante | 403 sin divulgación del recurso |
| ER03 | Identificador desconocido o de otro tenant | 404 indistinguible, sin enumeración |
| ER04 | Estado incompatible o concurrencia de revisión | 409 con correlación, sin overwrite |
| ER05 | DTO, hash, enum o timestamp inválidos | 400/422 con errores mínimos y tipados |
| ER06 | Tasa excedida | 429 con política de reintento acotada |
| ER07 | Check requerido `queued/in_progress` | BLOQUEADO, jamás PASS |
| ER08 | Check `failed/cancelled/skipped` | BLOQUEADO, no degradar verificador |
| ER09 | Dependencia/manifest no acreditada | BLOQUEADO; nueva verificación causal |
| ER10 | Servicio externo no disponible | 503 con correlación; sin prometer éxito |
| ER11 | Clave de idempotencia repetida/conflictiva | Replay seguro o 409; sin duplicar incidente |
| ER12 | Solicitud de cambio productivo no autorizado | 403 y registro auditable; ninguna mutación |

Los HTTP mostrados son **resultados de diseño propuestos**; ninguna prueba HTTP fue ejecutada. La aplicación definitiva se acuerda según los contratos reales ya expuestos, evitando respuestas inesperadas en consumidores existentes.

## 5. Validaciones, compatibilidad y casos de aceptación

| Test futuro | Positivo esperado | Negativo esperado |
| --- | --- | --- |
| AC01 | JWT/RBAC válido lee su release | Anónimo 401, rol sin permiso 403 |
| AC02 | Token tenant A accede solo a A | ID o host manipulados no devuelven B |
| AC03 | SHA/digest coinciden con artefacto | Hash no coincidente bloquea |
| AC04 | Scope Lock y checks causales success | Cancelled/skipped/in_progress nunca PASS |
| AC05 | Consulta GET no cambia estado | GET no dispara rollback o restore |
| AC06 | Evidencia observada tiene case ID y timestamp | Aserción sin ejecución no certifica |
| AC07 | Incidente se correlaciona y escala a rol permitido | Reintento/duplicado no crea caso doble |
| AC08 | Contrato v1 previo conserva semántica | Campos nuevos no rompen deserialización |
| AC09 | Rate limit aplica al actor y scope | Tráfico excesivo no fuerza aceptación |
| AC10 | A5 por Task Javier distinta desbloquea DAG | Autoaprobación o owner activo no desbloquea |

**AC01–AC10 = FUTUROS_NO_EJECUTADOS.** No hay colección de pruebas HTTP, .NET, MySQL, Playwright, SLA ni tenants A/B ejecutados por esta entrega documental. Todo endpoint de M03 necesitará código real, fixtures positivos/negativos, builds .NET y seguridad CI exact-SHA.

## 6. Impacto y criterios A0–A5

- **A0 estática:** dos Markdown exclusivamente, OP01–OP10, doce campos DTO, ER01–ER12 y AC01–AC10; fuentes y ausencia de secretos/encabezados vacíos.
- **A1 funcional documental:** cada AC contiene positivo y negativo falsables, sin resultados runtime.
- **A2 seguridad:** preserva la autoridad backend JWT/RBAC, aislamiento tenant A/B, auditoría y denegación de PROD. Las rutas existentes no se modifican.
- **A3 integración:** contrastar con M01.06 y M02.01, esquema operativo PLAN/COLA/DAG y control de compatibilidad de APIs existentes.
- **A4:** un commit de documento y CHANGELOG por fast-forward + expected HEAD, checks Scope Lock y V5 `completed/success` sobre SHA exacto, diff y readbacks.
- **A5:** revisor independiente perteneciente a las otras cinco Tasks Javier, nunca autora.

**Rollback del changeset:** `git revert` no destructivo sobre `dev` previa revalidación HEAD y evidencias; no borrar BITACORA. Sin implementación, migración, tokens, DNS, cuentas, servicios, QA, main ni producción.
