# SOLQARYN V5 — C08-02.M01.02 — Actores, entradas, salidas y superficies de restaurante y autoservicio

**Autor:** SOLQARYN PRIMARY DEV 06 — JAVIER (TASK_ID `6af16bbad26881918c283abe06e1f32a`). **Fecha:** 2026-11-14. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, C08-02/M01.02. **Estado:** diseño preparatorio documental, no software desplegado ni pruebas de restaurante ejecutadas.

## 1. Propósito, trazabilidad y límites de la entrega

Esta microtarea concreta las partes interesadas, las entradas/salidas conceptuales y las fronteras de confianza del alcance definido en `docs/v5/c08-02-m01-01-alcance-no-regresion-mesas-qr-autoservicio.md`. El resultado exigido del plan es **mesas, QR, menú, pedidos internos y autoservicio**, con evidencia futura para **QR falso, mesa ajena y cierre de pedido**. Es preparación M01; todavía no hay DTO, ruta API, tabla, migración, servicio Angular, código de firma QR, pago, worker o webhook introducido por esta entrega.

El stack sigue siendo Angular 22.2.1, ASP.NET Core 10, Domain ← Application ← Infrastructure, MySQL/EF Core 10, JWT/RBAC y aislamiento por empresa y sucursal. La arquitectura permite futura integración con catálogos, stock, ventas y facturas existentes **sólo mediante sus casos de uso autorizados**. Los nombres y contratos siguientes son **propuestos**, sujetos al inventario de código M01.03 y al diseño M02: no afirman que exista ningún endpoint.

## 2. Actores y permisos propuestos A01–A14

| Actor | Objetivo futuro | Autoridad y límite de confianza |
| --- | --- | --- |
| A01 Administrador de empresa | Parametrizar habilitación del rubro y sucursales | Requiere membresía vigente y RBAC en backend; no hereda permisos de otra empresa |
| A02 Gerente de sucursal | Gestionar mesas y menú de sede | Solo recursos propios de la sucursal autorizada |
| A03 Mesero | Abrir/ver pedidos asignados a mesa | No modificar caja, impuestos ni permiso de otros roles |
| A04 Operador de cocina | Leer cola de preparación y avanzar estados permitidos | Sin acceso a PII ni cobros; filtrar empresa/sede |
| A05 Cajero | Validar cierre comercial autorizado | No afirmar pago sin confirmación transaccional servidor |
| A06 Cliente visitante anónimo | Consultar menú público mínimo de la mesa | Sin privilegios de operador; QR no equivale a autenticación |
| A07 Cliente autenticado | Consultar su propio pedido si ese flujo se aprueba | JWT no implica pertenencia a mesa ajena |
| A08 Supervisor de operaciones | Observar incidencias autorizadas | Auditoría filtrada por tenant, sede y rol |
| A09 Servicio de catálogo | Proveer oferta, variantes y disponibilidad canónica | Nunca confiar en precio/stock recibido del cliente |
| A10 Servicio de inventario | Reservar o revertir stock si aplica | Transacciones verificadas; sin movimiento por lectura de QR |
| A11 Servicio de pedidos y ventas | Aplicar estados, totales y políticas vigentes | Validación de negocio y pertenencia backend |
| A12 Servicio de facturación/caja | Registrar cierre conforme a contrato existente | No generar factura ni liquidar en este documento |
| A13 Auditor de seguridad | Revisar correlación y rechazo de abuso | No acceder a claves, JWT, PII o datos de otra empresa |
| A14 Administrador técnico de plataforma | Operar observabilidad con permiso explícito | No es superusuario comercial implícito ni bypass tenant |

Estos actores son **roles conceptuales**, no nuevas entradas de RBAC. Una UI de menús diferente no otorga permisos. Roles revocados o sesiones caducadas deben denegarse en cada operación protegida.

## 3. Contratos de entradas/salidas I01–I14 — FUTUROS NO IMPLEMENTADOS

| Contrato | Entrada no confiable / origen | Salida mínima autorizada o error seguro |
| --- | --- | --- |
| I01 Resolver QR | Referencia opaca QR, origen y contexto de host | Resolución mesa/sede publicada si firma, vigencia y revocación son válidas; en otro caso rechazo uniforme |
| I02 Publicar menú | Selección de productos por administrador autorizado | Versión pública filtrada por empresa/sucursal y visibilidad, sin costos internos |
| I03 Leer menú | Mesa válida o contexto público | Productos visibles, variantes y precios del servidor, sin PII |
| I04 Abrir pedido | Mesa, líneas y clave de idempotencia del cliente | ID opaco de pedido y estado auténtico; nunca total suministrado como autoridad |
| I05 Modificar líneas | Pedido, variante, cantidad y versión esperada | Totales recalculados y conflicto seguro ante versión obsoleta |
| I06 Asociar mesa | Identificador de mesa, pedido y contexto validado | Vinculación solo dentro de empresa/sede; no mover pedidos ajenos |
| I07 Consultar pedido | Referencia de pedido y credenciales/claim público limitado | Proyección mínima de líneas y estado para titular/rol permitido |
| I08 Aceptar cocina | Pedido y transición solicitada por operador | Estado permitido y auditoría, o denegación sin efectos |
| I09 Marcar preparación | Pedido, versión y actor cocina | Evento de estado idempotente según máquina futura |
| I10 Servir/entregar | Pedido, mesa y confirmación autorizada | Estado de entrega consistente, sin cobrar |
| I11 Solicitar cierre | Pedido, versión y actor caja | Resultado definitivo condicionado a comprobaciones de pago/factura/stock |
| I12 Cancelar/revertir | Motivo saneado, estado y permiso | Compensación segura y evidencia de reversión, nunca borrado silencioso |
| I13 Recuperar fallo | Correlation ID y misma clave/payload idempotente | Resultado previo o fallo seguro, sin duplicar pedido/pago |
| I14 Consultar auditoría | Filtro por tenant/sede y rol auditor | Resultado paginado y saneado, sin tokens, datos ajenos ni secretos |

