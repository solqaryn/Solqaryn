# SOLQARYN V5 — C10-04.M02.03: diseño de persistencia, índices, migraciones y fixtures para caché, jobs, backups y exportaciones

**Naturaleza:** contrato de diseño exclusivamente DOCUMENTAL. **Autor:** PRIMARY DEV 03 JAVIER, TASK_ID 6adc2e4bd8a48191ac7d8641ed95a1a3. **Fecha:** 2026-11-15. **Repositorio:** solqaryn/Solqaryn rama dev, PROJECT_SCOPE_LOCK=STRICT. No hay DDL, EF migration, cambio de modelos, conexión a MySQL, dump, restore, eventos, workers ni exportaciones ejecutados en esta entrega.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA; Sheet 1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU (PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA). Al reclamo de M02.03 se comprobó PENDIENTE sin owner, COLA G/O=SI, DAG F=VALIDADO/G=SI y seis M01 LISTO+A5 independiente. Los otros documentos M02 son independientes: este diseño no presupone el cierre de M02.01 ni M02.02 ni libera M02.06.

## 1. Baseline observado, sin extrapolar certificación

| Fuente Git en dev | Blob verificado | Evidencia limitada |
| --- | --- | --- |
| backend/src/Application/Services/OutboxRetryProcessor.cs | 542c0a655a3ee0698e3d80f1854d43903b6470fa | Ya existe un procesador Application de lotes con intentos, stale claim, jitter, backoff y dependencia de repositorio; **no** es un scheduler genérico activo. |
| backend/src/Application/Interfaces/IPublicStoreCache.cs | 15aeb1c7f4afa6c3b8422b3bd0700d345446f3f8 | Contrato de caché de tienda y TTL para segmentos públicos; no equivale a tabla de caché privada. |
| backend/src/Infrastructure/Services/PublicStoreMemoryCache.cs | 1a102138b88bd54989eeade5acf7a5ee7bf63867 | Adapter IMemoryCache de un proceso, clave tenant/segmento/generación/hash; la coherencia entre nodos no está probada. |
| backend/src/Application/Interfaces/ICompraDocumentoStorageService.cs | 454d588851ecc7db810259e63705852360172928 | Contrato tenant-aware de documentos con default fail-closed; publicId y URL legacy no bastan para acceso. |
| docs/v5/c10-04-m01-03-inventario-codigo-cache-jobs-backup-exportaciones.md | da3ab0ef3606bae512038caeffa3160c427995af | Brechas G01–G12 sobre caché, lease, exports, backup y restore; son pendientes, no defectos ni pruebas realizadas. |
| docs/v5/c10-04-m01-06-gates-rollback-liberacion-cache-jobs.md | bbf0bfaa75d46362c2e5504d6f0a1840d27bda18 | Gates B01–B14 y rollback, todos conceptuales. |

No se inspectan credenciales, bases Aiven, Cloudinary ni objetos privados. Los siguientes nombres de estructuras son **candidatos para diseño futuro**, no entidades EF ni tablas reales, y deben reconciliarse con el esquema vigente antes de autorizar cualquier migración.

## 2. Catálogo conceptual de almacenamiento y relaciones

| Proyección lógica (FUTURA) | Clave e índices candidatos | Invariante de propiedad y retención |
| --- | --- | --- |
| CacheGeneration | (Ambiente, EmpresaId, Segmento) UNIQUE; secuencia Version | EmpresaId verificada por servidor; no usar como tabla de caché para contenido privado sensible |
| TrabajoSolicitud | Id opaco PK; (EmpresaId, ActorId, Operacion, IdempotencyKey) UNIQUE acotado | El payload hash/version acompaña idempotencia; clave repetida con carga distinta = conflicto |
| TrabajoIntento | Id PK; (EmpresaId, SolicitudId, NumeroIntento) UNIQUE; índice (Estado, ProximaEjecucionUtc) | FK compuesta con EmpresaId; lease/version no permite dos propietarios concurrentes |
| TrabajoLease | (EmpresaId, SolicitudId) UNIQUE; LeaseVersion CAS y VenceUtc | No se reclaman leases expirada sin validación de transacción actual y owner |
| ExportacionSolicitud | Id PK; (EmpresaId, ActorId, Estado, CreadoUtc), y (EmpresaId, RecursoId) | Filtro validado y recurso propio; datos completos no se guardan sin cifrado y cuota |
| ExportacionArtefacto | Id PK; (EmpresaId, SolicitudId, Version) UNIQUE | Hash y locator privado, estado publicado solo al completar y autorizar |
| ManifiestoRespaldo | Id PK; (Ambiente, EmpresaId, CreadoUtc), checksum inmutable | El manifiesto no significa que haya backup/restore operativo; retención y cifrado requieren aprobación |
| EnsayoRestauracion | Id PK; (ManifiestoId, AmbienteEfimero, CreadoUtc), resultado y hash | Prohibido destino PROD; acceso segregado con prueba integridad y tenant A/B |
| AuditoriaOperacion | Id PK; (EmpresaId, CorrelationId, CreadoUtc) acotado | Append-only de metadatos mínimos; sin tokens, passwords, URL firmada ni PII excesiva |

La lista de índices es **hipótesis de diseño**. Se requieren EXPLAIN, cardinalidad, presupuesto de índices, constraints y pruebas MySQL 8.4 antes de implementarla. Las FK compuestas (EmpresaId, Id) deberán examinar compatibilidad con tablas existentes; no se inventan nombres ni se alteran constraints del ERP. No hay creación de filas ni backfill.

## 3. Doce reglas persistentes para futura implementación

