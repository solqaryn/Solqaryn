# SOLQARYN V5 — C06-04.M02.01: entidades, ownership e invariantes del portal de cliente

**Alcance:** contrato M02 DOCUMENTAL, sin modelos EF, SQL, endpoints, componentes Angular ni servicios instalados. **Autor:** PRIMARY DEV 01 JAVIER_CHATGPT (`6adbb9fff1548191a1bd5b30d9e9984a`). **Repositorio:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Fecha:** 2026-11-15.

## 1. Autoridad, evidencia y frontera

STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, y Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. El preflight de `C06-04.M02.01`, PLAN fila 980, verificó PENDIENTE sin owner, COLA G/O=SI y DAG F=VALIDADO/G=SI. El cambio condicional exclusivo H980 PENDIENTE→EN_PROGRESO tuvo una ocurrencia y el owner I980 quedó asignado al TASK_ID propio, con lectura posterior. M01.06 publicado en SHA `12ed2af6682679fb371f67e3fbefda68839d6ea1` y blob `afd681f943aebae54717061a85d59e3b8933b75b` consta LISTO más A5 independiente por Task 01, autoría distinta de Task 03.

Las cinco fuentes M01 incluyen M01.01 blob `afa54d6939df46719cfe89a9f3b657b2bce38f03`, M01.02 `d0fc169736e28f93128b10b7c15f32406e1eb0a1`, M01.03 `163402f99029b7371c5ed898562467267e6c1a7a`, M01.04 `19508bcb6b20eaad6043d100d872c6e105775903` y M01.05 `f5ba1fe80dbfdf2e559d737ad86e14504988123e`; son especificaciones, no pruebas de portal integral. El cierre M01.06 conserva 16 bloqueos B01–B16, ocho condiciones H01–H08 y ocho rollback RB01–RB08 futuros. Esta preparación no elimina gates ni autoriza M03.

El portal existente tiene componentes de cuenta de tienda y pedidos, no acredita recuperación integral de cuentas, membresías, reservas, preferencias y documentos. No interpretar EmpresaId, Host, cookie, route, `X-Empresa-Id` o cualquier input del navegador como grant. El backend resuelve tenant, cliente, identidad y ownership; se rechazan recursos de otro tenant B y también de un cliente A2 distinto dentro del mismo tenant A.

## 2. Dieciséis entidades/relaciones lógicas FUTURAS, no tablas ni clases creadas

| ID | Entidad conceptual / vínculo | Clave e invariante de propiedad |
| --- | --- | --- |
| ENT01 | PortalCustomerPrincipal | PrincipalId del cliente separado de principal ERP, vinculado a TenantId |
| ENT02 | CustomerTenantMembership | (TenantId, CustomerId), permisos y revocación; ninguna membresía por host |
| ENT03 | CustomerSessionGrant | (TenantId, CustomerId, SessionId), TTL/revocación; sin privilegio ERP |
| ENT04 | CustomerProfileProjection | Datos mínimos editables por dueño, sin PII de otros clientes |
| ENT05 | CustomerVerifiedContact | Canal validado por CustomerId, no reusable por otro sin confirmación |
| ENT06 | CustomerRecoveryChallenge | Digest/token temporal por actor/tenant, intentos limitados, expiración |
| ENT07 | CustomerOrderReference | (TenantId, CustomerId, OrderId), lectura filtrada antes de paginar |
| ENT08 | CustomerAddressBookEntry | (TenantId, CustomerId, AddressId), privacidad y borrado versionado |
| ENT09 | CustomerFavoriteReference | (TenantId, CustomerId, ResourceId), idempotencia y validación de catálogo |
| ENT10 | CustomerReservationReference | (TenantId, CustomerId, ReservationId), cupo/estado verificados servidor |
| ENT11 | CustomerMembershipBenefitView | Beneficios por membresía vigente y empresa, sin resolver por flag UI |
| ENT12 | CustomerDocumentGrant | (TenantId, CustomerId, DocumentId), enlace privado expirable; no URL pública |
| ENT13 | CustomerNotificationPreference | (TenantId, CustomerId, Channel), opt-in/opt-out y auditoría mínima |
| ENT14 | CustomerPortalAuditRecord | Actor, entidad, acción, UTC y correlationId saneado; no payloads secretos |
| ENT15 | CustomerIdempotencyRecord | (TenantId, CustomerId, OperationKey), fingerprint+versión, sin duplicados |
| ENT16 | CustomerPortalAccessPolicy | Reglas tipadas por tenant y permisos backend; default denegar |

Relaciones **propuestas**: Principal 1:N sesiones y contactos; Customer 1:N direcciones, favoritos, pedidos, reservas, documentos y preferencias; CustomerTenantMembership vincula identidad y tenant; los records de auditoría/idempotencia refieren a operación y actor sin exponer secretos. Los identificadores opacos no sustituyen filtros `TenantId` + `CustomerId`. Revisar interoperabilidad con modelos existentes antes de agregar claves, tablas o migraciones: esta especificación no declara que todas las entidades deban almacenarse físicamente.

## 3. Dieciséis invariantes vinculantes a M03, pendientes de pruebas reales

