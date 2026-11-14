# SOLQARYN V5 — C11-01.M01.03: inventario causal de código y evidencia relacional

**Autor:** PRIMARY DEV 06 / JAVIER_CHATGPT / Task 6af16bbad26881918c283abe06e1f32a. **Microtarea:** C11-01.M01.03, PLAN_MAESTRO fila 1588; fuente STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA. **DAG:** VALIDADO/SI para preparación M01. **Repo:** solqaryn/Solqaryn, dev, PROJECT_SCOPE_LOCK=STRICT. **Fecha:** 2026-11-14.

## Pregunta y clasificación de la evidencia

Esta microtarea lee contratos y código disponibles para sustentar el futuro modelo relacional normalizado: claves primarias y foráneas, índices, ownership tenant, nulabilidad histórica, coherencia transaccional y compatibilidad MySQL. No diseña ni aplica migraciones, no certifica el estado físico de Aiven y no constituye una aprobación de implementación.

El HEAD de dev confirma .NET 10, EF Core 10.0.12 con proveedor Oracle MySql.EntityFrameworkCore 10.0.9, Domain <- Application <- Infrastructure y API. Este inventario registra **archivos y blobs de Git**, no ejecución de sus métodos, existencia de índices en un servidor ni cobertura de CI. La publicación de M01.01 y M01.02 documenta objetivo/actores, sin conceder autoridad SQL. El gate documental de M01.03 no abre M01.06, M02, M03, QA/main/PROD ni modifica el producto.

## Inventario verificable de 15 fuentes originales

| ID | Ruta en dev y SHA blob | Hecho verificable en código | Limitación o comprobación pendiente |
| --- | --- | --- | --- |
| S01 | backend/src/Domain/Entities/Empresa.cs — c44940df7c30983a4c23b93338e0cb9367962069 | Raíz Empresa con estado activo/inactivo | No inventar soft-delete ni certificar tabla física |
| S02 | backend/src/Domain/Entities/Cliente.cs — 8c6d4c4453e44f8c0be9475340465e1aadb3f787 | EmpresaId nullable, compatibilidad de históricos | No atribuir ownership a filas legacy por inferencia |
| S03 | backend/src/Domain/Entities/Sucursal.cs — 8746b11fa30cdac0b97241b6221100bedd813b35 | EmpresaId nullable y validación fail-closed de propietario | Auditar históricos y mapeos antes de endurecer NOT NULL |
| S04 | backend/src/Domain/Entities/UsuarioEmpresa.cs — f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c | Membresía empresarial con rol scoped, distinta de Usuario.RolId legacy | Verificar índices efectivos y controles en consumidores |
| S05 | backend/src/Infrastructure/Persistence/AppDbContext.cs — c4b32f6637f1b4426655bb0cd12af8dd9d229866 | DbContext con selección de contexto tenant verificado y soporte solo lectura | No inferir que todas las consultas o rutas aplican filtros |
| S06 | backend/src/Infrastructure/Persistence/CommercialTenantReadFilterModel.cs — f2422beec8f2d054c20184d073bad668513fa53b | Defensa EF de lectura para entidades con EmpresaId y preservación de filtros | Documenta explícitamente cobertura incompleta de jobs, HTTP público y bypass fuera de EF |
| S07 | backend/src/Infrastructure/Persistence/CommercialTenantPhysicalConstraints.cs — d021861d357de201049fece1b6c63b2c9c218250 | SQL canónico de FK compuestas EmpresaId + referencia comercial | La presencia de DDL en Git no acredita su aplicación |
| S08 | backend/src/Infrastructure/Persistence/CommercialTenantOwnershipGraphPhysicalConstraints.cs — 1d33d4d0ad321ac6f891e85064e2f2900a980f1b | Grafo de FK tenant-aware para descendientes comerciales; cuarentena de NULL antiguos | Auditar huerfanos, FK aplicadas y reglas ON DELETE en MySQL efímero |
| S09 | backend/src/Infrastructure/Persistence/CommercialTenantSalesPhysicalConstraints.cs — 31c3d9951f02a27878226e42e80a432b6ad114ce | FK compuestas de cotización, pedido, cliente y detalles | Probar operación de venta/factura transaccional; no asumirla por DDL |
| S10 | backend/src/Infrastructure/Persistence/CommercialTenantNaturalKeysPhysicalContract.cs — 7e93ab363c4e5f3e7639c4b796b4d96315ed1917 | Contrato VerifyAsync para 13 claves naturales empresariales y 13 índices legados | No declarar 13/13 PASS sin ejecución física de verificación |
| S11 | backend/src/Infrastructure/Persistence/CanonicalMySqlPhysicalContract.cs — f590f85fa5e372051ef3d2a3fab9552f6e7c8cbb | Complementos físicos de bootstrap fresco y verificación canónica | Un bootstrap fresco no equivale a migraciones aplicadas en entorno existente |
| S12 | backend/src/Infrastructure/Persistence/IdentidadUniversalSnapshotEfSource.cs — a02ddd5883d3b9f855144c178cb96ecf965a7411 | Adaptador EF de lectura de membresía empresarial/identidad, sin endpoint | No deriva permiso de consulta no verificado ni habilita escritura |
| S13 | backend/src/Infrastructure/Migrations/20260808165129_M1EndurecerIntegridadRelacional.cs — 7480fa634101ab9740f0183f8f937ee3e11d9fbb | Migración histórica de endurecimiento que altera índices y estructura | Su presencia no prueba estado applied, Down seguro ni ausencia de deriva |
| S14 | backend/src/Infrastructure/Solqaryn.Infrastructure.csproj — 6210d8b12abd77171d95d17e2292c1a319b89263 | EF Core 10.0.12, MySql.EntityFrameworkCore 10.0.9 y net10.0 declarados | Provider real por entorno debe comprobarse en configuración/CI |
| S15 | backend/src/Domain/Entities/Venta.cs — 64ab3cd92e81031e74137f420cd748c564e28f01 | Archivo entidad Venta en árbol del repositorio con blob identificado | Contenido detallado, mappings y relaciones NO_ANALIZADOS en esta muestra |

