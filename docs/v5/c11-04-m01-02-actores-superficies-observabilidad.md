# SOLQARYN V5 — C11-04.M01.02: actores, entradas, salidas y superficies de observabilidad

**Microtarea:** `C11-04.M01.02` (PLAN_MAESTRO fila 1695), preparación documental M01, C11-04 Observabilidad. **Owner:** PRIMARY DEV 03 / JAVIER_CHATGPT, Task `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Fuentes:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`); `C11-04.M01.01` y repositorio `solqaryn/Solqaryn` rama `dev`. **PROJECT_SCOPE_LOCK=STRICT**, fecha 2026-11-14.

## Propósito y límites

Identificar quién genera, consume, administra y audita señales de logs, métricas, trazas, alertas y costos, con entradas/salidas y fronteras de confianza verificables. Los sistemas existentes aportan middleware de correlación e instrumentos locales; esta tabla define interfaces **futuras/no implantadas** y no declara exportadores, tableros o telemetría operativa. El objetivo de C11-04 incluye SLOs, dashboards y prueba de diagnóstico como evidencia mínima futura, que aquí no se declara entregada.

**No se modifican:** controllers, Application/Domain/Infrastructure, `Program.cs`, pipeline, DB/MySQL, JWT, RBAC, tenant/sucursal, servicios externos, puertos, workflows, secrets, costos, alertas, events, Tasks, QA/main/PROD. Las configuraciones externas y el acceso a telemetría requieren permiso y revisión separados. Ningún usuario puede consultar cross-tenant mediante un correlationId.

## A01–A14 — actores y privilegios a gobernar

| Actor | Papel y privilegio máximo contemplado | Regla de frontera |
| --- | --- | --- |
| A01 Navegador Angular | Solicita API y recibe ID de correlación seguro | No es autoridad de permisos ni envía secretos a observabilidad |
| A02 API Controller | Genera códigos HTTP y maneja recursos autorizados | Tenant/sucursal se verifican en backend |
| A03 Middleware de Correlación | Sanitiza ID y crea alcance de logger | ID no equivale a bearer ni identifica un tenant |
| A04 Middleware de Excepciones | Normaliza errores y registra diagnóstico | Respuesta segura sin stack/PII/SQL/tokens |
| A05 Contadores HTTP .NET | Registra métricas de método/clase HTTP | Labels bajo cardinalidad acotada y sin usuarios |
| A06 Filtro búsqueda | Observa latencia y volumen sin término original | Rechazar payload, SKU, correo y teléfono |
| A07 Servicio de Aplicación | Produce eventos técnicos autorizados sin datos privados | Mantener fronteras Domain/Application |
| A08 Repositorio EF/MySQL | Emite timings sanitizados, no SQL sensible | Credenciales y resultados nunca en logs |
| A09 Administrador de plataforma | Define controles globales si autorización futura específica | No hereda permisos de tenant empresarial |
| A10 Administrador de empresa A | Consulta sólo diagnósticos de A si existen capacidades autorizadas | Nunca lee empresa B ni configuraciones globales |
| A11 Soporte lectura | Diagnóstico sólo de lectura documentado | No alterar reglas, cuotas, retención o alertas |
| A12 Operador de seguridad | Audita accesos y anomalías según permiso aprobado | Uso de mínimos privilegios, evidencias y retención |
| A13 Proveedor/Collector externo | Receptor futuro de datos anonimizados y acotados | Nunca habilitar sin contrato/cuota y revisión de seguridad |
| A14 Task Javier V5 | Lee evidencia de dev, registra A0–A5 y readbacks | No crea monitor perpetuo ni atribuye runtime PASS falso |

Los actores A09–A13 son roles/consumidores *propuestos*, no endpoints o cuentas provisionadas. La tabla no certifica IAM ni implementa dashboards.

## I01–I14 / O01–O14 — entradas y salidas de señales

| ID | Entrada o evento fuente | Salida/documento observable futuro | Restricción |
| --- | --- | --- | --- |
| I01/O01 | Método HTTP normalizado + status class | Counter de peticiones agregadas | Sin path con IDs ni querystring |
| I02/O02 | Duración ms de request | Histograma de latencia | No inferir SLO desde un único request |
| I03/O03 | HTTP 5xx | Counter errores por categoría segura | Nunca payload o excepción completa |
| I04/O04 | Umbral de petición lenta configurado | Counter slow-requests | Valor validado, sin alertas de pago automáticas |
| I05/O05 | Header X-Correlation-ID acotado | ID saneado en response y logger scope | Rechazar caracteres peligrosos/PII |
| I06/O06 | Activity.TraceId actual si existe | TraceId técnico para correlación | No publicar secretos ni suplantar autorización |
| I07/O07 | Error de validación | Log y código HTTP seguro | Sin stack/variables privadas |
| I08/O08 | Excepción de persistencia | Respuesta genérica + diagnóstico autorizado | No exponer SQL, credenciales |
| I09/O09 | Tiempo de búsqueda en ruta permitida | Percentiles y cantidad resultados agregados | Sin SKU/código/término crudo |
| I10/O10 | DbQueryTimingInterceptor | Duración DB agregada si autorizada | No labels con SQL raw/tenant id |
| I11/O11 | Health del proceso | Estado vivo no equivalente a readiness DB | No certificar PROD |
| I12/O12 | Health/readiness DB | Señal de dependencia con permisos | No divulgar endpoints internos/secretos |
| I13/O13 | Presupuesto y retención futuros | Estimación de costo, no factura/consumo real | Revisar cuota y autorización antes de activar |
| I14/O14 | Error rate/latencia con ventana real | SLI/SLO y alerta futura auditables | No inventar percentiles o datos de producción |

