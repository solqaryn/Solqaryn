# SOLQARYN V5 FINAL — C13-01.M01.03: inventario verificable de código y brechas de privacidad

**Microtarea:** C13-01.M01.03 — leer contratos, código y evidencia disponibles sin asumir certificación. **Autor:** SOLQARYN PRIMARY DEV 02 JAVIER, TASK_ID 6af16bb9492c8191b8e8c7d606f11507. **Ámbito:** solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT. **Fecha:** 2026-11-14.

**Autoridades:** [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). **Fuentes de la fase:** C13-01.M01.01, blob 79523b25247b3a2db4bc79ca723ba5adc698d7c3; C13-01.M01.02, blob 635fc95dbe1a32c628c72fd860af0a0d8e2e2dab. Este inventario no es aprobación jurídica, auditoría integral de cookies, inspección de tráfico real ni revisión certificada de todos los procesadores.

## 1. Contratos y código de dev realmente inspeccionados

Los SHA siguientes son **Git blob SHA-1 de archivos** en el preflight dev 07a198e890abbaaa4bc649aed6ac976615c10b79, no SHA de test PASS ni atestaciones de despliegue. Cada archivo se consultó desde GitHub en ese árbol.

| Ruta real | Blob exacto | Evidencia observada y límite probatorio |
| --- | --- | --- |
| frontend/src/app/core/auth/auth.service.ts | 9c9b58c4b289ccc3269b8e99b614a2a9dbaae557 | Token bearer en localStorage (clave solqaryn_token); evidencia de almacenamiento del cliente, **no** consentimiento para cookies ni sesión HttpOnly |
| frontend/src/app/core/auth/tenant-context.service.ts | 63a711355495d2f80efba35bffb2b8727401154a | Declara que localStorage no autoriza ni restaura contexto tenant verificado; consulta tenant-context al backend |
| frontend/src/app/core/guards/auth.guard.ts | b652a7af6343bdae83a4dc9f3065d9fc1aa120f6 | Protección de rutas vinculada a contexto tenant verificado; no cobertura de consentimiento |
| frontend/playwright.config.ts | 87946080e7fe87f5fa9e9c8d4c8da8a36f49d5da | Fixture/E2E configura estado de almacenamiento con cookies vacías en ciertos contextos; **no** demuestra ausencia de cookies reales del navegador |
| frontend/api/backend-proxy.js | 06c736622ddabcc3c73f6e2d8a10c25a5309a057 | Filtra encabezados sensibles, incluido cookie, al proxy; no equivale a inventario de scripts de terceros |
| backend/src/API/Controllers/AuditoriaController.cs | 82057f2acc571263ed7ed530eae0f81359b2f81a | Requiere autorización y permiso de ver auditoría; no es endpoint de derechos/consentimiento |
| backend/tests/Solqaryn.Tests/N19SerieAuditPrivacyTests.cs | 5bd3a4b1b7a4bb90c45a27149c408386c7bf1617 | Tests de privacidad de serie y auditoría de operación específica, no cobertura de preferencias |
| backend/tests/Solqaryn.Tests/P1015BPrivilegedAuditPrivacyTests.cs | c3321332fa4102111f03247da6063bb99d3bec1c | Test de auditoría privilegiada con EF InMemory, no ejecución contra MySQL remoto |
| docs/CERTIFICACION_N8_11_SEGURIDAD_CURRENT_STANDARD.md | 4ee1e7b23e6eb380a4fa8e420dc1c4f84486977c | Documento de alcance histórico menciona riesgo residual de bearer localStorage; no es certificación actual del módulo C13-01 |
| docs/v5/c03-02-m01-03-inventario-autorizacion-privacidad.md | 51b5a01a4770977aadcc780145a17aaed8a3fb41 | Inventario previo de autorización/tenant; no registra aceptación de políticas |
| docs/v5/c03-02-m01-05-criterios-aceptacion-tenant-privacidad.md | a5f551f1bd2cde2c42e6daf9e6e488f247e36602 | Oráculos de BOLA/IDOR y protección multitenant, no prueba de derechos implementados |

**Separación de evidencia:** lectura de código fuente ≠ prueba de integración; fixture Playwright con cookies vacías ≠ prueba de que navegador/SDK no genera cookies; documentación de seguridad ≠ homologación legal. No se han realizado durante esta microtarea pruebas de navegador con red/storage, carga de proveedores, endpoints HTTP, MySQL, jurisdicción o plazos.

## 2. Búsquedas focalizadas y cobertura que NO puede inferirse

Una búsqueda de código en el repositorio por consentimiento, cookies, privacidad, localStorage, auditoría y un nombre de controller de consentimiento produjo contratos parciales y referencias de documentación. La búsqueda literal ConsentController no devolvió resultados, pero **esto no demuestra que no exista una implementación parcial, otro nombre de clase, un SDK, un script inline o un tratamiento por servicio externo**. El código de auth con localStorage debe evaluarse como riesgo distinto de las cookies no esenciales.

No existe evidencia suficiente aquí para afirmar: consentimiento registrado operativamente, banner activo con categorías, versión de textos legalmente aprobada, inventario exhaustivo de tecnologías de seguimiento, base jurídica por jurisdicción, plazos reales de retención, eliminación total incluyendo backups, flujo de solicitudes de derechos, garantías del proveedor o una certificación multiempresa de dichos flujos. Estas ausencias son **brechas de evidencia**, no diagnósticos definitivos del sistema.

## 3. Brechas priorizadas y fuente de cierre

