# SOLQARYN V5 FINAL — C14-03.M01.06: barreras, rollback y liberación documental de promoción controlada

**Microtarea:** `C14-03.M01.06`. **Autor:** PRIMARY DEV 06 / TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Fecha:** 2026-11-15. **Repo:** `solqaryn/Solqaryn@dev`. **PROJECT_SCOPE_LOCK=STRICT**.

**Tipo de cierre permitido:** exclusivamente contrato terminal documental de preparación M01. **No es autorización de promoción QA/main/PROD, despliegue, secret rotation, pruebas E2E ejecutadas, migración de datos, cambio de dominio, checkout de producción ni activación de servicios externos.** La liberación documental habilitará sólo sucesoras que realmente satisfagan PLAN/COLA/DAG y sus propias revisiones.

## Preflight y cinco fuentes precedentes con A5 independiente

Autoridades: [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, `docs/AUTOMATION_AUTHORITY.md` y Skill canónica. En el reclamo institucional fila 2095, PLAN H=PENDIENTE/I vacío, COLA G/O=SI y DAG F=VALIDADO/G=SI; claim condicional y owner por Task Javier única con readback. La existencia de fuentes no equivale a ejecución funcional.

| Predecesor | Git blob observado en dev | Objeto documental; estado institucional |
| --- | --- | --- |
| M01.01 | `951bdd464157a429c8524d44ad0101dc1946a2e4` | Alcance y 16 invariantes de no regresión; LISTO y A5 independiente |
| M01.02 | `3ef9d5f9bf5b0bad584c5d41dee78ed57d80c29f` | Doce actores e interfaces de confianza; LISTO y A5 independiente |
| M01.03 | `2511bafbffacfc1083cd63670ad13c7b6c232ebb` | Doce fuentes Git y doce brechas no resueltas; LISTO y A5 independiente |
| M01.04 | `b081a162d0cc921da6ce3eded9e54b68b20bcfc7` | Catorce riesgos potenciales, ocho supuestos y ocho decisiones abiertas; LISTO y A5 independiente |
| M01.05 | `934a05c0eeb5033f70e67902b5a317f094d565ce` | Dieciséis pares P/N futuros y diez NFR; LISTO y A5 independiente |

Las cinco fuentes no constituyen certificado de paridad DEV→QA→PROD. En particular, riesgos/decisiones M01.04 siguen potenciales/ABIERTOS, y pares M01.05 son FUTUROS_NO_EJECUTADOS. No se puede suponer que la existencia de un workflow sea aprobación de GitHub Actions en un SHA concreto.

## Gates de bloqueo G01–G16 y responsables

| ID | Prueba causal que requerirá el futuro proceso | Si falta o falla |
| --- | --- | --- |
| G01 | Identidad SOLQARYN, Skill canónica y PROJECT_SCOPE_LOCK STRICT | Denegar escritura/certificación si falla |
| G02 | PLAN H/I, COLA G/O y DAG F/G SI por ID exacto | No forzar SI ni asignar owner ajeno |
| G03 | M01.01–05 LISTO con A5 por revisor distinto y blobs conservados | No cerrar M01.06 ni activar M02 |
| G04 | SHA exacto y digest de artefacto de release verificables | No inferir paridad entre ambientes |
| G05 | Branch protection y ausencia de force-push/reset destructivo | Preservar dev/main/qa/PROD sin autorización |
| G06 | GitHub Actions required completed/success exact SHA | Queued/skipped/cancelled/failure no PASS |
| G07 | Pruebas net10/Angular22/MySQL8.4 dirigidas según diff | No contar fuente de tests como ejecución |
| G08 | JWT firmado, membresía activa, RBAC y aislamiento A/B | No conceder acceso por flag Angular |
| G09 | Secretos segregados; ninguna variable confidencial en artefacto/log | Fuga impide cualquier promoción |
| G10 | Esquema EF/MySQL compatible y reversión probada en efímero | Sin DDL/restore real no acreditar gate |
| G11 | Transacciones/pagos, versión e idempotencia demostradas | Rechazar doble factura y pago duplicado |
| G12 | E2E con accesibilidad, foco y responsive medidos | Sin Playwright real no certificar UX |
| G13 | Paridad de configuración no sensible y health/alertas causales | No declarar ambiente saludable sin evidencia |
| G14 | BITACORA append-only, owner único, evento idempotente y readback | No reescribir historia o duplicar scope |
| G15 | Autorización explícita y distinta para QA y, más adelante, PROD | LISTO_DEV/A5 no son permiso de deploy |
| G16 | Plan de rollback específico por código/datos/proveedor y ensayo | Bloquear release sin reversión segura |

La regla de salida de **esta M01.06** es documental: predecesores LISTO+A5, revisión de fuentes, 16 gates escritos, 10 escenarios de reversión, 12 pares positivos/negativos futuros, CHANGELOG íntegro, Scope Lock y check V5 del SHA exacto. La regla de salida de una futura promoción es **distinta** y requiere permisos nuevos de ambiente, ejecutores autorizados, CI causal, seguridad/tenancy, configuración y rollback realmente probado.

## Matriz RB01–RB10 de recuperación, sin tocar entornos

| ID | Situación potencial | Medida autorizada dentro de dev |
| --- | --- | --- |
| RB01 | Scope Lock FAILED | Corregir token/archivo causal y revalidar sin desactivar gate |
| RB02 | V5 CHANGELOG mal atribuido | Restablecer sección contigua y validar procedencia |
| RB03 | HEAD cambió durante escritura | Leer nuevo HEAD y recomponer cambio aditivo fast-forward/CAS |
| RB04 | CAS owner o lease incierto | Abortar claim, preservar otro escritor y elegir independiente |
| RB05 | CI required cancelada | Conservar VALIDANDO; revalidar SHA vigente sin llamar PASS |
| RB06 | JWT/RBAC/tenant A/B regresión | Detener promoción y corregir en dev con pruebas negativas |
| RB07 | MySQL efímero revela incompatibilidad | No migrar datos reales; restaurar fixture/snapshot efímero |
| RB08 | Error financiero o duplicado de pedido | Contener flujo de ensayo, sin reversión de dinero real aquí |
| RB09 | Health o experiencia visual degradada | Proponer rollback artefacto probado con autorización por ambiente |
| RB10 | A5 autoría propia o falta permiso QA/PROD | Denegar avance, registrar evidencia y mantener gate NO |

Revertir un Markdown puede resolverse con `git revert` normal y BITACORA compensatoria; **no** revierte transacciones monetarias, datos MySQL ni artefactos desplegados. La reversión de esos sistemas requeriría contratos técnicos separados, credenciales y permisos explícitos, backup/restore probado y evidencias causales.

## Doce oráculos positivos/negativos propuestos, FUTUROS_NO_EJECUTADOS

| Positivo | Criterio FUTURO | Negativo | Fallo que debe bloquear |
| --- | --- | --- | --- |
| P01 | Mismo digest para artefacto probado y candidato | N01 | Digest distinto bloquea |
| P02 | Controles de Scope Lock y V5 exact SHA pasan | N02 | Skipped/cancelled nunca PASS |
| P03 | A5 independiente y owner exclusivo acreditados | N03 | Autoaprobación/lease disputado bloquea |
| P04 | QA tiene permiso explícito distinto de dev | N04 | A5 sola no habilita QA |
| P05 | PROD tiene autorización separada y paridad probada | N05 | LISTO_DEV/QA no autorizan producción |
| P06 | JWT/membresía/RBAC vigentes validan recurso A | N06 | Token revocado o tenant B queda denegado |
| P07 | Test backend y frontend pertinente concluye SUCCESS | N07 | Código de test sin ejecución no certifica |
| P08 | MySQL efímero mantiene esquema y relaciones | N08 | DDL/rollback no probado bloquea |
| P09 | Idempotencia evita doble pedido/factura/pago | N09 | Retry duplica efecto y se bloquea |
| P10 | E2E mide foco y accesibilidad responsive | N10 | CTA inaccesible o test no ejecutado bloquea |
| P11 | Health/readiness y alertas registran estado observable | N11 | Errores 5xx sin plan de reversión bloquean |
| P12 | Rollback de código preserva auditoría y datos | N12 | Force-push/reset o pérdida de registros bloquea |

Cada caso futuro debe identificar actor, tenant/empresa/sucursal, recurso, entrada, salida observable, SHA/árbol/digest, fixture sintético, job o prueba HTTP/Playwright/MySQL concreta, logs saneados, resultado nativo y responsable de revisión. **No afirmar PASS** de tests frontend/backend/seguridad/DB simplemente porque un documento los enumere.

## A0–A5, criterios de responsabilidad y postcondición DAG

**A0:** alcance estrictamente documental, dos Markdown, cero cambios ejecutables, credenciales o entornos. **A1:** revisión de 16 puertas, 10 rutas RB y 12 pares P/N con requisitos discriminantes; todos escenarios futuros. **A2:** JWT/RBAC, tenant A/B, owner exclusivo, no exposición de secretos, no autoaprobación ni aumento de permisos. **A3:** cinco predecesores M01.01–05 LISTO + A5 independiente y contraste de blobs anteriores; ninguna prueba real de paridad, rollback, datos o UX ejecutada. **A4:** publicar fast-forward/CAS en dev y comprobar diff, SHA exacto, `verify-solqaryn-project-scope` y V5 provenance `completed/success`; PLAN/COLA/BITACORA readback. Si la CI requerida está queued/in_progress/cancelled/failed, conservar VALIDANDO. **A5:** revisión posterior independiente por otra Task Javier; esta Task autora no se aprueba a sí misma.

**Resultado institucional permitido:** al concluir A0–A4, `C14-03.M01.06` puede quedar LISTO_DEV con A5_PENDIENTE; M02 permanece bloqueado si el DAG requiere A5. No crear permiso de QA/main/PROD, PR, rama, Task, evento, webhook ni proveedor. El eventual `A5_PASS` sólo actualizará evidencia institucional y recálculo de DAG, no ejecutará una promoción.

Rollback del documento: `git revert` normal en dev sin force-push ni reescritura de BITACORA.
