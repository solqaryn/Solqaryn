# SOLQARYN V5 — C12-04.M02.03: diseño de persistencia, índices, migraciones y fixtures para MySQL privado/cutover

**Autor:** PRIMARY DEV 03 — JAVIER, TASK_ID 6adc2e4bd8a48191ac7d8641ed95a1a3. **Ámbito:** solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT. **Fecha:** 2026-11-15. **Tipo:** CONTRATO DOCUMENTAL de M02.03; NO contiene DDL, migración EF, backfill, exportación real, dump, restore, MySQL privado conectado o cutover.

**Autoridad única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA; PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA en Sheet 1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU. Preflight individual comprobó PLAN H=PENDIENTE/I vacía, COLA G/O=SI, DAG F=VALIDADO/G=SI, M01.01–06 LISTO+A5. El ID M02.03 es independiente de M02.01, M02.02, M02.04 y M02.05; no se presupone su cierre.

## 1. Hechos de código y límites

| Fuente Git de dev | SHA de blob de inventario M01.03 | Uso correcto como evidencia |
| --- | --- | --- |
| backend/src/Infrastructure/Persistence/AppDbContext.cs | c4b32f6637f1b4426655bb0cd12af8dd9d229866 | DbContext y contextos Verified* de tenant; no acredita conexión ni consulta real de destino MySQL. |
| backend/src/Infrastructure/Solqaryn.Infrastructure.csproj | 6210d8b12abd77171d95d17e2292c1a319b89263 | .NET 10, EF Core 10.0.12 y MySql.EntityFrameworkCore 10.0.9; versiones de destino deben verificarse aparte. |
| backend/src/Infrastructure.Migrations/Solqaryn.Infrastructure.Migrations.csproj | 4fb87115e0c1e38a436d777c898341068c6cc668 | Proyecto de migraciones, sin prueba de DDL aplicado a una base. |
| backend/src/API/Program.cs | 01425cff56eb13e12022b1b81a2f322d17fd1fff | Composición API, middleware y DI; no demuestra autoridad de migrador ni cambios de hosting. |
| docs/v5/c12-04-m01-03-inventario-codigo-evidencia-mysql-cutover.md | 4da1375d688b2b96ce09f375baf3993866f18451 | 16 fuentes y G01–G14 como brechas, no incidencias confirmadas ni datos reales. |
| docs/v5/c12-04-m01-06-bloqueos-rollback-liberacion-mysql-privado-cutover.md | 0e1d0699054dc5039a29b766db75bf92aa754ed9 | B01–B16, P/N futuros y política de rollback, sin aprobaciones de cambio de proveedor. |

Las huellas SHA refieren a **archivos versionados**; no son checksums de tablas, respaldos, dumps o filas empresariales. Aiven, Render/Vercel y Cloudinary permanecen infraestructura descrita como actual; Hostinger/MySQL privado y Cloudflare no están activados ni autorizados por este documento. Se desconoce el volumen real, versión de destino, claves FK y cardinalidades de producción: nunca asumir inventario vacío.

## 2. Doce proyecciones de persistencia propuestas (NO creadas)

