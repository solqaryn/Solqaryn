# SOLQARYN V17 — C04-02.M02.06: dependencias técnicas y contrato de salida de contratos SaaS

**Microtarea:** C04-02.M02.06, PLAN_MAESTRO fila 625. **Ejecutor:** PRIMARY DEV 02 / JAVIER_CHATGPT, TASK_ID \`6af16bb9492c8191b8e8c7d606f11507\`. **Candado V17:** \`SQV17_C04_02_M02_06\`, namedRangeId \`2000000625\`. **Repo:** \`solqaryn/Solqaryn@dev\`, PROJECT_SCOPE_LOCK=STRICT. **Clase de riesgo de esta entrega:** R0, documentación autónoma sin runtime; riesgos comerciales, tenant, conciliación, EF/MySQL y autorización futuros son R2 o R3 según acción.

**Fuente institucional única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES (Drive \`1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA\`), PLAN_MAESTRO H/I/K, COLA G/O, DAG_TECNICO F/G y arquitectura del HEAD dev. **Resultado exacto:** fijar dependencias técnicas y contrato de salida del lote M02 sobre alta, renovación, suspensión, cancelación, mora y cierre contractual. Esta salida **NO** implementa firma, precio, proveedor, débito, renovaciones, cobros, API, tablas, jobs, webhooks ni cambios productivos.

## 1. Fuentes causales y condición de entrada

El DAG de C04-02.M02.06 exigió \`C04-02.M01.01-06+A5 ; C04-02.M02.01-05+A5 ; C04-01.M01.06+A5\`, con F=VALIDADO/G=SI y COLA G/O=SI en el claim. La tarea se reclamó solo tras comprobar H625=PENDIENTE, I625 vacío, una sola EN_PROGRESO propia y el candado V17. Estos gates habilitan **exclusivamente este documento de salida**, no operaciones financieras.

| Predecesor y entrega | Evidencia de código/blob de contrato | Ámbito y aprobación |
| --- | --- | --- |
| C04-02.M01.06 | SHA \`7a2559cf302d7929e762a4432d813756ff90f05c\`; documento \`b3499b87e62aca84fa398b0d5bccd8f00032cc2d\` | G01-G16 M01; A5 por DEV06 distinto de autor DEV05 |
| C04-02.M02.01 | SHA \`5b961b1da74a3e7bdb5e15a7506c5a2acedd6137\`; documento \`4d77b1860f8e805e1db9cfa880ca49c6c2baa3d6\` | dieciséis objetos, 18 invariantes; A5 independiente por DEV02 |
| C04-02.M02.02 | SHA \`dfb37113bae345df9fb3b29aa4ecafb467c46245\`; documento \`cdc7c386fdfb43b958381bbb32483d02ab64b088\` | diez operaciones, nueve DTO, doce errores; A5 independiente por DEV02 |
| C04-02.M02.03 | SHA \`2c3ae43cdb575dfe5683fba3782f7af9adc2ab7c\`; documento \`7c27a4ebb3f5f1e555cae1bf9952e379ca0f78d4\` | trece estructuras propuestas, diez constraints, siete etapas; A5 independiente por DEV02 |
| C04-02.M02.04 | SHA \`574c80963fc2e65ce4806f8e93e91b8f67914376\`; documento \`fb9370b1773b09dc16d852ff5c0e991ab173d7b9\` | dieciocho opciones, doce reglas de denegación; A5 independiente por DEV02 |
| C04-02.M02.05 | SHA \`4052e6e411b7a5b7ef71b0936864609dba53ed9a\`; documento \`2064c87663c18c4512a96b3b6a177d3591b32069\` | doce eventos inertes y doce reglas; A5 independiente por DEV02 |

El documento de entrada C04-01.M01.06 también es prerequisito lógico DAG: su vigencia está representada por gate calculado, no por un supuesto manual de éxito. Al decidir un sucesor se releerán **otra vez** los gates reales, incluyendo A5 independiente de ESTA microtarea M02.06. Una fila LISTO sin A5 requerido no libera automáticamente M03.

## 2. Contrato de no-regresión y frontera del runtime

- \`Plan\`, \`PlanLimite\`, \`Suscripcion\`, \`SuscripcionSaaSService\`, \`SuscripcionesSaaSController\` y la idempotencia técnica de onboarding existentes no representan contrato jurídico firmado ni precio/factura/ledger económico aprobado.
- **D01–D08 económicas y legales permanecen NO APROBADAS:** representación legal, términos/consentimiento, precio/moneda/impuestos, renovaciones, mora, proveedor, cancelación/reembolso y retención. No inventar su aprobación a partir de este lote.
- \`EmpresaId\` autenticada se valida en backend contra membership/RBAC y ownership de cada recurso; IDs de URL/body, guard Angular, plan SaaS o flag no crean autoridad.
- Versiones aceptadas, representación, consentimiento y eventos causales requieren almacenamiento inmutable/auditado y protección tenant A/B; no son tablas ni procesos implementados.
- Toda transición con posible impacto económico, refund, mora, pago, suspensión, datos irreversibles o proveedor externo se clasifica como R2/R3 y requiere autorización concreta, no heredada de M02.
- M02 es contrato conceptual: no se generaron clases EF Core, \`DbContext\`, migraciones, índices, código TypeScript, endpoints, fixtures ejecutables, payloads de PSP ni dispatcher.
- No cambiar QA, main, PROD, Cloudflare, DNS, Render/Aiven reales, secrets, schedules, roles o recursos comerciales.

## 3. Dieciséis puertas técnicas de salida (G01–G16)

| Gate | Criterio obligatorio para fase futura | Condición de bloqueo/falla |
| --- | --- | --- |
| G01 | Skill canónica, AGENTS, Scope Lock STRICT, dev y permisos Javier exactos | error de identidad, alcance o HEAD |
| G02 | PLAN/COLA/DAG del microtask y predecesores LISTO+A5 completos | VALIDANDO, A5 pendiente/fallida o fórmula G/O=NO |
| G03 | V17 namedRange exclusivo, CAS H, owner I y BITACORA idempotente | lock duplicado, carrera o readback incompleto |
| G04 | Modelo contractual no duplica Suscripcion técnica ni entitlement SaaS | contrato legal inferido de flag/estado técnico |
| G05 | D01 representación y D02 términos/versiones aprobadas | firma o aceptación implícita por checkbox |
| G06 | D03 tarifa, moneda, impuesto, periodo y D04 renovación aprobadas | precio calculado sin política y versión |
| G07 | D05 suspensión/morosidad/gracia y conciliación verificable | timeout, callback o deuda de otro tenant disparan suspensión |
| G08 | D06 proveedor autorizado, validación de firma y evidencia confiable | token/callback anónimo confirma pago |
| G09 | D07 cancelación/reembolso y D08 retención/privacidad autorizadas | borrar contratos o devolver dinero por suposición |
| G10 | Backend JWT/RBAC/membresía/revocación, resource ownership y tenant A/B | IDOR/BOLA, acceso B→A o confianza en URL/body |
| G11 | Estado/version CAS transaccional, idempotencia y única transición ganadora | carrera/replay altera contrato o cobra dos veces |
| G12 | FK compuestas por tenant, índices y migraciones aditivas reversibles | hijo A apunta padre B o pérdida de historial |
| G13 | Audit append-only, CorrelationId y privacidad/minimización/retención | fuga PII/JWT, hard-delete o log intertenant |
| G14 | Feature flags restrictivos, plataforma/política/RBAC dominan UI | toggle habilita cobro, mora, firma o privilegio |
| G15 | Tests pertinentes A1-A3: .NET 10 xUnit/HTTP, seguridad A/B; EF Oracle + MySQL 8.4 efímero Category=Integration si cambia DB; Angular/Chromium si UI | build sin integración, PASS de SKIPPED/CANCELLED/FAIL/IN_PROGRESS |
| G16 | A4 Scope Lock/V5 exact-SHA SUCCESS + BITACORA/readback, A5 independiente real por otra Task Javier | autor aprueba su propio cambio o QA/PROD sin permiso |

**Aplicabilidad honesta:** G04–G15 son requisitos futuros de implementación y promoción, **no** controles de código que este documento ya haya superado; el único A4 causal de M02.06 R0 se refiere al Markdown y CHANGELOG publicados.

## 4. Doce pares P/N de aceptación futura — NO EJECUTADOS

| Caso | Positivo esperado cuando exista implementación | Negativo/error que deberá fallar |
| --- | --- | --- |
| P01/N01 | alta A con representante y términos verificados | B/actor no autorizado acepta por A |
| P02/N02 | versión aceptada queda inmutable | update retroactivo sobrescribe términos |
| P03/N03 | contrato A mantiene ownership por todas las FK | referencia B/legacy NULL aparece en A |
| P04/N04 | renovación propuesta respeta UTC y política D04 | autorrenovación o precio implícito |
| P05/N05 | estado Activa→Suspendida requiere conciliación | caída PSP/timeout se interpreta como mora |
| P06/N06 | Suspendida→Activa con causa y permiso | Cancelada/Expirada reactivada |
| P07/N07 | cancelación D07 conserva auditoría e historia | reembolso implícito o borrado destructivo |
| P08/N08 | CAS de dos escritores admite exactamente uno | carrera duplica contrato, cargo o transición |
| P09/N09 | misma IdempotencyKey+digest retorna mismo resultado | payload diferente/scope B altera A |
| P10/N10 | callback verificado y reconciliado aporta evidencia | callback falso marca pagado |
| P11/N11 | lectura tenant A paginada, segura y redactada | JWT revocado, IDOR o B revela datos |
| P12/N12 | upgrade/rollback aditivo conserva versiones y ventas | migración rompe FK o elimina aceptación |

Los pares anteriores son **especificaciones de pruebas futuras**, 0/12 ejecutadas aquí y métricas \`NO_MEDIDO\`. La implementación .NET/EF/MySQL debe registrar test real, SHA, job, logs, suite Category=Integration en MySQL 8.4 efímero, PASS/FAIL y cobertura relevante. Un job Build exitoso o un mock no prueba motor real. Angular/Playwright obligatorio solamente si toca UI.

## 5. Invariantes de salida y decisiones pendientes

El lote M02 define entidades/relaciones/contratos, no un motor de pagos ni autorizaciones legales. Una futura implementación debe preservar el modelo \`Domain <- Application <- Infrastructure\` y API de entrada, con Scope Lock y controles JWT/RBAC. El frontend presenta datos, no concede permisos.

Las decisiones comerciales D01–D08 son entradas externas todavía no acreditadas y sus valores no pueden fabricarse para cerrar M03. El conjunto de notas M02 no valida firmas, certificados fiscales, jurisdicción, mora, restauración, rendimientos, SLAs ni integración PSP: **NO_MEDIDO / NO_APLICA_R0** hasta evidencias reales y permisos. Toda deuda del baseline que rompa CI obligatoria se reparará con cambio mínimo y sin invadir escritor activo.

El progreso al siguiente lote exige M02.06 LISTO_DEV por A0–A4 y A5 independiente de otra Task Javier, los demás predecesores efectivos y DAG/ COLA verdaderos. No forzar \`SI\` ni actuar sobre QA, main o PROD.

## 6. Auditoría A0–A5 y reversibilidad de esta unidad R0

- **A0:** validar requisito exacto C04-02.M02.06, fuentes M01/ M02.01–05, sus A5, scope de dos Markdown y 16 gates; no simular pruebas.
- **A1 (documental):** 16 bloqueos, doce pares de positivos/negativos/fallo transaccional como futuros; 0 tests runtime. No afirmar HTTP/DB operativos.
- **A2 (estático):** tenant A/B, JWT/RBAC/revocación, contratos separados de suscripción, privacidad, firma proveedor, conciliación y doble gasto.
- **A3:** \`NO_APLICA_R0\` a migraciones, Category=Integration MySQL/EF, .NET xUnit, API/HTTP, Angular y Playwright: este cambio sólo añade texto.
- **A4:** publicación dev fast-forward/CAS contra HEAD real, verificación de los dos blobs, \`verify-solqaryn-project-scope\` y \`V5 - Sin encabezados vacios ni atribucion cruzada\` \`completed/success\` exact-SHA o equivalencia auditada; BITACORA idempotente, PLAN/COLA/DAG readbacks y liberar V17 lock una vez.
- **A5:** otra Task Javier distinta de la autora deberá revisar fuente/artefacto, gates, diff, pruebas causales y defectos; A5_FAIL bloquea siguiente lote hasta reparación y nueva evidencia, nunca autoprobación.
- **Rollback documental:** \`git revert\` ordinario con HEAD revalidado, sin force-push, reset, borrar BITACORA, tocar datos reales o revertir cambios ajenos.

**Conclusión restringida:** contrato técnico de salida M02 documentado, sin implementar capacidad contractual, financiera, migración o evento runtime y sin permiso de promover a QA/main/PROD.
