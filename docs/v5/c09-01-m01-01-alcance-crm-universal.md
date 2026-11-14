# SOLQARYN V5 — C09-01.M01.01: objetivo, exclusiones y no-regresión del CRM universal

**Unidad:** C09-01.M01.01 · PLAN_MAESTRO fila 1298 · **Responsable:** PRIMARY DEV 05 JAVIER (TASK_ID 6adc2e55f47c81919f6374e561687076) · **Fecha:** 2026-11-14 · solqaryn/Solqaryn@dev · PROJECT_SCOPE_LOCK=STRICT.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA. Resultado esperado del lote C09-01: historial, segmentos, oportunidades, actividades y seguimiento de clientes; evidencia final futura de timeline aislado por empresa y permisos por rol. **Esta M01.01 sólo fija el contrato documental de preparación**. No crea un CRM funcional ni modifica contratos públicos, tablas, background jobs, correos, campañas, consentimientos, datos personales o infraestructura.

## Objetivo verificable y frontera con sistemas existentes

Preparar un CRM universal y modular que reutilice Cliente, Empresa y UsuarioEmpresa de SOLQARYN sin duplicar identidad, roles, catálogo, reservas, pedidos, ventas ni planes SaaS. El CRM futuro deberá poder representar un historial cronológico de interacciones con evidencia de actor, empresa, origen, autorización e idempotencia, y segmentación de clientes estrictamente por pertenencia empresarial. Las oportunidades y actividades futuras requieren dueño/rol, estado y trazabilidad; **ninguno de estos componentes se certifica como existente por este documento**.

| Capacidad propuesta | Autoridad/contrato existente a preservar | Límite de la propuesta |
| --- | --- | --- |
| Ficha de contacto CRM | Cliente y su ownership empresarial | No crear una segunda identidad cliente global |
| Timeline por empresa | Empresa, UsuarioEmpresa, auditoría y permisos backend | No publicar PII de otro tenant ni historiales globales |
| Segmentos comerciales | Etiquetas y reglas tipadas futuras | No usar filtros arbitrarios SQL/JS o exportación sin permiso |
| Oportunidades y seguimiento | Actores de venta y ownership por sucursal/empresa | No tratar cambios de etapa como cobros o facturación |
| Actividades/tareas de usuarios | Roles, autorizaciones y permisos vigentes | No crear nuevas Tasks de ChatGPT, workers o notificaciones |
| Comunicación futura C09-04 | Adaptadores aprobados y preferencias demostrables | Sin envío de email/WhatsApp, webhooks ni proveedores activados |

## Evidencia estática comprobada en GitHub dev

| Fuente real | Blob SHA | Hecho observado y límite |
| --- | --- | --- |
| backend/src/Domain/Entities/Cliente.cs | 8c6d4c4453e44f8c0be9475340465e1aadb3f787 | Cliente tiene EmpresaId nullable legado, Nombre, Teléfono, Correo y otros PII; no concede acceso universal |
| backend/src/Domain/Entities/Empresa.cs | c44940df7c30983a4c23b93338e0cb9367962069 | Raíz canónica Empresa; no prueba timeline CRM implementado |
| backend/src/Domain/Entities/UsuarioEmpresa.cs | f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c | Membresía y rol de usuario por empresa, distinta de cliente CRM |
| frontend/src/app/core/guards/permiso.guard.ts | 1052bd38041055cc81e84a14d5fe0364e169b1fe | Guarda de rutas; no sustituye validación RBAC real en API |
| frontend/src/app/core/interceptors/auth.interceptor.ts | 4bdf19135ed1932a99fd09ace1f900bcccc5b25b | X-Empresa-Id es un contexto solicitado, nunca autoridad cliente |

Estos cinco archivos fueron leídos por ruta/blobs en el HEAD del preflight. No se afirmó cobertura de todos los controladores, consultas, migraciones o pruebas existentes. La siguiente M01.03 deberá ampliar el inventario antes de diseñar cambios de persistencia o API.

## NR01–NR14: invariantes de no-regresión para implementación futura

- **NR01:** cada contacto CRM y registro histórico pertenece a una Empresa explícita y verificada desde backend.
- **NR02:** tenant A no puede leer, modificar, exportar o inferir PII y actividades del tenant B, incluso si conoce sus IDs.
- **NR03:** EmpresaId nulo legado exige reconciliación/denegación; nunca se asume alcance global.
- **NR04:** UsuarioEmpresa vigente y rol/permiso por acción autorizan CRM; un Cliente no hereda permisos administrativos.
- **NR05:** ocultar botones en Angular no autoriza ni impide por sí solo un endpoint; el servidor decide.
- **NR06:** host, slug, cabecera, cookie o EmpresaId remitido no establecen autorización.
- **NR07:** clientes, ventas, reservas, pedidos e inventario conservan su identidad original; CRM no duplica ni reescribe transacciones.
- **NR08:** una actividad histórica conserva actor, zona temporal y origen con trazabilidad; no reescribe auditoría preexistente.
- **NR09:** idempotencia de importación/sincronización futura impide entradas y notificaciones duplicadas.
- **NR10:** segmentos tipados/allowlisted no ejecutan SQL, JavaScript ni expresiones arbitrarias del usuario.
- **NR11:** contacto no implica consentimiento para publicidad; campañas futuras exigen preferencias, finalidad, opt-out y autorización propia.
- **NR12:** exportar, compartir, buscar y filtrar PII requiere alcance de tenant, rol, retención y minimización verificables.
- **NR13:** error de caché, proveedor, búsqueda o permisos produce denegación segura y no revela datos.
- **NR14:** UX futura mantiene teclado, foco, lectura clara, 320px y reduced motion; no degrada rutas operativas existentes.