S15 es evidencia **de presencia del archivo**, no de su lógica. Se distingue lectura directa del contenido (S01–S14, cuando se inspeccionó una sección concreta) de listado de árbol (S15). El modelo completo contiene más tipos, configuraciones y migraciones: **la ausencia de un archivo en esta tabla no significa que falte código ni que se haya auditado todo**.

## Brechas G01–G12 (evidencia pendiente, no defectos certificados)

- **G01:** matriz completa entidad -> tabla -> PK -> FK -> empresa/sucursal/usuario y cardinalidad, con origen de cada configuración EF.
- **G02:** estado físico por ambiente de schema, historial de migraciones y deriva entre modelos/DDL, sin tocar Aiven.
- **G03:** muestra y número real de filas legacy con EmpresaId NULL o huérfanas, con proceso de reconciliación autorizado.
- **G04:** comprobación efectiva de FKs compuestas y políticas ON DELETE sobre MySQL efímero.
- **G05:** ejecución de VerifyAsync para índices naturales y unicidad por tenant, con resultado/plan y SHA.
- **G06:** SQL/explain y presupuesto de índice para casos reales, sin métricas inventadas.
- **G07:** límites transaccionales y rollback de ventas, inventario, facturas, pagos y devoluciones.
- **G08:** cobertura de filtros EF frente a consultas raw SQL, rutas públicas y jobs que no pasan por DbContext.
- **G09:** colisión por empresa o sucursal y acceso cross-tenant A/B probado en integración, no inferido de un nombre de clase.
- **G10:** convivencia de campos nullable legacy con restricciones futuras y política de backfill sin borrado silencioso.
- **G11:** salida segura de fallos, logs y auditoría sin PII, token, cadena de conexión o SQL sensible.
- **G12:** aplicación y reversión real de migración versionada en MySQL efímero con checksum/conteos y plan de contingencia.

Se reconocen estas doce como **brechas de prueba o inventario**, no como vulnerabilidades o fallos ya demostrados. Para cerrar M01.03 no se requiere ejecutar CI de runtime por un diff únicamente Markdown; sí comprobar evidencia documental, Scope Lock y procedencia de CHANGELOG.

## Oráculos de pruebas futuros P01–P08 y N01–N12 — NO_EJECUTADOS

**Positivos:**
P01 leer en empresa A una entidad A autorizada; P02 negar por defecto históricos sin dueño y reconciliarlos de manera explícita en fixture efímero; P03 crear padre/hijo con FK compuesta válida; P04 verificar índice tenant-scoped con esquema físico MySQL y EXPLAIN; P05 aplicar una migración en MySQL efímero y revertirla; P06 ejecutar operación financiera/inventario atómica; P07 comprobar contexto temporal de soporte realmente read-only; P08 cotejar esquema físico y modelo EF sin diferencias injustificadas.

**Negativos:**
N01 tenant A lee o escribe entidad B; N02 reemplazar EmpresaId/slug/header enviado desde cliente; N03 escritura de descendiente con padre de otro tenant; N04 FK rota o fila histórica sin owner se convierte a NOT NULL sin plan; N05 índice global bloquea claves de empresas distintas; N06 soporte intenta SaveChanges; N07 consulta raw omite barrera tenancy; N08 operación financiera falla a mitad y persiste parcial; N09 migración destructiva sin rollback/backup; N10 log revela PII/secreto; N11 storefront público obtiene datos administrativos; N12 se interpreta un documento o check in_progress como autorización de producción.

Todos esos oráculos son especificaciones a convertir en tests A1–A3 de fases posteriores. **NO HAY PASS runtime, migración aplicada ni certificación de MySQL en esta microtarea.**

## Impacto, auditorías y reversión

**Componente:** documentación de modelo relacional, con fuentes Domain/Infrastructure y consumidores futuros API/Application/Angular. **Archivos modificables en M01.03:** este Markdown y sección aislada en CHANGELOG_AI.md. **API/DTO:** no cambian; **base de datos/datos/índices/transacciones:** no cambian; **seguridad JWT/RBAC/tenancy:** no cambia; **jobs, proveedores, observabilidad y secretos:** no cambian; **arquitectura canónica:** sin actualización, únicamente trazabilidad; **riesgo residual:** gaps G01–G12. **Rollback:** git revert ordinario del commit documental, conservando historia, no force-push.

**A0:** leer plan V5, Skill/HEAD exacto, Scope Lock, COLA, DAG y rutas originales. **A1:** verificar 15 entradas S, 12 gaps G y P/N futuros explícitos, sin certificar resultados. **A2:** garantizar no elevar permisos ni exponer PII, tenant A/B y soporte de solo lectura. **A3:** cotejar con límites de Domain/Application/Infrastructure y no inferir esquema live. **A4:** CAS fast-forward de dos Markdown en dev, check exact-SHA Scope Lock y V5 provenance completed/success, diff y readback PLAN/COLA/BITACORA. Suites .NET/Angular/MySQL sin causalidad para Markdown = NO_APLICA, jamás PASS.

**A5_PENDIENTE:** revisión independiente exclusiva de otra Task Javier permitida. La documentación del inventario no habilita M01.06, cambios IAM, migraciones, SQL de producción, eventos, webhook, QA/main/PROD, host ni recursos de pago.
