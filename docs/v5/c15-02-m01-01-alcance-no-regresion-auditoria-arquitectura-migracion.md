# SOLQARYN V5 FINAL — C15-02.M01.01: alcance y no-regresión de arquitectura/migración

**Fecha:** 2026-11-14. **Autor:** PRIMARY DEV 06 JAVIER (`6af16bbad26881918c283abe06e1f32a`). **Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Clase:** contrato documental de preparación M01, no auditoría integral, certificación ni migración ejecutada.

**Fuentes:** [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, `docs/AUTOMATION_STATUS.md`, `docs/AUTOMATION_AUTHORITY.md`, `PROJECT_CONTEXT.md` y `.agents/skills/solqaryn-project-governance/references/architecture.md`. Preflight institucional: PLAN 2198 H=PENDIENTE/I vacío, COLA G/O=SI, DAG 2159 F=VALIDADO/G=SI antes del reclamo. Es autorización para preparar alcance documental, no para cutover.

## 1. Objetivo y evidencias aún exigibles

Preparar el contrato de la futura auditoría C15-02 de arquitectura y migración: identificar componentes, interfaces, dependencias, límites de confianza, planes de reversión, riesgos y criterios de prueba antes de evaluar Hostinger, Cloudflare, R2, MySQL, DNS y proveedores. El resultado de lote requiere **diagrama, contratos, rollback y secretos no expuestos**. Estos elementos deben comprobarse con fuente y versión, pruebas positivas/negativas y contexto de entorno; redactar M01.01 no los certifica. No se ha ejecutado una migración, restauración, cambio DNS/TLS, inspección de cuentas externas, inventario real de objetos ni certificación integral de P0/P1.

## 2. Topología declarada frente al objetivo no implementado

| Área | Situación declarada en arquitectura vigente | Objetivo V5 futuro | Gate antes de una operación real |
| --- | --- | --- | --- |
| Frontend | Angular 22.2.1, Signals, Material/CDK, Zone.js 0.16.3; Vercel documentado | Frontend independiente y capa CDN compatible con Cloudflare | Build/artefacto exacto, rutas, accesibilidad, headers, rollback |
| Backend | ASP.NET Core 10, Domain ← Application ← Infrastructure; API; Render documentado | API aislada y observable | JWT/RBAC, CORS, rate limiting, health/readiness, secretos y smoke |
| Persistencia | MySQL, EF Core 10.0.12, MySql.EntityFrameworkCore 10.0.9; Aiven documentado | MySQL privado en topología objetivo | Backup, checksum/conteos, restore en entorno efímero, plan de rollback |
| Archivos | Cloudinary documentado, inventario de activos no acreditado aquí | R2 futuro con prefijo tenant y URLs firmadas | Inventario real (cero es admisible), ownership, TTL y fallback |
| Dominios | Host, DNS y TLS actuales sin auditoría operativa en esta unidad | Cloudflare DNS/TLS y dominios tenant | Inventario y autorización explícita, simulacro reversible |
| Jobs | Estado de workers/colas a constatar en código | Workers segregados e idempotentes | Identidad/tenant, retries, deduplicación y pruebas negativas |

La ruta conceptual existente es navegador → Angular → API ASP.NET Core → Application → Infrastructure/EF → MySQL. El guard UI no confiere autoridad: el backend comprueba identidad, RBAC y pertenencia a tenant/sucursal. Un objetivo documental no demuestra que R2 o Hostinger estén configurados o en servicio.

## 3. Dieciséis invariantes de no-regresión

| ID | Condición obligatoria | Negativo que la incumple |
| --- | --- | --- |
| NR01 | Distinguir topología desplegada de arquitectura objetivo | Marcar proveedor futuro como activo por figurar en el plan |
| NR02 | Respetar versiones de Angular 22.2.1 y net10.0 del código vigente | Cambiar framework sin alcance ni pruebas |
| NR03 | Conservar Domain y Application independientes de infraestructura | Acoplar dominio a proveedor de hosting o R2 |
| NR04 | JWT, RBAC y ownership se validan en el backend | Guard de Angular usado como única autorización |
| NR05 | Tenant/sucursal proceden de contexto confiable y pertenencia comprobada | Host, header, slug o body permite suplantar tenant |
| NR06 | Operaciones de tenant A no exponen recursos de B | IDOR, BOLA o exportación cruzada |
| NR07 | Migraciones EF/MySQL preservan historia y restricciones | DDL/transferencia destructiva sin reversión comprobable |
| NR08 | Backup y restore requieren ensayo y resultados medidos | Confundir la existencia de respaldo con restore íntegro |
| NR09 | Inventario de archivos refleja objetos reales, incluso cero | Crear una migración de imágenes inexistentes |
| NR10 | URLs firmadas futuras restringen dueño, TTL y acceso | Descarga de archivo de B o caducado desde A |
| NR11 | DNS/TLS/host-binding y CORS mantienen identidad y reversibilidad | Cutover rompe certificado o mezcla dominios tenant |
| NR12 | Jobs reintentan con contexto tenant e idempotencia | Replay duplica efectos sensibles o cruza empresas |
| NR13 | Secretos, PII y tokens se excluyen de Git, logs y documentos | Evidencia expone credenciales o datos privados |
| NR14 | CI causal corresponde al commit publicado exacto | queued/skipped/cancelled/failed u otro SHA se trata PASS |
| NR15 | A5 requiere TASK_ID distinto entre seis Javier | Autorrevisión o revisor de cuenta retirada |
| NR16 | DEV no implica autorización de QA, main ni PROD | Documento provoca despliegue o corte DNS sin permiso |

## 4. Ocho pares de aceptación futura — NO EJECUTADOS

| Caso | Positivo propuesto | Negativo bloqueante |
| --- | --- | --- |
| ARQ01 | Diagrama con fuentes y versiones exactas | Servicio objetivo dibujado como activo sin verificación |
| ARQ02 | Frontend/API responden con contratos y host válidos | Cambio de host rompe sesión o selecciona tenant ajeno |
| ARQ03 | Prueba A/B/JWT/RBAC rechaza acceso cruzado | El guard UI encubre HTTP 200 no autorizado |
| ARQ04 | Conteos, checksums, restore y rollback efímero concuerdan | Backup no restaurable se declara listo |
| ARQ05 | Inventario real de archivos con ownership y TTL | Archivo ajeno o inexistente se declara migrado |
| ARQ06 | Jobs preservan idempotencia y contexto tenant | Replay duplica operación o cambia tenant |
| ARQ07 | DNS/TLS/host-binding prueban ruta reversible autorizada | Cutover de dominio real sin rollback ni permiso |
| ARQ08 | Checks completed/success del SHA exacto y A5 independiente | Status pendiente/cancelado o autoaprobación se usan como PASS |

**ARQ01–ARQ08: FUTUROS_NO_EJECUTADOS.** No se realizaron pruebas HTTP, browser, MySQL, Docker, backup, restore, Cloudflare/R2 ni de infraestructura. Los riesgos no investigados son `NO_ACREDITADO`, no `FALLO_VERIFICADO`. La auditoría completa debe producir pruebas y observaciones reales con SHA, actor, tenant, entorno, resultado esperado/observado, evidencia redactada y autorización separada.

## 5. Exclusiones, fases siguientes y reversión

M01.02 deberá mapear actores, inputs/outputs y fronteras; M01.03 auditar código y evidencia existentes; M01.04 clasificar riesgos, supuestos y decisiones; M01.05 definir criterios observables; M01.06 conservar gate terminal con A5 independiente y prerrequisitos reales. Ninguna M01 documental libera M03 runtime, IAM/JWT/RBAC, migraciones, QA/main/PROD, compras, webhooks o eventos. Quedan fuera de este cambio API, DTO, Angular, DB, CI, proveedores, cuentas, secretos, DNS y datos reales.

**Impacto:** un documento `docs/v5` y entrada aditiva `CHANGELOG_AI.md`. **A0:** consistencia de fuentes, límites, NR y pares ARQ. **A1:** coherencia documental y casos positivos/negativos definidos, sin atribuir ejecución funcional. **A2:** no modificar JWT/RBAC/tenant ni exponer secretos. **A3:** cotejo conceptual contra fuentes; builds runtime `NO_APLICA` al diff Markdown. **A4:** fast-forward/CAS sobre HEAD actual, diff limitado, Scope Lock/V5 provenance `completed/success` exact-SHA y readbacks PLAN/COLA/BITACORA. **A5:** pendiente de otra Task Javier; no QA/PROD. **Rollback:** revert normal tras revalidar HEAD, nunca force-push ni reescritura de BITACORA.
