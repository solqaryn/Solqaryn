# SOLQARYN V5 — C11-04.M01.01: objetivo, no-alcance y no regresión de observabilidad

**Microtarea:** `C11-04.M01.01`, PLAN_MAESTRO fila 1694, M01 preparación documental de C11-04 (Observabilidad). **Responsable:** PRIMARY DEV 03 / JAVIER_CHATGPT / TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Fuente única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Rama:** `solqaryn/Solqaryn@dev`; **PROJECT_SCOPE_LOCK=STRICT**. Fecha: 2026-11-14.

## Resultado exigido y objetivo observable

Delimitar un contrato futuro de logs, métricas, trazas, alertas y costos **sin datos sensibles**, con evidencia pendiente de SLO, dashboard y prueba de diagnóstico. La presente unidad fija límites y criterios para siguientes M01/M02/M03, sin instalar agentes, exporters, alertas, dashboards externos, licencias ni integraciones. Es preparación documental, **no significa sistema de observabilidad certificado** ni disponibilidad real de telemetría externa.

La API .NET 10 ya registra instrumentos y correlación locales que requieren trazabilidad causal y evaluación posterior. El alcance futuro debe permitir diagnosticar errores 5xx, latencias de rutas con tráfico, fallas de dependencias y costos estimados por ambiente sin exponer identidad de clientes, JWT, cookies, tokens, payloads, SQL ni secretos. Los consumidores Angular 22.2.1 nunca se convierten en autoridad de seguridad, y el tenant/empresa/sucursal deben resolverse en backend.

## Frontera de alcance presente y futuro

**Incluido ahora:** referencia a código Git vivo; inventario de superficies observables; objetivo de correlación no sensible; 16 invariantes de no regresión; definición de pruebas futuras positivas y negativas; decisión explícita de no activar infraestructura ni declarar SLO medido.

**Trabajo futuro condicionado:** catálogo de señales y propietarios, matriz de riesgos, SLI/SLO respaldados por mediciones, endpoints de diagnóstico de bajo riesgo, reglas de cardinalidad de métricas, retención/consentimiento, aislamiento tenant A/B, dashboards, exporters, alertas anti-fatiga y presupuesto de costos. Cualquier modificación runtime requiere su M03 específica, autorización y CI del SHA exacto.

**Fuera de alcance:** cambios de `Program.cs`, middlewares, métricas, código de aplicación, bases de datos, migraciones, ACL/RBAC/JWT, datos o logs productivos, Cloudinary, Render, Aiven, proveedores externos, webhooks, dispatched events, otros Tasks, QA/main/PROD. No se compra servicio ni se crean dependencias externas.

## Estado de código verificado por blobs (existencia, no cobertura operativa)

| Fuente Git en dev | Blob verificado | Lectura y límite |
| --- | --- | --- |
| `backend/src/API/Middleware/CorrelationIdMiddleware.cs` | `3b76dca41bb4bc0b44f89c2a4664c79907f9f242` | Lee `X-Correlation-ID`, valida longitud/caracteres y usa `ILogger.BeginScope`; no acredita trazas externas |
| `backend/src/API/Observability/RequestObservability.cs` | `661c023526f1ca160dc70a82640d9dd1167808b0` | `Meter`, counters, histogram y snapshot local; tags método normalizado y clase HTTP, sin identidad tenant |
| `backend/src/API/Program.cs` | `01425cff56eb13e12022b1b81a2f322d17fd1fff` | Registra `RequestObservability` singleton y opciones, arquitectura DI existente; no comprobar exporter solo por DI |
| `backend/src/API/Middleware/ExceptionHandlingMiddleware.cs` | `baee40143e4e48b8df200e77892606af96d60b7f` | Excepciones y HTTP; el manejo de mensajes/logs requiere pruebas de PII y estado |
| `backend/src/API/Filters/MedirRendimientoBusquedaFilter.cs` | `10c346d14ca1d7e36ec02d029f5c762f6043396b` | Log de duraciones y percentiles de búsqueda sin términos crudos; validar cardinalidad y sanitización |
| `docs/PRODUCTION_READINESS_CONTRACT.md` | `10e43bd708e7ce9182e23038b17f947bc5b3fab2` | Documento previo no certifica exportador externo ni production-ready; no reutilizarlo como prueba actual |

La existencia de `RequestObservability` o `ILogger` **no demuestra** collector, backend externo, retención, cobertura del tráfico real, SLO, dashboard operativo ni respuesta de alertas. El texto de preparación productiva se trata como antecedente, no como autorización de despliegue.

## NR01–NR16 — invariantes de no regresión para el ciclo posterior

