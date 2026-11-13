# SOLQARYN V5 FINAL — C02-03.M01.01: objetivo, no-alcance y no-regresión de aprovisionamiento empresarial

**Tipo:** preparación exclusivamente documental, sin ejecutar aprovisionamiento ni alterar datos. **Microtarea:** C02-03.M01.01; **autor:** PRIMARY DEV 02 — JAVIER, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Repositorio/entorno:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT. Fuentes institucionales: Standard de Implementación V5 FINAL y PLAN_MAESTRO/COLA/DAG_TECNICO.

**Resultado futuro de C02-03:** alta, suspensión, reactivación, archivado y baja lógica de empresa, de manera idempotente; evidencia futura mínima: pruebas de repetición, fallo parcial y recuperación. Esta M01.01 **define límites y criterios**, no implementa un flujo de ciclo completo ni invoca endpoints reales, storage, migraciones o servicios de terceros.

## 1. Objetivo y estados conceptuales futuros

| Concepto | Resultado objetivo observable para implementación posterior | Límite y no regresión |
| --- | --- | --- |
| Borrador/alta | Registrar empresa con identidad única, creador autenticado y contexto de plataforma; no habilitar actividad comercial sin gates | `CrearBorrador` observado es un punto de partida específico; no equivale a certificación de alta universal |
| Preparación comercial | Verificar contratos, datos/tenancy, sucursales, membresía, host, idempotencia y aislamiento | Nunca habilitar cliente/ERP/tienda de B antes de pruebas y autorización |
| Activación | Cambiar a estado operativo autorizado con auditoría y rollback definidos | No tomar `Activa=true` de la UI como evidencia de onboarding comercial certificado |
| Suspensión | Bloquear nuevas operaciones según política server-side, sin destruir datos ni perder referencias | La suspensión no es baja lógica ni elimina de manera irreversible ventas/documentos |
| Reactivación | Revalidar credenciales/membresías, estado, cuota, seguridad y binding antes de abrir tráfico | No restaurar permisos/roles revocados de forma silenciosa |
| Archivo / baja lógica | Estado futuro distinguible de inactividad, con retención, auditoría, posibilidad de restauración y legal hold | `Empresa.Eliminado` actual devuelve `false` NotMapped; **no existe soft-delete de Empresa acreditado por ese modelo** |
| Reintentos y fallos parciales | Operación idempotente por clave/versión; efecto único o compensación trazable | Reintentar no duplica empresas, datos, hosts, auditorías materiales ni permisos |
| Integración de vida útil | Vincular empresa, UsuarioEmpresa, tienda, configuración/ERP y providers sólo tras compatibilidad | No inferir que un controller existente habilita por sí solo todo el flujo global |

**Contrato provisional:** un futuro estado de ciclo debe especificar actor, tenant, etapa, transición permitida, condición previa, idempotency key, correlation ID, versión y evento/auditoría, sin mezclar roles globales de plataforma con los empresariales. Esta tabla es diseño, NO persistencia implementada.

## 2. Evidencia real de código inspeccionada

| Archivo | Git blob SHA de dev | Contrato observado y límite |
| --- | --- | --- |
| `backend/src/Domain/Entities/Empresa.cs` | `c44940df7c30983a4c23b93338e0cb9367962069` | `Activa`, `Activar`, `Desactivar`; `Eliminado => false` NotMapped: no archiva ni baja lógicamente empresa |
| `backend/src/API/Controllers/EmpresasController.cs` | `cfedc20236e71db079529442ebf45b0431d339e2` | `GET/POST/PUT` y PATCH activar/desactivar, autorizaciones y `ExigirEmpresaAutorizada`; no es gestión global de lifecycle |
| `backend/src/API/Controllers/PlataformaEmpresasController.cs` | `87fbf187b7993ba0f05edff393500f24d0856833` | `GET /plataforma/empresas`, `POST /plataforma/empresas/borrador` con actor plataforma, validación, binding legacy; no certificar A/B comercial sin gates |
| `backend/src/Application/Services/EmpresaService.cs` | `c7a6e4a88f8a2b310ce5cf2bfb26596ca1d90f67` | Servicio exige barrera comercial legacy para `CreateAsync`, permite `CambiarEstadoAsync` y registra auditoría; no demuestra idempotencia por petición ni ciclo completo |
| `backend/src/Application/DTOs/EmpresaDto.cs` | `bbca706d08d5a9738798af14ca3bb99185b18a40` | DTO nombre y activa, sin estado de archivado, versión ni token de reintento |
| `backend/src/Domain/Entities/UsuarioEmpresa.cs` | `f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c` | Membresía empresarial con `Activa`, rol efectivo y comprobación de identidad, sin reactivación global de permisos implícita |

Los seis blobs son **evidencia estática del árbol dev**, no una prueba de backend, base, UI, tenant A/B ni servicios live. Releer al implementar.

## 3. No-regresión falsable: casos positivos y negativos futuros

