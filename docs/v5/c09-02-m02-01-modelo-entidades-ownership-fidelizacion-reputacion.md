# SOLQARYN V17 — C09-02.M02.01: modelo conceptual de fidelización y reputación

**Microtarea:** `C09-02.M02.01`, PLAN_MAESTRO fila 1340. **Ejecutor:** PRIMARY DEV 01, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Riesgo V15:** R0, contrato documental sin runtime. **Estado de pruebas del presente artefacto:** no se ejecutan MySQL, API HTTP, Angular ni Playwright; escenarios posteriores explícitamente FUTUROS_NO_EJECUTADOS.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES (Drive ID `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`); PLAN_MAESTRO H/I, COLA G/O, DAG_TECNICO F/G; Skill canónica en dev, V15 calidad adaptativa y V17 candado atómico. **Objetivo exacto:** definir entidades, ownership, relaciones e invariantes para puntos, promociones, reseñas, respuestas y posventa; no construir tablas, endpoints, canjes, campañas ni procesos.

## 1. Fuentes y frontera de responsabilidad

- `docs/v5/c09-02-m01-01-alcance-fidelizacion-reputacion.md` (blob `1030c70296220626c5341f58d99e68e47b8a6cb2`): alcance e invariantes preliminares.
- `docs/v5/c09-02-m01-03-inventario-codigo-brechas-fidelizacion.md` (blob `b5bbb9d2c8e85814390bd12c85e5f4d6bd3bfed7`): doce rutas existentes y brechas no certificadas.
- `docs/v5/c09-02-m01-06-gates-rollback-liberacion-fidelizacion.md` (blob `0bfaffb03f54411a17557924b64fcb67451bc0ee`): puertas G01-G16, doce oráculos futuros, rollback y requisitos de A5.
- Tipos heredados observados: `Cliente`, `Venta`, `CreditoCliente`, `UsuarioEmpresa`, `RegistroAuditoria`, `TiendaCuentaCliente`. Son dependencias conceptuales, **no** autorización ni implementación de ledger. `Cliente.EmpresaId=NULL` y `Venta.EmpresaId=NULL` no constituyen prueba de pertenencia a ninguna empresa.
- Se conservan `Domain <- Application <- Infrastructure` y API como entrada HTTP; frontend Angular no concede permisos. Toda persistencia futura estará sujeta a MySQL 8.4, EF Core 10, migraciones y pruebas de aislamiento.

## 2. Dieciséis entidades CONCEPTUALES (no clases ni tablas creadas)

| Clave | Entidad lógica futura | Ownership y relación obligatoria |
| --- | --- | --- |
| E01 | ProgramaFidelidad | EmpresaId no nulo; configura programa por tenant, estado y versión |
| E02 | CuentaPuntos | EmpresaId + ClienteId autenticado, único por programa/cliente, sin saldo monetario |
| E03 | MovimientoPuntos | EmpresaId + CuentaPuntosId + causa + idempotency key, solo append/compensación |
| E04 | AcumulacionVenta | EmpresaId + VentaId y MovimientoPuntosId; una acreditación confirmada por versión |
| E05 | CanjePuntos | EmpresaId + CuentaPuntosId + beneficio, cantidad, versión y estado transaccional |
| E06 | BeneficioPromocion | EmpresaId + programa, ventana UTC, cupo y reglas versionadas |
| E07 | AplicacionBeneficio | EmpresaId + BeneficioPromocionId + ClienteId + uso idempotente |
| E08 | ReglaAntifraude | EmpresaId + política por versión, sin revelar parámetros sensibles al cliente |
| E09 | AlertaAntifraude | EmpresaId + referencia causal, prioridad y revisión por rol específico |
| E10 | Resena | EmpresaId + ClienteId + recurso/venta autorizados, texto saneado y visibilidad |
| E11 | DecisionModeracion | EmpresaId + ResenaId + moderador, motivo, versión y estado reversible |
| E12 | ApelacionModeracion | EmpresaId + DecisionModeracionId + solicitante y resolución auditable |
| E13 | CasoPosventa | EmpresaId + ClienteId + VentaId opcional verificado, estado y privacidad |
| E14 | RespuestaPosventa | EmpresaId + CasoPosventaId + autor con membresía vigente, trazabilidad |
| E15 | ConsentimientoCanal | EmpresaId + ClienteId + canal + propósito, opt-in/opt-out y revocación |
| E16 | EventoAuditoriaFidelidad | EmpresaId + actor, acción, entidad, correlación y metadatos minimizados |

