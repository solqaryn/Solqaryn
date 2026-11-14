# Runbook N2.7 — Notas de crédito de proveedor

## Objetivo

Operar, validar y recuperar de forma controlada la funcionalidad N2.7 sin atribuirle responsabilidades de N2.8 ni garantías de infraestructura no demostradas.

## Verificación funcional

1. Confirmar autenticación y permiso `Compras:Ver` para consultas.
2. Crear una nota en `Borrador` con proveedor/factura válidos.
3. Verificar que sólo un borrador sea editable.
4. Registrar con permiso `Compras:Confirmar` y comprobar estado `Registrada`.
5. Verificar que el crédito acumulado no exceda el límite permitido por la factura.
6. Anular únicamente desde `Registrada` con permiso `Compras:Anular`.
7. Confirmar que una nota `Anulada` permanezca terminal.
8. Verificar que la UI no ofrezca `Anular` sobre `Borrador`.

## Diagnóstico

Ante un fallo:

- identificar SHA exacto y gate causal;
- revisar ProblemDetails/log correlacionado sin exponer secretos;
- separar defectos de datos, autorización, migración, concurrencia y UI;
- no reabrir puntos N2.7 ya certificados si el fallo pertenece a otro módulo.

## Persistencia y migración

Antes de una operación de rollback:

- confirmar que el rollback pertenece exclusivamente a N2.7;
- detener escrituras concurrentes sobre el área afectada;
- disponer de un mecanismo de preservación/restore verificado para el entorno si pudiera existir pérdida de datos;
- abortar si no puede garantizarse la recuperación necesaria.

El runbook no afirma que exista un `DownGuard` o un backup universal salvo evidencia explícita del entorno. El `Down()` de una migración no debe tratarse como sustituto de una estrategia de recuperación de datos.

## Gates de cierre

Baseline: `42f83b365392f45de39bd0e0ca4fa0638dd0eb10`.

- Development `32574284665` — SUCCESS
- Acceptance `32574284640` — SUCCESS
- Fase 8 `32574284638` — SUCCESS
- M13 `32574284639` — SUCCESS
- Recovery MySQL `32574284669` — SUCCESS
- M10 `32574284658` — SUCCESS

## Criterio de escalamiento

Un P0/P1 nuevo bloquea el cierre y debe corregirse dentro de N2.7.H o del punto causal correspondiente. Sin P0/P1 y con los gates aplicables verdes, se permite el rollup documental y la promoción del siguiente padre dependency-valid.
