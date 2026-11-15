# SOLQARYN V16 — C11-04.M02.06: contrato de salida de observabilidad

**ID:** C11-04.M02.06. **Riesgo V15:** R0 (solo documentación). **Rama:** `solqaryn/Solqaryn@dev`. **Scope Lock:** STRICT. **Autor ejecutor:** PRIMARY DEV 02, Task `6af16bb9492c8191b8e8c7d606f11507`.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), PLAN_MAESTRO fila 1705, COLA G/O y DAG_TECNICO F/G, más `docs/QUALITY_RISK_AUDIT_V15.md`. Resultado exigido: **fijar dependencias técnicas y contrato de salida de M02** para logs, métricas, trazas, alertas y costos sin información sensible. Este contrato **no certifica SLO ni habilita ejecución, publicación, proveedor, QA/main/PROD**.

## A0. Prerrequisitos, límites y alcance

- M01.01–06 de C11-04, M02.01–05 de C11-04 y los predecesores interbloque que indique el DAG vigente deben estar documentalmente LISTO y con A5 causal independiente donde lo exija el gate dinámico. El estado `VALIDANDO` o un A5 pendiente no se convierte en autorización de código o promoción.
- Contratos anteriores de M02: entidades/invariantes E01–E12, INV01–INV16 y REL01–REL10; API/DTO OP01–OP08, DTO01–DTO08, ERR01–ERR12, VAL01–VAL16; persistencia conceptual P01–P12, IDX01–IDX12 y MIG01–MIG08; configuración OPT01–OPT20, FL01–FL12 y VAL01–VAL18; eventos futuros EVT01–EVT14 e INV01–INV16 de `C11-04.M02.05`.
- La aplicación existente usa ASP.NET Core 10, Angular 22.2.1 y MySQL/EF Core con Oracle. Middleware de correlación, observabilidad y errores preexistente **no queda validado por este documento**. No se modifica código, base de datos, migración, API, worker, collector, exporter, dashboard, scheduler, DNS ni secreto.
- El frontend no concede autoridad. Cualquier consulta de señales privadas exige autorización backend por JWT/RBAC, membresía vigente, recurso y tenant derivado de identidad confiable. No aceptar tenant, roles o etiquetas privilegiadas del cliente.
- El camino de reversión del *changeset documental* es `git revert` normal del commit causal; preservar BITACORA. Reversión de servicios futuros solo después de pruebas aisladas y autorización independiente.
- `SLO`, p95/p99, presupuesto de error, retención, costo real, sampling, cardinalidad, volumen, carga y disponibilidad: **NO_MEDIDO**. No inventar objetivos, umbrales, denominadores ni resultados.

## G01–G16. Puertas verificables para una implementación futura

| Gate | Condición que deberá acreditarse | Si falta la evidencia |
| --- | --- | --- |
| G01 | Inventario firmado de superficies de logs, métricas, trazas y alertas; dueño y propósito | No activar señal nueva |
| G02 | Perímetro de tenant A/B, membresía y autoridad de plataforma/empresa comprobados | Rechazo fail-closed |
| G03 | JWT/RBAC, roles revocados y IDOR/BOLA probados con negativos reales | No ofrecer consulta privada |
| G04 | Esquema versionado, clasificación de información y allowlist de campos/etiquetas | Descartar evento inseguro |
| G05 | Redacción previa de PII, cookies, tokens, cabeceras, IP, SQL con valores y secretos | Bloquear serialización/exportación |
| G06 | Causation ID, correlation ID opaco, SHA y UTC demostrables sin atribuir acceso por ID | Rechazar correlación ambigua |
| G07 | Idempotencia, replay/TTL, clave+payload distinto y concurrencia probados | Rechazar duplicidad/conflicto |
| G08 | Límites de tamaño, tasa, cardinalidad y muestreo aprobados y ensayados en frontera | Deshabilitar ingestión/exportación |
| G09 | Política de retención y borrado legal aprobada, con segregación tenant y auditoría | Retener cero datos nuevos |
| G10 | Métricas SLI con numerador, denominador, ventana, ambiente, perfil y unidad | Reportar NO_MEDIDO |
| G11 | SLO y error budget con umbrales autorizados, historia comparable y muestras suficientes | Sin PASS de SLO ni alerta |
| G12 | Diagnóstico reproducible, traza sintética, incidente positivo y negativo verificados | No afirmar observabilidad completa |
| G13 | Fallo, timeout, caída del collector y pérdida de señales medidos sin degradar auth | Falla segura, pérdidas contadas |
| G14 | Estimación de costo/cuota y control de pago con permiso comercial expreso | Sin activar proveedor/cobro |
| G15 | Pruebas CI pertinentes SUCCESS, exact-SHA o equivalencia de blobs+dependencias, Scope Lock/V5 | No declarar certificación técnica |
| G16 | A0–A4, rollback probado, readback institucional y A5 independiente de otra Task Javier | Sin salida final de fase |

Los gates son **condiciones contractuales futuras**, no dieciséis comprobaciones ejecutadas hoy. Cada uno deberá registrar evidencia (run, SHA, log, autor, fecha, numerador/denominador o `NO_MEDIDO`), fallo y plan de reversión. Para una ejecución posterior R2 de integración, G02–G08, G13 y G15 requieren pruebas adversariales y reales, no solo texto.