Todas las O futuro son criterios de observabilidad propuestos, **no garantías** de que exista exporter, dashboard, retención configurada o servicio de alertas.

## F01–F12 — fronteras de seguridad y concurrencia

| Frontera | Riesgo | Política obligatoria |
| --- | --- | --- |
| F01 Angular→API | Cliente suplanta identidad/empresa | JWT/RBAC/tenant verificados en backend |
| F02 Header→Middleware | CorrelationId adversarial/inyección | Alfanumérico seguro, largo <=64, regeneración |
| F03 API→Logger | Payload privado en logs | Sanitizar; no registrar bodies, cookies ni tokens |
| F04 API→Metric Meter | Cardinalidad explosiva o PII en etiquetas | Labels cerradas: método normalizado y clase HTTP |
| F05 Error→Response | Stack SQL/PII en cliente | Envelope seguro con correlación acotada |
| F06 Aplicación→DB | SQL sensible/excepciones de EF | Log depurado, datos tenant A/B aislados |
| F07 Plataforma→Empresa | Admin global accede como enterprise sin membresía | Reglas separadas y autorización explícita |
| F08 Soporte→Configuración | Rol read-only muta alertas/cuotas | Denegar cualquier escritura |
| F09 Tenant A→Tenant B | Dashboard cross-tenant | Prueba negativa A/B antes de exponer |
| F10 API→Collector externo | Exportación transfronteriza de PII o pagos | Proveedor/cuota/retención requieren autorización explícita |
| F11 Task→Tasks | Llamadas a automatizaciones vistas como worker infinito | NO implementar event dispatcher ni nuevos Tasks |
| F12 Observabilidad→Operación | Alerta ejecuta cambios o restarts | Separación de alertamiento, acciones humanas y gobierno |

## Evidencia reproducible de fuentes actuales

- `backend/src/API/Middleware/CorrelationIdMiddleware.cs`, blob `3b76dca41bb4bc0b44f89c2a4664c79907f9f242`: valida el header, usa `TraceIdentifier`, logger scope y response.
- `backend/src/API/Observability/RequestObservability.cs`, blob `661c023526f1ca160dc70a82640d9dd1167808b0`: Meter `Solqaryn.API`, cuatro instrumentos, etiquetas de método y clase HTTP, snapshot local.
- `backend/src/API/Program.cs`, blob `01425cff56eb13e12022b1b81a2f322d17fd1fff`: registra el servicio de métricas y la configuración.
- `backend/src/API/Filters/MedirRendimientoBusquedaFilter.cs`, blob `10c346d14ca1d7e36ec02d029f5c762f6043396b`: logger de latencias de búsquedas con rutas acotadas.
- `backend/src/API/Middleware/ExceptionHandlingMiddleware.cs`, blob `baee40143e4e48b8df200e77892606af96d60b7f`: manejo de errores y logging, no cobertura ni ausencia universal de PII.
- `docs/PRODUCTION_READINESS_CONTRACT.md`, blob `10e43bd708e7ce9182e23038b17f947bc5b3fab2`: contrato previo de readiness, expresamente no demuestra exporter externo ni autorización a PROD.

**Control de no-regresión:** el nuevo texto y la sección CHANGELOG son los únicos archivos editables previstos. El sistema deberá revalidar riesgos de confidencialidad, etiquetas de alta cardinalidad, costos y aislamiento antes de implementar M03 o proveedores externos. Los ejemplos P01–P06/N01–N08 del documento M01.01 se consideran pruebas **FUTURAS_NO_EJECUTADAS**, no resultados.

## Auditorías y rollback

A0: validar identity/Skill/HEAD, CAS del PLAN fila1695, COLA G/O=SI, DAG fila1656 VALIDADO/SI, diff documental. A1: tabla completa de A01–A14, I/O01–14 y F01–F12, con separaciones explícitas. A2: revisar tenant A/B, soporte de lectura, JWT/RBAC, secretos y PII, sin cambio runtime. A3: comprobar blobs Git de middleware, meters, programa, filtro y documentación; HTTP/MySQL/collector/Angular Playwright **NO_APLICA** por Markdown. A4: fast-forward CAS dev, evidencias de SHA exacto, `verify-solqaryn-project-scope` y check `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success`, readback PLAN/COLA/BITACORA. A5: revisor con Task Javier distinto, antes de QA/main/PROD.

Rollback: `git revert` normal, sin force-push ni borrar historia de BITACORA.
