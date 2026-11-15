# SOLQARYN V5 FINAL — C01-04.M02.03: persistencia, índices, migraciones y datos de prueba del expediente universal

**Autor:** PRIMARY DEV 06 — JAVIER_CHATGPT, TASK_ID 6af16bbad26881918c283abe06e1f32a. **Fecha:** 2026-11-14 (America/Tegucigalpa). **Rama:** solqaryn/Solqaryn, dev. **Scope Lock:** STRICT.

**Resultado atómico:** especificar un contrato de diseño para persistencia, índices, migración futura reversible y datos sintéticos de prueba de expediente universal, cronología, decisiones, observaciones, adjuntos y versiones. **ESTADO: DOCUMENTAL_PROPUESTO_NO_IMPLEMENTADO**. Este documento no modifica entidades .NET, DbContext, EF, SQL, MySQL, endpoints, identidad, permisos, UI, datos ni despliegues; no certifica aislamiento multiempresa ni autoriza QA/main/PROD.

## 1. Autoridad, preflight y límites del diseño

Fuente comercial vigente: [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), microtarea C01-04.M02.03, «Definir persistencia, índices, migraciones y datos de prueba». Control institucional: [PLAN_MAESTRO, COLA, DAG_TECNICO y BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). Preflight individual: PLAN PENDIENTE sin owner, COLA G/O=SI y DAG F=VALIDADO/G=SI, prerrequisito C01-04.M01.01–06 con A5 independiente. El cierre documental M02.01 ya está en LISTO; M02.02 puede avanzar en paralelo y NO se da por terminado ni constituye predecesor efectivo de este ID.

El contrato M02.01 define EX01–EX16 e IN01–IN18 como agregados/invariantes **conceptuales, no tablas creadas**. Las fuentes de código verificadas bajo dev contienen:
- backend/src/Domain/Entities/RegistroAuditoria.cs: EmpresaId nullable (plataforma/legacy), Fecha, ReferenciaId, Entidad, CorrelationId, descripción y valores anteriores/nuevos; es evidencia de auditoría existente, NO expediente universal.
- backend/src/Domain/Entities/CompraDocumento.cs: EmpresaId nullable legacy, CompraId, PublicId, URL, tipo, tamaño y eliminación lógica; NO adjunto universal ni autorización por identificador público.
- backend/src/Domain/Security/ContextoTenantActual.cs: contexto construido a partir de UsuarioEmpresa activa; ExigirEmpresa falla cerrado cuando se cruza ámbito.
- backend/src/API/Controllers/AuditoriaController.cs: Authorize, RequierePermiso y condición de administrador sobre consulta de auditoría; NO endpoint de expediente.
- docs/v5/c01-04-m01-03-inventario-contratos-codigo-evidencia.md identifica como **riesgo por validar** que el repositorio AuditoriaRepository.GetFilteredAsync no expresa un predicado empresa propio; no equivale a afirmar una explotación ni a probar filtros globales.

**Frontera:** no promover registros legacy con EmpresaId nulo a un tenant supuesto. Toda decisión física sobre conservación, cifrado, proveedor de adjuntos, SQL e índices queda pendiente de diseño aprobado, impacto, migración y pruebas CI aisladas.

## 2. Modelo lógico de persistencia propuesto (NO IMPLEMENTADO)

| Clave lógica | Registro propuesto | Propiedad y vínculo invariante |
| --- | --- | --- |
| P01 | Expediente | EmpresaId no nulo, SucursalId opcional pero verificado; RecursoTipo y RecursoId autorizados |
| P02 | ExpedienteVersion | EmpresaId+ExpedienteId, Version monotónica, hash de contenido, actor, FechaUtc y motivo |
| P03 | EntradaTimeline | EmpresaId+ExpedienteId, Id y FechaUtc de orden estable, tipo, origen, versión |
| P04 | ReferenciaFuente | EmpresaId+ExpedienteId, OrigenModulo, clave externa y permiso de lectura revalidado |
| P05 | Observacion | EmpresaId+ExpedienteId, actor, texto saneado, versión y estado auditable |
| P06 | DecisionFormal | EmpresaId+ExpedienteId, permiso explícito, resultado, motivo y aprobación exigida |
| P07 | RevisionDecision | EmpresaId+DecisionId, revisor autorizado cuando aplique y sello de versión |
| P08 | AdjuntoReferencia | EmpresaId+ExpedienteId, tipo/MIME/tamaño y objeto privado sin URL pública estable |
| P09 | AdjuntoVersion | EmpresaId+AdjuntoId, hash, versión, retención y vínculo a revisión previa |
| P10 | ActorMembershipEvidence | Referencia mínima, no copia de JWT ni documento de identidad, a membresía verificable |
| P11 | ReciboIdempotencia | EmpresaId+scope+operación+clave, hash de payload canónico, respuesta/estado y caducidad |
| P12 | VinculoTraza | EmpresaId+ExpedienteId, referencia opaca a auditoría/autorización y CorrelationId saneado |

