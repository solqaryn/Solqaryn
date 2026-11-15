# SOLQARYN V5 — C05-01.M02.01: entidades, ownership e invariantes del sistema de diseño

**Entrega:** modelo contractual R0 de design tokens, componentes, accesibilidad y personalización multiempresa. **Autor:** PRIMARY DEV 03 JAVIER, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Fecha:** 2026-11-15.

## 1. Autoridad, antecedentes y límites reales

El ID `C05-01.M02.01` exige «Definir entidades, ownership, relaciones y reglas de invariantes». La autoridad ejecutable es el **STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL** (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), junto con PLAN_MAESTRO, COLA, DAG_TECNICO y `docs/QUALITY_RISK_AUDIT_V15.md`. Se constató el antecedente `C05-01.M01.01–06` en LISTO con A5 independiente y `C05-01.M01.06` revisada por una Task Javier distinta de su autor. Antes de la reclamación, COLA G728/O728=SI y DAG A689 F=VALIDADO/G=SI; PLAN H728=PENDIENTE e I728 vacío. Una sola reclamación con CAS de H728 y readback de I728 conserva la exclusividad.

**Esta entrega es exclusivamente un contrato documental:** las entidades siguientes NO son entidades EF, tablas, DTO, APIs ni componentes Angular instalados. No hay modificación de estilos, tema, JWT, RBAC, almacenamiento, secretos, jobs, migraciones, proveedor o despliegue. Las verificaciones de browser, contraste, responsive y MySQL se definen para implementación futura y NO se atribuyen como ejecutadas. Arquitectura presente: Angular 22.2.1/Material-CDK con Zone.js 0.16.3 y autoridad de sesión/tenant en ASP.NET Core 10, capas Domain–Application–Infrastructure–API, MySQL/EF Core 10. Un guard visual o un tema NO confieren permiso backend.

## 2. Entidades conceptuales, ámbito y propietarios (DS01–DS14)

| ID | Entidad/contrato futuro | Identidad y ownership | Regla observable |
| --- | --- | --- | --- |
| DS01 | SemanticToken | `tokenKey` y catálogo global versionado; owner plataforma | Los nombres son allowlist semántica, no CSS arbitrario |
| DS02 | TokenRevision | `tokenKey + revision`; owner plataforma | Revisión monotónica e inmutable tras publicación |
| DS03 | ComponentPattern | `patternKey + version`; owner plataforma | Define slot, estados, interacción y consumidores, no datos de otro tenant |
| DS04 | ComponentTokenBinding | `patternKey + tokenKey + revision`; owner plataforma | Resolver alias antes de visualizar, sin ciclos ni tokens ausentes |
| DS05 | ThemePreset | `presetId + version`; owner plataforma | Paleta y espaciado aprobados con alternativas accesibles |
| DS06 | TenantThemeAssignment | `empresaId + assignmentRevision`; owner tenant autorizado por servidor | Máximo un tema efectivo por empresa y revisión |
| DS07 | TenantBrandingOverride | `empresaId + overrideKey`; owner tenant | Sólo valores tipados/saneados; jamás HTML/JS/CSS libre |
| DS08 | BrandAssetReference | `empresaId + assetId`; owner tenant | Referencia opaca a medio autorizado, sin host ni secreto libre |
| DS09 | ThemeActivationIntent | `empresaId + intentId`; owner tenant + actor backend | Estado propuesto no cambia tema activo sin gate y revisión |
| DS10 | ThemeRevisionAudit | `eventId`; owner tenant; registro append-only | Historial con actor autorizado, SHA y valores saneados |
| DS11 | AccessibilityAcceptance | `componentVersion + viewport + theme`; owner del patrón | Evidencia real futura de contraste, foco, teclado y reduced-motion |
| DS12 | ComponentRegressionManifest | `manifestId + commitSha`; owner plataforma | Enumera pruebas reales por superficie y resultado exact-SHA |
| DS13 | TenantThemeContext | `empresaId + sessionEpoch`; owner sesión backend | Limpiar A antes de aceptar B; resultados HTTP antiguos se descartan |
| DS14 | SafeFallbackTheme | `presetId + version`; owner plataforma | Fallback neutral sin datos A/B ni privilegios nuevos |

