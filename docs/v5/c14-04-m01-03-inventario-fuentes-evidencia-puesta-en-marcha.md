# SOLQARYN V5 FINAL — C14-04.M01.03: inventario de fuentes y brechas de puesta en marcha

**Fecha:** 2026-11-14. **Autor:** PRIMARY DEV 03 Javier (`6adc2e4bd8a48191ac7d8641ed95a1a3`). **Rama:** `solqaryn/Solqaryn@dev`. **Scope Lock:** `STRICT`. **Clase:** inventario documental M01.03, sin activación ni ejecución runtime.

**Única fuente ejecutable:** [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). Comprobar antes de reclamar: PLAN fila 2128 PENDIENTE y sin owner; COLA G/O SI; DAG F VALIDADO/G SI. Antecedentes [M01.01](./c14-04-m01-01-alcance-puesta-en-marcha-soporte.md) y [M01.02](./c14-04-m01-02-actores-interfaces-puesta-en-marcha.md).

## 1. Método de inventario y límites

La fuente de evidencia es el **árbol de GitHub de un HEAD exacto** y sus blobs de rutas ya existentes, no la mera presencia de una mención en un plan. Los siguientes 19 blobs se contrastaron con el árbol Git vigente al preparar este documento, SHA base `03e3d24e1455adcc9c8f38b4239447fd56007dfd`, sin leer ni exponer secretos. La ruta, contenido versionado y checksum indican **existencia del artefacto de código**, no disponibilidad, seguridad ni configuración de cuentas remotas.

Las integraciones actuales documentadas son Vercel/Render/Aiven/Cloudinary; Hostinger/Cloudflare/R2 representan una dirección futura que requiere autorización separada. El código runtime no se modifica. No se han consultado datos productivos, proveedores de pago, DNS ni hosts del cliente. Se prohíbe inferir backend healthy, restore probado o cumplimiento SLO.

## 2. Inventario de 19 fuentes reales, paths y SHA de blob

| Ref | Archivo verificado en árbol de Git | Blob SHA exacto | Utilidad en C14-04 |
| --- | --- | --- | --- |
| S01 | `AGENTS.md` | `063d342891ec6e0b23b9adb195ef3b358b51548d` | Allowlist y autorizaciones de Tasks solo dev |
| S02 | `docs/PROJECT_SCOPE_LOCK.md` | `7dabef6946d75e0381f121a70458240ac702a30f` | Alcance STRICT, identidad y separación de entornos |
| S03 | `docs/AUTOMATION_STATUS.md` | `4b25c0e88e8fb593cbafb5f8836db42c7d703fca` | Estado operativo V5 y trazabilidad de ejecuciones |
| S04 | `docs/BACKUP_RESTORE_DESARROLLO_RUNBOOK.md` | `81e8b11557912fffcd741ff0f34faf69e0ed6f47` | Procedimiento DEV y variables por nombre sin valores sensibles |
| S05 | `.github/workflows/project-scope-lock.yml` | `dff65460c57d16bd38aaac67c138837f41c9c266` | Workflow de alcance y artefacto exact-SHA |
| S06 | `.github/workflows/ci.yml` | `c02c3f00c3dccb91e79bfd2b5befe2fb5b517263` | CI sin despliegue automático ni migración real Aiven |
| S07 | `.github/workflows/backup-restore-backend-smoke.yml` | `9ae22277208c78a13f6d7f81b8be740845a057fd` | Smoke de restore aislado condicional a disparador especial |
| S08 | `.github/workflows/backup-restore-dev.yml` | `cee9cdd1e22d006c21d9a23cc40ead6223d7295c` | Workflow DEV de backup/restore no invocado por esta tarea |
| S09 | `.github/workflows/environment-infra-parity.yml` | `de4c17e5e3ec81c442fd2bcd4d9a670f4433682d` | Definición de cotejo de entornos; no prueba paridad real |
| S10 | `frontend/api/seo.js` | `df98e7dd7b44ebb8f9bf928e56ca3d396bcf4f6e` | Superficie SEO de tienda pública |
| S11 | `frontend/api/robots.js` | `7ae514cfa9d5d645caba899dc10d2ccf0a9b34f9` | Política de robots de frontend |
| S12 | `frontend/api/sitemap.js` | `ee50880520c906178a7ebd31553f3ab6d28f6b2d` | Superficie sitemap pública |
| S13 | `frontend/server/seo-utils.js` | `b0886e30cc85efa6f800e558d5284412d8c573fc` | hostFromRequest, binding público y fetch con timeout |
| S14 | `frontend/package.json` | `e6d030f643421cb343cc12617e742469e88223c4` | Versión Angular 22.2.1 y scripts tipado/Playwright |
| S15 | `backend/src/API/Program.cs` | `01425cff56eb13e12022b1b81a2f322d17fd1fff` | API, seguridad, configuración y contexto de runtime |
| S16 | `backend/src/API/Solqaryn.API.csproj` | `2a7b33a0974d1ebe13676593d84b458b1243e037` | Target net10.0 y referencias de backend |
| S17 | `backend/src/Infrastructure/Solqaryn.Infrastructure.csproj` | `6210d8b12abd77171d95d17e2292c1a319b89263` | EF Core 10, proveedor MySQL Oracle |
| S18 | `docs/v5/c14-04-m01-01-alcance-puesta-en-marcha-soporte.md` | `d01f52b4c00ed8e3862169fcf0ecff4da97e6e36` | Contrato de alcance y 16 invariantes de este lote |
| S19 | `docs/v5/c14-04-m01-02-actores-interfaces-puesta-en-marcha.md` | `bfa649c8d77fe56840f9c2f6b6e8f67edc1e7bdd` | Mapa de actores, interfaces y límites de confianza |

