# RUNBOOK N3.6 — DEVOLUCIONES DE CLIENTE

## Propósito

Runbook operativo y de verificación del bloque ERP-N3.6 después de su certificación VAEP.

## Flujo funcional congelado

1. Crear una devolución conforme al contrato de N3.6.
2. Mientras esté en `Borrador`, solo las operaciones permitidas por dominio/RBAC pueden modificarla o confirmarla.
3. `Confirmar` requiere estado `Borrador` y permiso `Ventas/Confirmar`.
4. `Anular` requiere estado `Confirmada` y permiso `Ventas/Anular`.
5. Cualquier transición inválida falla cerrada; la UI no debe ofrecer acciones incompatibles con el estado.

## Verificación mínima

Antes de atribuir una regresión a N3.6:
- reproducirla sobre `Desarrollo`;
- identificar el HEAD funcional afectado;
- verificar autenticación y permisos runtime;
- revisar auditoría/correlation cuando aplique;
- ejecutar pruebas focalizadas del dominio/servicio/API/frontend afectado;
- usar Development/Acceptance/Fase8/M13 solo cuando sean causales para el changeset.

Baseline de certificación funcional: `6c5a3164ab11a1dcdcdfa9418c61bb0165251239`.

Gates certificados:
- Development `#32913855654` — SUCCESS.
- Acceptance `#32913854936` — SUCCESS.
- Fase 8 `#32913854958` — SUCCESS.
- M13 `#32913854923` — SUCCESS.

## Incidentes y recuperación

- No tratar un workflow legacy no causal como defecto de N3.6 sin evidencia directa.
- Un resultado Jules `COMPLETED` requiere REVIEW_FIRST antes de cualquier integración.
- Un fallo de bootstrap/base/schema previo a una sesión útil no consume intento funcional Jules.
- ATTEMPT1 + R2 máximo; R3 está prohibido. Un R2 funcional rechazado transfiere ownership a ChatGPT/VAEP QA_TAKEOVER.
- Mantener cambios de recuperación acotados al mismo parent y al scope causal.

## Persistencia y rollback

H no modifica esquema ni datos. Las operaciones de persistencia y rollback permanecen gobernadas por la implementación/migraciones certificadas en N3.6.C y por sus guards. No ejecutar DDL/DML manual desde este runbook. Ante un problema de datos, identificar primero la migración/operación causal y aplicar corrección forward-only o el mecanismo de rollback ya certificado para ese changeset.

## Cierre y continuidad

N3.6.H solo se considera cerrado cuando:
- N3.6.A–G están `LISTO`;
- la documentación canónica, `TASKS.md` y `CHANGELOG_AI.md` están reconciliados preservando historial;
- no existen P0/P1 bloqueantes conocidos;
- el selector fail-closed deja `N3.7.A` como siguiente parent dependency-valid.

Después del cierre, cualquier prewarm N3.7 continúa bajo `WORK_CAN_PIPELINE__PROMOTION_CANNOT` hasta satisfacer sus dependencias.
