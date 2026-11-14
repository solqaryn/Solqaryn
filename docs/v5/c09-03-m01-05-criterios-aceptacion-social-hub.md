# SOLQARYN V5 — C09-03.M01.05: criterios de aceptación y pruebas futuras del Social Hub

**Microtarea:** C09-03.M01.05 · PLAN_MAESTRO fila 1374 · Autor: PRIMARY DEV 02 JAVIER, TASK_ID `6af16bb9492c8191b8e8c7d606f11507` · Fecha 2026-11-14 · `solqaryn/Solqaryn@dev` · `PROJECT_SCOPE_LOCK=STRICT`.

**Autoridad ejecutable:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; salida de M01.05: «Definir aceptación funcional y técnica observable». El futuro C09-03 incluye campañas, contenido, segmentos, calendarios y métricas con consentimiento, límites y opt-out. Este documento sólo establece criterios reproducibles **para fases siguientes**, NO acredita Social Hub implementado ni tests aprobados.

**Preflight:** `PLAN_MAESTRO!H1374=PENDIENTE` sin responsable, `COLA!G1374/O1374=SI`, `DAG_TECNICO!F1335=VALIDADO/G1335=SI` antes del claim exclusivo. Esta habilitación documental no licencia un dispatcher, proveedor, datos reales, JWT/RBAC nuevos, M01.06, QA/main/PROD o cambios de cuentas.

## Trazabilidad de preparación y evidencia

| Fuente previa verificada en dev | Blob SHA | Cobertura y límite |
| --- | --- | --- |
| `docs/v5/c09-03-m01-01-alcance-social-hub-campanas.md` | `200a942044bac2b5c0a294ecacdf0ab75068a9ce` | Alcance futuro, NR01–NR16 y exclusiones |
| `docs/v5/c09-03-m01-02-actores-interfaces-social-hub-campanas.md` | `4e2458b7dec1018de610b485c5aedf8fbf8f3f20` | A01–A14, I01–I14, F01–F10 conceptuales |
| `docs/v5/c09-03-m01-03-inventario-codigo-brechas-social-hub-campanas.md` | `04d2fd108d784449304a6c34ffc690bf1237832d` | 14 rutas de código original y G01–G14 preguntas, sin inventario total |
| `docs/v5/c09-03-m01-04-riesgos-supuestos-decisiones-social-hub.md` | `cb112cc670b49365bcd5f4aec700fc124a007a52` | R01–R14 potenciales, S01–S08 no verificados, D01–D08 pendientes |
| `backend/src/Domain/Entities/Cliente.cs` | `8c6d4c4453e44f8c0be9475340465e1aadb3f787` | Cliente.EmpresaId nullable legado, no consentimiento acreditado |
| `backend/src/Domain/Entities/UsuarioEmpresa.cs` | `f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c` | Membresía, no permiso marketing automático |
| `backend/src/Domain/Entities/MensajeOutbox.cs` | `891572626c19cd6cf8f522fca5ec876333286ffb` | Intención durable, no envío ni worker de campaña habilitados |

La lectura es estática. La evidencia no autoriza afirmar que las APIs, campañas, segmentos o métricas existan actualmente ni permite tratar ausencia de prueba como ausencia definitiva de código.

## Matriz AC01–AC16 — aceptación FUTURA, NO EJECUTADA

Cada AC incluye una condición positiva P y un caso negativo N que deberán ejecutarse y guardarse con SHA, datos efímeros aislados, aserciones explícitas y logs sin PII cuando exista implementación real. **Ninguna fila es un PASS de la corrida documental.**

| AC | Capacidad | Positivo futuro P | Negativo futuro N |
| --- | --- | --- | --- |
| 01 | Borrador autorizado | Editor con JWT, membresía y permiso válido crea borrador de su empresa | Editor intenta crear borrador indicando EmpresaId de otro tenant |
| 02 | Edición controlada | Edición propia usa versión esperada y auditoría de actor | Versión obsoleta intenta sobrescribir edición posterior |
| 03 | Aprobación formal | Aprobador empresarial distinto puede aprobar versión vigente | Usuario lector/editor sin facultad aprueba por API directa |
| 04 | Publicación autorizada | Publicación futura requiere autorización backend y estado aprobado | Manipulación de guard Angular pretende publicar sin permiso |
| 05 | Segmentación tipada | Reglas allowlist se evalúan sólo con contactos permitidos | Campo SQL/JS arbitrario o script enviado como criterio |
| 06 | Consentimiento específico | Campaña por canal/finalidad utiliza opt-in acreditado | Se infiere consentimiento de una compra o simple contacto |
| 07 | Opt-out prioritario | Revocación evita efectos posteriores incluso si hay calendario | Programación anterior pretende enviar tras revocación |
| 08 | Aislamiento de datos | Una campaña sólo conoce miembros explícitamente del mismo tenant | ID conocido de cliente del tenant B en segmento A |
| 09 | Legacy nullable | Cliente EmpresaId NULL se bloquea hasta reconciliación | NULL tratado como acceso global o vínculo ambiguo |
| 10 | Idempotencia | Repetición de clave acotada a empresa/campaña/destino produce un efecto | Dos reintentos concurrentes duplican publicación |
| 11 | Contenido seguro | Pieza y recursos se validan contra MIME, enlace y ownership | XSS, media cross-tenant, SSRF o redirect inseguro |
| 12 | Cuotas y proveedor | Canal futuro aprobado respeta cuota y backoff acotado | Proveedor fallido dispara retry infinito o pérdida de trazabilidad |
| 13 | Credenciales protegidas | Referencias opacas de secretos evitan exposición en repositorio/log | OAuth/HMAC/SMTP token aparece en documento o respuesta |
| 14 | Métricas privadas | Agregados de su tenant respetan un umbral de privacidad definido | Filtro de cardinalidad baja permite inferir datos de B |
| 15 | Auditoría íntegra | Estado de campaña registra actor/tenant/origen/correlación sin PII excesiva | Cambio sin actor o log expone emails/tokens de clientes |
| 16 | Reversibilidad | Desactivación cancela efectos posteriores sin borrar historial | Rollback reenvía campañas, borra auditoría o cambia ventas/créditos |