| ID | Condición |
| --- | --- |
| I01 | JWT de cliente y principal ERP son dominios de confianza separados, sin escalamiento cruzado |
| I02 | Acceso exige tenant efectivo, membresía activa y CustomerId resueltos por backend |
| I03 | EmpresaId, Host, forwarded, slug y campos cliente nunca confieren autoridad |
| I04 | Consulta, paginación, caché y response filtran tenant A/B antes de exponer datos |
| I05 | Dos clientes A1/A2 del mismo tenant no ven pedidos/direcciones/documentos ajenos |
| I06 | Role/permiso revocado se revalida en cada mutación; flags UI no elevan privilegio |
| I07 | Recursos ausentes o de otro dueño retornan error neutro sin confirmar existencia |
| I08 | Recuperación usa token limitado, un solo uso, caducidad e intentos acotados |
| I09 | Enlaces de documentos privados no se indexan ni cachean en ámbito público |
| I10 | Mutación/versionado de perfil, direcciones y preferencias exige CAS o control de concurrencia |
| I11 | IdempotencyKey se liga a tenant, cliente, operación y fingerprint, sin efectos duplicados |
| I12 | Reservas y beneficios se validan contra estado/cupo/membresía vigentes, no vistas obsoletas |
| I13 | Cambio de sesión o tenant invalida proyecciones y caché previas, incluso respuestas tardías |
| I14 | Logs, errores y trazas no filtran JWT, tokens, email privado, documentos ni datos B |
| I15 | Fallo de SMTP/proveedor externo deniega confirmación falsa, registra causa y permite reversión |
| I16 | DNS, secretos, datos reales, pagos, QA/main/PROD exigen autorización específica separada |

## 4. Doce pares positivos/negativos FUTUROS_NO_EJECUTADOS

| Par | Positivo futuro | Negativo futuro |
| --- | --- | --- |
| PN01 | Cliente A autenticado lee pedido propio A | Actor B usando ID A recibe respuesta neutra |
| PN02 | Cliente A1 lee dirección propia en tenant A | Cliente A2 no enumera dirección de A1 |
| PN03 | Recuperación propia con token vigente y un uso | Token vencido/repetido o de otro tenant rechazado |
| PN04 | Principal de cliente muestra funciones permitidas | JWT de cliente no accede a endpoints ERP |
| PN05 | Favorito propio se añade una vez por clave | Doble submit o recurso B no duplica registro |
| PN06 | Reserva propia refleja cupo/estado verificado | Reserva ajena o sin cupo no se crea/lee |
| PN07 | Beneficio de membresía vigente se aplica | Membresía vencida no concede beneficio |
| PN08 | Documento privado con grant expirable propio | URL ajena/vencida nunca revela archivo B |
| PN09 | Preferencias del dueño guardan versión CAS | Cliente A2 o expectedVersion obsoleta rechazado |
| PN10 | Cambio A→B limpia estado y caché A | Respuesta tardía A no repuebla sesión B |
| PN11 | Errores/correlationId sin PII permiten soporte | Excepción revela JWT/email/documento en logs/response: rechazo |
| PN12 | Fallo de proveedor se reporta sin éxito falso | SMTP/servicio caído no crea estado confirmado ficticio |

Son **oráculos de aceptación, no tests ejecutados**. Futuros xUnit, HTTP integración, MySQL efímero y navegador Chromium/Playwright deberán probar positivos y negativos entre tenants A/B y clientes A1/A2, con rol revocado, recuperación, documentos y caché. No inferir cumplimiento de seguridad a partir de diseño.

## 5. Integración, fronteras y reversión

**Domain futuro:** ownership tenant+CustomerId, estados y versiones. **Application futuro:** autorización cliente/ERP, JWT, membresías y grants con denegación. **Infrastructure futuro:** EF Core 10, MySQL 8.4, FK/índices tenant-bound e idempotencia probados únicamente con servicio efímero autorizado. **API futura:** respuestas neutras/DTO, filtrado previo a paginación, control CAS, no aceptar tenant desde UI. **Angular futuro:** guards UX no reemplazan backend, aislamiento de estado al cambiar cuenta/contexto, accesibilidad y errores; pruebas Playwright dirigidas. **Operación futura:** PII minimizada, claves en secretos, auditar reversión, cuotas/reintentos acotados sin proveedor de pago activado.

Rollback futuro: revocar sesión/challenge, retirar proyección o URL privada, invalidar caché particionada, restaurar versión con CAS, revertir migración sólo después de restore probado en MySQL efímero. El rollback de este changeset **solo documental** es `git revert` normal en HEAD vigente, sin force-push/reset, preservando cambios ajenos y BITACORA.

## 6. Auditorías A0–A5 y salida

**A0:** leer Skill canónica, Scope Lock, PLAN H/I/K, COLA G/O, DAG F/G y C06-04.M01.06 con A5 auténtica; comprobar fuentes/blobs y claim CAS exclusivo. **A1 documental:** 16 entidades/relaciones, 16 invariantes y 12 pares PN, sin atribuir resultados de pruebas futuras. **A2 estática:** aislamiento A/B y A1/A2, JWT cliente vs ERP, RBAC/ownership, PII y recuperación fail-closed. **A3 documental:** coherencia con M01 B01–B16/H01–H08/RB01–RB08 y las capas actuales sin ejecutar migraciones. **A4:** publicar sólo Markdown propio más CHANGELOG autocontenido mediante fast-forward/CAS en dev; exigir Scope Lock y procedencia V5 completed/success exact-SHA, readbacks PLAN/COLA/DAG/BITACORA. **A5:** revisión independiente por Task distinta del autor antes de liberar sucesora técnica.

CI runtime .NET/Angular/MySQL/Playwright es **NO_APLICA** para diff solo Markdown; cancelled, skipped, in_progress o failed jamás son PASS requerido. No crear código, tablas, endpoints, workers, webhooks, Tasks, proveedores, secretos, datos live, QA/main/PROD por este contrato.
