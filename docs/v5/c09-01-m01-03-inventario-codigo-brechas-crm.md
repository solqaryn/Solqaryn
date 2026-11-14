# SOLQARYN V5 — C09-01.M01.03: inventario de contratos, código y brechas de CRM universal

**Microtarea:** C09-01.M01.03 · PLAN_MAESTRO fila 1300 · **Autor:** PRIMARY DEV 05 JAVIER, TASK_ID 6adc2e55f47c81919f6374e561687076 · **Fecha:** 2026-11-14 · **Repositorio:** solqaryn/Solqaryn@dev · PROJECT_SCOPE_LOCK=STRICT.

**Fuente ejecutable:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA). **Propósito:** leer contratos, código y evidencia existentes sin asumir implementación ni certificación CRM. La fase C09-01 exige en etapas posteriores historial, segmentos, oportunidades, actividades y seguimiento, con timeline aislado y permisos por rol. Esta M01.03 es exclusivamente **inventario técnico documental**.

## Antecedentes y límites

- C09-01.M01.01 (blob **4d35d8113cf1a1718dd7f487c50a06f51c1848b5**) delimita NR01–NR14, P01–P08 y N01–N12 FUTUROS_NO_EJECUTADOS.
- C09-01.M01.02 (blob **02a78bad5debea4678651624280a4353c54af24a**) define A01–A14, I01–I12 y F01–F09 como contratos propuestos, **no endpoints operativos**.
- La existencia de código de Cliente, ventas, auditoría y UI no implica motor CRM, timeline, oportunidades, segmento persistido o campañas. Este inventario **no ejecutó queries productivas, migraciones, xUnit, Angular o Playwright**, y no certifica el aislamiento transversal sólo por leer rutas.

## Inventario de dieciséis fuentes con blob Git verificado en dev

| Fuente real leída/listada en árbol dev | SHA del blob | Contrato observable y límite de la evidencia |
| --- | --- | --- |
| backend/src/Domain/Entities/Cliente.cs | 8c6d4c4453e44f8c0be9475340465e1aadb3f787 | Cliente con EmpresaId nullable legacy y PII; no es CRM tenant-global |
| backend/src/Domain/Entities/Empresa.cs | c44940df7c30983a4c23b93338e0cb9367962069 | Empresa raíz, no módulo CRM |
| backend/src/Domain/Entities/UsuarioEmpresa.cs | f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c | Pertenencia y rol empresarial, no consentimiento comercial |
| backend/src/API/Controllers/ClientesController.cs | 023d639f5c6ebf20519a22b175e2bf17c53e6d0d | Controller [Authorize] de clientes, no timeline CRM |
| backend/src/Application/Interfaces/IClienteService.cs | 2626e96632c2639cb66933431f2ad21c7d49f8cd | Operaciones CRUD, GetAll, activos y búsqueda, sin contratos de oportunidades |
| backend/src/Application/Services/ClienteService.cs | 51661879903e96064557f1a1b4bd3f2c1c79a77b | Servicio de cliente con CurrentUser y auditoría; no prueba autorización completa CRM |
| backend/src/Application/Interfaces/IClienteRepository.cs | 934f3a356c249bb61a0cd350c5eab1390671b7c2 | Contrato de lectura, búsqueda y persistencia de Cliente |
| backend/src/Infrastructure/Repositories/ClienteRepository.cs | 07bb969b52ea4273ac31975ebbc10d19af8a9d75 | EF/DbContext y consultas Cliente, filtros globales/tenancy requieren investigación adicional |
| backend/src/API/Controllers/AuditoriaController.cs | 82057f2acc571263ed7ed530eae0f81359b2f81a | Endpoint de auditoría [Authorize] y RequierePermiso, no timeline CRM |
| backend/src/Application/Services/AuditoriaService.cs | def2c1f7bc58e23f98a1065fcdf0963322f3705c | Contrato operativo de registrar auditoría, no historial CRM per se |
| frontend/src/app/services/cliente.service.ts | e651c4b9027318333d15a08a035ed904f3b91073 | ClienteService Angular usa rutas /clientes, no motor de oportunidades |
| frontend/src/app/features/clientes/clientes-list.component.ts | 3cb07e8e2fdcdb35d55de57f377bd446170b0e24 | Lista con filtros/orden, no demuestra segmentos CRM server-side |
| frontend/src/app/features/clientes/cliente-form.component.ts | 0363b26891ecce7d374f3e98fe67e963ac155fa6 | Formulario Angular y permisos UI, no autorización API |
| frontend/src/app/core/guards/permiso.guard.ts | 1052bd38041055cc81e84a14d5fe0364e169b1fe | Guarda Angular de rutas, no autoridad tenant de backend |
| frontend/src/app/core/interceptors/auth.interceptor.ts | 4bdf19135ed1932a99fd09ace1f900bcccc5b25b | X-Empresa-Id solicitado no prueba pertenencia server-side |
| backend/src/Application/Interfaces/IContextoTenantService.cs | 97d0a05456c66d069a56b9743bbc286af07502b1 | Interfaz de contexto tenant localizada, sin afirmar cobertura de cada endpoint |

**Procedencia:** blobs de rutas exactas consultados en GitHub dev; hashes adicionales observados en la lista del directorio original. Los cinco repositorios de contexto son componentes existentes, no evidencia de CRM completo. Las consultas EF no se certifican como vulnerables ni seguras en todos los casos por observar sólo ClienteRepository: deben inspeccionarse filtros efectivos, middleware, scopes y ejecución real en su microtarea correspondiente.

