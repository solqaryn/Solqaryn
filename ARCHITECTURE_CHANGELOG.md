## 2026-10-22 — Preparación de control de automatizaciones sin nuevos runtimes\n\nSe formaliza la frontera normativa futura en `docs/AUTOMATION_AUTHORITY.md`, manteniendo el control efectivo en `docs/AUTOMATION_STATUS.md`, las identidades humanas paritarias Javier 01 y Alex 02 y propiedad institucional separada. La microfase 1 no agrega infraestructura ni cambia el backend, frontend, tenancy, DB, deployment ni el ERP; cero Tasks/cron/ejecutores nuevos. Se añade un gate negativo de contrato al Project Scope Lock. Revertir por PR sin debilitar los controles actuales.\n\n---\n\n## 2026-10-22 — Migración documental nativa (autoridad comercial vigente)

- Fuente comercial aprobada: [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), Google Docs nativo, 492 unidades y 1.123 microtareas con texto y estructura certificados; sustituye el ID Word histórico `1T41ppLvC3x-p6sORCtpaPvzLWPP9_Z7j`.
- Seguimiento documental: [STANDARD DE AUTOMATIZACIONES EN SOLQARYN](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), 1.123 tareas en PENDIENTE, `PLAN_MAESTRO` editable, `COLA` calculada y `BITACORA` inicial vacía, sin ejecución automática.
- [STANDARD AUTORIDAD OPERATIVA VAEP](https://docs.google.com/document/d/1A525NGt09mBiZez_r6hFcdCYAdoOAwOZM--N0uXPBOQ/edit) conserva el contrato como referencia histórica de las diez Tasks retiradas; no habilita leases, cron ni despachos.
- Los enlaces e IDs que aparecen en secciones anteriores al corte son evidencia histórica, no referencias vigentes. El ID nuevo rige para validaciones de autoridad; la versión V3 y las 1.123 descripciones se conservan íntegramente.

---

## 2026-10-22 — Estado vigente después del retiro de automatizaciones

**ÚNICA AUTORIDAD OPERATIVA:** `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, `docs/AUTOMATION_STATUS.md` y el Plan Comercial Inicial Unificado. El antiguo motor VAEP de diez Tasks fue retirado. No existen supervisores, cron de despacho, epochs, leases ni manifest de coordinación autorizados. El ruleset exige solamente `verify-solqaryn-project-scope`, `SOLQARYN - Seguridad multiempresa` y `SOLQARYN - Build y pruebas backend`.

**El trabajo comercial nuevo no hereda fases, gates ni prioridades de planes anteriores.** Los apartados cronológicos anteriores al 22-10-2026 se conservan como antecedentes técnicos auditables, pero no autorizan tareas, despachos, escritura en Google Sheets, integración ni promoción a QA/PROD. El código de seguridad, compilación, base de datos, frontend y las pruebas funcionales reales del ERP permanece vigente según su impacto técnico.

---

## 2026-10-21 — Retiro propuesto del CAS DeveloperMetadata en VAEP DEV

Se elimina del arbol propuesto el constructor CAS dual y sus unitarios; CI deja de importarlo y falla ante su reintroduccion. La nueva ruta candidata es `logical_lease_dispatch.py` con `addNamedRange` unico atomico y readback, para un runtime exclusivamente de ChatGPT Tasks. Superficies: GitHub CI/AGENTS/VAEP_AUTHORITY y selector READ-ONLY; no hay cambios en MySQL, Angular, Render, Vercel, schemas, propietarios, eventos comerciales ni schedules. No se eliminan los DeveloperMetadata reales del Sheet. Verificar terminalidad autenticada, ausencia de locks incompatibles, cohorte 10/10 con derechos efectivos y GO antes de cualquier ejecucion comercial. Rollback: revert del PR sin alterar Sheets; queda PRE-GO no ejecutable.

## 2026-10-19 — DECISIÓN DEL PROPIETARIO: VERSIÓN COMERCIAL INICIAL PRIMERO

La única fuente **ejecutable en esta etapa** es [SOLQARYN PLAN COMERCIAL INICIAL UNIFICADO](https://drive.google.com/file/d/1T41ppLvC3x-p6sORCtpaPvzLWPP9_Z7j/view), ID `1T41ppLvC3x-p6sORCtpaPvzLWPP9_Z7j`. Su descomposición operativa de 492 fichas a 1123 microtareas está en el [control VAEP comercial](https://docs.google.com/spreadsheets/d/1gcVyCoyhLU0jFMwRtf0s5_x8FSnfBs38ojml1QF7Xwk/edit), pestaña `PLAN_COMERCIAL`. El antiguo Plan Maestro (ID `1YdQlNJ312HuziyKb9E-GEt55dcgSmuFGgfsHxyzPUaw`) se conserva como **visión futura no ejecutable**, sin borrar su historia. Esta decisión expresa del propietario reemplaza referencias antiguas de autoridad. Toda automatización debe comprobar que sus pares, su prompt, el contrato y la cola coinciden antes de activarse; mientras falte ese readback debe permanecer inactiva. No afecta controles de seguridad, CI, aislamiento A/B, backups ni prohibición de promover PROD sin autorización nueva.

## 2026-10-19 — Corrección 2: alta empresarial con administrador nuevo (candidato DEV)

- **Alcance:** sólo `POST /plataforma/empresas/alta-controlada`. Se añade la modalidad opcional `AdministradorNuevo { NombreUsuario, NombreCompleto, Password }`, mutuamente excluyente con `AdministradorUsuarioId` existente, manteniendo compatibilidad total con este último. UI global ofrece ambas opciones.
- **Persistencia:** usuario nuevo con BCrypt coste 12 y rol empresarial base NO administrador, empresa, `ConfigEmpresa`, `UsuarioEmpresa` con plantilla ADMIN Empresa y `RegistroAuditoria` de plataforma comparten una transacción/commit MySQL. No se insertan grants `UsuarioRolPlataforma`; ni respuesta ni auditoría incluyen contraseñas o hashes.
- **Invariantes:** requiere JWT y `PlataformaScopeService` live MySQL, más `MultiEmpresa:CommercialIsolationCertified` y `MultiEmpresa:PlatformOnboardingEnabled`; ambos OFF por defecto. Rol de sistema empresa activo/no borrado; denegación de credenciales débiles o usuario duplicado; no actividad comercial extra ni tienda publicada.
- **Pruebas y readback pendientes de certificación:** cuentas previas sin regresión, nueva cuenta protegida, rol empresarial sin autoridad global, dos flags, conflicto y duplicidad, inyección de fallas ConfigEmpresa/membresía/auditoría y rollback íntegro de Usuario/Empresa/ConfigEmpresa/membresía/auditoría en MySQL real; CI exact-head y readback DEV sin escrituras productivas. No se modifican QA/PROD, migraciones, esquema, banderas ni secretos.
- **Rollback:** revert del PR; no revertir ni borrar cuentas/empresas legítimas existentes. Estado `LISTO` exclusivamente con REVIEW_FIRST, CI, P0/P1=0 y evidencia causal de DEV.

## 2026-10-19 — Configuración pública tenant-safe por vinculación HMAC (candidato DEV)

- Nueva lectura estricta `GET /tienda/configuracion` para devolver exclusivamente nombre, logo y moneda de `Empresa` / `ConfigEmpresa` que corresponden al `EmpresaId` fijado por `VerifiedPublicStoreTenant`. No se lee la configuración singleton `EmpresaConfiguracion` de otra empresa ni secretos administrativos.
- `Host + StoreKey` provienen únicamente del proxy Vercel HMAC v2; Render valida método, ruta, timestamp, firma, host allowlisted, registro MySQL y empresa activa, y consume replay. `/empresa-configuracion/publica` permanece en cuarentena cuando no hay contexto inequívoco.
- Sólo se agrega la ruta de identidad al conjunto exacto de lecturas permitidas para tiendas secundarias; productos, checkout, cuentas, operaciones privadas y jobs conservan bloqueo. El proxy elimina cabeceras, JWT y cookies del cliente.
- La puerta Angular `/tiendas/:storeKey` muestra únicamente la identidad autorizada y nunca cae en otra empresa. El storefront legacy consulta identidad firmada si falla su bootstrap; SEO sólo obtiene metadata tras prueba HMAC (si falla, `noindex` y 503). No se alteran datos, tablas, banderas ni QA/PROD.
- Gate de cierre: revisión independiente, pruebas de falsificación/replay/suspensión A/B, regresiones, CI exact-head, readback real DEV y P0/P1=0. Rollback: revert de este PR, conservando la vinculación HMAC original.

## 2026-10-18 — DEV Microfase roles empresariales: ownership estricto (candidato)

- El control RBAC asigna `EmpresaId` desde el contexto empresarial validado por el backend a cada rol personalizado creado o duplicado; el cliente nunca controla ese propietario.
- Solo se aceptan por membresía roles personalizados del mismo tenant o plantillas `Ambito=Empresa, EsSistema=true, EmpresaId=NULL`. `RolService`, `UsuarioService`, `UsuarioScopeService`, filtros EF y permisos rechazan roles de B/plataforma y legacy ambiguos desde A.
- Las plantillas compartidas quedan de solo lectura para cambios de rol y matriz de permisos desde la administración de tenant; rol personalizado sin owner no puede modificarse ni conceder grants.
- Migración Oracle `20261018200000_TenantRoleOwnershipDeterministicBackfill`: atribuye roles anteriores solo cuando toda la instalación tiene UNA empresa (incluidas suspendidas); con A/B no infiere dueño ni desde membresías potencialmente manipuladas. No elimina filas. Los ambiguos conservan `EmpresaId=NULL` y no confieren RBAC.
- Pruebas unitarias, EF y MySQL efímero (incluidos replay de rol cruzado/plantilla/orfandad). La migración se valida sobre commit y MySQL temporal; no ejecutar en QA/PROD ni alterar banderas multiempresa.
- Rollback seguro: revertir cambios de aplicación solo con cuarentena de roles y operación A/B vigente; no deshacer ownership desde migración sin backup verificado. PENDIENTE REVIEW_FIRST, CI exact-head, readback DEV y P0/P1=0 antes de LISTO.

## 2026-10-16 — Microfase 1 DEV: anti-replay de firma HMAC v2 (candidato)

- El proxy genera un nonce aleatorio de 128 bits por solicitud; HMAC v2 firma nonce, timestamp, metodo, ruta, Host y StoreKey. Se rechazan v1, cabeceras duplicadas, nonces invalidos y expirados.
- Render consume atomica y una sola vez la firma validada mediante INSERT en PublicStoreProofReplays con PRIMARY KEY BINARY(32) compartida entre instancias MySQL; rechazo seguro si se repite, si falla la DB o si falta su migracion. Purgado acotado tras 180 segundos.
- Roll-forward con migracion 20261016200000_PublicStoreProofReplayGuard; no toca datos comerciales ni modifica la tienda legacy con flag OFF. No activa nuevas tiendas, QA ni PROD.
- Pruebas Node y .NET + MySQL efimero concurrente; confirmar CI exact-head antes del merge. Certificacion HMAC real y host/empresa DEV depende de readback independiente, no asumir LISTO solo por CI.
- Rollback funcional: mantener flags multitienda OFF, sin borrar empresas o bindings. La tabla anti-replay solo guarda hashes temporales.

## 2026-10-16 — Candidato DEV: recuperar navegación autenticada global/empresa sin relajar RBAC

- Incidente operativo observado en DEV: login y consulta de `tenant-context/1` y permisos devolvían HTTP 200 en Render, pero Angular mantenía la pantalla de selección y revalidaba por duplicado. Cambio localizado en frontend; no altera tablas, flags comerciales ni datos empresariales.
- `TenantContextService` comparte la solicitud concurrente por el mismo `EmpresaId`, impide que resultados asincrónicos obsoletos sobrescriban el contexto y acota la espera a 15 segundos con denegación segura; no usa localStorage como fuente de autoridad.
- El interceptor deja de enviar `X-Empresa-Id` al control-plane `/plataforma/*` y `/auth/*`; conserva JWT y marca tenant solicitada para rutas empresariales que vuelven a validarse en el servidor.
- Navegación explícita en el shell autenticado hacia `/plataforma` y botón `Cambiar empresa`, que revoca el contexto tenant local antes de volver al gate. Ninguna pantalla concede acceso global sin `GET /plataforma/sesion` válido contra MySQL.
- Validaciones obligatorias antes del cierre: unitarios de peticiones concurrentes/obsoletas, cabeceras y cambio de contexto; frontend build, autorización global denegada a usuario empresarial, CI exact-head, smoke con sesión real en DEV y recuperación de navegación. Fallos de membresía siguen denegados sin fallback.
- Candidato de reparación separado del PR #3658 (ya integrado); rollback revertible de frontend, sin tocar Render/Vercel/QA/PROD hasta aprobar la CI; no repetir migración ni pruebas de esquema.

## 2026-10-16 — Candidato PR #3658: alta empresarial preparatoria y registro persistente de tiendas (VALIDANDO)

- Alcance **DEV únicamente** del registro histórico del PR #3658. Las referencias `SQ-032`, `SQ-035`, `SQ-036`, `SQ-038`, `SQ-041` y `SQ-048` se conservan exclusivamente como trazabilidad técnica: no son autoridad, dependencia, orden ni bloqueo de la cola comercial vigente. No activar la comercialización privada ni promover QA/PROD.
- Se introduce `PublicStoreBindings` con FK a `Empresas`, índice único exacto `Host+StoreKey` e índice por empresa/estado. La gestión por autoridad global verificable crea asociaciones desde la consola sin modificar variables por cliente.
- `POST /plataforma/empresas/borrador` crea empresas **inactivas** con auditoría y transacción, sin activar tiendas ni operaciones privadas; el alta comercial certificada existente conserva sus dos feature gates.
- La creación del primer borrador fija atómicamente `Host upstream legacy + StoreKey vacío → EmpresaId` del único tenant vigente; no se reelige dueño si cambia cuál empresa está activa. Con 2+ empresas, el storefront legacy exige esta asignación persistente exacta y que su propietaria continúe activa.
- Resolver público firmado HMAC consulta DB únicamente con `PublicStore:EnableMultiEmpresaBinding=true` y `PublicStore:EnableDatabaseBindings=true`. Por defecto OFF. Se mantiene validación activa de Empresa por solicitud y se rechazan rutas comerciales de tiendas secundarias.
- La consola puede preparar borradores y guardar/publicar asociaciones, siempre bajo el control global MySQL de la API; las claves HMAC permanecen sólo en infraestructura DEV. La capacidad de catálogo/checkout A/B continúa pendiente de sus propias microfases.
- Verificación obligatoria: migración exact-head, pruebas de JWT/RBAC/aislamiento A/B/legacy fijo, CI/revisión P0/P1=0, despliegue y smoke DEV; no declarar LISTO con pruebas sólo efímeras.
- Reversión operativa: mantener desactivadas las banderas públicas e impedir reactivación insegura; revert del PR sólo tras comprobar dependencia de la nueva tabla. No borrar registros reales con `Down` automático.

## 2026-10-16 — Tiendas públicas por empresa: Microfase 1/6 (MVP comercial, VALIDANDO)

- **Solo DEV**. Vinculación pública **Host + StoreKey -> EmpresaId** desde allowlist backend `PublicStore:HostBindings`, comprobación `Empresa.Activa` en MySQL en cada petición y prueba criptográfica HMAC del proxy Vercel; `X-Forwarded-Host`, `X-Empresa-Id`, `Origin` y cabeceras `X-Solqaryn-Store-*` del cliente no confieren autoridad.
- Vercel firma método, ruta interna, host allowlisted, StoreKey y timestamp (tolerancia 60 segundos) con secreto runtime; Render verifica firma en tiempo constante y configuración exacta. Las claves están exclusivamente en variables de despliegue y **NO** en el repositorio.
- **Límite estricto de la microfase**: `/tiendas/:storeKey` en Angular y `/api/tiendas/{storeKey}/tienda/binding` validan únicamente la asociación. Por diseño **no** habilitan catálogo, cuenta, carrito, checkout ni operaciones comerciales de nueva tienda; Vercel y Render rechazan incluso firmas válidas para esas rutas hasta las microfases 2/3. La tienda actual `/tienda` preserva modo legacy monoempresa mientras el feature gate siga OFF.
- No activar automáticamente `PublicStore:EnableMultiEmpresaBinding`, `PublicStore:RequireVerifiedProxyProof`, `SOLQARYN_PUBLIC_STORE_PROOF_ENABLED`, onboarding ni `MultiEmpresa` con datos/hosts no verificados. El registro host↔clave↔empresa y el secreto simétrico correcto deberán configurarse y verificarse explícitamente antes de publicar nuevas tiendas.
- **Criterios**: host/clave desconocidos denegados; ausencia/falsificación/duplicados/replay de firma denegados; A/B distinto; empresa inactiva denegada; misma API sin contexto comercial privado; rutas vecinas/checkout cerradas; compilación, pruebas Node, C#, MySQL efímero y CI exact-head PASS; REVIEW_FIRST, P0/P1=0. Rollback de código mediante revert del PR; rollback operativo desactivando los gates específicos, sin tocar empresa ni tienda existentes.
- **Estado documental:** PR #3657 en revisión, **NO LISTO** hasta CI exact-head, despliegues y smoke de DEV dentro de alcance. QA/PROD, `solqaryn.com`, Aiven, DDL, asignación de clientes y Plan Maestro final inalterados.

## 2026-10-15 — Microfase 4, SOLQARYN MVP comercial inicial (candidato DEV)

**Alcance aprobado:** versión inicial, pequeña, funcional y comercial; **NO ejecutar el Plan Maestro final**. La arquitectura avanzada de control plane, planes/billing, dominios, módulos de pago, data placement, autoservicio integral y automatizaciones SaaS se dejan para una fase futura, sin imponer sus entregables como gates al MVP.

- **Directorio protegido:** `GET /plataforma/empresas` devuelve únicamente identidad de empresas, estado, conteo real de cuentas vigentes y paginación acotada (1–50), incluso si la empresa A está inactiva. Sin leer productos, inventario, facturas, archivos ni otros datos comerciales; auditoría global `EmpresaId=NULL`, respuesta no-cache.
- **Gestión mínima:** nuevo `PlataformaGestionMvpController` permite consulta de plantillas y roles empresariales propios de cada tenant, búsqueda exacta de cuenta, alta de identidad INACTIVA sin rol global/membresía implícita (se activa solo al asociarse por primera vez a una empresa dentro de la transacción auditada), asignar usuarios/admins a una empresa, cambiar/activar/desactivar membresías, cambiar nombre y suspender/reactivar empresas. Se protege al último administrador activo; no permite usar roles de Plataforma ni de otra empresa. La interfaz Angular presenta estas capacidades con confirmación para acciones de suspensión y revocación.
- **Seguridad:** cada endpoint exige JWT y `IPlataformaScopeService.ObtenerActualAsync` desde MySQL en cada petición; middleware permite solo métodos y segmentos positivos exactos bajo `/plataforma/gestion`. La auditoría de mutaciones comparte transacción con MySQL; los privilegios empresariales no sustituyen autoridad de plataforma. Ante error o revocación, falla cerrado.
- **Alta de empresas:** reutiliza el endpoint existente `POST /plataforma/empresas/alta-controlada`, que sigue requiriendo dos banderas independientes `MultiEmpresa:CommercialIsolationCertified` y `MultiEmpresa:PlatformOnboardingEnabled`. La consola muestra la creación deshabilitada mientras los controles estén OFF. **No activar las banderas ni ampliar la operación comercial sin certificación y readback operativo propios.**
- **Pruebas:** unitarias API/Angular (empresa A inactiva, rol de B rechazado en A, último admin protegido, auditoría, usuarios y JWT denegados); suite HTTP real con MySQL 8.4 efímero y JWT [run 38842020830](https://github.com/solqaryn/Solqaryn/actions/runs/38842020830) **PASS**: acceso global, empresa inactiva, altas de usuarios, roles, membresías, revocación y aislamiento A/B. Se usan fixtures genéricos bajo el scope lock, no identidades de empresas retiradas.
- **Estado:** implementación en rama del PR #3656, todavía **VALIDANDO** hasta REVIEW_FIRST, CI exact-head integral, P0/P1=0 y despliegues Render/Vercel DEV. No declarar `LISTO` antes de readback y smoke real.
- **Integridad:** ningún cambio deliberado de DDL, Aiven, QA, PROD, dominio ni flags de onboarding. Rollback del PR no afecta a Microfases 1–3; operaciones administrativas futuras deberán contar con auditoría y reversión explícitas.

## 2026-10-15 — Microfase 3: puerta global de plataforma en frontend (candidato DEV)

- La ruta Angular `/plataforma` utiliza `plataformaGuard` independiente de `authGuard` empresarial. Exige autenticación JWT vigente y **consulta nueva en cada navegación** de `GET /plataforma/sesion`; no acepta `EmpresaId`, roles JWT legacy, permisos empresariales ni cache client-side como concesión de autoridad.
- `PlataformaSesionService` valida el contrato de la API (success, ámbito exactamente `Plataforma`, administrador global y nombres de rol/usuario presentes). Una denegación 403, token caducado, API caída o payload ambiguo limpia la sesión local y redirige al acceso con diagnóstico seguro. La consola no contiene datos empresariales ni permite mutaciones.
- El login mantiene intacto el selector tenant/RBAC para el ERP y agrega acceso explícito a la consola solo tras autenticar. El superadministrador sin membresía empresarial ya no necesita forzar un tenant para acceder a su plano de control. No se reutiliza el panel de `/empresas`, que pertenece al ERP.
- El panel declara el alta de empresas como **no habilitada operativamente**. Las dos banderas existentes `MultiEmpresa:CommercialIsolationCertified` y `MultiEmpresa:PlatformOnboardingEnabled` permanecen OFF; no hay escrituras en Aiven, creación de tenants, migraciones, nuevas APIs o cambios a Render/Vercel/QA/PROD.
- Pruebas Vitest HTTP frontend: grant global válido, rechazo administrador empresarial, contrato 200 ambiguo y revocación inmediata 403; persiste la suite backend JWT/MySQL de Microfase 2. Requiere revisión independiente, compilación y CI exact-head, smoke real DEV autenticado y P0/P1=0 antes de fusionar o declarar `LISTO`.
- Rollback: cerrar la ruta /plataforma con revert de este PR, preservando Microfases 1/2 y los flags backend OFF. Registro histórico: `SQ-030`, `SQ-050`, `SQ-041` y `SQ-095` se conservan sólo para trazabilidad del changeset; no crean scope, prioridad, dependencia ni pendiente ejecutable.

## 2026-10-15 — Microfase 2: autoridad de plataforma independiente de EmpresaId (DEV)

- GET `/plataforma/sesion` exige JWT firmado y vigente (`[Authorize]`), y obtiene autoridad GLOBAL a través de `IPlataformaScopeService` / `PlataformaScopeService` consultando en cada solicitud MySQL: `Usuarios` activo/desbloqueado/no eliminado, `UsuarioRolesPlataforma.Activa`, `Roles` activo/no eliminado/`Ambito=Plataforma`/`EsAdministrador`/`EmpresaId=NULL`. Nunca concede privilegios por claims `esAdministrador`, `rolId`, `Usuario.RolId`, `UsuarioEmpresa` o cabeceras `X-Empresa-Id`. Si existen múltiples grants activos o identidad JWT obsoleta: 403. La cuenta `javier` creada en la microfase 1 podrá consultar su propio ámbito global aun con todas las empresas inactivas.
- El middleware de aislamiento multiempresa exceptúa **solo GET exacto** de la sesión, sin modificar cuarentena comercial ni habilitar onboarding. `PlataformaEmpresasController` reutiliza el mismo servicio, pero su alta controlada sigue sujeta a los dos flags independientes existentes OFF por defecto.
- Auditoría crítica `RegistrosAuditoria` para autorización concedida/denegada, con `EmpresaId=NULL`, contexto de plataforma/origen `RolPlataforma` únicamente en éxitos, correlación de petición y persistencia obligatoria antes de respuesta. `Cache-Control: private, no-store`. No se crean roles/empresas, no se cambian datos comerciales, no se modifica schema ni JWT emitter, ni se agrega ruta administrativa global adicional.
- Pruebas dirigidas de servicio y middleware: empresa inactiva, administrador empresarial rechazado, estado de usuario/rol/membresía revocados, duplicidad de roles, JWT obsoleto, rutas vecinas bloqueadas. El workflow `p4-ab-real-auth-ephemeral.yml` ejecuta API HTTP con JWT real y MySQL 8.4 efímero, prueba plataforma sin membresía empresarial, spoof de cabecera, rechazo A/B, revocación en vivo y auditoría sin tenant. Rollback: revert del PR; bootstrap persistido de microfase 1 no se modifica. Certificación final: CI exact-head y P0/P1=0.

## 2026-10-15 — P4: edición de clientes/proveedores A/B con RBAC (candidato DEV)

- `MultiEmpresa:PrivateMasterEditPilotEnabled=false` preserva la cuarentena de las demás rutas y habilita, solo con opt-in, `PUT /clientes/{id:positivo}` y `PUT /proveedores/{id:positivo}` para dos empresas. Exige JWT, empresa/membresía activa verificada desde servidor, `Clientes.Editar` o `Proveedores.Editar`, segundo filtro MVC y repositorios EF con propietario de empresa.
- Rechaza ID no positivo, otros métodos/subrutas, payload distinto de JSON o vacío y cuerpos mayores de 32 KiB. Ninguna cabecera de empresa constituye autorización independiente. No cambia esquema MySQL, datos persistentes, dominios, Cloudinary, inventarios ni rutas financieras.
- `CommercialMasterEditPilotGuardTests` verifica matriz de bandera, permisos, rutas y A/B. El workflow `p4-ab-real-auth-ephemeral.yml` activa el flag SOLO en runner MySQL 8.4 efímero y prueba con dos JWT ediciones propias, rechazos cruzados y readback sin alteraciones. Cualquier activación en Render DEV requiere decisión controlada y comprobación de empresas/usuarios reales. ROLLBACK: flag OFF y revert; CI exact-head y P0/P1=0 obligatorios antes del merge.

## 2026-10-15 — P4: archivos de compra y actualización producto A/B (candidato DEV)

- La bandera estricta `MultiEmpresa:PrivateCommercialFilesPilotEnabled=false` en `UnscopedCommercialTenantGuardMiddleware` admite únicamente `GET /compras/{id:positivo}/documentos` (Compras.Ver), `GET /compras/{id:positivo}/documentos/{docId:positivo}/descargar` (Compras.Exportar), `POST /compras/{id:positivo}/documentos` y `DELETE /compras/{id:positivo}/documentos/{docId:positivo}` (Compras.Editar), y `PUT /productos/{id:positivo}` (Productos.Editar, incluido formulario con imágenes). JWT+empresa activa+membresía+permiso servidor, segundo filtro MVC, repositorios EF y `StorageTenantContextResolver` se mantienen obligatorios; los endpoints no usan cabeceras como autoridad.
- PUT producto requiere multipart/form-data <=15 MiB, máximo cinco archivos individuales <=10 MiB; POST comprobante exige 1 archivo y multipart <=12 MiB (validación física/MIME/magic bytes en `CompraDocumentoService`). GET/DELETE no inventan validaciones de form. Las subrutas e IDs falsos, POST a otros endpoints y el flag apagado siguen dando 503 en multiempresa.
- Nueva `CommercialFilesPilotGuardTests` verifica matriz A/B positiva/negativa, spoof, usuario lector/no autenticado, formato y conteos inválidos. Workflow MySQL 8.4 efímero con 2 JWT reales verifica GET de anexos propios, GET ajeno denegado, producto propia edición multipart sin archivos preservando cantidad/precio/costo/stock, y edición ajena rechazada. **La subida y recuperación física desde Cloudinary con dos empresas no se dan por certificadas** con credenciales ficticias; siguen requiriendo ejecución real sobre DEV autorizado.
- Sin DDL, nuevos secretos, configuración Render activa ni promoción QA/PROD. Requiere REVIEW_FIRST + CI causal exact-head, P0/P1=0. Rollback bandera OFF y revert. Otros caminos de archivos/ZIP permanecen en el alcance de los pilotos previos, sin duplicarlos.

### P4 2026-10-15 — Corrección causal de anulación cruzada

- El gate HTTP/MySQL A/B detectó `HTTP 500` al intentar anular una venta B desde el JWT A: `VentaService.AnularAsync` arrojaba `KeyNotFoundException` al no encontrar la venta bajo la autorización actual, y `ExceptionHandlingMiddleware` lo capturaba como error interno. `CompraService.AnularAsync` tenía la misma falla.
- Ambos métodos emiten ahora `ResourceNotFoundException` genérica, que el middleware existente devuelve como 404 sin revelar IDs ni existencia de recursos ajenos. No se toca el filtro de datos, permisos, stock, operaciones financieras ni la transacción. El test MySQL real exige rechazo 404/4xx (jamás 500 ni 2xx) y reconcilia inventario después de anulaciones propias.
- Sin cambios DB, credenciales, Render, QA o PROD; revisión exact-head y regresión requeridas antes de fusionar.

## 2026-10-15 — P4: pagos y anulaciones comerciales A/B por JWT/RBAC (candidato DEV)

- Nueva cuarentena selectiva `MultiEmpresa:PrivateFinancialPilotEnabled=false` por defecto en `UnscopedCommercialTenantGuardMiddleware`. Solo HTTP POST de `/facturas/{id:positivo}/pagos` (`Facturacion.Aplicar`), `/facturas/{id:positivo}/pagos/{pagoId:positivo}/anular` (`Facturacion.Anular`), `/ventas/{id:positivo}/anular` (`Ventas.Anular`) y `/compras/{id:positivo}/anular` (`Compras.Anular`). Ningún otro método, subruta, pago, actualización o envío público se habilita.
- Permiso comprobado desde `TenantPermissionContext.RequireEmpresaIdAsync`, membresía JWT+RBAC relacional, empresa activa y marca `VerifiedCommercialTenant` exigida; filtro MVC comprueba segunda autorización. Las operaciones de dominio existentes verifican dueño de factura/documento por repositorio EF, aplican reversión de stock y contabilidad con transacciones y guard de relaciones MySQL; no se duplican servicios ni se alteran reglas financieras. JSON obligatorio, body de 1 a 32 KiB, sin multipart/archivos. Límite de 2+ tenants mantiene `503` fuera de whitelist.
- `CommercialFinancialPilotGuardTests` comprueba A/B y spoofing, acciones RBAC exactas, IDs ASCII positivos, flags OFF, payloads no JSON/demasiado grandes, usuario anónimo/solo lectura y rutas prohibidas. `scripts/ci/p4-ab-real-auth.mjs` agrega pagos concurrentes con **dos JWT reales** contra MySQL 8.4 efímero, rechazos de pagos/void ajenos, anulación de cada pago, reversión de venta y stock, anulación de compras independientes sin movimientos posteriores y preservación de integridad A/B. `PrivateFinancialPilotEnabled=true` solamente en runner de CI, no en Render DEV/Aiven/QA/PROD.
- `REVIEW_FIRST`, CI exact-head post-cambio (backend/security/MySQL/E2E), P0/P1=0 y prueba de rechazo cruzado obligatorias para merge. No marca P4 integral: quedan Cloudinary real, ZIP físico A/B, jobs, despliegue y aceptación DEV live. Rollback: flag OFF y revert sin DDL ni secretos nuevos.

## 2026-10-15 - P4 exportaciones administrativas tenant A/B (DEV candidato)

- Rutas GET exactas de reportes administrativos habilitables con `PrivateAdminReportPilotEnabled=false` por defecto, membresia server-side y permisos `ReportesAdministrativos.Ver` / `ReportesAdministrativos.Exportar`.
- Reutiliza ReporteAdministrativoService y sus consultas por EmpresaId. Prueba CI con MySQL efimero y dos JWT reales: aislamiento de usuarios, XLSX, CSV y PDF. Falla cerrado para empresas ajenas, otras rutas, POST y flag OFF.
- No cambia esquemas, datos persistentes, flags Render ni QA/PROD. REVIEW_FIRST y CI exact-head requeridos. Rollback: flag OFF y revert.

## 2026-10-15 — Punto 4: descarga privada de imágenes y ZIP de producto A/B (candidato DEV)

- `UnscopedCommercialTenantGuardMiddleware` añade el flag `MultiEmpresa:PrivateMediaExportPilotEnabled=false` por defecto. Permite únicamente `GET /productos/{productoId:int>0}/imagenes/{imagenId:int>0}/descargar` y `GET /productos/{productoId:int>0}/imagenes/descargar-todas` con `Productos.Exportar` y JWT, membresía/empresa activa RBAC verificadas en servidor, `VerifiedCommercialTenant` y doble comprobación del filtro MVC. IDs decimales ASCII positivos, sin rutas similares ni subrutas/otros verbos.
- El controlador existente primero obtiene producto mediante `ProductoService.GetByIdAsync` (filtro físico EF por EmpresaId), busca `ProductoImagen` dentro del producto autorizado y llama `ResolverTenantDescargaAsync` que revalida `IUsuarioScopeService.ObtenerActualAsync` y construye `StorageTenantContext`; la capa de imágenes valida ruta Cloudinary del tenant y la exportación ZIP es atómica ante archivos faltantes. No se agregan usuarios, descargas públicas, cargas de archivos ni acceso a imagen de empresa ajena.
- Pruebas: matriz unitaria `CommercialMediaExportPilotGuardTests` para positivo A, spoof B, flag OFF, permisos faltantes, sin JWT, rutas/métodos erróneos. El workflow con dos JWT HTTP/MySQL 8.4 efímero prueba producto A/B propio/ajeno y 404 por galería vacía, sin servicios ni secretos Cloudinary. Una prueba Cloudinary REAL controlada y validación ZIP con bytes permanecen pendientes y **este candidato no cierra el Punto 4**.
- Sin cambios de DDL, Cloudinary, Render, Aiven, QA ni PROD. REVIEW_FIRST, CI exact-head con P0/P1=0 y verificación DEV real son obligatorios antes de activar flags; rollback: flag OFF y revert.

## 2026-10-15 — P4 integraciones de lectura PDF/Reportes/Inventario A/B (candidato DEV)

- `UnscopedCommercialTenantGuardMiddleware` incorpora tres opt-ins estrictos, TODOS false por defecto: `MultiEmpresa:PrivatePdfPilotEnabled` (exclusivamente GET `/facturas/{id:positivo}/pdf` con `Facturacion.Exportar`), `MultiEmpresa:PrivateReportPilotEnabled` (GET `/ventas/reportes/resumen`, `/ventas/reportes/detalle` con `Ventas.Ver`, GET `/compras/reportes/detalle` con `Compras.Ver`) y `MultiEmpresa:PrivateInventarioReadPilotEnabled` (GET `/existencias-variante`, `/existencias-variante/{id:positivo}` con `Inventario.Ver` y `/inventario/movimientos`, `/inventario/movimientos/paged` con `MovimientosInventario.Ver`). Se exigen JWT, membresía, empresa activa, RBAC server-side, marca tenant verificada y filtros MVC/EF; un nombre parecido o subruta adicional no habilita acceso.
- El camino privado de PDF usa `FacturaService.GetByIdAsync` que limita owner y `QuestPdfFacturaService.ValidarTenantProduccionAsync` que exige identidad y empresa desde el servidor. `FacturaService.ToDtoAsync` (del PR padre #3643) ya eliminó dependencia de textos/logo singleton ajenos. No se habilitan otros documentos, adjuntos ni ZIP.
- `CommercialReadIntegrationsPilotGuardTests` prueba matriz de rutas propias/ajenas, flags apagados, RBAC, PDF Exportar versus Ver, ID positivo, sin activar pagos, rutas de compartir ni exportaciones generales. En workflow CI MySQL 8.4 efímero con dos JWT reales se añade descarga A/B PDF válida (`%PDF-`, MIME, bytes), prohibición de PDF cruzado, reporte ventas filtrado por VentaId (A y B), GET reporte compras con source `OrdenCompra` vacío en fixture sin OrdenCompra, Kardex por producto propio y GET existencias. Los tres flags se activan SOLO EN RUNNER, no en Render.
- No hay migraciones, modificaciones Aiven/Cloudinary/SMTP/Render, ni QR, ZIP, pagos, anulación o jobs habilitados. Aprobación requiere REVIEW_FIRST, CI exact-head, MySQL E2E/security, P0/P1=0, regresión y smoke DEV antes de afirmar P4 integral. Rollback inmediato: flags OFF, revert y preservar cuarentena de demás rutas.

### Protección adicional P4 de factura/PDF: identidad visual del owner (2026-10-15)

- `FacturaService.ToDtoAsync` antes adjuntaba `EmpresaConfiguracion` singleton global a cualquier factura, incluyendo logo y textos legales de otra empresa. Para `Factura.EmpresaId>0` ahora consulta exclusivamente `IEmpresaRepository.GetByIdAsync(empresaId persistido)` y utiliza sólo el logo del tenant; eslogan/textos fiscales no definidos por tenant permanecen NULL, nunca se rellenan desde singleton. La ruta de PDF público conserva acceso por capability ya verificado en el repositorio de facturas, sin exigir un JWT extra al receptor. Sin owner explícito se conserva presentación legacy únicamente si el repositorio permitió ese documento.
- `FacturaTenantVisualIdentityTests` verifica con mocks estrictos que factura B obtiene logo de B, no se invoca `GetActivaAsync()` global, y textos globales no aparecen en DTO. Sin cambios a Schema, migraciones, tokens ni storage real; REVIEW_FIRST y CI exact-head requeridos.

## 2026-10-15 — P4: confirmaciones Compra/Venta A/B y factura con identidad tenant (candidato DEV)

- **Hallazgo P1 prevenido:** `VentaService.ConfirmarAsync` usaba `EmpresaConfiguracionService.GetActivaEntidadAsync()` global para nombre, RTN, teléfono, correo y dirección de la factura. Bajo una membresía A/B verificada, eso podía imprimir identidad de otra empresa. Ahora resuelve exclusivamente `IEmpresaConfiguracionService.GetTenantAsync(EmpresaId autorizado)`, comprueba la misma empresa de la venta persistida y asigna `Factura.EmpresaId` desde esa venta. Para datos tenant no configurados deja NULL (nunca mezcla valores globales). Sin marca multiempresa conserva contrato singleton legacy exclusivamente.
- `UnscopedCommercialTenantGuardMiddleware` agrega flag `MultiEmpresa:PrivateConfirmPilotEnabled=false` por defecto. Whitelist sólo POST `/ventas/{id:positivo}/confirmar` y `/compras/{id:positivo}/confirmar`, con JWT, membresía RBAC vigente, permiso exacto `Ventas.Confirmar` o `Compras.Confirmar`, empresa activa y owner de documento verificado bajo SELECT ... FOR UPDATE. Sin operaciones de anular/cobrar/archivos/exportar. Corrige además precedencia para que los límites de body del piloto Borradores no bloqueen confirmaciones sin cuerpo.
- Nuevo test `CommercialConfirmPilotGuardTests`: permite sólo IDs positivos y método POST, permiso Confirmar, bandera explícita; niega B, token ausente, ID 0, otras acciones y rutas similares. Workflow MySQL 8.4 efímero con 2 JWT reales extiende el piloto A/B a confirmación de compra (suma stock), confirmación venta (resta stock), factura `EmpresaNombre` igual a empresa A/B, lectura cruzada prohibida y doble confirmación denegada. Nunca toca Aiven ni empresa persistente real.
- **Controles previos a fusión:** REVIEW_FIRST, CI exact-head, MySQL real efímero, security, P0/P1=0 y verificación causal de cada falla. Flags `PrivateConfirmPilotEnabled` encendidos exclusivamente en runner CI; en Render DEV, QA y PROD permanecen OFF. No considerar certificadas anulación, pagos, Cloudinary, Excel, ZIP, PDFs, reportes o workers por este piloto. Rollback: flag OFF y revert del changeset sin DDL.

## 2026-10-14 — P4: piloto de borradores Producto/Compra/Venta A/B con JWT (candidato DEV)

- `UnscopedCommercialTenantGuardMiddleware` incorpora opt-in `MultiEmpresa:PrivateDraftPilotEnabled=false` por defecto. Con exactamente 2 empresas, la bandera puede autorizar SOLO `POST /productos` con `Productos.Crear`, `POST /compras` con `Compras.Crear` y `POST /ventas` con `Ventas.Crear`; se mantiene comprobación de JWT, membresía RBAC empresarial server-side, Empresa activa, `VerifiedCommercialTenant` y segunda verificación `RequierePermiso` original de MVC. `X-Empresa-Id` jamás autoriza por sí sola.
- Seguridad de contenido: POST productos exige formulario válido menor a 256 KiB y sin `IFormFile`, rechazando imágenes antes de Cloudinary; POST ventas/compras exige JSON bajo 256 KiB. No se habilitan otros métodos/rutas, confirmación, anulación, cambios de stock, cobros, facturas, PDFs, reportes, exportaciones, ZIP ni jobs. Banderas de DEV Render y QA/PROD siguen apagadas/intactas.
- `CommercialDraftPilotGuardTests` valida privilegios, empresa A permitida/B denegada, banderas false, archivos, payload inválido, rutas de confirmación y pagos bloqueadas. El workflow de aceptación HTTP JWT real extiende el caso local MySQL 8.4 efímero: crea productos sin archivos por FormData, variante propia, borradores de compras/ventas en A/B, GET propios/ajenos y rechazos al utilizar producto de otra empresa.
- El nuevo alcance NO certifica operación financiera integral: requiere CI causal exact-head, REVIEW_FIRST, integración MySQL y aceptación real del workflow antes de merge. No hay migraciones, secretos productivos, modificaciones Aiven/Render/QA/PROD. Rollback: flag `PrivateDraftPilotEnabled` false y revert sin alterar datos.

## 2026-10-14 — P4 lectura documental privada de ventas, compras y facturas A/B (candidato DEV)

- Nueva lista blanca acotada detrás de `MultiEmpresa:PrivateReadPilotEnabled=false` (apagada por defecto): `GET /ventas` y `/ventas/{id:int}` (Ventas.Ver), `GET /compras` y `/compras/{id:int}` (Compras.Ver), `GET /facturas`, `/facturas/{id:int}` y `/facturas/venta/{ventaId:int}` (Facturacion.Ver). Sólo IDs positivos ASCII, sin aceptar prefijos ni subrutas.
- El middleware requiere JWT, selección empresarial resuelta por membresía/permisos RBAC del servidor, empresa activa y marca `VerifiedCommercialTenant`; el filtro MVC `RequierePermiso` y los filtros EF vuelven a exigir el ámbito. `UsuarioScopeService.ObtenerActualAsync()` aplica la membresía tenant-aware bajo la marca, y `FacturaRepository.LecturaPrivadaAsync` comprueba explícitamente owner y rol. La cabecera X-Empresa-Id nunca autoriza por sí sola.
- `CommercialDocumentReadPilotTests` añade matriz positiva A/B de siete rutas y negativas spoofing, JWT ausente, flags off, mutaciones, pagos, archivos, exportación PDF, subrutas y rutas parecidas; cubre filtro EF sobre Venta/Compra/Factura. El mock de permisos y EF InMemory NO sustituye JWT real ni MySQL E2E completo; no hay habilitación ni certificación integral P4.
- Sin POST/PUT/DELETE de ventas/compras/facturas, inventario, pagos, Cloudinary, ZIP, PDF, reportes, jobs, nuevos flags ni DDL/backfill. No modificar Aiven, Render ni QA/PROD. PR apilado tras #3639 y #3638; requiere REVIEW_FIRST, CI exact-head, gates MySQL/security y P0/P1=0 antes de merge. Rollback: flag de lecturas apagado y revert del changeset.

## 2026-10-14 — P4 catálogo de tipos de cliente y alta Cliente A/B (DEV, candidato)

- Bajo pilotos opt-in `MultiEmpresa:PrivateReadPilotEnabled=false` y `MultiEmpresa:PrivateWritePilotEnabled=false` (ambos apagados por defecto), la whitelist añade exclusivamente `GET /tipo-clientes`, `GET /tipo-clientes/activos`, `GET /tipo-clientes/{id:int}` con permiso `TiposClientes.Ver`, y `POST /tipo-clientes` con `TiposClientes.Crear`.
- `RequierePermiso` conserva doble control RBAC; el owner de `TipoCliente` shadow EmpresaId se deriva por `CommercialTenantOwnershipGraphWriteGuard` desde marca privada `VerifiedCommercialTenant`, sin aceptar cabeceras como autoridad. La ruta `POST /clientes` existente usa un TipoCliente propio por FK empresarial y owner RBAC; con dos empresas pueden crear cada una su tipo `GENERAL` y su cliente sin compartir registros.
- Prueba de integración `CommercialPilotCustomerTypesMySqlIntegrationTests` utiliza exclusivamente MySQL efímero localhost, base aleatoria y cleanup. Comprueba escritura real A/B de tipos y clientes, lectura propietaria/ajena, rechazo spoofing, sin JWT, flags apagados, ruta maliciosa y subruta de exportación. La autorización en prueba es simulada; falta login JWT real, UI y demás operaciones comerciales.
- PR apilado sobre #3638 y #3637, sin migraciones, Aiven, datos reales DEV, cambios Render, QA o PROD. Exige REVIEW_FIRST, CI exact-head MySQL y seguridad y P0/P1=0; no marcar P4 integral LISTO ni encender banderas hasta aceptación completa. Rollback: flags off y revert de changeset.

## 2026-10-14 — P4 detalle y búsquedas de maestros privados A/B (DEV, candidato)

- El piloto `MultiEmpresa:PrivateReadPilotEnabled` cubre además de las listas el detalle positivo `/{id:int}` de productos/clientes/proveedores y las consultas exactas `/clientes/activos`, `/clientes/buscar`, `/proveedores/activos`, `/proveedores/buscar`. Cada acción sigue protegida por `[Authorize]` y `[RequierePermiso(..., Ver)]` originales.
- `EsRutaDetallePositiva` admite un solo segmento numérico ASCII positivo y no habilita subrutas de imágenes, archivos, exportaciones, compras, ventas, identificadores inválidos o prefijos engañosos. Se exige permiso efectivo server-side y Empresa activa antes de marcar `VerifiedCommercialTenant`; los filtros EF ocultan datos B a A.
- Pruebas de middleware InMemory: permisos A positivos/B negativos, siete nuevas rutas aceptadas bajo flag, rutas parecidas y malformadas bloqueadas; prueba MySQL efímera amplía el piloto con detalle de producto propio y detalle ajeno invisible. Con flag off permanece 503. JWT/RBAC de pruebas es simulado; falta certificación con usuarios reales, frontend y módulos restantes.
- Dependencia de PR #3637 para el piloto POST. Sin migraciones, backfill, cambios a Aiven, Render, QA o PROD. REVIEW_FIRST, CI causal MySQL/seguridad y P0/P1=0 antes de fusionar. No certifica P4 integral. Rollback: flag off y revert de este changeset.

## 2026-10-14 — P4 piloto opt-in de altas A/B de clientes y proveedores (DEV, candidato)

- `UnscopedCommercialTenantGuardMiddleware` conserva la cuarentena P0 con 2+ empresas, pero separa el piloto de lecturas vigente del nuevo `MultiEmpresa:PrivateWritePilotEnabled=false` por defecto. Solo `POST /clientes` y `POST /proveedores` pueden atravesarla con JWT, selección empresarial resuelta en servidor, permiso relacional exacto `Crear` y Empresa activa revalidada en MySQL.
- El filtro MVC `RequierePermiso` mantiene su segunda comprobación. `VerifiedCommercialTenant` suministra el owner de la nueva fila a los servicios existentes; la cabecera nunca es autoridad. No se abre `POST /productos`, rutas de detalle, ventas, compras, inventario, Cloudinary, PDFs, reportes, exportaciones, jobs ni alta general de empresas.
- `CommercialPilotWriteMySqlIntegrationTests`: MySQL efímero localhost con base aleatoria, escrituras físicas A/B de proveedores, lectura filtrada por owner, rechazo A→B, sin JWT, flag apagado y rutas parecidas; la autorización de prueba se simula expresamente, por lo que falta aceptación HTTP con JWT y RBAC reales y servicio de Cliente completo.
- Sin DDL, backfill, modificaciones Aiven/Render/env ni QA/PROD. Requiere REVIEW_FIRST y CI exact-head MySQL/seguridad, P0/P1=0 antes de merge. El piloto NO certifica punto 4 ni autoriza encender flags o quitar `503` de otras rutas. Rollback: desactivar flag y revertir PR, sin migraciones.

## 2026-10-14 — P4: cierre de privilegios cruzados por roles empresariales (DEV, candidato)

- En `UsuarioScopeService.ContextosTenantValidos`, la membresía solo se considera válida con un rol `Ambito=Empresa` que sea plantilla global (`Rol.EmpresaId=null`) o pertenezca exactamente a la misma empresa de `UsuarioEmpresa.EmpresaId`. Un rol de Plataforma o un rol personalizado de B nunca confiere autorización en A aunque la membresía referencie un `RolId` existente.
- En `UsuarioService`, los caminos de alta, cambio y reactivación de membresía validan la misma relación rol/empresa antes de persistir o registrar permisos. `ProductionDataRepairService` deja de atribuir roles de Plataforma o de otra empresa a la reparación monoempresa legacy.
- Pruebas dirigidas: `UsuarioTenantRoleOwnershipTests` sobre EF InMemory + mocks y `UsuarioTenantRoleOwnershipMySqlIntegrationTests` con MySQL efímero de host local verificado, A/B y roles distintos. Se comprueban intentos cruzados y de Plataforma rechazados, roles plantilla permitidos, desactivación de rol y prohibición de persistir membresías inválidas.
- No DDL/backfill, no datos Aiven, no secretos ni cambios QA/PROD, ni autorización para levantar `503` o activar los flags multiempresa. Estado candidato hasta REVIEW_FIRST, P0/P1=0 y CI exact-head. Rollback por revert sin reversión de datos.

## 2026-10-14 — P4: natural keys empresariales reconciliadas sin migración duplicada (candidato)

- Origen recuperado: PR #3607 sin fusionar. Sus 13 claves incluían indebidamente la cuenta de tienda, cuya unicidad `(EmpresaId, CorreoNormalizado)` ya fue integrada en la migración `20261014112000_PublicStoreAccountEmailPerEmpresa`. Esta reconciliación incorpora **12 claves restantes** sin alterar el índice vigente de tienda.
- Migración Oracle incremental `20261014130000_CommercialTenantNaturalKeysPostStorefront`, índices compuestos por `EmpresaId`, columnas generadas para plantillas de roles sin dueño, detección de errores únicos tenant-aware y contratos físicos/verificación MySQL efímera. Ningún dato histórico se asigna ni elimina y no se activa el onboarding ni los endpoints comerciales de dos empresas.
- Scope DEV y CI. Estado candidato hasta revisión, gates exact-head y MySQL real efímero; cuarentena `503` intacta, QA/PROD no se modifican.

## 2026-10-14 — P4: onboarding transaccional de empresas desde autoridad de plataforma (candidato DEV)

- Nuevo `POST /plataforma/empresas/alta-controlada` con JWT, **dos flags independientes apagados por defecto** (`MultiEmpresa:CommercialIsolationCertified` y `MultiEmpresa:PlatformOnboardingEnabled`), y comprobación en MySQL de `UsuarioRolPlataforma` activo unido a Usuario vigente y Rol vigente con `Ambito=Plataforma`, `EsAdministrador=true`, `EmpresaId=NULL`. Un administrador empresarial jamás sustituye esa autoridad ni se crea/simula un administrador global en seed.
- La operación exige administrador inicial de la empresa válido, rol plantilla de ámbito Empresa y escribe **Empresa + UsuarioEmpresa + RegistroAuditoria** en una única transacción MySQL, conservando `AmbitoAutoridad=Plataforma` y `OrigenAutorizacion=RolPlataforma`. Ruta estricta de bypass solo del middleware HTTP para ese POST y cuando ambos flags y JWT están presentes; el controlador vuelve a autorizar server-side. La ruta legacy `POST /empresas` permanece bloqueada y las rutas comerciales siguen en cuarentena.
- Pruebas negativas de flags off, JWT/rol plataforma ausente, administrador inactivo, nombres repetidos, paths similares; prueba positiva de dos empresas y membresía inicial verificada. Prueba de integración MySQL efímera de transacción completa y rollback al fallar la auditoría, sin tocar Aiven. Ninguna migración ni nuevo usuario/secreto, ningún despliegue/config en Render/QA/PROD. **No habilitar flags hasta certificar el aislamiento comercial A/B integral; crear empresa B con cuarentena vigente puede bloquear las operaciones comerciales previas.** Rollback: revert del PR, sin migraciones.

## 2026-10-14 — P4: piloto privado de lecturas A/B (candidato, apagado por defecto)

- El middleware P0 mantiene la cuarentena `503` para 2+ empresas; sólo añade la bandera **`MultiEmpresa:PrivateReadPilotEnabled=false` por defecto**. Bajo opt-in controlado, únicamente `GET /productos`, `GET /clientes` y `GET /proveedores` pueden avanzar tras JWT autenticado, membresía/permisos RBAC efectivos de la empresa seleccionada y Empresa activa verificada en MySQL. `X-Empresa-Id` nunca autoriza por sí solo.
- La marca `VerifiedCommercialTenant` se establece antes de las consultas EF sólo tras la autorización server-side; el action filter `RequierePermiso` vuelve a verificar. Las rutas de detalle, mutaciones, archivos, exportaciones, jobs y alta de empresas siguen en `503` con 2+ empresas. No se modifica el esquema ni datos Aiven ni se habilita la bandera en Render/QA/PROD.
- Prueba negativa y positiva A/B en `UnscopedCommercialTenantGuardTests`: propietario A visible, B rechazado, JWT ausente, ruta similar y POST rechazados. Candidato sujeto a revisión exact-head y CI; **no constituye autorización para retirar la cuarentena ni certificación P4 integral**. Rollback: revert del PR; estado por defecto idéntico al anterior.

## 2026-10-14 — P4: lecturas auxiliares de métodos de pago con membresía tenant

- `GET /metodos-pago/activos` y `GET /metodos-pago/bancos-activos` tenían `[Authorize]` pero no emitían marca `VerifiedCommercialTenant` antes de consultar catálogos/bancos. Se agrega `RequiereMembresiaEmpresa`: autenticación validada, selección empresarial con cabecera **no autoritativa**, `IUsuarioScopeService.ObtenerActualAsync(empresaId)` con membresía/rol/empresa activos y marca empresarial solo tras comprobación.
- Se conserva la disponibilidad a empleados miembros de la empresa sin exigir el permiso administrativo `MetodosPago:Ver` para desplegables usados en cobros/ventas; las demás operaciones siguen con `RequierePermiso`. Pruebas de membresía A válida, B ajena, sin empresa seleccionada, JWT ausente, y contrato de atributos de ambos endpoints.
- Corrección sólo DEV, sin habilitar ruta alguna bajo cuarentena de 2+ empresas, sin migraciones, alteraciones Aiven, Cloudinary, QA ni PROD. Candidato a CI exact-head y REVIEW_FIRST; no representa certificación P4 integral.

## 2026-10-14 — P4: defensa de propietario en lotes Outbox (DEV candidato)

- `OutboxRetryProcessor.ProcesarLoteAsync` comprueba que **todos** los mensajes reclamados tienen `MensajeOutbox.EmpresaId` exactamente igual al `empresaId` del procesamiento antes de ejecutar el primer `IOutboxEffectDispatcher`. Ante lote mixto A/B lanza error fail-closed sin despachar, confirmar ni reprogramar filas.
- Test negativo `N72DOutboxRetryProcessorTests.Lote_Con_Mensaje_De_Otra_Empresa_No_Despacha_Ni_Confirma_Nada`; preserva las pruebas existentes de retry/stale/dead-letter y contratos de repositorio que ya utilizan `EmpresaId`.
- Incremento independiente de los PR #3608–#3611 y del trabajo de startup #3612: sin DDL, sin backfill, sin cambios QA/PROD, sin habilitar segundas empresas. Requiere CI exact-head, review y merge para considerarlo integrado. **No certifica por sí solo el punto #4 completo.**

## 2026-10-14 — P4: seed de roles no modifica roles específicos de A/B (candidato DEV)

- `SeedPermisoService` sólo encuentra/repara plantillas compartidas con `EmpresaId=NULL` y `Ambito=Empresa`. Durante startup nunca reactiva el rol `ADMINISTRADOR` propio de B, ni concede automáticamente todos los permisos a un rol administrador personalizado, ni modifica un rol de Plataforma.
- Prueba `SeedPermisoServiceTests.StartupMultiEmpresa_NoRehabilitaRolPropioNiConcedeGrantsAOtraEmpresa`: crea roles de B desactivados y administrador B personalizado; valida que seed global no los altere y que sólo la plantilla recibe grants.
- Candidato aún sujeto a CI exact-head, revisión P0/P1 y MySQL efímero antes de fusionarse; sin retirar la cuarentena HTTP ni habilitar empresas adicionales.

## 2026-10-14 — P4: tareas legacy de arranque bajo 2+ empresas (candidato DEV)

- `ProductionDataRepairService.RepairAsync`: cuenta empresas **antes** del UPDATE SQL global de `Usuarios`. Con 2+ empresas (activas o inactivas) devuelve resultado sin ejecutar normalización ni atribuir membresías; conserva reparación monoempresa/legacy. Corrige un write fuera de tenant que no cubría middleware HTTP.
- `SeedFiscalService.SeedDefaultsAsync`: no crea impuestos ni operaciones fiscales globales sin `EmpresaId` cuando existen 2+ empresas. La inicialización fiscal de nuevas empresas requiere onboarding explícito verificado; esta protección no la implementa ni permite activar empresas todavía.
- `StartupLegacyJobsMultiEmpresaIsolationIntegrationTests`: prueba MySQL descartable con dos empresas A/B, B inactiva y un usuario histórico marcado eliminado, certifica que ningún seed global lo reactivó y que no se insertaron impuestos sin dueño.
- DEV exclusivamente. Ninguna migración ni dato histórico real modificado; QA/PROD y dominios aplazados intocados.
- **Incremento de contención, NO certificación P4 integral.** La cuarentena HTTP con 2+ empresas sigue vigente; aún hay que certificar jobs tenant-aware, reportes/exportaciones y habilitación segura end-to-end sin bypass. Requiere CI exact-head y REVIEW_FIRST.

## 2026-10-13 — P4: auditoría pública atribuida a empresa verificada (DEV candidato)

- `AuditoriaService.RegistrarCoreAsync` conserva precedencia `VerifiedCommercialTenant` → `VerifiedPublicInvoiceTenant` → `VerifiedPublicStoreTenant` al asignar `RegistroAuditoria.EmpresaId`. Enlaces PDF y checkout no usan el `X-Empresa-Id` bruto ni requieren inventar una identidad RBAC.
- `AuditoriaTenantOwnershipTests` cubre factura pública, tienda, prioridad RBAC y marcas fuera de ruta. La consulta posterior de auditoría se mantiene filtrada por propiedad empresarial EF.
- Aplica al PR #3611; pendiente CI exact-head y REVIEW_FIRST. Sin cambios QA/PROD; cuarentena general de 2+ no se elimina.

## 2026-10-13 — P4: acceso exclusivo a PDF público mediante capability empresarial (candidato DEV)

- `UnscopedCommercialTenantGuardMiddleware` permite atravesar la cuarentena con dos empresas únicamente a `GET /facturas/publico/{token}/pdf` cuando la ruta coincide exactamente y el token tiene formato URL-safe (32-160 caracteres). El middleware NO verifica ni confiere la capability: `FacturaCompartirRepository` comprueba hash, vigencia, límite, EmpresaId persistido y FK empresarial antes de marcar el contexto HTTP; `FacturaRepository` solo lee el FacturaId autorizado y EF filtra por empresa.
- Ni `X-Empresa-Id` ni el host constituyen autoridad; POST, prefijos engañosos, tokens cortos, rutas adicionales, facturas privadas, exportaciones y demás operaciones comerciales permanecen bloqueadas con 2+ empresas.
- Pruebas negativas y positivas en `UnscopedCommercialTenantGuardTests`: la ruta solo atraviesa el middleware pero no emite `VerifiedPublicInvoiceTenant` sin validar el token en el servicio.
- **No se declara certificación P4 integral:** falta CI exact-head, REVIEW_FIRST y cobertura de operaciones privadas/jobs multiempresa. Sin cambios de dominios, datos históricos, QA ni PROD.

## 2026-10-13 — P4: resolver Host exacto para múltiples tiendas (candidato DEV)

- `ConfiguredPublicStoreHostTenantResolver` implementa `IPublicStoreHostTenantResolver` a partir de `PublicStore:HostBindings:{N}:Host` y `EmpresaId`, con opt-in `PublicStore:EnableMultiEmpresaBinding=true` que permanece **apagado por defecto**. Hosts deben ser ASCII exactos y distintos, sin wildcard, IP, URL ni doble asignación; cualquier configuración ambigua falla cerrada.
- `UnscopedCommercialTenantGuardMiddleware` permite `/tienda/*` con 2+ empresas SOLO cuando `Request.Host` coincide con allowlist server-side y la Empresa persiste activa; las cabeceras `X-Empresa-Id` y `X-Forwarded-Host` no autorizan tenant. Emite `VerifiedPublicStoreTenant` por solicitud y `PublicStoreTenantKeyProvider` produce caché `empresa:{id}` independiente. Hosts no registrados/suspendidos se rechazan. `/productos`, `/facturas`, `/reportes` y demás rutas privadas continúan en cuarentena con 2+.
- Pruebas EF/InMemory y MySQL descartable A/B: host A solo ve A y host B solo ve B; host malicioso se rechaza; ninguna ruta privada se desbloquea. DNS/certificado real y operación pública multinavegador aún pendientes; sin activar flag en DEV/QA/PROD ni mover dominio `solqaryn.com`.
- Este incremento NO representa el cierre general de P4: persisten tareas de workers sin HTTP, exportación, PDFs públicos, Cloudinary, permisos y E2E públicos completos. La habilitación productiva requiere certificación integral y autorización posterior.

## 2026-10-13 — P4 DEV: cache pública con tenant vinculado (candidato)

- `PublicStoreTenantKeyProvider` utiliza `VerifiedPublicStoreTenant` emitido server-side para solicitudes `/tienda/*`; claves canónicas `empresa:{EmpresaId}` y rechazo estricto de rutas públicas sin vinculación. No acepta como autoridad cabecera, DTO, query ni nombre comercial.
- Se mantiene la resolución legacy cacheada solo para llamadas sin ruta pública y únicamente si existe UNA empresa activa; jamás usa el resultado legacy para sustituir la marca faltante de un request público.
- Pruebas: dos solicitudes A/B producen identidades y entradas de caché separadas; una ruta pública con `X-Empresa-Id` arbitrario sin marca validada falla cerrada. Integración MySQL, CDN/Cloudinary y host->empresa siguen requiriendo certificación independiente.
- **No habilitar 2+ empresas ni retirar el middleware de cuarentena en este cambio.** Rama y pruebas DEV; no modificar QA ni PROD.

## 2026-10-13 — P4: identidad empresarial y cuenta de tienda por tenant (candidato DEV)

- `EmpresasController` usa `TenantPermissionContext` y no enumera Empresas B al pedir la lista bajo RBAC de A; una búsqueda puntual por la empresa autorizada sustituye la carga global. Lectura por ID y edición/activación/desactivación exigen id de empresa igual al autorizado.
- Las validaciones de venta en `AppDbContext` aplican `TenantQueryBypassGuard` al consultar incluso Productos eliminados y evitan mensajes de error con nombres de otra empresa.
- Cuenta pública `TiendaCuentaCliente`: índice único de correo normalizado ahora `(EmpresaId, CorreoNormalizado)`. Migración `20261014112000_PublicStoreAccountEmailPerEmpresa` crea índice compuesto ANTES de borrar el global; rollback intenta restaurar primero índice global auxiliar y falla cerrado si existen correos repetidos A/B. Snapshot Oracle EF actualizado. Prueba de integración MySQL A/B incluye rollback negativo.
- **Falta certificación exact-head, REVIEW_FIRST y respuesta segura a procesos sin contexto HTTP, tiendas por host/slug y autorización de plataforma para crear nuevas empresas.** Mantener cuarentena de 2+ empresas; sin QA/PROD ni backfill de datos históricos.

## 2026-10-13 — P4: correo de cuenta de tienda único por Empresa (candidato DEV)

- Nuevo delta de tienda pública sobre el candidato de lectura P4: `TiendaCuentaClienteConfiguration` cambia la unicidad del correo normalizado de GLOBAL a `(EmpresaId, CorreoNormalizado)`. La migración aditiva `20261014112000_PublicStoreAccountEmailPerEmpresa` reemplaza únicamente el índice comercial `UX_TiendaCuentasCliente_Correo`; no repite migraciones P3 ni reescribe datos/contraseñas/sesiones históricas.
- Pruebas MySQL efímeras: mismas credenciales/email sintético en empresas A y B con cuentas/clientes independientes, sin lectura cruzada; mismo email en dos clientes de A debe ser rechazado. `EmpresaId=NULL` histórico continúa en cuarentena, sin atribución forzada.
- **No se habilita la tienda con 2+ empresas por esta mejora aislada:** faltan host/slug público verificable, resolución/caché/identidad por empresa, jobs y certificación end-to-end A/B. Mantener bloqueo P0, no tocar QA/PROD y no declarar cierre integral hasta CI verde exact-head + REVIEW_FIRST.

## 2026-10-13 — P4 seguridad de usuarios, membresías y regresión fiscal (candidato DEV)

- Corregida la regresión E2E: tras crear un `Usuario` sin membresías, su creador puede consultar una lista vacía y asignarlo a la empresa RBAC vigente; otros actores y usuarios pertenecientes exclusivamente a B continúan inaccesibles desde A.
- Reactivación: el filtro de `Usuario` ya considera una membresía de la empresa incluso si está inactiva; `UsuarioRepository.GetEmpresasAsync` lista esa membresía solo en el tenant autorizado y `GetEmpresaAsync` puede localizarla para reactivación sin abrir membresías ajenas. Pruebas de servicio, EF y MySQL A/B.
- Se corrigió el fixture **exclusivamente efímero** de la aceptación `catalogos-aceptacion`: los impuestos fiscales de prueba y sus filas `ImpuestoOperaciones` reciben `EmpresaId` coherente; sin backfill ni atribución real de datos legacy.
- Seguridad previa: `TenantQueryBypassGuard`, permisos de Finanzas por tenant y endpoints `UsuariosController` mantienen filtros de propietario. Los cambios no alteran migraciones P3 ni permiten crear una segunda empresa; la cuarentena 503 sigue vigente hasta cobertura de tienda/archivos/jobs.
- **Validación:** CI exact-head, Playwright, integración MySQL y revisión P0/P1 son requisitos pendientes hasta resultado comprobado; prohibido promocionar QA/PROD o marcar P4 completo solo por compilación.

## 2026-10-13 — P4 DEV: revisión de bypass y recuperación segura de onboarding

- En el PR #3608, `UsuarioRepository.GetPendientePrimeraMembresiaAsync` permite finalizar SOLO la primera membresía de una identidad sin empresas creada por el mismo actor; no abre listados de usuarios ajenos. Los endpoints `UsuariosController` comprueban que empresa del body/ruta coincide con la marca RBAC verificada, censuran otras membresías y sustituyen autorizaciones legacy por permisos tenant-aware.
- `ReportesInventarioValorizacionController` comprueba `Finanzas:Ver` en la empresa autorizada antes de revelar costos.
- `TenantQueryBypassGuard` vuelve a imponer owner sobre consultas de maestros, variantes, ubicación, comprobación de tipos de cliente, cargas masivas, auditoría de costo de envío y bloqueos de inventario que utilizan `IgnoreQueryFilters`. Conserva el caso monoempresa antiguo sin marca y la cuarentena HTTP 2+; no es una vía de excepción para habilitar multiempresa.
- Nuevas pruebas cubren creación inicial por actor, bloqueo de modificaciones de membresías A→B, censura de lista de empresas y filtro A/B aun con `IgnoreQueryFilters`.
- **Candidato en rama; NO es certificación integral ni autorización de nueva empresa:** quedan revisión de workers/no HTTP y consultas SQL que ignoran EF, binding público multiempresa, cobertura integral A/B con MySQL real, CI exact-head y cierre REVIEW_FIRST/P0-P1. QA/PROD sin cambios.

## 2026-10-13 — P4 identidad compartida (candidato con CI pendiente)

- La entidad global `Usuario` carece de `EmpresaId` por diseño; su filtro de lectura administrativa por `UsuarioEmpresa` restringe listados y consultas de identidad a miembros de la empresa post-RBAC. Sin marca, login/renovación aún pueden buscar identidades globalmente sin revelar listados HTTP comerciales.
- Prueba: `CommercialTenantReadFilterTests.UsuariosCompartidos_SeListanSoloConMembresiaEnEmpresaActual`. Debe aprobar exact-head junto al resto de la matriz. No habilitar segundo tenant ni declarar cierre completo solo por esta protección.

## 2026-10-13 — P4 DEV: ampliación del candidato (sin cierre total)

- Lecturas EF: filtro por `EmpresaId` en entidades empresariales, conservando soft-delete y roles plantilla. Las rutas con `IgnoreQueryFilters` en conteos y reversión de existencias reintroducen explícitamente el owner.
- Tienda anónima: bajo la cuarentena de una sola empresa activa, `UnscopedCommercialTenantGuardMiddleware` fija una marca de empresa server-side restringida a `/tienda`. Los filtros y las altas de cuenta consumen esa marca, nunca `X-Empresa-Id` del cliente. Si no existe empresa pública única, `PublicStoreTenantKeyProvider` falla cerrado; no infiere por nombres o `public-config`.
- PDF público: un token hasheado solo puede establecer el scope de lectura si el enlace persistido no ha expirado, tiene `EmpresaId>0` y no contradice RBAC. Enlaces de prueba sin dueño no reciben autorización; la lectura de factura posterior se filtra por tenant.
- Evidencia requerida antes de fusionar: revisión y CI exact-head del PR #3608, pruebas por módulo y MySQL real. **No se ha eliminado la cuarentena de 2+ empresas y NO se declara el punto #4 cerrado**. Faltan tareas/jobs, SQL directo, todas las variantes de archivo/export y dominio->empresa pública para convivencia simultánea. QA y PROD intactos.

## 2026-10-13 — Punto #4 (candidato): filtros de lectura EF por tenant

- Agrega filtros EF dinámicos `EmpresaId` solo cuando existe marca tenant RBAC verificada y preserva filtros existentes. Incluye prueba A/B, legacy sin owner y roles plantilla. La cuarentena de segunda empresa continúa por ausencia de resolución pública y cobertura no EF; no hay DDL ni cambios QA/PROD. Candidato sujeto a CI exact-head y revisión.

## 2026-10-13 — Ajuste de compatibilidad tenant: roles plantilla y fixture E2E

- Los grants de roles empresariales plantilla (Rol.EmpresaId nulo, Ambito Empresa) conservan propietario nulo y no reciben EmpresaId arbitrario. Los grants de roles empresariales personalizados mantienen validación A/B; los roles de plataforma no se modifican bajo una marca tenant.
- El fixture MySQL descartable E2E asigna el tipo inicial SIN_CLASIFICAR al único tenant de prueba antes de insertar clientes; no se atribuyen datos históricos reales ni se relajan FKs de clientes/tipos.
- Pruebas dirigidas para roles plantilla, roles ajenos y autoridad de plataforma. QA y PROD sin cambios.

## 2026-10-13 — Candidato final P0 #3: completar `EmpresaId` en modelo comercial

- El PR #3603 está integrado. Esta ampliación incorpora ownership en 42 tablas adicionales, incluidas `RolPermisos`, catálogos, finanzas, almacenes, contabilidad, caja, tienda y configuración comercial, mediante columnas nullable para legacy descartable.
- Se añaden FKs a `Empresas`, índices de identidad por empresa, 15 índices únicos complementarios en padres y 86 FKs compuestas sobre referencias entre tablas con propiedad empresarial; 25 unicidades de negocio dejan de ser globales.
- El catálogo exclusivamente técnico sigue compartido (`Empresas`, `Usuarios`, `Permisos`, `Planes`, `PlanModulos`, `UsuarioRolesPlataforma`, `Bancos`, `MetodosPago`). La unicidad del hash de token público permanece global para evitar colisiones de URLs.
- Oracle EF10/MySQL 8.4: expansiones `20261013230000` y `20261013231000`, bootstrap fresh, fixture legacy y tests A/B. No eliminar ni reatribuir información de prueba, ni abrir multiempresa antes de cerrar aislamiento de lecturas/servicios.
- Estado de este changeset: candidato; sólo pasa a certificado tras CI verde exact-head, revisión y merge a `dev`. QA/PROD intocados.

## 2026-10-13 — Candidato P0: esquema tenant para cotizaciones y pedidos

- EmpresaId nullable y FKs empresariales en Cotizaciones, CotizacionDetalles, PedidosVenta y PedidoVentaDetalles; 9 FKs compuestas vinculadas a Clientes, Productos, Variantes y documentos.
- Idempotencia y origen de pedido únicos por (EmpresaId, IdempotencyKey / CotizacionId). Migración Oracle 20261013193000, snapshot, fresh/adopt bootstrap, guard RBAC en escrituras.
- La cuarentena multiempresa continúa; no se atribuyen fixtures de prueba, no hay cambios QA/PROD ni certificación de todo el grafo## 2026-10-13 — Candidato de expansión integral del grafo comercial P0 #3

- Derivadas 46 entidades comerciales EF adicionales con EmpresaId nullable desde las cinco raíces, sin reconstruir/atribuir registros históricos descartables.
- Incorporados índices por empresa de identidad y relaciones, 46 FKs a Empresas y 12 índices únicos adicionales en padres existentes; migración Oracle EF Core 10 y pruebas dirigidas.
- La expansión de esquema es estructural, no certifica todavía FKs compuestas de todas las referencias, lecturas/servicios/jobs ni autorización de segunda empresa. Mantener cuarentena, QA/PROD intactos.

## 2026-10-13 — Candidato tributación/descuentos por EmpresaId

- `VentaImpuestos`, `CompraImpuestos`, `VentaDescuentos`, `HistorialUsoDescuentos`: propiedad empresarial nullable para legacy, índices owner-padre, 5 FKs compuestas con Ventas/Compras/Clientes y validaciones de nuevas escrituras.
- Migración Oracle EF10 aditiva, bootstrap MySQL fresh/legacy, pruebas A/B; no altera datos, QA ni PROD. Quedan catálogos/configuraciones y otros descendientes por aislar.
- Mantener cuarentena multiempresa y las protecciones de lecturas/escrituras existentes.

## 2026-10-13 — Candidato adicional P0 #3: factura y adjuntos de compra por empresa

- `FacturaDetalles`, `FacturaPagos` y `CompraDocumentos` incorporan `EmpresaId`, indices por empresa, tres FKs compuestas documentadas/validadas en MySQL y guard de escrituras RBAC. `Facturas` incorpora indice unico `(EmpresaId, Id)`.
- Migracion Oracle expand-only, snapshot, bootstrap fresh/adopt, simulacion legacy, pruebas A/B y verificacion fisica de FKs; no modifica ni atribuye los fixtures historicos.
- No levantar cuarentena ni habilitar nueva empresa hasta completar todo el modelo y los read paths; QA/PROD no tocados.

## 2026-10-13 — PR #3599 (candidato): claves por empresa y dependientes principales

- Se agregan índices tenant y unicidades por empresa en cinco raíces, EmpresaId nullable a 5 descendientes (variante, foto, detalles de compra/venta, factura), guard de derivación y 12 FKs MySQL compuestas.
- Snapshot/migración Oracle EF10 y CI quedan sujetos a certificación exact-head; NO está autorizado levantar la cuarentena de la segunda empresa. Quedan otras dependencias del grafo comercial y filtros de lectura por resolver; no se alteran datos ni entornos QA/PROD.

## 2026-10-13 — Decisión de no atribuir fixtures comerciales DEV

- Los 19 registros comerciales sin EmpresaId son pruebas descartables por decisión del propietario; se retira el backfill histórico del alcance obligatorio.
- Cualquier limpieza futura exige inventario referencial, backup/restore y preservación de identidades y permisos. No se ejecutan cambios de BD con este commit.
- La cuarentena se mantiene; sigue pendiente partición de datos FUTUROS en SQL, API, docs, storefront público, trabajos y caché, y prueba A/B antes de dos empresas.

## 2026-10-13 — Candidato: autorización tenant de escrituras EF y grafos nuevos (#3596)

- `AppDbContext` conecta la marca server-side de RBAC con `CommercialCrossTenantWriteGuard`; los cambios a entidades comerciales con propietario ajeno se rechazan aun cuando el `EmpresaId` no cambia. Los grafos nuevos válidos se evalúan a través de entidades `Added` sin saltarse la revisión de propietarios persistidos.
- Seguridad parcial exclusivamente en rama PR: las lecturas, `EmpresaId=NULL` históricos, procesos fuera de HTTP, SQL directo y tienda pública siguen sin aislamiento final. DEV no está certificado para operar simultáneamente dos empresas.

## 2026-10-13 — Trazabilidad de auditoría con owner verificado (candidato)

- `AuditoriaService` registra `RegistroAuditoria.EmpresaId` cuando existe marca tenant autorizada por RBAC, sin confiar en headers. Auditoría no HTTP y legacy conserva NULL. No es aislamiento de lecturas ni backfill.

## 2026-10-13 — Validación de referencias comerciales explícitamente cruzadas (candidato DEV)

- Gate anterior a SaveChanges que rechaza vínculos con propietarios empresariales diferentes en Ventas/Clientes, Compras/Proveedores y detalles/Productos. No modifica el modelo, esquema, datos históricos ni QA/PROD; contención P0 se conserva.

## 2026-10-13 — Identificador de empresa autorizado en nuevas altas comerciales (candidato)

- Cinco servicios comerciales consumen el owner de la marca server-side posterior al filtro RBAC, no de header/payload. Datos legacy y flujos anónimos siguen pendientes; contención P0 activa.

## 2026-10-13 — Expand comercial tenant (candidato dev)

- Propiedad opcional `EmpresaId` y FK/índice restrictivos en cinco raíces; Oracle `20261013121500_CommercialTenantRootExpand` y bootstrap fresh/adopt compatibles. Histórico sin asignar, guard P0 preservado. Sin certificación multiempresa ni cambios a QA/PROD.

## 2026-10-12 — Contención P0 de tenancy comercial sin atribución arbitraria (candidato DEV)

- Se confirmó que Producto, Cliente, Proveedor, Compra y Venta todavía no tienen EmpresaId; el control RBAC/UsuarioEmpresa no garantiza propiedad de datos. Catálogos, variantes, inventario, facturas y documentos derivados deben migrarse de forma coherente.
- Guard obligatorio en pipeline API antes de controladores: en presencia de 0 o 2+ Empresas (incluidas inactivas) bloquea todas las rutas de datos con respuesta 503 sin exponer registros; permite únicamente autenticación, verificación tenant, health y CORS. Con una Empresa deja las rutas existentes sujetas a RBAC.
- Alta de una segunda empresa bloqueada también en EmpresaService, no sólo en POST /empresas. Sin nuevos secretos, provider, variable, esquema, DB, plan ni cambios a QA/PROD.
- Es una cuarentena transitoria: **no habilita multiempresa funcional y no es una certificación final**. Runbook obligatorio: docs/runbooks/TENANCY_DATOS_COMERCIALES_P0.md.

## 2026-10-12 — Readiness bajo demanda antes de login DEV

- Se mantiene el proxy Vercel project-bound y el arranque de ASP.NET Core sin tocar la arquitectura de datos.
- Angular comprueba GET /health/ready por HttpBackend (sin credenciales/JWT), con reintentos acotados sólo del GET; el POST login no se reintenta.
- El proxy informa HTTP 503 + Retry-After cuando falla su conexión, con logs de diagnóstico sin información privada. Se agregan pruebas de proxy y login.
- No se modifica infraestructura ni planes de los tres entornos. DEV necesita CI y despliegue exact-head para certificar, QA/PROD permanecen aplazados.
- Runbook: docs/runbooks/API_UPSTREAM_DISPONIBILIDAD_DEV.md.

## 2026-10-12 — Worker transitorio de normalización Cloudinary DEV, opt-in

- Nueva integración de un `BackgroundService` exclusivamente en Render Development y bajo flag de configuración, inicialmente deshabilitado. Añade verificación de cuenta Cloudinary/empresa y locators, hash SHA-256 sobre bytes descargados y copiados, modificación condicional de URL/PublicId y preservación de originales; sin endpoint nuevo ni cambios a modelos, API, esquema, QA o PROD.
- Impacto: API DI, Infrastructure Cloudinary/MySQL DEV, gate tenant de workers, pruebas de rutas/firma y observabilidad del progreso. Consumidores de imágenes conservan sus contratos y rutas.
- Reversibilidad: fuentes originales conservadas, backup cifrado Aiven DEV y restore efímero previo obligatorios, auditoría read-only post-ejecución; flag false hasta certificar. El PR preparatorio no constituye una copia ejecutada de los 11 assets.
- Evidencia causal requerida: CI exact-head, backup/restore DEV, métricas del worker y reauditoría 0/11 antes de declarar cierre.

## 2026-10-07 — Hotfix de sanitización de imágenes

- SixLabors.ImageSharp 3.1.12 se retira después de que los scanners NuGet comenzaron a reportar advisories high/moderate sobre esa versión.
- `ImagenUploadSecurity` migra a SkiaSharp 4.152.1 + native assets Linux, preservando JPG/PNG/WebP, firma/MIME/extensión, 10 MB, 4096 px, 16 MP y re-encode desde píxeles.
- No cambian persistencia, auth/RBAC, tenancy, QuestPDF, Cloudinary ni entornos.
- Los gates de Fase 7/Fase 8 continúan fail-closed ante vulnerabilidades conocidas.

## 2026-10-07 — Fase 8 NuGet y validación async

- FluentValidation 12.1.1 reemplaza la integración ASP.NET deprecada por DI oficial + `FluentValidationActionFilter` asíncrono, preservando ModelState y cancellation.
- Swashbuckle.AspNetCore 10.2.3 mantiene OpenAPI/Bearer y se corrige el contrato `IFormFile` para el generador sin cambiar el binding multipart.
- IdentityModel se alinea en 8.23.0; BCrypt.Net-Next 4.2.0, CloudinaryDotNet 1.29.3 y ClosedXML 0.105.1 quedan modernizados.
- ImageSharp permanece 3.1.12 por la nueva exigencia de licencia de build de la major 4; no se introduce un secreto/licencia ni gasto nuevo.
- Caching y JSON continúan bajo el shared framework .NET 10, sin referencias directas redundantes. Sin cambios de DB, provider, migraciones, main, QA o PROD.

## 2026-10-06 — Reconciliación post-merge Fase 7 DEV

- Fase 7 integrada mediante PR #3552 y certificada sobre el HEAD post-merge `7013a55cd7491c3c79d6808df5db9ba0b8580f40`.
- Runs post-merge: Fase 7 `37540172636` PASS; Fase 6 `37540172761` PASS; DEV CI `37540172730` PASS; aceptación `37540172606` 100/100; Fase 4 `37540172583` PASS; Fase 5 `37540172750` PASS.
- Estado canónico: `net10.0`, C# 14, SDK 10.0.401, runtime/ASP.NET 10.0.12, EF Core/Design/CLI 10.0.12, JwtBearer 10.0.12 y Oracle `MySql.EntityFrameworkCore` 10.0.9.
- Provider productivo único: Oracle. Las 107 migraciones Pomelo permanecen sólo como historia auditable; Oracle no las reproduce.
- El follow-up de cierre corrige N2.3 para `SslMode=Disabled` y reconcilia evidencia/documentación vigente.

## 2026-10-06 — Fase 7 .NET 10 / EF Core 10 / Oracle

- Backend retargeteado a `net10.0` y C# 14 con SDK 10.0.401 y runtime/ASP.NET 10.0.12.
- EF Core/Design/CLI quedan coherentes en 10.0.12; JwtBearer queda en 10.0.12; el provider productivo único es Oracle `MySql.EntityFrameworkCore` 10.0.9.
- Pomelo/MySqlConnector se retiran del runtime productivo. Las 107 migraciones históricas se conservan en un assembly separado de historia y no se reproducen con Oracle.
- La cadena activa de migraciones usa `Solqaryn.Infrastructure.Migrations` + `OracleBaseline`; el bootstrap fresh materializa el contrato físico canónico, valida 136 tablas y exige `has-pending-model-changes=0`.
- Docker usa SDK 10.0.401 y ASP.NET 10.0.12; los workflows operativos de backend quedan alineados a .NET 10. Los probes históricos explícitos de Fase 6 conservan sus versiones antiguas sólo para reproducibilidad histórica.
- Gate Fase 7 valida build, 2388 unitarios, integración Oracle/MySQL, tenancy fail-closed, auditoría de vulnerabilidades NuGet y smoke Docker con `/health` + `/health/ready`.
- Alcance: Fase 7 integrada en `dev`; sin promoción a `main`, QA ni PROD.

## 2026-10-05 — Reconciliación arquitectónica del exact-head vigente

- Los documentos canónicos vuelven a alinearse con el HEAD actual `425f9cab0ed291401c081d7946289875eb11e798`; Fase 6 exact-head `37330696772` terminó `success`, `FASE_6_MYSQL_EF_PROVIDER=PASS`, `P0=0`, `P1=0`, `PHASE7_EXECUTED=false`.
- El runtime/provider productivo sigue net8.0 + EF8/Pomelo 8.0.2; el candidato aislado queda fijado como SDK 10.0.401/runtime 10.0.12 y EF/Design/CLI 10.0.12 + Oracle Connector/NET EF 10.0.9, usando baseline/adopción separado y sin reproducir las 107 migraciones Pomelo.
- No hubo retarget productivo ni inicio de Fase 7. Evidencia: `docs/evidencias/modernizacion/PUNTO_27_DOCUMENTACION_ARQUITECTONICA_2026-10-05.md`.

## 2026-10-05 — Cierre formal de Fase 6 (STOP → PASS)

- Gate exact-head `37268035079` sobre `877af434ee15c8fd3a08f974bf58a62c91c03179`: todos los jobs `success`; `FASE_6_MYSQL_EF_PROVIDER=PASS`, `P0=0`, `P1=0`, `TARGETFRAMEWORK_CHANGE=ALLOWED_AFTER_PHASE6_CLOSE` y `PHASE7_EXECUTED=false`.
- Aceptación integral DEV `37268035136`, Fase 5 `37268035072`, Scope Lock `37268035185` y VAEP admission `37268035138` pasaron en el mismo HEAD.
- Se conserva el runtime net8/EF8/Pomelo hasta que se inicie Fase 7 por decisión separada. El cambio de estado no autoriza ni ejecuta retarget productivo, QA, `main` o PROD.
- Evidencia: `docs/evidencias/modernizacion/PUNTO_30_FASE6_STOP_A_PASS_2026-10-05.md`.

## 2026-10-05 — Punto 27: documentación arquitectónica reconciliada

- `ARCHITECTURE.md`, `PROJECT_CONTEXT.md` y `PROJECT_INDEX.md` identifican el mismo estado: runtime `net8.0`/EF Core 8/Pomelo 8; ruta futura certificada Oracle EF Core 10/net10 aislada; Fase 7 no ejecutada.
- La referencia vigente de Fase 6 es el run `37268913820` sobre el HEAD `2d84babb2863cbaadf0e201b9ff03ed113ecd476`; las referencias antiguas quedan como historia, no como certificación actual.
- Evidencia: `docs/evidencias/modernizacion/PUNTO_27_DOCUMENTACION_ARQUITECTONICA_2026-10-05.md`.

## 2026-10-05 — Reconciliación del cierre de Fase 6/provider

- El gate compuesto exact-head `37264684835`, sobre `d3a408487...`, certificó Fase 6 `PASS`; el workflow confirmó un único provider productivo Pomelo 8.0.2 y la suite MySQL 8.4 pasó 28/28.
- La ruta candidata net10.0 + EF Core 10.0.12 + `MySql.EntityFrameworkCore` 10.0.9 pasó en lanes aisladas. La ruta de migración/adopción Oracle no hace replay de las 107 migraciones Pomelo: parte del baseline SQL canónico y conserva un marcador de adopción; el rollback certificado vuelve a Pomelo sin alterar esquema ni datos.
- El runtime continúa net8.0 + EF Core 8/Pomelo. Fase 7 no se ejecutó; este cierre no cambia `.csproj` productivos ni autoriza despliegues.
- La entrada del 2026-10-04 que reporta `STOP` es una fotografía histórica previa a la resolución de la discrepancia de concurrencia; no representa el resultado vigente.

## 2026-10-04 — Gate arquitectónico MySQL/EF previo a net10

- El provider autoritativo vigente permanece Pomelo 8.0.2 / MySqlConnector 2.3.7 sobre EF Core 8.0.2 y net8.0; su baseline MySQL queda certificado con migraciones, SQL, semántica relacional, integración y aislamiento tenant.
- Oracle Connector/NET no es intercambiable sin migración arquitectónica: el historial Pomelo no reconstruye correctamente una base vacía y las excepciones cambian de MySqlConnector.MySqlException a MySql.Data.MySqlClient.MySqlException, rompiendo retry/traducciones existentes.
- Cambiar sólo TargetFramework a net10 queda prohibido: EF8 reproduce TypeLoadException en LINQ aunque la solución compile.
- La modernización net10 exige una lane EF/provider soportada y probada como unidad; hasta entonces el gate emite STOP y no altera runtime productivo.
- No se modifica QA, main, PROD, Aiven real ni ningún .csproj productivo.

## 2026-10-04 — Tooling frontend Fase 5

- Playwright Test pasa de 1.62.1 a 1.63.0.
- Vitest pasa de 4.1.11 a 5.0.3 sobre el target canónico `@angular/build:unit-test`.
- jsdom pasa de 29.1.1 a 30.0.1 exactamente según el alcance autorizado.
- html5-qrcode se conserva exactamente en 2.3.8; su mantenibilidad futura queda como deuda no bloqueante y no se sustituye en esta fase.
- Se preservan Angular 22.2.1, TypeScript 6.0.3, RxJS 7.8.2, tslib 2.8.1, Zone.js 0.16.3, Node 24.21.0 y npm 11.19.0.
- Alcance exclusivo dev; QA, main y PROD no se modifican.

## 2026-10-03 — Angular 21 → 22.2.1

- Frontend migrado major-a-major mediante `ng update` oficial: Angular Core/CLI 22.2.1.
- Angular Material/CDK migrados mediante schematics oficiales a 22.2.1.
- Matriz fijada exactamente a TypeScript 6.0.3, RxJS 7.8.2, tslib 2.8.1 y Zone.js 0.16.3; Node 24.21.0 y npm 11.19.0 se preservan.
- Se mantienen `@angular/build:application`, `@angular/build:dev-server` y `@angular/build:unit-test` con Vitest 4.1.11.
- No se adopta zoneless: `zone.js` permanece en polyfills y `provideZoneChangeDetection({ eventCoalescing: true })` sigue siendo explícito.
- Los schematics oficiales preservaron semántica existente mediante `ChangeDetectionStrategy.Eager`, `withXhr()` y ajustes de safe-navigation donde Angular 22 lo requirió.
- Run causal de migración: `37146078214` — SUCCESS; antes del push pasaron reinstalación limpia, 58/58 archivos + 217/217 unit tests, lint/contratos, audit productivo high+ y build configuration production sin deploy.
- Alcance exclusivo `dev`; QA, `main`, PROD, datos, secretos e infraestructura productiva no fueron modificados.

## 2026-10-03 — Angular 20 → 21

- Frontend migrado major-a-major mediante `ng update` oficial: Angular Core 21.2.25 / CLI 21.2.24.
- Angular Material/CDK migrados mediante schematics oficiales a 21.2.14.
- TypeScript queda en 5.9.3; Node 24.21.0 y npm 11.19.0 se preservan.
- `@angular/build` 21.2.24 + Vitest 4.1.11 soportan el target unit-test; build y dev-server quedan alineados a `@angular/build:application` / `@angular/build:dev-server` para evitar mezcla de familias de builders.
- La migración automática actualizó plantillas/control-flow y código compatible; no cambia backend, API, datos, RBAC, tenancy ni infraestructura productiva.
- Run causal de publicación: `37141861945` — SUCCESS antes de push.

## 2026-10-01 — Branding de factura tenant-owned y retiro del fallback global

- QuestPDF deja de consultar `AppSettings:LogoPublicUrl` como fallback global.
- La autoridad del logo de factura es `EmpresaConfiguracion.LogoUrl` propagada como `FacturaDto.EmpresaLogoUrl`.
- Si no existe logo válido o la descarga falla, se genera un monograma a partir del nombre de la empresa; se retira el monograma fijo de cliente.
- Esto elimina una dependencia de configuración global incompatible con multiempresa y mantiene `AppSettings__LogoPublicUrl` fuera del contrato Render.
- SMTP conserva su contrato OAuth2 actual; pruebas dirigidas validan que las claves legacy retiradas no son necesarias para configuración desplegada.

## 2026-10-01 — Paridad fail-closed DEV/QA/PROD en Render, GitHub y Aiven

- `RenderEnvironmentContractGuard` valida en startup exactamente 28 claves Render administradas, sin exponer valores, y mantiene separadas las identidades/secretos propios de DEV, QA y PROD.
- `EnvironmentDatabaseGuard` amplía la frontera de datos a los tres entornos y exige endpoint Aiven corporativo, puerto `14402` y `SslMode=Required` además de la pareja base/usuario.
- Se añade `Environment infrastructure parity` para certificar por GitHub Environment la estructura 4 variables + 3 secretos, binding MySQL, mínimo privilegio, denegación cross-env, token Aiven y passphrase de backup.
- QA demostró el contrato Render exacto en runtime; DEV detectó y bloqueó ocho variables legacy antes de reemplazar la instancia sana, dejando la limpieza como deuda de control-plane explícita.
- La topología Vercel queda definida como tres proyectos corporativos con binding por `VERCEL_PROJECT_ID`; QA requiere que su Production Branch canónico sea `qa`.
- No se introducen datos, migraciones ni servicios pagos.

## 2026-10-01 — Vercel runtime binding por identidad de proyecto

- `frontend/server/environment-binding.js` deja de depender de variables manuales como requisito primario y usa `VERCEL_PROJECT_ID` como identidad inmutable del deployment.
- Sólo los project IDs corporativos DEV y PROD están allowlisted; cada uno resuelve un único API upstream, public origin y política SEO.
- Variables explícitas, si existen, se validan como overrides de coherencia; cualquier contradicción, project ID desconocido o intento de cruce falla cerrado.
- Se conserva la prohibición de seleccionar backend por hostname/alias y no se introduce fallback entre entornos.
- Sin datos, migraciones, secretos ni servicios pagos.

## 2026-09-30 — Guard fail-closed Render → MySQL por entorno

- Se incorpora `EnvironmentDatabaseGuard` en el arranque de API cuando `RENDER=true`.
- `Development` sólo admite `solqaryn_dev` con `solqaryn_dev_user`; `Production` sólo `solqaryn_prod` con `solqaryn_prod_user`.
- Base cruzada, usuario cruzado, cadena incompleta o entorno ambiguo/no canónico abortan startup antes de registrar/usar `AppDbContext`.
- El guard se valida con pruebas negativas y deja preparado el mismo código para una futura promoción autorizada a PROD sin modificar infraestructura productiva ahora.
- Sin migraciones, escrituras de datos, cambios en `main`, secretos ni servicios pagos.

# ARCHITECTURE_CHANGELOG — Solqaryn

## 2026-09-30 — Binding Vercel/API fail-closed por proyecto

- Se elimina la selección de backend mediante hostname y el fallback genérico hacia DEV.
- `/api/*` usa un proxy server-side local y `environment-binding.js` exige `SOLQARYN_ENV` + `API_UPSTREAM` coherentes.
- DEV sólo admite Render DEV y PROD sólo Render PROD; alias/custom domain no seleccionan entorno.
- SEO/canonical usa `PUBLIC_ORIGIN` + `SEO_INDEXING_ENABLED`; un entorno desconocido o cruzado falla cerrado.
- Sin cambios en main/PROD, datos, migraciones ni servicios pagos.

## 2026-09-29 — Cache HTTP + ETag + compresión del storefront

- ASP.NET Core habilita Brotli/Gzip para JSON/text sobre HTTPS.
- Toda respuesta backend parte de `private, no-store, max-age=0`; sólo GET públicos allowlisted usan `PublicHttpCacheAttribute`.
- Identidad/tema/WhatsApp/categorías: `max-age=120, s-maxage=300, stale-while-revalidate=600`.
- Bootstrap: `max-age=15, s-maxage=30, stale-while-revalidate=60` por incluir destacados.
- Productos/listados/detalle: `max-age=5, s-maxage=15, must-revalidate`.
- Respuestas públicas cacheables emiten ETag débil SHA-256 y resuelven `If-None-Match` con 304; `Vary: Accept-Encoding` preserva equivalencia con compresión.
- Contexto de carrito, checkout, sesión, administración, documentos y errores permanecen fuera de cache público.
- Vercel marca bundles Angular hashados como immutable por un año y habilita caching de rewrites API sólo para respetar el `Cache-Control` upstream.
- Sin persistencia nueva, migraciones, secretos ni servicios pagos.

## 2026-09-29 — Cache público tenant-aware en dos niveles

- Angular conserva el observable de identidad en vuelo y comparte la lista de categorías entre rutas mediante replay, evitando solicitudes concurrentes duplicadas.
- El backend incorpora `IPublicStoreCache` con `PublicStoreMemoryCache` sobre `IMemoryCache`; las claves incluyen tenant, segmento, generación y hash de parámetros.
- TTL: identidad/tema/categorías 5 min; destacados 30 s; listados 15 s. Detalle, contexto de carrito y checkout permanecen fuera de cache.
- La cache usa lock por key contra stampede y generaciones para invalidación.
- `AppDbContext.SaveChangesAsync` invalida después de escrituras relevantes de producto/variante/imágenes/stock, categorías, identidad/WhatsApp, tema, marca/modelo y descuentos.
- La partición tenant usa `empresa:{id}` cuando la identidad pública se resuelve inequívocamente; ante ambigüedad usa `public-config:{id}` fail-safe.
- Topología vigente: cache in-process por instancia. Un escalado horizontal futuro exige implementación distribuida e invalidación compartida antes de asumir coherencia entre instancias.
- Sin migraciones, persistencia nueva, secretos, PROD, `main` ni servicio pagado.

## 2026-09-29 — Bootstrap único del storefront público

- Se incorpora `GET /tienda/bootstrap` como carga inicial canónica de identidad pública mínima, WhatsApp público resuelto, tema visual, hasta 6 categorías de navegación y hasta 4 destacados ligeros.
- La composición vive en `ITiendaBootstrapService`/`TiendaBootstrapService` y reutiliza autoridades existentes; no crea persistencia ni fuente de verdad paralela.
- El bootstrap resuelve WhatsApp mediante `IWhatsAppPublicoService`/`WhatsAppPublicoService`, preservando las reglas públicas vigentes y sin exponer referencias de secretos.
- Angular comparte el bootstrap entre consumidores concurrentes mediante `shareReplay`; identidad, tema y portada ya no generan el stampede de requests en el camino feliz.
- Los endpoints públicos previos permanecen disponibles para recovery y pantallas específicas.
- Sin migraciones, PROD, `main` ni servicio externo/pagado.

## 2026-09-29 — Separación resumen público vs detalle rico

- `GET /tienda/productos` y `/tienda/productos/destacados` pasan a `TiendaProductoResumenDto` + `ProductoCatalogoResumenReadModel`.
- El resumen conserva identidad, categoría, precio/oferta, disponibilidad, imagen principal y variantes mínimas; elimina galerías de variantes, color/talla y descripción completa del payload de listado.
- `GET /tienda/productos/{slug}` conserva el DTO rico y es la única lectura general que carga la galería completa del producto.
- `POST /tienda/productos/contexto` conserva el contrato rico acotado a IDs persistidos para rehidratación de carrito/cuenta.
- No hay migraciones, nueva persistencia, nueva fuente de verdad ni servicio externo/pagado.

## 2026-09-29 — Read path público ligero para catálogo/storefront

- Se separó la consulta pública de productos del repositorio administrativo con includes completos.
- Nueva vía: `ICatalogoPublicoService` + `IProductoCatalogoPublicoRepository` + proyecciones específicas.
- `GET /tienda/productos` pagina y filtra server-side; detalle carga galerías sólo para un producto.
- `POST /tienda/productos/contexto` rehidrata únicamente los IDs persistidos de carrito/cuenta.
- Inventario/promociones conservan sus autoridades existentes; no hay nueva persistencia ni migración.
- El frontend deja de usar una operación de “descargar catálogo completo” como dependencia transversal.


## 2026-09-28 — Baseline first-party de rendimiento DEV

- Instrumentación transversal DEV para separar tiempo HTTP de cantidad/tiempo MySQL sin registrar SQL, parámetros ni secretos.
- Angular mide TTFB, requests/bytes por pantalla y LCP/INP/CLS únicamente en DEV/local.
- Baseline reproducible de bundles raw/gzip/Brotli sin SaaS de observabilidad ni servicio pagado.
- PROD permanece con el baseline desactivado por defecto.
- Rutas: `backend/src/API/Observability`, `backend/src/API/Middleware/RequestObservabilityMiddleware.cs`, `frontend/src/app/core/performance`, `frontend/scripts/performance-bundle-baseline.mjs`, `docs/PERFORMANCE_BASELINE_DEV.md`.


## 2026-09-24 — Topología Aiven canónica y environments SOLQARYN

- GitHub queda normalizado a dos environments canónicos: `Desarrollo` y `Produccion`.
- Aiven usa el proyecto `solqaryn` y un único servicio MySQL Free `solqaryn-mysql`.
- Desarrollo usa `solqaryn_dev` + `solqaryn_dev_user`; Produccion usa `solqaryn_prod` + `solqaryn_prod_user`.
- `avnadmin` queda reservado para administración y los usuarios de aplicación fueron restringidos a su base respectiva.
- La separación entre DEV y PROD es lógica dentro del mismo servicio físico; host/puerto/recursos se comparten, mientras base, usuario, secretos y GitHub Environment permanecen aislados.
- La prueba Aiven del Environment Desarrollo y el backup cifrado + restore same-artifact en MySQL descartable quedaron validados en GitHub Actions sin exponer secretos.


Registro conciso de cambios que obligan a actualizar `PROJECT_INDEX.md`, `PROJECT_CONTEXT.md` o `ARCHITECTURE.md`. No reconstruye historial anterior.

## Convención

Cada entrada debe indicar fecha, cambio observable, documentos/rutas afectados y verificación. Solo se agrega cuando cambian arquitectura, módulos, integraciones, rutas/API, datos o comandos documentados.

## 2026-08-24 — Guía operativa por dominio

- Cambio: `PROJECT_INDEX.md` amplió el mapa por capas a una matriz navegable por dominio y flujos transversales.
- Cobertura: frontend, API, Application, Domain, Infrastructure, DB/integraciones y migraciones ancla de los dominios principales.
- Alcance: se definieron límites explícitos para evitar reinspecciones globales ante cambios locales.
- Verificación: rutas y archivos citados contrastados con el checkout; guard documental y diff validados; sin cambios de código o configuración.

## 2026-08-24 — Contexto histórico ChatGPT/VAEP

- Cambio: se incorporó `docs/CONTEXTO_CHATGPT_VAEP.md` y se enlazó desde `PROJECT_INDEX.md`.
- Alcance: automatización VAEP, validación causal, cadena compras-recepciones-reservas-facturación, no duplicación y consulta selectiva.
- Límite: referencia histórica/operativa de Solqaryn; las fuentes canónicas y el HEAD actual prevalecen.
- Verificación: enlaces y guard documental local comprobados; sin cambios en código de producción.

## 2026-08-24 — Inicialización del mapa técnico persistente

- Cambio: se consolidó `PROJECT_INDEX.md` como mapa operativo y se agregó un índice de decisión para cambios frecuentes.
- Cobertura: backend .NET por capas, frontend Angular por features, puntos de entrada, API, persistencia, configuración, dependencias, comandos y pruebas.
- Evidencia: inspección estática selectiva de manifiestos, solución/proyectos, `Program.cs`, rutas Angular, controladores, `AppDbContext`, migraciones y directorios de pruebas.
- Verificación: rutas, archivos, scripts y ejecutables de comandos comprobados localmente; no se ejecutó la aplicación ni se modificó código de producción.


## 2026-09-30 — Identidad técnica canónica y storefront tenant-neutral

- Se normalizaron nombres de solución, proyectos, namespaces, build outputs, claves técnicas, tests y scripts a SOLQARYN.
- El frontend público quedó desacoplado de nombres comerciales y pasó a un módulo genérico de storefront con ruta técnica /tienda.
- No hubo migración ni eliminación de datos; los valores históricos persistidos permanecen bajo control de datos/migraciones explícitas.

## 2026-10-14 — P4 SQL pesimista transversal: barrera empresarial antes de FOR UPDATE (candidato DEV)

- Nuevo `CommercialTenantSqlLockScope.RequireAsync`: exige marca comercial interna RBAC para accesos SQL directos si existen dos o más empresas, activas o no. Una marca de tienda pública/PDF jamás autoriza locks privados. Solo 0–1 empresas conservan el comportamiento histórico sin marca.
- Predicados parametrizados `EmpresaId` dentro del propio `SELECT ... FOR UPDATE` en 24 archivos de repositorios y servicios: cajas/sesiones, cotizaciones/pedidos/preparación, compras/suministros, inventario/variantes/existencias/almacén/trazabilidad, devoluciones, cuentas por pagar/facturas de proveedor, políticas de costeo y cargas masivas. Se comprueba antes de realizar el bloqueo, sin depender de `IgnoreQueryFilters` o composición EF posterior.
- `CargaMasivaService`: páginas, detalle y confirmación usan rol de membresía empresarial vigente con autorización privada; ya no aceptan `ICurrentUserService.EsAdministrador` de otra empresa como autoridad. Los locks de sus documentos/productos/variantes incluyen EmpresaId en SQL; para 0–1 empresas mantiene contrato antiguo.
- Prueba de alcance directo: dos empresas, una desactivada, sin RBAC o con marca pública = denegado; marca comercial A o B = alcance propio; empresa única = compatibilidad. La prueba integrada MySQL A/B sobre locks de productos permanece en la suite consolidada de P4.
- **No certificado, no habilitar multiempresa todavía**: CI exact-head/MySQL reales efímeros, regresiones de cargas, revisión de seguridad y readback de DEV obligatorios. No hay DDL/backfill ni cambios en QA/PROD; continúa la cuarentena HTTP y el dominio PROD aplazado.

## 2026-10-14 — P4 CI causal: el conteo previo a FOR UPDATE no debe abrir snapshot MySQL

- En el HEAD consolidado de PR #3623, el gate `CommercialTenantSqlLockScope.RequireAsync` hacía `SELECT COUNT` de empresas antes del lock `FOR UPDATE`. Bajo REPEATABLE READ, ese SELECT consistente abre el read view y puede permitir que dos transacciones concurrentes intenten insertar la misma variante técnica. El test de integración `ProductoVarianteTecnicaLifecycleIntegrationTests.AsegurarTecnica_Concurrente_CreaExactamenteUnaVariante` detectó duplicado en `IX_ProductoVariantes_ProductoTecnicoUnico`.
- Corrección limitada: en provider MySQL, el gate de 0–1/2+ empresas utiliza `SELECT e.Id AS Value FROM Empresas e ORDER BY e.Id LIMIT 2 FOR SHARE` (current read compartida) y evita crear snapshot MVCC antes de bloquear el documento; InMemory conserva el conteo para tests de contrato.
- Sin alterar DDL, integridad, propietarios, aislamiento A/B, credenciales ni QA/PROD; no se retira cuarentena P0. Cierre condicionado al nuevo CI exact-head, incluida la regresión de concurrencia MySQL. No equivale a certificación P4 integral.


## 2026-10-18 — DEV replay attestation temporal, opt-in (candidato)

Se incorpora un trabajador DEV restringido a Render Development y Aiven `solqaryn_dev`, apagado por defecto. Reusa el HMAC existente solo en memoria para una prueba GET de identidad por loopback (aceptacion/replay/reintento fresco) y confirma el digest en la tabla anti-replay. No altera contrato HTTP, schema empresarial, migraciones, RBAC ni rutas comerciales. Auditoria estatica y unitarios de registro obligatorio. Se retira tras certificacion; ver runbook. NO declarar LISTO sin ejecucion LIVE y controles exact-head.

## 2026-10-18 — DEV: readback con evidencia HMAC/suspension compuesta, retirado worker efimero

Tras tres PASS reales de Render sobre SHA `49195f34...` con firma 200, replay 503, firma fresca 200 y MySQL ledger correcto, la certificacion independiente incorpora la evidencia autentica de suspension/reactivacion de los intentos 5/6 del run 39084418982. Se reemplaza la exigencia imposible de una empresa suspendida y dos activas al mismo tiempo por una atestacion fija de autoria corporativa, con PR fusionado, comentario no alterado, rutas de guard/signer/proxy/binding byte-identicas al SHA acreditado y readback MySQL/HTTP actual. El estado `PASS_ATTESTED` queda separado de las pruebas ejecutadas en la corrida. Se retira el worker temporal, su flag de appsettings, su excepcion en scanner, tests y runbook; sin variar middleware HMAC, bindings, DDL, datos, permisos, QA ni PROD. El certificado y manual vigente estan en `docs/certifications/`.
