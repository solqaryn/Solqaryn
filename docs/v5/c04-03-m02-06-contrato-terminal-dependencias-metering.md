# SOLQARYN V17 — C04-03.M02.06: contrato terminal y dependencias de metering

**Autor:** PRIMARY DEV 05 Javier, TASK_ID `6adc2e55f47c81919f6374e561687076`. **Gobernanza:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`, STANDARD V5 FINAL. **Origen:** PLAN_MAESTRO fila 661, COLA G/O=SI al claim, DAG_TECNICO fila 622 F=VALIDADO/G=SI. **Candado:** V17 `SQV17_C04_03_M02_06`, ID `2000000661`. **Riesgo:** R0 documental, sin runtime ni autorizaciones comerciales.

## A0 — objetivo, dependencias y límite de autoridad

Esta unidad consolida **exclusivamente el contrato de salida M02** para medición de usuarios, operaciones, almacenamiento, archivos y uso por empresa. El preflight institucional comprobó `C04-03.M01.01–06` y `C04-03.M02.01–05` en LISTO con A5 independientes, y `C01-03.M01.06+A5` como predecesor del DAG. Ninguna fase M03 se libera sin su propio DAG y pruebas; LISTO de M02.06 tampoco suple **A5 independiente** de esta microtarea. No se crean endpoints, DTO en código, workers, eventos reales, impuestos, tarifas, facturas, migraciones, tablas, cuotas ni transacciones. D01–D08 (aprobaciones comerciales/políticas de metering) siguen **PENDIENTE** y nunca se infieren de este documento.

**Fuentes técnicas consolidadas:** M02.01 entidades, claves y ownership; M02.02 operaciones/DTO, errores y compatibilidad; M02.03 modelo conceptual de persistencia/índices/fixtures sin SQL ejecutado; M02.04 configuración tipada, ámbitos y flags fail-closed; M02.05 doce eventos conceptuales, identidad idempotente y correlación. Las diferencias entre documento y ejecutable son explícitas y permanecen trabajo futuro.

## Matriz terminal de 16 puertas G01–G16

| Puerta | Evidencia contractual necesaria para implementación posterior | Qué NO certifica M02 |
| --- | --- | --- |
| G01 — identidad | Tenant únicamente desde backend autenticado y sesión verificada | JWT HTTP real |
| G02 — propiedad | `EmpresaId` compuesto en recurso, referencias e índices futuros | Aislamiento SQL ejecutado |
| G03 — rol | Membresía y RBAC actuales/revocación sin fallback cliente | Autorización de API viva |
| G04 — origen | Operación/cantidad/unidad de evidencia server-side | Veracidad de proveedor productivo |
| G05 — catálogo | `MetricCode`, `MetricCatalogVersion`, `Unit` inmutables | Conversión tarifaria aprobada |
| G06 — tiempo | Ventanas UTC, límites y revisiones explícitas | Cobro de ventanas cerradas |
| G07 — idempotencia | Unicidad futura `(EmpresaId,SourceSystem,IdempotencyKey)`; digest aparte | Constraint DB ya instalado |
| G08 — conflicto | Misma clave con digest distinto → rechazo; nunca segundo ledger | Protección de carrera ya probada |
| G09 — atomicidad | Ledger/identidad/outbox en una transacción futura | Commit MySQL real |
| G10 — proyección | Ledger autoridad; proyección recalculable versionada | Conciliación ejecutada |
| G11 — reversión | Ajustes aditivos, no borrado de eventos ni auditoría | Restauración probada |
| G12 — privacidad | PII mínima, no JWT, ni secretos en eventos/logs/exportación | DLP o políticas live |
| G13 — límites | Fail-closed para `NULL`, 0, unlimited, overflow y enums desconocidos | Cuotas comerciales habilitadas |
| G14 — errores | Timeout/cancelación no devuelven éxito sin COMMIT | Retries distribuidos activos |
| G15 — despliegue | Scope Lock, CHANGELOG, CI exact-SHA y A5 propios de M03 | QA/main/PROD autorizados |
| G16 — comercial | D01–D08 aprobadas por autoridad competente separada | Precio, cobro, facturación o retención aprobados |

Una sucesora debe verificar explícitamente predecesores y aprobaciones que exige su DAG, además de la identidad/tenancy/seguridad de su diff. **M02.06 LISTO no es una licencia para saltarse G01–G16.** El sistema de fórmulas COLA/DAG sigue siendo el único calculador de elegibilidad; no se editan manualmente valores SI.

## A1/A2 — matriz de 12 pares para pruebas FUTURAS (NO ejecutadas)

| Positivo a implementar y ejecutar posteriormente | Negativo que debe bloquearse |
| --- | --- |
| P01 tenant A registra su propio evento | N01 actor A lee o deduplica evento de B |
| P02 replay mismo key y digest devuelve referencia original | N02 mismo key con digest distinto crea nuevo asiento |
| P03 dos productores concurrentes registran un efecto | N03 race confirma dos ledgers |
| P04 JWT/RBAC y membresía vigentes permiten preflight | N04 revocación aún admite operación |
| P05 fuente backend define cantidad y métrica | N05 browser manipula importe o cantidad |
| P06 catálogo mantiene unidad versionada | N06 mezcla bytes con operaciones sin conversión |
| P07 UTC estable identifica ventana única | N07 hora cliente falsificada altera corte |
| P08 política aprobada y vigente se consulta | N08 D01–D08 pendientes se tratan como aprobadas |
| P09 error transaccional mantiene estado reanudable | N09 fallo SQL se comunica como facturado |
| P10 ajuste aditivo referencia ledger anterior | N10 rollback borra historia |
| P11 log y exportación omiten PII | N11 JWT/email/token aparece en evento |
| P12 conciliación compara ledger y proyección | N12 divergencia dispara factura automática |

Estas son **especificaciones de aceptación**, no resultados de xUnit, MySQL 8.4, Playwright, HTTP o proveedores. Para posteriores cambios R2 se exigirán tests que puedan fallar realmente, integración MySQL efímero cuando haya EF/DB y navegador Chromium para UI, además de seguridad tenant A/B y revocación.

## Ocho rutas de reversión propuestas (futuras, sin ejecución)

1. **R01** Revertir el commit de un contrato incompatible mediante `git revert` normal tras comprobar HEAD/owner.
2. **R02** Deshabilitar futura opción de metering con flag backend autorizado, sin activar otro por defecto.
3. **R03** Ante conflicto de clave idempotente, denegar y conservar identidad original, no reiniciar ledger.
4. **R04** Ante fallo de procesamiento, dejar evento pendiente con causa, no emitir éxito sin COMMIT.
5. **R05** Ante discrepancia entre ledger/proyección, abrir reconciliación y suspender operaciones sensibles.
6. **R06** Ante permiso revocado, invalidar cache por tenant y negar nueva consulta sin borrar auditoría.
7. **R07** Ante política o versión inválida, bloquear cuotas/pagos hasta autorización explícita.
8. **R08** Ante migración futura fallida, ejecutar rollback probado en MySQL 8.4 efímero antes de tocar datos reales.

Ninguna ruta implica tocar hoy Aiven, Render, Vercel, bases reales, secretos, despliegues ni proveedores. El rollback de **esta entrega R0** es sólo el revert del commit documental.

## A3/A4/A5 — certificación proporcional y handoff

**A3:** SQL/EF/MySQL, JWT HTTP, xUnit backend, Angular, Playwright, workers, proveedores y escenarios de pago **NO_APLICA** como pruebas de runtime para este diff Markdown. **A4:** documento + encabezado CHANGELOG autocontenido en un commit dev fast-forward CAS de dos archivos, exact-SHA Scope Lock y V5 procedencia `completed/success`; readback PLAN/COLA/DAG y BITACORA, candado V17 antes de transiciones. No contar `queued/in_progress/skipped/cancelled/failure` como PASS de check requerido. **A5:** exclusivamente otro Task Javier distinto del autor inspeccionará contratos, source SHA, blob y CI; puede emitir FAIL. Ninguna revisión propia, ni QA/main/PROD.

**Salida institucional:** M02.01–05 fueron cerradas y revisadas independientemente; M02.06 necesita su A0–A4 y A5 propios. M03 permanece sometida a su DAG, seguridad R2 y decisión comercial donde aplique. D01–D08 no aprobadas.
