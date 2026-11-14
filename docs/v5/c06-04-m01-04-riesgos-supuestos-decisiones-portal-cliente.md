# SOLQARYN V5 — C06-04.M01.04: riesgos, supuestos y decisiones pendientes del portal de cliente

**Microtarea:** C06-04.M01.04, PLAN_MAESTRO fila 977. **Autor:** PRIMARY DEV 05 JAVIER, TASK_ID `6adc2e55f47c81919f6374e561687076`. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Repositorio:** solqaryn/Solqaryn@dev; PROJECT_SCOPE_LOCK=STRICT. **Fecha:** 2026-11-14.

## Alcance, procedencia y clasificación

Este documento identifica **riesgos potenciales, supuestos que deben probarse y decisiones aún no adoptadas** para la futura integración C06-04: cuenta de cliente, recuperación, pedidos, reservas, membresías, documentos, preferencias y comunicaciones. No se reportan incidentes observados ni se autorizan tecnologías, permisos, entidades de datos o despliegues por mera inclusión en una tabla.

**Línea base comprobada:** C06-04.M01.01 blob `afa54d6939df46719cfe89a9f3b657b2bce38f03` define NR01–NR12; M01.02 blob `d0fc169736e28f93128b10b7c15f32406e1eb0a1` describe A01–A12, I01–I12 y B01–B08; M01.03 blob `163402f99029b7371c5ed898562467267e6c1a7a` inventaría S01–S16 y G01–G12. `frontend/src/app/features/storefront/storefront-cuenta.service.ts` blob `34e245d5db4c534fb2d942356c697f47d2224969` ya expone llamadas a registro/login, direcciones, favoritos, pedidos y notificaciones, con sesión storefront separada del ERP. `frontend/src/app/core/auth/tenant-context.service.ts` blob `63a711355495d2f80efba35bffb2b8727401154a` exige revalidación server-side para la selección ERP. Ninguno acredita por sí solo seguridad HTTP, recuperación, membresía o descargas de portal ya terminadas.

Las observaciones siguientes son **hipótesis de riesgo o brechas de certificación**, no afirmaciones de que existan vulnerabilidades explotables. El trabajo aquí cambia únicamente Markdown, sin pruebas live, endpoints, notificaciones ni migraciones.

## R01–R12 — registro de riesgos potenciales

| ID | Riesgo hipotético | Control previsto y evidencia causal requerida | Severidad de diseño |
| --- | --- | --- | --- |
| R01 | Sesión del cliente se confunde con JWT/RBAC empresarial | Modelos de principal separados, negativos cliente→ERP y test de elevación de rol | Alta |
| R02 | Se usa tenant de URL, header o storage como autoridad | Backend resuelve contexto por ownership; prueba A/B, host manipulado y denegación | Crítica |
| R03 | Cliente X ve perfil/pedido de Y en mismo tenant | Policy de propiedad por recurso y pruebas IDOR negativas | Crítica |
| R04 | Registro o recuperación enumera correos/cuentas | Respuesta uniforme, rate limit y oráculos de tiempo/estado sin fuga | Alta |
| R05 | Token de recuperación reutilizable, largo o expuesto | Token opaco, un uso, TTL, hash en reposo y revocación; logs sin secretos | Crítica |
| R06 | Recibo del browser se toma como pago o pedido confirmado | Fuente autorizada backend, estado transaccional y pruebas de falsificación | Alta |
| R07 | Reservas duplican efectos por concurrencia/retry | Clave idempotente, locking lógico/DB y rollback probado | Alta |
| R08 | Membresía caducada conserva permisos o beneficios | TTL, verificación de estado vigente server-side y revocación | Alta |
| R09 | Documento/adjunto se descarga por URL adivinable | Ownership por tenant/cliente, firma corta, auditoría y tests de abuso | Crítica |
| R10 | Preferencias confunden compra con consentimiento de marketing | Opt-in granular y revocación por finalidad, no inferencias implícitas | Media |
| R11 | Caché/UI conserva información de otro cliente tras cambio de sesión | Limpiar sesión/estado, cache-key por identidad/tenant, pruebas A/B | Alta |
| R12 | Trazas, email o proveedor revelan PII, JWT o claves | Minimización/redacción, rate limit, ACL y pruebas de logging/plantillas | Alta |

Las severidades son **prioridades de modelado**, no hallazgos medidos. El dueño técnico deberá confirmar impacto y controles por módulo antes de que exista código de esa fase.

## S01–S08 — supuestos que requieren prueba

