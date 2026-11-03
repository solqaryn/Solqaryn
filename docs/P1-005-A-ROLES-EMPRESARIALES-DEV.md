# SOLQARYN — P1-005.A | Administradores empresariales

Fecha de implementación: 2026-11-02 (Honduras). Plan comercial vigente, celda `PLAN_MAESTRO!A34`. Rama reglamentaria `work/solqaryn-p1-005-a`, base `dev@cd842256e098e4c5c9b2d46889541a111379507f`.

## Causa y límites

Los controladores existentes de `/usuarios` y `/roles` tenían permisos en MVC y filtros EF, pero la cuarentena multiempresa impedía su funcionamiento con 2+ empresas; al mismo tiempo, la creación y edición de `Usuario.RolId` (identidad compartida) aceptaban rol por ID sin verificación explícita del propietario empresarial en esa ruta. Un administrador no puede recibir acceso global ni a datos de otra empresa por este cambio.

Se habilita solo una allowlist exacta de acciones de administración (GET/POST/PUT de usuarios y roles, membresías, matriz de permisos), con JWT real, membresía tenant activa, `EsAdministrador` desde MySQL, permisos vigentes y marca `VerifiedCommercialTenant` establecida únicamente tras RBAC. No se habilitan rutas de contabilidad, facturación, inventario, eliminación, activaciones, catálogo global de permisos ni plataforma. Se preservan filtros EF; no se confía en `X-Empresa-Id` como permiso.

### Contrato de operaciones

- Alta empresarial: `IUsuarioAdministracionEmpresaService.CrearAsync` exige permisos `Usuarios:Crear` y `Usuarios:AsignarRol`, admin de empresa vigente, rol de empresa asignable propio o plantilla de sistema. Duplicados globales no se exponen; alta de `Usuarios`, primera `UsuarioEmpresas` y auditoría crítica se realizan en una misma transacción MySQL. Una falla revierte la transacción completa.
- Concurrencia/TOCTOU: creación y edición hacen la lectura del rol administrador activo, los permisos, los propietarios y la escritura dentro de una transacción MySQL `IsolationLevel.Serializable`; cambios concurrentes incompatibles fallan/cancelan de forma segura. La certificación LIVE B debe comprobar revocación y consistencia.
- Cambio de rol: `Usuario.RolId` (compatibilidad) y `UsuarioEmpresa.RolId` (rol RBAC efectivo) se actualizan en la misma transacción y se registran en auditoría estricta; la vista no puede mostrar un rol diferente al autorizado.
- Edición empresarial: solo membresía activa y exclusiva de esa empresa. Identidades con otra membresía (incluso inactiva), asignación de Plataforma o rol global heredado son inmutables desde tenant. Rol destino debe ser propio o plantilla empresarial válida; cambio de rol y contraseña requieren permisos explícitos adicionales, revalidados desde el servicio. Auditoría estricta transaccional.
- Roles: los controladores existentes conservan propiedad, plantillas inmutables, filtros y auditoría. La matriz limita el catálogo a `Ambito=Empresa` y prohíbe conceder permisos globales a un rol tenant.
- Comportamiento denegado: cuentas compartidas A/B, rol ajeno, global, huérfano, sin admin vivo, subrutas vecinas, métodos inesperados, tamaños JSON excesivos y acceso anónimo.

## Corrección E2E de fixture de alta (2026-11-02)

La aceptación Fase 6 (`frontend/e2e/fase6-reportes-administrativos.spec.ts`) fallaba en el `beforeAll` porque creaba al usuario con `POST /usuarios` y repetía `POST /usuarios/{id}/empresas` para la misma empresa. Desde P1-005.A el primero ya crea `UsuarioEmpresa` en la misma transacción; el 400 `El usuario ya tiene una membresía registrada` era el rechazo correcto, no una falla de persistencia. El fixture ahora lee `GET /usuarios/{id}/empresas` y comprueba que existe exactamente una membresía activa de la empresa y rol esperados. Se conservan la regla de unicidad, las restricciones A/B, JWT/RBAC, las pruebas negativas y la integración de auditoría. No hay migraciones ni cambios a producción. Scope Lock requiere nuevas CI y revisión independientes exact-HEAD después del commit; la falla de Scope Lock anterior fue consecuencia de la E2E fallida.

## Matriz de impacto

- API: `UsuariosController`, `UnscopedCommercialTenantGuardMiddleware`, registro DI de `Program`.
- Application: interfaz nueva `IUsuarioAdministracionEmpresaService`, ajuste a `PermisoService`.
- Infrastructure: servicio tenant-scoped que reutiliza los flujos existentes `UsuarioService`, `UsuarioEmpresa`, `IAuditoriaService` y EF/MySQL.
- Domain: sin cambios.
- Datos/migraciones: ninguna migración ni escritura manual de datos DEV. Una vez fusionado, las operaciones autorizadas de la API escribirán la primera membresía en transacción y registrarán auditoría.
- Security: JWT, RBAC relacional, EF filters, no plataforma, no cross-tenant, no elevación por header ni JWT informativo.
- Frontend: proyección de capacidades UI administrativas ya existentes solo con `EsAdministrador` y cada grant vigente. Rutas Angular de usuarios/roles/matriz visibles cuando están autorizadas; ninguna autoridad nace de UI ni se agregan componentes.
- CI: xUnit dirigido para positivos/negativos y suite permanente backend/seguridad.
- Rollback: revert protegido del PR y mantener la cuarentena multiempresa (sin borrados o DML manual).

## Criterio A/B de cierre

**P1-005.A** requiere revisión independiente del SHA final, Scope Lock y tests dirigidos, merge protegido, readback de `dev`, y conciliación Sheets. A no se declara LISTO por el PR abierto.

**P1-005.B** es separada: tras A integrada y publicada en Render DEV, ejecutar prueba LIVE autenticada (HTTP/JWT/MySQL): admin A permite alta/edición/rol propio; rol B, usuario B, usuario compartido A/B y autoelevación global rechazan; confirmar atomicidad, membresía y auditoría MySQL durable sin secretos; limpieza de fixtures; Scope Lock, revisión y readback institucional. Ningún test InMemory sustituye B. Mantener `PENDIENTE` hasta integración A.
