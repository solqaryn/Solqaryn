# SOLQARYN V17 — C09-03.M02.06: dependencias técnicas y contrato de salida Social Hub

**ID:** `C09-03.M02.06` · **PLAN_MAESTRO fila 1381** · **Autor:** PRIMARY DEV 02 JAVIER (TASK_ID `6af16bb9492c8191b8e8c7d606f11507`) · **Repo:** `solqaryn/Solqaryn@dev` · `PROJECT_SCOPE_LOCK=STRICT` · **Riesgo R0:** exclusivamente documentación contractual.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), Skill canónica, AGENTS.md, V15–V17, PLAN/COLA/DAG. **Propósito:** fijar las dependencias y criterios de salida de C09-03.M02 para campañas, contenido, segmentos, calendario, métricas, consentimiento/opt-out e idempotencia. Ninguna sección autoriza campañas reales ni servicios nuevos.

## 1. Preflight, fuentes y alcance verificable

**En el CLAIM:** `PLAN_MAESTRO H1381=PENDIENTE/I1381 vacío`; `COLA G1381/O1381=SI`; `DAG_TECNICO fila 1342 F=VALIDADO/G=SI`, después de A5 independiente que desbloqueó esta unidad. Candado `SQV17_C09_03_M02_06` (`namedRangeId=2000001381`), `findReplace` puntual `PENDIENTE -> EN_PROGRESO` con `occurrencesChanged=1`, owner I=Task propia y readback. Sin segundo scope propio EN_PROGRESO.

**Antecedentes de la fase y fronteras:**
- `C09-03.M01.06` — `docs/v5/c09-03-m01-06-gates-rollback-liberacion-social-hub.md`: gates G01–G16, P/N 01–12 futuros, ocho pasos reversibles, decisiones comerciales D01–D08 abiertas. A5 independiente no equivale a permiso para proveedores.
- `C09-03.M02.01` — `docs/v5/c09-03-m02-01-entidades-ownership-social-hub.md`: entidades y ownership; tenant A/B, canal y consentimiento deben demostrarse en backend.
- `C09-03.M02.02` — `docs/v5/c09-03-m02-02-contratos-api-dto-errores-social-hub.md`: operaciones, DTO, errores conceptuales sin endpoints reales.
- `C09-03.M02.03` — `docs/v5/c09-03-m02-03-persistencia-indices-migraciones-social-hub.md`: índices y migraciones FUTURAS no aplicadas; no afirmar esquema existente.
- `C09-03.M02.04` — `docs/v5/c09-03-m02-04-configuracion-flags-social-hub.md`: opciones tipadas con DENY por defecto y sin flags encendidos.
- `C09-03.M02.05` — `docs/v5/c09-03-m02-05-eventos-idempotencia-correlacion-social-hub.md`: doce eventos EV01–EV12, doce garantías ID01–ID12, doce escenarios futuros; revisado independientemente por DEV02 y A5_PASS, sin outbox ni broker creados.

**Condiciones externas dinámicas:** DAG de esta unidad exige `C09-03.M01.01–06+A5`, `C09-03.M02.01–05+A5`, `C06-03.M01.06+A5`, `C09-01.M01.06+A5`. El gate SI calculado al CLAIM autoriza **esta entrega documental**, no acciones posteriores. M03 depende de sus propios predecesores, revisión A5 de M02.06 y puertas de fases C06/C09; no editar fórmulas G/O a mano.

**Diff permitido:** este Markdown y bloque contiguo en CHANGELOG_AI.md. No alteración ejecutable de Angular, ASP.NET Core 10, Domain/Application/Infrastructure/API, EF Core 10.0.12, proveedor Oracle MySql.EntityFrameworkCore 10.0.9, MySQL 8.4, JWT/RBAC, plantillas, credenciales, worker, redes ni datos Aiven. No se implementan campañas, mensajería, conectores ni respuestas HTTP.

## 2. Dieciséis barreras de salida contractual G01–G16

