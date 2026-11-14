# SOLQARYN V5 — C04-04.M01.06: bloqueo, rollback y condición de salida del lote M01 de cobros

**Autor DEV:** PRIMARY DEV 06 — JAVIER, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Fuente ejecutable:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), `PLAN_MAESTRO!A691:O691`. **Repositorio:** `solqaryn/Solqaryn@dev`, **PROJECT_SCOPE_LOCK=STRICT**. **Fecha:** 2026-11-14. **Naturaleza:** cierre **documental** de preparación M01; no despliega cobros ni acredita M02.

## 1. Autoridad y semántica precisa de la puerta

La tarea `C04-04.M01.06` pide definir bloqueo, rollback y condición de liberación del lote **C04-04 — Cobros y enlaces de pago**, cuyo resultado ulterior es preparar contratos provider-neutral para enlaces, webhooks, conciliación y reversión. El objetivo M01 es la **preparación verificable**, no crear medios de pago, acuerdos comerciales, configuración live ni autorización de cobrar.

El DAG de este ID se verificó en `DAG_TECNICO!A652:J652`, con `F=VALIDADO/G=SI` tras acreditar 60 predecesores preparatorios C01/C02/C03 M01.01–05 y los cinco antecedentes propios `C04-04.M01.01–05` en LISTO **con A5 independiente**. `COLA!G691/O691=SI`, `PLAN_MAESTRO!H691=PENDIENTE` sin owner al claim y el cambio exclusivo por CAS permitieron **escribir esta definición**. Eso no es aprobación para M02/M03 ni entorno QA/main/PROD.

## 2. Cinco entregas precedentes y evidencia A5 separada

| Microtarea | Entrega documental original | SHA original | A5 institucional |
| --- | --- | --- | --- |
| `C04-04.M01.01` | Objetivo y no-regresión provider-neutral | `57d534d2598546a9866f42c9148c133533e26f03` | PASS por Task J50 diferente de autora J40 |
| `C04-04.M01.02` | 12 actores, 16 interfaces, fronteras JWT/RBAC/tenant | `ae950f1ccaad262c91229eee6b549df7a29d202d` | PASS por Task J50 diferente de autora J40 |
| `C04-04.M01.03` | 13 fuentes, 12 brechas de evidencia | `c00e66b4a9d40d90931e6f4c204295e15c60864c` | PASS por Task J50 diferente de autora J40 |
| `C04-04.M01.04` | 16 riesgos R01–R16, diez decisiones D01–D10 abiertas | `11000a92e2a9db4158fb1399cc24ed20f2326567` | PASS por Task J50 diferente de autora J40 |
| `C04-04.M01.05` | 20 criterios AC01–AC20 futuros, NO_EJECUTADO | `df532a5454ddc51aacf1bab9bf47fa032584b9a7` | PASS por Task J50 diferente de autora J40 |

Estos A5 limitados **revisan documentación** de J40; no representan pruebas de una pasarela, un settlement, JWT end-to-end, HMAC live, MySQL/EF real ni un permiso comercial. El A5 de **esta nueva M01.06** debe emitirlo **otra Task Javier distinta de J50** y exige inspeccionar su propio SHA publicado. El histórico BITACORA es inmutable; no se autoaprueba este documento.

## 3. Condiciones bloqueantes antes de considerar M02 o software ejecutable

| Gate | Estado de esta fase | Para pasar a una implementación futura |
| --- | --- | --- |
| G01 Identidad/ámbito | Seis Tasks Javier, dev, Scope Lock STRICT | Repetir identidad, DAG individual, CAS y SHA en cada microtarea |
| G02 Contrato neutral al proveedor | Descripción M01 de enlaces/ingress/conciliación/reversión | Selección y configuración por autorización expresa; no presuponer proveedor |
| G03 Políticas comerciales | **D01–D10 abiertas**, sin tarifas/monedas/fees aprobadas | Decisión humana acreditada sobre flujo, divisa, vencimiento, devolución, impuestos y ownership |
| G04 Autenticación y multitenancy | Fronteras JWT/RBAC/A-B solo descritas | Pruebas reales API/Domain/Infrastructure del tenant A y denegación de B |
| G05 Webhook confiable | Firma, límites, replay e idempotencia especificados | HMAC exacto, timestamp, body limit, anti-replay, deduplicación duradera e integración CI |
| G06 Efectos económicos | **No hay cobro ni ledger activo nuevo** por M01 | Conciliación/ledger/rollback transaccional, pruebas de concurrencia, cero cobro duplicado |
| G07 Infraestructura y secretos | No se habilitan endpoints/productos ni secretos | Gestión de secretos fuera de repo, red, proveedores y costos sujetos a aprobación separada |
| G08 Evidencia de CI | Solo Markdown, Scope Lock/V5 en SHA | .NET 10, Angular 22.2.1/Playwright y MySQL 8.4 efímero según diff causal; skipped/pending nunca PASS |
| G09 Finalización de M01 | `M01.01–05` LISTO+A5, este contrato sujeto A0–A4 y A5 posterior | M02 solo con su DAG/owner/alcance real vigente y sin saltarse G03–G08 |
| G10 Entornos | Dev documental únicamente | QA, main y PROD necesitan otra autorización explícita; no promoción automática |