Los nombres P01–P12 son **etiquetas del diseño**, no nombres de tablas/DbSet existentes. Para toda FK hija, la identidad de empresa forma parte de la validación de ambos extremos: no aceptar ExpId de B en operación de A aunque un Id numérico coincida. Ninguna relación reutiliza permisos de plataforma o una sucursal ajena. La autoridad de actor, sucursal y empresa se vuelve a resolver server-side; no nace de parámetros, headers, filtros, rutas ni claims no reconciliados.

Las mutaciones propuestas usan versión comparada de manera atómica y no permiten dos escritores con una misma revisión. La cronología es proyección de hechos inmutables; reversión agrega nueva entrada compensatoria, no borra historia. Adjuntos deben volver a autorizar el acceso al contenido y cada versión al descargar; PublicId no equivale a permiso. Datos heredados sin EmpresaId seguro quedan en cuarentena hasta atribución demostrable, no en un tenant comodín.

## 3. Índices y restricciones candidatos (pendientes de prueba de carga)

| ID | Clave/índice potencial | Motivo y verificación requerida |
| --- | --- | --- |
| IX01 | P01: (EmpresaId, RecursoTipo, RecursoId) | Buscar expedientes por recurso sin full-scan global; unicidad solo si negocio confirma expediente único |
| IX02 | P03: (EmpresaId, ExpedienteId, FechaUtc DESC, Id DESC) | Paginación keyset determinista incluso con timestamps idénticos |
| IX03 | P02: UNIQUE (EmpresaId, ExpedienteId, Version) | Una versión por expediente, sin sobrescribir revisión histórica |
| IX04 | P05/P06: (EmpresaId, ExpedienteId, FechaUtc, Id) | Consultas de observaciones/decisiones ordenadas con tenant |
| IX05 | P08/P09: (EmpresaId, ExpedienteId, AdjuntoId, Version) | Recuperar versiones privadas por expediente verificado |
| IX06 | P11: UNIQUE (EmpresaId, RecursoTipo, RecursoId, Operacion, Clave) | Un efecto lógico ante reintento; hash diferente con clave igual debe generar conflicto |
| IX07 | P12: (EmpresaId, CorrelationId) | Relacionar trazas sin índice transversal accesible a usuario externo |
| IX08 | FK lógica compuesta (EmpresaId, IdPadre) en P02–P12 | Bloquear relaciones entre tenants por dato falsificado, aun con IDs coincidentes |

No declarar índices óptimos sin EXPLAIN, cardinalidad, collation, costos de escritura y plan real MySQL 8.4. Limitar columnas de texto/JSON en índices; validar tamaño de clave, selectividad, volumen, límite de páginas y retención. El identificador de cursor futuro debe enlazarse a empresa, filtros, orden y revisión, con expiración y firma verificable. El backend siempre filtra por empresa confirmada, además de cualquier particionado o FK.

## 4. Plan reversible de migración FUTURA (NO EJECUTADO)

1. **Gate de diseño:** M02 completo, A5 por otro TASK_ID, arquitectura/seguridad revisada, esquema EF real y proveedor MySQL 8.4 confirmados; obtener respaldo verificable en entorno efímero de CI sin tocar Aiven ni producción.
2. **Expand aditivo:** proponer migraciones versionadas para nuevas estructuras aisladas, inicialmente sin consultas activas ni escribir recursos legacy. Revisar dependencias de FK, restricciones, tiempos y locks. Ningún ALTER/CREATE se ha ejecutado aquí.
3. **Control de escritura:** flag seguro deshabilitado por defecto; autorizar empresa/sucursal/recurso desde backend, permisos explícitos, versión CAS, transacción auditable e idempotencia ligada a tenant y payload.
4. **Backfill condicionado:** solo fuentes con empresa atribuida inequívocamente y consentimiento/retención aplicables; lotes idempotentes, checksum por ámbito, reanudación segura y cuarentena de legacy ambiguo. No copiar valores anteriores/nuevos que contengan PII sin minimización.
5. **Lecturas en sombra/validación:** cotejar cuentas por empresa, orden keyset, 404/403 para B y revocados, retención, latencia y fuga en logs; comparar con fuente sin asumir equivalencia automática.
6. **Activación por gates:** habilitar consumidor únicamente tras CI backend/EF/MySQL efímero, negativas JWT/RBAC/tenant A/B, backup y rollback ensayados, dueño y autorización explícita para ambiente. Documento LISTO_DEV no activa ninguna funcionalidad.

