# SOLQARYN V5 — C05-03.M01.02: actores y fronteras del builder gobernado

**Autor:** PRIMARY DEV 05 / JAVIER_CHATGPT, TASK_ID 6adc2e55f47c81919f6374e561687076. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Doc 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA; PLAN_MAESTRO fila 795. **Repositorio/rama:** solqaryn/Solqaryn@dev, Scope Lock STRICT. **Fecha:** 2026-11-14.

## Objeto real y no-alcance

Identificar actores, entradas, salidas y superficies de confianza de un **builder de páginas/secciones propuesto** con catálogo versionado de componentes y campos tipados. Esta unidad M01.02 es un contrato documental, complementario de M01.01; **no implementa** editor, renderer, endpoints, persistencia, JWT, RBAC, worker, dispatcher, publicación o preview funcional. Que exista un servicio de tema visual no acredita la existencia del builder. Los datos configurables nunca son código ejecutable.

## Actores, entradas y límites de autoridad

| Actor | Entrada/intención futura | Salida futura | Frontera obligatoria |
| --- | --- | --- | --- |
| A01 Administrador tenant | Solicita editar borrador propio | Revisión tipada y auditada | Membresía y RBAC backend |
| A02 Editor delegado | Cambia texto/campos allowlisted | Borrador sin publicación | Permiso servidor, no UI |
| A03 Revisor/publicador | Solicita aprobación de versión | Publicación o rechazo | Rol separado y CAS de versión |
| A04 Usuario sin permiso | Intenta editar | 403/404 seguro | Denegación por API |
| A05 Visitante anónimo | Consulta página publicada | Solo contenido público | Resolver host/recurso en servidor |
| A06 Usuario tenant B | Pide ID/recurso de A | Sin acceso a A | Tenancy A/B |
| A07 Angular renderer futuro | Recibe manifiesto tipado | Componentes precompilados accesibles | Sin eval/HTML arbitrario |
| A08 Angular editor futuro | Solicita preview/guardar | Feedback y errores estructurados | Guard cliente no es autoridad |
| A09 Application service futuro | Valida caso de uso | Aceptar/rechazar y auditar | Domain sin Infrastructure |
| A10 Repository futuro | Guarda versión y tenant | Lectura controlada | MySQL no se altera ahora |
| A11 QA/seguridad | Define escenarios de abuso | Evidencia reproducible futura | No autoconfirmación |
| A12 Auditor autorizado | Examina historia versionada | Traza mínima sin secretos | Acceso restringido |

El cliente Angular no concede permisos por mostrar un botón. El backend deberá resolver JWT, pertenencia, roles y tenant A/B en cada operación privada; un host, slug, cabecera o empresaId enviado por el cliente jamás es prueba de autorización. Los actores son perfiles de diseño FUTUROS y no nuevos roles concedidos.

## Contratos de entrada/salida FUTUROS

| ID | Entrada | Procesamiento exigido | Salida o rechazo |
| --- | --- | --- | --- |
| I01 | Clave de componente | Catálogo fijo con versión | Renderer permitido o error de clave |
| I02 | Texto/número/booleano/enum | Tipo, longitud y clave allowlisted | Modelo tipado o rechazo |
| I03 | Secciones y orden | Límites de longitud, profundidad y ciclos | Árbol acotado o error |
| I04 | URL o recurso | Protocolo/destino permitido, sin SSRF | URL segura o rechazo |
| I05 | JWT + contexto empresa | Resolver identidad/tenant/membresía en API | Actor autorizado o 403 |
| I06 | ID de borrador | Ownership tenant y permiso | Borrador propio o 404 |
| I07 | Preview | Sin efectos ni persistencia | Vista previa aislada |
| I08 | Guardar | Validar esquema, revisión esperada | Nueva revisión o conflicto |
| I09 | Publicar | Permiso distinto, versión vigente | Publicación auditada o 403/409 |
| I10 | Revertir | Historial íntegro y permiso | Versión restaurada o rechazo |
| I11 | Host/slug público | Resolver binding por backend | Solo proyección pública |
| I12 | Error y auditoría | Minimizar dato, no JWT/secretos | Código de error sin información B |

**Bordes críticos:** navegador→API para permisos; manifiesto→renderer para impedir HTML/JS/SQL/CSS ejecutables; URL→medio para prevenir esquemas peligrosos y SSRF; borrador→publicación para impedir efectos en preview; tenant A→B para negar lecturas/escrituras y cachés cruzadas; versión→persistencia para CAS/revert; error→observabilidad para evitar secretos, tokens o datos privados.

## Fuentes reales leídas en HEAD dev (no pruebas de builder)

