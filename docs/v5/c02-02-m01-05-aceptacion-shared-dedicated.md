# SOLQARYN V5 FINAL — C02-02.M01.05: aceptación funcional y técnica del almacenamiento shared/dedicated

**Tipo 1: criterios documentales, sin implementación ni prueba de infraestructura real.** Fecha 2026-11-13; repo solqaryn/Solqaryn dev; PROJECT_SCOPE_LOCK=STRICT. Autor PRIMARY DEV 01 JAVIER, TASK_ID 6adbb9fff1548191a1bd5b30d9e9984a. Fuentes institucionales: Standard V5 FINAL, Doc 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA y Sheet 1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU.

## Frontera y propósito

La fase C02-02 pretende definir almacenamiento compartido, dedicado, routing **server-side**, ciclo de migración y rollback sin DSN arbitrario del cliente. La salida **de esta M01.05** es exclusivamente una matriz de aceptación observable positiva y negativa para futura implementación. El hecho de escribirla no crea bases, router, binding ni despliegue ni certifica restauración. DAG_TECNICO F=VALIDADO/G=SI autorizó únicamente preparación M01.01–05; C02-01.M01.01–02 y demás prerequisitos se leen desde PLAN/COLA. Las puertas M01.06/M02 siguen separadas, no se liberan automáticamente.

**Preflight por artefacto:** requiere_dotnet=NO; requiere_node_angular=NO; requiere_mysql=NO; requiere_docker=NO; requiere_e2e=NO, porque aquí no se modifica ejecutable. requiere_security=SI (criterios tenancy, DSN, secretos, fuga y rollback), requiere_scope_lock=SI (commit documental exact SHA). Para futuros cambios EF/router/migración sí se exigen dotnet/EF/MySQL 8.4/Docker cuando aplique en CI real y al SHA exacto. No inventar PASS de entorno live.

## Evidencias de diseño vigentes inspeccionadas

| Fuente | Blob Git SHA | Lo que se acredita (no lo que se presume) |
| --- | --- | --- |
| backend/src/Application/Interfaces/StorageTenantContext.cs | 716e8e205502d6300f555947c79edc2852fd63ba | Contexto de almacenamiento derivado de tenant verificado, guardia ExigirEmpresa y prefijo lógico. |
| backend/src/Domain/Security/ContextoTenantActual.cs | 7377d2d752985cc9baf31d08280f38ad94a6a114 | Fábrica de contexto por membresía activa, no valores del cliente. |
| backend/src/Infrastructure/Persistence/AppDbContext.cs | c4b32f6637f1b4426655bb0cd12af8dd9d229866 | Contextos de lectura marcados por servidor y restricciones de soporte read-only. |
| backend/src/API/Middleware/UnscopedCommercialTenantGuardMiddleware.cs | 558dda1eb5be729b2bd9a6d9fefbccd5db94addc | Cuarentena comercial multiempresa para rutas aún sin binding certificado. |
| backend/src/Infrastructure.Migrations/Migrations/20261013220000_CommercialTenantOwnershipGraphExpand.cs | 9170f552351d122ef7f74628e59f7c61aacdc28d | Migración histórica aditiva de EmpresaId nullable, no un corte a DB dedicada. |
| backend/src/Domain/Entities/UsuarioEmpresa.cs | f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c | Membresía tenant-aware por usuario y rol. |

**Límite:** ninguno de estos seis archivos demuestra por sí solo un router shared/dedicated completo, DSN dedicados ni backups/restores ejecutados. No inventar soluciones de Cloudflare/R2, Hostinger o Aiven dedicadas sin evidencia autorizada.

## Casos de aceptación técnica para implementación futura

