# C07-01.M05.05 — negativas reales JWT/HTTP/MySQL 8.4 (V22)

PLAN D1038: ejecutar caso negativo de seguridad, aislamiento o abuso, no certificar automáticamente la meta acumulativa E «Completa M05». Riesgo R2, solo dev, PROJECT_SCOPE_LOCK STRICT.

A0: se repara C0701CatalogVerifiedActorSource para exigir canonicalización decimal exacta del tenant de ruta. "0N" no equivale a "N", aun con JWT firmado y membresía válida. Se añade comprobación de JWT con claims "id" duplicados; no aceptar una autoridad ambigua. No alterar concesiones, tokens, datos productivos, pago, DI ni permisos. El adaptador permanece read-only y opt-in OFF por defecto.

A1/A2/A3: prueba integrada existente C0701M0303SignedJwtMysql84HttpIntegrationTests contra servidor HTTP real in-process, Bearer con firma e issuer/audience/expiración, Oracle EF en MySQL 8.4 efímero: acepta A permitido y deniega (403) ruta alias "0A", empresa B sin membresía, claims "id" firmados duplicados y revocación efectiva de RolPermiso; token adulterado y ausencia de token deniegan 401. Nuevas aserciones comparan comportamiento en el servicio real, no solo mocks. No certifica producto persistido o endpoint comercial nuevo, y los tests se ejecutan únicamente en el assembly xUnit. Requiere evidencia de TRX y versión del servidor para A3.

A4: workflow GitHub-hosted exact SHA, .NET 10 Release y TRX con cero fail/skip, Scope Lock y skill self-test; verificar checks globales requeridos, seguridad, backend y procedencia V5 antes de LISTO. Hasta entonces VALIDANDO. Rollback mediante git revert normal, sin borrar historia. CHANGELOG_AI.md >1MB permanece intacto por riesgo probado de truncamiento, comprobación V5 obligatoria. A5 por otra Task Javier distinta de DEV03. NO QA/main/PROD.
