# SOLQARYN V5 FINAL — C02-04.M03.01: reglas de dominio de consola global

**Autoria:** PRIMARY DEV 02 Javier, TASK_ID 6af16bb9492c8191b8e8c7d606f11507. **Repositorio:** solqaryn/Solqaryn@dev. **Alcance:** implementation real .NET 10 Domain + xUnit, sin operaciones privilegiadas efectivas.

## Contrato implementado

`V5GlobalControlPlaneDomainRules` introduce un preflight puro, que NO concede autoridad. Distingue tres intenciones de solo lectura: metadatos del propio tenant, directorio de metadatos de plataforma y lectura excepcional de metadatos con aprobación independiente. Toda salida satisfactoria dice `TRUSTED_RUNTIME_ENFORCEMENT_REQUIRED`: API/Application/Infrastructure deben comprobar identidad JWT real, grants, MFA, reautenticación sensible, pertenencia, revocación, permisos por acción, filtros de tenant y auditoría persistida. No existe ruta que realice mutaciones, exports o lecturas de PII.

La separación entre operador de plataforma y operador ERP es explícita: rol de tenant nunca acredita grant de plataforma. Para una solicitud excepcional exige origen de plataforma y grant independiente, motivo sanitizado (12–256 caracteres), destino tenant exacto, ventana temporal emitida por servidor <=15 minutos, verificación de revocación, aprobador distinto del actor y trazabilidad de auditoría. La identidad y el reloj provienen de snapshots que **sólo un adaptador servidor confiable podrá construir en el futuro**. Un caller no puede convertir flags en un permiso; este dominio no incorpora adapters ni endpoints.

## Pruebas reales y negativas

`V5GlobalControlPlaneDomainRulesTests` cubre: preflight propio sin autorización, ausencia JWT/rol/MFA/reauth, tenant A contra B, alteración por cliente, mutaciones y datos sensibles rechazados, rol ERP sin acceso plataforma, grant excepcional revocado o de tenant ajeno, motivo insuficiente, expiración/futuro/duración excesiva, autoaprobación y auditoría ausente. El resultado de xUnit y CI se registra separadamente cuando GitHub Actions termine en `completed/success`; documentación no equivale a prueba ejecutada.

## Impacto, dependencias y rollback

Implementación localizada en `backend/src/Domain/AutomationV5/` y xUnit de Domain. No cambia API/DTO/DI/EF/MySQL, autenticación live, Angular, migraciones, secretos, servicios externos, colas, webhooks, eventos ni QA/main/PROD. No desactiva controles ni agrega privilegios. Predecesores `C02-04.M02.01–06` están LISTO con A5 independiente; DAG F=VALIDADO/G=SI y COLA G/O=SI se verificaron antes del claim.

A0: API, dependencias e invariantes de dominio; A1: xUnit positiva y negativa; A2: tenant A/B, MFA, rechazo de privilegio cliente, dual-control y revocación; A3: build/pruebas .NET 10 y regresión de seguridad GitHub-hosted; A4: diff/SHA fast-forward, Scope Lock, procedencia CHANGELOG V5 y checks backend/seguridad/autorización aplicables **sobre SHA exacto**; ningún queued/cancelled/skipped/failed es PASS. Al publicarse pasa VALIDANDO antes de acreditar A1–A4. A5 solo otro TASK_ID Javier independiente tras LISTO DEV. Reversión mediante `git revert` normal preservando otros autores.
