# SOLQARYN V5 FINAL — C02-02.M01.02: actores, entradas, salidas y superficies shared/dedicated

**Clasificación:** TIPO 1, preparación documental preliminar M01.02. **Resultado futuro de la fase C02-02:** definir almacenamiento compartido, dedicado, selección server-side y migración reversible. Esta microtarea identifica actores, entradas, salidas y superficies afectadas; **NO implementa**, habilita, migra ni certifica un router dedicado. Fuente ejecutable: Standard V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA institucional (Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`) y código de `solqaryn/Solqaryn@dev`. Elaborado por PRIMARY DEV 03 — JAVIER; Task física `6adc2e4bd8a48191ac7d8641ed95a1a3`.

## 1. Actores y límites de autoridad

| Actor/componente | Entrada permitida | Salida/efecto futuro bajo verificación | Denegación obligatoria |
| --- | --- | --- | --- |
| Visitante anónimo | Host y recurso público publicado | Sólo datos públicos expresamente habilitados | No exponer DSN, identidad privada, empresa ni mapping de conexiones |
| Usuario autenticado | JWT válido más solicitud de empresa | Principal y membresía activa verificados por backend | JWT no concede acceso universal a cualquier EmpresaId |
| Miembro de empresa A/B | Empresa seleccionada y capacidad efectiva por pertenencia | Contexto tenant aislado de A o B | Cabecera, ruta, cookie, ID o cambio de host nunca concede acceso a otra empresa |
| Administrador de empresa | Capacidad RBAC de su empresa | Gestión sólo de recursos y configuración empresarial autorizados | No modificar placement global, claves ni DB de otras empresas |
| Operador de plataforma | Grant explícito de plataforma, identidad fuerte y trazabilidad | Proponer/autorizar placement y migración mediante control separado | La categoría empresarial o un claim suelto no habilitan operaciones de plataforma |
| Router de almacenamiento (objetivo FUTURO) | Contexto tenant server-side validado + catálogo versionado protegido | Elegir un destino permitido por mapping de empresa sin devolver DSN al cliente | Mapping ausente, ambiguo o revocado debe fallar cerrado, sin fallback cruzado |
| Worker de migración autorizado (objetivo FUTURO) | Orden autorizada, checkpoint, fuente/destino verificados, exclusión por tenant | Copia/reconciliación y corte reversible tras pruebas | No aceptar tenant, DSN o destino arbitrario desde payload de cola |
| Revisor independiente | SHA publicado y evidencia A0–A4 de otro TASK_ID | A5_PASS/FAIL con registro de 26 campos | No autocertificar ni promover QA/main/PROD |

Los actores funcionales, la cuenta GitHub y la identidad de la Task son principales distintos: nunca confundir identidad de ejecución con autorización comercial.

## 2. Matriz de entradas, salidas, consumidores y superficies

| Contrato | Entrada y autoridad | Salida/consumidor previsto | Superficie/validación pendiente |
| --- | --- | --- | --- |
| IO-01 | JWT validado por API + usuario activo | Principal server-side | Firma, expiración, audiencia, revocación y no aceptar EmpresaId como autoridad |
| IO-02 | Selección de empresa + UsuarioEmpresa activa | ContextoTenantActual y rol efectivo | Comprobar membresía contra MySQL, no reutilizar rol de empresa anterior |
| IO-03 | Contexto tenant validado | StorageTenantContext y prefijo lógico `empresas/{EmpresaId}` | No formar scope físico a partir de URL/path arbitrarios |
| IO-04 | Empresa autorizada + catálogo de placement futuro | Descriptor interno shared/dedicated versionado | Catálogo sólo servidor; estado, vigencia y unicidad de mapping |
| IO-05 | Descriptor autorizado de placement | Referencia interna a secreto/credencial de conexión | Nunca retornar DSN/password ni permitir selección por body/header |
| IO-06 | Descriptor y contexto de solicitud congelados | DbContext/connection factory futura | No cambiar destino dentro de request/transacción; evitar cross-tenant y fallback |
| IO-07 | Recurso con propietario empresa/sucursal | Repositorio EF, filtros, FK y operaciones | Mantener scope también en dedicated; auditar IgnoreQueryFilters/raw SQL |
| IO-08 | Contexto tenant + objeto autorizado | Adaptador de archivos/Cloudinary u otro proveedor existente | Prefijo y permisos por empresa; ningún bucket/tenant derivado del cliente |
| IO-09 | Orden de migración con autorización de plataforma | Plan, snapshot, job y checkpoint trazables | Control de concurrencia, idempotencia, cifrado y rollback antes del corte |
| IO-10 | Reconciliación fuente/destino (futuro) | Conteos, FK, checksums, evidencias de corte | Nunca promover si pierde filas, adjuntos, índices o transacciones |
| IO-11 | Error, denegación o caída de destino | Respuesta segura y auditoría redacted | No incluir DSN/JWT/PII ni enrutar a DB ajena ante fallo |
| IO-12 | Cambio de placement versionado, evento y decisión | Observabilidad/rollback/control de plataforma | Correlación y owner explícitos; no disparar despliegue o cambios de secretos por esta M01.02 |

Las rutas y tipos futuros son **contratos de diseño**, no clases, endpoints, jobs, bases dedicadas ni integraciones ya desplegados. Los códigos HTTP y APIs concretos se verificarán cuando exista implementación.

## 3. Contraste contra código observado en dev (referencias de lectura, no certificación live)

| Fuente | SHA blob Git | Alcance visible y limitación |
| --- | --- | --- |
| `backend/src/Application/Interfaces/StorageTenantContext.cs` | `716e8e205502d6300f555947c79edc2852fd63ba` | Contexto de almacenamiento desde ContextoTenantActual, prefijo tenant; NO router de bases |
| `backend/src/Domain/Security/ContextoTenantActual.cs` | `7377d2d752985cc9baf31d08280f38ad94a6a114` | Contexto desde membresía real; NO ubicación física |
| `backend/src/Infrastructure/Persistence/AppDbContext.cs` | `c4b32f6637f1b4426655bb0cd12af8dd9d229866` | Persistencia EF y controles de scope; NO dedicated por empresa |
| `backend/src/Infrastructure/Services/UsuarioScopeService.cs` | `14f4acbc5e1dc6a3ed1cf3485d54c7f1121b0e6f` | Membresía vigente desde MySQL; NO gestión de catálogo de placement |
| `backend/src/API/Program.cs` | `01425cff56eb13e12022b1b81a2f322d17fd1fff` | Composition root con connection string servidor y UseMySQL; NO router universal |
| `backend/src/Domain/Entities/UsuarioEmpresa.cs` | `f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c` | Relación usuario/empresa/rol; NO concede capacidad de mover bases |

No se han ejecutado pruebas de tenant A/B reales, MySQL dedicado, migraciones, restore, conexiones a Aiven ni cambios de seguridad. Un SHA de blob prueba identidad de archivo, no cobertura de integración.

## 4. Casos dirigidos: positivos y negativos FUTUROS

| ID | Tipo | Estímulo | Resultado y prueba exigibles en implementación posterior |
| --- | --- | --- | --- |
| P01 | POS | Miembro A activo con placement shared autorizado | Contexto A, conexión servidor autorizada y datos de A solamente |
| P02 | POS | Miembro B activo con placement dedicated registrado | Se selecciona sólo destino B sin exponer cadena a navegador ni logs |
| P03 | POS | Operador plataforma con grant solicita migración A en entorno descartable | Job autorizado con snapshot y checkpoints, reconciliación y ruta de rollback |
| P04 | POS | Worker reintenta checkpoint validado | Un único resultado durable, sin duplicar efectos |
| N01 | NEG | Cliente envía EmpresaId B/DSN/host/path en solicitud de A | Rechazo/ignorar entrada no confiable; sin acceso B |
| N02 | NEG | JWT válido pero membresía de A revocada | Denegar antes de obtener conexión o recurso |
| N03 | NEG | Administrador empresarial intenta editar placement de B | RBAC rechaza privilegio global |
| N04 | NEG | Mapping de B ausente, corrupto o revocado | Fail-closed sin fallback a shared o base de A |
| N05 | NEG | Conexión dedicada cae durante transacción | Rollback; nunca escribir en destino sustituto ajeno |
| N06 | NEG | Contexto tenant cambia mientras request/job continúa | Versión/fencing estable; ningún cruce A/B |
| N07 | NEG | Query EF sin filtro o SQL raw ve filas de empresa ajena | Denegación o prueba de aislamiento A/B, no asumir protección |
| N08 | NEG | Restore/migración pierde FK, fila, checksum o adjunto | No cortar ni promover; reconciliar o abortar con rollback |
| N09 | NEG | Job/mensaje contiene DSN, JWT, contraseña o PII | Redacción comprobable; sin secreto en logs, DTO o error |
| N10 | NEG | Repetición de mensaje/corte con mismo ID | Idempotencia, sin duplicados o doble escritura |
| N11 | NEG | Cambia host o cookie de A a B tras respuesta tardía | Revalidación de tenant y descarte de respuesta stale |
| N12 | NEG | Auditoría A5 por mismo TASK_ID o evento incompleto | Rechazar; BITACORA A:Z requiere EVENT_ID/IDEMPOTENCY_KEY únicos |
| N13 | NEG | Se cita job SKIPPED/CI de SHA anterior | No certificar A1–A4 ni Scope Lock por evidencia ajena |
| N14 | NEG | Se usa este documento M01.02 para liberar M01.06/M02 o PROD | Denegar: puertas de fase, pruebas reales y autorización aparte |

**P=4; N=14.** Estos escenarios son criterios comprobables para futuras tareas ejecutables, **NO** pruebas runtime ya ejecutadas.

## 5. Alcance y DAG técnico

`DAG_TECNICO` F=`VALIDADO`, G=`SI` para C02-02.M01.02, únicamente preparación documental. Requiere 18 prerequisitos: C00-01.M02.01–06 (6), C00-03.M01.01–05 (5), C01-01.M01.01–05 (5), C02-01.M01.01–02 (2), sujetos a verificación de PLAN_MAESTRO H/I y evidencia. COLA G=SI y O=SI sin dueño antes de reclamar. El inicio o VALIDANDO de otras unidades no sustituye cierre real de C02-02.M01.06/M02 ni autoriza migrar. Ante A5_FAIL, rollback o pérdida de premisa, revalidar y bloquear el avance dependiente.

**No alcance de M01.02:** cero cambios de .NET, API/DTO ejecutables, Angular, JWT, RBAC, EF, MySQL, Docker, E2E, CI, Cloudinary, Aiven/Render/Vercel, infraestructura, secretos, datos o licencias. Inventario de imágenes históricas vacío: migración NO_APLICA. No crear Tasks, ramas, PR ni editar qa/main/PROD.

## 6. Auditorías y salida por SHA exacto

**Clasificación:** `requiere_dotnet=NO`; `requiere_node_angular=NO`; `requiere_mysql=NO`; `requiere_docker=NO`; `requiere_e2e=NO` porque sólo se añaden documento y CHANGELOG; `requiere_security=SI_DOCUMENTAL` para JWT/RBAC/tenancy A/B, DSN y PII; `requiere_scope_lock=SI` para commit documental.

- A0: diff limpio, fuente oficial, contenido/no-alcance, ausencia de secretos, ninguna dependencia nueva.
- A1: matrices de ocho actores, doce IO y cuatro positivos/catorce negativos especificados; comprobar recuentos y que no se fingen tests.
- A2: revisar denegación de cliente/DSN/host/JWT, fail-closed, roles y segregación de tenants.
- A3: seis archivos Git y sus blob SHA, DAG 18 prerequisitos, PLAN/COLA, BITACORA 26 campos; pruebas técnicas no aplicables al diff documental.
- A4: sólo tras commit fast-forward `dev`, verificar SHA exacto, diff, blob, Scope Lock SUCCESS y readback de PLAN_MAESTRO/COLA/BITACORA; otros jobs ajenos no son PASS ni esperas obligatorias.
- A5: otro TASK_ID independiente verifica commit y casos positivos/negativos; `A5_PENDIENTE` puede coexistir con LISTO_DEV; no existe autorización implícita a QA/main/PROD.

**Rollback:** git revert normal aditivo del commit documental después de verificar HEAD y trabajo concurrente; jamás force-push, reset ni borrar historia. Revalidar contratos al cambiar selección de tenant, datos, migración o al alcanzar el cierre C02-01/C00/C01.
