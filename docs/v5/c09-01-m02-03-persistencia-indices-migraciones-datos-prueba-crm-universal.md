# SOLQARYN V17 — C09-01.M02.03: persistencia, índices, migraciones y datos de prueba conceptuales de CRM universal

**Microtarea:** C09-01.M02.03 · **Autor:** PRIMARY DEV 04 / JAVIER_CHATGPT · **TASK_ID:** \`6aeacb7820ac8191b7d768a6f33dfa09\` · **Riesgo de esta entrega:** R0 documental · **Repositorio/ramas:** \`solqaryn/Solqaryn@dev\` · **PROJECT_SCOPE_LOCK:** STRICT.

**Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES (Drive \`1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA\`), PLAN_MAESTRO fila 1306; DAG_TECNICO validado por ID, COLA G/O habilitados al claim. Objeto: definir la **estrategia futura** de persistencia, índices, migraciones reversibles y datos de prueba para historial, segmentos, oportunidades, actividades y seguimiento CRM multiempresa.

**Límite comprobable:** NO se han creado ni ejecutado clases EF Core, entidades físicas, tablas, migraciones, scripts SQL, procedimientos, índices, fixtures ejecutables, endpoints, servicios, workers, jobs, cambios Angular, credenciales ni operaciones en Aiven/MySQL. Las decisiones indicadas como propuestas requieren una microtarea M03 autorizada, pruebas reales con MySQL 8.4 efímero y revisión de seguridad antes de tocar el runtime. Este documento no certifica integración, latencia, planes EXPLAIN, restauración ni consistencia transaccional ya ejecutadas.

## 1. Autoridades y compatibilidad

- \`C09-01.M01.01–06\` establece no-regresión, actores, riesgos, criterios futuros y gates. \`C09-01.M02.01\` define E01–E16/INV01–INV20: \`Empresa\`, \`UsuarioEmpresa\` y \`Cliente\` son referencias de baseline; \`PerfilCRM\`, \`InteracciónCRM\`, \`ActividadCRM\`, \`OportunidadCRM\`, \`EtapaOportunidad\`, \`SegmentoCRM\`, \`PreferenciaContacto\` y otros son **conceptos no implementados**.
- \`C09-01.M02.02\` define operaciones O01–O08, DTO D01–D08, errores X01–X12 y validaciones V01–V16 como contratos futuros. Este diseño no convierte esos nombres en tablas, scopes JWT o rutas existentes.
- Stack vigente: .NET 10, EF Core 10.0.12 y \`MySql.EntityFrameworkCore\` 10.0.9 (Oracle) sobre MySQL 8.4, solo como baseline para una implementación posterior. Domain conserva invariantes; Application determina permiso y caso de uso; Infrastructure aislaría EF/repositorios; API no confiaría en tenant recibido del cliente.
- El \`Cliente.EmpresaId=NULL\` legado permanece \`SIN_OWNER_CONFIRMADO\`: no actualizar silenciosamente, inferir propietario ni exponer como cliente compartido. Las decisiones de conciliación, PII, consentimiento, retención, volumen y particionamiento D01–D08 siguen ABIERTAS.
- M02.03 es documentación independiente bajo DAG; no habilita anticipadamente M03, DB, migración, IAM/JWT/RBAC, QA, main ni PROD. Revisar A5 por otra Task Javier.

## 2. Catálogo lógico propuesto: claves, aislamiento y relaciones

| Propuesta | Clave primaria / vínculos conceptuales | Reglas previas a cualquier esquema |
| --- | --- | --- |
| \`PerfilCRM\` | ID opaco, \`EmpresaId\` verificado, \`ClienteId\` con owner compatible | unique lógico por empresa/cliente y versión; PII mínima |
| \`InteraccionCRM\` | ID, \`EmpresaId\`, \`ClienteId\`, \`ActorId\`, \`FechaUtc\` | append-only lógico; canal/tipo allowlisted; sin secretos en payload |
| \`ActividadCRM\` | ID, \`EmpresaId\`, \`ClienteId\`, responsable y fecha de vencimiento UTC | estado y versión; responsable miembro activo del mismo tenant |
| \`OportunidadCRM\` | ID, \`EmpresaId\`, \`ClienteId\`, etapa y \`Version\` | transición CAS; no insertar ventas/asientos implícitos |
| \`EtapaOportunidad\` | ID, \`EmpresaId\`, código tipado y orden | unique por empresa/código; nunca compartir pipeline intertenant |
| \`SegmentoCRM\` | ID, \`EmpresaId\`, versión de definición | filtros versionados y allowlisted; no SQL/JS del usuario |
| \`ReglaSegmentacion\` | ID, \`EmpresaId\`, \`SegmentoId\`, campo/operador permitido | pertenencia compuesta; costo/volumen acotados |
| \`VinculoResponsable\` | ID, \`EmpresaId\`, actor y referencia a Cliente/Actividad | asignación no equivale a permiso JWT/RBAC |
| \`PreferenciaContacto\` | ID, \`EmpresaId\`, \`ClienteId\`, finalidad y canal | consentimiento específico; opt-out prevalece |
| \`EvidenciaConsentimiento\` | ID, \`EmpresaId\`, preferencia, origen y timestamp | append-only auditable, propósito/retención controlados |
| \`ClaveIdempotenciaCRM\` | \`EmpresaId\`, operación, actor, clave hash y vencimiento | unique contextual; colisión de payload se rechaza |
| \`EventoAuditoriaCRM\` | ID, \`EmpresaId\`, actor, correlación, operación y resultado | retención mínima sin JWT, datos sensibles ni cuerpos completos |

**Restricción sistémica:** futuras FK \`(EmpresaId, RecursoId)\` y unique compuestos deben impedir vincular filas de B desde A incluso si se omite un filtro en Application. Las claves de \`Cliente\` legacy y su \`EmpresaId\` pueden exigir conciliación explícita antes de introducir referencias compuestas. Nunca añadir cascadas destructivas por defecto; cualquier operación de retención/anonimización requiere hold legal y auditoría de asociaciones.

## 3. Índices candidatos sujetos a EXPLAIN real (I01–I12)

| ID | Acceso esperado | Índice lógico a evaluar | Riesgo/criterio |
| --- | --- | --- | --- |
| I01 | ficha por cliente | \`PerfilCRM(EmpresaId, ClienteId)\` unique | evita duplicación y cross-tenant |
| I02 | timeline paginado | \`InteraccionCRM(EmpresaId, ClienteId, FechaUtc DESC, Id DESC)\` | cursor estable, evita escaneo entre empresas |
| I03 | actividades abiertas | \`ActividadCRM(EmpresaId, ResponsableId, Estado, VenceUtc)\` | se debe comprobar selectividad |
| I04 | oportunidad por cliente | \`OportunidadCRM(EmpresaId, ClienteId, Estado, ActualizadoUtc)\` | no filtrar sólo por Id global |
| I05 | cambios por etapa | \`OportunidadCRM(EmpresaId, EtapaId, Version)\` | CAS y cambio de etapa autorizado |
| I06 | etapas | \`EtapaOportunidad(EmpresaId, Codigo)\` unique | etapa B jamás reutilizada por A |
| I07 | segmentos | \`SegmentoCRM(EmpresaId, Estado, Version)\` | consultas acotadas |
| I08 | reglas | \`ReglaSegmentacion(EmpresaId, SegmentoId, Orden)\` | no ejecución de criterios sin allowlist |
| I09 | asignaciones | \`VinculoResponsable(EmpresaId, ActorId, ClienteId)\` | revocación y pertenencia siempre en backend |
| I10 | consentimiento | \`PreferenciaContacto(EmpresaId, ClienteId, Finalidad, Canal)\` | última revocación prevalece |
| I11 | deduplicación | \`ClaveIdempotenciaCRM(EmpresaId, Operacion, ActorId, ClaveHash)\` unique | evitar doble envío y claves cross-tenant |
| I12 | auditoría | \`EventoAuditoriaCRM(EmpresaId, FechaUtc, Id)\` | consultas restringidas por rol y retención |

Los índices **no se han creado ni medido**; en M03 confirmar cardinalidades, tamaño, selectividad, planes \`EXPLAIN ANALYZE\`, inserts concurrentes y coste de mantenimiento con dataset efímero, sin datos personales. Si un índice cambia rendimiento o unicidad, rediseñarlo con evidencia y rollback probado.

## 4. Diseño de migración FUTURA por etapas reversibles (M01–M08)

| Etapa | Acción planeada, NO ejecutada | Gate y reversión exigidos |
| --- | --- | --- |
| M01 | inventariar esquema Oracle EF vigente y FK/índices de \`Cliente\` | snapshot verificado y aprobación del diff; ninguna mutación |
| M02 | agregar estructuras nuevas opcionales de CRM por empresa | migración aditiva, transacción cuando soporte; rollback de estructuras vacías |
| M03 | introducir FK/índices compuestos y constraints de owner | probar A/B y nulos legados; no forzar backfill implícito |
| M04 | backfill opcional a partir de owner **verificado**, en lotes auditados | dry-run, doble control, conteo exacto; si no hay prueba de owner, cuarentena |
| M05 | habilitar lectura read-only detrás de bandera segura para tenant piloto | retorno inmediato a bandera OFF sin destruir datos |
| M06 | habilitar escritura controlada con idempotencia y CAS | prueba de concurrencia/transacción + compensación/reversión |
| M07 | restringir permisos y preparar retención/archivado | autorización legal, aprobación técnica, hold y pruebas negativas |
| M08 | limpieza futura o corte final sólo con aprobación aparte | nunca drop destructivo automático ni PROD como efecto lateral |

**Rollback:** preferir desactivar flags/flujo y mantener tablas para reconciliación; un \`Down()\` destructivo no es automáticamente seguro después de escrituras. Preparar scripts inversos y restauración en entorno efímero; confirmar tenant, conteos y auditoría. Prohibido saltar a datos reales/Aiven por la aprobación del contrato.

## 5. Matriz de fixtures sintéticos FUTUROS (F01–F12)

| Caso | Datos sintéticos propuestos | Oracle positivo / rechazo negativo que la futura suite debe comprobar |
| --- | --- | --- |
| F01 | Empresa A y B; Cliente A/B | A consulta sólo A; B forjado se deniega sin PII |
| F02 | Cliente legado \`EmpresaId=NULL\` | no se muestra hasta reconciliación documentada |
| F03 | actor A con membresía revocada | consultas y cambios rechazados incluso con token viejo |
| F04 | actividad A con responsable B | FK/servicio rechazan relación cruzada |
| F05 | timeline mismo UTC, IDs distintos | orden total determinista y cursor sin saltos |
| F06 | millones sintéticos de interacciones por empresa | paginación/budget medidos sin revelar conteos B |
| F07 | 2 writers sobre oportunidad A con \`Version\` igual | exactamente un CAS gana; el otro 409 sin mutación parcial |
| F08 | idempotencia misma clave/payload en A | un efecto/registro; clave igual en B no comparte respuesta |
| F09 | clave igual con payload diferente | colisión 409; ninguna duplicación |
| F10 | consentimiento válido y opt-out posterior | reenvío se deniega; auditoría mínima |
| F11 | fallo MySQL/timeout a mitad de transacción | rollback real, sin filas parciales ni default allow |
| F12 | migración \`Up()\` + rollback seguro en MySQL 8.4 efímero | constraints válidos, recuentos y restauración repetibles |

Todos los fixtures son **propuestas no ejecutadas**. Para la unidad M03 futura registrar los comandos \`dotnet test --filter Category=Integration\`, la versión real de MySQL 8.4, los logs y el número exacto de PASS/FAIL, comprobando que los tests con \`Category=Integration\` realmente se ejecutaron. Un job de compilación o migración sin tests **no** constituye A3 PASS.

## 6. Criterios de aceptación y controles de seguridad futuros

1. Toda consulta y escritura filtra por \`EmpresaId\` autoritativa y valida \`UsuarioEmpresa\` activo, rol y ownership real del recurso en backend; cabeceras, host, cookie y frontend son datos no confiables.
2. Las pruebas negativas cubren IDOR/BOLA, tenant A/B, rol lector/escritor, revocación, legado NULL, exportación sin permiso, consentimiento y ausencia de JWT.
3. Las consultas de timeline/segmentación son paginadas, deterministas y parametrizadas; restringen tiempo, filas y coste. No se aceptan nombres arbitrarios de campo, SQL o JS.
4. Versiones CAS, idempotency key contextual y transacción garantizan una sola escritura efectiva en retries/concurrencia; la auditoría registra correlación opaca sin PII.
5. Migración aditiva en MySQL efímero demuestra FK/constraints A/B, reversibilidad y consistencia de conteos; no se acepta un mock como prueba de motor.
6. Minimización de PII, consentimiento por canal/finalidad, opt-out prioritario, retención y hold legal se contrastan antes de contacto/export o borrado.
7. Para una entrega R2 de persistencia: \`dotnet build -c Release\`, xUnit positivos/negativos, \`Category=Integration\` en servicio efímero, Scope Lock/V5 y A5 de revisor independiente. Aún **NO_MEDIDO**.
8. Toda migración y despliegue real requieren scope posterior y autorización apropiada. Este documento no libera ramas QA/main/PROD ni secretos.

## 7. Cierre R0 de esta microtarea: evidencia y límites

- **A0**: revisión de alcance, dependencias M01+A5/DAG, contrato lógico, índices I01–I12, migraciones M01–M08, fixtures F01–F12 y plan de rollback documental; archivo y CHANGELOG vinculados al único ID.
- **A1**: inspección estática de criterios, matriz positiva/negativa y fronteras; **NO** hay asserts xUnit ejecutados en esta unidad documental.
- **A2**: revisión estática tenant A/B, JWT/RBAC, membresía/revocación, PII/consentimiento, legacy NULL e idempotencia; **NO** atribuir certificación ejecutable.
- **A3**: \`NO_APLICA_R0\` a compilación .NET, HTTP/API, MySQL/EF, migraciones, Angular y Playwright; NO ejecutados por este documento.
- **A4**: sólo podrá acreditarse tras commit fast-forward en \`dev\`, check \`verify-solqaryn-project-scope\` y \`V5 - Sin encabezados vacios ni atribucion cruzada\` \`completed/success\` del SHA exacto, diff/blobs verificados y readback institucional PLAN/COLA/BITACORA.
- **A5**: \`PENDIENTE\` revisión independiente por otra Task de las seis Javier con TASK_ID distinto de \`6aeacb7820ac8191b7d768a6f33dfa09\`; no se permite autoprobación.
- **Rollback técnico**: si el documento falla, restaurar mediante \`git revert\` aditivo sobre el SHA exacto preservando commits de otros; nunca \`reset --hard\`, force-push ni borrar BITACORA.

**Estado de evidencia:** TODO en esta sección requiere verificación causal al publicar; no se anticipan SUCCESS, ni tests ejecutados, ni creación de DB/API. El cierre DOCUMENTAL no satisface ejecución de M03 ni certifica lista para QA/PROD.
