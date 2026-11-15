# SOLQARYN V5 — C04-02.M02.03: persistencia futura, índices, migraciones y fixtures de contratos SaaS

**Autor:** PRIMARY DEV 06 — JAVIER (6af16bbad26881918c283abe06e1f32a). **Fecha:** 2026-11-15. **Ámbito:** solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT. **Naturaleza:** especificación documental M02.03 de persistencia y reversión. Este archivo no instala tablas, no aplica migraciones ni modifica la base de datos.

## 1. Autoridad y baseline real

Documento único ejecutable: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES, ID 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA. PLAN_MAESTRO fila 622, ID C04-02.M02.03, «Definir persistencia, índices, migraciones y datos de prueba». Gate previo y claim: PLAN H=PENDIENTE, I vacío, COLA G/O=SI y DAG_TECNICO F=VALIDADO/G=SI, luego CAS exclusivo H622=EN_PROGRESO, I622=TASK_ID DEV06. C04-02.M02.01/.02 se publicaron secuencialmente y quedaron LISTO_DEV documentales con A5 independiente pendiente; esta tarea M02.03 es independiente de ellas conforme DAG, no las declara aprobadas por A5.

Código inspeccionado en dev:
- backend/src/Infrastructure/Persistence/Configurations/SuscripcionesSaaSConfiguration.cs, blob 16631a0155a449ab71e7479c92e8ba8804303249. Planes tiene índice único Codigo; PlanLimites clave PlanId+Clave; Suscripciones tiene índices EmpresaId+Estado, EmpresaId+InicioUtc y PlanId+Estado, y claves externas RESTRICT. La entidad existente Suscripcion tiene EmpresaId.
- backend/src/Infrastructure/Repositories/SuscripcionesSaaSRepository.cs, blob f3340ca687867d6dc2d99dfff5452bbe5c9bbb79. Onboarding usa filtro de tenant y una transacción con tabla ledger SuscripcionSaaSIdempotencia y colisiones únicas; esto **no equivale** a ledger de contratos, cobros o renovaciones.
- backend/src/Infrastructure/Migrations/AppDbContextModelSnapshot.N69C.cs, blob 81d843b215fcce36d60ec2e446eb5576f3800b31, overlay EF para suscripción/planes; snapshot leído no demuestra migración aplicada en MySQL.
- backend/src/Domain/Entities/SuscripcionesSaaS.cs, blob d9b5dbe19fb728293b00b748f10ac10336debd2d: estado técnico Activa/Suspendida/Cancelada/Expirada, no contrato jurídico o conciliación.
- M02.01 propone modelo lógico de contratos, y M02.02 propone operaciones API/DTO; no son clases implementadas ni esquemas versionados. Mantener separación entre suscripción técnica existente y contrato económico aún no autorizado.

## 2. Tablas lógicas futuras y ownership tenant (NO creadas)

| Agregado lógico propuesto | Clave de negocio prevista | Relaciones y restricción de aislamiento |
| --- | --- | --- |
| ContratosComercialesSaaS | (EmpresaId, ContratoId) | Empresa obligatoria; SuscripcionId opcional, no implica aceptación ni pago |
| ContratoVersiones | (EmpresaId, ContratoId, NumeroVersion) | FK compuesta al Contrato; versiones aceptadas inmutables |
| ContratoPartes | (EmpresaId, ContratoId, ParteId) | Scope del mismo tenant; representación requiere D01 |
| ContratoTerminos | (EmpresaId, ContratoId, VersionId, TerminoId) | Referencia a texto legal versionado D02 |
| ContratoAceptaciones | (EmpresaId, ContratoId, VersionId, AceptacionId) | Consentimiento y UTC, no alterables después |
| ContratoPeriodos | (EmpresaId, ContratoId, PeriodoId) | UTC, orden y sin superposición indebida |
| ContratoRenovacionesPropuestas | (EmpresaId, ContratoId, PropuestaId) | Sin cargo/efecto antes de D03/D04/D06 |
| ContratoPoliticasReferencias | (EmpresaId, ContratoId, VersionId, PoliticaId) | Un identificador de aprobación real, no un valor supuesto |
| ContratoConciliacionesReferencias | (EmpresaId, ContratoId, ReferenciaId) | Evidencia confiable separada del callback/cargo |
| ContratoCasosMora | (EmpresaId, ContratoId, CasoId) | Sin mora automática antes de D05 y conciliación |
| ContratoTransiciones | (EmpresaId, ContratoId, Secuencia) | Append-only, previa/nueva/corr UTC y actor redactado |
| ContratoIdempotencia | (EmpresaId, Operacion, ClaveNormalizada) | Hash de payload, resultado duradero, UNIQUE por tenant/acción |
| ContratoAuditoria | (EmpresaId, ContratoId, AuditoriaId) | PII mínima, origen de acción y correlación, retención D08 |