La identidad y pertenencia empresarial se resuelven en backend mediante JWT validado, membresía persistida vigente y RBAC; el navegador no puede establecer `empresaId` como autoridad. El owner de DS06/07/08/09/10 NO es una clave de conexión ni una autorización independiente.

## 3. Relaciones, ciclo de vida y límites de confianza

- Un catálogo DS01 admite N revisiones DS02 y N bindings DS04; cada DS03 usa N bindings. Cambiar DS02 no altera una versión de componente ya publicada sin nueva revisión.
- DS05 contiene un conjunto de tokens DS01/DS02 y define cero o un DS14 elegible para fallo seguro; una empresa puede tener N revisiones históricas DS06, pero sólo **una asignación efectiva** bajo la revisión verificada.
- DS06 pertenece a una sola empresa y puede tener N DS07/DS08, todas con la misma propiedad y validación independiente; un override jamás puede crear una nueva propiedad o permiso.
- DS09 referencia la revisión esperada de DS06 y el actor autenticado, con clave idempotente y comparación CAS futura; `DRAFT -> REVIEWED -> VALIDATED -> READY -> ACTIVE` o `REJECTED`/ `ROLLED_BACK` sin saltar puertas. `DRAFT` y `READY` NO activan cambios.
- DS10 conserva todas las decisiones con correlación redactada; DS11/DS12 enlazan una versión concreta de tema/componente, SHA y ejecución de pruebas. Un documento de aceptación no equivale a pruebas browser ejecutadas.
- DS13 se invalida al cambiar tenant, cerrar sesión, revocar membresía o cambiar epoch. Cualquier respuesta asíncrona de A recibida en contexto B queda descartada antes de mutar el DOM o las variables CSS.
- DS14 se define como presentación neutral **sin** restaurar branding de una empresa anterior. No hay fallback hacia permisos de sesión, recursos, datos, API o infraestructura ajena.

## 4. Invariantes obligatorios INV01–INV18

1. **INV01 — Fuente de autoridad:** JWT/membresía/RBAC vigentes verificados por backend por operación; guard visual y theme no son autorización.
2. **INV02 — Tenant A/B:** no divulgar tokens privados, logos, fondos, configuración ni archivos de empresa A a B.
3. **INV03 — No CSS libre:** DS07 admite únicamente propiedades tipadas en allowlist; bloquear `url()` no autorizado, estilos HTML, scripts, selectores e inyección.
4. **INV04 — Control de assets:** DS08 requiere referencia interna autorizada, propiedad tenant y clasificación de contenido; nunca URL privilegiada sin validación.
5. **INV05 — Referencias íntegras:** DS04 resuelve tokens existentes, sin ciclos ni alias que rompan contraste.
6. **INV06 — Versionado:** DS02 y DS05 son inmutables después de publicación; cambios generan nueva revisión con historial.
7. **INV07 — Selección única:** a lo sumo un DS06 activo por empresa, con revisión monotónica y rechazo de escrituras obsoletas.
8. **INV08 — Revocación:** un permiso/membresía revocado invalida intención DS09; no se recupera ALLOW desde cache.
9. **INV09 — Idempotencia:** repetir DS09 con idéntico tenant, SHA, actor, revisión y payload no duplica activaciones ni audit events.
10. **INV10 — Conflicto de clave:** misma clave con empresa, payload o revisión diferente falla cerrado y no revela existencia de otra empresa.
11. **INV11 — Cambio A→B:** DS13 invalida branding, caches y respuestas tardías anteriores antes de visualizar B.
12. **INV12 — Contraste:** los estados normal/hover/focus/disabled/error requieren evidencia medible para colores y tipografías antes de runtime.
13. **INV13 — Teclado y foco:** componentes DS03 requieren labels, orden Tab, Escape/restauración, y nombre accesible.
14. **INV14 — Responsive:** medir 320, 768, 1280 px y zoom; tablas permiten scroll local, nunca pérdida de acción por overflow.
15. **INV15 — Movimiento reducido:** `prefers-reduced-motion` conserva funciones e información sin animación obligatoria.
16. **INV16 — Datos sensibles:** DS10 y DS12 no registran JWT, PII, URLs secretas, credenciales, contenido del cliente o datos de B.
17. **INV17 — Evidencia causal:** CI requerida `completed/success` en SHA exacto o descendiente con blobs/consumidores inalterados; no aceptar `cancelled/skipped/in_progress` como PASS.
18. **INV18 — A5 y entorno:** LISTO_DEV y A5 independiente son puertas distintas; ninguno autoriza QA, main, PROD, DNS, bases o proveedores.

