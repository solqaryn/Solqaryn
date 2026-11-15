# SOLQARYN V5 — C03-03.M02.01: entidades lógicas y ownership de auditoría de seguridad

**Microtarea:** `C03-03.M02.01`. **Autor:** PRIMARY DEV 06 / JAVIER_CHATGPT, Task `6af16bbad26881918c283abe06e1f32a`. **Ámbito exclusivo:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Fuente institucional:** STANDARD V5 FINAL, PLAN_MAESTRO, COLA, DAG_TECNICO y BITACORA. **Fase:** contrato M02 documental; no crea una nueva tabla, controlador, DTO, permiso, política de retención, sistema de logging, worker, servicio o migración.

## 1. Evidencia real, prerrequisitos y límites de certeza

El preflight verificó `C03-03.M02.01` con PLAN H512=PENDIENTE/I512 vacío, COLA G/O=SI, DAG F=VALIDADO/G=SI. El terminal documental `C03-03.M01.06` quedó LISTO con A5 independiente por DEV06 distinta de la autoría DEV03; las cinco M01.01–05 y el precedente C02-04.M01.06 ya figuraban LISTO+A5. Reclamación por CAS exclusivo H512 y readback de owner I512.

Fuentes del árbol `dev`:
- `backend/src/API/Controllers/AuditoriaController.cs` blob `82057f2acc571263ed7ed530eae0f81359b2f81a`: `[Authorize]`, `RequierePermiso(Auditoria, Ver)`, comprobación de administrador y `GET /auditoria`.
- `backend/src/Application/Services/AuditoriaService.cs` blob `def2c1f7bc58e23f98a1065fcdf0963322f3705c`: registro `RegistrarAsync` best-effort y `RegistrarEstrictoAsync` que propaga fallos. La presencia de ambos **no prueba** persistencia obligatoria en todas las operaciones.
- `backend/src/Infrastructure/Repositories/AuditoriaRepository.cs` blob `8fc9417b88da91ad1a5777ea77d9e31756230384`: filtros/paginación; el método no contiene predicado `EmpresaId` explícito. Deben verificarse filtros EF y autorizaciones del flujo antes de afirmar aislamiento o vulnerabilidad.
- `backend/src/Domain/Entities/RegistroAuditoria.cs` blob inventariado `d669ac397a78ba02a5ba19e2cfb4bc9c7a115462`: tenant opcional, actor, acción, resultados y trazas; la entidad **no acredita** cifrado, retención ni filtrado por sí misma.
- `backend/src/API/Middleware/CorrelationIdMiddleware.cs` blob `3b76dca41bb4bc0b44f89c2a4664c79907f9f242`: `X-Correlation-ID` saneado, nunca grant de tenant ni idempotencia.
- `docs/v5/c03-03-m01-03-inventario-auditoria-seguridad.md` blob `807eab0b4d6aa1a71beeb7d9df82b40e23c06969`, `docs/v5/c03-03-m01-05-criterios-auditoria-seguridad.md` blob `b8ad87e4164135ccbbe1a84e180e55bc07fbecb2` (AU01–AU16 futuros), y `docs/v5/c03-03-m01-06-bloqueo-rollback-liberacion-auditoria.md` blob `274dd951853459e687ea4fc359b03932f254e869` (siete gates, contención y A5 independiente).

Estos son blobs de fuente, NO resultados de tests. No se ejecutó HTTP, MySQL, EF efímero, export ni retención real en esta tarea.

## 2. Modelo conceptual A01–A16 (NO nuevas entidades EF)

