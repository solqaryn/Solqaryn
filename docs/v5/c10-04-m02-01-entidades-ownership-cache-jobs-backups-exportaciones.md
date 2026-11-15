# SOLQARYN V5 — C10-04.M02.01: entidades, ownership, relaciones e invariantes de caché, jobs, backups y exportaciones

**Entrega:** contrato técnico DOCUMENTAL de diseño M02.01, sin modelos EF, DDL, migraciones, servicios, endpoints ni ejecución. **Autor:** PRIMARY DEV 03 — JAVIER, TASK_ID 6adc2e4bd8a48191ac7d8641ed95a1a3. **Repositorio:** solqaryn/Solqaryn, rama dev, PROJECT_SCOPE_LOCK=STRICT. **Fecha:** 2026-11-15.

**Fuentes de autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA); PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA (Sheet 1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU); AGENTS.md, Skill de gobierno canónica y docs/PROJECT_SCOPE_LOCK.md. Los seis C10-04.M01.01–06 están LISTO con A5 independiente; la revisión de M01.06 fue emitida por DEV03, distinto de su autor DEV06. El DAG F=VALIDADO/G=SI y COLA G/O=SI habilitan **solo** esta unidad de preparación M02.01, sin permisos comerciales ni transición automática a M03, IAM, QA/main/PROD.

## 1. Evidencia real y límites de inferencia

| Fuente existente en dev | Blob Git cotejado | Hecho comprobado y frontera |
| --- | --- | --- |
| backend/src/Application/Interfaces/IPublicStoreCache.cs | 15aeb1c7f4afa6c3b8422b3bd0700d345446f3f8 | Contrato GetOrCreateAsync, InvalidateAll e InvalidateTenant; segmentos de storefront y TTL explícitos. No constituye caché privada genérica. |
| backend/src/Infrastructure/Services/PublicStoreMemoryCache.cs | 1a102138b88bd54989eeade5acf7a5ee7bf63867 | Caché de memoria segmentada por tenant, generaciones y hash de parámetros, con lock por clave; no acredita invalidación entre instancias. |
| backend/src/Application/Services/OutboxRetryProcessor.cs | 542c0a655a3ee0698e3d80f1854d43903b6470fa | Procesador de lote con política de intentos y backoff; no constituye un scheduler universal ni autoriza un worker nuevo. |
| backend/src/Application/Interfaces/ICompraDocumentoStorageService.cs | 454d588851ecc7db810259e63705852360172928 | Firmas tenant-aware con default NotSupportedException; URLs/publicId legacy no confieren propiedad ni permiso. |
| docs/v5/c10-04-m01-03-inventario-codigo-cache-jobs-backup-exportaciones.md | da3ab0ef3606bae512038caeffa3160c427995af | Diez fuentes inventariadas y doce brechas de evidencia; son pendientes, no bugs confirmados. |
| docs/v5/c10-04-m01-06-gates-rollback-liberacion-cache-jobs.md | bbf0bfaa75d46362c2e5504d6f0a1840d27bda18 | Contrato B01–B14, rollback y P01–P06/N01–N10 explícitamente futuros, no ejecutados. |

No se inspeccionó ningún objeto de clientes, bucket, copia real, credencial o base productiva. Cloudinary es dependencia existente; R2 y Hostinger son objetivos no activados. Un listado de modelos propuestos no demuestra persistencia, seguridad de lectura HTTP ni restauración operativa.

## 2. Modelo lógico propuesto (NO son entidades C# ni tablas existentes)

