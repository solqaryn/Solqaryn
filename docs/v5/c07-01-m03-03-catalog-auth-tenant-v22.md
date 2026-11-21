# SOLQARYN V22 — C07-01.M03.03, autorización JWT/RBAC y tenancy

**Acción PLAN D1024:** Implementar autorización, tenancy y denegación por defecto. **Autor:** DEV01 JAVIER. **Riesgo:** R2. Rama dev; PROJECT_SCOPE_LOCK=STRICT. PLAN E «Completa M03» es meta acumulada de fase, no certificación total por esta .03.

## A0 — Implementación y alcance
La fuente API C0701CatalogVerifiedActorSource implementa IC0701VerifiedCatalogActorReader de M03.02. Exige Bearer registrado con firma/issuer/audience/vida válidos, un identificador firmado único, coherencia con usuario autenticado, UsuarioEmpresa y Rol empresarial vigentes desde MySQL, y grant actual RolPermiso para Productos.Editar con ámbito Empresa. Tenant de ruta es selector no autoritativo: headers, rol JWT y supuesto superadmin no conceden acceso. Tras esperas de I/O se revalidan membresía y grant. Errores DB/JWT niegan sin revelar datos.
El adaptador solo se registra en Development y se activa con configuración servidor CatalogAuthorization:EnableReadOnlyProbe, OFF por defecto. No se registra controlador productivo nuevo ni se crea autorización de escritura. M03.04 deberá implementar lector propio de producto real, ownership/version, transacción MySQL CAS, auditoría y endpoints; NO certificados aquí.

## A1–A3 — Pruebas causales
C0701M0303SignedJwtMysql84HttpIntegrationTests usa TestServer HTTP, Bearer firmado, Oracle EF Core con MySQL 8.4 efímero, filas auténticas de Usuario, Empresa, UsuarioEmpresa, Rol, RolPermiso y Permiso. Casos: aceptación A por grant, token ausente, firma alterada, B sin membresía, header X-Empresa-Id falso, rol JWT administrador falso y revocación MySQL efectiva con JWT todavía válido. La ruta de prueba solo existe en DLL de tests, jamás API productiva. CI dirigida GitHub-hosted exact SHA y TRX 0 fallos/omitidos. Sin ese job exitoso, A0–A4 no terminan.
No se certifica autorización HTTP productiva sobre producto concreto, imagen/variante/categoría, escrituras, persistencia de catálogo, ni integración Angular/Chromium. Estas pruebas pertenecen a fases futuras.

## A4, A5, rollback
Exigir FF/CAS dev, Scope Lock y procedencia V5 exact-SHA, build y xUnit .NET10, checks seguridad/autoridad y CI MySQL 8.4 dedicada con TRX PASS. SKIPPED/CANCELLED/FAILED/IN_PROGRESS no equivalen PASS. Mantener VALIDANDO si pruebas externas siguen pendientes. A5 revisor Javier distinto DEV01. Reversión solo git revert normal, preservar BITACORA. Prohibido QA/main/PROD, secretos y proveedores productivos.
