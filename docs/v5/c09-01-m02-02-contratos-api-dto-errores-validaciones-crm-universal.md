# SOLQARYN V17 — C09-01.M02.02: API conceptual, DTO, errores y validaciones de CRM universal

**Microtarea:** `C09-01.M02.02`, PLAN_MAESTRO fila 1305. **Autor:** PRIMARY DEV 03, JAVIER_CHATGPT, `TASK_ID=6adc2e4bd8a48191ac7d8641ed95a1a3`. **Riesgo del cambio:** R0 documental; `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. Autoridad: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`.

**Objeto limitado:** proponer contratos de API, DTO, errores tipados, validaciones y compatibilidad para historial, clientes, actividades, segmentos y oportunidades CRM. **No existen endpoints nuevos por este documento; no se ejecuta HTTP, JWT, xUnit, Angular, EF, MySQL, Playwright, mensajes, jobs ni migraciones.** Un esquema no concede permiso de despliegue. La evidencia final futura del lote M02 es un timeline aislado y autorización por rol, no satisfecha con este texto.

## 1. Línea base y decisiones pendientes

- `C09-01.M01.01–06` define límites, fuentes, riesgos, aceptación y dieciséis gates, con A5 independiente de M01.06 verificada por DEV03. `C09-01.M02.01` fija dieciséis conceptos E01–E16, veinte invariantes INV01–INV20 y doce pares de pruebas futuras; está en LISTO_DEV documental, con su propio A5 todavía pendiente de otra Task. M02.02 es **preparación documental independiente** según DAG actual, no libera etapa técnica por depender de un documento en revisión.
- Entidades existentes verificadas por M01: `Cliente`, `Empresa`, `UsuarioEmpresa`. Los conceptos `PerfilCRM`, `InteracciónCRM`, `OportunidadCRM`, `SegmentoCRM` y afines son **propuestas, NO clases/entidades existentes**. No inventar controller/route/service. `Cliente.EmpresaId=NULL` legacy no implica visibilidad global.
- Cliente Angular podrá solicitar un contexto de Empresa, pero backend obtiene identidad, membresía, permisos, owner y revocación en cada operación. `X-Empresa-Id`, host, slug, cookie y frontend guard no son concesiones de autorización.
- Decisiones D01–D08 de diseño, retención, consentimiento, CAS y límites de consulta permanecen ABIERTAS. No activar contacto/campaña ni otorgar capacidades comerciales por la existencia de clientes históricos.

## 2. Ocho operaciones propuestas (identificadores, NO rutas implementadas)

| Operación | Entrada conceptual | Salida/efecto conceptual | Autorización futura |
| --- | --- | --- | --- |
| O01 ConsultarFicha | `ClienteRef`, contexto de Empresa solicitado | Ficha mínima del mismo tenant, campos filtrados | JWT + membresía activa + `crm.cliente.read` + owner del Cliente |
| O02 ListarTimeline | `ClienteRef`, cursor y límite | Interacciones del Cliente por tiempo UTC y correlación | `crm.actividad.read`; filtros tenant y sucursal verificables |
| O03 RegistrarActividad | `ClienteRef`, payload allowlisted, idempotency key | Actividad nueva o respuesta idempotente ya creada | `crm.actividad.write`; actor y Cliente en Empresa validada |
| O04 ListarOportunidades | `ClienteRef` opcional, filtro tipado y paginación | Oportunidades visibles de una Empresa | `crm.oportunidad.read`; owner y rol por recurso |
| O05 CambiarEtapaOportunidad | `OportunidadRef`, nueva etapa, versión esperada | Cambio transaccional futuro con CAS y auditoría | `crm.oportunidad.write`; asignación y estado permitido |
| O06 PrevisualizarSegmento | `SegmentoRef`, filtros permitidos, límite acotado | Conteo/resumen minimizado, no PII global | `crm.segmento.preview`; tenant y costos por consulta |
| O07 ConsultarPreferencia | `ClienteRef`, canal/finalidad | Estado de consentimiento con origen y revocación | `crm.consent.read`; propósito y permisos explícitos |
| O08 SolicitarExportacion | `ClienteRef` o segmento, finalidad autorizada | Resultado FUTURO asíncrono auditado, nunca archivo inmediato aquí | permiso separado `crm.export`, minimización, retención y rate limit |

Estos nombres y permisos son **candidatos de contrato**, NO scopes JWT existentes ni endpoints probados. Implementación posterior debe cotejar RBAC real y evitar inventar roles. `O08` no habilita worker ni proveedor.

## 3. Ocho DTO propuestos, sin clases creadas

