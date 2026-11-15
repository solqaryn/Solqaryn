# SOLQARYN V5 — C11-03.M02.03: persistencia, índices y fixtures conceptuales de outbox

**Alcance:** `C11-03.M02.03`, fase M02 de C11-03 Outbox y eventos. **Autor:** PRIMARY DEV 05 de JAVIER, `TASK_ID=6adc2e55f47c81919f6374e561687076`. **Rama y control:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT. Autoridad: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, PLAN_MAESTRO/COLA/DAG_TECNICO, A0–A5.

## 1. Estado real y decisiones de alcance

Esta unidad es **contrato documental** de persistencia y escenarios, sin modificar `AppDbContext`, entidades EF, índices SQL, migraciones, scripts, credenciales, trabajos programados ni servidores. El modelo físico continúa siendo el código real del repositorio y su propia historia de migraciones. Los nombres y variantes futuros de las tablas aquí descritos son **propuestas, no DDL autorizado**.

Fuentes Git comprobables:
- `backend/src/Domain/Entities/MensajeOutbox.cs`, blob `891572626c19cd6cf8f522fca5ec876333286ffb`: `EmpresaId`, `EventoId`, `ClaveIdempotencia`, tipo, payload, agregado, correlación, estado, intentos, fechas UTC y transiciones.
- `backend/src/Infrastructure/Repositories/MensajeOutboxRepository.cs`, blob `056d5aa380b53ed697ddcb6852df55394e6544a7`: consultas `EmpresaId`-scoped, `ExecuteUpdateAsync` condicional para claim, recuperación stale y confirmaciones. `AddAsync` solo añade al contexto: el caller debe confirmar la misma unidad de trabajo de negocio.
- `backend/src/Application/Interfaces/IMensajeOutboxRepository.cs`, blob `a7dc258df9b1123dfe54ea85ba6d4dcc2a1eb478`: interfaz de idempotencia, claim y estado.
- `backend/src/Application/Services/MensajeOutboxService.cs`, blob `a00d353c474d7d7f785a58f443c66c5f12a542f7`: rechazo cross-tenant y conflicto cuando igual clave designa otra solicitud.
- `backend/src/Application/Services/OutboxRetryProcessor.cs`, blob `542c0a655a3ee0698e3d80f1854d43903b6470fa`: lote completo validado antes de efectos, retry finito, auditoría redacted; **no** scheduler propio.
- `backend/tests/Solqaryn.Tests/N72DOutboxMySqlConcurrencyTests.cs`, blob `ebf6fd4861c55145dc40021152bf1c73ff4edce4`: fuente `[Trait(Category, Integration)]` con dos `AppDbContext` concurrentes; la presencia del test **no** demuestra que se haya ejecutado en esta microtarea.
- `docs/v5/c11-03-m01-06-gates-rollback-outbox-eventos.md`, blob `fb068b1687e4c1d33fe2989ca9678ae3fb42e297`: 14 barreras y 10 contenciones verificadas con A5 independiente.
- `docs/v5/c11-03-m02-01-entidades-ownership-invariantes-outbox.md`: contrato previo M02.01 LISTO_DEV de esta fase; A5 independiente sigue pendiente, sin que ello constituya autorización de M03.

## 2. Doce proyecciones de persistencia P01–P12

Son superficies conceptuales para inspección y eventual mapeo a entidades existentes; no se proponen doce nuevas tablas automáticamente.

