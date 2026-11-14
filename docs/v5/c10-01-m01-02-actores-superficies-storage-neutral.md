# SOLQARYN V5 — C10-01.M01.02: actores, entradas, salidas y fronteras de storage provider-neutral

**Unidad:** `C10-01.M01.02` del lote `C10-01 — Storage abstraction`, M01 de preparación. **Autor:** PRIMARY DEV 04 / JAVIER_CHATGPT, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; PLAN_MAESTRO fila 1443. **Repositorio:** `solqaryn/Solqaryn@dev`; PROJECT_SCOPE_LOCK=STRICT. **Fecha:** 2026-11-14.

## Alcance y delimitación temporal

Esta microtarea **documenta** las personas, procesos, interfaces y fronteras de confianza que habrá que verificar al definir una abstracción neutral de almacenamiento para imágenes, documentos, videos y exportaciones. Separa observaciones de la arquitectura vigente de componentes **futuros**; no implementa SDK, endpoints, repositorios, credenciales, buckets, Cloudflare R2, URLs firmadas ni migraciones. Cloudinary continúa siendo la integración de transición declarada; R2/Hostinger son objetivos futuros sujetos a autorización y gates propios.

El objetivo M01.01 previo fija doce invariantes de propiedad, tenant, permisos, MIME, tamaño, checksum, IDs opacos, errores, enlaces temporales y reversión. Aquí se definen los **contratos de entrada/salida esperados**; no se presume que una abstracción idéntica exista ya en .NET ni que haya archivos reales almacenados.

## Actores, permisos y superficies

| Actor/superficie | Propósito y entrada futura | Resultado o límite |
| --- | --- | --- |
| A01 — usuario autenticado de empresa A | Upload/consulta de objeto perteneciente a A | JWT y membresía/RBAC validados server-side; negar objeto B |
| A02 — administrador autorizado de empresa | Gestión documental limitada a su tenant | No adquiere control de objetos de otra empresa |
| A03 — consumidor público legítimo | Medio publicado explícitamente como público | DTO mínimo; no divulgar archivo privado ni URL persistente |
| A04 — servicio de aplicación backend | Solicitud tipada de lectura/escritura/exportación | Requiere contexto autorizado inmutable y operación concreta |
| A05 — capa Infrastructure/adaptador | Traduce contrato neutral a proveedor efectivo | No concede privilegios ni confía en tenant del cliente |
| A06 — proveedor vigente Cloudinary | Operación mediante integración existente, según configuración real | No interpretar esta lista como auditoría de credenciales |
| A07 — proveedor futuro opcional R2 | Solo una vez autorizado, configurado y probado | Ninguna activación ni secreto ahora |
| A08 — sistema de exportación PDF/archivo | Producir documento derivado de datos autorizados | Verificar propiedad, límites y no exponer PII cross-tenant |
| A09 — job de migración futuro | Copiar objetos con inventario/hashes y rollback probado | NO habilitado; ningún worker nuevo ahora |
| A10 — auditoría/observabilidad | Registrar operaciones y decisiones sin contenido sensible | No registrar bytes, tokens, URLs firmadas ni claves |
| A11 — gestor de claves/configuración | Proveer secreto por entorno y permisos mínimos | Secretos fuera de repo, sin nuevos permisos por este documento |
| A12 — consumidor frontend Angular | UI de subida/descarga con estados accesibles | No sustituye validación backend, no conoce credenciales del proveedor |

La UI puede sugerir una operación, **nunca** establecer `EmpresaId`, rol o propietario auténtico. Los límites JWT/RBAC y tenant A/B se validarán con integración positiva/negativa, no mediante esta matriz.

## Contrato de entradas y salidas previsto (interfaces futuras, no existentes certificadas)

