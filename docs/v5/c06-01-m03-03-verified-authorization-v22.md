# SOLQARYN V22 — C06-01.M03.03 autorización, tenancy y denegación por defecto

Rama dev; PROJECT_SCOPE_LOCK=STRICT. Dueño/autor DEV06. Riesgo R2. La acción exacta PLAN D880 es implementar autorización/tenancy/deny-by-default, no toda la fase M03 ni publicación.

## A0 — código y fronteras
El adaptador `C0601LandingVerifiedActorSource` valida el esquema JwtBearer real de ASP.NET, el actor autenticado, la ruta canónica `empresaId` y la empresa de origen verificada; obtiene y vuelve a validar membresía, rol empresarial activo y permiso relacional `Configuracion.Ver` mediante `IUsuarioScopeService`/Oracle EF. Rechaza JWT falso, rol borrado o revocado, actor suplantado, tenant B y principal sintético. El gate Application únicamente produce `READONLY_LANDING_AUTHORITY_PREFLIGHT_ONLY` sin conceder publicación, lectura privada, pago o emisión JWT. Configuración de servidor `LandingAuthorization:EnableReadOnlyActorProbe` OFF por defecto y DI solo en Development. Sin endpoint de negocio, DNS, datos ni secretos productivos.

## A1/A2 — positivos y negativos
Los tests unitarios prueban actor, membresía, rol/grant vigentes y denegación de host/roles del cliente, publicación, tenant ajeno, cancelación y proveedor no disponible. La prueba JWT real también verifica firma adulterada, actor distinto, ruta 0/alias/inyección, grants SQL revocados, rol eliminado, membresía ausente y aislamiento tenant A/B; no confundir éxito de mock con integración MySQL.

## A3 — MySQL 8.4 efímero y HTTP JWT genuino
Workflow `v22-c06-01-m03-03-jwt-mysql84-http.yml` levanta MySQL 8.4 loopback efímero, ejecuta TestServer ASP.NET Core con bearer firmado, Oracle EF contra DB aislada y revocación SQL; requiere archivo TRX >=1 test ejecutado y 0 failed/0 notExecuted. La ruta existe exclusivamente en el assembly xUnit, no en runtime. El workflow debe quedar COMPLETED SUCCESS exact-SHA y sus logs deben demostrar tests ejecutados; hasta entonces CI pendiente NO PASS.

## A4/A5 y limitaciones
Verificar Scope Lock, V5 procedencia, .NET 10 Release/xUnit, multiempresa y nueva prueba dirigida MySQL JWT HTTP exact-SHA; SKIPPED/CANCELLED/QUEUED/IN_PROGRESS requeridos NO PASS. A5 sólo por otra Task Javier con revisión genuina. M03.04 persistencia, M03.05 proveedores, M03.06 cierre, contenido legal, publicación, SEO, formularios y cualquier producción siguen sin certificar. Rollback por git revert normal, sin force push/QA/main/PROD.

## V22 DEV05 — reparacion causal de alias numerico de tenant (2026-11-21)

La revision cruzada detecto que `int.TryParse` permitia los alias numericos `empresaId=0N` y `empresaId=00N` del mismo tenant. La fuente autenticada exige ahora igualdad ordinal con su unica representacion decimal canonica. La regresion adversarial signed-JWT/HTTP/Oracle EF/MySQL 8.4 agrega dos 403 obligatorios sin perder el 200 positivo y el rechazo cruzado A/B. R2: solo preflight read-only DEV opt-in OFF, sin endpoint o datos nuevos de producto, cambios DB, grant ni publicacion. Requiere nueva CI exact-SHA, con H=VALIDANDO hasta SUCCESS causal; A5 por una tercera Task Javier distinta de DEV06 autor y DEV05 reparador. Rollback por git revert normal, sin QA/main/PROD.
