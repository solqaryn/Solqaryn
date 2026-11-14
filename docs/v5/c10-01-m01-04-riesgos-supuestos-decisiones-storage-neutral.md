# SOLQARYN V5 — C10-01.M01.04: riesgos, supuestos y decisiones de storage provider-neutral

**Microtarea:** `C10-01.M01.04` (PLAN_MAESTRO fila 1445); **autor:** SOLQARYN PRIMARY DEV 02 JAVIER / `6af16bb9492c8191b8e8c7d606f11507`; **fecha:** 2026-11-14. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`); `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`.

## Propósito y evidencia causal

Registrar **riesgos potenciales, supuestos falsables y decisiones explícitamente pendientes** para una abstracción FUTURA de archivos, imágenes, documentos, videos y exportaciones, sin convertir la documentación en permiso para servicios cloud. Este lote no crea ni modifica código, interfaces, claves, SDK, buckets, workers, webhooks, expedientes, base de datos ni archivos de usuario. Cloudinary aparece como integración actual declarada; R2/Hostinger son topología futura condicional, no proveedor activo certificado.

Fuentes observadas en el árbol anterior de dev:
- `docs/v5/c10-01-m01-01-objetivo-no-alcance-storage-neutral.md` (blob `622884a0a307e01c41ff1bec6797a1f36e67e21e`): NR01–NR12, límites de aislamiento y rollback.
- `docs/v5/c10-01-m01-02-actores-superficies-storage-neutral.md` (blob `6d18023f7bb42685cce150b6bbe29f1fa20c30c8`): A01–A12 e I01–I12, interfaces futuras.
- `docs/v5/c10-01-m01-03-inventario-codigo-evidencia-storage-neutral.md` (blob `1d807b2b9cba1f8b87a0e4e4e571f8e832ff0203`): inventario de interfaces Application, adaptadores Infrastructure, pruebas y brechas.
- Skill canónica de `dev`, `references/architecture.md` y `docs/PROJECT_SCOPE_LOCK.md`: capas Domain <- Application <- Infrastructure, Angular 22.2.1, .NET 10, MySQL, JWT/RBAC/tenant y secretos externos.

**Clasificación:** R = riesgo pendiente de medición, S = supuesto no demostrado, D = decisión NO APROBADA. Ningún R se informa como incidente comprobado; ninguna S se informa como verdad implementada. La evidencia no acredita backups, buckets R2, credenciales, checksum en producción ni migración autorizada.

## Registro R01–R12 de riesgos potenciales y criterio de bloqueo

| ID | Riesgo hipotético / evidencia requerida | Control de salida o NO_GO |
| --- | --- | --- |
| R01 | Confusión entre ID de objeto y tenant legítimo; falta prueba A/B | Derivar propietario desde contexto autenticado server-side, negar lectura cruzada |
| R02 | Permiso de lectura puede reutilizarse para upload, delete o export | Matriz RBAC distinta por operación, 403 seguro a rol de lectura |
| R03 | MIME declarado por cliente no coincide con bytes reales | Allowlist más inspección de contenido real; rechazar spoofing |
| R04 | Tamaño, cuota o streaming pueden agotar memoria o espacio | Límite antes y durante stream, corte seguro y limpieza |
| R05 | Checksums podrían no comparar bytes almacenados | Hash calculado y contraste con bytes persistidos/recuperados |
| R06 | Exposición de URL persistente o firmada fuera del tenant/TTL | Identificador opaco, TTL mínimo y descarga autorizada |
| R07 | Fallo de proveedor podría dejar objeto incompleto visible | Escritura atómica/compensación comprobada en servicio efímero |
| R08 | Ruta, nombre o metadato malicioso puede introducir traversal | Canonicalización, longitud, allowlist y no usar path del cliente |
| R09 | Logging puede incluir PII, token, URL firmada o contenido | Redacción y auditoría por correlación opaca, sin bytes ni secretos |
| R10 | Migración R2 podría perder trazabilidad o duplicar/borrar objetos | Inventario verificable, checksum paridad, backup, ensayo de restauración |
| R11 | Fallback a proveedor no autorizado podría cruzar entornos | Gate por ambiente, proveedor elegido explícitamente, fail-closed |
| R12 | UI/preview podría sustituir autorización backend y filtrar A/B | Pruebas HTTP tenant A/B, cache segmentation, 403/404 consistentes |

## Supuestos S01–S08 — falsables, no certificados

| ID | Supuesto por validar | Evidencia/propietario futuro |
| --- | --- | --- |
| S01 | El contrato actual de imágenes puede migrar sin cambiar sus consumidores | Trazado de llamadas y tests API/Domain/Application |
| S02 | Existe contexto de tenant confiable para cada operación de documento | Pruebas positivas/negativas sobre JWT/membresía vigentes |
| S03 | Los adaptadores disponibles soportan errores cancelables y streams controlados | Contratos .NET, tests de timeout y payload grande |
| S04 | Los tipos MIME del negocio pueden expresarse con allowlist finita | Inventario de documentos reales autorizado y reglas de dominio |
| S05 | El proveedor actual devuelve bytes/IDs suficientes para verificar checksums | Contrato real e integración efímera; no inferir de SDK |
| S06 | No hay objetos legacy fuera del inventario de migración | Inventario de origen bajo permiso expreso; supuesto no confirmado |
| S07 | Una URL temporal puede revocarse sin revelar identificadores internos | Prueba de caducidad, revocación y A/B sin producción |
| S08 | Se pueden medir costos, cuotas y rollback antes de cambiar proveedor | Análisis de permisos/precios con aprobación separada, sin contratación |

## Decisiones D01–D08 — PENDIENTES, prohibido inferir aprobación

| ID | Decisión pendiente | Condición exigida antes de decidir |
| --- | --- | --- |
| D01 | Ubicación final de interfaz provider-neutral y contratos de DTO | Revisión arquitectura y consumidores; mantener límites de capas |
| D02 | Política MIME/bytes/tamaño por clase y operación | Inventario real y pruebas con objetos de muestra |
| D03 | Modelo de IDs opacos, versiones y ownership | Revisión JWT/RBAC/tenant A/B y auditoría |
| D04 | Política de checksum, deduplicación y retención | Tolerancia a reintentos, privacidad y evidencia de integridad |
| D05 | Enlace temporal vs streaming proxy y TTL | Pruebas de expiración y no-exposición entre tenants |
| D06 | Política de errores, compensación y restauración | Simulación de fallo parcial en entorno efímero |
| D07 | Cloudinary actual vs R2/Hostinger futuro y corte de proveedor | Autorización comercial, seguridad, backup/restore y rollback |
| D08 | Métricas, logs, alertas y claves por entorno | Observabilidad sin PII y permiso explícito a secretos/infraestructura |

Las decisiones no aprobadas no se convierten en cambios por una fórmula DAG `VALIDADO/SI`; M01.04 prepara deliberación y pruebas futuras. Cualquier cambio a almacenamiento real, datos, migration, API, workers, secretes, cuotas contratadas o producción requiere nuevo alcance autorizado.

## Oráculos posteriores P01–P05 y N01–N12 — FUTUROS, NO_EJECUTADOS

| ID | Caso positivo especificado | Prueba futura |
| --- | --- | --- |
| P01 | Archivo permitido, tamaño permitido, MIME y bytes coincidentes | Upload/download efímero, hash igual |
| P02 | Tenant A autorizado lee objeto A, no B | Integración HTTP con dos empresas |
| P03 | RBAC escritura/consulta separado produce comportamiento esperado | Suite xUnit y HTTP/DTO |
| P04 | Fallo simulado no expone objeto incompleto | Integración adaptador efímero y rollback |
| P05 | Exportación autorizada preserva integridad y auditoría segura | PDF/test de bytes, logs sin PII |

**Negativos a implementar en etapas posteriores:** N01 A lee objeto B; N02 identidad ausente; N03 rol lectura hace delete; N04 `EmpresaId` o header alterado; N05 MIME de cabecera no coincide con contenido; N06 tamaño excedido; N07 hash discordante; N08 path traversal; N09 URL firmada expirada/reutilizada por B; N10 caída de proveedor deja objeto parcial; N11 log incluye token o URL privada; N12 migración R2 solicitada sin inventario, respaldo, permiso ni rollback. Cada uno debe producir rechazo o compensación segura con evidencia dirigida, **no PASS por especificación**.

## Preflight, auditorías y reversibilidad

**Dependencias:** `PLAN_MAESTRO!A1445=C10-01.M01.04` fue PENDIENTE sin responsable; `COLA!G1445=SI/O1445=SI`; `DAG_TECNICO!A1406=C10-01.M01.04/F1406=VALIDADO/G1406=SI`. Preparación C01-03/C02/C03 es documental; ni M01.06 ni M06.06 ni R2/MySQL/security runtime quedan liberados.

**Impacto:** exclusivamente este Markdown y sección indivisible de `CHANGELOG_AI.md`; ninguna capa ejecutable, API/DTO, migración, autenticación, caché, componente Angular o proveedor cambia. **A0:** verificar rutas fuentes y diff de dos Markdown, sin secretos; **A1:** completar inventarios R12/S08/D08 y P05/N12 con oráculos verificables FUTUROS; **A2:** preservar JWT/RBAC, tenant A/B, MIME real, tamaño, hashes y logging sin PII; **A3:** cotejar fuentes M01.01–03, arquitectura, DAG, seguridad y no autorización de proveedores; **A4:** publicación fast-forward CAS `dev`, check `verify-solqaryn-project-scope` y procedencia CHANGELOG V5 `completed/success` sobre SHA exacto, más readback de BITACORA/PLAN/COLA. CI compilación .NET/Angular/MySQL/Playwright sin código ejecutable cambiado es NO_APLICA causal, no PASS.

**A5_PENDIENTE:** revisión independiente por otra de las cinco Tasks Javier, prohibida autoaprobación; no se promociona a QA/main/PROD. **Rollback:** `git revert` normal del commit documental, conservando BITACORA histórica, sin force-push ni datos reales.
