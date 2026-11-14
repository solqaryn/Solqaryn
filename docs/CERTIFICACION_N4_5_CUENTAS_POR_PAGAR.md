# Certificación — ERP-N4.5 Cuentas por Pagar / Integración con Compras

## Alcance

ERP-N4.5 certifica la integración completa de Cuentas por Pagar con Compras sin crear una segunda autoridad financiera. El alcance N4.5 reutiliza la implementación canónica ya cerrada en ERP-N2.8 para obligación por factura de proveedor, contado/crédito, vencimientos, pagos parciales, anticipos, retenciones y saldo.

## Reutilización autoritativa

- Dominio/contratos: autoridad ERP-N2.8.B.
- Persistencia/migración: autoridad ERP-N2.8.C, incluida la migración canónica `20260822161500_N28_CuentasPorPagar`.
- Application/API: autoridad ERP-N2.8.D.
- Frontend/UX: autoridad ERP-N2.8.E.
- RBAC/auditoría/seguridad/observabilidad: autoridad ERP-N2.8.F.
- QA/regresión/CI: autoridad ERP-N2.8.G; baseline `f1b53dc55d623ccbd5d15d751addd1319e0fabf7`, con Development `#32602357277`, Acceptance `#32602357276`, Fase8 `#32602357307`, Recovery MySQL `#32602357294` y M13 `#32602357293` en `SUCCESS`; P0/P1=0.
- Documentación operativa: paquete ERP-N2.8 compuesto por `docs/ERP_N2_8_CUENTAS_POR_PAGAR.md`, ADR, OpenAPI, runbook, rollback y certificación canónica.

## Revalidación N4.5

N4.5.A-G fueron reconciliadas contra la autoridad vigente y cerradas únicamente cuando el alcance ya estaba cubierto o la evidencia exacta permitía justificar N/A sin duplicar dominio, schema, API, UI, seguridad ni pruebas. El baseline funcional vigente para el cierre N4.5 es `541ec12b72912c769c6f54b8821771e509818375`.

Los commits de control posteriores usados para coordinar Jules son manifests bajo `vaep/jules*` y no modifican producto. La equivalencia causal se verifica por diff antes del cierre H.

## Operación y rollback

No se introduce un segundo runbook ni un segundo procedimiento de rollback. Se reutilizan `docs/RUNBOOK_N2_8_CUENTAS_POR_PAGAR.md` y `docs/ROLLBACK_N2_8_CUENTAS_POR_PAGAR.md`, que siguen siendo la autoridad operativa para CxP.

## Dictamen

No existe delta productivo requerido por N4.5.H. El cierre documental se completa con esta certificación y la reconciliación aditiva/history-preserving de `TASKS.md` y `CHANGELOG_AI.md`. Solo después de verificar esos diffs, confirmar P0/P1=0 y ausencia de cambios productivos el controlador VAEP puede marcar N4.5.H `LISTO_REAL` y promover N4.6.A.