## Interfaces, consumidores y aislamiento

La envolvente conceptual de un evento incluye `event_type`, `schema_version`, `source_commit_sha`, `occurred_at_utc`, `correlation_id` opaco, `causation_id`, `idempotency_key`, `classification`, `environment`, `synthetic_run_id`, `measurement_window` y `payload_schema_ref`. Las etiquetas exportables son una allowlist limitada. El identificador de empresa pertenece al contexto protegido backend y **no se exporta como dimensión libre**.

Consumidores futuros posibles: equipo de soporte con lectura mínima autorizada, API interna de salud, servicios de auditoría y dashboards segregados; ninguno se crea o habilita aquí. No usar un `correlation_id` como credencial. Políticas revocadas invalidan lecturas futuras y buffers privados. Proveedores nuevos están desactivados hasta consentimiento técnico, comercial y de seguridad.

La guía de implementación debe definir en un changeset futuro la interacción con middleware, versiones de esquema, observadores y UI; si introduce nueva frontera arquitectónica, actualizar `ARCHITECTURE.md`, `PROJECT_CONTEXT.md`, `PROJECT_INDEX.md` y `ARCHITECTURE_CHANGELOG.md` según gobernanza. Las migraciones requieren MySQL 8.4 efímero y pruebas Category=Integration realmente ejecutadas; no basta un job verde con filtros que excluyan integración.

## A1–A3. Oráculos de aceptación T01–T12 (FUTUROS_NO_EJECUTADOS: 0/12)

| Test | Positivo futuro | Negativo / frontera futura |
| --- | --- | --- |
| T01 | Operador autorizado consulta señal propia | Tenant B o miembro revocado recibe denegación |
| T02 | Evento con esquema allowlisted y firma/autor válidos se acepta | Campo desconocido o token en payload se rechaza |
| T03 | Correlation y causation enlazan el SHA y run correcto | Correlación ajena jamás concede autoridad |
| T04 | Repetición misma clave/carga tiene un solo efecto | Misma clave con carga distinta produce conflicto |
| T05 | TTL vigente y clave de replay válida permiten idempotencia | TTL vencido/replay malicioso se rechaza |
| T06 | Dimensiones aprobadas limitan cardinalidad | Etiqueta arbitraria o cardinalidad límite+1 falla |
| T07 | Tamaño/tasa exactamente dentro de cuota aprobada pasa | Un byte o petición encima del límite falla |
| T08 | SLI con conteos y ventana reproduce cálculo | Falta denominador, ventana o unidad: NO_MEDIDO |
| T09 | Objetivo firmado usa umbral vigente y muestra suficiente | SLO no aprobado no produce PASS/alarma |
| T10 | Fallo de collector limita reintento y cuenta pérdidas | Retry infinito o fuga de PII queda bloqueado |
| T11 | Reporte de costo sintético conserva unidades y volumen | Falta autorización comercial: sin cobro/proveedor |
| T12 | Revert de dev conserva trazabilidad y dato preexistente | Force push, borrar BITACORA o promoción sin A5: rechazo |

**Ninguna de estas doce pruebas se ejecutó.** No existe certificación funcional HTTP/MySQL/browser, carga SLO, proveedor o alerta real en este changeset. A1 comprueba que los casos están especificados, no que se hayan ejecutado; A2 revisa estáticamente tenant A/B, permisos y privacidad. A3 (runtime, MySQL, Angular, broker, exporter, Playwright) = **NO_APLICA** para el diff estrictamente Markdown R0.

## RB01–RB08. Contención y rollback futuros

- **RB01:** revocar política o feature flag desde backend confiable, preservando auditoría.
- **RB02:** suspender collector/exporter sin desactivar controles de acceso.
- **RB03:** cortar ingesta en tenant afectado; no borrar ni exponer datos de otro tenant.
- **RB04:** degradar a modo sin señales nuevas, contar pérdidas y no afirmar disponibilidad.
- **RB05:** frenar retries y mensajes duplicados, conservar claves e historial causal.
- **RB06:** recuperar snapshot sintético aislado con checksum y rollback probado si hubiera DB futura.
- **RB07:** verificar integridad de mediciones, costos y retención tras revertir, con informe de pérdidas.
- **RB08:** revertir commit de dev por fast-forward/commit de reversión normal; conservar eventos BITACORA y solicitar nuevo A5 si cambia artefacto aprobado.

## A4 y A5. Estado de este entregable

El cierre **R0 documental** requiere verificar que el diff publicado solo incorpora este Markdown y una sección de `CHANGELOG_AI.md` autocontenida, sin encabezado vacío ni atribución cruzada; que `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` terminen `completed/success` sobre el SHA de publicación (o equivalencia rigurosa), y que PLAN/COLA/DAG/BITACORA muestren readback propio. Checks runtime no exigibles por este diff son `NO_APLICA` motivado, jamás PASS implícito.

`LISTO_DEV` con A0–A4 no equivale a `A5_PASS`. Un revisor Javier de TASK_ID **distinto** debe inspeccionar esta entrega, sus predecesores y pruebas, firmar A5 con SHA/blobs y readback o rechazarla con defecto. Cualquier ejecución posterior de métricas, logs, exportadores o dashboards deberá tratarse como alcance R2 separado, con pruebas reales y autorización vigente. No promover a QA/main/PROD.
