# SOLQARYN V17 — C11-01.M02.01: entidades, ownership, relaciones e invariantes del modelo relacional

**ID:** C11-01.M02.01 · **PLAN_MAESTRO:** fila 1592 · **Autor:** PRIMARY DEV 04 JAVIER, TASK_ID 6aeacb7820ac8191b7d768a6f33dfa09 · **Repositorio:** solqaryn/Solqaryn, rama dev · **PROJECT_SCOPE_LOCK=STRICT** · **Riesgo:** R0, documento conceptual sin modificación ejecutable.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES (Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA); V15 A0–A5, V16/V17; Skill canónica y AGENTS.md en dev; PLAN/COLA/DAG actuales. **Criterio exacto:** definir entidades, ownership, relaciones y reglas de invariantes para el futuro modelo relacional normalizado. La salida M02 exige diccionario, invariantes y reversibilidad. Este documento **no crea ni certifica una tabla, FK, índice, migración, endpoint, proveedor ni dato físico**.

## 1. Autoridad de la unidad y antecedentes

Antes del CLAIM, PLAN H1592=PENDIENTE/I1592 vacío, COLA G/O=SI y DAG_TECNICO fila 1553 F=VALIDADO/G=SI. Se obtuvo candado exclusivo SQV17_C11_01_M02_01 (namedRangeId 2000001592); findReplace sobre H1592 devolvió occurrencesChanged=1, se estableció owner I1592 a DEV04 y se comprobó readback y evento BITACORA. Sólo una EN_PROGRESO propia. Se preservan owner, CI y autorías de otras cinco Tasks Javier.

- M01.01: docs/v5/c11-01-m01-01-objetivo-no-alcance-modelo-relacional.md, blob 530958065528c4d2f8b0d9044b2e55776b0697e6; NR01–NR14, no-regresión y exclusiones.
- M01.03: docs/v5/c11-01-m01-03-inventario-codigo-evidencia-modelo-relacional.md, blob 4f45d732115187fcc9cc6b3a0e85e4c2896c96b8; 15 fuentes versionadas y 12 brechas que NO son defectos certificados.
- M01.05: docs/v5/c11-01-m01-05-criterios-aceptacion-modelo-relacional.md, blob 5873d61759e973762a780125408f92f9c179991f; 16 positivos, 16 negativos y 10 RNF futuros NO_EJECUTADOS.
- M01.06: docs/v5/c11-01-m01-06-gates-rollback-liberacion-modelo-relacional.md, blob f6908d23b07a1e0646e245287add9be8c18f7913; 16 bloqueos y 8 pasos rollback; LISTO_DEV con A5 independiente DEV04 y readback, sin ampliar autorización técnica.
- Stack observado por la Skill: Domain <- Application <- Infrastructure, API; ASP.NET Core 10, EF Core 10.0.12, MySql.EntityFrameworkCore Oracle 10.0.9, MySQL 8.4 objetivo. No sustituir el proveedor por un alternativo ni derivar acceso directo desde Angular.

## 2. Matriz conceptual de entidades y ownership, NO esquema físico certificado

Las rutas corresponden a nombres de archivos presentes en backend/src/Domain/Entities. Una relación marcada **a verificar** es un requisito futuro, no una afirmación de FK existente. El owner de acceso proviene del contexto backend autenticado, no de un identificador enviado por el cliente.

