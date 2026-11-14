# SOLQARYN V5 FINAL — C13-01.M01.02: actores, interfaces, entradas y salidas de privacidad/consentimiento

**Resultado de la microtarea:** identificar actores, entradas, salidas y superficies afectadas para la futura gestión de privacidad, cookies, términos, consentimiento, revocación, retención y derechos; no implementar la funcionalidad. **Autor:** SOLQARYN PRIMARY DEV 02 — JAVIER (TASK_ID 6af16bb9492c8191b8e8c7d606f11507), exclusivamente solqaryn/Solqaryn@dev y PROJECT_SCOPE_LOCK=STRICT. **Fecha:** 2026-11-14.

**Autoridad:** [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit) y el objetivo/no-alcance C13-01.M01.01 (blob Git 79523b25247b3a2db4bc79ca723ba5adc698d7c3). **Naturaleza:** análisis documental, sin obligación legal declarada ni certificado de cumplimiento.

## 1. Actores y límites de autoridad (potenciales, no roles nuevos)

| Actor/superficie | Entrada o decisión que puede originar | Permiso y frontera |
| --- | --- | --- |
| Visitante anónimo del sitio público | Navegar, ver información sobre privacidad y preferencias permitidas | Nunca infiere tenant ni obtiene registros personales de usuarios autenticados |
| Usuario autenticado de empresa A | Consultar avisos, preferencias propias, estado y eventual revocación | JWT válido, membresía/tenant A y propiedad del registro revalidados en backend |
| Usuario autenticado de empresa B | Las mismas acciones, sólo sobre datos de B | Denegación A/B y no filtración en respuestas/exports |
| Administrador de la empresa | Eventual gestión de políticas de su empresa dentro de atribuciones verificadas | El rol no concede acceso a toda PII ni a registros privados sin autorización específica |
| Operador de sucursal o recurso | Eventual visualización limitada de políticas aplicables | Sucursal/recurso/empresa deben resolverse server-side; no confiar en un selector UI |
| Administrador global autorizado | Eventual configuración de versiones de avisos en ámbito plataforma | Mínimo privilegio, segregación, auditoría y no suplantación de sujeto ni tenant |
| Soporte o auditor autorizado | Consultar evidencia mínima por caso autorizado | Acceso de duración/propósito limitado, datos minimizados y registro de consulta |
| Responsable legal/de privacidad designado | Validar texto, jurisdicción, base y plazos cuando exista designación formal | Perfil de revisión humana futuro; no atribuir hoy nombramiento ni cumplimiento |
| Servicio de identidad/JWT | Resolver sujeto/empresa/membresía/rol efectivos | Tokens y claims no bastan si membresía fue revocada |
| API ASP.NET Core 10 | Validar autoridad, registrar cambio solicitado y responder errores | Límite server-side JWT, RBAC, tenant y auditoría; sin endpoints nuevos aquí |
| Angular 22.2.1 | Mostrar avisos, categorías, elecciones y estados accesibles | UI sólo informa; no autoridad de autorización ni activación de trackers |
| Persistencia MySQL/EF Core 10 | Posible versión, evidencia mínima y retención autorizada | DDL/modelo futuro sujeto a migración aditiva y tests con MySQL efímero |
| Servicios de terceros y navegador | Posibles cookies/medios/analítica, a inventariar | Nunca declarar proveedor/categoría activos sin inventario técnico real |
| Operaciones y auditoría | Posible trazabilidad de solicitudes y revocaciones | Correlación opaca, no secretos ni contenido sensible en logs |
| Procesamiento asíncrono futuro | Posibles tareas de cumplimiento o notificación | NO implementado ni autorizado como webhook/dispatcher/Task en V5 |

Los actores anteriores son **categorías analíticas**. No crean usuarios, roles, permisos, consentimientos efectivos, obligaciones legales, endpoints, tablas, scripts externos ni asignaciones a individuos.

## 2. Catálogo de entradas y su validación exigible