| ID | Supuesto no certificado | Forma de falsarlo |
| --- | --- | --- |
| S01 | La sesión storefront vigente se limita a consumidor y no concede roles ERP | Intentar con la misma sesión endpoints admin; verificar denegación en backend |
| S02 | Cada perfil de cuenta cliente se vincula unívocamente al tenant aprobado | Dos empresas con identificadores superpuestos no comparten perfil |
| S03 | Un pedido de tienda se vincula solo a su titular real | Usuario B intenta consultar un ID de A dentro del mismo tenant |
| S04 | La recuperación puede integrarse sin debilitar el flujo de login existente | Token expirado, repetido, fuerza bruta y respuesta no enumerativa |
| S05 | Reservas y membresías admiten idempotencia y estado vigente por cliente | Reintentos simultáneos, cancelación, expiración y eventos fuera de orden |
| S06 | Almacenamiento de documentos permite URL temporal por propiedad | Descargas cruzadas, enlaces expirados y cambio de sesión |
| S07 | Consentimiento por finalidad se representa sin reutilizar datos de ERP | Marketing no autorizado tras compras o registro; revocación verificable |
| S08 | La experiencia móvil y el aislamiento se prueban sin datos reales | Fixtures A/B sintéticos, navegación 320 px, teclado y foco |

Si falla un supuesto, registrar diagnóstico verificable y volver al diseño del microalcance pertinente; nunca liberar automáticamente un gate posterior por este inventario.

## D01–D08 — decisiones PENDIENTES, sin aprobación tácita

| ID | Decisión futura pendiente | Rol propuesto para dictamen (NO es aprobación) | Gate antes de implementar |
| --- | --- | --- | --- |
| D01 | Fuente única de identidad de cliente y separación de usuario ERP | Arquitectura + responsable de seguridad | Diagrama de principal y tests de no elevación aprobados |
| D02 | Relación cuenta-cliente-tenant-sucursal y ownership de pedidos | Dominio/Application + seguridad multiempresa | Matriz A/B y X/Y, contratos y casos negativos verificados |
| D03 | Protocolo de recuperación, expiración y invalidación de sesiones | Seguridad + propietario de producto | Diseño de tokens/rate limit/telemetría con datos mínimos |
| D04 | Estado/autorización de reservas, idempotencia y concurrent writes | Domain + responsable de integración | Contrato transaccional y rollback de pruebas efímeras |
| D05 | Membresías, beneficios y vencimientos verificables | Producto/comercial + seguridad | Reglas comercialmente aprobadas y policy fail-closed |
| D06 | Documentos de cliente y URLs firmadas por tenant | Seguridad + responsable de almacenamiento | TTL, permisos, clave y test de expiración sin datos productivos |
| D07 | Preferencias, consentimiento, auditoría y retención | Producto + privacidad/legal | Texto/versión/finalidad con aprobación explícita pertinente |
| D08 | Política de despliegue, monitoreo y rollback por fases | Arquitectura + operación autorizada | CI exact SHA, seguridad A/B, A5 y autorización de entorno separada |

**Estado de todas D01–D08: PENDIENTE.** No se asignan responsabilidades personales, no se concede un permiso comercial o legal, no se crean trabajos entre Tasks, no se ejecutan servicios ni se contratan proveedores.

## Pruebas de diseño futuras, TODAS NO_EJECUTADAS

**Positivas P01–P06:** P01 cliente verifica alta y acceso de su propio tenant; P02 cuenta existente conserva dirección/favoritos/pedidos con ownership; P03 recuperación un uso; P04 reserva idempotente; P05 descarga documental autorizada; P06 preferencias granulares revocables.

**Negativas N01–N14:** N01 sesión storefront no abre ERP; N02 tenant B no lee A; N03 cliente Y del mismo tenant no lee a X; N04 usuario anónimo no ve privados; N05 token inválido/caducado falla; N06 recuperar cuenta no revela su existencia; N07 recibo local modificado no confirma pago; N08 reserva repetida no duplica; N09 membresía vencida no otorga acceso; N10 enlace documental ajeno/expirado falla; N11 no marketing sin consentimiento; N12 sesión vieja no conserva cache ajena; N13 mensajes de error no filtran identificadores; N14 logs no contienen PII, JWT, claves ni token de recuperación.

Estas pruebas son **oráculos posteriores**, no evidencia de xUnit, Playwright, MySQL, SMTP, seguridad o producción ejecutados.

## Impacto, auditoría y rollback

**Impacto real:** exclusivamente documento nuevo `docs/v5/` y sección autocontenida `CHANGELOG_AI.md`; sin modificaciones de Domain/Application/Infrastructure/API, Angular, DTO, persistencia, JWT/RBAC, endpoint público, correo, worker, auditoría runtime o secretos. Este documento preserva la separación consumidor/ERP y el control de dominio/tenant por backend, pero **no implementa** esas políticas.

**A0:** revisar S01–S16 y NR01–NR12, evaluar 12 riesgos, 8 supuestos y 8 decisiones. **A1–A3:** comprobar consistencia documental y cobertura de P/N, no tests runtime ni certificación de incidentes. **A4:** diff exactamente dos Markdown, fast-forward CAS, Scope Lock + V5 procedencia exact SHA con SUCCESS, readback institucional. **A5:** revisión independiente de otro TASK_ID Javier, nunca autorrevisión; todas D continúan PENDIENTE. **Rollback:** `git revert` ordinario de changeset, BITACORA inmutable. No QA/main/PROD ni activar eventos/dispatcher.
