# SOLQARYN V17 — C02-01.M03.04: preferencia tenant aislada por usuario

**Autor:** PRIMARY DEV 04 Javier (6aeacb7820ac8191b7d768a6f33dfa09). Solo `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Riesgo V15:** R2 (tenant, RBAC, concurrencia y preferencia persistida). **PLAN:** fila 305; candado V17 `SQV17_C02_01_M03_04` / `2000000305`.

## A0 — alcance real y dependencias

Se incorpora `C0201TenantPreferenceIsolatedSnapshotAdapter` (.NET 10 Infrastructure) como lector **sintético no autoritativo** de la decisión Application `V17IntelligentTenantLoginAuthorization` entregada en C02-01.M03.03. Captura y congela membresías de una instantánea de prueba, exige identidad de actor, revisión positiva y SHA fuente de 40 hexadecimales iguales a los observados; jamás concede acceso o emite JWT. No registra el adaptador en DI/HTTP, no crea base de datos ni consulta EF/MySQL. M03.04 permite adaptador aislado; conectar la preferencia real de usuario a persistencia, revalidar JWT y purgar caches exige una implementación posterior autorizada y evidencia propia.

## A1/A2 — pruebas de código y aislamiento

16 pruebas xUnit positivas, negativas, límites y cancelación: un tenant, N tenants y preferencia válida del mismo actor, preferencia de otra cuenta/revocada/desconocida, actor falso, revisión stale, SHA distinto, tenant seleccionado ajeno, membresía revocada, pistas de browser/host/rol, global sin concesión, mutación de colección posterior, sesión inválida, rol revocado, duplicados, constructor inválido y cancelación. La decisión Application siempre exige un adaptador autenticado separado para emitir credenciales reales. No se certifica L01–L10 de extremo a extremo, login HTTP, sesión JWT, MySQL ni routing.

## A3 — integración pertinente

Se enlaza código Infrastructure → Application en build/test .NET 10 GitHub-hosted con xUnit real. No se altera `DbContext`, EF, SQL, migraciones, persistencia ni UI; MySQL 8.4 efímero, Playwright, API real y transacción son **NO_APLICA al diff aislado** y serán obligatorios cuando se conecte el proveedor real. No se almacena contraseña, token, DSN ni PII.

## A4 — verificación y reversión

Un único commit fast-forward/CAS directo a `dev`, sin segundo WIP. Exigir comprobaciones `Backend Release y pruebas`, `SOLQARYN - Build y pruebas backend`, `SOLQARYN - Seguridad multiempresa`, `Authorization, files, secrets and tenant gate`, `verify-solqaryn-project-scope` y procedencia V5 `completed/success` sobre SHA exacto o equivalencia de blobs auditada, con readback PLAN/COLA/DAG/BITACORA y lock liberado. CI en cola conserva VALIDANDO y owner, sin inventar PASS. Reversión con `git revert` normal, sin force-push ni QA/main/PROD.

## A5 — independiente

Un TASK_ID distinto de las otras cinco Javier revisa el diff y las pruebas sin aceptar el relato del autor. LISTO_DEV (A0–A4) no equivale a A5 ni habilita cambios de sesión real.
