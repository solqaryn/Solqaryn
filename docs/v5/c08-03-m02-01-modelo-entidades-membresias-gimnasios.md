# SOLQARYN V5 — C08-03.M02.01 — Modelo conceptual de membresías y accesos de gimnasio

**Autor:** PRIMARY DEV 06 — JAVIER (`6af16bbad26881918c283abe06e1f32a`). **Fecha:** 2026-11-15. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, C08-03.M02.01 (PLAN_MAESTRO fila 1232). **Riesgo:** R0, solo documentación. **Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`.

## 1. Objetivo, predecesoras y fronteras

Definir entidades, ownership, relaciones e invariantes para planes y membresías comerciales, vigencia, suspensión, derechos de acceso y perfil de miembro de gimnasio. **Este trabajo no implementa** cobros, facturación, apertura de puertas, biometría, QR reales, usuarios, roles, tokens JWT, API, frontend, esquema EF/MySQL, workers, dispositivos o migraciones. El ERP de la empresa mantiene la autoridad económica y de facturación; control de acceso físico y proveedores solo se definirán en entregas futuras expresamente autorizadas.

C08-03.M01.01–06 están LISTO y cuentan con revisiones A5 independientes, incluido M01.06 con B01–B16, E01–E12 FUTUROS NO_EJECUTADOS y RB01–RB08. La elegibilidad de M02.01 se verificó por PLAN H=PENDIENTE, I vacío, COLA G/O=SI, DAG F=VALIDADO/G=SI al reclamar con candado V17; es un corte causal y no permiso de implementar la siguiente fase. Condiciones preparatorias de C02-04, C04-01 y bloques C01/C02/C04/C06 se gobiernan por DAG calculado. D01–D08 comerciales aún requieren aprobación de negocio.

## 2. Entidades conceptuales y propiedad — E01–E20 (NO CREADAS)

| ID | Entidad futura | Owner / relación | Límite obligatorio |
| --- | --- | --- | --- |
| E01 | Empresa | Tenant raíz | EmpresaId de servidor, no input de lector |
| E02 | SucursalGimnasio | Empresa 1:N | Contexto tenant+sede inequívoco |
| E03 | AreaAcceso | Sucursal 1:N | Zona habilitada según plan y política |
| E04 | PerfilMiembro | Empresa 1:N | Referencia de persona mínima, sin identidad B |
| E05 | Membresia | PerfilMiembro 1:N | Tenant, plan, período, estado y versión |
| E06 | PlanComercial | Empresa 1:N planes versionados | Precio y moneda autoridad comercial ERP |
| E07 | VersionPlan | Plan 1:N | Términos, zona, horario y política vigentes |
| E08 | PeriodoVigencia | Membresia 1:N | Inicio y fin UTC, zona IANA, reglas DST |
| E09 | SuspensionMembresia | Membresia 0:N | Razón, aprobación, intervalos sin superposición |
| E10 | RenovacionMembresia | Membresia 0:N | Idempotencia, invoice ref ERP, sin cobro autónomo |
| E11 | AsignacionSede | Membresia 0:N sedes | Alcance mínimo, revocación y vigencia |
| E12 | DerechoAcceso | Plan/Versión 1:N áreas | Regla server-side no autoritativa en UI |
| E13 | CredencialAcceso | Miembro 0:N versiones | Hash/alias opaco, TTL y revocación |
| E14 | PuntoAcceso | Sucursal 1:N | Dispositivo sólo identificado, no habilitado |
| E15 | PoliticaHorario | Sucursal/plan 1:N | UTC, zona horaria y ventana efectiva |
| E16 | EvaluacionAcceso | Credencial y política | Resultado allow/deny, versión, correlación |
| E17 | EventoAcceso | Evaluacion 0:N auditorías | Sin PII ni biometría bruta |
| E18 | BloqueoAcceso | Miembro 0:N | Suspendido/revocado/impago verificado en ERP |
| E19 | IdempotenciaMembresia | Actor+operación+tenant | Key/payloadHash/TTL e intento único |
| E20 | ConciliacionComercial | Membresia y ERP 0:N | Ref de pago/factura existente, sin duplicación |

Ninguna de estas entidades representa tabla ya confirmada ni autorización para generar migraciones. M02.02 definirá operaciones/DTO, M02.03 persistencia/indexes, M02.04 configuración, M02.05 eventos y M02.06 contrato terminal, cada una con su propia validación/A5.

## 3. Relaciones y restricciones del grafo

- `Empresa -> Sucursal -> Area/PuntoAcceso` obliga aislamiento A/B incluso si `PuntoAccesoId` o `PerfilMiembroId` coinciden. Nunca confiar en una ruta, QR, host o encabezado para resolver el tenant.
- `PerfilMiembro -> Membresia -> PlanComercial/VersionPlan -> DerechoAcceso`: derechos dependen de plan efectivo, versión, empresa y sede; membresía vencida o suspendida no otorga acceso, aunque la UI o un lector retengan caché.
- `Membresia -> PeriodoVigencia / Suspension / Renovacion`: los intervalos se calculan en UTC con zona IANA validada; cambios DST no prolongan ni duplican horarios. Renovar requiere conciliación de ERP y pruebas de no doble cobro.
- `PerfilMiembro -> CredencialAcceso -> EvaluacionAcceso -> EventoAcceso`: credencial/QR es un identificador opaco, no identidad suficiente ni autorización definitiva; revocar invalida uso ulterior, auditoría minimizada.
- `Membresia -> AsignacionSede -> PoliticaHorario`: la autorización resulta de intersección restrictiva; una sede diferente o un derecho caducado se deniega por defecto.
- `IdempotenciaMembresia` y `ConciliacionComercial` son contratos transversales, no nuevos escritores de facturación. Pago, impuestos, caja y saldo siguen responsabilidad ERP existente.

## 4. Invariantes de diseño — INV01–INV20

| ID | Invariante que deberá detectar un defecto al implementarse |
| --- | --- |
| INV01 | Identidad tenant y sede proceden de contexto autenticado de servidor |
| INV02 | Miembro A no es consultable ni modificable desde tenant/sede B |
| INV03 | Plan/version del miembro debe pertenecer al mismo tenant |
| INV04 | Derecho es intersección de plan, sede, horario, estado y revocación |
| INV05 | Membresía vencida, suspendida o cancelada nunca autoriza acceso |
| INV06 | Intervalos inicio/fin UTC obedecen semántica cerrada/abierta explícita |
| INV07 | Ambigüedad DST y reloj del cliente no extienden vigencia |
| INV08 | QR falso/copiado/vencido/revocado se rechaza sin revelar miembro |
| INV09 | Revocar rol o credencial invalida permisos persistentes/caché |
| INV10 | Autenticación/ruta/guard UI no sustituye autorización de backend |
| INV11 | Punto de acceso B no reconoce derechos de miembro A |
| INV12 | Dos renovaciones concurrentes usan versión/idempotencia: un ganador |
| INV13 | La misma key y payload es convergente, key con payload distinto falla |
| INV14 | Acceso fuera de horario, zona o plan se rechaza y audita mínimamente |
| INV15 | Suspensiones superpuestas no duplican días ni saldo |
| INV16 | Pago/factura/abono real sólo se confirma tras conciliación ERP |
| INV17 | Un error entre facturación y vigencia no produce membresía pagada ficticia |
| INV18 | Logs no conservan JWT, QR original, biometría, PAN o PII sensible |
| INV19 | Dispositivo offline o sin estado confiable deniega o usa política aprobada explícita, nunca éxito ficticio |
| INV20 | Código ejecutable, lector y DB requieren A0–A5, tests reales y permiso de ambiente independiente |

## 5. Oráculos positivos/negativos futuros — T01–T12 (NO_EJECUTADOS)

| Test | Positivo futuro | Negativo/frontera futuro |
| --- | --- | --- |
| T01 | Miembro A con plan A consulta perfil A | B no accede a perfil, sede o plan de A |
| T02 | Vigencia UTC válida permite evaluar | Expirado y segundos en frontera final => denegar |
| T03 | Zona IANA válida calcula horario | DST ambiguo o relojes cliente manipulados => denegar |
| T04 | QR vigente identifica miembro autorizado | QR falso/comprometido/revocado/B => no datos |
| T05 | Plan y área autorizados permiten evaluar | Área ajena/restringida => rechazo |
| T06 | Suspensión aprobada bloquea periodo | Intento durante suspensión => rechazo |
| T07 | Renovación conciliada ERP actualiza vigencia una vez | Replay/duplicado/carrera no crea doble cobro |
| T08 | Administrador con rol/sede vigentes evalúa | Rol revocado/tenant B bloqueado |
| T09 | Dispositivo de sede autorizada consulta | Dispositivo B/offline sin política => no apertura |
| T10 | Evento saneado contiene correlación mínima | Sin JWT, QR, biometría, PAN, PII cruda o datos B |
| T11 | Error de facturación mantiene estado seguro | Timeout no marca pagado ni acceso concedido |
| T12 | Flag apagado no altera ERP o puertas actuales | Nueva política no activa hardware sin autorización |

**Resultado de tests en este documento: NO_MEDIDO, 0 escenarios ejecutados.** Los tests .NET/HTTP/JWT/tenant, MySQL 8.4 efímero, dispositivos y Chromium corresponderán únicamente a implementaciones posteriores y gates independientes; pruebas futuras descritas NO son PASS actual.

## 6. Impacto, gobernanza, rollback y calidad R0

A0: fuentes M01.01–06 + modelo E01–20, invariantes INV01–20, T01–12 futuros, dependencia DAG y diff documental. A1: revisar consistencia positiva/negativa, fronteras UTC/DST y suspensiones, no ejecutar accesos reales. A2: diseño de control tenant A/B, RBAC revocado, tokens, UTC, idempotencia y privacidad con fail-closed. A3: NO_APLICA en R0 a xUnit/MySQL/HTTP/Angular/Playwright/dispositivos: diff exclusivamente Markdown. A4: commit fast-forward/CAS a dev y checks exact SHA Scope Lock/V5 SUCCESS, CHANGELOG autocontenido, PLAN/BITACORA readback. A5: revisión independiente de otra Task Javier diferente de DEV06, sin autopase.

No modificar Domain/Application/Infrastructure/API/Angular, cuentas, secretos, usuarios, persistencia, API, QR, hardware, PSP, caja, QA/main/PROD. Reversión: cambio documental aditivo o `git revert` normal tras HEAD/owner verificados, sin eliminar BITACORA ni sobreescribir archivos de otras Tasks.
