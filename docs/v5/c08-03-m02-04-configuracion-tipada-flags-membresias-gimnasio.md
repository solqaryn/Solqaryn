# SOLQARYN V17 — C08-03.M02.04: configuración tipada, ámbitos y flags seguros de membresías de gimnasio

**Alcance:** contrato R0 de configuración propuesto, no implementado. **Autor:** PRIMARY DEV 02 — JAVIER, TASK_ID 6af16bb9492c8191b8e8c7d606f11507. **Rama:** solqaryn/Solqaryn@dev. **Gobierno:** PROJECT_SCOPE_LOCK=STRICT, plan comercial institucional V5 y calidad adaptativa V15.

## 1. Autoridad, dependencias y límites

Resultado exigido: definir opciones tipadas, ámbito de configuración, banderas funcionales y validaciones futuras de membresías comerciales, vigencias, suspensión, acceso y perfil de gimnasio. PLAN_MAESTRO fila 1235, COLA G/O=SI, DAG F=VALIDADO/G=SI al reclamar esta unidad mediante lock V17 2000001235. Los seis contratos M01 se encuentran LISTO y con revisión A5 acreditada. M02.01 contiene el modelo conceptual; M02.02 define operaciones y DTO propuestos. M02.03 es independiente y NO es una migración ya realizada.

**No confundir** membresía comercial de gimnasio con usuarios, roles o suscripciones propias del SaaS. La identidad del tenant, sede, operador y recurso deberá derivarse en backend de fuentes autorizadas y no de parámetros confiados del cliente. Las decisiones D01–D08 del alcance M01.04 permanecen abiertas: ninguna opción elige hardware de acceso, PSP, nueva tabla, algoritmo definitivo ni autoriza despliegue.

Este contrato NO crea código, API, DTO ejecutable, base de datos, migración, worker, webhook, QR habilitante, lector de puerta, PSP, política de JWT o UI. Todas las opciones y pruebas siguientes son **candidatas FUTURAS NO_EJECUTADAS**, sujetas a implementación R2 posterior y evidencia CI real.

## 2. Resolución de ámbitos, fail-closed y precedencia

| Ámbito | Origen propuesto | Regla de límite |
| --- | --- | --- |
| S01 Plataforma | configuración versionada revisada; nunca secreto en repositorio | define máximos globales, nunca autoriza tenant por sí misma |
| S02 Empresa | id de empresa probado por sesión y membresía backend | solo reduce permisos y cuotas respecto a S01 |
| S03 Sucursal | sede activa perteneciente a empresa autenticada | no permite valores cruzados entre sedes |
| S04 Plan comercial de gimnasio | plan aprobado de membresía, no suscripción SaaS | no concede privilegios de ERP ni cobro |
| S05 Recurso/lector | recurso de acceso previamente autorizado | falla cerrado si equipo no está registrado o se desconoce su estado |
| S06 Sesión y decisión | contexto temporal de la solicitud autorizada | revocación prevalece frente a caché, flags y tokens previamente emitidos |

Precedencia para opciones de control de acceso: primero denegación por plataforma/seguridad y revocaciones; después límites tenant/sede/recurso; finalmente preferencias comerciales permitidas. No existe fallback a empresa global ni lectura desde un tenant distinto. Una omisión, versión inválida, configuración corrupta, ausencia de consentimiento o reloj ambiguo produce DENY, no una concesión de acceso. Cambios configurables deben auditar quién, cuándo, qué ámbito, versión antes/después y razón sin secretos ni PII.

## 3. Catálogo de opciones tipadas O01–O16

Valores iniciales son **propuestas de contrato**, no valores cargados en ninguna plataforma. Los enteros positivos siempre tienen inicial mayor que cero. Para campos que admiten cero se usa expresamente NonNegativeInteger; cero significa función deshabilitada cuando así se señala.

| ID | Opción lógica propuesta | Tipo y valor inicial | Ámbito y regla |
| --- | --- | --- | --- |
| O01 | MembershipModuleEnabled | Boolean = false | Empresa/sede; false impide alta, renovación y decisión de acceso |
| O02 | AccessDecisionEnabled | Boolean = false | Empresa/sede/recurso; nunca activa una puerta por sí sola |
| O03 | RequireOnlineAuthorization | Boolean = true | Plataforma; no hay decisión permisiva offline |
| O04 | MaxPendingEnrollmentsPerMember | NonNegativeInteger = 0 | Empresa; cero significa impedir nuevas altas hasta activación explícita |
| O05 | ExpiryGracePeriodSeconds | NonNegativeInteger = 0 | Plan; cero sin gracia, tope futuro sujeto a aprobación |
| O06 | ClockSkewToleranceSeconds | NonNegativeInteger = 0 | Plataforma/recurso, futuro límite máximo 30 segundos |
| O07 | AccessLimitPerWindow | NonNegativeInteger = 0 | Plan/sede; cero significa denegar consumo de accesos |
| O08 | IdempotencyWindowSeconds | PositiveInteger = 300 | Plataforma con máximo; nunca aceptar 0 ni número negativo |
| O09 | MaximumSuspensionDays | NonNegativeInteger = 0 | Plan; cero deshabilita suspensión voluntaria |
| O10 | RequireActiveMembership | Boolean = true | Plataforma, no puede relajarse por configuración comercial |
| O11 | RejectReplayedCredentials | Boolean = true | Plataforma/recurso; estado desconocido debe denegar |
| O12 | AuditRetentionDays | PositiveInteger = 30 | Política de retención por aprobar, no define obligación legal |
| O13 | RequiredMembershipStatus | Enum = ACTIVE | Solo estado ACTIVE verificado; SUSPENDED, EXPIRED y PENDING denegados |
| O14 | AccessCredentialTtlSeconds | PositiveInteger = 60 | Cota futura para credenciales efímeras, no crea tokens reales |
| O15 | OfflineAdmissionEnabled | Boolean = false | Hard-deny hasta una autorización R2 específica y evidencia de revocación |
| O16 | ExternalEventEmissionEnabled | Boolean = false | No crea outbox/webhook/worker; activación necesita autorización separada |