| Entrada conceptual FUTURA | Origen posible | Comprobación mínima antes de aceptar |
| --- | --- | --- |
| Solicitud de lectura de aviso/política versionada | Visitante o usuario | Versión publicada, ámbito, idioma, integridad y control de caché |
| Elección de preferencias por categoría | Sujeto o dispositivo | Propiedad y base aplicable verificadas; categoría inventariada; no activar no-esenciales antes de decisión pertinente |
| Revocación de elección anterior | Titular autenticado | Autoridad de sujeto/empresa, versión, trazabilidad y efecto futuro definido; no borrar auditoría histórica |
| Petición de acceso/corrección/supresión/exportación | Titular o autorizado | Identidad, capacidad legítima, recursos propios y validación humana/jurídica cuando corresponda |
| Publicación de nuevo texto normativo | Responsable expresamente autorizado | Aprobación humana, revisión jurisdiccional, vigencia y rollback por versión |
| Política de retención | Servicio autorizado/operador | Categoría y fundamento corroborados; no aplicar TTL destructivo por inferencia |
| Cambio de tenant/sucursal/recurso | Contexto API | Fuente server-side; rechazar IDs suministrados por cliente que no pertenezcan al sujeto |
| Cookie/SDK detectado | Navegador, Angular o proveedor | Evidencia de red/storage inspeccionada; no deducir desde nombre de archivo ni documento |

**No existe en esta unidad una acción real de POST/PUT/DELETE ni un flujo de consentimiento operativo.** Antes de cualquier entrega M03/M04, M01.03 debe inventariar comportamiento real, proveedores/cookies, datos, jurisdicciones y rutas; las decisiones abiertas se formalizan en M01.04.

## 3. Salidas y garantías observables esperadas (contratos futuros)

- S01: aviso legible y versionado, con ámbitos/localización explícitos, disponible sin filtrar registros de otros usuarios.
- S02: preferencia efectiva evaluada por categoría/version/sujeto cuando exista base y registro implementados; respuesta segura sin exponer tokens.
- S03: acuse de revocación verificable con fecha, versión y correlación opaca, sin prometer eliminación inmediata de datos que deban conservarse lícitamente.
- S04: respuesta de rechazo 401/403/404 apropiada, sin existencia o metadatos de tenant ajeno ni enumeración de usuarios.
- S05: registro mínimo de auditoría de decisión/cambio, sin PII excesiva, secretos, JWT, email completo o cookies sensibles.
- S06: comprobante de solicitud de derechos con estado y canal de atención autorizados, sin afirmar que el derecho esté jurídicamente reconocido hasta determinar jurisdicción.
- S07: estado de no disponibilidad fail-closed si configuración, identidad, versión o proveedor no son verificables.
- S08: documentación de impacto/retención/versionado y rollback revalidada por el responsable humano competente, si existe tal designación.

Todos los S01–S08 son **criterios objetivo**, no salidas ejecutadas. Su existencia en una tabla no acredita funcionalidad ni certifica cumplimiento.

## 4. Fronteras de confianza, seguridad y privacidad por capa

1. **Browser/Angular → API:** el navegador no valida la autoridad final. Evitar scripts no esenciales antes de opción pertinente; interfaz accesible por teclado, lector de pantalla, foco, contraste y error claro, sujeto a inventario posterior.
2. **API → Application:** autenticar, revalidar empresa A/B, membresía, rol efectivo, propiedad y finalidad. Error genérico ante IDs ajenos y no exponer existencia del recurso.
3. **Application → Infrastructure/MySQL:** separar datos por tenant con claves consistentes; auditar actualizaciones sin registrar contenido sensible. Cualquier retención o migración destructiva requiere orden, base y tests con MySQL efímero.
4. **Plataforma global → tenant:** configuración global no autoriza leer/exportar datos de todas las empresas por defecto; soporte debe operar con permisos y trazas limitadas.
5. **Proveedor externo → sistema:** contratos de datos y scripts reales deben confirmarse antes de mapear cookies/trackers; no asumir país, ley, base de consentimiento o plazo desde esta preparación.
6. **Auditoría/telemetría:** correlación pseudónima y granular, sin usar una Bitácora de desarrollo como repositorio de consentimientos de clientes. No crear dispatcher, worker, webhook ni llamadas entre Tasks.
7. **Seguridad de lectura/descarga:** negar BOLA/IDOR, proteger cachés por tenant+actor+versión, tokens de un solo uso cuando corresponda, manejo de CSRF/XSS y minimización de URL/referrer.

