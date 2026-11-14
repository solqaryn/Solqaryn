# DEV — Punto 9: Aiven y topología después de reducir queries

Fecha: 2026-09-29  
Repositorio: `solqaryn/Solqaryn`  
Rama: `dev`  
Servicios pagos adquiridos: **0**

## Objetivo

Evaluar si la latencia restante justifica cambiar Aiven o la región del backend, únicamente después de reducir el número de queries del storefront.

No se ejecuta ninguna mudanza destructiva, no se cambia tráfico PROD y no se compra ningún servicio.

## Estado medido del código

La optimización previa ya redujo el listado público de productos de **7 queries** a **5 queries** por miss.

Pasadas calientes DEV posteriores al read-model ligero:

- 254.1 ms total / 5 queries / 109.2 ms DB;
- 216.5 ms total / 5 queries / 107.5 ms DB;
- 225.4 ms total / 5 queries / 111.9 ms DB;
- 224.3 ms total / 5 queries / 112.3 ms DB.

Con cache tenant-aware:

- listados cache hit: **0 queries**;
- bootstrap cache hit: **0 queries**;
- categorías compartidas: **0 queries**.

Conclusión: el código dejó de hacer un número excesivo de round trips en el camino caliente. La deuda restante ya puede analizarse como topología/infraestructura y no como sustituto de una optimización de queries pendiente.

## Topología actual verificada

### Aiven

- proyecto: `solqaryn`;
- servicio MySQL: `solqaryn-mysql`;
- cloud: `do-sfo`;
- plan registrado: `free-1-1gb`;
- DEV y PROD comparten el servicio físico, con bases y usuarios separados.

### Render DEV — readback vivo

- workspace: `SOLQARYN` (`solqaryn.platform@outlook.com`);
- service ID: `srv-daqvla49v7es738up9vg`;
- servicio: `solqaryn-api-dev`;
- rama: `dev`;
- región: `oregon`;
- plan: `free`;
- instancias configuradas: 1;
- health: `/health`;
- DB: `solqaryn_dev`.

Oregon y San Francisco permanecen en la costa oeste. Con las métricas actuales no existe evidencia que justifique mover DEV.

### Render PROD — readback vivo

- workspace: `SOLQARYN` (`solqaryn.platform@outlook.com`);
- service ID: `srv-dapl2j49v7es73907om0`;
- servicio: `solqaryn-api-prod`;
- rama: `main`;
- región: `virginia`;
- plan: `free`;
- instancias configuradas: 1;
- health: `/health`;
- DB objetivo: `solqaryn_prod`.

Virginia -> San Francisco introduce un trayecto interregional mucho mayor que DEV. Con 5 queries por miss, cada request sensible a DB paga varias veces la latencia de red entre backend y MySQL.

## Decisión del Punto 9

### DEV

**Mantener Oregon + Aiven do-sfo.**

No se cambia Aiven, no se crea otro MySQL y no se cambia región DEV.

### PROD

**No ejecutar mudanza ahora.**

El riesgo de topología queda registrado, pero cambiar región de PROD requiere autorización productiva nueva y explícita.

La vía profesional, si la medición productiva confirma penalización material, es:

1. crear un backend PROD candidato en región oeste;
2. usar exactamente la misma rama/imagen/configuración productiva, con secretos PROD separados y sin migraciones automáticas;
3. validar `/health` y `/health/ready`;
4. verificar conexión a `solqaryn_prod`, RBAC/tenancy y lecturas críticas;
5. ejecutar smoke/read-only y comparar TTFB/Server-Timing/DB contra el backend Virginia;
6. mantener Virginia como blue y el candidato oeste como green;
7. cambiar tráfico sólo después de autorización explícita del propietario;
8. conservar rollback inmediato al backend anterior;
9. retirar el backend anterior únicamente después de una ventana de estabilidad certificada.

No se mueve Aiven para resolver este punto. La opción preferente, si se confirma la penalización, es acercar el backend PROD al MySQL existente antes que hacer una migración destructiva de base de datos.

## Gate de decisión para una futura acción PROD

No solicitar ni ejecutar un cutover por intuición. Debe existir una muestra comparable y causal que demuestre una penalización material en PROD con el mismo endpoint y carga.

Comparar al menos:

- total API;
- DB total;
- query count;
- TTFB;
- cache miss vs hit;
- cold start separado;
- mismo endpoint/params/payload;
- varias pasadas calientes.

Sólo si el candidato oeste mejora de forma consistente la parte atribuible a DB/red sin regresiones funcionales, se propone el cutover.

## Restricciones

- cero compras/upgrades;
- cero cambios Aiven;
- cero cambios de datos;
- cero cambios de secretos;
- cero cambios de `main`;
- cero cambios de tráfico PROD;
- cero migraciones productivas;
- cero acciones destructivas.

## Readback vivo y cierre

Después de retirar la conexión duplicada, Render expone un único workspace corporativo: `SOLQARYN` con `solqaryn.platform@outlook.com`.

La lectura viva de servicios confirma la topología versionada:

- DEV = Oregon / free / `dev`;
- PROD = Virginia / free / `main`.

La consulta de métricas Render en la ventana 2026-09-29 00:00Z -> 2026-09-30 00:30Z devolvió CPU/memoria, pero no series de `http_latency` ni `http_request_count` para ninguno de los dos servicios. Por tanto **no existe hoy evidencia provider-side suficiente para cuantificar una penalización PROD Virginia -> San Francisco**.

Los logs de observabilidad DEV sí confirman nuevamente el camino caliente del storefront:

- productos: 212.4 ms / 5 queries / 110.2 ms DB;
- productos: 208.1 ms / 5 queries / 106.9 ms DB;
- productos: 207.4 ms / 5 queries / 104.1 ms DB;
- productos: 220.3 ms / 5 queries / 113.3 ms DB;
- productos: 257.0 ms / 5 queries / 106.4 ms DB;
- productos: 230.6 ms / 5 queries / 109.9 ms DB;
- hits de bootstrap/categorías observados con 0 queries y tiempos de pocos milisegundos.

También existen outliers de warm-up/cold-start donde el tiempo total crece mucho más que el tiempo DB, por lo que no deben atribuirse a Aiven ni a la distancia de red.

**Cierre del Punto 9:** no se mueve Aiven, no se cambia DEV y no se crea un backend PROD oeste ahora. La diferencia de región PROD queda como riesgo técnico documentado, no como defecto demostrado. Un blue/green oeste sólo se abre si una medición productiva comparable demuestra una mejora material y existe autorización explícita del propietario.
