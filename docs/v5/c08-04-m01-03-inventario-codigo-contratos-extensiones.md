# SOLQARYN V5 — C08-04.M01.03 — Inventario de código, contratos y evidencia de extensiones sectoriales

**Autor:** PRIMARY DEV 03 JAVIER (`6adc2e4bd8a48191ac7d8641ed95a1a3`). **Fecha:** 2026-11-14. **Plan V5 FINAL:** C08-04 / M01.03, PLAN_MAESTRO fila 1264. **Repositorio:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT. **Entrega:** auditoría documental de fuentes y brechas; sin cambio de runtime.

## 1. Propósito y alcance de la inspección

Identificar las fuentes reales que podrán alimentar una matriz futura de capacidades por sector (reservas, membresías comerciales, catálogo, storefront y módulos adicionales), conservando un solo control de tenant, usuarios, empresa, permisos, auditoría y recursos. No se da por implementado un motor de capabilities, y no se certifica aislamiento end-to-end por la sola existencia de clases, rutas o guards.

**Antecedentes contrastados:** C08-04.M01.01 `docs/v5/c08-04-m01-01-alcance-extensiones-por-rubro.md` (blob `41dd2a10d667124c021791278d43ceab3dc02c35`) define NR01–NR12; C08-04.M01.02 `docs/v5/c08-04-m01-02-actores-entradas-salidas-extensiones.md` (blob `606e34598c50a3419d535115679631b705d46f84`) define A01–A14/I01–I12. Ninguno representa A5 propia ni aprobación de M01.06.

## 2. Inventario de once fuentes de código verificadas por blob

| Fuente real en dev | Blob SHA | Observación constatada; límite de alcance |
| --- | --- | --- |
| `backend/src/Domain/Entities/Empresa.cs` | `c44940df7c30983a4c23b93338e0cb9367962069` | Raíz empresarial con ciclo activo/inactivo; no acredita activation toggle de rubro |
| `backend/src/Domain/Entities/UsuarioEmpresa.cs` | `f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c` | Membresía y rol dentro de Empresa; no equivale a membresía comercial de gimnasio |
| `backend/src/Domain/Entities/Producto.cs` | `19632ce1aebe6a9cbc4379523367531852ee3e7e` | `EmpresaId` nullable legado, exige resolver propiedad en backend |
| `backend/src/Domain/Entities/Sucursal.cs` | `8746b11fa30cdac0b97241b6221100bedd813b35` | `EmpresaId` nullable con validación para asignar empresa, sin sectorización probada |
| `backend/src/Domain/Entities/Cliente.cs` | `8c6d4c4453e44f8c0be9475340465e1aadb3f787` | Datos personales y `EmpresaId` nullable, obliga a no filtrar PII de B |
| `backend/src/Domain/Entities/PublicStoreBinding.cs` | `dae03bcd963281d74e1d02e460934ab0783d39d5` | Host, StoreKey, EmpresaId, Activa: esquema no demuestra validación HTTP del binding |
| `frontend/src/app/app.routes.ts` | `2f560c14693455b79ac4546e9333fb74586ef234` | Rutas tienda públicas y rutas privadas con guards, no prueba enforcement server-side |
| `frontend/src/app/core/guards/permiso.guard.ts` | `1052bd38041055cc81e84a14d5fe0364e169b1fe` | Guarda de navegación con módulo/acción; no es autoridad de seguridad backend |
| `frontend/src/app/core/auth/permisos-runtime.service.ts` | `b72cf568c9fdd294d98433db238644796cb75994` | Proyección de permisos y rutas; no prueba feature flag por sector |
| `frontend/src/app/core/auth/tenant-context.service.ts` | `63a711355495d2f80efba35bffb2b8727401154a` | Contexto tenant solicitado/verificado, cliente no autoriza accesos por sí solo |
| `frontend/src/app/core/interceptors/auth.interceptor.ts` | `4bdf19135ed1932a99fd09ace1f900bcccc5b25b` | `X-Empresa-Id` expresa solicitud; no sustituye pertenencia server-side |

**Alcance de verificación:** lectura de archivos puntuales en `dev` y hashes de sus blobs. No se trazaron todos los controladores, políticas, queries EF, migraciones o ejecuciones E2E; cualquier conclusión sobre cobertura transversal queda PENDIENTE DE DEMOSTRACIÓN. El intento de ubicar una entidad `Suscripcion.cs` en la ruta supuesta Domain/Entities no produjo archivo; no se infiere su inexistencia ni se registra como fuente verificada.

## 3. Diagnóstico causal: diez brechas por demostrar, NO fallos certificados

