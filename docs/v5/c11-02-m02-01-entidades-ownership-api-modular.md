# SOLQARYN V5 — C11-02.M02.01: entidades lógicas, ownership e invariantes de API modular

**Microtarea:** `C11-02.M02.01`. **Autor:** PRIMARY DEV 06 / JAVIER_CHATGPT, `TASK_ID=6af16bbad26881918c283abe06e1f32a`. **Repositorio/única rama:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, PLAN_MAESTRO, COLA, DAG_TECNICO y BITACORA. **Modalidad:** contrato exclusivamente documental de M02; NO implementa clases, servicios, endpoints, cambios JWT/RBAC, persistencia, migraciones ni frontend.

## 1. A0 — Fuentes, límites e impacto

Antes de reclamar se constató PLAN `C11-02.M02.01` PENDIENTE, sin responsable, COLA G/O=SI y DAG F=VALIDADO/G=SI; el terminal `C11-02.M01.06` fue cerrado en LISTO con A5 de revisor Javier independiente de DEV04. El claim de esta unidad usa CAS de una sola fila H1628 y responsable DEV06 con readback. Las M01.01–05 ya tenían A5 independientes; M01.06 establece 16 gates G01–G16, seis positivos y diez negativos y ocho pasos de reversión **FUTUROS_NO_EJECUTADOS**.

Anclajes a código existente, no a implementaciones hipotéticas:
- `backend/src/API/Controllers/AuthController.cs`, blob `af437c869b4800a499c42aeff00347de3a3cc521`: login, renovación, códigos 401 y rate limit.
- `backend/src/API/Program.cs`, blob vigente `321fc0a3c29d5cbc53f5e30373f82939e907d6f0`: composición de middlewares, JWT, autorización y CORS; el contenido registrado en M01.03 fue snapshot anterior, no autoridad congelada.
- `backend/src/API/Solqaryn.API.csproj`, blob `2a7b33a0974d1ebe13676593d84b458b1243e037`: API ASP.NET Core `net10.0`.
- `backend/src/Infrastructure/Solqaryn.Infrastructure.csproj`, blob `6210d8b12abd77171d95d17e2292c1a319b89263`: EF Core 10.0.12 y proveedor Oracle `MySql.EntityFrameworkCore` 10.0.9.
- `docs/v5/c11-02-m01-03-inventario-contratos-codigo-api-modular.md`, blob `72d3fec054b19398f17a7c9b2cb7139a86fc9033`: dieciséis referencias de código por capa. `docs/v5/c11-02-m01-05-criterios-aceptacion-api-modular.md`, blob `d94c28285ea75f1ff813fd5bcd54fbc9f312186b`: 18 criterios positivos y negativos futuros. `docs/v5/c11-02-m01-06-gates-rollback-salida-api-modular.md`, blob `a096c5b0cdc4a520664ca41d2dfc5c53d48a4cb9`: dieciséis gates y reversión.

**Impacto de esta entrega:** sólo `docs/v5` y `CHANGELOG_AI.md`. No se cambia Domain, Application, Infrastructure, API, Angular, MySQL, contratos públicos, secretos, auditoría runtime, reglas de despliegue ni datos. Los consumidores futuros no quedan habilitados por este documento.

## 2. Modelo lógico E01–E16 (NO clases/tablas nuevas)

