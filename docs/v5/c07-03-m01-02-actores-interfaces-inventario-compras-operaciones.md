# SOLQARYN V5 — C07-03.M01.02 — Actores, interfaces y fronteras de inventario, compras y operaciones

**Autor:** PRIMARY DEV 05 — JAVIER | **Fecha:** 2026-11-14 | **Fuente ejecutable:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, C07-03.M01.02. **Naturaleza:** preparación documental, no funcionalidad nueva ni aprobación de accesos.

## 1. Entrada, salida y criterio de alcance

Esta unidad identifica actores, entradas, salidas, interfaces y superficies de confianza del bloque C07-03, preservando almacenes, compras, costeo, reservas, conteos y movimientos. **Entrada:** contexto empresarial autorizado por servidor, usuario/membresía/rol, sucursal/almacén, identificadores de documento y carga útil validada. **Salida esperada:** consulta o transición con estado, auditoría y correlación, delimitada a una empresa. **Criterio de esta M01.02:** inventario contrastable de interfaces y fronteras para las posteriores auditorías M01.03, M02 y pruebas M05; no certifica la operación.

Se hereda el contrato de no-regresión de `docs/v5/c07-03-m01-01-alcance-inventario-compras-operaciones.md` (NR01–NR12). El portal/ERP de cliente no decide la autoridad por `EmpresaId`, `SucursalId`, `AlmacenId`, `ProductoId`, documento, cabecera ni claim sin comprobación servidor. No cambiar roles, políticas, schemas, rutas o balances en esta microtarea.

## 2. Actores del contrato propuesto (A01–A12)

| ID | Actor o consumidor | Entrada/salida y derecho a comprobar |
| --- | --- | --- |
| A01 | Administrador autorizado de empresa | Habilitación de almacenes, sucursales y permisos; no autoridad sobre otros tenants |
| A02 | Responsable de sucursal | Consulta de inventario propio y movimientos físicos permitidos |
| A03 | Jefe de almacén | Recepción, ubicación y aprobación conforme roles, nunca por UI solamente |
| A04 | Operador de conteo | Captura detallada de cantidades, sin confirmar automáticamente ajustes |
| A05 | Aprobador de transferencias | Estados solicitar→aprobar→despachar→recibir con autorización separada |
| A06 | Responsable de compras | Solicitudes y órdenes, proveedor e idempotencia de creación |
| A07 | Operador de recepción de compras | Captura contra saldo de orden, sin doble recepción por retry |
| A08 | Encargado de reservas | Activar, consumir, liberar o cancelar según estado y permiso |
| A09 | Analista de costos autorizado | Consulta de costo y política cuando RBAC/Finanzas lo permite |
| A10 | Auditor de inventario | Lectura paginada, reconciliación y evidencias sin posibilidad implícita de mutar |
| A11 | Consumidor del storefront/catálogo | Proyección pública reducida, sin revelar stock privado ni costo |
| A12 | Servicio de Application/Infrastructure | Validación, transacción, correlación y auditoría en servidor; no constituye actor externo aprobado |

Estos son **roles y responsabilidades a verificar**, no pruebas de que exista cada perfil o de que tenga permisos asignados hoy. La matriz definitiva RBAC debe derivarse de configuraciones y pruebas causales.

## 3. Interfaces y superficies existentes o por auditar (I01–I12)

| ID | Interfaz / origen | Estado observado y límite |
| --- | --- | --- |
| I01 | `AlmacenesController`, ruta `almacenes` | GET/POST/PUT con [Authorize] y permisos Almacenes; empresa se verifica en servicios |
| I02 | `UbicacionesAlmacenController`, ruta `ubicaciones-almacen` | GET/POST/PUT y permisos específicos; evitar ubicación fuera de almacén autorizado |
| I03 | `OrdenesCompraController`, ruta `ordenes-compra` | POST exige `Idempotency-Key`; autorización por Compras; DB y replay no comprobados |
| I04 | `RecepcionesCompraController`, ruta `recepciones-compra` | Consulta de saldo de orden; POST exige `Idempotency-Key`; no afirmar doble retry probado |
| I05 | `ReservasInventarioController`, ruta `reservas-inventario` | Consulta, creación, consumo, liberación, expiración; permisos por acción |
| I06 | `TransferenciasInventarioController`, ruta `transferencias-inventario` | Solicitud, aprobación, despacho y recepción; auditabilidad sin inferir transacción efectiva |
| I07 | `ConteosInventarioController`, ruta `conteos-inventario` | Captura y ajuste borrador; exige confirmación posterior |
| I08 | `MovimientosInventarioController`, ruta `inventario/movimientos` | Lectura GET y paginación; filtros deben comprobar tenant en Application/Infrastructure |
| I09 | `CosteoInventarioController`, ruta `costeo-inventario` | Política vigente/histórica y modificación con permiso; auditoría y cierre quedan por probar |
| I10 | `ReportesInventarioReconciliacionController` | Ruta `inventario/reportes/reconciliacion`; `TenantPermissionContext`, scope guard y censura de costo sin Finanzas |
| I11 | `AjustesInventarioController` | Archivo de API localizado en árbol Git; endpoints, guards y efectos **PENDIENTES de inspección M01.03** |
| I12 | `frontend/src/app/features/*` consumidor ERP | Frontend consumidor a identificar por ruta/servicio exacto en M01.03; no es autoridad de seguridad |

