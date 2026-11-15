# SOLQARYN V5 — C10-01.M02.06: contrato terminal de dependencias y salida M02 de almacenamiento neutral

**Responsable:** PRIMARY DEV 04 JAVIER (`6aeacb7820ac8191b7d768a6f33dfa09`). **Fecha:** 2026-11-15. **Repositorio:** `solqaryn/Solqaryn`, rama `dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Autoridad única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, documento `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; PLAN_MAESTRO/COLA/DAG_TECNICO institucional `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`.

**Tipo de entrega: contrato exclusivamente documental de cierre de diseño M02.** No crea endpoints, interfaces compiladas, migraciones EF/MySQL, índices, adaptadores R2, buckets, webhooks, mensajes, workers, colas, feature flags, credenciales ni cambio de proveedor. No convierte pruebas futuras en pruebas ejecutadas ni certifica la implementación M03. Cloudinary continúa como integración real vigente y R2/Hostinger son topologías futuras, no habilitadas por este documento.

## 1. Gate causal de entrada y precedencia

La validación por ID de esta unidad mostró `PLAN_MAESTRO!H=PENDIENTE`, `I` sin responsable, `COLA!G=SI/O=SI` y `DAG_TECNICO!F=VALIDADO/G=SI` antes de reclamarla. Los predecesores revisados en PLAN y en el DAG fueron: `C10-01.M01.01–06` en LISTO con A5 independiente, `C10-01.M02.01–05` en LISTO con A5 de otra Task Javier y `C01-03.M01.06` en LISTO con A5. La verificación de esos estados sólo acredita el permiso para elaborar este contrato; una actualización posterior del DAG o una A5_FAIL causal obliga a reevaluar.

| Origen | Evidencia de contrato leída | Efecto permitido en M02.06 |
| --- | --- | --- |
| M01.03 y M01.06 | Inventario de interfaces actuales, BL01–BL14 y rutas de rollback RB01–RB10 | Preservar integración Cloudinary, titularidad y controles de bytes |
| M02.01 | Ocho entidades lógicas y 18 invariantes de propietario y versionado | Aceptar sólo un modelo de propiedad por empresa derivada en servidor |
| M02.02 | Ocho operaciones propuestas, siete DTO y doce denegaciones | Definir límites de compatibilidad sin publicar API nuevas |
| M02.03 | Siete entidades relacionales propuestas, seis accesos e índices posibles, diez fixtures y siete pasos de migración futura | Mantener toda operación SQL/EF en estado NO EJECUTADO |
| M02.04 | Doce contratos de configuración tipada y 16 invariantes | Mantener `LegacyOnly`, prohibir cutover/flags nuevos por defecto |
| M02.05 | Ocho mensajes hipotéticos, 14 campos, 14 reglas fail-closed y nueve pares positivos/negativos | No crear event dispatcher ni deducir PASS por un mensaje recibido |
| C01-03.M01.06 | Contrato terminal del bloque relacionado en LISTO y A5 | Revalidar sus dependencias al iniciar cualquier trabajo M03 |

Los contratos de interfaz de compatibilidad constatados en M01.03 incluyen `IImageStorageService`, `ICompraDocumentoStorageService`, `StorageTenantContext`, `CloudinaryImageStorageService`, `CloudinaryCompraDocumentoStorageService` e `ImagenUploadSecurity`. Su existencia no demuestra soporte neutral para vídeo, exportación, restauración ni transferencia efectiva entre proveedores.

## 2. Requisitos obligatorios de salida M02