| Interfaz | Entrada mínima bajo autorización | Salida prevista / denegación |
| --- | --- | --- |
| I01 Solicitar carga | contexto de usuario confiable, clase de archivo, MIME declarado, bytes/stream, longitud | resultado tipado con ID opaco y hash validado; rechazar tamaño, MIME o rol no permitido |
| I02 Consultar metadatos | ID opaco + propósito de lectura | datos mínimos del propio tenant; 403/404 seguro para B |
| I03 Descargar | ID y autorización de operación | stream validado/URL temporal de alcance limitado solo donde aplique |
| I04 Exportar documento | ámbito de consulta, formato allowlisted, autorización real | documento cuyo contenido procede del tenant autorizado, límites y checksum |
| I05 Borrar/revocar | ID y permiso específico de borrado, motivo/auditoría | tombstone/revocación gobernada, no hard-delete cruzado |
| I06 Listar | consulta paginada limitada al tenant y tipos autorizados | IDs opacos, no listado global de buckets |
| I07 Verificar integridad | checksum esperado + bytes/objeto recibido | éxito solo al comparar contenido real; error seguro ante mismatch |
| I08 Gestionar error | fallo proveedor, timeout, quota o conexión | error acotado, rollback/compensación futura, sin secret leak |
| I09 Publicar medio permitido | recurso con clasificación explícita pública, TTL/cache y policy | URL/control de exposición mínimo; nunca por simple nombre de archivo |
| I10 Registrar auditoría | correlación opaca, actor, tenant resuelto server-side, acción | evento sin PII/archivo/URL firmada ni credenciales |
| I11 Seleccionar proveedor | política aprobada por ambiente + feature gate | adaptador vigente, no fallback a proveedor retirado por suposición |
| I12 Migrar/restaurar (futuro) | manifest, inventario real, hashes y respaldo previamente certificado | equivalencia de conteos/checksums y recuperación probada o NO_GO |

Estos doce contratos son **expectativas de futuras microtareas**, no APIs ya publicadas. El plan requiere evidencias MIME, tamaño, checksum y read/write; cualquier contrato definitivo necesita inspeccionar implementaciones y consumidores antes de escribir código.

## Fronteras de confianza y amenazas a verificar

**F01 cliente→API:** extensiones, headers, slugs, `EmpresaId`, roles y MIME provienen de una fuente no confiable; la autenticación/autorización efectiva es del servidor. **F02 API→Application:** identificar permisos por operación y owner real; no resolver tenant por argumento arbitrario. **F03 Application→Infrastructure:** contrato tipado sin SDK específico, proveedor aislado. **F04 Infrastructure→proveedor:** credenciales fuera de Git, allowlist de destino, timeouts y límites de streaming. **F05 proveedor→descarga:** respuesta y metadatos verificables, con TTL/alcance adecuado. **F06 exportación→auditoría:** contenido y PII reservados, logs minimizados. **F07 migración futura:** no copiar/borrar datos sin inventario, respaldo, prueba de restauración y permiso comercial explícito.

## Criterios positivos futuros — NO_EJECUTADOS

**P01:** usuario A sube medio permitido, checksum de bytes coincide y solo A puede recuperarlo. **P02:** servicio exporta PDF del tenant autenticado con contenido correcto y metadatos seguros. **P03:** cliente público lee solamente medio publicado bajo política explícita. **P04:** fallo efímero de proveedor activa compensación sin objeto parcial visible. **P05:** auditoría registra operación y correlación sin datos secretos.

## Criterios negativos futuros — NO_EJECUTADOS

**N01:** usuario A intenta leer ID de B y es rechazado; **N02:** caller sin JWT intenta upload privado; **N03:** rol lectura intenta delete; **N04:** modifica `EmpresaId` o slug; **N05:** cambia MIME sin cambiar firma real del contenido; **N06:** sobrepasa tamaño/cuota; **N07:** checksum no coincide; **N08:** intenta path traversal; **N09:** usa URL expirada o de otro tenant; **N10:** error del proveedor muestra secreto en respuesta/log; **N11:** intenta exportar información de empresa B; **N12:** solicita iniciar migración a R2 sin autorización/restore ni backup. Rechazo/compensación segura exigidos en futuras suites; **no son resultados PASS**.

## Integración, precedencia y comprobaciones de esta microtarea

**A0:** documento limita el trabajo a dos Markdown, sin secretos, permisos ni cambios de runtime. **A1:** I01–I12, P01–P05 y N01–N12 delimitan comportamiento observable, no se ejecutan. **A2:** revisar fronteras JWT/RBAC/tenant, MIME, cuotas, checksum, links y auditoría solo a nivel contractual; sin declarar test de seguridad real. **A3:** coherencia con M01.01, skill, arquitectura actual, plan V5 y prerequisitos documentales de `DAG_TECNICO` sin inferir autoridad de infraestructura. **A4:** commit fast-forward serializado en dev, SHA exacto Scope Lock/V5 CHANGELOG en completed/success y readback PLAN/COLA/BITACORA; compilaciones generales no causales = NO_APLICA al Markdown, no PASS. **A5:** aprobación independiente por uno de los otros cinco TASK_ID Javier antes de gates posteriores.

**Rollback:** `git revert` normal del documento y sección de CHANGELOG, sin force-push. No hay llamadas a APIs, cambios en Cloudinary, Cloudflare, MySQL, QA/main/PROD, workers, Tasks, DNS, secretos, dominios, ni pagos.
