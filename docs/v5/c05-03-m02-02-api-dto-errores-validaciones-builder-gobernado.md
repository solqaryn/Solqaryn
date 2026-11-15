# SOLQARYN V16 — C05-03.M02.02 contratos API, DTO, errores y validaciones del builder gobernado

**ID:** C05-03.M02.02 / PLAN fila 801 · **Owner:** PRIMARY DEV 03 JAVIER, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3` · **Repo:** `solqaryn/Solqaryn@dev` · `PROJECT_SCOPE_LOCK=STRICT` · **V15 R0:** documental, 2026-11-15.

**Fuentes autorizadas:** Standard de Implementacion de Automatizaciones V5 FINAL Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; Sheet PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. No se crean API, DTO compilados, rutas, services, componentes Angular ni migraciones.

## 1. Alcance y dependencias institucionales

**Preclaim:** PLAN H801=PENDIENTE e I801 vacio, COLA G801/O801=SI y DAG F762=VALIDADO/G762=SI por antecedente C05-03.M01.01–06 LISTO + A5 independiente. C05-03.M02.01, actualmente documento de entidades/ownership, es una referencia para coherencia pero su A5 individual no se suple. M01.06 `docs/v5/c05-03-m01-06-bloqueos-rollback-liberacion-builder-gobernado.md` (blob cbe0cad601e0640592d9ce76025bbed08fdbc7ad) conserva gates BL01–BL16, decisiones D01–D08 pendientes y rollback. Entidades E01-E12 e invariantes INV01-INV16 de M02.01 son propuestas, no schema implementado.

Esta especificacion propone endpoints/DTO **hipoteticos**, no rutas reales, no grants ni interaccion con proveedores. No interpretar como exito operacional .NET, MySQL, Angular, browser, HTTP ni JWT. No configurar o ejecutar webhooks/eventos/worker, DNS, Vercel, Render, QA/main/PROD, datos privados o compras.

## 2. Ocho operaciones OP01–OP08 FUTURAS_NO_IMPLEMENTADAS

| Operacion | Nombre conceptual (NO ruta activa) | Condicion de seguridad obligatoria |
| --- | --- | --- |
| OP01 | ListAvailableComponents | Catalogo allowlisted; no reflexion, SQL, script ni componentes dinamicos |
| OP02 | GetOwnDraft | Requiere auth, membership y owner tenant de borrador |
| OP03 | CreateOwnDraft | Crea borrador no publicado con version/limites y auth; no ejecucion |
| OP04 | ReplaceOwnSection | Validacion tipada por schema/version y CAS de pagina; sin HTML libre |
| OP05 | PreviewOwnDraft | Solo lectura, sin publicación ni proveedores, con tenant verificado |
| OP06 | SubmitForApproval | Estado de revision sujeto a rol y version exacta, sin publicacion implicita |
| OP07 | PublishApprovedRevision | Solo autoridad backend, host vinculado y aprobación revocable |
| OP08 | RollbackOwnPublication | Revierte por nueva revision aprobada sin borrar historia ni acceder a B |

La publicacion futura debe exigir approval versionada y autorizacion backend por recurso; no basta ocultar botones Angular ni confiar en claimed tenant. Los metodos HTTP, rutas y DTO definitivos requieren revision de integracion/autorizaciones del backend antes de existir.

## 3. Ocho DTO conceptuales D01–D08 (NO COMPILADOS)

| DTO | Campos candidatos | Origen y restriccion |
| --- | --- | --- |
| D01 ComponentDescriptor | typeKey, version, allowedProperties | Platform signed allowlist, sin source ejecutable |
| D02 DraftCreateRequest | pageKey, locale, templateKey | tenant nunca viene de request |
| D03 SectionMutationRequest | sectionId, componentType, properties, expectedRevision | schema tipado, CAS, cuotas |
| D04 DraftDetailsResponse | draftId, revision, sections, status | solo tenant autorizado |
| D05 PreviewRequest | draftId, expectedRevision, viewportPreset | no side effects y limite de payload |
| D06 PublicationApprovalRequest | draftId, revision, approvalReason | role, membership y snapshot verificados |
| D07 PublicProjectionResponse | publicKey, version, approvedComponents | sin PII, sesiones ni drafts |
| D08 RollbackRequest | publicKey, fromRevision, toRevision, reason | permiso, integridad y versionamiento append-only |

Ningun DTO acepta CompanyId o TenantId editable como autoridad; el identificador empresarial proviene del contexto servidor verificado. El campo de texto de una seccion debe renderizarse escapado; nunca se evalua como markup ni codigo. Versiones, limites y campos deben validarse contra catalogos tipados del sistema, no parsearse libremente.

## 4. Doce respuestas de error ERR01–ERR12 (CONTRATOS, NO PROBADAS HTTP)

| Error | Codigo propuesto | Condicion y redaccion segura |
| --- | --- | --- |
| ERR01 | 401 | Token ausente/caducado, error opaco |
| ERR02 | 403 | Rol, membresia o permiso revocado |
| ERR03 | 404 | Recurso ajeno o inexistente, sin enumeracion |
| ERR04 | 409 | Revision obsoleta / CAS conflict |
| ERR05 | 422 | TypeKey/versión de componente no permitidos |
| ERR06 | 422 | Propiedad extra o tipo/longitud erroneo |
| ERR07 | 422 | Esquema URL prohibido o potencial SSRF |
| ERR08 | 413 | Payload o profundidad excede cuota |
| ERR09 | 409 | Publicacion sin aprobacion para revision exacta |
| ERR10 | 403 | Host no vinculado al tenant por servidor |
| ERR11 | 503 | Fuente confiable de ownership/permissions no disponible |
| ERR12 | 400 | Preview/rollback intenta efecto no autorizado |

Los codigos HTTP definitivos pueden variar por politica del backend, pero el rechazo fail-closed y la no enumeracion de recursos ajenos son invariantes. Ningun error devuelve PII, JWT, stack traces, DSN, provider secrets ni IDs externos; actor/tenant se guardan con correlacion segura sin secreto.

## 5. Catorce validaciones V01–V14 para implementacion futura

| ID | Validacion obligatoria |
| --- | --- |
| V01 | tenant y usuario se resuelven de JWT verificado en servidor, no header/query client |
| V02 | resource ownership se vuelve a chequear por accion: listar, editar, previsualizar, aprobar, publicar |
| V03 | membresia y rol vigentes se revalidan en operaciones sensibles; revocacion fail-closed |
| V04 | component typeKey + version debe existir en catalogo precompilado allowlisted |
| V05 | propiedades cumplen esquema tipado y no admiten claves desconocidas ni mass assignment |
| V06 | texto es escapado y CSS solo design tokens, no parsear HTML/JS/SQL libre |
| V07 | URI solo esquemas/origenes permitidos, sin destinos internos/file/javascript |
| V08 | draft y section IDs deben estar en el mismo tenant, pagina y revision |
| V09 | expectedRevision obligatorio para ediciones/publicacion/rollback; conflicto 409 |
| V10 | limites maximos de bytes/secciones/profundidad antes de persistir o renderizar |
| V11 | preview nunca escribe ni envia webhook/email ni dispara worker/queue |
| V12 | public projection no incluye PII, secretos, drafts privados ni referencias a tenant B |
| V13 | host/slug se vinculan por autoridad server-side y cache segmentada por host/tenant |
| V14 | audit trail redacta datos sensibles y mantiene actor, rol, tenant, version, operacion |

**Riesgos:** tenant A/B, IDOR/BOLA, XSS/SSRF, CSRF/CORS cuando aplique, lost update, revocacion, replay, acceso anonimo y versionado se validaran en codigo real. No usar SQL, HTML/JS arbitrario ni permisos por UI, no romper ERP actual ni TemaVisual existente.

## 6. Diez pares P/N T01–T10 FUTUROS_NO_EJECUTADOS

| Test | Positivo futuro | Negativo/limite que debe fallar |
| --- | --- | --- |
| T01 | Listado autorizado retorna solo componentes tipados | Component key no registrada se rechaza sin ejecutar script |
| T02 | Owner A obtiene su borrador | Actor A intenta recurso B y recibe 404/403 sin fuga |
| T03 | Edit con CAS valido crea revision | Edit con revision obsoleta falla 409 y preserva historia |
| T04 | Texto inocuo y URI HTTPS permitida son datos | HTML/JS inline o file/javascript/SSRF se rechaza |
| T05 | Preview propio es read-only | Preview no escribe, publica, envia correo ni despacha eventos |
| T06 | Usuario con rol vigente somete revision | Rol revocado o token vencido no permite aprobar |
| T07 | Host verificado expone proyeccion minima | Host B/slug manipulado no filtra draft A |
| T08 | Publicador con aprobacion vigente publica revision | Aprobacion para otra version o tenant se rechaza |
| T09 | Payload dentro de cuotas se valida | Payload enorme/recursion profunda falla sin efectos |
| T10 | Rollback autorizado genera nueva version | Rollback sin rol/conflicto no borra historia ni altera B |

**Resultados reales aqui:** 10/10 pares documentados, **0/10 pruebas HTTP/.NET/Angular/MySQL ejecutadas** en esta entrega. El futuro codigo UI requiere Angular 22 build y Playwright Chromium, backend requiere .NET 10 xUnit y seguridad A/B/roles; persistencia futura MySQL 8.4 efimero con Category=Integration. No asumir builds verdes de otras rutas como estas pruebas.

## 7. A0–A5, rollback y criterio de salida

- **A0:** alcance M02.02, requisitos, 8 operaciones, 8 DTO, 12 errores y 14 validaciones, contratos precedentes y diff de solo documento/CHANGELOG.
- **A1:** 10 pares de escenarios positivos/negativos definidos; no se ejecuta API; pruebas reales posteriores.
- **A2:** JWT/RBAC/membresia/tenant A/B, XSS/SSRF/limites/CAS/privacy documentados fail-closed; no se modifica runtime.
- **A3:** trazabilidad a M01.06 y M02.01; HTTP, MySQL, Angular, Playwright, proveedores NO_APLICA para diff Markdown-only.
- **A4:** publicacion FF/CAS en dev, Scope Lock y procedencia V5 `completed/success` exact SHA, readback PLAN H/I, COLA/DAG y BITACORA A:Z.
- **A5:** review por otra Task Javier con TASK_ID distinto; no autoprobacion. QA/main/PROD requieren autorizacion separada.

**Rollback de este R0:** `git revert` aditivo del commit documental con readback; no implica rollback de API inexistente. M03 no se habilita por un borrador/VALIDANDO o contrato sin gates verdaderos. No cambiar formulas COLA/DAG a SI manualmente ni secret/config activa.
