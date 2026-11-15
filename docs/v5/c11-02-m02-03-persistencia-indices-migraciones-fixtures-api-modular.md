# SOLQARYN V5 — C11-02.M02.03: persistencia, índices, migraciones y fixtures de API modular

**ID:** C11-02.M02.03, etapa M02 de contratos y modelo del bloque C11-02 (API modular). **Autor:** PRIMARY DEV 01 / JAVIER_CHATGPT, TASK_ID=6adbb9fff1548191a1bd5b30d9e9984a. **Repositorio:** solqaryn/Solqaryn, dev; PROJECT_SCOPE_LOCK=STRICT. **Fuente institucional:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA. Fecha: 2026-11-15.

## 1. Alcance y fuentes verificadas

Esta microtarea documenta cómo una futura evolución de puertos/repositorios/DTOs de API modular **deberá proteger los datos**. No crea ni ejecuta migración EF, SQL DDL, tablas, índices, fixtures reales, jobs, endpoints, API runtime ni writes en MySQL. No modifica autorización JWT/RBAC/tenancy, secretos, infraestructura, Task, QA/main/PROD. La declaración de un índice candidato no prueba que exista ni es permiso para crearlo. La documentación es revisable antes de una implementación M03/M05 sometida a pruebas reales.

Preflight por ID: C11-02.M01.01–06 LISTO con A5 independiente, incluyendo M01.06 (autor DEV04, revisor DEV06); PLAN H1630=PENDIENTE/I vacío, COLA G/O=SI y DAG F=VALIDADO/G=SI antes del claim, que sólo permite la preparación M02.03. No deducir habilitación de M02.06 o runtime de esta propuesta.

Fuentes Git realmente leídas en dev:

| Fuente | Blob Git | Evidencia y limitación |
| --- | --- | --- |
| backend/src/Infrastructure/Persistence/AppDbContext.cs | c4b32f6637f1b4426655bb0cd12af8dd9d229866 | DbContext EF con mecanismo de tenant; leer un filtro no certifica todos los SQL |
| backend/src/Application/Interfaces/IUsuarioRepository.cs | b10ad0e57dacce3157215f79c8f4b4f98292d72c | Puertos GetById, GetPaged, membresías, SaveChanges; contratos actuales, no nuevas operaciones |
| backend/src/Application/Common/PagedRequest.cs | 886b4d068ea862cdf577c46c02bf30099fb737bb | Page >=1, PageSize restringido 1..200, SortBy/SortDirection, UsuarioIdScope interno ignorado en JSON |
| backend/src/Domain/Entities/UsuarioEmpresa.cs | f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c | UsuarioId, EmpresaId, RolId y membresía activa; claim legacy no otorga empresa |
| backend/src/Application/Interfaces/IUsuarioScopeService.cs | 5345ee2de1b1e942bff8eded658f5dc27c0a6745 | Contexto tenant verificado por membresía, separado del legacy |
| backend/src/API/Middleware/SupportReadOnlyCommandInterceptor.cs | 3b80b59899522b9b41681ed75b8269f8720fe297 | Barrera adicional a escrituras de soporte; necesita comprobación runtime específica |
| backend/src/API/Controllers/UsuariosController.cs | 53d1eabb7a5e04b5e35be114c3021bf343d9bcc8 | Ruta de usuarios, lecturas paginadas y permiso backend |
| docs/v5/c11-02-m01-03-inventario-contratos-codigo-api-modular.md | 72d3fec054b19398f17a7c9b2cb7139a86fc9033 | Inventario parcial de 16 archivos, no auditoría completa |
| docs/v5/c11-02-m01-06-gates-rollback-salida-api-modular.md | a096c5b0cdc4a520664ca41d2dfc5c53d48a4cb9 | Gate de salida M01 con 16 bloqueos, rollback y pruebas futuras |

## 2. Doce proyecciones candidatas PR01–PR12 (sin tablas nuevas)

| ID | Proyección/caso | Invariante que deberá conservar |
| --- | --- | --- |
| PR01 | Usuario de autenticación | Identificador estable; no devolver hashes ni credenciales en DTO |
| PR02 | UsuarioEmpresa activa | (UsuarioId,EmpresaId) vinculada al rol empresarial vigente |
| PR03 | Contexto del tenant solicitado | Validación backend de membresía activa, no header como grant |
| PR04 | Sesión de plataforma global | Autoridad relacional de plataforma separada de EmpresaId |
| PR05 | Roles/permisos por empresa | No mezclar rol legacy, global y tenant real |
| PR06 | Usuario paginado | UsuarioIdScope derivado de servidor, no de JSON |
| PR07 | Cuenta de soporte | Lectura permitida sin mutaciones SQL/EF ni elevación de scope |
| PR08 | RegistroAuditoria | Metadata saneada con actor, resultado y UTC; sin tokens/PII |
| PR09 | Configuración de feature/plan | Lectura no permite habilitar permisos por override client-side |
| PR10 | Respuesta de error | Código estable, sin filtrar claves, objetos B ni SQL |
| PR11 | Referencia de agregado | Buscar recurso por ID sólo después de comprobar ownership |
| PR12 | Resultado de transacción | No afirmar éxito si SaveChanges/commit falla o quedó parcial |

Son **proyecciones de diseño**, no nombres de tablas existentes ni modelo de migración autorizado. Para un futuro repositorio, reutilizar interfaces existentes y separar las responsabilidades Domain/Application/Infrastructure.

## 3. Ocho índices/constraints candidatos IX01–IX08 — NO CREADOS

