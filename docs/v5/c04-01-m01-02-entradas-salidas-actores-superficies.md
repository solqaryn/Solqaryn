# SOLQARYN V5 — C04-01.M01.02: entradas, salidas, actores y superficies de planes SaaS

**Microtarea:** `C04-01.M01.02` (PLAN_MAESTRO fila 579). **Autor:** PRIMARY DEV 02 — JAVIER, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Rama y límite:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Fecha:** 2026-11-14. **Clase:** contrato documental de preparación M01, no habilita cobros, concesión de permisos ni migraciones.

## 1. Alcance y dependencia causal

Este documento desglosa los **actores, entradas, salidas y superficies** del futuro catálogo C04-01, sin agregar interfaces ejecutables. Autoridad: Google Doc **STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL** (`1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), fila 579 de PLAN_MAESTRO y DAG_TECNICO fila 540 con `F=VALIDADO/G=SI` para esta tarea. `COLA!G579/O579=SI` y owner vacío fueron comprobados antes del CAS exclusivo. El contrato complementa `C04-01.M01.01` (objetivo/no-alcance), **sin promover automáticamente M01.06 ni M02**.

El resultado exigido del bloque futuro es un catálogo versionado de planes, capacidades, límites, precios, periodicidad y cambios de plan con cumplimiento server-side y auditoría. La arquitectura existente contiene una primera superficie de suscripción/entitlements, **no** una certificación de precios, upgrades, impuestos o pasarela comercial.

## 2. Actores y facultades: distinguiendo hechos y decisiones futuras

| Actor o subsistema | Responsabilidad identificada | Autoridad real / limitación |
| --- | --- | --- |
| Usuario de empresa/tenant autenticado | Consultar suscripción, módulos y límites en su tenant | JWT válido, membresía vigente y RBAC; el selector de empresa no es autoridad por sí mismo |
| Operador con permiso `Configuracion/Crear` en tenant | Solicitar onboarding SaaS por API actual | `[Authorize]` y `[RequierePermiso]` en Controller; service resuelve tenant antes de consulta |
| Operador con `Configuracion/Ver` | Consultar suscripción, límites, entitlements | No puede cambiar catálogo ni condiciones económicas por tener permiso de lectura |
| Administrador del catálogo de plataforma **futuro** | Proponer versiones, capacidades, tarifas y periodos sujetos a política | **No se atribuye un rol o permiso ya existente**; requiere diseño RBAC/A5 y autorización posterior |
| Cliente Angular | Mostrar datos devueltos y errores accesibles sin exponer otros tenants | Nunca concede entitlements ni verifica membresía por sustitución del backend |
| Servicio Application SaaS | Resolver tenant, idempotencia, reglas de vigencia, rechazo y auditoría | No aceptar EmpresaId del body como autoridad ni relajar permisos por plan contratado |
| Repositorio y MySQL | Consultar planes, suscripciones, módulos y ledger de replay | Transacciones e índices se prueban en CI MySQL cuando se alteren, no en M01 |
| Proveedor de cobros/worker/event dispatcher **futuro** | Ninguna operación en esta entrega; contrato futuro separado | Sin credenciales, pasarelas, webhooks, jobs, eventos ni cargos aquí |

## 3. Mapa de entradas y salidas del código vigente inspeccionado

| Borde | Entradas visibles | Salidas visibles / errores esperados | Código fuente y evidencia |
| --- | --- | --- | --- |
| Onboarding HTTP | `POST /saas/tenants/{empresaId}/onboarding`, header `Idempotency-Key`, body `PlanCodigo, InicioUtc` | `SuscripcionSaaSDto`; 400 sin key, rechazo de tenant no autorizado, plan inexistente, replay distinto o suscripción activa | `backend/src/API/Controllers/SuscripcionesSaaSController.cs`, blob `cab95db5a33c6a15448e2437481eaaac25ffc4fe` |
| Lectura de suscripción | `GET /saas/tenants/{empresaId}/suscripcion`, `instanteUtc` opcional | Plan, estado, inicio/fin del tenant verificado; sin datos tenant B | Mismo controller + `SuscripcionesSaaSService.cs`, blob `bb501285f11cd4b32709a58a2c51efa19c3664ea` |
| Límites | `GET /saas/tenants/{empresaId}/limites`, `pagina`, `tamanoPagina`, `clave` | Página acotada `PaginaSuscripcionSaaSDto<Limite...>`, error si paginación inválida | `SuscripcionesSaaSApiContracts.cs`, blob `6045a167fb90fe3dea2fa87280d0a29cec57c6be` |
| Entitlement de módulo | `GET /saas/tenants/{empresaId}/modulos/{moduloClave}/entitlement` | `habilitado`, `motivo`, plan/clave del tenant, nunca permiso RBAC administrativo | Controller, DTO y servicio Application arriba |
| Persistencia/lectura | `codigoPlan`, `empresaId` validado y marca temporal UTC; clave idempotente | `Plan` activo con límites, `Suscripcion` vigente y replay durable por tenant | `backend/src/Infrastructure/Repositories/SuscripcionesSaaSRepository.cs`, blob `f3340ca687867d6dc2d99dfff5452bbe5c9bbb79` |
| Cliente Angular | `empresaId` seleccionado, key de idempotencia, filtros y módulo | `Observable<ApiResponse<...>>`; manejo de errores/UI por componente | `frontend/src/app/services/suscripcion-saas.service.ts`, blob `b3883c10afce06a39bdf4d0dec356a553f8aa32f`; `frontend/src/app/features/configuracion/suscripcion-saas-card.component.ts`, blob `7909433c414089b89b87ebff20017c2704a7f085` |

**Ausencias a diseñar después (no inferir de los DTO anteriores):** estructura de versión de tarifa, moneda autorizada, impuestos, periodicidad, vigencia del precio, historial de upgrades/downgrades y reglas de prorrateo; consola del catálogo global; proceso de aprobación comercial; eventos de facturación. No se crean aquí endpoints, tablas, JSON ni políticas de precios ficticias.

## 4. Flujo de confianza y límites multiempresa

1. **Entrada HTTP:** `empresaId` en ruta identifica la intención del usuario, no prueba que posea esa empresa. `[Authorize]` y RBAC son condición necesaria, no suficiente para saltarse membresía.
2. **Application:** `IUsuarioScopeService` debe resolver la empresa del usuario activo antes de I/O, verificar acceso real y denegar tenant B, sucursal no autorizada y membresía revocada.
3. **Dominio:** `Plan`/limites describen oferta; `Suscripcion.EmpresaId` es asignación de tenant; vigencia UTC y estados impiden consultar suscripción cerrada como activa.
4. **Persistencia:** consultas por `EmpresaId`, con ledger idempotente por empresa, evitan que requests simultáneos creen suscripciones duplicadas; cualquier migración o índice futuro exige CI con MySQL efímero y rollback no destructivo.
5. **Salida:** DTO minimizado, error estable, auditoría sin secretos ni datos de otro tenant; ningún entitlement comercial confiere administración del tenant.
6. **UI:** Angular renderiza estados/loading/error y solicita desde tenant seleccionado, pero las validaciones del navegador **no cuentan** como autorización de backend.

## 5. Matriz de superficies afectadas y contratos futuros

| Capa | Estado actual observado | Dependencia de diseño futuro | No tocar en M01.02 |
| --- | --- | --- | --- |
| Domain | `Plan`, `PlanLimite`, `Suscripcion`, `PlanModulo` | Identidad/versionado de plan y precios, moneda, transición de reglas | Ninguna entidad o validación ejecutable |
| Application | Servicio SaaS, DTO, scopes tenant y auditoría | Política para versiones, transiciones y límites con motivo de decisión | Ninguna decisión comercial runtime |
| Infrastructure/MySQL | Consultas, ledger de idempotencia y repositorio EF | Migraciones reversibles, unicidad/índices de versión, pruebas concurrencia | Ningún schema, dato ni índice real |
| API | Rutas SaaS protegidas y códigos de error | Contrato de consulta catálogo versionado y cambios de suscripción autorizados | Ningún endpoint nuevo |
| Angular | Servicio tipado, card suscripción, estados accesibles | UX de catálogo/precio clara, moneda y periodo explícitos, sin affordance engañosa | Ningún componente modificado |
| Seguridad | JWT, RBAC, TenantContext, auditoría | Amenazas del catálogo, abuso de upgrade, replay y roles superpuestos | Sin secretos ni permisos nuevos |
| Operación | GitHub Actions/Scope Lock/Sheet canónico | CI real por componente y rollout reversible en fases posteriores | No dispatcher, webhooks, cobros ni QA/main/PROD |

## 6. Escenarios de aceptación DOCUMENTALES — no ejecutados como runtime

**Positivos P01–P05:** lectura de suscripción del tenant autorizado; límites válidos; entitlement por módulo del plan activo con permiso de lectura; onboarding repetido con clave/payload idénticos sin duplicado; propuesta futura de versión de plan auditada y sin activación automática.

**Negativos N01–N10:** sin JWT; JWT válido pero sin membresía; `empresaId` de tenant B; permiso RBAC insuficiente; clave `Idempotency-Key` ausente o reutilizada con payload distinto; plan inactivo/no registrado; tamaño de página fuera de 1–100; propuesta de moneda/tarifa sin autoridad comercial; downgrade retroactivo que destruye historia; caída/duplicación de callback de proveedor externo que concede permisos o cobra.

Estos escenarios son **contratos para pruebas posteriores**; los tests .NET, Angular, MySQL y Playwright de funcionalidades comerciales nuevas no se han ejecutado en esta microtarea. Para este documento A1–A3 implican comprobar completitud y coherencia textual, no fingir ejecución de casos.

## 7. No regresión, observabilidad, rollback y entrega

- Mantener las invariantes `NR01–NR10` del documento `C04-01.M01.01`; proteger historia de suscripciones y uso comercial.
- Metadatos del futuro catálogo deberían permitir auditoría de versión/actor/tenant/motivo/fecha sin PII o tokens en logs; esta entrega no despliega logging ni capturas reales.
- Un catálogo global no debe permitir a un usuario tenant elevar privilegios para editar planes de otros tenants. Todo permiso nuevo exige gate posterior.
- Rollback **solo documental**: `git revert` normal del commit con este Markdown y una sección íntegra de `CHANGELOG_AI.md`; no tocar BD ni historia por la reversión del documento.
- Cierre A0–A4: verificar documentos, diffs, pruebas documentales, Scope Lock y procedencia CHANGELOG sobre SHA exacto. `A5_PENDIENTE` sólo para otra Task Javier, con BITACORA A:Z y readback de PLAN/COLA.

**Este artefacto no habilita M01.06, M02, JWT/RBAC global, cobros, worker/eventos, despliegue ni QA/main/PROD.**
