# SOLQARYN V17 — C14-02.M03.03 autorización, tenancy y denegación por defecto

**Microtarea:** C14-02.M03.03. **Riesgo:** R2 (JWT/RBAC, BOLA, host, archivos, jobs y auditoría). **Owner:** DEV06 Task `6af16bbad26881918c283abe06e1f32a`. **Rama:** dev; Scope Lock STRICT.

## A0 — Alcance y dependencias
Se implementa `C1402SecurityAcceptanceAuthorizationGate` (capa Application) con lector `IC1402SecurityAcceptanceEvidenceReader` inyectado, utilizando los contratos previos `V5SecurityAcceptanceApplicationService` de M03.02 y `V5SecurityAcceptanceDomainRules` de M03.01. Predecesores M02.01–06 y M03.01–02 comprobados con DAG validado y A5 independiente. Modifica solo código de prevalidación, pruebas xUnit, documento y CHANGELOG. No se crean controllers, DI efectivo, migraciones, HTTP, exportadores, brokers, jobs ni usuarios.

El lector de evidencia debe obtener de backend confiable SHA, JWT verificado, membresía viva, rol/RBAC relacional, ownership persistido, recurso y fence de auditoría. No se aceptan valores de cliente para conceder permisos. `CheckAsync` valida antes de I/O recurso acotado, SHA exacto, superficie reconocida y rechaza override de tenant/rol/restore; valida después la fuente confiable, tenant A/B, revocación, y aplica Domain/Application previo. Devuelve siempre un resultado de **prevalidación no autorizante**.

## A1 — Pruebas positivas, negativas y fronteras
La suite `C1402SecurityAcceptanceAuthorizationGateTests` incluye doce xUnit [Fact] para flujo positivo sin concesión, solicitudes nulas, SHA/ID inválidos, overrides que no llegan al lector, superficie desconocida, recurso extranjero, SHA antiguo, JWT/RBAC/membresía vencidos, tenant B y recurso revocado, superficie desalineada, spoofing a nivel Domain, prueba sensible, lector sin evidencia y cancelación. Ejecutar en GitHub Actions .NET 10 sobre el SHA publicado: no confundir especificación de pruebas con resultado ejecutado.

## A2 — Seguridad
El flujo nunca produce grants, access token, permiso de recurso, autorización de restauración ni certificación de pentest. Incluso al cumplir precondiciones exige otro adaptador que vuelva a autenticar y consultar permisos/propiedad en el backend real. Un SHA, un ID o una prueba sintética no son credenciales. La ruta fail-closed preserva BOLA/IDOR, revocación, separación A/B y denegación de restore.

## A3 — Integración y medición
Integra código Domain->Application mediante xUnit reales de CI. MySQL EF/migraciones, HTTP, Playwright, SLO/observabilidad, pruebas de proveedor y pentest ejecutable son **NO_APLICA** al changeset de un gate Application aislado, nunca PASS ni ceros ficticios. La aceptación completa C14-02 permanece pendiente de sus microfases M05/M06 y evidencia R2 separada.

## A4 — Checks causales y rollback
Se requieren `SOLQARYN - Build y pruebas backend`, `Backend Release y pruebas`, `SOLQARYN - Seguridad multiempresa`, `Authorization, files, secrets and tenant gate`, Scope Lock y procedencia V5, todos completed/success en SHA exacto o descendiente equivalente con blobs idénticos. CANCELED/FAILED/SKIPPED/QUEUED/IN_PROGRESS no constituyen PASS. Plan/Cola/DAG/BITACORA con readback, candado V17 2000002068 y rollback `git revert` normal, sin QA/main/PROD.

## A5 — Independencia
La revisión del código y pruebas corresponde a otra de las cinco Tasks Javier distinta del autor DEV06, sin autoaprobación. Ningún PASS de dev da autorización de deploy.