| ID | Entidad/contrato lógico | Ownership verificado que deberá imponer el servidor |
| --- | --- | --- |
| E01 | Actor de sesión | Identidad autenticada y sesión vigente; claim no equivale a privilegio |
| E02 | Membresía empresarial | Relación real actor–empresa activa, obtenida de fuente confiable |
| E03 | Empresa/tenant | Recurso empresa en persistencia, nunca elegido unilateralmente por DTO |
| E04 | Rol empresarial | Rol vigente ligado a empresa, no al catálogo global de plataforma |
| E05 | Permiso por recurso/acción | Acceso calculado en backend al recurso concreto |
| E06 | Scope de plataforma | Grant global explícito, sin contaminar autorización de empresa |
| E07 | Scope soporte lectura | Identidad especial únicamente consultiva, sin SaveChanges ni DML |
| E08 | Endpoint/ruta | Requiere un sujeto, ámbito, acción y política de acceso declarados |
| E09 | DTO de entrada | Allowlist de campos y validación backend; no mass assignment |
| E10 | DTO de salida | Contrato versión/compatibilidad, sin campos privados de otro tenant |
| E11 | Recurso de negocio | Owner empresa real persistido, chequeo BOLA/IDOR antes de acceso |
| E12 | Operación transaccional | Agregados y mutaciones ACID dentro de un tenant y transacción |
| E13 | Error seguro `ApiResponse` | Código/estatus coherente sin SQL, stack, token, PII ni enumeración |
| E14 | Auditoría y correlación | CorrelationId de trazabilidad opaco, jamás autoridad JWT/RBAC |
| E15 | Control de cuota/límites | Rate-limit/paginación de servidor, sin escala libre de cliente |
| E16 | Evidencia de revisión y release | SHA, diff, CI causal y A5 independiente antes de otros entornos |

**Relaciones conceptuales:** Actor N:M Empresa mediante Membresía; Empresa 1:N Recurso; Rol pertenece a Empresa, y Permiso aplica a recurso/acción; Scope plataforma independiente de Rol empresarial; Ruta N:1 policy; DTO nunca es dueño ni fuente de tenant; transacción opera exclusivamente recursos autorizados del mismo tenant; auditoría no concede permisos. Estas son condiciones de contrato, **no mapeos EF nuevos ni esquema implementado**.

## 3. Invariantes fail-closed I01–I18

| ID | Invariante exigible en desarrollo futuro |
| --- | --- |
| I01 | Ningún header, query, slug o body concede EmpresaId ni membresía |
| I02 | Token inválido, sesión revocada o usuario inactivo devuelve 401 sin datos |
| I03 | Sin membresía activa para A, no se obtiene autoridad ni se consulta A |
| I04 | Recurso de B está vedado incluso a actor válido de A |
| I05 | Rol de plataforma requiere grant explícito y nunca confiere acceso empresarial por defecto |
| I06 | Soporte lectura no puede emitir DML, mutar tracked entities o invocar comandos |
| I07 | Cada endpoint comprueba autorización real backend, no sólo guard Angular |
| I08 | Cada DTO admite únicamente campos allowlisted; `EmpresaId` y rol no elevan privilegios |
| I09 | Contrato de `ApiResponse` preserva forma/códigos o exige versionado y revisión de consumidores |
| I10 | Errores 400/401/403/404/409 evitan stack, SQL, token, correo, secretos y PII |
| I11 | No se permite enumerar existencia de datos B mediante diferencias de status o mensaje |
| I12 | Paginación, filtros, sort, tamaño y tasa de consulta tienen límites server-side |
| I13 | Operación multientidad hace commit/rollback transaccional, sin efecto parcial |
| I14 | Idempotencia y control de concurrencia impiden duplicación de efectos |
| I15 | Logs solo admiten campos redactados; correlationId no identifica al actor |
| I16 | Cambios EF/DDL exigen migrador único y ensayo MySQL 8.4 efímero con reversión |
| I17 | Check requerido sólo PASS con `completed/success` en SHA pertinente, nunca cancelled/skipped |
| I18 | Revisor A5 distinto del autor y permiso de entorno separado antes de QA/main/PROD |

**Validación tenant A/B:** un actor autenticado A intentando recurso B debe ser denegado antes de devolver datos, mutar, registrar auditoría sensible o confirmar un efecto; no se confunde autoría con `EmpresaId` recibido. Preservar reglas de JWT/BCrypt/RBAC, CORS y rate limiting existentes.

## 4. Matriz de fallos F01–F10 y contención futura

