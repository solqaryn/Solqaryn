# SOLQARYN V5 — C10-02.M01.04: riesgos, supuestos y decisiones de R2 y dominios

**Unidad:** C10-02.M01.04, PLAN_MAESTRO fila 1481. **Autor:** PRIMARY DEV 04 / JAVIER_CHATGPT / 6aeacb7820ac8191b7d768a6f33dfa09. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Doc Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Repositorio:** `solqaryn/Solqaryn@dev`; PROJECT_SCOPE_LOCK=STRICT; fecha 2026-11-14.

## Alcance y evidencia disponible

Esta unidad documental enumera **riesgos potenciales R01–R12, supuestos falsables S01–S08 y decisiones abiertas D01–D08** para el objetivo futuro de C10-02: buckets privados, prefijos por tenant, acceso firmado temporal, expiración, replay y lifecycle sin cutover todavía. No define ni autoriza ningún despliegue, proveedor, URL, dominio, token, bucket ni política activa. La arquitectura actual declara integración Cloudinary y contratos tenant-aware, mientras Cloudflare R2/Hostinger siguen siendo objetivos condicionados.

Fuentes inspeccionadas: `docs/v5/c10-02-m01-01-alcance-r2-dominios-archivos.md` (blob `3359ab47c9d6a472d39c64a627da057ac01ddd1f`, NR01–NR14); `docs/v5/c10-02-m01-02-actores-interfaces-r2-dominios.md` (blob `185471a4de72653b6e3a199ee7a8aaa34ef3ea88`, A01–A14, I/O y fronteras); `docs/v5/c10-02-m01-03-inventario-fuentes-brechas-r2-dominios.md` (blob `2b3b5de9f410ef2e287e2863bef9ca83e9358400`, fuentes de código y G01–G12); contratos Cloudinary y `StorageTenantContext` ya auditados en M01.03. PLAN/DAG autorizan preparación documental, no runtime.

## Matriz R01–R12 — riesgos POTENCIALES, no hallazgos comprobados

| Riesgo | Escenario a impedir en fases de implementación | Evidencia futura necesaria |
| --- | --- | --- |
| R01 Cross-tenant | Prefijo de empresa A permite descubrir objeto B | Prueba A/B real de autorización backend y keys |
| R02 Confusión de autoridad | Header `EmpresaId`/slug público decide bucket sin contexto JWT | Caso negativo de manipulación de tenant |
| R03 Privacidad | Bucket o CDN expone URLs permanentes o políticas públicas | Inspección de ACL/bucket policy, escaneo de exposición |
| R04 Firmas | URL firmada admite método/key diferentes o no expira | Fixtures de firmas y reloj/TTL controlado |
| R05 Replay | URL bearer reusable se confunde con token one-time | Modelo explícito de replay, revocación y test con reuso |
| R06 Caché | CDN/proxy sirve objeto de A a B tras cambiar sesión | Cache partition/no-store e invalidación |
| R07 Paths | Nombre/prefijo permite traversal o colisiones | Fuzzing de keys, controles unicode y normalización |
| R08 Contenido | MIME de cliente oculta bytes prohibidos o archivo demasiado grande | Byte signatures, quotas, streaming e integridad |
| R09 Fuga de secretos | Token de firma o credenciales aparecen en trazas o repositorio | Negative log assertions y secret scanner |
| R10 Pérdida de datos | Lifecycle elimina objeto con retención/hold o migración parcial | Inventario, backup, política de retención, restauración |
| R11 Caída proveedor | Timeout/reintento causa duplicados u objeto parcial visible | Fault injection, idempotencia y compensación |
| R12 Cutover prematuro | DNS/R2 sustituye Cloudinary sin autorización ni rollback | Aprobación explícita separada, plan de reversión e integración |

Los riesgos son **hipótesis de análisis**, no vulnerabilidades confirmadas. La ausencia de prueba o de acceso a infraestructura no autoriza calificarlos como reparados ni como fallos reales.

## Supuestos S01–S08 — pendientes de verificación