| ID | Entidad/contrato lógico | Dueño/alcance y confidencialidad |
| --- | --- | --- |
| A01 | Actor autenticado | Principal del servidor; identidad activa y JWT verificado |
| A02 | Membresía/rol empresarial | Empresa real y permiso actual; nunca body/header arbitrario |
| A03 | Principal de plataforma | Autoridad global explícita, separada del rol empresarial |
| A04 | Empresa auditada | Tenant persistido del recurso/evento y fuente de política |
| A05 | Recurso sensible | Owner empresarial verificado antes de lectura o cambio |
| A06 | Acción auditable | Tipo de acción allowlisted, no cadena libre de cliente |
| A07 | Evento de auditoría | Registro resultado/código/tiempo UTC y referencia mínima |
| A08 | Denegación de acceso | Resultado rechazado sin exponer recurso o actor de B |
| A09 | Cambio crítico | Operación que requiere evidencia estricta o rollback |
| A10 | Contexto de correlación | Identificador opaco saneado; no permite acceso |
| A11 | Motivo de acceso excepcional | Texto/código minimizado, política y aprobador distintos |
| A12 | Alcance excepcional temporal | Capacidad futura expirable y revocable; NO implementada |
| A13 | Datos anteriores/nuevos | Proyección redactada y allowlisted, nunca serialización irrestricta |
| A14 | Política de retención | Regla legal/negocio futura explícitamente aprobada por owner |
| A15 | Exportación/consulta administrativa | Sólo rol+permiso+tenant, paginación y campos mínimos |
| A16 | Evidencia de control de calidad | SHA, pruebas reales, A5 independiente, rollback y bitácora |

**Relaciones conceptuales:** Actor N:M Empresa por membresía; Evento N:1 Empresa o ámbito global autenticado; Evento N:1 Acción/Recurso, y opcional contexto de correlación. Consulta administrativa exige permiso **y** ámbito de visibilidad, no sólo rol. Un cambio crítico no queda autorizado por el éxito best-effort de un logger. El acceso excepcional es **propuesta futura**, no elevación de permisos creada aquí. Las políticas de retención/exportación son propuestas que requieren autorización y definición legal antes de introducirse.

## 3. Invariantes S01–S18 de autorización y minimización

| ID | Condición de seguridad fail-closed |
| --- | --- |
| S01 | Ningún `X-Empresa-Id`, `X-Correlation-ID`, IP ni path otorga membresía |
| S02 | Actor no autenticado o rol sin permiso de auditoría no consulta eventos |
| S03 | Admin A no ve eventos de B salvo grant global explícito y auditado |
| S04 | `EmpresaId` opcional del modelo no habilita consulta cross-tenant |
| S05 | Antes de certificar aislamiento, contrastar predicados reales y filtros EF con prueba A/B |
| S06 | Plataforma y empresa usan matrices de permiso distintas; no se fusionan scopes |
| S07 | `RegistrarAsync` best-effort no se usa como evidencia universal de commit |
| S08 | Operación crítica con auditoría estricta falla de forma controlada y revierte cuando la política exige |
| S09 | Old/new de entidades se serializan solo desde allowlist/redacción explícita |
| S10 | JWT, OTP, contraseña, DSN, SQL, PII y tokens no aparecen en trazas, errores, exports |
| S11 | Correlación opaca saneada no es autoridad ni clave idempotente |
| S12 | Los tiempos de evento provienen de UTC servidor con límites de consulta |
| S13 | Consulta/export exige paginación, filtro permitido y rate limiting servidor |
| S14 | Actor revocado no conserva consulta ni privilegio de lectura anterior |
| S15 | Excepcionalidad futura exige permiso independiente, motivo, TTL, revocación y revisor |
| S16 | Retención/purga no opera sin política aprobada y rollback/backup específico |
| S17 | `completed/success` exact-SHA obligatorio para checks causales; queued/cancelled/skipped ≠ PASS |
| S18 | A5 exige Task Javier distinta del autor, sin liberar QA/main/PROD por documentos |

**Ambigüedad reconocida:** ausencia de filtro explícito `EmpresaId` en método consultado no es demostración de fuga porque los filtros globales de EF y middleware pueden aplicar otras barreras; tampoco autoriza concluir que la confidencialidad ya está acreditada. La prueba negativa integrada A/B es un requisito real futuro, no un hallazgo confirmado.

## 4. Fallos conceptuales D01–D10 y contención