| Falla | Comportamiento seguro esperado |
| --- | --- |
| F01 Actor anónimo/caducado | 401, nunca `ApiResponse.Success=true` |
| F02 Rol empresarial falso | 403/404 sin visibilidad de otro tenant |
| F03 Soporte intenta escritura | Denegación antes de `SaveChanges`; verificar cero efectos |
| F04 Cliente envía `EmpresaId` ajeno | Ignorar o rechazar; ningún bypass de resource ownership |
| F05 DTO con propiedad protegida | Rechazar sin asignación masiva |
| F06 Conflicto de transacción | Rollback íntegro y 409 seguro |
| F07 Error desconocido con datos privados | Redactar servidor/log, preservar estado consistente |
| F08 Peticiones repetidas concurrentes | Idempotencia comprobada, un solo efecto |
| F09 SQL/EF schema alterado sin owner | NO_GO, no migrar desde este Markdown |
| F10 CI incompleta o A5 del autor | Estado VALIDANDO, no liberar sucesoras |

## 5. Oráculos de aceptación P01/N01–P10/N10 FUTUROS_NO_EJECUTADOS

| Par | Positivo futuro con herramienta real | Negativo futuro |
| --- | --- | --- |
| P01/N01 | HTTP autenticado actor A consulta recurso A | A intenta B con URL/DTO manipulado |
| P02/N02 | Sesión vigente renueva token bajo permisos | Sesión revocada recibe 401 |
| P03/N03 | Usuario con dos empresas cambia al scope válido | Reutiliza JWT de A para recurso B |
| P04/N04 | Rol empresa permite acción autorizada | Rol revocado o plataforma implícita intenta escribir |
| P05/N05 | Soporte lee datos permitidos con SQL guard | Soporte intenta DML/SaveChanges |
| P06/N06 | DTO permitido se valida y responde compatible | Mass assignment de rol o EmpresaId |
| P07/N07 | Error dominio retorna 409 sin PII | SQL/stack/token aparece en HTTP/log |
| P08/N08 | MySQL efímero revierte transacción fallida | Efecto parcial tras crash |
| P09/N09 | Idempotencia de request evita duplicados | Carrera produce dos efectos |
| P10/N10 | CI exact SHA y A5 independiente permiten gate | Check skipped/cancelled o autor se revisa |

Estos 20 escenarios **NO se han ejecutado como parte de M02.01**. Requieren .NET/xUnit y HTTP reales, Playwright de consumidor cuando pertinente, MySQL 8.4 efímero para datos, y pruebas negativas JWT/RBAC/tenant A/B en microtareas ejecutables posteriores.

## 6. A0–A5, dependencias y reversión

**A0:** Skill local igual a HEAD canónico, GitHub Javier/Drive, Scope Lock STRICT y fuente V5; PLAN PENDIENTE sin owner, COLA G/O=SI, DAG F=VALIDADO/G=SI; CAS H1628, owner I1628. **A1:** revisar 16 entidades, 18 invariantes, 10 fallas y diez pares positivos/negativos como contrato; no reportar implementación. **A2:** aislamiento A/B, JWT/RBAC, revocación, soporte lectura, PII, fail-closed y sin confiar en payload del cliente. **A3:** verificar correspondencia conceptual con `AuthController.cs`, `Program.cs`, `ApiResponse`, servicios/contratos del inventario M01, net10/EF/MySQL; compilación y navegador **NO_APLICA por cambio sólo Markdown**, nunca PASS inventado. **A4:** publicar en dev `docs/v5/c11-02-m02-01-entidades-ownership-api-modular.md` y sección propia de `CHANGELOG_AI.md` por fast-forward CAS; comprobar Scope Lock y V5 `completed/success` en SHA exacto, diff, BITACORA, PLAN/COLA/DAG y owner. **A5:** revisión real independiente por DEV01–DEV05, nunca DEV06, antes de habilitar sucesores que la requieran.

**Rollback:** `git revert` normal del changeset documental preservando historial y cambios ajenos; registrar evento compensatorio si hace falta. No ejecutar API, DB, EF migrations, workers, eventos, infraestructura, QA, main, PROD, DNS, Cloudflare/R2 ni nuevos pagos/Tasks por este contrato.
