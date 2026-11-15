# SOLQARYN V5 — C08-02.M02.03 — Persistencia conceptual, índices y fixtures para mesas/QR

**Autor:** PRIMARY DEV 06 — JAVIER, `6af16bbad26881918c283abe06e1f32a`. **Fecha:** 2026-11-15. **Riesgo R0:** diseño documental, no migración. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, C08-02.M02.03, PLAN fila 1198. **Ámbito:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT.

## 1. Alcance y precondiciones

Definir **únicamente** persistencia propuesta, claves/índices, estrategia de migración reversible **futura** y datos de prueba para restaurante/autoservicio. No modifica `DbContext`, entidades EF, migraciones, MySQL, credenciales, facturación, stock, endpoints o QR en vivo. El backend real usa .NET 10, EF Core 10.0.12 y MySql.EntityFrameworkCore Oracle 10.0.9 declarado; cualquier implementación posterior deberá comprobarlo con CI y una instancia MySQL 8.4 **efímera**. No extrapolar la documentación a una migración ejecutada.

Predecesores: M01.01–06 LISTO+A5; modelo conceptual M02.01 publicado por DEV06, con 20 entidades y 20 invariantes. M02.02 (API/DTO) es una tarea diferente bajo su propio owner y no se combina con este changeset. Al claim V17, PLAN H1198=PENDIENTE/I vacío, COLA G/O=SI, DAG F=VALIDADO/G=SI y candado `SQV17_C08_02_M02_03`. Las decisiones pendientes D01–D08 de M01 no son autorizaciones de runtime.

## 2. Esquema relacional propuesto (NO CREADO)

| Objeto lógico futuro | Claves / propiedad | Relación y condición de seguridad |
| --- | --- | --- |
| MesaServicio | PK opaca, EmpresaId, SucursalId, ZonaId?, Version | FK compuesta a Sucursal y dueño único |
| MesaAliasQR | PK opaca, MesaId+EmpresaId+SucursalId, Generacion | Alias público con estado/TTL, nunca rol |
| CredencialMesa | PK, AliasId, TokenHash, ExpiresAt, RevokedAt | Guardar hash con secreto no recuperable; nada en URL/log claro |
| SesionMesa | PK, EmpresaId, SucursalId, MesaId, ExpiresAt, Version | Lease acotado a mesa, jamás rol de empleado |
| MenuVersionPublica | PK, EmpresaId, SucursalId, Version, PublishedAt | Solo proyección habilitada |
| MenuItemPublico | PK, MenuVersionId, ProductoERPId, VarianteId? | Precio/IVA de autoridad servidor; ocultos no expuestos |
| MenuOpcion | PK, MenuItemId, OpcionKey, Limitaciones | Cantidad/rango y compatibilidad |
| PedidoMesa | PK, EmpresaId, SucursalId, MesaId, SesionId, Version | Estado terminal y autoría en servidor |
| LineaPedidoMesa | PK, PedidoId, ProductoERPId, Cantidad | Precio/IVA verificados, snapshot trazable |
| ComandaMesa | PK, PedidoId, Destino, Version | Cocina de misma sede y autorización vigente |
| EventoPedidoMesa | PK, PedidoId, Version, EstadoAntes, EstadoDespues | Auditoría y secuencia sin PII |
| ReservaStockERPRef | PK, PedidoId, LineaId, ERPReservationId | Solo referencia a stock autoritativo existente |
| CierrePedidoMesa | PK, PedidoId, ClosedAt, ERPReceiptRef? | Máximo uno por pedido, idempotente |
| IdempotenciaRestaurante | PK, EmpresaId, SucursalId, Scope, KeyHash, PayloadHash | Estado final único, TTL, respuesta saneada |
| AuditoriaRestaurante | PK, EmpresaId, SucursalId, CorrelationId, Resultado | Sin JWT, QR secreto, PAN ni PII cruda |
| ConciliacionRestauranteERP | PK, PedidoId, ERPRef, Estado, Version | Reconciliación trazable y compensación segura |