**Diseño, no SQL:** la PK simple interna puede utilizar surrogate numérico si EF lo requiere, pero todas las FK de hijos a padres deben demostrar que EmpresaId coincide. Un índice únicamente por ContratoId global no constituye autorización. RESTRICT en referencias de historial; no cascadas destructivas, salvo política explícitamente aprobada después de revisión.

## 3. Diez índices/constraints conceptuales a validar (no DDL ejecutado)

| Código | Constraint o índice candidato | Efecto y prueba futura |
| --- | --- | --- |
| IDX01 | UNIQUE(EmpresaId, ContratoId) | Identidad scoped |
| IDX02 | UNIQUE(EmpresaId, ContratoId, NumeroVersion) | Sin dos versiones número idéntico |
| IDX03 | FK(ContratoVersiones.EmpresaId, ContratoId)→Contrato | Impide referencia cruzada tenant B |
| IDX04 | UNIQUE(EmpresaId, ContratoId, IdentificadorAceptacion) | Reintentos no duplican firmas |
| IDX05 | INDEX(EmpresaId, EstadoContractual, FechaEfectivaUtc) | Listados filtrados por tenant |
| IDX06 | INDEX(EmpresaId, ContratoId, PeriodoInicioUtc) | Lectura temporal eficiente sin acceso global |
| IDX07 | UNIQUE(EmpresaId, Operacion, IdempotencyKey) | Carrera/replay correcto |
| IDX08 | INDEX(EmpresaId, ContratoId, SecuenciaTransicion) | Orden causal de historia append-only |
| IDX09 | INDEX(EmpresaId, ContratoId, EstadoConciliacion) | Acceso restringido a conciliaciones |
| IDX10 | RESTRICT FK entre contrato/version/historial | Preservar evidencias en cancelación y rollback |

Los nombres de tablas/índices son **candidatos**, no el esquema EF vigente. Un UNIQUE de «un solo contrato activo por tenant» puede requerir estrategia de concurrencia distinta según estados y periodos; no suponer índice parcial disponible en Oracle MySQL. El diseño final debe elegir y probar esquema compatible con MySQL 8.4 real en un contenedor efímero, con transaction isolation y conflictos deterministas.

## 4. Plan de migración aditiva futuro M01–M07, no ejecutado

**M01 — preflight:** comprobar proveedor Oracle MySql.EntityFrameworkCore vigente, EF Core 10, versión MySQL 8.4, backup/restauración efímeros y dependencias; sin tocar Aiven real.

**M02 — expansión:** agregar objetos nuevos por migraciones EF versionadas, sin renombrar/borrar Suscripciones, Planes, PlanLimites o ledger de onboarding. Índices y longitudes requieren pruebas reales para evitar locks.

**M03 — semilla opcional:** crear referencias contractuales solo en fixtures ficticios; **no** fabricar aceptación ni convertir suscripciones históricas a contratos legales aceptados.

**M04 — compatibilidad:** lectura del sistema legado existente sigue intacta; servicios nuevos detrás de gates explícitos, no de feature flags configurados por cliente.

**M05 — doble validación:** al implementar, correr Category=Integration **real** con MySQL 8.4 efímero y EF Oracle, comprobar migraciones idempotentes, rollback preparado, interbloqueos, constraints, índices, collations y tenant A/B. Build verde sin Category=Integration ejecutada es insuficiente.

