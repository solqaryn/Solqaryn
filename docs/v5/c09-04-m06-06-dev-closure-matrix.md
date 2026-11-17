# C09-04.M06.06 — Cierre controlado DEV y mapa de sucesión de mensajería
Fecha de revisión: 2026-11-17. Scope: `solqaryn/Solqaryn@dev`. PROJECT_SCOPE_LOCK=STRICT.
Estado de esta entrega: **artefacto de cierre sujeto a CI, A0–A4 y A5 ajena; no equivale por sí solo a un despliegue, autorización de producción ni cierre del plan.**

## A0 — Requisitos, identidad y riesgos
Microtarea `C09-04.M06.06`: cerrar estado del lote, derivar la habilitación del siguiente bloque del DAG real y registrar los límites de pruebas del sistema de mensajería (Email, WhatsApp, proveedores adicionales). Predecesores expresos: `C09-04.M05.01–06 + A5` y `C09-04.M06.01–05 + A5`. PLAN_MAESTRO, COLA y DAG_TECNICO del Sheet institucional son fuentes ejecutables; este documento no fuerza ninguna fórmula.
Clasificación **R2 de gobierno de dependencias** por su potencial de desbloqueo técnico, aunque el diff es sólo documental. El defecto transversal P1 de coincidencias textuales/COUNTIF con comodines en el DAG **permanece abierto**; no se modifica ninguna fórmula ni se considera resuelto en esta entrega. R3 (datos productivos, Aiven vivo, proveedores, secreto o cutover) queda **NO_GO**.

## Matriz comprobada de entregas M05
| ID | SHA fuente | Evidencia aplicable | Alcance limitado |
| --- | --- | --- | --- |
| M05.01 | `0263060c4f6042e634f26bd5f6c5119babb5bebf` | 7/7 xUnit, A5 independiente | Idempotencia y revocación simuladas; no envío real |
| M05.02 | `9a0dab0695a8212b29222c68db57ade9a11beffa` | 8/8 xUnit, A5 independiente | Controlador/servicio directo; no JWT HTTP real |
| M05.03 | `a44a227f4bc4e5ee6a77762b997afadccdee86e2` | 3/3 xUnit MySQL 8.4 efímero, A5 independiente | CAS/replay/rollback A/B; no proveedor vivo |
| M05.04 | `655a28e7633bd5b3d32a56a8e2e7b7cd4d731323` | 3/3 xUnit, A5 independiente | Aceptación sintética, no certificación integral |
| M05.05 | `76ddaee9990cb1dece9af8813c62612915255b8c` | 11/11 xUnit negativos y Scope Lock, A5 independiente | **V5 no ejecutado en ese SHA: 0 de 29 check-runs**, no se cuenta PASS |
| M05.06 | `3dd2b7be2787c87dcf021eecd628e7238503217d` | 4/4 Chromium, A5 independiente | Navegación y estados simulados, no proveedor/JWT HTTP |

Los seis ID están `LISTO` con registro A5 de Task Javier distinta en PLAN/BITACORA. Las cifras citadas corresponden a sus ejecuciones dirigidas, no a una certificación transversal del ERP.

## Matriz comprobada de entregas M06 antes del nuevo cierre
| ID | SHA de referencia corregido | Estado y evidencia |
| --- | --- | --- |
| M06.01 | `cc2757189229c457aa3111cdffacaa550476a569` | LISTO + A5_PASS correctiva ajena; manifiesto de evidencia corregido: M05.05 no tuvo V5 exact-SHA |
| M06.02 | `a595dbd23f80b20ceaf4de2998ccdfd844992b53` | LISTO + A5_PASS correctiva ajena; matriz por SHA y check, sin V5 ficticio |
| M06.03 | `f1a69376edd966724da28b1fb630e7144af6725c` | LISTO + A5_PASS ajena; riesgos R01–R10, decisiones D01–D08 |
| M06.04 | `9749c4bceac40ebcfc19ddddeffc720c67670721` | LISTO + A5_PASS correctiva DEV03; prueba MySQL 8.4 TRX **5/5**, rollback A/B y rechazo de dígitos Unicode antes de persistir |
| M06.05 | `f2bd80a8005b68f61fda4ef59db156209c7f2edf` | LISTO + A5_PASS ajena de solicitud documental de revisión; esa solicitud no constituye aprobación de producción |

En `M06.04` el código de dominio valida sólo ASCII `0–9` después de `+`; el CHECK Oracle/MySQL usa `^[+][0-9]{8,15}$`. Las regresiones reales contra MySQL 8.4 efímero utilizan caracteres árabe-índicos y fullwidth, verifican excepción sin mutación y conservan las configuraciones de dos empresas. Workflow dirigido run [44193319692](https://github.com/solqaryn/Solqaryn/actions/runs/44193319692) y Oracle [44193319971](https://github.com/solqaryn/Solqaryn/actions/runs/44193319971) finalizaron SUCCESS exact-SHA. El A5_PASS correctivo está en BITACORA y no elimina eventos históricos A5_FAIL.

## A1–A3 — Evidencia negativa, frontera y exclusiones
**Positivos:** once entregas predecesoras `LISTO` con A5 independientes; pruebas reales xUnit/Chromium/MySQL donde aplica; correcciones P2 auditadas. **Negativos:** un check ausente sigue sin contarse PASS; A5_FAIL históricos conservados; ninguna etiqueta VALIDANDO, SKIPPED, CANCELLED o QUEUED reemplaza un check requerido. **Frontera:** sin acceso real a Meta/WhatsApp, SMTP vivo, JWT HTTP firmado end-to-end para todo el lote, consentimiento de marketing, integraciones externas/secretos ni garantía global de entrega o de privacidad. Tenant A/B se prueba únicamente en las rutas especificadas por los tests, no en la totalidad del SaaS.
Dado que este changeset es documental, compilación .NET, Playwright y MySQL nuevos son **NO_APLICA al diff**, no PASS; se conserva como antecedente la CI de los SHA de código realmente probados. Los validadores requeridos para esta publicación son Scope Lock, V5/procedencia, integridad del árbol y controles de changelog del SHA de este changeset. Nada aquí debe desactivar tests.

## A4 — Cierre DEV y reglas de liberación
Al publicar, comprobar fast-forward sobre el HEAD leído, código/doc y CHANGELOG juntos, el SHA exacto, Scope Lock SUCCESS, V5/procedencia SUCCESS, ausencia de errores P0/P1 **propios de la entrega**, lectura posterior de PLAN H/I y BITACORA, y recálculo de COLA G/O y DAG F/G. Los sucesores sólo se liberan por fórmulas reales y dependencias completas; jamás se escriben `SI` manuales. **Si el P1 sistémico de fórmulas deja una sucesora habilitada sin evidencia causal, se bloqueará ese scope y se registrará una reparación separada.**
`VALIDANDO` con CI externa pendiente no habilita LISTO ni A5. `LISTO_DEV`, de obtenerse, no equivale a A5_PASS: un revisor de Task Javier distinta deberá emitir aprobación o rechazo. No ejecutar cambios en QA/main/PROD, producción, Aiven vivo, DNS, servicios de pago o credenciales.

## Rollback y trazabilidad
Revertir únicamente el changeset documental mediante `git revert` normal sobre HEAD verificado; preservar eventos BITACORA y los A5_FAIL históricos. Recalcular DAG después de cualquier transición y conservar readback. Autoría de esta documentación: DEV03; A5 requiere otra Task Javier autorizada.