| Objeto conceptual | Propiedad, identidad y cardinalidad | Restricción de negocio y seguridad |
| --- | --- | --- |
| ParticionCacheTenant | Ambiente + empresa backend verificada + segmento + versión; una empresa tiene N segmentos | EmpresaId derivada del principal validado y recurso persistido, no de header/host/client; un tenant jamás comparte namespace con otro |
| EntradaCacheVersionada | Clave opaca por partición, hash de parámetros, generación y vencimiento | Valores privados nunca sirven al usuario no autorizado; publicación posterior a permiso y revisión de versión |
| EpochInvalidacion | Contador lógico por partición/segmento, monotonía por cambio autorizado | Invalidación debe impedir reutilizar datos viejos, incluso ante revocación; sincronización distribuida es fase futura |
| SolicitudTrabajo | ID opaco + empresa + actor + operación permitida + payload hash | Estado inicial sin efecto; no se acepta objeto ni permiso arbitrario desde navegador |
| IntentoTrabajoLease | Relación N:1 con SolicitudTrabajo, lease token/version y deadline | Un solo consumidor gana reclamo atómico; no tomar el mismo lease por mera antigüedad |
| RegistroIdempotencia | Clave única por empresa + actor + operación + hash de contenido + versión | Misma clave/carga devuelve el mismo resultado; distinta carga produce conflicto sin segundo efecto |
| EstadoReintento | Contador, próxima fecha, categoría de error redactada y máximo acotado | Fallo transitorio no provoca tormenta; agotamiento deja evidencia durable para revisión |
| SolicitudExportacion | Empresa, actor, filtro validado, recurso, permiso y cuota | La preparación de archivo no confiere permiso de descarga ni acceso entre empresas |
| ArtefactoExportacion | ID opaco, solicitud, hash, tamaño, MIME, expiración y estado | Solo disponible tras resultado íntegro; sanear CSV, URL de duración corta y no-store |
| ManifiestoBackup | Ambiente, alcance, fecha, versión esquema, hash de artefacto, retención | Requiere autorización segregada y cifrado; no derivar RPO/RTO de la existencia de un archivo |
| EnsayoRestoreEfimero | Manifiesto 1:N, resultado, conteos, hashes y mediciones verificables | Nunca aplicar restore en producción; resultados incompletos o divergentes son NO_GO |
| EvidenciaAuditoria | Evento append-only de actor, operación, tenant, correlación, decisión y tiempo UTC | Datos mínimos, sin JWT, OTP, DSN, URLs bearer, PII de clientes ni borrado silencioso |

Las relaciones son objetivos a validar con modelos EF actuales antes de M03: Empresa 1:N particiones/solicitudes/exportaciones/manifiestos; SolicitudTrabajo 1:N intentos; SolicitudExportacion 0:1 artefacto confirmado; ManifiestoBackup 0:N ensayos efímeros. Ninguna FK, índice, enum o migración se materializa en este cambio.

## 3. Dieciocho invariantes contrastables antes de codificar

- OWN01: la autoridad tenant procede de identidad, membresía y propiedad persistida verificadas en backend; nunca de query, slug, X-Empresa-Id ni JWT aislado.
- OWN02: todo recurso privado tiene exactamente una empresa propietaria; NULL/legacy ambiguo se niega y se pone en cuarentena.
- OWN03: particiones de caché privadas incluyen empresa, ambiente, segmento, permisos y versión; prohibidas claves globales que mezclen A/B.
- OWN04: el storefront público actual no se reinterpreta como autorización de exportación o acceso al ERP.
- OWN05: una invalidación por revocación se debe observar antes de la siguiente lectura privilegiada; la caché nunca reemplaza RBAC actual.
- OWN06: tiempo de expiración y cuotas se aplican en servidor, son finitos y no ampliables por el cliente.
- OWN07: cada trabajo exige permiso explícito, identidad confiable y propiedad del recurso antes de aceptar la solicitud.
- OWN08: idempotencia combina empresa, operación, actor, versión y payload normalizado; una misma clave con distinta carga es conflicto.
- OWN09: lease atómico y versión impiden duplicidad de efectos con dos consumidores o retry tras crash.
- OWN10: backoff, deadline y máximo de intentos acotan el trabajo; un retry no cambia tenant o scope.
- OWN11: un trabajo revocado o cancelado deja evidencia y no reanuda privilegios sin nueva autorización.
- OWN12: una exportación verifica RBAC, pertenencia, límites y sanidad CSV antes de generar o permitir descarga.
- OWN13: archivos parciales y artefactos de tamaño/hash incorrecto nunca pasan a CONFIRMADO.
- OWN14: URLs/identificadores de almacenamiento no constituyen permiso; enlaces temporales son restringidos y revocables.
- OWN15: todo backup futuro requiere entorno, manifiesto, cifrado y huella real; no se publica un respaldo sin evidencia durable.
- OWN16: todo restore futuro se ensaya en recursos efímeros y compara integridad, schema y aislamiento A/B antes de reclamar recuperabilidad.
- OWN17: la indisponibilidad de MySQL, proveedor, caché, auditoría o MFA exigible produce resultado seguro; no fallback permisivo ni 2xx ficticio.
- OWN18: un LISTO documental y su A5 solamente alimentan el DAG por fórmula; no conceden permiso a SQL/worker/eventos/webhook/R2/QA/main/PROD.

