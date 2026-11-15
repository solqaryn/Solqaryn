# SOLQARYN V16 — C08-01.M02.03: persistencia, índices, migraciones y fixtures de citas/agenda

**ID:** C08-01.M02.03, PLAN_MAESTRO fila 1162. **Autor ejecutor:** PRIMARY DEV 05 JAVIER, TASK_ID \`6adc2e55f47c81919f6374e561687076\`. **Rama:** \`solqaryn/Solqaryn@dev\`. **Alcance:** PROJECT_SCOPE_LOCK=STRICT. **Riesgo V15:** R0, contrato documental sin runtime.

**Fuente ejecutable:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Google Doc \`1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA\`; PLAN_MAESTRO/COLA/DAG_TECNICO. Resultado de fase: citas, horarios, disponibilidad, personal, recursos y cancelaciones, con evidencia futura de conflicto horario, zona IANA/DST y autorización.

## 1. Trazabilidad, límites y decisiones abiertas

- C08-01.M01.06: barreras G01–G16, rollback y decisiones D01–D08 **pendientes**, sin autorización de DB/API/servicios nuevos.
- C08-01.M02.01: E01–E14, INV01–INV18, REL01–REL12 y T01–T12: conceptos y oráculos **FUTUROS_NO_EJECUTADOS**.
- C08-01.M02.02: OP01–OP12, DTO01–DTO12, ERR01–ERR14 y VAL01–VAL18: contratos semánticos **no endpoints activos**.
- Este documento propone persistencia e índices para evaluación posterior. **No** crea clases EF, DbSets, DDL, migraciones, tablas MySQL, datos, endpoints, workers, eventos, webhooks, cron, planes de pago ni cambios productivos. No se confirma reserva, se altera inventario ni se manipula información personal.
- La identidad y la empresa se derivarán en backend de sesión/membresía verificada; \`EmpresaId\`, \`SucursalId\`, host y cualquier claim no comprobado enviado por cliente son filtros no confiables. Negar acceso cuando faltan ownership, rol, sesión, permisos o versión.

## 2. Estructuras conceptuales de persistencia P01–P12 (FUTURAS)

| ID | Agregado/conjunto propuesto | Columnas mínimas candidatas e invariantes |
| --- | --- | --- |
| P01 | SucursalAgenda | EmpresaId, SucursalId, ZonaIana, Version; tenant y zona aprobada |
| P02 | ServicioAgenda | EmpresaId, SucursalId, ServicioId, DuracionMin, BufferMin, Estado, Version |
| P03 | ProfesionalAgenda | EmpresaId, SucursalId, ProfesionalId, Estado, Version; membresía habilitada |
| P04 | RecursoAgenda | EmpresaId, SucursalId, RecursoId, Capacidad, Estado, Version |
| P05 | ServicioProfesional | EmpresaId, SucursalId, ServicioId, ProfesionalId, Vigencia; relación validada |
| P06 | JornadaProfesional | EmpresaId, SucursalId, ProfesionalId, Dia, InicioLocal, FinLocal, ZonaIana, Version |
| P07 | JornadaRecurso | EmpresaId, SucursalId, RecursoId, Dia, Intervalo, Capacidad, Version |
| P08 | BloqueoAgenda | EmpresaId, SucursalId, BloqueoId, Sujeto, InicioUtc, FinUtc, MotivoCodigo, Version |
| P09 | CitaAgenda | EmpresaId, SucursalId, CitaId, ClienteScopeId, ServicioId, ProfesionalId, InicioUtc, FinUtc, Estado, Version |
| P10 | OcupacionCitaRecurso | EmpresaId, SucursalId, CitaId, RecursoId, InicioUtc, FinUtc, Estado, Version |
| P11 | TransicionCita | EmpresaId, SucursalId, CitaId, Tipo, EstadoPrevio, EstadoNuevo, VersionPrev, VersionNueva, CorrelationId |
| P12 | IntentoIdempotenteAgenda | EmpresaId, ActorScopeId, Comando, KeyHash, FingerprintHash, Estado, ResultadoReferencia, ExpiracionUtc |

Los nombres son **esquema de diseño, no afirmación de tablas existentes**. Todo registro subordinado tiene pertenencia a empresa/sucursal coherente y FKs compuestas por tenant donde proceda. No reutilizar \`ReservaInventario\` ni inventar una FK de agenda a saldos ERP. Auditar actor/correlación sin almacenar tokens, teléfono, nombre completo ni payload sensible en índices o claves idempotentes. Retención, cifrado y borrado quedan sujetos a D01–D08, política legal y revisión R2.

## 3. Índices y restricciones candidatas IDX01–IDX12 (NO CREADOS)

| ID | Clave candidata | Propósito / advertencia |
| --- | --- | --- |
| IDX01 | (EmpresaId, SucursalId, ServicioId, Estado) | visibilidad de servicios por empresa |
| IDX02 | (EmpresaId, SucursalId, ProfesionalId, Estado) | agenda profesional autorizada |
| IDX03 | (EmpresaId, SucursalId, RecursoId, Estado) | recursos y capacidad por sede |
| IDX04 | (EmpresaId, SucursalId, ServicioId, ProfesionalId) | relación servicio-personal sin cruce A/B |
| IDX05 | (EmpresaId, SucursalId, ProfesionalId, Dia) | jornada con zona revisada |
| IDX06 | (EmpresaId, SucursalId, RecursoId, Dia) | jornada y excepciones de recurso |
| IDX07 | (EmpresaId, SucursalId, Sujeto, InicioUtc, FinUtc) | bloqueos y solapamientos |
| IDX08 | (EmpresaId, SucursalId, ProfesionalId, InicioUtc, Estado) | citas por profesional, orden estable |
| IDX09 | (EmpresaId, SucursalId, RecursoId, InicioUtc, FinUtc) | ocupación candidata y diagnóstico |
| IDX10 | (EmpresaId, SucursalId, CitaId, Version) | optimistic concurrency y lectura puntual |
| IDX11 | (EmpresaId, SucursalId, CitaId, CorrelationId) | transición/auditoría sin PII |
| IDX12 | (EmpresaId, ActorScopeId, Comando, KeyHash) | idempotencia tenant/actor/acción scoped |

Una unicidad simple sobre \`InicioUtc\` **no** impide intervalos parcialmente superpuestos ni varios recursos en paralelo. La exclusión real exige transacciones, aislamiento/bloqueo apropiado, orden determinista de adquisición, revalidación de disponibilidad dentro de la transacción y prueba adversarial con 64 intentos simultáneos en MySQL 8.4 **efímero**. Los índices se revisarán mediante EXPLAIN/planes y datos sintéticos antes de cualquier migración. No se promete rendimiento, bloqueo de huecos ni exclusión temporal mediante este Markdown.

## 4. Ruta de migración aditiva propuesta MIG01–MIG08 (NO EJECUTADA)

| Paso futuro | Precondición obligatoria / validación y reversión |
| --- | --- |
| MIG01 | inventariar AppDbContext y migraciones existentes; mapear P01–P12 a entidades reales, comprobar colisiones y autorizar diseño |
| MIG02 | establecer límites de tenant, RBAC, privacidad, retención y versiones; rechazo A/B y rollback documentados |
| MIG03 | crear tablas/columnas **aditivas** y FKs compuestas en MySQL efímero bajo CI, jamás aplicar a Aiven real |
| MIG04 | definir índices IDX01–IDX12 según cardinalidad real; medir EXPLAIN y evitar bloqueos excesivos |
| MIG05 | migrar/backfill solo fixtures sintéticos, por lotes auditables e idempotentes, sin importar clientes reales |
| MIG06 | activar pruebas de intervalos, DST ambiguo/inexistente, CAS, replay y 64 operaciones concurrentes en MySQL 8.4 efímero |
| MIG07 | habilitar integración de Application/API únicamente mediante scope técnico independiente con autorización, CI y A5 |
| MIG08 | ensayar reversa compensatoria de versión/esquema/datos sintéticos y confirmar que agenda y ERP permanecen separados |

**No hay pasos ejecutados: 0/8.** Para migración irreversible o producción, NO_GO hasta orden expresa R3, backups verificables, ventana, pruebas de restore y rollback. Jamás inferir que este plan autoriza EF CLI, modificaciones a \`solqaryn_dev\` o un servicio live.

## 5. Fixtures de aceptación FUTUROS F01–F12, positivos y negativos

| Caso | Positivo futuro | Negativo/falla obligatoria |
| --- | --- | --- |
| F01 | sede A guarda zona IANA aprobada | sede B no puede modificar zona de A |
| F02 | servicio activo acepta duración/buffer dentro de límites | cero, negativo o overflow rechaza |
| F03 | profesional de A asignado a servicio de A | profesional revocado/otra empresa rechaza |
| F04 | recurso disponible de A respeta capacidad/estado | recurso de B o inactivo rechaza |
| F05 | jornada/feriado deja intervalos válidos | bloqueo o ausencia impide confirmar |
| F06 | UTC e IANA resuelven instante con offset explícito | DST inexistente o ambiguo sin offset rechaza |
| F07 | cita dentro de turno queda pendiente/confirmada solo tras transacción | 64 intentos mismo slot no crean doble ocupación |
| F08 | recursos múltiples se adquieren atómicamente | un recurso ocupado revierte toda mutación |
| F09 | replay misma clave/tenant/actor/payload es idempotente | fingerprint/tenant alterado no reutiliza resultado |
| F10 | reprogramación válida conserva historia/versiones | fallo intermedio deja ocupación original |
| F11 | cancelación válida libera una vez y audita | cancelación duplicada/versión obsoleta no libera dos veces |
| F12 | proyección pública mínima no exhibe PII ni inventario | B, token inválido o timeout no revelan datos ni cambian ERP |

**Resultado actual:** 12 pares diseñados, **0/12 ejecutados**; MySQL/HTTP/xUnit/Playwright/Chromium, concurrencia 64, latency, query plans y cobertura **NO_MEDIDO**. Datos de prueba deben ser sintéticos A/B con zona DST (p. ej. America/New_York) y zona sin transición (America/Tegucigalpa); jamás importar información personal real.

## 6. Evidencia A0–A5 y rollback

- **A0:** requisito exacto M02.03 y fuentes M01.06/M02.01/M02.02; diff limitado a este documento y \`CHANGELOG_AI.md\`; clasificación R0.
- **A1:** revisar consistencia P01–P12/IDX01–IDX12/MIG01–MIG08/F01–F12, con negativos, error, frontera, zona horaria y reversión **escritos, no ejecutados**.
- **A2:** revisión estática JWT/RBAC/revocación, aislamiento tenant A/B, IDOR/BOLA, PII, intervalos y replay. Implementación de controles **no afirmada**.
- **A3:** cotejo documental de contratos previos; MySQL/EF/API/.NET/Angular/browser **NO_APLICA** al diff sin runtime. En implementación R2 sí serán exigibles y no se podrá sustituir con mocks.
- **A4:** publicar **sólo** los dos Markdown por fast-forward/CAS en \`dev\`, con Scope Lock y procedencia V5 \`completed/success\` sobre SHA exacto; actualizar PLAN/BITACORA y readbacks causales.
- **A5:** Task Javier distinta del autor verifica independentemente documento, errores potenciales, SHA, CI pertinente y gates; \`LISTO_DEV\` sin A5 no libera sucesora técnica que lo requiera.

**Rollback:** \`git revert\` normal sobre commit aditivo, previa relectura del HEAD actual y CAS; nunca force-push, borrar BITACORA, manipular COLA/DAG G/O ni restaurar datos productivos. Si un control falla, mantener VALIDANDO con owner y diagnóstico, no inventar PASS. QA/main/PROD permanecen fuera de alcance.