## G01–G12: brechas de evidencia, NO defectos certificados

| Brecha | Pregunta precisa pendiente | Evidencia futura necesaria |
| --- | --- | --- |
| G01 | ¿Existe una entidad/contrato específico de actividad y timeline CRM? | Tipo Domain, DTO, API, tests de lectura cronológica y orden |
| G02 | ¿Hay oportunidad comercial separada de venta/facturación? | Máquina de estados, referencia comercial y test de no duplicación |
| G03 | ¿El Cliente legado EmpresaId NULL está correctamente atribuido? | Política de reconciliación, fixtures y denegación segura |
| G04 | ¿Todas las consultas, búsquedas y reportes CRM filtran tenant? | Global EF filters, repositorios y pruebas HTTP A/B negativas |
| G05 | ¿Cada acción CRM tiene permisos concretos del backend? | Matriz rol/acción/recurso, 401/403/404 y revocación |
| G06 | ¿Los segmentos están tipados/allowlisted? | DTO de filtros, consultas parametrizadas y negativos SQL/JS |
| G07 | ¿Exportación y búsqueda preservan PII y límites? | Pruebas alcance, minimización, rate limit y paginación |
| G08 | ¿Existe consentimiento por canal y revocación de campañas? | Contrato de opt-out, evidencia y política de retención |
| G09 | ¿Las actividades se deduplican ante replay/concurrencia? | CAS/idempotencia y pruebas MySQL efímero |
| G10 | ¿La auditoría y CRM timeline están separadas semánticamente? | Contractos de eventos, correlación y prevención de duplicados |
| G11 | ¿Las vistas Angular guardan accesibilidad en CRM futuro? | Test teclado/foco, 320px, estados y reduced motion |
| G12 | ¿Se conservan clientes/ventas reales al revertir una activación CRM? | Rollback, relaciones, pruebas de integridad y no regresión |

Una G identifica **algo aún no demostrado en esta inspección**, no ausencia global de código ni una vulnerabilidad confirmada; no abrir accesos de datos para verificarla fuera de un scope autorizado.

## Matriz de oráculos POSITIVOS/NEGATIVOS FUTUROS_NO_EJECUTADOS

| Caso | Positivo futuro que exigir | Negativo que debe fallar cerrado |
| --- | --- | --- |
| P01/N01 | A con permiso obtiene perfil y timeline sólo de Cliente A | A consulta ID de B y no recibe PII/historial |
| P02/N02 | Crear actividad una vez con actor y empresa | Replay/concurrencia no duplica anotación |
| P03/N03 | Oportunidad con estado y dueño autorizados | Rol lector o sede B no cambia etapa |
| P04/N04 | Segmento allowlisted retorna sólo contactos del tenant A | SQL/JS o filtro B rechazados |
| P05/N05 | Export A autorizado y auditado contiene datos mínimos | Rol ajeno, cabecera falsa o EmpresaId NULL no exportan |
| P06/N06 | Consentimiento A habilita sólo el canal aprobado | Opt-out o ausencia de permiso impide envío |
| P07/N07 | Listado y búsqueda mantienen separación A/B | Caché vieja, timeout o error no filtran B |
| P08/N08 | Angular accesible ofrece foco y feedback de 401/403 | Manipular guard/ruta no evita backend |

Otros negativos críticos para implementación: N09 JWT vencido/revocado; N10 suplantación por slug/host; N11 campos no allowlisted y payload arbitrario; N12 borrar/vaciar historial al desactivar CRM. **No se ejecutó ninguna de estas pruebas;** los números no constituyen PASS de xUnit, MySQL, Angular, Playwright o API HTTP.

## Impacto, seguridad, gates y reversión

- **A0:** identidad Task J40, Skill canónica y arquitectura, Scope Lock exact HEAD; PLAN H/I, COLA G/O, DAG F VALIDADO/G SI, CAS sin escrituras simultáneas. Se excluyen archivos de otros scopes.
- **A1:** lectura de fuentes concretas, comprobación de hashes, contradicciones, 12 brechas honestamente no certificadas y P/N futuros. No prometer cobertura completa del repositorio.
- **A2:** análisis de tenant A/B, JWT/RBAC, PII, Cliente.EmpresaId NULL, exportación, consentimiento y replay; no se reclama prueba funcional de seguridad.
- **A3:** relación Domain/Application/Infrastructure/API/Angular y auditoría presente; runtime/compilación/integración EF/MySQL/E2E **NO_APLICA exclusivamente al diff Markdown**, sin atribuir PASS ficticio.
- **A4:** publicar docs/v5 y sección indivisible de CHANGELOG_AI.md por fast-forward con CAS de HEAD, verificar blobs/diff y checks Scope Lock y procedencia V5 completed/success de SHA exacto, readbacks PLAN/COLA/BITACORA.
- **A5:** PENDIENTE de otra Task Javier distinta de esta autora; no se autorrevisa ni autoriza QA/main/PROD.
- **Rollback:** git revert normal del commit, sin force-push, cambios de datos, secretos o borrado de BITACORA.

**Siguiente fase:** M01.04 deberá evaluar riesgos y decisiones de G01–G12; M01.05 aceptación observable; M01.06 y M02–M06 requieren gates propios. No se implementaron CRM real, campañas, mensajería, base de datos, worker, webhooks, migraciones ni nueva configuración.
