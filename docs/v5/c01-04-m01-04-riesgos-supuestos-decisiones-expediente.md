# SOLQARYN V5 FINAL — C01-04.M01.04: riesgos, supuestos y decisiones del expediente universal

**Microtarea:** C01-04.M01.04, preparación documental de M01; **resultado atómico:** registrar riesgos, supuestos y decisiones pendientes. **Autor:** PRIMARY DEV 02 — JAVIER, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`; `solqaryn/Solqaryn@dev`, Scope Lock STRICT. **Fuente única:** Google Doc V5 FINAL y Sheet PLAN_MAESTRO/COLA/DAG_TECNICO.

**Meta futura de C01-04:** unificar cronología, decisiones, observaciones, adjuntos y versiones, con contrato verificable de timeline y consulta aislada por ámbito. Este documento **no** afirma que el expediente universal, las tablas, APIs, jobs, pantallas o storage estén implementados; analiza riesgos previos a su diseño/implementación. Complementa M01.01–03 sin liberar M01.06/M02.

## 1. Registro de riesgos con evidencia, contención y prueba causal pendiente

| Riesgo | Evidencia observable o hipótesis | Severidad de diseño | Control que se debe definir antes de implementación y prueba futura |
| --- | --- | --- | --- |
| EXP-R01, lectura cruzada A/B | `AuditoriaRepository.GetFilteredAsync` (blob `8fc9417b88da91ad1a5777ea77d9e31756230384`) no muestra un filtro `EmpresaId` en su consulta; controller tiene restricción de administrador | Alto si se reutiliza para expediente | Investigar filtros globales AppDbContext, exigir ownership server-side por entidad/tenant y test negativo A→B; **no afirmar vulnerabilidad explotada** con esta lectura |
| EXP-R02, membrecía revocada o token stale | `ContextoTenantActual` blob `7377d2d752985cc9baf31d08280f38ad94a6a114` se fabrica desde UsuarioEmpresa activa, no claims de cliente | Alto | Revalidar autorización en cada request/operación de adjunto; prueba rol revocado y cambio A→B |
| EXP-R03, orden incorrecto de cronología | Repositorio existente ordena `Fecha` descendente sin desempate explícito por `Id` | Medio | Orden estable `timestamp, id` y paginación/cursor probados con eventos de misma hora |
| EXP-R04, overwrites/versionado | `RegistroAuditoria` ofrece valores antiguos/nuevos pero no una máquina de versionado de expediente | Alto | Diseñar versión esperada y conflictos optimistas, idempotency key y auditoría aditiva; nunca sustituir historia de origen |
| EXP-R05, observación tratada como autorización | `AuditoriaDto` lleva descripción y motivo, no estructura de decisión/approval | Alto | Estados y firmantes separados; autorización siempre RBAC backend; observaciones no conceden permisos |
| EXP-R06, adjuntos de B/legacy expuestos | `CompraDocumento` blob `7472b6418b5da34c58b5f7572f3fd6a858abac86` tiene EmpresaId nullable y URL/PublicId | Crítico potencial si contrato es reutilizado sin ownership | Reautorizar acceso a bytes/metadatos, storage tenant-aware, evitar URL pública permanente y cuarentena legacy; test descarga A→B |
| EXP-R07, compatibilidad storage incompleta | `ICompraDocumentoStorageService` blob `454d588851ecc7db810259e63705852360172928` mantiene overloads legacy y defaults tenant-aware fail-closed | Alto | Adaptadores explícitos, pruebas de fallo cerrado y verificación de consumidor antes de soporte universal |
| EXP-R08, respuestas tardías de UI | `auditoria.service.ts` blob `e7d51d65833bcfb7dbeac24a42751c9ed229f34e` sólo implementa GET con filtros, no garantía de descartar consultas de tenant anterior | Alto | Cancelar/validar contexto y bloquear paint stale tras logout o cambio de empresa; Playwright negativo A→B |
| EXP-R09, inyección y PII en logs | `RegistroAuditoria` contiene UserAgent, Ip, Descripción, Motivo, JSON de valores anteriores/nuevos y error | Alto | Redactar PII/secretos, definir retención, límites de longitud, normalización y consulta auditable |
| EXP-R10, eventos duplicados y parcialidad | No se encontró en los diez artefactos inspeccionados un contrato universal de outbox+expediente con confirmación transaccional | Alto | Diseñar idempotencia, correlación, orden y rollback en operaciones compuestas; probar fallo parcial de proveedor |
| EXP-R11, confusión entre auditoría y expediente | `AuditoriaController` blob `82057f2acc571263ed7ed530eae0f81359b2f81a` expone GET /auditoria para administradores, no entidad expediente | Medio | Especificar superficie nueva en M02 sin reutilizar privilegios globales; pruebas tenant/rol |
| EXP-R12, certificación prematura | V5 M01.01–05 sólo admite preparación documental validada por DAG; datos/runtimes futuros aún no probados | Alto (gobernanza) | Prohibir declarar LISTO funcional, A5_PASS propia, jobs SKIPPED como PASS, releases QA/main/PROD o M01.06 por contador |

**Importante:** R01/R06/R08 son riesgos de reutilización a verificar, **no certificaciones de brecha de seguridad ni cambios al código**. La vía de ejecución futura exige pruebas negativas concretas y revisión independiente.

## 2. Supuestos sujetos a validación

| Supuesto | Evidencia actual y estado | Verificación pendiente |
| --- | --- | --- |
| EXP-S01: recurso de negocio identifica empresa propietaria | Existen CompraDocumento.EmpresaId y ContextoTenantActual; **parcial** | Contratos de ownership por cada tipo de recurso y sucursal |
| EXP-S02: auditoría puede alimentar una vista provisional | RegistroAuditoria y filtros existen; **parcial** | Confidencialidad, scope explícito y orden estable; no sustituir un expediente |
| EXP-S03: adjuntos privados operan con storage tenant-aware | Interfaz permite overloads pero default falla cerrado; **sin certificación** | Implementación segura, API/DB/Cloudinary, expiración y negativos cross-tenant |
| EXP-S04: idempotencia evita duplicados concurrentes | No se encontró contrato universal en inventario; **pendiente** | Operación transaccional real + reintento y concurrencia |
| EXP-S05: UI evita mezcla de contextos | Servicio Angular existente no acredita política global; **pendiente** | E2E cambios tenant/logout con respuesta demorada |
| EXP-S06: migración histórica de imágenes necesaria | Inventario informado = cero; **NO_APLICA** para históricos | Archivos futuros bajo permisos, retención, MIME, tamaño y tenancy |

## 3. Decisiones documentadas y pendientes

**DEC-01 (fijada para diseño):** el backend determina ámbito de expediente desde membresía y propietario real; ni EmpresaId, slug, header, URL ni frontend conceden autoridad.

**DEC-02 (pendiente M02):** modelo de expediente polimórfico o agregado específico por recurso; establecer invariantes de referencia válida, tenant, sucursal y políticas de lifecycle, sin añadir nueva entidad hoy.

**DEC-03 (pendiente M02):** cronología con orden total estable, momento del evento vs persistencia, cursor y versionado; no inferir secuencia de timestamp solo.

**DEC-04 (pendiente M02):** decisiones requieren actor/rol, aceptación y estado auditado; observación es texto no autorizante. Separar evidencia legal/operativa.

**DEC-05 (pendiente integración):** adjuntos privados, storage tenant-aware, URL temporal y revocación; no usar legacy publicId como derecho de lectura.

**DEC-06 (pendiente ejecución):** concurrencia/idempotencia, outbox/evento, errores seguros y reversión aditiva, sin reescribir historia de ventas/documentos.

**DEC-07 (pendiente UI):** accesibilidad, paginación y descarte de respuestas anteriores al contexto de tenant actual; frontend no autoriza.

**DEC-08 (revisión):** A0–A4 proporcionales; A5 por TASK_ID diferente antes de QA/main/PROD. Ninguna aprobación documental habilita tests inexistentes ni promoción.

## 4. Validación y límites de esta microtarea

**DAG:** C01-04.M01.04 F=VALIDADO/G=SI; trece prerrequisitos C00-01.M02.01–06, C00-03.M01.01–05 y C01-01.M01.01–02 LISTO. Este gate sólo autoriza preparación M01.01–05; cierre M01.06/M02 siguen bloqueados hasta C00 final y seguridad de tenancy.

**Clasificación:** TIPO1_DOC. requiere_dotnet=NO, requiere_node_angular=NO, requiere_mysql=NO, requiere_docker=NO, requiere_e2e=NO por no afectar runtime. requiere_security=SI (revisión de riesgos JWT/RBAC/tenancy/PII/adjuntos); requiere_scope_lock=SI (commit dev).

**A0:** diff, 12 riesgos, 6 supuestos, 8 decisiones, estructura Markdown y secretos; **A1:** positivos documentales: registrar fuentes/riesgo y mitigación, distinguir certezas/hipótesis; negativos: rechazar un expediente B, un adjunto legacy inseguro y una autoaprobación ficticia; **A2:** fronteras multiempresa, RBAC, JWT, PII, storage; **A3:** comparar con V5/DAG/10 fuentes reales/M01.01–03 y las dependencias; builds .NET/Angular/MySQL/Docker/E2E **NO_APLICA**. **A4:** sólo tras commit FF: SHA exacto, Scope Lock SUCCESS, diff, rollback, readback PLAN/COLA/BITACORA 26. **A5:** pendiente de otro TASK_ID.

**Impacto:** sólo documentación V5 + CHANGELOG_AI.md + registros institucionales, sin alterar API, datos, migraciones, secrets, permisos ni infraestructura. **Rollback:** git revert normal tras releer dev; nunca force-push/reset, borrar filas de BITACORA ni promover qa/main/PROD. Revalidar los riesgos al programar M02.
