# QA Takeover Evidence — N2.5.G ThreeWayMatch Regression

> Source input: Jules C `N2.5.G.C2` ATTEMPT=2/2. Jules exhausted its retry budget on the protocol evidence gate. ChatGPT/VAEP re-reviewed the regression ChangeSet and salvages the focused regression tests and factual evidence only; no Jules R3 is authorized.

## Validaciones cubiertas
- Match exacto sin tolerancias implícitas.
- Solo `EstadoRecepcionCompra.Recibida` y `EstadoFacturaProveedor.Registrada` participan como evidencia vigente.
- Discrepancia de moneda se registra a nivel de cabecera con sentinela `OrdenCompraDetalleId = 0`.
- Evaluación repetida es determinista y no introduce efectos laterales.
- Diferencias pequeñas de precio siguen siendo discrepancia; no se inventan thresholds ni FX.

## Pruebas
- Nueva suite: `backend/tests/Solqaryn.Tests/ThreeWayMatchRegressionTests.cs`.
- El artifact Jules reportó ejecución focalizada de dominio/application/regresión y `git diff --check`; ese reporte se conserva como input.
- La certificación final depende del CI causal del HEAD donde ChatGPT/VAEP integre este takeover.

## Límites
- No se declara PASS de integración contra una base de datos live si no fue ejecutada causalmente.
- No se promueve N2.5.G antes de N2.5.F.
- Ningún `COMPLETED` de Jules sustituye REVIEW-FIRST externo.

## Disposición
`QA_TAKEOVER_INTEGRATED_CANDIDATE / VALIDANDO`. No R3+. `LISTO` requiere CI/DoD causal y cero P0/P1.
