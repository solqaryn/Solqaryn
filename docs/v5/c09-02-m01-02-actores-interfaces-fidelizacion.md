# SOLQARYN V5 — C09-02.M01.02: actores, interfaces y fronteras de fidelización y reputación

**Microtarea:** C09-02.M01.02 · PLAN_MAESTRO fila 1335 · **Autor:** PRIMARY DEV 01 JAVIER, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a` · **Fecha:** 2026-11-14 · `solqaryn/Solqaryn@dev` · `PROJECT_SCOPE_LOCK=STRICT`.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Alcance documental:** identificar actores, entradas, salidas y superficies de confianza para puntos, promociones, reseñas y atención posventa sin activar funcionalidades. Base: `docs/v5/c09-02-m01-01-alcance-fidelizacion-reputacion.md` (blob `1030c70296220626c5341f58d99e68e47b8a6cb2`), NR01–NR14 y P01–P08/N01–N08 **FUTUROS_NO_EJECUTADOS**.

## Inventario de código existente contrastado

| Ruta original en dev | Blob Git | Límite de evidencia |
| --- | --- | --- |
| `backend/src/Domain/Entities/Cliente.cs` | `8c6d4c4453e44f8c0be9475340465e1aadb3f787` | Datos cliente y `EmpresaId` nullable; no un ledger de puntos |
| `backend/src/Domain/Entities/UsuarioEmpresa.cs` | `f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c` | Membership empresarial; no permisos de moderación futuros |
| `backend/src/Domain/Entities/TiendaCuentaCliente.cs` | `aa2bc1541afffd68aa980520178d2b6f0d5e7aad` | Cuenta pública, sesiones y favoritos; no otorga puntos |
| `backend/src/Domain/Entities/Venta.cs` | `64ab3cd92e81031e74137f420cd748c564e28f01` | Venta y relación Cliente, no contrato de canje |
| `backend/src/Domain/Entities/CreditoCliente.cs` | `651fb9f9530daded4a11611a7d58a1f1cc401183` | Crédito monetario comercial, diferente de fidelidad |
| `backend/src/Domain/Entities/RegistroAuditoria.cs` | `d669ac397a78ba02a5ba19e2cfb4bc9c7a115462` | Registro de acciones por actor/empresa, no historia económica de puntos |

Los blobs son evidencia estática del código listado, **no una verificación exhaustiva de todo el repositorio** ni pruebas de endpoints/reviews/antifraude. Un contrato futuro no debe reemplazar ni exponer estos recursos por simple analogía.

## A01–A14: actores y responsabilidades futuras, SIN AUTORIZACIÓN ACTUAL

| Actor | Superficie prevista | Límite de autoridad |
| --- | --- | --- |
| A01 Cliente autenticado de tienda | Consultar saldo/reseñas propias | Sólo su cuenta, empresa y consentimientos válidos |
| A02 Visitante público | Consultar reseñas aprobadas | Ninguna lectura de PII, saldo o detalles privados |
| A03 Comprador vinculado a venta | Solicitar puntos de compra | La venta y el Cliente deben pertenecer al mismo tenant |
| A04 Cajero empresarial | Consultar beneficio de una venta | No conceder puntos manuales sin permiso específico |
| A05 Encargado de sucursal | Gestionar promociones de su sede | No afectar sucursales ajenas |
| A06 Administrador empresarial | Solicitar política de fidelidad | No habilitar permisos ni datos de empresa B |
| A07 Moderador de reputación | Evaluar reseña y motivo | Decisión reversible/auditada y sin acceso a otros tenants |
| A08 Personal posventa | Gestionar tickets propios | Información mínima y autorización por caso |
| A09 Auditor empresarial | Leer movimientos y decisiones | Sólo lectura; no modificar ledger o notas originales |
| A10 Soporte de plataforma | Diagnóstico técnico | No obtener PII de clientes por privilegio implícito |
| A11 Servicio Application autorizado | Aplicar reglas futuras de puntos | Actor y empresa verificados, idempotencia obligatoria |
| A12 Repositorio MySQL/EF | Guardar registros futuro | Filtro tenant, transacción, controles de versión |
| A13 Proveedor de mensajería eventual | Entrega opt-in si autorizado | Ningún token de proveedor ni worker habilitado aquí |
| A14 Revisor independiente Javier | A5 de documentos y pruebas | Task distinta de autora, sin promover entornos |

Los roles A01–A14 describen **actores hipotéticos o actuales como entidades técnicas**, no otorgan permisos. JWT, RBAC, pertenencia y estado se deben comprobar server-side.

## I01–I12: interfaces futuras y entradas/salidas observables

| ID | Entrada potencial | Salida esperada | Comprobación negativa indispensable |
| --- | --- | --- | --- |
| I01 | Identidad autenticada y tenant verificado | Contexto empresarial efectivo | Header/slug/EmpresaId del cliente no lo sustituyen |
| I02 | Consulta saldo con paginación | Movimientos propios y saldo calculado | Usuario B y sesión revocada no reciben datos |
| I03 | Confirmación de venta idempotente | Registro durable de acumulación una vez | Venta anulada/replay no duplican incentivo |
| I04 | Solicitud de canje con versión y límite | Débito y comprobante correlacionados | Carrera/versión vieja no causa doble gasto |
| I05 | Creación de promoción allowlisted | Política tipada, versión y vigencia | Clave arbitraria, promoción B o vencida se rechazan |
| I06 | Vista de promociones públicas | Proyección mínima por tenant | No expone descuento privado ni datos de cuentas |
| I07 | Reseña con identidad/política | Estado pendiente de moderación | Payload HTML, PII o autor falso se rechaza |
| I08 | Acción de moderador con motivo | Transición auditada, apelación recuperable | Rol ajeno no puede ocultar ni eliminar |
| I09 | Respuesta de empresa a reseña válida | Respuesta asociada y saneada | No permite script ni suplantación de otra empresa |
| I10 | Solicitud de seguimiento posventa | Ticket referenciado a venta/Cliente | Ticket B inaccesible a agente A |
| I11 | Opt-out por canal/finalidad | Consentimiento revocado verificable | Reintento/caché no envían campañas sin permiso |
| I12 | Desactivación/rollback autorizados | Historial preservado y control reversible | No borra facturas, crédito o puntos históricos |

Estas interfaces son **propuestas documentales, no rutas API existentes**. M02–M03 deben especificar API, DTO, esquema, permisos, transacciones y pruebas; no usar un worker ni sistema de eventos nuevo bajo la prohibición V5.

## F01–F09: fronteras de confianza

| Frontera | Protección requerida |
| --- | --- |
| F01 Navegador/Angular → API | Nunca aceptar el tenant ni RBAC desde UI; JWT válido y permiso backend |
| F02 Sesión de tienda → Cliente/Venta | Ownership y empresa verificados; `EmpresaId=NULL` no es comodín |
| F03 API → Application | DTO tipado, allowlist, autorización por recurso y motivo |
| F04 Application → MySQL | Transacción, key de idempotencia tenant-scoped y control de versión |
| F05 Incentivo → crédito/contabilidad | No conversión a dinero, `CreditoCliente` o `Venta` sin decisión aprobada |
| F06 Reseña pública → moderación | PII mínima, contenido saneado, decisión auditable y reversible |
| F07 Atención posventa → historial CRM | No exponer timeline, ticket, Cliente o venta de otro tenant |
| F08 Consentimiento → canal externo | Opt-in vigente, supresión; no habilitar proveedor/worker en esta fase |
| F09 Auditoría/observabilidad → soporte | Redacción de PII/secrets, correlación mínima y logs no privilegiados |

## Matriz de pruebas FUTURAS NO_EJECUTADAS

| Caso | Positivo futuro | Negativo futuro |
| --- | --- | --- |
| P01/N01 | Cliente A autorizado consulta sus puntos | Sesión de B accede a saldo A |
| P02/N02 | Venta A válida acredita una vez | Replay/carrera no duplica movimiento |
| P03/N03 | Canje autorizado ajusta ledger coherente | Saldo insuficiente o versión obsoleta se rechazan |
| P04/N04 | Promoción tipada vigente se usa en tenant A | Regla expirada/no allowlisted/B se rechaza |
| P05/N05 | Reseña autorizada se presenta sin PII | HTML/script, falsificación y datos B se bloquean |
| P06/N06 | Moderador A decide con auditoría y reversión | Rol lector o moderador B no modifica reseña |
| P07/N07 | Soporte A atiende ticket A vinculado a venta | Ticket/venta B inaccesible incluso por ID |
| P08/N08 | UI accesible informa estado a 320px con foco | Bypass de guard no evita API server-side |

**Ninguna P/N se ejecutó.** No inferir resultados de xUnit, MySQL efímero, Angular, Playwright ni autorización desde este documento.

## Impacto, gates y rollback

- **Alcance del diff:** sólo este Markdown y sección indivisible en `CHANGELOG_AI.md`. Ninguna modificación de Domain, Application, API, EF/MySQL, JWT/RBAC, Angular, secretos, worker, proveedores, datos reales o QA/main/PROD.
- **A0:** identidad Javier :00, Skill/canónica, Scope Lock exact HEAD, PLAN H1335/I1335, COLA G/O y DAG F/G, claim CAS con readback y archivos sin conflicto.
- **A1:** contraste positivo/negativo de actores A01–A14, interfaces I01–I12, fronteras F01–F09 y pruebas futuras frente a M01.01 y seis blobs.
- **A2:** análisis tenant A/B, JWT/RBAC, fraude, PII, promos, legado NULL, opt-out e idempotencia; no certificar seguridad runtime.
- **A3:** mapa Domain/Application/Infrastructure/API/Angular y vínculos de integración futuros; compilaciones, MySQL y E2E `NO_APLICA` a cambios solo Markdown.
- **A4:** publicar fast-forward CAS sobre HEAD dev actual, verificar diff/blob, checks Scope Lock y procedencia V5 `completed/success` SHA exacto, y readback PLAN/COLA/BITACORA.
- **A5:** pendiente de otra Task Javier, distinta del autor, sin autorización QA/main/PROD.
- **Rollback:** git revert normal del commit documental, sin force-push ni borrar BITACORA histórica.

**Próxima unidad:** M01.03 inventario técnico específico; M01.04 riesgos/decisiones; M01.05 aceptación. M01.06/M02–M06 requieren DAG técnico y evidencias propias. Ni una fase documental ni su A5 habilitan pagos, eventos ejecutables o deploy.
