# SOLQARYN V16 — C08-01.M02.06: dependencias, gates y contrato de salida para citas y agenda

**ID:** C08-01.M02.06, PLAN_MAESTRO fila 1165. **Autor:** PRIMARY DEV 06 JAVIER, `6af16bbad26881918c283abe06e1f32a`. **Riesgo:** R0 documental, `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL en Google Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; PLAN/COLA/DAG institucionales. Esta unidad especifica salidas y dependencias para futura agenda/citas, **no activa reservas, calendario, worker, email, API, MySQL, frontend, pagos o proveedor**.

## 1. Dependencias de M01 y M02, sin atajos técnicos

- M01.01–06 LISTO+A5 independiente por cada precedente; M02.01–05 LISTO+A5 real. Al reclamar: PLAN H1165=PENDIENTE, owner vacío, COLA G/O=SI y DAG F=VALIDADO/G=SI. Se reevalúa tras cada transición, no se congela disponibilidad histórica.
- M02.01 propone servicio, sucursal, profesional, recurso, jornada, bloqueos, citas y ownership tenant; doce conceptos P de diseño no son tablas desplegadas. M02.02 propone API/DTO y errores sin endpoints ejecutados. M02.03 P01–12, IDX01–12 y MIG01–08 son persistencia/índices/migraciones hipotéticos, sin tocar datos. M02.04 OPT01–18, FL01–12, VAL01–16 fija flags restrictivos deshabilitados por defecto. M02.05 EV01–12, IDEM01–14 y COR01–12 son eventos y oráculos conceptuales, no eventos emitidos.
- Una entrega R0 puede preparar documentación M03, pero **nunca** concede autorización para escribir inventario, programar citas reales, sincronizar calendarios, activar email, habilitar API, implementar workers, migrar tablas o desplegar QA/main/PROD por sí sola.
- Fuente de autoridad: backend ASP.NET Core 10 verifica JWT, membresía/roles, revocación, tenant A/B, sucursal, profesional, cliente y propiedad del recurso. Angular solo presenta UX. Una zona horaria elegida en navegador no determina identidad o autorización.

## 2. Dieciséis gates de salida G01–G16 FUTUROS_NO_EJECUTADOS

| ID | Regla requerida para código real | NO_GO si |
| --- | --- | --- |
| G01 | SHA y alcance autorizados por Scope Lock | Se publica en QA/main/PROD o cambia proveedor sin permiso |
| G02 | Actor autenticado, rol actual y revocación verificados | Permisos heredados, rol expirado o usuario de otra empresa |
| G03 | Tenant A/B y sucursal resueltos desde backend y DB | Slug, query, header o cookie cliente concede acceso |
| G04 | Servicio, profesional, recurso y jornada tienen owner persistido | Identificadores cruzan empresa/sucursal o entidad inactiva |
| G05 | Fechas usan UTC y zona IANA versionada/consistente | DST ambiguo/inexistente se convierte en doble reserva |
| G06 | Capacidad y disponibilidad verificadas transaccionalmente | Consulta de UI se interpreta como lock de un slot |
| G07 | Idempotencia por tenant, actor, comando y fingerprint | Reintento crea dos citas o misma clave cambia contenido |
| G08 | Exclusividad de reserva por recurso/intervalo | Dos writers/conflictos no detectados sobreocupen |
| G09 | Transición de estado y cancelación son atómicas y auditables | Horario se libera sin cerrar cita o queda huérfano |
| G10 | Reprogramación preserva slot antiguo hasta adquirir nuevo | Cambio parcial pierde ambas reservas |
| G11 | Reglas comerciales, plazos, buffers y máximos validados | Duraciones negativas, overflow, calendario ilimitado |
| G12 | API privada rechaza fuga de PII y enumeración | Disponibilidad revela usuario, cita o recurso ajeno |
| G13 | Proveedores externos/email/worker son opt-in y aprobados | Webhook, SMTP o calendario se activa por documento/flag |
| G14 | MySQL 8.4 efímero valida EF, índices, transacciones y rollback | Solo se ejecuta Category!=Integration o build verde |
| G15 | CI/seguridad/ScopeLock/V5 corresponden al SHA causal | FAILED/CANCELLED/SKIPPED/IN_PROGRESS declarado PASS |
| G16 | A0–A4, A5 independiente y DAG real acreditados | No hay readback, reviewer distinto o evidencia testable |

Los gates definen condiciones para una fase ejecutable futura. **No se han ejecutado** migraciones, tests transaccionales, consultas SQL ni escenarios de navegador en este documento.

## 3. Ocho planes de reversión RB01–RB08 (no ejecutados)

| ID | Acción futura, condicionada a permisos y entorno |
| --- | --- |
| RB01 | Fijar SHA, manifest, dependencias M02/M03, owner, estado y alcance del cambio |
| RB02 | Ejecutar backup y restore efímero con datos sintéticos de tenants A/B |
| RB03 | Validar transacciones, unique keys, índices y locks con MySQL 8.4 aislado |
| RB04 | Probar reversibilidad de migraciones expand-contract sin perder citas ni historial |
| RB05 | Cancelar propuestas/flags sin borrar reservas legítimas |
| RB06 | Reconciliar recursos y ocupación por empresa, sucursal, profesional y UTC |
| RB07 | Repetir negativos 401/403, revocación, concurrencia y DST tras revert |
| RB08 | Registrar incidentes/CI/autor/revisor, hacer `git revert` normal y readback |

No ejecutar rollback de proveedor ni producción desde una Task documental. Para este cambio Markdown sólo corresponde revert Git normal, conservando BITACORA.

## 4. Diez pruebas positivas/negativas T01–T10 (0/10 FUTUROS_NO_EJECUTADOS)

| ID | Positivo futuro | Negativo, frontera o fallo futuro |
| --- | --- | --- |
| T01 | Empresa A reserva su servicio/sucursal autorizados | Empresa B o rol revocado intenta leer/crear cita de A |
| T02 | Dos solicitudes concurrentes compiten y solo una gana | Dos confirmaciones sobre mismo slot/recurso son FAIL |
| T03 | Misma clave/fingerprint reutiliza resultado | Igual clave con payload distinto se rechaza |
| T04 | Zona IANA maneja fecha/hora válidas | Hora inexistente/duplicada DST se rechaza o desambigua |
| T05 | Buffer/duración/capacidad están dentro de límites | Negativo, overflow, horizonte ilimitado se rechaza |
| T06 | Reprogramación intercambia slots atómicamente | Fallo intermedio conserva cita previa, sin pérdida |
| T07 | Cancelación única libera recurso una vez | Repetición/replay no duplica liberación ni borra auditoría |
| T08 | API no expone cliente ni recurso ajeno | Enumeración o cross-tenant produce 401/403 seguro |
| T09 | Integración MySQL 8.4 EF con transacción es verificable | Test filtrado sin Category=Integration no certifica |
| T10 | CI exact-SHA PASS y readbacks habilitan revisión | CANCELLED/SKIPPED/IN_PROGRESS nunca libera sucesora |

Estos son **oráculos FUTUROS_NO_EJECUTADOS, 0/10**. No afirmar tests xUnit, Playwright, browser, MySQL, HTTP, pagos o proveedores aprobados. Métricas de desempeño, cancelación y satisfacción = **NO_MEDIDO**.

## 5. A0–A5, riesgos y siguiente frontera

**A0:** revisión de contrato V5 y M01/M02, gates 16, reversas 8 y T01–T10; diff de dos archivos Markdown únicamente. **A1:** coherencia de criterios positivos/negativos *futuros*, sin atribuir ejecución funcional. **A2:** separación A/B, JWT/RBAC, revocación, ownership, DST, confidencialidad, idempotencia y bloqueo de sobrecupo declarados sin modificar runtime. **A3:** .NET/MySQL/API/Playwright/SMTP/worker: `NO_APLICA` al diff R0; requeridos con pruebas verdaderas cuando M03 programe código. **A4:** publicar en dev por FF/CAS, verificar Scope Lock y procedencia V5 exact-SHA, readbacks PLAN/COLA/DAG/BITACORA, sin tomar CI cancelada como PASS. **A5:** revisión de Task Javier distinta, independiente de la autora, capaz de registrar A5_FAIL; M03 no se libera por LISTO sin A5.

**Rollback de esta unidad:** `git revert` normal después de verificar HEAD y registrar evidencia sin modificar historia BITACORA. No se habilitó una agenda funcional, contratos HTTP, proveedores, escrituras MySQL, secretos, DNS ni QA/main/PROD.
