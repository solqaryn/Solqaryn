# SOLQARYN V5 — C11-02.M02.02: contrato de API/DTO, errores y compatibilidad

**ID:** C11-02.M02.02. **Lote:** C11-02 API modular, fase M02 de contratos y modelo. **Responsable:** PRIMARY DEV 01, TASK_ID=6adbb9fff1548191a1bd5b30d9e9984a. **Repositorio/branch:** solqaryn/Solqaryn, dev; PROJECT_SCOPE_LOCK=STRICT. **Fuente única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL y PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA. Fecha de preparación: 2026-11-15.

## 1. Decisión de alcance

Se define un **contrato de diseño futuro** para mantener controladores pequeños, DTOs tipados, errores seguros y clientes existentes en la evolución de API modular. La salida NO instala endpoints ni cambia los contratos públicos actuales. No se modifican controladores, Application, Domain, Infrastructure, JWT/RBAC, EF/MySQL, servicios, DI, Angular, dependencias, tareas programadas, red, secretos o proveedores. El gate de M02.02 no libera automáticamente M02.06, M03 ni QA/main/PROD; cada sucesor exige PLAN/COLA/DAG, owner, evidencia y A5 propios.

Precondición revisada: C11-02.M01.01–06 están LISTO con A5 independientes, incluido M01.06 de autor DEV04 y revisor DEV06. DAG_TECNICO por ID C11-02.M02.02 tiene F=VALIDADO/G=SI; COLA G/O=SI y PLAN pendiente sin owner antes de la reclamación. Estas verificaciones autorizan **solo documentación M02**, no implementación.

## 2. Evidencias reales del código vigente

| Componente | Blob Git verificado | Límite de interpretación |
| --- | --- | --- |
| API/Controllers/AuthController.cs | af437c869b4800a499c42aeff00347de3a3cc521 | POST auth/login anónimo con rate limit AuthLogin; POST auth/renovar autorizado y rechazo si sesión inválida |
| Application/Interfaces/IAuthService.cs | 986743c5ba7015c5c6bda564cc50781f3b26cd65 | LoginAsync y RenovarAsync retornan LoginResponseDto? |
| Application/DTOs/LoginDto.cs | 6851ae4763fab9bc05bcf14ab75c0c8fd446089e | LoginRequestDto (NombreUsuario, Password); LoginResponseDto con Token/ExpiraEn, rol y perfil |
| Application/Common/ApiResponse.cs | aba995eae50ad756712961c9c394f5038a31a142 | ApiResponse<T>: Success, Data, Message, Errors; fábricas Ok/Fail |
| API/Controllers/TenantContextController.cs | 2407c2eb6779ed724e841c688ee14dabbf163171 | GET tenant-context/empresas y GET tenant-context/{empresaId}; membresía server-side, rechazo no enumerante |
| API/Controllers/PlataformaSesionController.cs | 965a05321a8b8c3f90011addb84e14bcc5978775 | GET plataforma/sesion, rol global separado, no-store y auditoría |
| API/Controllers/UsuariosController.cs | 53d1eabb7a5e04b5e35be114c3021bf343d9bcc8 | Auth y permiso empresarial en rutas de usuarios; respuestas ApiResponse, GET paginado |
| API/Filters/TenantPermissionContext.cs | df7d49f3e185c538f3b3dafabb36c3a971a7257d | X-Empresa-Id es selector, nunca autorización; rechaza selector ambiguo |
| API/Filters/FluentValidationActionFilter.cs | fec754eeb2c7e41599155f25a24e8adbb53298bb | Resuelve validadores de argumentos; ausencia de validador no demuestra cobertura total |
| API/Middleware/ExceptionHandlingMiddleware.cs | baee40143e4e48b8df200e77892606af96d60b7f | Manejo de ValidationException, BusinessRuleException y excepciones; revisar redacción por contrato |
| docs/v5/c11-02-m01-03-inventario-contratos-codigo-api-modular.md | 72d3fec054b19398f17a7c9b2cb7139a86fc9033 | Inventario limitado a 16 fuentes, doce brechas de evidencia, no auditoría integral |
| docs/v5/c11-02-m01-06-gates-rollback-salida-api-modular.md | a096c5b0cdc4a520664ca41d2dfc5c53d48a4cb9 | Dieciséis gates G, P06/N10 futuros y ocho medidas de rollback |

