# SOLQARYN V5 — C11-04.M01.04: matriz de riesgos, supuestos y decisiones de observabilidad

**ID:** `C11-04.M01.04`; PLAN_MAESTRO fila 1697; M01 documental, C11-04 (logs, métricas, trazas, alertas y costos sin datos sensibles). **Owner:** PRIMARY DEV 04 / JAVIER_CHATGPT / `6aeacb7820ac8191b7d768a6f33dfa09`. **Origen:** [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit). **Referencia de código:** `solqaryn/Solqaryn@dev`, base `c0a8dce7539308064af9c2f4fd646ae3ff98a8d9`. **PROJECT_SCOPE_LOCK=STRICT**, 2026-11-14.

## Objetivo y frontera

Registrar riesgos POTENCIALES, supuestos NO_VERIFICADOS y decisiones PENDIENTES antes de implementar telemetría extendida. La evidencia principal está en `C11-04.M01.01` (alcance), `C11-04.M01.02` (actores/fronteras) y `C11-04.M01.03` (11 fuentes y 12 brechas). **Un riesgo identificado no es una vulnerabilidad confirmada.** No se atribuyen pruebas ejecutadas a escenarios de aceptación futuros.

**Alcance de este changeset:** sólo documentación `docs/v5` y una sección autocontenida de `CHANGELOG_AI.md`. No cambia runtime .NET 10, Angular 22.2.1, MySQL, JWT/RBAC, tenant, infraestructura, tarifas, eventos, backups, secrets, workflow ni automatizaciones. Sin QA/main/PROD ni proveedor externo.

## Fuentes técnicas con blobs del árbol inspeccionado

| Archivo | Blob Git | Evidencia factual |
|---|---|---|
| `backend/src/API/Middleware/CorrelationIdMiddleware.cs` | `3b76dca41bb4bc0b44f89c2a4664c79907f9f242` | Cabecera saneada, 64 caracteres máx.; no autoriza acceso |
| `backend/src/API/Observability/RequestObservability.cs` | `661c023526f1ca160dc70a82640d9dd1167808b0` | Counters, histograma, etiquetas normalizadas, sólo instrumento local |
| `backend/src/API/Middleware/RequestObservabilityMiddleware.cs` | `4fdb711c558750dfaf8a9c415f2f4058cbb4f336` | Registra tiempo/estado y datos EF agregados; configurable |
| `backend/src/API/Middleware/ExceptionHandlingMiddleware.cs` | `baee40143e4e48b8df200e77892606af96d60b7f` | Hay rutas que registran objetos Exception; revisar sanitización en futuros sinks |
| `backend/src/API/Filters/BusquedaRendimientoMetricas.cs` | `306814e497ef4c80ef365406f54d5ae12463e73a` | Ventana hasta 200 muestras por ruta, no prueba techo global de claves |
| `backend/src/API/Observability/DbQueryTimingInterceptor.cs` | `8c59faffe25b4b677ba04ec65fa9feec2d2faca8` | Captura duración de comando EF por contexto sin texto SQL |
| `backend/tests/Solqaryn.Tests/API/RequestObservabilityTests.cs` | `e6b873e7929704823468be5c20ff8f868812a18e` | Contiene pruebas xUnit; su existencia no equivale a ejecución dentro de esta tarea |
| `docs/v5/c11-04-m01-03-inventario-codigo-evidencia-observabilidad.md` | `9c5f2eeda4f60226a681a08db883c77f47c633d7` | Registro M01.03 publicado y delimitación de brechas |

## R01–R12 — riesgos potenciales y mitigación futura

| ID | Riesgo potencial, NO CONFIRMADO | Severidad prospectiva | Control/ensayo requerido |
|---|---|---|---|
| R01 | Excepción con contenido privado incluido en registro estructurado | Alto | Probar redacción de propiedades/Exception antes de sinks futuros; no asumir filtrado automático |
| R02 | X-Correlation-ID externo tratado erróneamente como autoridad de acceso | Alto | Correlación sólo diagnóstico; permisos siempre backend JWT/RBAC/tenant |
| R03 | Ruta/label no acotada causa cardinalidad y consumo de memoria | Alto | Catálogo de etiquetas y cuota global; revisar diccionario de muestras por ruta |
| R04 | Exporter o proveedor de observabilidad genera costo sin cuota aprobada | Alto | Budget/limite de eventos y aprobación explícita de proveedor antes de habilitar |
| R05 | Consulta diagnóstica permite inferir actividad de otro tenant | Alto | Pruebas A/B con 403/404 y sin agregados cruzados |
| R06 | Rol soporte altera umbrales, retención o alertas | Alto | Aplicar read-only efectivo por backend, roles separados y auditoría |
| R07 | Declarar SLO PASS con ventana incompleta o telemetría ausente | Medio | Estado NO_MEDIDO ante lagunas; firma de fuente/ventana/SLI |
| R08 | Caída de collector degrada API o bloquea ruta crítica | Alto | Exportación desacoplada con límite de cola y prueba fail-open operacional sin fallar auth |
| R09 | Reintentos ilimitados duplican señales o provocan facturación | Alto | Backoff finito, idempotencia de ingesta, circuit breaker y métricas de pérdidas |
| R10 | Retención y residencia de logs no cumplen política de PII/tenant | Alto | Inventario de datos, TTL, acceso y borrado aprobado antes de servicios externos |
| R11 | Tracing de alta cardinalidad captura payloads, SQL o identificadores | Alto | Allowlist de atributos y muestreo; prohibir headers/URL sensibles |
| R12 | Diagnóstico publica métricas DB sensibles a usuarios no autorizados | Medio | Separar trazas internas de respuesta y revisar Server-Timing y RBAC |

