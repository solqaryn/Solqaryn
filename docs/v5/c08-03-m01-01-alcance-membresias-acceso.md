# SOLQARYN V5 — C08-03.M01.01: objetivo, no-alcance y no-regresión de membresías y acceso

**ID y fuente:** C08-03.M01.01, PLAN_MAESTRO fila 1226, ámbito C08-03 — Membresías y acceso, M01 preparación. STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA. **Autor:** PRIMARY DEV 02 / JAVIER_CHATGPT / Task 6af16bb9492c8191b8e8c7d606f11507. **Rama:** solqaryn/Solqaryn@dev; PROJECT_SCOPE_LOCK=STRICT. Fecha 2026-11-14.

## Objetivo verificable de la preparación

Delimitar el contrato futuro de planes de membresía de gimnasio, vigencia, suspensión, identidad de miembro, perfil y control de acceso a recursos del establecimiento. La evidencia de fases posteriores deberá demostrar que ninguna credencial, identidad, plan o fecha de expiración permite saltar la membresía empresarial verificada, acceder a otra empresa o recuperar acceso después de suspensión, vencimiento o revocación. Los resultados mínimos exigidos por el plan son **control de acceso, expiración y auditoría**.

**Alcance exacto de M01.01:** fijar objetivos y exclusiones, invariantes observables y escenarios futuros; no es implementación ni certificación de accesos físicos. Las fases M01.02–05 inventariarán actores/contratos, fuentes reales, riesgos y aceptación. M01.06 tiene un gate separado y no queda autorizado por M01.01. El sistema actual se describe por su arquitectura publicada, no se presupone que exista un módulo de gimnasio, torniquete o proveedor de cobro.

## Fronteras y principios arquitectónicos

- El frontend Angular 22.2.1 (Signals/Material) muestra estados y accesibilidad; nunca concede privilegios. ASP.NET Core 10 en API/Application verifica autenticación, contexto tenant y operación. Domain declara reglas; Infrastructure aislaría integraciones/EF Core/MySQL si una fase posterior fuese autorizada.
- Diferenciar la **membresía de la empresa/tenant**, que determina autorización al ERP, de la **membresía comercial del gimnasio**, que habilitaría servicios concretos de un cliente. Una no sustituye a la otra. Roles de plataforma o claims aportados por el cliente no conceden recursos empresariales.
- No confiar en empresa, gimnasio, persona, sucursal, zona horaria, plan, estado de pago ni fecha enviados desde browser o dispositivo. Resolver identidad y tenant desde autenticación y relaciones server-side verificadas.
- Toda decisión de acceso futura deberá ser fail-closed y auditada con correlación opaca, sin PII excesiva, secretos, token QR sin protección ni credenciales en logs. La condición de pago, si corresponde, exige fuente transaccional autorizada; no inferir abonos ni activar cobros.
- Dependencias previas documentales según DAG dinámico: C01-01/02/03/04, C02-01/02/03/04, C04-01 y C06-01/02/03/04, unidades M01.01–05; el gate F=VALIDADO/G=SI habilita solo esta **preparación documental**. No autoriza migraciones, validación live, IAM/JWT/RBAC, hardware, QA, main ni PROD.

## Invariantes NR01–NR14 de no-regresión — contratos futuros

| ID | Regla de aceptación esperada | Condición de rechazo / NO_GO |
| --- | --- | --- |
| NR01 | Toda solicitud resuelve identidad y tenant activos en servidor | Cliente decide EmpresaId, gimnasio o propietario |
| NR02 | Membresía empresarial y sus permisos se verifican antes de consultar perfil de gimnasio | Un abono personal sustituye permisos JWT/RBAC |
| NR03 | Un cliente de tenant A nunca ve ni usa la membresía, plan o ingreso de B | Consulta global, IDOR o caché sin segmentación |
| NR04 | Plan y derechos son específicos del tenant y de la persona | Copiar plan, QR o credencial a otro miembro |
| NR05 | Fecha inicio, fin y zona horaria tienen semántica declarada | Expiración por comparación de cadenas o reloj cliente |
| NR06 | El instante límite de vigencia se interpreta sin ambigüedad UTC/IANA | Acceso indebidamente extendido en DST o cambio de huso |
| NR07 | Suspensión, revocación y baja bloquean nuevos accesos desde confirmación | Token o caché mantiene derechos anteriores |
| NR08 | Renovación exige prueba transaccional autorizada e idempotencia | Reintento cobra/activa varias veces o concede sin pago |
| NR09 | Permisos de lectura, alta, cambio, suspensión y acceso son distintos | Usuario consulta y por ello puede reactivar membresías |
| NR10 | Personas y recursos de múltiples sucursales se restringen por autorización | Membresía de sede A abre recursos no contratados de B |
| NR11 | Entradas concurrentes y reintentos se gobiernan sin doble uso inadvertido | Dos accesos simultáneos violan límite de uso |
| NR12 | Cada decisión de acceso se audita con acción, actor, motivo y correlación mínima | Logs revelan datos clínicos, PII o credencial de acceso |
| NR13 | Errores de conexión o proveedor aplican política segura | Offline se interpreta automáticamente como permiso |
| NR14 | Ninguna configuración documental activa hardware, QR, PSP o infraestructura | Salida M01 usada como orden para producción |

