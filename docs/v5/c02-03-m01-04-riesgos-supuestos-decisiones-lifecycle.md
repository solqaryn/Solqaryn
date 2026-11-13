# SOLQARYN V5 FINAL — C02-03.M01.04: riesgos, supuestos y decisiones del ciclo de vida tenant

**Ámbito exclusivo:** preparación DOCUMENTAL M01.04 para **C02-03 Aprovisionamiento y ciclo de vida**. Resultado futuro del bloque: alta, suspensión, reactivación, archivado y baja lógica idempotentes, con pruebas de repetición, fallo parcial y recuperación. **Esta unidad no implementa, migra, activa, desactiva, elimina ni restaura ninguna empresa, usuario, base, credencial o integración real.** Autor: PRIMARY DEV 03 — JAVIER, Task `6adc2e4bd8a48191ac7d8641ed95a1a3`; proyecto único `solqaryn/Solqaryn@dev`; Scope Lock STRICT.

Fuentes: Standard V5 FINAL Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA` y Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU` (PLAN_MAESTRO, COLA, DAG_TECNICO, BITACORA A:Z). Skill y arquitectura canónicas. Un diseño no es autorización para datos productivos, QA/main/PROD ni despliegues.

## 1. Registro preliminar de riesgos con controles falsables futuros

| ID | Severidad | Riesgo/escenario negativo | Mitigación futura y prueba causal requerida |
| --- | --- | --- | --- |
| R01 | CRÍTICA | Admin de empresa A suspende/reactiva/baja empresa B cambiando ID, slug o host | RBAC de plataforma separado, membresía verificada server-side; caso A→B denegado antes de persistencia |
| R02 | CRÍTICA | Comando de borrado físico dispara DROP o DELETE en tablas financieras por confundir baja lógica | Operaciones de borrado físico fuera de alcance, retención explícita, pruebas DB que no pierden filas ni FK |
| R03 | CRÍTICA | JWT antiguo de usuario suspendido continúa escribiendo durante transición | Revalidar estado de empresa y membresía en backend por operación, invalidación de sesiones; test 401/403 real |
| R04 | CRÍTICA | Worker utiliza `EmpresaId` del mensaje del cliente y modifica otro tenant | Contexto de tenant firmado/verificado, resource ownership y claims no confiables; test A/B cross-tenant |
| R05 | ALTA | Reintento de alta o reactivación duplica empresa, roles o recursos | Idempotency key por operación/tenant/payload y transacción; test mismo ID dos veces con un resultado |
| R06 | ALTA | Misma clave de idempotencia con payload alterado reescribe estado anterior | Hash de intención inmutable y conflicto explícito, test payload distinto con mismo key |
| R07 | ALTA | Suspensión y reactivación simultáneas producen estado incoherente | CAS/versión monotónica, fencing y auditoría de orden; prueba de carreras repetida |
| R08 | ALTA | Archivado deja endpoints/colas/cache con acceso privado habilitado | Matriz de superficies por estado, invalidación de caché, denegaciones API/worker; prueba REST + worker |
| R09 | ALTA | Falla parcial al crear tenant deja credenciales, buckets u objetos huérfanos | Saga/checkpoint y compensación explícita, inventario de efectos, alertas y reanudación; test fallo parcial |
| R10 | ALTA | Se reactiva empresa sin revisar membresías revocadas o credenciales comprometidas | Revalidación identidad, grants, secret rotation y estados; test revocación previa |
| R11 | ALTA | Log o evento de lifecycle expone JWT, DSN, correo, password o PII | Redacción de payloads, IDs mínimos, auditoría estructurada; test negativo sobre logs y DTO |
| R12 | ALTA | Transición de tenant afecta respaldos/retención de otra empresa o invalida rollback | Backups aislados por empresa, checksums y restauración descartable; prueba A/B, conteos y FK |

Los controles de R01–R12 son **exigencias de diseño para etapas ejecutables**, no pruebas ya ejecutadas. En particular, el estado empresarial actual no acredita que exista una máquina universal de lifecycle o una saga productiva.

## 2. Supuestos pendientes (no afirmar como hechos)

- **S01 — Autoridad de plataforma:** existirá un contrato de grants explícito que distinga operación global de administración empresarial; falta validación del contrato final.
- **S02 — Idempotencia persistida:** habrá un registro duradero de comandos y resultados con unicidad por tenant/acción/versión, no sólo control en memoria; requiere migración/test.
- **S03 — Retención legal/contable:** se definirán periodos por jurisdicción y datos sensibles antes de permitir archivado o baja lógica, y sin borrado físico implícito.
- **S04 — Recuperación de fallos:** cada paso con efecto irreversible tendrá compensación o bloqueo seguro y prueba reproducible; no asumir rollback automático del proveedor.
- **S05 — Consistencia de consumidores:** API, workers, jobs, caches, adjuntos y conexiones consultarán estado vigente y no conservarán autoridad tenant caducada; requiere mapa de consumidores.
- **S06 — Capacidad operacional:** la futura plataforma soportará monitoreo, incidentes, alertas, backup y acceso de revisión independiente antes de habilitar cambios reales; hoy no se certifica esa capacidad.

## 3. Decisiones pendientes y criterios de resolución

