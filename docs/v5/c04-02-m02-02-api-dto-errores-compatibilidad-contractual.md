# SOLQARYN V5 — C04-02.M02.02: contratos propuestos API/DTO/errores y compatibilidad

**Autor:** PRIMARY DEV 06 — JAVIER (TASK_ID 6af16bbad26881918c283abe06e1f32a). **Fecha:** 2026-11-15. **Ámbito:** solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT. **Naturaleza:** diseño conceptual M02 de API/DTO/validaciones. Ningún endpoint, servicio, cliente Angular, migración ni cobro se implementa en esta microtarea.

## 1. Fuente, dependencias y contraste con código vivo

Fuente ejecutable única: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES, Doc 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA; PLAN_MAESTRO fila 621, C04-02.M02.02 «Definir API, DTO, errores, validaciones y compatibilidad». En el preflight PLAN H621=PENDIENTE/I621 vacío, COLA G621/O621=SI, DAG_TECNICO F582=VALIDADO/G582=SI, tras lo cual se realizó claim único con H621 EN_PROGRESO, I621=DEV06 y readback. C04-02.M02.01 quedó LISTO_DEV documental con A5 pendiente ajena; los dos diseños M02 son independientes según gate dinámico del DAG, pero se desarrollan estrictamente de uno en uno.

API existente: backend/src/API/Controllers/SuscripcionesSaaSController.cs (blob cab95db5a33c6a15448e2437481eaaac25ffc4fe) expone ruta saas/tenants/{empresaId}, onboarding, suscripcion, limites y módulos/entitlement; aplica Authorize y RequierePermiso. La ruta empresaId es selección, no autoridad. DTO existentes en backend/src/Application/DTOs/SuscripcionesSaaSApiContracts.cs (blob 6045a167fb90fe3dea2fa87280d0a29cec57c6be) contienen OnboardingSuscripcionSaaSRequest, SuscripcionSaaSDto, LimitesSuscripcionSaaSQuery, EntitlementModuloSaaSDto y códigos SAAS_*; **NO** incluyen un contrato económico legal versionado. El puerto en backend/src/Application/Interfaces/ISuscripcionesSaaSService.cs (blob ee320815a588ff28a6c60bf3d9634991a050e6a1) requiere validar tenant con IUsuarioScopeService antes de I/O, no autoriza un contrato nuevo. El cliente Angular frontend/src/app/core/models/suscripcion-saas.model.ts (blob efbd1c24ffd12e2475645d21553cc70451dd92cc) conserva sus propiedades de suscripción técnica, no precio ni mora.

Las decisiones económicas D01–D08 siguen **NO APROBADAS**, según el precedente M01.06. La superficie siguiente es solo propuesta detrás de permisos y políticas futuras; NO registrar rutas nuevas ni alterar las existentes en esta entrega.

## 2. Diez operaciones API **propuestas**, sin servidor implementado

Toda ruta siguiente es una ilustración de diseño bajo prefijo versionado potencial api/v1/tenants/{empresaId}/contratos; **no está disponible hoy**. Su ruta no determina autorización: Authorization, permiso granular contractual, TenantScope server-side y control de ownership han de ejecutarse antes de repositorio. IDs no autorizados responden sin revelar existencia entre empresas.

| Operación futura | Método lógico y recurso | Control exigido y frontera |
| --- | --- | --- |
| API01 | POST /contratos/borradores | EmpresaId verificado, rol de borrador; ninguna alta financiera |
| API02 | GET /contratos/{contratoId} | Solo propietario tenant y miembro vigente, redacción PII |
| API03 | GET /contratos?estado=&cursor= | Filtro server-side por tenant, paginación acotada |
| API04 | POST /contratos/{id}/versiones | Crear nueva versión; no mutar versión aceptada |
| API05 | POST /contratos/{id}/aceptaciones | Representación D01, términos D02 y consentimiento verificable |
| API06 | POST /contratos/{id}/solicitudes-renovacion | Propuesta sin renovar/cobrar automáticamente; D03/D04 |
| API07 | POST /contratos/{id}/suspensiones | Estado previo válido y política D05 aprobada |
| API08 | POST /contratos/{id}/reactivaciones | Reconciliación y autoridad, nunca reactivar terminal |
| API09 | POST /contratos/{id}/cancelaciones | Motivo y D07 aprobados; sin reembolso implícito |
| API10 | GET /contratos/{id}/historial | Lectura filtrada, auditoría estable y retención D08 aprobada |