## 5. Oráculos positivos/negativos futuros (NO EJECUTADOS)

| Par | Positivo futuro | Denegación / regresión exigida |
| --- | --- | --- |
| P01/N01 | Patrón DS03 consume tokens DS01/DS04 versionados | Alias circular o token no definido altera CSS silenciosamente |
| P02/N02 | Tema DS05 válido conserva contraste en todos los estados | Color de foco/error ilegible se presenta como WCAG PASS |
| P03/N03 | Sesión A aplica exclusivamente DS06/DS07 de A | B hereda logo, paleta o respuesta tardía de A |
| P04/N04 | Override tipado saneado acepta valor permitido | CSS arbitrario, `url()` remoto o HTML/JS se ejecuta |
| P05/N05 | Actor con rol y membresía vigentes propone DS09 | Token revocado, rol insuficiente o empresa ajena activa tema |
| P06/N06 | CAS de DS09 con revisión vigente publica una sola vez | Dos carreras activan revisiones distintas del mismo tenant |
| P07/N07 | Idempotencia retorna resultado único a reintento idéntico | Replay cross-tenant o payload distinto se acepta |
| P08/N08 | Falla de tema usa DS14 neutral sin exponer datos A/B | Fallback conserva sesión/caché/branding anterior |
| P09/N09 | Modal y formulario mantienen foco, labels y errores | Escape no funciona, texto no accesible, Tab salta controles |
| P10/N10 | Manifiesto DS12 enlaza prueba Chromium real y SHA | Test mock/CI cancelada o documento se atribuye a prueba real |

Los P/N son criterios de futura instrumentación, **no** tests Vitest, Playwright, xUnit, MySQL, HTTP, WCAG ni pentesting ejecutados en esta microtarea.

## 6. Calidad A0–A5, handoff y rollback

**A0 (R0):** revisar este modelo contra los seis antecedentes M01 y el diff exclusivo de documento + sección CHANGELOG; sin cambios de arquitectura ejecutable. **A1:** consistencia de DS01–14, INV01–18, cardinalidades, estados y 10 pares P/N redactados como futuros no ejecutados. **A2:** inspección estática de tenant A/B, JWT/RBAC, CSS inyectable, fuentes de assets, revocación e idempotencia, sin afirmar ejecución. **A3:** contrato alineado a Angular standalone y backend .NET actual, sin tocar API/EF/MySQL/Playwright: comprobaciones runtime **NO_APLICA al diff Markdown**, no PASS. **A4:** publicación individual en `dev` mediante fast-forward y HEAD esperado, verificación `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` con SHA exacto, más readback PLAN/COLA/DAG/BITACORA. **A5:** revisión independiente por otra Task Javier distinta de la autora, con facultad de A5_FAIL ante defectos, antes de dependencias que la requieran.

**Salida real:** contrato de entidades y reglas, no creación de tablas, tokens CSS, código Angular, integración de branding ni aceptación de accesibilidad; siguientes M02.02–.06 requieren gates reactivos propios. **Rollback:** `git revert` ordinario del changeset documental en `dev` tras verificar HEAD; no borrar BITACORA, no force-push ni restaurar datos mediante Git. Ningún cambio a QA/main/PROD, servidores, Cloudflare, DNS, Secret Manager, Aiven o pagos.
