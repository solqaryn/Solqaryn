# SOLQARYN V5 — C01-01.M05.04: aceptación positiva ejecutable de identidad universal

**Autor:** PRIMARY DEV 04 JAVIER_CHATGPT, TASK_ID 6aeacb7820ac8191b7d768a6f33dfa09. **Alcance:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT. Fuente: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL y PLAN_MAESTRO/COLA/DAG_TECNICO. **Resultado:** tres casos xUnit positivos reales sobre el código Application/Domain existente, no una especificación futura.

## Contrato comprobable

- **P01:** `IdentidadUniversalApplicationService.ResolverActualAsync` revalida contextos válidos de un mismo usuario con empresa A/rol A y B/rol B, sin tomar `Usuario.RolId` legacy como autorización.
- **P02:** una sucursal activa de la empresa propia se consulta explícitamente mediante su ID, y el contexto retornado preserva empresa y rol.
- **P03:** varias solicitudes válidas vuelven a llamar la fuente confiable de snapshots y preservan identidad, empresa y rol, sin cachear permisos de forma implícita.

**Límite de aislamiento de prueba:** fuente de snapshots en memoria con rastreo de consultas; se ejecuta el servicio de Application y las reglas reales de Domain. Este changeset no instancia DB ni transporta JWT/HTTP ni demuestra seguridad de API, multiinstancia, integridad de MySQL o revocación en vivo. Esos negativos quedan en tests existentes y en `C01-01.M05.05`, con su propio DAG, propietario y CI. P02 no implica navegación ni autorización de sucursal en una pantalla.

## A0–A5 y rollback

- A0: verificar HEAD/Scope Lock, dependencias, owner CAS, diff y compatibilidad con `backend/tests/Solqaryn.Tests/Application/IdentidadUniversalApplicationServiceTests.cs` y `backend/src/Application/Security/IdentidadUniversalApplicationService.cs`.
- A1: CI xUnit realmente ejecutada, tres positivos con assertions de IDs/roles/consultas; ningún resultado especulativo.
- A2: rol no deriva de campo legacy, tenants A/B permanecen diferenciados; la fuente en memoria no es un boundary de producción ni reemplaza pruebas de denegación/tenancy negativas.
- A3: `dotnet test` mediante GitHub-hosted Actions dirigido a backend .NET 10, checks de seguridad pertinentes; sin EF/MySQL real ni nueva dependencia.
- A4: commit fast-forward/CAS en dev, check `verify-solqaryn-project-scope`, Build/pruebas backend, Seguridad multiempresa y controles pertinentes `completed/success` sobre SHA aplicable, con PLAN/COLA/BITACORA readback.
- A5: revisión independiente por otra Task Javier (TASK_ID distinto) antes de cualquier liberación que la exija; no autoaprobación.

**Reversión:** `git revert` ordinario del commit que añade solo pruebas y documentación, preservando otras Tasks, historial y cambios concurrentes. No modificar JWT/RBAC/tenant productivos, endpoints, MySQL, QA/main/PROD, cuentas, secretos, DNS ni proveedores.