| ID | Regla obligatoria |
| --- | --- |
| NR01 | Ningún span, log o métrica incluye JWT, refresh token, cookies, API keys, secretos o cadenas de conexión |
| NR02 | No registrar body HTTP, query sensible, email, teléfono, documento de identidad o datos comerciales crudos |
| NR03 | CorrelationId saneado y acotado, generado de forma segura para valor recibido inválido |
| NR04 | TraceId/correlación no son credenciales ni otorgan acceso a recursos |
| NR05 | Métricas con etiquetas acotadas; no IDs individuales de tenant, usuario, cliente o recurso en labels |
| NR06 | Tenant A no puede consultar telemetría ni incidentes de tenant B por API o dashboard |
| NR07 | Soporte read-only no puede modificar configuración de alertas, retención, fuentes o roles |
| NR08 | Contadores de request y 5xx no exponen payload ni permiten inferir datos de otro tenant |
| NR09 | Fallo del collector/exporter es aislado y no produce caída del endpoint comercial |
| NR10 | Alertas no reintentan ni generan efectos comerciales, workers ni tareas de ChatGPT |
| NR11 | Cualquier SLI/SLO se calcula con intervalos, denominadores y tráfico realmente medidos |
| NR12 | No definir cumplimiento de SLO por checks de build, mocks o texto Markdown |
| NR13 | Costos de almacenamiento/exportación se justifican con cuotas y retención antes de activación |
| NR14 | Logs de excepción al cliente no revelan SQL, stack, secretos ni datos sensibles |
| NR15 | Health/readiness distingue vida del proceso de dependencia DB; no se interpreta como producción autorizada |
| NR16 | Cambios futuros conservan JWT/RBAC, filtros tenant, rollback y verificación GitHub Actions exact-SHA |

## Casos futuros de aceptación y rechazo (NO EJECUTADOS)

| Caso | Estímulo | Oráculo para evaluación futura |
| --- | --- | --- |
| P01 | Request correcto tenant A con CorrelationId saneado | Respuesta y log seguro vinculables sin información personal |
| P02 | Request 5xx sintético sin secretos | Counter/status y señal de diagnóstico, con muestreo documentado |
| P03 | Ruta lenta sintética con umbral configurado | Histograma/contador consistente y unidad ms |
| P04 | Tracing habilitado en entorno efímero autorizado | Span sin payload, con propagación de contexto y cardinalidad acotada |
| P05 | Servicio de métricas temporalmente indisponible | API mantiene disponibilidad funcional y registra fallo seguro |
| P06 | Operador autorizado lee tablero de tenant A | Sólo agregados permitidos y categorías documentadas |
| N01 | Header de correlación con saltos de línea/PII o exceso de 64 caracteres | Rechazar/sustituir ID sin inyección en logs |
| N02 | Usuario de A solicita observabilidad de B | 403/404, no fugas ni agregados inferibles |
| N03 | Rol soporte intenta alterar alertas o retención | Denegación, sin cambios y con auditoría |
| N04 | Instrumentación recibe tokens/PII en request | No aparecen en logs, tags, span o export |
| N05 | Etiqueta usa identificador único por request/tenant | Cardinalidad rechazada o anonimizada conforme política |
| N06 | Collector cae o tasa de ingesta excede cuota | Degradación controlada sin reintentos infinitos ni cobros |
| N07 | Error contiene SQL, stack o cadena de conexión | Respuesta genérica y log depurado |
| N08 | Dashboard sin fuente de datos o sin medición real | NO CERTIFICADO, jamás declarar SLO cumplido |

Todos estos P/N son **FUTUROS_NO_EJECUTADOS**. No se configuró/exportó telemetría ni se comprobó aislamiento multiempresa en runtime mediante esta microtarea.

## A0–A5, autorizaciones y rollback

**A0:** Skill canónica y `dev` exacto, fuente V5, PLAN fila 1694 PENDIENTE sin owner, COLA G/O=SI y DAG F/G=VALIDADO/SI fila 1655; preflight de archivos e impacto sólo Markdown. **A1:** contrato documentado con objetivos, límites, fuentes, 16 NR y P/N futuros. **A2:** amenazas de confidencialidad, cardinalidad, soporte y aislamiento A/B delineadas sin código ejecutable. **A3:** correspondencia de código API/.NET y antecedentes, sin certificar pruebas de HTTP/MySQL/exporter; esas suites son NO_APLICA para el diff Markdown. **A4:** publicación directa CAS fast-forward, verificar diff/blobs y `verify-solqaryn-project-scope` + `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` del SHA exacto; readback PLAN/COLA/BITACORA. **A5:** revisión independiente por otra de las cinco Tasks Javier, posterior a LISTO_DEV, antes de QA/main/PROD.

**Reversión:** `git revert` normal de documento y CHANGELOG, sin reescribir historia, preservar eventos BITACORA. Ningún cambio a bases, secretos, infraestructura, proveedores, usuarios, schedules o eventos.
