# Certificación ERP-N2.1 — SolicitudCompra

## Dictamen

ERP-N2.1 queda **LISTO**. La capacidad `SolicitudCompra` está implementada y certificada de extremo a extremo en la rama `Desarrollo`, sin tocar `main`, Producción, secretos ni infraestructura productiva.

## Alcance certificado

Lifecycle canónico:

`Borrador -> Solicitada -> Aprobada/Rechazada`.

`SolicitudCompra` permanece separada de `Compra` y no materializa inventario, Kardex, costeo ni finanzas. La aprobación tampoco crea implícitamente una orden de compra; esa materialización pertenece a ERP-N2.2.

Quedan cubiertos dominio/contratos, persistencia EF y migración MySQL, aplicación/API, concurrencia, frontend/UX, RBAC, auditoría, observabilidad, regresión y documentación operativa.

## Evidencia funcional

- Baseline funcional: `a1a6f699cbad0186d0e0d7d7ac7f366c51009f7c`.
- Tree funcional: `ec0c9b03b42b54e8c58794a966b7be55cd3cbffc`.
- CI funcional: Development `32172981351` — `SUCCESS`.
- Backend: 994/994 pruebas en verde en el baseline funcional.
- Artifact backend: `9338088182`.

## Evidencia documental causal

- Commit documental previo: `d8760bff2e9322e6f09612f64a89c2de888aa9d8`.
- Tree documental: `a0e0e1583272b4dcead62cbf95d2d000c551e10a`.
- Development `32177459360` — `SUCCESS` sobre `d8760bff...`.
- Acceptance `32177459477` — `SUCCESS` sobre `d8760bff...`.

Los gates pendientes al checkpoint anterior concluyeron correctamente; no existe fallo causal abierto de N2.1.H.

## Documentación canónica

- `docs/ERP_N2_1_SOLICITUD_COMPRA.md`
- `docs/ADR_N2_1_SOLICITUD_COMPRA_INDEPENDIENTE.md`
- `docs/RUNBOOK_N2_1_SOLICITUD_COMPRA.md`
- `docs/ERP_N2_1_SOLICITUD_COMPRA_PREFLIGHT.md`

## Cierre y handoff

N2.1.A–H quedan cerrados. El siguiente punto elegible bajo FINISH_FIRST es `N2.2.A — Orden de compra — Auditoría y preflight`.

El scope temporalmente exclusivo de Jules (`.github/workflows/vaep-jules-secondary.yml`, `.github/workflows/vaep-jules-diagnostic.yml`, `docs/VAEP_JULES.md` cuando sea causal y `vaep/jules/**`) permanece fuera de este cierre y no fue modificado.
