# P0-011.B — Certificación negativa de la frontera comercial inicial (DEV)

**Microtarea:** P0-011.B. **Task propietaria:** `6adbbc9ad9e88191bea8eb69104afb25` (`SECONDARY_REVIEW_01`, `ALEX_CHATGPT`). **Cuenta GitHub:** `morales35alex`. **Rama exclusiva:** `work/solqaryn-p0-011-b`.

**Fuente comercial única:** `STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES`, Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, versión V3, frontera inicial (§2) y ficha P0-011. Autoridad técnica `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, contrato 3+3 y `docs/AUTOMATION_STATUS.md` del HEAD DEV. El Plan Maestro histórico es **visión no ejecutable**.

## Dictamen delimitado

**PASS de clasificación documental condicionado a prueba y CI; NO certificación de funcionalidades ni despliegues.** P0-011.A estableció la matriz C01–C20 en `docs/P0-011-A-FRONTERA-VERSION-INICIAL-DEV.md` y fue cerrada después de merge protegido del PR #3760. Esta microtarea B comprueba separadamente la negativa solicitada: **una mención al Maestro NO habilita ejecutar Experience Studio, agentes IA, prestaciones enterprise ni otras capacidades FUTURO**. No se instala, enciende, compra, despliega ni programa ninguna de ellas.

| ID | Frontera que debe permanecer | Resultado requerido |
| --- | --- | --- |
| C14 | Dedicated/shared placement dinámico, migración automatizada y cells | FUTURO; denegar inclusión inicial |
| C15 | Marketplace logístico, repartidores y fulfillment avanzado | FUTURO; denegar inclusión inicial |
| C16 | Experience Studio, builders y personalización arbitraria | FUTURO; denegar inclusión inicial |
| C17 | Suscripción self-service, metering, billing y upgrades | FUTURO; denegar inclusión inicial |
| C18 | SCIM/SAML completos, agentes IA y API Economy/SDK | FUTURO; denegar inclusión inicial |
| C19 | Industry packs, DMS/GRC/EHS/ESG, PPM/PLM y fabricación | FUTURO; denegar inclusión inicial |
| C20 | Compras y upgrades pagados automáticos | FUTURO; presupuesto cero compras |
| C11–C13 | Subdominios, dominios/DNS/TLS y promoción QA/PROD | CONDICIONAL; no activarlos desde DEV |
| C07–C08 | Contratación manual y seguridad JWT/RBAC/auditoría | INICIAL; no diferir controles vigentes |

**No equivalencia:** que una palabra aparezca en frontend/ERP existente, en una tabla o en un plan archivado NO demuestra que la función esté activa ni autoriza recrearla. Funciones ERP ya existentes se preservan; no se borran por su nombre. El alcance de este changeset es **solo acta, script Node de solo lectura, workflow de validación y trazabilidad**; no modifica Angular, .NET, MySQL, contratos API, autenticación, aislamiento empresarial ni integraciones.

## Negativos automatizados y limites de evidencia

Ejecutar `node scripts/verify-p0-011-b-future-exclusions.mjs --self-test` en un checkout del SHA del PR. El script exige 20 IDs únicos, las siete filas C14–C20 FUTURO, C11–C13 CONDICIONAL y C07/C08 INICIAL. Falla ante reclasificar Experience Studio, IA, enterprise, billing o bases dedicadas como inicial, activar dominios por clasificación, diferir seguridad, duplicar u omitir filas, falsificar su título o retirar referencias. Las mutaciones negativas son **sintéticas**; nunca tocan secretos ni recursos externos.

Esta comprobación demuestra **consistencia e intencionalidad del límite documental**, no ejecución real de un controlador de autorización, ausencia absoluta de código futuro, accesibilidad UI, E2E, recuperación MySQL, Render/Vercel READY ni aislamiento de tenant en producción. Una aprobación de este PR **no** certifica dichas capacidades y no sustituye las pruebas de negocio de cada microtarea inicial.

## Seguridad, prueba, consumidores y reversión

- **Capas afectadas:** documentación DEV, un validador Node estándar y workflow GitHub Actions de solo lectura. Sin migraciones, DTO/API, persistencia, secretos, DNS ni cambios a entornos ajenos.
- **Consumidores:** selector comercial por microtarea P0/P1, responsables de priorización y auditoría; una futura modificación de C14–C20 exige un cambio de autoridad independiente y revisión de seguridad.
- **Regresión:** 20/20 filas y 12 negativos; CI exacto Project Scope Lock, Seguridad multiempresa y Build backend; revisión independiente del SHA final.
- **Cierre:** PR hacia `dev`, aprobación GitHub de login distinto del autor, merge protegido verificado, SHA integración igual al HEAD observado y readback de `PLAN_MAESTRO!H23:M23`, `COLA!E23` calculada y `BITACORA`. Antes de eso: `EN_PROGRESO`/`VALIDANDO`, nunca `LISTO`.
- **Rollback:** revert del changeset por PR protegido, preservando la matriz A y los módulos ERP existentes. No se elimina ni reinterpreta ningún dato.

**Estado real de despliegue y funciones empresariales:** NO_VERIFICADO, fuera del alcance de esta certificación negativa.