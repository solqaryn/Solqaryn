# SOLQARYN V5 — C08-03.M01.05: criterios de aceptación observables para membresías y acceso

**Autor:** PRIMARY DEV 02 / JAVIER_CHATGPT / TASK `6af16bb9492c8191b8e8c7d606f11507`. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, PLAN_MAESTRO fila 1230, C08-03 fase M01. **Repositorio:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT, 2026-11-14.

## Propósito y condición de evidencia

Preparar criterios funcionales y técnicos para futuras membresías comerciales de gimnasio, vigencia, suspensión, perfil, acceso y auditoría. **Criterios FUTUROS NO_EJECUTADOS**: no hay endpoint/torniquete/QR/gimnasio real certificado por esta microtarea. Preparación anterior: M01.01 blob `79bcd2aa2dc4c2f5cb8abdf243334d09ed6d347b` (NR01–NR14), M01.02 blob `6bfc9fd5f36a232f1d37be64cf00d9fb23de78d2` (interfaces), M01.03 blob `41dd66b36650f5e0e836038d0efe6905a1c9a423` (12 rutas reales), M01.04 blob `66fec276fbbc2c336d872eb4eb2af9a3d43d2dc4` (R01–R14, S01–S08, D01–D08 todavía PENDIENTES). Estos antecedentes no convierten en PASS sus tests futuros ni certifican A5 propio.

Arquitectura real inspeccionada: Angular 22.2.1, ASP.NET Core net10.0, EF Core 10.0.12, MySQL Oracle 10.0.9, JWT/RBAC/tenant. Se diferencia `UsuarioEmpresa` (membresía de empresa) de `Suscripcion` (plan SaaS) y de una membresía comercial hipotética; `Cliente.EmpresaId` nullable impide atribuir ownership histórico por inferencia. Este documento **no implementa** CRUD, esquema, pagos, workers, webhooks, lectores ni migraciones.

## Matriz de 14 criterios AC y 28 oráculos positivos/negativos FUTUROS

| Criterio | Caso positivo P01–P14 que una implementación debe demostrar | Caso negativo N01–N14 que debe rechazar |
| --- | --- | --- |
| AC01 | **P01** Titular de tenant A consulta sólo su propia membresía vigente | **N01** Identificador de B desde A nunca revela existencia ni contenido |
| AC02 | **P02** Recepción autorizada ve sólo miembros de su empresa y sede | **N02** Operador con empresa revocada o sede no vinculada recibe rechazo |
| AC03 | **P03** Alta autorizada crea estado explícito sin conceder acceso prematuro | **N03** Alta sin permiso o sin evidencia de ownership no activa miembro |
| AC04 | **P04** Plan de gimnasio se diferencia de plan de Suscripción SaaS | **N04** Cambiar plan SaaS no concede ni extiende derecho del gimnasio |
| AC05 | **P05** La vigencia se evalúa [inicio,fin) en UTC con IANA declarada | **N05** Reloj cliente alterado o DST ambiguo no extiende acceso |
| AC06 | **P06** Suspender revoca acceso inmediatamente después de confirmación | **N06** QR/cache previo a suspensión no conserva derechos |
| AC07 | **P07** Renovar con operación autorizada y prueba comercial válida es idempotente | **N07** Pago no verificado, callback falso o replay no activan vigencia |
| AC08 | **P08** Check-in en sede/recurso permitido registra un solo consumo | **N08** Dos solicitudes concurrentes no duplican entrada ni cuota |
| AC09 | **P09** La decisión de acceso aplica JWT/RBAC vigente en servidor | **N09** Permiso de lectura, JWT vencido o claim fabricado no autoriza acceso |
| AC10 | **P10** La identidad empresarial y comercial se comprueban separadamente | **N10** Membresía comercial activa no suple UsuarioEmpresa inactivo |
| AC11 | **P11** Se registra motivo/correlación con PII estrictamente mínima | **N11** Nunca se escriben secretos, JWT, QR durable o perfil ajeno en logs |
| AC12 | **P12** Dispositivo verificado recibe allow/deny de TTL corto si se autoriza | **N12** Lector offline/timeout nunca se interpreta como permiso |
| AC13 | **P13** UI accesible comunica estado y error sin conceder autorizaciones | **N13** Teclado, móvil 320px y lector de pantalla no pierden acceso a mensaje |
| AC14 | **P14** Reversión conserva integridad comercial y auditoría | **N14** Rollback no borra eventos ni migra/activa sistemas fuera del gate |

