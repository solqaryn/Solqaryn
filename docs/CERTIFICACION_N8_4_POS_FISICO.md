# Certificación ERP-N8.4 — POS físico

Fecha de cierre documental: 2026-09-15
Autoridad operativa: `docs/VAEP_AUTHORITY.md`
Rama certificada: `Desarrollo`

## Alcance

N8.4 certifica el alcance de POS físico sobre la rama `Desarrollo`: contrato de capacidades para scanner/impresora térmica, reutilización de superficies de producto compatibles y cierre del checkpoint físico sin fabricar evidencia de hardware. El cierre de N8.4.G se apoya en evidencia persistida de scanner y en la aceptación explícita del propietario para la validación de impresora; el controller no afirma una observación física adicional ni genera evidencia sintética.

## Cadena certificada

- N8.4.A PRE — `LISTO_REAL`: `vaep/evidence/receipts/N8.4.A_LISTO_REAL_20260915T080110Z_SUP48.json`.
- N8.4.B DOMAIN — `LISTO_REAL`: `vaep/evidence/receipts/N8.4.B_LISTO_REAL_20260915T080250Z_SUP48.json`.
- N8.4.C DB_MIG — `LISTO_REAL`: `vaep/evidence/receipts/N8.4.C_LISTO_REAL_20260915T080340Z_SUP48.json`.
- N8.4.D BACKEND_API — `LISTO_REAL`: `vaep/evidence/receipts/N8.4.D_LISTO_REAL_20260915T080440Z_SUP48.json`.
- N8.4.E FRONTEND_UX — `LISTO_REAL`: `vaep/evidence/receipts/N8.4.E_LISTO_REAL_20260915T080540Z_SUP48.json`.
- N8.4.F SEC_AUDIT — `LISTO_REAL`: `vaep/evidence/receipts/N8.4.F_LISTO_REAL_20260915T080630Z_SUP48.json`.
- N8.4.G TEST_CI — `LISTO_REAL` mediante reconciliación explícita del propietario: `vaep/evidence/receipts/OWNER_RECONCILIATION_N8.1.G_N8.1.H_N8.4.G_20260915T193239Z.json`.

## Contrato causal y checkpoint físico

El contrato canónico de capacidad física es `docs/N8_4_POS_PHYSICAL_CAPABILITY_CONTRACT.md`. El sistema distingue capacidades físicas disponibles/no disponibles y no presenta un fallback como si fuera éxito físico real.

La reconciliación del propietario declara explícitamente que la validación de impresora fue revisada/corroborada y aprobada, mantiene `P0=0` y `P1=0`, elimina el blocker de N8.4.G y registra `OWNER_ACCEPTED_PHYSICAL_POS_CLOSURE`. La misma evidencia declara `synthetic_evidence_created=false` y `operator_observation_claimed=false`; por tanto, esta certificación preserva la distinción entre aceptación del propietario y observación física directa del controller.

Los incidentes/revisiones históricos que registraron el bloqueo de hardware se conservan intactos como evidencia temporal y no se reescriben; la reconciliación posterior es la evidencia de resolución.

## Seguridad y límites

N8.4.H es un checkpoint documental. No introduce un delta de runtime de autenticación, RBAC, persistencia, migraciones, secretos, infraestructura ni datos productivos. La certificación no autoriza almacenar ni exponer material sensible y no sustituye controles de acceso existentes.

## Rollback

El rollback de este checkpoint documental consiste únicamente en revertir los artefactos documentales de N8.4.H si fueran incorrectos. No se requiere rollback de esquema, datos, secretos, despliegue o Producción por este cierre documental.

## Guardas

- `main`: intacta.
- Producción: intacta.
- deploy: no ejecutado.
- secretos: no tocados.
- PR #2: no merge, no auto-merge.
- evidencia física sintética: no creada.
- P0 abiertos atribuibles al checkpoint: 0.
- P1 abiertos atribuibles al checkpoint: 0.

N8.4.H sólo pasa a `LISTO_REAL` mediante REVIEW_FIRST documental, receipt verificable y reconciliación del control-plane; este documento por sí solo no sustituye ese cierre.
