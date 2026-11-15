# SOLQARYN V5 — C10-03.M02.01: entidades, ownership e invariantes del inventario condicionado

**Autor:** PRIMARY DEV 06 — JAVIER (6af16bbad26881918c283abe06e1f32a). **Fecha:** 2026-11-15. **Scope:** solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT. **Naturaleza:** diseño documental de almacenamiento seguro futuro; sin inventario real, exportación, migración, proveedor nuevo ni código runtime.

## 1. Alcance, fuente y estado real

Autoridad institucional: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Google Doc 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA; PLAN_MAESTRO fila 1520, C10-03.M02.01, «Definir entidades, ownership, relaciones y reglas de invariantes». El resultado futuro del bloque es **verificar inventario real**; si todas las fuentes acreditan CERO, omitir exportación, copia, checksum y carga de migración históricos; preparar almacenamiento seguro para nuevas cargas. Al corte de esta entrega, INVENTARIO_HISTORICO=NO_VERIFICADO. No existen datos en esta revisión que permitan declararlo CERO_VERIFICADO ni POSITIVO_VERIFICADO.

El scope había sido reclamado previamente por DEV02 sin entrega y posteriormente reencolado con evento de recuperación institucional. Esta corrida relee PLAN H1520=PENDIENTE/I vacío, COLA G1520/O1520=SI, DAG_TECNICO F1481=VALIDADO/G1481=SI y realiza **un nuevo CLAIM único** con CAS y readback al TASK_ID DEV06, conservando la nota histórica y sin atribuirse código del intento anterior. C10-03.M01.06 está LISTO con A5 independiente, no acredita inventario ni permisos para acceder proveedores.

Evidencia de código viva: backend/src/Application/Interfaces/IImageStorageService.cs (blob f74f496957dfa7cc1d1ec00a26444ec4befa24fa) posee overloads tenant-aware de upload/download/delete que por defecto fallan con NotSupportedException sin implementación explícita. backend/src/Application/Interfaces/StorageTenantContext.cs (blob 716e8e205502d6300f555947c79edc2852fd63ba) deriva EmpresaId/UsuarioId de scope verificado en backend, exige ownership y genera prefijos empresas/{EmpresaId} con rechazo de traversal. Inventario estático C10-03.M01.03 (blob 517836ab01996521234b083076524c570b553326) registra adaptadores Cloudinary, folder resolver, seguridad MIME, tests y script de auditoría **leídos pero no ejecutados**. Los SHA son evidencia del código, no número de objetos productivos.

## 2. Trece objetos lógicos FUTUROS, no tablas creadas

| Objeto lógico propuesto | Identidad/ownership y vínculo | Regla de seguridad |
| --- | --- | --- |
| FuenteInventario | FuenteId, ambiente y proveedor allowlisted | No autoriza consultar producción |
| EjecucionInventario | EjecucionId, FuenteId, identidad de auditor, UTC | Solo ejecución autenticada futura |
| EstadoInventario | EjecucionId, DESCONOCIDO/CERO_VERIFICADO/POSITIVO_VERIFICADO/INCONSISTENTE | Nunca convertir UNKNOWN en cero |
| ReferenciaMedio | EmpresaId, MedioId, entidad dueña, tipo | FK tenant-bound; no exponer locator crudo |
| LocalizadorProveedor | ReferenciaMedioId, proveedor, identificador opaco | Prefijo no es autorización de propiedad |
| EvidenciaConteoFuente | EjecucionId, origen, conteo, timestamp | Fuente validada antes de concluir |
| ReconciliacionInventario | EjecucionId, conteo DB/proveedor, divergencias | Un resultado parcial no cierra inventario |
| ManifiestoHistoricos | EjecucionId, objetos autorizados, versión de manifiesto | Solo si POSITIVO_VERIFICADO |
| EvidenciaIntegridad | ReferenciaMedioId, hash/tamaño/algoritmo, fuente | Sin publicar contenido ni PII |
| PoliticaRetencionMedio | ReferenciaMedioId, hold/expiración/version | Retención gana a lifecycle destructivo |
| SolicitudNuevaCarga | EmpresaId, actor servidor, tipo, tamaño | MIME por bytes, cuota y permiso |
| ResultadoCargaTemporal | SolicitudId, estado, referencia opaca, nonce | Reintentos y fallos compensables |
| AuditoriaStorage | EmpresaId, operación, correlación, UTC, resultado | No guardar secrets, URLs firmadas ni token |

La entidad referencia debe pertenecer a **una empresa verificada**, y las referencias de auditoría por empresa nunca sirven para autorizar lectura sin RBAC. Ninguno de estos nombres constituye DDL ni contrato público ya existente. Separar estrictamente inventario de históricos de las cargas sintéticas nuevas y de las futuras cargas reales.

## 3. Catorce invariantes obligatorias (INV01–INV14)