| Gate | Prueba documental exigida en esta unidad | Qué NO certifica |
| --- | --- | --- |
| OUT01 Autoridad | Empresa y membresía vigentes determinadas por backend; jamás URL, cabecera o `EmpresaId` del cliente | Autorización JWT/RBAC de runtime |
| OUT02 Propiedad | Recurso, propietario, versión y localizador opaco unidos al mismo tenant | Tablas EF o filtros MySQL ya aplicados |
| OUT03 Contratos | Operaciones/DTO conceptualizados con validación estricta, error seguro y compatibilidad de clientes actuales | Rutas HTTP nuevas o payloads desplegados |
| OUT04 Bytes | MIME detectado de contenido, longitud medida y SHA-256 de bytes reales del servidor como futuro criterio de confirmación | Cargas reales, hashes calculados o descarga de objetos |
| OUT05 Privacidad | Sin tokens, PII, URL firmada o ruta real de proveedor en API y observabilidad; enlaces temporales revocables y con caducidad | Prueba HTTP o proveedor de enlaces firmado |
| OUT06 Idempotencia | Clave delimitada por empresa/actor/operación/objeto, versión esperada y prohibición de publicación parcial | Persistencia transaccional distribuida |
| OUT07 Configuración | Políticas tipadas y restrictivas, modo actual por defecto; no seleccionar ni contratar un proveedor nuevo | Flags reales instalados o cutover |
| OUT08 Migración futura | Diseñar manifiesto, inventario de objetos, verificación de conteos/bytes/hashes, restauración y rollback probado en ambiente efímero | Respaldos, restauraciones o migraciones ejecutadas |
| OUT09 Compatibilidad | Revisión de consumidores Cloudinary existentes, separación de capas y contrato aditivo antes de cambios | Build .NET/Angular o regresión HTTP |
| OUT10 Publicación | Git dev por fast-forward/CAS, Scope Lock y procedencia CHANGELOG exitosos en SHA exacto, PLAN/BITACORA readback | CI que esté queued, cancelled, skipped o failed |
| OUT11 Independencia | A5 pendiente para Task Javier diferente de esta autora, con SHA, blobs, diff y controles propios | A5 propia, QA, main o PROD |
| OUT12 Alcance | Prohibir secretos, datos reales, Cloudflare R2 en vivo, IAM, DNS, pagos y nuevas tareas programadas | Autorización de infraestructura o ejecución comercial futura |

Los doce gates son criterios de **integridad y coherencia de diseño**. La satisfacción documental de OUT01–OUT12 no sustituye las comprobaciones A0–A5 de implementación, integración efímera y seguridad que exija cada sucesora.

## 3. Fronteras y secuencia de implementación futura

**Domain:** validar propiedad de recursos, estados, versiones, idempotencia y transición de estado en lógica aislada del proveedor. El dominio no debe referenciar Cloudinary, R2, controladores, APIs, tablas o clientes Angular.

**Application:** casos de uso autenticados con permisos RBAC por acción; validación de tenant desde sesión/membresía de servidor; interfaces provider-neutral aditivas y resultados de error seguros. Separar lectura, escritura, revocación, exportación y restauración. No permitir autorización implícita por flags.

**Infrastructure:** implementar en tareas M03 independientes los adaptadores reales, EF/MySQL, pruebas de integridad y eventual adaptación de proveedor. No activar un proveedor nuevo sin gate comercial, permisos, pruebas de seguridad, datos sintéticos y autorización posterior.

**API/Angular:** conservar consumidores actuales. Cualquier endpoint, DTO compilado o pantalla futura debe tener rutas y permisos verificados server-side, tests positivos y negativos A/B, límites de carga/streaming, manejo de error y compatibilidad. Angular jamás decide el tenant o el permiso efectivo.

**Datos, observabilidad y reversión:** separar metadatos privados y bytes; definir retención y auditoría sin PII; medir conteos y hashes auténticos; cualquier esquema o migración requiere rollback verificado en MySQL 8.4 efímero y ausencia de escrituras productivas. Las nuevas operaciones deben registrar correlación sin filtrar secretos. No hay migración SQL ni rollback de datos en esta microtarea.

La secuencia futura mínima es: `M02 diseño validado + A5 independiente` → `preflight DAG de cada M03` → `código con A0–A4 y CI dirigida exact-SHA` → `A5 ajena` → `pruebas funcionales, seguridad, integración, restauración y rollback según las fases autorizadas`. Ningún paso puede saltarse la puerta específica de su microtarea. No se cambia manualmente `COLA!G/O` o `DAG!F/G`.

