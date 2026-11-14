# N4.2.A — Bancos — Preflight canónico (VAEP takeover)

## Estado y propósito

Preflight material para ERP-N4 / N4.2 Bancos. Este documento consolida el trabajo útil de Jules B/C y cubre por takeover de ChatGPT/VAEP los scopes A/D que agotaron lane budget. No promueve por sí mismo fases posteriores sin actualizar el control-plane.

Alcance funcional rector: crear `CuentaBancaria` y soportar depósito, retiro, transferencia, comisión, interés y preparación para conciliación bancaria, preservando el histórico existente.

## Evidencia REVIEW_FIRST

- Jules B: sesión `16440213122964364229`, run `33411850345`, artifact `9765905464`, COMPLETED. Patch útil y acotado de persistencia/datos.
- Jules C: sesión `12629408416847173955`, run `33411871318`, artifact `9766020701`, COMPLETED. Patch útil y acotado de API/UX.
- Jules A: sesión `6993927918434621830`, run `33411832005`, throughput stall tras actividad real; controller takeover aplicado al scope de dominio.
- Jules D: run `33411889330`, throughput stall; controller takeover aplicado al scope de RBAC/seguridad/QA.

## 1. Baseline reutilizable confirmado

- `backend/src/Domain/Entities/Catalogos/Banco.cs`: catálogo bancario normalizado; hereda `AuditableEntity` y declara explícitamente que será reutilizable para Tesorería/Cuentas Bancarias.
- `backend/src/Domain/Common/AuditableEntity.cs`: auditoría base de creación/actualización por usuario.
- `backend/src/Domain/Entities/MovimientoFinanciero.cs`: movimiento financiero genérico existente con tipo, categoría, monto, método de pago, módulo origen y referencias a compra/venta/factura.
- `backend/src/API/Controllers/FinanzasController.cs`: patrón vigente `[Authorize]` + `[RequierePermiso(ModuloSistema.Finanzas, ...)]`, endpoints de movimientos/resumen/anulación y `ApiResponse<T>`.
- `backend/src/Domain/Enums/ModuloSistema.cs`: existen `Finanzas = 7` y `Caja = 31`; no existe módulo `Bancos` a la fecha del preflight.
- `backend/src/API/Controllers/ConciliacionController.cs`: la ruta `conciliacion` existente corresponde a three-way-match de Compras; NO es conciliación bancaria y no debe reutilizarse semánticamente como si lo fuera.
- Persistencia histórica detectada por Jules B: `BancoConfiguration` y migración `20260812190253_ERP_N05_BancoNormalizadoCanonical.cs`, además de `FacturaPagos.BancoId` y snapshots bancarios.

## 2. Gaps funcionales concretos

NO_EXISTING_COMPONENT para el alcance N4.2: `CuentaBancaria`, movimiento bancario especializado, transferencia bancaria, comisiones/intereses bancarios y contratos de conciliación bancaria específicos. El catálogo `Banco` no sustituye una cuenta bancaria de la empresa.

Se requiere un modelo explícito para cuenta bancaria y movimientos con trazabilidad suficiente para operaciones financieras. La conciliación bancaria detallada se completa en N4.3, pero N4.2 debe dejar contratos y referencias que no bloqueen esa fase.

## 3. Dominio e invariantes requeridas para N4.2.B

### CuentaBancaria

- FK obligatoria a `Banco`.
- Identificación funcional de cuenta sin duplicados dentro del mismo banco/empresa.
- Moneda ISO de 3 caracteres o estándar canónico equivalente del proyecto.
- Estado explícito Activa/Inactiva; una cuenta inactiva no puede recibir nuevas operaciones de negocio.
- No eliminar histórico financiero por soft-delete o cambios de catálogo.
- El saldo debe mantenerse coherente con movimientos; cualquier saldo materializado debe actualizarse dentro de la misma transacción de aplicación que crea el movimiento.

### MovimientoBancario

