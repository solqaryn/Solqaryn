# SOLQARYN V5 — C07-03.M01.01 — Alcance y no-regresión de inventario, compras y operaciones

**Autor:** PRIMARY DEV 05 — JAVIER | **Fecha:** 2026-11-14 | **Fuente única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, C07-03, fase M01.01 | **Tipo:** contrato documental preparatorio; no código nuevo ni certificación de un flujo ejecutado.

## 1. Objetivo y límites verificables

Delimitar la continuidad del ERP actual para almacenes, compras, órdenes, recepciones, transferencias, reservas, conteos, costeo y trazabilidad. La salida del bloque exige preservar existencias e historial, y en fases posteriores acreditar no-regresión funcional y conciliación cuantitativa. Esta microtarea M01.01 solo fija **objetivo, no-alcance y criterio de no-regresión**; la auditoría exhaustiva de API, servicios, SQL, tests, transacciones y endpoints corresponde a M01.03 y fases posteriores.

Se mantiene como autoridad la empresa resuelta por el servidor, los permisos backend, la sucursal/almacén, el estado del documento y la evidencia transaccional. Ni un `EmpresaId`, `AlmacenId`, `ProductoId` o `UsuarioId` enviado por el cliente autoriza por sí mismo una operación. Las cifras de inventario y costo no deben derivarse de decisiones de interfaz, respuestas cacheadas o referencias históricas huérfanas.

## 2. Evidencia parcial de código existente (blobs de Git, corte dev 2026-11-14)

| Archivo verificado | Blob SHA | Observación delimitada |
| --- | --- | --- |
| `backend/src/Domain/Entities/Almacen.cs` | `b420419660dd15e5c90b15dc10f7459edd967966` | El almacén deriva empresa a través de sucursal y falla si falta jerarquía; no se infiere que cada consulta EF la aplique |
| `backend/src/Domain/Entities/ReservaInventario.cs` | `9e097bbf04d5c2e0bca61426ea673b2306efe7d9` | Estados de reserva y transiciones Activar/Consumir/Liberar/Expirar; no certifica concurrencia ni aislamiento DB |
| `backend/src/Domain/Entities/TransferenciaInventario.cs` | `b2ded478a0083d9e5d8ed6fd10c974409e2b6ac6` | Topología origen/destino distintos y transiciones de aprobación/despacho/recepción, sujetas a verificación de servicio |
| `backend/src/Domain/Entities/MovimientoInventario.cs` | `a1093f36aea78e6851fe4b364e4eb710832a7e61` | CorrelationId y orígenes tipados; almacén nullable para histórico previo al cutover, no acceso global |
| `backend/src/Domain/Entities/OrdenCompra.cs` | `211e21ce0210add8e5e88f79c7237fd673270fdc` | Fingerprint durable de idempotencia y estados de aprobación; no equivale a reconciliación MySQL probada |
| `backend/src/API/Controllers/ReservasInventarioController.cs` | `3cbddb32ea5696028eea5ac5f78e3f78a5971e89` | Controlador [Authorize] y permisos por acción para reservas, consumo y liberación |
| `backend/src/API/Controllers/TransferenciasInventarioController.cs` | `2a3c0ed7cea58cc09f610124b53ed47b43ab44ac` | Lectura/escritura con atributos de permisos de inventario y auditoría; servicios/movimientos requieren inspección separada |
| `backend/src/API/Controllers/OrdenesCompraController.cs` | `c57e143da4a0b5b6fba79a7b12a192d1c5b95d44` | Idempotency-Key requerido en alta y permisos de compras; no acredita reintentos concurrentes reales |
| `backend/src/API/Controllers/ConteosInventarioController.cs` | `f995bf6938fb4546168e52bf9c3c443a99f11843` | Estados, captura y generación de borrador de ajuste, que exige confirmación posterior |
| `backend/src/API/Controllers/MovimientosInventarioController.cs` | `8dd4e9c1ea3f56141de71f7aeefb2c0a8de171c2` | Lectura filtrada/paginada autorizada; no prueba aislamiento A/B de consultas implementadas |

**Regla de evidencia:** presencia de un método, DTO o atributo no demuestra ejecución, permisos efectivos, SQL transaccional o flujo extremo a extremo. Los blobs anteriores fueron leídos selectivamente, no se ejecutaron .NET/MySQL, requests HTTP ni escenarios con empresas A/B en esta M01.01.

## 3. Alcance y exclusiones

