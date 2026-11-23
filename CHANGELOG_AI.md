

## 2026-11-22 — C03-03.M03.03: ensayo MySQL 8.4 físico de revocación (DEV05)
- Se añade xUnit `Category=Integration` sobre Oracle EF/MySQL 8.4 efímero: JWT firmado en contexto de prueba, consulta relacional A/B, grant de auditoría sólo lectura, denegación de recurso cruzado, revocación y rollback con esquema desechable y limpieza. No cambia runtime ni otorga permisos.
- El ensayo se publica en `dev` con Scope Lock STRICT y V17 exclusivo; GitHub-hosted CI exact-SHA pendiente. EF InMemory anterior no se contabiliza como MySQL; tampoco se certifica HTTP real, DI call-site ni sink durable. Conservar C03-03.M03.03 VALIDANDO con owner histórico DEV03; A5 requiere otra Task distinta de DEV03, DEV06 y DEV05. No QA/main/PROD.