- **D01:** máquina de estados permitidos y prohibidos, incluidas transición inicial, rechazo, cancelación y recuperación. Autoridad: backend plataforma, no UI.
- **D02:** esquema de idempotencia, semántica de replay, control de concurrencia CAS/fencing y correlación; documentar respuesta ante payload distinto.
- **D03:** política de retención, archivos/documentos/inventario/contabilidad, reversión de baja lógica y eliminación física separada.
- **D04:** autoridad JWT/RBAC/membresía por estado, invalidación de sesiones y tiempos efectivos de suspensión/reactivación.
- **D05:** ciclo de saga/workers: checkpoints transaccionales, reintentos acotados, compensaciones, dead-letter, alarmas y datos de auditoría sin PII.
- **D06:** criterios de implementación: qué componentes requieren CI dotnet/net10, MySQL 8.4/EF, tests negativos multiempresa y simulación de fallos reales.
- **D07:** responsabilidades sobre secretos/credenciales y acceso a integraciones durante suspensión, archivo y restauración, sin selección desde cliente.
- **D08:** gate de liberación de M01.06/M02: dependencias y A5 causal reales, sin interpretar esta documentación como permiso de ejecutar.

**Las ocho decisiones permanecen abiertas:** no cambiar roles, proveedores, productos de pago ni crear infraestructura para resolverlas dentro de M01.04.

## 4. Fuentes de código inspeccionadas y límites

| Archivo del HEAD dev | Blob SHA observado | Limitación |
| --- | --- | --- |
| `backend/src/Domain/Entities/Empresa.cs` | `c44940df7c30983a4c23b93338e0cb9367962069` | Entidad/estado actual; no workflow universal de alta-baja |
| `backend/src/Domain/Entities/UsuarioEmpresa.cs` | `f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c` | Membresía y rol tenant; no basta para autorizar operator global |
| `backend/src/Domain/Security/ContextoTenantActual.cs` | `7377d2d752985cc9baf31d08280f38ad94a6a114` | Contexto tenant seguro desde membresía, no aplica transiciones por sí mismo |
| `backend/src/Application/Interfaces/StorageTenantContext.cs` | `716e8e205502d6300f555947c79edc2852fd63ba` | Scope de recursos tenant, no autoriza borrar object storage |
| `backend/src/Infrastructure/Services/UsuarioScopeService.cs` | `14f4acbc5e1dc6a3ed1cf3485d54c7f1121b0e6f` | Verifica pertenencia en MySQL, no certifica revocación universal post-suspensión |

Estos blobs demuestran fuentes leídas, no ejecución de pruebas o de transiciones reales.

## 5. Pruebas documentales dirigidas de esta M01.04

- **P01 (positivo documental):** cada riesgo R01–R12 contiene severidad, escenario de daño concreto y control con prueba futura.
- **P02 (positivo documental):** seis supuestos S01–S06 y ocho decisiones D01–D08 están expresamente pendientes, sin presentarlas como capacidad implementada.
- **N01 (negativo):** ninguna entrada cliente/host/JWT/EmpresaId/DSN se eleva a autoridad para cambios de estado o destino.
- **N02 (negativo):** no se afirma que exista provisioning/archivado/suspensión automatizados, restauración live o evidencia de MySQL real.
- **N03 (negativo):** no se da PASS dotnet/mysql/angular/docker/e2e mediante este diff, ni se equipara SKIPPED a SUCCESS.
- **N04 (negativo):** no se habilita M01.06/M02 ni QA/main/PROD, ni se cambia trabajo de otro scope.
- **N05 (negativo):** no se documenta borrado físico como opción por defecto; retención y reversión quedan pendientes.

## 6. DAG y pruebas A0–A5

`DAG_TECNICO!F=VALIDADO` y G=SI para C02-03.M01.04; 19 predecesores documentales: C00-01.M02.01–06 (6), C00-03.M01.01–05 (5), C01-01.M01.01–05 (5), C02-02.M01.01–03 (3). Revalidar cada estado PLAN_MAESTRO H/I con evidencia y elegibilidad COLA G/O antes del claim. Esta excepción técnica permite únicamente M01.01–05; jamás libera fase ejecutable, gate C00 ni A5 pendiente.

**Tipo 1 sólo documental:** `requiere_dotnet=NO`, `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO`; `requiere_security=SI_DOCUMENTAL` para JWT/RBAC/tenant A/B, PII y retención; `requiere_scope_lock=SI` en commit. **A0** integridad de diff, secretos, dependencias, decisiones sin evidencia ficticia; **A1** P01/P02/N01–N05 y matriz R/S/D; **A2** fronteras tenant, permisos, revocación, idempotencia y datos; **A3** cinco blobs y DAG19, V5, PLAN/COLA/BITACORA A:Z; **A4** commit FF dev SHA exacto, Scope Lock completed/success, readback y rollback; **A5** otro TASK_ID revisa independiente, sin autocertificación.

**No-alcance:** cero modificaciones de .NET, Angular, MySQL/EF/migraciones, API, workers, auditores, cachés, Cloudinary, repositorios, datos vivos, proveedores, secretos, GitHub CI, QA, main, PROD, RRULE, Tasks o ramas/PR. Inventario de imágenes históricas cero: migración no aplica. La futura implementación requiere autorización de lo que exceda dev.

**Rollback documental:** `git revert <SHA_MICROTAREA>` normal, aditivo y con revalidación de HEAD; sin reset/force-push. Reexaminar R/S/D cuando se cierre C00/C01/C02-02, cuando se implemente lifecycle y antes de cualquier promoción.