Este cotejo es documental. Ni compilar ni leer fuentes acredita todos los endpoints, autorización real de cada ruta, estado de base productiva, pruebas HTTP o aceptación de consumidores.

## 3. Diez contratos candidatos de operación OP01–OP10

| ID | Superficie existente / futuro caso de uso | Contrato propuesto y compatibilidad |
| --- | --- | --- |
| OP01 | POST auth/login | Mantener request tipado, rate limit y 401 por credenciales inválidas; no registrar Password/Token |
| OP02 | POST auth/renovar | Revalidar identidad, revocación, expiración y sesión; no confiar sólo en un claim |
| OP03 | GET tenant-context/empresas | Listar únicamente empresas seleccionables del sujeto autenticado |
| OP04 | GET tenant-context/{empresaId} | Selector positivo pero no autoritativo; obtener membresía activa de backend o denegar sin enumerar |
| OP05 | GET plataforma/sesion | Identidad global separada de EmpresaId; caché deshabilitada, audit trail |
| OP06 | GET usuarios | Permiso Usuarios.Ver y tenant real; no fuga de listados cruzados |
| OP07 | GET usuarios/paginado | Page/size/sort tipados y acotados; preservar shape PagedResult y ApiResponse |
| OP08 | GET usuarios/{id:int} | Evitar IDOR al consultar id de otro tenant; 404 genérico donde corresponda |
| OP09 | Futuro caso de uso Application | Interface por responsabilidad, cancelación, validación y DTO explícitos; sin exponer entidad EF |
| OP10 | Error de cualquier endpoint | 400/401/403/404/409/500 clasificados, sin PII/SQL/stack; no cambiar HTTP ni ApiResponse sin versionado |

OP09/OP10 son contratos de evolución, **no** rutas o handlers nuevos. No se asignan códigos concretos que no estén demostrados para cada caso por tests futuros.

## 4. Dieciséis invariantes de compatibilidad y seguridad V01–V16

| ID | Invariante exigida antes de cambiar runtime |
| --- | --- |
| V01 | La identidad autenticada es requisito en endpoints privados; login anónimo conserva rate limit |
| V02 | Ningún EmpresaId/header/body/query concede autoridad por sí mismo |
| V03 | UsuarioEmpresa activa y permisos RBAC se verifican en backend por empresa |
| V04 | Permiso de plataforma no implica acceso empresarial |
| V05 | Soporte read-only no puede ejecutar comandos mutables ni usar bypass de DB |
| V06 | DTO de entrada usa allowlist, límites y validación server-side; niega mass assignment |
| V07 | Respuestas públicas conservan status/shape o se versionan tras inventariar consumidores |
| V08 | LoginResponseDto.Token y credenciales no se registran en logs ni trazas |
| V09 | Errores de autorización no enumeran empresas, usuarios privados ni recursos B |
| V10 | Errores no muestran SQL, excepciones crudas, stack ni nombres privados |
| V11 | Validación y model state no aceptan coerciones o payload ambivalente como grant |
| V12 | Operación idempotente genera a lo sumo un efecto al repetirse bajo carrera |
| V13 | EF/MySQL futuros aplican transacciones, historial y rollback probado antes de aprobar |
| V14 | CorrelationId de auditoría no es token de autorización ni acceso a otro tenant |
| V15 | Angular guard, branding o feature flag jamás reemplazan autorización de API |
| V16 | Required CI es completed/success en SHA causal; A5 distinto de autor, no QA/PROD implícitos |

## 5. Matriz de aceptación futura P01/N01–P10/N10 — NO EJECUTADA

