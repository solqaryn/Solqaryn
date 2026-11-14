# Certificación ERP-N8.6 — WhatsApp real

Fecha de revalidación current-standard: 2026-09-17
Autoridad operativa: `docs/VAEP_AUTHORITY.md`
Rama certificada: `Desarrollo`

## Alcance vigente

N8.6 valida el flujo de salida a WhatsApp que realmente implementa Solqaryn en Desarrollo. La arquitectura vigente no implementa un envío server-side mediante Meta Cloud API, Twilio u otro proveedor con credenciales propias del backend. El comportamiento real es un handoff desde la factura hacia el endpoint público `https://wa.me/<telefono>?text=...`, después de validar permiso, teléfono y mensaje, y registrar únicamente la auditoría local del handoff.

La certificación no afirma `sent`, `delivered` ni `read` porque esos estados no son observables por la aplicación actual. Tampoco inventa una sesión, QR, token de proveedor, credencial ni envío automático inexistente. La automatización de revalidación no envió mensajes a destinatarios externos.

## Cadena current-standard

- N8.6.A PRE — `LISTO`: `vaep/evidence/receipts/N8.6.A_REVALIDATED_CURRENT_STANDARD_LISTO_20260917T065700Z_SUP48.json`.
- N8.6.B DOMAIN — `LISTO`: `vaep/evidence/receipts/N8.6.B_REVALIDATED_CURRENT_STANDARD_LISTO_20260917T070400Z_SUP48.json`.
- N8.6.C DB_MIG — `LISTO`: `vaep/evidence/receipts/N8.6.C_REVALIDATED_CURRENT_STANDARD_LISTO_20260917T070800Z_SUP48.json`.
- N8.6.D BACKEND_API — `LISTO`: `vaep/evidence/receipts/N8.6.D_REVALIDATED_CURRENT_STANDARD_LISTO_20260917T080158Z_SUP48.json`.
- N8.6.E FRONTEND_UX — `LISTO`: `vaep/evidence/receipts/N8.6.E_REVALIDATED_CURRENT_STANDARD_LISTO_20260917T084200Z_SUP36.json`.
- N8.6.F SEC_AUDIT — `LISTO`: `vaep/evidence/receipts/N8.6.F_REVALIDATED_CURRENT_STANDARD_LISTO_20260917T084400Z_SUP36.json`.
- N8.6.G TEST_CI — `LISTO`: `vaep/evidence/receipts/N8.6.G_REVALIDATED_CURRENT_STANDARD_LISTO_20260917T084800Z_SUP36.json`.

## Functional head y regresión causal

Functional tested head: `51929a108322acf01e5b63cc4274d4c7b2bd78c9`.

La comparación hasta el control head previo al cierre de G demostró exclusivamente adiciones de evidencia VAEP después del functional head, sin delta funcional de producto.

Checks causales y relevantes terminales en verde sobre el functional head:

- `WhatsApp handoff directed tests`: run `35198650568`, job `105127810344`, `success`.
- `API, RBAC, auditoría, MySQL, Angular y E2E`: run `35198655381`, job `105127826272`, `success`.
- `Static contracts, tests and production build`: run `35198655109`, job `105127824656`, `success`.
- `unit-frontend`: run `35198655025`, job `105127824227`, `success`.
- `Configuración, aislamiento y endurecimiento`: run `35198655092`, job `105127824870`, `success`.
- `Dependencias vulnerables de .NET`: run `35198655092`, job `105127825070`, `success`.

El inventario del functional head contiene 29 check-runs; no se detectaron conclusiones `failure` ni `cancelled`. Los jobs `skipped` observados corresponden a paths/scopes no causales para este cambio y no sustituyen los checks causales anteriores.

## Seguridad y verdad operativa

El backend de WhatsApp conserva `[Authorize]`, exige `Configuracion.Editar` para la sesión/configuración, resuelve tenant desde el usuario autenticado y falla cerrado ante verify-token ausente o inválido. La comparación de verify-token usa tiempo fijo y los warnings relevantes incluyen correlation id. El frontend exige `facturas:compartir-whatsapp`, usa `noopener,noreferrer`, valida teléfono/mensaje, maneja popup bloqueado sin afirmar entrega y no expone secretos.

P0 atribuibles abiertos: 0.
P1 atribuibles abiertos: 0.
P2 atribuibles abiertos: 0.

## Rollback

N8.6 no requiere rollback de esquema ni datos por esta revalidación. Si fuera necesario revertir el gate causal añadido para la verificación current-standard, puede retirarse `.github/workflows/n8-6-whatsapp-gate.yml` sin alterar la funcionalidad de handoff ya existente. Las evidencias VAEP son históricas y no se reescriben.

## Guardas

- `main`: intacta.
- Producción: intacta.
- PR #2: no merge ni auto-merge.
- secretos: no expuestos.
- DNS/certificados: intactos.
- mensajes externos enviados por esta revalidación: 0.

N8.6.H sólo pasa a `LISTO` después de REVIEW_FIRST documental, reconciliación history-preserving de los ledgers requeridos, receipt final y write/readback del control-plane. Este documento no sustituye ese cierre.
