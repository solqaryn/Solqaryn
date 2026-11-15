# SOLQARYN V17 — C13-02.M02.02: contratos API/DTO, errores y validación accesible

**Microtarea:** `C13-02.M02.02` · **PLAN_MAESTRO:** fila 1917 · **autor:** PRIMARY DEV 02, JAVIER_CHATGPT, `6af16bb9492c8191b8e8c7d606f11507` · **Riesgo:** R0 documental sin runtime · **Fecha:** 2026-11-15 · **Rama:** `solqaryn/Solqaryn@dev` · **PROJECT_SCOPE_LOCK:** STRICT.

**Fuente única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), gobernanza V15/V16/V17 y Sheet institucional. **Alcance exacto:** definir contratos futuros de estados accesibles, DTO y mensajes de error, validaciones y compatibilidad entre Angular y API existente. **No** se crean endpoints, DTO de código, componentes, rutas, CSS, guards, cambios de JWT/RBAC, providers, cambios SQL o tests ejecutables. No declarar WCAG 2.2 AA ni accesibilidad normativa por escribir estas especificaciones.

## 1. Precondiciones, fuentes y separación de responsabilidades

- `C13-02.M01.06`, `docs/v5/c13-02-m01-06-gates-rollback-liberacion-accesibilidad.md`, blob Git `1831e51f6718a2fa7be8ea57a9105a98626bf9e1`, define G01–G16, 12 pares P/N futuros, rollback y límites de certificación. Fue aprobado A5 independientemente por DEV06; según PLAN/COLA/DAG, **sólo** habilita preparación M02.
- `C13-02.M01.01–05` contienen no regresiones NR01–NR14, actores A01–A14, superficies I01–I12, riesgos R01–R14, supuestos S01–S10 y decisiones D01–D10. Ninguno demuestra cobertura universal de UI o cumplimiento WCAG sin pruebas observables.
- La M02.01 de entidades/ownership constituye **trabajo independiente** de otra Task; la presente propuesta no presume nombres ni privilegios de un modelo todavía no cerrado. Cada contrato se cotejará con su artefacto definitivo antes de escribir código.
- **Autoridad real:** API ASP.NET Core 10 valida tenant, membresía, JWT/RBAC, recurso y revocación; un Angular guard, rol visual, estado aria, link o DTO de pantalla no concede permiso. Angular 22.2.1/Material/CDK pueden presentar estados accesibles, pero no sustituyen esta frontera.
- Elegibilidad V17 de esta unidad: `PLAN H1917=PENDIENTE/I vacío`, `COLA G1917/O1917=SI`, `DAG_TECNICO F1878=VALIDADO/G1878=SI`, un solo EN_PROGRESO propio, candado `SQV17_C13_02_M02_02` namedRangeId `2000001917`. Habilitación de diseño no autoriza implementación R1/R2, ni QA/main/PROD.

## 2. Ocho operaciones conceptuales para contratos de presentación (NO endpoints nuevos)

| ID | Operación conceptual futura | Contrato de entrada/salida y autoridad |
| --- | --- | --- |
| O01 | ConsultarEstadoVista | Estado de carga/vacío/error/éxito desde API existente; nunca inventar recursos |
| O02 | ConsultarPermisosEfectivos | Código de capacidad mínimo calculado **por backend**, sin exponer claims privados o rol bruto |
| O03 | ValidarFormulario | Errores por campo con identificador estable y texto; no aceptar input con validación solo cliente |
| O04 | ConsultarListadoPaginado | Datos allowlisted, paginación tipada y metadatos para tabla accesible sin exponer tenant B |
| O05 | ResolverAccionPermitida | Acción por recurso/versión/membresía, con respuesta 403/404 redactada si denegada |
| O06 | ConsultarEstadoOperacion | Referencia de operación y correlación opaca; no declarar éxito hasta confirmación server-side |
| O07 | SolicitarCancelacionSegura | Intención de cancelar o volver atrás, sin mutación irreversible desde un control UI |
| O08 | ConsultarRecuperacionError | Mensaje/acción de reintento permitida por causa, sin exponer stack, PII, JWT o secretos |

Estas son **operaciones de diseño**, no rutas REST existentes o autorizadas; la futura implementación deberá mapear cada una a capacidades reales y evitar endpoints redundantes. Estados y errores se presentan en el idioma soportado con mensajes comprensibles; el backend devuelve semántica estructurada y códigos estables en vez de HTML ejecutable.

## 3. Ocho DTO semánticos candidatos (NO tipos compilados)

