# SOLQARYN — P0: aislamiento empresarial de datos comerciales

### Actualización P0 #3 — PR #3599 (13 octubre 2026; candidato, NO fusionado)

Se añadieron unicidades comerciales por EmpresaId e índices compuestos de cinco raíces. Variantes, imágenes, detalles de compra y venta y facturas reciben EmpresaId nullable derivado/validado, además de doce claves foráneas compuestas en MySQL. Se incorporó migración Oracle EF10 y contratos físicos de bootstrap. Esto es un tramo de la solución, NO su certificación: quedan entidades comerciales descendientes por clasificar/proteger, filtros de lectura y validación MySQL exact-head. No levantar cuarentena de segunda empresa, no tocar QA ni PROD y no reasignar los 19 registros descartables.


**Estado 2026-10-13: contención operativa desplegada en Render DEV. Candidato expand introduce `EmpresaId?` e índices/FK en cinco raíces, con migración Oracle; los registros históricos continúan sin atribución y NO existe aislamiento multiempresa certificado.**

## Decisión del propietario — datos de prueba DEV, sin backfill (2026-10-13)

Los 19 registros sin EmpresaId (8 productos, 2 clientes, 2 proveedores, 3 compras y 4 ventas) son pruebas descartables. No se atribuirán a ninguna empresa ni es necesario reconstruir su dueño histórico. La auditoría permanece como evidencia, no como requisito para avanzar.

El borrado comercial NO se ha ejecutado: la limpieza futura solo en DEV deberá proteger usuarios, contraseñas/hashes, roles, permisos, membresías UsuarioEmpresa y configuración necesaria para login; inventariar 132 entidades/66 descendientes, hacer backup cifrado fresco, restaurar en MySQL descartable, ensayar purga referencial transaccional y verificar resultados. Véase docs/tenancy/LIMPIEZA_DATOS_PRUEBA_DEV.md. No eliminar Cloudinary ni otro activo por este acuerdo.

Lo obligatorio para habilitar empresas FUTURAS es aislamiento A/B de todas las lecturas, escrituras, relaciones, facturas, inventarios, archivos, trabajos y la tienda pública. La cuarentena para 2+ empresas sigue activa y QA/PROD no se modifican.

## 2026-10-13 — Evidencia de ejecución real (DEV, lectura y control sin atribución)

