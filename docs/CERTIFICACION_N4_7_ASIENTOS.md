# Certificación ERP-N4.7 — Asientos contables

## Estado

Documento canónico de certificación para `N4.7 — Asientos`. Esta publicación materializa el cierre documental requerido por `N4.7.H`; **no declara por sí sola `LISTO_REAL`**. La declaración final corresponde exclusivamente a ChatGPT/VAEP después de REVIEW_FIRST, exact-head CI/gates aplicables terminales y `P0=0/P1=0`.

## Alcance certificado previo

El alcance funcional ya certificado en `N4.7.B`–`N4.7.G` introduce `AsientoContable` y `AsientoDetalle` reutilizando `CuentaContable` como autoridad del plan de cuentas. El modelo, persistencia, aplicación/API, frontend, RBAC/auditoría/seguridad y QA están cerrados mediante evidencia VAEP previa.

Invariante contable obligatorio:

```text
Total Debe = Total Haber
```

No se certifica en este punto un motor automático de contabilización de ventas, compras, cobros, pagos, inventario, costo de venta, devoluciones, ajustes, caja o banco; ese alcance pertenece a `N4.8` y posteriores.

## Evidencia funcional y QA

Baseline funcional/QA exacto:

```text
c8d1e373ba8ea008bf773e69afa10f5f18d6de8b
```

Evidencia de certificación integral utilizada por el controller:

```text
M13 run 33887431495 — attempt 2 — SUCCESS
P0 abiertos = 0
P1 abiertos = 0
```

El cierre `N4.7.F` incorporó cobertura dirigida de seguridad/auditoría para `AsientosContablesController`. `N4.7.G` reutilizó la misma autoridad exact-head para QA/regresión/CI sin fabricar suites duplicadas.

## Contrato funcional consolidado

- `AsientoContable` representa la cabecera auditable del asiento.
- `AsientoDetalle` representa las líneas Debe/Haber vinculadas a cuentas del plan vigente.
- Se exige al menos un conjunto de líneas válido y balanceado antes de aceptar la operación.
- Una línea no admite simultáneamente importe positivo en Debe y Haber.
- Los importes negativos son inválidos.
- La suma total de Débitos debe ser exactamente igual a la suma total de Créditos.
- `CuentaContable` permanece como autoridad de las cuentas; N4.7 no crea un catálogo paralelo.
- La superficie HTTP permanece autenticada y su autorización usa RBAC relacional; no se documenta ni autoriza bypass.
- El frontend consume la API certificada y conserva estados loading/error/vacío, validación del asiento y permisos de interfaz.

## Persistencia y migración

La persistencia de `AsientoContable`/`AsientoDetalle` y su snapshot EF fueron reconciliados antes de los cierres posteriores. La evidencia de `N4.7.C` certificó el modelo sin drift pendiente y preservó relaciones, índices, precisión monetaria y constraints aplicables.

Este documento no autoriza ejecutar migraciones contra Producción.

## Operación, rollback y recuperación

- Cualquier cambio posterior de schema debe continuar mediante migración forward-only revisada y sus gates aplicables.
- No se autoriza rollback destructivo improvisado sobre datos contables existentes.
- Ante un defecto posterior, preservar evidencia, detener promoción dependiente si compromete integridad y aplicar corrección forward o restauración desde un respaldo autorizado según el runbook transversal vigente.
- Ninguna acción de esta certificación toca Producción, secretos, dominios, certificados ni infraestructura productiva.

## Seguridad y observabilidad

La certificación previa de `N4.7.F/G` cubre autorización, seguridad HTTP y regresión aplicable sobre el exact-head funcional indicado. La observabilidad transversal existente se reutiliza; N4.7 no introduce secretos ni mecanismos alternativos de autenticación.

## Límites deliberados

Quedan fuera de N4.7:

- motor automático/configurable de contabilización (`N4.8`);
- períodos contables (`N4.9`);
- cualquier despliegue productivo;
- merge a `main`;
- modificación de secretos o infraestructura productiva.

## Criterio final de cierre H

`N4.7.H` puede pasar a `LISTO_REAL` únicamente cuando el controller confirme sobre el HEAD documental vigente:

1. REVIEW_FIRST del delta documental;
2. alcance limpio y history-preserving;
3. gates/CI aplicables terminales;
4. `P0=0` y `P1=0`;
5. rollup operativo sincronizado en las fuentes de estado;
6. promoción de `N4.8.A` solo después del cierre real de H.

Hasta entonces este archivo es evidencia documental preparada para certificación, no una auto-certificación de estado.