| Proyección conceptual futura | Llave e índice candidato (por validar en EF/MySQL efímero) | Restricción y reversibilidad |
| --- | --- | --- |
| PlanMigracion | PlanId PK; (Ambiente, Estado, Version) | Sin aprobación explícita no pasa a ejecutable |
| FuenteVersionada | SourceId PK; (PlanId, VersionEsquema) UNIQUE | Guarda metadata saneada, jamás secretos de conexión |
| DestinoDeclarado | TargetId PK; (PlanId, Tipo, VersionEsperada) | Hostinger futuro no se infiere provisionado |
| TablaAlcance | TablaId PK; (PlanId, Esquema, Nombre) UNIQUE | Nombres fuente verificados cuando exista autorización |
| ParticionEmpresa | (TablaId, EmpresaId) UNIQUE; clasificación | EmpresaId del servidor/owner; NULL ambiguo queda en cuarentena |
| MigracionVersionEsperada | (PlanId, NombreMigracion) UNIQUE | Control comparativo, no ejecutor automático de EF |
| ManifiestoArtefacto | Id PK; (PlanId, FechaUtc, Sha256) | Hash real y tamaños medidos en futuro snapshot |
| EnsayoRestore | Id PK; (PlanId, ManifestId, AmbienteEfimero, Estado) | Restricción contra destinos productivos o compartidos |
| ConteoComparado | (EnsayoId, TablaId, EmpresaId) UNIQUE | Conteos origen/destino, diferencias explicadas |
| HashComparado | (EnsayoId, TablaId, CampoCanon, Version) UNIQUE | Canonicalización reproducible, NULL/locale/orden documentados |
| VentanaCongelacion | (PlanId, StartUtc, StopUtc, Version) | Owner humano, freeze, punto de retorno y delta |
| EvidenciaReversion | Id PK; (PlanId, Intento, CreatedUtc) | Append-only, sin cambiar BITACORA histórica ni datos reales |

Estas proyecciones son nombres de trabajo para documentación, no tablas, entidades C#, migrations o DTO ya existentes. Los índices propuestos requerirán medición EXPLAIN, análisis de cardinalidad, FK compuestas y compatibilidad con esquema real. No crear ni alterar índices por este contrato.

## 3. Catorce invariantes y precondiciones de migración futura

- DB01: PLAN y permisos comerciales/técnicos se verifican en el HEAD y origen/destino autorizados antes de cualquier conexión.
- DB02: ninguna tabla o segmento se asigna a empresa por query, header, DNS, usuario de navegador o inferencia del Id local.
- DB03: FK y unicidad tenant-aware deben preservar aislamiento; referencias cross-tenant se rechazan, no se reparan con un owner artificial.
- DB04: filas sin empresa inequívoca permanecen bloqueadas y requieren conciliación autorizada antes de mover datos.
- DB05: el origen y destino registrarán esquema, versión EF, provider, collation, charset, timezone y semántica de NULL antes de comparación.
- DB06: snapshot consistente necesita hash calculado sobre bytes reales, alcance, versión, UTC y cadena de custodia.
- DB07: copia cifrada con retención y privilegio mínimos no puede almacenarse en repositorio, logs, Tasks o carpetas públicas.
- DB08: solo un migrador autenticado aplica DDL con lock/lease efectivo; dos ejecutores simultáneos deben ser rechazados.
- DB09: migración expand/contract mantiene compatibilidad con API y cliente anterior hasta finalización de rollback posible.
- DB10: confirmación de restore requiere DB EFIMERA autorizada, FK/índices/identidades intactos y conteos/hashes por tenant A/B.
- DB11: cambios en facturas, pagos, inventario y outbox respetarán transacción e idempotencia; no duplicación por retry o reconexión.
- DB12: permisos JWT/RBAC, revocación y Verified* se prueban de nuevo tras restore, incluyendo procesos sin HttpContext.
- DB13: ventana de cutover y vuelta atrás requieren owner, freeze, reconciliación del delta y mediciones RPO/RTO, no supuestos.
- DB14: gates Scope Lock, CI, A0–A5 y DAG jamás autorizan por sí solos Hostinger, MySQL live, QA/main/PROD o secretos nuevos.

## 4. Secuencia expand/contract y reversión futura — NO ejecutada

1. Registrar versión fuente, esquema EF real, tipos, índices y FK; no leer datos reales antes de autorización explícita.
2. Preparar migración **aditiva** compatible y revertible en proyecto adecuado, solo durante microtarea técnica futura.
3. Ejecutar MySQL 8.4 **efímero** en CI dirigida con datasets sintéticos A/B, operaciones críticas y FK.
4. Comparar conteos por empresa, hashes semánticos, secuencias y constraints; una diferencia no explicada es NO_GO.
5. Probar dos migradores concurrentes, fallo medio proceso, revocación y restauración aislada en CI, con logs saneados.
6. Documentar rollback completo y reconciliación de escrituras/delta con aprobación del owner antes de cualquier cutover.