**Nota crítica:** los nombres son conceptuales. E01 Empresa, E02 Sucursal y productos/ventas/inventario ERP **ya tienen autoridad existente**: esta tabla NO propone duplicar sus registros maestros. Las entidades de diseño M02.01 E01–E20 y el esquema futuro requieren un mapeo formal antes de generar un solo archivo de migración.

## 3. Índices y claves de unicidad futuros (NO APLICADOS)

| Índice/regla | Propósito | Resultado negativo que debe garantizar |
| --- | --- | --- |
| U01: MesaServicio(EmpresaId,SucursalId,MesaKeyActivo) | Mesa inequívoca por sede | Repetir mesa activa misma sede falla |
| U02: MesaAliasQR(TokenHash) global cuando token sea opaco global | Ningún QR público ambiguo entre tenants | Un mismo token A/B jamás resuelve dos mesas |
| U03: MesaAliasQR( EmpresaId,SucursalId,MesaId,Generacion ) | Versionado de alias y rotación | Alias de mesa anterior no se revive |
| U04: CierrePedidoMesa(PedidoId) | Cierre único | Doble cierre/replay no duplica factura |
| U05: IdempotenciaRestaurante(EmpresaId,SucursalId,Scope,KeyHash) | Reintentos convergentes | Key repetida con PayloadHash diferente => conflicto |
| U06: PedidoMesa(EmpresaId,SucursalId,MesaId,Version) / token de concurrencia | Optimista y versionado | Escritor obsoleto no vence actualización |
| F01: FKs compuestas por (EmpresaId,SucursalId,Id) | Anti cross-tenant por integridad | Mesa/sesión/pedido de B no se enlaza desde A |
| F02: LineaPedidoMesa(PedidoId), EventoPedidoMesa(PedidoId,Version) | Lectura ordenada y auditoría | Evento fuera de pedido o versión duplicada falla |
| F03: AuditoriaRestaurante(EmpresaId,OccurredAt,CorrelationId) | Investigación sin lectura masiva | Consulta de B no filtra logs de A |
| F04: MenuVersionPublica(EmpresaId,SucursalId,PublishedAt) | Proyección publicada por tenant | Menú inactivo/ajeno no es visible |

Las claves concretas, longitud/collation de `TokenHash`, elección de `rowversion` compatible con Oracle MySQL, índices parciales y eliminación lógica requieren diseño y prueba de proveedor **antes** de M03. No asumir índices filtrados de otros motores en MySQL. Si un token público no es globalmente único, diseñar namespace verificable **antes de publicar URLs**; nunca resolver QR por un ID sin tenant y validación.

## 4. Reglas de transacción y evolución

- Todas las operaciones que muten Pedido/Linea/Cierre deben llevar TenantId/SucursalId del servidor. Validar rol, sesión vigente, recurso propio y estado/versión antes de persistir; una FK sola **no sustituye JWT/RBAC**.
- Cierre, consumo/reserva de stock, factura y caja requieren una sola autoridad de consistencia. Definir transacción local o outbox/compensación únicamente en el scope futuro autorizado, no introducir worker aquí.
- Idempotencia distingue key, tipo, scope y hash del payload; retorno idéntico ante retry y conflicto ante payload divergente. Probar carreras y actualización obsoleta.
- Eliminar/archivar alias QR conserva auditoría sin preservar secretos, y revoca todas las sesiones/alias dependientes de acuerdo con contrato explícito. Nada debe resucitar un QR expirado.
- Datos sensibles minimizados: hash/token no reversible, TTL, cifrado/rotación sólo si se autoriza; no persistir PAN, JWT, QR claro ni PII innecesaria en fixtures o logs.

## 5. Plan de migración y reversión FUTUROS — NO EJECUTADOS

