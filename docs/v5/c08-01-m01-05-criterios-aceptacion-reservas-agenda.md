# SOLQARYN V5 — C08-01.M01.05 — Criterios observables de aceptación de reservas y agenda

**Autor:** SOLQARYN PRIMARY DEV 05 — JAVIER (`6adc2e55f47c81919f6374e561687076`). **Fecha:** 2026-11-14. **Fuente única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, C08-01, M01.05. **Ámbito:** preparación documental de aceptación de citas, horarios, disponibilidad, personal, recursos y cancelaciones. **No se implementó ni probó aquí una agenda real.**

## 1. Resultado exigido, fuentes y límites de la evidencia

La salida institucional de M01.05 es definir **aceptación funcional y técnica observable**, con especial atención a conflictos de horario, zona IANA/UTC y autorización. Este documento define oráculos y evidencia requerida para las fases futuras, no pruebas que hayan pasado. La disponibilidad del DAG documental no autoriza activar JWT, cambiar RBAC, ejecutar migraciones MySQL, crear workers/eventos, integrar un proveedor de calendario, enviar notificaciones ni desplegar.

Fuentes verificadas en `dev`: M01.01 `docs/v5/c08-01-m01-01-alcance-reservas-agenda.md` (blob `26805ab07dd3f20cf986b6da4ebc24fb702fa8f6`, invariantes NR01–NR12); M01.02 `docs/v5/c08-01-m01-02-actores-interfaces-reservas-agenda.md` (blob `912cd97789de5faaacbe9e9abef1b5553a49a009`, A01–A14/I01–I14/F01–F07); M01.03 `docs/v5/c08-01-m01-03-inventario-fuentes-brechas-reservas-agenda.md` (blob `0394cb17b01c6d676d8664d5c1f1df8ae729d551`, G01–G10); y M01.04 `docs/v5/c08-01-m01-04-riesgos-supuestos-decisiones-reservas-agenda.md` (blob `18ed57e8807a175faec8ddddaf7e2e4c2cd66061`, R01–R16/S01–S08/D01–D08 pendientes).

Preflight individual: `PLAN_MAESTRO!H1158=PENDIENTE/I1158=vacío` antes del CAS, `COLA!G1158/O1158=SI`, `DAG_TECNICO!A1119=C08-01.M01.05,F1119=VALIDADO,G1119=SI`. Los antecedentes C01/C02/C06 que permitieron M01 preparatorio no sustituyen los gates terminales M01.06/M02. M01.04 recién publicado conserva A5 pendiente; el diseño no atribuye su A5 a esta Task ni asume QA.

## 2. Matriz AC01–AC12: oráculos positivos y negativos FUTUROS NO_EJECUTADOS

Cada criterio requiere **prueba ejecutada en fase de implementación**, resultado binario verificable, SHA exacto, fixture/control de tenant y evidencia de rollback cuando aplique. Ninguna fila está marcada PASS runtime.

