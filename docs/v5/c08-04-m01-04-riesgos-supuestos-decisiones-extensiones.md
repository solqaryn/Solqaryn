# SOLQARYN V5 — C08-04.M01.04: riesgos, supuestos y decisiones de extensiones por rubro

**Microtarea:** C08-04.M01.04 · PLAN_MAESTRO fila 1265 · **Autor:** PRIMARY DEV 05 / JAVIER_CHATGPT / TASK_ID 6adc2e55f47c81919f6374e561687076 · **Fecha:** 2026-11-14 · **Repositorio:** solqaryn/Solqaryn@dev · PROJECT_SCOPE_LOCK=STRICT.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA. Esta M01.04 documenta riesgos POTENCIALES, supuestos NO_VERIFICADOS y decisiones PENDIENTES; **no identifica vulnerabilidades explotadas ni demuestra funciones sectoriales ejecutables**. No activa capabilities ni modifica Angular, API, MySQL, JWT, RBAC, infraestructura, pagos, datos reales o entornos.

## Fuentes y alcance de evidencia

| Fuente de dev | Blob exacto leído en preflight | Alcance verificable |
| --- | --- | --- |
| docs/v5/c08-04-m01-01-alcance-extensiones-por-rubro.md | 41dd2a10d667124c021791278d43ceab3dc02c35 | NR01–NR12 y escenarios futuros |
| docs/v5/c08-04-m01-02-actores-entradas-salidas-extensiones.md | 606e34598c50a3419d535115679631b705d46f84 | A01–A14 e I01–I12, interfaces propuestas |
| docs/v5/c08-04-m01-03-inventario-codigo-contratos-extensiones.md | f34c6b59328d9d5a9e106e55eedeb250d50fb900 | Once fuentes y G01–G10 por demostrar |
| docs/v5/c08-04-m01-05-criterios-aceptacion-extensiones-sectoriales.md | 9ad425e66e7839d38a2416cf97615b20e0e129a2 | AC01–AC14 y NFR01–NFR10, futuro NO_EJECUTADO |
| backend/src/Domain/Entities/Empresa.cs | c44940df7c30983a4c23b93338e0cb9367962069 | Raíz de identidad empresarial; no prueba un motor de rubros |
| backend/src/Domain/Entities/UsuarioEmpresa.cs | f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c | Membresía organizacional; no equivale a suscripción comercial |
| frontend/src/app/core/guards/permiso.guard.ts | 1052bd38041055cc81e84a14d5fe0364e169b1fe | Restricción de navegación; nunca autoridad backend |

La inspección de estos documentos y blobs no prueba integración completa entre reservas, membresías, catálogo y sitios. En particular, EmpresaId nullable histórico no concede visibilidad global; un host, slug o X-Empresa-Id solicitado no sustituyen la pertenencia verificada. Las brechas G01–G10 son **evidencia por obtener**, no defectos certificados.

## R01–R16: matriz de riesgos POTENCIALES y mitigación futura

