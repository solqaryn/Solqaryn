## 2026-10-13 — Ajuste de compatibilidad tenant: roles plantilla y fixture E2E

- Los grants de roles empresariales plantilla (Rol.EmpresaId nulo, Ambito Empresa) conservan propietario nulo y no reciben EmpresaId arbitrario. Los grants de roles empresariales personalizados mantienen validación A/B; los roles de plataforma no se modifican bajo una marca tenant.
- El fixture MySQL descartable E2E asigna el tipo inicial SIN_CLASIFICAR al único tenant de prueba antes de insertar clientes; no se atribuyen datos históricos reales ni se relajan FKs de clientes/tipos.
- Pruebas dirigidas para roles plantilla, roles ajenos y autoridad de plataforma. QA y PROD sin cambios.

# SOLQARYN — Propiedad física del modelo comercial

La expansión del esquema asigna `EmpresaId` (nullable sólo para registros históricos descartables) a los objetos de negocio. Todo registro futuro asociado con una empresa debe tener propiedad asignada desde contexto verificado en backend, nunca desde cabecera o DTO del cliente.

**Pertenencia empresarial:** 42 módulos complementarios: inventarios y almacenes, catálogos personalizados, cajas/contabilidad/finanzas, configuraciones comerciales, descuentos/impuestos, documentos públicos de factura, importaciones, y asignaciones `RolPermisos`. Las 46 tablas derivadas y raíces ya incorporadas en PR #3599–#3603 se preservan.

**Compartidos sólo por diseño de plataforma:** `Empresas`, `Usuarios`, `Permisos`, `Planes`, `PlanModulos`, `UsuarioRolesPlataforma`, `Bancos` y `MetodosPago`. El usuario pertenece a empresas mediante `UsuarioEmpresas`, que ya tiene `EmpresaId`; los roles empresariales y sus permisos sí están aislados por empresa.

La migración conserva historiales sin atribución y agrega índices de identidad y claves naturales por `EmpresaId`, junto con FKs compuestas `(EmpresaId, ReferenciaId)` hacia `(EmpresaId, Id)`. El token público de factura conserva unicidad global porque se resuelve sin una clave de empresa en la URL.

**Límites del cierre estructural:** no equivale a certificar todavía todos los filtros de lectura, endpoints, procesos batch y cache, ni habilitar dos empresas simultáneas. La cuarentena sigue activa hasta completar los controles de acceso restantes.

**Validación para cierre:** compilación exact-head, `dotnet ef migrations has-pending-model-changes`, migración desde fixture legacy MySQL 8.4, `INFORMATION_SCHEMA` de índices/FKs, pruebas de aislamiento A/B y adopción fresh de `__EFMigrationsHistory`.
