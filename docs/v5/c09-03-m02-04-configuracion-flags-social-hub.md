# SOLQARYN V17 — C09-03.M02.04: configuración tipada, ámbitos y feature flags seguros (Social Hub)

**Autor:** SOLQARYN PRIMARY DEV05 Javier, `6adc2e55f47c81919f6374e561687076`. **Riesgo:** R0 documental; no modifica configuración de runtime ni añade claves reales. **Repo:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`.
**Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, PLAN_MAESTRO fila 1379, COLA fila 1379, DAG_TECNICO fila 1340, contrato M01 y C09-03.M02.01/M02.02; sólo esta microtarea, sin crear tareas/fases externas. **Estado:** configuración propuesta a implementar y probar después en fases autorizadas; no es una implementación certificada.

## A0. Alcance y políticas invariantes

Social Hub y campañas necesitan separación de `EmpresaId`, canal, finalidad, sucursal cuando corresponda, roles, consentimiento, opt-out y proveedor. El ámbito autorizante **nace en el backend**, de identidad/tenancy verificadas y permisos relacionales vigentes; no aceptar EmpresaId, usuario o rol del payload para asignar privilegios. Las opciones de interfaz son pistas visuales, nunca autorizaciones. La ausencia de configuración, una opción desconocida, un proveedor no validado o un estado ambiguo resultan en `DENY`, sin envío de mensajes ni procesamiento remoto.

El catálogo que sigue es un **contrato tipado futuro**. Ninguna variable, flag, secreto, endpoint, proveedor, tabla, precio, cuenta externa o dependencia se activa al añadir esta documentación. Mantener API/Application/Domain/Infrastructure/Angular existentes intactos hasta aprobación por unidad real.

## Doce opciones tipadas propuestas

| Código / clave conceptual | Tipo y valor inicial propuesto | Ámbito y decisión |
| --- | --- | --- |
| F01 SocialHubEnabled | boolean = false | plataforma: módulo deshabilitado hasta gate posterior |
| F02 CampaignDraftEnabled | boolean = false | empresa: crear borradores sólo con RBAC del backend |
| F03 CampaignApprovalEnabled | boolean = false | empresa: aprobación con rol separado de edición |
| F04 CampaignSendEnabled | boolean = false | plataforma + empresa + canal: envío externo cerrado |
| F05 SegmentBuilderEnabled | boolean = false | empresa: criterios permitidos, sin SQL/JS del usuario |
| F06 ConsentCaptureEnabled | boolean = false | empresa + propósito/canal: prueba y revocación obligatorias |
| F07 ConsentStrictEnforcement | boolean = true | plataforma: nunca desactivar comprobación de consentimiento |
| F08 OptOutStrictEnforcement | boolean = true | plataforma: baja prioritaria, no anulable por snapshot |
| F09 CalendarSchedulerEnabled | boolean = false | plataforma + empresa: sin activar schedulers/Tasks |
| F10 ProviderDispatchEnabled | boolean = false | plataforma + proveedor allowlist: sin invocar externos |
| F11 CampaignMetricsEnabled | boolean = false | empresa: agregación con cohorte mínima y RBAC |
| F12 ExternalMediaEnabled | boolean = false | empresa + canal: permitir sólo referencias propias validadas |

**No existen por esta entrega F01–F12 en código ni en variables de entorno.** Los dos controles estrictos F07/F08 son invariantes conceptuales de seguridad y no pueden hacerse opcionales por configuración de una empresa, rol administrador o fallo de red. En particular F04, F09, F10 y F12 sólo podrían cambiar a true tras autorización explícita para el componente real, revisión de consentimiento, límites de proveedor, integraciones, costos y CI pertinente; no se infiere permiso productivo.

## Tipos y precedencia propuestos (no nuevos DTO ejecutables)

`SocialHubConfigSnapshot` conceptual: `Revision` no vacía, `Source` opaca, `EvaluatedAtUtc`, `ExpiresAtUtc`, valores F01–F12 tipados, `TenantScope` derivado del servidor, `ChannelPurpose` allowlist y `PolicyVersion`. Su uso requiere firma/autenticidad o carga desde fuente confiable; un objeto arbitrario de frontend no es seguro. Nunca guardar `AccessToken`, destinatarios o claves de proveedor en snapshots/telemetría.

Precedencia restrictiva: **deny global / bloqueo de seguridad > revocación y opt-out > deny de empresa > deny de canal/proveedor > falta de rol/pertenencia > habilitaciones específicas**. Una habilitación específica no puede revocar un bloqueo anterior. La evaluación debe fallar si faltan fuente confiable, revisión, fecha, zona, propósito o permisos. El resultado de evaluar flags se vuelve a consultar antes de cualquier efecto externo, junto con consentimiento y pertenencia. Los valores antiguos y las cachés caducadas nunca conceden permisos; no permitir toggles basados sólo en el cliente.

Tiempos: UTC, expiración explícita y revisión monotónica. Concurrencia: `IfMatch`/CAS de revisión en la configuración futura, no sobrescribir una actualización de seguridad reciente. Rollback: reponer `false` para operaciones de envío/worker sin borrar auditoría, consentimiento ni opt-out; nunca reintentar automáticamente mensajes con efecto ambiguo. Observabilidad: código de decisión, traceId opaco y versión de política; nunca datos sensibles o IDs de destinatarios.

## Reglas de rechazo por fronteras

| Invariante | Límite que debe imponerse al implementar |
| --- | --- |
| P01 | falta EmpresaId autenticada = no concesión |
| P02 | empresa B no hereda flags o proveedores de A |
| P03 | rol expirado/revocado invalida capacidad aunque flag siga true |
| P04 | API backend decide; frontend puede ocultar, nunca elevar |
| P05 | configuración ausente/desconocida/mal tipada = closed |
| P06 | cualquier bandera de envío false bloquea el efecto |
| P07 | opt-out gana frente a calendario, retry y consentimiento anterior |
| P08 | ConsentStrictEnforcement y OptOutStrictEnforcement nunca se apagan por tenant |
| P09 | flags de proveedores deben usar catálogo explícito y permisos efectivos |
| P10 | contenido/medio con owner ajeno o URL SSRF no se publica |
| P11 | revisión/etag obsoletos no sustituyen configuración actual |
| P12 | timeout o fallo de origen de configuración no activan fallback permisivo |

## Matriz de doce pares de aceptación futuros — NO EJECUTADOS

| Caso | Positivo para pruebas posteriores | Negativo que debe fallar |
| --- | --- | --- |
| T01 | empresa A lee snapshot autorizado A | token de B intenta reutilizar snapshot A |
| T02 | rol de editor permite borrador si F02 verdadero | rol revocado intenta crear |
| T03 | aprobador autorizado distinto del editor | misma identidad intenta autoaprobar |
| T04 | aprobación y consentimiento vigentes permiten intención | F04 false intenta enviar |
| T05 | segmento allowlist limita criterios | SQL/JS arbitrario o segmento de B |
| T06 | prueba de consentimiento vigente por canal | consentimiento de otro propósito/canal |
| T07 | opt-out bloquea reenvíos | snapshot/cache vieja reactiva baja |
| T08 | calendario UTC versionado registra intención | scheduler deshabilitado o instante vencido |
| T09 | proveedor allowlist válido y F10 true | proveedor inexistente o URL externa arbitraria |
| T10 | medio propio saneado disponible | mediaId B o URL SSRF |
| T11 | métrica agregada cohorte permitida | consulta de cohortes pequeñas/PII |
| T12 | rollback detiene nuevo efecto preservando registro | timeout/replay produce segundo envío |

**Resultado real:** 0/12 pares ejecutados. No hay pruebas .NET/xUnit, API/HTTP/JWT, MySQL 8.4/EF, Angular/Playwright, envío, Cloudinary, SMTP ni proveedor externo ejecutadas por este documento. Todos esos escenarios se deberán acreditar en futuras microtareas de código/seguridad que efectivamente cambien runtime.

## A0–A5, dependencias y rollback

A0: el diff de esta microtarea debe limitarse a este Markdown y una sección indivisible de `CHANGELOG_AI.md`; leer PLAN/COLA/DAG actuales, autoría y Scope Lock. A1: revisión estática F01–F12, P01–P12 y T01–T12 con rechazo adversarial conceptual; no confundir texto con test PASS. A2: separación A/B, JWT/RBAC, revocación, consentimiento, opt-out, SSRF, idempotencia y logs diseñados fail-closed; son requisitos futuros, no controles nuevos en ejecución. A3: MySQL/EF, xUnit, UI/browser y proveedores **NO_APLICA R0** porque no se modifica código, esquema, endpoints ni despliegues. A4: fast-forward CAS en dev, Scope Lock y V5 provenance `completed/success` exact SHA, readback institucional y BITACORA idempotente, `VALIDANDO` mientras CI obligatoria esté en curso. A5: sólo revisión independiente de otra Task Javier, con posibilidad real de rechazar. No habilitar sucesores técnicos por VALIDANDO ni atribuir A5 propia.

Rollback autorizado, si fuera necesario, mediante nuevo `git revert` no destructivo después de validar HEAD, preservar BITACORA y fuentes institucionales; no force-push ni reemplazo de datos legítimos. Toda activación futura de proveedor o envío de campañas necesita permisos nuevos y seguros, sin acceso a QA/main/PROD bajo este alcance.