- `Monto > 0`; el signo/efecto se deriva del tipo de movimiento.
- Tipos mínimos: Depósito, Retiro, TransferenciaSalida, TransferenciaEntrada, Comisión e Interés.
- Fecha de operación y fecha de registro separables cuando aplique.
- Referencia/origen trazable e idempotencia para escrituras repetibles.
- Las operaciones anuladas/revertidas no se borran físicamente.

### Transferencia

- Cuenta origen distinta de destino.
- Monto positivo.
- Débito y crédito deben ser atómicos y compartir correlación/idempotencia.
- No permitir salida desde cuenta inactiva; política de saldo insuficiente debe quedar explícita y testeada.

## 4. Persistencia y migración — findings de Jules B aceptados

- Crear tablas aditivas para cuentas/movimientos; evitar mutaciones destructivas de `Bancos` y `FacturaPagos`.
- Preservar `FacturaPagos.BancoId`, `BancoCodigoSnapshot` y `BancoNombreSnapshot`.
- Los `CajaMovimientos` históricos `DepositoBanco` no poseen `CuentaBancariaId`; cualquier relación retrospectiva debe ser nullable o usar una estrategia de backfill explícita y auditada, nunca inventar asociaciones históricas.
- Índices mínimos: cuenta/estado; movimientos por cuenta+fecha; idempotency key único cuando aplique.
- Constraints mínimos: monto positivo; cuentas distintas en transferencia; unicidad funcional de cuenta.
- Validar migración/snapshot/preflight/postcheck en MySQL 8.4 y rollback lógico documentado.

## 5. API/UX — findings de Jules C aceptados

- Reusar convenciones de `FinanzasController` y `ApiResponse<T>`; no crear contratos HTTP incompatibles.
- Lecturas deben permitir filtros por cuenta, rango de fechas, tipo/estado y paginación.
- Escrituras financieras deben ser idempotentes y devolver conflictos de negocio de forma consistente.
- Flujos UI requeridos posteriormente: mantenimiento de cuentas, movimientos, depósito/retiro/transferencia, comisión/interés y preparación de conciliación.
- Reusar patrones de `frontend/src/app/features/caja/caja-flujo-shell.component.ts` para estados de carga/error/vacío, responsive y accesibilidad; no acoplar Caja con Bancos de forma que rompa ownership.

## 6. RBAC, auditoría, seguridad y observabilidad — takeover D

- Baseline inmediato: `ModuloSistema.Finanzas` + `RequierePermiso`; la creación de un módulo `Bancos` separado se difiere a N4.2.F y debe justificarse antes de modificar `ModuloSistema`.
- Ningún endpoint bancario puede depender de `EsAdministrador` como bypass de autorización.
- Operaciones monetarias requieren actor, correlación/idempotencia, referencia, fecha y motivo para anulaciones/reversiones.
- No registrar números completos de cuenta, credenciales, secretos o payloads sensibles en logs.
- Transferencias y movimientos deben ser transaccionales y fail-closed ante errores parciales.
- QA posterior debe cubrir autorización denegada, idempotencia duplicada, cuenta inactiva, fondos insuficientes/política definida, transferencia misma cuenta, rollback ante fallo intermedio y preservación histórica.

## 7. Criterios de aceptación del preflight

1. Estado real y componentes reutilizables identificados con rutas concretas.
2. Gaps marcados como NO_EXISTING_COMPONENT donde corresponde.
3. Invariantes de dominio, persistencia, API/UX y seguridad definidas sin implementar fuera de fase.
4. Riesgos históricos y estrategia de rollback documentados.
5. N4.2.B puede iniciar con scopes disjuntos y verificables.
6. N4.2.C/D/E/F/G/H siguen bloqueados por su cadena normal; no se promocionan anticipadamente.

## Dictamen

`N4.2.A PRE = APTO_PARA_LISTO_REAL` por REVIEW_FIRST B/C + takeover A/D. No se detecta P0/P1 causal que impida comenzar N4.2.B. El siguiente paso correcto es subdividir N4.2.B en cambios de dominio/contrato no solapados, integrar REVIEW_FIRST y ejecutar pruebas dirigidas antes de promover N4.2.C.
