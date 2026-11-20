# C02-03.M03.03 — evidencia adversarial de grant global duplicado (V22)

- **Riesgo:** R2, frontera de autoridad global, JWT y aislamiento tenant.
- **Acción de PLAN D376:** implementar autorización, tenancy y denegación por defecto. Este incremento refuerza pruebas reales del lector de autoridad, pero **no cierra** la autorización de mutaciones de ciclo de vida.
- **Cambio:** ampliar la prueba existente `C0203M0303RealPlatformReaderMysql84IntegrationTests` para insertar en MySQL 8.4 efímero una segunda asignación global activa para el mismo actor y rol, hacer dos peticiones HTTP con JWT firmado y exigir 403 en tenants A/B. Al quitar la asignación duplicada, el mismo JWT vuelve a obtener 200 en A. No se manipulan tablas productivas ni configuración de permisos.
- **A0:** únicamente test de integración, esta evidencia y `CHANGELOG_AI.md`; sin cambios de API, Domain, Application, Infrastructure, migraciones o secreto.
- **A1/A2:** positivo 200 antes/después, negativo 403 con grant duplicado en A y B; las aserciones son falsables por defecto de `PlataformaScopeService`.
- **A3:** GitHub-hosted, servicio MySQL 8.4 aislado, Oracle EF, JWT firmado y TestServer HTTP del lector real; exigir ejecutar prueba y no contar mocks como integración.
- **A4 pendiente hasta CI exact-SHA:** `C02-03.M03.03 - real signed JWT HTTP MySQL 8.4`, Backend Release, seguridad multiempresa, `verify-solqaryn-project-scope`, V5. `IN_PROGRESS`, `FAILURE`, `SKIPPED` requeridos nunca PASS.
- **Límite de fase:** no se implementa todavía CAS duradero, política de revisión atomizada de alta/suspensión/reactivación/baja lógica ni mutación transaccional multiempresa. No aprobar `VALIDANDO -> LISTO` ni A5 hasta A0-A4 completos. A5 posterior por Task Javier distinta del autor histórico DEV05 y del reparador DEV06.
- **Rollback:** revertir normalmente el commit de dev; el test elimina el grant duplicado y borra su esquema efímero en `finally`. Sin QA/main/PROD, proveedores reales ni pagos.
