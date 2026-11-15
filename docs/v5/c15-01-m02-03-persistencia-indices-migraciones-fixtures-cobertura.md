# SOLQARYN V5 FINAL — C15-01.M02.03: persistencia, indices, migraciones y fixtures de auditoria de cobertura

**Autor:** PRIMARY DEV 06, JAVIER_CHATGPT; TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Repositorio:** `solqaryn/Solqaryn` rama `dev`; `PROJECT_SCOPE_LOCK=STRICT`. Esta es una especificacion **DOCUMENTAL** de persistencia posible para C15-01. NO modifica esquema MySQL, DbContext, seeds, migraciones, datos, CI, eventos ni ambientes.

## 1. Autoridad, dependencias y hallazgos abiertos

Fuentes: [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit); [PLAN/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit); [M01.06](c15-01-m01-06-bloqueos-rollback-liberacion-cobertura.md), [M02.01](c15-01-m02-01-modelo-entidades-ownership-trazabilidad-cobertura.md) y [M02.02](c15-01-m02-02-contratos-api-dto-errores-compatibilidad-cobertura.md). M02.01 define CT01–CT16 de propiedad/trazabilidad; M02.02 define propuestas API/DTO y errores, NO rutas desplegadas.

Preflight individual: PLAN H2170=PENDIENTE e I vacia, COLA G2170/O2170=SI, DAG A2131=C15-01.M02.03, F=VALIDADO/G=SI, Scope Lock exact-SHA completado SUCCESS. Un unico claim con CAS y readback de owner evita reserva especulativa. Los 2340 IDs son tamaño del plan, no certificado de ausencia de huerfanos o duplicados.

## 2. Topologia fisica real y frontera de diseno

El proyecto utiliza ASP.NET Core 10, EF Core 10.0.12, proveedor `MySql.EntityFrameworkCore` 10.0.9 y MySQL 8.4. Archivos verificados en Git: `backend/src/Infrastructure/Persistence/AppDbContext.cs` contiene DbSets comerciales y `AuthorizedReadEmpresaId` derivado de marcas de tenant verificadas por backend; `TenantQueryBypassGuard.cs` aplica empresa autorizada a consultas con filtros omitidos; `CanonicalMySqlPhysicalContract.cs` verifica constraints/triggers del esquema comercial. Ninguno representa hoy una base de datos independiente para la auditoria institucional V5. NO reutilizar los DbSets de ventas o permisos comerciales para almacenar tokens o BITACORA de Tasks.

El almacenamiento conceptual CT01–CT16 es una **proyeccion analitica futura** con fuente original en STANDARD, PLAN/COLA/DAG, BITACORA, GitHub y Automations. Si alguna futura autorizacion exige materializarlo, elegir diseño aditivo, control de revision, almacenamiento minimizado y permisos separados, con nueva migracion EF y CI MySQL efimera, no ahora.

## 3. Contratos de registros logicos, sin CREATE TABLE

| ID | Registro logico | Clave propuesta | Integridad futura | Negativo |
| --- | --- | --- | --- | --- |
| PD01 | RequirementSource | source_revision + source_key | Revisiones inmutables | No duplicar contenido sensible |
| PD02 | RequirementOccurrence | source_revision + location_hash | Hash de ubicacion normalizada | No guardar tokens ni correos de usuarios |
| PD03 | PlanMicrotask | microtask_id exacto | Unicidad de 2340 IDs | No confiar en numero de fila |
| PD04 | CoverageEdge | source_revision + requirement_key + microtask_id | Join exacto N:M | Evidencia documental obligatoria |
| PD05 | TechnicalDependency | predecessor_id + successor_id | DAG sin ciclos | Gate G=SI calculado, no editable |
| PD06 | OwnershipInterval | microtask_id + owner_task + start_utc | Cero o uno activo | No afirmar CAS entre servicios sin soporte |
| PD07 | AuditEvent | event_id + idempotency_key | Append-only de 26 campos | Evitar PII o secretos |
| PD08 | GitEvidence | microtask_id + SHA + blob | Arbol Git y version exacta | No reescribir historia |
| PD09 | CheckRun | head_sha + check_name + attempt | Conclusion y status separados | SKIPPED y CANCELLED no son PASS |
| PD10 | IndependentReview | microtask_id + author_task + reviewer_task | Autor diferente al revisor | Sello A5 requiere diff, blob, seguridad |
| PD11 | Finding | finding_id + source_cut + severity | Persistir excepciones P0/P1 | No borrar hallazgos por nuevo corte |
| PD12 | Retest | finding_id + retest_sha + evidence | Cierre causal auditado | Sin retest no declarar cero errores |

Relacion: PD01 1:N PD02; PD02 N:M PD03 mediante PD04; PD03 N:N PD03 mediante PD05; PD03 0:1 PD06 activo; PD03 1:N PD07; PD08 1:N PD09 con SHA exacto; PD10 refiere PD08; PD11 1:N PD12. Son restricciones conceptuales, no DDL ejecutado ni integridad transaccional garantizada entre Google y GitHub.

## 4. Indices e inspecciones condicionales, no creados

