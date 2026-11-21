# C06-01.M05.03 — Integración REAL de proveedor/persistencia (V23, DEV)

## Requisito A0 y límites

PLAN_MAESTRO fila 892, acción D «Ejecutar integración de persistencia o proveedor cuando corresponda». Se implementa **una prueba de integración real** para comprobar que ASP.NET Core 10 puede iniciarse con el modelo y proveedor Oracle EF Core 10, sobre un contenedor **MySQL 8.4 efímero de GitHub-hosted**, con evidencia de persistencia SQL, transacción real y rollback completo. La ruta pública de landing no crea ni escribe empresas ni transfiere datos privados.

**Sin cambios de runtime, esquema persistente, clientes Angular, secretos reales ni configuración productiva**. No usar Aiven real, QA/main/PROD ni recursos con costo. Toda contraseña y empresa de prueba existe exclusivamente en el entorno desechable del runner; no se comparte con datos reales.

## A1–A3: comprobaciones causales

1. Verificar servidor MySQL real versión 8.4 y estructura real Oracle EF10 bootstrap, al menos 139 tablas y cero migraciones pendientes en fixture.
2. En una única sesión de SQL ejecutar transacción sobre \`Empresas\`: insertar Id 906010503, confirmar 1 fila dentro de transacción, efectuar \`ROLLBACK\`, confirmar 0 filas; desde otra conexión confirmar de nuevo 0. Esto es persistencia y rollback real, no memoria ni mock.
3. Arrancar API \`Solqaryn.API.csproj\` Release 10 conectada al MySQL efímero; comprobar endpoints públicos \`GET /health\` y \`GET /health/ready\` con 200, sin exponer connection strings o secretos.
4. Compilar .NET10 Release y ejecutar \`scripts/verify-project-scope.mjs\`, \`scripts/verify-solqaryn-skills.mjs --self-test\`, \`scripts/quality/verify-v5-changelog-provenance.mjs\` exact SHA. Registrar que \`SKIPPED/CANCELLED/FAIL/IN_PROGRESS\` no equivalen a PASS.

## Condicional V23 y seguridad

Capa y rol: solamente un script de integración y workflow GitHub-hosted + este documento. **SKREF-03,04,05,06,10,11,12 NO_APLICA** porque no se cambia Angular/UI, motion, copy de producto, modelo formal de amenazas ni perfilado .NET; governance local canónica se aplica. No importar librerías/skills.

**Alcance negativo:** esta prueba es A3 real de proveedor, pero no certifica autorización JWT/RBAC firmada, políticas tenant A/B, BOLA/IDOR, checkout, pagos, APIs privadas ni UX visual de la fase M05. La validación visual/teclado queda en las microtareas M04/M05 de frontend. Cualquier P0/P1 en otro scope persiste. Si la prueba falla, mantener VALIDANDO y reparar causa real antes de pasar a LISTO; A5 requiere revisión independiente por otra Task Javier distinta del autor DEV06. Rollback Git: \`git revert\` estándar; la base MySQL efímera se destruye después del job.

## Continuidad

CI dirigido \`.github/workflows/v23-c06-01-m05-03-provider.yml\` para la misma SHA publicada de \`dev\`, con evidencia de logs y de SQL rollback. No certificar un \`STATUS=in_progress\`. CHANGELOG_AI.md grande no se sobreescribe si GitHub no devuelve íntegro su contenido; el registro de alcance queda en este documento y BITACORA.
