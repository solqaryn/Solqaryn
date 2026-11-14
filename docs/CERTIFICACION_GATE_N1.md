# GATE-N1 — Certificación y cierre formal ERP-N1

Fecha: 2026-08-18 (America/Tegucigalpa).

## Dictamen

**GATE-N1 = LISTO.**

ERP-N1 queda formalmente cerrado y habilita ERP-N2 conforme a dependencias del VAEP v2.

## Dependencias obligatorias

Las diez unidades de inventario empresarial finalizaron su microtarea H en estado LISTO:

- N1.1.H — Sucursales.
- N1.2.H — Almacenes.
- N1.3.H — Ubicaciones internas.
- N1.4.H — Existencias por variante.
- N1.5.H — Kardex empresarial.
- N1.6.H — Transferencias.
- N1.7.H — Conteos físicos.
- N1.8.H — Reservas.
- N1.9.H — Series, lotes y vencimientos.
- N1.10.H — Costeo.

## Baseline técnico reciente

El último baseline funcional de la fase, `142435e063767e6106bdc8dad2ccb9dd7645f137`, fue certificado por:

- Desarrollo — Compilación y pruebas `#32134812652`: SUCCESS.
- Fase 8 — Validación completa automatizada `#32134812633`: SUCCESS.
- M10 — UI/UX empresarial y accesibilidad `#32134812567`: SUCCESS.
- Desarrollo — aceptación funcional integral `#32134812695`: SUCCESS.
- M13 — Auditoría integral y certificación final `#32134812757`: SUCCESS.
- Recuperación de migración MySQL `#32134812773`: SUCCESS.
- Backup operativo `#32134812761`: SUCCESS.
- Backup y restauración `#32134812681`: SUCCESS.

El paquete documental final de N1.10 fue publicado después como commits `[skip ci]`, sin modificar código ejecutable ni esquema.

## Cobertura de fase

ERP-N1 deja materializados y documentados:

- sucursales, almacenes y ubicaciones;
- existencia por variante como autoridad cuantitativa;
- Kardex empresarial;
- transferencias y recepción controlada;
- conteos físicos y ajustes trazables;
- reservas y prevención de overselling;
- lotes, series y vencimientos opt-in;
- política de costeo por empresa con Promedio/FIFO/Estándar.

Los tracks aplicables T0–T12 fueron cubiertos por los cierres individuales y la regresión transversal: arquitectura, persistencia/migraciones, seguridad, auditoría, QA, API, frontend/UX, performance/observabilidad, DevOps y documentación.

## Restricciones preservadas

- Sólo `Desarrollo` fue modificada.
- `main` y Producción permanecen fuera de alcance.
- PR #2 debe continuar abierto y Draft.
- Sin ramas adicionales, force-push, merge/auto-merge ni exposición de secretos.

## Handoff

Con GATE-N1 LISTO, el siguiente punto elegible del Plan Maestro es **N2.1.A — Solicitud de compra — Auditoría y preflight**, sujeto a la verificación operativa normal del runner.