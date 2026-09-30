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

## Topología actual versionada

### Aiven

- proyecto: `solqaryn`;
- servicio MySQL: `solqaryn-mysql`;
- cloud: `do-sfo`;
- plan registrado: `free-1-1gb`;
- DEV y PROD comparten el servicio físico, con bases y usuarios separados.

### Render DEV

- servicio: `solqaryn-api-dev`;
- región: `oregon`;
- plan: `free`;
- DB: `solqaryn_dev`.

Oregon y San Francisco permanecen en la costa oeste. Con las métricas actuales no existe evidencia que justifique mover DEV.

### Render PROD

- servicio: `solqaryn-api-prod`;
- región versionada: `virginia`;
- plan: `free`;
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

## Bloqueo operativo actual

La conexión Render disponible en esta sesión no tiene un workspace seleccionado. El conector exige que el propietario confirme explícitamente cuál workspace usar antes de cualquier lectura de servicios/métricas. Por seguridad no se seleccionó uno automáticamente.

Esto no invalida la conclusión de DEV porque la evidencia causal de rendimiento y la topología versionada ya son suficientes para descartar una mudanza DEV. Una lectura fresca de Render sólo es necesaria para certificar metadata/métricas vivas adicionales o preparar una futura comparación blue/green PROD.