## Supuestos por comprobar, no hechos certificados

S01: existe un catálogo de miembros por empresa o deberá diseñarse sin reutilizar identidades impropias. S02: los planes pueden restringir sede, horario, recurso o número de usos; definirlo en M02. S03: habrá un origen de tiempo server-side y zona IANA configurada con pruebas de DST. S04: la relación entre pago y vigencia requiere definición explícita por el propietario. S05: equipos/lectores externos no están autorizados ni inventariados. S06: cualquier tratamiento de PII debe ajustarse a roles reales y minimización. Los seis supuestos permanecen **PENDIENTES** hasta cotejo de código y decisión posterior; no se crean entidades, permisos o cuentas por esta nota.

## Escenarios positivos FUTUROS NO_EJECUTADOS

- P01: miembro de empresa A con permisos y plan activo accede solo al recurso autorizado dentro del intervalo.
- P02: consulta del titular legítimo devuelve únicamente estado y vigencia de su propia membresía, sin PII ajena.
- P03: acceso vence exactamente en instante límite acordado y funciona para zonas horarias distintas.
- P04: suspensión y posterior reactivación autorizada actualizan permisos de manera coherente, con auditoría.
- P05: reintento idempotente de renovación válida deja un único efecto comercial observable.
- P06: UI móvil/teclado comunica estado activo, vencido y suspendido con foco y textos accesibles.

## Escenarios negativos FUTUROS NO_EJECUTADOS

- N01: usar token o ID de B desde tenant A: rechazo sin datos B.
- N02: cambiar EmpresaId/gimnasio/sucursal/slug en header, body o URL: rechazo por contexto confiable.
- N03: miembro vencido solicita acceso: rechazo aun si el browser muestra estado antiguo.
- N04: miembro suspendido utiliza QR cacheado: rechazo y auditoría mínima.
- N05: usuario sin JWT donde sea obligatorio solicita permiso: 401/403 según contrato final.
- N06: rol de lectura intenta suspensión, renovación o alta: rechazo por operación.
- N07: terminal modifica fecha/plan/estado pago: no aumenta derechos.
- N08: hora local ambigua en cambio DST o zona inexistente: denegación segura/no extensión.
- N09: doble solicitud concurrente supera cupo de entradas: no concede doble uso.
- N10: mismo identificador idempotente con payload distinto: conflicto seguro.
- N11: lector offline o timeout de verificación: no interpretar disponibilidad como autorización.
- N12: revocación de membresía empresarial deja una comercial activa: sin acceso ERP cross-tenant.
- N13: log/URL/QR contiene PII, token durable o firma secreta: no exponer ni registrar.
- N14: solicitud de activar pasarela de pago, torniquete, R2, migraciones o producción desde M01: NO_GO.

Los escenarios P01–P06 y N01–N14 se proponen como **oráculos futuros**, no son tests ejecutados; exigir luego integración real .NET/MySQL/JWT/RBAC y pruebas A/B/Playwright según los cambios implementados, con SHA exacto y datos efímeros.

## Alcance negativo de este changeset, pruebas y rollback

**Exclusiones:** no añadir ni editar Domain/Application/Infrastructure/API, DTO, rutas Angular, migraciones, tablas MySQL, seguridad activa, permisos, endpoints, lectores, QR, pagos, SDK, secretos, infraestructura, funciones programadas, eventos/webhooks, QA/main/PROD. No se declara gimnasio implementado, dispositivos activos ni pruebas de acceso físico realizadas.

**A0** revisar fuentes V5, Skill/HEAD, Scope Lock, DAG y diff solo dos Markdown sin datos sensibles. **A1** comprobar que los objetivos, NR01–NR14 y escenarios futuros son concretos y se distinguen de pruebas reales. **A2** verificar por revisión documental la preservación JWT/RBAC, aislamiento A/B, UTC/DST, revocación y privacidad; sin PASS de seguridad runtime. **A3** conciliar este contrato con arquitectura y predecesores documentales; .NET/Angular/MySQL/Playwright de runtime = NO_APLICA a Markdown, nunca PASS inventado. **A4** publicación fast-forward por CAS en dev; comprobación exact-SHA de verify-solqaryn-project-scope y procedencia CHANGELOG V5 en completed/success; lectura de PLAN/COLA/BITACORA y exclusividad. **A5** queda PENDIENTE para otra Task Javier diferente a autora, sin autoaprobación.

**Rollback:** git revert regular del commit que agregue esta especificación y su sección de CHANGELOG, conservando BITACORA y sin force-push ni afectar otros cambios. Ningún gate de M01.06, M02, QA/main/PROD se libera por este cierre.