## 4. Estados y transiciones FUTURAS, sin implementar

**Trabajo:** PROPUESTO → AUTORIZADO → RECLAMADO → ENTREGADO; en fallo RECLAMADO → REPROGRAMADO (hasta tope) o BLOQUEADO/COMPENSADO. Reapertura exige nueva evaluación de identidad, lease y permiso. **Exportación:** SOLICITADA → VALIDADA → GENERANDO → CONFIRMADA o RECHAZADA/REVOCADA; artefactos parciales jamás son visibles. **Ensayo restore:** PLANIFICADO → VERIFICANDO → APROBADO_SOLO_EFIMERO o FALLIDO. Ninguna transición futura es una Task programada ni una llamada entre ejecutores Javier.

## 5. Matriz positiva/negativa a ejecutar posteriormente — FUTUROS_NO_EJECUTADOS

| Caso | Afirmación positiva futura | Denegación negativa futura |
| --- | --- | --- |
| P01/N01 | Empresa A cachea su segmento con TTL válido | Principal B o header B no lee contenido A |
| P02/N02 | Invalidación A actualiza generación de A | Token revocado no obtiene valor obsoleto |
| P03/N03 | Un trabajo con grant vigente se reclama una vez | Dos workers no ganan el mismo lease |
| P04/N04 | Retry idéntico devuelve mismo efecto | Mismo ID con payload diferente se rechaza |
| P05/N05 | Un error transitorio reprograma con backoff | Cuota o máximo agotado no ejecuta de más |
| P06/N06 | Exportación de filas A aplica RBAC y cuotas | Usuario B o sin permiso no descarga A |
| P07/N07 | CSV sintético escapa fórmula potencial | Fórmula hostil no llega al usuario |
| P08/N08 | Snapshot futuro restaura en DB efímera con hash | Copia corrupta y restore en PROD se deniegan |
| P09/N09 | Job cancelado conserva log mínimo saneado | Log con JWT, PII o URL firmada falla gate |
| P10/N10 | Contratos conservan interfaz Cloudinary existente | Flags futuros no activan R2, webhook o migración |

Todos los P/N son **oráculos de diseño, NO pruebas xUnit/HTTP/MySQL/Playwright ejecutadas ni resultados PASS**. En M03–M05 se exigirán casos reales con .NET 10, EF/MySQL efímero, pruebas de concurrencia e identidad/tenant A/B, CI exact-SHA y revisión A5 independiente. No asumir caché distribuida, restore o exportación genérica ya implementados.

## 6. Riesgo, impacto y pruebas por puerta

**Impacto actual:** solo este Markdown y sección inseparable de CHANGELOG_AI.md. Domain/Application diseñarían invariantes e idempotencia; Infrastructure evaluaría cache/Outbox/efectos, EF y adaptadores; API exigiría JWT/RBAC, membresía, recursos y cuotas; Angular mostraría estados sin decidir permisos. Ningún servicio nuevo, trabajo externo, estructura DB, secreto, scheduler o proveedor se agrega ahora.

**A0:** cotejo de ID, cinco antecedentes con A5, M01.06 independiente, fuente V5, Scope Lock, archivos y ausencia de secretos; inspección del diff y de la sección CHANGELOG. **A1:** presencia coherente de 12 entidades, 18 reglas y 10 pares P/N *de diseño*, no funcionalidad runtime. **A2:** cada operación niega cross-tenant A/B, permisos revocados, URL ajena, idempotencia falsa, leakage y restore no autorizado. **A3:** compatibilidad conceptual con IPublicStoreCache, PublicStoreMemoryCache, OutboxRetryProcessor e ICompraDocumentoStorageService; compilación y MySQL son NO_APLICA para Markdown-only, no PASS. **A4:** commit fast-forward con compare de HEAD, verificar cambios y blobs, Scope Lock y chequeo V5 completed/success en SHA publicado y lectura posterior PLAN/COLA/DAG/BITACORA. **A5:** otra Task Javier (TASK_ID distinto) revisa fuente, independencia y gates antes de la fase siguiente.

**Rollback documental:** git revert normal del commit tras reconciliar HEAD y dependencias, sin rewrite, sin editar BITACORA pasada ni afectar datos. No se autoriza R2, Hostinger, backups reales, workers, dispositivos, compras, QA/main/PROD.