- **Esquema aplicado y verificado sobre Aiven DEV:** [run #38406239720](https://github.com/solqaryn/Solqaryn/actions/runs/38406239720). Preflight `already_expanded`; cinco columnas `EmpresaId?`, cinco FKs e índices y marca Oracle EF presentes. El paso DDL se omitió por idempotencia; no se duplicó la migración.
- **Registros preservados:** 8 productos, 2 clientes, 2 proveedores, 3 compras y 4 ventas; **19/19 mantienen `EmpresaId=NULL`**.
- **Procedencia limitada:** [run #38409321128](https://github.com/solqaryn/Solqaryn/actions/runs/38409321128) verificó que cada registro cuenta con autor y que no hay autores sin membresía activa o con múltiples membresías activas **actuales**. Los 14 enlaces comerciales revisados tienen owner incompleto. Esto **no demuestra** propiedad empresarial histórica.
- **Modelo relacional:** inventario de 132 entidades EF, 20 con `EmpresaId` y 66 entidades adicionales alcanzables desde las raíces comerciales en `docs/tenancy/INVENTARIO_GRAFO_COMERCIAL_P0_DEV.md`. No equivale a auditoría de todas las tablas físicas, endpoints, tareas o integraciones.
- **Candidatos de código pendientes:** #3594 propaga owner verificado en nuevas altas administrativas; #3596 rechaza enlaces empresariales cruzados y cambios ordinarios de owner. No activar multiempresa ni cerrar el P0 sin integración, pruebas A/B y saneamiento de procedencia.

## Corrección de grafos comerciales nuevos en una misma unidad de trabajo

El guard comercial también diferencia un registro ya persistido de un padre o referencia que EF está insertando en el mismo `SaveChangesAsync`. Solo acepta un owner del ChangeTracker cuando esa entidad está `Added`; para referencias ya persistidas consulta el owner de la base de datos sin confiar en navegaciones. Esto permite compras, ventas y clientes nuevos de la misma empresa sin relajar el rechazo A/B y conserva en cuarentena las filas legacy `EmpresaId=NULL`. Cobertura inicial de cinco pruebas dirigidas; requiere CI exact-head.

## Comprobación post-RBAC para escrituras con dueño explícito (candidato PR #3596)

En solicitudes donde `RequierePermiso` estableció la marca segura de empresa, `AppDbContext` transmite ese tenant al guard de escritura: no es lícito crear, modificar o eliminar `Producto`, `Cliente`, `Proveedor`, `Compra` o `Venta` de un propietario explícito diferente, aunque no se modifique el valor `EmpresaId`. La cabecera `X-Empresa-Id` no concede esta autoridad. Los datos históricos sin owner conocido mantienen cuarentena; una petición sin marca verificada no se considera habilitada para multiempresa. Esta defensa no cubre todas las consultas, el catálogo público, los demás descendientes ni las rutas de SQL directo.

## Contención adicional candidata en PR #3596 (aún no equivale a multiempresa)

La validación previa a guardar ahora alcanza ambos overloads async de EF y bloquea las rutas síncronas con mutación comercial o escritura relacional; consulta el owner persistido para impedir `Update` desconectado con `EmpresaId` suplantado, contrasta FKs guardados (no navegaciones cambiantes), rechaza enlaces de owner conocido hacia filas históricas sin dueño y verifica que la variante del detalle pertenezca al producto. Hay pruebas de regresión nuevas; verificar CI exact-head antes de integrar. `ExecuteUpdate`, SQL directo, filtros de todas las lecturas, derivados, tienda anónima, procedencia histórica y migración permanecen como P0. No quitar el middleware preventivo.

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

## Auditoría tenant-aware de nuevas operaciones (candidato)

La nueva versión de `AuditoriaService` marca eventos futuros con la empresa comprobada por RBAC; no atribuye las auditorías existentes ni prueba su titularidad. Las consultas de auditoría siguen necesitando filtros de empresa antes de permitir 2+ tenants.

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
- Para los 19 registros de prueba de DEV, no hacer backfill histórico: ejecutar solo la limpieza de fixtures comerciales cuando su alcance y dependencias estén inventariados y protegidos, con backup/restore comprobado y usuarios intactos. Los futuros datos reales deben tener owner server-side obligatorio desde su creación; ningún registro sin tenant puede exponerse a otra empresa.
- Proteger lecturas (listas, detalles, agregados, reportes, búsquedas y exports), escrituras (POST/PUT/PATCH/DELETE), asociaciones por ID, borrado lógico, `ExecuteUpdate`, `FromSql`, `IgnoreQueryFilters`, streaming, archivos, jobs, webhooks, caché y el storefront **también sin JWT**. El tenant válido procede del servidor a partir de membresía activa y binding público de dominio/storefront verificable; nunca de un `EmpresaId` suministrado libremente.
- Guardar y restaurar backup DEV **antes** de migrar o reparar filas; ensayar migración Oracle EF Core 10/MySQL 8.4.8 en base efímera, validar `has-pending-model-changes=0`, FKs y conteo de huérfanos/filas sin dueño. No introducir un nuevo provider ni actualizar Aiven.
- Ejecutar pruebas multiempresa **A/B** reales: mismas claves SKU/RTN/folios permitidas por empresa; todos los CRUD; referencias ajenas vía URL, filtros, DTOs, cargas masivas y acceso directo; usuario con acceso a dos empresas que cambia de tenant; RBAC; exportación de PDF/Excel/ZIP; facturas y datos financieros; consulta anónima por dominios; jobs y caché. Rechazo 403/404 sin filtración de existencia.
- Certificar exact-head DEV (backend, integración MySQL, migraciones, frontend, Playwright, auditoría, scope gate y recuperación de backup). **Solo entonces** retirar la cuarentena y considerar DEV listo. La promoción QA → PROD requiere decisión separada y permanece aplazada.

## Criterio de cierre inequívoco

**NO CERRADO** hasta que dos empresas simultáneas puedan usar productos, clientes, proveedores y operaciones independientes sobre una misma base, sin mezcla, sin bloqueos de cuarentena, y existan evidencia de migración, ownership, pruebas de seguridad y despliegue DEV real. CI verde de esta contención no equivale a certificación multiempresa.

### Alcance y reversibilidad

Cambio exclusivo en código `dev`, sin alterar esquema, migraciones, filas de Aiven, secretos, Cloudinary, Render, Vercel, QA ni PROD. Reversión por revert del PR en `dev` tras analizar la seguridad: **no** retirar el guard de producción si los datos no tienen ownership certificado.
