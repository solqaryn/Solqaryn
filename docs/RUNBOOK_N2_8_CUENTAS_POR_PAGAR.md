# Runbook — ERP-N2.8 Cuentas por Pagar

## Objetivo

Operar y diagnosticar la superficie de Cuentas por Pagar sin mezclarla con recepción física, inventario o contabilidad posterior.

## Preflight

1. Confirmar rama `Desarrollo` y versión desplegable aprobada.
2. Confirmar acceso a MySQL y backup reciente antes de cualquier operación de esquema.
3. Verificar migración `20260822161500_N28_CuentasPorPagar` aplicada y tablas `CuentasPorPagar` / `AplicacionesCuentaPorPagar` presentes.
4. Verificar health/readiness de API.
5. Verificar catálogo RBAC de Finanzas y usuario de prueba sin privilegios administrativos implícitos.

## Smoke funcional

- Consultar lista de obligaciones abiertas.
- Consultar por proveedor y por ID.
- Registrar desde una FacturaProveedor válida/Registrada; repetir la misma solicitud y comprobar idempotencia.
- Registrar una aplicación con referencia idempotente; repetirla y verificar ausencia de duplicado.
- Revertir el último movimiento elegible y comprobar trazabilidad.
- Comprobar que una FacturaProveedor no válida o transición inválida falla cerrado.

## Seguridad

- Anónimo debe ser rechazado.
- `Finanzas/Ver` habilita lecturas, no mutaciones.
- `Finanzas/Crear` gobierna el alta desde factura.
- `Finanzas/Editar` gobierna aplicación/reversión.
- Correlation ID y errores deben conservar el contrato transversal y no filtrar detalles internos.

## Diagnóstico

1. Correlacionar solicitud con logs/auditoría por correlation ID.
2. Revisar estado de FacturaProveedor antes de investigar la obligación.
3. Revisar idempotencia antes de repetir una mutación.
4. Verificar saldo original, saldo pendiente y aplicaciones no revertidas.
5. Ante discrepancia de esquema, detener mutaciones y usar preflight/postcheck de la migración; no improvisar DDL.

## Cierre operativo

Una ejecución es aceptable cuando Development, Acceptance, Fase8 y M13 del mismo HEAD son SUCCESS, las pruebas dirigidas no muestran P0/P1 y el rollback/data-safety queda documentado.
