# SOLQARYN V22 — C04-02.M03.03 autorización, tenancy y denegación por defecto

Rama `dev`, PROJECT_SCOPE_LOCK=STRICT. Autor: DEV05, Task `6adc2e55f47c81919f6374e561687076`. Riesgo R2. Acción exacta PLAN D628: «Implementar autorización, tenancy y denegación por defecto». Este incremento **no** certifica toda la fase M03 ni reemplaza D01–D08.

## A0 — alcance, dependencias y arquitectura
`C0402ContractVerifiedActorSource` es un adaptador **real** al manejador registrado JwtBearer de ASP.NET Core y la membresía tenant actual vía `IUsuarioScopeService`. Requiere los grants empresariales relacionales `Facturacion.Ver` y `Configuracion.Ver`, rol activo del tenant y revalidación posterior a I/O de rol, permisos y membresía. La ruta `empresaId` es un selector canónico, no autoridad; nunca se acepta principal sintético ni un token sin verificación de firma. Cualquier ausencia, excepción o cambio de rol deniega sin exponer detalles. Los dos grants son solo condición para **lectura de preflight**, nunca permiso contractual de escritura.

El adaptador se registra únicamente en entorno **Development** y solo produce snapshots cuando el servidor habilita explícitamente `ContractAuthorization:EnableReadOnlyActorProbe=true` (default OFF); no se crea endpoint, almacenamiento de contratos, transacción EF, JWT emisor, acción comercial, cargo, refund, webhook o cambio de estado persistido. Consume el contrato Application M03.02 y las reglas Domain M03.01 sin dependencia Domain->API. No habilita producción, QA ni main.

## A1/A2 — negativas adversariales
`C0402M0303ContractVerifiedActorTests` valida JWT real firmado contra `JwtBearer` de Microsoft, actor diferente, firma inválida, ruta tenant 0/alias 0100/tenant B/inyección, grants faltantes, rol o membresía ajenos, revocación de grant SQL-model, default OFF y cancelación. EF InMemory utilizado exclusivamente por estas pruebas de lógica de permisos **NO** se representa como prueba de MySQL 8.4.

## A3 — integración y límites pendientes
El nuevo código consulta el modelo EF real de `Rol`, `RolPermiso`, `Permiso` y la membresía actual. Para certificación completa R2 de su uso con Oracle EF/MySQL se requiere prueba dirigida MySQL 8.4 efímera en GitHub-hosted, JWT HTTP firmado y revocación actual contra ese motor, con resultado real PASS. No se certifica esa integración sin evidencia. El futuro adaptador de contexto contractual persistido debe revalidar propiedad, versión y políticas comerciales en la misma transacción CAS antes de cualquier efecto.

## A4/A5 y reversión
Inspeccionar `verify-solqaryn-project-scope`, build Release, xUnit, seguridad multiempresa y V5 exact-SHA de GitHub Actions. Mientras alguna prueba causal requerida siga QUEUED/IN_PROGRESS/FAIL, dejar PLAN en VALIDANDO. A5 únicamente por otra Task Javier distinta de DEV05. Reversión mediante `git revert` normal + CI, nunca force-push. BITACORA append-only, COLA/DAG formulas, sin secreto/productivo/pago.

## A3 — incremento nuevo de integración real GitHub-hosted

Se añade `C0402M0303SignedJwtMysql84HttpIntegrationTests` (Category=Integration) con `TestServer` real de ASP.NET Core/JwtBearer, firma HMAC de prueba, `UsuarioScopeService` real, Oracle EF Core y MySQL 8.4 efímero con esquema aislado y cleanup. La ruta del controlador existe **solo en el ensamblado de pruebas**, nunca se registra en el backend DEV/QA/PROD. Prueba de forma causal: anónimo 401, JWT manipulado 401, empresa A 200 solo preflight, empresa B 403 y revocación de permiso `Configuracion.Ver` 403. El permiso de facturación y configuración es requisito de *lectura* y nunca concede mutación o acceso contractual privado.

Workflow dedicado `.github/workflows/v22-c04-02-m03-03-jwt-mysql84-http.yml`: GitHub-hosted .NET10, imagen `mysql:8.4`, comprobación de `SELECT VERSION()`, scope lock exact SHA y TRX con 1 o más casos, cero fallos/omitidos. El resultado se acredita solamente tras revisar job completed/success, TRX y fuente/blob del mismo SHA; mientras esté queued/in_progress es **WAIT_CI** no PASS. Se mantiene sin secretos productivos, cobro, despliegue ni aprobación A5 propia.
