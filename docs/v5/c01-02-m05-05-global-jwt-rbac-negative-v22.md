# C01-02.M05.05 — Negativos JWT/RBAC de catálogo privado global (V22)

## A0 — unidad y frontera
ID existente C01-02.M05.05, PLAN D: prueba negativa de seguridad y aislamiento; criterio global de no exponer el catálogo privado a anónimo ni administrador empresarial. Riesgo R2. Cambio estrictamente test-only en backend .NET 10; no altera controllers, DI de producción, JWT secrets, EF migrations, API pública, UI, pagos ni datos de proveedores. La ruta actual verificada es GET /plataforma/gestion/roles?empresaId=..., protegida por [Authorize] y por PlataformaScopeService con grant de plataforma vivo. NO se crea ni afirma la disponibilidad del futuro catálogo completo de QA global.

## A1 y A2 — evidencia que debe ejecutar GitHub-hosted
Suite C0102M0505GlobalJwtRbacNegativeTests: TestServer ASP.NET MVC y JwtBearer criptográfico (issuer, audience, vigencia, llave). El anónimo recibe 401, el administrador empresarial con JWT válido y claims falsos de privilegio de plataforma recibe 403, una firma ajena recibe 401, JWT caducado recibe 401. La autoridad global vigente puede consultar solo el catálogo del tenant A (nunca los roles exclusivos del tenant B), y el mismo JWT válido se vuelve 403 al revocar su UsuarioRolPlataforma en el contexto de datos. No se debe insertar auditoría para peticiones rechazadas; sí una para la lectura autorizada. Datos/firmas son efímeros de test.

## A3 — alcance y limitación explícita
Pipeline real de autenticación HTTP y servicio de autoridad real, pero EF Core InMemory aislado. NO equivale a MySQL 8.4 físico, Oracle EF productivo, BOLA de archivos o jobs, rutas no registradas, ni navegación Angular/Chromium. Ningún mock de autorización crea autorización de runtime. Las integraciones posteriores de la fase M05 no quedan certificadas por esta unidad test-only.

## A4 — condiciones de cierre
Ejecutar en GitHub Actions SHA causal exacto: .NET 10 Backend Release y SOLQARYN backend xUnit con 0 FAIL/0 SKIP, seguridad multiempresa, Authorization/files/tenant, Scope Lock verify-solqaryn-project-scope, V5 procedencia y demás checks pertinentes completados SUCCESS. QUEUED/IN_PROGRESS/FAIL/SKIPPED no son PASS. Transición EN_PROGRESO→VALIDANDO es checkpoint de entrega, NO LISTO. La meta acumulativa de M05 y otras unidades siguen independientes.

## A5 y reversión
Revisión A5 real por otra Task Javier distinta de DEV06 tras A0–A4. Revisión debe inspeccionar autenticidad JWT, grant/rol server-side, negativos tenant y revocación. Rollback sin efecto comercial: git revert del test y este documento preservando commits ajenos; nunca borrar BITACORA histórica ni manipular fórmulas DAG/COLA. Cero QA/main/PROD, credenciales y pagos reales.
