# RUNBOOK — N2.5 Three-Way Match

## Operación estándar
La evaluación se consulta mediante `GET /conciliacion/ordenes-compra/{ordenCompraId}/three-way-match`. Es una operación de lectura protegida por autenticación y permiso `Compras/Ver`.

## Evidencia elegible
- Recepciones: solo `EstadoRecepcionCompra.Recibida`.
- Facturas: solo `EstadoFacturaProveedor.Registrada`.
- Comparación: exacta, sin FX ni tolerancias implícitas.

## Casos operativos

### Orden no encontrada
El servicio produce `ResourceNotFoundException`. Confirmar el identificador y que la orden exista; no crear datos artificiales para satisfacer la consulta.

### Evidencia inestable
Si el total/conjunto paginado cambia durante la evaluación, el servicio produce `BusinessRuleException` y falla cerrado. Dado que el endpoint es GET, puede solicitarse una nueva evaluación una vez estabilizada la evidencia; no transformar esta regla en retry automático para operaciones mutativas.

### Discrepancia de moneda
Se representa con `OrdenCompraDetalleId = 0`, tipo `Moneda`. No cambiar el sentinela por una FK artificial a un detalle inexistente.

### Violación `CK_ThreeWayMatchDiscrepancias_OrdenDetalleSentinela`
`OrdenCompraDetalleId` debe ser `0` para cabecera o un id positivo de detalle. Un valor negativo es defecto de mapeo/dominio y debe corregirse; no relajar el check.

### Estado/tipo fuera de enum
- Resultado: 0 Pendiente, 1 Aprobado, 2 Discrepancia.
- Tipo: 1 Cantidad, 2 Precio, 3 Descuento, 4 Impuesto, 5 Moneda.
No ampliar enums desde operación sin cambio de contrato aprobado.

### Problemas MySQL / red
No atribuir causa sin logs correlacionados. Registrar `causa no determinada`, conservar evidencia y revisar el fallo causal. Está prohibido asumir que un timeout es ambiental o seguro para reintentar una transacción.

## Validación de release
Antes de promover N2.5:
1. build/tests backend y frontend aplicables;
2. tests focalizados ThreeWayMatch;
3. QA/regresión y RBAC;
4. CI causal del HEAD final;
5. cero P0/P1;
6. documentación/certificación reconciliada.

## Rollback
El `Down()` de N2.5 elimina las tablas de resultado/discrepancias. Tratarlo como destructivo: backup, quiescencia, autorización, restore plan y postchecks obligatorios. Sin ellos, ABORT.

## Seguridad
No tocar main/Producción, no merge/force-push, no secretos, no cambios productivos desde este runbook.