| ID | Candidato futuro | Verificación obligatoria antes de cualquier DDL |
| --- | --- | --- |
| IX01 | UsuarioEmpresa (UsuarioId, EmpresaId) | Consultar DDL real y unicidad efectiva; no duplicar índice existente |
| IX02 | UsuarioEmpresa (EmpresaId, Activa) | Plan de consulta, selectividad, cardinalidad y costo MySQL 8.4 |
| IX03 | Usuario por nombre normalizado | Semántica Unicode/collation y unicidad sin alterar login existente |
| IX04 | Rol de membresía tenant | Foreign key y protección de rol de otra empresa |
| IX05 | Auditoría (EmpresaId, UTC) | Comprobar tenant y retención de datos, evitar indexar PII |
| IX06 | Paginación de usuarios con orden permitido | EXPLAIN, tie-break determinista y límite PageSize 1..200 |
| IX07 | Relaciones de soporte read-only | Evitar DML y escaneo global, comprobar predicado tenant |
| IX08 | Eventos/operaciones idempotentes existentes | No añadir índice a Outbox sin unidad M03/MySQL autorizada |

No se conoce el EXPLAIN/índice físico real de estos candidatos en esta microtarea. Cada sugerencia debe compararse con índices, consultas, cardinalidad y locks reales en CI efímera; ningún índice se declara implementado.

## 4. Seis etapas de migración posible MG01–MG06, TODAS PENDIENTES

| ID | Paso requerido en una futura autorización técnica |
| --- | --- |
| MG01 | Backup/restore y muestras sintéticas con baseline de esquema y checksum |
| MG02 | Comparación EF model↔schema MySQL y detección de índice/constraint ya existente |
| MG03 | Ensayo reversible de migración en servicio MySQL 8.4 efímero de GitHub Actions |
| MG04 | Revalidación JWT/RBAC/membresía activa, soporte read-only y tenant A/B |
| MG05 | Medir locks, paginación, queries y rollback bajo fallo parcial, no datos productivos |
| MG06 | CI exact SHA, evidencia, A5 distinta, readback PLAN/COLA/DAG y autorización de entorno separada |

Ningún paso de esta tabla fue ejecutado. No crear ni aplicar una migración a partir de un documento M02.

## 5. Diez fixtures/casos positivos-negativos F01–F10 FUTUROS_NO_EJECUTADOS

| ID | Positivo futuro | Negativo futuro |
| --- | --- | --- |
| F01 | Usuario A con membresía A activa puede consultar A | Usuario A consulta empresa B |
| F02 | Usuario con dos membresías usa la elegida validada | Header EmpresaId falso concede B |
| F03 | Rol con lectura accede al conjunto permitido | Rol revocado aún lista datos |
| F04 | PagedRequest limita 1..200 y responde estable | PageSize 9999 provoca extracción ilimitada |
| F05 | SortBy allowlisted da orden determinista | SortBy libre permite SQL arbitrario |
| F06 | AppDbContext filtra datos tenant-bound | Consulta indirecta omite predicado tenant |
| F07 | Soporte realiza SELECT en datos autorizados | Soporte llama UPDATE/DELETE/SaveChanges |
| F08 | Auditoría registra UTC y códigos saneados | Log almacena JWT, password o PII |
| F09 | Transacción válida confirma una vez | Fallo parcial deja estado inconsistente |
| F10 | Rollback de migración de CI conserva filas | Migración fallida pierde tenant B |

**No se ejecutaron estas pruebas ni las de regresión .NET, MySQL, Playwright o HTTP.** Son condiciones a incorporar a tests reales en fases técnicas y de verificación. Los fixtures no se materializaron ni se inyectó dato alguno.

## 6. Ocho invariantes de seguridad/rollback SG01–SG08

| ID | Contrato fail-closed |
| --- | --- |
| SG01 | EmpresaId permitido se resuelve en backend y desde membresía vigente |
| SG02 | El sujeto global/plataforma no hereda autoridad empresarial |
| SG03 | Índice o FK nuevo no relaja aislamiento de tenant ni roles |
| SG04 | Paginación y filtros no usan texto SQL libre del cliente |
| SG05 | Errores de EF/MySQL no se devuelven sin saneamiento |
| SG06 | No migrar datos reales ni desplegar sin backup probado y orden explícita |
| SG07 | Revertir este documento con git revert normal y BITACORA aditiva |
| SG08 | Check required distinto de completed/success o A5 del autor es NO_PASS |

## 7. Validación A0–A5 y salida

**A0:** comprobar Task/Skill/HEAD, PLAN H/I, COLA G/O, DAG F/G y fuente M01; restringir diff al Markdown y sección autocontenida de CHANGELOG_AI.md. **A1:** cotejar 12 proyecciones, 8 índices candidatos, 6 etapas de migración FUTURAS y 10 fixtures positivos/negativos no ejecutados; no fabricar pruebas. **A2:** revisar tenant A/B, membresía, JWT/RBAC, paginación, read-only de soporte, privacidad y transacción/rollback de manera documental. **A3:** contrastar Domain/Application/Infrastructure/API reales y referencias de blob sin alterarlos; MySQL/EF/HTTP/Playwright no aplican al changeset Markdown y NO cuentan como PASS. **A4:** commit fast-forward/CAS en dev y readback por SHA, Scope Lock y V5 completed/success exacto, PLAN/COLA/BITACORA A:Z comprobados. **A5:** Task Javier distinta de DEV01; esta entrega conserva A5_PENDIENTE hasta revisión independiente.

**No se activan migraciones, índices, usuarios, roles, datos, secretos, infraestructura, QA, main ni PROD.** Reevaluar cualquier sucesora por sus predecesores técnicos y su propio estado; esta especificación no es una aprobación de código M03.