| Ref. | Proyección lógica / claves | Condición de ownership e integridad |
| --- | --- | --- |
| P01 | Mensaje outbox: `(EmpresaId, Id, EventoId)` | `EmpresaId` requerido; ID externo no autoriza lectura de otro tenant |
| P02 | Unicidad idempotente: `(EmpresaId, ClaveIdempotencia)` | Valor normalizado; mismo key y solicitud distinta conflictivo |
| P03 | Disponibilidad: `(EmpresaId, Estado, DisponibleDesdeUtc, Id)` | Solo `Pendiente/Fallido` y ventana vencida son elegibles |
| P04 | Claim activo: `(EmpresaId, Id, Intentos, ProcesandoDesdeUtc)` | Actualización condicional de una fila, intento esperado |
| P05 | Recuperación stale: `(EmpresaId, Estado, ProcesandoDesdeUtc)` | Corte UTC y CAS sin arrebatar ejecución activa |
| P06 | Resultado durable: `(EmpresaId, Id, Estado, EntregadoEnUtc)` | `Entregado` solo tras acuse del intento vigente |
| P07 | Fallo y retry: `(EmpresaId, Id, Intentos, DisponibleDesdeUtc)` | Backoff y reintentos limitados, sin retry infinito |
| P08 | Dead-letter: `(EmpresaId, Id, Estado, UltimoError)` | Histórico no se elimina; errores públicos saneados |
| P09 | Agregado causal: `(EmpresaId, TipoAgregado, IdAgregado)` | Referencia opcional no sustituye FK/ownership verificados |
| P10 | Correlación: `(EmpresaId, CorrelationId)` | Solo trazabilidad y búsqueda tenant-bound, no permiso |
| P11 | Versión/orden por agregado (futuro) | Requiere contador/versionado probado antes de añadir almacenamiento |
| P12 | Destino y autorización externa (futuro) | No se crea tabla de destino, conector ni bus en esta entrega |

Las proyecciones P01–P10 derivan de propiedades y filtros existentes; P11/P12 están marcadas **FUTURO**, sin columna o índice SQL creado.

## 3. Ocho índices candidatos IX01–IX08, NO EJECUTADOS

| Índice | Columnas/criterio conceptual | Justificación y riesgos que habrá que medir |
| --- | --- | --- |
| IX01 | `UNIQUE(EmpresaId, ClaveIdempotencia)` | Evita duplicación concurrente por tenant; cotejar índice actual antes de proponer DDL |
| IX02 | `UNIQUE(EmpresaId, EventoId)` | Identidad durable sin colisión tenant; no duplicar unicidad ya cubierta |
| IX03 | `(EmpresaId, Estado, DisponibleDesdeUtc, Id)` | Búsqueda ordenada de pendientes, con EXPLAIN medido |
| IX04 | `(EmpresaId, Estado, ProcesandoDesdeUtc, Id)` | Localización de stale, sin transformar antigüedad en permiso |
| IX05 | `(EmpresaId, Id, Intentos, Estado)` | Predicado de confirmación CAS, validar selectividad real |
| IX06 | `(EmpresaId, TipoAgregado, IdAgregado, CreadoEnUtc)` | Consulta por agregado, solo si requisito de orden lo justifica |
| IX07 | `(EmpresaId, CorrelationId, CreadoEnUtc)` | Auditoría correlacionada acotada, sin PII |
| IX08 | `(EmpresaId, Estado, UltimoIntentoEnUtc)` | Supervisión finita de fallos y dead-letter sin escanear todo |

Estos ocho candidatos no afirman inexistencia de índices actuales. Primero inspeccionar mapping EF y MySQL real **en CI efímera autorizada** y evitar duplicados, scans costosos y claves fuera del prefijo tenant. No ejecutar `CREATE INDEX` en esta fase.

## 4. Seis etapas condicionales de migración M01–M06 — FUTURAS_NO_EJECUTADAS

| Fase | Acción futura únicamente tras autorización y CI causal | Evidencia / rollback exigible |
| --- | --- | --- |
| M01 | Inventariar EF mapping y migraciones ya aplicadas por entorno | Diff y checksum de esquema, sin presumir estado remoto |
| M02 | Diseñar expand aditivo mínimo si un índice/columna falta realmente | Migración versionada con reversión y costo de locks |
| M03 | Ejecutar variante en MySQL 8.4 **efímero** con dataset sintético A/B | Migrador único, FK, EXPLAIN, timeout y concurrencia |
| M04 | Validar idempotencia y dos claims simultáneos del mismo tenant | Un ganador, estado consistente, rollback por fallo |
| M05 | Verificar carga acotada, índices, plan de consulta y estados DeadLetter | Métricas medidas, no objetivos inventados |
| M06 | Ensayar down/restore efímero y decidir por gate independiente | Readback de hashes, conteos, migraciones y rollback |