Las referencias de un mismo cliente no pueden unir entidades de otra empresa. Los nombres anteriores son **propuestas contractuales**, no decisiones de esquema aplicadas.

## 3. Relaciones obligatorias

| Relación propuesta | Cardinalidad conceptual | Rechazo verificable futuro |
| --- | --- | --- |
| Empresa → ProgramaFidelidad | 1:N | programa de tenant B consultado desde A |
| ProgramaFidelidad → CuentaPuntos | 1:N | identidad de cliente sin ownership comprobado |
| CuentaPuntos → MovimientoPuntos | 1:N, append-only | actualización destructiva de historia |
| Venta → AcumulacionVenta | 1:0..1 por versión/cause | replay o venta anulada acredita |
| CuentaPuntos → CanjePuntos | 1:N transaccional | saldo negativo o doble canje concurrente |
| BeneficioPromocion → AplicacionBeneficio | 1:N, cupo controlado | promoción de B, caducada o agotada |
| Resena → DecisionModeracion | 1:N historial de decisiones | moderador de B cambia publicación de A |
| DecisionModeracion → ApelacionModeracion | 1:N auditable | apelación ignora revocación |
| CasoPosventa → RespuestaPosventa | 1:N, misma EmpresaId | respuesta filtra venta/cliente ajeno |
| Cliente → ConsentimientoCanal | 1:N por canal/propósito | revocación ignorada al reintentar |
| Todas las operaciones → EventoAuditoriaFidelidad | N:1 correlación lógica | PII o token en bitácora pública |

No imponer FK o índices definitivos desde M02.01: corresponden a M02.03 y deben probarse sobre proveedor real y datos sintéticos aislados.

## 4. Veinte invariantes de dominio y seguridad propuestos

| ID | Invariante verificable en futura implementación |
| --- | --- |
| INV01 | Toda entidad nueva tiene EmpresaId explícita, obligatoria e inmutable en su historial |
| INV02 | EmpresaId, ClienteId, VentaId y recurso se validan contra relación real y membresía activa |
| INV03 | EmpresaId=NULL heredado exige conciliación explícita, nunca asignación silenciosa |
| INV04 | Puntos, dinero, crédito comercial, facturas e inventario permanecen libros separados |
| INV05 | Una acreditación requiere venta confirmada, tenant igual y clave idempotente única |
| INV06 | Anulación/reversión crea movimiento compensatorio, nunca borra el anterior |
| INV07 | Canje, saldo y cupo se verifican en transacción con versión/concurrencia reales |
| INV08 | Saldo insuficiente o carrera no admite efecto parcial ni doble canje |
| INV09 | Promoción se limita por tenant, vigencia UTC, versión, cupo y segmento permitido |
| INV10 | La evaluación antifraude es determinista, auditable y fail-closed ante incertidumbre |
| INV11 | JWT válido no sustituye RBAC granular, recurso, membresía ni revocación |
| INV12 | Cliente A no accede a saldos, beneficios, reseñas o posventa de empresa B |
| INV13 | Reseña debe verificar autor/recurso y sanitizar contenido antes de publicar |
| INV14 | Moderación usa permiso vigente, motivo y versión, con decisión reversible |
| INV15 | Apelación no puede autoaprobarse ni suprimir historial de moderación |
| INV16 | Mensajería exige consentimiento vigente al envío y en cada reintento |
| INV17 | Opt-out/revocación invalida colas/cache y evita contactos posteriores |
| INV18 | Errores, logs, exportaciones y caché omiten tokens, PII y datos cross-tenant |
| INV19 | Eventos tienen correlationId y claves idempotentes acotadas al tenant y operación |
| INV20 | Baja del programa revoca operaciones nuevas preservando trazabilidad, ventas y créditos |

