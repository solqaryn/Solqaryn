# SOLQARYN V16 — C12-02.M02.04 configuración tipada, ámbitos y flags seguros del frontend separado

**Microtarea:** C12-02.M02.04, PLAN fila 1775; PRIMARY DEV 04 Task `6aeacb7820ac8191b7d768a6f33dfa09` · dev · Scope Lock STRICT · **V15 R0 documento**. No es migración de proveedor ni configuración activada.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`) y Sheet PLAN/COLA/DAG. Antecedentes M01.01–06 LISTO+A5; M02.01/02 son contratos documentales, no código aplicado. D01–D10 de M01 siguen decisiones PENDIENTES.

## 1. Fuente técnica y delimitación de ámbitos

La rama dev contiene `frontend/angular.json` blob `8b00f8fee80fdfafe1864e233be7d5f20ad444e3` (budget de build initial y configuración de Angular) y `frontend/vercel.json` blob `5f12bcf667a21afcfc4314b66787ff66627ae666` (reglas de rewrite/ignore build). **Leer configuración no prueba publicación ni un estado live**. Cualquier edición de dichos archivos, CSP/CORS/headers de proveedor, DNS, Hostinger, Vercel, Cloudflare o rollback de edge sería otro alcance con riesgo R2/R3 y autorización propia. Los valores de este documento son **propuestos, inertes, no variables reales de entorno**.

Se distinguen los ámbitos lógicos `release`, `entorno`, `seguridad`, `enrutamiento`, `privacidad` y `autorización`. Las propuestas no incluyen credenciales, strings de conexión ni una flag que deshabilite JWT/RBAC o permita que el navegador elija tenant. Presupuesto frontend actualmente presenta discrepancia documentada 650/750 KB vs 1/2 MiB, que no se resuelve cambiando valores en texto.

## 2. Catorce opciones CFG01–CFG14 conceptuales

| ID | Clave propuesta | Tipo/valor permitido conceptual | Ámbito | Propósito/denegación |
| --- | --- | --- | --- | --- |
| CFG01 | ReleaseSha | SHA 40 hex, inmutable | release | Identidad de artefactos |
| CFG02 | AssetManifestHash | digest fijo verificable | release | Previene mezcla de versiones |
| CFG03 | MinimumClientVersion | semver explícito | release | Compatibilidad progresiva |
| CFG04 | PublicAssetBaseAlias | alias de CDN allowlisted | entorno | Nunca URL arbitraria/SSRF |
| CFG05 | ApiBackendAlias | alias de backend verificado | entorno | No envía auth a tercero |
| CFG06 | PrivateCachePolicy | enum NO_STORE | seguridad | No caché compartida de identidad |
| CFG07 | PublicCacheTtlSeconds | entero acotado >=0 | entorno | Sin tokens/sesión en cache |
| CFG08 | CspPolicyRevision | identificador versionado | seguridad | No script arbitrario |
| CFG09 | CorsOriginAllowlist | lista de orígenes explícitos | seguridad | No wildcard + credenciales |
| CFG10 | SpaFallbackPolicy | enum PUBLIC_ONLY/SAFE_ROUTE | enrutamiento | No bypass de 401/403 |
| CFG11 | SeoPublicRouteAllowlist | lista de rutas públicas | enrutamiento | Sin ERP/PII ni token |
| CFG12 | BrowserTelemetryMode | enum OFF/SANITIZED | privacidad | No eventos con PII |
| CFG13 | RollbackReleaseSha | SHA anterior confirmado | release | No promover sin equivalencia |
| CFG14 | DeploymentTarget | enum DEV_ONLY | autorización | QA/PROD inhabilitado |

**No emitir ninguna opción al bundle sin revisión.** Se mantendrá la distinción entre configuración pública permitida y configuración secreta del servidor. Flags de seguridad fail-closed, versionado y rollout acotado; nunca activar una opción que cambie dominios/ambientes desde texto.

## 3. Catorce reglas V01–V14, para pruebas FUTURAS_NO_EJECUTADAS

| ID | Regla de validación obligatoria |
| --- | --- |
| V01 | Bloquear SHA vacío/malformado y manifiesto sin hash |
| V02 | Rechazar versiones antiguas incompatibles antes de reemplazar assets |
| V03 | Permitir solo alias conocido y dominio canonical validado, no URL SSRF |
| V04 | Validar backend alias contra entorno; nunca transferir token a otro host |
| V05 | Impedir caché de respuestas con sesión/Authorization/cookie |
| V06 | Acotar TTL público; no usar privado como default |
| V07 | CSP versionada restrictiva y sin ejecución arbitraria |
| V08 | CORS con credenciales exige allowlist, no * |
| V09 | Fallback SPA solo en rutas aprobadas sin suplantar 401/403 backend |
| V10 | SEO indexa solo rutas públicas verificadas |
| V11 | Telemetría exige redacción de PII, tokens y DSN |
| V12 | Rollback exige SHA original y artefactos anteriores verificables |
| V13 | DeployTarget=DEV_ONLY en esta tarea, sin producción implícita |
| V14 | Tenant y roles se resuelven exclusivamente mediante backend JWT/RBAC A/B |

Aislamiento A/B, revocación de roles, respuestas 401/403 y ownership se validan **en backend**, no por un booleano de cliente. Ningún header de browser, variable pública o feature flag concede permisos.

## 4. Diez pares T01–T10 — oráculos futuros

| ID | Positivo que se deberá ejecutar | Negativo/error que debe fallar |
| --- | --- | --- |
| T01 | Hash de release verificable | SHA manipulada o manifiesto roto falla |
| T02 | Base alias pública permitida | URL externa o host SSRF se rechaza |
| T03 | API alias conserva autenticación | Token enviado a host no aprobado falla |
| T04 | No-store en privados | Caché pública de tenant B desde A falla |
| T05 | CSP restringe script | Script arbitrario permitido falla |
| T06 | CORS exacto autorizado | Wildcard con credenciales falla |
| T07 | Deep link SPA público correcto | Fallback de ruta privada ignora 403 falla |
| T08 | SEO público indexable | Ruta ERP o PII en sitemap falla |
| T09 | Rollback autorizado y reversible | SHA anterior sin prueba o manifiesto falla |
| T10 | Entorno DEV aislado y sin secretos | QA/PROD o logging de DSN en cambio falla |

**Ejecutados aquí: 0/10.** No se hizo build de código modificado ni validación de servidor, browser, proveedor, MySQL o HTTP. Los oráculos deberán implementarse con fixtures sintéticos, SHA y registro de resultados para R2 futuro.

## 5. Matriz A0–A5, compatibilidad y rollback

- **A0:** alcance en este Markdown y sección íntegra `CHANGELOG_AI.md`; 14 opciones tipadas conceptuales, seis ámbitos, relación M01/M02 sin cambiar runtime.
- **A1:** 14 reglas y 10 pares positivos/negativos especificados; pruebas futuras, no PASS.
- **A2:** seguridad tenant A/B, JWT/RBAC, CORS/CSP, host allowlist, privacidad y segmentación cache fail-closed documentadas, ninguna concesión real.
- **A3:** verificadas fuentes Angular/Vercel por blob; build, E2E, MySQL y proveedor NO_APLICA porque el diff es R0 Markdown. Futura aplicación exige CI real por SHA, pruebas negativas y autorización.
- **A4:** commit fast-forward/CAS directo dev, `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` completed/success exact SHA, PLAN/COLA/DAG/BITACORA readback.
- **A5:** revisión independiente por Task Javier distinta del autor, con pruebas/contratos y SHA reales; no self review ni desbloqueo técnico por VALIDANDO.
- **Rollback para este documento:** `git revert` convencional tras revisar HEAD/owners, conservar BITACORA. No existe rollback de hosting/dominios ejecutado en esta entrega.

**Salida contractual:** LISTO_DEV solo certifica documento R0, no flags activas, deploy, CSP real, cambio de presupuesto, QA/main/PROD, secretos o servicios de pago.
