# SOLQARYN V19 — C09-04.M06.03 | Riesgos y decisiones de mensajería multicanal

Fecha: 17 de noviembre de 2026. Autor: DEV06, TASK_ID `6af16bbad26881918c283abe06e1f32a`. Sólo `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT. PLAN_MAESTRO fila 1438; lock V17 `SQV17_C09_04_M06_03`, ID `2000001438`. **Riesgo del changeset R0 DOCUMENTAL**, aunque el proceso de mensajería auditado involucra riesgos R2.

## A0: fuentes, alcance y autoridad
El objetivo de la fila M06.03 es **documentar limitaciones, riesgos abiertos y decisiones tomadas**, no activar ni probar proveedores ni certificar toda la fase. Artefactos de origen:
- [C09-04.M06.01, auditoría prueba real/mock](https://github.com/solqaryn/Solqaryn/commit/434de297aef583f46011b87de469c63bd84c7b8b): matriz M05.01–06 que separa xUnit fake, MySQL 8.4 efímero y Chromium con sesión simulada.
- [C09-04.M06.02, manifiesto de versiones/diffs](https://github.com/solqaryn/Solqaryn/commit/ba76baf7d564e5c4b293952b96ff78e4f5dad3ba): seis SHAs, seis runs y procedimiento reproducible.
- Conjunto M05 con seis jobs GitHub Actions concluidos: [M05.01, 7/7](https://github.com/solqaryn/Solqaryn/actions/runs/44121223928), [M05.02, 8/8](https://github.com/solqaryn/Solqaryn/actions/runs/44124315470), [M05.03, 3/3 Integration real MySQL 8.4](https://github.com/solqaryn/Solqaryn/actions/runs/44126557626), [M05.04, 3/3](https://github.com/solqaryn/Solqaryn/actions/runs/44126350846), [M05.05 corregida, 11/11](https://github.com/solqaryn/Solqaryn/actions/runs/44129286222), [M05.06, 4/4 Chromium](https://github.com/solqaryn/Solqaryn/actions/runs/44140941910).
Se verificó PLAN_MAESTRO M05.01–06 LISTO y A5 de distinta Task Javier. Esto acredita **las pruebas explícitas de sus cambios**, no los controles externos abajo listados. El changeset actual modifica sólo este documento y una sección contigua de CHANGELOG_AI.md. Las referencias arquitectónicas permanecen Angular 22.2.1, .NET 10, EF Core 10.0.12 y Oracle MySQL 8.4 en CI. No cambia Domain/Application/Infrastructure/API, frontend runtime, JWT, schemas, secretos ni infraestructura.

## Registro de riesgos vivos R01–R10 y acción posterior
Estado `ABIERTO` significa que una prueba causal necesaria no está acreditada; no representa un fallo de las suites dirigidas. `NO_MEDIDO` se usa donde no existe numerador/denominador o experimento.

| ID | Riesgo, severidad tentativa | Evidencia sí existente | Brecha no certificada / condición de salida | Owner de decisión |
| --- | --- | --- | --- | --- |
| R01 | **ALTO** — webhook de terceros sin autenticidad | Pruebas unitarias negativas del worker/outbox M05.01/05 | Firma de webhook, timestamp, audiencia, rechazo de firma/secret rotado y replay HTTP reales, autorizados en integración separada | Futura microtarea técnica elegible del plan + revisor A5 distinto |
| R02 | **ALTO** — replay duplicado por entrega de proveedor | M05.01 7/7, M05.03 MySQL A/B 3/3, M05.05 11/11 | Idempotencia durable con callbacks/concurrencia/red externa real, reconciliación sin duplicar envío; `NO_MEDIDO` sobre red | Futura microtarea de integración independiente |
| R03 | **ALTO** — fuga PII/secretos en logs y payloads | Errores opacos y denegaciones en M05.02/M05.05, Chromium 503 | Revisar minimización, retención, consentimientos y destinatario de proveedores autorizados; prohibido usar PII real | Microtarea de seguridad/privacidad y A5 ajena |
| R04 | **ALTO** — aislamiento empresarial insuficiente en borde HTTP | M05.03 3/3 MySQL efímero y pruebas unitarias A/B | JWT firmado real, RBAC/revocación y BOLA/IDOR a través de endpoint HTTP persistente en entorno aislado | Microtarea R2 backend/API bajo DAG |
| R05 | **MEDIO/ALTO** — estado de entrega ambiguo | CAS/lease, stale confirma/replay en pruebas M05.03 | Estados proveedor real ACCEPTED/FAILED/RETRY/DLQ, idempotency key persistente y reconciliación de recibos | Integración específica del proveedor, no esta auditoría |
| R06 | **MEDIO** — fallos/latencia de red producen reintentos excesivos | Escenarios simulados de retry/conflicto | Política de backoff real, rate limit y métricas observables por tenant bajo proveedor autorizado; `NO_MEDIDO` | Integración y observabilidad |
| R07 | **MEDIO** — accesibilidad parcial de panel | M05.06: cuatro Chromium 320/375/768/1280, Tab/Enter, 403/503 | No se midió conformidad WCAG global, lectores de pantalla ni SLO real; cota 20 s es aserto de CI | Microtarea UX/QA pertinente |
| R08 | **ALTO** — envío con consentimiento revocado | M05.01 consentimiento/worker; M05.05 contexto inválido | Revocación servidor comprobada inmediatamente antes del llamado real a proveedor, TOCTOU y auditoría de intentos | Backend/integración R2 |
| R09 | **MEDIO** — aserciones de tests ocultan defecto | M05.05 corrige lookup Moq y pasa 11/11 | Mantener aserciones adversariales robustas y mutación dirigida; no tratar old SHA `443707d9` como check correcto | Autor técnico y A5 diferente |
| R10 | **CRÍTICO si PROD** — promoción prematura y pérdida de evidencia | Seis M05 LISTO+A5 verificadas en DEV | Fuera de alcance autorización para QA/main/PROD, proveedores/secretos o datos reales; NO_GO R3 sin orden explícita | Propietario humano para R3, ninguna Task la infiere |

El riesgo P1 de fórmula/wildcard DAG reportado por otras entregas sigue **abierto fuera de este scope**. No se cambian fórmulas G/O ni se convierte `VALIDANDO` en autorización para sucesores técnicos.

## Decisiones D01–D08
- **D01 — Evidencia limitada:** reconocer como reales MySQL 8.4 efímero M05.03 y Chromium M05.06, y etiquetar como mocks los dobles de repositorio/servicio/proveedor. Nunca agregar a una sola métrica sin denominador.
- **D02 — Seguridad fail-closed:** rechazos tenant, consentimiento, idempotencia y roles deben seguir probándose a nivel backend/API verdadero cuando una unidad R2 autorizada lo requiera.
- **D03 — Proveedores:** no integrar credenciales SMTP/WhatsApp ni webhooks reales como parte de esta entrega R0; registrar ausencia de prueba como `NO_MEDIDO`, no `PASS`.
- **D04 — Privacidad:** no recopilar, imprimir ni compartir datos reales de empresas, teléfonos o tokens en CI/bitácoras; únicamente fixtures sintéticos.
- **D05 — Observabilidad:** futuras métricas de duplicaciones, tasa de entrega, reintentos, revocaciones y latencia necesitarán numerador, denominador, intervalo y entorno; hoy `NO_MEDIDO`.
- **D06 — Calidad A0–A5:** exigir fuente SHA/Run/Job, checks `completed/success`, readback PLAN/COLA/DAG/BITACORA; SKIPPED/CANCELLED/IN_PROGRESS no son PASS. A5 de DEV06 deberá otorgarla **otro** Task_ID Javier.
- **D07 — Reversión:** rollback de este documento exclusivamente `git revert` normal con HEAD revalidado. Los estados y BITACORA históricos no se eliminan.
- **D08 — Liberación:** ninguna decisión aquí autoriza migración, deployment, M06.06, producción, cambios de planes o salto de los gates DAG.

## A1 asertos verificables y negativos
Positivos: (P1) seis SHAs presentes en M06.02 y matriz coincidente con PLAN; (P2) los seis runs tienen job `completed/success` y conteos documentados 7/8/3/3/11/4; (P3) M05.03 `Category=Integration` real en MySQL 8.4 efímero y M05.06 Chromium real, sin confundirlos. Negativos: (N1) un mock jamás certifica proveedor real; (N2) un check SKIPPED/IN_PROGRESS no se suma a PASS; (N3) ni M05 LISTO+A5 ni este R0 permiten ejecutar R3; (N4) JWT firmado, consentimientos en proveedor, webhooks, PII y entrega externa permanecen NO_MEDIDO; (N5) no se deduce que la fórmula DAG queda corregida.

## A2/A3 — pertinencia, integración y rollback
A2: auditoría documental de fronteras de seguridad y aislamiento, sin cambios ejecutables. A3: `NO_APLICA` integración NUEVA por diff R0, sin inventar MySQL/API/browser nuevos; se citan pruebas anteriores según SHAs exactos. A4 exige dos archivos únicamente en commit fast-forward `dev`, Scope Lock y V5 exitosos en SHA propio, PLAN/COLA/DAG/BITACORA readback y riesgos preservados. A5 pendiente revisión independiente por otro Javier. Sin QA/main/PROD, sin modificación de programaciones ni cuentas. Rollback documental: git revert normal no destructivo, evidencia histórica permanece.
