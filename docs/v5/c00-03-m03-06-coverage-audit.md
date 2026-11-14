# C00-03.M03.06 — auditoría estructurada y errores seguros para cobertura V5

**Alcance real:** servicio C# de Application `V5CoverageAuditService` que valida entradas existentes mediante `V5RequirementCoverageRules` y emite logs estructurados con `ILogger<T>`, categorías fijas, SHA causal y digest SHA-256 de ID validado. Pruebas xUnit positivas y negativas en `V5CoverageAuditServiceTests`.

**Seguridad:** no registra claves de requisitos, referencias de contratos, pruebas, evidencias, texto libre de excepción, datos de clientes ni credenciales. La API `RecordSafeFailure` limita los códigos a una allowlist; todo mensaje no reconocido se convierte en `UNCLASSIFIED_FAILURE`. Un SHA inválido deniega la validación y no se registra en claro. `ValidateBatchAndAudit` registra el conteo, no las entradas, y rechaza duplicados con el validador de dominio.

**Límites:** este servicio es ejecutable y escribe en el proveedor `ILogger` que inyecte el consumidor, pero no está conectado a un endpoint, cola ni pipeline de producción, no sustituye la BITACORA A:Z, no persiste por sí solo, no acredita envío ni recepción de eventos y no contiene tenant IDs. No concede permisos JWT/RBAC, no crea tareas ni altera la persistencia MySQL; la integración futura exige revisión adicional antes de considerar cobertura end-to-end.

**Clasificación:** requiere_dotnet=SI (C# .NET 10); requiere_node_angular=NO; requiere_mysql=NO (sin BD); requiere_docker=NO; requiere_e2e=NO; requiere_security=SI (no fuga de secretos/PII y denegación); requiere_scope_lock=SI. A0 inspección de diff, secretos, tipos y riesgos; A1 pruebas xUnit 7 escenarios; A2 pruebas negativas sobre credenciales y SHA; A3 compilación/regresión .NET 10 en jobs GitHub Actions exact-SHA; A4 readback de commits, CI pertinente, PLAN/COLA/BITACORA y rollback con git revert aditivo. A5 solo por otra Task Javier.

**No certifica** integración live, MySQL, JWT real ni journaling institucional por sí sola. Prohibida promoción a QA/main/PROD.
