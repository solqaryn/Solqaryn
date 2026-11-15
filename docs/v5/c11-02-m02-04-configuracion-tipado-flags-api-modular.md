# SOLQARYN V5 — C11-02.M02.04: configuración tipada, ámbitos y feature flags de API modular

**Microtarea:** `C11-02.M02.04`, fase M02 contractual de `C11-02` API modular. **Autor:** PRIMARY DEV 06 / JAVIER_CHATGPT, `TASK_ID=6af16bbad26881918c283abe06e1f32a`. **Repositorio/rama:** `solqaryn/Solqaryn@dev` exclusivamente, `PROJECT_SCOPE_LOCK=STRICT`. **Fuente:** STANDARD V5 FINAL + PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA. **Naturaleza:** especificación conceptual, **no** actualización de appsettings, variables, DI, APIs, DTO, servicios, flags operativos, bases de datos o frontend.

## 1. A0 — contrato precedente, código observado y límites

PLAN H1631 estaba PENDIENTE e I1631 vacío; COLA G/O=SI y DAG F=VALIDADO/G=SI. El predecesor `C11-02.M01.06` tiene LISTO y A5_PASS independiente (revisor DEV06 distinto del autor DEV04). Se tomó la unidad por CAS único sobre H1631 y se leyó posteriormente I1631 con el Task ID de DEV06. Las restantes M02.01–05 pueden ser documentos independientes con responsables distintos; no se deben sobrescribir sus archivos ni reinterpretar sus tests como ejecutados.

Los contratos existentes examinados son `backend/src/API/Program.cs` (blob `321fc0a3c29d5cbc53f5e30373f82939e907d6f0`, políticas JWT Bearer, CORS allowlist y rate-limiter de login), `backend/src/API/Controllers/AuthController.cs` (blob `af437c869b4800a499c42aeff00347de3a3cc521`, rate limiting `AuthLogin` y renovación autenticada), API `net10.0` (`backend/src/API/Solqaryn.API.csproj`, blob `2a7b33a0974d1ebe13676593d84b458b1243e037`) y EF Core 10.0.12 + proveedor Oracle MySQL 10.0.9 (`backend/src/Infrastructure/Solqaryn.Infrastructure.csproj`, blob `6210d8b12abd77171d95d17e2292c1a319b89263`). Las referencias M01.03 (blob `72d3fec054b19398f17a7c9b2cb7139a86fc9033`), M01.05 (blob `d94c28285ea75f1ff813fd5bcd54fbc9f312186b`) y terminal M01.06 (blob `a096c5b0cdc4a520664ca41d2dfc5c53d48a4cb9`) delimitan los requisitos, no autorizan runtime. No se inspeccionaron secretos ni configuraciones de servicio vivo.

## 2. Catálogo F01–F16: opciones PROPUESTAS, NO claves existentes

| ID | Tipo conceptual | Autoridad/ámbito futuro | Default restrictivo y validación |
| --- | --- | --- | --- |
| F01 | `ApiModuleEnabled: bool` | Plataforma/entorno | false para módulos nuevos hasta aprobación |
| F02 | `ApiContractVersion: semver` | Plataforma/versionado | valor allowlisted, rechazar versión desconocida |
| F03 | `TenantResolutionMode: enum` | Plataforma seguridad | servidor verificado; nunca body/header/slug como grant |
| F04 | `PlatformScopeEnabled: bool` | Autoridad plataforma | false sin permiso global explícito |
| F05 | `CompanyMembershipRequired: bool` | Invariante global | true para recursos empresariales |
| F06 | `SupportReadOnly: bool` | Contexto soporte | true, imposible habilitar DML por tenant |
| F07 | `JwtIssuerAudiencePolicy: referencia` | Entorno/seguridad | conjunto confiable sin JWT libre |
| F08 | `JwtSigningKeySource: referencia segura` | Entorno/seguridad | secreto ausente: NO_READY, nunca fallback |
| F09 | `AllowedOrigins: conjunto` | Plataforma | allowlist cerrada, sin wildcard sensible |
| F10 | `RequestBodyLimitBytes: int` | Plataforma | límite positivo acotado; nunca cliente lo eleva |
| F11 | `PageSizeMax: int` | Plataforma/módulo | acotado y server-side, consulta no ilimitada |
| F12 | `LoginRateLimit: política` | Plataforma/seguridad | restricción server-side, nunca desactivada por tenant |
| F13 | `IdempotencyRequired: bool` | Operaciones con efectos | true cuando una operación pueda repetirse |
| F14 | `ErrorDisclosurePolicy: enum` | Plataforma/seguridad | código seguro y redacción estricta, sin stack/SQL |
| F15 | `SupportCommandGuard: bool` | Seguridad/soporte | true, DML prohibido en soporte de lectura |
| F16 | `ApiRolloutKillSwitch: bool` | Operador de plataforma autorizado | desactivación de módulo seguro; nunca grant de ejecución |

