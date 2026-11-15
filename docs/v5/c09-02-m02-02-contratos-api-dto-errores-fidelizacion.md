# SOLQARYN V17 — C09-02.M02.02: API, DTO, errores y compatibilidad conceptual de fidelización

**ID:** `C09-02.M02.02`; PLAN_MAESTRO fila 1341; **autor:** PRIMARY DEV 01, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`; **rama:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Riesgo actual:** R0 documentación. Los riesgos de runtime son R2 futuros (tenant, dinero/crédito, idempotencia, privacidad). No se implementan HTTP, servicios, DTO C#, migraciones, formularios, mensajes ni permisos.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, PLAN/COLA/DAG, V15 A0-A5 y V17. Objetivo M02.02: definir contrato futuro de API, DTO, errores, validación y compatibilidad para puntos, promociones, reseñas, moderación y posventa; no confundir el documento con una API disponible.

## 1. Evidencia base y fronteras

- `docs/v5/c09-02-m01-06-gates-rollback-liberacion-fidelizacion.md` (blob `0bfaffb03f54411a17557924b64fcb67451bc0ee`): G01-G16 y alcance/rollback institucional.
- `docs/v5/c09-02-m01-03-inventario-codigo-brechas-fidelizacion.md` (blob `b5bbb9d2c8e85814390bd12c85e5f4d6bd3bfed7`): `Cliente`, `Venta`, `CreditoCliente`, `UsuarioEmpresa`, `RegistroAuditoria` como sustratos, sin demostrar ledger o API de puntos.
- `C09-02.M02.01` publicado en dev SHA `94ca301cef9faceedaa985beacf892b9b277a4c5`, documento `docs/v5/c09-02-m02-01-modelo-entidades-ownership-fidelizacion-reputacion.md`, blob `0f55f250f7c12badcd49717dc3e7a43579e6496a`: 16 entidades e INV01–INV20 contractuales. El M02.01 aún requiere A5 independiente, por lo que **no se considera aprobado** ni se libera un sucesor técnico por esta referencia.
- Aislamiento multiempresa real exige backend autoritativo: `EmpresaId` derivada de credenciales/membresía, nunca de body, slug, host, header o parámetro confiado. `EmpresaId=NULL` heredado no transfiere ownership. `CreditoCliente`, venta, factura, dinero y puntos son dominios separados.

## 2. Ocho operaciones futuras (NO HABILITADAS)

| ID | Operación lógica propuesta | Entrada mínima futura | Seguridad, idempotencia y resultado esperado |
| --- | --- | --- | --- |
| O01 | Consultar cuenta/ledger del cliente | cliente y paginación válidos | cliente propia empresa o rol autorizado; resultado filtrado por tenant |
| O02 | Simular/consultar beneficios elegibles | programa, versión y fecha UTC | sin mutar saldo, sólo promoción vigente para tenant |
| O03 | Acreditar puntos por venta confirmada | venta validada, key y versión | backend revisa venta/tenant, transacción única, rechaza replay |
| O04 | Canjear puntos | cuenta, beneficio, cantidad, key y versión | saldo/cupo y concurrencia transaccional; no side effects parciales |
| O05 | Consultar y presentar reseña | recurso/venta y texto saneado | autor legítimo, consentimiento/PII mínima, moderación requerida |
| O06 | Moderar o apelar reseña | reseña, decisión/apelación, motivo | rol vigente, auditoría, separación de autor y revisor |
| O07 | Abrir/consultar posventa | cliente, venta y caso autorizado | recurso/venta pertenecen al mismo tenant; no IDOR |
| O08 | Actualizar consentimiento/opt-out | cliente, canal, propósito, versión | revocación inmediata, bloqueo de reintentos/envíos posteriores |

Las rutas URL, métodos, políticas, códigos definitivos, persistencia e interfaces se acordarán al implementar M03; **O01–O08 son nombres lógicos**, no afirmación de endpoints reales ni autorización para abrirlos.

## 3. Ocho DTO CONCEPTUALES sin tipos ni campos secretos implementados

| DTO propuesto | Campos mínimos y límites |
| --- | --- |
| D01 CuentaPuntosView | ID opaco, programa, saldo no monetario, versión, tiempo UTC; sin PII excesiva |
| D02 MovimientoPuntosView | tipo, causa, referencia opaca de venta, fecha UTC, importe en puntos y correlación |
| D03 BeneficioElegibleView | beneficio, regla/version, cuota remanente visible, inicio/fin UTC |
| D04 SolicitudAcreditacion | venta identificada en backend, idempotency key, versión esperada; no aceptar EmpresaId cliente |
| D05 SolicitudCanje | cuenta opaca, beneficio, cantidad positiva, clave y versión; no permitir saldo arbitrario |
| D06 ResenaRequest | recurso autorizado, texto limitado, consentimientos y versión; sin HTML/scripts |
| D07 ModeracionApelacionRequest | decisión, motivo obligatorio, versión, identidad del actor desde JWT |
| D08 PosventaConsentimientoView | referencia opaca de caso, estado, canal/propósito y opt-out; privacidad mínima |

Toda clave `EmpresaId` debe ser resuelta en servidor, no recibida como autoridad. Identificadores de cliente/venta/beneficio son referencias sujetas a ownership, no permisos.

## 4. Doce errores seguros propuestos

| Error lógico | Situación futura | Respuesta y ausencia de filtración |
| --- | --- | --- |
| X01 UNAUTHENTICATED | JWT ausente/inválido | 401 sin revelar tenant, usuario ni tokens |
| X02 FORBIDDEN_ROLE | rol o permiso revocado | 403 genérico con correlación no sensible |
| X03 RESOURCE_NOT_FOUND | recurso de otro tenant o inexistente | 404 indistinguible, sin IDOR |
| X04 LEGACY_OWNER_UNRESOLVED | `EmpresaId=NULL` no conciliado | rechazo seguro y auditoría mínima |
| X05 INVALID_INPUT | tipo/rango/cantidad o texto inválido | validación de campo sin datos sensibles |
| X06 VERSION_CONFLICT | versión stale o carrera CAS | 409, reintento seguro sin doble efecto |
| X07 DUPLICATE_OPERATION | key idempotente ya usada | devolver resultado idéntico autorizado o conflicto |
| X08 INSUFFICIENT_POINTS | saldo inferior al canje | rechazo sin ledger parcial |
| X09 PROMOTION_UNAVAILABLE | tenant, cupo o UTC inválidos | sin revelar campañas privadas de B |
| X10 REVIEW_REJECTED | texto no saneado/identidad dudosa | no persistir ni publicar |
| X11 CONSENT_REVOKED | opt-out o finalidad ausente | no enviar, incluso en reintento |
| X12 DEPENDENCY_UNAVAILABLE | DB/proveedor/eventual timeout | fail-closed, sin confirmar efecto indeterminado |

Estos códigos son identificadores de contrato para diseñar tests; no son respuestas HTTP ya observadas. Un error no puede incluir stack traces, secretos, correos privados ni saldos cross-tenant.

## 5. Dieciséis reglas de validación y compatibilidad

| ID | Criterio futuro obligatorio |
| --- | --- |
| V01 | Verificar autenticación JWT y revocación antes de resolver el recurso |
| V02 | Validar membresía activa y permiso granular por operación y empresa |
| V03 | Aislar `EmpresaId` servidor; rechazar cualquier intento de suplantación desde input |
| V04 | Rechazar identificadores de Cliente/Venta/Reseña de tenant B en tenant A |
| V05 | Rechazar `EmpresaId=NULL` de legado no conciliado |
| V06 | Validar cantidad de puntos con rango positivo, precisión y saldo no monetario |
| V07 | Validar key idempotente única por tenant+operación y retención limitada |
| V08 | Prevenir replay y carreras por versión/concurrencia transaccional |
| V09 | Usar UTC y calendario de promoción con cupo y versión |
| V10 | Separar promociones/puntos de `CreditoCliente`, venta y dinero |
| V11 | Sanear reseñas contra HTML/script, limitar longitud y PII |
| V12 | Moderación requiere motivo, rol distinto del autor y posibilidad de apelación |
| V13 | Posventa exige relación legítima cliente/venta/caso de la misma empresa |
| V14 | Consentimiento por canal y propósito se comprueba al enviar y reintentar |
| V15 | Cambios de contrato serán versionados y tendrán deserialización compatible explícita |
| V16 | Paginar resultados, limitar carga, tasa y redactar logs/errores/cachés por tenant |

Los consumidores actuales de `ClientesController` y `cliente.service.ts` no cambian. La futura integración debe respetar sus contratos o exponer evolución versionada; no modificar recursos existentes indirectamente.

## 6. Doce escenarios P/N FUTUROS_NO_EJECUTADOS

| Par | Positivo de aceptación futuro | Negativo/limite que deberá fallar |
| --- | --- | --- |
| P01/N01 | Cuenta A retorna sólo ledger A | ID de cuenta B, legacy NULL o sesión sin membresía |
| P02/N02 | Venta A confirmada acredita una vez | venta B/duplicada/anulada intenta acreditar |
| P03/N03 | Canje válido debita saldo atómicamente | dos canjes simultáneos sobre saldo insuficiente |
| P04/N04 | Promoción UTC vigente y con cupo es elegible | expiración/cupo 0/tenant B se aplica |
| P05/N05 | DTO idempotente repetido devuelve estado estable | key reciclada con payload distinto altera puntos |
| P06/N06 | Autor autorizado genera reseña saneada | HTML/PII o autor de otro tenant publica |
| P07/N07 | Moderador vigente decide y audita motivo | rol lector/revocado o autor se autoaprueba |
| P08/N08 | Apelación permitida conserva historia | hard delete de moderación elude auditoría |
| P09/N09 | Posventa A consulta su venta/caso | acceso IDOR de caso o venta B filtra datos |
| P10/N10 | Consentimiento vigente habilita canal | opt-out, retry o cache envía después de revocar |
| P11/N11 | Respuesta compatible y paginada evita datos privados | payload obsoleto/masivo expone registros |
| P12/N12 | Timeout sin confirmar efecto conserva recuperación | proveedor no disponible produce doble efecto |

**Métrica ejecutada:** 0/12 pruebas HTTP/MySQL/Playwright implementadas por esta unidad; calidad runtime `NO_MEDIDO`. M03/M05 deberán producir tests que fallen ante errores en esos casos; no se contabilizan como PASS documental.

## 7. Gates R0, seguridad futura y rollback

- **A0:** verificar objetivo, antecedentes, diff documental y ownership, sin crear rutas/DTO runtime.
- **A1:** inspeccionar consistencia y contradicciones de las 8 operaciones, 8 DTO, 12 errores, 16 validaciones y 12 P/N propuestos; no alegar que fueron ejecutados.
- **A2:** JWT/RBAC/tenant A/B, membresía revocada, `EmpresaId=NULL`, PII, consentimiento, idempotencia y antifraude **solo contrato estático**.
- **A3:** integración de MySQL, .NET/API, UI, PSP, mensajería y Playwright `NO_APLICA` para edición exclusiva de Markdown; será obligatoria en código R2.
- **A4:** commits fast-forward en dev con `verify-solqaryn-project-scope` y procedencia V5 `completed/success` del SHA causal o equivalencia íntegra, BITACORA idempotente, PLAN/COLA/DAG readback. Otros checks en progreso no son PASS.
- **A5:** revisor Javier de TASK_ID distinto al autor valida diff, pruebas y fuentes; `LISTO_DEV` no es A5_PASS ni autorización para M03.
- **Publicación:** V17 candado `SQV17_C09_02_M02_02`, namedRangeId `2000001341`, sólo este documento y sección compacta de `CHANGELOG_AI.md`. Estado de la entrega y evidencia tienen que mantenerse reproducibles.
- **Rollback:** revert Git normal sobre HEAD nuevo si hubiese error documental, preservar BITACORA histórica y cambios ajenos; no modificar fórmulas/tenancy ni saltar permisos. Ningún cambio QA/main/PROD, secretos, datos reales, pagos ni mensajes.

**Resultado restringido:** contrato conceptual de operaciones/DTO/errores/validación; implementación y certificación HTTP futura **pendientes**.