| ID | Entidad existente | Ownership / contexto exigido | Relación e integridad por verificar |
| --- | --- | --- | --- |
| E01 | Empresa | Raíz empresarial; estado vigente verificado | PK estable; no asumir soft-delete |
| E02 | Sucursal | Empresa de la sucursal, con legacy nullable | Empresa–Sucursal y cuarentena de NULL |
| E03 | Usuario | Identidad global, NO derecho universal entre empresas | Membresía se resuelve vía UsuarioEmpresa |
| E04 | UsuarioEmpresa | Membresía activa por empresa y rol scoped | Usuario–Empresa–Rol; revocación prevalece |
| E05 | Rol | Autoridad contextual según asignaciones vigentes | No inferir permiso por RolId legacy aislado |
| E06 | RolPermiso | Permiso efectivo ligado al rol autorizado | Denegar permiso ajeno y rol inactivo |
| E07 | Cliente | EmpresaId nullable histórico comprobado | Empresa–Cliente, sin atribuir huérfanos |
| E08 | Producto | Propiedad/visibilidad a comprobar por mapping | Empresa–Producto; guard backend obligatorio |
| E09 | ProductoVariante | Hereda contexto verificable del producto padre | Producto–Variante, sin referencias cruzadas |
| E10 | Almacen | Empresa/sucursal a comprobar de modo explícito | Sucursal–Almacén y políticas de movimiento |
| E11 | Venta | Autoridad empresarial y consistencia comercial | Empresa–Venta–Cliente; transacción completa |
| E12 | VentaDetalle | Ownership heredado del padre verificado | Venta–Detalle–Producto; FK compuesta candidata |
| E13 | Factura | Empresa y autorización fiscal verificadas | Venta–Factura/Cliente; unicidad scoped candidata |
| E14 | FacturaPago | Mismo ámbito empresarial de factura | Factura–Pago y monto/estado coherentes |
| E15 | MovimientoInventario | Empresa, almacén, producto y actor autorizados | Almacén–Producto–Movimiento, rollback atómico |
| E16 | RegistroAuditoria | Contexto actor/empresa de operación legítima | Referencia estable y retención sin PII/secreto crudo |

**Estado de evidencia:** Empresa.cs y Cliente.cs fueron identificados en M01.01; Cliente.EmpresaId y Sucursal.EmpresaId nullable están documentados. AppDbContext, filtros, contratos físicos e índices requieren lectura exacta de mappings e integración MySQL efímera antes de asegurar que una FK, filtrado o índice existe y funciona. El inventario M01.03 identifica rutas, no cuenta filas reales ni acredita aplicación de migraciones.

## 3. Invariantes contractuales I01–I16

| Regla | Condición contractual / bloqueo |
| --- | --- |
| I01 | Ninguna FK candidata se considera aplicada sin evidencia de schema físico y migration history |
| I02 | Tenant efectivo de lectura/escritura se deriva del backend autenticado, nunca de EmpresaId, slug o header controlado |
| I03 | Tenant A no lee ni escribe entidad B por IDOR/BOLA, navegación ni raw SQL |
| I04 | Usuario y UsuarioEmpresa se combinan con membresía vigente y rol scoped; revocación da DENY |
| I05 | Sucursal y Cliente con EmpresaId NULL permanecen en cuarentena de autorización hasta reconciliación explícita |
| I06 | FK compuesta empresa+identidad de recurso se considera candidata hasta verificar modelo, DDL y ON DELETE |
| I07 | Claves naturales únicas son por ámbito autorizado, salvo excepción justificada y medida |
| I08 | Evitar ambigüedad de propiedad si padre e hijo pertenecen a empresas distintas |
| I09 | Transacción de venta/detalle/stock/factura/pago cumple todo-o-nada ante excepción |
| I10 | Soporte temporal read-only rechaza cualquier SaveChanges y evita bypass por tracking |
| I11 | Rutas públicas storefront nunca convierten capacidades públicas en permisos administrativos |
| I12 | Indexar solamente con consulta, EXPLAIN, presupuesto y cardinalidad de fixtures identificados |
| I13 | Expand/contract conserva compatibilidad de lectores y escritores; rollback probado antes del DDL |
| I14 | No endurecer NOT NULL ni borrar filas legacy sin censo, decisión, backup y compensación autorizada |
| I15 | Auditoría evita tokens, SQL sensible, cadenas de conexión y PII cruda |
| I16 | CI in_progress, skipped, cancelled o failed no es PASS; A5 y aprobación de despliegue son puertas separadas |

## 4. Doce pares de aceptación FUTUROS_NO_EJECUTADOS

