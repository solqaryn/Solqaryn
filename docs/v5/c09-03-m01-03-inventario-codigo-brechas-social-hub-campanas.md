# SOLQARYN V5 — C09-03.M01.03: inventario de código y brechas de Social Hub/campañas

**Microtarea:** C09-03.M01.03 · PLAN_MAESTRO fila 1372 · Autor PRIMARY DEV 02 JAVIER (`6af16bb9492c8191b8e8c7d606f11507`) · Fecha 2026-11-14 · `solqaryn/Solqaryn@dev` · `PROJECT_SCOPE_LOCK=STRICT`.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. El alcance de esta unidad es «Leer contratos, código y evidencia disponibles sin asumir certificación». La futura capacidad Social Hub/campañas abarca contenido, segmentos, calendarios y métricas, con límites, consentimiento y opt-out. **No implementar servicios, workers, webhooks, eventos o APIs en M01.03.**

**Preflight:** `PLAN_MAESTRO!H1372=PENDIENTE` sin owner, `COLA!G1372/O1372=SI`, `DAG_TECNICO!F1333=VALIDADO/G1333=SI`; claim exclusivo por Task Javier 02 y readback antes del cambio. Es aptitud documental, no permiso comercial para tratar PII.

## Evidencia estática verificada por ruta y blob

Las siguientes catorce rutas se leyeron desde el repositorio original y rama `dev`. Cada SHA identifica contenido existente del árbol consultado, no un hallazgo de seguridad ni un test funcional.

| Capa | Ruta vigente | Blob SHA | Capacidad confirmable / brecha específica |
| --- | --- | --- | --- |
| Domain | `backend/src/Domain/Entities/Cliente.cs` | `8c6d4c4453e44f8c0be9475340465e1aadb3f787` | Cliente tenant-legacy nullable; NO consentimiento de campaña |
| Domain | `backend/src/Domain/Entities/UsuarioEmpresa.cs` | `f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c` | Membresía de usuario/empresa; NO permiso marketing inferido |
| Application | `backend/src/Application/Interfaces/IContextoTenantService.cs` | `97d0a05456c66d069a56b9743bbc286af07502b1` | Contrato de tenant; no certifica todos los endpoints |
| API | `backend/src/API/Controllers/ClientesController.cs` | `023d639f5c6ebf20519a22b175e2bf17c53e6d0d` | Superficie cliente; no API de campañas |
| Application | `backend/src/Application/Services/ClienteService.cs` | `51661879903e96064557f1a1b4bd3f2c1c79a77b` | Gestión cliente actual, no segmentador universal certificado |
| Infrastructure | `backend/src/Infrastructure/Repositories/ClienteRepository.cs` | `07bb969b52ea4273ac31975ebbc10d19af8a9d75` | Repositorio Cliente, no tabla de consentimiento acreditada |
| Domain | `backend/src/Domain/Entities/MensajeOutbox.cs` | `891572626c19cd6cf8f522fca5ec876333286ffb` | Intención durable, no publicación automatizada externa |
| Application | `backend/src/Application/Services/MensajeOutboxService.cs` | `a00d353c474d7d7f785a58f443c66c5f12a542f7` | Staging de intención con límites; no marketing habilitado |
| Application | `backend/src/Application/Services/OutboxRetryPolicy.cs` | `a9bcacda24a7ba031a271b527d8ca2043a8510b9` | Política de reintentos, no prueba entrega externa |
| API | `backend/src/API/Controllers/EmailEmpresarialController.cs` | `e7acfb132c9aedf0e22be9cb3acd6e86e4718833` | Canal empresarial; no permiso de campañas implícito |
| Infrastructure | `backend/src/Infrastructure/Services/SmtpEmailService.cs` | `7ff845fb6571b66c5da5d41be85b79ade92e27cf` | Transporte SMTP existente; no contacto promocional consentido |
| Frontend | `frontend/src/app/core/interceptors/auth.interceptor.ts` | `4bdf19135ed1932a99fd09ace1f900bcccc5b25b` | Contexto solicitado por cliente, nunca autorización suficiente |
| Frontend | `frontend/src/app/core/guards/permiso.guard.ts` | `1052bd38041055cc81e84a14d5fe0364e169b1fe` | Guard UX; API confirma permisos |
| Domain | `backend/src/Domain/Entities/RegistroAuditoria.cs` | `d669ac397a78ba02a5ba19e2cfb4bc9c7a115462` | Estructura auditoría; no métricas de marketing |

**Límite del inventario:** se seleccionaron 14 rutas relevantes para identidad, tenant, clientes, mensajería y UI. No se ha buscado exhaustivamente cada ruta del repositorio, ni se certifica inexistencia absoluta de campañas en otro módulo o proveedor. Las observaciones negativas significan «no demostrado por esta fuente», nunca «definitivamente ausente». La selección se realizó por revisión estática de código y contratos, sin acceso a clientes, bases de datos, contactos o secretos.

## Contratos previos trazables

| Referencia documental | Blob SHA | Aporte y límite |
| --- | --- | --- |
| `docs/v5/c09-03-m01-01-alcance-social-hub-campanas.md` | `200a942044bac2b5c0a294ecacdf0ab75068a9ce` | Objetivo, exclusiones, NR01–NR16; no workflow operativo |
| `docs/v5/c09-03-m01-02-actores-interfaces-social-hub-campanas.md` | `4e2458b7dec1018de610b485c5aedf8fbf8f3f20` | A01–A14, I01–I14, F01–F10 conceptuales; no nuevas APIs |
| `docs/v5/c09-01-m01-03-inventario-codigo-brechas-crm.md` | `34e0a8a652a9e92900644c3e7c8fc59ada58b5f8` | Inventario CRM de contexto; no prueba segmentación marketing |
| `docs/v5/c09-04-m01-03-inventario-codigo-brechas-mensajeria.md` | `3626d69599d4c96f87c69464558dac31a9fa9ba8` | Contratos de transporte y outbox; no habilita campañas |