Este catálogo es un **contrato propuesto**: no hay que inferir propiedades C# ni variables de entorno creadas, desactivar políticas vigentes o cambiar configuración en `Program.cs`. Donde ya existe una política, la propuesta exige conservarla o endurecerla después de pruebas dirigidas; no duplicarla.

## 3. Jerarquía, precedencia y ownership

1. **Invariantes innegociables:** tenant A/B, autorización servidor JWT/RBAC, revocación, scope de plataforma separado, prohibición de escritura en soporte, redacción de secretos, A5 independiente y Scope Lock. No existen overrides inferiores.
2. **Plataforma/entorno:** fija versiones, límites, CORS, seguridad JWT, fuentes de secretos, rate-limit, módulos permitidos y kill switches; las modificaciones futuras exigen operador autorizado y evidencia de CI.
3. **Empresa:** puede imponer restricciones adicionales sobre módulos explícitamente permitidos; jamás concede permisos globales, desactiva rate limiting, elige signing keys, enciende rutas administrativas ni accede a otro tenant.
4. **Usuario/sucursal:** sólo contexto autenticado y membresía/rol verificados. Ningún claim o DTO fabrica autoridad de empresa.
5. **Cliente Angular:** configura la presentación, no habilita rutas ni permisos backend; estado almacenado o URL manipulada se vuelven NO_GO.
6. **Contención:** si un flag de seguridad o secreto falta, fallar cerrado; un interruptor apagado conserva denegación y no autoriza una promoción futura.

## 4. V01–V18: controles de validación y rollback

| ID | Regla verificable en implementación futura |
| --- | --- |
| V01 | Rechazar clave de configuración no allowlisted que altere autoridad |
| V02 | Denegar empresa obtenida sólo de header, slug, query o body |
| V03 | JWT ausente/expirado inválido no activa módulo ni sesión |
| V04 | Membresía revocada invalida acceso aun si la UI conserva estado previo |
| V05 | Scope global y scope empresa no se mezclan entre servicios |
| V06 | Soporte sólo lectura nunca puede cambiar EF tracking/SaveChanges |
| V07 | URL CORS no allowlisted no queda permitida por wildcard |
| V08 | Faltan issuer/audience/key: no readiness de autenticación |
| V09 | Una versión DTO desconocida no deserializa campos de autoridad |
| V10 | Payload malformado o demasiado grande se rechaza antes de mutar |
| V11 | Paginación o sort no permitido nunca dispara lectura ilimitada |
| V12 | Rate-limit de login permanece operativo con solicitudes concurrentes |
| V13 | Feature flag false bloquea acceso en servidor, no sólo en menú Angular |
| V14 | Tenant no supera límites globales ni activa un módulo deshabilitado |
| V15 | Error/telemetría jamás vuelca SQL, stack, JWT, DSN, correo ni PII |
| V16 | Mutaciones idempotentes no causan doble efecto con reintento |
| V17 | Deshacer un cambio no reabre permisos ni pierde trazabilidad y rollback |
| V18 | CI requerida exact-SHA completa SUCCESS y A5 por otra Task antes de gates |

