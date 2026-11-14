# Certificación N3.2 — PedidoVenta

## Dictamen actual

**N3.2.H = VALIDANDO.** N3.2.A-G están funcionalmente certificados; este paquete H debe pasar su propio CI causal y luego reconciliar los registros colaborativos antes de emitir `LISTO` final.

## Evidencia funcional certificada

Baseline acumulado: `58a6550094a043556367b79c89e4ac963bd34a4a`.

- Development `#32731508498` — SUCCESS.
- Acceptance `#32731508548` — SUCCESS.
- Fase 8 `#32731508654` — SUCCESS.
- M13 `#32731508646` — SUCCESS.
- Recovery MySQL `#32731508448` — SUCCESS.
- P0/P1 bloqueantes conocidos en N3.2.A-G: 0.

## Evidencia de control previa al paquete H

HEAD `a923c715b0d3490b3e4c8d0dedafbf3da8df9ada`:

- Development `#32739427111` — SUCCESS.
- Acceptance `#32739427514` — SUCCESS.
- Fase 8 `#32739427393` — SUCCESS.
- M13 `#32739427454` — SUCCESS, incluyendo Backend/MySQL/migraciones, Runtime/seguridad HTTP/Playwright integral, Frontend, Docker y dictamen automatizado.
- Recovery MySQL `#32739427189` — SUCCESS.

Los failures ERP-N0.x/legacy observados en paralelo no se atribuyen a N3.2.H sin evidencia causal.

## REVIEW-FIRST Jules H

- Jules A / cierre: evidencia útil, no integrada por recomendaciones stale; RELEASED.
- Jules B / runbook: no integrado por incumplir el hard-gate de dos self-reviews independientes; evidencia factual reutilizada por QA takeover.
- Jules C / certificación: no integrado por el mismo hard-gate; evidencia factual reutilizada por QA takeover.
- Jules D / integridad documental ATTEMPT1: rechazado.
- Jules D / R2 FINAL ATTEMPT2/2: scope y self-reviews correctos, pero contenido stale que afirmaba ausencia de PedidoVenta y delegaba hechos ya cerrados a N3.2.B; `JULES_RETRY_EXHAUSTED -> QA_TAKEOVER`, R3+ prohibido.

Ningún artifact rechazado fue aplicado a `Desarrollo`.

## Controles verificados

- `PedidoVenta` existe como agregado independiente.
- Migración canónica y snapshot de PedidoVenta están materializados.
- API `/pedidos-venta` está protegida con `[Authorize]` y RBAC Ventas.
- Frontend `pedidos-venta` usa rutas protegidas y dispone de cobertura E2E RBAC.
- N3.2 no adelanta reserva de inventario, Kardex, facturación o finanzas.

## Gate final de H

Para cerrar N3.2.H deben cumplirse simultáneamente:

1. este paquete documental publicado en `Desarrollo`;
2. CI causal del HEAD del paquete en verde;
3. `TASKS.md` y `CHANGELOG_AI.md` reconciliados de forma aditiva/preservadora;
4. COLA/CONFIG/BITACORA actualizados;
5. P0=0 y P1=0.

Sólo entonces: `N3.2.H=LISTO`, Parent40 `38/40`, GAP `2`, y rebind inmediato a `N3.3.A`.
