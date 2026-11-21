# SOLQARYN V22 — C06-01.M03.01, invariantes de dominio del sitio público

**Repositorio**: solqaryn/Solqaryn, rama dev, PROJECT_SCOPE_LOCK=STRICT. **Task autora**: JAVIER PRIMARY DEV06, `6af16bbad26881918c283abe06e1f32a`. **Riesgo**: R2 por superficie pública/PII/tenancy; implementación acotada al Domain puro, sin I/O ni endpoints.

## A0: salida atómica del PLAN D878

«Implementar reglas de dominio y validaciones server side». Se añade `C0601LandingDomainRules.Evaluate` con dos entradas conceptuales inertes: un snapshot versionado que **deberá provenir de un adaptador de plataforma verificado** y un contexto de lectura que **deberá generar el servidor**. No toma headers/host/empresa aportados por cliente ni consulta ERP. El resultado `IsCandidateForFurtherReview` solamente señala que las precondiciones sintácticas y de riesgo son candidatas para revisión posterior: los cuatro flags de autoridad `AuthorizesPublication/PrivateAccess/Payment/CertifiesHttpOrMysql` son **siempre false**. No hay grants ni superficie HTTP pública creada.

Los contratos M02 C06-01 y sus A5 fijan entidades/invariantes E01–E14, INV01–INV18, G01–G16. La raíz del Angular vigente continúa llevando al login. Se preservan rutas ERP/tienda. Reglas fail-closed: host ASCII canónico, ruta ASCII sin query/fragment/escape, host/ruta exactamente equivalentes al contexto del servidor, raíces privadas negadas, versión y content key válidas, ventana de vigencia UTC [desde,hasta), aprobación y snapshot activo, rechazo de borrador, cruces tenant A/B, precio sin fuente, legal sin aprobación y marcado ejecutable.

## A1: regresiones que fallan si la regla se degrada

`C0601M0301LandingDomainRulesTests` contiene positivos solo de *candidato*, y negativos de fuente sin verificar, rutas privadas, URL traversal, query/CRLF, host spoof/no canónico, datos de tenant, precio/legal no aprobados, HTML ejecutable, borrador, revisión inexistente, claves con inyección, vigencia vencida y UTC incorrecto. Corren xUnit/.NET 10 en GitHub-hosted del SHA publicado; un build por sí solo no se reporta como integración.

## A2–A3: fronteras y limitaciones expresas

- JWT/RBAC, tenant A/B reales, lectura SQL MySQL/EF y autorización de publicación todavía **NO IMPLEMENTADOS por este ID**. M03.01 no toca esos runtimes; las pruebas HTTP/JWT/MySQL no son atribuibles a este diff puro Domain. Se registran como `NO_APLICA_A_M03_01`, **no como PASS**. La posterior M03.02/03/04 deberá implementar integración real y demostrar negativos.
- D01–D08 del M01.04 siguen sin decisiones aprobadas: precio, plan comercial, contenido legal, consentimiento, demo, SMTP, leads, SEO publicado, hosts y dominios **NO_GO**. No se conecta proveedor, no se habilita web ni se publican textos de cliente.
- Ninguna regla usa datos reales de clientes ni crea APIs, tablas, migrations, Angular routes, jobs, pagos o cookies. Los datos de prueba son sintéticos. Las precondiciones reciben registros; sin servidor con fuente confiable un atacante podría falsificarlos, de modo que **nunca autorizan ejecución**.

## A4–A5, observabilidad y rollback

A4 exige SHA exacto de commit fast-forward con Scope Lock/V5 y .NET Release/xUnit, backend y seguridad relevantes `completed/success`; estado `queued/in_progress/skipped/cancelled/failed` de un check obligatorio nunca PASS. Preservar `PLAN H878=VALIDANDO` mientras CI externa verifica, `I878` del autor original, BITACORA append-only con RUN/SHA/event_id y readback de COLA/DAG sin forzar fórmulas. `A5` debe practicar revisión genuina una Task Javier distinta de DEV06. `CHANGELOG_AI.md` queda inalterado en este changeset para no truncar ni intercalar la historia de >1 MB: el expediente incremental queda aquí y en BITACORA; el verificador V5 existente sigue gobernando provenance.

Rollback: `git revert` ordinario del changeset en dev y nueva validación/cadena de eventos institucionales; nunca force-push/reset ni borrar BITACORA. No QA, main ni PROD.