- PERS01: todos los registros comerciales privados portan empresa propietaria positiva verificada desde backend, no desde un encabezado o parámetro de navegador.
- PERS02: empresa A y B no comparten claves de trabajo, idempotencia, exportación, caché ni artifact locator aunque coincidan sus IDs locales.
- PERS03: versiones de autorización, tenant, permiso y esquema se revalidan antes de cada mutación y lectura privilegiada.
- PERS04: clave de idempotencia incluye actor, empresa, operación, payload normalizado y versión; reutilización distinta falla cerrada.
- PERS05: reclamación de trabajo se hará por actualización condicional con versión y transacción, jamás por SELECT seguido de UPDATE incondicional.
- PERS06: lease vencido no reautoriza permisos caducados, jobs abandonados ni acciones entre empresas.
- PERS07: la transición a resultado CONFIRMADO y evidencia durable es atómica para efectos financieros o de exportación.
- PERS08: el máximo de intentos, deadline, tamaño y cuota se valida en servidor; un error de proveedor conserva estado recuperable y no genera retry infinito.
- PERS09: dato legado sin EmpresaId inequívoco se mantiene en cuarentena, no se asigna tácitamente a la empresa predeterminada.
- PERS10: artefactos parciales, hashes no verificados y vínculos expirados nunca se exponen por ruta, cache o respuesta HTTP.
- PERS11: backfill, tablas nuevas e índices solo ocurrirían con migración reversible aprobada, datos sintéticos/efímeros y test exact SHA.
- PERS12: backup y restore requieren integridad, cifrado y permisos segregados; la mera existencia del script o manifiesto no es éxito ni RPO/RTO medido.

## 4. Contrato expand/contract FUTURO (SIN ejecutar)

1. **Preflight y modelo:** reconciliar entidades, esquema y FK reales; verificar que no existe reutilización de tabla/índice, definir ownership, DB efímera y presupuesto.
2. **Expand:** añadir estructuras o campos nullable solo si los contratos vigentes lo admiten y existe autorización específica; sin cambiar lecturas del ERP de golpe.
3. **Cuarentena histórica:** identificar filas legacy con identidad ambigua; jamás inventar EmpresaId ni copiar datos entre A y B.
4. **Validación:** poblar solo fixtures sintéticos de ambos tenants; comparar conteos, hashes, FK e intentos; probar concurrencia y rollback.
5. **Cutover controlado:** habilitar lecturas/escrituras nuevas únicamente tras CI de MySQL/EF, A0–A5, política comercial/seguridad y autorización de despliegue separada.
6. **Contract y reversión:** no suprimir rutas/columnas legacy sin plan de compatibilidad, punto de retorno y prueba real de restauración en ambiente efímero. No ejecutar DROP, force-push ni reinicialización productiva.

Se requiere un plan de backout que preserve historial y no deje referencias huérfanas. Los pasos anteriores son **plan futuro no autorizado**: ninguna migración, backup o restauración ha ocurrido por este texto.

## 5. Diez fixtures positivos/negativos FUTUROS_NO_EJECUTADOS

| Fixture | Aceptación positiva futura | Rechazo negativo futuro |
| --- | --- | --- |
| F01 | Tenant A obtiene entrada propia con clave A/segmento/version | Tenant B prueba misma clave o recurso de A |
| F02 | Invalidación A cambia generación observable | Reuso de valor anterior tras revocación |
| F03 | Dos consumidores reclaman una sola vez por versión | Ambos ganan el mismo lease sin CAS |
| F04 | Reintento idempotente igual conserva efecto único | Misma clave con distinto payload duplica efecto |
| F05 | Dead-letter registra el intento máximo y causa segura | Reintentos exceden cuota o filtran excepciones |
| F06 | Export autorizado A conserva hash y autoría | Export de B o CSV formula no se filtra/sanea |
| F07 | Export fallido queda parcial e inaccesible | URL de resultado parcial se sirve 200 |
| F08 | Backup efímero genera manifiesto/hash cotejable | Snapshot corrupto se declara restaurable |
| F09 | Restore efímero valida FK/tenants y conteos | Restore modifica PROD o mezcla empresas |
| F10 | Error de proveedor mantiene estado auditado | Fail-open, fallback Cloudinary/R2 implícito o sin log |

Todos son **escenarios propuestos**, NO hubo fixtures cargados en MySQL, test xUnit, CI MySQL, export o restore ejecutados. En una futura M03/M05 se exigirán pruebas reales con base MySQL 8.4 efímera, EF Core, caso concurrente, aislamiento tenant A/B, SHA exacto y condiciones de rollback.

## 6. Seguridad, pruebas A0–A5 y reversibilidad

**A0:** Scope Lock/Skill/HEAD, seis antecedentes M01 LISTO+A5, estado exclusivo del ID, fuentes y diff Markdown sin secretos. **A1:** revisar nueve proyecciones, 12 reglas, 6 pasos expand/contract y 10 fixtures P/N *de diseño*. **A2:** asegurar que ninguna regla admite JWT aislado, tenant inyectado, query-filter bypass, SQL en datos reales, fugas PII, fallback proveedor o acceso interempresa. **A3:** compatibilidad conceptual con caché pública, Outbox actual y documentos tenant-aware; builds .NET, pruebas MySQL y migraciones = NO_APLICA al diff Markdown (no PASS). **A4:** publicación fast-forward/CAS contra HEAD vivo, verificación de dos Markdown y SHA, CI Scope Lock y procedencia V5 completed/success exact-SHA, eventos y readback PLAN/COLA/DAG/BITACORA. **A5:** revisión por Task Javier distinta de la autora antes de liberar sucesoras.

**Reversión actual:** git revert normal del changeset documental, conservando historia y BITACORA. No modifica despliegue, seguridad IAM, tenant, secrets, MySQL real, Aiven, Cloudinary, Cloudflare R2, backups, QA/main/PROD ni tareas programadas.
