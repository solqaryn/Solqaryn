# SOLQARYN V5 FINAL — C01-04.M01.03: inventario de contratos, código y evidencia del expediente universal

**Resultado atómico:** leer contratos, código y evidencia disponibles sin asumir certificación. **Tipo:** preparación documental preliminar M01.03 de C01-04, no implementación. **Autor:** PRIMARY DEV 02 — JAVIER; TASK_ID `6af16bb9492c8191b8e8c7d606f11507`; repositorio exclusivo `solqaryn/Solqaryn@dev` y Scope Lock STRICT.

**Autoridad:** Standard de Implementación V5 FINAL, PLAN_MAESTRO/COLA/DAG_TECNICO; M01.01 objetivo y no-regresión, M01.02 actores/I/O. **Resultado de fase futuro:** unificar cronología, decisiones, observaciones, adjuntos y versiones con contrato de timeline y consulta aislada por ámbito. Este inventario no acredita una API ni base de datos universal de expedientes.

## 1. Fuentes de código efectivamente inspeccionadas

| Archivo de dev | Git blob SHA observado | Evidencia concreta y límite |
| --- | --- | --- |
| `backend/src/API/Controllers/AuditoriaController.cs` | `82057f2acc571263ed7ed530eae0f81359b2f81a` | GET `/auditoria`, autenticado con permiso de ver; rechaza si `EsAdministrador` es falso. No expone timeline de expediente |
| `backend/src/Application/DTOs/AuditoriaDto.cs` | `2bc2bc437441d0677a4f86deea7aad743d26fbf6` | DTO con usuario, módulo, acción, fecha, filtros y paginación, no contrato de versiones/documentos universales |
| `backend/src/Application/Services/AuditoriaService.cs` | `def2c1f7bc58e23f98a1065fcdf0963322f3705c` | Normaliza la correlación desde HttpContext, persiste registros y proyecta DTOs; emplea marcadores server-side de empresa |
| `backend/src/Infrastructure/Repositories/AuditoriaRepository.cs` | `8fc9417b88da91ad1a5777ea77d9e31756230384` | Query de registros, filtros, conteo y `OrderByDescending(Fecha)`; **el método visible no aplica un predicado tenant explícito**: revisar autorización y filtros efectivos antes de reutilizarlo para expedientes |
| `backend/src/Domain/Entities/RegistroAuditoria.cs` | `d669ac397a78ba02a5ba19e2cfb4bc9c7a115462` | Registro con EmpresaId nullable, actor, CorrelationId, valores antes/después, motivo y resultado. No garantiza ownership de expediente |
| `backend/src/Domain/Entities/CompraDocumento.cs` | `7472b6418b5da34c58b5f7572f3fd6a858abac86` | Documento de compra con EmpresaId nullable legacy, URL, PublicId, tipo, tamaño, referencias y borrado lógico. No es adjunto universal |
| `backend/src/Application/Interfaces/ICompraDocumentoStorageService.cs` | `454d588851ecc7db810259e63705852360172928` | Contratos legacy y overloads tenant-aware con fallback fail-closed. No asumir autorización por publicId |
| `backend/src/Domain/Security/ContextoTenantActual.cs` | `7377d2d752985cc9baf31d08280f38ad94a6a114` | Contexto validado desde UsuarioEmpresa y rol efectivo; `ExigirEmpresa` falla cerrado para ámbito cruzado |
| `frontend/src/app/services/auditoria.service.ts` | `e7d51d65833bcfb7dbeac24a42751c9ed229f34e` | Cliente HttpClient para GET auditoría con filtros; no endpoint de expediente ni guardias de aislamiento certificadas |
| `frontend/src/app/core/models/auditoria.model.ts` | `bb125ff74cef158eeedf2cf8241cb5a682637cc1` | Modelo visual del registro auditado; no prueba render de timeline ni protección de versiones |

Se inspeccionaron los contenidos fuente y los diez SHA de blobs en el árbol de `dev`. Una lectura estática **no reemplaza** pruebas HTTP, JWT, RBAC, DB/EF/MySQL, Angular/E2E ni verificación de storage. Los SHA se vuelven a verificar al implementar.

## 2. Contratos disponibles y brechas concretas