| DTO futuro | Campos conceptuales | Validación y privacidad |
| --- | --- | --- |
| D01 EstadoVista | `kind`, `titleKey`, `descriptionKey`, `retryAllowed` | kind cerrado loading/empty/error/success/forbidden; no PII |
| D02 AccesoRecurso | `resourceRef`, `canView`, `canEdit`, `reasonCode` | servidor determina permisos; no token/rol interno |
| D03 CampoValidado | `fieldKey`, `errorCode`, `paramsSafe`, `controlId` | etiqueta y error asociado; nunca raw HTML |
| D04 ResultadoFormulario | `submissionRef`, `fieldErrors`, `generalError`, `stateVersion` | éxito únicamente tras operación confirmada |
| D05 ResultadoPaginado | `rows`, `page`, `pageSize`, `totalIfAllowed` | campos y tenant allowlisted, orden accesible |
| D06 AccionPantalla | `actionKind`, `labelKey`, `disabledReason`, `requiresConfirm` | disabled no sustituye autorización server-side |
| D07 EstadoOperacion | `operationRef`, `status`, `confirmedAtUtc`, `correlationId` | no exponer claves idempotencia/PII |
| D08 AvisoAccesible | `severity`, `messageKey`, `focusTarget`, `announcementPriority` | evitar spam de live regions y robo de foco |

No exigir ni crear almacenamiento de estos DTO en MySQL. No persistir etiquetas, estados aria o preferencia visual en tablas de negocio salvo contrato autorizado y política de privacidad. Si hay un estado de progreso externo, no mostrar éxito ficticio ni spinner eterno.

## 4. Doce errores seguros y acciones de recuperación propuestas

| Error futuro | Causa o frontera | Comportamiento accesible esperado |
| --- | --- | --- |
| X01 INPUT_REQUIRED | campo requerido ausente | texto/aria-describedby y foco al primer error |
| X02 INPUT_OUT_OF_RANGE | límite numérico, fecha o tamaño | rango legible; no corregir silenciosamente |
| X03 INPUT_FORMAT_INVALID | formato no parseable | ejemplo localizado, sin eco de datos sensibles |
| X04 AUTH_REQUIRED | sesión ausente/expirada | solicitar autenticación con retorno seguro |
| X05 PERMISSION_DENIED | rol/membresía revocada o recurso B | 403/404 sin enumeración; foco en encabezado |
| X06 RESOURCE_NOT_FOUND | ID ausente o no visible | mensaje neutral, enlace de regreso accesible |
| X07 CONFLICT_VERSION | CAS/edición concurrente | avisar conflicto sin sobrescribir otros cambios |
| X08 RATE_LIMITED | cuota excedida | demora/reintento permitido sin loop automático |
| X09 NETWORK_UNAVAILABLE | offline/timeout | conservar datos locales no sensibles, permitir retry |
| X10 SERVICE_UNAVAILABLE | error servidor/dependencia | sin stack ni éxito falso, correlación opaca |
| X11 OPERATION_IN_PROGRESS | confirmado sólo como pendiente | anunciar progreso y estado verificable, no éxito |
| X12 OPERATION_UNCERTAIN | respuesta ambigua/no durable | evitar doble-submit, consulta segura antes de repetir |

Los códigos son **propuestos**, no afirmaciones de que existan actualmente. Exigir API status exactos y contratos internos en M03/M04 antes de implementación; respuestas del servidor no deben devolver email, JWT, stack traces o recurso de otro tenant.

## 5. Dieciséis validaciones y compatibilidad negativa

- **V01:** `kind`, `severity`, `actionKind` y `status` deben ser enums cerrados; valores desconocidos -> estado de error seguro, nunca éxito.
- **V02:** usar claves de traducción allowlisted, sin interpolar HTML ni interpretar markdown no confiable.
- **V03:** cada campo inválido debe relacionar etiqueta, mensaje y control `id` estable, incluidas validaciones del servidor.
- **V04:** al enviar con errores, mover foco al resumen o primer error sin crear trampa ni moverlo repetidamente.
- **V05:** en dialogs/menus, restaurar foco al elemento disparador cuando exista; no dejar foco detrás de overlay.
- **V06:** no asumir que `aria-disabled` impide una acción ni que ocultar el botón equivale a RBAC real.
- **V07:** toda referencia a recurso/tarea/tenant se valida en servidor contra JWT, membresía activa y ownership por Empresa.
- **V08:** el caso 403/404 no filtra títulos, rutas, identificadores, PII o permisos del tenant ajeno.
- **V09:** paginación exige `page>=1`, tamaño acotado, filtrado y orden allowlisted; no permitir consultas sin límite.
- **V10:** estado `success` solo cuando respuesta durable verifica resultado; `in_progress` y timeout no se transforman a éxito.
- **V11:** reintentos no ejecutan doble mutación: idempotencia/correlación verificables en backend cuando sean aplicables.
- **V12:** errores, anuncios y notificaciones usan texto conciso y no interrumpen repetidamente lector de pantalla; `aria-live` según gravedad.
- **V13:** interfaz con zoom/reflow 320/390/768/1280 no debe ocultar acciones críticas, ni cambiar orden de lectura por CSS sin control.
- **V14:** temas, forced-colors, foco visible y reduced-motion exigen evidencia visual/humana; no se certifican desde tokens de estilo.
- **V15:** respuesta `loading`, `empty`, `error`, `forbidden` y `success` deben distinguirse de manera no exclusivamente cromática.
- **V16:** reversión/cancelación conserva formularios y evidencia no sensible; jamás deshabilitar JWT/RBAC ni forzar flags por accesibilidad.

