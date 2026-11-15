# SOLQARYN V5 FINAL — C14-01.M02.04: configuración tipada, ámbitos y flags de aceptación

**Tipo:** contrato documental independiente M02 de aceptación funcional; no es configuración ejecutable. **Autor:** PRIMARY DEV 06 (`6af16bbad26881918c283abe06e1f32a`). **Fecha:** 2026-11-15. **Repo:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT.

## Preflight institucional y límites

Se verifica [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN_MAESTRO/COLA/DAG](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), M01.01–06 LISTO y A5 independiente. Al claim C14-01.M02.04 tenía PLAN H=PENDIENTE/I vacío, COLA G/O SI y DAG F=VALIDADO/G=SI. Esta tarea no depende técnicamente de M02.05 (ambas parten de M01+A5), aunque debe preservar contratos compatibles. Sin migraciones, secretos, cambios de entornos, URLs o flags reales.

Proponemos validación tipada para que un futuro orquestador de **pruebas efímeras** nunca transforme un flag de interfaz en permiso. Resolución de precedencia: baseline global seguro → política de entorno autorizada → tenant verificado → rubro/plan permitido → sucursal del mismo tenant → recurso/escenario, con **DENY siempre dominante**. Ninguna capa descendente puede habilitar una operación restringida por backend.

## Catálogo CFG01–CFG16 conceptual

| ID | Clave candidata | Tipo | Ámbito de resolución | Default seguro | Regla de control |
| --- | --- | --- | --- | --- | --- |
| CFG01 | acceptance.testMode | boolean | global/env | false | Sin sandbox sintético no ejecutar pruebas |
| CFG02 | acceptance.publicFlow | boolean | tenant + product | false | No abre rutas privadas |
| CFG03 | acceptance.privateFlow | boolean | tenant + membership | false | JWT y grants backend siempre requeridos |
| CFG04 | acceptance.adminFlow | boolean | tenant + role | false | El administrador no omite A/B |
| CFG05 | acceptance.tenantIsolation | enum strict | global | strict | No existe valor permisivo |
| CFG06 | acceptance.branchScope | string/identifier | tenant + branch | deny | Sucursal respaldada por backend |
| CFG07 | acceptance.fixtureProfile | enum synthetic | CI efímera | none | No importar datos reales |
| CFG08 | acceptance.browserMatrix | readonly viewport[] | scenario | [] | 320/390/768/1280 como casos futuros |
| CFG09 | acceptance.negativeCoverage | boolean | scenario | true | Negativos mínimos no opcionales |
| CFG10 | acceptance.sourceSha | sha40 | run | unset | Rechazar SHA ausente u obsoleto |
| CFG11 | acceptance.evidenceRetention | duration enum | env/security | zero | Sólo evidencia saneada autorizada |
| CFG12 | acceptance.maxAttempts | integer 1..5 | CI | 1 | Evitar reintentos infinitos |
| CFG13 | commerce.paymentSimulation | boolean | CI synthetic only | false | No invocar PSP ni cobrar |
| CFG14 | commerce.fiscalSimulation | boolean | CI synthetic only | false | No emitir documentos fiscales |
| CFG15 | security.requireBackendGrant | boolean | global | true | No relajar RBAC desde UI |
| CFG16 | release.allowPromotion | boolean | release authority | false | No habilitar QA/main/PROD por M02 |

**No se agregan claves reales a appsettings, Angular, CI, base de datos ni a proveedores.** Los nombres anteriores son propuestas auditables, no prueba de disponibilidad de una funcionalidad. Cualquier implementación requerirá propietarios de configuración, contrato de schema y tests de precedencia por tenant A/B.

## Doce validaciones obligatorias VT01–VT12

| Validación | Resultado exigido |
| --- | --- |
| VT01 | tipo y enum válidos, campos desconocidos rechazados |
| VT02 | configuración global no permite desactivar tenantIsolation |
| VT03 | prevalece denegación sobre grants de menor autoridad |
| VT04 | tenant A no hereda flags de tenant B |
| VT05 | membresía revocada no recupera privateFlow |
| VT06 | branchScope exige pertenencia verificada por servidor |
| VT07 | false/off predeterminado para cualquier efecto comercial |
| VT08 | sourceSha válido y exacto antes de artefactos certificables |
| VT09 | fixtureProfile sólo sintético y aprobado |
| VT10 | maxAttempts acotado, sin relanzamientos infinitos |
| VT11 | retención mínima, sin token, PII ni enlace privado |
| VT12 | promoción siempre fuera del alcance de esta configuración |

Un lector de flags no es una fuente de autorización. Deberá fallar cerrado ante valor desconocido, sintaxis inválida, pertenencia de tenant o sucursal no demostrada, versión incompatible y lease/CI cancelados.

## Oráculos futuros P01–P04 y N01–N12 (NO EJECUTADOS)

| ID | Positivo FUTURO | ID | Negativo FUTURO |
| --- | --- | --- | --- |
| P01 | CI efímera con testMode habilitado y actor sintético válido | N01 | Intento desde frontend de cambiar JWT/RBAC sin permiso denegado |
| P02 | Tenant A y B conservan flags diferentes sin fuga | N02 | Tenant B adopta flag A por header manipulable |
| P03 | Scenario con SHA exacto habilita sólo evidencia saneada | N03 | SHA obsoleto marca un resultado falso PASS |
| P04 | UI responsive muestra estado de error y acción accesible | N04 | Modo test abre datos o recursos privados |

Negativas adicionales FUTURAS: N05 permiso revocado, N06 sucursal externa, N07 payload de flag no tipado, N08 CI cancelada, N09 intentos por encima de maxAttempts, N10 simulador que solicita cobro externo, N11 token/PII en captura y N12 flag que pretende permitir QA/PROD. Todas requieren pruebas reales en etapas posteriores con fixture sintético y CI exact-SHA.

## A0–A5, riesgos y rollback

A0: dos Markdown, dieciséis claves conceptuales, doce validaciones y oráculos delimitados; sin código ni secretos. A1: revisión positiva/negativa estructural, NO runtime. A2: política de denegación, tenant A/B, revocación de permisos, alcance de rubro/plan/sucursal; no confundir frontend con backend. A3: M01.01–06 LISTO+A5 y compatibilidad conceptual con .NET10, Angular y MySQL8.4, sin publicar cambios de configuración. A4: publicación fast-forward/CAS en dev, blobs verificados, `verify-solqaryn-project-scope` y comprobación V5 `completed/success` en SHA exacto o equivalencia causal comprobada; readback institucional. Si la CI falta, mantener VALIDANDO. A5 por una Task Javier distinta.

Rollback: `git revert` del documento sin reescribir historia, BITACORA append-only. No HTTP, pagos, DDL, webhooks, validaciones E2E ejecutadas, QA/main/PROD.
