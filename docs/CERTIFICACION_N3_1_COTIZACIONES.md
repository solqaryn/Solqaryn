# CERTIFICACIÓN N3.1 — Cotizaciones

## Estado

**LISTO para cierre VAEP N3.1.H**, sujeto únicamente a la reconciliación administrativa de `TASKS.md`, `CHANGELOG_AI.md`, COLA/CONFIG/BITÁCORA en la misma continuidad.

## Alcance certificado

N3.1 implementa Cotizaciones como documento comercial previo al Pedido de Venta. El agregado `Cotizacion` conserva snapshots de cliente y producto, detalles valorizados y el lifecycle autoritativo:

`Borrador → Enviada → Aceptada/Rechazada → Convertida`.

Las transiciones validan usuario y UTC antes de mutar estado; una cotización sólo se envía con cliente y al menos un detalle válido. La duplicación crea un nuevo borrador sin heredar estado terminal.

## Aplicación y API

La vertical backend está expuesta bajo `/cotizaciones` con `[Authorize]` y permisos del módulo `Ventas` por operación. El servicio usa transacciones para mutaciones, auditoría estricta y repositorio con lectura de actualización donde corresponde. El controller depende de `ICotizacionService` mediante DI.

Permisos contractuales vigentes:

- consulta: `Ventas:Ver`;
- creación: `Ventas:Crear`;
- edición: `Ventas:Editar`;
- eliminación de borrador: `Ventas:EliminarPermanente`;
- envío/conversión: permisos de confirmación aplicables;
- aceptación: `Ventas:Aprobar`;
- rechazo: `Ventas:Rechazar`;
- duplicación: `Ventas:Duplicar`.

## Frontend y UX

La superficie frontend de Cotizaciones fue materializada y corregida durante N3.1.E. Incluye navegación/rutas protegidas, listado/formulario/detalle y acciones de lifecycle alineadas al contrato HTTP/RBAC. Los defectos de tipado descubiertos por lint fueron corregidos causalmente antes del cierre.

## Persistencia y QA

La persistencia/migración N3.1.C, Application/API N3.1.D, Frontend/UX N3.1.E, seguridad N3.1.F y QA/regresión N3.1.G quedaron cerrados en COLA antes de abrir H. El baseline funcional inmediato del cierre es:

`d4d296e229d266a1442de3bc4e07b03bfab35a9f`.

El HEAD de control-plane previo al cierre documental es:

`eea11fb0e3ba1f1afc3010362f87caecf89f6c22`.

Ese HEAD difiere del baseline funcional únicamente por el manifest evidence-only de Jules A para H; no altera aplicación ni contratos.

## Gates finales

Sobre `eea11fb0e3ba1f1afc3010362f87caecf89f6c22` terminaron en `SUCCESS`:

- Development: `#32687639976`;
- Acceptance: `#32687639981`;
- Fase 8: `#32687640010`;
- M13: `#32687640016`;
- Recovery MySQL: `#32687640017`.

Los failures de workflows históricos ERP-N0.x paralelos no son gates causales de N3.1.H.

## REVIEW-FIRST / Jules

El dispatch de Jules A para la matriz de cierre no produjo Issue/sesión ni actividad técnica útil verificable después del umbral de recuperación. Conforme VAEP v3.21 se clasifica como `BOOTSTRAP_STALLED_NO_SESSION`, `ACTIVE=NO`; no consume ATTEMPT1 de contenido. ChatGPT/VAEP ejecuta el cierre documental por QA takeover. Jules B/C/D permanecen liberados y sin ownership duplicado.

## P0/P1 y dictamen

- P0 bloqueantes conocidos: **0**.
- P1 bloqueantes conocidos: **0**.
- `main`/Producción/merge/auto-merge/ramas/force-push/secrets/deploy: **no tocados**.
- PR #2: debe permanecer **OPEN + DRAFT + no merged**, `Desarrollo → main`.

Con la reconciliación administrativa de cierre realizada, `N3.1.H` puede marcarse `LISTO`. El selector fail-closed debe reejecutarse inmediatamente; el siguiente padre dependency-valid es `N3.2.A — Pedidos de venta / Auditoría y preflight`.