| ID | Estímulo futuro | Resultado exigido / prueba pertinente |
| --- | --- | --- |
| NR-01 (+) | Actor plataforma autenticado solicita borrador con nombre nuevo | Identidad creada una vez, sin acceso comercial prematuro; API + MySQL temporal |
| NR-02 (+) | Repite la misma alta con idempotency key válida | Respuesta coherente, cero duplicación de Empresa/bindings/eventos; test repetición |
| NR-03 (+) | Empresa activa autorizada es suspendida por transición válida | Operaciones comerciales nuevas se deniegan; historial conservado, auditoría causal |
| NR-04 (+) | Empresa suspendida recupera condición de reactivación certificada | Reactivación sin restaurar membresías revocadas ni datos de B, con versionado |
| NR-05 (+) | Recurso de empresa A se archiva con retención aprobada | Registro lógico y referencias legales preservados; readback DB y política de retención |
| NR-06 (-) | Administrador de empresa intenta acción global de plataforma | 403/denegación; nunca atribuir rol plataforma por ser admin empresarial |
| NR-07 (-) | Cliente modifica EmpresaId/host/tenant/DSN en solicitud de alta B | Servidor rechaza o ignora dato no autorizado; no toca A ni proveedor |
| NR-08 (-) | Se activa B sin aislamiento comercial, binding o gate exigible | Fail-closed, sin habilitar tienda ni operaciones hasta certificar |
| NR-09 (-) | Fallo a mitad de alta escribe empresa, pero no binding/membresía | Rollback o estado recuperable sin mostrar empresa falsamente activa |
| NR-10 (-) | Dos altas concurrentes con misma identidad | Una única empresa o conflicto seguro; sin duplicar registros |
| NR-11 (-) | Suspensión/archivado intenta borrar ventas, auditorías o adjuntos fuente | Rechazo y conservación de historia; no borrado físico por defecto |
| NR-12 (-) | Reintento de suspensión/reactivación ya aplicada | Idempotencia y auditoría sin efectos duplicados |
| NR-13 (-) | Miembro A solicita archivar/reactivar B | Denegación server-side y cero datos B expuestos |
| NR-14 (-) | Token revocado/rol perdido intenta operación durante transición | Revalidar membresía y rol efectivo al punto de control, sin permisos stale |
| NR-15 (-) | Migración, MySQL o Docker requerida queda SKIPPED/CANCELLED | No declarar A3/A4 de implementación aprobada por falso PASS |
| NR-16 (-) | Revisor A5 tiene mismo TASK_ID que autor o SHA incorrecto | NO_GO/A5_FAIL; no publicar automáticamente qa/main/PROD |

Estos 16 escenarios son **condiciones para la fase ejecutable**, no tests realizados por esta documentación. Se requiere prueba real A/B, fallo parcial y recuperación cuando se programe el ciclo de vida.

## 4. DAG, clasificación y seguridad

El DAG para C02-03.M01.01 está VALIDADO/SI con **19 prerrequisitos LISTO**: C00-01.M02.01–06, C00-03.M01.01–05, C01-01.M01.01–05 y C02-02.M01.01–03. Al preflight, PLAN_MAESTRO H=PENDIENTE sin responsable y COLA G/O=SI. Este gate sólo habilita la preparación documental M01.01–05; M01.06/M02 permanece independiente y exige revisar C00/C01, seguridad y contratos.

**Clasificación TIPO1:** requiere_dotnet=NO, requiere_node_angular=NO, requiere_mysql=NO, requiere_docker=NO, requiere_e2e=NO (no cambia runtime); requiere_security=SI_DOCUMENTAL por identidad, roles, tenancy A/B, PII y transición reversible; requiere_scope_lock=SI por commit de docs en dev. Las futuras pruebas de código aplicarán .NET 10, EF/MySQL 8.4, Node/Angular y E2E **según archivos de implementación**, no por existencia de CI.

**A0** evaluar dos archivos documentales, integridad, secretos y referencias; **A1** objetivo, ocho conceptos de ciclo, NR positivos/negativos; **A2** revisar JWT/RBAC, actores plataforma/empresa, aislamiento, reintentos/PII y restricciones de suspensión; **A3** contrato V5/DAG, código y consumidores verificados, sin simular DB/HTTP; **A4**, luego de publicar, verificar SHA fast-forward, diff/blob, Scope Lock exact-SHA y readback PLAN/COLA/BITACORA 26 campos; **A5_PENDIENTE** revisión posterior de otra Task, sin QA/main/PROD automático.

**Impacto:** nuevo documento V5, CHANGELOG_AI.md y eventos institucionales. No cambia API, Domain, Application, EF, MySQL, Angular, credenciales, datos, migraciones, servicios ni configuración en producción. Imágenes históricas = cero, migración NO_APLICA; archivos futuros bajo controles tenant-aware. **Rollback:** git revert normal del commit documental tras revalidar HEAD, no force-push/reset; conservar BITACORA y autoria histórica.