| ID | Positivo observable futuro | Negativo obligatorio futuro | Evidencia/gate exigible |
| --- | --- | --- | --- |
| AC01 — Publicación | Empresa A publica servicio, sede y franjas visibles autorizadas | Host/ID de B no filtra horarios privados ni PII | HTTP A/B, filtrado por servidor, 403/404 |
| AC02 — Membresía | Profesional A activo lee sólo su agenda/sede permitida | JWT revocado, rol lector o usuario de B no opera citas | JWT/RBAC real, pruebas positivas y negativas |
| AC03 — Capacidad | Una reserva de recurso capacidad uno confirma en hueco libre | 64 peticiones concurrentes no confirman doble ocupación | CI .NET + MySQL efímero, readback único |
| AC04 — Turnos | Calendario aplica turnos, descanso, feriados y ausencias | Turno cerrado o profesional ausente jamás expone slot disponible | Fixtures horarios y negación de disponibilidad |
| AC05 — Duración | Servidor reserva duración y buffers canónicos | Payload manipulado de duración/precio/capacidad no altera ocupación | Tests Application/API y esquema DTO |
| AC06 — Zona | UTC de cita conserva hora local y zona IANA de sede | Instante DST inexistente o duplicado sin desambiguación falla | Casos deterministas de UTC/IANA/DST |
| AC07 — Replay | Reintento idéntico conserva una única cita y resultado | Misma clave idempotente con payload distinto rechaza sin mutar | Transacción durable, clave ligada a tenant/actor |
| AC08 — Reprogramación | Actor autorizado cambia cita a nuevo recurso/hueco válido de forma atómica | Versión obsoleta o conflicto deja cita original consistente | MySQL rollback e integración HTTP |
| AC09 — Cancelación | Cancelar propia cita válida libera slot una vez y audita | Repetir cancelación/estado terminal no libera dos veces | Tests máquina de estados + auditoría |
| AC10 — Privacidad | Proyección pública contiene sólo campos expresamente autorizados | Exportación/log/UI no expone JWT, teléfonos o cita ajena | Sanitización, anti-IDOR, security tests |
| AC11 — UX accesible | Teclado/lector/320px muestran carga, error y estado confirmado por servidor | Timeout no presenta éxito optimista ni pierde recuperación idempotente | Angular/Playwright accesibilidad y red lenta |
| AC12 — No regresión ERP | Flujo de citas no altera stock, venta, pedido o factura existente | Cancelación/replay horario no modifica `ReservaInventario` | Regresión combinada de inventario y agenda |

**Convención:** el oráculo positivo necesita assert explícito; el negativo requiere un estado final del servidor/DB verificable, no sólo un mensaje UI. `SKIPPED`, `CANCELLED`, `FAILURE`, `QUEUED` o `IN_PROGRESS` **no son PASS** si el check es obligatorio. Los IDs AC son contratos de prueba propuestos, no test methods ni jobs ya presentes.

## 3. Requisitos no funcionales NFR01–NFR08 y pruebas contra abuso

- **NFR01 — Aislamiento:** en todas las mutaciones, tenant A/B y sucursal deben resolverse de contexto confiable; búsquedas por ID cruzado no revelan existencia ni estado privado.
- **NFR02 — Concurrencia:** confirmar exclusión de doble ocupación con múltiples conexiones/transacciones MySQL; un semáforo local en una instancia no es prueba suficiente.
- **NFR03 — Tiempo:** almacenar instante UTC y zona IANA validada; no usar `DateTime.Now` del browser como autoridad ni asumir que todas las sedes están en Tegucigalpa.
- **NFR04 — Accesibilidad:** navegación con teclado, foco visible, lector de pantalla, mensajes recuperables y diseño a 320px sin confirmar por anticipado.
- **NFR05 — Observabilidad:** logs/auditoría con correlación, tenant, actor, operación, resultado y versión; nunca JWT, credenciales o PII excesiva.
- **NFR06 — Resiliencia:** retry con idempotencia, timeout, cancelación transaccional y fallo entre insert/ocupación sin inconsistencias durables.
- **NFR07 — Compatibilidad:** contratos de sucursales, clientes, stock reservado, documentos y facturación existentes conservan comportamiento probado.
- **NFR08 — Reversibilidad:** diseño y migraciones futuras deben permitir rollback sin borrar citas legítimas ni romper estados históricos; no se ejecuta hoy.

Los mecanismos de rate limiting, selección de proyección pública y duración máxima deberán definirse en M02/M03 con pruebas de abuso; no se inventan umbrales, proveedores ni SLAs aquí.

## 4. Condiciones de evidencia y NO_GO de salida

