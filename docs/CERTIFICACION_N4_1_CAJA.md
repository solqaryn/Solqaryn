# N4.1 — Caja — Certificación de cierre

## Estado

**VALIDANDO / NO LISTO** hasta completar el rollup documental final y la revisión causal del controller sobre el estado exacto de `Desarrollo`.

Este documento registra evidencia verificable del parent N4.1 sin autorizar `main`, Producción, merge, auto-merge, force-push, secretos ni despliegues.

## Autoridad funcional

- SHA funcional/documental de referencia: `a1522d589940e87e6ca48dd8adf32d309cce2fb3`.
- El HEAD actual de `Desarrollo` puede contener commits posteriores exclusivamente de control VAEP/Jules; esos manifests no sustituyen la autoridad funcional.
- Runbook canónico: `docs/RUNBOOK_N4_1_CAJA.md`.
- Rollback/recuperación canónico: `docs/ROLLBACK_N4_1_CAJA.md`.

## Alcance certificado

N4.1 Caja conserva las capacidades existentes de Caja, CajaSesion y CajaMovimiento para apertura, operaciones/movimientos, arqueo y cierre, con autorización específica por acción, trazabilidad/auditoría y reglas fail-closed conforme a la implementación vigente.

No se certifican capacidades inexistentes. En particular, la revisión factual del repositorio no demuestra un controlador HTTP específico de Caja; por ello un gate OpenAPI/Swagger de Caja se clasifica **N/A** mientras esa superficie HTTP no exista. Esta clasificación no crea como requisito de cierre la implementación de un controlador nuevo.

## Evidencia CI y seguridad

Sobre `a1522d589940e87e6ca48dd8adf32d309cce2fb3` existen checks exact-head completados con resultado `success`, incluyendo el dictamen automatizado M13, dependencias productivas npm, dependencias vulnerables .NET y configuración/aislamiento/endurecimiento. El workflow genérico `Solqaryn CI` para ese SHA aparece `skipped`; por tanto no se contabiliza como PASS y no sustituye los checks causales aplicables.

La revisión del controller no mantiene P0/P1 funcionales reproducibles conocidos atribuibles a Caja en esta evidencia. Cualquier nuevo defecto P0/P1 reproducible reabre el gate y bloquea la promoción.

## Jules / QA takeover

Los resultados Jules son evidencia auxiliar y requieren review. Un `COMPLETED`, manifest o workflow no equivale a `LISTO`.

- B58 fue aceptado como evidencia read-only sin patch.
- C43 R2 FINAL falló su contrato operativo read-only; R3 queda prohibido y el cierre documental pasa a QA takeover del controller.
- D48 R2 FINAL falló su contrato operativo read-only al ejecutar pruebas pese a la prohibición explícita; R3 queda prohibido y la clasificación CI queda bajo QA takeover del controller.
- A58 ATTEMPT1 falló el contrato zero-write y fue enviado a R2 FINAL.

Los fallos operativos Jules anteriores no constituyen por sí mismos defectos funcionales de Caja, pero sus artifacts rechazados no pueden usarse como prueba positiva de cierre.

## Criterio de promoción

N4.1.H solo puede pasar a `LISTO_REAL` cuando se cumplan simultáneamente:

1. runbook, rollback y esta certificación estén presentes y consistentes;
2. `TASKS.md` y `CHANGELOG_AI.md` se reconcilien de forma aditiva/history-preserving;
3. gates causales aplicables estén verdes o clasificados N/A con evidencia;
4. P0=0 y P1=0 reproducibles atribuibles al parent;
5. no exista review terminal pendiente que pueda invalidar el cierre;
6. el controller persista el cierre en COLA/CONFIG/BITACORA y ejecute selector fail-closed;
7. exista evidencia obligatoria, actual y real del respaldo cifrado M11 de `Desarrollo`, junto con la validación aplicable de restauración/drill correlacionada con el artefacto cifrado, su checksum y metadatos.

Hasta entonces, **N4.1.H permanece EN_PROGRESO/VALIDANDO y no se promueve N4.2.A**.
