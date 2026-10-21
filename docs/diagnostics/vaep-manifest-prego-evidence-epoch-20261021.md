# SOLQARYN — Certificacion del manifiesto logico PRE-GO (21/10/2026)

## Alcance

Solo registrar y certificar el manifiesto logico de GitHub `vaep/control/logical-lease-candidate.json`. No se habilita ejecucion comercial, no se modifica `CONFIG.RUNTIME_MODE`, no se actualiza `_AUTOMATION_SOURCE.WRITE_POLICY`, no se reclama microtarea, no se crea una Task, no se reintentan operaciones denegadas.

## Fuentes autenticadas

- Base inspeccionada: `dev` HEAD `e3decb9f7d54b5ee977500f35b55d9c5928c7f7b`; GitHub 30/30 check-runs SUCCESS e incluido `verify-solqaryn-project-scope`.
- Owner hold: hoja comercial `1gcVyCoyhLU0jFMwRtf0s5_x8FSnfBs38ojml1QF7Xwk`, `CONFIG!B18=PAUSADO`, `_AUTOMATION_SOURCE` 10/10 habilitadas con politica CAS antigua uniforme y `_RUNTIME_SOURCE` 1123 microtareas con **cero OWNER**, 0 `EN_PROGRESO` y 0 `VALIDANDO`.
- Orden del propietario: `_EVENTS_SOURCE` fila 94, `OWNER_ACCEPTS_2_EXISTING_GITHUB_DIAGNOSTICS_GATE_CLOSED` / `CLOSED_OWNER_ACCEPTED_NO_REPEAT_PROBES`. No pedir mas pruebas de escritura para esta certificacion administrativa.

## Las dos evidencias aceptadas

| Task | Identidad | Commit | Fingerprint |
| --- | --- | --- | --- |
| Primary :00 / `6ad7bbf79c488191bf893e6c08ea9b5f` | `jmejia31` | [bdee818f](https://github.com/solqaryn/Solqaryn/commit/bdee818f0da1eb4261b7f848673d3e3acac48779) | `VAEP_TASK_GH_WRITE:6ad7bbf79c488191bf893e6c08ea9b5f` |
| Supervisor :29 / `6abd6241c3e48191a8856ed7b0f42c5c` | `morales35alex` | [96317187](https://github.com/solqaryn/Solqaryn/commit/963171871e5eebbbd48d522d2ef847bfdffb044b) | `VAEP_TASK_GH_WRITE:6abd6241c3e48191a8856ed7b0f42c5c` |

Cada commit y archivo diagnostico temporal se leyo desde GitHub. Estas son evidencias administrativas expresamente aceptadas por el propietario, **no** readbacks directos del estado terminal de las Tasks ni autorizacion de una operacion de leasing.

## Epoch y contrato

- `routing_epoch=VAEP_LOGICAL_20261021_PILOT`; `routing_epoch_state=FROZEN_PRE_GO`.
- Cohorte ordenada e inmutable: [Primary :00, Supervisor :29].
- `mode=PRE_GO_READ_ONLY_CERTIFIED` y `activation_permission=OWNER_APPROVED_PRE_GO_ONLY`.
- Referencia del contrato tecnico: `scripts/vaep/logical_lease_dispatch.py` y [PR #3709](https://github.com/solqaryn/Solqaryn/pull/3709), fusionado, con `APPROVED` por `jmejia31` a las 21:58:12Z. Los casos aislados de Google Sheets verificaron 1/10 contendientes ganadores sobre el mismo scope, 10/10 scopes independientes y rechazo de un escritor obsoleto: `docs/diagnostics/vaep-logical-fence-provider-20261021.md`.
- Exclusividad futura: `AddNamedRangeRequest` inmutable, en el mismo lote atomico que la transicion; WIP=1, reviewer distinto, fencing y readback; nunca reintroducir CAS dual.
- **No GO comercial:** `evaluate_live_cutover_manifest` sigue denegando el manifiesto PRE-GO. Antes de cualquier claim material verificar el estado terminal de invocacion anterior del mismo ejecutor (no inventarlo), permiso efectivo de esa Task, HEAD vivo y CI exact-head, 10/10 politicas nuevas, 10 prompts y epoch coincidentes. Si alguna falta, bloqueo local fail-closed.

## Alcance de la certificacion

`evaluate_pre_go_manifest` y sus pruebas realizan una validacion separada de *preparacion* y rechazan cualquier certificado si se fabrican evidencias, se mezcla protocolo, se cambia epoch o se intenta elevar a ACTIVO sin gates. Esta certificacion prueba el contrato de preparacion, **no** afirma haber activado el runtime ni revoca la suspension posterior del propietario.

Rollback previo al GO: restaurar el manifiesto PRE-GO a la propuesta anterior; no tocar propietarios, leases, flags de Tasks ni estados comerciales. No migrar a `ACTIVE_CERTIFIED` por sustitucion manual de un string sin readback de los 10 policies.
