# Fase 8 — Modernización NuGet .NET 10

Fecha: 2026-10-07  
Repositorio: `solqaryn/Solqaryn`  
Rama objetivo: `dev`  
PR inicial de trabajo: #3555; PR de integración: #3556; reconciliación post-merge: #3557  
HEAD inicial congelado: `cb75bae58770662567a0fcf57f04ebdd7786dfdc`  
Baseline NuGet causal: run `37610431956` — SUCCESS sobre `e1442d39a2271ed411d0cb428c68d93ac47f46ba`.

## Baseline antes de cambiar paquetes

- FluentValidation 11.9.2 — outdated.
- FluentValidation.AspNetCore 11.3.0 — deprecated/Legacy; REMOVED.
- BCrypt.Net-Next 4.0.3 — outdated.
- Swashbuckle.AspNetCore 6.6.2 — outdated.
- CloudinaryDotNet 1.25.1 — outdated.
- ClosedXML 0.104.2 — outdated.
- SixLabors.ImageSharp 3.1.12 — sin vulnerabilidad conocida en las fuentes del gate; existe 4.1.2.
- System.IdentityModel.Tokens.Jwt 8.23.0 — ya alineado.
- Microsoft.IdentityModel.Protocols/OpenIdConnect 8.19.2 — transitivos; se alinean a 8.23.0.
- Microsoft.AspNetCore.Http.Features 5.0.17 — no existe en el árbol runtime actual; `FormOptions` proviene del shared framework.
- Microsoft.Extensions.Caching.Memory — 10.0.12 transitivo/shared framework; sin PackageReference productivo directo.
- System.Text.Json — shared framework; sin PackageReference productivo directo.
- Vulnerabilidades NuGet conocidas en baseline: 0.

## Matriz de decisión

| Familia | Antes | Después | Decisión |
|---|---|---|---|
| FluentValidation | 11.9.2 | 12.1.1 | UPDATED |
| FluentValidation.AspNetCore | 11.3.0 | ausente | REMOVED |
| FluentValidation DI | 11.5.1 transitive | 12.1.1 directo API | UPDATED |
| BCrypt.Net-Next | 4.0.3 | 4.2.0 | UPDATED |
| Swashbuckle.AspNetCore | 6.6.2 | 10.2.3 | UPDATED |
| CloudinaryDotNet | 1.25.1 | 1.29.3 | UPDATED |
| ClosedXML | 0.104.2 | 0.105.1 | UPDATED |
| System.IdentityModel.Tokens.Jwt | 8.23.0 | 8.23.0 | NO_CHANGE_ALREADY_ALIGNED |
| IdentityModel Protocols/OpenIdConnect | 8.19.2 transitive | 8.23.0 | UPDATED / ALIGNED |
| SixLabors.ImageSharp | 3.1.12 | 3.1.12 | NO_CHANGE_LICENSE_BOUNDARY |
| Microsoft.AspNetCore.Http.Features | no PackageReference | no PackageReference | NOT_APPLICABLE / ABSENT |
| Caching.Memory | 10.0.12 framework/transitive | 10.0.12 framework/transitive | NO_CHANGE_ALREADY_ALIGNED |
| System.Text.Json | .NET 10 shared framework | .NET 10 shared framework | NO_CHANGE_ALREADY_ALIGNED |

## FluentValidation 12

Se elimina `FluentValidation.AspNetCore` y `AddFluentValidationAutoValidation()`. La API registra validadores con `FluentValidation.DependencyInjectionExtensions 12.1.1` y ejecuta validación mediante `FluentValidationActionFilter`, un `IAsyncActionFilter` que resuelve `IValidator<T>`, usa `ValidateAsync` con `RequestAborted` y delega la respuesta inválida al `InvalidModelStateResponseFactory` configurado por ASP.NET Core.

El gate previo al cierre registró 2,395/2,395 unitarias y 82/82 pruebas causales verdes antes del ajuste final OpenAPI/IdentityModel. El gate final vuelve a ejecutar la suite completa y las pruebas dirigidas.

## Swashbuckle 10 / multipart

La major 10 detectó un contrato no soportado: `[FromForm]` aplicado directamente a `IFormFile` en `CargasMasivasController.Validar`. Se retira únicamente ese atributo redundante; ASP.NET Core conserva binding de `IFormFile` desde `multipart/form-data` por convención. El gate exige generación real de `/swagger/v1/swagger.json`, Swagger UI, Bearer y schema multipart con `archivo` binario.

## ImageSharp

ImageSharp 4 requiere una licencia Six Labors válida en build para dependencias directas. Fase 8 no introduce secretos, licencias ni obligaciones comerciales sin autorización. Se mantiene 3.1.12 y se certifican las pruebas de seguridad de imágenes existentes; el gate NuGet exige cero vulnerabilidades conocidas.

## Seguridad, datos y arquitectura

- EF Core 10.0.12 y Oracle `MySql.EntityFrameworkCore` 10.0.9 no cambian.
- No se modifican migraciones ni schema.
- JWT issuer/audience/lifetime/signing/ClockSkew permanecen fail-closed.
- BCrypt no hace rehash masivo; se valida compatibilidad de hashes existentes.
- Cloudinary no cambia credenciales ni entornos.
- Caching mantiene partición tenant-aware.
- JSON no cambia wire format deliberadamente.
- `main`, QA y PROD no se modifican.
- Fase 9 no se inicia.

## Cierre

La autoridad final es `.github/workflows/modernization-phase8-nuget.yml` sobre el HEAD exacto. `Dictamen Fase 8` debe emitir `FASE_8_NUGET_MODERNIZATION=PASS`, las versiones seleccionadas y P0/P1. Fases 6 y 7 son required checks de `dev` y deben permanecer PASS en PR y post-merge.

## Integración y reconciliación post-merge

- PR de integración: #3556.
- Merge inicial de Fase 8 en `dev`: `b1344393318ed14591e3c3e3f78c47cce9e9fd1b`.
- En el post-merge, Fase 8 volvió a ejecutar sobre ese HEAD y terminó `SUCCESS`.
- El run `37650207722` del gate `DEV - recuperación de migración MySQL parcial` falló por una deuda de CI previa: todavía instalaba `dotnet-ef` globalmente y trataba de reproducir migraciones Pomelo históricas que Fase 7 ya retiró de la cadena activa.
- La reconciliación se realiza en PR #3557 sin reintroducir Pomelo: tool manifest EF 10.0.12, `SslMode=Disabled`, bootstrap fresh Oracle, recuperación del marcador `20261006111818_OracleBaseline`, `has-pending-model-changes=0`, health/readiness e idempotencia de segundo arranque.
- No se modifican entidades, migraciones activas, schema, datos, QA, `main` ni PROD.

