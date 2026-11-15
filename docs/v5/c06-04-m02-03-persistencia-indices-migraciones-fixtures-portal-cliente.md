# SOLQARYN V5 FINAL — C06-04.M02.03: persistencia e índices del portal de cliente

**Contrato DOCUMENTAL — ninguna tabla, migración, EF entity, SQL, seed ni prueba MySQL ejecutada.** Autor PRIMARY DEV 02 JAVIER_CHATGPT, TASK_ID 6af16bb9492c8191b8e8c7d606f11507; repo solqaryn/Solqaryn, rama dev, PROJECT_SCOPE_LOCK=STRICT. Fuente: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL; PLAN_MAESTRO fila 982, COLA G/O=SI, DAG fila 943 F=VALIDADO/G=SI.

## 1. Alcance y fronteras

M01.06 fue publicado en 12ed2af6682679fb371f67e3fbefda68839d6ea1 y aprobado A5 por Task independiente; M02.01 está documentado en docs/v5/c06-04-m02-01-entidades-ownership-invariantes-portal-cliente.md, SHA 3742c0153a8266d1b1f2f8ff7b82873114cad407. Sus entidades ENT01–ENT16 son lógicas, no orden de crear tablas. El portal existente no acredita registro/recuperación integral, membresías, reservas, documentos ni preferencias.

Preservar filtrado por TenantId+CustomerId resueltos en servidor ANTES de ordenar, paginar, cachear y emitir enlaces. A/B son tenants distintos; A1/A2 son clientes diferentes dentro de A. Un CustomerId aislado, Host, EmpresaId, slug, cabecera o cookie no conceden acceso. JWT CUSTOMER no hereda permisos ERP. Cualquier lectura sin alcance válido debe fallar cerrada.

## 2. Doce agrupaciones candidatas de modelo, no tablas creadas

| ID | Relación / proyección futura | Restricción de ownership |
| --- | --- | --- |
| DB01 | CustomerTenantMembership | TenantId+CustomerId, vigencia y version de membresía |
| DB02 | CustomerSessionGrant | Principal de cliente, tenant, hash de sesión, revocación, TTL |
| DB03 | CustomerProfileProjection | TenantId+CustomerId, versión CAS para campos permitidos |
| DB04 | CustomerVerifiedContact | Canal validado y digesto privado, sin email público |
| DB05 | CustomerRecoveryChallenge | Digest del token, expiración, consumedUtc, límites de intentos |
| DB06 | CustomerOrderReference | FK lógica a pedido existente, tenant y cliente propietario |
| DB07 | CustomerAddressBookEntry | TenantId+CustomerId+AddressId, borrado versionado |
| DB08 | CustomerReservationReference | Referencia propia y estado real de reserva, sin promesa de cupo |
| DB09 | CustomerDocumentGrant | Documento privado, dueño, expiración, revocación |
| DB10 | CustomerNotificationPreference | Consentimiento tipado por canal y versión |
| DB11 | CustomerIdempotencyRecord | Tenant, actor, operación, hash de clave y fingerprint |
| DB12 | CustomerPortalAuditRecord | Actor, tenant, acción, UTC y correlationId saneado |

Son candidatas a proyecciones o reutilización de entidades existentes. No migrar ni duplicar Pedido, Usuario, Cliente, Reserva o EF mappings sin inventario del modelo persistido y su autoridad. Beneficios de membresía y favoritos pueden ser vistas de modelos vigentes, no necesariamente nuevas tablas.

## 3. Diez índices y restricciones conceptuales NO_CREADAS

| ID | Clave / restricción propuesta | Finalidad de prueba futura |
| --- | --- | --- |
| IDX01 | UNIQUE(TenantId,CustomerId) en Membership | Impedir membresía duplicada cross-tenant |
| IDX02 | (TenantId,CustomerId,SessionDigest) | Revocación privada y caducidad |
| IDX03 | (TenantId,CustomerId,ProfileVersion) | CAS real de perfil, 409 cuando 0 filas |
| IDX04 | UNIQUE(TenantId,CustomerId,Channel,Digest) | Contacto propio, sin PII de índice público |
| IDX05 | UNIQUE(TenantId,CustomerId,ChallengeDigest) | Rechazar replay y challenge de B |
| IDX06 | (TenantId,CustomerId,OrderId) | Aislamiento de pedidos antes de cursor |
| IDX07 | (TenantId,CustomerId,AddressId) | Bloquear acceso intra-tenant A1/A2 |
| IDX08 | (TenantId,CustomerId,ReservationId) | No autorizar por ReservationId solo |
| IDX09 | (TenantId,CustomerId,DocumentId,ExpiresAtUtc) | Grants temporales sin URL pública |
| IDX10 | UNIQUE(TenantId,CustomerId,Operation,KeyDigest) | Idempotencia transaccional y fingerprint |