| Gate | Evidencia exigida para futura implementación | Resultado sin evidencia |
| --- | --- | --- |
| G01 | Gobierno canónico, repo exacto, Task Javier allowlist, Scope Lock y SHA exacto | NO_GO al escritor |
| G02 | PLAN/COLA/DAG por ID actual, predecesoras LISTO con A5 causal si aplica | Mantener gate NO |
| G03 | V17 namedRange exclusivo, CAS H con 1 cambio, owner I, BITACORA 26 campos readback | Abort por contención |
| G04 | Propiedad tenant/empresa verificada en backend para campaña, segmento, contenido, calendario y métricas | Rechazar A→B |
| G05 | JWT, UsuarioEmpresa vigente, roles scoped y revisor de campaña distinto de editor | Denegar self-approval y rol revocado |
| G06 | Consentimiento específico por finalidad, destinatario, empresa y canal con prueba rastreable | No enviar |
| G07 | Opt-out, supresión y revocación revalidados justo antes de efecto y de retry | Cancelar publicación |
| G08 | Contenido/medio/URL propios saneados; protección XSS, SSRF y filtración de datos | Rechazo y auditoría segura |
| G09 | Segmentación acotada, filtros allowlist, privacidad por cohorte y paginación estable | Denegar consulta arbitraria |
| G10 | Idempotencia con digest tenant+campaña+canal+destino y CAS single-writer | Bloquear doble envío |
| G11 | Timezone/UTC y versión de agenda, cancelación y expiración resueltas sin carreras | No despachar |
| G12 | Proveedor y canal allowlist con credenciales autorizadas, cuotas, costo y límites explícitos | Envío deshabilitado |
| G13 | Timeout/429/5xx y recibos correlacionados; UNKNOWN_OUTCOME no genera retry ciego | Conciliar, no duplicar |
| G14 | Métricas agregadas tenant-scoped, umbral mínimo, retención y logs sin JWT/PII/secretos | No exportar |
| G15 | Tests reales xUnit/HTTP/Angular/Playwright/EF MySQL 8.4 efímero cuando procedan; checks exact-SHA SUCCESS | CI pendiente/FAIL/SKIPPED NO PASS |
| G16 | A5 independiente distinta del autor, ausencia P0/P1, readback DAG y autorización ambiental separada | No liberar M03/QA/main/PROD |

Estas puertas especifican requisitos **futuros**; su escritura no acredita implementación, contratos de proveedor, pruebas runtime, cumplimiento legal ni seguridad real. Cualquier R3 de producción/efectos irreversibles es NO_GO sin permiso individual nuevo.

## 3. Doce pruebas causales futuras: 0/12 ejecutadas

| Par | Positivo requerido en futura integración | Negativo que debe fallar ante un defecto |
| --- | --- | --- |
| P01/N01 | Editor A consulta/cambia borrador de A autorizado | Editor A obtiene campaña B por ID |
| P02/N02 | Aprobador distinto con rol activo aprueba revisión | Self-approval, rol revocado o JWT inválido |
| P03/N03 | Opt-in vigente coincide con canal/finalidad | Consentimiento para otro canal se reutiliza |
| P04/N04 | Opt-out bloquea efectos nuevos inmediatamente | Snapshot obsoleto causa envío tras opt-out |
| P05/N05 | Segmento A usa filtros parametrizados y autorizados | SQL/JS arbitrario o sujeto B incluido |
| P06/N06 | Replay clave+payload retorna un único resultado | Dos escritores/envíos con la misma clave |
| P07/N07 | URL/media propia y saneada se publica | SSRF, XSS o archivo de empresa B |
| P08/N08 | Publicación cancelada respeta versión vigente | Dispatch ocurre después de cancelación |
| P09/N09 | ACK de proveedor se correlaciona con intento real | Recibo extranjero/falsificado marca entrega |
| P10/N10 | Retry finito procesa fallo transitorio clasificado | Resultado remoto desconocido provoca doble efecto |
| P11/N11 | Cohorte segura agrega métrica A sin PII | Grupo pequeño permite reidentificación |
| P12/N12 | MySQL efímero prueba esquema/outbox, rollback y tenant | CI build sin tests Integration se certifica como PASS |

**Evidencia real de esta microtarea R0:** `0/12` pruebas ejecutadas en runtime, `NO_MEDIDO` para latencias/costos; sólo validación de requisitos, alcance y checks documentales. Estas son especificaciones para futuras pruebas, no xUnit/HTTP/Chromium/MySQL/EF/proveedor aprobados.

