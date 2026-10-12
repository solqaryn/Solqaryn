# SOLQARYN Architecture Reference

## Stack vigente

- Frontend: Angular 22.2.1, standalone components, Signals, Angular Material/CDK 22.2.1, lazy routes, auth/permission guards; Zone.js 0.16.3 y `provideZoneChangeDetection` permanecen activos.
- Backend: ASP.NET Core 8 Web API.
- Capas: Domain <- Application <- Infrastructure; API como composition root y superficie HTTP.
- Persistencia: MySQL, EF Core 8, Pomelo, migraciones versionadas.
- Seguridad: JWT Bearer, BCrypt, RBAC relacional, auditoria, CORS por lista explicita, rate limiting, security headers.
- Integraciones: Cloudinary para medios/documentos, QuestPDF para PDF, SMTP para correo.
- Browser/E2E: Playwright/Chromium.
- Infraestructura vigente documentada: Vercel, Render, Aiven y Cloudinary.

## Flujos principales

Peticion autenticada:

`Browser -> Angular Route -> Guard -> Component -> HTTP Service -> Controller -> Application Service -> Repository -> MySQL`

Persistencia:

`Application Service -> Repository/UnitOfWork -> AppDbContext -> MySQL`

## Fronteras

- Frontend controla experiencia; backend controla autorizacion real.
- Domain no debe depender de infraestructura.
- Application coordina casos de uso y contratos.
- Infrastructure implementa persistencia e integraciones concretas.
- API expone HTTP, middleware, auth, health/readiness y DI.

## Datos y transacciones

- Preferir cambios aditivos durante transiciones delicadas.
- Revisar operaciones destructivas.
- Preservar historia comercial/contable referenciada.
- Operaciones que tocan inventario, finanzas o documentos relacionados requieren consistencia transaccional y auditoria proporcional.

## Cambio arquitectonico

Tratar como arquitectonico: nueva capa/proyecto principal, cambio de framework mayor, reemplazo de persistencia, redisenio transversal de auth/RBAC, nuevo bus distribuido, gateway/BFF, microservicios, cambio fuerte de deployment/observabilidad/seguridad o nueva frontera mayor de dominio.

No tratar como arquitectonico por defecto: CRUD localizado, campo puntual, correccion UI, endpoint menor o refactor interno sin cambio de fronteras.
