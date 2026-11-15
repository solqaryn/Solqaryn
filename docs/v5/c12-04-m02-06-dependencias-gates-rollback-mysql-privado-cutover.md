# SOLQARYN V16 — C12-04.M02.06 contrato de salida, gates y rollback MySQL privado / cutover

**Microtarea:** C12-04.M02.06, PLAN fila 1849, R0 documental · **Owner:** PRIMARY DEV 03, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3` · **Repo:** `solqaryn/Solqaryn@dev` · **PROJECT_SCOPE_LOCK:** STRICT · 2026-11-15.

**Autoridad institucional:** STANDARD DE IMPLEMENTACION DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. **No implementar** migraciones, backups, restores, DB privados, eventos, workers, cambios de Aiven, Hostinger, Cloudflare, DNS, secretos, QA/main/PROD ni servicios de pago.

## 1. Prerrequisitos y salida documental

Se establece el contrato final M02 para un **futuro** MySQL privado/cutover sin mover datos. La implementacion existente declara ASP.NET Core 10, EF Core 10.0.12 con Oracle MySql.EntityFrameworkCore 10.0.9 y MySQL 8.4, pero aqui no se prueba instancia, grants ni conexion. Preservar persistencia historica, separacion tenant A/B, rollback y limites de autorizacion. El resultado de M02.06 no acredita migracion ejecutada.

| Antecedente | Artefacto / blob actual | Contrato y limite |
| --- | --- | --- |
| M02.01 | `docs/v5/c12-04-m02-01-entidades-ownership-mysql-privado-cutover.md` blob `d1a12db8a678cca012f45039b2ee5ab7c3367b4` | 12 entidades y 16 invariantes conceptuales |
| M02.02 | `docs/v5/c12-04-m02-02-contratos-api-dto-errores-cutover-mysql.md` blob `26efab9bdf40be20b6040017c2bc66b6ba8b1d05` | 9 operaciones futuras y DTO/errores, sin API |
| M02.03 | `docs/v5/c12-04-m02-03-persistencia-indices-migraciones-fixtures-mysql-privado.md` blob `39924d297cfa9f258c93a9497e89241891058b91` | 12 proyecciones, constraints, migraciones y fixtures NO creados |
| M02.04 | `docs/v5/c12-04-m02-04-opciones-flags-cutover-mysql.md` blob `9c6ab074dd1726879d2fe18fa1fb96d83960a63d` | 14 opciones y 14 validaciones inertes |
| M02.05 | `docs/v5/c12-04-m02-05-eventos-idempotencia-correlacion-limites-mysql-cutover.md` blob `22c6fb317680b4cdcb3c7776d7398f246ca91953` | 10 señales futuras, 16 invariantes, sin eventos emitidos |

Ademas se requiere **C12-04.M01.01–06 LISTO+A5** individual y **C12-04.M02.01–05 LISTO+A5**, segun DAG exacto, sin inferir aprobacion QA/PROD. Preclaim C12-04.M02.06: PLAN H1849=PENDIENTE, I1849 vacio; COLA G1849/O1849=SI; DAG A1810, F1810=VALIDADO, G1810=SI. Tras el CLAIM CAS solo esta unidad se desarrolla, con una EN_PROGRESO propia a la vez.

## 2. Dieciseis controles G01–G16 (bloqueos FUTUROS_NO_EJECUTADOS)

| ID | Motivo | NO_GO si |
| --- | --- | --- |
| G01 | Autoridad | No existe permiso explicito de base, entorno o destino, no cutover |
| G02 | Identidad/tenant | Resolucion tenant por query/host cliente sin verificacion JWT/RBAC server-side |
| G03 | Grants MySQL | Cuenta audit/maintenance obtiene privilegios administrativos o WITH GRANT OPTION |
| G04 | Schema EF | Proveedor/version EF o mappings no alineados con Oracle MySql.EntityFrameworkCore |
| G05 | Concurrencia | Dos migradores/owners compiten sin lease/CAS transaccional |
| G06 | Backup | Backup cifrado, integro, restaurable y verificado no existe en entorno autorizado |
| G07 | MySQL 8.4 | No corrieron realmente tests Category=Integration con MySQL 8.4 efimero |
| G08 | Expand-contract | DDL destructivo o renombre rompe consumidores existentes/rollback |
| G09 | Trazabilidad | No se pueden conciliar conteos, checksums y relaciones por tenant |
| G10 | PII/secretos | Dump, log, URI de conexion o historial expone datos sensibles |
| G11 | RBAC | Revocacion, roles y membership no probados con casos A/B negativos |
| G12 | Cutover | Cambio DNS/infra/connection string sin ventana, autorizacion y plan de reversa |
| G13 | Integridad | Pedidos, facturas y precios pierden claves/snapshots durante transformacion |
| G14 | Replicacion | Desfase/no determinismo o reconciliacion no medidos se asumen PASS |
| G15 | CI | Check requerido failed/skipped/cancelled/queued/in_progress se toma por success |
| G16 | Gate final | Faltan A0-A4, Scope Lock STRICT, A5 independiente, DAG o permiso de entorno |

Los G01-G16 no son defectos confirmados de Aiven/Render ni pruebas ejecutadas. Ninguna salida R0 ni un A5 documental autoriza tocar un servidor real. Las futuras migraciones requeriran MySQL 8.4 efimero y pruebas verdaderas Category=Integration, cero FAIL, verificando aislamiento, permisos y reversa. No convertir un build verde en prueba DB.

## 3. Ocho acciones RB01–RB08 para rollback FUTURO_NO_EJECUTADO

| ID | Runbook condicionado; no ejecutado |
| --- | --- |
| RB01 | Guardar SHA, manifiesto de esquemas, versiones de API/EF/MySQL y owners del cambio |
| RB02 | Probar respaldo/restauracion completa en entorno efimero con datos sinteticos A/B |
| RB03 | Establecer migrador unico, lease/CAS y bloqueo de escrituras de migracion concurrentes |
| RB04 | Ensayar expand-contract con verificaciones de consistencia, indices/keys y control de transacciones |
| RB05 | Comparar conteos/hashes por tenant y snapshots de inventario, facturas y pedidos |
| RB06 | Deshacer solo opciones tipadas versionadas y conexiones autorizadas, sin hardcoded secrets |
| RB07 | Realizar smoke de autenticacion, aislamiento A/B, 401/403, DB y rollback en entorno aislado |
| RB08 | Registrar incidentes SHA, aprobacion independiente, secuencia de reversion y readbacks |

Estas acciones son **planes**, no restauraciones realizadas. Para la entrega R0 actual, rollback posible: `git revert` normal del commit Markdown verificando HEAD/owners, preservando BITACORA historica. DDL, proveedor, DNS, DB y clientes siguen sin modificar.

## 4. Diez pares P/N T01–T10 FUTUROS_NO_EJECUTADOS

| Test | Positivo futuro | Negativo/error futuro |
| --- | --- | --- |
| T01 | Un tenant lee solo su esquema/datos autorizados | Tenant B aparece desde A o rol revocado sigue viendo datos |
| T02 | Migrador unico adquiere lease valido | Dos migradores inician concurrently y ambos modifican DDL |
| T03 | Restore reconstruye fixtures y referencias | Dump incompleto o checksums divergen al restaurar |
| T04 | Expansiones no rompen API actual | DDL destructivo elimina columna exigida por cliente anterior |
| T05 | Cifrado y secrets se mantienen fuera del repositorio | DSN/token/password aparece en Git o logs |
| T06 | Reconciliacion de conteos por tenant | Conteos agregados ocultan faltantes por tenant B |
| T07 | EF Oracle funciona con MySQL 8.4 efimero | Category!=Integration se interpreta falsamente como prueba DB |
| T08 | Cutover mantiene transacciones e idempotencia | Retry repite factura o pedido despues de timeout |
| T09 | Rollback restaura datos, schema y permisos | Rollback parcial deja permisos amplios o relaciones rotas |
| T10 | Controles de CI y A5 exactos acreditan cambio | SKIPPED/CANCELLED o self-review habilita M03 |

**Cobertura actual:** 10/10 parejas **especificadas**, **0/10 ejecutadas** en MySQL, .NET, API o navegador. Se require probar con fixtures sinteticos en ambiente autorizado, logs y SHA exacto al pasar a M03/M05. Esta unidad nunca declara pruebas productivas, restore real ni migracion de datos.

## 5. Cierre A0–A5 y limites de autoridad

- **A0:** Documento M02.06 de prerequisitos y matriz de riesgos con 5 contratos M02.01–05, M01.01–06 y rollback; diff limitado a dos Markdown; V15 R0.
- **A1:** 16 gates, 8 acciones de reversa y 10 pares positivos/negativos definidos; son pruebas FUTURAS, no PASS runtime.
- **A2:** Documentados JWT/RBAC/tenant A/B, revocacion, DB grants minimos, datos sensibles, scopes y concurrencia fail-closed; seguridad no alterada.
- **A3:** Coherencia contractual de M01/M02, sin ejecutar EF, SQL, MySQL, API, Angular, Browser o proveedor; esos jobs NO_APLICA al diff Markdown.
- **A4:** Commit fast-forward/CAS dev con Scope Lock y procedencia V5 `completed/success` exact SHA o equivalencia auditada, mas PLAN/COLA/DAG/BITACORA readback. Checks requeridos en progreso/cancelled no son PASS.
- **A5:** Solo otra de las cinco Tasks Javier, TASK_ID distinto del autor, puede revisar y acreditar despues de LISTO; no se autopasa.

**Salida:** LISTO_DEV significa contrato documental A0–A4, no significa que C12-04.M03, migrador o cutover esten listos. Antes de reclamar M03 recalcular PLAN H/I, COLA G/O y DAG F/G y verificar A5 reales; riesgo R2 para codigo EF/MySQL, pruebas aisladas y permisos de entornos. Nunca forzar formulas SI o alterar secretos, usuarios DB, produccion, ramas QA/main, RRULE o prompts.