## 4. Puertas de decisión, dependencias y frontera operativa

Las decisiones D01–D08 de M01.04 siguen abiertas: legitimidad de consentimiento por canal y finalidad, modelo de RBAC/editor/aprobador, fuente/ownership de segmentos, validación de contenido y URLs, programación y límites de reintento, proveedor/costo/cuota, métricas/retención, migración/rollback y aceptación. **No se presume aprobación comercial o jurídica.**

**M02.06:** se cierra sólo como documento R0 al publicar los dos Markdown por commit serial fast-forward sobre dev, con Scope Lock y V5 `completed/success` en SHA causal, readback PLAN/COLA/DAG/BITACORA, A5_PENDIENTE. No se crean tablas/eventos reales, no se reconfiguran productores ni se envían mensajes.

**M03–M06:** cuando DAG real habilite una ID, implementar código real con pruebas positivas/negativas, aislamiento A/B, autoridad JWT/RBAC, revocación/opt-out, idempotencia bajo carreras, integración EF Oracle/MySQL 8.4 efímero si toca esquema/datos, Playwright/Chromium si UI cambia, proveedores solo con credenciales autorizadas. A5 de Task diferente no sustituye aprobación QA/main/PROD.

**Dependencias temporales:** un VALIDANDO de predecesora no equivale a LISTO+A5; CI CANCELLED/SKIPPED/IN_PROGRESS no es PASS; evitar nuevos pushes que cancelen required checks de otros SHA conforme CI_CANCEL_GUARD.

## 5. Ocho pasos de reversión documentada

1. Capturar microtask ID, autor/revisor, source/tested SHA, HEAD, diff, blobs, owner actual, efecto y RUN_ID únicamente si nativo.
2. Releer candado V17 y evidencia de otros escritores; no transferir owner por reloj ni borrar BITACORA.
3. Identificar si error es redacción documental, propiedad tenant, consentimiento, contenido, UI, proveedor, persistencia o gate CI.
4. Para este Markdown, aplicar corrección aditiva o `git revert` normal fast-forward después de CAS HEAD; no force-push/reset, no borrar historial.
5. Si existe futuro efecto externo, suspender emisiones nuevas del scope afectado sin eliminar opt-outs, recibos ni auditoría.
6. Para cambios EF/outbox posteriores, usar backup/fixtures y restauración verificada en MySQL 8.4 efímero; nunca reparar en Aiven productivo desde este contrato.
7. Reejecutar negativos tenant A/B, revocación, opt-out entre programación y envío, duplicados, recibos forjados y límites; Scope Lock/V5 y tests requeridos SUCCESS del SHA.
8. Registrar eventos idempotentes, preservar original A5_FAIL si existiera, hacer readback PLAN/COLA/DAG y exigir revisión A5 distinta antes de desbloqueos.

## 6. A0–A5 y condiciones de aceptación de esta entrega

**A0:** requisitos de M02, seis contratos previos, DAG habilitado, diff dos Markdown, sin arquitectura/API/DB ejecutables. **A1:** 16 gates, 12 pares P/N y 8 pasos de rollback revisables; `0/12` runtime. **A2:** consentimiento, tenant A/B, JWT/RBAC, revocación, opt-out, SSRF/PII, idempotencia y errores diseñados sin afirmar enforcement nuevo. **A3:** MySQL/EF/.NET/HTTP/Playwright/proveedores `NO_APLICA` al cambio documental, pero serán exigibles cuando código afectado cambie. **A4:** commit FF exacto, Scope Lock y procedencia V5 `completed/success`, hashes y readback Sheet/BITACORA; no contabilizar tests externos pendientes como éxito. **A5:** revisión independiente de otra Task Javier; sin ella no se liberan sucesoras que la requieran.

**Rollback de este alcance:** revert Git normal/dev o corrección aditiva con preflight y readback; ningún cambio a `main`, QA, PROD, cuentas de pago, secretos, RRULE o datos de empresa. **Resultado contractual:** el lote M02 dispone de definición de salida documental, pero ni M03 ni uso comercial están certificados por este texto.
