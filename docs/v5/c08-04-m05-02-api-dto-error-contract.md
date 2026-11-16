# C08-04.M05.02 — Contrato seguro de Application DTO, evidencias y errores

**Autor técnico:** SOLQARYN PRIMARY DEV 01, Task ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Rama:** `dev`. **Riesgo:** R2, autorización, tenant y evidencias de backend. **PROJECT_SCOPE_LOCK:** STRICT.

## A0: alcance real y superficie de impacto

El contrato de M02.02 define operaciones HTTP **futuras**, no registradas. El incremento M05.02 comprueba la superficie real y compilada de `C0804SectorModuleApplicationGate.EvaluateAsync`: DTO `Request`, DTO `Result` y lector `IC0804SectorModuleEvidenceReader`. Repara una excepción del lector que antes podía propagar un mensaje sensible del proveedor, sin entregar un código de rechazo estable. El nuevo rechazo es `TRUSTED_MODULE_EVIDENCE_UNAVAILABLE`. Se conservan el caso positivo `TRUSTED_MUTATION_ADAPTER_REQUIRED` no autorizante, los vetos por tenant, RBAC/revocación y la propagación explícita de cancelaciones.

Capa Application y pruebas xUnit. No crea controlador/ruta HTTP, registros DI, datos, migraciones, JWT, grants, un proveedor live ni acciones sobre módulos. Los límites de autorización siguen fail-closed, la UI no otorga privilegios y toda mutación futura requiere CAS autenticado, permisos y auditoría. No cambiar Domain/Infrastructure. Consumidores actuales: `C0804SectorModuleAuthorizationFence` y su `StaticEvidenceReader`, además de pruebas de la suite sectorial.

## A1/A2: oráculos capaces de detectar defectos

- Positivo existente: evidencia confiable completa produce exclusivamente solicitud de adaptador futuro; `AuthorizesTenantAccess=false`, `AuthorizesModuleActivation=false`, `WritesToDatabase=false`.
- Negativos existentes: tenant B ajeno, owner diferente, SHA obsoleto, JWT/RBAC/revocación y client overrides no habilitan adaptador.
- Nueva regresión adversarial: excepción del lector con texto `SECRET_JWT_AND_TENANT_B` produce solo código opaco, no privilegio.
- Nueva frontera de disponibilidad: timeout del lector produce el mismo código seguro sin compartir host o contraseña.
- Cancelación desde la lectura: `OperationCanceledException` se propaga, no se clasifica como éxito.
- Entrada DTO con SHA inválido: rechazo previo a I/O (`Calls=0`), aun con tenant assertion adversarial.

## A3/A4: evidencia y no-aplicabilidad

CI dirigida por GitHub-hosted `.github/workflows/v18-c08-04-m05-02-contract.yml`, ejecuta `dotnet test` Release con filtro de pruebas `C0804SectorModuleApplicationGateTests`. También exigir Backend Release, Seguridad multiempresa, Scope Lock y V5 SUCCESS en SHA publicado, o equivalencia de blobs/CI demostrada; nunca `SKIPPED/CANCELLED/QUEUED/IN_PROGRESS` como PASS. Este test usa fuente sintética in-process: **NO_ACREDITA HTTP real, JWT middleware, MySQL 8.4, integración de proveedor ni activación real**. A3 MySQL/HTTP/live NO_APLICA a esta modificación puramente Application, pero la certificación futura de rutas del contrato M02.02 sigue pendiente.

**Reversión:** `git revert` ordinario del commit, preservando historia y BITACORA. **A5:** otra Task Javier distinta de la autora; revisión del diff, causas, negativos, SHA y checks. Nunca promover a QA/main/PROD por este cierre.