No se propone ningún endpoint directo para crear cargos, migrar DB, publicar un dominio, activar dispatcher, borrar historial, ejecutar morosidad automática o conceder RBAC por UI. Los métodos POST con efectos permanecen NO_GO hasta decisiones y pruebas.

## 3. Nueve DTO de diseño (nombres conceptuales, no tipos compilados)

| DTO propuesto | Campos de negocio mínimos | Exclusión de autoridad |
| --- | --- | --- |
| ContratoBorradorSolicitud | Propuesta de términos/version, referencia autorizada, correlación | Sin EmpresaId autorizante, precio supuesto ni estado aprobado |
| ContratoResumenRespuesta | ContratoId, VersionId, estado, fechas UTC, revisión ETag | No expone PII de otras empresas |
| ContratosPaginaRespuesta | Items, cursor, hasMore, total si política lo permite | Cursor no rompe tenant ni excede límites |
| VersionContratoSolicitud | BaseVersion, referencia de términos, hash esperado | Nunca modificar versión aceptada |
| AceptacionContratoSolicitud | VersionId, evidencia consentimiento, actor reconocido | El actor se resuelve servidor, no por input |
| RenovacionPropuestaSolicitud | VersionBase, periodo propuesto UTC, motivo | No autoriza tarifa ni renovación real |
| TransicionContratoSolicitud | EstadoEsperado, versión, causa, correlationId | No permite elegir cualquier estado arbitrario |
| CancelacionContratoSolicitud | Motivo, fecha efectiva, referencia aprobación | Sin refund automático |
| HistorialContratoRespuesta | TimestampUtc, estado anterior/nuevo, actor redactado, causa | Sin secreto, payload económico sensible o tenant B |

Todos los request se validan con esquema estricto y límites explícitos de tamaño. No aceptar campos extra para cambiar EmpresaId, PrincipalId, Rol, SuscripcionId confiable ni moneda/impuesto no aprobados. El servidor debe construir el ámbito autorizado y los campos de auditoría desde contexto propio. La documentación no registra nuevos DTO en Angular/.NET.

## 4. Matriz de errores futuros y no filtración

| Código lógico previsto | HTTP sugerido según política futura | Manejo requerido |
| --- | --- | --- |
| CONTRACT_AUTH_REQUIRED | 401 | JWT ausente/expirado, sin detalle de usuario |
| CONTRACT_ACTION_FORBIDDEN | 403 | Miembro/rol/acción no autorizada |
| CONTRACT_NOT_FOUND | 404 | Contrato inexistente **o ajeno** con respuesta indistinguible |
| CONTRACT_INVALID_INPUT | 400 | Esquema, tipo o fecha inválidos, mensajes genéricos |
| CONTRACT_VERSION_CONFLICT | 409 | ETag/versión esperada difiere; no sobrescribir |
| CONTRACT_INVALID_STATE | 409 | Transición incompatible con estado real |
| CONTRACT_IDEMPOTENCY_CONFLICT | 409 | Misma clave con payload diferente |
| CONTRACT_APPROVAL_REQUIRED | 422 o 409 | D01–D08 ausentes; nunca fingir autorización |
| CONTRACT_POLICY_UNAVAILABLE | 503 | Política/proveedor no confiable, no cobrar/suspender |
| CONTRACT_RATE_LIMITED | 429 | Límites de intento y cabeceras no reveladoras |
| CONTRACT_RECONCILIATION_PENDING | 409 | Conciliación aún no acredita pago/mora |
| CONTRACT_INTERNAL_ERROR | 500 | CorrelationId y error redactado, sin stack/PII |

La matriz de status es **propuesta**; los handlers y pruebas actuales deben mantenerse sin cambios. 404 uniforme para evitar BOLA/IDOR; validar detalles de negociación 401/403 sin reducir las protecciones existentes.

## 5. Contrato de idempotencia, compatibilidad y seguridad

