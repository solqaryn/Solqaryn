# SOLQARYN V5 FINAL — C01-01.M01.01: objetivo, no-alcance y no-regresión de identidad universal

**Tipo:** preparación exclusivamente documental M01. **Resultado futuro C01-01:** dominio de identidad y pertenencia para empresa, sucursal, recurso, usuario, membresía y contexto; no se declara implementado ni probado integralmente por este documento. **Fuente ejecutable:** Standard de Implementación V5 FINAL y Sheet PLAN_MAESTRO/COLA; DAG_TECNICO de C01-01.M01.01 en estado VALIDADO. Prerrequisitos: C00-01.M02.01–06 y C00-03.M01.01–05, comprobados LISTO. No se habilitan M01.06, M02 ni fases ejecutables por esa preparación.

## 1. Objetivo y frontera

Definir, antes de cualquier cambio de código, el objetivo de una **identidad universal con aislamiento multiempresa**: una persona/usuario puede tener membresías diferenciadas por empresa, un rol de plataforma nunca se infiere de rol empresarial y el recurso operativo debe heredar un ámbito verificable (empresa y, cuando aplique, sucursal). El JWT no es permiso universal: el backend resuelve y valida sesión, membresía activa, rol y ámbito para la operación solicitada. No se acepta `EmpresaId`, `SucursalId`, slug, query, cookie o header proporcionados por el navegador como prueba de pertenencia.

La salida atómica de **M01.01** consiste exclusivamente en objetivo, exclusiones y pruebas de no-regresión definidas de forma falsable; C01-01.M01.02–05 completarán inventarios, riesgos y aceptación. No inventar tablas nuevas, endpoints o migraciones sólo porque aparecen en este modelo documental.

## 2. Evidencia del dominio existente (lectura de dev)

| Evidencia real de repositorio | Observación delimitada | Riesgo que debe conservar el diseño |
| --- | --- | --- |
| `backend/src/Domain/Entities/Empresa.cs` (blob `c44940df7c30983a4c23b93338e0cb9367962069`) | Existe raíz `Empresa` y marca de actividad | No deducir aislamiento total por existencia de entidad |
| `backend/src/Domain/Entities/Sucursal.cs` (blob `8746b11fa30cdac0b97241b6221100bedd813b35`) | `EmpresaId` nullable por compatibilidad; resolver propietario inválido debe fallar cerrado | No aceptar sucursal sin empresa positiva para writes nuevos |
| `backend/src/Domain/Entities/UsuarioEmpresa.cs` (blob `f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c`) | Membresía explícita usuario/empresa y rol efectivo del ámbito | No reutilizar rol de empresa A en empresa B |
| `backend/src/Domain/Entities/Usuario.cs` (blob `cfe319bb61e405379fa632ed18d3183cd2df1a68`) | Modelo `Usuario` con RolId legacy y hash de contraseña | No usar compatibilidad legacy como permiso multitenant |
| `backend/src/Domain/Entities/Rol.cs` (blob `a4f913d66f2c6bd2351155d66a93f9d12dc18a09`) | Roles con ámbito Empresa/Plataforma y propietario opcional | Nunca elevar un rol empresarial a autoridad global |
| `backend/src/Domain/Security/ContextoTenantActual.cs` (blob `7377d2d752985cc9baf31d08280f38ad94a6a114`) | Fábrica desde membresía verificada, no claims arbitrarios | No permitir que el cliente construya contexto confiable |

Estas observaciones son de código leído, **no** certifican persistencia, contratos externos ni cobertura de seguridad de todo el sistema. Los consumidores, API, EF/MySQL y pruebas completas deberán mapearse antes de ejecución M02–M05.

## 3. Fuera de alcance

No modificar JWT, BCrypt, RBAC, claims, política de cookies, esquema tenant, catálogos de roles, servicios, endpoints, componentes Angular, filtros, contexto EF, tablas, índices, migraciones o datos reales. No crear clases, DTO, rutas, flags, usuarios, proveedores, secretos o automatizaciones. No activar roles globales ni permisos implícitos, no tocar QA/main/PROD, DNS, Render, Aiven, Vercel, Hostinger ni Cloudflare. No alterar el inventario de imágenes históricas reportado cero ni simular migración. Esta entrega no autoriza un cambio de arquitectura ni el cierre anticipado de C01-01 completo.