## P01–P08 / N01–N12: oráculos FUTUROS_NO_EJECUTADOS

| Caso | Positivo futuro que exigirá prueba | Negativo que deberá ser rechazado |
| --- | --- | --- |
| P01/N01 | Vendedor autorizado A consulta ficha de Cliente A | Con ID B conocido no obtiene ficha, teléfono ni correo |
| P02/N02 | Registrar una actividad A produce línea de tiempo del mismo tenant | Usuario B o rol sin permiso no escribe ni lee historial A |
| P03/N03 | Segmentación allowlisted A retorna sólo clientes A | Segmento SQL/JS arbitrario o filtro cross-tenant se rechaza |
| P04/N04 | Oportunidad A enlaza cliente, usuario y sucursal autorizados | Sucursal/empresa B, rol revocado o dueño sin vínculo se rechazan |
| P05/N05 | Dos reintentos con la misma idempotency key producen un resultado | Replay/concurrencia no duplican actividad o efecto comercial |
| P06/N06 | Exportar datos A deja traza mínima y respeta finalidad autorizada | Exportación por UI alterada no evita seguridad ni filtra B |
| P07/N07 | Opt-out futuro bloquea campañas sin borrar historial lícito | Contacto sin consentimiento no recibe mensajes |
| P08/N08 | Vista accesible responde con estados claros y foco correcto | Error API, 401/403 o caché caducada no muestra PII previamente privada |

**Negativos adicionales:** N09 JWT expirado/revocado; N10 EmpresaId NULL histórico no reconciliado; N11 payload malicioso y campos no allowlisted; N12 pérdida de proveedor/búsqueda no abre permisos ni causa exportación. Los positivos y negativos son especificaciones FUTURAS NO_EJECUTADAS: no se ejecutó xUnit, MySQL, API, Angular ni Playwright como parte del documento.

## Exclusiones, dependencias y decisiones pendientes

**Excluido del cambio actual:** crear entidades CRM, DTO/API, migraciones EF/MySQL, tabla de actividades, campañas masivas, colas, workers, webhooks, SMTP/WhatsApp, nuevos roles, flags activos, secretos, pruebas en producción, QA/main/PROD o modificación de Tasks programadas. No se reusa sin autorización una fuente de mensajes/comunicación o de facturación como si fuera CRM.

**Decisiones de etapas futuras por validar:** modelo y reglas de propiedad de Cliente histórico, retención y finalidad de PII, segmentos allowlisted, permisos CRM por operación, reconciliación de timeline con auditoría, consentimiento por canal, deduplicación/importación y límites de cuota/rendimiento. Se registran preguntas sin inventar respuestas de producto.

**Dependencias:** DAG_TECNICO para C09-01.M01.01 F=VALIDADO/G=SI y COLA G/O=SI habilita sólo esta preparación documental. M01.02 mapea actores/interfaces; M01.03 inventaría código y pruebas; M01.04 documentará riesgos/supuestos/decisiones; M01.05 define aceptación; M01.06 y M02–M06 requieren gates reales independientes. Completar esta M01.01 no libera implementación de CRM, mensajería, migraciones ni despliegues.

## A0–A5 causal, seguridad, pruebas y reversión

- **A0:** Skill y arquitectura canónicas contrastadas, identidad J40, HEAD de dev y Scope Lock exacto, elegibilidad PLAN/COLA/DAG, CAS propietario exclusivo y archivos sin colisión.
- **A1 documental:** objetivo, exclusiones, NR01–NR14 y ocho pares P/N consistentes con fuentes reales; no se declara PASS de funcionalidad inexistente.
- **A2 documental:** riesgos de tenant A/B, JWT/RBAC, PII, consentimientos, legacy NULL, replay y fail-closed; no se certifica aislamiento HTTP real.
- **A3 documental:** mapa Domain/Application/Infrastructure/API/Angular y fronteras, sin modificar servicios; compilaciones, migraciones y E2E NO_APLICA a un diff exclusivamente Markdown.
- **A4:** publicación directa fast-forward con CAS de HEAD, diff de dos Markdown y blobs exactos, comprobaciones Scope Lock + provenance V5 completed/success sobre SHA, BITACORA/PLAN/COLA readback.
- **A5:** debe efectuarla otra Task Javier de ID distinto, sin autorrevisión ni promoción implícita.
- **Rollback:** git revert normal del commit, no force-push ni edición retrospectiva de BITACORA.

Esta entrega es **preparación documental de CRM** y no certifica un servicio nuevo, permiso productivo ni ausencia demostrada de vulnerabilidades.
