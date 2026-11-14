# SOLQARYN V5 — C09-03.M01.04: riesgos, supuestos y decisiones abiertas del Social Hub

**Unidad:** C09-03.M01.04 · `PLAN_MAESTRO` fila 1373 · Autor PRIMARY DEV 02 JAVIER, TASK_ID `6af16bb9492c8191b8e8c7d606f11507` · Fecha 2026-11-14 · `solqaryn/Solqaryn@dev` · `PROJECT_SCOPE_LOCK=STRICT`.

**Fuente ejecutable:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. Esta M01.04 cumple «Registrar riesgos, supuestos y decisiones pendientes» del lote Social Hub y campañas. El ámbito FUTURO C09-03 incluye contenido, segmentos, calendarios y métricas, evidencias de consentimiento, opt-out y límites. **Este documento no declara vulnerabilidades presentes ni defectos probados en el código; registra hipótesis de riesgo y decisiones todavía abiertas.**

**Preflight y exclusividad:** PLAN H1373=PENDIENTE, owner vacío; COLA G1373/O1373=SI; DAG F1334=VALIDADO/G1334=SI (preparación documental) antes del único CAS. No autoriza M03 runtime, IAM/JWT, R2, migraciones, webhooks, servicios externos, QA/main/PROD ni A5 propio.

## Evidencia original y frontera de implementación

| Artefacto real leído | Blob | Límite para este análisis |
| --- | --- | --- |
| `docs/v5/c09-03-m01-01-alcance-social-hub-campanas.md` | `200a942044bac2b5c0a294ecacdf0ab75068a9ce` | NR01-NR16 y objetivo futuro, no producto funcional |
| `docs/v5/c09-03-m01-02-actores-interfaces-social-hub-campanas.md` | `4e2458b7dec1018de610b485c5aedf8fbf8f3f20` | A01-A14/I01-I14/F01-F10 conceptuales |
| `docs/v5/c09-03-m01-03-inventario-codigo-brechas-social-hub-campanas.md` | `04d2fd108d784449304a6c34ffc690bf1237832d` | 14 rutas reales, G01-G14 brechas de evidencia, no defectos certificados |
| `backend/src/Domain/Entities/Cliente.cs` | `8c6d4c4453e44f8c0be9475340465e1aadb3f787` | EmpresaId nullable legacy, no opt-in universal |
| `backend/src/Domain/Entities/UsuarioEmpresa.cs` | `f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c` | Membresía, no rol de campañas inferible |
| `backend/src/Domain/Entities/MensajeOutbox.cs` | `891572626c19cd6cf8f522fca5ec876333286ffb` | Intención durable, no proveedor/dispatcher marketing probado |
| `frontend/src/app/core/guards/permiso.guard.ts` | `1052bd38041055cc81e84a14d5fe0364e169b1fe` | Guarda UI, no autorización API |

Los blobs se cotejaron con GitHub `dev` en las unidades preparatorias y el inventario inmediato; la inexistencia de una función no se deduce de una lista parcial. El análisis no usa datos reales de contactos, secretos ni servicios externos.

## R01–R14: riesgos POTENCIALES, no incidentes

