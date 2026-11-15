# SOLQARYN V5 — C06-04.M03.02: servicio Application de prevalidación de portal cliente

**Plan:** STANDARD V5 FINAL Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, PLAN fila 987, DAG fila 948. **Autor:** Task PRIMARY DEV 02 `6af16bb9492c8191b8e8c7d606f11507`, ámbito `solqaryn/Solqaryn@dev`, Scope Lock STRICT.

## Implementación real, límites

Se incorpora `C0604PortalClienteApplicationService` y la interfaz `IC0604PortalVerifiedSnapshotSource` en Application, reutilizando `C0604PortalClienteDomainRules` M03.01 de Domain. `PreflightAsync` es asíncrono y evalúa identidad/tenant, cuenta CUSTOMER, sesión activa, cliente y recurso persistidos, pertenencia, permisos, revocación y versión optimista. La fuente de confianza tiene que obtener los hechos desde backend autenticado: ningún cliente puede autorizarse con `ClientTenantHint`. Al fallar los controles, retorna un código seguro sin datos de negocio.

La clase no crea ruta HTTP, no registra servicios en DI, no emite JWT, no carga un perfil, no ejecuta pedidos, cobros, reservas, webhooks ni workers, no usa MySQL ni modifica entidad. Incluso una validación estructural favorable sólo devuelve `AUTHENTICATED_CUSTOMER_ADAPTER_REQUIRED`, todos los flags de autorización están fijados a false. La revisión posterior del adaptador debe demostrar autenticación JWT, vinculación de hash de sesión, membresía real y version CAS atómico.

## Denegaciones

- Request no permitido / ID path hostil / versión <=0: `REQUEST_INVALID` antes de leer fuente.
- Contexto servidor incompleto o tiempo no UTC: `TRUSTED_BACKEND_SNAPSHOT_REQUIRED`.
- JWT, token de sesión, membresía o permiso no acreditados o revocados: `AUTHENTICATION_OR_CUSTOMER_PERMISSION_REQUIRED`.
- Tenant hint del cliente distinto de tenant servidor: `CLIENT_TENANT_OVERRIDE_DENIED`.
- Cuenta/cliente/sesión o recurso del tenant B/cliente A2, NULL propietario, expiración o versión stale: `CUSTOMER_TENANT_SESSION_OR_VERSION_DENIED`.
- Contexto coherente: `AUTHENTICATED_CUSTOMER_ADAPTER_REQUIRED` que NO concede acceso.

Se agregan diez casos xUnit programados, con rutas positivas y negativas de tenant A/B y clientes A1/A2, membresía, JWT y sesión, versión y cancelación. Escribir pruebas no es evidencia de que se ejecutaron; GitHub Actions .NET+seguridad exact SHA es obligatorio para LISTO.

## A0–A5 y rollback

A0: validar skill, Scope Lock, HEAD, DAG/COLA, propietario y scope real. A1: tests positivos/negativos de servicio y Domain. A2: separación de tenant/cliente, revocación JWT, sesión y permiso; no declarar certificada autenticación HTTP sin adaptador. A3: Application usa Domain existente, no integración MySQL/Angular, por lo que esos checks no aplican a este diff; backend/seguridad sí aplican. A4: build xUnit, seguridad, autorización, Scope Lock y V5 exact SHA `completed/success`, readback PLAN/COLA/DAG/BITACORA. Si CI sigue corriendo, preservar VALIDANDO y owner. A5 otra Task Javier revisa la entrega tras LISTO; nunca autovalidación. Rollback `git revert` aditivo, sin modificar QA/main/PROD ni datos reales.