| ID | Clave hipotetica | Uso | Condicion negativa |
| --- | --- | --- |
| IX01 | microtask_id UNIQUE | Evitar dos IDs identicos | Dos filas con mismo ID deben reportar colision |
| IX02 | source_revision, requirement_key | Revisar cobertura por fuente/version | No confundir revision nueva con antigua |
| IX03 | requirement_key, microtask_id | Buscar aristas CoverageEdge | No construir arista por similitud |
| IX04 | predecessor_id, successor_id | Detectar ciclos y bloqueo | Nunca cambiar gates para que pasen |
| IX05 | event_id UNIQUE / idempotency_key UNIQUE | Detener duplicacion de eventos | No asumir atomicidad API Sheets |
| IX06 | finding_id, severity, status | Encontrar hallazgos P0/P1 pendientes | No ocultar errores por filtro |

IX01–IX06 son criterios prospectivos. Antes de cualquier migracion se deben demostrar cardinalidad, costo, retencion, collation, consultas reales y ausencia de nuevos cruces tenant. Para datos existentes se usaran consultas de solo lectura y protecciones actuales, nunca un INSERT automatico de auditoria en datos de negocio ni un dump sin minimizacion.

## 5. Plan de migracion futuro condicionado

1. `MIG01`: obtener autorizacion nueva, snapshot consistente del STANDARD y Sheet, inventario de permisos y aprobacion de tenancy con archivos/DB de destino; sin credenciales en Git.
2. `MIG02`: proponer migracion EF aditiva versionada en proyecto correcto y entorno temporal aislado, sin alterar esquema de empresa del proveedor.
3. `MIG03`: ejecutar pruebas con MySQL efimero real sobre fixtures sintéticos de empresas A/B y CI exact-SHA; validar indices, constraints, FK y negativas de acceso.
4. `MIG04`: comparar hashes y cantidad de enlaces requisito-ID por revision, rechazar huerfanos/duplicados criticos y diferencias de estado/owner/A5 no demostradas.
5. `MIG05`: demostrar rollback de codigo y datos sin borrar historial comercial, preservar BITACORA append-only y registrar todos los hallazgos.
6. `MIG06`: eventual liberacion por gate A0–A5 y autorizacion distinta para QA/main/PROD. Este documento no satisface ninguna MIG por si solo.

## 6. Fixtures sintéticos positivos y negativos FUTUROS_NO_EJECUTADOS

| ID | Positivo posterior | Negativo posterior |
| --- | --- | --- |
| FX01 | 2340 IDs distintos coinciden por microtask_id | Se detecta duplicado aunque filas contiguas aparenten bien |
| FX02 | Requisito con enlace verificable se marca trazable | Requisito sin CoverageEdge crea hallazgo huérfano |
| FX03 | Dos ocurrencias textuales no son automáticamente duplicado crítico | Contratos contradictorios con igual key sí crean hallazgo |
| FX04 | Una Task obtiene CAS exclusivo | Dos escritores sobre mismo ID => conflicto no certificado |
| FX05 | A5 de Task distinta con SHA exacto se acredita | A5 de propio autor o cuenta retirada no valida |
| FX06 | SUCCESS exact SHA se guarda como aprobado | SKIPPED/CANCELLED/FAILED/in_progress no cuenta PASS |
| FX07 | GATE DAG recalcula según PLAN H/I/K | VALIDANDO no desbloquea M02.06 terminal |
| FX08 | Consulta usa tenant verificado del backend | Parametro EmpresaId ajeno no concede lectura |
| FX09 | Redaccion de datos personales es estable | Ningun token/PII se exporta en fixtures |
| FX10 | Migracion prospectiva seria aditiva y reversible | DDL destructivo en DB productiva es NO_GO |
| FX11 | Timestamps UTC permiten conciliacion de cortes | Nativo y self-report se confunden |
| FX12 | Un hallazgo cerrado exige SHA y retest reproducible | Dejar P0/P1 cero por falta de ejecucion falla |

**FX01–FX12 no fueron ejecutados.** No se declaran certificados cero requisitos huerfanos, cero duplicados criticos, cero P0/P1, concurrencia libre de races ni integridad de esquema runtime. Los identificadores y ejemplos son especificacion de futuros tests con CI MySQL/EF cuando exista cambio real.

## 7. Impacto, auditorias, rollback

- **A0:** comprobar plan V5, predecesor M01.06, M02.01/02, 12 registros PD, 6 indices IX y 12 casos FX; revisar exclusivamente este documento y CHANGELOG.
- **A1:** coherencia de casos positivos y negativos documental; no afirmar pruebas SQL. **A2:** contratos de tenant A/B, JWT/RBAC, redaccion de PII; sin diff de runtime, pruebas HTTP NO_APLICA. **A3:** contraste de AppDbContext, TenantQueryBypassGuard y CanonicalMySqlPhysicalContract reales; migraciones MySQL NO_APLICA al Markdown, no PASS ficticio.
- **A4:** publicar por fast-forward con HEAD CAS, Scope Lock y control de procedencia V5 completados success en commit exacto; BITACORA y PLAN/COLA/DAG readbacks. **A5:** revisor Javier independiente de DEV06, pendiente hasta auditoria formal, sin autovalidacion.
- **Rollback:** revert de commit documentario por nuevo commit Git en dev; no reset, force-push ni edicion de BITACORA historica. Cualquier migracion efectiva futura exige rollback de datos autorizado distinto, no aplica hoy. Ningun efecto en QA/main/PROD ni servicios de pago.
