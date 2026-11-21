# SOLQARYN V22 — C06-01.M03.02: servicio Application de preflight público

**Microtarea:** C06-01.M03.02, PLAN D879: «Implementar caso de uso, servicio o aplicación con límites claros». **Autor:** PRIMARY DEV03 JAVIER, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. Rama exclusiva `dev`; PROJECT_SCOPE_LOCK=STRICT. Clasificación R2 por frontera futura de publicación, aislamiento y confianza del servidor.

## A0 — entrega atómica y límites

`C0601LandingApplicationService.PrepareAsync` encadena una interfaz de contexto canónico del servidor (`IC0601VerifiedPublicReadContextSource`), una interfaz de contenido verificado por ruta (`IC0601VerifiedPublicContentSource`) y los predicados existentes `C0601LandingDomainRules.Evaluate` de M03.01. La ruta de consulta proviene exclusivamente del contexto devuelto por la fuente servidor, no de un DTO cliente. Un candidato válido devuelve `PLATFORM_PUBLICATION_AND_LEGAL_GATES_STILL_REQUIRED`, sin contenido, permisos ni efectos. `AuthorizesPublication`, `AuthorizesPrivateAccess`, `AuthorizesPayment`, `CertifiesJwtMysqlOrHttp` y `ContainsPublishedContent` son incondicionalmente false.

El servicio es una frontera de aplicación verificable, **no** un endpoint, DI registrado, adapter JWT, SQL, HTTP, CMS, proveedor real o publicación. Las interfaces todavía necesitan implementaciones confiables y persistencia verificable en una microtarea posterior; un fake no constituye autenticación. No se alteran Domain, entidades, datos, migraciones ni configuración de producción.

## A1 — pruebas adversariales reproducibles

`C0601M0302LandingApplicationServiceTests` comprueba candidato positivo no autorizado, lookup únicamente por ruta servidor, contextos nulos/no verificados sin lectura posterior, suplantación del host A/B, fuga tenant, rutas privadas, markup, precio/legal sin autoridad, draft/revisión caduca, fuentes indisponibles, snapshot ausente, cancelación previa y durante la lectura, y dependencias nulas. Se ejecuta mediante `dotnet test ... --filter Category!=Integration` en GitHub-hosted .NET 10. Un SUCCESS de compilación no sustituye la ejecución real de tests.

## A2–A3 — seguridad, integración y limitación de fase

El cambio solo añade código Application y unitarias, sin API, autenticación real, MySQL, UI o infra: `NO_APLICA_A_M03_02` para integración HTTP/JWT/RBAC/MySQL/Chromium en este diff, **nunca PASS**. Las fuentes de confianza son puertos sin implementaciones; no se certifica origen real, publicación, dominio público, price/legal D01–D08, SEO, consentimiento, SMTP, leads, multiempresa en runtime o seguridad general. Rechazo fail-closed; no retorna PII ni secretos.

## A4/A5, rollback y seguimiento

Verificar SHA fuente publicado FF/CAS en `dev`, Scope Lock, V5/provenance y pruebas backend/seguridad pertinentes del SHA causal o equivalencia de blobs auditada. `SKIPPED/CANCELLED/FAIL/IN_PROGRESS` no cuentan como PASS requerido. Mantener PLAN en VALIDANDO con owner DEV03 cuando CI externa pendiente y actualizar BITACORA append-only. Sólo pasar a LISTO cuando A0–A4 aplicables estén comprobados, mediante lock V17, CAS y readback. A5 por otra Task Javier autorizada con revisión independiente (DEV03 no se autoaprueba). No desbloquear sucesoras por VALIDANDO.

Rollback mediante `git revert` ordinario con nueva CI en dev, nunca force push; no editar historia BITACORA, formulas DAG/COLA, QA, main, PROD ni pagos.