| DTO | Campos mínimos propuestos | Restricción clave |
| --- | --- | --- |
| D01 `ClienteFichaView` | `ClienteId`, alias visible, `EmpresaId` interno, campos permitidos | correo/teléfono solo con permiso y finalidad |
| D02 `TimelinePageQuery` | `ClienteId`, `Cursor`, `Limit`, rango UTC | `Limit` acotado y cursor firmado o ligado al tenant |
| D03 `ActividadCreateRequest` | `ClienteId`, `TipoAllowlisted`, texto mínimo, `IdempotencyKey` | actor y Empresa desde servidor, no de body |
| D04 `ActividadView` | `Id`, `ClienteId`, `ActorRef`, `FechaUtc`, resultado permitido | datos de actor minimizados; evento inmutable |
| D05 `OportunidadStateChange` | `OportunidadId`, `EtapaDestino`, `ExpectedVersion` | rechazo de versión vieja y transición prohibida |
| D06 `SegmentPreviewQuery` | `SegmentoId`, filtro tipado, `Limit` | no campos SQL/JS arbitrarios ni exportación implícita |
| D07 `PreferenciaContactoView` | `ClienteId`, `Finalidad`, `Canal`, `Estado`, `RevocadoUtc` | falta de consentimiento significa no enviar |
| D08 `ExportRequest` | `Scope`, `Finalidad`, columnas allowlisted, `IdempotencyKey` | permiso de export distinto de lectura y resultado auditable |

Los campos no se reflejan en schemas actuales. Deben validarse ante la arquitectura Domain/Application/Infrastructure/API antes de cualquier implementación; un identificador cliente por sí solo no determina Empresa.

## 4. Doce errores futuros, con divulgación mínima

| Código conceptual | Situación | Conducta fail-closed requerida |
| --- | --- | --- |
| X01 `AUTH_REQUIRED` | JWT ausente/expirado | 401 seguro, sin PII |
| X02 `MEMBERSHIP_INACTIVE` | membresía revocada | 403, sin caché autorizada |
| X03 `PERMISSION_DENIED` | rol/scope insuficiente | 403, sin revelar existencia de recursos |
| X04 `TENANT_RESOURCE_MISMATCH` | A intenta recurso B | 403/404 uniforme |
| X05 `OWNER_UNRESOLVED` | Cliente legacy `EmpresaId=NULL` | denegar y solicitar reconciliación autorizada |
| X06 `VALIDATION_FAILED` | DTO fuera de límites | 400, lista segura de campos |
| X07 `STATE_CONFLICT` | CAS/versión obsoleta | 409, sin mutación parcial |
| X08 `IDEMPOTENCY_COLLISION` | misma clave, distinto payload/owner | 409, no repetir efecto |
| X09 `QUERY_NOT_ALLOWLISTED` | segmento/operador no autorizado | 400/403 seguro |
| X10 `CONSENT_NOT_GRANTED` | opt-out, revocación o propósito ausente | denegación, cero envíos |
| X11 `RATE_LIMITED` | costo, cursor o cuota excedidos | 429, reintento seguro sin datos |
| X12 `DEPENDENCY_FAILURE` | DB/proveedor/timeout/caché | 503/500 genérico, sin apertura de permisos |

Los HTTP status son propuesta, no una promesa de que el backend los responda actualmente. Registrar `correlationId` opaco sin incluir JWT, SQL ni PII.

## 5. Dieciséis validaciones de contrato

- **V01** autenticar JWT en backend y vincular sujeto a sesión autorizada.
- **V02** verificar `UsuarioEmpresa` activo al momento de cada operación y revalidar tras revocación.
- **V03** exigir permiso granular por operación real y rol, sin confiar en guard Angular.
- **V04** resolver `EmpresaId` de Cliente/recurso desde datos confiables, no desde ruta/cabecera.
- **V05** denegar `EmpresaId=NULL` legacy no reconciliado y no exponer registros globalmente.
- **V06** cotejar actor, Cliente, interacción y oportunidad con la misma Empresa en cada escritura.
- **V07** limitar DTO a campos allowlisted; evitar mass assignment de owner, rol o consentimiento.
- **V08** validar identificadores, longitudes, UTF-8, rangos UTC y payload malicioso.
- **V09** limitar paginación y cursor por tenant, actor, filtros y costo.
- **V10** ordenar timeline establemente por UTC+identificador y no duplicar por retries.
- **V11** ligar idempotency key a Empresa+actor+operación+payload y expiración.
- **V12** imponer `ExpectedVersion` y transiciones de etapa permitidas con CAS transaccional.
- **V13** segmentación exclusivamente tipada y parametrizada; prohibir SQL/JS de usuario.
- **V14** verificar consentimiento por canal, finalidad y fecha, opt-out y revocación antes de cualquier envío futuro.
- **V15** separar permiso de export del de lectura, minimizar PII, auditar actor/finalidad y retener sólo lo autorizado.
- **V16** fallos, timeout, cache miss, proveedor o JWT vencido no producen default allow; errores sin filtrado de PII.

