# Solqaryn — ERP para la operación de VariStorehn

Solqaryn es una aplicación web para administrar productos, variantes, inventario, compras, ventas, clientes, proveedores, facturación, finanzas, usuarios, roles, permisos, auditoría y reportes, con evolución planificada hacia un ERP empresarial completo.

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

- Frontend: Angular 20, standalone components, Signals, Angular Material.
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
dotnet restore InventoryApp.sln
dotnet build InventoryApp.sln
```

Frontend:

```powershell
cd frontend
npm ci
npm start
```

## Configuración

Nunca guardar secretos reales en Git. Configurar mediante variables/secret stores del entorno:

```text
ConnectionStrings__DefaultConnection
Database__ServerVersion
Jwt__Secret
Jwt__Issuer
Jwt__Audience
Cloudinary__CloudName
Cloudinary__ApiKey
Cloudinary__ApiSecret
Smtp__Host
Smtp__UsuarioSmtp
Smtp__AuthenticationMode
Smtp__OAuth2ClientId
Smtp__OAuth2RefreshToken
Smtp__OAuth2TokenEndpoint
Smtp__OAuth2Scope
SeedAdmin__Username
SeedAdmin__Password
Database__ApplyMigrationsOnStartup
```

La lista anterior refleja la configuración canónica desplegada. DEV y PROD usan Outlook.com con OAuth2/Modern Auth; no se provisionan `Smtp__PasswordSmtp` ni `Smtp__OAuth2ClientSecret` en Render. El soporte de autenticación por contraseña permanece únicamente como compatibilidad interna/pruebas y no como secreto de los entornos desplegados.

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
dotnet build InventoryApp.sln --configuration Release
dotnet test InventoryApp.sln --configuration Release
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
3. PR #2 es histórico y está cerrado/fusionado; no reabrirlo. No abrir ni fusionar un nuevo PR hacia `main` sin autorización nueva y explícita de Javier Mejía.
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