## 5. Matriz de oráculos (FUTUROS_NO_EJECUTADOS)

**Positivos de diseño:** P01 visitante ve sólo aviso público versionado; P02 usuario A consulta sus elecciones sin ver B; P03 revocación propia válida produce estado/auditoría futura; P04 usuario con necesidades de accesibilidad opera preferencias con teclado y confirmación comprensible.

**Negativos de diseño:** N01 token de A consulta elección B; N02 membresía revocada; N03 rol empresarial intenta configurar política global; N04 URL de export con ID ajeno; N05 categoría sin inventario; N06 preferencia sin versión; N07 tracker no-esencial activado antes de autorización necesaria; N08 caché multiempresa compartida; N09 revocación duplicada no idempotente; N10 contenido secreto en log; N11 script de proveedor no autorizado; N12 TTL destructivo sin fundamento; N13 cookie de terceros desconocida se asume ausente; N14 idioma inaccesible/teclado bloqueado; N15 solicitud anónima obtiene PII; N16 error de proveedor genera permiso implícito.

Esos oráculos permiten revisar el contrato, **no constituyen pruebas HTTP, navegador, MySQL ni cumplimiento legal ejecutados**.

## 6. Trazabilidad del código actual y límites de evidencia

| Origen real del dev en el preflight | Blob exacto verificado | Límite probatorio |
| --- | --- | --- |
| backend/tests/Solqaryn.Tests/N19SerieAuditPrivacyTests.cs | 5bd3a4b1b7a4bb90c45a27149c408386c7bf1617 | Tests de datos de serie/auditoría, no gestión global de cookies |
| backend/tests/Solqaryn.Tests/P1015BPrivilegedAuditPrivacyTests.cs | c3321332fa4102111f03247da6063bb99d3bec1c | Privacidad de auditoría privilegiada in-memory, no política jurídica |
| docs/v5/c03-02-m01-03-inventario-autorizacion-privacidad.md | 51b5a01a4770977aadcc780145a17aaed8a3fb41 | Inventario de JWT/RBAC/tenant, no preferencias desplegadas |
| docs/v5/c03-02-m01-05-criterios-aceptacion-tenant-privacidad.md | a5f551f1bd2cde2c42e6daf9e6e488f247e36602 | Oráculos BOLA/IDOR, no autorización de implementar ahora |

C13-01.M01.03 debe profundizar el inventario real de Angular/API, red, cookies, proveedores y bases de datos. No inferir ausencia de componente por búsqueda nominal ni inventar certificaciones.

## 7. A0–A5, dependencias y reversión

- **A0:** diff restringido a este documento y CHANGELOG; ausencia de secretos, encabezados ## vacíos y atribución cruzada.
- **A1:** inspección documental de P01–P04, N01–N16 y S01–S08; pruebas futuras explícitamente no ejecutadas.
- **A2:** análisis de límites de sujeto, tenant A/B, JWT/RBAC, sucursal/recurso, PII y proveedores; ninguna concesión nueva.
- **A3:** consistencia con C13-01.M01.01 y cuatro blobs/contratos precedentes; no afirmar integración HTTP/EF/UI por un diff Markdown.
- **A4:** publicación fast-forward/CAS sobre HEAD dev, Scope Lock y check V5 de procedencia completados SUCCESS en SHA exacto, readback Git/PLAN/COLA y BITACORA A:Z.
- **A5:** revisión independiente por otra Task Javier, pendiente tras LISTO_DEV; no QA/main/PROD ni cumplimiento legal certificado.

**Rollback:** git revert normal del commit en dev (sin force-push ni modificación de BITACORA histórica) y evento compensatorio aditivo tras preflight. No hay DDL, cookies reales, proveedor, script, datos productivos o entorno externo alterados por esta unidad. Las seis Tasks mantienen su configuración, y cada siguiente ID requiere preflight/Gates DAG vigentes.
