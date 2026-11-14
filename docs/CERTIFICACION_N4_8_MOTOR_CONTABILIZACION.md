# Certificación N4.8 — Motor de contabilización

## Alcance

Este documento consolida la evidencia de cierre técnico del punto ERP-N4.8 — Motor de contabilización, cuyo objetivo es generar asientos configurables para venta, compra, cobro, pago, inventario, costo de venta, devolución, ajuste, caja y banco.

La autoridad operativa permanece exclusivamente en `docs/VAEP_AUTHORITY.md`. Este documento es evidencia de certificación y no redefine reglas VAEP.

## Baseline funcional previo al rollup

- Rama: `Desarrollo`.
- Baseline funcional revisado: `94f8133e038da205fc5af8b09d6e98e5eeca5352`.
- PR #2: `Desarrollo -> main`, OPEN + DRAFT, sin merge.
- `main` permanece congelada en `85b4e02814823e9671803c23798a6ff0bf05c8f6`.
- Producción, secretos y deploy quedan fuera de alcance.

## Evidencia A–G

El estado operativo canónico certificó secuencialmente N4.8.A–N4.8.G antes de promover N4.8.H. El último cierre funcional previo a esta documentación es N4.8.G, certificado mediante REVIEW_FIRST sobre el baseline `94f8133e038da205fc5af8b09d6e98e5eeca5352`.

En ese baseline se observó la matriz de GitHub Actions terminal para los checks aplicables. `Solqaryn CI` con conclusión `skipped` no se utiliza como PASS. Los gates históricos/legacy que no son causales al delta de N4.8 tampoco se reinterpretan como PASS.

## Seguridad y defectos bloqueantes

En la revalidación inmediatamente anterior a este rollup:

- Issues P0 abiertos: 0.
- Issues P1 abiertos: 0.
- PR #2 permanece OPEN + DRAFT.
- No se autoriza merge a `main`.
- No se autoriza deploy ni modificación de Producción.

## Criterio de N4.8.H

N4.8.H sólo puede declararse `LISTO_REAL` después de:

1. REVIEW_FIRST de este rollup documental y de la evidencia canónica afectada.
2. Confirmar que el changeset es history-preserving y no altera comportamiento funcional.
3. Drenar la CI causal del exact-head documental y clasificar únicamente gates aplicables.
4. Revalidar P0=0 y P1=0.
5. Persistir el cierre en COLA/CONFIG/EJECUCION_MANUAL/BITACORA.

Por tanto, la creación de este documento **no equivale por sí sola a `LISTO_REAL`**.

## Rollback

El rollback de este changeset documental consiste exclusivamente en revertir este archivo de certificación. No requiere migraciones, cambios de base de datos, infraestructura, secretos ni despliegues.

## Estado al materializar

`N4.8.H = VALIDANDO / DOC_CERT_MATERIALIZED / CI_CAUSAL_PENDING`.

El cierre formal del parent N4.8 queda condicionado al checkpoint final descrito arriba.