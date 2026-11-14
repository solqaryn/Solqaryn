# C00-01.M02.04 — Configuración tipada, ámbitos y feature flags seguros

Repositorio `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. Autor: `JAVIER_CHATGPT`, `PRIMARY DEV 05`, TASK_ID `6adc2e55f47c81919f6374e561687076`. Fuente única V5 FINAL: Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA` y seguimiento Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`.

## Alcance y evidencia

Esta microtarea **define** los contratos observables para configuración, tipos, precedencia y feature flags existentes; no crea un sistema de flags, no cambia `appsettings`, infraestructura, secretos, endpoint, roles, datos, despliegue ni comportamiento de ejecución. La fuente original en GitHub fue comprobada sobre `dev` el 13 de noviembre de 2026:

- `backend/src/API/Security/CommercialTenantUiCapabilities.cs`: Git blob `6805c75c939cc488dd2f392180584d6b8f175986`, utiliza `IConfiguration.GetValue<bool>` para pilotos de UI y `dto.EsAdministrador` más grants para rutas administrativas. La UI **no autoriza** backend.
- `backend/src/API/Configuration/RenderEnvironmentContractGuard.cs`: Git blob `2c38ebb90a8360b61c9e05df96ff3a9abd5b8f65`, revisa nombre de ambiente y lista cerrada de 28 claves administradas, ausencia, extras y valores; reporta fingerprint sin incluir secretos.
- `backend/src/API/Configuration/EnvironmentDatabaseGuard.cs`: Git blob `896aa003876f2b4299dfe5d4f1d69b83b5d1be22`, liga `Development`/`Staging`/`Production` con servidores, DB y usuarios de cada ámbito, exige TLS.
- `backend/src/API/appsettings.json`: Git blob `276a8267f521db65f6fcbea79efa6a108c4a936c`, muestra tipos JSON para `Jwt`, `Security`, `Database`, `Cors` y `Observability`; los valores de ejemplo NO son secretos operativos.
- `frontend/src/environments/environment.ts`: Git blob `fcc3144df39328605e7d7b561d4024b930fdfd59`, muestra `production` booleano y `apiUrl` string; no concede permisos de API.

## Contratos de aceptación propuestos (NO_CIERTOS_HASTA_EJECUCION_REAL)

| ID | Regla de configuración documentada | Prueba positiva futura | Prueba negativa futura | Evidencia requerida | Estado |
| --- | --- | --- | --- | --- | --- |
| CFG01 | Cada opción se registra con tipo (`boolean`, `int`, `string` o lista) y origen permitido | Flag booleano false no habilita ruta piloto | String inesperado o valor fuera de rango se rechaza, no se convierte en grant | Test .NET con `IConfiguration` y opciones validadas | DEFINIDO_NO_CERTIFICADO |
| CFG02 | `MultiEmpresa:PrivateReadPilotEnabled` habilita solo rutas de lectura listadas | `true` con empresa múltiple muestra lectura prevista | `false` o empresa simple no anuncia lectura piloto | Test .NET del proyector de capacidades | DEFINIDO_NO_CERTIFICADO |
| CFG03 | `PrivateWritePilotEnabled` y `PrivateMasterEditPilotEnabled` no habilitan roles implícitos | Grant real y flag adecuado muestran solo acción autorizada | Administrador sin grant y usuario sin rol no ven edición administrativa | Test .NET con permisos reales y casos negativos | DEFINIDO_NO_CERTIFICADO |
| CFG04 | `PrivateDraftPilotEnabled` no activa confirmación, reportes, pagos ni exportaciones | Solo borradores habilitados se anuncian | Pago o confirmación inferidos por bandera de borrador quedan denegados | Test de matriz UI/API y RBAC | DEFINIDO_NO_CERTIFICADO |
| CFG05 | `RenderEnvironmentContractGuard` conserva allowlist exacta | 28 claves válidas en ambiente aislado se aceptan | Clave extra, faltante, vacía o ambiente desconocido produce fail closed | Test .NET sin valores secretos reales | DEFINIDO_NO_CERTIFICADO |
| CFG06 | `EnvironmentDatabaseGuard` liga ambiente con la base correcta y TLS | Binding de Development válido sobre fixture de test | Identidad DB de Production usada en Development se rechaza | Test .NET aislado sin conectarse a BD real | DEFINIDO_NO_CERTIFICADO |
| CFG07 | Config frontend es presentación, jamás token ni autoridad tenant | Angular usa `apiUrl` tipado sin modificar tenant | `empresaId`, slug, URL o bandera de navegador no dan acceso cross-tenant | Prueba Angular + HTTP JWT/RBAC A/B autenticada | DEFINIDO_NO_CERTIFICADO |
| CFG08 | Rollback y trazabilidad no alteran secretos, flags reales ni producción | Cambio documental revertible con commit aditivo | A5_PASS inventado o CI SKIPPED no permiten QA/main/PROD | A0–A4 SHA exacto, BITACORA y A5 independiente | DEFINIDO_NO_CERTIFICADO |

## Seguridad, límites y cierre

Se exige **fail closed** ante configuración desconocida o inválida: ninguna bandera de UI sustituye JWT, BCrypt, RBAC, membresía o aislamiento tenant A/B del backend. No almacenar PII, credenciales, `Jwt:Secret` ni tokens en Git, informes o BITACORA; los proveedores siguen siendo los actuales hasta autorización separada. La matriz es **definición**, no implementación de `IOptions<T>` ni certificación de .NET/HTTP/MySQL/Angular/browser. Un test documental Node 22 solo valida integridad del contrato y negativos sintéticos: **no prueba servicio live ni tenancy real**.

`PLAN_MAESTRO!H` gobierna el estado; `COLA!E/G/O` y `P:Q` derivan de fórmulas, `BITACORA` es apendicular. Antes de publicar: estado `VALIDANDO`, A0 calidad estática, A1 positivo, A2 negativos de seguridad, A3 prueba documental; commit directo fast-forward a `dev` sin PR/ramas. A4 exige diff, pruebas post-commit, CI real de SHA exacto, readback PLAN/COLA/BITACORA. Sólo A0–A4 PASS permiten LISTO_DEV con `A5_PENDIENTE` de otro TASK_ID. `A5_PASS` independiente es condición necesaria, no autorización suficiente, para QA/main/PROD. Rollback con `git revert <SHA_PUBLICADO>` aditivo, nunca force-push ni reset destructivo. Inventario de imágenes históricas cero: migración `NO_APLICA`.