La implementación posterior no aprobará un AC por un mock superficial si el efecto depende de HTTP, MySQL, API, provider o control de concurrencia real. Casos de riesgo alto (tenant, revocación, idempotencia, credenciales) deberán cubrir escenarios negativos reales sobre la frontera backend y, cuando aplique, integración con servicio efímero.

## Requisitos no funcionales RNF01–RNF10

| ID | Categoría | Criterio de demostración futura |
| --- | --- | --- |
| RNF01 | Separación tenant A/B | Acceso cruzado siempre denegado por servidor, aunque IDs o headers se manipulen |
| RNF02 | Autorización granular | JWT, membresía activa, RBAC por acción; guard UX no autoriza |
| RNF03 | Privacidad y minimización | Sólo datos imprescindibles, retención y umbrales definidos antes de uso |
| RNF04 | Consentimiento verificable | Finalidad, canal, fecha, revocación y origen auditable |
| RNF05 | Idempotencia y concurrencia | Sin doble publicación ante retry, replay o clic simultáneo |
| RNF06 | Disponibilidad degradada segura | Provider caído/cuota agotada no envía ni reintenta indefinidamente |
| RNF07 | Observabilidad | Logs estructurados por tenant/correlation ID sin secretos ni PII plena |
| RNF08 | Accesibilidad Angular | Teclado, labels, foco, contraste, errores y responsive real probados |
| RNF09 | Rendimiento sostenible | Paginación y filtros indexables con umbrales medidos, sin cifras inventadas |
| RNF10 | Rollback verificable | Desactivar funciones preserva historia e impide nuevos efectos, sin fuerza sobre main/PROD |

No se definen arbitrariamente latencias, cuotas, tamaños máximos o SLA sin carga, política comercial ni ambiente de prueba medidos. Las metas cuantitativas y decisiones D01-D08 siguen abiertas hasta aprobación correspondiente.

## Pruebas A1–A3 por tecnología futura

| Área | Evidencia exigible al implementar | Qué NO se realizó ahora |
| --- | --- | --- |
| .NET 10/Application/API | xUnit de validación positiva/negativa, auth A/B, caducidad, versión, consentimiento y estado | No se compilaron nuevos endpoints ni se ejecutó suite de campaña |
| MySQL 8.4/EF Core 10 | Índices/constraints y transacciones con servicio de CI efímero, rollback, carreras/idempotencia | No hubo migración, DB viva ni benchmark |
| Angular 22.2.1 | Formularios, estados, focus management, permisos y navegación, pruebas de componente y E2E | No se modificó UI ni se ejecutó Playwright del módulo |
| Proveedor/outbox | Contrato, retry, cuotas, HMAC, replay, error/ACK sin secretos | No se invocó SMTP, WhatsApp, API social, worker ni webhook |
| Seguridad/privacidad | Logs sanitizados, matriz de permisos, aislamiento tenant y opt-out probado | No se acreditaron consentimientos ni datos reales |

Estas pruebas quedan `FUTUROS_NO_EJECUTADOS`; las compilaciones generales de código vigente no pueden sustituirlas. Backend debe rechazar toda acción no autorizada independientemente de guardas Angular o cabeceras solicitadas.

## Gates, límites y rollback

- **A0:** revisar matriz AC01–AC16, RNF01–RNF10, alcance, fuentes/blobs y diff exclusivo de documentación y CHANGELOG sin encabezados vacíos.
- **A1 documental:** verificar P01–P16 y N01–N16 descritos como futuros, no aserciones ejecutadas; `NO_APLICA` a pruebas de runtime sin código nuevo.
- **A2 documental:** auditar condiciones de tenant A/B, JWT/RBAC, EmpresaId NULL, consentimiento/opt-out, replay, PII y secretos como criterios fail-closed, nunca como vulnerabilidades descubiertas.
- **A3 documental:** rastrear M01.01–04 y contratos técnicos existentes a futuras pruebas; HTTP/MySQL/browser/proveedores `NO_APLICA` a este changeset Markdown.
- **A4:** commit en `dev` por CAS fast-forward, dos archivos, hashes y diff exactos; `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` sobre SHA específico; PLAN/COLA y BITACORA readback.
- **A5:** revisión técnica independiente por otra Task Javier, nunca por este autor; no promueve QA/main/PROD.
- **No-alcance:** sin endpoints, nuevas rutas UI, eventos, dispatcher, workers, jobs, proveedores, migraciones, datos productivos, secretos, cuentas de pago, releases ni cambios de configuración física.
- **Rollback:** `git revert` por nuevo commit en `dev` sin reset, force-push o manipulación histórica de BITACORA.
