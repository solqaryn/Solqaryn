# SOLQARYN V5 FINAL — C02-01.M01.04: riesgos, supuestos y decisiones del contexto de empresa

**Clasificación:** TIPO 1, preparación documental provisional M01.04; no implementación. **Fecha:** 2026-11-13. **Proyecto:** solqaryn/Solqaryn, dev, PROJECT_SCOPE_LOCK=STRICT. **Autor:** PRIMARY DEV 01 — JAVIER, TASK_ID 6adbb9fff1548191a1bd5b30d9e9984a; esto no representa una revisión independiente A5.

**Fuente institucional:** Standard V5 FINAL, Google Doc 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA y Sheet 1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU (PLAN_MAESTRO, COLA, DAG_TECNICO, BITACORA).

## Objetivo y exclusiones

La futura fase C02-01 debe definir el contexto seguro de empresa por membresía, host y JWT sin elevar señales de cliente a autoridad. **Entrega atómica M01.04:** registrar riesgos, supuestos por verificar y decisiones abiertas; no constituye servicio nuevo, certificación multiempresa live, ni aprobación de C02-01.M01.06 o M02–M06. DAG_TECNICO autoriza solamente esta preparación documental provisional; revalidar contra C00 y C01-01 al cierre del lote.

**Fuera de alcance:** no modificar JWT, BCrypt, roles, claims, host mapping, API, middleware, DTO, Angular, EF/MySQL, migraciones, secretos, DNS, Cloudflare, R2, tareas, reglas de rama o datos productivos; tampoco QA/main/PROD, PR o despliegues. No inventar una migración de imágenes históricas: inventario cero, NO_APLICA. Una futura arquitectura objetivo Hostinger/Cloudflare no es infraestructura desplegada.

