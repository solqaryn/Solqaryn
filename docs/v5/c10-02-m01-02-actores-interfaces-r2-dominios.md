# SOLQARYN V5 — C10-02.M01.02: actores, entradas, salidas y fronteras de R2 y dominios de archivos

**Alcance:** C10-02.M01.02, fila 1479 de PLAN_MAESTRO; preparación documental M01, no implementación. **Autor:** PRIMARY DEV 04 / Task 6aeacb7820ac8191b7d768a6f33dfa09. **Fuente única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Rama:** solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT, 2026-11-14.

## Propósito y límite de certeza

Identificar actores, entradas, salidas, consumidores, interfaces **candidatas** y superficies de confianza de un futuro módulo para buckets privados, prefijos por tenant, dominios y URLs de acceso limitado, caducidad, prevención/medición de replay y lifecycle. Resultado esperado del C10-02: acceso privado, prefijos, replay y expiración sin cutover de infraestructura.

**TODOS los contratos descritos son candidatos NO_IMPLEMENTADOS**. No se han habilitado R2, DNS, credenciales, cuentas, secrets, rutas web, workers, lifecycle ni endpoints. Cloudinary es la integración de medios actualmente documentada; su activación efectiva por entorno requiere comprobación de configuración. El documento de C10-02.M01.01 determina NR01–NR14, entre ellos tenant desde el backend y URLs bearer que no son automáticamente de un solo uso.

## Matriz de actores A01–A14 (roles y componentes conceptuales)

| ID | Actor / límite de autoridad | Acceso admitido en diseño futuro |
| --- | --- | --- |
| A01 | Usuario final autenticado de tenant A | Solicitar acceso a objeto propio según permisos efectivos |
| A02 | Usuario final de tenant B | Solo objetos B; nunca inferir permiso de URL/key A |
| A03 | Administrador de tenant | Políticas para su empresa, sin privilegio global de storage |
| A04 | Gestor de documentos tenant | Crear/reemplazar según RBAC, sin acceso irrestricto a otros tenants |
| A05 | Lector de documentos | Lectura autorizada, sin permiso de upload/delete |
| A06 | Auditor tenant | Evidencias metadatos permitidas y redactadas; no URLs secretas |
| A07 | Operador global de plataforma | Operación privilegiada auditada y explícita; no bypass automático |
| A08 | Cliente Angular 22.2.1 | UI/estado del usuario; ningún secreto ni juicio final de autorización |
| A09 | API ASP.NET Core 10 | JWT, RBAC, owner y tenant server-side, firma delegada de acceso |
| A10 | Application Service | Caso de uso/contrato independiente del SDK y del bucket |
| A11 | Infrastructure storage adapter | Acceso a proveedor autorizado por ambiente, con mínimo privilegio |
| A12 | Cloudinary (integración documentada vigente) | Proveedor actual sujeto a configuración y contratos existentes |
| A13 | Cloudflare R2 (destino FUTURO) | Objetivo de arquitectura, no infraestructura ni tokens activos |
| A14 | Cache/CDN/proxy o lifecycle futuro | Frontera no confiable o sensible; respetar TTL, tenant y retención |

## Entradas y salidas conceptuales I01–I12/O01–O12

| ID | Entrada al caso de uso futuro (no endpoint existente) | Salida/decisión obligatoria |
| --- | --- | --- |
| I01/O01 | JWT y principal autenticado | Identidad/tenant real resueltos por backend o denegación segura |
| I02/O02 | ID opaco de archivo | Metadata propia filtrada y clasificada o 404/403 no enumerativo |
| I03/O03 | Operación read/write/delete/export | Autorización RBAC granular por recurso y método |
| I04/O04 | Contenido y nombre del upload | Validación MIME por bytes, nombre saneado y tamaño limitado |
| I05/O05 | Digest esperado y longitud | Checksum calculado de bytes reales y verificación |
| I06/O06 | Solicitud de acceso temporal | URL de recurso y verbo restringidos, TTL corto y auditable |
| I07/O07 | Instante UTC / reloj confiable | Rechazo al expirar; clock-skew definido y probado |
| I08/O08 | Estado de revocación/replay | Resultado explícito: bearer reusable o token one-time validado |
| I09/O09 | Policy de bucket/prefijo por tenant | Key interna opaca, separada y nunca expuesta en DTO |
| I10/O10 | Estado de caché, cliente y cambio tenant | Invalidación A→B sin descarga del tenant anterior |
| I11/O11 | Policy retención/lifecycle | No borrar evidencia sujeta a conservación; reversibilidad |
| I12/O12 | Fallo/quota del proveedor | Error tipado y registro saneado, sin URL/credencial/bytes ni objeto parcial |

