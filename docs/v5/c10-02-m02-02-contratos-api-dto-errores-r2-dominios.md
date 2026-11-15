# SOLQARYN V5 — C10-02.M02.02: contratos API/DTO y errores seguros de almacenamiento privado

**Autor:** PRIMARY DEV 01 JAVIER, Task `6adbb9fff1548191a1bd5b30d9e9984a`. **Repositorio:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), PLAN/COLA/DAG Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. **Fecha:** 2026-11-15.

## Autoridad y alcance

Diseñar **contratos conceptuales API, DTO, validaciones y compatibilidad** para archivos privados con separación de tenant, versiones, firmas y futura migración de proveedor. **No implementar** endpoints, DTO C#, workers, R2, CDN, DNS, CORS, Cloudinary, nuevas credenciales, bases de datos, migraciones, permisos, secretos ni cambios de producción.

`C10-02.M01.06` (SHA `3c115e7f3dc9ea48edac2239e5515f9470468d99`) tiene A5 independiente de DEV01; el DAG exacto de `C10-02.M02.02` quedó F=VALIDADO/G=SI, COLA G/O=SI y PLAN PENDIENTE sin owner en preclaim. La entrega M02.01 aporta modelo conceptual `d63c79514c6ededead603551f45cd4423d8f997f`, pero no se presupone su A5 como gate. M02.02 es independiente conforme al DAG.

La fuente M01.03 es un inventario documental de contratos vigentes de archivos/Cloudinary. No asumir que R2 está contratado o habilitado por ambiente. Las decisiones D01–D08 sobre infraestructura, dominio, retención, costos y seguridad siguen pendientes, por lo que las siguientes operaciones sólo representan diseño futuro.

## Operaciones proyectadas — NINGUNA RUTA HTTP REGISTRADA

| Operación conceptual | Entrada mínima | Comprobación servidor y salida prevista |
| --- | --- | --- |
| OP01 Consultar política | Recurso/canal y contexto autenticado | Ámbito, MIME y tamaño permitidos sin credenciales |
| OP02 Preparar ingreso | Propósito, tamaño esperado, hash y clave idempotente | JWT/RBAC, empresa, cuota y referencia temporal restringida |
| OP03 Confirmar ingreso | Referencia opaca y digest calculado | Comprobar bytes, tamaño, malware cuando aplique y propiedad |
| OP04 Consultar metadatos | Recurso opaco + versión | DTO saneado del propio tenant, sin bucket/key privados |
| OP05 Solicitar lectura | Recurso/versión/propósito | Autorizar nuevamente y preparar capacidad con método/TTL |
| OP06 Revocar acceso futuro | Recurso/versión + versión esperada | Revocación de emisión, sin garantizar invalidación bearer ya emitido |
| OP07 Listar recursos | Filtros allowlisted, cursor, límites | Respuesta paginada por tenant sin enumeración cruzada |
| OP08 Despublicar archivo | Recurso/versión/expectedVersion | Estado lógico, sin borrado físico ni violar legal hold |
| OP09 Consultar auditoría | Referencia + ventana autorizada | Eventos redactados, sin token, URL firmada ni PII privada |
| OP10 Consultar integridad | Versión + hash esperado | Resultado de comparación verificable, no falsa certificación |
| OP11 Previsualizar traslado | Manifiesto sintético/versionado | Plan de diferencias sin mover objetos, DNS ni Cloudinary |
| OP12 Consultar retención | Recurso + clasificación | Política vigente explícita; ausente = NO_GO para borrado |

**No son endpoints existentes ni facultades concedidas.** No diseñar URL firmada como permiso de lectura: RBAC y tenant se evalúan en servidor **antes** de emitirla y cada alcance/verbo/TTL debe acotarse por política.

## DTO de contrato (sin clases C# ni secretos)

| DTO propuesto | Campos públicos mínimos | Exclusiones y validación |
| --- | --- | --- |
| `StoragePolicyViewV1` | allowedPurpose, maxSize, mimeAllowlist | Sin credenciales de proveedor |
| `UploadIntentV1` | opaqueResourceRef, expectedSize, digest, expectedVersion | Empresa no viene como autoridad del cliente |
| `UploadOutcomeV1` | resourceRef, version, safeState, digestRef | No URL privada ni bucket key |
| `PrivateAssetViewV1` | resourceRef, type, size, status, createdUtc | Sin detalles de otro tenant |
| `ReadCapabilityRequestV1` | resourceRef, version, purpose | Verbo/TTL validados en backend |
| `RetentionDecisionV1` | resourceRef, policyRef, holdState | No delete sin decisión aprobada |
| `TransferPreviewV1` | manifestRef, sourceDigest, count, gapCount | No indica cutover ni proveedor activo |
| `SafeFileErrorV1` | code, correlationRef, retryable | Sin firma, token, PII ni ruta interna |

