# SOLQARYN V5 — C11-04.M01.03: inventario de código y evidencia de observabilidad

**Microtarea:** `C11-04.M01.03` · PLAN_MAESTRO fila 1696 · C11-04 Observabilidad · M01 preparación. **Responsable:** PRIMARY DEV 04 / JAVIER_CHATGPT / Task `6aeacb7820ac8191b7d768a6f33dfa09`. **Fuente institucional:** [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [Sheet V5](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). **Corte de código:** `solqaryn/Solqaryn@dev`, commit `21fd394447c71e3727827b269aedcd07ca964386`. **Scope Lock:** STRICT. Fecha 2026-11-14.

## Propósito y alcance verificable

Inspeccionar contratos, rutas y evidencia real disponible para el objetivo futuro de logs, métricas, trazas, alertas y costos sin datos sensibles. El inventario fija blobs Git específicos y separa tres planos: **código existente**, **pruebas definidas** y **funciones futuras no certificadas**. Las fuentes xUnit aparecen como código inspeccionado, no como resultados de una ejecución de pruebas de esta microtarea.

**Cambio actual:** sólo este documento y una sección de `CHANGELOG_AI.md`. Sin modificación de API, middleware, EF/MySQL, Angular, configuración, RBAC/JWT, secretos, SaaS multiempresa, proveedores, workflows, precios, Tasks, QA/main/PROD. No se crea collector, exporter, dashboard, alerta, worker, webhook ni servicio externo.

## Inventario trazable de 11 fuentes Git

La columna de blob corresponde al árbol de `dev` en el SHA citado. `Leído` significa inspección de contenido fuente, **no** despliegue ni observación de ambiente.

| # | Ruta | Blob SHA | Hallazgo y límite |
|---|---|---|---|
| 01 | `backend/src/API/Program.cs` | `01425cff56eb13e12022b1b81a2f322d17fd1fff` | Leído: registra interceptores EF, opciones y singleton de observabilidad; conecta los tres middleware de correlación, observabilidad y excepciones. |
| 02 | `backend/src/API/Middleware/CorrelationIdMiddleware.cs` | `3b76dca41bb4bc0b44f89c2a4664c79907f9f242` | Leído: cabecera X-Correlation-ID con límite 64, caracteres seguros, GUID alternativo y alcance de logger. |
| 03 | `backend/src/API/Middleware/RequestObservabilityMiddleware.cs` | `4fdb711c558750dfaf8a9c415f2f4058cbb4f336` | Leído: mide duración, HTTP status, consultas EF; genera contadores y logs locales según opciones. |
| 04 | `backend/src/API/Middleware/ExceptionHandlingMiddleware.cs` | `baee40143e4e48b8df200e77892606af96d60b7f` | Leído: captura excepciones y selecciona respuestas HTTP; varias ramas registran objeto Exception, sujeto a revisión de PII antes de exportar. |
| 05 | `backend/src/API/Observability/RequestObservability.cs` | `661c023526f1ca160dc70a82640d9dd1167808b0` | Leído: Meter interno con counters solicitudes, errores 5xx, lentas e histograma; tags método acotado y clase HTTP. |
| 06 | `backend/src/API/Observability/DbQueryTimingInterceptor.cs` | `8c59faffe25b4b677ba04ec65fa9feec2d2faca8` | Leído: suma duración de comandos EF al estado por petición; este interceptor no exporta SQL. |
| 07 | `backend/src/API/Observability/RequestPerformanceContext.cs` | `ab7218141439428bfd238c63ee9f8683dffe84c9` | Leído: contadores y duración DB por HttpContext, protegidos por lock. |
| 08 | `backend/src/API/Filters/BusquedaRendimientoMetricas.cs` | `306814e497ef4c80ef365406f54d5ae12463e73a` | Leído: hasta 200 muestras por clave de ruta y p50/p95 en memoria; no demuestra límite global de claves. |
| 09 | `backend/tests/Solqaryn.Tests/API/RequestObservabilityTests.cs` | `e6b873e7929704823468be5c20ff8f868812a18e` | Leído: pruebas xUnit codificadas para counters, clase de método, contexto DB, correlación y middleware; no reejecutadas en esta unidad. |
| 10 | `docs/v5/c11-04-m01-01-alcance-no-regresion-observabilidad.md` | `f02a3032240ae60260bf5dcee41d617a33787b9b` | Leído: alcance M01, invariantes NR y exclusiones de cambios runtime. |
| 11 | `docs/v5/c11-04-m01-02-actores-superficies-observabilidad.md` | `4b245c717ebd577f2db72360b160e438ec0d45d6` | Leído: actores, entradas/salidas, límites de confianza y restricciones de permisos. |

## Lo que puede y no puede afirmarse

- **Existente en código:** medición de solicitudes, errores 5xx, duración y solicitudes lentas mediante `Meter`; contexto de correlación; timings EF por solicitud; manejo de excepciones; pruebas xUnit almacenadas en Git.
- **No acreditado por esta unidad:** exportación externa, pipeline OpenTelemetry/collector activo, monitoreo de producción, tasas SLO, dashboard servido a usuarios, alertas operativas, costo real, pruebas HTTP/MySQL/Playwright en entorno, acceso cross-tenant o capacidad de despliegue.
- **Seguridad:** una cabecera `X-Correlation-ID` válida se propaga para diagnóstico; **no es** identidad, autenticación ni autorización. La seguridad real permanece en el backend. Cualquier sink futuro deberá suprimir token, cookie, email, teléfono, payload, SQL, identificadores sensibles y datos multiempresa.
- **Alcance de afirmación:** verificación de blobs/lectura estática. Los checks del SHA del repositorio acreditan controles de integración generales, pero no prueban por sí solos que una integración externa de observabilidad exista o funcione.

## G01–G12 — brechas de evidencia, no vulnerabilidades confirmadas

- G01 No hay evidencia de exporter/collector externo operando en esta revisión; no inferir ausencia absoluta ni prometer entrega.
- G02 SLI/SLO aún sin mediciones de producción/ambiente autorizadas y ventanas firmadas.
- G03 No se verificó dashboard real ni permisos de consulta por tenant A/B.
- G04 No se confirmó regla de alertas, deduplicación, escalamiento o presupuesto de fatiga.
- G05 No se auditó cardinalidad global de rutas/labels bajo tráfico adversarial.
- G06 No se acreditó filtrado integral de mensajes y excepciones frente a PII, JWT o secretos en sinks externos.
- G07 No se evaluó acceso de soporte read-only al diagnóstico por permisos backend.
- G08 No hay prueba cruzada de correlación de Browser -> API -> DB en sistema de integración.
- G09 No se verificó retención, borrado, residencias y permisos de lectura de logs fuera del proceso.
- G10 No se acreditaron costos, cuotas ni límites reales de proveedor de telemetría.
- G11 No se probó impacto de observabilidad en rendimiento ni rollback runtime.
- G12 No se realizaron ensayos tenant A/B con datos de dos empresas y denegaciones de exportación.

## Matriz de comprobación futura (no ejecutada aquí)

| Caso | Positivo esperado | Negativo esperado | Prueba causal requerida |
|---|---|---|---|
| P01/N01 | Counter 2xx y duración agregados | Método/etiqueta fuera de catálogo → `OTHER` | xUnit dirigida con listener del Meter |
| P02/N02 | 5xx incrementa error sin payload | Excepción con token ficticio no llega a log/sink | test de logger y redacción de secretos |
| P03/N03 | CorrelationId seguro y acotado | ID >64 o con control/espacios se reemplaza | xUnit middleware y respuesta |
| P04/N04 | Tenant A lee exclusivamente diagnóstico A | usuario de B no accede a A | integración HTTP con JWT/RBAC A/B |
| P05/N05 | P95 de muestra acotada y conocida | alta cardinalidad de rutas no agota recursos | test de carga y cuota definida |
| P06/N06 | Timings EF disponibles para el request | SQL/connection string no aparece en señal externa | integración EF/MySQL efímera |
| P07/N07 | Error 5xx correlacionable sin secreto | etiqueta, stack o payload sensible no se exporta | collector simulado, nunca proveedor real sin autorización |
| P08/N08 | SLO con ventana, umbral y fuente firmada | ausencia de datos impide declarar SLO PASS | revisión de fuente de métricas + consulta de lectura |

Estos casos son **FUTUROS_NO_EJECUTADOS**, no un conteo de pruebas aprobadas ni criterio para liberar M03, QA, main o PROD.

## Decisiones, impacto y rollback

- Componente: observabilidad backend y catálogo documental de C11-04. Capas afectadas por este changeset: **ninguna capa ejecutable**. Consumidores futuros: API, soporte autorizado, operadores de seguridad y dashboard, sujetos a autenticación y autorización backend.
- Datos: cero migraciones/consultas adicionales, cero cambios de tenant, cero eventos, cero transferencias de datos o costos.
- Antes de una implementación M03: documentar cardinalidad máxima, inventario de campos permitidos, retención, control de cuota, roles de consulta, evidencia SLO y pruebas A/B negativas. No asumir que una fuente documental libera esos gates.
- Validación A0–A4 de esta unidad: comprobar existencia y blobs de fuentes, exactitud de afirmaciones, ausencia de datos sensibles, diff exclusivamente Markdown, Scope Lock y procedencia de CHANGELOG sobre SHA publicado; las suites runtime no causales se consignan NO_APLICA documentado, no como PASS.
- Rollback: `git revert <SHA-publicado>` en commit normal sobre dev, nunca force-push; revertir únicamente el documento/entrada de CHANGELOG conforme a gobernanza.
- A5: `A5_PENDIENTE` para Task Javier distinta de la autora. Nada aquí autoriza QA/main/PROD.