1. **MIG01 inventario:** levantar entidades/índices reales y mapeo ERP de dev; medir duplicados y conflicto tenant, solo lectura, sin tocar Aiven productivo.
2. **MIG02 fase expand:** scripts EF provider Oracle MySQL en rama/alcance autorizado, tablas nuevas aditivas y FKs compuestas con comprobaciones previas. Evitar lock de escrituras sobre tablas críticas.
3. **MIG03 backfill efímero:** crear dataset sintético A/B, sin PII, verificar conteos/hash y ausencia de cruces tenant; backfill idempotente por batches; no ejecutar en esta entrega.
4. **MIG04 pruebas:** levantar servicio MySQL 8.4 aislado en GitHub Actions; ejecutar Category=Integration **real** contra migración, fixtures, índices, 0 fallos y logs de SQL/rollback. Un build verde no basta.
5. **MIG05 compatibilidad:** verificar lecturas antiguas/nuevas, feature flag apagado por defecto y rollback por contrato de datos; desplegar sólo con autorización separada (R3 cuando afecte producción).
6. **MIG06 reversión:** desactivar feature, restaurar snapshot sólo si la operación lo autoriza y demostrar integridad/conciliación. Nunca borrar datos comerciales, reset forzado o DROP antes de confirmar ausencia de dependencias.

## 6. Fixtures de aceptación propuestos — F01–F12 (TODOS NO_EJECUTADOS)

| Fixture futuro | Caso positivo | Negativo/frontera que debe fallar |
| --- | --- | --- |
| F01 | Empresa A/sucursal A/mesa A únicas | Duplicado mesa activa misma sede |
| F02 | Alias QR A vigente y hash único | Mismo hash/alias en B o expirado |
| F03 | Sesión mesa A válida | Sesión de B o mesa cerrada |
| F04 | Menú A publicado | SKU oculto, versión antigua o menú B |
| F05 | Pedido A y línea autorizada | Cliente envía TenantId B |
| F06 | Cantidad positiva y precio servidor | Cantidad 0/negativa, precio manipulado |
| F07 | Key idempotente reintentada converge | Mismo key y payload distinto |
| F08 | Estado con versión vigente avanza | Versión stale/estado terminal |
| F09 | Cocina autorizada de sede A | Cocina B/rol revocado |
| F10 | Un cierre produce una referencia ERP | Dos cierres concurrentes/doble efecto |
| F11 | Caída entre stock y factura compensa | Registro parcial irreversible |
| F12 | Audit log saneado y consultable por A | JWT/QR/PAN/PII o evento de B visible en A |

**Resultado de F01–F12 en este scope: NO_MEDIDO; 0 migraciones y 0 pruebas MySQL ejecutadas.** La ejecución futura debe demostrar positivos, negativos, límites y rollback en CI real con versión, SHA, job y logs; no declarar Category=Integration si el filtro omitió esos tests.

## 7. Calidad, impacto y cierre

A0: contraste con M01.06 y M02.01, modelo relacional, diez reglas de claves y doce fixtures futuros. A1: revisión de casos positivos/negativos de diseño, **no asserts ejecutados**. A2: tenant A/B, QR falso, roles, revocación, idempotencia y no doble cierre documentados. A3: MySQL/EF/.NET/HTTP/browser **NO_APLICA al diff exclusivamente documental**. A4: solo doc+CHANGELOG en dev; Scope Lock y V5 completed/success del SHA causal más readback PLAN/BITACORA. A5: otra Task Javier diferente a DEV06 revisa formalmente el contrato antes de autorizar sucesoras.

Impacto actual: cero cambios en backend Domain/Application/Infrastructure/API, frontend, MySQL, secretos, permisos, pagos o despliegues. Rollback actual: `git revert` no destructivo o corrección aditiva, dejando BITACORA histórica intacta y sin liberar DAG falso. R3 de producción permanece NO_GO sin mandato nuevo.