**Rollback futuro:** deshabilitar nuevo lector/escritor, preservar entradas auditables, revertir consumidor/configuración de forma aditiva y restaurar exclusivamente mediante procedimiento probado. Una migración destructiva o eliminación de columnas no queda aprobada aquí. El rollback inmediato de este documento es revert normal del commit Markdown en dev, sin force-push.

## 5. Fixtures sintéticos y escenarios positivos/negativos FUTUROS_NO_EJECUTADOS

Proponer empresas A=1001 y B=2002, usuarios A1 y B1, sucursales A11/B22, expedientes E-A/E-B y dos entradas con igual FechaUtc en A, sin datos personales reales. Crear un usuario A revocado, un rol A solo-lectura, una decisión que exige aprobación separada, un adjunto privado de B y dos solicitudes concurrentes con versión igual.

| ID | Positivo futuro | Negativo futuro |
| --- | --- | --- |
| FT01 | A1 lista solo expedientes A | A1 con Id E-B no ve metadatos ni cronología B |
| FT02 | Cursor A con empate FechaUtc pagina sin duplicar | Cursor A modificado para empresa B se rechaza |
| FT03 | Dos versiones del adjunto propio preservan historial | PublicId de B no permite bytes en A |
| FT04 | Replay idéntico conserva un solo recibo | Clave igual con payload distinto genera conflicto |
| FT05 | CAS acepta una de dos escrituras de misma versión | Segundo escritor no pisa la primera decisión |
| FT06 | Lectura por recurso requiere membresía vigente | Membresía revocada rechaza lecturas y mutaciones |
| FT07 | Filtro server-side incluye empresa/sucursal | EmpresaId nulo legacy no se publica como global |
| FT08 | Decisión con revisor habilitado deja traza | Observación o rol sin permiso no aprueba decisión |
| FT09 | Aislamiento se conserva en búsquedas y conteos | Conteo global o URL compartida no revela B |
| FT10 | Redacción de errores retorna correlación segura | JWT, cookies, PII y stacktrace no salen en logs |
| FT11 | Reversión genera hecho compensatorio | Rollback no borra hechos ni cambia origen comercial |
| FT12 | CI y blobs del SHA pertinente son verificables | SKIPPED/CANCELLED/queued no se informan como PASS |

Todos los pares son **oráculos propuestos, no pruebas ejecutadas**. Cuando exista código real se necesitan pruebas xUnit, MySQL efímero e integración API; si aparece frontend, pruebas Angular/Playwright, seguridad A/B y carga proporcional.

## 6. Validaciones proporcionales y decisión de salida

- **A0 documental:** coherencia con M01/M02.01, doce registros lógicos, ocho índices candidatos, seis pasos futuros, doce pares FT y ausencia de secretos o SQL ejecutado; verificar CHANGELOG sin encabezados vacíos ni viñetas cruzadas.
- **A1 documental:** casos positivos y negativos falsables descritos; **NO_EJECUTADOS**. Ninguna validación funcional de expediente se declara aprobada.
- **A2 de alcance:** especificación fail-closed JWT/RBAC/tenant A/B, versionado y cuarentena legacy, sin manipular controles ejecutables; **NO_APLICA_POR_DIFF_DOCUMENTAL** para pruebas runtime.
- **A3 documental:** conciliación con STANDARD, PLAN, COLA y DAG de ID exacto, sin activar API/EF/MySQL ni liberar M03; readbacks institucionales.
- **A4 obligatorio:** solamente tras publicar dos archivos Markdown en dev por fast-forward/HEAD CAS, revisar diff, blobs y checks Scope Lock y V5 completados con success para **SHA exacto**. Si hay cola CI pertinente, mantener VALIDANDO; nunca convertir cancelación/fallo en PASS.
- **A5 independiente pendiente:** debe revisar otra de las cinco Tasks Javier diferente de 6af16bbad26881918c283abe06e1f32a, sobre SHA y diff conservados. No realizar autoaprobación ni prometer QA/main/PROD.

**Criterio de no-regresión:** ninguna tabla, migration, controller, provider, tenant, permiso, configuración ni secreto cambia por este documento. Si un implementador futuro decide materializar P01–P12, deberá reconciliar nombres/relaciones con el esquema vigente, medir índices, verificar grants, probar migración/rollback y añadir evidencia distinta a esta revisión documental.
