# ADR — N2.5 Three-Way Match (Conciliación de Compras)

## Contexto
N2.5 reconcilia `OrdenCompra` (N2.2), `RecepcionCompra` (N2.3) y `FacturaProveedor` (N2.4) sin inventar tolerancias, FX ni efectos de CxP. El resultado conserva estado y discrepancias exactas.

## Decisiones
1. `backend/src/Domain/Entities/ThreeWayMatchResult.cs` es la entidad de resultado auditable y usa `ThreeWayMatchLineDiscrepancy` para discrepancias.
2. Solo recepciones `Recibida` y facturas `Registrada` son evidencia vigente; borradores y anulados no participan.
3. El algoritmo es exact/fail-closed: cantidad, precio, descuento, impuesto y moneda deben coincidir según el contrato vigente; no existen tolerancias implícitas.
4. `OrdenCompraDetalleId = 0` es el sentinela de discrepancias de cabecera, por ejemplo moneda.
5. Persistencia: `ThreeWayMatchResultados` referencia `OrdenesCompra` con `Restrict`; `ThreeWayMatchDiscrepancias` referencia al resultado con `Cascade` y no tiene FK dura a `OrdenCompraDetalles`, preservando el sentinela 0.
6. La migración `20260821053500_N2_5_ThreeWayMatchPersistencia` aplica checks de estado/tipo/sentinela y no realiza backfill legacy.
7. La API de evaluación es lectura (`GET`) y requiere autenticación + permiso `Compras/Ver`.

## Consecuencias
- Las discrepancias de cabecera pueden persistirse sin violar FK hacia detalles.
- La ausencia de FK física a `OrdenCompraDetalles` exige conservar la integridad lógica desde el dominio/servicio.
- La evaluación rechaza evidencia inestable en lugar de producir un match potencialmente incompleto.
- El `Down()` de la migración elimina las tablas N2.5 y por tanto es destructivo para la evidencia histórica; no se considera rollback seguro por defecto.

## Estado
**LISTO / CERTIFICADO.** ChatGPT/VAEP cerró N2.5.H después de reconciliar E/F/G y verificar sobre el mismo HEAD funcional `5022c04b74780af871ab9d56c58c376d57b6519e` los gates Development `32497393667`, Acceptance `32497393606`, Fase8 `32497393712` y M13 `32497393747`, todos `SUCCESS`, con P0=0/P1=0.