### Blobs de evidencia (Git dev, lectura selectiva)

| Fuente | SHA |
| --- | --- |
| `backend/src/API/Controllers/AlmacenesController.cs` | `abcc1e615eb4f3d75704562ae8c82cf603839c2d` |
| `backend/src/API/Controllers/UbicacionesAlmacenController.cs` | `60ba7144d1c26db758679f2209450d7a5b2e9eac` |
| `backend/src/API/Controllers/OrdenesCompraController.cs` | `c57e143da4a0b5b6fba79a7b12a192d1c5b95d44` |
| `backend/src/API/Controllers/RecepcionesCompraController.cs` | `3c100f3907a10b5159d8bebc6c8d192057c1847d` |
| `backend/src/API/Controllers/ReservasInventarioController.cs` | `3cbddb32ea5696028eea5ac5f78e3f78a5971e89` |
| `backend/src/API/Controllers/TransferenciasInventarioController.cs` | `2a3c0ed7cea58cc09f610124b53ed47b43ab44ac` |
| `backend/src/API/Controllers/ConteosInventarioController.cs` | `f995bf6938fb4546168e52bf9c3c443a99f11843` |
| `backend/src/API/Controllers/MovimientosInventarioController.cs` | `8dd4e9c1ea3f56141de71f7aeefb2c0a8de171c2` |
| `backend/src/API/Controllers/CosteoInventarioController.cs` | `9a5ffd9852ecb1c89d5a47d30d21b3453b01a615` |
| `backend/src/API/Controllers/ReportesInventarioReconciliacionController.cs` | `b0ad8f5143fd34beca236c57beaabbf9941a9393` |

## 4. Fronteras de confianza B01–B10

- **B01 cliente→API:** JWT y RBAC del backend, nunca confiar en guardas UI para autorizar.
- **B02 usuario→empresa:** membresía, empresa activa, recurso y tenant resultan de servidor; rechazar IDs arbitrarios cruzados.
- **B03 empresa→sucursal/almacén:** jerarquía canónica; NULL heredado no se trata como global ni habilita stock.
- **B04 almacén→ubicación:** ubicación solo dentro de almacén propio, con estado y autorización coherentes.
- **B05 documento→estado:** el creador de borrador no recibe aprobación automática; transiciones inválidas fallan cerradas.
- **B06 compras→recepciones:** claves idempotentes, saldos, moneda y proveedor deben verificarse contra orden vigente.
- **B07 reserva→movimiento:** concurrencia e idempotencia deben impedir doble consumo de la misma unidad.
- **B08 transferencia→inventarios:** movimiento de salida y entrada no queda unilateral tras error parcial.
- **B09 costeo→reporte:** datos financieros solo a rol permitido; el controlador de reconciliación contiene censura condicional.
- **B10 API→persistencia/auditoría:** EF/MySQL, commits, reversión, correlación y logs deben ser probados, sin inferirlos de DTOs.

## 5. Oráculos futuros de aceptación (NO_EJECUTADOS)

**Positivos P01–P06:** P01 listar almacenes propios con permisos; P02 alta de orden idempotente por tenant; P03 recepción sin saldo excedido; P04 reserva/liberación y correlación conservadas; P05 transferencia propia conciliada origen/destino; P06 reporte autorizado oculta costos si falta permiso de Finanzas.

**Negativos N01–N10:** N01 GET almacén de B desde A; N02 ubicación de B; N03 POST orden con misma clave/payload distinto; N04 doble recepción concurrente; N05 reserva consumida dos veces; N06 transferencia origen=destino; N07 aprobación por rol sin permiso; N08 filtro de reporte de sucursal no autorizada; N09 costo financiero sin permiso; N10 error entre despacho/recepción con inventario unilateral.

**No ejecutados:** `P01–P06` y `N01–N10` son diseño de pruebas futuras; no hay readback HTTP/EF/MySQL, prueba A/B live, Playwright ni autorización productiva certificada por M01.02. La constatación de atributos `[Authorize]`/`[RequierePermiso]` no acredita aplicación completa ni ausencia de bypass.

## 6. Riesgos de integración, salida y reversión

M01.03 deberá contrastar fuentes, DTO, servicios, repositorios, query filters EF, pruebas, datos existentes y consumidores Angular, poniendo especial atención a aislamiento tenant, roles, historial, reintentos, balances y conciliación. M01.04 registrará riesgos/decisiones; M01.05 definirá aceptación cuantitativa. Los gates M01.06, M02/M03, seguridad, migraciones, worker, QA/main/PROD no se liberan por este texto.

**A0/A1/A2:** coherencia y revisión estática del documento y sus referencias; seguridad/tenant preservados porque el diff solo modifica Markdown. **A3:** NO_APLICA al runtime por ausencia de cambios ejecutables; ninguna prueba de API/DB se da por PASS. **A4:** exige fast-forward CAS, diff mínimo de 2 Markdown, Scope Lock y V5 provenance exact-SHA completados con éxito y readback institucional. **A5:** PENDIENTE de otra Task Javier distinta del autor. **Rollback:** revertir commit documental normalmente y actualizar trazabilidad sin borrar BITACORA.

**Resultado atómico:** tabla de 12 actores, 12 interfaces, 10 fronteras y oráculos positivos/negativos por ejecutar. No crea roles, endpoints, asientos, migraciones, workers ni permisos.
