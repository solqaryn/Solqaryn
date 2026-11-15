# SOLQARYN V5 — C15-05.M03.01 reglas de dominio de auditoría operativa

Microtarea: C15-05.M03.01. Alcance: implementación .NET 10 en Domain y pruebas xUnit, dev únicamente. Autor PRIMARY DEV 06, TASK_ID 6af16bbad26881918c283abe06e1f32a.

## Alcance y dependencias
El preflight institucional confirmó LISTO y A5 independiente para C15-05.M02.01–06, DAG F=VALIDADO G=SI y COLA G/O=SI antes del claim exclusivo. La transición fue registrada en PLAN H/I y BITACORA. No hay una orden para ejecutar un dispatcher, crear eventos, cambiar SQL, IAM, JWT/RBAC, tenants, APIs públicas ni desplegar QA/main/PROD.

El nuevo validador puro V5OperationalAuditDomainRules opera sobre un snapshot provisto por un llamador confiable: revisión de fuente, catálogo de IDs permitidos, estados, duplicados, owner en allowlist, SHA de publicación y prueba documental de check/A5 independiente. Sólo devuelve READY_FOR_EXTERNAL_VERIFICATION cuando todo el snapshot suministrado está completo. Nunca certifica que el snapshot sea el catálogo oficial, que los enlaces sean auténticos, que CI sea SUCCESS o que los tenants estén aislados realmente. Esos gates exigen conexiones autenticadas y pruebas de integración externas.

## Pruebas positivas y negativas
Pruebas xUnit dirigidas incluyen snapshot completo, estados PENDIENTE/VALIDANDO, IDs desconocidos y repetidos, owner no autorizado, autoaprobación o reviewer externo, SHA inválido y referencias de evidencia vacías/control chars. La evaluación no cambia estado institucional ni realiza operaciones de red.

## Seguridad, impacto y rollback
Impacto limitado a Domain/AutomationV5, tests/Domain y este documento; sin nuevas entidades EF ni migraciones. La clase no acepta autoridad desde tenant_id o cliente web, ni expone HTTP, credenciales o APIs. No cambia dependencias actuales ni reglas de aislamiento multiempresa; CI backend y seguridad siguen siendo obligatorias por ser código real. El rollback es git revert normal del commit publicado, nunca force-push.

## Criterios A0–A5
A0: diff, alcance, identificadores y pruebas revisados. A1: xUnit positivo/negativo ejecutado en CI (NO PASS hasta que termine). A2: verificación fail-closed y regresiones de seguridad en CI. A3: compilación e integración backend causales. A4: commit SHA exacto, Scope Lock, V5 CHANGELOG y checks pertinentes SUCCESS. A5: revisor de otra Task Javier. En ausencia de checks completos, conservar VALIDANDO y publicar evidencia sin afirmar LISTO_DEV.
