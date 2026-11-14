# SOLQARYN V5 — C10-02.M01.01: objetivo, no-alcance y no-regresión de R2 y dominios de archivos

**Unidad:** C10-02.M01.01, PLAN_MAESTRO fila 1478. **Autor:** PRIMARY DEV 04 JAVIER / TASK_ID 6aeacb7820ac8191b7d768a6f33dfa09. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA). **Rama:** solqaryn/Solqaryn@dev. **PROJECT_SCOPE_LOCK:** STRICT. **Fecha:** 2026-11-14.

## Objetivo verificable

Definir el contrato PREPARATORIO de objetos privados, buckets y prefijos aislados por tenant, dominio de archivos, enlaces temporales firmados, expiración, replay, retención y lifecycle **sin activar ni migrar infraestructura**. La salida pretendida del plan C10-02 es «Buckets, prefijos tenant, URLs firmadas, expiración y lifecycle sin depender todavía del cutover de infraestructura» y su evidencia mínima es «Acceso privado, prefijos, replay y expiración».

Este cambio es **documental M01.01**. No crea bucket, dominio, secreto, URL, tarea de lifecycle, endpoint, worker, webhook, objeto, cuenta de pago, infraestructura o migración. Cloudflare R2/Hostinger son objetivos condicionales futuros; la arquitectura vigente documenta Cloudinary, QuestPDF y SMTP. No se certifica el proveedor real de un ambiente leyendo este objetivo.

## Dependencias y fuentes revisadas

- El DAG individual de C10-02.M01.01 declara F=VALIDADO/G=SI para preparación documental, con antecedentes M01.01–05 de C10-01, C02-01/02/03/04 y C03-01/02/03/04. Esta puerta no autoriza M01.06, IAM, migraciones, código M03 ni QA/main/PROD.
- `docs/v5/c10-01-m01-01-objetivo-no-alcance-storage-neutral.md` define tenant y propietario server-side, MIME/tamaño/checksum, objetos opacos, lectura/escritura y reversión. `docs/v5/c10-01-m01-05-criterios-aceptacion-storage-neutral.md` exige escenarios de aislamiento A/B, URL expirada, error del proveedor, auditoría y fallback seguro. Son antecedentes documentales, no evidencia de que R2 esté operativo.
- La Skill canónica y `references/architecture.md` mantienen Angular 22.2.1, ASP.NET Core 10, capas Domain/Application/Infrastructure/API, MySQL/EF Core y controles JWT/RBAC/tenancy obligatorios en backend.
- El inventario de objetos reales, el proveedor activo por entorno y la condición de migración requieren validación separada; **no se deducen cantidades ni credenciales** de este plan.

## Dentro y fuera del alcance

**Diseño futuro:** identidad opaca del objeto, owner/tenant determinado por servidor, espacio de nombres por tenant, permiso independiente por operación, generación de enlace efímero restringido por recurso/método, política de firma/TTL/revocación, límites MIME/tamaño/hash, auditoría saneada, preservación de retención y rollback de una futura migración expresamente autorizada.

**Fuera de esta microtarea:** crear/cambiar Cloudflare R2, DNS, CORS, buckets, tokens, dominios, certificados, clientes SDK, datos reales, repositorios, secretos, MySQL/EF, endpoints, despliegues, workers, webhooks, planes de pago, movimiento Cloudinary→R2, relanzar otras Tasks o publicar a QA/main/PROD. Un enlace firmado sigue siendo una capacidad bearer reutilizable dentro del TTL salvo que un mecanismo server-side adicional imponga one-time con estado/idempotencia; no se atribuye prevención de replay nativa.

## Invariantes NR01–NR14 de no-regresión

