# SOLQARYN V5 FINAL — C15-03.M02.06: contrato de salida y dependencias de auditoría de seguridad/tenancy

**Autor:** PRIMARY DEV 01, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a` · 14 de noviembre de 2026 (America/Tegucigalpa) · `solqaryn/Solqaryn@dev` · `PROJECT_SCOPE_LOCK=STRICT`.

## 1. Autoridad, reclamo exclusivo y declaración de alcance

Fuentes únicas: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), `PLAN_MAESTRO!A2245:K2245`, `COLA!A2245:O2245`, `DAG_TECNICO!A2206:J2206`. Antes del claim: PLAN H=PENDIENTE e I vacío, COLA G/O=SI, DAG F=VALIDADO/G=SI; claim J00 exclusivo por transición CAS H→EN_PROGRESO, I=TASK_ID y readback. El antecedente M01.01–06 y los cinco M02.01–05 están LISTO con A5 independiente.

**Esta salida M02.06 es documental.** No contiene DDL, migrations, índices nuevos, código runtime, API, entidad EF, pentesting, auditoría de tráfico real, webhooks, servidores de eventos, nuevas Tasks, cambios de roles/JWT/RBAC, secretos ni operaciones QA/main/PROD. Sus escenarios de aceptación son ORÁCULOS_FUTUROS_NO_EJECUTADOS; tampoco declara cero hallazgos P0/P1.

## 2. Trazabilidad de predecesores con revisión A5 ajena al autor

| Predecesor | Artefacto conceptual | SHA fuente | Revisión independiente documentada |
| --- | --- | --- | --- |
| C15-03.M01.01–06 | Alcance, actores, fuentes, riesgos, criterios y puerta M01 | Consultar PLAN/BITACORA para cada unidad | Todas LISTO+A5, no autorización de runtime |
| C15-03.M02.01 | Modelo de amenazas, límites de ownership y controles SEC/INV | `b31a152b29c4165b05b2e7acc0c0d9749be560a0` | A5_PASS de otra Task J40, autor J30 |
| C15-03.M02.02 | Contratos de API/DTO/errores y denegaciones | `6c47140cabd2befb913cd6ef688b884f141ea170` | A5_PASS de J40, autor J30 |
| C15-03.M02.03 | Persistencia, índices y fixtures hipotéticos | `ceef8b40162c23b0baf6cf81f30d93449c184b12` | A5_PASS de J00, autor J40, blob `446f166c4d5527a44bcd2190371a290284e64e20` |
| C15-03.M02.04 | Configuración tipada, flags restrictivos y autorizaciones | `92a01c21476e38f3437bc202805d6b25ba121bcc` | A5_PASS de J40, autor J30 |
| C15-03.M02.05 | Eventos conceptuales, idempotencia y límites | `1cac833ca62a2db30a228098238dd0d6e19539c4` | A5_PASS de J40, autor J30 |

Cada PASS es una revisión independiente documental registrada en PLAN K y BITACORA A:Z; no un PASS de pruebas MySQL o peticiones HTTP. Después del A5 de M02.03, el DAG G2206 pasó a SI y COLA G/O2245 a SI. Esta liberación permite **preparar** M02.06, no ejecutar M03 o promover entornos. A5 de esta nueva M02.06 está PENDIENTE por otro TASK_ID distinto de J00.

## 3. Controles de salida verificables para M03: SECOUT01–SECOUT16

| Control | Condición técnica futura | Evidencia causal requerida |
| --- | --- | --- |
| SECOUT01 | Una identidad tenant procede de JWT/membresía/permiso servidor; encabezado de empresa nunca concede acceso | Tests HTTP A/B, sin tomar X-Empresa-Id como autoridad |
| SECOUT02 | Consultas EF sólo acceden al tenant verificado, incluso IgnoreQueryFilters y consultas elevadas | Pruebas de query guards, bypass fail-closed, snapshots y logs |
| SECOUT03 | Todas las FK/relaciones comerciales validan owner padre/hijo y pertenencia | Tests MySQL 8.4 efímero sobre 12 FK compuestas y negativos A/B |
| SECOUT04 | EmpresaId NULL legado permanece aislado sin reconciliación autorizada | Cero lectura/escritura por heurística, prueba de NULL y restore |
| SECOUT05 | Rol/membresía/sucursal revocados deniegan aunque JWT siga presente | Regresión de autorización y consulta a permisos vigentes |
| SECOUT06 | Tenant B no se filtra en errores, conteos, IDs, búsqueda, paginación ni logs de A | Pruebas de enumeración y redacción, 403/404 uniformes |
| SECOUT07 | Acceso a archivos y URLs verifica ownership, expiración, revocación y ACL | HTTP/descarga negativa B, URL vencida, sin bytes B |
| SECOUT08 | Jobs/colas y servicios de mantenimiento no omiten filtros comerciales | Escenarios de actor de servicio y de ausencia de tenant |
| SECOUT09 | Control plane y operaciones admin sólo con autorización y auditoría independientes | Roles y scope verificados, rechazo de identidad arbitraria |
| SECOUT10 | Ningún secreto, JWT, password, token, PAN/CVV, PII no necesaria aparece en evidencia/logs | Escaneo árbol, logs, artefactos y regresiones sanitizadas |
| SECOUT11 | Idempotencia/compensación no duplica ni altera efecto de otra empresa | Clave scoped tenant, concurrencia y transacciones reversibles |
| SECOUT12 | Señales de seguridad declaradas están separadas de ejecución y resultados reales | Diferenciar NOT_EXECUTED de PASS y de A5 documental |
| SECOUT13 | Fuente/commit/check run y su HEAD pertenecen al mismo SHA causal | GitHub exact-SHA status=completed conclusion=success; no CANCELLED |
| SECOUT14 | A5 siempre procede de otra Task Javier y corresponde al diff y evidencia exacta | TASK_ID autor distinto de revisor, readback PLAN K/BITACORA |
| SECOUT15 | Concurrencia de owners no permite doble claim ni dos escrituras | CAS/bloqueo realmente atómico con lectura posterior, no lectura+write |
| SECOUT16 | Fail o inconcluso no libera DAG, migraciones ni QA/main/PROD | PLAN H/I/K y DAG F/G y COLA G/O recálculo, sin forzar gates |

**Riesgo específico a revalidar en M03:** `TenantQueryBypassGuard` puede devolver una consulta sin agregar filtro si falta `AuthorizedReadEmpresaId`; esto NO se certifica seguro por la sola existencia de middleware. Se requieren pruebas exactas de solicitudes sin contexto, `IgnoreQueryFilters`, permisos de servicio, lectura A/B y combinación de guards EF. `CommercialCrossTenantWriteGuard` protege parte de las escrituras y **no** sustituye autorización de lectura ni RBAC.

## 4. Pares de pruebas NEG01–NEG12 — FUTUROS NO_EJECUTADOS

| Caso | Positivo esperado | Negativo a demostrar |
| --- | --- | --- |
| NEG01 | Token/membresía A lee recurso A autorizado | Token A conoce id B: 404/403 uniforme sin datos B |
| NEG02 | Query EF usa marca de tenant verificada | Sin contexto verificado, bypass no entrega filas A/B |
| NEG03 | Documento y líneas A conservan FK/ownership | Inyección de relación B o EmpresaId NULL falla |
| NEG04 | Sucursal A y rol vigente autorizan operación | Rol revocado o sucursal B deniega y no muta |
| NEG05 | Exportación A usa enlace temporal protegido | URL revocada/expirada o tenant B obtiene cero bytes |
| NEG06 | Control plane auditado permite operación a su admin | Usuario común/header spoofing no ejecuta job |
| NEG07 | Acción idempotente de A se aplica una sola vez | Replay o clave idéntica B nunca comparte resultado A |
| NEG08 | Logs de incidencia conservan correlación redactada | Stack trace, token JWT, PII B y secretos ausentes |
| NEG09 | GitHub run exact SHA completed/success se acredita | queued/skipped/cancelled/failed no recibe PASS |
| NEG10 | Simulación rollback restaura estado sintético A/B | Interrupción no deja cambios parciales cross-tenant |
| NEG11 | Revisión independiente emite evidencia por SHA | Autor propio o revisor fuera de seis no libera dependencias |
| NEG12 | Fuente institucional coherente activa sólo la siguiente PENDIENTE real | VALIDANDO/A5 faltante, owner ocupado o DAG NO bloquea claim |

NEG01–NEG12 **no se ejecutaron aquí**; se requiere CI dirigida .NET/API, aislamiento JWT/RBAC, EF/MySQL 8.4 efímero, seguridad real y revisión externa para acreditar cada caso.

## 5. Bloqueos, decisiones pendientes y rollback futuro

- **B01:** no existe autorización de negocio para exposición cross-tenant, IAM, migración de datos antiguos, nuevo proveedor ni producción. No permitir excepciones de seguridad por decisiones comerciales pendientes.
- **B02:** ninguna documentación de M02 acredita por sí sola el estado de pruebas M03. Hay que ejecutar negativos contra código/DB/HTTP reales antes de certificar.
- **B03:** confirmar cómo el middleware inyecta el ámbito tenant hasta cada consulta EF y la prohibición de consulta sin identidad con privilegios de servicio.
- **B04:** cada FK compuesta y flag de autorización debe contrastarse contra esquema EF real y provider Oracle, no crear índices duplicados por descripción.
- **B05:** A5 de M02.06 corresponde a otra Task Javier; este autor no puede autoaprobarla.
- **B06:** los estados y gates son dinámicos; sólo COLA G/O=SI y DAG F=VALIDADO/G=SI con PLAN PENDIENTE sin owner autorizan claim.
- **B07:** rollback documental de este changeset: `git revert` normal del commit en dev + BITACORA compensatoria append-only/readback, sin reescribir historia.
- **B08:** implementación SQL/EF/API futura exige un rollback técnico probado en MySQL efímero y pruebas de denegación por empresa/usuario/sucursal.

## 6. Evidencia A0–A5 de esta microtarea

**A0:** cotejo de fuentes oficiales y matriz de seis antecedentes, SECOUT01–16, NEG01–12 y B01–08; dos Markdown en diff; no cambios productivos ni secretos. **A1:** revisión de oráculos positivos/negativos como texto FUTURO_NO_EJECUTADO, jamás PASS HTTP. **A2:** JWT/RBAC, empresa A/B, cuentas servicio, archivos, control plane, PII y legacy NULL considerados sin alegar aislamiento vivo. **A3:** coherencia con Domain/Application/Infrastructure/API y con artefactos M02.01–05; no SQL ni integración ejecutada, por lo que .NET/MySQL runtime NO_APLICA al diff documental. **A4:** publicación por fast-forward/CAS `dev`, Scope Lock y V5 CHANGELOG `completed/success` exact-SHA, SHA/blobs/diff y readbacks PLAN/COLA/DAG/BITACORA. **A5:** `A5_PENDIENTE` por una de las otras cinco Tasks Javier; no es PASS de seguridad producción.

Esta unidad no crea nueva API, DTO, DDL, migration, job ni webhook. Toda certificación runtime y despliegue sigue sujeto a gates posteriores y autorización expresa.