| ID | Evento adverso | Rechazo o respuesta requerida |
| --- | --- | --- |
| D01 | Admin de A intenta consultar historial B | Denegar sin enumerar eventos |
| D02 | Rol revocado conserva token antiguo | Revalidar permiso/membresía, no devolver datos |
| D03 | Header de correlación inyecta CRLF o longitud excesiva | Sanear o generar ID nuevo opaco |
| D04 | Logger recoge JWT, contraseña o cambios íntegros | Redacción por allowlist antes de persistir |
| D05 | Falla DB durante cambio crítico con auditoría obligatoria | Rollback de la operación acoplada |
| D06 | Registro best-effort se interpreta como garantía de durabilidad | NO_GO para certificar flujo estricto |
| D07 | Exportación sin autorización ni alcance | Rechazar y registrar código seguro |
| D08 | Política de retención indefinida/desconocida | NO implementar purga ni export masivo |
| D09 | Reintento de cambio crítico duplica eventos/efectos | Idempotencia y concurrencia real por actor/recurso |
| D10 | CI cancelada o A5 autora aprueba | VALIDANDO/NO_GO, sin modificar DAG manualmente |

## 5. P01/N01–P10/N10 — escenarios FUTUROS_NO_EJECUTADOS

| Par | Positivo futuro | Negativo futuro |
| --- | --- | --- |
| P01/N01 | Admin A con permiso consulta auditoría A | Admin A intenta ver historial B |
| P02/N02 | Auditoría de denegación muestra código seguro | Actor anónimo conoce PII o existencia de B |
| P03/N03 | Revocación de rol impide más consultas | Rol revocado conserva acceso |
| P04/N04 | Logger de cambio crítico persiste evento redactado | Old/new contienen JWT, DSN o PII |
| P05/N05 | Auditoría estricta y operación forman unidad consistente | DB falla y efecto crítico persiste sin evidencia exigida |
| P06/N06 | Correlation-ID válido atraviesa capa segura | Header CRLF/longitud inválida inyecta log |
| P07/N07 | Consulta paginada entrega alcance autorizado | Filtro cliente fuerza export sin límite |
| P08/N08 | Export autorizado con permiso y política | Export sobre tenant ajeno o con secreto |
| P09/N09 | Retención aprobada conserva/borrado conforme | Purga sin permiso o sin backup, irreversible |
| P10/N10 | SHA exacto CI y A5 independiente acreditan revisión | Check cancelled/skipped o autor se revisa |

Estos veinte casos **no se ejecutaron**: son oráculos para M03/M05 de código y futura integración HTTP/EF/MySQL efímera, pruebas de privacidad y revocación. No convertirlos en evidencias positivas.

## 6. A0–A5, riesgos y rollback

**A0:** skill canónica, owner Javier, HEAD dev, Scope Lock, plan/cola/DAG y CAS único para H512/I512. **A1:** revisión de 16 entidades, 18 invariantes, 10 fallos y diez pares P/N futuros, con referencias exactas a código y M01. **A2:** permisos, tenant A/B, correlación, privacidad/redacción, auditoría estricta, retención y denegaciones fail-closed; sin crear IAM ni alterar JWT. **A3:** cotejo conceptual con `AuditoriaController`, `AuditoriaService`, `AuditoriaRepository`, `RegistroAuditoria`, `CorrelationIdMiddleware` y tests inventariados; .NET/EF/MySQL/HTTP no causales para diff exclusivamente Markdown: `NO_APLICA`, jamás PASS ficticio. **A4:** publicar sólo `docs/v5/c03-03-m02-01-entidades-ownership-auditoria-seguridad.md` y sección de `CHANGELOG_AI.md` autocontenida, commit fast-forward/CAS dev; exigir Scope Lock y procedencia V5 `completed/success` exact-SHA, diff, BITACORA y readback PLAN/COLA/DAG. **A5:** otra Task Javier distinta de DEV06, sin liberar M03/QA/main/PROD por documento.

**Rollback de esta entrega documental:** `git revert` normal del commit tras releer dev, evento institucional compensatorio aditivo si es necesario; ningún registro, política de retención, endpoint, tabla, DB, JWT ni infraestructura alterada. **Prohibido** desplegar, modificar QA/main/PROD, migraciones reales, permisos nuevos, eventos, webhooks, Tasks o secretos.
