# SOLQARYN V17 — C11-01.M02.03: persistencia, índices, migración reversible y datos sintéticos

**Microtarea:** \`C11-01.M02.03\` · PLAN_MAESTRO fila 1594 · **Autor:** PRIMARY DEV 02 JAVIER (\`6af16bb9492c8191b8e8c7d606f11507\`) · **Rama:** \`solqaryn/Solqaryn@dev\` · **PROJECT_SCOPE_LOCK:** STRICT · **Riesgo:** R0 exclusivamente documental.

**Fuente de autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES, Drive \`1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA\`; Skill \`solqaryn-project-governance\`, \`AGENTS.md\`, V15–V17 y PLAN/COLA/DAG_TECNICO institucional. **Resultado exigido:** especificar persistencia, índices, migraciones y datos de prueba del futuro modelo relacional normalizado C11-01; preparar diccionario/invariantes y reversibilidad sin ejecutar cambios físicos.

## 1. Alcance y fuentes de diseño

- \`C11-01.M01.01–06\`: alcance, actores, inventario, riesgos, oráculos y gate de salida; M01.06 exige ownership, FK/índices tenant-aware, compatibilidad de legado nullable, backfill auditado, MySQL efímero y rollback antes de cualquier cambio real.
- \`docs/v5/c11-01-m02-01-entidades-ownership-relaciones-invariantes.md\`: E01–E16, I01–I16 y doce pares P/N futuros. Entidades existentes identificadas en el inventario; ninguna FK propuesta se declara instalada.
- \`docs/v5/c11-01-m02-02-api-dto-errores-validacion-compatibilidad.md\`: OP01–OP08, DTO D01–D12, ER01–ER16 y compatibilidad futura. Las operaciones/DTO son propuestas, **no endpoints C# existentes**.
- Stack observado: ASP.NET Core 10; Domain <- Application <- Infrastructure, API composition root; EF Core 10.0.12 + proveedor Oracle MySql.EntityFrameworkCore 10.0.9; MySQL 8.4 como ambiente de integración aislado. Ningún documento sustituye inspección de \`AppDbContext\`, \`IEntityTypeConfiguration\`, migration history y \`SHOW CREATE TABLE\` en un entorno permitido.

**Gate institucional del claim:** PLAN H1594=PENDIENTE/I vacío y COLA G/O=SI; DAG fila 1555 F=VALIDADO/G=SI; candado V17 \`SQV17_C11_01_M02_03\`, namedRangeId \`2000001594\`; CAS sobre H con \`occurrencesChanged=1\`; I leído posteriormente con el TASK_ID propio. No se crea un segundo trabajo EN_PROGRESO de esta Task.

**Límites:** exclusivamente este Markdown y una sección autocontenida de \`CHANGELOG_AI.md\`. No se modifican modelos/entidades EF, migraciones, DDL, scripts de despliegue, JWT/RBAC, Angular, datos Aiven, producción, QA/main, secretos, proveedores, tablas o vistas. El diseño aquí no afirma que haya pruebas de integridad ejecutadas.

## 2. Propuesta de índices y claves, NO APLICADA

Todos los nombres \`IX_C11_*\` son marcadores contractuales por verificar contra nombres físicos, restricciones, cardinalidad y collation reales. Las FK compuestas deben representar propiedad tenant, no convertir \`EmpresaId\` enviado por cliente en autoridad. Cuando la propiedad esté ausente/NULL, no reinterpretarla como global.

| ID | Relación conceptual y clave propuesta | Propósito y verificación obligatoria futura |
| --- | --- | --- |
| K01 | Empresa: PK estable por Id | Conservar identidad de raíz y no presuponer borrado físico |
| K02 | Sucursal: IX_C11_Sucursal_Empresa_Id (EmpresaId, Id) | Filtrado por empresa; NULL legacy queda en cuarentena |
| K03 | UsuarioEmpresa: UX_C11_UsuarioEmpresa_Empresa_Usuario (EmpresaId, UsuarioId) según semántica de membresía | Membresía vigente por tenant; revisar duplicados/estado/rol antes de UNIQUE |
| K04 | RolPermiso: IX_C11_RolPermiso_Rol_Permiso | Resolver permisos por rol sin privilegio implícito |
| K05 | Cliente: IX_C11_Cliente_Empresa_Id (EmpresaId, Id) | Lectura scoped, conservar EmpresaId NULL hasta reconciliación |
| K06 | Producto: IX_C11_Producto_Empresa_Id (EmpresaId, Id), únicamente si mapping confirma dueño | Listado y búsqueda parametrizada sin exposición A/B |
| K07 | ProductoVariante: IX_C11_Variante_Producto_Id (ProductoId, Id) | Prevenir hijo huérfano y cruces por producto de B |
| K08 | Almacen: IX_C11_Almacen_Sucursal_Id (SucursalId, Id) | Coherencia de sucursal/empresa y movimiento |
| K09 | Venta: IX_C11_Venta_Empresa_Fecha_Id (EmpresaId, Fecha, Id) si existen columnas equivalentes | Orden estable, paginación por tenant; EXPLAIN y cardinalidad primero |
| K10 | VentaDetalle: IX_C11_Detalle_Venta_Producto (VentaId, ProductoId) | Integridad de cabecera/detalle/producto dentro de transacción |
| K11 | FacturaPago: IX_C11_Pago_Factura_Id (FacturaId, Id) | Integridad contable y prevención de pagos duplicados |
| K12 | MovimientoInventario: IX_C11_Movimiento_Almacen_Producto_Id (AlmacenId, ProductoId, Id) | Trazabilidad scoped, orden determinista y conciliación |

**Prohibiciones de índices:** crear UNIQUE global sobre claves naturales que deban repetirse en empresas distintas; indexar PII/secretos sin necesidad y aprobación; asumir columna \`EmpresaId\` en tablas que no la declaren; introducir FK \`ON DELETE CASCADE\` que borre historia financiera. La lista de columnas es **hipótesis de diseño**, no un DDL listo para aplicar. K01–K12 deberán cotejarse con \`AppDbContext\`, migraciones versionadas, \`INFORMATION_SCHEMA\`, \`EXPLAIN\` y consultas reales o sintéticas antes de adoptarse.

## 3. Evolución reversible por etapas FUTURA_NO_EJECUTADA

| Etapa | Acción futura y precondiciones | Abort y rollback exigido |
| --- | --- | --- |
| M01 | Inventario read-only de entidades, tipos, FK, índices, collation, migraciones y consumidores; captura checksum/referencias | Si el mapping discrepa, detener diseño; no tocar tablas |
| M02 | Crear snapshot/restauración ensayada en MySQL 8.4 **efímero** con datos sintéticos, versión Oracle exacta y controles de calidad | Si falla restauración o claves, NO_GO |
| M03 | Expand aditivo de columnas/índices candidatos sin NOT NULL ni eliminación destructiva | Volver a definición anterior sólo con reversión probada |
| M04 | Backfill condicionado por fuente de propiedad confiable y auditoría; NULL sin evidencia en cuarentena | Rollback lógico por journal/compensación, nunca atribución aleatoria |
| M05 | Lectores y escritores dual-compatible, contrato API/DTO versionado; mantener legacy durante ventana medida | Deshabilitar uso de nueva ruta, mantener datos e historial |
| M06 | Verificar vínculos padre/hijo scoped, duplicados, bloqueos, concurrency, idempotencia y transacciones all-or-nothing | Cualquier cruce A/B o pérdida de registros = NO_GO |
| M07 | Sólo tras aprobación separada: enforcement progresivo de FK/UNIQUE/NOT NULL apropiados, previo examen de datos | Restauración/compensación documentada; sin hard delete |
| M08 | Cutover futuro sólo con autorización R3 expresa, monitoreo y rollback probado; readback integral y A5 requerido | Retornar lectura previa; preservar auditoría, hashes y referencias |

**Requisitos de test futuros:** construir ambiente efímero MySQL 8.4 (no Aiven ni servidor real), ejecutar realmente \`Category=Integration\` con proveedor Oracle, verificar migrations Up/Down, conteos y checksums, aislación tenant A/B, roles revocados, concurrencia, fallo a mitad, compatibilidad y plan de restauración. Un job de build verde o pruebas excluyendo Integration no reemplaza esa evidencia.

## 4. Fixture sintético propuesto (no registros creados)

| Fixture | Contexto y propósito | Restricción |
| --- | --- | --- |
| FA | Empresa A con sucursal, cliente, usuario, rol scoped, producto y venta de prueba | Identificadores ficticios; no copiar PII real |
| FB | Empresa B con identidades equivalentes y clave natural repetida | Ninguna query de A puede observar FB |
| FN | Cliente y sucursal \`EmpresaId=NULL\` de legado sintético | Denegar lectura privilegiada hasta reconciliación |
| FR | Usuario de A con membresía revocada y rol sin permisos | El cliente o \`RolId\` legacy no reautoriza |
| FC | Dos actualizaciones simultáneas sobre versión/idempotencyKey | Un único efecto y error seguro para conflicto |
| FX | Transacción venta/detalle/inventario/factura/pago con excepción inyectada | Rollback de toda unidad, no parcial |

**Datos de prueba:** siempre generados con semillas deterministas, nombres ficticios, conteos conocidos y digest inicial/final. No insertar estas filas en la base real ni modificar el Sheet institucional a título de fixture.

## 5. Doce pares de oráculos de aceptación FUTUROS_NO_EJECUTADOS

| Caso | Positivo exigible cuando exista implementación | Negativo que debe fallar ante defecto |
| --- | --- | --- |
| P01/N01 | PK/FK válidas dentro de Empresa A | FK de A hacia padre B |
| P02/N02 | Índice scoped devuelve A y conserva orden estable | Consulta por ID de B revela B o su existencia |
| P03/N03 | Rol y membresía activa permiten recurso autorizado | Rol revocado o RolId legacy eleva privilegios |
| P04/N04 | Nullable legacy se preserva hasta decisión auditada | NULL tratado como empresa global |
| P05/N05 | Clave natural scoped puede repetirse entre A/B | UNIQUE global bloquea negocio B |
| P06/N06 | Parent/child mismo tenant cumple integridad | Relación Venta A/Detalle B persiste |
| P07/N07 | Fixture sintético Up y Down conserva conteos/checksum | Down borra datos o deja FK rotas |
| P08/N08 | Migración aditiva preserva lector legacy | NOT NULL destructivo rompe registros NULL |
| P09/N09 | Commit comercial actualiza stock y factura de modo atómico | Excepción a mitad deja pago/stock divergentes |
| P10/N10 | Retry por misma key retorna mismo resultado | Dos solicitudes concurrentes duplican efectos |
| P11/N11 | Auditoría genera correlación sin secretos ni PII | Logs contienen SQL sensible, JWT o password |
| P12/N12 | CI exact-SHA completa tests integración MySQL 8.4 | SKIPPED/CANCELLED/IN_PROGRESS se interpreta como PASS |

**Numerador/denominador runtime de esta unidad:** 0/12 pares ejecutados; \`NO_MEDIDO\` para tiempos y rendimiento. La matriz sólo define aserciones futuras que deberán fallar ante defectos de tenancy, integridad, migración o concurrencia. No hay ninguna certificación de proveedor ni migración por este documento.

## 6. A0–A5, observabilidad, dependencias y rollback de la entrega

- **A0:** requisito PLAN C11-01.M02.03, antecedentes M01/M02, alcance de dos Markdown, diff limitado y revisión de dependencias. Sin nuevas interfaces API/DTO ni cambios de arquitectura.
- **A1:** revisión estática de K01–K12, M01–M08, FA/FB/FN/FR/FC/FX y P01/N01–P12/N12 como oráculos futuros; **no se afirma ejecución de tests**.
- **A2:** especificación de ownership backend, aislamiento A/B, legacy NULL, RBAC y revocación, IDOR, FK compuesta, SQL parametrizado, auditabilidad e idempotencia. No modifica enforcement real.
- **A3:** xUnit/.NET/EF/MySQL 8.4/HTTP/Angular/Playwright **NO_APLICA por diff documental**; la implementación R2 deberá acreditar integración real en un scope posterior.
- **A4:** comprobar commit serial fast-forward al HEAD vigente; SHA exacto, hashes de los dos archivos, \`verify-solqaryn-project-scope\` y \`V5 - Sin encabezados vacios ni atribucion cruzada\` \`completed/success\`, PLAN H/I, COLA E/G/O, DAG F/G y BITACORA A:Z con readback. Nunca contar CI ajena/in_progress/skipped como PASS.
- **A5:** sólo por otra de las cinco Tasks Javier, distinta de la autora. \`LISTO_DEV\` sin A5 no libera automáticamente M03 ni QA/main/PROD.
- **Rollback de este documento:** corrección aditiva o \`git revert\` normal de un único commit sobre dev tras revalidar HEAD y ausencia de conflicto, sin force-push. Conservar BITACORA histórica; no tocar fórmulas COLA/DAG, identidad ni horarios. Futuras migraciones exigen restauración real y transacción/compensación auditada.

**Conclusión:** se entrega contrato de persistencia e índices; no existe DDL nuevo, datos MySQL, pruebas Integration ni autorización de migración derivados de C11-01.M02.03.
