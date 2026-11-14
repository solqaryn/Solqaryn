# Certificación ERP-N3.11 — POS / Venta rápida

## Dictamen

N3.11.A-G están certificados para el alcance vigente reutilizando la autoridad existente de `Venta`; no existe una segunda superficie POS de dominio, persistencia, API o RBAC autorizada.

## Autoridad y alcance

- `Venta` permanece como autoridad comercial de la venta rápida.
- La experiencia existente `ventas/nueva` es la superficie de UI reutilizada.
- N3.11.F reutiliza autenticación, RBAC, auditoría, seguridad y observabilidad existentes de Venta.
- N3.11.G cierra QA/regresión/CI como alcance reuse-only / N/A product-delta.
- No se materializan por inferencia cashier/session/terminal, split tender/cambio combinado, suspensión/reanudación, receipt/reprint, offline mode, idempotencia POS específica ni permisos POS nuevos; esas capacidades permanecen `DECISION_PENDING` salvo requisito autoritativo posterior.

## Evidencia de cierre

- N3.11.E: baseline `aa62de2f9389b1d976c633bb2d99c979baaace44`, `LISTO_REAL`.
- N3.11.F: Issue #1108 `LISTO_REAL`, Development `33102110092=SUCCESS`, M13 `33102109772=SUCCESS`, P0=0/P1=0.
- N3.11.G: Issue #1120 `LISTO_REAL`; REVIEW_FIRST A #1117 PASS evidence-only, B #1119 rechazado/no integrado por contradicción contractual, C #1118 PASS evidence-only; P0=0/P1=0.
- Los workflows legacy no relacionados no se convierten en blockers sin evidencia causal.

## Cierre H pendiente

Este documento materializa la certificación canónica de N3.11.H, pero H no debe marcarse `LISTO` hasta completar y hard-verificar el rollup aditivo/history-preserving de `TASKS.md` y `CHANGELOG_AI.md`, reconciliar COLA/CONFIG/BITÁCORA/TAREAS_PROGRAMADAS y confirmar P0=0/P1=0 del cierre documental.

## Restricciones

Desarrollo únicamente. PR #2 debe permanecer Draft `Desarrollo → main`, sin merge. No tocar `main`, Producción, ramas nuevas, auto-merge, force-push, secretos ni deploy.
