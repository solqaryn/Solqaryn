# SOLQARYN V5 FINAL — C15-02.M02.02: contratos API/DTO, errores, validaciones y compatibilidad de auditoría de arquitectura

**Corte:** 2026-11-14 America/Tegucigalpa. **Autor:** PRIMARY DEV 04 JAVIER, TASK_ID 6aeacb7820ac8191b7d768a6f33dfa09. **Ámbito:** solqaryn/Solqaryn, rama dev, Scope Lock STRICT. **Naturaleza:** diseño documental, no endpoints ejecutables.

## 1. Autoridad y fronteras

Fuente única: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL y PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA del Sheet institucional. Predecesor C15-02.M01.06 con A5 independiente y matriz de 16 bloqueos, 8 reversión y 8 gates; hermano M02.01 define entidades conceptuales ENT01–ENT16, ownership e INV01–INV16. El DAG permite preparación de contratos M02, no migración, API real ni recursos Cloudflare/Hostinger/R2.

Arquitectura actual: Angular 22.2.1; .NET 10 API; Domain→Application→Infrastructure con persistencia MySQL/EF Core 10 proveedor Oracle. JWT/RBAC, CORS, tenant y permisos se validan exclusivamente en backend. Hosting actual Vercel/Render/Aiven/Cloudinary según arquitectura; cualquier inventario vivo queda NO_VERIFICADO. No se cambian endpoints, DTO de código, base de datos, job, DNS, certificado, secreto, entorno ni provider.

## 2. Operaciones conceptuales de sólo lectura y prevalidación (API01–API12)

Los nombres DTO y rutas son identificadores contractuales **futuros**, no promesas de endpoints ni rutas implementadas. No se autorizan mutaciones reales: toda consulta requiere identidad autenticada, tenant resuelto por backend, rol y fuente versionada. Las salidas jamás incluyen secretos ni topology privada ajena.

| ID | Intención de contrato | Entrada conceptual | Respuesta conceptual | Regla de denegación |
| --- | --- | --- | --- | --- |
| API01 | Consultar estado de auditoría | AuditoriaConsulta{tenantScope,sourceSha} | ResumenReadOnly{estado,evidenceRef} | No inferir acceso por tenant de cliente |
| API02 | Leer inventario de proveedores | InventarioFiltro{ambiente,tipo} | RecursoResumen{provider,ownerVerificado}[] | Sin inventario real, responder NO_ACREDITADO |
| API03 | Leer frontera del servicio | ServicioRef{serviceId,sourceSha} | ContratoFrontera{auth,dependencias} | Ocultar topología privada de tenant ajeno |
| API04 | Solicitar plan de ensayo aislado | EnsayoSolicitud{manifestRef,scope} | EnsayoPlan{precondiciones,noGo} | No iniciar restore por generar contrato |
| API05 | Consultar manifiesto backup | BackupRef{snapshotId,tenant} | ManifiestoResumen{hash,origen} | No incluir credenciales ni path sensible |
| API06 | Verificar resultado restore efímero | RestoreRef{attemptId,sha} | ResultadoRestore{observado,checksums} | Ausencia de ensayo no es PASS |
| API07 | Consultar inventario de objetos | ObjetosFiltro{tenant,provider} | ObjetoResumen{opaqueKey,expiry}[] | Conteo cero exige fuente acreditada |
| API08 | Prevalidar vinculación de host | HostBindingSolicitud{hostname,tenant} | HostDecision{allow,reason} | Nunca asignar tenant por host sólo |
| API09 | Prevalidar TLS | TlsRef{bindingId} | TlsEstado{validity,expiry} | Nunca exponer material privado |
| API10 | Consultar plan de rollback | CutoverRef{planId,sha} | RollbackResumen{checkpoint,owner} | No ejecutar rollback automáticamente |
| API11 | Consultar evidencia causal CI | CiRef{commit,requiredCheck} | CheckDecision{status,conclusion} | CANCELLED/SKIPPED no equivale PASS |
| API12 | Consultar revisión A5 | ReviewRef{microId,sourceSha} | A5Decision{author,reviewer,decision} | Rechazar autoaprobación y SHA obsoleto |

Entradas con tenantScope o tenant son únicamente **solicitudes** que deben contrastarse con el contexto real; no son autoridad. El campo sourceSha no puede sustituirse por una versión obsoleta. Salidas con resultado NO_ACREDITADO no deben convertirse a success verdadero sin lectura y comprobación independiente del origen.

## 3. Validaciones de contrato V01–V16

- V01: exigir identidad autenticada y firma/token verificado por backend.
- V02: revalidar la membresía y el rol efectivos en cada acceso sensible.
- V03: derivar propietario desde contexto, no desde host, slug ni cabeceras.
- V04: denegar EmpresaId o tenantScope contradictorios, incluso si el objeto existe.
- V05: rechazar referencias a recursos desconocidos o de otros tenants.
- V06: exigir versionado del contrato y source SHA de 40 hex para evidencia Git.
- V07: mantener listas permitidas de provider/tipo/campo, no SQL ni HTML arbitrario.
- V08: rechazar paths de archivo absolutos, claves privadas y credenciales en DTO.
- V09: limitar longitud, paginación y cuotas por tenant, con rate limit backend.
- V10: identificar NULL histórico ambiguo como NO_ACREDITADO, no imputarlo a un tenant.
- V11: exigir checksum y origen de manifest de backup antes de analizarlo.
- V12: exigir restauración demostrada separada del mero snapshot de respaldo.
- V13: exigir vínculo de dominio firmado por autoridad y host normalizado.
- V14: aceptar CI requerida sólo como completed/success del SHA aplicable.
- V15: exigir Task Javier revisor distinta del autor para A5, con evento idempotente.
- V16: no permitir operaciones sobre QA/main/PROD ni cambios de proveedor desde estos contratos documentales.

