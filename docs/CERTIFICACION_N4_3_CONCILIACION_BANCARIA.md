# ERP-N4.3 — Conciliación bancaria — Certificación canónica

## Estado

`N4.3.H — Documentación y certificación`

Baseline funcional certificado previo al paquete documental:

`ad0cf70fc6ced126de1878b61fe4ae02c8d41a01`

Repositorio: `jmejia31/Solqaryn`
Rama autorizada: `Desarrollo`
PR rector: `#2 Desarrollo -> main`, OPEN + DRAFT, sin merge ni auto-merge.
Producción: no tocada.

## Alcance funcional certificado

N4.3 implementa la conciliación bancaria necesaria para importar/registrar movimientos y relacionarlos con pagos, depósitos, transferencias y movimientos financieros existentes sin crear una segunda autoridad financiera.

La capacidad final incluye:

- contratos y lógica de conciliación en Application;
- superficie HTTP `ConciliacionBancariaController`;
- repositorio/DI runtime de conciliación y operaciones bancarias;
- frontend de cuentas/conciliación con UX y pruebas asociadas;
- RBAC granular para consultar/importar/editar;
- auditoría de operaciones sensibles;
- observabilidad mediante logging estructurado;
- pruebas de autorización, auditoría y observabilidad;
- regresión CI exact-head.

## Autoridad y límites

La conciliación relaciona información bancaria con autoridades financieras ya existentes. No autoriza duplicar ledger, alterar Producción, inventar movimientos externos ni reconciliar silenciosamente discrepancias no demostradas.

Las operaciones protegidas permanecen fail-closed mediante autenticación/RBAC. Los cambios sensibles mantienen auditoría y la observabilidad no debe registrar secretos ni PII innecesaria.

## Evidencia CI exact-head

Sobre `ad0cf70fc6ced126de1878b61fe4ae02c8d41a01` los workflows aplicables observados quedaron terminales `SUCCESS`, incluyendo:

- M9 — Cargas masivas profesionales;
- M10 — UI/UX empresarial y accesibilidad;
- M11 — Backup y restauración en Desarrollo;
- M11 — Backup operativo de Desarrollo;
- M12 — Automatización transversal;
- ERP-N0.2 — CatalogoProducto legacy;
- ERP-N0.3 — ProductoVariante autoridad única;
- ERP-N0.4 — RBAC relacional;
- ERP-N0.5 — MetodoPago histórico;
- ERP-N0.6 — Referencias polimórficas;
- ERP-N1.1 — Sucursales;
- ERP-N1.2 — Almacenes;
- N2.3 — RecepcionCompra frontend CI;
- N2.3 — RecepcionCompra unit frontend;
- Fase 2 — auditoría de configuración y dependencias;
- Bloque 2C.1 — variante técnica y migración.

`Solqaryn CI` apareció `SKIPPED` y no se contabiliza como PASS.

P0/P1 atribuibles conocidos al cierre funcional: `0/0`.

## Rollback y operación

Este cierre documental no autoriza rollback destructivo de datos ni despliegue. Ante una regresión futura:

1. detener la promoción del padre afectado;
2. identificar el primer SHA causal en `Desarrollo`;
3. corregir forward-only cuando existan datos persistidos;
4. reejecutar gates aplicables sobre exact-head;
5. no tocar `main` ni Producción sin autorización expresa.

Para incidentes de conciliación se debe conservar trazabilidad del movimiento bancario, referencia financiera relacionada, usuario/correlation-id y resultado de la operación, evitando secretos y PII no necesaria.

## Checkpoint N4.3.H

El DoD técnico previo está satisfecho por N4.3.A-G y por la matriz exact-head verde del baseline funcional. Este documento materializa la certificación canónica de N4.3.H.

El cierre formal del control-plane sólo puede marcarse `LISTO_REAL` cuando `TASKS.md` y `CHANGELOG_AI.md` queden reconciliados de forma aditiva/history-preserving y el HEAD documental final vuelva a tener gates aplicables terminales sin P0/P1.

## Guardrails preservados

- rama única de trabajo: `Desarrollo`;
- `main` congelada;
- PR #2 OPEN + DRAFT;
- merge y auto-merge prohibidos;
- no ramas nuevas;
- no force-push;
- no secretos;
- no deploy;
- Producción no tocada.