**Lectura causal limitada:** `frontend/server/seo-utils.js` contiene `hostFromRequest` a partir de `x-forwarded-host` o `host`, `isIndexableHost` compara hostname con `publicOrigin` configurado y `fetchJson` aplica timeout. Esto identifica un **vector para pruebas negativas de suplantación**, no una vulnerabilidad certificada. El runbook de backup y los workflows indican **procedimientos posibles**, no que restore o paridad hayan ocurrido. `Program.cs` muestra superficie de inicialización de seguridad; no se probó el servicio HTTP en este changeset.

## 3. Doce brechas que requieren evidencia adicional, no hallazgos de fallo

| Ref | Aspecto sin acreditación por esta unidad | Evidencia real pendiente de una etapa autorizada |
| --- | --- | --- |
| G01 | Disponibilidad del sitio, HTTPS y DNS públicos | No se capturó smoke real ni estado DNS/TLS; requiere autorización y prueba del ambiente concreto |
| G02 | Host spoofing, CORS y resolución de tenant | El código de SEO/host permite ubicar frontera, pero no acredita tests adversariales HTTP tenant A/B |
| G03 | R2/Hostinger/Cloudflare operativos | Topología futura no desplegada ni contratada por este changeset; no inferir acceso ni cuota |
| G04 | Cloudinary y políticas ACL en producción | No se hicieron lecturas de cuentas ni objetos; el inventario no acredita ownership efectivo |
| G05 | Backups funcionales y RPO/RTO | Runbooks/workflows existen, sin prueba de snapshot y restore aislado ejecutada en este SHA |
| G06 | Paridad configuración DEV/QA/PROD | Workflow presente no verifica por sí mismo paridad ni permiso de despliegue |
| G07 | Rutas backend, health y readiness en ambiente real | Program.cs evidencia diseño, no prueba HTTP/5xx ni conectividad a servicio |
| G08 | Trazas, alertas, SLO/SLI y correlación | No se revisaron métricas reales ni operación de monitorización |
| G09 | Incident commander, guardia y tiempos de escalación | No se acreditó rotación on-call, canal de contacto ni simulacro de incidente |
| G10 | Rollback/restore ensayado y consistencia de negocio | Sin ejercicio de revert de despliegue, DB o archivo; no usar reset ni datos reales |
| G11 | Playwright en UX/teclado/responsive y error | Package scripts son referencias, no ejecución de browser por esta tarea |
| G12 | Seguridad JWT/RBAC/tenant y secreto en pipelines | CI de este Markdown no es prueba causal de aislamiento y no habilita QA/main/PROD |

