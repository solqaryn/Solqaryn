# SOLQARYN V17 — C13-02.M02.04: configuración tipada y feature flags seguros de accesibilidad

**Microtarea:** C13-02.M02.04. **PLAN_MAESTRO:** fila 1919. **Autor:** PRIMARY DEV 06 Javier, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Riesgo R0:** contrato documental, sin runtime. **Repositorio único:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. Autoridad V5 FINAL, V15 adaptativa, V16 secuencial y V17 candado atómico.

## Propósito y no-alcance

Esta unidad define **opciones tipadas, ámbitos, precedencia y flags conceptuales** para pruebas y experiencias accesibles de teclado/foco, contraste, reflow, estados, errores y lectores, sin alterar controles de autorización. No escribe TS, Angular, .NET, SCSS, EF/MySQL, configuraciones en ejecución, secrets, base de datos, feature services ni CI. Cualquier implementación futura de estos flags en Angular/API será otra tarea y necesitará pruebas genuinas, A0–A5 y chequeos de tenant. No certifica WCAG, no ejecuta Playwright/Chromium ni ajusta umbrales normativos.

**Contratos relacionados:** M01.01–06, incluido `docs/v5/c13-02-m01-06-gates-rollback-liberacion-accesibilidad.md` (16 gates G01–G16), y `docs/v5/c13-02-m02-01-modelo-conceptual-accesibilidad.md` (16 conceptos, 20 invariantes, 12 pares P/N FUTUROS_NO_EJECUTADOS). M02.03 define solamente la persistencia conceptual: este contrato no le asigna valores runtime ni declara migración realizada. M02.02/M02.05 son unidades con owner y DAG propios.

## Esquema propuesto de configuración tipada (no código)

| Opción tipada propuesta | Tipo conceptual | Valor seguro inicial / restricción |
| --- | --- | --- |
| accessibility.enabled | boolean | `true` para controles básicos; nunca omitir accesibilidad por bandera |
| keyboard.focusVisibleRequired | boolean | `true` siempre en navegación por teclado |
| keyboard.escapeClosesOverlay | boolean | `true` donde el componente y semántica lo permitan |
| keyboard.restoreFocusOnClose | boolean | `true`; si falla, registrar defecto reproducible |
| keyboard.skipLinkEnabled | boolean | `true`; destino real y estable |
| forms.textErrorRequired | boolean | `true`; prohibido error solo color |
| forms.labelRequired | boolean | `true`; nombre accesible por control |
| overlays.backgroundInert | boolean | `true` para diálogos modales |
| visual.allowReducedMotion | boolean | `true`; nunca exigir animación para consumir información |
| visual.contrastEvidenceRequired | boolean | `true` antes de atribuir PASS |
| visual.testViewports | uint[] | [320,390,768,1280] solo matriz futura, no cobertura ejecutada |
| visual.zoomTestLevels | uint[] | [100,200,400] matriz futura con validador de rango |
| visual.supportForcedColors | boolean | `true`; el estado no puede desaparecer |
| feedback.announceLoading | boolean | `true` sin interrumpir foco útil |
| feedback.announceError | boolean | `true` con texto redactado y recuperación |
| feedback.noSensitiveSuccessDetail | boolean | `true`; nunca usar estado UI como autorización |
| assistive.landmarksRequired | boolean | `true`; nombres e idioma consistentes |
| assistive.requireManualReview | boolean | `true` donde el lector/UX exija valoración humana |
| privacy.redactEvidence | boolean | `true`; JWT/cookies/PII prohibidos en artifacts |
| security.requireServerSideTenant | boolean | `true` y **no configurable a false** |
| security.requireLiveRBAC | boolean | `true` y **no configurable a false** |
| security.denyRevokedActor | boolean | `true` y **no configurable a false** |
| audit.requireSourceSha | boolean | `true`; `PASS` sin SHA es inválido |
| audit.requireIndependentA5 | boolean | `true`; revisor distinto de autor |

Son **24 propuestas de contrato**, no flags creados. Las claves con prefijo security, privacy o audit nunca pueden degradar JWT/RBAC, aislamiento tenant A/B, redacción o controles de aprobación. En implementación futura, datos inválidos o ausentes deben resolverse fail-closed, no conservar permisos por defecto.

## Ámbitos, precedencia y resolución segura

Ámbitos propuestos: **PLATFORM** (estándar mínimo, seguridad no sobreescribible), **TENANT** (empresa A o B sin herencia cruzada), **MODULE** (componente y grupo de rutas), **SURFACE** (vista/estado particular), **USER_PREFERENCE** (preferencias inocuas como reduced motion). La evaluación efectiva futura debe ser `PLATFORM_SECURITY ∧ TENANT_ALLOWED ∧ MODULE_ALLOWED ∧ SURFACE_ALLOWED` para controles opcionales, con preferencia de usuario solo cuando no relaja un requisito. Un `false` de ámbito inferior jamás puede desactivar redacción/roles/tenant; un `true` inferior no puede habilitar superficie denegada por superior.

