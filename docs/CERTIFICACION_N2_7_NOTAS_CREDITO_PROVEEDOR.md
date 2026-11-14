# Certificación N2.7 — Notas de crédito de proveedor

## Dictamen

**Estado de evidencia: APTO PARA CIERRE DOCUMENTAL.**

Baseline funcional certificado: `42f83b365392f45de39bd0e0ca4fa0638dd0eb10`.

## Evidencia técnica

N2.7.A–G se encuentran cerrados en el tablero VAEP. N2.7.H consolida la documentación/certificación sin reabrir implementación ya certificada.

Gates finales del baseline:

| Gate | Run | Resultado |
| --- | ---: | --- |
| Development | 32574284665 | SUCCESS |
| Acceptance | 32574284640 | SUCCESS |
| Fase 8 | 32574284638 | SUCCESS |
| M13 | 32574284639 | SUCCESS |
| Recovery MySQL | 32574284669 | SUCCESS |
| M10 | 32574284658 | SUCCESS |

M13 completó el camino integral, incluido Playwright y dictamen automatizado final.

## Contrato certificado

- Documento: `NotaCreditoProveedor`.
- Lifecycle: `Borrador → Registrada → Anulada`.
- API base: `notas-credito-proveedor`.
- RBAC: `Compras:Ver`, `Compras:Crear`, `Compras:Editar`, `Compras:Confirmar`, `Compras:Anular`.
- Frontend: Editar/Registrar sólo en Borrador; Anular sólo en Registrada.
- Concurrencia: crédito acumulado por factura serializado bajo transacción.
- Persistencia: esquema/migración/snapshot/pre-post checks certificados.
- Frontera: no se certifica CxP completo ni efectos de stock/Kardex no materializados.

## P0/P1

P0 abiertos conocidos: **0**.

P1 abiertos conocidos: **0**.

## Documentación canónica

- `docs/ERP_N2_7_NOTAS_CREDITO_PROVEEDOR.md`
- `docs/ADR_N2_7_NOTA_CREDITO_AUTORIDAD_DOCUMENTAL.md`
- `docs/RUNBOOK_N2_7_NOTAS_CREDITO_PROVEEDOR.md`
- `docs/CERTIFICACION_N2_7_NOTAS_CREDITO_PROVEEDOR.md`
- `docs/qa/N2_7_H_CERTIFICATION_CHECKPOINT_V321.md`

## Cierre

La promoción formal de `N2.7.H` a `LISTO` requiere que `TASKS.md`, `CHANGELOG_AI.md` y COLA/CONFIG reflejen este mismo dictamen. Hasta completar esa reconciliación administrativa no debe falsearse `LISTO`.
