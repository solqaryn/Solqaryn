# Punto 28 — Gate CI permanente para provider

Fecha: 2026-10-05 (UTC)  
Repositorio: `solqaryn/Solqaryn`  
Rama protegida: `dev`

## Gate versionado

El workflow `.github/workflows/modernization-phase6-mysql-ef-provider.yml` corre automáticamente en los pushes a `dev` y en **todo** pull request dirigido a `dev`, sin filtro de paths para el evento PR. Publica el check estable `Dictamen Fase 6`, valida scope y provider productivo, ejecuta los probes MySQL/Oracle y falla cerrado como `STOP` ante cualquier lane o autoridad requerida fallida. `workflow_dispatch` se conserva como ejecución manual auxiliar.

La lane candidata es efímera: no altera los proyectos productivos ni promueve el TargetFramework.

## Enforcement GitHub — readback verificado

El ruleset activo **`SOLQARYN - Protección dev`** (ID `22829243`) aplica únicamente a `refs/heads/dev`. Su readback de GitHub API, posterior al guardado del 2026-10-05, confirma:

- regla `pull_request` activa; aprobaciones mínimas configuradas en `0`;
- regla `required_status_checks` activa con el contexto exacto **`Dictamen Fase 6`**, origen GitHub Actions (integration ID `15368`);
- reglas existentes de protección contra eliminación y force-push preservadas;
- sin bypass actors y sin exigir que la rama esté actualizada antes de integrar.

Los rulesets de `main` y `qa` se leyeron antes/después de esta operación y permanecen sin cambios. No se modificaron despliegues ni datos.

## Evidencia y validación

Sobre el HEAD `2f8c465cc0397c2e00a526dc0bc79c1d116587aa`, antes del readback final del ruleset:

- [Fase 6 DEV — run 37334513061](https://github.com/solqaryn/Solqaryn/actions/runs/37334513061): todos los jobs, incluido `Dictamen Fase 6`, `success`.
- [Aceptación integral DEV — run 37334512724](https://github.com/solqaryn/Solqaryn/actions/runs/37334512724): Playwright integral, SMTP y PDF, `success`.
- [Scope lock — run 37334512579](https://github.com/solqaryn/Solqaryn/actions/runs/37334512579): `success`.
- [VAEP — run 37334512834](https://github.com/solqaryn/Solqaryn/actions/runs/37334512834): `success`.

El PR que incorpora este readback a `dev` debe satisfacer el status check ahora requerido antes de poder integrarse; su ejecución se registra al terminar.

## Resultado y límites

**Punto 28: enforcement configurado y leído de vuelta; integración de esta evidencia pendiente del PR/check obligatorio.** El gate automático existe y GitHub ya exige tanto PR como `Dictamen Fase 6` para `dev`.

No se inició Fase 7, no se cambió `main`/PROD y el runtime productivo permanece en net8/EF8/Pomelo.

MAPA_ARQUITECTURA: SIN_CAMBIO.
