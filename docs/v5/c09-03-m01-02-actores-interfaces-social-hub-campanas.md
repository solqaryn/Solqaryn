# SOLQARYN V5 — C09-03.M01.02: actores, interfaces y fronteras de Social Hub y campañas

**Microtarea:** C09-03.M01.02 · PLAN_MAESTRO fila 1371 · Autor: PRIMARY DEV 02 JAVIER, TASK_ID `6af16bb9492c8191b8e8c7d606f11507` · 2026-11-14 · `solqaryn/Solqaryn@dev` · `PROJECT_SCOPE_LOCK=STRICT`.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Salida de M01.02:** identificar entradas, salidas, actores y superficies afectadas. Las «campañas, contenido, segmentos, calendarios y métricas» del C09-03 son capacidad **FUTURA**. Esta unidad no crea un flujo de campañas operativo ni habilita proveedores.

**Preflight:** `PLAN_MAESTRO!H1371=PENDIENTE` sin propietario, `COLA!G1371/O1371=SI`, `DAG_TECNICO!F1332=VALIDADO/G1332=SI` antes del claim exclusivo. El DAG autoriza este contrato documental M01, no la ejecución, aprobación A5, importaciones o promoción de entorno.

## Actores A01–A14 propuestos — no confundir con roles ya creados

| ID | Actor/sistema futuro | Entrada pretendida y autoridad requerida |
| --- | --- | --- |
| A01 | Administrador empresarial de marketing | Define configuración sólo de su Empresa; JWT, membresía y permiso backend |
| A02 | Editor de contenido de la empresa | Prepara borradores dentro del tenant, sin privilegios de publicación por defecto |
| A03 | Aprobador/publicador empresarial | Autoriza publicación específica con rol y contexto vigentes |
| A04 | Operador de cumplimiento/consentimiento | Gestiona preferencias con trazabilidad y propósito, sin acceso global a PII |
| A05 | Cliente/contacto final | Puede gestionar preferencias propias, nunca consultar segmentos ajenos |
| A06 | Usuario con acceso de solo lectura | Consulta métricas autorizadas y agregadas, sin exportaciones irrestrictas |
| A07 | Soporte de plataforma sujeto a autorización extraordinaria | Diagnóstico auditable con mínimo privilegio y límite tenant explícito |
| A08 | Servicio de identidad y pertenencia | Resuelve claims JWT, membresía UsuarioEmpresa y permiso real, sin confiar en cabecera |
| A09 | Servicio de segmentación propuesto | Evalúa filtros tipados dentro de la empresa, sin SQL/JS arbitrario |
| A10 | Calendario/planificador futuro | Mantiene intención programada; sin activación automática en esta fase |
| A11 | Motor de preferencias propuesto | Comprueba opt-in, revocación, finalidad y canal en cada intento futuro |
| A12 | Adaptador de mensajería futuro C09-04 | Recibe intención autorizada y deduplicable; envío NO habilitado |
| A13 | Proveedor social/externo futuro | Requiere contrato, límites, credenciales y autorización separada |
| A14 | Auditoría y observabilidad | Registra actor, tenant y correlación sin secretos ni PII excesiva |

Los actores A01–A14 constituyen **responsabilidades conceptuales**. No se afirma que existan esos 14 roles, servicios, permisos o endpoints.

## Interfaces I01–I14, entradas y salidas propuestas

| ID | Frontera o interfaz futura | Entrada mínima y validación | Salida controlada |
| --- | --- | --- | --- |
| I01 | Angular borradores -> API de contenidos | JWT, Empresa activa, permiso editar, contenido permitido | Borrador versionado, nunca publicación |
| I02 | Angular aprobación -> API | Documento y revisión esperada, rol de aprobador | Aprobación auditada o denegación |
| I03 | API -> Application | Contexto resuelto por backend, comando tipado, correlation ID | Resultado seguro sin PII cruzada |
| I04 | Application -> Domain | Empresa, reglas de estado y presupuesto de cuota | Validación determinista, sin efecto externo |
| I05 | API -> servicio segmentos | Filtros allowlist y pertenencia vigentes | IDs mínimos sólo del tenant |
| I06 | CRM C09-01 -> Social Hub | Referencias a Cliente y finalidad | Elegibilidad futura, nunca consentimiento inferido |
| I07 | Preferencias -> planificador | Consentimiento por canal y revocación | Bloqueo/permiso vigente de intención |
| I08 | Planificador -> outbox C09-04 futuro | Clave idempotente empresa+campaña+destino | Intención durable, sin enviar aún |
| I09 | Backend -> proveedor externo futuro | Credencial fuera de repositorio, cuota y alcance | ACK/error normalizado; no garantía de entrega |
| I10 | API -> métricas agregadas | Rango temporal/tenant/rol, sin consulta arbitraria | Estadísticas con umbral de privacidad |
| I11 | Auditoría -> registros | Actor, evento, motivo, Empresa y correlación | Registro minimizado, sin secretos |
| I12 | Cliente -> preferencias/opt-out | Identidad/verificación del canal | Baja verificable, no exposición de perfil |
| I13 | Gestión de medios -> contenido | Identificador autorizado, tipo MIME y límites | Referencia permitida; no SSRF ni subida libre |
| I14 | UI accesible -> API | Formulario validado, estado y prevención doble acción | Respuesta, error seguro, foco y feedback |

En todos los contratos, **el tenant y RBAC se resuelven en API**, no desde el título del sitio, host, cookie, slug, querystring ni `X-Empresa-Id` aportado por el navegador. La interfaz de consentimiento debe distinguir marketing, transacciones y finalidades específicas.

