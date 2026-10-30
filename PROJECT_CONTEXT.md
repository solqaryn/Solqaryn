## 2026-10-23 — AUTORIDAD HISTÓRICA M3.1 (3+3, sustituida 2026-10-30)

En ese corte se autorizó el diseño 3+3; posteriormente se instaló el inventario y se autorizó individualmente un allowlist comercial 5/6. El 3+3 queda como fotografía histórica, sustituida el 2026-10-30 por inventario configurable sin techo interno fijo. La nueva regla no amplía permisos ni cambia las Tasks físicas actuales. Priorizar revisión de PR en VALIDANDO, autocorrección en el mismo PR y revisión ajena de los commits del revisor; la microtarea solo libera dependientes después de LISTO respaldado por merge protegido y readback del Sheet. Ramas temporales por microtarea con limpieza automática y máximo dos scopes concurrentes. VAEP/Jules retirados; qa/main/PROD excluidos. Único contrato nuevo: `docs/AUTOMATION_3X3_OPERATING_CONTRACT.md`; `docs/AUTOMATION_STATUS.md` conserva el contador físico vigente. El PR #3734 ya fue integrado a dev mediante 17ca160ae5f2d1e20ef27b1820e670fbfb1099d8; esta migración conserva íntegros sus ocho archivos y sus controles de autoridad comercial. Los registros históricos siguientes no conceden autoridad vigente.

---

## 2026-10-23 — P0-012.A | Autoridad documental del Plan Comercial Inicial

