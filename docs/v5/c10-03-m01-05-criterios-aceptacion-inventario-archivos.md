# SOLQARYN V5 — C10-03.M01.05: criterios observables de inventario real y almacenamiento futuro

**Microtarea:** C10-03.M01.05, PLAN_MAESTRO fila 1518, DAG_TECNICO fila 1479 F=VALIDADO/G=SI al preflight. **Autor:** PRIMARY DEV 01 / TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Repo:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT, fecha 2026-11-14.

## Objetivo y distinción de resultados

El resultado exigido de fase es **verificar un inventario real de medios; si no existen archivos históricos, registrar cero y omitir exportación, copia, checksum o carga de migración; preparar almacenamiento seguro para futuros archivos**. El inventario real al corte sigue **NO_VERIFICADO**: esta M01.05 no consultó proveedores, MySQL de producción ni archivos. No se infiere cero. No afirma que Cloudflare R2 esté activo ni que Cloudinary carezca de archivos. Esta unidad es **exclusivamente documentación de aceptación futura**, no una prueba en vivo, funcionalidad implementada, permiso de cutover o nueva migración.

La aceptación de futuros scopes M03 o de inventario exige **resultados observados** con fuente autenticada, tenant y ambiente correctos y readback. Una fila de DAG validada sólo autoriza esta documentación, no acceso a proveedor o apertura de datos.

## Preparación de fixtures sintéticos para pruebas FUTURAS

- **F01:** empresas A y B sintéticas, usuario anónimo, usuario suspendido, lector, gestor y operador con permisos minimizados. JWT y pertenencia resueltos en backend; header/slug nunca autoridad de tenant.
- **F02:** fuente simulada de referencias MySQL, proveedor efímero y un segundo proveedor representado por stub, con casos disjuntos y objetos huérfanos; ningún dato real.
- **F03:** escenarios separados con conteo auténtico futuro 0, positivo e inconsistente, con fecha de corte, metadatos y lista de proveedor; `NO_VERIFICADO` no participa como cero.
- **F04:** archivos sintéticos con bytes correctos/incorrectos, MIME, extensión, tamaño, stream y checksum, además de políticas de retención y hold.
- **F05:** fake clock UTC para TTL, firma/método/objeto, control explícito de bearer replay, invalidación cache A/B y error de proveedor inyectado.
- **F06:** reporte redactado con hash de manifiesto si existieran históricos, correlación, ambiente, owner, autorización explícita y rollback de fixture no productivo; nunca secretos ni URL firmada.
- **F07:** pruebas reales en GitHub Actions sólo cuando se altere runtime: .NET 10 backend/xUnit, MySQL 8.4 efímero para EF, Angular 22.2.1 y Playwright para UI. Checks `completed/success` del SHA exacto requeridos; `SKIPPED`, `CANCELLED`, `FAIL` o `IN_PROGRESS` nunca PASS pertinente.

## Matriz AC01–AC14: positivos y negativos propuestos, NO_EJECUTADOS

| AC | Positivo futuro | Negativo futuro | Evidencia indispensable |
| --- | --- | --- | --- |
| AC01 | P01: inventario autenticado completo acredita cero real | N01: se reporta cero sin consultar ambas fuentes | Fecha, proveedor, base, identidad y conciliación |
| AC02 | P02: cero acreditado omite toda exportación/copia/checksum histórico | N02: al conocer cero inicia una migración vacía innecesaria | Pipeline registra SKIP_HISTORICAL y cero operaciones |
| AC03 | P03: inventario positivo concilia objetos y referencias | N03: se omiten objetos huérfanos o fuentes | Manifiesto sin datos sensibles y casos discrepantes |
| AC04 | P04: estado inconsistente genera NO_GO a la migración | N04: inconsistencia se interpreta como cero | Estado explícito y caso negativo reconciliado |
| AC05 | P05: A consulta sólo metadatos A autorizados | N05: A enumera archivos B | Tenant A/B en API y DTO no enumerativo |
| AC06 | P06: gestor autorizado obtiene capacidad propia | N06: lector o cliente manipula owner para export/delete | Test JWT/RBAC y contexto server-side |
| AC07 | P07: fixture nuevo tiene origen NEW y no es histórico | N07: nuevo fixture se suma al conteo histórico | Fuente y clasificación discriminadas |
| AC08 | P08: upload de fixture válido verifica bytes MIME extensión | N08: MIME/extension falsificados pasan | Validador de bytes, límites y checksums |
| AC09 | P09: tamaño, cuota y stream correctos son aceptados | N09: exceso/truncado deja objeto parcial | Test límite y limpieza transaccional o compensación |
| AC10 | P10: firma limita recurso, método y expiración | N10: replay se presume prohibido sin estado one-time | Reloj falso, TTL y política de reuso explícita |
| AC11 | P11: cambiar A→B invalida caché anterior | N11: CDN/cliente sirve objeto A a B | Test caché y control de sesión |
| AC12 | P12: archivo con retención/hold permanece íntegro | N12: lifecycle elimina archivo bajo hold | Fixtures de retención y restauración |
| AC13 | P13: falla el proveedor y se recupera sin duplicados | N13: timeout/retry publica objeto parcial o token en log | Fault injection, idempotencia y redacción |
| AC14 | P14: propuesta futura de migración autorizada prueba rollback | N14: cutover R2/DNS sin autorización, hashes o reversión | Gate externo, validación de hashes y plan reversible |