## Evidencia estática observada en dev

| Referencia real | Blob Git | Frontera acreditada y límite |
| --- | --- | --- |
| `docs/v5/c09-03-m01-01-alcance-social-hub-campanas.md` | `200a942044bac2b5c0a294ecacdf0ab75068a9ce` | Contrato previo de NR01–NR16, no funcionalidad live |
| `backend/src/Domain/Entities/Cliente.cs` | `8c6d4c4453e44f8c0be9475340465e1aadb3f787` | Cliente con EmpresaId nullable legado; sin opt-in global acreditado |
| `backend/src/Domain/Entities/UsuarioEmpresa.cs` | `f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c` | Relación usuario/empresa; rol efectivo exige verificación adicional |
| `backend/src/Domain/Entities/MensajeOutbox.cs` | `891572626c19cd6cf8f522fca5ec876333286ffb` | Contrato de intención durable, no scheduler o dispatcher certificado |
| `backend/src/Domain/Entities/RegistroAuditoria.cs` | `d669ac397a78ba02a5ba19e2cfb4bc9c7a115462` | Registro de auditoría, no plataforma de analítica de campañas |
| `frontend/src/app/core/guards/permiso.guard.ts` | `1052bd38041055cc81e84a14d5fe0364e169b1fe` | Guard Angular, nunca autorización backend suficiente |

Este conjunto acota el preflight; **no** es un inventario exhaustivo del repositorio ni certifica integración con proveedores. M01.03 realizará inventario ampliado de código y evidencia por blob.

## Fronteras de confianza F01–F10

- **F01 navegador/API:** guard UI jamás concede permisos. Toda acción se deniega sin JWT/membresía/permiso server-side.
- **F02 tenant A/B:** empresa de contexto, ID de recurso y destinatario deben coincidir o mostrar vínculo autorizado por política vigente.
- **F03 legado:** `EmpresaId=NULL` no permite alcance global; exigir reconciliación verificada.
- **F04 consentimiento:** contacto y compra no significan autorización comercial; opt-out invalida envíos futuros pendientes.
- **F05 segmentación:** atributos/operadores allowlisted, límites de volumen y minimización; no ejecutar expresiones arbitrarias.
- **F06 contenidos/medios:** validar formato, XSS, redirecciones, tamaños, URLs y acceso a adjuntos por tenant.
- **F07 calendarios/outbox:** controlar carreras, reintentos, dedupe, cuotas y cambios de autorización al ejecutar.
- **F08 proveedores:** credenciales referenciadas fuera de Git; no publicar mensajes reales por este contrato.
- **F09 analítica:** evitar exposición individual de contactos mediante filtros, exportación o resultados de baja cardinalidad.
- **F10 observabilidad:** logs con IDs de correlación, no tokens, emails completos, PII innecesaria ni datos de B.

## Pruebas futuras — NO EJECUTADAS

| ID | Escenario de aceptación propuesto | Resultado exigible |
| --- | --- | --- |
| P01 | Editor con permiso y tenant A crea borrador tipado | Borrador solo A, sin publicación |
| P02 | Aprobador autorizado aprueba versión válida | Transición auditada y versionada |
| P03 | Usuario pide opt-out por canal propio | Revocación eficaz antes del próximo envío |
| P04 | Tenant A consulta métricas agregadas permitidas | Solo agregados de A con privacidad |
| N01 | Cliente de A intenta editar pieza de B | Denegación server-side |
| N02 | Rol solo lectura llama API de publicación | Denegación pese a UI manipulada |
| N03 | EmpresaId no reconciliado / JWT revocado | Fail-closed |
| N04 | Consentimiento ausente o revocado | No publicación ni emisión |
| N05 | Replay, doble clic o reintento duplicado | Rechazo/deduplicación |
| N06 | SQL/JS/XSS/URL insegura como entrada | Rechazo y salida segura |
| N07 | Provider caído, cuota agotada o secreto ausente | Error limitado sin reintento infinito |
| N08 | Intento de exportar PII de segmento B | Denegación y sin filtración |

**Estado:** `FUTUROS_NO_EJECUTADOS`. Las validaciones reales de .NET/MySQL/Angular/Playwright, emisión de proveedores y métricas se reservan a fases posteriores, sujetas a gates, CI y autorización correspondiente.

## Impacto, verificación y rollback

- Sólo se agrega este documento `docs/v5/` y una sección autocontenida del `CHANGELOG_AI.md`. No se cambian Domain/Application/Infrastructure/API, controladores, DTO, tablas, secretos, workers, servicios, observabilidad productiva ni QA/main/PROD.
- **A0:** verificar alcance del diff, fuentes, títulos y límites; **A1:** comprobar que se identifican actores/interfaces y respuestas positivas/negativas en el documento; **A2:** comprobar fronteras tenant A/B, JWT/RBAC, privacidad, consentimiento y idempotencia; **A3:** trazar blobs originales y contrato M01.01; pruebas runtime `NO_APLICA` motivado; **A4:** fast-forward CAS y readback de dos blobs, check `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` del SHA exacto, estado PLAN/COLA y evento BITACORA.
- **A5:** exclusivamente otra Task Javier distinta al autor, con evidencia del mismo commit. Este documento no habilita emisiones ni promoción comercial.
- **Rollback:** nuevo `git revert` de la publicación, sin force-push, sin borrar registros históricos ni deshacer trabajo ajeno.
