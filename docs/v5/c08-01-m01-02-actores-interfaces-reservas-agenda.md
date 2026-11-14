# SOLQARYN V5 — C08-01.M01.02 — Actores, entradas, salidas e interfaces de reservas y agenda

**Autor:** PRIMARY DEV 03 — JAVIER (`6adc2e4bd8a48191ac7d8641ed95a1a3`). **Fecha:** 2026-11-14. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL; C08-01 (Reservas y agenda), M01.02. **Naturaleza:** documento preparatorio, no implementación ni certificación de funcionalidad.

## 1. Alcance y antecedente acreditado

El objetivo futuro C08-01 son citas, horarios, disponibilidad, personal, recursos y cancelaciones, con pruebas reproducibles de conflictos de horario, zona y autorización. Esta M01.02 identifica actores, entradas, salidas, interfaces y fronteras de confianza, y **no** crea API, reservas, migraciones, workers, notificaciones ni UI. Su antecedente `C08-01.M01.01` se publicó en `docs/v5/c08-01-m01-01-alcance-reservas-agenda.md`, blob `26805ab07dd3f20cf986b6da4ebc24fb702fa8f6`, con invariantes NR01–NR12 y pruebas P01–P08/N01–N12 **FUTUROS NO_EJECUTADOS**. No se presupone aprobación A5 del antecedente. Reserva de inventario no equivale a reserva de tiempo.

## 2. Actores y superficies A01–A14 — contratos futuros

| ID | Actor / superficie | Entrada por verificar | Salida / límite esperado |
| --- | --- | --- | --- |
| A01 | Cliente solicitante | Servicio publicado, sede, franja y contacto mínimo | Solicitud con estado explícito, no confirmación inventada |
| A02 | Profesional | JWT, membresía y permiso de sede vigentes | Agenda propia y cambios dentro del rol |
| A03 | Recepción | Rol activo, sede, referencia de cita y operación | Crear, reprogramar o cancelar con autorización backend y auditoría |
| A04 | Administrador de empresa | Empresa verificada, RBAC, política de agenda | Configuración autorizada, nunca rol global |
| A05 | Supervisor | Permiso específico y alcance de lectura | Vista mínima, sin PII de otros tenants |
| A06 | Visitante anónimo | Host/storeKey resuelto confiablemente | Sólo horarios publicados, sin datos de profesionales privados |
| A07 | Cliente Angular 22.2.1 | Estado visual y hora local no confiables | UX accesible; no decide permisos |
| A08 | API ASP.NET Core 10 | Token o contexto público permitido, DTO tipado | Validación, límites, rate limiting y denegación segura |
| A09 | Servicio Application futuro | Tenant/actor resueltos y clave idempotente | Orquestación de estados, sin efectos duplicados |
| A10 | Domain futuro | Duración, buffers, capacidad, política temporal | Validación determinista de disponibilidad y transiciones |
| A11 | Infrastructure/MySQL futuro | Alcance tenant, versión, recurso y transacción | Conflicto atómico ante doble ocupación, sin migración ahora |
| A12 | Proveedor de notificación futuro | Evento post-commit, permiso y destino minimizado | Aviso idempotente; no se envía hoy |
| A13 | Auditoría y logs | Correlación, actor, tenant, recurso y motivo saneado | Trazabilidad sin JWT, secretos ni PII impropia |
| A14 | Soporte autorizado | Ticket, permisos y tenant explícitos | Diagnóstico limitado; sin impersonación |

## 3. Interfaces conceptuales I01–I14 — NO IMPLEMENTADAS

| ID | Entrada futura | Respuesta / rechazo previsto |
| --- | --- | --- |
| I01 | Servicio publicado y host validado | DTO público mínimo del tenant |
| I02 | Sede, servicio, intervalo y zona | Disponibilidad paginada y privada |
| I03 | Sesión de profesional y filtros propios | Sólo citas dentro de su alcance |
| I04 | Turnos, descansos y ausencias con rol autorizado | Versión auditable o rechazo |
| I05 | Recurso y capacidad por sucursal | Invariante server-side; rechazo de recurso ajeno |
| I06 | Solicitud de cita con clave idempotente | Estado explícito o conflicto transaccional |
| I07 | Confirmación posterior a política y disponibilidad | Resultado único, nunca éxito optimista |
| I08 | Reprogramación con versión y permiso | Nueva ocupación atómica o rechazo por conflicto |
| I09 | Cancelación autorizada | Liberación una sola vez, con auditoría |
| I10 | Consulta de referencia opaca propia | Estado mínimo, sin enumeración |
| I11 | UTC, zona IANA y transición DST | Hora inexistente rechazada, ambigua desambiguada |
| I12 | Reintento tras timeout | Mismo resultado para clave y payload iguales |
| I13 | Envío futuro de aviso tras commit | Evento verificado e idempotente |
| I14 | Consulta de auditoría autorizada | Filtro por tenant/rol y minimización |

Estos identificadores no son endpoints ni DTOs reales. M02 deberá elegir rutas, modelos, estados y almacenamiento tras revisar código, consumidores y permisos. Ningún escenario de esta tabla fue ejecutado.

## 4. Fronteras de confianza F01–F07, riesgos y propiedad de datos

