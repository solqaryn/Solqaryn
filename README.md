# SOLQARYN — plataforma empresarial multiempresa

SOLQARYN es una plataforma empresarial multiempresa para administrar productos, variantes, inventario, compras, ventas, clientes, proveedores, facturación, finanzas, usuarios, roles, permisos, auditoría y reportes, con evolución planificada hacia un ERP empresarial completo.

La factura actual se considera comprobante comercial interno mientras no exista habilitación fiscal SAR/CAI aplicable.

## Memoria canónica del proyecto

Para evitar reanalizar el repositorio en cada sesión, el equipo debe usar:

1. `AGENTS.md` — reglas obligatorias.
2. `PROJECT_CONTEXT.md` — contexto técnico principal.
3. `TASKS.md` — pendientes.
4. `PROJECT_INDEX.md` — mapa del repositorio.
5. `ARCHITECTURE.md` — arquitectura/patrones.
6. `CHANGELOG_AI.md` — bitácora colaborativa.

No volver a recorrer todo el repositorio ni releer archivos documentados si no cambiaron.

## Stack

- Frontend: Angular 22.2.1, standalone components, Signals, Angular Material/CDK 22.2.1; Zone.js 0.16.3 permanece activo y no se ha migrado a zoneless.
- Backend: ASP.NET Core 8 Web API.
- Capas backend: Domain, Application, Infrastructure, API.
- Datos: MySQL + EF Core 8/Pomelo.
- Seguridad: JWT, BCrypt, RBAC relacional.
- Medios: Cloudinary.
- PDF: QuestPDF.
- Correo: SMTP.
- E2E: Playwright/Chromium.
- Infraestructura: Vercel, Render, Aiven y Cloudinary.

## Funcionalidad principal

- productos y variantes;
- color, talla, marca y modelo;
- categorías;
- inventario y movimientos;
- compras/proveedores;
- ventas/clientes;
- facturación/pagos;
- descuentos, impuestos y costos de envío;
- finanzas;
- usuarios, roles y permisos;
- auditoría;
- cargas masivas y reportes administrativos;
- perfil/configuración visual/empresa.

## Arquitectura

Backend:

```text
Domain <- Application <- Infrastructure
                    ^
                    |
                   API (composition root / HTTP)
```

Flujo típico:

```text
Angular -> API -> Application Service -> Repository -> EF Core -> MySQL
```

Ver `ARCHITECTURE.md` para detalles y `PROJECT_INDEX.md` para localizar componentes.

## Preparación local

El acceso local y la participación de agentes se rigen por `AGENTS.md`: Javier Mejía conserva la decisión final; Codex participa solo por orden explícita y AntiG/Antigravity permanece `RESERVED_INACTIVE`. ChatGPT/VAEP y Chat B operan como controladores/QA remotos sobre `dev` bajo el MAESTRO.

```powershell
git fetch origin
git switch dev
git pull --rebase origin dev
```

Backend:

```powershell
cd backend
dotnet restore Solqaryn.sln
dotnet build Solqaryn.sln
```

Frontend:

```powershell
cd frontend
npm ci
npm start
```

## Configuración

Nunca guardar secretos reales en Git. Render DEV y PROD comparten exactamente el mismo contrato de 28 variables; solo cambian los valores que son propios de cada entorno.

```text
ASPNETCORE_ENVIRONMENT
AllowedHosts
Database__ServerVersion
Database__ApplyMigrationsOnStartup
ConnectionStrings__DefaultConnection
Jwt__Secret
Jwt__Issuer
Jwt__Audience
Jwt__ExpiraMinutos
Security__LoginRateLimitPerMinute
Cloudinary__CloudName
Cloudinary__ApiKey
Cloudinary__ApiSecret
Cloudinary__EnvironmentPrefix
Cors__AllowedOrigins__0
AppSettings__BackendPublicUrl
AppSettings__EnlacePublicoFacturaHorasValidez
AppSettings__EnlacePublicoFacturaMaximoAccesos
AppSettings__CorreoFacturaIdempotenciaMinutos
Smtp__Host
Smtp__Port
Smtp__UsuarioSmtp
Smtp__OAuth2ClientId
Smtp__OAuth2RefreshToken
Smtp__NombreRemitente
Smtp__TimeoutSeconds
Smtp__MaxAttempts
Smtp__RetryBaseDelayMilliseconds
```

SMTP desplegado usa exclusivamente Outlook.com + OAuth2/Modern Auth. No forman parte del contrato de Render `Smtp__PasswordSmtp`, `Smtp__OAuth2ClientSecret`, `Smtp__AuthenticationMode`, `Smtp__OAuth2TokenEndpoint`, `Smtp__OAuth2Scope`, `Smtp__UsarSsl`, `Smtp__RequiereAutenticacion`, `Smtp__CorreoRemitente` ni `Smtp__CorreoRespuesta`. STARTTLS, autenticación OAuth2, endpoint, scope, remitente y Reply-To se resuelven mediante invariantes/defaults seguros del backend.

Tampoco se despliegan `Swagger__Enabled`, `AppSettings__LogoPublicUrl`, `SeedAdmin__Username` ni `SeedAdmin__Password`: sus valores desplegados eran redundantes o de bootstrap y no deben permanecer como variables de servicios ya provisionados.

## Migraciones

- versionadas con EF Core;
- revisar `Up()` y operaciones destructivas;
- preferir transición aditiva/expand-and-contract;
- no aplicar migraciones en Producción sin autorización expresa, respaldo y validación;
- no ejecutar simultáneamente dos mecanismos de aplicación de la misma migración.

## Validación

La validación es proporcional al cambio, según `AGENTS.md`.

Comandos globales disponibles cuando el alcance lo justifique:

```powershell
cd backend
dotnet build Solqaryn.sln --configuration Release
dotnet test Solqaryn.sln --configuration Release
```

```powershell
cd frontend
npm ci
npm run build:prod
```

```powershell
cd frontend
npx playwright test --config=playwright.config.ts
```

No mantener números fijos de pruebas como estado permanente en este README; consultar CI/commit vigente.

## Estructura

```text
backend/
  src/
    Domain/
    Application/
    Infrastructure/
    API/
  tests/
frontend/
  e2e/
  src/app/
    core/
    features/
    services/
docs/
.github/workflows/
scripts/
```

## Flujo de publicación

1. Trabajar **únicamente en `dev`**.
2. No crear ramas adicionales sin autorización expresa.
3. No abrir ni fusionar un PR hacia `main` sin autorización nueva y explícita del propietario.
4. Ejecutar validación proporcional y CI cuando aplique.
5. No tocar Producción.
6. Fusionar a `main` únicamente cuando Javier Mejía lo autorice expresamente.

## Colaboración eficiente

- fuente base: `PROJECT_CONTEXT.md`;
- no reindexar todo el repo por cada solicitud;
- no releer archivos sin cambios;
- tras reconexión, recuperar estado con `PROJECT_CONTEXT.md`, `TASKS.md` y Git;
- analizar archivo objetivo + dependencias directas;
- detener la exploración cuando el objetivo esté cumplido.

Reglas completas: `AGENTS.md` y `docs/COLABORACION_IA.md`.
