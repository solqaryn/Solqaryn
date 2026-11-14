# C00-03.M06.01 — Auditoría causal de suficiencia de pruebas de cobertura V5

**Objeto:** verificar que C00-03.M05.01–06 comprueba únicamente lo que se implementó, sin llamar a fixtures, mocks, almacenamiento en memoria o una sesión sintética una certificación institucional. Es un entregable de análisis: no cambia API, servicios, UI, DB, permisos ni workflows.

**Autoridad y dependencias:** repositorio solqaryn/Solqaryn, rama dev, Plan V5 FINAL. En PLAN_MAESTRO se verificaron M05.01–06 en LISTO, DAG_TECNICO F=VALIDADO/G=SI y COLA G/O=SI para esta preparación. Fuentes consultadas en HEAD 5427439834a88b95d7370cf3f9690bdad4adbbf7 y checks nativos de cada SHA de origen. No se simula una nueva ejecución de suites históricas.

| Microtarea | Evidencia inspeccionada y SHA de origen | Alcance real que sí respaldan las pruebas | Límite NO certificado |
| --- | --- | --- | --- |
| M05.01 | `backend/tests/Solqaryn.Tests/Application/V5CoverageM05RuleRegressionTests.cs`, blob `8d4f5978`, commit `f22e5441`; backend, seguridad, autorización y Scope Lock SUCCESS | xUnit de reglas de evidencia, catálogo, duplicidad, SHA obsoleto y rechazo atómico de lote inválido | No valida evidencia externa ni estados vivos de Sheets |
| M05.02 | `backend/tests/Solqaryn.Tests/API/V5C0003M0502CoverageHttpContractTests.cs`, blob `c91d8c87`, commit `8d6e12e8`; backend, seguridad, auth y Scope Lock SUCCESS | TestServer para DTO permitido HTTP 200 y negativas HTTP 400 sin afirmar evidencia certificada | Endpoint de prueba, no JWT/RBAC de API productiva ni MySQL |
| M05.03 | `backend/tests/Solqaryn.Tests/Application/V5C0003M0503ProviderIntegrationTests.cs`, blob `219c5375`, commit `f320739b`; backend Release 3574/3574, seguridad y auth en reruns exact-SHA, Scope Lock SUCCESS | Proveedor V5CoverageMemoryAdapter + V5CoverageTenantProjectionService: guardar/leer/proyectar, EVIDENCE_NOT_VERIFIED, bloqueo cross-scope y rechazo de revisión vieja | In-memory solamente; no CAS/lease Google Sheets, EF/MySQL ni JWT HTTP. Jobs originales CANCELLED siguen sin PASS |
| M05.04 | `frontend/e2e/v5-plataforma-browser-guards.spec.ts`; commit `eced2da1`; Chromium/Angular/Scope Lock SUCCESS | UI de cobertura, vista vacía tipada, estados permitidos y 403, sin escrituras institucionales | Token y servidor simulados; no sesión JWT real |
| M05.05 | `backend/tests/Solqaryn.Tests/API/V5C0003M0505CoverageIsolationTests.cs`, blob `ad4a070f`, commit `1f04379d`; backend/seguridad/auth/Scope Lock SUCCESS | Rechazos de actor, scope, tenant y mappings ausentes o ajenos, sin filas filtradas; caso válido mantiene evidencia no verificada | Mapas de tenancy confiados son fixtures, no aislamiento HTTP/MySQL live |
| M05.06 | `frontend/e2e/v5-plataforma-browser-guards.spec.ts`, blob HEAD `72e10a1c`, commit `fe73c45f`; Chromium/frontend/Scope Lock SUCCESS | Chromium real para rutas, navegación, viewport 390/1280, teclado, tiempo runner y denegación sin token | No SLA productivo, JWT real ni APIs de plan/cola/claims |

## Dictamen positivo y negativos

**Suficiente** para los contratos acotados implementados: reglas puras, DTO de TestServer, proveedor/proyección en memoria, guard con datos de prueba confiados, UI Angular con tráfico interceptado. Las pruebas fueron genuinas en CI sobre sus SHA, pero esta auditoría documental las inspecciona; no las ejecuta nuevamente.

**Insuficiente** para certificar: CAS institucional Google Sheets, leasing entre procesos, JWT/BCrypt/RBAC servidor con credenciales reales, tenant A/B persistido, MySQL durable, trazabilidad de servicios externos, integridad live o despliegue. Un estado LISTO de snapshot no certifica la evidencia, y check SKIPPED/CANCELLED/IN_PROGRESS no equivale a PASS requerido.

## Clasificación, controles y reversión

Para **este diff documental**: `requiere_dotnet=NO_APLICA`; `requiere_node_angular=NO_APLICA`; `requiere_mysql=NO_APLICA`; `requiere_docker=NO_APLICA`; `requiere_e2e=NO_APLICA`. `requiere_security=SI` para impedir falsas garantías JWT/RBAC/tenant; `requiere_scope_lock=SI` para gobierno/procedencia.

A0: verificar fuentes, diff, hashes, CHANGELOG y secretos. A1: contrastar caso positivo y casos negativos por contrato. A2: conservar límites del aislamiento y autenticación. A3: seis prerrequisitos, correspondencia de pruebas y consumidores; no introducir implementación nueva. A4: después de publicar fast-forward a dev, verificar commit SHA, Scope Lock y CHANGELOG aplicables, rollback y PLAN/COLA/BITACORA. A5_PENDIENTE exige distinta Task Javier. Ninguna conclusión libera M06.06 ni QA/main/PROD sin sus propias puertas.

Rollback no destructivo: git revert ordinario del commit documental y revalidación de SHA; nunca borrar eventos BITACORA ni reescribir commits de compañeros.
