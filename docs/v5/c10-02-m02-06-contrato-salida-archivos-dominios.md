# SOLQARYN V5 — C10-02.M02.06: contrato técnico de salida para archivos privados y dominios

**Unidad institucional:** C10-02.M02.06 · PLAN_MAESTRO fila 1489 · DAG_TECNICO fila 1450 · **Autor:** PRIMARY DEV 03 (`6adc2e4bd8a48191ac7d8641ed95a1a3`), `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. Fuente única: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Doc Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. Fecha: 15 noviembre 2026.

## Contrato y naturaleza de la entrega

La microtarea **fija dependencias técnicas y el contrato de salida del lote M02** para archivos privados, dominios y almacenamiento futuro. Su resultado es **documental**: no implanta R2, Cloudflare DNS, Hostinger, nuevos endpoints, migraciones EF/MySQL, colas, workers, webhooks, secrets, cambios Cloudinary ni datos reales. El modelo actual sigue vigente. Una descripción de pruebas futuras no acredita pruebas de runtime ni autoriza el futuro cutover.

Al preflight la evidencia institucional muestra `C10-02.M02.01–05` en **LISTO + A5_PASS independientes**; `PLAN_MAESTRO!H1489=PENDIENTE` sin responsable, `COLA!G1489/O1489=SI`, `DAG_TECNICO!F1450=VALIDADO/G1450=SI`. Estas observaciones autorizan preparar **este documento**, no cambiar proveedores ni liberar código M03 antes del A5 posterior.

## Contratos previos integrados sin suplantar implementación
- **M02.01:** proyecciones conceptuales de ownership, tenant, recurso y versiones con invariantes de privacidad; no modelos EF persistidos.
- **M02.02:** operaciones API/DTO, validaciones y errores propuestos; no endpoints ni HTTP real.
- **M02.03:** doce proyecciones de persistencia, doce índices candidatos, doce reglas de integridad, doce pares de fixtures `FUTUROS_NO_EJECUTADOS`; sin DDL ni objetos.
- **M02.04:** dieciocho opciones tipadas conceptuales con precedencia restrictiva PLATFORM_SECURITY > DEPLOYMENT_ENV > TENANT_FILES > RESOURCE_POLICY; sin configuración runtime activa.
- **M02.05:** doce eventos inertes, catorce reglas de idempotencia/replay y doce parejas de aceptación futura; no bus, outbox o dispatcher.
- **M01.06:** alcance, fronteras y riesgos de la futura preparación, sin conectar proveedor o modificar DNS/host.

La autoridad de empresa, recurso y operación procede de JWT/membresía/RBAC vigente **validada en backend**; ni header, EmpresaId de cliente, host, prefijo, path ni token de evento atribuyen permisos.

## Condiciones de bloqueo G01–G16 para futuros M03/operaciones reales

| Gate | Evidencia necesaria antes de implementar efectos reales |
| --- | --- |
| G01 | A5 independiente de esta M02.06 y gates DAG/COLA reactivos; no liberar a mano |
| G02 | Proveedor, ámbito de cuenta y credenciales aprobados separadamente, sin secretos en repo |
| G03 | Identidad, pertenencia y RBAC del tenant A/B comprobados server-side en cada operación |
| G04 | Inventario histórico real autenticado y conciliado; `NO_VERIFICADO` jamás es `CERO` |
| G05 | Propiedad por recurso, versión, clasificación y entorno probada; NULL legacy no es público |
| G06 | MIME por bytes, extensión, tamaño, stream y cuota testados con payloads negativos |
| G07 | Firma privada por objeto, verbo, usuario, audience y vencimiento UTC; invalidación/replay |
| G08 | Prefijo/key canonizados y rechazo de traversal, colisión y host B desde tenant A |
| G09 | Intersección restrictiva de configuración tipada; cambios versionados/CAS y auditoría |
| G10 | Retención/hold y snapshots históricos prevalecen sobre lifecycle y eliminaciones |
| G11 | Cache, CDN y host separados por empresa, recurso y versión, sin 403/404 enumerativos |
| G12 | Reintentos limitados, idempotencyKey + digest, outbox transaccional cuando corresponda |
| G13 | Logs, errores, correlación y auditoría saneados, sin PII, JWT, URL firmada o HMAC |
| G14 | CI dirigida .NET/Angular/MySQL efímero/seguridad, `completed/success` SHA causal según scope |
| G15 | Backup/restore y rollback de objetos/ACL/versiones verificados con fixtures autorizados |
| G16 | Orden adicional expresa antes de Cloudflare R2/DNS, Hostinger, datos, QA/main/PROD o cutover |

**Todos G01–G16 son restricciones futuras, no certificados PASS.** Una futura M03 técnica debe implementar código y pruebas reales, no reutilizar este documento como servicio implementado. Un inventario cero confirmado implica omitir totalmente exportación/copia/checksum de históricos; si es positivo requiere manifiesto, checksums y rollback. No inferir cero sin fuentes autenticadas.

## Ocho ensayos de reversión contractuales R01–R08 — NO_EJECUTADOS

| Escenario | Condición de reversión y seguridad |
| --- | --- |
| R01 | Proveedor aún no aprobado: mantener Cloudinary y abortar cambio de destino |
| R02 | Inventario desconocido/inconsistente: detener migración, preservar objetos originales y abrir conciliación |
| R03 | Cero real verificado: registrar ausencia de históricos, omitir exportación/copia/checksum |
| R04 | Archivos positivos: discrepancia digest/versión detiene publicación, conserva fuente y manifiesto |
| R05 | Tenant o rol cruzado/no válido: denegar, no enumerar ni registrar URL privada |
| R06 | Firma expirada/replay/host no reconocido: bloqueo e invalidación scoped con mínima auditoría |
| R07 | Timeout en outbox/lifecycle: compensación idempotente y preservación de retención |
| R08 | Cutover futuro autorizado falla: revertir bindings/config, comprobar lecturas A/B y restauración |

Ningún R01–R08 representa un test o rollback ejecutado hoy. Su implementación necesitará fixture no productivo, fuente acreditada, evidencia UTC, SHA, permisos, resultado P/N, restauración y readbacks.

## Revisión A0–A5, impacto y rollback de este documento

**Impacto real:** un Markdown nuevo `docs/v5` y una sección autocontenida en `CHANGELOG_AI.md`; no hay alteraciones en Domain/Application/Infrastructure/API, EF/MySQL, Angular, security runtime, CORS, secretos, proveedores, almacenamiento o despliegue.

- A0: Skill canónica, repo/HEAD/dev, PLAN/DAG/COLA y exclusividad de owner, dependencias M02.01–05+A5 verificadas.
- A1: coherencia de las cinco unidades de contratos y G01–G16/R01–R08 como requisitos falsables; pruebas runtime `NO_EJECUTADAS`.
- A2: revisión documental de JWT/RBAC/tenant A/B, privacidad, MIME, TTL, HMAC/replay, hold e idempotencia; no autorización viva.
- A3: conciliación documental con las interfaces y servicios existentes; .NET/Angular/Playwright/EF/MySQL/R2 `NO_APLICA` al diff Markdown, no PASS.
- A4: commit fast-forward/CAS en dev; checks `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` exact-SHA y readback PLAN/COLA/BITACORA antes de LISTO.
- A5: **PENDIENTE** revisión independiente por otra Task Javier distinta de DEV03; M03 no se desbloquea sin este paso y otros prerequisitos explícitos.

Rollback documental mediante `git revert` normal del SHA causal, sin fuerza ni reescritura. Historial BITACORA es aditivo y no se edita; QA/main/PROD, DNS, host, almacenamiento, secretos y migraciones quedan intactos.