El flujo anterior describe **acciones futuras no autorizadas**; no ejecuta EF, mysqldump, backup, migrador, DDL, conexión, DNS, Cloudflare, VPS, migración Hostinger ni rollback real.

## 5. Diez fixtures positivos/negativos FUTUROS_NO_EJECUTADOS

| Par | Positivo requerido en próxima fase autorizada | Negativo obligatorio |
| --- | --- | --- |
| P01/N01 | Versión MySQL/EF compatible con esquema de ensayo | Provider, collation o versión desconocidos |
| P02/N02 | Manifiesto cifrado verifica SHA y alcance exacto | Copia dañada o sin hash firma |
| P03/N03 | Tenant A conserva filas A con FK válidas | A o soporte no autorizado lee filas B |
| P04/N04 | Dos tenants idénticos en Id local permanecen separados | PK/FK sin EmpresaId mezcla pertenencias |
| P05/N05 | Migrador único opera con exclusividad probada | Dos procesos aplican DDL en paralelo |
| P06/N06 | Restore efímero conserva conteos/hash por tenant | Diferencia silenciosa declarada éxito |
| P07/N07 | JWT/RBAC vigente deniega sin permiso después del ensayo | Token revocado vuelve a tener privilegio |
| P08/N08 | Factura, pago e inventario no se duplican por retry | Corte provoca doble movimiento |
| P09/N09 | Health/readiness refleja DB de prueba disponible | Conexión caída retorna ready/200 |
| P10/N10 | Reversión reconcilia escrituras sintéticas y auditoría | Rollback pierde delta o altera PROD |

**Todos los fixtures están DISEÑADOS pero NO EJECUTADOS.** No hay SQL, datos reales, pruebas xUnit nuevas, servicio efímero ni metadatos restaurados en esta unidad. El futuro gate M03/M05 exigirá CI exact-SHA con MySQL 8.4 efímero, autorización comercial/tenancy, SHA manifest y rollback ensayado.

## 6. Capas impactadas, A0–A5 y límites

**Impacto actual:** sólo el Markdown de esta unidad y sección autocontenida de CHANGELOG_AI.md. El futuro backend Domain/Application conservará reglas de propiedad, EF Infrastructure el versionado y consultas seguras, API el control JWT/RBAC/Verified*, y frontend la representación de estado sin gestionar secretos o conexiones. Los usuarios comerciales no son DBAs y un operador global requiere control explícito de plataforma.

**A0:** Skill/Scope Lock/HEAD y gates M01 LISTO+A5, PLAN/COLA/DAG, ownership y dos Markdown. **A1:** doce proyecciones, catorce reglas, seis pasos y diez pares P/N como oráculos FUTUROS. **A2:** revisar rechazo cross-tenant, acceso global indebido, secretos, irreversible DDL y diferencia no aprobada. **A3:** cotejar con AppDbContext Verified*, versiones EF/MySQL, proyecto separado de migraciones y los docs M01 sin conectar a MySQL; integración, compilación .NET y tests MySQL = NO_APLICA al diff solo Markdown y nunca PASS inventado. **A4:** publicación fast-forward/CAS contra HEAD actual; verificar diff únicamente docs/CHANGELOG, Scope Lock y procedencia V5 completed/success exact SHA; leer PLAN/COLA/DAG/BITACORA por transición. **A5:** review independiente por otra Task Javier distinta de la autora, sin autoaprobación.

**Rollback documental real:** git revert normal de commit específico, conservando historia y cambios concurrentes, sin force-push/reset. No se crea infraestructura, modifica IAM/roles, toca Aiven live/Hostinger, mueve PII, compra servicios ni cambia QA/main/PROD.
