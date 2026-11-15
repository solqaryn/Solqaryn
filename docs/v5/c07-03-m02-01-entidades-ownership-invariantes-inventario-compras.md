# SOLQARYN V16 — C07-03.M02.01: entidades, ownership, relaciones e invariantes de inventario y compras

**Microtarea:** C07-03.M02.01, PLAN_MAESTRO fila 1088. **Autor:** PRIMARY DEV 05 JAVIER (`6adc2e55f47c81919f6374e561687076`), exclusivamente `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT. **Riesgo V15:** R0 DOCUMENTAL, 2026-11-15.

**Autoridad única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), PLAN_MAESTRO/COLA/DAG_TECNICO. **Resultado exigido:** entidades, propiedad, relaciones e invariantes para almacenes, sucursales, reservas, compras, recepciones, transferencias, conteos, costeo, movimientos y conciliación, preservando el ERP y tenant. **No se crean entidades, tablas, EF migrations, endpoints ni cambios de stock**.

## 1. Precedentes y límites

`C07-03.M01.06` define bloqueos BL01–BL16, reversión RB01–RB10, salida SAL01–SAL12; `M01.03` inventaría 20 archivos de código real y 5 archivos de pruebas con blobs, sin acreditar ejecución. `M01.04` conserva riesgos R01–R12 hipotéticos, supuestos S01–S08 sin certificar y decisiones D01–D08 pendientes. `M01.05` tiene aceptación P01–P14/N01–N14 **futura, no ejecutada**. Al reclamar, PLAN H1088=PENDIENTE, I1088 vacío, COLA G1088/O1088=SI y DAG F1049=VALIDADO/G1049=SI.

Arquitectura real: Domain <- Application <- Infrastructure, API como composición. Los recursos existentes `Almacen`, `ReservaInventario`, `TransferenciaInventario`, `MovimientoInventario`, `OrdenCompra` y servicios relacionados están inventariados en M01.03, sin asumir que su mera existencia certifica aislamiento, transacción o cobertura. No introducir modelos paralelos que dupliquen entidades; esta matriz es contrato para refinar código **solo** en microtareas técnicas posteriores y CI auténtica. No cambiar JWT/RBAC, permisos, datos de Aiven, inventario real, finanzas, proveedores, frontend, secretos ni QA/main/PROD.

## 2. Doce entidades y relaciones de negocio E01–E12 (referencias conceptuales, no clases nuevas)

| ID | Dominio/relación | Propiedad/autorización requerida |
| --- | --- | --- |
| E01 | Empresa -> Sucursal | EmpresaId se obtiene server-side y se verifica antes de consultar |
| E02 | Sucursal -> Almacen | almacén pertenece a sucursal de la misma empresa; NULL legado no concede acceso global |
| E03 | Almacen -> Ubicacion | ubicación debe pertenecer a almacén autorizado y mismo tenant |
| E04 | ProductoVariante -> ExistenciaAlmacen | saldo por ámbito correcto; no sumar B en A |
| E05 | ReservaInventario -> ExistenciaAlmacen | reserva/consumo/liberación atómicos, nunca doble decremento |
| E06 | OrdenCompra -> Proveedor | clave idempotente, fingerprint de operación y autorización de empresa |
| E07 | OrdenCompra -> RecepcionCompra | saldo pendiente, parcialidad y reintento sin duplicación |
| E08 | RecepcionCompra -> MovimientoInventario | movimientos y kardex acoplados a transacción válida |
| E09 | TransferenciaInventario -> AlmacenOrigen/Destino | ambos extremos válidos, distintos y tenant-scoped |
| E10 | ConteoInventario -> AjusteBorrador | conteo no confirma ajuste ni modifica stock automáticamente |
| E11 | MovimientoInventario -> Kardex/Costeo | referencia, correlación y snapshot histórico sin pérdida |
| E12 | ReporteReconciliacion -> Saldos/Costos | datos del tenant y proyección de costos condicionada por permisos |

Son **nombres de relaciones referenciales**; cardinalidades/PK/FK, tables y migration design se determinan en M02.03 luego de leer el esquema vigente. Si una sucursal/almacén no tiene propiedad demostrable, cuarentena o error, nunca herencia permisiva.

## 3. Dieciséis invariantes INV01–INV16

| ID | Regla contractualmente exigida |
| --- | --- |
| INV01 | EmpresaId del usuario se valida backend con membresía/estado y no se confía en ID cliente |
| INV02 | Sucursal y almacén pertenecen a misma empresa, sin fallback cuando jerarquía es NULL |
| INV03 | Rol/permiso se evalúa por acción y recurso incluso si UI oculta botones |
| INV04 | Revocación de rol, sesión o membresía impide nuevas operaciones |
| INV05 | Reserva no excede disponibilidad, ni bloquea stock de otra empresa |
| INV06 | Doble reserva concurrente no consume saldo dos veces; prueba transaccional requerida |
| INV07 | OrdenCompra con misma Idempotency-Key y fingerprint no duplica estado |
| INV08 | Misma clave con fingerprint distinto falla sin escribir |
| INV09 | Recepción parcial calcula saldo sin exceder pendiente ni repetir movimiento |
| INV10 | Transferencia requiere origen/destino válidos y transición atómica o compensación probada |
| INV11 | Conteo deja ajuste borrador hasta autorización de confirmación |
| INV12 | Kardex/movimiento mantiene origen tipado, correlación y tenant verificable |
| INV13 | Costo histórico y redondeo se preservan mediante snapshot/contrato de política explícita |
| INV14 | Costos/PII no se exponen a rol sin Finanzas ni a tenant B en reporte/exportación |
| INV15 | Fallo a mitad de operación revierte saldo, movimiento, kardex y costos conjuntamente |
| INV16 | Auditoría minimizada conserva actor, tenant, operación, correlación y resultado sin secretos |

**No afirmar INV01–INV16 implementadas/probadas**. Las condiciones requieren pruebas de concurrencia/MySQL y autorización por recurso antes de cerrar cambios R2. La documentación tampoco certifica que todos los reportes y exportaciones sean seguros.

## 4. Diez relaciones y cardinalidades orientativas REL01–REL10

| ID | Relación conceptual | Condición |
| --- | --- | --- |
| REL01 | Empresa 1:N Sucursal | siempre scoped por empresa |
| REL02 | Sucursal 1:N Almacen | NULL/legacy analizado sin defaults globales |
| REL03 | Almacen 1:N Ubicacion | ubicación y permisos del mismo tenant |
| REL04 | Existencia 1:N Reserva | saldo disponible calculado transaccionalmente |
| REL05 | OrdenCompra 1:N Recepcion | recepciones no exceden saldo |
| REL06 | Recepcion 1:N Movimiento | idempotencia y correlación |
| REL07 | Transferencia 1:N Movimiento | extremos y estados consistentes |
| REL08 | Conteo 1:N LineaConteo | ajuste posterior autorizado |
| REL09 | Movimiento 1:N DetalleCosto | snapshot financiero histórico |
| REL10 | Empresa 1:N ReporteReconciliacion | costo redactado por permisos |

Cardinalidades son hipótesis, no ALTER TABLE. Verificar todas contra modelos EF/SQL reales antes de definir constraints.

## 5. Doce escenarios T01–T12 positivos/negativos FUTUROS_NO_EJECUTADOS

| ID | Positivo futuro | Negativo/fallo futuro |
| --- | --- | --- |
| T01 | Almacen A de Sucursal A visible a rol autorizado | token B no accede a Almacen A |
| T02 | Recurso con empresa validada opera | NULL legacy no aparece global |
| T03 | Reserva con saldo suficiente se compromete | dos reservas paralelas no sobreconsumen |
| T04 | OrdenCompra con key repetida responde idempotente | misma key distinto fingerprint => conflicto |
| T05 | Recepción parcial actualiza saldo transaccional | reintento no duplica kardex |
| T06 | Transferencia A entre dos almacenes propios | extremo tenant B u origen=destino falla |
| T07 | Conteo conserva borrador pendiente | confirmación sin permiso no ajusta stock |
| T08 | Movimiento conserva origen/correlación | operación parcial hace rollback |
| T09 | Reporte por empresa con permiso muestra costo | sin Finanzas costo queda censurado |
| T10 | Revocación impide escritura posterior | JWT/rol obsoleto no muta datos |
| T11 | Snapshot costo preserva historia válida | redondeo o versión corrupta no se impone |
| T12 | UI con 4xx/5xx no confirma éxito | red/offline no genera segunda mutación |

**Doce pares documentados; cero pruebas ejecutadas en esta microtarea.** Al implementar R2 se requerirá xUnit real, negativos tenant A/B, concurrencia y MySQL 8.4 efímero `Category=Integration`, build/Playwright si cambia UI, seguridad exact SHA. Un mock no acredita transacción MySQL ni acceso HTTP real.

## 6. Gates adaptativos, impacto y rollback

- **A0:** criterio M02.01 y precedentes M01.01–06, E01–12/INV01–16/REL01–10/T01–12; dos Markdown (este archivo y CHANGELOG) únicamente.
- **A1:** revisión de pertenencia, estado, replay, concurrencia y prueba negativa documental; sin afirmar ejecución.
- **A2:** JWT/RBAC, empresa/sucursal/almacén A/B, stock, costo, PII, revocación y fallback fail-closed contractuales; código intacto.
- **A3:** cotejo con inventario de fuentes de M01.03; NO_APLICA ejecutar MySQL/.NET/Angular/HTTP/Playwright por diff exclusivamente R0 documental.
- **A4:** publicar fast-forward/CAS en dev; Scope Lock y V5/procedencia `completed/success` exact SHA, BITACORA 26 columnas y readback PLAN/COLA/DAG.
- **A5:** revisión independiente por otra Task Javier con TASK_ID distinto; A5 pendiente hasta evidencia. Este LISTO documental no libera automáticamente M03, migraciones, QA/main/PROD.

**Reversión de este R0:** `git revert` normal y lectura HEAD, conservar BITACORA, no modificar existencias ni saldo, no force-push. Decisiones D01–D08 y bloqueos BL01–BL16 permanecen vigentes.