Antes de ejecutar DDL, revisar collation, longitud de claves MySQL 8.4, nulos, costos de índices, FK compuestas, escrituras concurrentes, plans de consulta y cardinalidad real. TenantId NULL jamás significa permiso de plataforma. La autorización efectiva se comprueba igualmente en Application/API; no descansar solo en la FK.

## 4. Contratos transaccionales, pruebas y reversibilidad FUTURAS

**Recuperación:** identificar desafío por tenant+cliente, comparar digest y expiración, consumir de forma atómica, revocar sesiones afectadas y auditar; ante fallo, revertir sin emitir confirmación falsa. **Perfil y preferencias:** actualización CAS con versión de fila, conflicto claro 409; una lectura seguida de write sin condición NO es CAS. **Pedidos/reservas:** consultas filtradas antes de paginación; el estado de reserva no constituye cupo adquirido. **Documentos:** grant privado expirable, revocación y no-store; sin URL permanente de Cloudinary. **Idempotencia:** repetición misma key+fingerprint retorna efecto único, payload diferente con misma key da conflicto. **Auditoría:** campos mínimos, sin JWT, token, correo o documento en logs/response.

Plan futuro de migración (NO EJECUTADO): inventariar entidades y DB schema vigente; crear base MySQL 8.4 efímera con datos sintéticos A/B, A1/A2; evaluar backfill y migración aditiva; ejecutar prueba de FK/unique/CAS/transacciones; comparar plan de ejecución; ensayar rollback/restore en efímero con evidencia; activar feature únicamente tras autorización de M03/A5. No usar producción, Aiven live, credenciales ni datos reales.

## 5. Ocho pares de fixtures FUTUROS_NO_EJECUTADOS

| ID | Positivo futuro | Negativo futuro |
| --- | --- | --- |
| FX01 | Tenant A y B con CustomerId igual conservan datos separados | Query CustomerId sin tenant no expone A/B |
| FX02 | A1 lee solamente sus OrderId y ReservationId | A2 no enumera pedido o reserva A1 |
| FX03 | Perfil versión 2 cambia a 3 por CAS | Versión 1 obsoleta no sobreescribe |
| FX04 | Challenge propio vigente se consume una vez | B, expirado, repetido o manipulado denegado |
| FX05 | Grant documento A1 vigente autoriza lectura | A2/B o grant caducado rechazan bytes y URL |
| FX06 | Dos requests con igual key+fingerprint dan un efecto | Diferente fingerprint con la misma key => conflicto |
| FX07 | Consentimiento A1 se revoca con versión y auditoría | Body CustomerId A2 no altera consentimiento A1 |
| FX08 | Fallo provocado en mitad de transacción hace rollback | Error SMTP/DB no confirma recuperación ni estado falso |

Fixtures son propuestas de datos, NO ocho tests pasados. Las pruebas de integración MySQL, xUnit JWT/RBAC, servicio API y browser deberán ejecutarse en M03/M05 en CI dirigida y exact-SHA; ninguna afirmación de runtime se deriva de este documento.

## 6. Impacto, A0–A5 y rollback

Domain: invariantes por actor/tenant y versiones; Application: autorización, transacciones y datos mínimos; Infrastructure: EF Core 10, proveedor Oracle MySql.EntityFrameworkCore 10.0.9 y MySQL 8.4 en validación posterior; API: filtros y errores neutrales; Angular: estado privado y caché invalidada al cambiar cliente. Los cambios de código, endpoints, eventos, migraciones y arquitectura son CERO en esta entrega. Secretos, JWT real, QA/main/PROD y proveedores de pago NO autorizados.

A0 = skill/canon, Scope Lock, PLAN/COLA/DAG y claim por ID. A1 documental = 12 agrupaciones, 10 restricciones y 8 pares FX cotejables sin ejecución. A2 = aislamiento A/B y A1/A2, hash de recovery, CAS y fail-closed. A3 = handoff conceptual por capas; backend/MySQL/Angular/Playwright NO_APLICA al diff Markdown, nunca PASS. A4 = commit FF con expected_sha, diff Markdown+CHANGELOG, check Scope Lock y V5 completed/success exact SHA, BITACORA y readback. A5 = revisión posterior de otra Task Javier distinta del autor; no autoaprobar.

Rollback de este changeset solo documental: git revert normal, preservar historia y Bitácora, nunca reset/force. Ninguna migración en vivo, SQL, datos reales ni despliegue.
