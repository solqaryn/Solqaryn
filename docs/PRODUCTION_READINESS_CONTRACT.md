# Contrato de preparación para Producción — Solqaryn

Autoridad vigente: `docs/PROJECT_SCOPE_LOCK.md`, `AGENTS.md` y autorizaciones expresas del propietario. No depende de un plan histórico.

## Propósito

Este documento define lo que debe demostrarse antes de afirmar que el ERP está preparado para Producción. No autoriza deploy, no contiene secretos y no sustituye pruebas técnicas del producto ni autorización de promoción. Un health check verde o un build exitoso por sí solos **no** significan Production Ready.

## Configuración requerida por entorno

Los secretos deben llegar exclusivamente desde el gestor de secretos/variables del entorno. En Git sólo se conservan placeholders. Como mínimo deben suministrarse y validarse fuera del repositorio:

- `ConnectionStrings__DefaultConnection`.
- `Jwt__Secret`, `Jwt__Issuer`, `Jwt__Audience` y expiración aprobada.
- `Cors__AllowedOrigins` con orígenes exactos de cada entorno.
- `Cloudinary__CloudName`, `Cloudinary__ApiKey`, `Cloudinary__ApiSecret` cuando storage externo esté habilitado.
- `Smtp__Host`, credenciales/remitente y política TLS cuando correo real esté habilitado.
- `AppSettings__BackendPublicUrl` y URLs públicas autorizadas.
- parámetros de rate limiting y cualquier feature flag autorizada.

Nunca se debe promover `CHANGE_ME`, credenciales E2E, localhost ni secretos de ejemplo como configuración productiva.

## Gates técnicos obligatorios antes de Producción

1. Build Release backend y build production frontend sobre el SHA candidato exacto.
2. Unit/integration/contract/E2E/security/migration/performance gates aplicables en verde.
3. Migraciones verificadas en staging comparable y backup restaurable probado.
4. `/health` y `/health/ready` distinguen proceso vivo de dependencia DB disponible.
5. HTTPS/HSTS, CORS exacto, JWT, RBAC y protección por recurso certificados.
6. Correlation ID y logging estructurado presentes sin datos sensibles.
7. Métricas, tracing, alertas y dashboard operativo externos certificados conforme T9/N8.12.
8. Staging comparable a Producción certificado conforme N8.13.
9. Rollback de aplicación/configuración/DB ensayado cuando corresponda.
10. RPO/RTO, responsables, runbooks y comunicación de incidente definidos.
11. P0/P1 abiertos = 0 para el release candidate.
12. Autorización humana explícita para cualquier operación de Producción.

## Estado observado 2026-09-10

Ya existen correlation ID, manejo global de excepciones, security headers, HSTS fuera de Development, HTTPS redirect y health/readiness. El repositorio conserva placeholders para JWT/DB/Cloudinary/SMTP y no debe reemplazarlos por secretos reales.

**No existe todavía un exporter externo certificado de métricas/tracing/alertas (OpenTelemetry, Application Insights, Sentry o equivalente) y N8.12/T9 continúa siendo requisito futuro. Tampoco N8.13 staging ni N9 release están cerrados.**

Por tanto, el dictamen actual es:

`PRODUCTION_READY = FALSE`

`PRODUCTION_READINESS_CONTRACT = DEFINED_AND_ENFORCEABLE`

Esto es deliberadamente fail-closed: preparación y observabilidad parcial no deben convertirse en una certificación productiva antes de N8/N9.