## 4. Invariantes y casos de no-regresión exigibles a la implementación futura

| Caso | Prueba positiva o negativa | Resultado esperado y evidencia futura |
| --- | --- | --- |
| NR-01 (+) | Usuario con membresía activa en empresa A y rol autorizado consulta recurso de A | Backend permite sólo el recurso propio, con prueba real de API/DB |
| NR-02 (+) | Usuario con membresías distintas cambia explícitamente contexto A→B | Se verifica nueva membresía y rol efectivo; sin fuga de A |
| NR-03 (+) | Administrador autorizado de sucursal accede a recurso de su ámbito | Propiedad empresa/sucursal resuelta server-side, auditada |
| NR-04 (-) | Usuario de A cambia `EmpresaId`, slug, URL o header a B | Denegación segura 403/404 y cero datos o mutaciones de B |
| NR-05 (-) | Token caducado, suplantado o membresía revocada intenta acción protegida | 401/403, sin confiar sólo en claims/cache anterior |
| NR-06 (-) | Rol empresarial solicita permiso reservado a plataforma | Denegación por ámbito, aunque el nombre del rol coincida |
| NR-07 (-) | Sucursal sin EmpresaId positivo o recurso sin propietario verificable | Fail-closed; no fallback a primer tenant |
| NR-08 (-) | Usuario de sucursal A solicita inventario/documentos de sucursal B sin permiso transversal | Aislamiento por alcance y contrato; no fuga ni side effects |
| NR-09 (-) | Payload malicioso, inyección, mass assignment o IDs inconsistentes | Validación server-side, error seguro, sin ejecutar payload ni modificar otras empresas |
| NR-10 (-) | Frontend oculta control pero usuario llama API sin rol | Backend continúa denegando por JWT/membresía/RBAC |
| NR-11 (control) | Error parcial en cambio de membresía o relación de recursos | Transacción, auditoría y rollback no destructivo; sin permisos huérfanos |
| NR-12 (control) | A5 del mismo TASK_ID o evento BITACORA truncado | Rechazar autoprobación y registro incompleto; BITACORA A:Z con 26 campos e idempotencia |

**No-regresión de M01.01 documental:** comprobar que objetivo, exclusiones, riesgos y casos negativos están explícitos y se contrastaron con las fuentes; NO reportar NR-01–12 como tests ejecutados de API, DB o browser. Pruebas dirigidas de implementación pertenecen a los cambios reales posteriores y usarán GitHub Actions del SHA exacto, no la CI de una rama anterior.

## 5. Gates, continuidad y rollback

- Identidad y autoridad: `solqaryn/Solqaryn@dev`, Skill canónica, `AGENTS.md`, Scope Lock STRICT; Plan V5 `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA` y Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`.
- DAG_TECNICO de este ID validado con once requisitos LISTO; adelantamiento reservado sólo para `M01.01–05` documental. El objetivo de mantener 2N–3N candidatas no convierte una dependencia no cumplida en permiso de despacho. Antes de M01.06/M02/código revalidar C00-01 final y contratos C01-01.
- Validación proporcional de este documento: `requiere_dotnet=NO`, `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO`; `requiere_security=SI` (JWT/RBAC/membresía/tenant, PII/secretos, casos negativos), `requiere_scope_lock=SI` para SHA de publicación.
- A0 documento/diff/no secretos; A1 objetivo/no-alcance y NR-01–12 verificables; A2 fronteras de seguridad y denegaciones; A3 coherencia con fuentes código, DAG, PLAN/COLA y BITACORA A:Z; A4 commit directo fast-forward y check `verify-solqaryn-project-scope` completed/success exact-SHA con readback; A5_PENDIENTE por otra Task para cualquier promoción posterior.
- Reversión: Git revert normal del commit documental previa revisión de HEAD/concurrencia, sin reescribir historia ni afectar datos. QA, main y PROD no están autorizados por M01.01.