Estas etapas no son una orden de migrar datos productivos, ni Aiven/Hostinger, ni desplegar endpoints o trabajadores. **Ninguna se ejecutó.**

## 5. Doce fixtures sintéticos FX01–FX12, no creados en MySQL

| Fixture | Input positivo esperado | Fallo/negativo que debe rechazarse |
| --- | --- | --- |
| FX01 | Empresa A con mensaje Pendiente y UTC correcto | EmpresaId nulo/cero o fecha local |
| FX02 | Empresa B con mensaje propio | Claim de B desde contexto de A |
| FX03 | Idempotency key A repetida con payload idéntico | Mismo key A con payload diferente |
| FX04 | Igual key textual en A y B, sin interferencia | Lookup global no filtrado por EmpresaId |
| FX05 | Dos procesos compiten por un mensaje A | Ambos reciben el mismo claim |
| FX06 | Claim A expirado se recupera condicionalmente | Ack del antiguo intento tras rescate |
| FX07 | Lote A de dos mensajes únicos | Última fila contaminada por B |
| FX08 | Procesamiento falla y reprograma con backoff finito | Próximo intento anterior al ahora UTC |
| FX09 | Fallos hasta límite generan DeadLetter | Reactivación automática tras límite |
| FX10 | CorrelationId válida solo para trazabilidad | Usarla como capacidad de lectura transversal |
| FX11 | Payload confidencial en intento, auditada solo metadata | JWT/PII/token aparece en logs |
| FX12 | DDL aditivo/deshacer en DB efímera con hash final | FK/índice corrupto se certifica exitoso |

Cada fixture es una **especificación** de test futuro, no un test ejecutado ni un registro insertado. Los positivos/negativos abarcan tenant A/B, concurrencia CAS, duplicados, clock UTC, privacidad, reversión e integridad.

## 6. Gates, impacto y rollback

**A0 estática:** PLAN H/I, COLA G/O, DAG F/G verificados para un claim exclusivo; diff de dos Markdown, fuentes nombradas con blobs Git y ninguna escritura SQL/EF. **A1 documental:** cotejar 12 proyecciones, ocho índices, seis etapas y 12 fixtures realmente escritos, sin atribuir resultados futuros. **A2 seguridad:** exigir `EmpresaId` en filtros y claves, CAS por intento, JWT/RBAC server-side, PII redacted y denegación A/B; no modificar autenticación. **A3 integración:** coherencia conceptual con `MensajeOutboxRepository`, `IMensajeOutboxRepository`, `MensajeOutboxService`, `OutboxRetryProcessor` y pruebas existentes; las pruebas SQL/.NET/Angular no son causales a un diff exclusivamente Markdown y se clasifican `NO_APLICA`, no PASS. **A4:** fast-forward con CAS `dev`, diffs exactos, CHANGELOG autocontenido, Scope Lock y V5 `completed/success` SHA exacto, BITACORA y PLAN/COLA/DAG readback. **A5:** revisión real posterior por otra de las seis Tasks Javier, diferente del autor.

**Rollback del trabajo actual:** revert normal del changeset documental con readback del HEAD; no hay migración, job ni dato que revertir. Si el futuro alcance M03 implementa SQL real, debe cumplir integración MySQL 8.4 efímera, tenant A/B, fallos y restauración sin afectar producción. Se prohíbe cambiar QA/main/PROD, DNS, secretos, cuentas, proveedores, nuevas Tasks o eventos operativos.
