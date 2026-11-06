# P1-011.B — Cierre atómico de alta de administrador nuevo (DEV)

## Alcance y trazabilidad

- Plan Comercial Inicial, PLAN_MAESTRO fila 69, orden 68; dependencia explícita P1-011.E.
- Autora de esta entrega: ALEX :10, TASK_ID `6adbbc9ad9e88191bea8eb69104afb25`, GitHub `morales35alex`. Rama canónica `work/solqaryn-p1-011-b`.
- Recibos de predecesoras verificados en GitHub y PLAN_MAESTRO al preparar esta entrega:
  - P1-011.C, preparación del caso negativo, PR #3860 MERGED (`be26bdcbf69a4d0663603211eef9a94ebd8eca71`), LISTO.
  - P1-011.D, regresión MySQL de auditoría fallida, PR #3870 MERGED (`5b9549c2608a81f4302891775677029b117a9444`), LISTO.
  - P1-011.E, regresión positiva de alta empresarial, PR #3880 MERGED (`95965fb934d8490faacfbbaeeb616b21fcdeb1d5`), LISTO.
- No se modifica código productivo, endpoints, DTO, JWT, RBAC, migraciones, flags remotos, entornos ni datos reales.

## Cobertura reutilizada sin duplicar implementación

1. **Positivo ejecutable por filtro P1011B:** `P1011BAtomicAdminClosureTests.P1011B_AltaExitosaConservaBCryptRolMembresiaYAuditoria` reutiliza `P1011EPositiveAdminOnboardingRegressionTests`, que ejecuta el positivo de P1-011.A. Aserciones de usuario nuevo, contraseña BCrypt, rol empresarial, una membresía, configuración y auditoría. Se ejecuta en fixture sintético EF InMemory; no es una prueba HTTP/JWT/MySQL remota.
2. **Negativo transaccional en MySQL 8.4 efímero:** `PlataformaEmpresasMySqlTransactionIntegrationTests.AltaA_B_EsTransaccional_ConAuditoriaYRollbackEnMySqlReal` contiene inyección de fallos de ConfigEmpresa, membresía y RegistroAuditoria tras insertar usuario/empresa nuevos, y readback con ausencia de usuarios/empresas huérfanos. P1-011.D lo registra como `AltaAdminNuevoAuditoriaRollbackRealMySqlTests` con `Category=Integration`; se ejecuta en el job de integración con MySQL, no en el workflow dirigido sin MySQL.
3. **Contrato dirigido P1011B:** una segunda prueba verifica que la regresión de integración real conserva `Fact`, `Category=Integration` y el método original. Esta comprobación por reflexión **no ejecuta rollback** y no sustituye el resultado del job MySQL.
4. **BCrypt ante error:** `PlataformaEmpresasController.AltaControlada` calcula `BCrypt.HashPassword` antes de abrir la transacción y antes de `_db.Usuarios.Add`. Esto respalda ausencia de escritura si falla el hashing en ese punto, pero **no existe aquí un test de inyección de fallo BCrypt**. Requerir evidencia causal específica si la aceptación exige un fallo artificial de hashing, sin declararlo PASS por inspección.

## Gates pendientes y decisión

- No se ejecutaron pruebas locales en esta intervención; CI sobre SHA del PR y filtro `FullyQualifiedName~P1011B` deben demostrar más de cero pruebas, cero fallos.
- Exigir job MySQL `Category=Integration` exact-SHA con prueba de rollback ejecutada, Scope Lock y seguridad multiempresa SUCCESS, y comprobación de que no se pierdan controles JWT/RBAC de la API.
- Si los escenarios negativos exigidos carecen de cobertura causal completa (especialmente falla de hashing BCrypt), registrar `REQUEST_CHANGES` o `NO_GO` del alcance restante, nunca `TECH_REVIEW_PASS` ficticio.
- Para LISTO: revisión efectiva de TASK_ID distinto de ALEX :10, checks requeridos exact-SHA SUCCESS, merge protegido a dev, CI postmerge exact merge SHA y readback PLAN_MAESTRO / COLA / BITACORA. PR del desarrollo no equivale a cierre.
- Rollback: revertir únicamente este PR de pruebas y documentación mediante operación protegida si aparece una regresión. Mantener resultados y autores originales de C/D/E.

**Estado de esta acta:** cobertura preparada para validación externa; ninguna CI nueva ni certificación de P1-011.B se afirma hasta observar sus resultados.
