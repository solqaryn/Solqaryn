# P0-009.B — Negativa CI/Vercel sin ocultamiento de fallos de cuota (DEV)

PROJECT_SCOPE_LOCK=STRICT
MICROTAREA=P0-009.B
ESTADO=PRUEBA_NEGATIVA
VERIFICACION_PROD=NO_REALIZADA

**Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V3, P0-009, L1/L14. **Task:** SECONDARY_REVIEW_01 / ALEX_CHATGPT / 6adbbc9ad9e88191bea8eb69104afb25, GitHub morales35alex. **Base:** dev@9a558516be56980d0673654bf96e6bf1e537ea00. Dependencias P0-009.A y P0-012.B en LISTO (COLA readback).

## Evidencia reutilizada con sus límites

P0-009.A, PR #3755, formalmente revisado y fusionado en dev@934940ec02a8c03f8493c63a82278974373aeea9, registró CI exact-SHA y una batería pura de 1 testigo más 8 casos negativos de checks, paridad y aprobaciones. Las cifras 48/48 y 21/21 del Plan son referencias históricas NO_REVALIDADAS en HEAD vivo. No convertir CI verde en READY Render/Vercel ni en certificación de PROD.

El caso negativo exige **no ocultar un fallo histórico reportado de Vercel PROD por límite de compilaciones**. Esta Task no consulta ni modifica QA/main/PROD, proveedores Vercel, secretos, endpoints ni infraestructura. Prueba exclusivamente un clasificador con datos sintéticos y la fuente documental vigente P0-009.A. No existe readback nativo PROD.

## Prueba negativa y nueve comprobaciones

Con DEV CI verde y un registro artificial etiquetado INSTITUTIONAL_PROD con state=failure y reason=api-deployments-free-per-day, el clasificador conserva PROD_QUOTA_FAILURE_RECORDED_HISTORICAL; jamás lo convierte en READY. Un reporte sin fecha o referencia conserva PROD_QUOTA_FAILURE_REPORTED_UNVERIFIED. Este hecho es de naturaleza ficticia/retrospectiva, no implica un nuevo fallo observado.

Los nueve casos negativos comprueban: ausencia de reporte no es éxito; CI y testigos sintéticos no verifican proveedor; el target production de un proyecto DEV no es PROD institucional; DEV READY con SHA anterior no prueba paridad; un éxito PROD no verificado no acredita READY; falla de cuota con metadatos incompletos se conserva; una falla distinta también se conserva; CI sin check obligatorio no pasa; y ningún conjunto de fixtures declara overallCommercialReady=true.

**Reproducción:** node scripts/verify-p0-009-b-ci-status-boundary.mjs --self-test sobre checkout auténtico. El validador anterior de P0-009.A también se autoejecuta de manera sintética al importar en modo self-test. El nuevo script ejecuta 1 testigo y 9 negativas sin leer red ni credenciales. Se aporta workflow pull_request de contents:read; su éxito solo cuenta tras job real de GitHub Actions sobre SHA exacto.

**Límite obligatorio:** preservar un reporte histórico o simulado no certifica el estado actual de PROD; esta Task no realizó lecturas ni cambios PROD. No hay despliegues, compras, escalado de proveedor, E2E, RBAC, aislamiento multiempresa en tiempo real ni readiness comercial. El resultado es una certificación de lógica negativa, no una corrección de cuota.

**Impacto:** documento, script Node y workflow aislado, más CHANGELOG_AI.md. Sin cambios runtime Angular/.NET/DB ni secretos. Rollback por revert protegido; no force-push. LISTO requiere CI 3/3 exact-HEAD, job Node nativo, aprobación GitHub independiente, merge protegido dev y readback PLAN_MAESTRO/COLA/BITACORA.
