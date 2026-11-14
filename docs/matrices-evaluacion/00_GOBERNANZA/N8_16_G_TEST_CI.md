# N8.16.G — TEST_CI / gobierno de matrices

Estado: `LISTO_REAL`.

## Checks automatizados

El validador canónico es `scripts/validate_matrix_governance.py` y el gate dedicado es `.github/workflows/matrix-governance.yml` (`Desarrollo - Gobierno de matrices`). El contrato de regresión en backend es `backend/tests/Solqaryn.Tests/MatrixGovernanceContractTests.cs` y forma parte del gate causal de `Desarrollo`.

Debe rechazar, al menos:

- `MATRIX_ID` duplicado o fuera del patrón estable;
- IDs basados en row/index/posición visual;
- parent desconocido;
- columnas canónicas vacías en el catálogo;
- implementación root duplicada dentro del catálogo inicial;
- diferencia entre conteo declarado y filas reales;
- falta de campos obligatorios de identidad/ownership/data/backend/frontend/security/evidence en la plantilla;
- matrices materializadas incompletas: si un documento fuera de `00_GOBERNANZA` declara `MATRIX_ID:`, todos los campos obligatorios deben existir y tener valor; cuando un campo no aplique debe usarse `N/A:<reason>`;
- placeholders injustificados (`TBD`, `TODO`, `POR DEFINIR` o `PENDIENTE DE DEFINIR`) en campos obligatorios de una matriz materializada o en columnas canónicas del catálogo;
- ausencia del invariante `MATERIAL_WITHOUT_ID` prohibido;
- pérdida de cobertura de cualquiera de los cinco tipos UI representativos exigidos por N8.16.G.

## Muestras representativas de contrato UI

Estas muestras son **fixtures de gobierno, no inventario de producto ni afirmación de implementación**. Su única función es hacer reproducible el contrato de clasificación antes de que los módulos posteriores materialicen matrices visuales concretas.

| muestra | CONTRACT_KIND canónico | semántica mínima validada |
|---|---|---|
| `screen` | `SCREEN` | superficie navegable con estado e interacción material |
| `dialog` | `BUSINESS_DIALOG` | diálogo de negocio con entrada/validación/resultado |
| `widget` | `EMBEDDED_INTERACTIVE` | unidad embebida con datos, estado o interacción material |
| `shell` | `SHELL` | contenedor/ruta/menú que organiza superficies dependientes |
| `shared` | `SHARED_PRIMITIVE` | primitiva compartida con contrato material reutilizable |

El test `MatrixGovernanceContractTests` debe leer esta tabla y exigir exactamente las cinco parejas anteriores. Si falta una muestra, si se duplica una clave o si cambia su `CONTRACT_KIND`, el gate falla. Así la aceptación `screen/dialog/widget/shell/shared` queda comprobada por CI sin fabricar componentes productivos.

## FIRST_DETECTOR_OWNS_RECOVERY

REVIEW_FIRST detectó que la prueba anterior sólo verificaba strings del validador y no ejecutaba escenarios negativos. Al agregar self-tests ejecutables, el primer gate expuso además un falso negativo real: el extractor usaba `\s*`, que podía consumir el salto de línea y tratar el siguiente campo como valor de un campo vacío. La recuperación same-run quedó cerrada reemplazando ese match por whitespace horizontal (`[ \t]*`) y ejecutando en cada gate fixtures negativas para campo faltante, campo vacío, placeholder injustificado e ID inválido.

Evidencia REVIEW_FIRST: `vaep/evidence/reviews/N8.16.G_REVIEW_FIRST_20260916T015655Z_SUP48.json`. Resultado final: `P0=0`, `P1=0`.

## Gates causales

- Gobierno de matrices sobre `e33efc2810199985968bc2e1e6e43f69dcac816b`: run `35046042251`, job/check `104635949174`, terminal `SUCCESS`. Este run ejecutó el validador ya corregido y sus fixtures negativas.
- Backend Release/tests sobre `f1a19862158df6ade27911ea71986b5521829bf4`: run `35046000934`, job/check `104635828543`, terminal `SUCCESS`. Ese candidate contiene el contrato `MatrixGovernanceContractTests` que exige la presencia de los self-tests ejecutables.
- Los commits posteriores de REVIEW_FIRST/documentación son control/evidencia; no cambian producto, esquema, script ni test funcional. Cualquier cambio posterior en gobierno/script/test invalida esta equivalencia y exige nuevo gate causal.

El gate corre sólo sobre `Desarrollo`, no despliega y no toca datos. `N8.16.G` queda certificable en `LISTO_REAL` con REVIEW_FIRST `P0=0/P1=0`; su sucesor dependency-valid es `N8.16.H — DOC_CERT`.
