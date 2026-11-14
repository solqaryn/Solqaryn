# Certificación ERP-N8.11 — Seguridad

Fecha de cierre documental: 2026-09-15
Autoridad operativa: `docs/VAEP_AUTHORITY.md`
Rama certificada: `Desarrollo`

## Alcance

N8.11 certifica el punto ERP-N8 de Seguridad sobre `Desarrollo`: revisión dirigida de autenticación y autorización, aislamiento tenant, límites de uploads/imports, superficies públicas y webhooks, headers/configuración, rate limits, secretos/configuración y auditoría de vulnerabilidades de dependencias. El cierre no autoriza Producción, deploy, secretos ni cambios en `main`.

## Cadena certificada

- N8.11.A PRE — `LISTO_REAL`: `vaep/evidence/receipts/N8.11.A_LISTO_REAL_20260915T211514Z_SUP48.json`.
- N8.11.B DOMAIN — `LISTO_REAL`: `vaep/evidence/receipts/N8.11.B_LISTO_REAL_20260915T211710Z_SUP48.json`.
- N8.11.C DB_MIG — `LISTO_REAL`: `vaep/evidence/receipts/N8.11.C_LISTO_REAL_20260915T211745Z_SUP48.json`.
- N8.11.D BACKEND_API — `LISTO_REAL`: `vaep/evidence/receipts/N8.11.D_LISTO_REAL_20260915T214530Z_SUP24.json`.
- N8.11.E FRONTEND_UX — `LISTO_REAL`: `vaep/evidence/receipts/N8.11.E_LISTO_REAL_20260915T214812Z_SUP24.json`.
- N8.11.F SEC_AUDIT — `LISTO_REAL`: `vaep/evidence/receipts/N8.11.F_LISTO_REAL_20260915T214942Z_SUP24.json`.
- N8.11.G TEST_CI — `LISTO_REAL`: `vaep/evidence/receipts/N8.11.G_LISTO_REAL_20260915T215112Z_SUP24.json`.

## Evidencia causal

La implementación material de N8.11.D incorporó contratos de regresión sobre límites de seguridad del backend, incluyendo autenticación requerida en pagos online, límites de tamaño en cargas masivas, rate limiting explícito en la tienda pública y restricción del anonimato al método de ingreso del webhook público. Los commits de implementación certificados por el receipt D son `f2e620451b5bbdfba250658dd3bd8feec5b32661` y `81af92dbfb8f33de8bd6091c6f23647b3183f5f7`.

N8.11.F cerró la auditoría de seguridad con P0=0, P1=0 y P2=0. Sus gates causales quedaron terminales PASS sobre el functional candidate `91a7051bdb75c858af08d0e28368d827c4c0f6b8`: hardening de configuración de seguridad, auditoría npm high/critical y auditoría de vulnerabilidades .NET.

N8.11.G confirmó sobre el mismo functional candidate los gates causales de backend non-integration, frontend unit, frontend lint/build y security audit. Fallos o cancelaciones no causales de otros workflows no se usaron para fabricar PASS.

## Seguridad y límites

N8.11.H es un checkpoint documental. No introduce delta funcional de autenticación, autorización, persistencia, esquema, datos, secretos, infraestructura ni Producción. B, C y E cerraron como N/A material donde no existía un cambio causal que justificara inventar dominio, migración o UI.

## Rollback

El rollback de este checkpoint documental consiste únicamente en revertir los artefactos documentales de N8.11.H si fueran incorrectos. El delta funcional previo permanece gobernado por los receipts A-G y sus gates causales; no se requiere rollback de esquema, datos, secretos, deploy o Producción por H.

## Guardas

- `main`: intacta.
- Producción: intacta.
- deploy: no ejecutado.
- secretos: no tocados.
- PR #2: no merge, no auto-merge.
- P0 abiertos atribuibles al checkpoint: 0.
- P1 abiertos atribuibles al checkpoint: 0.

N8.11.H sólo pasa a `LISTO_REAL` mediante REVIEW_FIRST documental, receipt verificable y reconciliación del control-plane; este documento por sí solo no sustituye ese cierre.