No se habilita ninguna función basándose en esta tabla. La revisión futura puede rechazar una invariante incompatible con el modelo real y exigir corrección.

## 5. Doce pares de aceptación futura — NO EJECUTADOS

| Par | Positivo futuro | Negativo/falla cerrada futura |
| --- | --- | --- |
| P01/N01 | Cliente A consulta su CuentaPuntos A | Cliente B o legacy NULL intenta verla |
| P02/N02 | Venta confirmada genera un crédito de puntos | Replay/venta anulada genera doble crédito |
| P03/N03 | Reversión compensa ledger sin destruir historia | Borrado o crédito monetario confundido |
| P04/N04 | Canje con saldo/version correctos se confirma una vez | Canje paralelo/insuficiente tiene efecto parcial |
| P05/N05 | Promo A en UTC/cupo correcto se aplica | Promo B/expirada/sin cupo se acepta |
| P06/N06 | Antifraude marca evento justificado con actor | Reglas ausentes producen aceptación silenciosa |
| P07/N07 | Cliente legítimo publica reseña saneada | HTML/PII/suplantación se publica |
| P08/N08 | Moderador A autorizado registra decisión y motivo | Lector, revocado o moderador B modifica |
| P09/N09 | Apelación válida preserva decisión original | Autor aprueba apelación propia sin reviewer |
| P10/N10 | Caso de posventa accede a Venta A legítima | IDOR de ticket/venta B filtra datos |
| P11/N11 | Consentimiento vigente permite canal A | Opt-out, retry o cambio de canal envía |
| P12/N12 | Baja conserva libros y bloquea nuevos puntos | Hard-delete o nuevo canje después de baja |

**Ejecución en este cambio:** 0/12 pruebas de runtime realizadas; cobertura y tasas de fallo en runtime **NO_MEDIDO**. Estas pruebas requieren implementación M03/M05, CI xUnit y MySQL 8.4 efímero cuando corresponda, y Angular/Chromium sólo si cambia UI.

## 6. Impacto, dependencias, gates y rollback

- **Impacto real del cambio R0:** únicamente este documento y una sección autocontenida de `CHANGELOG_AI.md`. No cambia Domain, Application, Infrastructure, API, Angular, datos, migraciones, secretos, configuración, CI o servicios. No ejecuta cobranzas, descuentos, puntos, campañas ni mensajes.
- **Preparación M02.02–05:** cada ID debe diseñar sus propios contratos de API/DTO, persistencia, flags y eventos; esta entrega no se considera A5_PASS ni libera M03.
- **Gate M03/M05:** A0 requisitos/diff; A1 pruebas funcionales con negativos capaces de fallar; A2 tenant A/B, JWT/RBAC, revocación; A3 transacciones MySQL 8.4 EF si se modifican datos, API/Playwright según superficie; A4 Scope Lock/V5 y CI exact-SHA o equivalencia probada; A5 independiente por una Task Javier distinta. Cualquier check FAIL/CANCELLED/SKIPPED/IN_PROGRESS requerido es NO_PASS.
- **Riesgos:** concurrencia/ledger, fraude, privacidad, moderación, IDOR/tenant, consentimiento y replays son **R2 futuros**, sin declaración de seguridad runtime presente.
- **Publicación M02.01:** candado V17 `SQV17_C09_02_M02_01` fila 1340 (`2000001340`), único owner, BITACORA idempotente y readbacks; A5 pendiente de revisor Javier distinto. El prefiltro institucional no sustituye revisión de SHA.
- **Rollback:** revertir cambios Git con commit inverso ordinario tras verificar HEAD, sin force-push, migraciones ni alteración de BITACORA histórica. Un A5_FAIL obliga reparación aditiva y revisión independiente; no editar fórmulas de COLA/DAG.

**Conclusión limitada:** modelo conceptual de entidades/ownership/relaciones y veinte invariantes, sin API, ledger, puntos, promociones, moderación o posventa operativas. `PROJECT_SCOPE_LOCK=STRICT`, sin QA/main/PROD ni permisos productivos.
