# DEV — Punto 10: Render Free fuera de la ruta comercial

Fecha: 2026-09-29  
Repositorio: `solqaryn/Solqaryn`  
Rama: `dev`  
Compras/upgrades ejecutados: **0**

## Objetivo

Separar correctamente dos decisiones:

1. DEV puede permanecer en Render Free si aceptamos cold starts.
2. PROD comercial no debe considerarse `always-on` mientras continúe en un web service Free.

Por instrucción explícita del propietario, este punto **no compra ni activa ningún servicio pago**.

## Readback vivo Render

Workspace corporativo: `SOLQARYN` / `solqaryn.platform@outlook.com`.

### DEV

- servicio: `solqaryn-api-dev`;
- service ID: `srv-daqvla49v7es738up9vg`;
- rama: `dev`;
- región: `oregon`;
- plan vivo: `free`;
- instancias configuradas: 1;
- health: `/health`.

### PROD

- servicio: `solqaryn-api-prod`;
- service ID: `srv-dapl2j49v7es73907om0`;
- rama: `main`;
- región: `virginia`;
- plan vivo: `free`;
- instancias configuradas: 1;
- health: `/health`.

No se modificó ninguno.

## Política vigente del proveedor

La documentación oficial actual de Render indica que los web services Free hacen spin-down después de 15 minutos sin tráfico entrante y vuelven a arrancar con la siguiente petición. Render también documenta que los planes de compute pagos no hacen spin-down.

Referencias oficiales:

- https://render.com/docs/your-first-deploy
- https://render.com/docs/faq

No se implementan pings artificiales como workaround.

## Evidencia causal DEV de instancia fría vs caliente

Logs `PerformanceBaseline` de Render DEV:

- `GET /tienda/bootstrap`: **2998.2 ms**, 7 queries, 259.1 ms DB;
- siguiente hit: **5.9 ms**, 0 queries;
- otra ventana fría: **3371.5 ms**, 3 queries, 130.6 ms DB;
- request concurrente: **2608.6 ms**, 4 queries, 89.1 ms DB;
- después: **13.5 ms**, 0 queries;
- después: **1.7 ms**, 0 queries.

Interpretación:

- el tiempo DB explica sólo una fracción pequeña de los requests fríos de varios segundos;
- una vez despierto y/o cacheado, el servicio responde en milisegundos;
- no existe justificación para añadir CPU/RAM grande basándonos en esta evidencia;
- el defecto observado es compatibilidad de Render Free con latencia comercial estable, no falta demostrada de capacidad de cómputo.

## Auditoría de keep-alive

Búsqueda dirigida en el repositorio:

- `keepalive`: 0 implementaciones;
- `keep-alive`: 0 implementaciones;
- `uptimerobot`: 0 implementaciones;
- cron/curl público para Render: 0 implementaciones.

Los `curl /health` existentes en CI apuntan a `localhost` para esperar servicios efímeros de pruebas. No mantienen despierto Render y son correctos.

## Guarda añadida

`scripts/validate-render-free-policy.mjs` falla si un workflow programado intenta pegarle a `*.onrender.com` o introduce un keep-alive explícito.

La guarda está integrada en `.github/workflows/project-scope-lock.yml`.

Objetivo: impedir que un futuro cambio o agente oculte el problema de sleep con tráfico sintético.

## Decisión

### DEV

Se mantiene `free` por ahora.

Es válido para desarrollo y pruebas funcionales cuando se separan explícitamente cold start y pasada caliente. Para benchmarks estables se debe calentar primero la instancia y etiquetar la primera petición como warm-up.

### PROD

El servicio permanece `free` porque el propietario prohibió comprar servicios.

Por tanto, **PROD no puede certificarse como backend always-on comercial bajo la configuración actual**.

La transición futura correcta requiere:

1. autorización explícita para gasto productivo;
2. elegir el menor plan always-on que satisfaga las métricas reales;
3. no sobredimensionar CPU/RAM sin evidencia;
4. combinarlo con la decisión de topología del Punto 9: si se crea un backend oeste, hacerlo blue/green;
5. health/readiness, smoke y comparación causal;
6. cutover sólo con autorización productiva;
7. rollback al servicio anterior hasta cerrar estabilidad.

## Alcance y seguridad

- compras: 0;
- plan Render cambiado: no;
- pings artificiales: no;
- Aiven: sin cambios;
- datos/migraciones: sin cambios;
- secretos: sin cambios;
- `main`: sin cambios;
- tráfico PROD: sin cambios.

## Estado

**Implementación de política y evidencia: LISTA en DEV.**

**Objetivo operacional “PROD always-on”: BLOQUEADO deliberadamente por la regla de cero compras.**

No existe una solución técnica gratuita dentro de Render Free que convierta ese plan en un servicio oficialmente always-on sin contradecir la política del proveedor o introducir un keep-alive artificial.