| ID | Riesgo hipotético | Condición desencadenante | Control exigible en una implementación futura | Evidencia/prueba futura |
| --- | --- | --- | --- | --- |
| 01 | Consentimiento desactualizado | Programación previa a baja/opt-out | Consultar preferencia vigente antes de efecto y registrar cambio | Prueba negativa: opt-out entre programación y ejecución |
| 02 | Acceso cruzado entre empresas | Segmento/campaña de B referenciado desde A | Validar tenant de recurso y usuario server-side | Prueba A/B con ID ajeno y sin filtración |
| 03 | Propiedad NULL heredada | Cliente sin EmpresaId verificable | Fail-closed hasta reconciliación auditable | Caso Cliente.EmpresaId NULL denegado |
| 04 | Elevación de privilegios UI | Guard Angular sustituido por permiso real | RBAC/membresía vigentes en API por acción | Petición HTTP directa con rol lector denegada |
| 05 | Ausencia de finalidades explícitas | Contacto existente tratado como opt-in marketing | Consentimiento tipado por finalidad/canal | Contacto con compra previa pero sin opt-in denegado |
| 06 | Consulta arbitraria de segmentos | Filtro dinámico ejecuta SQL/JS | Campos y operadores allowlist; límites | Payload con expresión arbitraria rechazado |
| 07 | Doble publicación/replay | Duplicación por retry o doble clic | Idempotencia tenant+campaña+destino y transición atómica | Dos intents concurrentes dan un efecto |
| 08 | Publicación de contenido nocivo | HTML, enlace o media no autorizados | Validación de MIME, URLs, XSS, ownership | Contenido con script/SSRF rechazado |
| 09 | Secretos de proveedor expuestos | Token OAuth/SMTP accidental en log o repo | Secret reference, redacción y rotación | Escaneo y log sin credenciales |
| 10 | Métricas reidentificables | Filtros de pequeños grupos revelan PII | Umbrales, agregación y controles de exportación | Query de cardinalidad baja denegada |
| 11 | Cuota/rate limit omitidos | Ráfaga externa tras backoff | Cuota por empresa/proveedor, retry acotado | 429 simulado sin bucle infinito |
| 12 | Falta de auditoría de aprobaciones | Publicación sin actor/correlación | Historia append-only, aprobador y versionado | Diff y eventos trazan decisión |
| 13 | Dependencia no desplegada | Outbox o canal transaccional confundido con marketing | Separar contratos y autorización comercial | Preflight deniega emisión sin proveedor habilitado |
| 14 | Reversión incompleta | Campaña detenida borra datos ajenos o reenvía | Desactivación sin eliminar evidencia y sin nuevas emisiones | Rollback de función preserva auditoría |

**Criterio de severidad cualitativa:** cualquier fuga tenant A/B, envío sin consentimiento, bypass JWT/RBAC, secreto filtrado o doble efecto comercial es bloqueante hasta demostrarse el control y la prueba negativa. No existe aquí valoración CVSS ni auditoría de explotación; una categoría de riesgo no es una falla detectada.

## S01–S08: supuestos NO_VERIFICADOS

| ID | Hipótesis | Estado y fuente faltante | Tratamiento conservador |
| --- | --- | --- | --- |
| S01 | Habrá política de consentimiento/finalidades por canal | No confirmada; preguntar arquitectura de preferencias | Requerir contrato antes de M02 |
| S02 | El rol marketing será granular y empresarial | No confirmado; RBAC actual no implica rol nuevo | Validar permisos reales con código |
| S03 | Habrá proveedor social autorizado y límites de cuota | No confirmado; no configurar credenciales | Mantener proveedor deshabilitado |
| S04 | Se podrá vincular Cliente a segmento con tenant explícito | No confirmado por registros legacy NULL | Conservar fail-closed |
| S05 | El planificador podrá tener estados auditables idempotentes | No confirmado, M01 sólo preparación | No crear job en esta fase |
| S06 | Las métricas agregadas tendrán umbral de privacidad | No hay umbral contractual probado | Definir con seguridad/legal en M02 |
| S07 | El contenido podrá reutilizar sistema de medios vigente | No se verificó seguridad/media cross-tenant | Exigir inspección de adaptador |
| S08 | Los sistemas externos podrán dar ACK/reintentos limitados | No certificado, depende de proveedor | Pruebas de fallo efímero futuro |

Los supuestos no son órdenes de contratación, cambios de proveedor o permisos. Se mantienen falsables y bloquean su uso como hechos mientras no exista evidencia institucional.

## D01–D08: decisiones PENDIENTES

