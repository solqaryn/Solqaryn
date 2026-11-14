# SOLQARYN V5 — C08-03.M01.02: actores, interfaces y fronteras de membresías

**Autor:** PRIMARY DEV 02, JAVIER_CHATGPT, Task 6af16bb9492c8191b8e8c7d606f11507. **Unidad:** C08-03.M01.02, PLAN_MAESTRO fila 1227, C08-03 Membresías y acceso de gimnasio, fase M01. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA). **Repositorio:** solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT. Fecha 2026-11-14.

## Naturaleza de este contrato y trazabilidad

Documentar **entradas, salidas, actores y superficies afectadas** de futuros planes de membresía, vigencias, accesos, suspensiones y perfil de gimnasio. El requisito final del bloque incluye control de acceso, expiración y auditoría. Este documento no certifica que esas operaciones existan. Es preparación, sin cambios de código, endpoints, hardware, datos, integraciones, permisos, cobros, Workers, Tasks, QA/main/PROD ni infraestructura.

Antecedente propio C08-03.M01.01: docs/v5/c08-03-m01-01-alcance-membresias-acceso.md, blob Git 79bcd2aa2dc4c2f5cb8abdf243334d09ed6d347b, SHA dev publicado 90c944e0fb7b0e6aa5be03630b5f3a955ce08b2e, con NR01–NR14 y P01–P06/N01–N14 FUTUROS NO_EJECUTADOS. La aprobación A5 de ese antecedente es independiente y no se presume. El DAG de M01.02 exige su propio F=VALIDADO/G=SI; no convierte el antecedente en requisito de permisos o activación del runtime.

## Actores y superficies: A01–A14

| Actor / superficie | Entrada prevista | Salida o decisión esperada y límite |
| --- | --- | --- |
| A01 Titular de la membresía de gimnasio | sesión verificada, recurso propio y acción permitida | resumen mínimo propio; nunca perfil de empresa ajena |
| A02 Operador de recepción | credencial empresarial, permiso específico, referencia de miembro | validar check-in dentro del tenant y sede autorizada |
| A03 Supervisor de gimnasio | permiso empresarial explícito, selección de sede legitimada | revisar incidencias sin poder conceder rol global |
| A04 Administrador comercial autorizado | contrato y datos de plan limitados a empresa propia | definir vigencia y suspensión con auditoría e invariantes |
| A05 Cliente Angular móvil/web | UI, evento de usuario, estado local no confiable | mostrar estados accesibles, no decidir autorización |
| A06 Router/guard Angular | token/sesión y navegación | UX preliminar; backend vuelve a verificar cada acción |
| A07 API ASP.NET Core 10 | JWT, ruta, DTO y headers NO confiables | autenticar, validar membresía empresarial, negar por defecto |
| A08 Application Service futuro | contexto de tenant y actor verificados, operación tipada | orquestar evaluación de derecho de uso sin confiar en cliente |
| A09 Domain futuro | plan, intervalo, suspensión, recurso/uso | evaluar invariantes puras sin infraestructura ni fecha client-side |
| A10 Infrastructure/MySQL futuro | consulta tenant-scoped, transacción autorizada | impedir IDOR, carreras y dobles usos, solo con migración aprobada |
| A11 Dispositivo/lector externo futuro | credencial opaca de duración definida, solicitud de acceso | nunca abrir automáticamente en offline; hardware NO habilitado |
| A12 Proveedor de cobro futuro | webhook/verificación de pago con fuente y firma verificadas | evento idempotente solo tras autorización, ninguna compra ahora |
| A13 Auditoría/observabilidad | motivo, timestamp UTC, correlación, tenant resuelto | registro minimizado, no incluir JWT, QR, PII ni secreto |
| A14 Operador de soporte autorizado | motivo/incidencia y rol con mínimo privilegio | resolver sin asumir impersonación ni leer perfil de otros tenants |

Ninguna de estas filas asegura que el actor o endpoint esté implementado. La identidad empresarial y sus roles no se derivan de un plan comercial de gimnasio; un plan de gimnasio tampoco concede permisos administrativos de SOLQARYN.

## Contratos de entradas y salidas futuros I01–I14 (NO IMPLEMENTADOS)

| Interfaz | Entrada mínima que deberá validarse | Salida / rechazo esperado |
| --- | --- | --- |
| I01 Consultar mi membresía | identidad JWT, contexto tenant server-side, propósito | plan/estado/vigencia mínimos propios, sin divulgar empresa B |
| I02 Consultar miembro autorizado | selector opaco y permiso de lectura de recepción | perfil mínimo tenant-scoped, 403/404 seguro si no autorizado |
| I03 Registrar plan | actor empresarial permitido, condiciones y alcance de sede | versión de plan y auditoría; rechazo de permisos ajenos |
| I04 Alta de membresía | usuario final verificado, plan vigente, contrato, sede | membresía en estado explícito, sin activación por UI |
| I05 Evaluar derecho de acceso | recurso/sede, instante UTC confiable, identidad resuelta | allow/deny tipado con reason code seguro y TTL acotado |
| I06 Check-in atómico | derecho validado, clave idempotente, política de uso | un consumo confirmado por transacción; no doble registro |
| I07 Suspender/revocar | motivo, rol autorizado y revisión de estado actual | negar inmediatamente nuevos accesos; evento de auditoría |
| I08 Renovar | solicitud comercial + prueba de pago si aplica | nueva vigencia idempotente, no activar por callback no verificado |
| I09 Consultar historial de acceso | alcance propio, paginación, filtro permitido | eventos minimizados y tenant-scoped, nunca exportación global |
| I10 Reportar incidencia | referencia opaca, actor y motivo | correlación de soporte sin revelar credencial o contenido privado |
| I11 Verificar zona y expiración | instante UTC server-side y zona IANA configurada | decisión determinista para límite de vigencia/DST |
| I12 Invalidar credencial | cambio de estado/rol/revocación | token/cache anterior sin derechos residuales |
| I13 Consumir solicitud de dispositivo | credencial de un solo alcance + identidad/tenant + TTL | verificación remota y rechazo offline cuando no comprobable |
| I14 Conciliar auditoría | acciones con correlación/sede/estado | trazabilidad sin registrar bytes, QR permanente o PII sensible |