## 4. Catálogo de errores conceptuales (ERR01–ERR12)

Los códigos HTTP siguientes son **oráculos de diseño** para una futura API; no afirma que el SaaS los exponga hoy. Los mensajes no revelan recursos ajenos, PII, secreto, existencia de objetos cross-tenant ni stack trace.

| ID | Código lógico | HTTP futuro sugerido | Causa de rechazo |
| --- | --- | --- | --- |
| ERR01 | UNAUTHENTICATED | 401 conceptual | Sin contexto de identidad comprobada |
| ERR02 | FORBIDDEN_SCOPE | 403 conceptual | Tenant ajeno o pertenencia revocada |
| ERR03 | INVALID_CONTRACT | 400 conceptual | DTO malformado o campo no permitido |
| ERR04 | UNKNOWN_REFERENCE | 404 conceptual | Recurso no registrado o no visible |
| ERR05 | STALE_SOURCE | 409 conceptual | SHA/catalogo origen no vigente |
| ERR06 | CONCURRENT_CHANGE | 409 conceptual | Cambio de owner o revisión concurrente |
| ERR07 | PRECONDITION_MISSING | 412 conceptual | Gate DAG o A5 aún no satisfecho |
| ERR08 | CHECK_NOT_PASS | 409 conceptual | CI requerido cancelled/skipped/queued/failed |
| ERR09 | INVENTORY_UNVERIFIED | 412 conceptual | Proveedor, archivo o DNS sin inventario |
| ERR10 | RESTORE_NOT_PROVEN | 412 conceptual | Backup sin restore exitoso |
| ERR11 | RATE_LIMITED | 429 conceptual | Exceso de peticiones o recursos |
| ERR12 | OPERATION_NOT_AUTHORIZED | 403 conceptual | Migración/cutover sin orden de entorno |

Los conflictos de concurrencia no se resuelven con sobreescritura: se relee origen, SHA, owner y lease; sin CAS atómico acreditado no se declara exclusividad distribuida. Un 404 no certifica que un recurso exista o no exista para otra empresa. Una prueba cancelada no se reetiqueta como PASS.

## 5. Compatibilidad y versionado

- COMP01: evolución aditiva de campos, sin remoción silenciosa de propiedades usadas por Angular o API existentes.
- COMP02: no cambiar el modelo de autorización ni reescribir JWT/RBAC/tenancy en esta microtarea.
- COMP03: registrar versión de DTO, source SHA y error tipado antes de aplicar cualquier contrato futuro.
- COMP04: consumidores deben manejar NO_ACREDITADO y errores de permiso sin degradar seguridad ni UX.
- COMP05: rutas y DTO reales requieren implementación específica M03 con CI .NET/Angular, pruebas negativas y rollback.
- COMP06: cambios de proveedor, dominio, TLS o DB requieren orden de ambiente distinta, inventario vivo, restauración comprobada y cutover reversible.
- COMP07: cada futuro endpoint mantiene límite de respuesta y no devuelve secretos o mensajes internos de seguridad.
- COMP08: un documento M02 en LISTO+A5 sólo prepara trabajo, no activa infraestructura ni satisface M02.06 terminal.

## 6. Casos positivos/negativos futuros (T01–T08)

| ID | Positivo esperado | Negativo obligatorio |
| --- | --- | --- |
| T01 | consulta tenant A con rol | consulta a tenant B sin membership rechazada |
| T02 | inventario firmado con owner | inventario supuesto o con owner vacío bloqueado |
| T03 | SHA vigente y checklist causal | SHA distinto o CI cancelada sin PASS |
| T04 | backup con manifiesto y hash | backup sin restore se queda NO_ACREDITADO |
| T05 | host normalizado autorizado | host/slug falso no autoriza tenant |
| T06 | referencia secreta opaca | clave o token recibido por DTO rechazado |
| T07 | A5 con author != reviewer | A5 firmada por el mismo autor rechazada |
| T08 | rollback con checkpoint de ensayo | rollback sin prueba y sin autorización bloqueado |

Estos casos son **FUTUROS_NO_EJECUTADOS**. No hay pruebas HTTP, MySQL, DNS, R2, TLS ni proveedores realizadas en M02.02. La validación documental sólo inspecciona consistencia de contratos, códigos y oráculos, sin certificar respuestas reales.

## 7. Pruebas de entrega documental, riesgos y rollback

**A0:** revisar fuente estándar, M01.06, ENT/INV M02.01, Skill canónica y PLAN/COLA/DAG. **A1:** comprobar API01–API12, V01–V16, ERR01–ERR12 y T01–T08 positivos/negativos como texto; runtime NO_APLICA. **A2:** comprobar límites JWT/RBAC/tenant, confidencialidad y ausencia de tokens de terceros. **A3:** contraste de consumidores Angular/API conceptual, compatibilidad y rollback, sin cambiar interfaces reales. **A4:** verificar árbol Git fast-forward en dev con solo dos Markdown, Scope Lock y procedencia V5 completed/success exact SHA, BITACORA y readback institucional. **A5:** revisión técnica independiente de otra Task Javier.

Riesgos pendientes: RBAC actual no certificado aquí, inventario de proveedores real no leído, backup/restore no ejecutados, configuración de DNS/TLS no examinada. Mantener NO_ACREDITADO. Rollback únicamente mediante nuevo git revert normal y evento compensatorio append-only; sin force-push, cambios en QA/main/PROD o datos de clientes.