**Política fail-closed:** toda puerta de código o movimiento económico permanece NO_GO sin evidencia causal y autorización independiente. Una fila `DAG F=VALIDADO/G=SI` solo permite el ámbito **que describe**; no equivale a aprobación comercial, secretos, datos productivos o migraciones. La palabra **“Habilita M02”** del plan significa habilitación **condicional de selección documental** cuando el gate dinámico del siguiente ID también sea SI, no permiso automático de desplegar.

## 4. Rollback y contención por escenario (procedimientos FUTUROS, no ejecutados)

| Riesgo/escenario | Medida preventiva exigida | Plan de reversión / condición NO_GO |
| --- | --- | --- |
| Enlace incorrecto o saldo/moneda incompatible | Validaciones Domain, autorización tenant, monto no negativo, moneda admitida | No iniciar intento; dejar factura intacta y registrar causa segura |
| Replay de link, webhook o llamada concurrente | Idempotencia por actor/tenant/provider/payload, índice único y transacción | No duplicar cargo ni asiento; rechazar payload distinto con misma clave |
| Firma inválida y callback masivo/tardío | HMAC, reloj, tamaño y origen controlados con secretos server-side | Rechazar antes de efectos; no marcar `Pagado` por callback no reconciliado |
| Fallo de proveedor/timeout | Máquina de estados, consulta duradera, reintento acotado | Mantener estado incierto/auditable, no recobrar a ciegas |
| Reembolso/reversión | Autorización separada, referencia inmutable y compensación contable | No eliminar historia; compensar con entrada auditable tras aprobación |
| Error de esquema o migración | MySQL efímero, prueba backup/restore, migración aditiva | Rollback reversible sin perder datos; STOP si integridad no demostrada |
| Fuga de datos o autorización tenant A/B | JWT/RBAC/membresía real, consultas filtradas, logs minimizados | Bloquear operación y acceso; investigar sin exponer PII/secretos |
| Regresión UI/web | Angular guards solo de presentación, errores textuales y E2E | Retirar frontend afectado sin convertir guard en seguridad backend |

Para revertir **este documento** únicamente: `git revert` normal de su commit publicado en dev, revisión de diff, sin force-push ni borrar eventos históricos. Ningún rollback de datos, proveedor, reembolso, migración o pago se ejecutó aquí.

## 5. Oráculos documentales de la puerta (no pruebas runtime)

**Positivos a comprobar documentalmente:**
- P01: los cinco M01 antecedentes muestran LISTO y A5 PASS de Task distinta a su autor.
- P02: alcance de M02 separado de activación de proveedor, precio y migración.
- P03: condiciones de bloqueo G01–G10, rollback y cadena de autorizaciones explícitas.
- P04: matriz AC01–AC20 sigue marcada NO_EJECUTADO hasta CI futura.

**Negativos / criterios de rechazo:** N01 M01 solo VALIDANDO; N02 A5 firmado por su autor; N03 sin JWT; N04 tenant B; N05 idempotencia volátil; N06 firma HMAC inválida; N07 callback replay; N08 saltarse aprobación comercial; N09 liquidar por retorno de proveedor; N10 log con secretos; N11 migración no reversible; N12 QA/PROD inferidos de dev. Son **diseños de validación**, no resultados de test .NET/MySQL/browser.

## 6. Evidencia A0–A4 de esta microtarea y siguiente acción

**A0:** revisar autoridad V5, 60 predecesores, cinco A5, diff y secretos. **A1:** coherencia de G01–G10 y P01–P04/N01–N12 como contratos. **A2:** no habilitar JWT/RBAC, A/B, pasarela, secretos ni efectos económicos; mantener fail-closed. **A3:** predecesores, documentos M01 y reversibilidad consistente, sin runtime afirmado. **A4:** publicación GitHub por fast-forward CAS en dev, checks `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` en SHA exacto, readbacks PLAN/COLA/BITACORA. Los jobs backend/frontend/seguridad no son pruebas **requeridas** para diff exclusivamente Markdown (`NO_APLICA` motivado; no afirmamos PASS).

**Resultado posible:** LISTO_DEV documental **solo** después de A0–A4, con A5_PENDIENTE para otra Task Javier, sin QA/main/PROD. El gate del siguiente lote exige propia evaluación DAG, autorización y alcance, no inferencia de un documento.
