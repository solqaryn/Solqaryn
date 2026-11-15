# SOLQARYN V5 FINAL — C06-02.M02.02: API, DTO, errores, validaciones y compatibilidad de landing por tenant

**Tipo:** especificación documental M02.02, sin rutas HTTP implementadas. **Autor:** PRIMARY DEV 06 JAVIER_CHATGPT, Task `6af16bbad26881918c283abe06e1f32a`. **Rama:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Fuentes:** Standard V5 FINAL en Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`, Skill canónica, M01.06 y M02.01.

## 1. Autoridad y alcance

Este contrato propone comportamientos FUTUROS para una landing pública administrable por empresa. M02.01 modeló 16 entidades, 16 invariantes y diez pares de oráculos de ownership; M01.06 fijó 16 bloqueos y ocho planes de reversión. Su existencia no autoriza registrar una ruta, controller, servicio, modelo EF, MySQL, DNS, TLS, Cloudflare/R2, worker, webhook ni dispatcher. La única autoridad runtime de identidad, host verificado, tenant A/B y permisos RBAC será el backend con datos actuales; Angular y los headers del navegador no prueban ownership.

Preflight institucional de esta unidad: `PLAN_MAESTRO!A909=C06-02.M02.02`, H=PENDIENTE, I libre, `COLA!G/O=SI`, `DAG_TECNICO!F=VALIDADO/G=SI` y predecesores calculados M01.01–06 LISTO+A5 independientes. Cada fila de API o DTO abajo es **contrato previsto, NO código existente**. Solo los endpoints vigentes del storefront conservan su semántica actual.

## 2. Operaciones lógicas propuestas — SIN registrar endpoints

| ID y operación lógica FUTURA | Resultado/entrada propuesta | Denegación obligatoria |
| --- | --- | --- |
| API01 Leer landing pública | GET lógico por host canónico aprobado y versión publicada | Host desconocido/tenant no aprobado → no revelar directorio ni contenido de B |
| API02 Vista previa | Consulta de revisión no pública con token efímero revocable | Sin permiso, grant expirado o indexación accidental → rechazo |
| API03 Listar borradores propios | Paginación de revisiones bajo tenant autenticado | Tenant B, cursor A reutilizado o paginación sin límite → rechazo |
| API04 Crear revisión | Nuevo borrador tipado con versión y ownership servidor | Cliente proporciona tenant B, JS/HTML activo o versión inexistente → rechazo |
| API05 Editar bloques | CAS de revisión en estado editable con permiso Editor | Publicada, ajena, vieja o bloque malformado → conflicto seguro |
| API06 Enviar a aprobación | Propuesta sin efecto, con aprobador distinto y diff minimizado | Flag `aprobado=true` o autoaprobación → denegación |
| API07 Publicar versión | Revisión aprobada, RBAC Publicador y CAS, audit strict | Sin permiso, host validado, cambio crítico sin auditoría → NO_GO |
| API08 Revertir versión | Restaurar revisión publicada compatible y auditada | Snapshot ausente, tenant B o rollback destructivo → NO_GO |
| API09 Resolver assets públicos | Referencia opaca y pertenencia al tenant/host aprobado | Media privada o de B → nunca entregar URL firmada |
| API10 Consultar SEO público | canonical/robots/sitemap solo de versión autorizada | Borrador/preview en índices o host no verificado → rechazo |

No se fijan verbos/paths REST concretos para nuevas operaciones: deberán reconciliarse con controladores existentes antes de M03. **API09/10 no otorgan acceso a medios privados por redacción**. La relación entre editor y publicador exige autoridad separada y revalidación server-side de revocación.

## 3. Contratos DTO conceptuales y origen de confianza

| DTO propuesto | Campos mínimos y fuente | Restricción fail-closed |
| --- | --- | --- |
| LandingQuery | host normalizado, ruta, version opcional, limite de tamaño | Server-side binding+contexto de publicación; no confiar en forwarded host |
| LandingPublicProjection | landingId opaco, revision publicada, bloques tipados, URLs públicas | Nunca incluye grants, JWT, borradores o correlación interna |
| LandingDraftInput | schemaVersion, bloques tipados, expectedVersion, idempotencyKey opaca | TenantId de cliente solo selector, no owner ni permiso |
| LandingDraftView | id revision, estado, checksum, editor y timestamps minimizados | Solo autenticado con edición y tenant vigente |
| LandingPreviewGrant | scope tenant, revision, actor, expiración y revocación | Emitido y verificado por servidor; no cookie global |
| LandingApprovalRequest | revisión, motivo, actor, expectedVersion y diff saneado | No permite introducir aprobador propio como verdad |
| LandingPublicationDecision | resultado, publicador distinto, version CAS y auditRef | Sin política/grant no se crea publicada |
| LandingRollbackRequest | landingId, revisionOrigen, versionDestino, expectedVersion | No altera historial, producción o data ajena sin plan y autoridad |
| LandingError | code público allowlisted, correlationId saneado, detalle mínimo | Nunca JWT, SQL, datos de B ni rutas internas |

La fuente del `tenantScope` y `ActorId` es el principal verificado por el backend y el almacenamiento de ownership, nunca un header, cookie, host o campo de cliente. Todos los DTO deben tener versión explícita; campos sensibles desconocidos se rechazan y ausencia de `approved` jamás se interpreta como true. Un `correlationId` de solicitud es solo diagnóstico y no deduplica operaciones; `idempotencyKey` nunca confiere permiso de publicación.

## 4. Catálogo de errores propuesto (no respuestas HTTP ya probadas)

| Código | HTTP orientativo FUTURO | Contenido seguro |
| --- | --- | --- |
| E01 UNAUTHENTICATED | 401 | JWT ausente/caducado: mensaje genérico |
| E02 ACTION_FORBIDDEN | 403 | RBAC, membresía o reautenticación no demostradas |
| E03 RESOURCE_NOT_VISIBLE | 404/403 neutro | Recurso B o no visible: no enumerar existencia |
| E04 HOST_NOT_VERIFIED | 404/409 | Host no aprobado/no asignado; no fallback a tenant |
| E05 PREVIEW_GRANT_INVALID | 403 | Token expirado/revocado/no indexable |
| E06 VERSION_CONFLICT | 409 | CAS esperado obsoleto, sin diff privado |
| E07 INVALID_BLOCK_SCHEMA | 400/422 | Contenido/atributo fuera de catálogo cerrado |
| E08 APPROVER_NOT_INDEPENDENT | 403/409 | Editor intenta aprobar su propia revisión |
| E09 PUBLICATION_NOT_AUTHORIZED | 403/423 | Falta grant publicador, pruebas o política |
| E10 AUDIT_STRICT_FAILED | 503 | Revertir efecto futuro y respuesta neutral |
| E11 ASSET_NOT_VISIBLE | 404 | Asset privado/tenant B sin revelar URL firmada |
| E12 RATE_LIMIT_EXCEEDED | 429 | Cuota segura actor+tenant; sin contador de B |
| E13 RETENTION_POLICY_MISSING | 423 | Sin política no purgar ni exportar |
| E14 INCOMPATIBLE_SCHEMA_VERSION | 409/422 | No interpretar falta de campo como aprobación |
| E15 UNVERIFIED_PROXY_CONTEXT | 400/403 | Forwarded/Host no confiable; sin tenant default |
| E16 DEPENDENCY_NOT_READY | 409/423 | DAG/CI/A5 incompleto no dispara publicación |

La tabla no modifica `ExceptionHandlingMiddleware` ni el backend actual. Deben validarse contratos de privacidad contra código/HTTP reales y protocolos de compatibilidad antes de exponer rutas. Un servicio no disponible debe fallar cerrado sin revelar si un tenant existe ni propagar JWT, PII, SQL, ruta interna o URL firmada.

## 5. Compatibilidad y seguridad por contrato

- **Versionado:** clientes viejos pueden ignorar campos opcionales no críticos; cambios en identidad, tenant, permisos, versión CAS o estado aprobado requieren versión incompatible y pruebas en cada consumidor.
- **Tenancy y host:** normalizar punycode/hostname, validar proxy confiable y ownership antes de resolver HTML, caché, SEO o assets. Host desconocido no usa un tenant por defecto.
- **Edición y publicación:** controles distintos, autenticación fresca, permisos revocables, auditoría strict antes de reconocer efecto y CAS por revisión; no existe botón/claim que sustituya servidor.
- **Preview y SEO:** no-store/noindex, grants acotados y revocables, jamás exponer borrador en sitemap. Las URLs públicas tienen que provenir de política de host y activo aprobados.
- **Cache y contenido:** claves tenant+host+revisión, bloques de tipos cerrados, CSP/sanitización, owner assets; nunca colisión cross-tenant ni HTML activo.
- **Datos:** un futuro diseño EF/MySQL necesitará MySQL 8.4 efímero, constraints/rollback y filtros A/B; este documento no crea ni prueba ninguna tabla.

## 6. Oráculos positivos y negativos FUTUROS_NO_EJECUTADOS

| Caso | Positivo futuro requerido | Negativo futuro requerido |
| --- | --- | --- |
| PN01 | Host A aprobado → solo landing A publicada | Host B/unknown o spoofed forwarded → ninguna información A |
| PN02 | JWT Editor A permite borrador A con CAS | JWT B, membresía revocada o rol legacy no edita A |
| PN03 | Publicador distinto con rol vigente finaliza revisión aprobada | Editor como único aprobador, flag UI o SHA no verificado no publica |
| PN04 | Preview autorizado con TTL muestra revisión a actor legítimo | Anónimo, expirado o token revocado no ve contenido |
| PN05 | Bloque tipado correcto se serializa sin scripts | HTML activo, SQL, path traversal o esquema desconocido se rechazan |
| PN06 | Asset A público verificable genera referencia pública | Asset de B o privado no expone URL ni key |
| PN07 | Cursor A enumera únicamente revisiones A | Cursor B usado por A no revela filas, conteos ni metadata B |
| PN08 | Publicada aprobada produce canonical e indexable adecuado | Draft/preview no entra en robots, sitemap ni cache pública |
| PN09 | Cambio crítico con auditoría strict confirma efecto único | Fallo de DB/timeout revierte y no afirma ACK durable |
| PN10 | Rollback a versión A previamente aprobada es CAS y auditable | Versión B, snapshot faltante o rol no autorizado no altera A |
| PN11 | Cambios de contrato preservan versiones de clientes compatibles | Cliente viejo sin campo de permiso no obtiene grant por defecto |
| PN12 | CI de futura implementación prueba HTTP/MySQL A/B y browser | Documento/mock/CI cancelada no es certificación de runtime |

Los doce pares son inspección de diseño, **no pruebas HTTP, Playwright, SQL, xUnit ni MySQL ejecutadas**. No afirmar que `hostFromRequest`, JWT o permisos actuales satisfacen automáticamente los casos; deben inspeccionarse proxies, filtros y reglas efectivas antes de materializar M03.

## 7. Cierre A0–A5 y rollback

**A0:** dependencia M01+A5, M02.01, diff limitado a documento y CHANGELOG, versión y ausencia de secretos. **A1 documental:** correspondencia de 10 operaciones propuestas, 9 DTO, 16 errores y 12 pares P/N, incluidos rechazos por tenant, RBAC y host no confiable. **A2:** seguridad A/B, BOLA/IDOR, mínimos de PII, preview y permisos separados; sin certificar runtime. **A3:** contrato futuro respetando arquitectura Domain/Application/Infrastructure/API/UI; `dotnet`, Angular, MySQL, Docker y Playwright **NO_APLICA** al cambio Markdown, nunca PASS inventado. **A4:** publicar commit FF/CAS en `dev`, verificar Scope Lock y V5 `completed/success` exact SHA, blobs/diff y readbacks PLAN/COLA/DAG/BITACORA A:Z. **A5:** una de las otras cinco Tasks Javier, distinta del autor DEV06, revisa independencia, pruebas pertinentes y contrato antes de liberar dependencias.

**Rollback:** `git revert <SHA>` normal y evento compensatorio append-only en caso necesario; jamás force-push, modificación histórica, cambios de datos productivos ni QA/main/PROD. **NO IMPLEMENTAR EVENTOS TODAVÍA**, no instalar cola, dispatcher, webhook ni una automatización nueva.