**M06 — transición controlada:** verificar A0–A4 del código, Scope Lock, V5, A5 independiente y autorización económica D01–D08 según operación; no habilitar pagos ni callbacks sin mandato específico.

**M07 — reversibilidad:** git revert para diff de aplicación y plan de reversión de migración ensayado sobre copia efímera sin perder historia, contratos, aprobaciones o referencias. Si el esquema ya recibió datos reales en futuro, preferir rollback lógico/forward-fix aditivo; no DROP automático destructivo.

Ninguna etapa anterior se ejecuta por registrar estas instrucciones.

## 5. Fixtures A/B/C propuestas y doce pares de pruebas futuras

Identidades ficticias: Empresa A=1001, Empresa B=2002, actor A con rol contractual autorizado, actor B sin ámbito A, actor C revocado. Contratos A-101/A-102 y B-201 referencian sus propios padres. Moneda/precio/impuesto/mora son **NO CONFIGURADOS**, no se inventan importes. Versiones V1/V2 y fechas UTC de ejemplo deben ser números sintéticos no ligados a clientes reales.

| Escenario positivo de integración FUTURA | Escenario negativo FUTURO |
| --- | --- |
| P01 Crear borrador A sin cargo | N01 actor B inserta contrato Empresa A |
| P02 Dos versiones monotónicas A | N02 duplicado EmpresaId+ContratoId+Versión |
| P03 FK hijo con EmpresaId de A | N03 referencia hijo A→contrato B |
| P04 Consentimiento aceptado con evidencia real | N04 aceptación sin identidad/autorización |
| P05 Paginación A devuelve solo A | N05 filtro omite EmpresaId y revela B |
| P06 Idempotency-Key replay idéntico | N06 misma clave con payload distinto |
| P07 Dos operaciones concurrentes resuelven un ganador | N07 carrera duplica contrato/transición |
| P08 Periodo UTC sin superposición | N08 periodo inverso o renovado retroactivamente |
| P09 Suspensión con estado válido y política | N09 suspender por timeout de proveedor |
| P10 Historial append-only tras cancelación | N10 DELETE en cascada rompe evidencia |
| P11 Migración EF aditiva sobre MySQL vacío/fixture | N11 migración destruye tabla existente |
| P12 Reversión ensayada conserva estado anterior | N12 rollback de datos pierde versiones aceptadas |

P01–P12/N01–N12 **NO han sido ejecutadas**. En una etapa posterior su CI debe identificar nombre del test real, Category=Integration, job, SHA, logs, resultado 0 FAIL y MySQL efímero 8.4, además de xUnit, aislamiento A/B y pruebas de rollback proporcionales. Un reporte de Build/Release o migración con tests SKIPPED/CANCELLED no los sustituye.

## 6. Impacto, controles A0–A5 y salida

Impacto actual exclusivamente este Markdown y nueva sección completa de CHANGELOG_AI.md; cero cambios en Domain, Application, API, Infrastructure, modelos Angular, migraciones EF, SQL, secretos, datos, proveedores, billing, workflows o entornos.

**A0:** lectura autenticada de configuración EF, repository transaccional actual, snapshot y agregados existentes por SHA, fuentes V5 y DAG. **A1:** diseño de trece objetos, diez constraints, siete etapas propuestas y doce pares P/N conceptuales; NO prueba funcional ejecutada. **A2:** tenant A/B, JWT/RBAC, PK/FK compuestas, idempotencia y datos privados fail-closed documentados. **A3:** compatibilidad de diseño con EF Oracle/MySQL 8.4, Domain y API actuales sin modificar; Category=Integration/MySQL no aplica al diff Markdown. **A4:** commit directo fast-forward/CAS solo en dev, Scope Lock y V5 exact-SHA completed/success, PLAN/COLA/BITACORA readback. **A5:** otra Task Javier debe revisar con evidencias independiente del autor; no autoaprobación.

D01–D08 comerciales siguen pendientes. Este documento NO constituye migración probada, autorización QA/main/PROD, operación financiera ni permiso de cambiar datos productivos. **Rollback documental:** git revert normal del changeset Markdown/CHANGELOG tras comprobar HEAD, sin reescribir BITACORA histórica.