## S01–S08 — supuestos explícitos sin evidencia de certificación

| ID | Supuesto | Estado |
|---|---|---|
| S01 | No se ha certificado proveedor collector/exporter activo | NO_VERIFICADO |
| S02 | No se dispone de base estadística firmada para SLO | NO_VERIFICADO |
| S03 | La tasa de eventos/ingesta por tenant aún no está medida | NO_VERIFICADO |
| S04 | No se ha ensayado retención/borrado de logs fuera de proceso | NO_VERIFICADO |
| S05 | No está acreditada una consola autorizada tenant-aware | NO_VERIFICADO |
| S06 | No hay contrato aprobado de costos/proveedores para señales externas | NO_VERIFICADO |
| S07 | No se ha confirmado protección total contra PII en todas las excepciones/sinks | NO_VERIFICADO |
| S08 | No hay pruebas de carga de cardinalidad máxima para etiquetas y rutas | NO_VERIFICADO |

La falta de evidencia no demuestra que una función no exista. Cualquier afirmación operativa requiere leer fuente, ambiente y checks específicos; por defecto queda **NO_VERIFICADO**.

## D01–D08 — decisiones pendientes de autoridad humana y técnica

| ID | Decisión | Responsable decisor propuesto, no asignación automática | Criterio de cierre |
|---|---|---|---|
| D01 | Política de campos permitidos/suprimidos | Equipo seguridad + propietario | Pendiente; no activar exporter |
| D02 | Catálogo de etiquetas, cardinalidad, cuotas por instancia y tenant | Arquitectura | Pendiente; degradar sin afectar API |
| D03 | SLI/SLO, ventanas, umbrales y tratamiento de ausencias | Propietario producto y SRE | Pendiente; no declarar cumplimiento |
| D04 | Modelo de autorización de observabilidad por plataforma/tenant/soporte | Seguridad | Pendiente; test A/B obligatorio |
| D05 | Retención, acceso, borrado, residencia y procedimiento incidentes | Propietario y legal | Pendiente; sin datos externos |
| D06 | Collector y proveedor/eventual costo | Propietario | Pendiente; no compras/activación |
| D07 | Alertas, on-call, deduplicación, presupuesto ruido/costo | Operación | Pendiente; nada automático |
| D08 | Plan de ensayo, rollback y evidencia de throughput | QA/arquitectura | Pendiente; requiere CI dirigida de M03 |

No crear nuevas Tasks ni enviar notificaciones por este documento. La tabla no autoriza a los responsables propuestos a habilitar proveedores o editar permisos; cada cambio debe pasar la autoridad vigente.

## Ensayos propuestos P01–P08 y N01–N08 (FUTUROS_NO_EJECUTADOS)

| Par | Positivo futuro | Negativo futuro |
|---|---|---|
| P01/N01 | CorrelationId válido no sensible | Reemplazar header inválido o demasiado largo; jamás usarlo como login |
| P02/N02 | Métricas sólo método normalizado y clase de HTTP | Ruta arbitraria/ID personal no pasa como label de alta cardinalidad |
| P03/N03 | Error 5xx genera contador interno sin payload | Exception con secreto ficticio no aparece en sink |
| P04/N04 | Soporte autorizado consulta indicadores tenant A | Soporte sin rol o tenant B recibe 403/404 sin inferencias |
| P05/N05 | Worker observabilidad pierde proveedor sin bloquear negocio | Fallo prolongado no causa retries infinitos ni cargos |
| P06/N06 | SLO basado en ventana y fuente con datos | Falta de medidas produce NO_MEDIDO |
| P07/N07 | Timings EF agregados por request sin SQL | Consulta MySQL con parámetros privados no expone SQL/connection string |
| P08/N08 | Alerta definida con límite de frecuencia y retención | Ruido/tormenta de alerts no envía notificaciones ilimitadas |

Estos oráculos requieren xUnit, integración HTTP JWT/RBAC, MySQL efímero, pruebas tenant A/B y simulación de exporter sólo cuando exista microtarea M03 autorizada. No se inventan resultados ni se promueve a QA/main/PROD.

## Auditorías y rollback

- **A0:** id exacto, fuente institucional, DAG F/G VALIDADO/SI, COLA G/O=SI y ausencia de owner antes de claim; analizar sólo `docs/v5` + CHANGELOG.
- **A1:** coherencia entre 12 riesgos, 8 supuestos, 8 decisiones y escenarios P8/N8; son especificación documental sin ejecución.
- **A2:** riesgos de confidencialidad, cardinalidad, cross-tenant, JWT/RBAC y soporte sólo lectura definidos; no se modifican ACL ni datos.
- **A3:** trazabilidad de ocho blobs y contratos del mismo árbol; ninguna suite HTTP/MySQL/Playwright se acredita por simple referencia.
- **A4:** diff exclusivo de Markdown, commit fast-forward con CAS de HEAD, chequeos exact-SHA `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` en `completed/success`, BITACORA y readback.
- **A5:** pendiente revisión independiente por Task Javier distinta de la autora antes de liberar siguientes gates. Rollback mediante `git revert <SHA-publicado>` normal, sin force-push ni modificación de historia.