| ID | Decisión requerida en fase posterior | Estado / bloqueo actual | Autoridad técnica/comercial responsable |
| --- | --- | --- | --- |
| D01 | Definir modelo de consentimiento, finalidad, canal y opt-out | Sin decisión; bloqueo para envío | Propietario+arquitectura en M02 |
| D02 | Definir matriz RBAC por crear/editar/aprobar/publicar/exportar | Sin decisión; denegar ejecución | Seguridad/propietario |
| D03 | Definir fuente de verdad de segmentos y regla de tenant NULL | Sin decisión; no exportar PII | Backend/data |
| D04 | Definir política de contenido externo, XSS, URL y medios | Sin decisión; no publicar contenido | Seguridad/frontend |
| D05 | Definir contrato de programación, idempotencia y cancelación | Sin decisión; no workers | Backend/arquitectura |
| D06 | Aprobar proveedores, costos, cuotas, credenciales y canales | Sin decisión; cero activación de proveedores | Propietario explícito |
| D07 | Fijar umbrales de agregación, retención y auditoría | Sin decisión; métricas futuras | Privacidad/seguridad |
| D08 | Diseñar plan reversible y pruebas integración / negativas | Sin decisión; sin A5 ni liberación | QA/engineering |

Ninguna decisión pendiente se interpreta como una solicitud para modificar contratos actuales. Hasta decisión verificable, mantener funciones no desplegadas, sin autorización de proveedor ni envío.

## Escenarios futuros de verificación positiva y negativa — NO EJECUTADOS

| Caso | Escenario | Resultado exigible |
| --- | --- | --- |
| P01 | Autor empresarial propone borrador legítimo en su tenant | Documento versionado sin publicar |
| P02 | Consentimiento específico vigente por finalidad/canal | Elegibilidad y comprobación auditada |
| P03 | Opt-out llega antes de programación | Se cancela intención pendiente |
| P04 | Reintento con clave idempotente válida | Se observa un único efecto lógico |
| P05 | Métricas de campaña propia y cardinalidad adecuada | Agregados permitidos, no PII |
| P06 | Desactivar campaña por operador autorizado | Sin nuevas emisiones, preserva auditoría |
| N01 | Tenant A usa ID de campaña/segmento de B | Denegación sin filtración |
| N02 | Token expirado o membresía revocada | Denegación en backend |
| N03 | Cliente EmpresaId NULL no reconciliado | Sin inferencia de propiedad global |
| N04 | Sin consentimiento / opt-out confirmado | Rechazar publicación/envío |
| N05 | Doble clic, retry simultáneo o replay malicioso | Dedupe y auditoría |
| N06 | Inyección SQL/JS/XSS, SSRF o media de B | Rechazo del contenido |
| N07 | Falta credencial o proveedor supera cuota | Fallo seguro, retries limitados |
| N08 | Exportación segmentada capaz de revelar B/PII | Denegación con respuesta segura |
| N09 | Usuario lector intenta publicar vía HTTP | Denegación por RBAC backend |
| N10 | Cancelación en paralelo con intento de emisión | No se envía tras revocación |

Todos los P01–P06 y N01–N10 son `FUTUROS_NO_EJECUTADOS`. No se ejecutaron dotnet tests, HTTP, MySQL, Playwright ni pruebas de proveedor específicas de campañas; no se atribuye PASS de runtime. El contrato permite diseñar M02 posteriormente bajo DAG y A5 causales.

## Impacto y gates de cierre documental

- **Archivos afectados:** sólo este `docs/v5/` y una sección autocontenida del `CHANGELOG_AI.md` por fast-forward CAS. Sin alteración del backend, frontend, base de datos, JWT/RBAC, secretos, usuarios, QA/main/PROD, cuentas o automatizaciones.
- **A0:** trazabilidad de tres M01 precedentes, blobs reales, supuestos separados de defectos; ausencia de encabezados vacíos.
- **A1:** comprobar presencia de R01-R14, S01-S08, D01-D08 y matriz P/N descriptiva. Pruebas de runtime NO_APLICA por cambios sólo Markdown.
- **A2:** validar enfoque fail-closed sobre tenant A/B, JWT/RBAC, consentimiento/opt-out, replay, PII y secretos, sin declarar cobertura ejecutada.
- **A3:** integración documental con contratos existentes y consumidores futuros, sin afirmar proveedores activos.
- **A4:** comprobar SHA exacto del commit, diff/blobs de dos archivos, `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success`; readback PLAN/COLA/BITACORA.
- **A5:** revisión distinta del Task autor entre las otras cinco Javier; independiente y pendiente. No habilita promoción o cambios de infraestructura.
- **Rollback:** revertir el commit mediante nuevo `git revert` sin reset/force-push y sin alterar BITACORA histórica.