| Par | Positivo que deberá probarse | Negativo que deberá denegarse o revertirse |
| --- | --- | --- |
| P01/N01 | Empresa A consulta su entidad A autorizada | A consulta por ID de B y no filtra existencia B |
| P02/N02 | UsuarioEmpresa activa habilita rol específico | Rol revocado o legacy RolId no concede acceso |
| P03/N03 | Sucursal con owner verificado pertenece a Empresa | Sucursal legacy NULL se usa como autorización |
| P04/N04 | Cliente legacy NULL sobrevive al plan de backfill | Asignación silenciosa o NOT NULL destructivo |
| P05/N05 | Producto/variante del mismo ámbito se relacionan | Variante de B se agrega a venta de A |
| P06/N06 | Padre/hijo tenant-aware acepta FK válida | FK con padre de otra empresa se rechaza |
| P07/N07 | Índice compuesto admite claves iguales entre A y B | Índice global bloquea B indebidamente |
| P08/N08 | Venta, stock y factura confirman una transacción | Falla a mitad revierte todos los cambios |
| P09/N09 | Soporte puede consultar sin modificaciones | Soporte ejecuta SaveChanges o SQL de escritura |
| P10/N10 | Migración se aplica en MySQL 8.4 efímero | Down rompe referencias o borra datos del fixture |
| P11/N11 | Endpoint devuelve error controlado y trazable | Token, cadena, SQL o PII aparecen en logs |
| P12/N12 | SHA correcto y tests reales completados | Build verde aislado se confunde con integración/A5 |

**Numerador/denominador ejecutados:** 0/12 pares en runtime; por diseño R0 sólo se entrega este contrato. No se atribuye PASS a API, xUnit, Playwright, MySQL 8.4, EF, fixture tenant A/B, performance o proveedor. Estos oráculos deberán convertirse en tests reales en la microtarea técnica pertinente.

## 5. Dependencias, pruebas pertinentes y rollback

**Impacto A0:** sólo este Markdown y sección autocontenida de CHANGELOG_AI.md; Domain, Application, Infrastructure, API y Angular permanecen intactos. **Interfaces/DTO:** sin cambio. **Datos/transacciones/migraciones:** sin ejecución ni escritura. **Autorización:** no se modifica JWT/RBAC/tenant, aunque se especifican obligaciones futuras de aislamiento. **Observabilidad/jobs/proveedores:** sin cambios. **Arquitectura:** ningún cambio de frontera, no se actualizan mapas canónicos. La futura M02.02 u otra sucesora exige PLAN H=PENDIENTE/I vacío, COLA G/O=SI, DAG F=VALIDADO/G=SI y candado V17 propio; M02.01 LISTO no equivale a A5.

**A1:** cotejo de E01–E16, I01–I16 y P01/N01–P12/N12; los oráculos deben poder detectar defectos reales al implementarse. **A2:** controles conceptuales de tenancy A/B, rol revocado, legacy NULL, soporte read-only, SSRF/PII si corresponde a consumidores, sin ejecución. **A3:** pruebas MySQL/EF/.NET/HTTP/browser NO_APLICA por no cambiar runtime; la necesidad futura de MySQL 8.4 efímero no se cancela. **A4:** requeridos de documentación: diff exacto, Scope Lock y procedencia V5 completed/success del SHA publicado, cabecera CHANGELOG junto a viñetas, BITACORA/PLAN/COLA/DAG con readback. **A5:** independiente por otra Task Javier distinta del autor.

**Rollback documental:** identificar source SHA y sus dos blobs; usar corrección aditiva o git revert normal tras revalidar HEAD, Scope Lock y ausencia de conflictos. No force-push ni reset, no cambiar fórmulas de COLA/DAG ni borrar BITACORA. Para cualquier implementación posterior, exigir ensayo expand/contract, backup, migración inversa/compensación, checksums y pruebas de pérdida de datos en MySQL efímero. Nunca usar Aiven o QA/main/PROD sin otra autorización específica.
