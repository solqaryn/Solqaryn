# C07-01.M05.03 — integración/persistencia real MySQL 8.4 (V22)

PLAN D1036: Ejecutar prueba de integración/persistencia/proveedor cuando corresponda. Se entrega un incremento test-only R2 sobre adaptador aislado real, no certificar fase M05 completa ni modificar runtime/PROD.
A0: nuevo archivo xUnit, workflow GitHub-hosted y este documento. Sin alterar código runtime, tenancy/auth, EF migrations ni proveedores comerciales.
A1: lectura positiva desde InnoDB en contenedor físico MySQL 8.4 (verificación SELECT VERSION), fila tenant A y B, versión, actor, rechazo de datos duplicados y eliminados.
A2: aislamiento real mediante WHERE TenantId/ProductoId de C0701IsolatedMySqlResourceReader, B no consulta A, producto sin owner legacy NO_GO, categoría B y revocación rechazada en reglas de dominio. La sesión/rol es fixture: NO certifica JWT firmado ni endpoint HTTP ni autorización transaccional real de escritura.
A3: servicio efímero MySQL 8.4 en GitHub-hosted, esquema exclusivo c0701_m0503_bootstrap, UPDATE transaccional con rollback físico y aislamiento de versión, limpieza DROP TABLE. No Aiven ni datos vivos.
A4: exact SHA + ScopeLock Skill self-test, .NET Release xUnit, TRX total>=1 passed==executed, 0 failed/notExecuted; backend/security checks pertinentes SUCCESS. QUEUED/IN_PROGRESS/CANCELLED/SKIPPED nunca PASS. Owner, V17 CAS y readback PLAN/COLA/BITACORA.
A5: revisión independiente por otra Task Javier distinta de DEV01. Limitación: no certifica MySQL transacción productiva, real HTTP/JWT ni fase completa. Rollback git revert, nunca fuerza historia.
