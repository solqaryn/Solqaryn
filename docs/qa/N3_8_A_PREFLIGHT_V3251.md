# N3.8.A — Nota de débito de cliente — Auditoría y preflight

## Dictamen

Estado de preflight: **APTO PARA CIERRE DE N3.8.A / IMPLEMENTACIÓN TODAVÍA NO AUTORIZADA FUERA DE N3.8.B+**.

Este documento fija únicamente el estado real, alcance, riesgos y decisiones pendientes del futuro bloque N3.8. No crea `NotaDebitoCliente`, no define todavía su lifecycle fiscal/contable y no promueve N3.8.B.

## CURRENT_CONFIRMED_FACT

- El repositorio `jmejia31/Solqaryn` en `Desarrollo` no contiene actualmente una entidad, servicio, controller, DTO, ruta frontend o migración denominada `NotaDebitoCliente`.
- N3.7 `NotaCreditoCliente` ya está cerrado y constituye únicamente un patrón adyacente observado; su semántica no se transfiere automáticamente a una nota de débito.
- La autoridad actual de facturación/venta continúa en los modelos y contratos existentes de `Factura` / `Venta`; cualquier relación futura de una nota de débito con esas autoridades deberá probarse antes de fijarla como contrato.
- N3.8.A es un preflight: no corresponde introducir todavía cambios grandes de dominio, persistencia, Application/API o frontend.

## SCOPE PROVISIONAL PARA N3.8.B+

Si la necesidad funcional/legal de NotaDebitoCliente queda confirmada por el contrato autoritativo del proyecto, el slice deberá evaluar como mínimo:

1. dominio e invariantes propias de `NotaDebitoCliente`;
2. relación —si aplica y se demuestra— con documentos de venta/facturación existentes;
3. persistencia, índices, cardinalidades y rollback derivados del dominio aprobado;
4. Application/API y RBAC únicamente después de aprobar contratos;
5. UI/UX y pruebas después de disponer de API autoritativa;
6. auditoría, seguridad, regresión y cierre documental en sus padres F/G/H.

## OUT OF SCOPE / NO INVENTAR EN N3.8.A

- lifecycle (`Borrador`, `Emitida`, `Anulada` u otros) no demostrado;
- numeración o requisitos fiscales;
- impacto automático en cuentas por cobrar, caja, saldo de factura o contabilidad;
- movimiento de inventario/Kardex;
- idempotencia, tolerancias monetarias o reglas de redondeo;
- vínculo obligatorio a `FacturaId`, `VentaId`, `DevolucionClienteId` u otra entidad;
- cardinalidad, FK, `DeleteBehavior`, índices o precisión SQL;
- endpoints, DTOs, permisos/RBAC o rutas frontend;
- selección de documentos o efectos downstream automáticos.

Todo lo anterior permanece **DECISION_PENDING/RISK** hasta evidencia autoritativa específica de N3.8.

## Riesgos principales

- Duplicar o contradecir la autoridad financiera/fiscal existente por copiar el diseño de `NotaCreditoCliente` sin fundamento.
- Introducir efectos contables o de inventario no aprobados.
- Diseñar persistencia antes de fijar cardinalidad y ownership del documento.
- Convertir patrones observados de módulos adyacentes en reglas universales.

## Estrategia de implementación segura

- N3.8.B debe fijar primero el contrato mínimo de dominio y separar explícitamente `CURRENT_CONFIRMED_FACT`, `OBSERVED_PATTERN` y `DECISION_PENDING`.
- N3.8.C deriva exclusivamente del dominio aprobado; ninguna migración debe adelantarse.
- N3.8.D/E/F/G/H permanecen dependency-blocked para promoción, aunque pueden recibir prewarm evidence-only bajo `WORK_CAN_PIPELINE__PROMOTION_CANNOT`.
- Cualquier decisión fiscal/legal no demostrada se mantiene fuera del código y se registra como blocker/decision pending, nunca se infiere.

## Estrategia de rollback

N3.8.A no modifica producto ni datos; rollback funcional = N/A. Los futuros cambios deberán tener rollback proporcional al slice y jamás ejecutarse en Producción durante esta fase.

## Validación del preflight

- inspección dirigida de ausencia de `NotaDebitoCliente` en el repositorio;
- revisión del cierre N3.7 únicamente como patrón adyacente;
- separación explícita de hechos, patrones y decisiones pendientes;
- no se creó código de producto, migración, API, frontend, rama, merge ni deploy.

## Criterio de salida N3.8.A

N3.8.A puede considerarse `LISTO` cuando este preflight quede registrado como autoridad de alcance y el selector rebindeé a N3.8.B sin convertir ninguna decisión pendiente en contrato. P0/P1 atribuibles al preflight: **0 conocidos**.
