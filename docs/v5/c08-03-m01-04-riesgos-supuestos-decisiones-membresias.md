# SOLQARYN V5 — C08-03.M01.04: riesgos, supuestos y decisiones de membresías y acceso

**Autor:** PRIMARY DEV 02 / JAVIER_CHATGPT / Task `6af16bb9492c8191b8e8c7d606f11507`. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, PLAN_MAESTRO fila 1229. **Rama:** `solqaryn/Solqaryn@dev`, STRICT. Fecha: 2026-11-14.

## Evidencia y naturaleza

Unidad M01.04 **documental**, con riesgos **POTENCIALES** y decisiones **PENDIENTES**, no defectos certificados. Antecedentes: M01.01 blob `79bcd2aa2dc4c2f5cb8abdf243334d09ed6d347b` (objetivos y NR01–NR14); M01.02 blob `6bfc9fd5f36a232f1d37be64cf00d9fb23de78d2` (interfaces futuras); M01.03 commit `8822649e91d2a7b71ebec55d55872b1544e72af1`, documento blob `41dd66b36650f5e0e836038d0efe6905a1c9a423` (12 rutas código reales). El DAG de M01.04 tiene F=VALIDADO/G=SI y COLA G/O=SI: habilita preparación documental, no hardware, pagos, IAM, migraciones, QA ni PROD.

El inventario anterior observó `Cliente.EmpresaId` nullable, `UsuarioEmpresa` empresarial distinta del abono comercial, `Suscripcion` SaaS distinta de membresía de gimnasio y rutas comerciales actuales de Clientes/SaaS sin check-in en los archivos inspeccionados. **La ausencia de endpoint de gimnasio en archivos observados no demuestra ausencia global del módulo ni autoriza construirlo sin preflight de código.**

## Matriz de riesgos potenciales R01–R14

| Riesgo | Evento que se debe prevenir en futuras fases | Mitigación de diseño requerida |
| --- | --- | --- |
| R01 | Cruce tenant A/B por selector enviado por cliente | Consultar sólo tenant derivado de sesión y vínculo server-side |
| R02 | Plan comercial equivocado por reutilizar Suscripcion SaaS | Separar dominio de abono de gimnasio de suscripciones SaaS |
| R03 | Cliente histórico sin ownership empresarial acreditado | No inferir EmpresaId si es nulo; bloquear acceso hasta reconciliar |
| R04 | Acceso después de vencimiento por reloj local o DST | Evaluar frontera temporal con UTC e IANA probado |
| R05 | Reutilización de QR/credencial durable | TTL, nonce, audiencias y verificación remota con replay rechazado |
| R06 | Permiso RBAC de lectura permite suspender/reactivar | Separar operaciones y permisos por empresa y sucursal |
| R07 | Suspensión o baja no invalida caché | Revocación efectiva y política de consistencia de lectura |
| R08 | Doble check-in concurrente o carrera de cupo | Transacción/idempotencia en persistencia autorizada |
| R09 | Renovación activada por callback de pago no verificado | Prueba de pago autenticada, firma, conciliación e idempotencia |
| R10 | Exceso de PII o credenciales en auditoría | Minimizar datos y correlacionar sin tokens ni secretos |
| R11 | Dispositivo offline concede acceso por defecto | Fail-closed; sin hardware ni offline aprobados |
| R12 | Una sede permite derechos de otra sede | Tenant+sucursal+recurso comprobados en servidor |
| R13 | Interfaz Angular induce a creer que guard equivale a auth | Backend revalida antes de toda operación |
| R14 | Documento M01 se interpreta como orden de despliegue | Gate específico posterior, pruebas reales, A5 y autorización explícita |

Estos riesgos no son vulnerabilidades verificadas en producción. La mitigación exigiría implementación, tests y CI causales antes de declarar PASS. Las fronteras cliente→API→Application→Domain→Infrastructure nunca permiten que claims, EmpresaId, plan o pago aportados por el cliente determinen autorización real.

## Supuestos falsables S01–S08, todavía NO_VERIFICADOS

| Supuesto | Hipótesis de trabajo no confirmada | Evidencia futura exigida |
| --- | --- | --- |
| S01 | La fuente de miembro comercial podría derivarse de Cliente, no comprobado | Revisión de identidad/ownership y esquema relacional sin datos vivos |
| S02 | Los planes podrían limitar zona, horario, uso y recursos | Definir reglas comerciales sin escribir migración |
| S03 | Existe o se diseñará un reloj confiable de servidor con zona IANA | Pruebas deterministas UTC/DST en M03 |
| S04 | El pago podría condicionar activación o renovación | Decisión de negocio documentada, sin PSP |
| S05 | Podrían existir lectores/QR futuros, no inventariados | Inventario autorizado de hardware y protocolo antes de integración |
| S06 | El modelo de roles podría requerir nuevos permisos por acción | Matriz RBAC con separación de funciones y no autoascenso |
| S07 | Podría necesitarse auditoría específica de acceso | Contrato de retención/PII/correlación |
| S08 | Se necesitarían fixtures de tenant A/B para pruebas | Datos sintéticos efímeros, nunca datos productivos |