- **Versionado:** introducir una ruta distinta y DTO diferenciados del contrato técnico SaaS. Mantener formas, estados y respuestas actuales de /onboarding, /suscripcion, /limites, /modulos/{clave}/entitlement, sin reinterpretarlas como contratos legales pagados.
- **Optimistic concurrency:** versión esperada/ETag en transiciones; rechazar carreras y replay con payload divergente. Idempotency-Key por tenant, acción y hash de solicitud, con ledger durable **por implementar y probar** en MySQL 8.4 efímero antes de cualquier operación financiera.
- **Aislamiento:** verificar membresía/rol tenant A/B antes del primer I/O y en cada recurso hijo; no usar el EmpresaId del body/ruta como fuente de permiso. Tokens revocados y sesiones vencidas fallan cerrado. No equiparar entitlement con RBAC.
- **Validaciones:** UTC obligatorio, fechas monotónicas, strings normalizadas y con límite, límites de página acotados, semántica de terminales, versiones inmutables, aceptación explícita y política económica autorizada. Rechazar payload excesivo, claves duplicadas y campos inesperados.
- **Compatibilidad Angular:** frontend existente no consume rutas nuevas hasta integración autorizada y pruebas de routing, teclado, mensajes de error, loading, privacidad y accesibilidad; API y UI separan experiencia de autorización real.
- **Observabilidad:** correlación y auditoría con identificadores de evento y estado sin secretos ni información de otra empresa; retención/exportación requieren D08 y política legal aprobada. No emitir eventos reales.

## 6. Doce pares de aceptación FUTURA, no tests ejecutados

| Positivo por acreditar en etapa runtime | Negativo obligatorio en etapa runtime |
| --- | --- |
| P01 Crear borrador de A con rol válido | N01 B intenta crear para A por path/body alterados |
| P02 Obtener contrato propio | N02 B accede por ContratoId ajeno y recibe respuesta indistinguible |
| P03 Paginación dentro de límite | N03 cursor de B o tamaño desbordado |
| P04 Nueva versión sobre base vigente | N04 sobrescribir versión aceptada |
| P05 Consentimiento y rol documentados | N05 aceptación con actor provisto solo por body |
| P06 Renovación propuesta posterior a periodo | N06 renovar retroactivo o con precio no aprobado |
| P07 Suspender con D05 y causa válida | N07 suspender por timeout o no conciliado |
| P08 Reactivar suspendida con permiso | N08 reactivar cancelada/expirada |
| P09 Cancelar con política D07 | N09 reembolso/cargo tácito |
| P10 Leer historial del mismo tenant | N10 fuga tenant B o auditoría mutable |
| P11 Replay exacto idempotente | N11 misma clave con payload distinto o carrera ganadora |
| P12 Error seguro con correlationId | N12 stack trace, secreto o PII en error/log |

**Los 24 oráculos anteriores están definidos en Markdown y NO son ejecuciones de xUnit, HTTP, Playwright, MySQL ni pruebas de seguridad runtime.** Requerir tests funcionales positivos/negativos en el SHA del código futuro, MySQL/EF reales con servicio efímero si existe persistencia, y controles de autorización tenant A/B antes de declarar A1–A3 de implementación.

## 7. Impacto y aceptación documental A0–A5

**A0:** verificados fuente institucional, PLAN/COLA/DAG por ID, código .NET API/DTO/puertos y tipo Angular existentes; diff limitado a esta especificación y CHANGELOG_AI. **A1:** consistencia de 10 operaciones, 9 DTO, 12 errores y 12 pares P/N futuros, no ejecución funcional. **A2:** contrato de rechazo JWT/RBAC/tenant A/B, BOLA, idempotencia y errores privados documentados sin cambiar permisos. **A3:** compatibilidad conceptual entre Domain, Application, API y Angular; pruebas de backend/frontend/MySQL/HTTP no aplican al diff Markdown. **A4:** commit dev fast-forward/CAS, Scope Lock y verificación de procedencia V5 en completed/success sobre SHA publicado, PLAN y BITACORA más readback. **A5:** revisión por Task Javier distinta del autor, pendiente; nunca autoprobación.

No se crean endpoints, se aceptan decisiones D01–D08, se habilita billing, se ejecutan migraciones, se cambian contraseñas/secretos ni se modifica QA/main/PROD. **Rollback documental:** git revert ordinario preservando cambios ajenos, sin alterar BITACORA histórica ni borrar datos.