| Necesidad futura | Contrato existente reutilizable parcialmente | Brecha que debe resolverse en M02+ |
| --- | --- | --- |
| Cronología/auditoría | RegistroAuditoria + AuditoriaService/Repository y DTO paginado | Evento de expediente por recurso/tenant, orden estable con desempate ID, aislamiento de consulta y temporalidad evento vs escritura |
| Decisiones/observaciones | Descripción, motivo, actor y valores antes/después auditados | Entidad/contrato tipado de decisiones y observaciones, estados, aprobación y versionado; no confundir texto libre con decisión aprobada |
| Adjuntos | CompraDocumento y storage de compras | Ownership de recurso/empresa, URL de lectura privada con reautorización, MIME/tamaño, revocación y versionado |
| Versiones y concurrencia | Registro de valores antiguos/nuevos y correlación | Referencias de versión, cambios idempotentes y `If-Match`/equivalente; rollback auditable y sin overwrite |
| Seguridad multiempresa | ContextoTenantActual, guardas del controller, VerifiedCommercialTenant | Verificar repositorio y consulta con filtro tenant A/B y membresía revocada: el método de auditoría leído no agrega tal filtro por sí mismo |
| UI / navegación | AuditoriaService y modelo frontend | Timeline accessible/responsive, descarte de respuestas stale A→B, paginación y no exposición de PII |

**Hallazgo documental que NO constituye certificación de vulnerabilidad explotable:** el repositorio de auditoría no muestra un predicado `EmpresaId` en `GetFilteredAsync`. La ruta visible exige administrador y permiso, por lo que no se puede concluir un bypass sin examinar políticas adicionales, contextos y pruebas reales. Para reutilización en expediente debe exigirse prueba negativa de empresa A consultando B y evaluación del filtrado global de AppDbContext. No se modifica ni publica dato sensible.

## 3. Pruebas futuras reproducibles (diseño, NO ejecución)

**Positivos especificados:** P01 usuario miembro A recibe sólo timeline A en orden estable; P02 recurso propio vincula evento+adjunto privado autorizado; P03 reintento idempotente retorna mismo evento y número de versión; P04 paginación no duplica ni omite con empates de fecha.

**Negativos especificados:** N01 actor A falsifica EmpresaId/recurso B y no obtiene eventos/adjuntos; N02 membresía revocada o rol insuficiente falla cerrado; N03 publicId/URL de archivo de B no permite descarga; N04 petición `If-Match` obsoleta no reescribe historial; N05 respuesta asíncrona A tras cambio de empresa a B se descarta; N06 JS/HTML o contenido desmesurado no se ejecuta/persiste sin validar; N07 un filtro sin `EmpresaId` efectivo impide certificación de aislamiento; N08 CI de SHA antiguo o job SKIPPED no equivale a PASS.

Cada escenario se acreditará en la fase ejecutable con backend .NET, tests seguridad/tenancy, MySQL 8.4 y/o Playwright, cuando realmente cambie ese consumidor. Estos son criterios de aceptación, no resultados ejecutados.

## 4. Riesgos, auditorías y trazabilidad V5

**Dependencias:** `DAG_TECNICO` para C01-04.M01.03 F=VALIDADO/G=SI. Trece prerrequisitos de M01: C00-01.M02.01–06, C00-03.M01.01–05 y C01-01.M01.01–02, todos LISTO al preflight; PLAN_MAESTRO PENDIENTE y COLA G/O SI sin responsable antes del claim. Sólo documentación M01.01–05 independiente, **no** M01.06, M02 o certificación de C00.

**Clasificación:** `requiere_dotnet=NO`, `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO`; `requiere_security=SI` documental por JWT/RBAC/tenancy/PII; `requiere_scope_lock=SI` por commit de docs en dev.

**A0** integridad de diff, Markdown, 10 referencias y ausencia de secretos; **A1** cobertura de contratos existentes/brechas y 4 positivos + 8 negativos; **A2** análisis de límites de tenant, pertenencia, acceso a archivos, PII e idempotencia sin afirmar pruebas live; **A3** cotejo con código, DAG V5 y arquitectura, sin recompilar runtime ajeno. **A4**, sólo poscommit: diff exacto, SHA, Scope Lock completed/success por `head_sha`, readback PLAN/COLA/BITACORA A:Z 26; **A5_PENDIENTE** para TASK_ID distinto antes de QA/main/PROD.

**Impacto:** sólo Markdown V5, CHANGELOG_AI.md y registros Sheets, sin alterar API/DTO, Domain, EF/MySQL, frontend, dependencias, secretos, deployments ni archivos. Imágenes históricas no existentes: migración NO_APLICA. **Rollback:** `git revert` normal del commit documental tras verificar HEAD y trabajo concurrente; nunca force-push, reset ni borrar eventos. Revalidar todas las fuentes al implementar.