**Clasificación de pruebas:** requiere_dotnet=NO (sin C#), requiere_node_angular=NO (sin frontend/JS), requiere_mysql=NO (sin datos/schema), requiere_docker=NO, requiere_e2e=NO, requiere_security=SI (fronteras JWT/RBAC/tenant y riesgos), requiere_scope_lock=SI (commit documental exact-SHA). NO_RUNTIME_CERTIFICATION: este texto no afirma haber ejecutado API, MySQL o Playwright.

## Fuentes de código efectivamente leídas en dev

| Archivo | Blob SHA | Observación limitada |
| --- | --- | --- |
| backend/src/API/Controllers/TenantContextController.cs | 2407c2eb6779ed724e841c688ee14dabbf163171 | Selección exige autenticación y servicio de membresía; un ID solicitado no confiere autorización. |
| backend/src/API/Filters/RequiereMembresiaEmpresaAttribute.cs | 17c7d191e3e1b34f56c3d27ba4992b512526542b | Resuelve membresía activa antes de marcar contexto empresarial server-side. |
| backend/src/Application/Security/VerifiedCommercialTenant.cs | 2da140ce9d7f47329d39d292d3271596467910cc | La marca de ámbito autorizada proviene del servidor, no de cabecera, query o DTO. |
| backend/src/Application/Security/VerifiedPublicStoreTenant.cs | 1bd272b848a2293184aae0e10f84f99f2b0b5b3d | Marca pública para rutas /tienda; no autoriza operaciones administrativas ni reemplaza RBAC. |
| backend/src/Domain/Entities/UsuarioEmpresa.cs | f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c | Vincula usuario, empresa, rol y actividad; rol legacy no sustituye autorización tenant. |
| backend/src/API/Controllers/AuditoriaController.cs | 82057f2acc571263ed7ed530eae0f81359b2f81a | Lectura requiere autenticación, permiso y rol administrador. |

Los blobs identifican código revisado, no avalan el estado de todas las rutas ni comportamiento de instalaciones reales.

## Riesgos y controles exigibles

| ID | Riesgo / negativo reproducible | Criticidad | Control y prueba futura |
| --- | --- | --- | --- |
| R01 | Usuario en empresa A fuerza EmpresaId B por header, query o DTO | CRITICA | Resolver ámbito tras membresía server-side; A→B debe denegar sin lectura ni escritura B. |
| R02 | Host desconocido, ambiguo o manipulado selecciona empresa | CRITICA | Host allowlisted y relación única verificada; desconocido falla cerrado sin tenant por defecto. |
| R03 | JWT válido pero membresía revocada sigue autorizando | CRITICA | Autorizar en cada operación sensible con contexto y membresía vigente; prueba revocación. |
| R04 | Rol empresarial o legado se eleva a plataforma o se reutiliza en otra empresa | ALTA | Separar autoridad global y empresarial; probar que rol A no da permiso B. |
| R05 | Recurso o adjunto sin propietario positivo se expone a otro tenant | ALTA | Filtro server-side por empresa y recurso; denegar export/listado y cambios A→B. |
| R06 | Error distinto para empresa inexistente vs no autorizada revela tenants | ALTA | Respuesta denegada sin enumeración; caso negativo de mensajes/códigos. |
| R07 | Caché o contexto de solicitud se reutiliza después de cambiar tenant | ALTA | Contexto por sesión/request con key tenant-bound; probar A→B→A y sesión inválida. |
| R08 | Auditoría o eventos registran tokens, contraseñas, PII de terceros | ALTA | Sanitizar, limitar lectura/retención y verificar logs positivos/negativos. |
| R09 | Flag/configuración de tenant permite privilegios o proveedor no autorizado | ALTA | Allowlist de claves/tipos, RBAC, default denegado; negativa de override. |
| R10 | CI anterior o documentación se confunde con PASS de integración nueva | ALTA | SHA exacto, test causal por superficie y revisor distinto; SKIPPED no es PASS. |

Defecto crítico sin evidencia suficiente bloquea la unidad ejecutable afectada. Ningún cierre documental sustituye pruebas JWT/RBAC/MySQL reales.

## Supuestos por validar, no certificados

- S01: una persona puede pertenecer a múltiples empresas con permisos distintos; falta matriz de sesiones/recursos y revocación.
- S02: host→empresa tiene unicidad verificable por entorno; falta inventario de proxy confiable, certificados y dominios.
- S03: cada recurso/sucursal está delimitado por empresa y consulta EF/API; falta inventario exhaustivo en HEAD de implementación.
- S04: los cambios de roles o membresías invalidan autorización/caché en el tiempo adecuado; faltan pruebas con tokens activos.
- S05: auditoría retiene información mínima sin PII/tokens sensibles; falta revisión integral de serialización, lectura y expiración.

## Decisiones pendientes

- D01: autoridad única que resuelve contexto de empresa para sesión autenticada y rutas públicas; no crear resolutor duplicado silencioso.
- D02: reglas de cambio/revocación de empresa, sesión y membresía; la UI no concede permiso.
- D03: roles plataforma vs tenant e impersonación autorizada; exigir identidad, trazabilidad y denegación por defecto.
- D04: host desconocido o multi-match; no elegir empresa arbitraria, documentar error seguro.
- D05: matriz mínima de negativos A/B, revocación, errores, auditoría y rollback para M02–M05.
- D06: compatibilidad, índices y migraciones futuras; sin SQL real ni alteraciones en M01.04.

## Verificaciones de aceptación documental

**P1 positivo:** 10 riesgos R01–R10 tienen gravedad, barrera y caso comprobable; P2 positivo: cinco supuestos S01–S05 distinguen hipótesis de evidencia y seis decisiones D01–D06 permanecen abiertas. **N1 negativo:** ninguna cabecera X-Empresa-Id, host arbitrario, slug, JWT sin membresía o rol legado otorga permiso. **N2 negativo:** ninguna prueba HTTP/DB/Playwright ni A5_PASS se atribuye a esta M01.04. **N3 negativo:** registrar riesgos no certifica C02-01 ni abre gates M01.06/M02. **A3 documental:** cruzar blobs, fuente V5, PLAN H/I, COLA E/G/O, DAG VALIDADO y BITACORA física A:Z (26 campos).

**Revalidar:** al cerrar C00-01 y C01-01, al cambiar HEAD/gobierno, antes de ejecutar M01.06/M02 y antes de QA/main/PROD. **Rollback:** nuevo revert aditivo y auditable de este documento/CHANGELOG, nunca force-push; conservar eventos BITACORA históricos. **A5 independiente:** pendiente de TASK_ID ajeno al autor, no concede despliegue.