## Brechas/questions G01–G14 — no tratarlas como defectos confirmados

| ID | Pregunta que deberá contestarse antes de implementación | Evidencia que faltaría |
| --- | --- | --- |
| G01 | ¿Cómo verificar consentimientos por canal/finalidad sin depender de Cliente.Contacto? | Modelo de preferencias y tests positivos/negativos |
| G02 | ¿Cómo materializar opt-out efectivo antes de cada efecto externo? | Contrato atómico con casos revocación/competencia |
| G03 | ¿Qué actor y permiso pueden crear, aprobar o publicar una campaña? | RBAC específico y pruebas API de acceso denegado |
| G04 | ¿Cómo se resuelve el EmpresaId nullable legado? | Política de ownership y casos A/B/NULL |
| G05 | ¿Qué atributos se autorizan en filtros de segmentos? | Allowlist tipada y evaluación segura |
| G06 | ¿Cómo versionar contenido, calendario y revisión? | Modelo de estados/versiones e idempotencia |
| G07 | ¿Cómo evitar XSS/SSRF y adjuntos de tenant ajeno? | Validador de contenido, tipo y propiedad |
| G08 | ¿Qué contrato separado rige email/WhatsApp/redes sociales? | Adaptadores autorizados, cuotas, secretos aislados |
| G09 | ¿Cómo prevenir duplicación en retry/replay? | Clave idempotente por tenant/canal/destino |
| G10 | ¿Cómo preservar auditoría sin registrar PII o tokens sensibles? | Esquema de log minimizado y pruebas |
| G11 | ¿Qué agregaciones de métricas evitan reidentificación? | Umbrales de privacidad y contratos de exportación |
| G12 | ¿Cómo representar fallo/quota del proveedor sin reintentos infinitos? | Máquina de estados, DLQ y límites auditados |
| G13 | ¿Qué interfaces Angular serán accesibles y ligeras? | Diseño responsive, teclado, foco y Playwright futuros |
| G14 | ¿Cómo desactivar campañas sin perder historia comercial? | Rollback lógico y simulación de escenarios |

**Distinción obligatoria:** una brecha de *evidencia* no equivale a una vulnerabilidad confirmada, ni a una justificación para habilitar nuevas APIs. C09-03.M02 será la etapa apropiada para diseñar contratos; M03 para código real con pruebas .NET/MySQL/Angular y controles de seguridad. La orden vigente prohíbe implementar eventos ahora.

## Escenarios de comprobación futura — NO EJECUTADOS

| ID | Prueba planificada | Resultado esperado |
| --- | --- | --- |
| P01 | Usuario de empresa A autorizado consulta sus borradores | Sólo recursos A y mínimo privilegio |
| P02 | Opt-out previo a campaña | Intención pendiente no publicada |
| P03 | Segmentación con campos allowlisted | IDs mínimos del tenant solicitante |
| P04 | Intención repetida con misma clave tenant/canal/destino | Dedupe, sin doble efecto |
| N01 | API recibe ID de segmento de B | Denegación sin filtrar PII |
| N02 | JWT revocado, rol lector o membresía eliminada | Denegación backend |
| N03 | Cliente.EmpresaId NULL no reconciliado | Fail-closed, sin alcance global |
| N04 | Falta consentimiento o hay opt-out | Nunca se programa/emite |
| N05 | Payload con SQL/JS/XSS o enlace inseguro | Rechazo de datos no permitidos |
| N06 | Dos workers/reintentos concurrentes | No efectos duplicados |
| N07 | Provider caído o sobre cuota | Fallo acotado y correlacionado |
| N08 | Consulta de métricas de B disfrazada de filtro global | Denegación y no inferencia |

Estas pruebas son `FUTUROS_NO_EJECUTADOS`. No se ejecutó ningún workflow de marketing, envío SMTP, webhook, migración o test de integración de esta capacidad por este changeset.

## Impacto, auditorías y rollback

- **Componente primario:** preparación documental del Social Hub. **Archivos cambiados:** este `docs/v5/` y una sección completa del `CHANGELOG_AI.md`; sin cambios Domain/Application/Infrastructure/API/Angular, tablas, migraciones, secretos, servicios, cuentas, QA/main/PROD.
- **A0:** verificar las 14 rutas/blobs y 4 contratos documentales, solo documentación + changelog, no encabezados intercalados.
- **A1:** revisión estática de G01–G14 y pruebas positivas/negativas propuestas; `NO_APLICA` para tests funcionales runtime.
- **A2:** revisar riesgos tenant A/B, JWT/RBAC, legado NULL, consentimiento/opt-out, replay, HMAC, PII, credenciales sin declarar vulnerabilidades existentes.
- **A3:** trazar capas/consumidores y diferenciar código existente de capacidades futuras; MySQL, HTTP y Playwright `NO_APLICA` para una edición exclusivamente Markdown.
- **A4:** fast-forward CAS exact-HEAD, diff de dos archivos, blobs, checks `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` en `completed/success` sobre commit exacto y readback PLAN/COLA/BITACORA.
- **A5:** únicamente otra de las cinco Tasks Javier, con SHA y evidencia originales. No autoaprobar ni liberar QA/main/PROD.
- **Rollback documental:** realizar `git revert` sobre dev mediante commit nuevo, sin reset/force-push ni alteración del registro histórico.