| Ruta | Git blob SHA | Evidencia y límite |
| --- | --- | --- |
| frontend/src/app/core/models/tema-visual.model.ts | 9d99a1da70c9c6da408d9a564e91512150b4aedd | TemaVisual y CAMPOS_TEMA de colores, no catálogo builder |
| frontend/src/app/services/theme-applier.service.ts | cbccc2795cb1b010f74ce5b2b4dc1fb20f0b41f3 | Aplica/limpia tema con colores, no sanitiza HTML general |
| frontend/src/app/services/tema-visual.service.ts | 588aa46fc4c10f8d6be85628e919a7bd89f366ac | GET/PUT/restaurar tema, no API publicación de páginas |
| backend/src/API/Controllers/TemaVisualController.cs | a33991fbf941c1fc3048dd30425f6342a0b0c6a4 | Controlador del tema, no ACL general builder |
| frontend/src/app/app.routes.ts | 2f560c14693455b79ac4546e9333fb74586ef234 | Rutas/guards, no RBAC autoritativo |
| frontend/src/styles/_enterprise-design-system.scss | 6a188590af9a958bff2e3e0375c0c46cdb2b14e6 | Tokens/responsive, no editor configurable |

Estas seis rutas fueron inspeccionadas y sus blobs cotejados. Son evidencia limitada de Angular/SCSS/API existentes; ni el modelo TemaVisual ni su operación restaurar garantizan versionado CAS, editor público, renderer de secciones o controles nuevos de servidor.

## Matriz de aceptación FUTURA (no tests ejecutados)

| Positivo | Resultado observable futuro |
| --- | --- |
| P01 | Clave válida resuelve componente Angular fijo |
| P02 | Valores tipados válidos producen preview sin ejecutar scripts |
| P03 | Tenant A autorizado guarda solo borrador A |
| P04 | Publicación de revisión vigente exige permiso y permite reversión |
| P05 | Viewport de 320 px conserva teclado/foco y reduced-motion |

| Negativo | Rechazo futuro exigido |
| --- | --- |
| N01 | script/handler ejecutable |
| N02 | HTML o CSS libre no allowlisted |
| N03 | SQL interpretado como comando (texto inocuo es dato) |
| N04 | javascript:/data: peligrosa o URL de red interna |
| N05 | Payload enorme |
| N06 | Profundidad o sección recursiva excesiva |
| N07 | Componente desconocido |
| N08 | Campo adicional o tipo incorrecto |
| N09 | Usuario A accede a página de B |
| N10 | Usuario sin permiso intenta publicar |
| N11 | Revisión obsoleta pisa nueva sin CAS |
| N12 | Preview dispara publicación/job/webhook |

Los cinco P y doce N son **oráculos para etapas posteriores**, no resultados de pruebas .NET/Angular/Playwright/MySQL ejecutadas aquí. N03 prohíbe interpretar SQL, sin censurar texto inocuo. Se requiere diseño posterior de tamaño máximo, roles, UX y contrato exacto antes de código.

## Dependencias, seguridad, controles A0–A5 y rollback

Preflight: PLAN_MAESTRO!H795=PENDIENTE/I795 vacío, COLA!G795/O795=SI y DAG_TECNICO!A756=C05-03.M01.02/F756=VALIDADO/G756=SI antes de claim exclusivo; C05-01/C05-02, C01/C02 son referencias preparatorias, no liberación automática M01.06/M02–M06. Cambios de esta unidad: **solo docs/v5 y CHANGELOG_AI.md**; sin datos, transacciones, endpoints, migraciones, UI, servicios, JWT/RBAC, Cloudinary, R2 ni tareas programadas.

**A0:** diff Markdown, blobs fuente y ausencia de secretos. **A1:** revisar 12 actores, 12 interfaces, 5 P y 12 N escritos como expectativas futuras. **A2:** exigir tenencia A/B, denegación backend, CSP/sanitización, SSRF, cotas de payload y versionado; no afirmar pruebas runtime. **A3:** coherencia con M01.01, seis blobs y autoridad V5. **A4:** commit fast-forward a dev con Scope Lock y check de procedencia del CHANGELOG en completed/success SHA exacto; readback PLAN/COLA/BITACORA. Compilaciones .NET/Angular y MySQL/Playwright **NO_APLICA** a diff solo Markdown, nunca PASS inferido. **A5:** exclusivamente una de las otras cinco Tasks Javier; no autorrevisión ni promoción QA/main/PROD.

**Rollback:** git revert normal de la entrega documental, sin fuerza ni reescritura; no se toca main, QA, PROD, dominios, infraestructura, pagos o secretos.