| ID | Tipo | Escenario/estímulo | Resultado exigible y evidencia futura |
| --- | --- | --- | --- |
| AC01 | POS | Empresa A autorizada tiene placement shared registrado por backend | A enruta exclusivamente a la conexión aprobada; test router + consulta MySQL descartable. |
| AC02 | POS | Empresa B autorizada tiene placement dedicated registrado | B usa sólo destino dedicado configurado server-side, sin leer A; test resolver + A/B. |
| AC03 | POS | Actor autorizado lee recurso A con StorageTenantContext de A | Prefijo/owner A y control de contexto coinciden; caso .NET y lectura aislada. |
| AC04 | POS | Migración A en entorno descartable con snapshot verificado | Counts/checksums de A iguales en destino; B permanece inalterada; report y restore test. |
| AC05 | POS | Se reanuda migración A interrumpida con checkpoint válido | No duplica filas/eventos ni cambia placement a mitad; test idempotente. |
| AC06 | POS | Corte de A completado y health de destino correcto | Switch con versionado/fencing y lectura consistente; logs sin DSN/PII. |
| AC07 | NEG | Cliente envia DSN de B o placement forzado en body/header | Backend ignora/rechaza; ruta A nunca conecta a B; test HTTP y router. |
| AC08 | NEG | Empresa B maliciosa intenta consultar objeto con prefijo A | Rechazo antes de leer; no exponer metadatos ni payload de A; test almacenamiento. |
| AC09 | NEG | Placement desconocido, ausente, corrupto o duplicado | Fail-closed sin fallback a DB default de otro tenant; test configuración. |
| AC10 | NEG | Se revoca membresía A durante operación de storage | Control server-side rechaza/revalida según atomicidad definida; test revocación. |
| AC11 | NEG | DSN/secret aparece en DTO, log, error o trace | CI/seguridad detecta exposición y rechaza; test negativo de redacción. |
| AC12 | NEG | Fallo de copia a mitad de migración A | Destino no se publica; rollback conserva origen, sin pérdida de B; restore descartable. |
| AC13 | NEG | Migración se reintenta con mismo token/ID | No doble escritura ni eventos, checkpoint idempotente; test EF/MySQL. |
| AC14 | NEG | Dual-write devuelve valores diferentes en shared/dedicated | No promover/cortar, registrar divergencia y revertir vía auditada; pruebas comparativas. |
| AC15 | NEG | Tabla legacy con EmpresaId null o FK no verificada se expone | Quarantena y backfill autorizado, nunca asignación arbitraria a A/B; pruebas DB. |
| AC16 | NEG | Caída dedicada de A intenta usar shared de B | No fallback cross-tenant, error seguro y rollback; test falla de conexión. |
| AC17 | NEG | Se cambia placement mientras hay request/transacción abierta | Fencing/versión estable por operación, no lectura cruzada; test concurrencia. |
| AC18 | NEG | Backup de A contiene filas/adjuntos B o checksum incorrecto | Detener/invalidar corte, reparar sólo en entorno autorizado; test A/B. |
| AC19 | NEG | Job MySQL requerido SKIPPED o test de SHA anterior se cita como PASS | Rechazar A3/A4; verificar check-runs/logs exact-SHA y tests realmente ejecutados. |
| AC20 | NEG | Un operador trata preparación M01 como autorización PROD | Denegar promoción sin gates y A5 independiente, backup y permiso del propietario. |

**Positivos = 6 y negativos = 14.** Un futuro workflow pertinente deberá comprobar las acciones reales, y no sólo compilar. La forma exacta de cada API, conexión y migración sigue PENDIENTE: esta matriz expresa un resultado futuro, no inventa clases ni endpoints implementados.

## Pruebas documentales de esta unidad y barreras de salida

P1: 20 criterios AC01–AC20 con 6 positivos y 14 negativos, cada uno con estímulo, respuesta exigible y evidencia concreta. P2: seis fuentes Git ancladas a blobs y arquitectura .NET 10 / MySQL Oracle/EF que debe revisarse antes de ejecutar. N1: ninguna entrada permite DSN, tenantId, host, JWT/rol sin autorización server-side. N2: ninguna frase afirma que hoy existe DB dedicada, router, migración, restore o E2E exitoso. N3: no declarar SKIPPED, verde heredado o mock como PASS causal. N4: sin modificación de QA, main, producción, secretos, DNS, proveedores, tareas, ramas ni PR.

A0 = revisión estática documental y ausencia de secretos. A1 = matriz P1/P2, N1–N4 y criterios falsables. A2 = aislamiento A/B, credenciales, permisos RBAC y autorización tenant, idempotencia y rollback explícitos; un administrador de plataforma no obtiene automáticamente propiedad ni autorización para storage de empresa ajena. A3 = contratos Git, Standard V5, DAG, PLAN_MAESTRO H/I, COLA E/G/O y BITACORA A:Z (26 campos); MySQL runtime NO_APLICA. A4 = commit fast-forward, SHA exacto, Scope Lock SUCCESS y readback. A5 = revisión independiente por otro TASK_ID; NO autoaprobar QA/main/PROD.

**Rollback de documentación:** revert aditivo auditado de documento y CHANGELOG con nueva revisión, nunca force-push ni modificar registros o ramas ajenos. **Revalidaciones:** al cerrar C00/C01 y C02-01, antes de M01.06/M02, ante cambio de proveedor/placement o ante autorización de migración. No crear una microtarea 2341.