No se adoptan rutas HTTP, nombres de DTO ni esquemas persistentes concretos en esta preparación: deberán fijarse en M02 con inspección de código y compatibilidad previa. Una URL firmada no elimina la necesidad de autorización **antes** de emitirla; la revocación instantánea depende del mecanismo escogido.

## Fronteras de confianza F01–F08

| ID | Cruce y riesgo | Control futuro exigible |
| --- | --- | --- |
| F01 | Browser / Angular → API | JWT/RBAC/tenant validado server-side, no confiar en slug/header/EmpresaId |
| F02 | API → Application | Pasar contexto de autorización validado, no key arbitraria |
| F03 | Application → Infrastructure | Contrato provider-neutral, scope por operación/tenant |
| F04 | Infrastructure → Cloudinary/R2 | Secrets externos, TLS, permisos mínimos; fallback solo autorizado |
| F05 | API → URL temporal → navegador externo | TTL, recurso y verbo mínimos; bearer/replay y revocación explícitos |
| F06 | CDN/cache/proxy → otro usuario o tenant | Cache partition, no-store donde aplique, invalidación A/B |
| F07 | Lifecycle → almacenamiento retenido | Derecho de retención/hold, snapshot, backup/rollback antes de borrar |
| F08 | Servicio → observabilidad/auditoría | Correlación sin tokens, urls privadas, PII ni payloads |

**Problemas a resolver en M02/M03:** modelo real de claves y migración reversible, endpoint/DTO existentes, expiración ante clock skew, eventual acceso CDN, controles one-time/replay, autenticación de firmas y políticas legales de borrado. Ninguno se considera implementado ni autorizado por esta tabla.

## Oráculos P/N FUTUROS — NO_EJECUTADOS

**Positivos:** P01 usuario A lee objeto A con rol adecuado y URL expirable; P02 rol A escritura válida emite metadata opaca sin key; P03 B opera sin acceder al namespace A; P04 expire/revocation medidos con reloj fijo; P05 cambio de tenant invalida caches; P06 retención preserva documento ante lifecycle.

**Negativos:** N01 usuario A solicita ID de objeto B; N02 header de tenant falsificado; N03 solo lector intenta delete; N04 URL expirada/método alterado; N05 bearer replay dentro de TTL se mide (no se presume bloqueado); N06 path traversal/prefix injection; N07 MIME falso/bytes peligrosos; N08 límite de stream excedido; N09 caché A visible a B; N10 logger contiene signed URL/token; N11 lifecycle elimina objeto con hold; N12 proveedor no autorizado se activa por fallback.

Todos son criterios para pruebas posteriores con datasets sintéticos A/B, CI causal y SHA exacto. **No hay PASS funcional, Playwright, xUnit ni integración R2 ejecutados en M01.02**.

## Impacto, controles A0–A5 y reversión

**Impacto real:** solamente este Markdown y sección completa de `CHANGELOG_AI.md`; cero rutas API, DLL, Angular, datos, migraciones, secretos, R2, dominios, workers o configuración. Se respeta Domain ← Application ← Infrastructure, API como frontera y seguridad backend. **A0:** plan/Skill/DAG exactos y owner único. **A1:** cotejo de A01–A14, I/O01–12 y F01–F08 con M01.01 y fuente V5. **A2:** revisión documental de aislamiento JWT/RBAC, tokens, TTL, replay y secretos. **A3:** chequeo de compatibilidad con C10-01 sin tests de runtime (NO_APLICA motivado). **A4:** fast-forward en `dev`, checks Scope Lock y procedencia CHANGELOG `completed/success` sobre SHA publicado y readback de PLAN/COLA/BITACORA. **A5:** revisión independiente por otra Task Javier, nunca autoaprobación.

**Rollback:** `git revert` del commit documental; no force-push, no cambios productivos ni borrado de archivos reales. La existencia de este documento **no habilita** M01.06/M03 ni QA/main/PROD.
