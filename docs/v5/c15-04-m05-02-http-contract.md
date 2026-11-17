# C15-04.M05.02 — contrato HTTP/DTO y errores seguros (V19)

Autor DEV05 Javier, TASK_ID 6adc2e55f47c81919f6374e561687076, repo solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT. PLAN fila 2295 y candado V17 2000002295. Riesgo R2 conservador por contrato de autorización de plataforma.

A0: cambio test-only de Angular 22 con seis Vitest, workflow GitHub-hosted, evidencia y CHANGELOG; servicio real PlataformaSesionService sin modificaciones. Sin backend .NET, API real, JWT, RBAC, tenant MySQL, EF, DTO de servidor ni cambios de permisos o migraciones. Rollback git revert normal.

A1: contrato positivo GET /plataforma/sesion con respuesta DTO válida y señal autorizada; 200 Empresa y 200 inválidos (sin data, rol vacío, success=false, esAdministrador=false) no autorizan; 401 tras grant previo lo borra inmediatamente; 403 no conserva autoridad; 503 no autoriza ni reintenta. Seis tests con HttpClient real Angular conectado a HttpTestingController de inyección: asserts falsables, fronteras y errores. No se inventa cobertura servidor.

A2: se verifica solo frontera cliente y que sesión local se borra. El HttpErrorResponse puede transportar datos del servidor a consumidores y NO se certifica sanitización universal de errores del backend; ocultamiento de mensajes visuales depende de la vista y sus propias pruebas. JWT firmado/roles reales/aislamiento empresa A/B son NO_CERTIFICADOS por mocks HTTP.

A3: Vitest Angular TestBed + HttpClientTesting real en GitHub-hosted Node24/npm11, lint y build. HTTP servidor verdadero, MySQL, redes externas, EF y proveedor Aiven NO_APLICA al diff test-only. No hay productos o datos reales.

A4: para LISTO exigir 6/6 tests, lint/build, Scope Lock, V5/procedencia, checks requeridos de seguridad/frontend del SHA exacto o descendiente causal equivalente, readbacks PLAN/COLA/DAG/BITACORA. QUEUED, IN_PROGRESS, SKIPPED, CANCELLED, FAILED nunca PASS. VALIDANDO con CI pendiente.

A5 requiere Task Javier distinta de DEV05, sin QA/main/PROD y sin autoaprobación.