- INV01: El estado inicial de inventario es NO_VERIFICADO hasta consulta autenticada de todas las fuentes relevantes; no se usa la ausencia de UI como prueba.
- INV02: CERO_VERIFICADO exige conciliación entre referencias persistidas y objetos del proveedor con identidad, fuente, ambiente, UTC y alcance; no exportar históricos cuando es cero.
- INV03: POSITIVO_VERIFICADO exige manifiesto limitado por tenant, clasificación, integridad, retención y conteos conciliados **antes** de contemplar copia.
- INV04: INCONSISTENTE, parcial o fallo del proveedor impiden migración; preservar datos y reportar incertidumbre, nunca forzar cero.
- INV05: EmpresaId/UsuarioId de operación se obtienen de StorageTenantContext server-side; header, ruta, JSON y locator no conceden identidad ni permisos.
- INV06: El dueño de un MedioId debe coincidir con tenant de referencia y con scope del recurso; acceso A→B/B→A falla cerrado.
- INV07: Locators, carpetas, URL pública y prefijos no prueban ownership; no permitir enumeración ni traversal.
- INV08: MIME inspeccionado por bytes y extensión y límites de tamaño/dimensiones comprobados antes de upload, nunca solo Content-Type declarado.
- INV09: URLs privadas temporales limitadas a recurso, método, TTL, firma válida y política de replay/expiración explícita.
- INV10: Secretos, firmas de URL, credenciales, binarios, PII y metadata sensible nunca se escriben en BITACORA, docs, logs, plan ni CHANGELOG.
- INV11: Operaciones repetidas requieren idempotencia y compensación comprobada; fallo no deja objeto parcial ni doble referencia.
- INV12: Retención/hold y auditoría impiden destrucción de histórico por un simple cambio de proveedor o contrato.
- INV13: No habilitar R2, DNS, cutover, datos productivos, migración ni factura por cambios documentales o flags de UI.
- INV14: A0–A4 verdaderos, pruebas causales por SHA y A5 independiente son puertas distintas; CI CANCELLED/SKIPPED/QUEUED no es PASS requerido.

## 4. Relaciones, estados y criterios de conciliación

Relaciones conceptuales: FuenteInventario 1:N EjecucionInventario; EjecucionInventario 1:N EvidenciaConteoFuente/Manifiesto; Empresa 1:N ReferenciaMedio; ReferenciaMedio 0:N EvidenciaIntegridad/PoliticaRetencion/Auditoria; SolicitudNuevaCarga 0..1 ResultadoCargaTemporal. La asociación ReferenciaMedio→EmpresaId es obligatoria incluso si un proveedor emite un localizador global. Históricos no se insertan como fixtures nuevos.

Una ejecución no puede saltar de NO_VERIFICADO a CERO_VERIFICADO sin prueba de los dos planos (metadatos DB y objetos proveedor), y no puede emitir POSITIVO_VERIFICADO desde un listado no autorizado o parcial. Si el conteo es cero debidamente acreditado, la migración de históricos pasa a NO_APLICA real y los tests de nuevas cargas se hacen en entorno sintético autorizado. No se supone que una fuente esté vacía.

## 5. Diez pares P/N FUTUROS; no ejecutados

| Positivo de etapa autorizada posterior | Negativo a rechazar |
| --- | --- |
| P01 Inventario autenticado 0 en todas las fuentes | N01 Inferir 0 desde pantalla o documentación |
| P02 Conteo positivo reconciliado y manifiesto | N02 Copiar objetos con conteos no conciliados |
| P03 Fixture A con tenant autenticado | N03 B accede a objeto A por locator conocido |
| P04 Descarga opaca tras validación owner | N04 Solo URL pública permite ver privados |
| P05 Archivo válido dentro de límites | N05 MIME falso, truncado, tamaño excesivo |
| P06 Reintento idempotente no duplica carga | N06 Timeout deja copia parcial/doble |
| P07 URL firmada corta y acotada | N07 Replay/caducada/método no permitido |
| P08 Hold evita borrado | N08 Delete fuerza borrado de objeto retenido |
| P09 Auditoría correlaciona sin secretos | N09 URL firmada/token/PII registrado |
| P10 Rollback de fixture sin pérdida | N10 Cutover R2/Cloudinary/Hostinger sin permiso |

P01–P10/N01–N10 son **oráculos de prueba futura**, no pruebas ejecutadas ni evidencia de inventario. No se usaron APIs de Cloudinary/R2, datos MySQL, proveedor DNS ni scripts de auditoría en esta microtarea.

## 6. Arquitectura, reversibilidad, A0–A5

**Impacto directo:** este Markdown y sección autocontenida de CHANGELOG_AI.md. Cero modificaciones en Domain/Application/Infrastructure, Angular, almacenamiento externo, SQL, JWT/RBAC, claves, proveedores, entornos y datos. El diseño podrá alimentar M02.02–.06 solo con DAG verdadero. La etapa M03 requerirá código y pruebas reales de MIME, uploads, tenant A/B, proveedor efímero, MySQL cuando corresponda y seguridad.

**A0:** fuente V5, requeue histórica conservada, claim único, lectura de IImageStorageService/StorageTenantContext y matriz M01.03 de blobs, Scope Lock. **A1:** 13 objetos, 14 invariantes y 10 P/N conceptuales revisables; sin inventario ni suite ejecutada. **A2:** tenant A/B, JWT/RBAC, MIME, URL firmada, hold, idempotencia y secretos fallan cerrado documentalmente. **A3:** compatibilidad de diseño con adaptadores actuales, C10-01/02 y arquitectura; backend, HTTP, MySQL, navegador y proveedor NO_APLICA para diff Markdown, no PASS ficticio. **A4:** publicar un commit fast-forward/CAS con dos Markdown, Scope Lock y V5 exact-SHA completed/success y readbacks institucionales. **A5:** una Task Javier distinta de DEV06 revisará diff, SHA, autoría y evidencia.

**Rollback:** git revert normal de esos dos documentos. Ninguna acción de esta M02.01 autoriza exportar, borrar, checksum histórico, cambiar R2/DNS, tocar QA/main/PROD ni declarar cero.