| Par | Positivo exigible en futura CI | Negativo que debe denegarse |
| --- | --- | --- |
| P01/N01 | Login válido entrega response tipado | Credenciales inválidas o brute force sin límite |
| P02/N02 | Renovación de sesión vigente | Token/usuario revocado obtiene nueva sesión |
| P03/N03 | Listado de tenants autorizados A | Actor B enumera empresa A |
| P04/N04 | Selector de empresa con membresía activa | Header X-Empresa-Id falsificado cruza tenant |
| P05/N05 | Rol de plataforma válido consulta sesión global | Rol de plataforma crea permiso empresa inexistente |
| P06/N06 | GET usuarios con permiso vigente A | Usuario B lee objeto de A por id |
| P07/N07 | Paginación/sort permitido y acotado | Tamaño excesivo y sort arbitrario |
| P08/N08 | DTO válido produce status y ApiResponse esperado | Campos de rol/EmpresaId extra producen mass assignment |
| P09/N09 | Excepción saneada da error estable | 500 revela JWT, SQL, Password o stack |
| P10/N10 | CI dirigida exact SHA y A5 independiente | CANCELLED/SKIPPED/IN_PROGRESS o autor propia A5 cuenta PASS |

Todos los casos son **FUTUROS_NO_EJECUTADOS**. Esta entrega no ejecuta HTTP, xUnit, MySQL, Angular, Playwright, pruebas A/B ni Docker. Implementaciones M03 requerirán código real y pruebas GitHub-hosted causales; no se simula compatibilidad.

## 6. Ocho medidas de reversión RB01–RB08

| ID | Medida |
| --- | --- |
| RB01 | Revertir cambios de Markdown mediante git revert normal, sin force-push o rewrite |
| RB02 | Antes de revertir verificar HEAD/owner y preservar trabajo concurrente ajeno |
| RB03 | Mantener BITACORA histórica; agregar eventos correctivos únicos si procede |
| RB04 | Congelar sólo endpoint/feature afectado, no otros scopes seguros |
| RB05 | Futuras API/DTO se revierten con tests de status/response y consumidores |
| RB06 | Futuras migraciones EF/MySQL exigen backup, transacción/reversibilidad y datos sintéticos |
| RB07 | Si falla seguridad tenant/JWT/RBAC, denegar por defecto y conservar evidence de la CI |
| RB08 | Nunca usar revisión documental para alterar producción, QA, main, secretos o proveedores |

## 7. A0–A5, evidencia y salida

- **A0:** Skill/HEAD exactos, owner exclusivo, PLAN/COLA/DAG vigentes; diff restringido a este documento + sección autocontenida de CHANGELOG_AI.md.
- **A1:** inspección de los diez OP, dieciséis V, diez P/N futuros y ocho RB, con estado futuro explícito; no atribuir resultados de tests.
- **A2:** verificar que el contrato niega cross-tenant A/B, revocación, cambios de rol, acceso global→empresa, PII, secretos, SQL expuesto y mass assignment. No certificar despliegue.
- **A3:** contrastar los blobs citados en API/Application/Middleware y documentos M01; no tocar esos archivos. dotnet, MySQL, Playwright y browser son NO_APLICA **al diff documental**, no PASS.
- **A4:** commit fast-forward/CAS en dev, fuente SHA/blob y diff dos Markdown, comprobar Scope Lock + procedencia V5 exact-SHA completed/success, BITACORA A:Z y PLAN/COLA/DAG readback.
- **A5:** pendiente de Task Javier diferente de DEV01 sobre SHA/diff, código referenciado y pruebas pertinentes; no autoaprobar. El cierre A0–A4 de documentación no certifica seguridad de toda la API ni habilita automáticamente una M03.

El cambio no ejecuta migraciones, crea proveedores, llama entre Tasks, cambia tenant/JWT/RBAC, despliega a QA/main/PROD ni modifica datos reales. La siguiente microtarea se selecciona sólo tras reevaluar DAG, PLAN, COLA y exclusividad vigente.