**Estado de G01–G12:** `NO_ACREDITADO_EN_ESTA_MICROTAREA`, no “FALLO_VERIFICADO”. No crear tickets de seguridad falsos ni inventar alertas/P0/P1. La ausencia de evidencia en una lectura acotada tampoco demuestra inexistencia global.

## 4. Ocho pares de pruebas futuras, no ejecutadas

| Caso | Resultado positivo a acreditar | Negativo bloqueante |
| --- | --- | --- |
| P01/N01 | Identificar archivo, versión SHA y digest de servicio en entorno autorizado | Servicio responde versión distinta o no declarada |
| P02/N02 | Verificar host permitido y tenant correcto por HTTP real | Host manipulado filtra tenant o configuración |
| P03/N03 | JWT/RBAC permite sólo objeto propio A | A logra leer recurso B |
| P04/N04 | Inventario de medios con owner/ACL verificado | URL ajena/caducada obtiene archivo |
| P05/N05 | Snapshot sintético recuperado con integridad | Restore destructivo, sin reconciliación o no ejecutado |
| P06/N06 | Alertas y health correlacionan 5xx/job/SHA | Incidente sin señal, dueño o escalación |
| P07/N07 | Playwright verifica loading/error/teclado responsive | Spec sin corrida o regresión se declara PASS |
| P08/N08 | A5 independiente y autorización adicional por entorno | LISTO_DEV/CI implica deploy sin permiso |

**Todos los P01–P08 y N01–N08 están `FUTUROS_NO_EJECUTADOS`.** Un script Playwright presente no equivale a navegador corrido; un workflow de MySQL no certifica restauración ni una descripción de JWT/RBAC demuestra aislamiento. Requerir para cualquier ejecución real `CASE_ID`, actor/rol, tenant A/B, URL/entorno autorizado, request sintético, resultado esperado/observado, `head_sha`, run/job ID, log redactado, reviewer y rollback.

## 5. Impacto, pruebas proporcionales y reversión

**Componentes afectados:** exclusivamente este archivo Markdown y entrada autocontenida `CHANGELOG_AI.md`. Sin cambios a rutas Angular, API/DTO, Domain/Application/Infrastructure, MySQL, IAM, JWT/RBAC, servidor, DNS, TLS, R2, Cloudinary, Alerting, datos, secrets o schedulers. Consumidores futuros: M01.04 para riesgos; M01.05 criterios; M01.06 y M02–M06 sólo con DAG y autorizaciones separadas. No liberar fase por inventario documental.

- **A0 estática:** 19 rutas/blobs verificados, 12 brechas identificadas sin confundirlas con fallos y 8 pares P/N; verificar procedencia CHANGELOG sin encabezado vacío.
- **A1 documental:** matriz de evidencia y pruebas positivas/negativas falsables, marcadas futuras.
- **A2 seguridad documental:** no se extrajeron tokens, credenciales, PII ni datos de tenants; JWT/RBAC/tenant A/B preservados por no tocar runtime.
- **A3 integración documental:** fuentes M01.01/M01.02 y versiones .NET 10, Angular 22.2.1, MySQL/EF cotejadas; compilaciones/Playwright/MySQL son `NO_APLICA` para estos dos Markdown y **no PASS**.
- **A4 publicación:** fast-forward/CAS dev, diff de dos Markdown, `verify-solqaryn-project-scope` y check V5 de procedencia `completed/success` sobre SHA publicado, readback PLAN/COLA/BITACORA.
- **A5:** revisión independiente pendiente a otra Task Javier distinta; A5 tampoco autoriza QA/main/PROD.

**Reversión:** revert normal exclusivo del commit documental tras inspeccionar HEAD actual; no force-push, reset, borrado histórico, despliegue ni migración.
