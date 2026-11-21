# C04-01.M03.05: integración de cola aislada MySQL 8.4

PLAN D594 permite worker, proveedor, cola o integración cuando aplique. Se implementa una cola **aislada de desarrollo** con InnoDB, claim/ACK atómico por tenant, lease temporal y revisión CAS. A diferencia de un mock, CI usa MySQL 8.4 físico efímero. El servicio no se registra en DI de API, no ejecuta worker de pagos, no interactúa con PSP, ni altera cuotas comerciales, secretos o schemas de producto.

R2 A0: código y contratos limitados a cola aislada; A1: positivos, competidor simultáneo, worker falso, revisión obsoleta y repetición; A2: tenants A/B sin acceso cruzado y replay posterior a vencimiento; A3: test de integración real MySQL8.4 en GitHub-hosted TRX ejecutado; A4: requeridos ScopeLock, V5, Backend Release, seguridad y workflow por SHA. CI en progreso no es PASS. A5 por Task Javier distinta a DEV06.

La identidad tenant de parámetros no es prueba de JWT; producción requeriría fuente firmada de identidad y RBAC, host worker real, permisos, auditoría, renovación robusta y comprobación de cuotas con transacción atómica. No certificar fase M03 completa ni activar cobros o releases. Rollback dev: git revert normal del commit atómico; MySQL CI se destruye al acabar. No cambios qa/main/PROD.
