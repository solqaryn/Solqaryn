# SOLQARYN V5 — C11-04.M01.06: barreras, rollback y liberación documental de observabilidad

**Microtarea:** C11-04.M01.06; **fila PLAN_MAESTRO:** 1699; **autor técnico:** PRIMARY DEV 06 — JAVIER, TASK_ID `6af16bbad26881918c283abe06e1f32a`; **rama única:** `solqaryn/Solqaryn@dev`; **Scope Lock:** STRICT; **fecha:** 2026-11-15. Fuente única: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`).

## Objetivo, entregable y límite de autoridad

Definir las **condiciones comprobables de salida documental de M01** para logs, métricas, trazas, alertas y costos sin datos sensibles. Esta entrega R0 documenta bloqueos y reversibilidad; **no instala ni activa** collector, exporters, instrumentación, dashboards, alertas, bases, endpoints, tareas, webhooks, proveedores ni producción. La evidencia exigida de **SLO medido, dashboard funcional y diagnóstico reproducible** queda explícitamente **NO_MEDIDO/NO_IMPLEMENTADO** para fases de código y pruebas posteriores. No declarar M01 documental como certificación funcional de observabilidad.

Predecesoras M01.01–05 con estado PLAN=LISTO y revisión A5 independiente acreditada:
| ID | Aporte documental verificado | Archivo |
| --- | --- | --- |
| C11-04.M01.01 | Alcance, no regresión y límites de señales | `docs/v5/c11-04-m01-01-alcance-no-regresion-observabilidad.md` |
| C11-04.M01.02 | Actores, interfaces y fronteras de confianza | `docs/v5/c11-04-m01-02-actores-superficies-observabilidad.md` |
| C11-04.M01.03 | Inventario de once fuentes y brechas de evidencia | `docs/v5/c11-04-m01-03-inventario-codigo-evidencia-observabilidad.md` |
| C11-04.M01.04 | Riesgos potenciales, supuestos no verificados, decisiones D01–D08 pendientes | `docs/v5/c11-04-m01-04-riesgos-supuestos-decisiones-observabilidad.md` |
| C11-04.M01.05 | AC01–AC18, N01–N18, RNF01–RNF12 FUTUROS_NO_EJECUTADOS | `docs/v5/c11-04-m01-05-criterios-aceptacion-observabilidad.md` |

La observabilidad actual contiene, entre otras superficies inventariadas, `CorrelationIdMiddleware`, `RequestObservability`, `ExceptionHandlingMiddleware`, `MedirRendimientoBusquedaFilter` y `Program.cs` en API. Su existencia **no acredita** SLO, collector, retención, aislamiento efectivo de dashboard ni exportación segura.

## Barreras de salida M01 y criterios verificables

| Gate | Condición documental exigible | Evidencia / comportamiento ante ausencia |
| --- | --- | --- |
| G01 | Plan V5/ID únicos; rama dev, Task Javier autorizada y Scope Lock STRICT | Sin identidad o autoridad: NO_GO |
| G02 | M01.01–05 en LISTO y A5 independientes reales; propietarios conservados | Releer PLAN y BITACORA; si falta A5, no liberar |
| G03 | DAG del ID F=VALIDADO/G=SI y COLA G/O=SI antes de reclamar | No editar fórmulas ni forzar SI |
| G04 | Superficies de origen trazadas a rutas y blobs, sin confundir inventario con ejecución | Referencias Git y M01.01–04 |
| G05 | AC18/N18/RNF12 de M01.05 expresan oráculos FUTUROS, no PASS de HTTP/MySQL | Mantener NO_EJECUTADO |
| G06 | Lista permitida de campos y supresión PII/secretos (D01) | Pendiente; no activar exporter |
| G07 | Labels agregados, cardinalidad, cuotas y fallback (D02) | Pendiente; ninguna serie por individuo |
| G08 | Definiciones de SLI/SLO, ventana, baseline, fuente y error budget (D03) | NO_MEDIDO sin muestras y denominadores |
| G09 | Roles de plataforma/empresa/soporte y permisos tenant A/B (D04) | JWT/RBAC backend real pendiente de pruebas |
| G10 | Retención, acceso, borrado, residencia y respuesta a incidente (D05) | No persistir señales fuera de controles actuales |
| G11 | Autorización explícita de collector/proveedor/costo (D06) | Ningún gasto ni integración live |
| G12 | Alertas con deduplicación, cuota, on-call humano y límites de costo (D07) | Sin Task, dispatcher ni webhook |
| G13 | Plan de ensayo, baseline, reversión y throughput (D08) | No certificar métricas ni disponibilidad |
| G14 | M03 futura: A1 positivos/negativos, A2 JWT/RBAC/tenant A/B, A3 HTTP/efímero MySQL cuando aplique | Build/doc/mock no sustituyen integración |
| G15 | Publicación documental con diff limitado, CHANGELOG autocontenido, Scope Lock + procedencia V5 completed/success exact SHA | CANCELLED/SKIPPED/IN_PROGRESS no son PASS |
| G16 | Readbacks PLAN/COLA/DAG/BITACORA por transición y A5 distinta de autora antes de liberar M02 | LISTO_DEV no equivale a A5_PASS |

**Condición de salida atómica de este ID:** documento verificable + A0–A4 R0 + HEAD/Scope Lock/V5 correspondientes + evento idempotente y readback. Registrar A5_PENDIENTE tras LISTO. **Condición distinta para habilitar M02:** A5_PASS independiente de una de las otras cinco Tasks Javier sobre este SHA, todos los gates previos válidos, PLAN M02=PENDIENTE sin owner y lectura actual de DAG F/G=VALIDADO/SI y COLA G/O=SI. Una preparación documental jamás autoriza M03 runtime, QA/main/PROD o migraciones.

## Matriz de aceptación futura, no ejecutada en M01.06

| Escenario | Resultado esperado | Rechazo verificable |
| --- | --- | --- |
| P01/N01 | CorrelationId saneado y trazabilidad de request | Header CR/LF, PII o longitud excesiva se registra sin sanear |
| P02/N02 | HTTP 5xx agregado sin información privada | Stack, token, SQL o cadena de conexión en log o respuesta |
| P03/N03 | Usuario autorizado A consulta únicamente agregados A | Usuario B, anónimo o rol sin permiso lee datos de A |
| P04/N04 | Soporte read-only consulta indicadores permitidos | Soporte altera retención, política o alertas |
| P05/N05 | Collector efímero degradado sin bloquear negocio | Retry infinito, caída de API o cargo inesperado |
| P06/N06 | Cálculo SLI sobre dataset, ventana y denominador reproducibles | Cero muestras o sin ventana genera SLO PASS |
| P07/N07 | Métrica EF/MySQL agregada sin parámetros sensibles | SQL/PII en etiquetas o mezcla cross-tenant |
| P08/N08 | Alertas con límite y deduplicación verificables | Tormenta notifica ilimitadamente o ejecuta otra Task |
| P09/N09 | Dashboard accesible, responsive y autorizado | UI oculta datos pero API permite BOLA/IDOR |
| P10/N10 | Revert deja rutas y control de tenant estables | Rollback modifica datos, secretos o elimina auditoría |

Estos P/N son **FUTUROS_NO_EJECUTADOS** y requieren pruebas reales cuando exista microtarea de implementación autorizada. Metas p95, overhead, tamaño de muestra, RPO/RTO y presupuesto permanecen **NO_MEDIDO** hasta decisión, fuente y medición; no usar denominadores imaginarios.

## Reversión segura y responsables

1. Ante exposición de PII, cruce de tenant, error de auth, cardinalidad descontrolada o fallo de CI requerido, clasificar defecto (P0/P1 bloquea promoción), conservar SHA, evidencia y autoría.
2. Para **este documento**, revertir únicamente su commit con `git revert` normal a `dev` y CAS de HEAD; no force-push, reset ni borrado de BITACORA.
3. Una futura funcionalidad runtime sólo podrá deshabilitarse mediante mecanismo de rollback expresamente documentado, sin tocar QA/main/PROD ni recursos reales sin autorización separada.
4. Probar tras revert contrato anterior, límites JWT/RBAC, tenant A/B, errores seguros y señales realmente implementadas; no aceptar verificación teórica como prueba runtime.
5. Reconstruir versiones y blobs del origen, registrar causa, efecto y SHA de reversión, con append idempotente en BITACORA y readback.
6. Recalcular DAG/COLA para sucesoras y mantener A5_FAIL o dependencias abiertas hasta reparación causal independiente.
7. No cambiar prompts de seis Tasks, CONFIG global, DNS, R2, secretos, datos productivos ni configuración de proveedores.
8. Si falta exclusiva CAS, check required o permiso, aplicar NO_GO acotado y tomar otro trabajo seguro, sin declarar cierre.

## A0–A5 y handoff

- **A0:** C11-04.M01.06 R0, fuentes M01.01–05, G01–G16, dependencias y rutas delimitadas; delta solo este Markdown y sección autocontenida de CHANGELOG.
- **A1:** revisión verificable de diez pares de aceptación/rechazo futuro y dieciséis gates de salida; no hay asserts de runtime ejecutados.
- **A2:** contrato de no exposición de PII, seguridad JWT/RBAC, propiedad tenant A/B, roles de soporte lectura y fail-closed; ningún enforcement modificado.
- **A3:** coherencia con inventario API/EF previo; HTTP, MySQL 8.4, Angular, Playwright, exporter y SLO reales **NO_APLICA** al diff R0, no PASS.
- **A4:** publicar commit fast-forward/CAS exact HEAD, confirmar Scope Lock/procedencia V5 completed/success en SHA publicado; PLAN/BITACORA readbacks.
- **A5:** revisión real por Task Javier diferente de DEV06, comparando diff, criterios, seguridad y limitaciones; puede resultar PASS o FAIL. Sin A5, no liberar M02.

No se implementaron eventos, nuevas Tasks, alertas, cobros, servidores, collectors, migraciones ni despliegues. El resultado de M01 es **documentación de gobernanza comprobable**, no certificación operativa de observabilidad.
