# SOLQARYN V5 FINAL — C01-04.M01.02: actores, entradas, salidas y superficies del expediente universal

**Clase:** TIPO 1, preparación documental de M01.02. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL y Sheet institucional (PLAN_MAESTRO/COLA/DAG_TECNICO). **Rama:** solqaryn/Solqaryn@dev, Scope Lock STRICT. **Autor:** PRIMARY DEV 02 — JAVIER, TASK_ID 6af16bb9492c8191b8e8c7d606f11507.

El objetivo futuro de C01-04 es unir **cronología, decisiones, observaciones, adjuntos y versiones** en un expediente consultable bajo autorización real. Esta entrega **identifica actores y flujos propuestos**, sin declarar implementada una API, tabla, UI o historial universal. Complementa M01.01 (objetivo/no-regresión), sin completar M01.03–05 ni liberar M01.06/M02.

## 1. Actores y frontera de confianza

| Actor | Entrada o intención futura | Salida condicionada a permisos | Denegación / límite |
| --- | --- | --- | --- |
| Visitante sin sesión | Ruta de documento o expediente público explícitamente permitido | Sólo metadatos publicados por política expresa | URL/slug no concede acceso a expediente privado |
| Miembro autenticado de empresa A | Solicita timeline de recurso propio | Eventos, decisiones y versiones A según rol efectivo y ownership | EmpresaId, header, cookie o recurso B no conceden autorización |
| Responsable de expediente | Agrega observación o solicita decisión | Entrada con actor, UTC, motivo, correlación y versión | Observación no equivale a aprobación formal |
| Administrador empresarial | Consulta historial autorizado de su empresa | Vista limitada a rol, capacidad y datos propios | No recibe privilegio global ni datos de B |
| Operador de plataforma autorizado | Acción excepcional de política explícita | Intervención mínima y auditada | Rol de empresa no se eleva por UI |
| Servicio o worker confiable | Evento de negocio o reintento | Entrada idempotente y correlacionada con fuente | Payload falso o duplicado no crea hecho adicional |
| Revisor independiente | SHA, A0–A4 y casos pertinentes | Dictamen A5 de TASK_ID diferente | Ni autoaprobación ni promoción implícita QA/main/PROD |

La identidad comercial, la cuenta GitHub, el TASK_ID y la cuenta ChatGPT son principales diferentes; ninguno sustituye a otro.

## 2. Entradas, salidas y consumidores candidatos

| Contrato provisional | Entrada | Salida o consumidor futuro | Negativo exigido |
| --- | --- | --- | --- |
| EXP-IO-01 | JWT validado y petición de recurso | Contexto usuario/empresa desde membresía real | JWT A + referencia B: denegación sin fuga |
| EXP-IO-02 | Tipo e identificador de recurso | Expediente y ámbito tenant/sucursal verificados | ID huérfano o de otro tenant: no revela metadatos |
| EXP-IO-03 | Cursor, rango temporal, filtros allowlisted | Timeline ordenado con desempate estable y paginación | Cursor o filtro cross-tenant no expone datos |
| EXP-IO-04 | Observación o decisión y actor autorizado | Entrada nueva con actor, tiempo, motivo y versión | HTML/script, rol insuficiente o PII excesiva: no persistir |
| EXP-IO-05 | Versión esperada / clave idempotente | Confirmación única o conflicto explícito | Reintento no duplica ni sobrescribe historial |
| EXP-IO-06 | Adjunto, content type, tamaño y ownership | Metadatos autorizados y acceso privado condicionado | URL/publicId de B no sirve bytes ni metadatos |
| EXP-IO-07 | Evento de módulo legítimo | Registro correlacionado a estado fuente | Falla parcial no fabrica decisión o borra fuente |
| EXP-IO-08 | Cambio de empresa o logout durante consulta UI | UI descarta respuesta de contexto anterior | Respuesta tardía de A no aparece en B |
| EXP-IO-09 | Petición de versiones de expediente | Referencias anterior/siguiente y motivo autorizado | Reversión no reescribe historia |
| EXP-IO-10 | Error de entrada/autorización/storage | Error seguro y correlación sin secretos | No exponer token, stacktrace, URL privada ni PII |

Son **contratos propuestos para M02+**, no endpoints actuales, DTO aprobados ni prueba de persistencia.

## 3. Superficies reales inspeccionadas en dev

