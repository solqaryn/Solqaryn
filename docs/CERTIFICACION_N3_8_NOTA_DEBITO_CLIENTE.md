# Certificación N3.8 — Nota de débito de cliente

## Dictamen
N3.8.A-H queda cerrado para el alcance autoritativo actual como **N/A CON EVIDENCIA / REQUISITO CONDICIONAL NO ACTIVADO**. No se afirma que `NotaDebitoCliente` haya sido implementada.

## Evidencia
- N3.8.A preflight: `034ec3305422016d6c571d0ffcf1332e3bbbe6b6`.
- N3.8.B dominio/contratos N/A: `affb58f2b9e7d8ab25c051fed5b9f4ee5f317584`.
- N3.8.C-G rollup N/A: `3a89725e4a76c4d85c0c4adc04f0affa4a61e79a`.
- No existe requisito legal/operativo autoritativo suficiente para fijar lifecycle, fiscalidad, contabilidad, cardinalidad o idempotencia de `NotaDebitoCliente`.

## DoD y seguridad
Delta funcional N3.8 = 0. No se modifican dominio, persistencia, API, frontend, RBAC, datos ni Producción. P0/P1 atribuibles conocidos bajo el requisito actual = 0.

## Regla de reapertura
Si legislación u operación exige NotaDebitoCliente en el futuro, N3.8 debe reabrirse desde dominio/contratos con requisito explícito antes de crear persistencia, API o UI.