- Microtarea `P0-012.A`, origen `PLAN_MAESTRO!A24:R24` y `COLA!A24:N24` del Sheet institucional: `P0`, `PENDIENTE`, sin dependencias previas, `DEPENDENCIAS_OK=SI` al iniciar el trabajo. La certificación independiente `P0-012.B` sigue pendiente y depende de esta entrega.
- Se comprobó el merge del contrato [PR #3733](https://github.com/solqaryn/Solqaryn/pull/3733), HEAD base `3a98afbf290e8895e177861183b623ed3cb5bed0`; autoridad operativa viva en `docs/AUTOMATION_STATUS.md`: `AUTOMATION_RUNTIME=ACTIVE_DEV`, `SCHEDULED_CHATGPT_TASKS_AUTHORIZED=1`, VAEP y Jules sin autorizar. Únicamente la Primary de Javier puede preparar ramas/PRs DEV; Alex mantiene revisión separada READ_ONLY. No hay autorización de merge ni de QA/PROD.
- `docs/PROJECT_SCOPE_LOCK.md` fija `COMMERCIAL_AUTHORITY_MODE=CURRENT_PLAN_ONLY` y `LEGACY_MASTER_EXECUTABLE=FALSE`: el único plan ejecutable es [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit). El Maestro antiguo (SQ-000..SQ-353 y Word histórico) queda como visión futura no ejecutable; ninguna Task, Sheet ni documentacion histórica lo reactivan.
- Se armonizaron `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, `docs/AUTOMATION_STATUS.md` y `docs/AUTOMATION_AUTHORITY.md` para separar el piloto DEV vigente de los registros históricos M1-M3. Se ampliaron pruebas negativas en `scripts/validate-automation-authority.mjs` para impedir regresiones a autoridad histórica o cambio silencioso de fuente comercial.
- Entrega no productiva: rama `work/solqaryn-p0-012-a` hacia `dev`; las pruebas efectivas y revisión independientes deben comprobarse en CI del PR antes de certificar `P0-012.B`. No se alteran ERP, base de datos, secretos, Sheets, despliegues ni otras ramas. Rollback: revertir únicamente los commits de este PR mediante nuevo PR protegido, sin reactivar autoridad heredada.

---

## 2026-10-22 — Migración documental nativa (autoridad comercial vigente)

- Fuente comercial aprobada: [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), Google Docs nativo, 492 unidades y 1.123 microtareas con texto y estructura certificados; sustituye el ID Word histórico `1T41ppLvC3x-p6sORCtpaPvzLWPP9_Z7j`.
- Seguimiento documental: [STANDARD DE AUTOMATIZACIONES EN SOLQARYN](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), 1.123 tareas en PENDIENTE, `PLAN_MAESTRO` editable, `COLA` calculada y `BITACORA` inicial vacía, sin ejecución automática.
- [STANDARD AUTORIDAD OPERATIVA VAEP](https://docs.google.com/document/d/1A525NGt09mBiZez_r6hFcdCYAdoOAwOZM--N0uXPBOQ/edit) conserva el contrato como referencia histórica de las diez Tasks retiradas; no habilita leases, cron ni despachos.
- Los enlaces e IDs que aparecen en secciones anteriores al corte son evidencia histórica, no referencias vigentes. El ID nuevo rige para validaciones de autoridad; la versión V3 y las 1.123 descripciones se conservan íntegramente.

---

## 2026-10-22 — Estado vigente después del retiro de automatizaciones

**ÚNICA AUTORIDAD OPERATIVA:** `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, `docs/AUTOMATION_STATUS.md` y el Plan Comercial Inicial Unificado. El antiguo motor VAEP de diez Tasks fue retirado. No existen supervisores, cron de despacho, epochs, leases ni manifest de coordinación autorizados. El ruleset exige solamente `verify-solqaryn-project-scope`, `SOLQARYN - Seguridad multiempresa` y `SOLQARYN - Build y pruebas backend`.

**El trabajo comercial nuevo no hereda fases, gates ni prioridades de planes anteriores.** Los apartados cronológicos anteriores al 22-10-2026 se conservan como antecedentes técnicos auditables, pero no autorizan tareas, despachos, escritura en Google Sheets, integración ni promoción a QA/PROD. El código de seguridad, compilación, base de datos, frontend y las pruebas funcionales reales del ERP permanece vigente según su impacto técnico.

---

## 2026-10-21 — VAEP actual: codigo CAS retirado en propuesta, ejecucion NO_GO

El Plan Comercial Inicial Unificado sigue siendo la unica fuente de trabajo (1123 scopes). Sus 10 Tasks estan habilitadas y las 10 `WRITE_POLICY` y `LAST_HEAD` se sincronizaron en Sheets, pero `CONFIG.RUNTIME_MODE=PAUSADO`. El manifiesto esta en `PRE_GO_CONTRACT_CERTIFIED`, no autoriza claims y acredita administrativamente solo :00 y :29. Se retira el escritor `DeveloperMetadata` CAS dual de la rama candidata y se establece el motor logico `addNamedRange` como unica ruta futura, sin habilitarla ni limpiar locks historicos. No se ha certificado equivalencia con las automatizaciones de VarApp ni la terminalidad del proveedor ChatGPT; nada de 10 writers comercialmente activos.

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

## 2026-10-14 — P4: 12 claves naturales empresariales restantes (candidato DEV)

- Reconciliación del antiguo PR #3607 con el HEAD DEV posterior a #3633, sin repetir la migración `20261014112000_PublicStoreAccountEmailPerEmpresa`. Las 12 restricciones naturales de devoluciones, compras, recepciones, reservas, series, solicitudes, sucursales y roles cambian de unicidad global a `(EmpresaId, clave)`. Las cuentas de tienda **ya** conservan su índice `UX_TiendaCuentasCliente_Correo` compuesto por `(EmpresaId, CorreoNormalizado)`.
- Nueva migración Oracle `20261014130000_CommercialTenantNaturalKeysPostStorefront`: expand-first (crear índice compuesto antes de retirar unicidad global); preserva datos históricos y una columna generada única para roles plantilla con `EmpresaId IS NULL`. No hace backfill, no cambia dominios ni altera QA/PROD. El rollback de índices globales queda prohibido sin respaldo.
- Validaciones de modelo, DDL físico MySQL, fixture legacy descartable y fresh bootstrap comprueban 12 claves + el índice de tienda ya certificado. Esta rama es **candidata**: requiere REVIEW_FIRST, CI exact-head, pruebas MySQL reales y merge antes de declarar integración. No retira la cuarentena HTTP `503` ni certifica operaciones A/B completas.

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

## 2026-10-14 — P4 admin reports tenant isolation (PR #3614, candidato DEV)

- ReportesAdministrativos: exige tenant RBAC verificado en resumen, usuarios, roles, auditoría y exportaciones CSV/XLSX/PDF. No acepta como autorización marcas de tienda pública ni token PDF.
- Roles efectivos y recuentos proceden de UsuarioEmpresa.RolId del tenant (no del Usuario.RolId global legacy). Las consultas de auditoría y sus archivos filtran EmpresaId explícitamente; filas sin dueño permanecen en cuarentena.
- Tests negativos: sin RBAC, capability pública, tenant A/B con roles globales divergentes, CSV y auditoría sin contaminación cruzada; se preserva la prueba PDF con RBAC legítimo.
- No DDL ni backfill; cuarentena HTTP de 2+ empresas preservada. **No certificar P4 integral** hasta CI causal exact-head y demás vías privadas, workers, archivos y aceptación MySQL A/B.

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

## 2026-10-13 — Candidato DEV: lecturas empresariales por marca RBAC (punto #4)

- Se registra un filtro EF generalizado que combina el owner `EmpresaId` con los filtros previos cuando `VerifiedCommercialTenant` está presente. No hay atribución de registros históricos y los roles/permiso de plantilla siguen compartidos por diseño.
- Esto constituye contención adicional, no la certificación integral de #4: rutas sin marca, tienda anónima, inventarios/jobs/SQL directo, exports, archivos y filtros omitidos aún requieren auditoría A/B. La cuarentena de segunda empresa permanece hasta cierre integral; QA/PROD intocados.

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

# PROJECT_CONTEXT — SOLQARYN

### Actualización P0 #3 — PR #3599 (13 octubre 2026; candidato, NO fusionado)

Se añadieron unicidades comerciales por EmpresaId e índices compuestos de cinco raíces. Variantes, imágenes, detalles de compra y venta y facturas reciben EmpresaId nullable derivado/validado, además de doce claves foráneas compuestas en MySQL. Se incorporó migración Oracle EF10 y contratos físicos de bootstrap. Esto es un tramo de la solución, NO su certificación: quedan entidades comerciales descendientes por clasificar/proteger, filtros de lectura y validación MySQL exact-head. No levantar cuarentena de segunda empresa, no tocar QA ni PROD y no reasignar los 19 registros descartables.


> Contexto técnico canónico de estado actual. Este archivo describe únicamente la realidad vigente necesaria para trabajar sobre SOLQARYN.

## 1. Identidad y repositorio

- PROJECT_ID: `SOLQARYN`
- PROJECT_SCOPE_LOCK: `STRICT`
- `PROJECT_SCOPE_LOCK=STRICT`
- Plataforma: SOLQARYN.
- Repositorio: `solqaryn/Solqaryn`.
- Rama ordinaria de trabajo: `dev`.
- Rama productiva: `main`; cualquier cambio requiere autorización explícita vigente.
- GitHub Environments canónicos: `DEV`, `QA` y `PROD`.
- Identidad corporativa operativa: `solqaryn.platform@outlook.com`.
- Las empresas cliente/tenants no definen la identidad técnica ni operativa de SOLQARYN.
- El árbol versionado vigente aplica un gate repository-wide que rechaza cualquier identificador de plataforma retirado y la terminología inglesa heredada del dominio de inventario; no existe una identidad paralela a SOLQARYN.
- `Inventario` permanece como módulo funcional ERP legítimo en español. Sus rutas administrativas canónicas viven bajo `/inventario/...`; no existe un wrapper `/app/inventario/...` ni un store/plataforma legacy separado.

## 2. Regla de estado vivo

Antes de actuar:

1. leer `docs/VAEP_AUTHORITY.md`;
2. releer HEAD vivo de `dev`;
3. consultar sólo el estado operativo necesario para la tarea;
4. validar dependencias técnicas reales del scope;
5. usar CI/tests/readbacks causales cuando corresponda.

Ningún plan, fila, gate, fase o secuencia que no esté incorporado al MAESTRO vigente puede condicionar trabajo nuevo.

## 3. Arquitectura vigente

- **Protección comercial PR #3596 integrada en dev (d851ae19):** `AppDbContext` obtiene `VerifiedCommercialTenant` desde `IHttpContextAccessor` cuando existe marca post-RBAC y bloquea escrituras comerciales a owners explícitos distintos; asegura nuevos grafos de venta/compra en la misma transacción. No aísla lecturas ni datos históricos `EmpresaId=NULL`; no retirar cuarentena.

- Bitácora nueva tenant-aware: `AuditoriaService` firma `RegistroAuditoria.EmpresaId` sólo desde marca autorizada de RBAC. Las filas históricas y jobs sin contexto siguen sin atribución; no equivale a filtro empresarial de auditoría.


- Análisis P0 del snapshot Oracle EF10: 132 entidades mapeadas, 20 con `EmpresaId` y 66 entidades adicionales conectadas directa/indirectamente con cinco raíces comerciales. Inventario en `docs/tenancy/INVENTARIO_GRAFO_COMERCIAL_P0_DEV.md`. No clasificar las restantes como globales por omisión ni retirar la cuarentena.


- Validación parcial de escritura `CommercialCrossTenantWriteGuard`: si una venta/compra/detalle y su referencia ya tienen owners positivos diferentes, SaveChanges rechaza la asociación. Los NULL históricos permanecen sin atribuir y no se considera aislamiento por fila completo.


SOLQARYN es una plataforma empresarial multiempresa.

- Frontend: Angular 22.2.1 standalone, Signals y Angular Material/CDK 22.2.1. Toolchain frontend vigente: Node.js 24.21.0 LTS + npm 11.19.0 + TypeScript 6.0.3 + RxJS 7.8.2 + tslib 2.8.1 + Zone.js 0.16.3; `provideZoneChangeDetection` y el polyfill `zone.js` permanecen activos, sin conversión a zoneless. CI usa patches exactos y Vercel se gobierna por `engines.node=24.x`.
- Identidad técnica de código: namespaces/assemblies/proyectos usan Solqaryn.*; el storefront fuente es tenant-neutral y vive bajo features/storefront.
- Backend: ASP.NET Core 10 Web API sobre `net10.0`, C# 14, SDK 10.0.401 y runtime/ASP.NET 10.0.12.
- Capas: Domain <- Application <- Infrastructure; API compone y expone.
- Persistencia: MySQL con EF Core 10.0.12 y Oracle `MySql.EntityFrameworkCore` 10.0.9.
- Migración de versiones: Fase 7 está integrada y certificada en `dev`. El toolchain canónico es SDK 10.0.401, runtime/ASP.NET 10.0.12, `net10.0`, C# 14, EF Core/Design/CLI 10.0.12, JwtBearer 10.0.12 y provider Oracle `MySql.EntityFrameworkCore` 10.0.9. Los proyectos productivos no referencian Pomelo ni MySqlConnector; las 107 migraciones históricas Pomelo se preservan fuera del runtime para auditoría/rollback histórico, mientras las migraciones activas usan el assembly Oracle y `OracleBaseline`. El bootstrap fresh produce 136 tablas, `pending-model=0`, y CI conserva los contratos 1062/1205/1213, tenancy fail-closed y la integración MySQL. HEAD post-merge certificado: `7013a55cd7491c3c79d6808df5db9ba0b8580f40`. No se ha promovido Fase 7 a `main`, QA ni PROD.
- Fase 8 NuGet: FluentValidation 12.1.1 + DI oficial con filtro async propio; FluentValidation.AspNetCore retirado; BCrypt.Net-Next 4.2.0; Swashbuckle.AspNetCore 10.2.3; CloudinaryDotNet 1.29.3; ClosedXML 0.105.1; IdentityModel 8.23.0 alineado; SkiaSharp 4.152.1 como sanitizador de imágenes; ImageSharp 3.1.12 retirado por advisories NuGet; caching/JSON siguen resueltos por .NET 10 sin PackageReference redundante.
- Fase 10 tooling de pruebas .NET 10: `Microsoft.NET.Test.Sdk 18.10.1` y `Moq 4.21.0`; `Microsoft.EntityFrameworkCore.InMemory` y `.Sqlite` conservan `10.0.12` en consonancia con EF Core productivo `10.0.12`; `xunit 2.8.1` y `xunit.runner.visualstudio 2.8.2` permanecen en v2. Gate dedicado `modernization-phase10-testing-net.yml`; la evaluación de xUnit v3 se separa de este cambio. Solo test/CI, no runtime, provider ni esquema productivo.
- Seguridad: JWT, BCrypt, RBAC relacional, auditoría, CORS explícito, rate limiting y security headers.
- Integraciones vigentes: Cloudinary, QuestPDF y SMTP; DEV, QA y PROD usan Outlook.com con OAuth2/Modern Auth para `solqaryn.platform@outlook.com`.
- Facturas PDF: el branding visual se resuelve por empresa/tenant (`EmpresaConfiguracion.LogoUrl`); si no existe logo válido, QuestPDF usa un monograma derivado de `EmpresaNombre`. No existe fallback global `AppSettings__LogoPublicUrl` en Render ni branding fijo de un cliente.
- E2E/browser: Playwright/Chromium.
- Baseline de rendimiento DEV first-party: duración API, cantidad/tiempo de queries MySQL, TTFB/requests/bytes por pantalla, LCP/INP/CLS y tamaños de bundles; no requiere un servicio de observabilidad pagado.
- Storefront público de productos: read path dedicado con proyecciones ligeras, paginación/filtros server-side y contexto de carrito por IDs; `GET /tienda/productos` y destacados usan `TiendaProductoResumenDto` con imagen principal y variantes mínimas, mientras el DTO rico con galería queda reservado al detalle/contexto acotado; las lecturas públicas ya no materializan el catálogo administrativo completo.
- Bootstrap storefront: `GET /tienda/bootstrap` es la carga inicial canónica y pequeña para identidad pública, WhatsApp, tema, hasta 6 categorías de navegación y 4 destacados; Angular deduplica consumidores concurrentes y reserva las lecturas públicas separadas para recovery o pantallas específicas.
- Cache storefront: Angular comparte identidad en vuelo y categorías entre rutas; backend usa `IMemoryCache` tenant-aware con invalidación generacional. TTL vigentes: identidad/tema/categorías 5 min, destacados 30 s y listados 15 s. Detalle/contexto/checkout no se cachean. Si Render/API pasa a múltiples instancias simultáneas, esta implementación local debe sustituirse por una cache distribuida con invalidación compartida antes de asumir coherencia cross-instance.
- Cache HTTP storefront: ASP.NET comprime JSON/text con Brotli/Gzip y usa `no-store` por defecto. Sólo GET públicos allowlisted emiten `Cache-Control` público + ETag/304: identidad/categorías con TTL moderado y SWR, bootstrap corto por contener destacados, productos con TTL corto/must-revalidate. Vercel marca bundles Angular hashados como immutable por un año; el proxy server-side preserva `Cache-Control`/ETag/Vary emitidos por el backend. Sesión, contexto, checkout y administración nunca son cache público.
- La autorización del backend es la autoridad; la UI nunca sustituye controles de seguridad.
- **P0 de tenancy comercial vigente:** Aiven DEV tiene EmpresaId nullable en cinco raíces y protecciones parciales #3596; #3597 completó auditoría de procedencia. Los 19 registros históricos son PRUEBAS descartables por decisión del propietario (2026-10-13): no se hará backfill ni búsqueda adicional de dueño histórico. La futura limpieza preservará usuarios, credenciales, roles, permisos, membresías y relaciones imprescindibles de autenticación; no se ha ejecutado. El P0 real consiste en filtrar y proteger todos los DATOS FUTUROS de cada empresa en SQL, APIs, relaciones, storefront público, archivos, inventarios, finanzas, jobs y caché con pruebas A/B. Hasta ese certificado continúan cuarentena y bloqueo de alta de segunda empresa.
- Tenancy, integridad transaccional y trazabilidad deben preservarse en cambios de negocio.

Consultar `ARCHITECTURE.md` para cambios estructurales y `PROJECT_INDEX.md` para navegación dirigida.

## 4. Infraestructura canónica

### GitHub
- Repositorio: `solqaryn/Solqaryn`.
- Trabajo: `dev`.
- Environments: `DEV`, `QA` y `PROD`.

### Render
- Servicio DEV: `solqaryn-api-dev`.
- Servicio QA: `solqaryn-api-qa`.
- Servicio PROD: `solqaryn-api-prod`.
- Estado de compute actual: DEV, QA y PROD están en Render `free`. DEV/QA pueden aceptar cold starts para ahorro; PROD no se considera backend comercial `always-on` mientras permanezca Free. Está prohibido usar keep-alive artificial como sustituto de un plan always-on; cualquier upgrade productivo requiere autorización explícita de gasto.
- Health de plataforma Render: `/health` (liveness rápida). `/health/ready` se conserva para readiness/diagnóstico de dependencias como MySQL, pero no como probe de despliegue.
- SMTP DEV, QA y PROD: `smtp-mail.outlook.com:587` + STARTTLS + OAuth2/Modern Auth con identidad `solqaryn.platform@outlook.com`.
- DEV, QA y PROD no provisionan contraseña SMTP ni client secret OAuth2; cada entorno mantiene su propio refresh token en Render.
- Contrato Render canónico: 28 claves idénticas por nombre en DEV, QA y PROD; sólo cambian valores dependientes del entorno. Inventario y justificación: `docs/RENDER_ENVIRONMENT_CONTRACT.md`.
- En Render, el arranque backend aplica `EnvironmentDatabaseGuard`: `Development` sólo acepta `solqaryn_dev` + `solqaryn_dev_user`, `Staging` sólo `solqaryn_qa` + `solqaryn_qa_user`, y `Production` sólo `solqaryn_prod` + `solqaryn_prod_user`; además exige el endpoint Aiven corporativo `solqaryn-mysql-solqaryn.h.aivencloud.com:14402` con `SslMode=Required`. Entorno, host, puerto, TLS, base o usuario incompatibles fallan cerrados antes de registrar `AppDbContext`.
- `RenderEnvironmentContractGuard` valida en cada arranque Render las 28 claves administradas: mismo conjunto de nombres en DEV/QA/PROD, valores obligatorios no vacíos y constantes públicas compartidas coherentes; los secretos y las identidades propias del entorno pueden diferir. El runtime sólo registra conteo y fingerprints no sensibles.

### Binding Vercel -> API

- El backend de un deployment no se decide por hostname ni alias.
- `frontend/server/environment-binding.js` usa `VERCEL_PROJECT_ID` como identidad primaria del deployment y sólo reconoce los tres proyectos corporativos autorizados.
- Proyecto `prj_1Anhx5mWyXEBX89lWC24Py6JXe7A` resuelve exclusivamente DEV -> `solqaryn-api-dev-fxx8.onrender.com`; `prj_n5STx5F6VboqXd1oLUMR8AvZZtml` resuelve exclusivamente QA -> `solqaryn-api-qa.onrender.com`; `prj_si3ORH7lBhM4aSAYfYvXsbJT2lHA` resuelve exclusivamente PROD -> `solqaryn-api-prod.onrender.com`.
- `SOLQARYN_ENV`, `API_UPSTREAM`, `PUBLIC_ORIGIN` y `SEO_INDEXING_ENABLED` son controles opcionales de coherencia: si existen, deben coincidir con el proyecto canónico o el runtime falla cerrado.
- `frontend/api/backend-proxy.js` es la frontera server-side de `/api/*`; alias, previews y custom domains no cambian el upstream.
- Mitigación de arranques Render Free (candidato DEV 2026-10-12): el proxy clasifica únicamente errores de socket como `503 API_UPSTREAM_UNAVAILABLE` con `Retry-After`, sin datos sensibles en los logs. El login comprueba `GET /health/ready` por `HttpBackend` sin credenciales ni JWT (máximo 3 GET transitorios); solo después hace un único POST login. No hay keep-alive, cambios a QA/PROD ni garantía de always-on; requiere despliegue exact-head para validación real. Ver `docs/runbooks/API_UPSTREAM_DISPONIBILIDAD_DEV.md`.
- Un `VERCEL_PROJECT_ID` desconocido jamás obtiene fallback DEV/PROD.

### Vercel
- Proyecto DEV activo: `solqaryn-dev`.
- Proyecto QA activo: `solqaryn-qa`.
- Proyecto PROD corporativo activo: `solqaryn-prod`.
- PROD fue certificado sobre la rama `main` y permanece operativo mediante el alias administrado `https://solqaryn-prod.vercel.app`.
- No se reutiliza ningún proyecto personal o legacy.

### Aiven
- Proyecto: `solqaryn`.
- Servicio MySQL: `solqaryn-mysql`.
- **MySQL Aiven: 8.4.8 aprobada por el propietario el 2026-10-10; congelada sin upgrade ni maintenance-start**. La Fase 1 prueba la versión real vía `SELECT VERSION()` y falla si difiere; DEV/QA/PROD comparten ese servidor, con bases/usuarios segregados. MySQL 8.4 de Docker en CI es efímero e independiente del servicio Aiven.
- Bases: `solqaryn_dev`, `solqaryn_qa` y `solqaryn_prod`.
- Usuarios de aplicación separados por entorno: `solqaryn_dev_user`, `solqaryn_qa_user`, `solqaryn_prod_user`.

### Cloudflare
- La cuenta/DNS corporativos pertenecen a SOLQARYN.
- `solqaryn.com` está delegado correctamente a Cloudflare.
- DEV y PROD continúan usando las URLs administradas actuales mientras el cutover del dominio personalizado permanezca aplazado.
- El cutover DNS hacia PROD es una decisión deliberadamente diferida y no bloquea el estado productivo certificado.

### Cierre técnico DEV / futura promoción

- El contrato de evidencia, rollback y precondiciones para una futura promoción vive en `docs/DEV_CIERRE_TECNICO_PROMOCION.md`.
- Ese runbook no autoriza `main`/PROD ni modifica el Plan Maestro; cualquier promoción requiere autorización explícita nueva y certificación exact-head.
- La promoción nunca puede resolver PROD mediante infraestructura DEV ni reintroducir fallback por hostname/alias.

## 5. Estado de legado y migración histórica

- La migración histórica del tenant inicial hacia PROD ya fue ejecutada y certificada; el respaldo verificado se utilizó como fuente controlada de migración.
- Ningún deployment, proyecto, servicio, cuenta personal, repositorio, dominio o variable legacy se considera dependencia de SOLQARYN.
- La infraestructura legacy retirada no se consulta ni se reactiva como fallback.
- Los tenants/empresas cliente permanecen como datos de negocio dentro de SOLQARYN y nunca como identidad del código, rutas, proyectos o infraestructura.
- El estado productivo vigente se sostiene exclusivamente sobre la infraestructura corporativa certificada de SOLQARYN.

## 6. Dominios funcionales

Áreas principales:

- empresas y configuración empresarial;
- autenticación, usuarios, roles y permisos;
- productos, variantes, catálogos e imágenes;
- inventario, almacenes, ubicaciones, reservas y movimientos;
- compras, proveedores y cuentas por pagar;
- ventas, clientes, cotizaciones, pedidos y facturación;
- finanzas;
- descuentos e impuestos;
- auditoría;
- reportes;
- tienda pública de empresas cliente;
- automatizaciones de operación y control.

## 7. Invariantes

- No exponer secretos.
- No mezclar datos entre empresas.
- No confiar en permisos visuales como sustituto del backend.
- Migraciones y cambios de datos deben ser explícitos, verificables y recuperables.
- No force-push.
- Revalidar HEAD antes de publicar.
- Cambios en `main`, PROD o infraestructura productiva requieren autorización explícita vigente.

## 8. Ejecución comercial inicial vigente (autoridad única)

A partir de la decisión explícita del propietario del **19 de octubre de 2026**, únicamente el archivo **SOLQARYN PLAN COMERCIAL INICIAL UNIFICADO**, Drive ID `1T41ppLvC3x-p6sORCtpaPvzLWPP9_Z7j`, autoriza tareas de desarrollo de la versión comercial. La hoja única **SOLQARYN - PLAN COMERCIAL INICIAL - AUTOMATIZACIONES**, ID `1gcVyCoyhLU0jFMwRtf0s5_x8FSnfBs38ojml1QF7Xwk`, es superficie ejecutiva derivada de 492 fichas documentales y 1123 microtareas (`P0-001.A` ... `P9-013.B`).

- Tareas visibles: `PLAN_COMERCIAL` y `COLA`, con `_COMERCIAL_SOURCE` y `_RUNTIME_SOURCE` ocultas como fuentes; `_AUTOMATION_SOURCE` y `_EVENTS_SOURCE` conservan estado y trazabilidad.
- Paneles vigentes: `DASHBOARD`, `CONFIG`, `BITACORA`, `LEYENDA`, `TAREAS_PROGRAMADAS`, `TAREAS_DE_SUPERVISION`, `AUTOMATIZACIONES` y `CONTROL_TOWER`.
- Las 1123 tareas están inicialmente `PENDIENTE`. La cuenta principal de Javier tiene cinco primarias configuradas pero en pausa; las cinco supervisoras de Alex **no han sido sincronizadas ni verificadas** (sin permiso de edición cruzada) y siguen bloqueando la activación. Estado de admisión: `NO_GO` hasta integración de PR, controles de CI, sincronización 10/10 y autorización efectiva de ejecución.
- El esquema Google Sheets recorta filas/columnas visibles; protegidas para evitar mutaciones manuales. El propietario aún puede administrar su propia hoja (limitación de Google Sheets, no prometer bloqueo absoluto a propietario).
- Las unidades ejecutables sólo provienen del plan comercial y de estado técnico fresco de `dev`. Documentos de visión futura y pruebas/planes históricos son **NO EJECUTABLES**, archivados para conservación y sin dependencia ni fallback.
- El control de cambios exige microtarea mínima, dependency-valid, lease exclusivo, verificación de MySQL/RBAC/tenancy cuando afecte, REVIEW_FIRST independiente, CI exact-head, P0/P1=0, evidencia y rollback. Toda promoción a QA/main/PROD, secretos, DNS o datos productivos requiere autorización explícita nueva.
- Canon de ejecución: `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, `docs/VAEP_AUTHORITY.md`, HEAD vivo de `dev`, contrato VAEP Drive `1frrmekon0pBTrLcXk0yUZSJa4uISjEzrwrni3ja5y38` y sólo después plan/hoja comercial.

No usar índices SQ o gates de otros planes para seleccionar nuevos trabajos; los registros Git y CHANGELOG se preservan como evidencia histórica inmutable, no como authority de la cola actual.

## Cloudinary DEV: normalización física y descargas LISTO (2026-10-12)

`DevCloudinaryLegacyNormalizationHostedService` (Infrastructure, compuesto en API) completó la normalización de **11/11 imágenes** de `ProductoImagenes` a locators DEV canónicos con comparación SHA-256 y actualización condicional (`originals_deleted=0`). Respaldo/restauración verificados: [run 38319634960](https://github.com/solqaryn/Solqaryn/actions/runs/38319634960). Auditoría MySQL read-only: [run 38323224517](https://github.com/solqaryn/Solqaryn/actions/runs/38323224517) PASS con `remaining=0`. El flag `Cloudinary:NormalizeDevLegacyImagesOnStartup=false` volvió a estar versionado y está LIVE en Render DEV SHA `f6f79b9d`. El propietario verificó descargas individual y ZIP autenticadas tras la normalización, **aceptación manual PASS** (no CI ZIP automatizado). Evidencia: `docs/CLOUDINARY_AISLAMIENTO.md`. QA/PROD y dominio fuera de alcance; promoción DEV → QA → PROD aplazada.

## 2026-10-14 — P4 ventas: reportes con membresía de empresa verificada (PR #3615 candidato)

- `ReporteVentasService` deja de utilizar `UsuarioScopeActual` global; resuelve `UsuarioTenantScopeActual` comprobando membresía de empresa desde el contexto server-side y añade `Venta.EmpresaId` explícito al conjunto de filas consultables.
- Sin tenant verificado, los resúmenes/detalles retornan cero filas; `VendedorId` suministrado por el cliente no expande el alcance. Pruebas A/B simultáneas y fixtures con sucursales de un mismo propietario.
- Sólo DEV; sin migraciones ni backfill, sin retirar cuarentena de segundas empresas, sin QA/PROD. Candidato sujeto a REVIEW_FIRST, CI exact-head y pruebas MySQL A/B.

## 2026-10-14 — P4 compras: reportes con ownership de documentos A/B (PR #3616 candidato)

- `ReporteComprasService` exige una empresa seleccionada por servidor y membresía vigente mediante `IUsuarioScopeService.ObtenerActualAsync(empresaId)`; en ausencia, rechaza incluso invocaciones fuera de middleware HTTP.
- El detalle de orden y sus anexos de facturas, recepciones, devoluciones y evaluaciones preservan `EmpresaId` explícito (EF.Property en propiedad shadow) en consultas y joins.
- La aceptación unitaria usa owner empresa A y B, demuestra separación y rechaza solicitudes sin scope. Sin DDL/backfill ni escritura a datos reales. Cuarentena general de 2+ permanece; sujeto a exact-head MySQL, CI y REVIEW_FIRST.

## 2026-10-14 — P4: locks SQL comerciales y conteos por EmpresaId (candidato DEV)

- `VentaRepository.GetByIdForUpdateAsync` y `CompraRepository.GetByIdForUpdateAsync` verifican el tenant RBAC privado y la membresía activa antes de realizar `SELECT ... FOR UPDATE`. En ambas ramas (administrador y no administrador) el predicado parametrizado `EmpresaId` está dentro del SQL de bloqueo; no depende de la composición de filtros EF.
- `CompraRepository.ContarTodasAsync` conserva la numeración histórica solo cuando existen 0-1 empresas; con 2+ deniega contextos sin RBAC o con capability pública, y aun al usar `IgnoreQueryFilters` cuenta solo filas del tenant validado. Los locks SQL conservan el modo monoempresa bajo conteo de empresas y exigen propietario en SQL con 2+.
- Prueba MySQL descartable A/B: documentos con números coincidentes, lecturas propias, cruces negados incluso mediante SQL FOR UPDATE, conteos A/B y rechazo de capabilities públicas. No hay migración, backfill, ni alteración de datos reales.
- **No certificar todavía el punto 4 integral ni quitar la cuarentena HTTP**: quedan repositorios tenant-aware de consultas ordinarias, inventario/Kardex, archivos/exports, workers y E2E de dos empresas. Gates obligatorios CI exact-head y revisión de seguridad previa a merge.

## 2026-10-14 — P4 inventario: filtros físicos requieren administrador de empresa (candidato DEV)

- Los reportes de Kardex, StockHealth y Reconciliación no aceptan `UsuarioScopeActual.EsAdministrador` legacy para filtros explícitos de Sucursal/Almacén/Ubicación: deben usar `UsuarioTenantScopeActual` validado contra el `EmpresaId` autorizado por `RequierePermiso`.
- La sobrecarga legacy de `ReporteInventarioScopeGuard` rechaza solicitudes con dimensión física; la nueva comprueba simultáneamente EmpresaId y permiso administrador de la membresía. En Kardex la empresa se obtiene de la marca RBAC del servidor; sin marca no se eleva acceso.
- Tests de autorización: legacy admin negado, cruce A→B negado y administrador empresarial positivo. Sin DDL, backfill, cambios de API/DTO ni QA/PROD. Aun quedan consultas legacy del repositorio Kardex; NO retirar contención 503 ni afirmar certificación P4 integral.

## 2026-10-14 — P4 Kardex: lectura A/B y orígenes tipados tenant-scoped (candidato DEV)

- `MovimientoInventarioRepository` ya no aplica exclusivamente el rol legacy/global para las consultas Kardex `GetByProductoAsync`, `GetFilteredAsync`, `GetPagedAsync`. Si existe marca RBAC, exige membresía vigente y filtra por `EmpresaId` shadow; los no administradores mantienen restricción por creador.
- En 2+ empresas, las consultas sin marca RBAC privada devuelven 0 resultados aun ante marca pública /tienda. Accesos auxiliares a orígenes tipados, movimientos posteriores y validaciones también usan query empresarial verificada. La compatibilidad legacy se conserva solo cuando existen como máximo una empresa (por evitar regresiones monoempresa mientras permanece cuarentena).
- Test A/B InMemory con productos/movimientos privados, lectura propia, cruce negado y admin global/ticket público sin autoridad. Sin DDL ni backfill ni cambios en QA/PROD. **No es certificado P4 integral**; falta confirmar CI exact-head y otras rutas no migradas.

## 2026-10-14 — P4 facturas privadas y PDF público con tenant verificable (candidato DEV)

- `FacturaRepository` obtiene la membresía `UsuarioTenantScopeActual` de la marca RBAC empresarial, aplica `Factura.EmpresaId` explícito a GetById/GetByVentaId/GetAll y preserva limitaciones por usuario no-administrador; si no existe marca y hay 2+ empresas, deniega incluso al admin global legacy. Compatibilidad monoempresa 0-1 preservada.
- El método `GetByIdParaEnlacePublicoValidadoAsync` requiere **ambas** marcas causales emitidas por verificación de token hash: `PublicInvoiceAccessContext.FacturaIdKey` y `VerifiedPublicInvoiceTenant.Resolve` en la ruta PDF pública; además exige `Factura.EmpresaId` exacto en consulta. La capability pública no habilita listados privados ni facturas de B.
- Test InMemory A/B cubre facturas propias, ID ajeno, vendedor/venta ajenos, enumeración vacía con 2+ sin RBAC, enlace A válido, enlace B denegado y path/capability ausentes. Sin migración/backfill, sin tocar QA/PROD; cuarentena general 503 permanece.
- Pendiente CI exact-head MySQL/E2E + REVIEW_FIRST; no representa certificación completa del punto 4.

## 2026-10-14 — P4 autoridad central IUsuarioScopeService fail-closed (candidato DEV)

- `UsuarioScopeService.ObtenerActualAsync()` legacy deja de conceder `Usuario.RolId/EsAdministrador` global con dos o más empresas, incluso si una está inactiva o se presenta únicamente una marca pública. Sin empresa privada seleccionada retorna null.
- Si `VerifiedCommercialTenant` certifica EmpresaId, el contrato legacy deriva por compatibilidad el rol desde `UsuarioEmpresa` activa y validada (usuario, empresa y rol vigentes), por lo que los consumidores aún no migrados no pueden elevar permisos con el rol global de otra empresa.
- Se conserva comportamiento histórico para 0-1 empresas, solo bajo cuarentena HTTP existente. Prueba A/B: mismo usuario con rol operador A y admin B, null sin marca y marca tienda pública, roles correctos con RBAC A/B. Sin DDL/backfill, sin tocar QA/PROD ni habilitar nuevas empresas.
- Candidato sujeto a CI exact-head + pruebas MySQL/E2E de consumidores; **no representa certificación integral de P4**, siguen necesarios los guards de repositorios/raw SQL/archivos/jobs y su aceptación A/B.

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

## 2026-10-14 — P4 archivos: scope Cloudinary no acepta header de otra empresa (candidato DEV)

- El resolutor común `StorageTenantContextResolver` ahora exige que `X-Empresa-Id` coincida con `VerifiedCommercialTenant` cuando la acción ya tiene empresa verificada por RBAC. Una segunda membresía legítima B nunca puede reutilizar una autorización HTTP concedida bajo A para subir, leer o eliminar archivos B.
- Se comprueba además que `IUsuarioScopeService.ObtenerActualAsync(empresaId)` devuelva exactamente `scope.EmpresaId == empresaId`; contextos inconsistentes fallan cerrados. La selección y verificación de membresía existente se conserva para solicitudes monoempresa sin marca, mientras permanece la cuarentena 503 para 2+.
- Nuevas pruebas: RBAC A+header B con membresías múltiples (rechazo antes de consultar B), scope B devuelto para header A (rechazo), y marca/membresía A legítimas (permitido). Sin cambios de DDL, backups, Cloudinary real, QA/PROD o dominios.
- **No constituye certificado integral P4**: exige CI exact-head MySQL/seguridad, A/B downloads y readback de despliegue DEV.

## 2026-10-14 — P4: las secuencias fiscales no aceptan selector de empresa diferente al RBAC

- `SecuenciaDocumentoService.ValidarScopeAsync` exige coincidencia del `EmpresaId` del request con `AuthorizedCommercialEmpresaId` si la acción ya fue autorizada, antes de reservar o consultar números. Un usuario perteneciente a empresas A y B no puede utilizar la autorización A para reservar una secuencia fiscal B.
- Con 2+ empresas sin marca RBAC privada falla cerrado también ante invocaciones no HTTP; el contrato histórico de instalaciones con 0–1 empresa permanece. Comprueba igualmente que la membresía devuelta por el servicio coincide con la empresa solicitada.
- Tests A/B: autorización A con solicitud B y membresía B vigente rechazada sin invocar la membresía B; sin marca RBAC con dos empresas rechazada; no se reserva ni se incrementa numeración de B. Se conserva compare-and-swap MySQL y evidencia de transacciones anterior. No hay DDL, backfill ni cambios QA/PROD, cuarentena 503 sigue activa.

## 2026-10-18 — DEV Microfase 1: cierre de deuda del readback compuesto

La identificacion publica multiempresa DEV se certifico mediante evidencia
HMAC real en Render (secuencia 200/503/200, ledger MySQL PASS) y evidencias
historicas auditadas de suspension/reactivacion A/B; ambas empresas activas,
legacy 200 y rutas desconocidas rechazadas en el ultimo smoke. Fuente corporativa:
comentario 6251764256 del PR #3666; SHA certificado `49195f34...`.
El antiguo workflow exigia estados incompatibles en una misma ejecucion y
nunca consultaba la evidencia firmada de Render. La version vigente lee
`docs/certifications/dev-public-store-microfase1.json`, verifica GitHub
autor/codigo exacto y corrobora en vivo MySQL, A/B, legacy y rechazos, con
status `PASS_ATTESTED` separado del PASS en comprobaciones ejecutadas.
No suspende empresas ni repite HMAC por rutina. Trabajador temporal retirado;
flag Render desactivado; sin ampliacion comercial ni cambios QA/PROD.
Referencia: `docs/certifications/DEV_MICROFASE1_READBACK.md`.