**Todos los P01–P14/N01–N14 anteriores son FUTUROS NO_EJECUTADOS;** no son ejecuciones exitosas ni evidencia actual de proveedor. AC02 aplica únicamente tras acreditar AC01 con cero. AC14 es una precondición de futura fase posterior y no permite ejecutarla desde este documento.

## NFR01–NFR08 de aceptación no funcional

| NFR | Requisito observacional para trabajo futuro |
| --- | --- |
| NFR01 | NO_VERIFICADO, CERO, POSITIVO e INCONSISTENTE se conservan como estados distintos con evidencia |
| NFR02 | No se exportan/copian/hashean históricos cuando el inventario cero está verificado |
| NFR03 | No se permite enumeración ni lectura cross-tenant A/B y auth backend default-deny |
| NFR04 | Logs/manifiestos no revelan URLs firmadas, keys, tokens, secretos o PII |
| NFR05 | MIME se valida por bytes y límites de tamaño/stream, checksum y cuota |
| NFR06 | Retención y hold bloquean eliminaciones no autorizadas |
| NFR07 | Fallas y reintentos son idempotentes, con compensación y rollback probado |
| NFR08 | No hay migración, cutover ni promoción QA/main/PROD por un gate M01 documental |

**Trazabilidad de fuentes:** C10-03.M01.01 (blob `ca7ccfaf423ead017a127acde393d53cf6ab181b`) para ramas de inventario y NR01–NR14; C10-03.M01.02 (blob `dde15b96f610dcd0b7712129e69bcba04a8586bd`) para A01–A14, I/O01–14 y F01–F09; C10-03.M01.04 (blob `5430f7eeacd2203324617acc61d428c4b3403d21`) para R01–R14, S01–S08 y D01–D08. C10-02.M01.01–05 complementa URL privada/TTL/replay y controles tenant; ninguna fuente acredita inventario ni proveedor real.

## A0–A5, CI causal, impacto y rollback

**Modificaciones de esta unidad:** sólo este Markdown y una nueva sección indivisible de `CHANGELOG_AI.md` en dev. Ningún cambio de Domain/Application/Infrastructure/API, endpoints, Angular, SQL, EF, JWT/RBAC, Cloudinary, R2, DNS, cuentas, secretos, workflows o datos.

- **A0:** claim único con PLAN PENDIENTE libre, COLA G/O=SI y DAG fila1479 F=VALIDADO/G=SI, ownership exclusivo, Scope Lock STRICT.
- **A1:** AC01–AC14, P/N futuros y NFR validados como contratos falsables; resultados runtime y conteo real NO_EJECUTADOS/NO_VERIFICADOS.
- **A2:** revisión documental de JWT/RBAC/tenant A/B, MIME, TTL, replay, caché, retención, PII y acceso mínimo.
- **A3:** coherencia documental con M01.01/.02/.04 y C10-02; tests .NET/Angular/MySQL/Playwright **NO_APLICA por diff sólo Markdown**, no PASS inventado.
- **A4:** publicación GitHub fast-forward CAS, checks `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` **completed/success exact SHA**, readback PLAN/COLA/BITACORA.
- **A5:** PENDIENTE hasta inspección independiente por otra Task Javier distinta del autor; no autoprobación.
- **Rollback:** `git revert` normal del commit sin force-push; M01.06, M02, M03, datos y despliegue mantienen gates propios. Nunca QA/main/PROD por este documento.