| Riesgo | Evento futuro a prevenir | Control y evidencia exigibles |
| --- | --- | --- |
| R01 | Una capability permite consultar recursos del tenant B con sesión A | Contexto A/B resuelto server-side; casos positivos y 403/404 negativos |
| R02 | Una bandera de UI habilita funciones sin RBAC backend | Autorizar cada recurso/acción en API; test de acceso directo sin Angular |
| R03 | Estado de empresa suspendida queda usable por caché vieja | Validar empresa activa y versión vigente, denegación por defecto |
| R04 | Un host/slug/header manipulado selecciona otra empresa | Normalizar y validar binding público; ninguna elevación a zona privada |
| R05 | Producto, cliente o sucursal con EmpresaId NULL se vuelve global | Reconciliar ownership o bloquear, con fixtures legacy A/B |
| R06 | Membresía gimnasio se confunde con UsuarioEmpresa o plan SaaS | Contratos distintos y tests de privilegios/entitlement |
| R07 | Extensión crea una segunda identidad, catálogo o entidad Empresa | Reutilización versionada del núcleo existente, verificación de esquema |
| R08 | Activación con clave arbitraria inyecta HTML/JS/SQL o permisos | Allowlist tipada de capacidades y validación de payload con negativos |
| R09 | Reactivación o desactivación borra reservas, facturas o auditoría | Transacción de estado, historial inmutable y pruebas de regresión |
| R10 | Dos operadores pisan una versión de activación concurrente | CAS/idempotencia por tenant y versión; prueba de conflicto reproducible |
| R11 | Reintento por timeout duplica efectos comerciales | Idempotency key por acción y deduplicación en persistencia autorizada |
| R12 | JWT expirado o usuario revocado conserva acceso vía caché | Comprobar sesión/RBAC/estado efectivo, prueba de revocación |
| R13 | Soporte, auditoría o telemetría revela PII o secretos del tenant B | Minimización, correlación saneada y tests de no filtración |
| R14 | Un proveedor/flag cache falla abierto y activa capacidades | Fail-closed y fallback conservador; inyección de fallo controlada |
| R15 | Ruta sectorial pesada degrada UI ligera o accesibilidad | Lazy loading, teclado/foco, 320px, reduced motion y pruebas E2E futuras |
| R16 | Un documento preparatorio se interpreta como licencia de despliegue | Gates M01.06–M06 y A5 independientes; autorización separada QA/main/PROD |

**Clasificación:** R01–R16 son riesgos de diseño, no hallazgos de producción; se validarán o descartarán con pruebas y rutas concretas de M02–M05. Su mitigación no se implementa en este changeset.

## S01–S08: supuestos falsables NO_VERIFICADOS

| Supuesto | Hipótesis sin certificar | Cómo comprobar o descartar |
| --- | --- | --- |
| S01 | Existirá catálogo tipado allowlisted por tenant y rubro | Contrato de dominio, DTO, migración evaluada y prueba de clave desconocida |
| S02 | Los módulos podrán compartir Cliente/Producto/Sucursal sin colisiones | Mapa de ownership, consultas de datos y fixtures A/B |
| S03 | El RBAC actual cubrirá todas las acciones sectoriales | Matriz operación/recurso/permiso e integración JWT real |
| S04 | El binding público podrá seleccionar sólo proyección de lectura | Verificación normalización de host y autorización privada independiente |
| S05 | Desactivar no exigirá borrar historial ni cerrar derechos adquiridos | Decisión comercial, contratos de transición y pruebas persistentes |
| S06 | La configuración conservará versión y permitirá rollback coherente | Diseño CAS, auditoría y prueba reversible |
| S07 | El flujo de reservas/membresía no duplicará contabilidad o SaaS billing | Revisión de dominio, transacciones y propiedad de datos |
| S08 | UI sectorial podrá mantenerse modular, accesible y liviana | Presupuestos medidos en build y Playwright con fixtures reales |

No inferir de un supuesto la creación de una entidad, una tabla, un worker, un plan de pago, un nuevo endpoint o un permiso. Si no se valida, mantener NO_GO del cambio dependiente.

## D01–D08: decisiones PENDIENTES para futuras fases

| Decisión | Pregunta a resolver | Política mientras siga pendiente |
| --- | --- | --- |
| D01 | ¿Quién gobierna el catálogo de capabilities y su versión? | No crear activaciones editables ni aceptar claves arbitrarias |
| D02 | ¿Qué rubros y recursos pueden habilitarse por tenant/sucursal? | Denegar alcance no enumerado y preservar ownership |
| D03 | ¿Qué matriz RBAC por operación/rol aprueba activación y reversión? | No derivar permisos de la configuración o de Angular |
| D04 | ¿Cómo se reconcilia EmpresaId NULL legado sin fuga? | No convertir NULL en lectura global ni migrar sin plan |
| D05 | ¿Cuál es la semántica de suspensión y del historial previo? | Bloquear nuevas altas; preservar evidencias y operaciones históricas |
| D06 | ¿Cuál es el contrato de idempotencia, CAS y auditoría? | No activar acciones con reintentos no seguros |
| D07 | ¿Qué reglas relacionan reserva, membresía, storefront y catálogo? | No reutilizar SaaS billing ni duplicar identidad de empresa |
| D08 | ¿Qué gates de rollback, soporte, PII y pruebas preceden despliegue? | Mantener dev-only, sin nuevos secretos, DNS ni proveedores |

