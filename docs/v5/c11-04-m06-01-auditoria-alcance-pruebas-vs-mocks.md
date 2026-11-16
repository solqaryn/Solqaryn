# C11-04.M06.01 — auditoría de pruebas frente al alcance real y los mocks

**Ámbito:** SOLQARYN, repositorio `solqaryn/Solqaryn`, rama `dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Task autora:** DEV04, ID `6aeacb7820ac8191b7d768a6f33dfa09`. **PLAN_MAESTRO:** fila 1724, alcance R0 documental. **Base de revisión:** `f2444b55d7d63b74bdde90284f678ef3ad860159`.

## Objetivo, materialidad y fronteras (A0)

La obligación M06.01 es verificar que la fase C11-04.M05.01–06 prueba **lo que dice probar**, sin extrapolar una sesión simulada a autenticación JWT, aislamiento multiempresa real, MySQL, un exportador de telemetría, costes o SLO de producción. Se inspeccionaron las evidencias históricas en PLAN_MAESTRO/ BITACORA y los artifacts y check-runs de GitHub Actions vinculados a SHAs causales. El documento no modifica código, rutas, roles, endpoints, arquitectura, datos, reglas de cola ni infraestructura; `A1/A2/A3` de runtime de **esta auditoría** se califican NO_APLICA por ausencia de cambios funcionales. La comprobación documental de correspondencia y las evidencias observables sí son obligatorias. Reversión técnica: `git revert` normal del changeset documental, sin reescribir historia, BITACORA ni datos.

## Matriz de correspondencia verificada (fase M05)

| Microtarea | SHA causal | Prueba acreditada y clase real | Frontera que NO acredita |
| --- | --- | --- | --- |
| M05.01 | `29f4e4b1605d291566db73a840683d2be0c2a157` | Vitest 22/22 y Chromium 14/14, controles de vistas de observabilidad, revocación simulada y error 429, run 43866547232 | No autorización JWT/tenant HTTP real, datos comerciales, exporter ni MySQL |
| M05.02 | `a31cb7bae9a1acf7151f37e69711d342fd89d05b` | xUnit .NET 10 en proceso: contratos DTO, casos negativos, errores opacos y matriz sintética A/B/revocación | No middleware JWT real ni petición HTTP a API ni MySQL |
| M05.03 | `ee1169b09f9638cf58cf48df1879f80de8abce84` | xUnit Release 14/14 con `LoggerFactory` / `ILoggerProvider` reales **en proceso**; corrida 43960859810 | No exporter externo, persistencia duradera, MySQL ni ejecución HTTP |
| M05.04 | `4a6a4c4d78929a5b025cc368f00f21e6ac531269` | 22 Vitest + 15 Chromium GitHub-hosted; caso 200 ilustrativo, 320/1280, sin doble GET/exportación | Sesión 200 sintética; no JWT, acceso a proveedor ni SLO real |
| M05.05 | `0243c612b57c8df312acc996df5b6086f435d278` | 22 Vitest + 16 Chromium; 429, revocación visual, no fuga de texto privado, no doble solicitud | 429 simulado; no rate-limiter ni aislamiento efectivo backend |
| M05.06 | `e8a2663ccec7964a005ca7b73bdebebc0158ddb0` | **Playwright/Chromium real 4/4** y **xUnit .NET 10 14/14** en GitHub Actions 43971041616; 320/375/768/1280; Tab real; 403/503, GET retenido y no exporter | HTTP 200/403/503 son fixtures; sin JWT, MySQL, exporter externo, CWV, SLO o coste medido |

Los valores son evidencias correspondientes a esos SHAs, no permisos para reutilizar PASS en otro SHA sin cotejar blobs y configuración de pruebas. Cada M05 se conserva en LISTO; A5 independiente de M05.06 quedó registrado en esta ejecución por DEV04 (autor original DEV03). Los nombres de trabajos del proveedor se cotejaron con logs cuando fueron necesarios; los jobs auxiliares SKIPPED **no** cuentan como PASS.

## Aserciones adversariales y fronteras de aceptación

- **Positivo demostrable:** en Chromium real se renderizan tarjetas/puntos y controles esperados en las dimensiones señaladas; en .NET se ejecutan pruebas xUnit reales contra código de logging en proceso.
- **Negativo demostrable dentro del fixture:** 403/429/503 ocultan los elementos privados, revocación visual y errores opacos; los tests de M05.06 comprueban foco visible con Tab real y GET retenido sin duplicación.
- **Frontera:** 320 px y 1280 px muestran comportamiento responsive; no son métricas de rendimiento web ni dispositivos físicos certificados.
- **Error/abuso no cubierto extremo a extremo:** sesiones sintéticas no acreditan un JWT firmado, roles reales de múltiples tenants o revocación en backend; no se observó envío a un collector real, persistencia durable, SLO de disponibilidad, RTO/RPO, presupuesto monetario ni métricas Core Web Vitals reales.
- **Riesgo abierto:** falso positivo si se presentaran estos mocks como certificación de autorización real o telemetría operativa. Clasificación: R0 de esta auditoría; los riesgos de cambios futuros de JWT, tenant, integraciones y seguridad son R2 y exigen pruebas propias A1–A4 (y A5 ajena).

## Comprobación de calidad y decisiones (A1–A5)

**A0:** la matriz relaciona los seis IDs existentes con SHA de origen, técnica de prueba, resultado observado y limitaciones; no aparecen filas nuevas ni cambios de runtime.

**A1 (documental):** revisión de consistencia: los seis IDs se encuentran LISTO con autor identificado, pruebas en GitHub para su tecnología y procedimientos capaces de fallar en escenarios positivos, negativos, fronteras o error. Los mocks se etiquetan explícitamente y no se reinterpretan como conexión viva. Para este documento no se inventa un test HTTP, browser o base de datos adicional.

**A2/A3:** NO_APLICA **a la implementación de este documento** (sin JWT/RBAC/tenant, DB o proveedor alterados). Las evidencias ya existentes de M05.01–06 se describen con su nivel real, no con alcance extrapolado.

**A4:** publicar exclusivamente en dev con CHANGELOG autocontenido, comprobar `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` en el SHA **nuevo** o equivalencia causal verificable, y efectuar readback de PLAN_MAESTRO/COLA/DAG/BITACORA bajo V17. Si CI está QUEUED/IN_PROGRESS, conservar VALIDANDO y no declarar LISTO.

**A5:** revisión técnica independiente por otra Task Javier distinta de DEV04; el autor de este documento no puede autoaprobarse.

## Decisiones y siguiente control

El lote M05 dispone de evidencias de pruebas dirigidas y de algunas integraciones **en proceso**, pero **no constituye certificación de producción** de telemetría, exportador, MySQL, JWT, RBAC real ni aislamiento entre clientes. `CWV/SLO/coste/RTO/RPO=NO_MEDIDO`, conservar sin numerador/denominador artificial. El seguimiento de estas brechas se incorpora a M06.02–06 y tareas técnicas habilitadas por DAG sin poner manualmente SI ni superar controles de seguridad. No promover QA/main/PROD, no crear autorizaciones humanas ni servicios externos.

**Readback y SHA definitivo:** se registrarán de manera causal en PLAN_MAESTRO y BITACORA al publicar y pasar de estado, sin inventarlos en este documento.
