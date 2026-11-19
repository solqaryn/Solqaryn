# C14-02.M05.05 — aceptación negativa de seguridad

Riesgo R2; incremento de pruebas, no despliegue. Suite xUnit .NET 10 sobre clases reales Application/Domain (el lector de evidencia usa fixture inmóvil). Cubre BOLA/IDOR en siete superficies, spoofing y enumeración tenant, sucursal, host, enlaces de archivo, jobs/antirreplay, revocación, sesión, RBAC, caché, restauración denegada, traversal, SHA obsoleto, fallo de fuente y cancelación.

GitHub Actions exige .NET 10 Release, Scope Lock, TRX real con >=18 casos ejecutados y cero fallos/omitidos. No declarar PASS antes de la ejecución causal.

**Límites:** no se han probado por esta suite JWT firmado, HTTP API en TestHost, MySQL de negocio, proveedores reales, restauración ni ausencia global de defectos P0/P1. Estos gates R2 no son NO_APLICA: requieren pruebas complementarias y readback, y la unidad permanece VALIDANDO hasta A0–A4 suficientes. A5 sólo por otra Task Javier, distinta del autor y reparador. No permite QA/main/PROD, grants, secretos ni cambios productivos.

A0: pruebas/workflow/documentación/CHANGELOG sin modificaciones de backend de producción, API, DTO, migraciones, UI o infraestructura. A1/A2: regresiones adversariales reales de reglas de acceso del preflight. A3: integración HTTP/JWT/MySQL todavía no acreditada. A4: checks exact-SHA más Scope Lock/V5 y TRX por confirmar. Rollback: git revert normal del commit, sin force-push.