## 6. Diez pares de pruebas futuras NO_EJECUTADAS

| Par | Positivo futuro | Negativo/falla exigido |
| --- | --- | --- |
| P01/N01 | A autorizado obtiene D01 de Cliente A | A con ID B recibe denegación sin PII |
| P02/N02 | rol lector A consulta Timeline A paginado | rol sin membresía o revocado no lee |
| P03/N03 | actividad idempotente se crea exactamente una vez | replay/concurrencia no duplica actividad |
| P04/N04 | oportunidad A cambia con versión válida | B, rol lector o versión obsoleta no mutan |
| P05/N05 | filtro allowlisted A previsualiza segmento A | SQL/JS libre, conteo B o cursor inválido fallan |
| P06/N06 | consentimiento activo en canal/finalidad devuelve estado | opt-out o falta de consentimiento bloquean uso |
| P07/N07 | exportador con permiso específico minimiza campos A | lectura básica no habilita export ni PII B |
| P08/N08 | Cliente legado reconciliado con owner explícito puede consultarse | `EmpresaId=NULL` sigue bloqueado |
| P09/N09 | rate limit responde sin cambiar datos | petición ilimitada y timeout no filtran PII |
| P10/N10 | error de dependencia produce respuesta segura y trazable | fallo DB/caché jamás permite acceso por defecto |

**Métrica de ejecución:** `0/10` pares ejecutados en esta unidad, resultado funcional `NO_MEDIDO`. R2 futura debe ejecutar xUnit, pruebas negativas JWT/RBAC A/B, integración real sobre MySQL 8.4 efímero si se toca EF, y Playwright si cambia UI. No presentar mocks o builds como certificado de API/DB.

## 7. Compatibilidad, seguridad y rollback

Las rutas REST, URI, DTO reales y códigos de error actuales deben levantarse de código antes de implementar. Estas operaciones no reemplazan API existente ni modifican permisos JWT, colas de mensajes, `Cliente`, ventas, reservas o inventario. Preservar Domain <- Application <- Infrastructure; API compone; UI es consumidor sin autoridad. Ningún payload enviado por frontend reasigna Empresa o actor.

**Compatibilidad futura:** versionar los DTO, mantener campos opcionales explícitos, evitar cambios rompientes de controladores y aplicar lectura dual/migración sólo con plan probado. Legacy `Cliente.EmpresaId=NULL` se trata como cuarentena hasta decisión D02. No exportar ni contactar por defecto.

**Rollback de ESTE cambio R0:** `git revert` normal del commit de documentación tras comprobar HEAD y escritores, no force-push, no borrar BITACORA. Si se detecta un riesgo no aclarado, mantener fase técnica bloqueada; ningún DDL o deploy se ejecutó aquí.

## 8. Criterios A0–A5 exactos

- **A0:** plan, COLA G/O, DAG F/G y A5 preparatoria verificados; candado V17 `SQV17_C09_01_M02_02` ID `2000001305` sobre H:I fila 1305, un único scope y diff de dos Markdown.
- **A1 R0:** revisar 8 operaciones, 8 DTO, 12 errores, 16 validaciones y 10 pares P/N como **contratos futuros**, sin afirmar API real.
- **A2 R0:** comprobar estáticamente tenant A/B, JWT/RBAC, revocación, PII, legacy NULL, idempotencia, CAS, consentimientos y errores fail-closed; no certificar seguridad runtime.
- **A3:** `NO_APLICA` para integración HTTP, .NET, MySQL/EF, Angular/Playwright y proveedores por ser exclusivamente Markdown.
- **A4:** publicar una sola entrega documental con `CHANGELOG_AI.md` autocontenido en dev mediante fast-forward/CAS, verificar Scope Lock y procedencia V5 `completed/success` de SHA exacto o equivalencia demostrada, y efectuar readback PLAN/BITACORA. Una CI en curso no equivale a PASS.
- **A5:** revisión por una **Task Javier distinta de DEV03**, con diff/SHA/criterios y pruebas verificables; LISTO_DEV no habilita automáticamente fases técnicas. QA/main/PROD y servicios reales no autorizados por este contrato.