**Protocolo futuro:** especificar Arrange/Act/Assert con fixtures efímeros de empresas A y B, roles de solo lectura y operadores, miembro activo/suspendido/vencido, dos sedes, recursos, UTC/IANA, DST, reintentos, y concurrencia. Demostrar rechazo 401/403/404 seguro según contrato para acceso no autorizado, sin revelar la existencia de un objeto de B. Ninguna de las 28 pruebas está ejecutada aquí: son **futuros oráculos**, no un log de CI.

## Criterios no funcionales NFR01–NFR08

- **NFR01 Seguridad:** todos los accesos verifican JWT, UsuarioEmpresa activa, tenant/sede/permiso y membresía comercial en servidor.
- **NFR02 Privacidad:** no registrar ni enviar PII ajena, secretos, JWT, QR persistente o datos sensibles en mensajes de error.
- **NFR03 Precisión temporal:** frontera de expiración determinista UTC/IANA con DST cubierto, reloj cliente no confiable.
- **NFR04 Idempotencia y concurrencia:** renovar/check-in con una única transición atómica y conflictos explícitos.
- **NFR05 Resiliencia:** timeout, desconexión, caché obsoleta, lector offline y backend no disponible no aumentan derechos.
- **NFR06 Accesibilidad:** formularios y estados con etiquetas, foco visible, teclado y lectura a 320 px; Angular no decide permisos.
- **NFR07 Observabilidad:** razón/correlación opaca, actor y empresa resueltos, mínima retención y sin credenciales en logs.
- **NFR08 Rollback:** reversión aditiva/sin pérdida de auditoría con integridad de entradas y no despliegue automático.

## Secuencia de evidencia de una implementación posterior

| Área | Instrumentación obligatoria cuando cambie código | Resultado mínimo |
| --- | --- | --- |
| Domain/Application .NET 10 | Unit + contract tests positivos/negativos con permisos y estado | Reglas y denegación verificables |
| API/JWT/RBAC | Integration tests auth/tenant A-B, IDs manipulados y filtros | Nunca IDOR o privilegio transversal |
| EF/MySQL | Servicio MySQL efímero en CI, idempotencia y carrera de check-in | Un solo efecto confirmado por operación |
| Angular 22.2.1 | Build, unit + Playwright teclado/móvil y estados | UI accesible sin conceder derechos |
| Eventos/lector/pagos (sólo tras autorización nueva) | Pruebas firma, TTL, replay, offline, integridad | Rechazo seguro, cero cobros sin evidencia |
| GitHub | Check-runs del commit exacto, Scope Lock, seguridad y regresión pertinente | completed/success para pruebas requeridas |

**Puntos abiertos:** D01–D08 de M01.04 requieren resolución real antes de implementar los flujos que dependan de ellas. Una preparación M01 aprobada no libera M01.06/M02 ni un componente sensible. No asumir que un DAG G=SI es autorización de hardware, IAM, datos vivos o main/QA/PROD.

## Auditorías de esta unidad documental y rollback

**A0** contrastar HEAD/Skill/Scope Lock, texto de M01.01–04, COLA G/O=SI, DAG F=VALIDADO/G=SI y diff de dos Markdown. **A1** verificar matriz AC01–AC14, P01–P14/N01–N14 y ocho NFR con resultados esperados concretos, sin llamar tests ejecutados. **A2** constatar prohibición de tenant cross-over, JWT/RBAC/PII, QR, pagos, caché y privilegios por UI. **A3** mapear Domain/Application/API/Infrastructure/Angular y CI futura sin inventar dependencias implementadas. **A4** commit FF/CAS en dev, diff/blobs/readback, check Scope Lock y V5 provenance `completed/success` sobre SHA exacto; compilaciones .NET/Angular sin cambio de runtime se clasifican NO_APLICA, nunca PASS. **A5** sólo otra de las cinco Tasks Javier; la autora no puede aprobar.

Rollback documental con `git revert <SHA>` y evento BITACORA aditivo, nunca force-push. No se modifican entidades, endpoints, migraciones, datos, UI, trabajadores, hardware, proveedores, secretos, programaciones ni QA/main/PROD.