**Reglas comunes:** `EmpresaId`, `SucursalId`, `MesaId`, actor, `isPaid`, precio, IVA, total y flags del cliente son datos **no confiables**. El servidor resuelve identidad, contexto y pertenencia canónica, verifica token/roles cuando corresponde, impone versionado optimista y no publica ID internos sensibles en vistas anónimas. Errores 400/401/403/404/409/410 futuros deberán decidirse con política uniforme para no filtrar existencia entre tenants.

## 4. Superficies y fronteras de confianza F01–F09

| Frontera | Entrada → destino | Control futuro y denegación segura |
| --- | --- | --- |
| F01 Cámara/QR → navegador | URL y payload manipulables | Tratar como referencia pública no confiable; prevenir copia, expiración y replay |
| F02 Navegador → API pública | Menú/mesa anónimos | Sólo proyección publicada, rate limiting, CORS/headers según servidor |
| F03 Angular operador → API protegida | JWT, claims, sede seleccionada | Revalidar JWT/membresía/rol/contexto en backend; no confiar en guard de UI |
| F04 API → Application | DTOs, ids y versiones | Validación semántica, pertenencia y máquina de estados; fallar cerrado |
| F05 Application → Domain | Transiciones de pedido | Estados y montos invariantes; no efectos externos sin regla explícita |
| F06 Application → Infrastructure/MySQL | Tenant, sede, mesa, pedido, stock | Filtro compuesto real, concurrencia y unidad transaccional |
| F07 Pedido → cocina/caja | Actualizaciones y correlación | Menor privilegio; no mezclar cocina, caja ni sucursales |
| F08 Pedido → factura/stock/pago | Resultado comercial | Consistencia y reversión; sin proveedor de pago activado por documentación |
| F09 Auditoría → observabilidad | Identidad, tenant y outcome | Minimizar PII; sin JWT, QR secreto, PAN o datos del tenant B |

Cada petición protegida deberá ensayarse con **tenant A y B** y **sucursales diferentes**, tanto positiva como negativamente. Una mesa válida en B debe permanecer inaccesible desde A aunque la referencia exista o haya sido copiada. QR firmado y expiración por sí solos no conceden permisos de cocina, caja o facturación.

## 5. Escenarios de aceptación futuros y separación de evidencia

- **P01/N01:** QR vigente de mesa A muestra sólo menú público A / QR falso o revocado no revela datos.
- **P02/N02:** mesero autorizado abre pedido de sede A / IDOR con mesa B es rechazado sin persistencia.
- **P03/N03:** cocina autorizada avanza un pedido de su sede / cocinero sin rol intenta avanzar pedido ajeno.
- **P04/N04:** caja autorizada cierra una vez un pedido válido / replay o versión obsoleta no duplica cierre, stock ni factura.
- **P05/N05:** reintento con misma clave devuelve el mismo resultado / clave reutilizada con payload diferente falla sin efecto.
- **P06/N06:** UI accesible refleja estado servido por API / desconexión, 401 o 409 no exhibe confirmación ficticia.

**Todos son FUTUROS NO_EJECUTADOS.** Validarlos en implementación requerirá .NET/xUnit, MySQL efímero para aislamiento y transacciones, pruebas Angular/Playwright si existe UI y seguimiento de SHA exacto en GitHub Actions. Redactarlos hoy no es un PASS funcional, de seguridad, de integración o de comercio.

## 6. Dependencias, impacto y revisión

En la lectura inicial del 2026-11-14, `PLAN_MAESTRO!A1191:I1191` indicaba M01.02 libre; `COLA!G1191/O1191` y `DAG_TECNICO!F1152/G1152` indicaban `SI/SI` y `VALIDADO/SI`. Las fórmulas son elegibilidad para **análisis documental**, no permiso de M01.06, IAM, JWT/RBAC, DB, migraciones, infraestructura, R2 ni QA/main/PROD. El scope sigue limitado a `docs/v5` y `CHANGELOG_AI.md`.

**Impacto:** ninguna capa de código ni consumidor API alterado, ninguna migración/tabla/secret/worker/cuenta configurada; la seguridad es contrato para verificar posteriormente. Observabilidad futura: correlación y auditoría con tenant/sede/actor sin información sensible. **Rollback:** `git revert` normal del commit documental y evento compensatorio institucional, no eliminación de historial.

**A0:** documento y CHANGELOG consistentes, sin encabezado sin viñetas y sin secretos. **A1:** comprobar la trazabilidad de catorce actores, catorce contratos y nueve fronteras (revisión documental, no tests runtime). **A2:** revisar amenazas QR falso, mesa B, replay, cierre y JWT/RBAC A/B como diseño solamente. **A3:** contraste documental con M01.01, plan V5 y arquitectura; CI .NET/Angular/MySQL/Playwright no es PASS por redactar este documento. **A4:** publicar commit fast-forward y certificar `verify-solqaryn-project-scope` y procedencia V5 en SHA exacto, después readback PLAN/COLA/BITACORA. **A5:** revisión independiente por cualquiera de las otras cinco Tasks Javier; jamás autoaprobación.

Sin compra, activación de eventos, Webhooks, pagos, producción, main, QA ni migración de recursos.