- **F01, Browser → API:** EmpresaId, sucursal, servicio, duración, precio, reloj y disponibilidad provistos por cliente no conceden autoridad; backend resuelve tenant, permiso y datos canónicos. El host o encabezado forwarded no confiable no selecciona empresa B.
- **F02, API → Application:** JWT/RBAC/membresía, pertenencia y sede se revalidan por acción; público sólo ve proyecciones explícitamente publicadas; fallo de contexto implica denegar.
- **F03, Application → Domain:** transformar hora local mediante IANA validada a instante UTC, distinguiendo DST ambiguo/inexistente; no depender del navegador ni de `DateTime.Now`.
- **F04, Domain → Infrastructure:** futuras reservas de capacidad uno requieren exclusión atómica con transacción, índice/constraint o compare-and-swap; dos peticiones concurrentes jamás confirman el mismo recurso.
- **F05, Commit → notificación:** avisos futuros sólo con cita persistida y evento idempotente; no hay proveedor ni webhooks activados por este documento.
- **F06, Auditoría → soporte:** correlación y razón saneadas; proteger PII, teléfonos, tokens, datos de clientes de tenant B y exportaciones.
- **F07, UI → usuario:** foco, teclado, lector de pantalla, estado vacío/carga/error, 320px y baja conectividad; timeout no significa reserva confirmada.

La futura cita pertenece a una empresa y sucursal; deberá distinguir propuesta, confirmada, cancelada, ausencia y completada con cambios autorizados. Un empleado no recibe un rol global por gestionar horarios. Un campo de reserva de stock no puede ser reutilizado como lock temporal de agenda. **NO_GO** futuro si faltan evidencia A/B, protección IDOR, conflicto concurrente, zona DST o reversión idempotente.

## 5. Oráculos positivos y negativos futuros — NO_EJECUTADOS

| Par | Positivo futuro | Negativo futuro |
| --- | --- | --- |
| P01/N01 | Tenant A sólo consulta horarios publicados A | Host o cita B no divulga contenido B |
| P02/N02 | Profesional autorizado consulta agenda propia | Rol revocado o ajeno obtiene 403/404 seguro |
| P03/N03 | Capacidad uno admite una cita libre | Dos altas simultáneas no duplican ocupación |
| P04/N04 | Servidor aplica duración y buffers | Cliente no fuerza hueco reduciendo duración |
| P05/N05 | Instante UTC se presenta en zona IANA correcta | Hora DST inexistente/ambigua no pasa sin resolución |
| P06/N06 | Reprogramación autorizada actualiza una vez | Versión obsoleta o estado terminal no muta agenda |
| P07/N07 | Cancelación autorizada libera franja una vez | Replay no libera ni cancela dos veces |
| P08/N08 | UI móvil accesible refleja estado real | Timeout no muestra confirmación ficticia |

Casos negativos adicionales **N09–N14**: N09 IDOR de cliente B; N10 permiso revocado durante operación; N11 `EmpresaId`/sucursal falsificados; N12 misma idempotency key con payload distinto; N13 intervalo/zona/volumen fuera de límite; N14 auditoría, URL o exportación con JWT, PII o datos de tenant ajeno. Todos son **FUTUROS NO_EJECUTADOS**: no son PASS HTTP, SQL, xUnit, Angular, Docker ni Playwright.

## 6. Impacto, preflight, comprobaciones y rollback

**Preflight:** PLAN_MAESTRO H1155=PENDIENTE con I vacía antes del CAS exclusivo, COLA G1155/O1155=SI, DAG_TECNICO A1116 ID exacto y F1116=VALIDADO/G1116=SI. El antecedente verificó 60/60 preparaciones C01/C02/C06 M01.01–05; independencia únicamente documental. No se habilitan M01.06 terminal, M02/M03, JWT, base de datos, cobros, calendarios externos, migraciones o QA/main/PROD.

**Impacto real:** sólo este documento en `docs/v5` y una sección autocontenida de `CHANGELOG_AI.md`, sin cambiar código Domain/Application/Infrastructure/API/DTO, Angular, MySQL, secretos, autenticación, proveedores, jobs, Task schedule ni arquitectura.

**A0** comprobar plan, identidad, DAG, dependencias, diff Markdown y ausencia de secretos/encabezados vacíos. **A1** revisar A01–A14, I01–I14 y escenarios de diseño, nunca confundir oráculos futuros con pruebas ejecutadas. **A2** revisar límites JWT/RBAC/tenant A/B, PII, concurrencia, UTC/IANA y seguridad fail-closed sin certificar runtime. **A3** contrastar NR01–NR12 del antecedente y capas del repositorio; compilaciones .NET, Angular y MySQL/Playwright son `NO_APLICA` exclusivamente al diff Markdown, no PASS de producto. **A4** publicación a `dev` por commit fast-forward con lease CAS, verificación SHA/tree/blob y checks obligatorios `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` completed/success en SHA publicado, con readback PLAN/COLA/BITACORA. **A5** revisión posterior por otra Task Javier diferente del autor.

**Rollback:** `git revert` normal del commit documental y sección CHANGELOG, más evento compensatorio nuevo de BITACORA. No force-push, no borrar evidencia histórica y no promover QA/main/PROD.