Se requiere validar `ApiResponse` y convenciones de errores/DTO actuales antes de introducir versiones reales. Los nombres no sustituyen interfaces existentes ni cambian firma de Cloudinary.

## Errores conceptuales E01–E14 y comportamiento seguro

| Código | Denegación futura exigida |
| --- | --- |
| E01 | JWT ausente/expirado → 401 sin emitir capacidad |
| E02 | Rol sin permiso → 403 sin listar recursos |
| E03 | Recurso de tenant B consultado desde A → denegar sin enumerar |
| E04 | ID o `EmpresaId` enviado en payload intenta elevar autoridad → ignorar/denegar |
| E05 | Key con traversal o doble codificación → rechazo seguro |
| E06 | MIME no permitido o contenido real discrepante → cuarentena/rechazo |
| E07 | Tamaño, streaming o cuota superados → fallo sin publicar objeto parcial |
| E08 | Firma inválida, método incorrecto o TTL vencido → no conceder lectura |
| E09 | Token bearer/replay no admitido por política → denegar y registrar |
| E10 | Versión obsoleta o mismo idempotencyKey con digest distinto → conflicto |
| E11 | Archivo en hold/retención → no borrar ni despublicar indebidamente |
| E12 | Proveedor no autorizado/configurado → NO_GO; sin fallback oculto |
| E13 | Hash/contador de traslado incongruente → no cutover, conservar origen |
| E14 | Log/error contiene secreto, firma o PII → sanear sin registrar payload |

Errores de archivos inexistentes y prohibidos no deben permitir enumerar IDs. Un timeout en confirmación mantiene estado desconocido y recuperable: no presumir objeto final válido ni reintentar publicación ciegamente.

## Oráculos futuros NO EJECUTADOS

| Positivo futuro | Negativo que debe fallar |
| --- | --- |
| P01 A consulta su recurso A | N01 A obtiene recurso B |
| P02 JWT y RBAC autorizan lectura | N02 Rol revocado emite URL |
| P03 Prefijo permitido se canoniza | N03 Traversal cruza tenant |
| P04 Bytes/MIME/hash coinciden | N04 Contenido falso se confirma |
| P05 Capacidad válida limita método/TTL | N05 Firma alterada sirve |
| P06 Reintento idempotente no duplica | N06 Misma key con digest distinto publica |
| P07 Retención explícita protege objeto | N07 Legal hold se borra |
| P08 Estado de upload parcial se recupera | N08 Timeout publica objeto truncado |
| P09 Caché privado separa empresas | N09 Host de B muestra archivo A |
| P10 Vista de auditoría está saneada | N10 Log filtra URL/token |
| P11 Cutover simulado compara hashes | N11 Manifest incompleto certifica traslado |
| P12 Reversión conserva origen/verificación | N12 Destino falla y origen se borra |

Los doce pares P/N son **criterios FUTUROS_NO_EJECUTADOS**; ningún test HTTP, .NET, Angular, navegador, R2 o DNS fue ejecutado ni se emitieron enlaces, credenciales, archivos o cargos.

## A0–A5, seguridad y rollback

**A0:** revisar Skill, fuente, HEAD, Scope Lock, owner CAS y diff de este documento más CHANGELOG autocontenido. **A1:** cotejo documental de 12 operaciones, 8 DTO, 14 errores y 12 pares P/N sin contabilizar ejecución. **A2:** límites JWT/RBAC/tenant A/B, firma/TTL/replay, caché, idempotencia, privacidad y legal hold fail-closed. **A3:** compatibilidad conceptual con Cloudinary/archivos y API existentes; .NET, MySQL, Playwright y R2 `NO_APLICA` a diff Markdown y no son PASS. **A4:** publicar fast-forward/CAS, inspeccionar SHA y blobs, comprobar `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` completed/success SHA exacto y readbacks PLAN/COLA/DAG/BITACORA. Una CI requerida queued/failed/cancelled/skipped nunca es PASS.

**A5:** revisión independiente por Task Javier distinta del autor DEV01; LISTO_DEV documental no autoriza R2, DNS, credenciales, IAM/DB, M03, QA/main/PROD.

**Rollback:** revertir documento/CHANGELOG mediante `git revert` normal o corrección aditiva con CAS y trazabilidad; no borrar BITACORA. Traslado futuro exige manifiesto, readback de digest/ACL, backup y restauración ensayados, nunca tocar proveedor real o datos de clientes en esta microtarea.