| ID | Contrato futuro verificable | Incumplimiento que obliga a NO_GO |
| --- | --- | --- |
| NR01 | Tenant/owner se obtienen del contexto autorizado del backend | Usar `EmpresaId`, slug o header del cliente como autoridad |
| NR02 | Objeto público identificado por clave opaca | DTO revela bucket, key, ruta física o nombre secreto |
| NR03 | Prefijos separan objetos de A/B sin colisiones | A puede enumerar, leer o sobrescribir clave de B |
| NR04 | Credencial y bucket de cada ambiente fuera del repo | Token en Git, logs, HTML o URL |
| NR05 | Permisos lectura, upload, delete y export separados | Permiso read se convierte en write/delete |
| NR06 | Enlace privado limita objeto, verbo y TTL | URL permanente, ampliable o con scope de otro recurso |
| NR07 | Replay se decide y ensaya explícitamente | Se declara one-time basándose solo en TTL de una URL bearer |
| NR08 | Expiración/revocación no se eluden vía caché A/B | Objeto B se sirve en sesión A o link caducado en caché |
| NR09 | MIME real, tamaño/stream y hash se verifican | Solo extensión o header del usuario como control |
| NR10 | Lifecycle preserva retención legal y recuperación | Borrado automático de dato sujeto a conservación |
| NR11 | Errores no enumeran la existencia de objetos ajenos | 403/404 o telemetría filtran metadata privada |
| NR12 | Cutover exige inventario, hashes y rollback probado | Migración/destrucción sin aprobación separada |
| NR13 | Auditoría conserva correlación y acción, sin secretos | Log revela URL firmada, token, bytes o PII |
| NR14 | M01 documental nunca acredita runtime | Marcar P/N como pruebas ejecutadas o activar R2 por el DAG |

## Escenarios de aceptación y rechazo FUTUROS — NO_EJECUTADOS

**Positivos (P):** P01 firma de lectura para objeto A y rol autorizado, con TTL/metodo/recurso acotados; P02 operación de upload valida bytes/MIME/checksum y cuota; P03 enlace caduca en reloj controlado y se rechaza después; P04 cambios A→B limpian caché y estado anterior; P05 proveedor efímero falla sin publicar objeto parcial; P06 una migración expresamente autorizada conserva hashes y demuestra reversión.

**Negativos (N):** N01 A intenta leer objeto B; N02 JWT ausente; N03 rol de lectura intenta borrar; N04 cliente manipula tenant/prefijo/traversal; N05 URL caducada o método equivocado; N06 enlace reusable permite replay dentro de TTL, se registra y requiere decisión explícita (no fingir mitigación); N07 content-type benigno pero bytes prohibidos; N08 tamaño excedido produce objeto parcial; N09 caché cross-tenant; N10 log expone firma/token/PII; N11 lifecycle borra archivo retenido; N12 se solicita R2/DNS/cutover sin permiso.

Todos P01–P06/N01–N12 son **oráculos propuestos, NO EJECUTADOS**. Requieren fixtures A/B y CI causal .NET/Angular/MySQL/servicio efímero cuando se implemente. No constituyen PASS actual.

## Impacto, auditoría y rollback

**Capas y datos:** solo `docs/v5` y `CHANGELOG_AI.md`; ninguna API/DTO, tabla, columna, permiso, frontend, persistencia ni componente se altera. **Seguridad:** aislamiento tenant, backend JWT/RBAC, URLs bearer, expiración, replay, PII/secretos y retención quedan especificados, no implementados. **Observabilidad:** eventos sanitizados son requisito futuro, no instrumentación actual.

**A0:** alcance/owner/fuente/DAG y exclusiones. **A1:** NR y escenarios verificables documentados, sin falsear ejecución. **A2:** denegación por defecto, tenant A/B y riesgo de replay explícitos. **A3:** compatibilidad documental con C10-01 y arquitectura; CI runtime `NO_APLICA` por diff de Markdown. **A4:** commit fast-forward serializado y controles Scope Lock/procedencia V5 `completed/success` para SHA exacto; readback PLAN/COLA/BITACORA. **A5:** otra Task Javier de ID distinto revisará posteriormente; mantener `A5_PENDIENTE`.

**Rollback:** `git revert` aditivo del commit documental, sin force-push ni modificación de datos o infraestructura.