**Compatibilidad:** no cambiar contratos JSON reales sin inventario de consumidores, versión/migración y regresión. Diseñar `displayKey` internacionalizable, pero no suponer que todos los locales están implementados.

## 6. Doce pares positivos/negativos FUTUROS_NO_EJECUTADOS

| Par | Positivo futuro medible | Negativo futuro que debe fallar |
| --- | --- | --- |
| P01/N01 | formulario anuncia requerido y foco | error sólo color o sin asociación |
| P02/N02 | tab/shift-tab recorre acción visible | control solo mouse o foco perdido |
| P03/N03 | diálogo conserva/restaura foco | trampa permanente/fondo activo |
| P04/N04 | listado A paginado accesible | tenant B o página arbitraria expone datos |
| P05/N05 | mensaje 403 es neutral y localizable | PII o título de recurso B filtrado |
| P06/N06 | timeout conserva estado honesto | error de red muestra éxito falso |
| P07/N07 | acción confirmada incluye correlación válida | doble clic dispara dos efectos |
| P08/N08 | 320px y zoom mantienen acciones | reflow corta botón o tabla no operable |
| P09/N09 | error se anuncia al lector sin ruido | live region repetida en bucle |
| P10/N10 | reduced-motion respeta preferencia | animación imprescindible o contenido perdido |
| P11/N11 | rol autorizado ejecuta acción con servidor | bypass guard Angular concede API |
| P12/N12 | revocación inmediata quita acción | sesión revocada o recurso B permite mutar |

**Pruebas ejecutadas aquí:** `0/12` pares; browser, lector real, UI runtime, WCAG y latencia = `NO_MEDIDO`. Son oráculos a convertir en Vitest/Playwright/Chromium y tests de API en fases de implementación. Una especificación textual no certifica WCAG.

## 7. Impacto, rollback y A0–A5 proporcional

- **Domain/Application:** solo especificación de semántica y errores; no se crean modelos ni servicios. **API:** autoridad tenant/JWT/RBAC real debe verificarse en fases R2 de código. **Angular:** ninguna vista, guard, estilo o interacción se cambia ahora. **MySQL/EF:** NO_APLICA por diff solo Markdown.
- **A0:** criterios M02.02, M01.06+A5, PLAN/COLA/DAG y claim/candado exclusivo `2000001917`; diff únicamente este documento + encabezado/viñetas del CHANGELOG.
- **A1:** revisión de O01–O08/D01–D08/X01–X12/V01–V16, casos positivos/negativos escritos; **no** se atribuye PASS de comportamiento ejecutado.
- **A2:** diseño estático de permiso backend, aislamiento A/B, revocación, no PII, errores seguros, accesibilidad/guard y estado honesto; no equivalencia a pruebas de seguridad runtime.
- **A3:** integración .NET/HTTP/MySQL/Angular/Playwright/lector `NO_APLICA` por naturaleza documental, no se presenta como PASS.
- **A4:** publicación fast-forward contra HEAD dev actual, checks exactos `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` en SHA causal o equivalencia auditada de blobs, PLAN/COLA/DAG/BITACORA readback.
- **A5:** revisor entre las otras cinco Tasks Javier, TASK_ID distinto de DEV02, revisa contrato/diff/checks y puede fallar; `LISTO_DEV` no equivale a A5.
- **Rollback:** `git revert` normal y dirigido, jamás force-push/reset, borrar BITACORA ni modificar archivos/estado de otra Task. **Sin QA/main/PROD, secretos, workers, proveedores, datos reales o cambio de agendas.**

**Salida:** diseño de contratos de accesibilidad R0, no implementación funcional ni certificación WCAG. Todos los futuros controles requieren pruebas y decisión de autoridad antes de promoción.