| Referencia existente | Archivo y blob observado | Alcance probado por lectura, NO por ejecución |
| --- | --- | --- |
| HTTP de auditoría | backend/src/API/Controllers/AuditoriaController.cs — 82057f2acc571263ed7ed530eae0f81359b2f81a | GET /auditoria exige autenticación, permiso Ver y administrador, pero no es API universal de expediente |
| DTO de auditoría | backend/src/Application/DTOs/AuditoriaDto.cs — 2bc2bc437441d0677a4f86deea7aad743d26fbf6 | Filtros de auditoría, fecha, usuario y paginación; no define timeline genérico |
| Modelo auditado | backend/src/Domain/Entities/RegistroAuditoria.cs — d669ac397a78ba02a5ba19e2cfb4bc9c7a115462 | EmpresaId nullable, actor, correlación, valores antes/después; no demuestra aislamiento de todas las consultas |
| Adjunto de compras | backend/src/Domain/Entities/CompraDocumento.cs — 7472b6418b5da34c58b5f7572f3fd6a858abac86 | EmpresaId nullable legacy, tipo, tamaño, URL, borrado lógico; no es adjunto universal |
| Interfaz storage | backend/src/Application/Interfaces/ICompraDocumentoStorageService.cs — 454d588851ecc7db810259e63705852360172928 | Overloads tenant-aware default fallan cerrados; API legacy no se puede asumir segura para expedientes |
| Contexto tenant | backend/src/Domain/Security/ContextoTenantActual.cs — 7377d2d752985cc9baf31d08280f38ad94a6a114 | Exige UsuarioEmpresa, rol efectivo y EmpresaId coincidente, no claims arbitrarios |

Las seis referencias son código real leído en dev y son **parciales**: no prueban API/DB/Cloudinary/E2E universal. Toda implementación futura relee versiones y consumidores.

## 4. Comprobaciones documentales y pruebas posteriores

**Positivos de M01.02:** P1, siete actores con autoridad/entrada/salida/denegación; P2, diez IO con límites de tenant y versionado; P3, seis archivos observados sin sobreafirmar capacidades.

**Negativos de M01.02:** N1, EmpresaId/URL cliente nunca concede B; N2, observación no es decisión aprobada; N3, adjunto legacy o ajeno no se entrega sin validación; N4, auditoría existente no implica expediente universal; N5, respuesta vieja de A no aparece en B; N6, check SKIPPED, SHA anterior o job no ejecutado no es PASS exigible.

**Pruebas futuras NO ejecutadas aquí:** API .NET positivo A/negativo B; persistencia y orden/paginación MySQL si se implementan; rol revocado, adjuntos y versionado/idempotencia; Angular accesible y E2E al cambiar de tenant.

## 5. Dependencias, auditorías, impacto y rollback

DAG_TECNICO C01-04.M01.02 F=VALIDADO/G=SI. En preflight: 13 prerequisitos LISTO — C00-01.M02.01–06, C00-03.M01.01–05 y C01-01.M01.01–02; PLAN_MAESTRO PENDIENTE y COLA G/O=SI, sin dueño antes de claim. Sólo autoriza preparación documental M01.01–05, nunca M01.06/M02 ni C00 final.

**Clasificación:** requiere_dotnet=NO, requiere_node_angular=NO, requiere_mysql=NO, requiere_docker=NO, requiere_e2e=NO (sin cambio ejecutable); requiere_security=SI (JWT/RBAC, tenant A/B, PII, adjuntos); requiere_scope_lock=SI (commit documental dev).

**A0:** diff, Markdown, duplicidad/secretos, integridad. **A1:** matriz de actores e IO, P1–P3/N1–N6; sin fingir runtime. **A2:** límites JWT/RBAC/membresía, tenant, adjuntos y PII. **A3:** V5/DAG/fuentes/Skill y contrato documental; .NET/Angular/MySQL/Docker/E2E NO_APLICA a este diff. **A4:** commit fast-forward dev, SHA exacto, blobs, Scope Lock completed/success y readback PLAN/COLA/BITACORA A:Z (26). **A5:** independiente, sin autopromoción.

**Impacto:** nuevo Markdown V5, CHANGELOG_AI.md y Sheets; ningún cambio a Domain/Application/API/Infraestructura, Angular, DB, migraciones, permisos, secretos, Cloudinary o datos de producción. Consumidores: futuras M01.03–05/M02, equipo y revisores V5. Imágenes históricas inexistentes: migración NO_APLICA; archivos futuros privados requieren storage tenant-aware.

**Rollback:** git revert normal del commit documental con HEAD revalidado; no force-push, reset, borrado de evidencia ni modificación qa/main/PROD. Revalidar C00 final e identidad C01-01 antes de liberar M01.06/M02.
