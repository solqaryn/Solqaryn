# SOLQARYN V5 FINAL — C07-04.M02.06: dependencias técnicas y contrato de salida del lote pedidos/pagos

**Fecha:** 2026-11-14 (America/Tegucigalpa) · **Autor:** PRIMARY DEV 01, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a` · **Repositorio:** `solqaryn/Solqaryn@dev` · **PROJECT_SCOPE_LOCK:** STRICT.

## 1. Autoridad, límites y no-alcance

Fuente ejecutable única: `STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES` V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; `PLAN_MAESTRO!A1129:K1129`, `COLA!A1129:O1129`, `DAG_TECNICO!A1090:J1090`. El objetivo de esta microtarea es **documentar y verificar el contrato de salida de M02**, no materializarlo. Preflight: PLAN H1129=PENDIENTE, I1129 vacío, COLA G/O=SI, DAG F=VALIDADO/G=SI. Tras CAS exclusivo, PLAN H1129=EN_PROGRESO e I1129=TASK_ID propio.

**NO IMPLEMENTADO / NO EJECUTADO:** este documento no añade endpoints, DTO .NET, entidades EF, tablas, índices, migraciones, SQL, pagos, PSP, eventos, colas, webhooks, dispatcher, workers, facturación, conversiones de pedido ni cambios de configuración, secretos, infraestructura o datos reales. Las pruebas positivas y negativas descritas son oráculos FUTUROS_NO_EJECUTADOS. No autoriza QA/main/PROD ni cambios productivos. Sólo se pretende publicar este Markdown y una sección autocontenida en `CHANGELOG_AI.md`.

Stack preservado: ASP.NET Core 10, Angular 22.2.1, MySQL 8.4 como referencia de pruebas efímeras, EF Core 10.0.12/Oracle MySql.EntityFrameworkCore 10.0.9; Domain ← Application ← Infrastructure, con API como borde. JWT Bearer, RBAC relacional, pertenencia tenant derivada del servidor, aislamiento A/B, auditoría, límites de tasa y protección de secretos continúan obligatorios.

## 2. Matriz de procedencia y A5 de los predecesores

| Prerrequisito | Entrega documental delimitada | SHA de origen | Comprobación independiente |
| --- | --- | --- | --- |
| C07-04.M01.01–06 | Alcance, riesgos, aceptación y puerta de salida M01 | Estados LISTO y A5 de las seis unidades en PLAN | A5 independiente y readback; no certificación económica ni fiscal |
| C07-04.M02.01 | Modelo lógico de entidades, ownership y catorce invariantes | `198bc58f28784800656633f7c2b4da0d8d02b74a` | A5_PASS independiente J00, blob `419ffd807a1ffa7d7364690130a3707f8cb7ec6c` |
| C07-04.M02.02 | Diez operaciones, doce DTO, catorce errores y compatibilidad de API propuesta | `02634cdbeee7903e849ccec1594f62c89f3d06e9` | A5_PASS independiente J00, blob `a61fbf7796b6f67fcc4ef32c7f0da3f25bfa4188` |
| C07-04.M02.03 | Persistencia lógica, diez índices hipotéticos, ocho pasos reversibles y fixtures | `979447713e17593ab6ebf683df9b75ae585e6f0d` | A5_PASS independiente J00, blob `08d1148b78b78f89bc2fb656942f28c1479ddcb7` |
| C07-04.M02.04 | Configuración tipada conceptual, dieciocho parámetros y catorce validaciones | `e678877ff8af25b5fa4890310af0f41b0396553e` | A5_PASS independiente J00, blob `c2993ff06f4a5fb20637fbc5672b756ab5edb032` |
| C07-04.M02.05 | Catorce eventos conceptuales, idempotencia y límites, sin emitir ninguno | `64dbf929a4ba49f74bfb9655e261ae28c203fbcd` | A5_PASS independiente J00, blob `9b17565598f8c48d490fde377ed2e7d1b617689a` |

Los cinco A5 de M02.01–05 corresponden a revisión real de PRIMARY DEV 01 distinta de PRIMARY DEV 06 (autora), con readback en PLAN K1124:K1128, eventos independientes BITACORA A:Z y recálculo DAG G1090=SI / COLA G/O1129=SI **antes** de la reclamación de M02.06. Los checks Scope Lock y procedencia V5 se verificaron SUCCESS en cada SHA fuente; las suites runtime canceladas en fuentes documentales no se contabilizan como PASS. Este registro no es A5 de la propia M02.06.

## 3. Contrato de salida técnica OUT01–OUT16

| ID | Condición obligatoria para una futura implementación | Evidencia requerida antes de efecto real |
| --- | --- | --- |
| OUT01 | El identificador de empresa procede de JWT y membresía verificadas en backend | HTTP A/B positivo/negativo y rechazo de ID inyectado |
| OUT02 | Cotización, cliente, producto, variante, pedido y factura comparten owner persistido | Tests EF/MySQL efímero con mezclas A/B y datos legado NULL |
| OUT03 | Cotización aceptada sólo se convierte a un pedido una vez | Idempotencia durable con carreras y reintentos |
| OUT04 | Estados y versiones de pedido, venta, factura y pago se validan server-side | Concurrencia expectedVersion y negativos de estados obsoletos |
| OUT05 | Totales, impuestos, moneda, descuentos y saldos usan decimales y redondeo definido | Fixtures deterministas, sin pérdida de centavos |
| OUT06 | Facturas mantienen snapshot fiscal y correlativo legal autorizado | Revisión por jurisdicción, emisor/sucursal y pruebas de duplicidad |
| OUT07 | Ningún cobro opera sin PSP, contrato, credenciales y permiso explícitos vigentes | Denegación por proveedor ausente, firma inválida y rol revocado |
| OUT08 | Una confirmación de pago no se aplica dos veces por webhook o replay | Idempotencia durable y transacción sin doble cargo |
| OUT09 | Stock/reserva/caja no se confirman parcialmente | Rollback/compensación ante excepción y reintento concurrente |
| OUT10 | Cancelaciones y reembolsos preservan historia y motivo | Reversión autorizada, no hard-delete y no segundo reembolso |
| OUT11 | Exportaciones respetan permisos, pertenencia, TTL y revocación | Negativos de URL vencida, recurso de B y payload con PII |
| OUT12 | Campos históricos EmpresaId NULL no se adscriben heurísticamente | Migración de reconciliación revisada, fail-closed en lectura/escritura |
| OUT13 | Reglas de flags son restrictivas: DENY/UNKNOWN prevalecen | Test de override permisivo y acceso posterior a revocación |
| OUT14 | Las notificaciones son proyecciones de transacciones confirmadas | No emitir eventos antes de commit, orden causal y bounded retries |
| OUT15 | Logs/errores no exponen JWT, secretos, PAN/CVV o datos de tenant ajeno | Escáner y pruebas de redacción / error-envelope estable |
| OUT16 | CI, auditorías A0–A5, Scope Lock y dependencias son obligatorios por SHA | Check-runs pertinentes completed/success y readbacks institucionales |

Las condiciones OUT01–OUT16 son **exigencias para M03 y posteriores**, no pruebas superadas por M02.06. Las integraciones MySQL/HTTP/PSP requieren autorizaciones adicionales específicas; A5 documental nunca suplanta seguridad runtime.

## 4. Matriz de rechazo y recuperación FUT01–FUT12 — NO_EJECUTADOS

| Caso | Camino positivo futuro | Negativo / rollback requerido |
| --- | --- | --- |
| FUT01 | Cotización A válida produce pedido único | Cliente B en A: 403/404 sin escritura ni datos B |
| FUT02 | Factura propia conserva snapshot fiscal válido | Factura con emisor no autorizado o RTN incorrecto: no emitir |
| FUT03 | Pago autorizado actualiza saldo y caja una vez | Replay o mismo key con importe diferente: no segundo cobro |
| FUT04 | Reserva de producto propio reduce stock una vez | Carrera o stock insuficiente: cancelar efectos parciales |
| FUT05 | Operación con expectedVersion correcta avanza | Versión anterior: conflicto y estado intacto |
| FUT06 | Cancelación con permiso guarda motivo y compensación | Rol revocado o tenant B: no anular ni borrar |
| FUT07 | Exportación de factura propia sirve datos permitidos | Enlace vencido o factura B: cero bytes expuestos |
| FUT08 | Se acepta configuración restrictiva del servidor | Flag cliente/plan desconocido no habilita pagos |
| FUT09 | Evento asociado a transacción confirmada tiene causación estable | Fallo de DB no emite ni procesa evento |
| FUT10 | Callback auténtico se concilia idempotentemente | Firma no válida, PSP no autorizado o doble entrega: denegar |
| FUT11 | Registro legado reconciliado con evidencia humana queda aislado | EmpresaId NULL o asociación inferida por URL: denegar |
| FUT12 | Prueba de rollback en MySQL efímero preserva consistencia | Error a mitad de operación: cero factura/pago/stock huérfanos |

Estos son casos de aceptación futura, **NO pruebas ejecutadas**. No afirmar HTTP 200/403, MySQL 8.4, pentesting, WCAG, RPO/RTO ni integración PSP a partir de tablas Markdown.

## 5. Dependencias, orden y puerta al siguiente lote

- **D01:** conservar M01.01–06 y M02.01–05 LISTO+A5 independientes comprobados como antecedentes de M02.06; no editar gates para compensar una falta.
- **D02:** completar M02.06 con A0–A4 documentales verdaderas y check Scope Lock más procedencia V5 completed/success sobre SHA publicado, y readback de PLAN/COLA/DAG/BITACORA. No confundir compilaciones runtime de SHA distinto ni CANCELLED/SKIPPED con PASS obligatorio.
- **D03:** dejar M02.06 en LISTO_DEV con A5_PENDIENTE para otra Task Javier. La revisión independiente requiere comprobar diff, blob, SHA, dependencias, límites de seguridad y eventos; no puede realizarla su autor J00.
- **D04:** **M03 no se libera hasta que el DAG vigente calcule G=SI y COLA G/O=SI**, y se confirme H=PENDIENTE/I vacío y permisos/Scope Lock. M03 implica código real .NET/Angular/MySQL y CI causal; no se suple con nuevos documentos.
- **D05:** las decisiones comerciales/fiscales de M01 y proveedores PSP permanecen abiertas cuando corresponda; ninguna de estas declaraciones contrata, habilita pagos ni migraciones.
- **D06:** si una prueba pertinente sigue queued/in_progress, mantener únicamente la microtarea afectada VALIDANDO, no fabricar LISTO; continuar trabajo independiente bajo owner exclusivo y CAS.

El gate de salida de **esta** microtarea es la consistencia verificable de documentación, decisiones pendientes y evidencia de seguridad estática. No es una aprobación de ventas, pagos o despliegue en producción.

## 6. Auditorías A0–A5 y rollback de este changeset

- **A0 estática:** ID en fuentes actuales; verificar OUT01–OUT16, FUT01–FUT12, tabla de seis antecedentes y un diff de únicamente dos Markdown sin secretos ni código runtime.
- **A1 funcional documental:** inspección de la coherencia de doce positivos y doce negativos propuestos; `NO_APLICA` a ejecución HTTP/DB/PSP por este cambio exclusivamente documental.
- **A2 seguridad:** mantener límites de autorización, pertenencia tenant A/B, JWT/RBAC, idempotencia, fiscalidad, secretos y denegación fail-closed. No atribuir prueba de aislamiento vivo.
- **A3 integración:** contrastar modelos propuestos con las cuatro fuentes de código Git ya citadas por M02.01/03 y arquitectura oficial; sin DDL, DTO ni endpoint modificado. Runtime build/integration `NO_APLICA` como causal del presente diff Markdown.
- **A4 publicación:** commit fast-forward esperado sobre HEAD de `dev`, CHANGELOG con encabezado y viñetas juntos; comparar diff/blob, verificar Scope Lock y V5 checks exact-SHA `completed/success`, PLAN/COLA/DAG/BITACORA readback después de cada transición.
- **A5 independiente:** `A5_PENDIENTE`, exclusivamente otro TASK_ID entre las otras cinco Tasks Javier; nunca autoaprobación ni promoción implícita.

**Rollback:** revert normal del commit documental y, si afectó el registro de estados, nuevo evento compensatorio append-only en BITACORA con readback; nunca force-push, reset destructivo, edición de historia, cobros o cambios de proveedor.
