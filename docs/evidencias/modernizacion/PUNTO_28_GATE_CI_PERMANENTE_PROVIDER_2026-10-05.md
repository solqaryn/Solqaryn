# Punto 28 — Gate CI permanente para provider

Fecha: 2026-10-05 (UTC)  
Repositorio: `solqaryn/Solqaryn`  
Rama de integración: `dev`

## Control permanente en el repositorio

El workflow `.github/workflows/modernization-phase6-mysql-ef-provider.yml` se ejecuta automáticamente para los cambios relevantes que llegan por `push` a `dev`, y para **todo** `pull_request` cuyo destino sea `dev`; el evento PR ya no tiene filtro de paths, por lo que siempre crea el check estable `Dictamen Fase 6`. También conserva `workflow_dispatch` como vía manual, no como única vía.

Para ambos eventos automáticos valida el destino/branch `dev`, el scope canónico del repositorio, net8/EF8/Pomelo 8 productivo y exactamente un provider productivo. Después corre el baseline MySQL, las comprobaciones físicas/funcionales y la lane aislada Oracle EF10/net10; el dictamen final falla cerrado como `STOP` si alguna autoridad o lane requerida falla. La lane candidata no modifica los proyectos productivos.

## Brecha de enforcement detectada

La regla activa de GitHub `SOLQARYN - Protección dev` (ruleset `22829243`) aplica a `refs/heads/dev`, pero actualmente sólo contiene `deletion` y `non_fast_forward`. No exige pull request ni el check `Dictamen Fase 6`. Por ello, el workflow es automático pero GitHub **todavía no lo hace obligatorio para integrar cambios**; no es correcto afirmar que por sí solo impide una regresión.

Para cerrar el punto, un administrador debe configurar el ruleset de `dev` para exigir PR y el check requerido **`Dictamen Fase 6`**. La integración GitHub disponible en esta sesión permite leer rulesets, pero no editarlos; no se simula ese cambio ni se declara cerrado sin readback posterior.

## Estado

**Punto 28: ABIERTO — enforcement externo pendiente.** La parte versionada ya garantiza que el evento PR a `dev` produzca el check aunque cambien archivos fuera de los paths tradicionales. La protección que obliga a respetar ese check aún debe confirmarse en GitHub.

- Ejecución previa del push automático: run `37265475905`, commit `a5c93f14d0a3ee29573e7f0dcd23d4addeddd699`.
- Ruleset vigente verificado por GitHub API el 2026-10-05: [SOLQARYN - Protección dev](https://github.com/solqaryn/Solqaryn/rules/22829243).
- Fase 7 permanece sin ejecutar.

MAPA_ARQUITECTURA: SIN_CAMBIO.
