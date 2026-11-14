# Certificación N3.6 — Devoluciones de cliente

## Dictamen

ERP-N3.6 tiene sus microtareas funcionales N3.6.A–G cerradas y certificadas. N3.6.H permanece reservado al cierre documental, reconciliación de evidencia y rollup; esta certificación no autoriza por sí sola un `LISTO` de H hasta completar el paquete documental aplicable, TASKS/CHANGELOG de forma preservadora, CI causal del HEAD documental y P0/P1=0.

## Evidencia funcional certificada

- Baseline funcional certificado: `6c5a3164ab11a1dcdcdfa9418c61bb0165251239`.
- N3.6.A — auditoría y preflight: LISTO.
- N3.6.B — dominio y contratos: LISTO.
- N3.6.C — persistencia, migración y datos: LISTO.
- N3.6.D — aplicación, servicios y API: LISTO.
- N3.6.E — frontend y UX: LISTO; control #633.
- N3.6.F — RBAC, auditoría, seguridad y observabilidad: LISTO; control #640.
- N3.6.G — QA, regresión y CI: LISTO; control #641.
- Development `#32913855654`: SUCCESS.
- Acceptance `#32913854936`: SUCCESS.
- Fase8 `#32913854958`: SUCCESS.
- M13 `#32913854923`: SUCCESS.
- P0/P1 bloqueantes conocidos atribuibles al baseline funcional: 0.

## Autoridad y límites

El cierre N3.6 conserva como autoridad el contrato materializado y certificado durante B–G. Esta certificación no inventa ni amplía semántica funcional. En particular:

- no traslada automáticamente reglas de `DevolucionProveedor` a `DevolucionCliente`;
- no autoriza efectos físicos o financieros distintos de los ya implementados y certificados;
- no crea endpoints, permisos, lifecycle, cardinalidades, idempotencia, rollback o política de inventario adicionales por analogía;
- no reabre N3.6.A–G por artifacts Jules tardíos o evidence-only;
- no habilita promoción de N3.7 mientras N3.6.H no esté cerrado formalmente.

## Rollback y recuperación

Ante una regresión atribuible a N3.6:

1. detener la promoción del parent afectado en `Desarrollo`;
2. preservar documentos históricos y datos ya materializados;
3. identificar el delta causal exacto en dominio, persistencia, Application/API, frontend o seguridad;
4. corregir forward-only sin force-push ni reescritura destructiva;
5. reejecutar únicamente la validación causal aplicable antes de reabrir promoción.

Producción queda fuera de alcance de este cierre.

## DoD de N3.6.H

Para declarar `N3.6.H = LISTO` todavía deben cumplirse conjuntamente:

- reconciliar la documentación canónica realmente aplicable sin convertir patrones adyacentes en requisitos ficticios;
- incorporar la certificación final y evidencia de cierre en `Desarrollo`;
- reconciliar `TASKS.md` y `CHANGELOG_AI.md` preservando íntegramente su historia;
- obtener CI causal válida del HEAD documental cuando corresponda;
- mantener P0=0 y P1=0;
- ejecutar selector fail-closed y rebind inmediato al primer parent dependency-valid posterior.

No se presume que ADR, ERD, OpenAPI o runbook adicionales sean obligatorios salvo evidencia autoritativa de aplicabilidad.

## Estado final de este documento

**Resultado:** evidencia canónica de certificación materializada para N3.6.H.  
**Promoción de H:** todavía bloqueada hasta completar el DoD documental anterior.  
**Siguiente parent permitido tras H:** N3.7.A, sujeto a selector y dependencias frescas.