- **P01:** misma superficie en A y B mantiene configuración independiente; sobreescritura A no afecta B.
- **P02:** módulo aplica subopción no sensible sobre default platform permitido, sin eliminar controles mínimos.
- **P03:** preferencia reduced-motion reduce movimiento sin retirar funcionalidad ni información.
- **P04:** estado de error accesible conserva relación label/error y redacción sin detalles sensibles.
- **P05:** versión de configuración se fija por `source_sha + config_revision` para reproducir resultados.
- **P06:** rollback de opción permitida deja el baseline de foco/teclado utilizable.
- **N01:** tenant B intenta leer/editar opciones privadas de A y debe rechazarse.
- **N02:** surface o usuario intenta poner `security.requireServerSideTenant=false` y debe fallar.
- **N03:** rol revocado intenta usar flag activado para invocar endpoint y backend debe denegar.
- **N04:** valor de viewport fuera de rango (0, negativo, desbordamiento) debe rechazarse.
- **N05:** configuración ausente/malformada no admite éxito ficticio de accesibilidad.
- **N06:** colisión o replay de revisión obsoleta no pisa ajuste más reciente sin CAS.

Todos P01–P06 y N01–N06 son **FUTUROS_NO_EJECUTADOS**. No atribuir a esta unidad un assertion de backend, un test browser o verificación WCAG que no haya ocurrido.

## Invariantes y contratos de error

1. I01: tenantId de configuración y recurso coincide con el actor autentificado; rutas públicas no autorizan acceso a datos empresariales.
2. I02: configuración de UI no sustituye JWT, RBAC ni revocación en API.
3. I03: revisiones de configuración son inmutables para evidencia ya cerrada; registrar `config_revision` y SHA.
4. I04: cache de configuración futura se particiona por tenant y recurso; nunca globalizar valores A/B.
5. I05: permisos de escritura de config requieren autoridad real; flags no son grants.
6. I06: dato inválido adopta estado seguro, nunca permiso más amplio.
7. I07: errores se muestran con texto, foco accesible y contenido redactado.
8. I08: contraste, zoom y lector requieren medición/revisión reales antes de PASS.
9. I09: preferencias no eliminan required focus, labelling, reflow o aria.
10. I10: un resultado de CI SKIPPED/CANCELLED/IN_PROGRESS/FAILED no equivale a prueba aprobada.
11. I11: ningún controlador usa `accessibility.enabled=false` para saltar validaciones del backend.
12. I12: compatibilidad de flag nuevo es aditiva y reversible, con prueba A/B y rollback documentado.
13. I13: feature flag inaccesible/desconocido no cambia la titularidad del recurso.
14. I14: `NO_APLICA` de pruebas runtime a un Markdown no es certificación de interfaz.
15. I15: A5 independiente no se considera aprobado por el autor ni por leer un resumen.
16. I16: no medir cumplimiento WCAG sin estándar/versión/nivel/autoridad decididos y evidencias completas.

Errores conceptuales para un futuro contrato API: `CONFIG_TENANT_MISMATCH`, `CONFIG_FORBIDDEN_OVERRIDE`, `CONFIG_REVISION_CONFLICT`, `CONFIG_INVALID_RANGE`, `CONFIG_UNTRUSTED_SOURCE`, `CONFIG_SENSITIVE_VALUE` y `CONFIG_UNKNOWN_FLAG`. Son **nombres propuestos**, no DTO/HTTP implementados. No devolver detalles de otros tenants o secretos.

## Compatibilidad, pruebas futuras y rollback

La implementación real deberá verificar Angular 22.2.1 (tipado/compilación, semántica, teclado y Vitest), Playwright/Chromium en rutas afectadas y .NET 10 (JWT/RBAC, tenancy A/B, revocación, errores negativos). Si se cambia persistencia EF, MySQL 8.4 aislado con tests `Category=Integration` genuinos. El proveedor de mapas/Cloudinary/SMTP no participa en este diff.

**A0 R0:** revisar ID, PLAN/COLA/DAG, seis M01 y contratos M02, 24 opciones tipadas, matriz de ámbitos, seis casos positivos y seis negativos diseñados; diff exclusivo a documento + CHANGELOG.

**A1 R0:** comprobar coherencia estática de los casos y errores, no presentar P/N futuros como pruebas ejecutadas.

**A2 R0:** revisar invariantes 01–16, precedencia de permisos, tenant A/B, revocación y redacción como obligaciones que deben fallar cerrado en implementación.

**A3 R0:** `NO_APLICA` builds/backend/Angular, browser, EF/MySQL y proveedores por diff sólo Markdown.

**A4 R0:** commit `dev` serializado por fast-forward/CAS, checks Scope Lock y V5 `completed/success` sobre SHA causal o equivalencia probada, PLAN/BITACORA readback. No contar auxiliar SKIPPED/IN_PROGRESS.

**A5:** una Task Javier diferente del autor revisa fuentes, contenido, debilidades y pruebas futuras; puede registrar rechazo. M02.06/M03/M04 requieren DAG/puertas reales.

Rollback documental con `git revert` normal, preservar BITACORA histórica y recalcular sucesoras. No desplegar a QA/main/PROD, alterar secretos ni modificar runtime o base en este alcance.

**Resultado:** contrato versionable de configuración accesible y flags restringidos por tenant, sin software o certificación WCAG.