Si alguna validación no cuenta con evidencia positiva y negativa real, no convertir `FUTURO` en `PASS`; mantener alcance bloqueado y continuar solo las unidades independientes.

## 5. P01/N01–P10/N10 — pruebas futuras NO EJECUTADAS

| Par | Caso positivo futuro | Rechazo negativo futuro |
| --- | --- | --- |
| P01/N01 | JWT válido de A con membresía vigente accede a recurso A | Token vencido o actor B intenta recurso A |
| P02/N02 | Plataforma con grant global explícito opera ruta global | Actor empresa intenta privilegio plataforma |
| P03/N03 | Soporte autorizado consulta sin mutación | Soporte intenta DML o `SaveChanges` |
| P04/N04 | Origen CORS allowlisted y payload acotado | Origen no autorizado o body sobredimensionado |
| P05/N05 | Rate-limit de login controla ráfaga | Múltiples intentos eluden límite por cambio de tenant |
| P06/N06 | Paginación autorizada acota resultados | Query exige página/tamaño/sort ilimitados |
| P07/N07 | DTO válido responde versión compatible | Mass assignment envía rol o EmpresaId falsificados |
| P08/N08 | Error controlado devuelve código seguro | Error muestra SQL, stack o secreto |
| P09/N09 | Idempotencia responde un solo efecto | Reintento concurrente produce doble efecto |
| P10/N10 | Flag autorizado y CI exact SHA mantienen seguridad | Flag ambiguo, CI cancelled o autor firma su A5 |

**Ningún P/N fue ejecutado por este documento.** Requieren pruebas reales .NET 10/xUnit, autorización y HTTP con tenants A/B; EF/MySQL 8.4 efímero cuando se modifique persistencia; Angular/Playwright cuando se cambien superficies. No reproducir como resultados los escenarios de M01.05.

## 6. Contrato de adopción gradual y reversión

- Elegir una operación/DTO real por microtarea; registrar archivos, datos, consumidores y riesgos; no agrupar permisos o esquema con presentación sin razón causal.
- Introducir opciones tipadas sólo mediante implementación M03 posterior autorizada, nunca desde este M02.04; validar valores faltantes y migración de configuración.
- Antes de habilitar cualquier bandera, probar rutas positivas y negativas A/B, rol revocado, origen no confiable, sin secreto, límites y compatibilidad de API/Angular.
- Activar únicamente después de CI dirigida completed/success y A5 independiente, con rollback del cambio real, auditoría y permiso de entorno separado.
- Para revertir **esta documentación**, `git revert` normal de los dos Markdown tras releer HEAD, preservando BITACORA y cambios concurrentes. No hay código, configuración, data ni servicios que deshacer.

## 7. A0–A4 y A5 obligatorios para este changeset

**A0:** Skill y arquitectura canónicas, identidad Javier, Scope Lock, HEAD; PLAN PENDIENTE sin dueño, COLA G/O SI, DAG VALIDADO SI, CAS por H1631 y owner único. **A1:** cotejar 16 opciones propuestas, 18 reglas V y diez pares P/N como criterios futuros, no código. **A2:** tenant A/B, JWT/RBAC, roles de plataforma, soporte lectura, CORS, secretos, rate limit y fail-closed revisados conceptualmente. **A3:** confirmar que el código actual de `Program.cs` y `AuthController.cs` respalda los puntos de integración; CI de compilación y MySQL **NO_APLICA** por diff sólo Markdown, nunca son PASS fingidos. **A4:** commit atómico de `docs/v5/c11-02-m02-04-configuracion-tipado-flags-api-modular.md` más sección autocontenida de `CHANGELOG_AI.md`, fast-forward/CAS en dev; check Scope Lock y V5 exact SHA `completed/success`, readback de Git/PLAN/COLA/DAG/BITACORA. **A5:** por DEV01–DEV05, distinto del autor DEV06; ningún A5 propio ni QA/main/PROD.

No se crean eventos, webhooks, Tasks, workers, secretos, migraciones, proveedores, DNS, infraestructura, pagos ni cambios productivos.
