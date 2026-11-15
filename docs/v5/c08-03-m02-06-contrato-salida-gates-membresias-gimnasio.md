# SOLQARYN V17 — C08-03.M02.06: contrato terminal de salida de M02 para membresías de gimnasio

**ID:** C08-03.M02.06, PLAN_MAESTRO fila 1237. **Autor:** PRIMARY DEV 02 — JAVIER, TASK_ID \`6af16bb9492c8191b8e8c7d606f11507\`. **Alcance exclusivo:** \`solqaryn/Solqaryn@dev\`, PROJECT_SCOPE_LOCK=STRICT. **Clasificación V15:** R0, documento sin runtime. **Fuente única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES y PLAN/COLA/DAG institucionales.

## 1. Propósito, condición terminal y no-alcance

Fijar las **dependencias técnicas y el contrato de salida del lote M02** de membresías comerciales de gimnasio: planes, vigencias, accesos, suspensiones y perfil. No crear ni activar un plan comercial, cobro, lector físico, QR, token, acceso, tabla, entidad, API, worker, integración, feature flag o migración. La documentación M01.01–06 define alcance; M02.01–05 define modelos, operaciones, persistencia, opciones y eventos solo *conceptuales*.

La unidad fue reclamada cuando PLAN H1237=PENDIENTE/I vacío, COLA G/O=SI, DAG F=VALIDADO/G=SI, mediante candado nombrado V17 sobre H:I de la fila. Las microtareas M02.01–05 constaban LISTO con A5 independiente en su instante de habilitación; ello acredita **sólo posibilidad de redactar el contrato terminal**. El estado LISTO de M02.06 sin A5 de otra Task no libera automáticamente M03 ni autoriza cambios ejecutables. Deben recalcularse PLAN, COLA y DAG antes de cada sucesora.

**Separación esencial:** la membresía comercial de un gimnasio no es la pertenencia \`UsuarioEmpresa\`, no sustituye \`SuscripcionesSaaS\` y no concede un rol del ERP. Cualquier futura autorización de ingreso físico exige contexto de empresa y sede verificados server-side; el QR no es JWT, la interfaz no concede permisos y un cobro no se infiere de un evento. D01–D08 del análisis M01 continúan PENDIENTES; no se suplen por este contrato.

## 2. Matriz de precedencia y evidencias

| Documento | Entrega esperada y relación | Prueba real aún no acreditada por R0 |
| --- | --- | --- |
| C08-03.M01.01–05 | ámbito, actores, mapa técnico, riesgos y criterios de aceptación | no implica membresía real ni política física |
| C08-03.M01.06 | contrato terminal anterior con 16 barreras y rollback futuro | no equivale a licencia de activar acceso |
| C08-03.M02.01 | veinte entidades, ownership, veinte invariantes, doce pares futuros | no existe clase o esquema EF verificado |
| C08-03.M02.02 | ocho operaciones, ocho DTO, doce errores, dieciséis reglas, diez pares futuros | ningún endpoint HTTP creado |
| C08-03.M02.03 | dieciséis proyecciones, doce índices, ocho etapas y doce fixtures futuros | ninguna migración MySQL ejecutada |
| C08-03.M02.04 | seis ámbitos, dieciséis opciones tipadas, doce reglas, diez pares futuros | ninguna flag en runtime activada |
| C08-03.M02.05 | dieciséis eventos inertes, dieciocho invariantes, diez límites y doce pares futuros | ningún broker, worker, PSP ni webhook ejecutado |

Los artefactos solo sirven de **precondiciones documentales**. Un A5_PASS de cada una debe provenir de una Task Javier distinta de su autora; la firma histórica de una microtarea no sustituye los checks futuros sobre nuevo SHA.

## 3. Dieciséis barreras de liberación B01–B16

| Gate | Condición causal mínima para una implementación posterior | Resultado de bloqueo |
| --- | --- | --- |
| B01 | Identidad de empresa resuelta en backend desde membresía SaaS y sesión válidas | tenant ausente o derivado de cliente => DENY |
| B02 | Sucursal, recurso, titular y plan pertenecen a la misma empresa | referencia ajena, incluso identificador válido => 403/404 seguro |
| B03 | Membresía comercial y roles de ERP permanecen separados | plan gimnasio no eleva permisos JWT/RBAC |
| B04 | Estado ACTIVE, vigencia UTC \`[inicio, fin)\`, zona IANA y DST inequívocos | SUSPENDED, EXPIRED, PENDING y fin exacto => DENY |
| B05 | Revocación de rol, sesión, QR, credencial y plan invalida caché antes del acceso siguiente | token viejo u offline no concede entrada |
| B06 | Idempotencia por tenant, operación y fingerprint, control de versión y replay | payload divergente / acceso duplicado => rechazo sin efecto |
| B07 | Decisión de acceso fail-closed validada en servidor | QR, lector, app o sitio web no son autoridad |
| B08 | Borrador, renovación, suspensión y activación mantienen transiciones autorizadas | comprobante/callback no firmado no equivale a pago |
| B09 | Propiedad, referencias y FKs/índices revisadas para Oracle EF/MySQL real | no aplicar esquema desde diseño conceptual |
| B10 | Migración reversible aditiva con backup y test Category=Integration en MySQL 8.4 efímero | sin logs y 0 fallos => no migrar |
| B11 | Pruebas funcionales de fronteras, errores, revocación y tenant A/B por SHA | build verde aislado no acredita negocio |
| B12 | Autorizaciones JWT/RBAC backend negativas y auditoría PII-segura en tests | mock/guard UI no basta |
| B13 | Adaptador de pago, PSP o lector exige autorización y proveedor probado | no usar hardware, secreto o dinero real por inferencia |
| B14 | Versionado de opciones/eventos y rollback de configuraciones restrictivas | flag activada sin contratos y observabilidad => bloquear |
| B15 | A0–A4 de cada microtarea y A5 independiente donde DAG exija; Scope Lock/V5 SUCCESS | CANCELLED/SKIPPED/IN_PROGRESS no equivale PASS |
| B16 | Gate explícito de ambiente para QA/main/PROD y prueba de regresión proporcional | LISTO DEV o A5 no otorgan despliegue |

**Estado ahora:** B01–B16 son barreras futuras; ningún PASS runtime derivado de este Markdown. No liberar B09–B13 ni B16 por documentación o A5 del contrato.

## 4. Doce pares de aceptación positiva/negativa futuros E01–E12 (NO_EJECUTADOS)

| Caso | Oráculo positivo para fase R2 posterior | Oráculo negativo obligatorio |
| --- | --- | --- |
| E01 | operador A con rol vigente ve membresías de A | operador B busca identificador de A y no obtiene datos |
| E02 | miembro/sede/plan de A encajan con FKs de A | relación de sede B o usuario sin permiso rechaza |
| E03 | alta autorizada crea BORRADOR sin derechos de ingreso | cliente impone ACTIVE o paid=true y no activa |
| E04 | instante justo antes de EndUtc puede ser válido | instante = EndUtc y hora DST ambigua denegados |
| E05 | suspensión revoca credencial antes de próxima decisión | credencial cacheada o rol revocado no accede |
| E06 | check-in con nonce válido obtiene decisión inequívoca | replay, QR copiado o lector offline => DENY |
| E07 | renovación confirmada mediante ERP autoritativo es idempotente | callback falso o reintento no duplica cargo |
| E08 | mismo key+fingerprint converge a un efecto | misma key con payload distinto produce conflicto |
| E09 | actualización de flags con versión CAS es auditable | versión obsoleta y scope B no actualizan |
| E10 | dos operadores concurrentes respetan cupo y estado | carrera no crea acceso de más ni doble efecto |
| E11 | rollback de migración efímera conserva FKs e historial | falla de backfill no asigna dueño por defecto |
| E12 | auditoría mínima conserva correlationId opaco | JWT, PAN, QR persistente, PII o tenant B no salen en log |

**Ejecución aquí:** 0/24 ramas de aceptación implementadas/ejecutadas. La matriz define condiciones de prueba, no resultados xUnit, HTTP, navegador, SQL o pruebas de hardware.

## 5. Ocho rutas de reversión y operación RB01–RB08 futuras

| Ruta | Disparador | Estrategia permitida en implementación autorizada |
| --- | --- | --- |
| RB01 | flag de membresías/apertura falla | desactivar flag en plataforma/sede, negar acceso, conservar historial |
| RB02 | regresión de JWT/RBAC/tenant | volver al estado seguro, revocar credenciales, pruebas A/B antes de reactivar |
| RB03 | fallo de integridad FK o migración MySQL | abortar migración en DB efímera, restaurar snapshot de prueba y verificar conteos |
| RB04 | expiración/DST calculadas incorrectamente | desactivar acceso y recalcular vigencia; nunca extender por defecto |
| RB05 | QR/lector o proveedor no responde | DENY y registrar indisponibilidad, sin fallback de entrada física |
| RB06 | callback/conciliación ERP inconsistente | mantener renovación PENDIENTE, no cobrar/reprobar sin verdad transaccional |
| RB07 | eventos fuera de orden, replay o cola divergente | cuarentena y revisión causal; no reiniciar consumo sin idempotencia |
| RB08 | regresión API/Angular/CI o incompatibilidad de contratos | desactivar nueva superficie, revertir commit permitido y preservar trazas |

El **rollback de esta entrega R0** es únicamente \`git revert\` normal del commit Markdown de \`dev\` tras verificar HEAD, sin reset ni force-push. No existe migración, pago, cambio productivo o dato real introducido por este documento que revertir.

## 6. Puerta DAG, A0–A5 y decisión

**A0:** un nuevo archivo Markdown de M02.06 y una sección de CHANGELOG autocontenida; A0 incluye alcance, lista de predecesoras, dependencias, diff y reversión. **A1:** inspección documental de B01–B16 y de E01–E12 positivos/negativos NO_EJECUTADOS. **A2:** verificación contractual estática de tenant A/B, roles/revocación, UTC/DST, replay, duplicación de cobros, PII y fallback offline. **A3:** ejecución real .NET, EF/MySQL, API/HTTP, QR, PSP, hardware y Angular/Playwright = NO_APLICA_R0 porque no hay código. **A4:** fast-forward/CAS en dev, validadores Scope Lock y procedencia V5 completed/success por SHA, PLAN/COLA/DAG/BITACORA con readback; no contabilizar checks auxiliares que aún corren. **A5:** otra de las cinco Tasks Javier, distinta de la autora, revisa independientemente B/E/RB, documento, pruebas realmente aplicables y diffs; puede rechazar.

**Resultado terminal:** M02.06 puede pasar a LISTO DEV solo después de A0–A4 documentales verdaderamente acreditados. Cualquier M03 de código o M06 de liberación sigue bajo DAG real y los A5 pertinentes, nunca bajo una afirmación de este texto. No se crea runtime de eventos, bandera real, hardware, cuenta, schedule, QA/main/PROD, datos ni secretos.
