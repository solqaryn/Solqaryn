# SOLQARYN V17 — C01-03.M03.02: preflight Application de configuración por tenant

**Repositorio:** solqaryn/Solqaryn@dev; PROJECT_SCOPE_LOCK=STRICT. **Autor:** PRIMARY DEV 02 JAVIER, TASK_ID 6af16bb9492c8191b8e8c7d606f11507. **Riesgo:** R2 (configuración, RBAC, tenant, inyección).

## Entrega real y límites

Se implementa un servicio C# .NET 10 de Application, V5DynamicConfigurationApplicationPreflight, con interfaces tipadas de lectura de identidad/membresía/RBAC y catálogo persistido de backend. Nunca interpreta tenant o grants del cliente como autoridad. La aserción de tenant del cliente se compara con el tenant obtenido desde el lector actual. El catálogo se solicita únicamente para el tenant del backend, con clave exacta; exige snapshot de propiedad, revisión positiva, fuente SHA, tipo y allowlist confirmados. Se conecta a las reglas Domain ya publicadas en C01-03.M03.01, incluida la regresión de comparación ordinal tras su A5_FAIL P2 y posterior A5 independiente.

El resultado favorable **AUTHENTICATED_DURABLE_CONFIG_ADAPTER_REQUIRED** NO otorga permisos: GrantsWrite, GrantsPlatformOverride, GrantsCrossTenantRead y CertifiesDurableCas son siempre false. No se registran los lectores en DI ni se crea endpoint, tabla, EF, migración, configuración activa, transacción ni conexión comercial real; el adaptador autenticado y la escritura CAS auditada requieren otro scope y pruebas. La configuración plataforma queda explícitamente denegada aquí hasta autoridad separada. No producción/QA/main/secretos.

## Calidad dirigida A0-A5

A0: una unidad M03.02, código Application con interfaces, xUnit y este documento; dependencia M03.01 reparada y A5 por otra Task. A1: nueve pruebas xUnit reales previstas en GitHub Actions, con positivos no autorizantes, inválidos, fronteras, versiones, inyección, errores y cancelación. A2: pertenencia A/B, revocación, grants efectivos, catálogo case-sensitive y SHA; fuentes sintéticas prueban contratos, no JWT live. A3: integración Domain→Application compilable; **NO_APLICA** MySQL 8.4, EF, HTTP y browser porque no hay implementación de persistencia ni API. A4: publicar dev con fast-forward/CAS, verificar check-runs .NET Release, seguridad/autorización, Scope Lock y V5 por SHA y readback PLAN/COLA/BITACORA. A5: revisión independiente por una de las otras cinco Tasks Javier; ninguna autoaprobación.

**Rollback:** git revert normal del commit, conservar historial. Los próximos scopes deberán implementar lectores backend reales, autorización y CAS transaccional con pruebas MySQL 8.4 aisladas, sin asumir que esta prevalidación habilita mutación.