| Capa futura | Evidencia mínima para reclamar implementación | Bloqueo obligatorio |
| --- | --- | --- |
| Domain | Agregado/estados/invariantes y tests deterministas positivos/negativos | Ownership o máquina de estados ambiguos |
| Application | Casos de uso con contexto tenant/rol/sucursal y contratos tipados | Permisos resueltos desde client payload |
| Infrastructure/MySQL | Entidades/migración versionada, índices/versión y transacciones rollback con servicio efímero | No demostrar no solapamiento concurrente |
| API/JWT/RBAC | Policies backend, host/publicación seguros, A/B e IDOR | Cualquier ruta permisiva, divulgación o auth UI-only |
| Angular | Estados accesibles, zona clara, respuesta real y errores recuperables | Confirmación ficticia al timeout |
| CI/cierre | Builds, pruebas pertinentes, Scope Lock exact-SHA; BITACORA/PLAN/COLA readback | Check obligatorio no concluido SUCCESS |

**NO_GO** explícito cuando no pueda observarse un negativo, falte certeza de zona/tenant/permiso, exista solapamiento concurrente, se requiera acceso real a proveedor/datos productivos o la única evidencia sea documental. No publicar QA/main/PROD por un LISTO_DEV ni por A5 documental.

## 5. Trazabilidad de decisiones D01–D08, riesgos y revisión

- **AC01/02/10, NFR01/05:** M01.04 R01/R02/R11/R14 y D01/D06; M01.01 NR01/NR02/NR09; M01.02 F01/F02/F06.
- **AC03/04/05/08/09, NFR02/06:** M01.04 R03/R04/R05/R09/R10 y D02/D03/D07; M01.03 G02/G03/G04/G07; M01.01 NR03–NR05/NR07–NR08.
- **AC06, NFR03:** M01.04 R06/R07/D04; M01.03 G05; M01.02 I11/F03.
- **AC07:** M01.04 R08/D07, M01.02 I12, M01.01 NR08.
- **AC11, NFR04:** M01.04 R13, M01.03 G08, M01.01 NR11.
- **AC12, NFR07/08:** M01.04 R12/R15/R16, M01.03 G09/G10 y M01.01 NR10/NR12.

**D01–D08 siguen PENDIENTES** hasta diseño, permisos y pruebas; el documento no los decide ni presupone ejecución de workers/eventos. Si las pruebas futuras contradicen supuestos, conservar historial y registrar revisión causal nueva sin alterar los eventos previos.

## 6. Auditoría A0–A5 y rollback documental

- **A0:** confirmar identidad del plan, `dev`, scope lock exacto, DAG/estado/owner por CAS, diff de sólo Markdown, fuentes y ausencia de secretos/encabezados vacíos.
- **A1:** verificar que AC01–AC12 y NFR01–NFR08 cubren positivo+negativo observable y oráculos de M01.01–04; esto es revisión **documental**, no resultados de tests ejecutados.
- **A2:** revisión estática del diseño tenant A/B, JWT/RBAC, IDOR, privacidad, DST, concurrencia y fail-closed; no acreditar HTTP ni base de datos real.
- **A3:** coherencia de fuentes y arquitectura Domain–Application–Infrastructure–API/Angular; .NET, npm/Playwright y MySQL son `NO_APLICA` al cambio exclusivamente Markdown, jamás PASS runtime.
- **A4:** publicar en `dev` por fast-forward con SHA/tree/blob, obtener `completed/success` de `verify-solqaryn-project-scope` y control `V5 - Sin encabezados vacios ni atribucion cruzada` sobre SHA exacto, registrar BITACORA A:Z y verificar PLAN/COLA.
- **A5:** revisión posterior por una Task Javier distinta de `6adc2e55f47c81919f6374e561687076` antes de cualquier promoción diferenciada.

Rollback: `git revert` normal del documento y su entrada de `CHANGELOG_AI.md`, registro compensatorio nuevo, sin force-push, borrado de BITACORA ni intervención QA/main/PROD. Esta entrega no modifica código, datos, roles, cuentas, integraciones, URLs, eventos, facturación ni Task schedules.
