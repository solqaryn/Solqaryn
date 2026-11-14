# N4.7.A — Asientos — Auditoría y preflight

## Estado

Preflight materializado sobre `Desarrollo` después del cierre real de `N4.6.H`. Este documento no promueve ni cierra `N4.7.B` y no declara `N4.7.A` como `LISTO_REAL` hasta que la certificación exact-head aplicable sea terminal y se mantenga `P0=0/P1=0`.

## Dependencia y alcance autoritativo

- Dependencia requerida: `N4.6.H` — satisfecha por el rollup canónico de N4.6.
- Scope del Plan Maestro: crear `AsientoContable` / `AsientoDetalle` garantizando `Débitos = Créditos`.
- Rama autorizada: `Desarrollo`.
- Fuera de alcance de este preflight: `main`, Producción, deploy, secretos, ramas nuevas, merge/auto-merge y el motor automático de contabilización de N4.8.

## Auditoría del estado actual

1. El dominio de `Desarrollo` no contiene una entidad top-level `AsientoContable` ni `AsientoDetalle` en el inventario actual de `backend/src/Domain/Entities` revisado para este preflight.
2. `AppDbContext` no expone `DbSet<AsientoContable>` ni `DbSet<AsientoDetalle>`; por tanto N4.7 no debe tratarse como funcionalidad preexistente ni cerrarse por N/A.
3. N4.6 ya aporta `CuentaContable`, jerarquía y seguridad del plan de cuentas. N4.7 debe reutilizar esa autoridad; no debe crear un segundo catálogo de cuentas.
4. N4.8 depende de N4.7 y es el motor de contabilización automática. N4.7 debe limitarse al modelo y operación de asientos, sin adelantar reglas automáticas de venta/compra/cobro/pago/inventario.

## Contratos mínimos derivados del Plan Maestro

La implementación posterior debe cubrir como mínimo:

- `AsientoContable`: identificador, fecha contable, concepto/descripción, estado/lifecycle, referencia/correlación auditable y colección de detalles.
- `AsientoDetalle`: referencia a `CuentaContable`, naturaleza débito/crédito expresada sin ambigüedad, importe decimal positivo y relación obligatoria con el asiento.
- Invariante duro: la suma de débitos debe ser exactamente igual a la suma de créditos antes de contabilizar/confirmar.
- Prohibir asientos vacíos, importes negativos/cero no válidos, cuentas inexistentes/inactivas o cuentas que no aceptan movimientos.
- La validación de balance y reglas críticas debe residir en backend/domain; el frontend no es autoridad.
- Persistencia N4.7.C deberá definir FK/índices/precisión decimal/migración/snapshot y rollback seguro sin tocar Producción.
- Application/API N4.7.D deberá preservar autorización, auditoría y respuestas consistentes con los patrones existentes.
- N4.7.E/F/G/H permanecen dependency-held hasta sus predecesoras reales.

## Riesgos y decisiones fail-closed

- No reutilizar `MovimientoFinanciero` como sustituto implícito de asiento contable: representa otro concepto y mezclar autoridades produciría doble semántica.
- No generar asientos automáticamente desde operaciones existentes dentro de N4.7; esa responsabilidad pertenece a N4.8.
- No aceptar desbalance por redondeo silencioso. La precisión y estrategia de redondeo deben quedar definidas explícitamente antes de cerrar persistencia/aplicación.
- No declarar `LISTO_REAL` por presencia de archivos o por dispatch; exige DoD, P0/P1=0 y gates aplicables terminales.

## Handoff dependency-valid

Si este preflight obtiene certificación exact-head terminal, el siguiente cierre candidato es `N4.7.B — Dominio y contratos`. El delta de B debe crear únicamente el modelo de dominio/contratos necesarios para asientos y sus invariantes, sin persistencia/API/UI adelantadas salvo lo estrictamente necesario para compilar contratos.