| Brecha | Evidencia adicional exigible antes de implementar o certificar | Control de seguridad |
| --- | --- | --- |
| B01 Cookies y storage por origen real | Inspección browser DevTools/Playwright con red y document.cookie/storage por superficies, sin extraer secretos | No habilitar tracking no esencial sin gate aplicable |
| B02 Proveedores y SDK | Inventario real de scripts, llamadas de red, Cloudinary/SMTP/otros servicios, finalidades y dominios | CORS, CSP, minimización, revisión de contratos |
| B03 Textos y jurisdicción | Identificar titulares, regiones, propósitos, categorías y revisión especializada | No afirmar plazos/obligaciones genéricos |
| B04 Versión y aceptación | Definir versionado y estado anterior/corriente por sujeto y ámbito con producto autorizado | No migrar consentimiento automáticamente |
| B05 Revocación efectiva | Evidencia de efecto en próximos usos de finalidad concreta | Fail-closed, idempotencia y trazabilidad sin PII |
| B06 Tenant A/B | Pruebas HTTP positivas/negativas para lectura, cambio, exportación y revocación | JWT, RBAC y propiedad server-side |
| B07 Datos y retención | Mapa de colecciones/tablas/logs/backups y obligaciones verificadas | No aplicar SQL destructivo por inferencia |
| B08 Derechos del titular | Canal, verificación de identidad, recepción, plazos aprobados y segregación | Rechazo BOLA/IDOR, acceso mínimo |
| B09 Accesibilidad | Evaluación real con teclado, lector, enfoque, contraste y navegación sin coerción | UX no sustituye autorización |
| B10 Observabilidad | Correlación opaca, auditoría mínima y borrado/protección de secretos en logs | Nunca JWT, contraseñas ni PII de B |
| B11 Cookies de autenticación futuras | Comparar riesgo localStorage con posible rediseño HttpOnly/CSRF aprobado | No migrar auth por esta M01 |
| B12 Plan de implementación y reversión | Dependencias reales por equipo, proveedor y pruebas de integración | No activar eventos, dispatcher, Task adicional ni PROD |

## 4. Oráculos para una implementación posterior (FUTUROS_NO_EJECUTADOS)

**Positivos:** P01 sujeto autenticado A consulta solo su estado versionado; P02 revocación autorizada deja trazabilidad mínima de la finalidad; P03 rechazo de tracking opcional conserva funciones necesarias y accesibilidad; P04 auditor con permiso preciso obtiene evidencia redactada sin PII excedente.

**Negativos:** N01 A consulta registro de B; N02 usuario con membresía revocada; N03 rol de sucursal intenta configuración global; N04 script desconocido se activa sin inventario; N05 ausencia de decisión se interpreta aceptación; N06 cambio de versión conserva consentimiento caducado; N07 un export incluye información de B; N08 claim/empresaId cliente sustituye tenant verificado; N09 token JWT aparece en auditoría; N10 eliminación ignora obligaciones/backups; N11 proveedor no disponible crea aceptación; N12 replay duplicado cambia otra versión/tenant; N13 una cookie de terceros se infiere ausente por fixture vacío; N14 error UI permite trackers; N15 cache mezcla sujeto A/B; N16 el documento M01 autoriza migraciones o despliegue.

No se ejecutaron estos oráculos en .NET/MySQL/Angular/Playwright en esta entrega Markdown: son **criterios contractuales** para auditorías posteriores, no PASS funcionales.

## 5. Impacto por capas, dependencias y aceptación A0–A5

- **Frontend Angular 22.2.1:** solamente lectura de auth, guards y configuración E2E; no archivos frontend modificados ni nuevas cookies.
- **Backend .NET 10:** sólo lectura de controller/auditoría/tests; no cambio de Domain/Application/Infrastructure/API, endpoints, DTO, JWT ni permisos.
- **Datos MySQL/EF:** sin modelos, DDL, fixtures ni datos modificados. Eventual esquema consentimientos requeriría diseño tenant-key y pruebas contra MySQL efímero.
- **Seguridad:** sujeto autenticado, tenant server-side, rol efectivo, recurso, PII, CSRF/XSS y filtración cross-tenant deben revisarse en una implementación futura, con rollback dirigido.
- **A0:** comprobar diff limitado a este archivo y sección autocontenida de CHANGELOG, referencias de once blobs, ausencia de secretos y encabezados ## vacíos.
- **A1:** inspección de 4 positivos, 16 negativos y 12 brechas; estos casos son FUTUROS_NO_EJECUTADOS, no tests reales.
- **A2:** verificar fronteras JWT/RBAC, tenant A/B, localStorage, cookies, privacidad y fail-closed; sin permiso adicional.
- **A3:** coherencia con C13-01.M01.01/.02 y once fuentes Git; integrar código/DB/browser es NO_APLICA al diff Markdown.
- **A4:** commit fast-forward/CAS en dev, check-run verify-solqaryn-project-scope y V5 CHANGELOG en completed/success del SHA exacto; readback Git, PLAN/COLA y BITACORA append-only.
- **A5:** PENDIENTE de otra de las cinco Tasks Javier; no autoaprobación, ni autorización de QA/main/PROD o cumplimiento normativo.

**Rollback:** git revert normal del changeset documental con HEAD revalidado, no force-push, no alteración de BITACORA histórica; registrar compensación idempotente si aplica. La elegibilidad futura M01.04/.05/M01.06/M02 depende de su propio DAG, revisiones causales y permisos vigentes.
