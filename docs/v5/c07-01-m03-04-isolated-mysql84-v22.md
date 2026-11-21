# SOLQARYN V22 — C07-01.M03.04: recurso de catálogo aislado, MySQL 8.4

PLAN_MAESTRO D1025: implementar persistencia, migración reversible **o adaptador aislado**. Se eligió explícitamente el último como unidad R2. «Completa M03» en E es objetivo acumulativo, NO una afirmación de completar toda la fase con .04. Fuente: STANDARD V5 FINAL, PLAN/COLA/DAG institucional, solqaryn/Solqaryn@dev. PROJECT_SCOPE_LOCK=STRICT.

## A0: cambio real y arquitectura

El lector C0701IsolatedMySqlResourceReader implementa IC0701VerifiedCatalogResourceReader de Application. Infrastructure recibe DbConnection de una fábrica explícita, no registra nada en API/DI productiva, y no modifica AppDbContext/EF ni esquema de Aiven. La tabla V22C0701CatalogSnapshotIsolated se aprovisiona exclusivamente en la base MySQL efímera de CI. Consulta parametrizada de producto para tenant autenticado y versión, REPEATABLE READ, rollback de lectura. Falla cerrado en legacy TenantId NULL, dato ambiguo, categoría/variante con empresa diferente, versión no positiva, inactivo o eliminado. Sin lecturas de B desde A. La sucursal se conserva en snapshot para verificación por Domain y autorización posterior.

No se afirma autorización JWT/HTTP ni propiedad de Producto comercial real por el solo resultado de esta lectura: el actor firmado/membresía/RBAC pertenece a .03, y cualquier escritura futura debe revalidarlos junto con propiedad/version en UNA transacción de BD. El adaptador no altera inventario, stock, impuestos, precios, pedidos, facturas ni snapshots históricos.

## A1–A3: evidencia causal

C0701M0304IsolatedMySqlResourceReaderTests usa servidor MySQL 8.4 real en GitHub-hosted con env variable dedicada, contraseña solo local al job y prueba SELECT VERSION()=8.4.*. Rechazos: tenant B, huérfano/NULL, duplicados, categoría y variante B, versión inválida/desactualizada, borrado lógico, inactividad, ids inválidos, cancelación y error SQL; positivo A y B, y rollback InnoDB de insert temporal. Workflow v22-c07-01-m03-04-isolated-mysql84.yml exige xUnit Release/TRX total=passed, cero FAIL/NOT_EXECUTED, Scope Lock exact-SHA. No confundir mocks o jobs en curso con integración PASS.

## A4, A5, limitaciones y reversión

Sólo concluir VALIDANDO→LISTO tras confirmar CI real exact-SHA completed/success, A0–A3 según D1025, Scope Lock, V5, backend/security requeridos, readback de PLAN/COLA/BITACORA y V17 CAS. A5 por otra Task Javier con revisión independiente real. No se certifican migraciones reales, endpoint CRUD/HTTP JWT productivo, Angular, SKU, atribución de Producto legacy, stock/precios, lectura Aiven ni R3; son etapas separadas. Rollback mediante revert normal de dev, sin force-push, servicio MySQL efímero destruido por GitHub-hosted. Ninguna operación QA/main/PROD ni secretos o compras.