| Brecha | Pregunta técnica pendiente | Evidencia exigible después |
| --- | --- | --- |
| G01 Registro de capabilities | ¿Existe catálogo server-side versionado/allowlisted para rubros? | Ruta, esquema, API y test de rechazo de ID desconocido |
| G02 Relación con autorización | ¿Exige todo endpoint sectorial tenant efectivo y permiso concreto? | Tests 401/403/404 A/B y acceso directo sin UI |
| G03 Habilitación/deshabilitación | ¿Bloquea nuevas altas al desactivar sin borrar historia ni otros rubros? | Contrato de transición, test persistente e historial |
| G04 Ownership legado | ¿Se resuelve sin fuga `EmpresaId` NULL de Producto/Cliente/Sucursal? | Filtros, índices y fixtures históricos segregados |
| G05 Membership comercial | ¿Se distingue membresía de gimnasio de UsuarioEmpresa y SaaS? | Dominio/DTO/pruebas semánticas y políticas |
| G06 Host y storefront | ¿Se normaliza host y se verifica binding en backend? | Tests de host manipulado, tienda ajena y previsualización |
| G07 Origen de permisos UI | ¿Coincide mapping route/permisos con decisiones del backend? | Matriz API/guard sin rutas de bypass |
| G08 Idempotencia/auditoría | ¿Se deduplican replay/activaciones y se preserva actor/tenant? | Tests de concurrencia, event ID y rollback |
| G09 Resiliencia | ¿Errores de caché/config/proveedor conservan deny-by-default? | Tests de timeout, revocación y config inválida |
| G10 UI accesible | ¿Los módulos futuros respetan teclado, foco, 320px y reduced motion? | Angular/Playwright real y no exposición de PII |

Estas brechas identifican **ausencia de verificación en esta microtarea**, no necesariamente ausencia de código o una vulnerabilidad demostrada.

## 4. Oráculos futuros para M02–M05 — NO_EJECUTADOS

| Escenario | Resultado positivo que exigir | Resultado negativo que rechazar |
| --- | --- | --- |
| P01/N01 | Empresa A con rol y capability válida consulta sólo su rubro | Desde A, ID de B no expone datos |
| P02/N02 | Con permiso, activación queda versionada y auditada | Usuario revocado o rol insuficiente no activa sector |
| P03/N03 | Desactivar detiene nuevas operaciones preservando histórico | Desactivar no elimina registros ni habilita una API privada |
| P04/N04 | Cliente público consulta solo proyección por binding validado | Host/slug manipulado no otorga identidad ni acceso privado |
| P05/N05 | Reintento se resuelve una sola vez con idempotency key | Replay/duplicación no crea efecto nuevo |
| P06/N06 | Configuración segura se revierte a versión autorizada | Error de caché o proveedor no activa capacidades |
| P07/N07 | Propiedad histórica está inequívocamente delimitada | Registro legacy sin owner no se comparte con otros tenants |
| P08/N08 | UI accesible preserva permisos y errores comprensibles | Manipular Angular, ruta o cabecera no modifica autorización |

Ningún escenario fue ejecutado como test de .NET/Angular/MySQL/Playwright; son **criterios futuros**, no pruebas ni CI PASS. Las próximas fases deben aportar las pruebas dirigidas y de seguridad causal sobre SHA exacto.

## 5. Impacto, dependencias, seguridad y reversión

- **Impacto actual:** dos Markdown (`docs/v5/` y `CHANGELOG_AI.md`); sin cambios en Domain/Application/Infrastructure/API/Angular activos, schema MySQL, JWT/RBAC, servicios, secretos, workers, webhooks, datos, Tasks o despliegues.
- **M01.04:** convertir brechas en riesgos, supuestos falsables y decisiones pendientes, citando evidencias; **M01.05:** definir aceptación funcional y técnica. **M01.06/M02–M06:** mantener gates individuales, validación de dependencias y A5 independiente, sin automatizar promoción.
- **A0–A3 documentales:** verificación de fuentes originales, contradicciones, no-regresión de tenant/JWT/RBAC, trazabilidad y positivos/negativos FUTUROS; runtime **NO_APLICA**, no se presenta como PASS.
- **A4:** diff Markdown y blobs, publicación fast-forward/CAS, Scope Lock y procedencia CHANGELOG V5 completed/success exact-SHA, BITACORA y PLAN/COLA readback. **A5:** sólo Task Javier distinta de la autora.
- **Rollback:** `git revert` normal del commit de documentación, sin force-push. QA, main y producción NO AUTORIZADAS.
