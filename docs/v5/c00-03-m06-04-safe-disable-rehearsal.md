# C00-03.M06.04 — Ensayo no destructivo de desactivación de cobertura V5

**Alcance real:** prueba dirigida xUnit del componente existente `V5CoverageMemoryAdapter` (`backend/src/Application/AutomationV5/V5CoverageMemoryAdapter.cs`), sin cambiar servicios productivos, JWT, RBAC, dependencias, esquema, proveedor de base de datos o automatizaciones. Se usa la **allowlist entregada al adaptador** como gate revocable: rechazar escrituras/lecturas al retirar permisos y comprobar que la evidencia previamente almacenada permanece intacta al restaurarlos. Esta técnica simula una desactivación local; **no** pausa Tasks, revierte commits, revierte Google Sheets ni implementa kill switch distribuido.

## Implementación y pruebas causales

`backend/tests/Solqaryn.Tests/Application/V5C0003M0604CoverageSafeDeactivationTests.cs` contiene ocho casos xUnit (dos variantes Theory y seis Facts) con el adaptador **real** en memoria:

1. Rechazar escritura cuando se revoca actor, conservando el registro previamente autorizado.
2. Rechazar escritura cuando se cierra el scope, sin borrar la evidencia.
3. Ocultar lectura a actor retirado y recuperar exactamente el mismo registro al restaurar la allowlist.
4. Ocultar lectura con scope cerrado y restaurar misma revisión/entrada después.
5. Impedir que una revisión antigua sobrescriba evidencia válida tras reapertura.
6. Rechazar reutilización de clave idempotente con contenido distinto.
7. Rechazar escritura por capacidad agotada, preservando snapshot previo.
8. Mantener aislamiento de namespace: scope alterno autorizado no ve el registro original.

No se usan mocks para sustituir el proveedor bajo prueba; las entradas son fixtures controlados. No hay operación destructiva ni persistencia live.

## Clasificación y limitaciones

- `requiere_dotnet=SI` porque se agregan tests C#/.NET 10 y se exige build/test xUnit **real** de GitHub Actions del SHA publicado.
- `requiere_security=SI` por denegación de actor/scope, idempotencia y separación de namespace; requiere suite de seguridad pertinente y gate de autorización cuando sea obligatorio para la rama.
- `requiere_scope_lock=SI` y verificador de procedencia CHANGELOG en el mismo SHA.
- `requiere_node_angular=NO_APLICA`, `requiere_mysql=NO_APLICA`, `requiere_docker=NO_APLICA`, `requiere_e2e=NO_APLICA`: diff de pruebas .NET, sin cambios Angular, migrations, EF, servicios MySQL o UI.
- **NO ACREDITADO:** recuperación tras reinicio, preservación de datos en MySQL/Google Sheets, CAS/lease interproceso, rollback productivo, autenticación JWT/RBAC live, aislamiento real de empresas A/B, workers o despliegue.
- Las Tasks Javier autorizadas mantienen su configuración y el gobierno V5. No se crea scheduler, webhook o dispatcher. Este ensayo no libera por sí solo `C00-03.M06.06` ni QA/main/PROD.

## Auditorías y reversión

A0 inspeccionar diff de tests, secreto accidental, dependencias y CHANGELOG autocontenido; A1 ejecutar ocho casos positivos/negativos del proveedor real; A2 verificar denegaciones y separación de scope sin sobrecertificar tenancy; A3 build/test .NET 10 y seguridad proporcionales en CI exact-SHA; A4 revalidar commit directo fast-forward, blobs, check-runs requeridos y readback PLAN/COLA/BITACORA. En ausencia de CI pertinente SUCCESS, mantener VALIDANDO. A5_PENDIENTE de **otra** Task Javier entre seis autorizadas. Rollback del changeset mediante `git revert` ordinario con HEAD revalidado, sin force-push ni borrar evidencia institucional.