| Flujo | Dentro del contrato de preservación | Fuera de esta microtarea |
| --- | --- | --- |
| Inventario | Separación por empresa/sucursal/almacén, unidades y ajustes con motivo y auditoría | Cambiar existencias reales, recalcular costos o aplicar migraciones |
| Compras | Solicitud, aprobación, orden, recepción e idempotencia de documentos | Emitir órdenes reales, comprar o integrar proveedores |
| Reservas | Borrador, activación, consumo, liberación y expiración sin doble descuento | Jobs de expiración, mensajería o workers |
| Transferencias | Origen y destino válidos, aprobación, despacho, recepción y conciliación | Automatizar movimientos o asientos sobre bases reales |
| Conteos y costeo | Captura trazable, borrador de ajuste, reconciliación y snapshots | Ejecutar ajustes, revalorización o corrección histórica |
| Reportes | Lectura paginada por alcance de tenant y permisos | Exponer PII, costos o inventario privado por tienda pública |
| UI | Mantener ergonomía, mensajes y estados existentes | Activar rutas, rediseñar pantallas, añadir dependencias |
| Plataforma | Compatibilidad Domain/Application/Infrastructure/API, JWT/RBAC | Cambiar autorización, tenants, DNS, Hostinger, Cloudflare, QA/main/PROD |

## 4. Invariantes de no-regresión NR01–NR12

- **NR01 — Tenant:** empresa A nunca lee ni muta almacenes, existencias, compras ni documentos de B, aunque conozca IDs reales.
- **NR02 — Jerarquía:** un almacén requiere sucursal autorizada; referencias incompletas o con propietario contradictorio fallan cerradas.
- **NR03 — Reserva:** un mismo stock no puede reservarse/consumirse dos veces por carreras o reintentos; conservar historial y motivo.
- **NR04 — Transferencias:** origen y destino distintos, ambos válidos en la topología autorizada; despacho y recepción no duplican unidades.
- **NR05 — Compra:** alta idempotente bajo misma clave/payload; misma clave con payload distinto se rechaza sin duplicar orden.
- **NR06 — Conteo:** una captura y un ajuste generado mantienen trazabilidad y aprobación separada; no pasar de borrador a impacto sin control.
- **NR07 — Costeo:** costo, impuesto, fecha y moneda históricos no se recalculan por cambios posteriores de catálogo.
- **NR08 — Movimiento:** cada operación nueva tiene correlación, causa y origen tipado; campos NULL históricos no habilitan escrituras ambiguas.
- **NR09 — Permisos:** JWT, membresía/rol vigentes y permiso por acción se validan en servidor; UI/claim no sustituyen RBAC.
- **NR10 — Consistencia:** errores parciales, rollback o reintentos no dejan inventario negativo, transferencias unilaterales ni doble recepción.
- **NR11 — Privacidad:** listados, exportaciones, búsqueda y reportes filtran tenant y no filtran detalles sensibles de otras empresas.
- **NR12 — Continuidad:** las rutas y contratos ERP existentes conservan compatibilidad observable, auditoría, mensajes seguros y reversión sin reescribir historia.

## 5. Criterios futuros y casos de negación (NO_EJECUTADOS)

**Positivos P01–P06, a ejecutar en fases de código/QA pertinentes:** P01 stock por A y sucursal autorizado; P02 reserva activa y liberación sin pérdida de unidades; P03 transferencia origen→destino conciliada; P04 orden de compra reintentada con misma clave y mismo payload sin duplicado; P05 conteo→ajuste borrador→aprobación explícita; P06 reporte paginado sin fuga y movimientos correlacionados.

**Negativos N01–N08, NO_EJECUTADOS:** N01 tenant B con ID de A; N02 almacén sin sucursal cargada; N03 transferencia mismo almacén; N04 misma idempotency-key con payload distinto; N05 doble consumo/reserva concurrente; N06 rol sin permiso sobre aprobación; N07 recepción parcial seguida de fallo transaccional; N08 reporte/exportación que intenta acceder a costos/PII ajenos.

Los ensayos deberán usar pruebas positivas y negativas reales en .NET 10/EF Core 10/MySQL 8.4 efímero en CI, además de seguridad multiempresa A/B y, donde haya frontend, Angular 22.2.1/Playwright. **Ninguno de esos ensayos se ejecutó ni se declara PASS por este documento.**

## 6. Gates, rollback y entrega

- El DAG actual valida esta **preparación documental**; no habilita por sí solo migraciones, operaciones financieras, integraciones externas, workers ni despliegues.
- M01.02 documentará actores e interfaces, M01.03 inventariará contratos/servicios/test con profundidad causal, M01.04 riesgos y decisiones y M01.05 criterios verificables. M01.06 y lotes posteriores requieren sus propios gates y pruebas.
- Para esta entrega, A0 es revisión estática del diff y procedencia de blobs, A1 es coherencia documental positiva/negativa, A2 constancia del no-cambio de JWT/RBAC/tenant/PII, A3 runtime **NO_APLICA** por modificación Markdown, A4 exige publicación fast-forward, Scope Lock y procedencia V5 `completed/success` sobre SHA exacto, más readback PLAN/COLA/BITACORA.
- A5 **PENDIENTE** hasta revisión por otra de las cinco Tasks Javier; no autoriza QA/main/PROD.
- Reversión: `git revert` normal del commit documental, sin force-push ni reescritura, después de evaluar dependencias y trazas institucionales. No modificar BITACORA histórica.

**Resultado:** límites y no-regresión documentados para C07-03.M01.01. Ningún endpoint, servicio, tabla, configuración, secreto, pago o inventario real ha sido modificado.