**DTOs y URLs anteriores son contratos conceptuales**, no nuevas rutas reales. Determinar nombres, esquemas, tipos, enums y errores en M02 después de inspeccionar consumidores actuales y el estado real del código.

## Fronteras de confianza, datos y condiciones de no-regresión

**F01 Browser→API:** no confiar en EmpresaId, UID, fecha, sede, permiso, estado o cobro que envía el cliente. **F02 API→Application:** contexto empresarial/JWT/RBAC autenticado, rol vigente y operación concreta; negar en ausencia de evidencia. **F03 Application→Domain:** autorización ya comprobada, reglas de vigencia con UTC/IANA y estado explícito; ninguna regla depende de reloj local del dispositivo. **F04 Domain→Infrastructure/MySQL:** consultar y escribir únicamente por tenant con concurrencia, integridad y transacción; diseño futuro, migraciones NO autorizadas. **F05 API→dispositivo:** TTL, replay, revocación y disponibilidad fail-closed; no acceso offline automático. **F06 pago→membresía:** evento idempotente con firma y prueba de pago real; sin pasarelas, cargos ni webhooks hoy. **F07 registros→soporte:** correlación opaca, auditoría mínima y acceso autorizado, jamás exponer tokens/PII.

Clasificar como **NO_GO** de implementación futura si no puede probarse segregación A/B, ausencia de IDOR, expiración con zona/DST, revocación inmediata, idempotencia, autorización por rol/sede, logs sanitizados y rollback. M01.02 no autoriza M01.06 terminal, M02, migraciones, nuevos tenants, hardware, Workers, QA/main/PROD ni pagos.

## Pruebas positivas futuras NO_EJECUTADAS

P01: titular A consulta su perfil vigente y no obtiene información de otra empresa. P02: recepción autorizada realiza un único check-in dentro de plan y sede vigentes. P03: suspensión autorizada invalida acceso y deja razón auditada. P04: renovación verificada idempotente cambia vigencia una sola vez. P05: comprobación de expiración en UTC/IANA es reproducible a través de DST. P06: UI Angular informa estado y rechazos con teclado, lector de pantalla y 320px.

## Pruebas negativas futuras NO_EJECUTADAS

N01: A consulta selector de B; N02: mutar EmpresaId/slug o sede; N03: rol lectura suspende o renueva; N04: JWT vencido; N05: membresía comercial activa pero empresa revocada; N06: usuario suspendido usa credencial cacheada; N07: plan vencido cambia hora client-side; N08: DST ambiguo al límite; N09: replay de check-in; N10: idempotency key con payload diferente; N11: dispositivo offline sin prueba; N12: callback de pago no firmado concede acceso; N13: exportación cruza tenant; N14: auditoría contiene QR, JWT, secreto o PII; N15: intento de activar hardware, migración o webhook por este documento. En todos los casos el contrato futuro requiere **denegación segura, sin acceso ni filtración**.

P01–P06 y N01–N15 son diseños de prueba, **no resultados ni invocaciones a MySQL, API, Playwright, proveedor o dispositivos**. Las pruebas reales se realizarán únicamente en microtareas autorizadas de código con CI dirigida del SHA exacto.

## Impacto, auditoría y reversión M01.02

**Impacto real:** agregar un solo documento docs/v5 y una sección intacta a CHANGELOG_AI.md. Sin cambios Domain/Application/Infrastructure/API/DTO, Angular, bases, seguridad activa, secretos, pagos, webhooks, tareas, producción ni migraciones. Riesgo principal: confundir contrato con permiso de implementación; mitigación = NO_EJECUTADAS, NO_GO y A5 independiente.

**A0** verificar plan V5, ID, DAG F/G, autor, alcance y ausencia de secretos. **A1** revisar A01–A14, I01–I14 y P/N futuros observables (no tests). **A2** documentar tenant A/B, JWT/RBAC, expiración, suspensión, replay y logs sin cambiar seguridad activa. **A3** contrastar NR01–NR14 del antecedente, arquitectura vigente y fuentes del catálogo; runtime .NET/Angular/MySQL/Playwright es NO_APLICA a Markdown, no PASS. **A4** publicar solo dev por fast-forward CAS de HEAD, verificar Scope Lock y V5 provenance del SHA publicado con completed/success, comparar blobs y PLAN/COLA/BITACORA. **A5_PENDIENTE** para otra Task Javier distinta a autora.

**Rollback:** git revert normal del commit documental y de su entrada de CHANGELOG, sin borrar BITACORA ni reescribir historia. Las otras microtareas y QA/main/PROD conservan sus gates propios.
