# C14-04.M05.03 - integracion real de proveedor GitHub Checks en DEV

Riesgo R2 por evidencia de release y concurrencia. Unica rama solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT. Una sola EN_PROGRESO propia, lock V17 fila 2152.

A0: Se incorporan script Node 24 read-only, pruebas negativas, workflow GitHub Actions real, documentacion y CHANGELOG. No se altera Angular, .NET, MySQL, JWT, RBAC, API, tenants ni infraestructura. Rollback: git revert normal bajo HEAD CAS. El token GITHUB_TOKEN efimero solo se usa con permisos checks:read y contents:read; jamas se registra.

A1: positivo exact-SHA, rechazo de SHA ajeno, required queued/in_progress/cancelled/skipped/failed, ausencia y duplicados de checks, paginacion incompleta y respuesta no valida. A2: no se acepta autoridad de release, QA o PROD, ni se certifica tenant, aunque todos los checks leidos esten verdes.

A3: el job GitHub-hosted ejecuta autenticadamente GET /repos/solqaryn/Solqaryn/commits/SHA/check-runs?per_page=100 sobre el SHA actual con token de solo lectura, sin usar proveedores simulados para afirmar integracion. No hay push, dispatch de despliegue ni modificacion de reglas. La suite Node adversarial utiliza respuestas sinteticas y es independiente del GET real.

A4: verificar que el job dirigido, Scope Lock, V5 y los checks causales del SHA publicado terminen completed/success; queued/in_progress/skipped/cancelled no son PASS. El indicador CHECKS_READ_ONLY_VERIFIED solo significa lectura consistente, NO permiso de salida. A5: revision independiente por otra Task Javier. Quedan fuera de alcance JWT firmado, MySQL empresarial, Vercel/Render/Aiven, DNS, soporte real, backups, RPO/RTO y toda autorizacion R3/QA/main/PROD.