## 4. Oráculos positivos y negativos futuros (NO EJECUTADOS)

| ID | Resultado positivo esperado después de implementar | Negativo que debe fallar cerrado |
| --- | --- | --- |
| P01/N01 | A lee objeto A con membresía y permiso vigentes | A adivina un ID de B y lee sus metadatos |
| P02/N02 | MIME verificado por firma coincide con tipo aprobado | Extensión permitida y bytes reales maliciosos |
| P03/N03 | SHA-256 se calcula de los bytes almacenados y se comprueba al leer | Hash declarado por cliente da PASS sin comparación |
| P04/N04 | Reintento idempotente con mismo contenido produce un efecto | Mismo identificador y bytes distintos reescriben versión |
| P05/N05 | Carga parcial permanece invisible hasta confirmación | Objeto truncado se publica al fallar proveedor |
| P06/N06 | Denegación por JWT/RBAC/tenant ausente retorna error seguro | `EmpresaId=NULL` u otro tenant se interpreta como global |
| P07/N07 | Enlace efímero válido caduca y respeta permisos actuales | Grant revocado o URL vencida aún descarga |
| P08/N08 | Configuración inexistente mantiene proveedor actual sin cutover | Cliente activa R2 por feature flag, URL o header |
| P09/N09 | Manifiesto de restauración sintética compara bytes, conteos y hashes | Migración real o rollback sin backup y evidencia |
| P10/N10 | Catálogo de eventos futuro no contiene secretos ni autoridad de Task | Mensaje, webhook o evento se toma como LISTO/A5_PASS |
| P11/N11 | A5 de otra Task Javier acredita SHA y blob original de este diseño | Autora aprueba su propia M02.06 |
| P12/N12 | Pruebas CI posteriores marcan `completed/success` del SHA pertinente | `cancelled`, `skipped` o `in_progress` se atribuyen como PASS |

Esta tabla es una **matriz de aceptación futura**. Los escenarios no se ejecutaron contra HTTP, MySQL, proveedores, navegador, disco o datos de usuarios durante esta entrega.

## 5. Contrato de cierre, dependencias bloqueantes y rollback

**A0:** diff limitado a este Markdown y sección autocontenida de `CHANGELOG_AI.md`; archivos base, interfaces, secretos y estado de otros scopes inalterados. **A1:** revisión positiva y negativa de OUT01–OUT12, P01/N01–P12/N12, identificación de fuentes verificables y ausencia de afirmaciones runtime. **A2:** verificación documental de JWT/RBAC, límites de confianza multiempresa A/B, idempotencia, datos privados, integridad, MIME/tamaño y defaults fail-closed, sin inferir tests ejecutados. **A3:** revisión de contratos M01/M02 y consumidores Cloudinary; compilación .NET/Angular, HTTP, EF/MySQL, Playwright y broker = NO_APLICA a este changeset exclusivamente Markdown. **A4:** publicación a `dev` con parent/HEAD exacto, Scope Lock y procedencia V5 `completed/success`, diff/blobs, estados y evento BITACORA confirmados por readback. **A5:** permanecer `A5_PENDIENTE` hasta revisión independiente de otra Task Javier diferente de PRIMARY DEV 04.

**Bloqueadores que NO se eliminan mediante esta entrega:** un A5_FAIL causal, dependencia no LISTO+A5 cuando así lo exige el DAG, CI obligatoria fallida o cancelada para M03, permisos insuficientes, evidencia de tenancy/seguridad/rollback ausente, o un propietario/escritor activo de la siguiente microtarea. Validar las siguientes candidatas por ID; jamás inferir un gate SI por analogía o por un documento.

**Rollback del cambio actual:** `git revert` normal del commit documental con reconciliación de HEAD concurrente; preservar BITACORA histórica, autores y revisiones A5 existentes. Ningún rollback SQL, borrado de objetos, cambio de proveedor, migración, despliegue, DNS, secret, QA/main/PROD ni ejecución de eventos fue necesario o autorizado.