O01, O02, O15 y O16 no constituyen un permiso para operar servicios reales: incluso si en un archivo futuro aparecen true, JWT/RBAC/tenant, políticas, disponibilidad real y autorización institucional siguen siendo obligatorios. O12 no decide por sí misma el plazo legal aplicable; cualquier política real requerirá análisis de privacidad y jurisdicción.

## 4. Reglas de validación y seguridad FG01–FG12

| Regla | Rechazo o validación exigida antes de implementar |
| --- | --- |
| FG01 | Rechazar configuración sin EmpresaId real o cuya sede no pertenezca al tenant validado |
| FG02 | Rechazar solicitudes de administrador o usuario revocado aunque su token no haya expirado |
| FG03 | Rechazar cualquier intento de elevar roles, aplicar flags a otra empresa o heredar defaults permisivos |
| FG04 | Validar enteros por tipo exacto, rango y overflow: PositiveInteger no acepta cero |
| FG05 | Rechazar NaN, strings numéricos, negativos, números fraccionarios o campos desconocidos |
| FG06 | Invalidar cachés/decisiones tras suspensión, expiración, revocación o cambio de configuración |
| FG07 | Aplicar fechas UTC y zona IANA de sede; negar fecha local ambigua o inexistente por DST |
| FG08 | Rechazar reproducción de QR/nonce, cambio de fingerprint de clave idempotente y cruce de tenant |
| FG09 | No interpretar respuestas de hardware offline o proveedor desconocido como acceso autorizado |
| FG10 | Prohibir exposición de PII, credenciales, secretos y tokens en errores, eventos y auditoría |
| FG11 | Rechazar plan comercial desactivado, sede inactiva o recurso no autorizado aunque O01/O02 sean true |
| FG12 | Exigir versionado/CAS por ámbito y rollback verificable antes de cambiar flags en runtime |

Prohibido reemplazar controles backend por Angular, claims manipulables, QR, cachés sin aislamiento o redirecciones. No cambiar las fronteras Domain <- Application <- Infrastructure; API compone. Una implementación futura deberá verificar proveedor Oracle EF/MySQL vigente, sin inferir persistencia a partir de este documento.

## 5. Pruebas de aceptación futuras — NO_EJECUTADAS

| Caso | Escenario positivo futuro | Escenario negativo futuro |
| --- | --- | --- |
| T01 | Empresa A autorizada consulta sus opciones tipadas | Empresa B intenta leer o modificar flags de A; rechazo |
| T02 | Sede válida hereda límites restrictivos | Sede ajena/ausente intenta activar acceso; DENY |
| T03 | PositiveInteger de O08=300 se acepta | O08=0, -1 o 1.5 se rechaza |
| T04 | O05=0 significa gracia de cero segundos | Número fuera de rango u overflow se rechaza |
| T05 | Membresía ACTIVE en vigencia UTC se evalúa | SUSPENDED/EXPIRED/PENDING denegada sin fuga de datos |
| T06 | Configuración versionada cambia con rol vigente | Token/rol revocado o If-Match desactualizado se rechaza |
| T07 | Cálculo de vencimiento en zona de sede es inequívoco | Hora DST repetida/saltada sin offset se deniega |
| T08 | Idempotency key igual con igual fingerprint se deduplica | Replay cambiado, tenant distinto o nonce vencido se rechaza |
| T09 | Auditoría enmascara identificadores personales | Error o log intenta incluir QR, JWT, secreto o PII |
| T10 | Rollback revierte opciones conservando historial | Pérdida de proveedor/caché/lector offline no concede entrada |

**Medición runtime:** 0/20 escenarios ejecutados; no existe PASS de HTTP, .NET, MySQL, hardware o browser por este changeset. A1/A2 de esta microtarea consisten en revisión documental de reglas y casos negativos; A3 para integración real es NO_APLICA por R0.

## 6. Compatibilidad, reversibilidad y aceptación del contrato

Una entrega R2 futura deberá registrar fuente y versión de opciones, validar esquema y valores de manera estricta, probar tenant A/B, revocaciones y transacciones/rollback que afecten acceso; no habilitar sede ni lector hasta acreditar controles aplicables. Las modificaciones que cambien EF o datos exigen MySQL 8.4 efímero e integración real; las que cambien UI requieren Angular, accesibilidad y Playwright pertinentes; nuevas integraciones requieren acceso real autorizado.

Rollback del presente contrato R0: revertir el commit documental con git revert normal y preservar BITACORA, sin force-push. No existen migraciones, datos ni runtime que revertir. A0: alcance y diff confinados a este documento y sección autocontenida de CHANGELOG_AI.md; A1/A2: tablas FG/T estáticas revisables; A3: NO_APLICA ejecución runtime por ausencia de cambios de código; A4: Scope Lock y procedencia V5 completed/success del SHA de publicación y readback de PLAN/COLA/BITACORA; A5: revisión independiente posterior de otra de las cinco Tasks Javier. LISTO DEV no autoriza QA, main ni PROD.
