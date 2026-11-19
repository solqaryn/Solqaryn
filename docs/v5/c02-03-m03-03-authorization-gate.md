# C02-03.M03.03 — autorización tenant y denegación por defecto (DEV)

**Fecha:** 2026-11-18. **Rama:** dev. **Scope Lock:** STRICT. **Riesgo:** R2.

## A0 — Alcance y arquitectura

Se agrega una puerta Application de lectura `C0203TenantLifecycleAuthorizationGate` dependiente de `IC0203VerifiedOperatorEvidenceReader`. El consumidor entrega únicamente tenant objetivo y operación, sin JWT, identidad de actor, grants ni DSN de procedencia cliente. El lector de confianza es una interfaz para un futuro proveedor **servidor** que deberá verificar firma JWT en middleware, rol global actual, grant explícito, revocación, sesión/membresía fresca, tenant dueño y política/fence actuales. Deniega con un código uniforme si cualquiera falta. Una validación positiva habilita únicamente continuar al preflight Application; jamás se ejecuta mutación, autorización de grant, CAS, restauración, borrado o despliegue.

**Límite crítico:** NO se registra implementación viva de `IC0203VerifiedOperatorEvidenceReader` en DI. Los flags del snapshot son afirmaciones que un adaptador real deberá obtener de fuentes verificadas, no JWT válidos por sí mismos. La clase no expone endpoint HTTP ni altera EF/MySQL, migraciones o datos. No confundir pruebas sintéticas con aislamiento real A/B en API o con autorización productiva. Este incremento NO completa aún toda la aceptación de C02-03.M03.03.

## A1/A2 — Pruebas negativas

xUnit en GitHub-hosted comprueba el preflight positivo no autorizante, cliente que intenta autodeclarar permisos/actor/JWT/DSN/cutover, tenant B, grant revocado, JWT no verificado, rol faltante, política obsoleta, ownership no verificado, lectura sin fence, fuente nula o fallida, error saneado, cancelación, caracteres Unicode invisibles e IDs al límite. No se emite tenant, JWT o datos privados en respuestas de error.

## A3/A4 — Integración, CI y revisión

La integración HTTP JWT firmado con RBAC persistente, revocación real y consultas EF/MySQL multi-tenant A/B no está implementada en este diff y por tanto **NO_APLICA a las pruebas del módulo inerte**; sigue **PENDIENTE para la aceptación integral** de la microtarea. El workflow dirigido compila `.NET 10` Release y ejecuta suite xUnit exacta; Scope Lock, V5, backend y seguridad deben constar SUCCESS en el SHA causal. QUEUED/IN_PROGRESS/FAILED/SKIPPED/CANCELLED nunca PASS. Mantener VALIDANDO mientras falten pruebas reales de integración y A5 independiente de Task Javier distinta.

## Rollback

`git revert` normal del commit exclusivo de este scope tras validar HEAD, con readback y sin borrar BITACORA o modificar DAG/cola. Nunca QA/main/PROD.
