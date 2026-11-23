

## 2026-11-22 — C03-03.M03.03: autorización JWT/RBAC/tenant read-only audit (R2)
- Adaptador API Security real C0303VerifiedAuditTenantReaders, opt-in DEV OFF por defecto: JWT Bearer firmado, tenant server-side, membresía y rol administrador en SQL, grant Auditoria.Ver actual y revalidado; source Empresa audit-read exclusivamente. No concede privilegios, acciones críticas, ni escribe auditorías.
- xUnit con esquema JWT firmado y EF InMemory adversarial para A/B, spoof route/JWT, rol/permisos, revocación, bloqueo, recurso cruzado, cancelación; **no** certifica MySQL 8.4 físico, HTTP ni sink durable. GitHub-hosted exact SHA requerido, VALIDANDO pendiente mientras CI externa.
- Scope Lock STRICT, V17 CAS, solo dev, sin migraciones ni nuevos endpoints; la persistencia y el call-site corresponden a sucesoras. A5 requiere Javier distinto de DEV03. Rollback git revert, nunca QA/main/PROD.
