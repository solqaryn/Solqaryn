# SOLQARYN V16 — C12-02.M02.01 entidades, ownership e invariantes del frontend separado

**ID:** C12-02.M02.01 · **owner ejecutor:** PRIMARY DEV 04 JAVIER, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09` · **rama:** `solqaryn/Solqaryn@dev` · **Scope Lock:** STRICT · **riesgo V15:** R0 exclusivamente documental, sin runtime.

**Fuentes:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), Sheet PLAN_MAESTRO/COLA/DAG, contratos C12-02.M01.01–06. El antecedente terminal M01.06 corresponde al blob `fe54682eb1657861340ea3b51009e70c03959567` y ya recibió A5 independiente. Las decisiones D01–D10 sobre proxy, caché, dominio/TLS, headers, proyecto/ambiente, SEO, versionado, observabilidad, presupuestos y promoción **continúan pendientes**; no se consideran resueltas por crear este modelo.

## 1. Estado observado y frontera

La configuración actual en código contiene `frontend/angular.json` (`8b00f8fee80fdfafe1864e233be7d5f20ad444e3`), `frontend/vercel.json` (`5f12bcf667a21afcfc4314b66787ff66627ae666`) y `frontend/src/app/app.routes.ts` (releída en el HEAD de preparación, Angular standalone y rutas privadas/públicas). Los archivos demuestran configuración presente, **no** estado live del proveedor, publicación de infraestructura ni observancia de seguridad en HTTP. No tocar Vercel/Render/Aiven, Hostinger, Cloudflare, DNS/TLS, infraestructura, secretos, QA/main/PROD o datos reales.

Los doce elementos siguientes son **entidades de contrato conceptual**, no tablas MySQL, clases C# ni interfaces TypeScript creadas.

## 2. Entidades E01–E12 y ownership

| ID | Entidad conceptual | Owner / frontera de confianza |
| --- | --- | --- |
| E01 | FrontendRelease | Pipeline dev de código verificado; SHA y versión inmutables por artefacto |
| E02 | BuildArtifact | Release que produjo el bundle; hash y manifiesto, no mutable por navegador |
| E03 | AssetManifest | BuildArtifact y rutas públicas permitidas; sin rutas privadas ni tokens |
| E04 | RouteDefinition | Repo frontend; política pública/privada declarada, nunca autoridad de backend |
| E05 | DomainBinding | Plataforma por ambiente; dominio/host solo con permiso expreso, nunca tenant por header sin verificar |
| E06 | ProxyRule | Propietario de infraestructura autorizado; preservar Authorization y errores 401/403 |
| E07 | SecurityHeaderPolicy | Política por entorno; CSP/CORS/HSTS sin comodines con credenciales |
| E08 | CachePartition | Política pública o privada con TTL y segmentación explícita por usuario/tenant |
| E09 | TenantContextReference | Resuelto por backend con JWT/RBAC, membresía y revocación; frontend solo presentación |
| E10 | DeploymentEnvironment | dev/QA/PROD físicamente separados; esta microtarea solo dev y no despliega |
| E11 | RollbackPointer | Release previo verificado, reglas/headers/assets compatibles y readback causal |
| E12 | ObservabilityRecord | Pipeline o flujo con SHA, categoría y correlación sin PII/secretos |

**Relaciones:** FrontendRelease 1:N BuildArtifact; BuildArtifact 1:1 AssetManifest; RouteDefinition N:1 FrontendRelease; DomainBinding N:1 DeploymentEnvironment; ProxyRule/SecurityHeaderPolicy/CachePartition N:1 DeploymentEnvironment; TenantContextReference se valida **solo** en backend y no se materializa como privilegio cliente; RollbackPointer N:1 FrontendRelease/entorno; ObservabilityRecord N:1 SHA/contexto seguro.

## 3. Invariantes INV01–INV16, de cumplimiento futuro no probado

| ID | Invariante conceptual verificable |
| --- | --- |
| INV01 | Un artefacto identifica SHA/lockfile/configuración y sus hashes de entrada |
| INV02 | Un release no apunta a archivos de otro SHA sin compatibilidad ensayada |
| INV03 | Solo rutas públicas aprobadas entran en sitemap, robots y caché compartida |
| INV04 | Ruta privada no obtiene autorización por guard Angular, host, slug o parámetro |
| INV05 | El backend controla JWT, RBAC, membresía tenant A/B y revocación en cada operación |
| INV06 | Recursos con usuario/tenant o Authorization jamás quedan en caché pública |
| INV07 | Rewrites y proxy preservan cabeceras necesarias y 401/403 sin convertir error en 200 |
| INV08 | CORS con credenciales no usa wildcard; CSP bloquea ejecución arbitraria |
| INV09 | Manifiesto/código entregado no incluye secretos ni claves de infraestructura |
| INV10 | Host binding se valida contra allowlist y entorno, no decide tenancy por sí mismo |
| INV11 | Rutas profundas SPA tienen fallback solo donde corresponde; no exponen endpoints privados |
| INV12 | Recursos críticos reservan dimensiones, accesibilidad de teclado y movimiento reducido |
| INV13 | Optimización respeta presupuesto acordado; discrepancia 650–750 KB vs 1–2 MiB se mantiene abierta hasta decisión y prueba |
| INV14 | Rollback restituye versión, reglas edge, headers y TTL, no solo Git |
| INV15 | Operaciones de publicación, promoción y DNS requieren autorización nueva por entorno |
| INV16 | Ningún SUCCESS de CI cancelado, omitido o de distinto SHA sustituye prueba causal exacta |

Son barreras **por ejecutar y verificar**. La creación del contrato no acredita que hoy se cumplan ni constituye findings P0/P1 sobre un proveedor externo.

## 4. Diez pares T01–T10 de aceptación futura (0/10 ejecutados)

| ID | Positivo futuro | Negativo/frontera que debe fallar |
| --- | --- | --- |
| T01 | Artefacto enlaza SHA y hashes correctos | Asset de otro release se sirve como idéntico |
| T02 | Rutas públicas en sitemap y privadas ocultas | Ruta ERP/PII aparece en indexación pública |
| T03 | Ruta protegida mantiene 401/403 backend | Guard cliente o rewrite entrega contenido privado |
| T04 | Sesión tenant A no lee datos de B | Cookie, URL o header induce cross-tenant |
| T05 | Caché compartida solo sirve contenido público | Payload autenticado de A visible por B |
| T06 | Proxy/CORS/CSP mantienen restricciones | Origen wildcard con credenciales o script sin allowlist |
| T07 | SPA deep link y refresh conservan autorización | Fallback SPA ignora 403 y expone token |
| T08 | 320 px/teclado/foco y movimiento reducido funcionan | CLS o foco perdido por lazy-load/bundle |
| T09 | Rollback reproduce release, headers y cache | Revert Git deja edge incompatible |
| T10 | CI, A5 y Scope Lock acreditan SHA correcto | SKIPPED/CANCELLED/self-review se computa PASS |

## 5. Evidencia, rollback y salida A0–A5

- **A0:** alcance estrictamente `docs/v5/c12-02-m02-01...` y sección autocontenida del CHANGELOG; E01-E12, 16 invariantes, dependencias M01, fuentes existentes; no implementar migraciones ni despliegue.
- **A1:** 10 pares T definidos; **0/10 ejecutados**. El contrato es texto y requiere inspección de integridad, no tests falsos de Angular.
- **A2:** límites JWT/RBAC/tenant A/B, privacidad, caché/headers, rechazo por defecto documentados; código de seguridad no cambia.
- **A3:** se cotejan fuentes del repo y antecedentes M01; Playwright, HTTP, MySQL y proveedores **NO_APLICA** a diff exclusivamente documental. Para futuras M03/M04 exigir integración real y negative tests según riesgo R2.
- **A4:** fast-forward/CAS del HEAD de dev, `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` completed/success por SHA, PLAN/COLA/DAG/BITACORA readback; no contar QUEUED/SKIPPED/FAILED como PASS.
- **A5:** una Task Javier distinta del autor deberá revisar requisitos, blobs, diffs y CI; sin A5 causal no liberar sucesoras M03.
- **Rollback documental:** `git revert` normal del commit después de contrastar HEAD y archivos compartidos, conservando BITACORA. Restitución de infraestructura, DNS, proxy, cache o secrets **no realizada**, requeriría permiso específico.

**Salida:** LISTO_DEV solo significa contrato conceptual documentado; el frontend separado sigue sin implementación o publicación acreditada por esta microtarea. D01–D10 y performance/seguridad runtime siguen abiertos, sin certificación de QA/main/PROD.