| ID | Supuesto a contrastar | Método futuro de verificación |
| --- | --- | --- |
| S01 | Existe una política de objetos y tenants consistente entre medios/documentos | Inventario de consumidores y clasificación de datos |
| S02 | El contexto tenant de backend puede asociarse a cada objeto | Revisar `StorageTenantContext` y authorization tests A/B |
| S03 | Los proveedores de cada ambiente están inventariados | Revisión de configuración sin mostrar secretos |
| S04 | El proveedor futuro puede firmar acceso con TTL mínimo aceptable | Prototipo efímero sin datos productivos |
| S05 | La revocación requiere un gate más allá de TTL si es inmediata | Threat model y tests de token/caché |
| S06 | Retención legal aplica a categorías diferenciadas | Matriz legal/negocio aprobada por dueño competente |
| S07 | Hay un conjunto de objetos elegible para migración futura | Conteos/hash respaldados, sin inferir tamaño actual |
| S08 | El rollback podrá mantener escrituras nuevas coherentes | Ensayo de reconciliación y reversión en ambiente efímero |

**Ningún supuesto queda acreditado por su declaración.** Si un supuesto es falso, el siguiente diseño debe cambiar sin poner datos en riesgo.

## Decisiones D01–D08 — PENDIENTES (NO APROBADAS)

| ID | Decisión requerida antes de habilitar implementación o cutover |
| --- | --- |
| D01 | Elegir estrategia provider-neutral y interfaz exacta, con compatibilidad Cloudinary |
| D02 | Definir topología privada bucket/prefijos/ambientes y esquema de owner verificable |
| D03 | Determinar método de acceso temporal, expiración máxima y validación de firma |
| D04 | Resolver one-time/replay/revocación vs URL bearer reutilizable durante TTL |
| D05 | Establecer confianza de dominios/CDN/proxy, CORS y políticas de caché |
| D06 | Formalizar límites MIME/tamaño/checksum, cifrado y auditoría saneada |
| D07 | Acordar lifecycle por clasificación/retención, backup y restauración |
| D08 | Aprobar por separado R2/DNS/cutover/costos, migración, observabilidad y rollback |

D01–D08 son **preguntas a resolver en etapas futuras**, no permisos. No autorizar R2, DNS, compras, buckets, tokens o despliegue a partir de esta tabla.

## Escenarios de contraste FUTUROS — NO_EJECUTADOS

**Positivos P01–P06:** autorización A→A, B→B y denegación cruzada; URL firmada limitada por verbo/recurso/TTL; upload de bytes permitidos con MIME y checksum real; invalidación de caché al cambiar tenant; retención que bloquea lifecycle; reversión de una migración autorizada con comparación de hashes.

**Negativos N01–N12:** A lee key B; cabecera de tenant falsificada; rol solo lectura intenta delete; URL vencida; replay de URL reusable no detectado; prefijo con traversal; MIME falso; upload sobre límite; caché compartida A/B; secreto/URL en logs; lifecycle sobre objeto en hold; cutover/activación R2 no autorizados.

Estos son criterios de pruebas para fases siguientes, no resultados. No se ejecutó MySQL, xUnit, Playwright, integración R2 ni ensayo de migración en esta M01 documental. La prueba runtime NO_APLICA causalmente al diff de Markdown; Scope Lock y procedencia del CHANGELOG sí son controles pertinentes exigidos por SHA.

## Seguridad, capas, A0–A5 y reversibilidad

**Impacto real:** un documento nuevo y sección indivisible de `CHANGELOG_AI.md`, sin alterar API, Application, Domain, Infrastructure, frontend, persistencia, JWT/RBAC, Cloudinary, R2, IAM, datos, DNS, producción ni cuentas. **A0:** autoridad del STANDARD, Scope Lock, owner, DAG y archivos restringidos. **A1:** consistencia entre 12 riesgos, 8 supuestos, 8 decisiones y antecedentes. **A2:** amenazas tenant A/B, bearer URL, TTL, replay, secretos, caché y retención, sin cambiar seguridad activa. **A3:** interoperabilidad documental con C10-01 y C10-02 M01.01–03; tests de runtime NO_APLICA al diff. **A4:** publicación fast-forward serializada en dev; checks `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` exitosos exact-SHA, con readback PLAN/COLA/BITACORA. **A5:** revisión independiente por otra Task Javier distinta de autora, posterior y registrada; nunca autoaprobación.

**Rollback:** `git revert` del commit documental con nuevo commit, sin force-push, sin cambios de objetos reales ni historial de BITACORA. El estado M01.04 no habilita terminal M01.06/M03, migraciones ni QA/main/PROD.
