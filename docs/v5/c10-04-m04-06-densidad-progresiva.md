# C10-04.M04.06 - Densidad y carga progresiva de continuidad privada

Riesgo R1 UI, ampliado a R2 por ruta privada y CI. PLAN V5: verificar densidad, carga progresiva y percepción de ligereza. Pruebas dirigidas sobre UI Angular existente, sin alterar el componente, permisos, JWT, API, modelos, base de datos, worker, backup o despliegue.

## A0 - Alcance y rollback
- Prerrequisitos y elegibilidad confirmados por PLAN_MAESTRO H/I, COLA G/O y DAG F/G en el CLAIM V17. Solo una EN_PROGRESO propia.
- Componente usa plataformaGuard, unico main de shell, section.continuidad, grid de tarjetas responsive, estado aria-busy y revalidacion con fail-closed.
- Entrega cuatro archivos: Playwright E2E, workflow GitHub-hosted, este documento y CHANGELOG_AI. Rollback git revert normal luego de revalidar HEAD, sin force-push.

## A1 - Positivo, negativo, fronteras y errores
- En 320/768/1440 px exigir respectivamente 1/2/2 columnas visuales, cuatro tarjetas, un solo landmark main, tarjetas visibles y overflow horizontal <= 2 px.
- Mantener la solicitud sintetica de revalidacion sin resolver: aria-busy=true, boton disabled, 0 tarjetas. Liberar respuesta y demostrar recuperacion, boton enabled y 4 tarjetas.
- Negativo: cambiar 200 a 403 y negar tarjetas, aviso privado y contenido del error.
- La densidad y percepcion de ligereza son oraculos de DOM/layout y estados; latencia, CPU y Core Web Vitals son NO_MEDIDO, no inventar cifras.

## A2 - Seguridad y limites
- Ruta privada protegida por plataformaGuard, pero los tests usan JWT ficticio y respuesta HTTP sintética exclusivamente local. No acreditan JWT firmado, roles reales, tenant A/B, BOLA, API, MySQL, backups, worker ni restauracion de datos. No filtrar secretos.
- NO_APLICA para DB/EF/worker/backend porque este changeset solo instrumenta browser/UI; NO es permiso de produccion.

## A3 - Integracion pertinente
- Angular real levantado en localhost de GitHub-hosted runner, Node 24.21.0, npm 11.19.0, Playwright Chromium real; cinco tests. Sin servicios de pago, datos o credenciales reales.

## A4/A5 - Cierre
- Exigir commit fast-forward en dev, exact-SHA Scope Lock/V5/seguridad, lint/build, cinco tests Chromium completed/success, evidencia de CI y readback PLAN/COLA/DAG/BITACORA.
- Queued, in_progress, skipped y cancelled no son PASS. Si CI sigue externa, conservar VALIDANDO con SHA/RUN y A5 pendiente de otra Task Javier.
- No QA, main ni produccion. No declarar pruebas no ejecutadas.
