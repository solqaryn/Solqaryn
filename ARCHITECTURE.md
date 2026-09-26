# ARCHITECTURE — Solqaryn

## 1. Estilo arquitectónico

Solqaryn usa una arquitectura backend por capas, cercana a Clean Architecture pragmática:

`Domain <- Application <- Infrastructure`

`API` compone las dependencias y expone la aplicación por HTTP. Infrastructure implementa contratos definidos hacia Application/Domain. La dirección conceptual debe mantener las reglas de negocio independientes de detalles externos siempre que sea razonable.

El frontend Angular se organiza por funcionalidades con servicios compartidos y controles transversales en `core`.

## 2. Componentes principales

### Frontend Angular

Responsabilidades:

- navegación y UX;
- formularios y validación de presentación;
- guards de sesión/permisos;
- consumo de API;
- representación de módulos ERP;
- lazy loading de componentes.

### ASP.NET Core API

Responsabilidades:

- endpoints REST;
- autenticación/autorización;
- validación;
- coordinación de casos de uso;
- middleware de errores/seguridad;
- rate limiting;
- health/readiness;
- composición DI.

### Application

Responsabilidades:

- casos de uso;
- servicios de aplicación;
- DTO y contratos;
- validadores;
- coordinación de reglas funcionales.

### Domain

Responsabilidades:

- entidades y conceptos del negocio;
- enums aún vigentes/transitorios;
- invariantes que no dependen de infraestructura.

### Infrastructure

Responsabilidades:

- EF Core/MySQL;
- repositorios;
- configuraciones de entidades;
- migraciones;
- Cloudinary;
- SMTP;
- QuestPDF;
- integraciones concretas.

## 3. Flujos

### Petición autenticada

`Browser -> Angular Route -> authGuard/permisoGuard -> Component -> Service HTTP -> Controller -> Application Service -> Repository -> MySQL`

La ocultación de botones/rutas en frontend es UX, no frontera de seguridad. El backend debe rechazar operaciones no autorizadas.

### Persistencia

`Application Service -> Repository/UnitOfWork -> AppDbContext -> MySQL`

Cuando una operación modifica inventario/finanzas/documentos relacionados, debe preservarse consistencia transaccional y trazabilidad.

### Archivos y documentos

- imágenes/documentos: adaptadores Cloudinary;
- factura PDF: QuestPDF;
- correo: SMTP; PROD autentica Outlook.com mediante OAuth2/Modern Auth y mantiene secretos/tokens fuera del repositorio;
- enlaces públicos de factura: token seguro, expiración/revocación según implementación vigente.

## 4. Patrones vigentes

- Dependency Injection.
- Repository.
- Unit of Work donde aplica.
- DTO/Service layer.
- FluentValidation.
- Soft-delete.
- Auditoría transversal.
- RBAC relacional.
- Lazy loading de rutas Angular.
- Expand-and-contract para migraciones legacy delicadas.

## 5. Seguridad

- JWT Bearer con issuer/audience/secret configurables.
- BCrypt para contraseñas.
- CORS por lista explícita.
- Rate limiting de login.
- Security headers.
- Separación estricta de PROD/DEV.
- Secretos fuera del repositorio.
- SMTP OAuth2 en PROD usa access tokens efímeros obtenidos desde refresh token; no usa contraseña SMTP básica.
- `main` congelada durante el trabajo en `dev`.

RBAC debe basarse en relaciones persistentes y permisos explícitos, evitando bypasses implícitos por banderas administrativas.

## 6. Datos

Persistencia principal: MySQL mediante EF Core/Pomelo.

Topología operacional vigente en Aiven:

- proyecto `solqaryn`;
- un único servicio MySQL Free `solqaryn-mysql`;
- base `solqaryn_dev` con usuario `solqaryn_dev_user` para DEV;
- base `solqaryn_prod` con usuario `solqaryn_prod_user` para PROD;
- `avnadmin` reservado para administración;
- aislamiento DEV/PROD lógico por base, usuario y GitHub Environment; ambos comparten el mismo servicio físico Aiven.

Reglas:

- migraciones versionadas;
- no ejecutar migraciones productivas sin autorización;
- revisar operaciones destructivas;
- preferir migraciones aditivas durante transiciones;
- conservar historial cuando exista impacto contable/comercial;
- evitar eliminar físicamente catálogos referenciados por documentos históricos.

## 7. Fronteras del ERP

Orden rector:

`N0 saneamiento -> N1 inventario -> N2 compras -> N3 ventas -> N4 tesorería/CxC/CxP/contabilidad -> N5 BI -> N6 multiempresa -> N7 integraciones -> N8 production readiness -> N9 go-live/hypercare`

Los transversales T0–T12 aplican durante todo el roadmap.

## 8. Principios de cambio

1. Cambios pequeños antes que refactors globales.
2. No duplicar conceptos existentes.
3. No introducir una segunda vía de autorización, cálculo o persistencia sin razón arquitectónica documentada.
4. Preservar compatibilidad durante migraciones legacy.
5. Toda nueva dependencia transversal debe justificarse.
6. Toda modificación estructural debe actualizar `PROJECT_CONTEXT.md`, `PROJECT_INDEX.md`, este archivo y `ARCHITECTURE_CHANGELOG.md`.

## 9. Qué se considera cambio arquitectónico importante

Requiere renovar el mapa arquitectónico una vez si ocurre, por ejemplo:

- nuevo proyecto/capa principal;
- cambio de framework mayor con impacto real;
- reemplazo de EF Core/MySQL;
- rediseño de RBAC/autenticación;
- multiempresa con cambio de tenancy transversal;
- event bus/mensajería distribuida;
- nuevo gateway/BFF;
- partición en microservicios;
- incorporación de un módulo ERP mayor que introduzca nuevas fronteras de dominio;
- cambio fuerte de despliegue, observabilidad o seguridad transversal.

No requieren reescaneo completo: correcciones de UI, CRUD, validaciones puntuales, nuevos campos localizados, pequeños endpoints o refactors internos sin cambio de fronteras.