**Regla:** si un supuesto no se verifica, no se crea la entidad ni se libera M01.06/M02 por mera declaración. El modelo de cliente, pertenencia empresarial y plan comercial permanecen distintos hasta demostrar su relación real.

## Decisiones D01–D08 que requieren autoridad de negocio/arquitectura

| ID | Decisión concreta | Estado | Política de seguridad mientras está pendiente |
| --- | --- | --- | --- |
| D01 | ¿Cliente comercial o entidad Miembro independiente? | PENDIENTE | No afirmar modelo ni migrar datos |
| D02 | ¿Qué atributos y límites tiene PlanGimnasio? | PENDIENTE | No reutilizar Plan SaaS |
| D03 | ¿Semántica exacta de expiración y zona de negocio? | PENDIENTE | Denegar en ambigüedad |
| D04 | ¿Qué actores crean, suspenden o reactivan? | PENDIENTE | Privilegio mínimo |
| D05 | ¿Qué sede/recurso y cuota se valida? | PENDIENTE | No extender acceso a otra sede |
| D06 | ¿Cómo tratar offline y QR de vida corta? | PENDIENTE | No conectar hardware ni abrir automáticamente |
| D07 | ¿Cuál es el contrato de pago e idempotencia? | PENDIENTE | Sin cobros ni webhooks |
| D08 | ¿Qué retención, borrado y auditoría aplican? | PENDIENTE | Minimización PII |

La documentación explicita las decisiones pendientes para evitar implementaciones especulativas. Ninguna requiere interacción humana inmediata para cerrar esta unidad M01.04 **documental**. Sí se exige resolución comprobable antes de cualquier nueva implementación que dependa de ellas.

## Escenarios de aceptación futuros NO_EJECUTADOS

**TP01–TP08 positivos futuros:** TP01 miembro A vigente consulta estado A; TP02 recepción con RBAC registra un acceso A autorizado; TP03 límite de vigencia UTC/DST es reproducible; TP04 suspensión autorizada niega nuevos accesos; TP05 renovación con pago confirmado se aplica una sola vez; TP06 una entrada concurrente no duplica consumo; TP07 el operador ve auditoría mínima de su sede; TP08 Angular comunica estado y errores accesibles por teclado.

**TN01–TN12 negativos futuros:** TN01 ID/tenant B en sesión A; TN02 EmpresaId/sucursal falsificados; TN03 Cliente huérfano sin ownership; TN04 rol lector activa o suspende; TN05 JWT revocado; TN06 plan vencido o suspendido; TN07 reloj local manipulado/DST ambiguo; TN08 QR replay o credencial de otro usuario; TN09 doble check-in de cupo agotado; TN10 callback de pago no firmado; TN11 lector offline con timeout; TN12 auditoría con PII/token duradero. Todos **requieren rechazo fail-closed** si la operación no puede autorizarse. Ninguno fue ejecutado aquí como test real.

## Auditoría documental A0–A4 y revisión independiente A5

**A0:** preflight de fuentes canónicas, owner/Scope Lock, GitHub HEAD, DAG exacto, dos Markdown y provenance CHANGELOG. **A1:** comprobar R01–R14, S01–S08, D01–D08 y escenarios TP/TN no ejecutados; no inventar test PASS. **A2:** analizar aislamiento tenant A/B, JWT/RBAC, PII, QR, pagos, relojes y denegación por defecto. **A3:** vincular hechos de M01.03 a controles futuros sin editar API, Angular, MySQL o hardware. **A4:** publicación directa fast-forward con CAS, diff y blobs exactos, checks Scope Lock/CHANGELOG `completed/success` del SHA, readback en PLAN/COLA/BITACORA. Compilaciones .NET/Angular/seguridad sin cambios de runtime = `NO_APLICA` motivado, no PASS. **A5:** estrictamente otra Task Javier, distinta de la autora.

**Rollback:** `git revert` normal del commit con nuevos eventos BITACORA, sin borrar historial ni force-push. Sin migraciones, entidades de dominio, nuevos permisos, endpoints, dispositivos, proveedores, pagos, webhook, nuevos Jobs/Tasks, main/QA/PROD. M01.06 y M02 no quedan autorizadas sólo por este documento.
