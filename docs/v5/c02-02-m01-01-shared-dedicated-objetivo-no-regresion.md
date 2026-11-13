# SOLQARYN V5 FINAL — C02-02.M01.01: objetivo y no-regresión del placement shared/dedicated

**Estado:** preparación DOCUMENTAL PRELIMINAR M01.01, no implementación ni migración. **Resultado futuro de C02-02:** definir almacenamiento compartido, dedicado, router server-side y ciclo de migración reversible. **Nunca** obtener DSN, credenciales o ubicación física de datos de URL, JWT, `EmpresaId`, header, usuario o navegador. Fuente: Plan V5 FINAL, Skill canónica, `AGENTS.md`, Scope Lock STRICT, código real `dev`, Sheet institucional (PLAN_MAESTRO, COLA, DAG_TECNICO, BITACORA A:Z).

## 1. Objetivo verificable y límites de confianza

- **Shared:** persistencia MySQL compartida con contexto de tenant validado en API y predicados estrictos por empresa/sucursal/recurso; ningún `IgnoreQueryFilters` sin scope explícito debe exponer otra empresa.
- **Dedicated:** posible almacenamiento separado por tenant, elegido exclusivamente desde un catálogo de placement **servidor**, autenticado y gobernado. Es un objetivo futuro, no una opción actualmente certificada o habilitada.
- **Selección de placement:** resolver identidad de usuario y tenant por membresía real y JWT verificado; buscar mapping tenant→ubicación en almacén controlado por servidor; nunca aceptar DSN ni nombre de base de datos del cliente.
- **Migración:** flujo controlado con snapshot/backup verificado, reconciliación de conteos/constraints, escrituras duales **sólo si su diseño se aprueba**, compatibilidad/ventana de reversión, integridad, trazabilidad, cifrado y bloqueo de escrituras ambiguas. Nada de esto se ejecuta en M01.01.
- **Fallback:** ante mapping ausente, destino caído, credencial vencida o tenant revocado, error seguro y no selección de base ajena ni fallback global a primera instancia.
- **Criterio de seguridad:** roles RBAC de empresa/plataforma separados, PII y secretos redactados, autorización en backend, no inferida por UI, host ni headers.
- **Cierre de esta unidad:** fijar objetivos, no-alcance, NR-01–NR-13 y fuentes reales; no afirmar conexión, router, migración ni test multi-base exitoso.

## 2. Código real observado y alcance de inferencia

| Archivo leído en dev | Blob SHA exacto | Observación verificable, sin sobrecertificación |
| --- | --- | --- |
| `backend/src/Infrastructure/Persistence/AppDbContext.cs` | `c4b32f6637f1b4426655bb0cd12af8dd9d229866` | `DbContext` actual y marcas de empresa autorizada; no acredita placement dedicado |
| `backend/src/Infrastructure/Persistence/TenantQueryBypassGuard.cs` | `2ba91bab0420f7e0fc840cf9c96e2b974838694d` | Guarda de lecturas por `AuthorizedReadEmpresaId`; faltan pruebas de todos los consumidores |
| `backend/src/Infrastructure/Services/UsuarioScopeService.cs` | `14f4acbc5e1dc6a3ed1cf3485d54c7f1121b0e6f` | Resolución de membresía tenant-aware; persistencia física no queda seleccionada por ella |
| `backend/src/API/Program.cs` | `01425cff56eb13e12022b1b81a2f322d17fd1fff` | `GetConnectionString("DefaultConnection")` y `UseMySQL` se componen en servidor: no existe evidencia aquí de router shared/dedicated universal |
| `backend/src/Domain/Entities/Empresa.cs` | `c44940df7c30983a4c23b93338e0cb9367962069` | Empresa identificable/activa, no implica datastore por empresa |
| `docs/v5/c02-01-m01-01-objetivo-no-regresion-tenant.md` | `1e25701c5f407178585a5769a186fb4578e57deb` | Objetivo documental anterior de host/JWT/membresía, no una implementación de routing |

El stack declarado es ASP.NET Core 10, EF Core 10 con proveedor Oracle MySQL, frontend Angular 22 y Aiven MySQL 8.4.11 según fuentes institucionales previas. Esta unidad **no abre conexiones a Aiven** ni instala SDK, emula tenant, consulta datos vivos o ejecuta migraciones. Ninguna URL o nombre de servidor se incorpora como secreto.

## 3. Criterios de no-regresión (tests FUTUROS, no ejecutados)

