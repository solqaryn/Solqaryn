# SOLQARYN — P0: aislamiento empresarial de datos comerciales

**Estado 2026-10-13: contención operativa desplegada en Render DEV. Candidato expand introduce `EmpresaId?` e índices/FK en cinco raíces, con migración Oracle; los registros históricos continúan sin atribución y NO existe aislamiento multiempresa certificado.**

## Diagnóstico exacto en el HEAD de DEV

- En el baseline anterior, `Producto`, `Cliente`, `Proveedor`, `Compra` y `Venta` carecían de `EmpresaId`; el candidato introduce propiedad nullable, índice y FK, pero **no añade aún filtros SQL ni ownership atribuido**. Sus consultas por ID/listas aún requieren aislamiento empresarial explícito.
- Las categorías, tipos de cliente, marcas/modelos, variantes, existencias, facturas, pedidos, pagos, descuentos, movimientos y documentos derivados pueden consumir referencias de esas entidades. Proteger sólo controladores o `X-Empresa-Id` no aísla el grafo.
- `UsuarioEmpresa`, `IUsuarioScopeService` y `RequierePermiso` validan membresía/rol en el backend, pero **no demuestran ownership de cada fila consultada**.
- El catálogo público `/tienda` es anónimo y actualmente carece de una partición de datos comercialmente independiente por tenant. La clave de caché no constituye un filtro SQL.
- No se afirma que haya ocurrido una divulgación histórica de datos; el defecto comprobado es que la separación por empresa no está garantizada.

## Expansión no destructiva de esquema (candidato)

- La migración `20261013121500_CommercialTenantRootExpand` añade las cinco columnas nullable, índices y claves foráneas restrictivas. No atribuye filas históricas ni levanta la cuarentena.
- La prueba `.github/workflows/tenant-commercial-root-expand-ephemeral.yml` debe restaurar el caso de esquema legacy en MySQL descartable, ejecutar la migración y demostrar cinco columnas/FK/índices, conservación de filas y reinvocación idempotente.
- Respaldo previo real DEV con restore same-artifact comprobado: Actions [#38399423450](https://github.com/solqaryn/Solqaryn/actions/runs/38399423450). No significa backfill ni certificación multiempresa.

## Contención de emergencia — sin atribución de datos

`UnscopedCommercialTenantGuardMiddleware` se ejecuta en la API **antes de los controladores** y revisa, sin caché, hasta dos registros de `Empresas`, incluso empresas inactivas:

1. **Una empresa existente**: conserva la operación habitual (con JWT, RBAC y filtros preexistentes). No modifica datos comerciales ni inventa una asociación tenant.
2. **Cero o dos o más empresas**: bloquea todos los endpoints de datos de la API, incluidos públicos, con HTTP 503 y `TENANT_DATA_ISOLATION_REQUIRED`, sin ejecutar los controladores ni retornar datos de negocio.
3. **Nuevas empresas**: `POST /empresas` queda bloqueado. También `EmpresaService.CreateAsync` rechaza una segunda empresa desde otros consumidores del servicio; por tanto, no basta con cambiar una ruta HTTP para eludir el bloqueo.
4. Solo permanecen disponibles: `GET /health`, `GET /health/ready`, `POST /auth/login`, `POST /auth/renovar`, `GET /tenant-context/:id` (con su autenticación y verificación existentes), y `OPTIONS` para negociación CORS. Ninguna excepción habilita lectura de productos o documentos.
5. Ante error al consultar `Empresas`, el request falla antes de los controladores. El gate no registra ni devuelve registros, JWT, credenciales ni atributos de empresas.

**Consecuencia de seguridad deliberada:** si DEV ya tiene más de una empresa registrada, las funciones comerciales quedarán temporalmente inaccesibles al desplegar. Es **una cuarentena P0**, no una implementación terminada de multiempresa ni una solución aceptable como producto final. No fusionar con QA/PROD mientras siga esta deuda.

## Trabajo imprescindible para levantar la cuarentena y declarar cerrado el P0

- Inventariar **todas** las tablas tenant-owned (maestros, catálogos, variantes, inventario, almacenes, cotizaciones, pedidos, compras, ventas, facturación, pagos, contabilidad, logística, imágenes, exportaciones, búsquedas, caché y jobs) y sus relaciones y referencias externas; distinguir explícitamente datos globales de datos de empresa.
- Añadir ownership `EmpresaId` durable y `NOT NULL` para datos nuevos, con FK hacia `Empresas`, índices y unicidades compuestas por empresa; en descendientes con ownership derivado, imponer FK/consistencia de pertenencia transaccional al ancestro.
- Auditar registros legacy y elaborar un **mapa de atribución con evidencia** desde información de negocio fiable, no por usuario, primer tenant, `MIN(EmpresaId)`, encabezado HTTP ni una coincidencia arbitraria. Los datos ambiguos quedan en cuarentena, nunca visibles a cualquier empresa.
- Proteger lecturas (listas, detalles, agregados, reportes, búsquedas y exports), escrituras (POST/PUT/PATCH/DELETE), asociaciones por ID, borrado lógico, `ExecuteUpdate`, `FromSql`, `IgnoreQueryFilters`, streaming, archivos, jobs, webhooks, caché y el storefront **también sin JWT**. El tenant válido procede del servidor a partir de membresía activa y binding público de dominio/storefront verificable; nunca de un `EmpresaId` suministrado libremente.
- Guardar y restaurar backup DEV **antes** de migrar o reparar filas; ensayar migración Oracle EF Core 10/MySQL 8.4.8 en base efímera, validar `has-pending-model-changes=0`, FKs y conteo de huérfanos/filas sin dueño. No introducir un nuevo provider ni actualizar Aiven.
- Ejecutar pruebas multiempresa **A/B** reales: mismas claves SKU/RTN/folios permitidas por empresa; todos los CRUD; referencias ajenas vía URL, filtros, DTOs, cargas masivas y acceso directo; usuario con acceso a dos empresas que cambia de tenant; RBAC; exportación de PDF/Excel/ZIP; facturas y datos financieros; consulta anónima por dominios; jobs y caché. Rechazo 403/404 sin filtración de existencia.
- Certificar exact-head DEV (backend, integración MySQL, migraciones, frontend, Playwright, auditoría, scope gate y recuperación de backup). **Solo entonces** retirar la cuarentena y considerar DEV listo. La promoción QA → PROD requiere decisión separada y permanece aplazada.

## Criterio de cierre inequívoco

**NO CERRADO** hasta que dos empresas simultáneas puedan usar productos, clientes, proveedores y operaciones independientes sobre una misma base, sin mezcla, sin bloqueos de cuarentena, y existan evidencia de migración, ownership, pruebas de seguridad y despliegue DEV real. CI verde de esta contención no equivale a certificación multiempresa.

### Alcance y reversibilidad

Cambio exclusivo en código `dev`, sin alterar esquema, migraciones, filas de Aiven, secretos, Cloudinary, Render, Vercel, QA ni PROD. Reversión por revert del PR en `dev` tras analizar la seguridad: **no** retirar el guard de producción si los datos no tienen ownership certificado.