Las decisiones quedan registradas, no tomadas por inferencia. No se requiere inventar una resolución comercial para cerrar esta microtarea **documental**; sí antes de implementar cambios que dependan de ella.

## Pruebas futuras positivas y negativas — TODAS NO_EJECUTADAS

| Caso | Positivo futuro | Negativo futuro / rechazo requerido |
| --- | --- | --- |
| TP01/TN01 | Admin A con permiso y capability válida obtiene recursos de A | Token A con ID de B no accede ni filtra PII |
| TP02/TN02 | Cambio autorizado A queda versionado y auditado | Rol lector, JWT revocado o cambio desde B se rechazan |
| TP03/TN03 | Desactivar bloquea nuevas operaciones sin perder historial | Flag UI, ruta directa o caché vieja no habilitan API |
| TP04/TN04 | Configuración allowlisted admite rollback de versión A | Clave desconocida, HTML/JS/SQL y payload extra se rechazan |
| TP05/TN05 | Reintento idéntico no duplica el efecto | Replay o carrera multiusuario no crean operación duplicada |
| TP06/TN06 | Host validado sólo expone contenido público permitido | Host falsificado o forwarded host no concede sesión privada |
| TP07/TN07 | Producto/cliente/sucursal con owner verificado se segrega | Registro legacy de propiedad incierta no se comparte |
| TP08/TN08 | UI permite teclado, foco y navegación 320px | Alterar guard/interceptor no elude RBAC backend |

TP/TN anteriores son especificaciones y **no representan ejecuciones, PASS de CI, cobertura real o ausencia de vulnerabilidades**. M03 requiere código real .NET 10/Angular 22.2.1/MySQL 8.4 con pruebas causales exact-SHA cuando proceda.

## Fronteras, cierre A0–A5 y rollback

- **Impacto actual:** sólo este Markdown y sección autocontenida en CHANGELOG_AI.md; no cambia Domain, Application, Infrastructure, API, frontend, EF/MySQL, RBAC, JWT, PII, jobs, proveedores o observabilidad real. Consumidores futuros: M01.06/M02–M06 condicionados por DAG vivo.
- **A0:** identidad Javier DEV 05, autorización exclusiva CAS, Skill canónica, Scope Lock exact HEAD, PLAN H/I, COLA G/O y DAG F/G; revisión del archivo nuevo y exclusión de ruta ajena.
- **A1:** coherencia documental de R01–R16, S01–S08, D01–D08 y TP/TN con las fuentes M01.01–03 y criterios M01.05. Los escenarios son FUTUROS_NO_EJECUTADOS.
- **A2:** análisis de amenazas tenant A/B, JWT/RBAC, revocación, host, replay, ownership legacy, PII, fallos de caché y fail-closed; no se certifica seguridad runtime.
- **A3:** trazabilidad documental de interfaces futuras entre Domain/Application/Infrastructure/API/Angular; builds, MySQL, E2E y pruebas de runtime son NO_APLICA sólo si el diff final es exclusivamente Markdown.
- **A4:** diff y blobs del SHA publicado en dev mediante fast-forward/CAS; Scope Lock y control V5 de procedencia del CHANGELOG completed/success en **ese SHA**; PLAN/COLA/BITACORA readback. SKIPPED/CANCELLED/IN_PROGRESS no son PASS cuando un check es requerido.
- **A5:** PENDIENTE, necesariamente otra Task Javier distinta del autor; no certificación ni promoción automática a QA/main/PROD.
- **Rollback:** git revert normal de la publicación documental, con nueva evidencia auditada y sin force-push ni borrado de BITACORA.

Las futuras implementaciones de pago, eventos/dispatcher, workers, migraciones, cambios de IAM, R2, DNS o producción siguen **fuera de este alcance**.