| Caso | Entrada / escenario | Resultado obligatorio en futura implementación |
| --- | --- | --- |
| NR-01 (+) | Tenant A activo y membresía validada en topology shared | Conexión server-side autorizada y datos sólo de A |
| NR-02 (+) | Tenant B con mapping dedicado válido y secreto en vault | Ruta B al destino B, sin exponer DSN al navegador |
| NR-03 (+) | Migración autorizada A shared→dedicated con snapshot íntegro | Reconciliar tablas/filas/índices, no perder commits ni audit trail |
| NR-04 (-) | Usuario A cambia `EmpresaId`/header/host a B | No acceder ni leer datos B, aunque conozca la ruta física |
| NR-05 (-) | Cliente entrega DSN arbitrario o nombre de base | Rechazo; el servidor ignora cualquier placement no confiable |
| NR-06 (-) | Mapping de placement falta o tiene versión incompatible | Fail-closed; sin fallback automático a primera base |
| NR-07 (-) | Membresía revocada después del JWT | Denegar antes de conexión, lectura o escritura |
| NR-08 (-) | Usuario empresarial solicita cambiar mapping/global de tenant | RBAC niega sin privilegio plataforma explícito |
| NR-09 (-) | `IgnoreQueryFilters` o query raw ignora scope autorizado | Bloquear / test A-B real y sin fuga de PII |
| NR-10 (-) | Error de conexión al dedicado B durante escritura | Rollback transaccional, sin desviar escritura a shared A |
| NR-11 (-) | Snapshot/restauración pierde FK, índices o contador de filas | No cortar tráfico al destino; reconciliar y abortar promoción |
| NR-12 (-) | Reintento de migración/evento duplicado | Idempotencia, un solo estado observable, sin duplicar escrituras |
| NR-13 (-) | Log, mensaje de error o evento muestra JWT/DSN/contraseña | Redactar secreto/PII; auditar sin exponer credenciales |

**Clasificación de evidencia:** la matriz propone tres positivos y diez negativos verificables con MySQL/CI real **cuando se implemente** el router/migración; en M01.01 se revisa sólo el documento, no se declara haberlos corrido. Los compromisos reales de migración, infraestructura, secrets y producción necesitan autorización adicional.

## 4. Dependencias institucionales y alcance

DAG_TECNICO F=`VALIDADO`, G=`SI` para **preparación documental** independiente M01.01–05, con 18 prerequisitos: C00-01.M02.01–06; C00-03.M01.01–05; C01-01.M01.01–05; C02-01.M01.01–02, todos verificados LISTO al reclamar. No liberar M01.06/M02 ni infraestructura por la mera preparación. Si algún predecessor cambia por A5_FAIL/rollback o se reclaman scopes solapados, revalidar y fallar cerrado.

**No-alcance:** no se altera `AppDbContext`, entidades, migraciones EF, servicios, controllers, Angular, workflows de CI, variables secretas, proveedores, Aiven, Render, Vercel, DNS, QA/main/PROD, reglas de ramas ni roles. No se implementa tenant DB router ni conexión nueva; no se solicita ni cobra infraestructura. El impacto futuro se limita a decisiones documentadas de API/DB/routing/transacciones/observabilidad y sus consumidores.

## 5. Puertas A0–A5 proporcionales de esta unidad

- **TIPO1 documental:** `requiere_dotnet=NO`, `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO`. No hay código/migración que ejecutar.
- `requiere_security=SI_DOCUMENTAL`: revisar aislamiento A/B, JWT/RBAC, tenant/secretos, logs y fallback.
- `requiere_scope_lock=SI`: commit documental directo `dev` debe pasar `verify-solqaryn-project-scope` sobre SHA exacto.
- **A0:** revisar diff de archivo nuevo + CHANGELOG aditivo, secretos y referencias.
- **A1:** validar objetivos, contrato y 13 casos NR positivos/negativos propuestos.
- **A2:** revisar límites de autoridad, DSN no confiable, denial, rollback y exposición PII, no test runtime.
- **A3:** cotejar seis blobs fuentes, 18 dependencias DAG, plan y arquitectura, sin crear rutas de integración ficticias.
- **A4:** publicar fast-forward dev, comprobar SHA exacto, Scope Lock completed/success, cambios, rollback y readback de PLAN/COLA/BITACORA 26 campos.
- **A5:** otra Task realiza revisión independiente; registrar A5_PENDIENTE antes de LISTO_DEV. No afirmar A5_PASS ni promocionar QA/main/PROD.

**Rollback documental:** `git revert <SHA_DE_ESTA_ENTREGA>` normal tras revisar HEAD y trabajo concurrente, sin reset ni force-push. Las imágenes históricas inexistentes no requieren migración (`NO_APLICA`).
