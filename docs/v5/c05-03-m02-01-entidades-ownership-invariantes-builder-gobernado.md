# SOLQARYN V16 — C05-03.M02.01 entidades, ownership e invariantes del builder gobernado

**Microtarea:** C05-03.M02.01, PLAN_MAESTRO fila 800; owner PRIMARY DEV 03 JAVIER, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`; `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT; politica V15 **R0 DOCUMENTAL**, 2026-11-15.

**Fuente de autoridad:** STANDARD DE IMPLEMENTACION DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; Sheets `PLAN_MAESTRO`, `COLA`, `DAG_TECNICO`, `BITACORA` en `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. Alcance unicamente modelo conceptual M02.01 sin activacion de builder.

## 1. Precedencia y no-regresion

Al reclamar se verificaron PLAN H800=PENDIENTE, I800 vacio, COLA G800/O800=SI, DAG F761=VALIDADO/G761=SI despues de C05-03.M01.01–06 LISTO y A5 independiente de C05-03.M01.06. El contrato de preparacion `docs/v5/c05-03-m01-06-bloqueos-rollback-liberacion-builder-gobernado.md` (blob `cbe0cad601e0640592d9ce76025bbed08fdbc7ad`) conserva BL01–BL16, RB01–RB10, LG01–LG12, las decisiones D01–D08 pendientes y los 10 pares T FUTUROS_NO_EJECUTADOS. M02.01 no resuelve por si mismo esos permisos.

Solo se agrega este Markdown mas una seccion completa en CHANGELOG_AI.md. **No se crean clases C#, tablas, migraciones, API, endpoints, componentes Angular, rutas, previews activos, HTML/JS/CSS arbitrarios, versionadores, caches, providers, flags, cuentas, secretos, DNS, QA/main/PROD o datos reales.** La UI no reemplaza JWT/RBAC, autorizacion API ni propiedad por tenant; TemaVisual/SCSS existentes no significan que haya builder implementado.

## 2. Doce entidades conceptuales E01–E12 (NO CREADAS)

| ID / concepto | Owner conceptual | Invariante de identidad |
| --- | --- | --- |
| E01 TenantContextRef | server | EmpresaId verificado de auth; nunca slug/URL cliente |
| E02 ComponentTypeDefinition | platform | Clave y version de componente precompilado allowlist; nunca script |
| E03 PropertySchemaDefinition | platform | Campos tipados, limites y defaults seguros con fuente versionada |
| E04 DesignTokenPreset | platform | Opciones SCSS/token allowlist, no estilos arbitrarios |
| E05 PageDraft | tenant | Borrador privado ligado a EmpresaId y autor; no publicado por default |
| E06 SectionPlacement | tenant/page | Referencias ordenadas a ComponentTypeDefinition y props inertes |
| E07 PageRevision | tenant/page | Version monotona y hash/ETag para CAS y rollback aditivo |
| E08 PublicationApproval | tenant/page | Identidad/rol/revision aprobada server-side, revocable |
| E09 PublicProjection | tenant/host | Vista publica minima derivada de revision aprobada y host validado |
| E10 HostBindingCandidate | platform/tenant | Host verificado por autoridad, no claimed desde query/headers |
| E11 PreviewContext | tenant/user | Evaluacion read-only sobre borrador propio, sin side effects |
| E12 BuilderAuditTrail | tenant/resource | Registro inmutable acotado, sin PII/secretos en payload |

Todas las relaciones que cruzen tenant, pagina, revision, seccion, publicacion u host exigen comprobacion server-side contra el actor autorizado; el `TenantContextRef` no se construye desde un input editable del cliente. Los nombres anteriores no son entidades EF reales ni promesa de nuevas tablas; cualquier persistencia futura debe usar MySQL 8.4 efimero, migrations y test Category=Integration en CI autorizado antes de certificar.

## 3. Dieciseis invariantes INV01–INV16

| ID | Regla fail-closed futura |
| --- | --- |
| INV01 | Todo PageDraft/Revision/SectionPlacement mantiene EmpresaId server-side y verificacion de propiedad |
| INV02 | Ningun componente o atributo se interpreta sin clave, version y PropertySchema permitidos |
| INV03 | Texto de usuario es dato escapado; nunca HTML/JS/CSS/SQL ejecutable desde el builder |
| INV04 | URLs publicas cumplen allowlist de esquema/destino; privadas, javascript, file y SSRF se rechazan |
| INV05 | Tokens de diseno son opciones tipadas/limitadas; CSS libre no puede burlar CSP |
| INV06 | Seccion pertenece a pagina del mismo tenant; foreign ID o tenant B no es legible ni mutable |
| INV07 | Usuario autentico no implica permiso editar, aprobar o publicar; verificar RBAC por operacion |
| INV08 | Revocacion de membresia/rol invalida acceso a borrador, preview y publicacion |
| INV09 | Publicacion solo sobre PageRevision actual y approval vinculada; CAS/ETag contra lost update |
| INV10 | Preview es read-only sin publicar, enviar correo, disparar worker o modificar BD |
| INV11 | PublicProjection incluye contenido minimo aprobado; no PII, tokens ni drafts privados |
| INV12 | HostBindingCandidate no concede ownership; requiere verificacion server-side independiente |
| INV13 | Cache/slug/proyeccion publica segmentada por tenant y host verificados, sin spillover A/B |
| INV14 | Limites de cantidad secciones, profundidad, bytes y payload previenen consumo excesivo |
| INV15 | Rollback crea nueva version y conserva revisiones/historial, sin hard-delete ni acceso ajeno |
| INV16 | Auditoria redaccion segura conserva actor, tenant, version, operacion, resultado y correlacion |

Las invariantes son criterios de diseno, no assertions de una implementacion. Si una decision D01–D08 sigue pendiente y su prueba es exigible, NO_GO a la microtarea tecnica afectada sin liberar artificialmente COLA/DAG.

## 4. Relaciones conceptuales permitidas y limites

| Relacion futura | Cardinalidad orientativa | Condicion |
| --- | --- | --- |
| TenantContextRef → PageDraft | 1 a N | EmpresaId de auth, cada borrador tenant-isolated |
| PageDraft → SectionPlacement | 1 a N | Orden y conteo limitados, secciones solo en pagina propia |
| SectionPlacement → ComponentTypeDefinition | N a 1 | TypeKey/version allowlisted y prop schema tipado |
| PageDraft → PageRevision | 1 a N | Revisiones append-only, version CAS |
| PageRevision → PublicationApproval | 1 a N | Aprobacion vinculada a revision/autoridad especificas |
| PageRevision → PublicProjection | 1 a 0..1 | Solo publico aprobado, nunca draft privado |
| HostBindingCandidate → PublicProjection | 1 a N | Host/tenant verificados, cache particionada |
| PageDraft → BuilderAuditTrail | 1 a N | Auditoria minima sin PII ni secretos |

Los alcances de esas relaciones son candidatos para M02.03, no decisiones de schema ni indices ejecutados. No romper el ERP existente, la configuracion visual vigente ni rutas autenticadas.

## 5. Diez escenarios P/N T01–T10 FUTUROS_NO_EJECUTADOS

| ID | Positivo futuro | Negativo/falla futura |
| --- | --- | --- |
| T01 | Borrador propio guarda propiedad tipada | Tenant B intenta editar PageDraft A y debe recibir 403 |
| T02 | Componente versionado esta en allowlist | Clave/version desconocida rechazada sin render arbitrario |
| T03 | Texto inocuo se escapa | Script/handler insertado queda dato, no ejecucion |
| T04 | URL segura se valida | file/javascript/red privada se rechazan |
| T05 | Publicador valido usa revision vigente | Revision obsoleta devuelve conflicto y no muta |
| T06 | Revocacion invalida permiso de edicion | JWT con rol revocado no habilita escrituras |
| T07 | Preview propio no cambia estado | Preview no dispara eventos, worker, email ni publicacion |
| T08 | Host aprovado resuelve proyeccion minima | Slug/host B no abre borrador A ni cache B |
| T09 | Rollback autorizado crea revision nueva | Rollback sin permiso/historia rota debe fallar |
| T10 | Editor accesible 320px teclado y foco | Fallo a11y o limite bytes/profundidad bloquea publicar |

Se han **documentado 10/10 pares**, **ejecutado 0/10** contra Angular/.NET/MySQL/browser. Implementacion posterior R2 de auth/tenancy exige negativos A/B, revocacion, IDOR; cambios UI exigen Angular build/Playwright; DB EF exige MySQL 8.4 efimero Category=Integration real. Aqui esos jobs no aplican causalmente al Markdown.

## 6. Evidencia A0–A5, impacto y rollback

- **A0:** ID, autoridades, fuente M01.06, no-regresion, 12 conceptos E y 16 invariantes; dos Markdown como unico diff y rollback no destructivo.
- **A1:** 10 pares P/N y restricciones documentadas capaces de detectar defectos futuros; los tests NO se ejecutaron.
- **A2:** ownership, tenant A/B, revocacion, RBAC, XSS/SSRF, CAS, PII, auditoria y cache especificados fail-closed; sin introducir superficies runtime.
- **A3:** cotejo documental de fuente M01.06, stack y DAG; MySQL/API/Angular/Chromium/Playwright no aplican a esta entrega R0.
- **A4:** commit dev fast-forward serial con comprobacion de HEAD, diff/blobs; `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` completed/success exact SHA, BITACORA 26 columnas y readbacks PLAN/COLA/DAG.
- **A5:** revision independiente por otra de las cinco Tasks Javier con TASK_ID diferente, puede rechazar; nunca autocalificada.

**Rollback documental:** `git revert` normal del commit tras revisar HEAD, conservar BITACORA y no forzar cambios de rama. `LISTO_DEV` acredita solo este contrato R0, no implementacion del builder, compatibilidad HTTP, prueba MySQL, autorizacion QA/main/PROD, cambio de proveedores ni desbloqueo M03; sucesoras requieren DAG/owner/pruebas y A5 pertinentes.
