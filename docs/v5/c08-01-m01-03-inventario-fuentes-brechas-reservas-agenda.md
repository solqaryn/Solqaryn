# SOLQARYN V5 — C08-01.M01.03 — Inventario real de fuentes y brechas de agenda

**Autor:** PRIMARY DEV 03 — JAVIER (TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`). **Fecha:** 2026-11-14. **Fuente institucional:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, C08-01 (Reservas y agenda), M01.03. **Ámbito:** inventario documental verificable sobre `dev`, sin cambios runtime ni certificación de reservas.

## 1. Método, alcance y límite de lo observado

La M01.03 exige **leer contratos, código y evidencia disponibles sin asumir certificación**. Se consultó el árbol de archivos actual de `backend/src/Domain/Entities` (100 entradas), `backend/src/Application/Interfaces` (174), `backend/src/Application/Services` (95), `backend/src/API/Controllers` (99) y `frontend/src/app/features` (50), además de archivos representativos de inventario, sucursales y rutas. Se buscaron nombres que incluyeran agenda, cita, horarios, servicio, calendario, personal, sucursal y reservas. La ausencia de un nombre en estas rutas **no demuestra** ausencia absoluta en todos los proyectos, migraciones, tests, ramas, código generado o bibliotecas; tampoco certifica defectos runtime.

El antecedente `C08-01.M01.01`, blob `26805ab07dd3f20cf986b6da4ebc24fb702fa8f6`, definió NR01–NR12 y escenarios P01–P08/N01–N12 **FUTUROS NO_EJECUTADOS**. La preparación propia `C08-01.M01.02` (blob `912cd97789de5faaacbe9e9abef1b5553a49a009`) describió actores A01–A14 e interfaces I01–I14, **sin implementarlas**. Ninguna de ellas es evidencia de aceptación final M03/M05 ni de aprobación A5.

## 2. Inventario de fuentes exactas (observaciones acotadas)

| Código / contrato | Git blob SHA revisado | Hallazgo verificable y límite |
| --- | --- | --- |
| `backend/src/Domain/Entities/ReservaInventario.cs` | `9e097bbf04d5c2e0bca61426ea673b2306efe7d9` | Tiene estados `Borrador`, `Activa`, `Consumida`, `Liberada` y transición de documento vinculado a `Venta`/`PedidoVenta`. Gestiona stock, **no** bloquea calendarios de personal |
| `backend/src/Application/Services/ReservaInventarioService.cs` | `c8efe2fc5becc0edcca7981e710106004611f350` | La propia descripción especifica `StockReservado`, `ExistenciaVariante`, lock pesimista y auditoría de reserva de **inventario**. No convertir ese lock en evidencia de exclusión temporal para citas |
| `backend/src/Application/Interfaces/IReservaInventarioService.cs` | `491751b7e6030a69a89cd3e8e97dfa94cd23470e` | Contratos `CreateAsync`, `ActivarAsync`, `ConsumirAsync`, `LiberarAsync`, `ExpirarAsync`, `CancelarAsync` para inventario, no para citas |
| `backend/src/API/Controllers/ReservasInventarioController.cs` | `3cbddb32ea5696028eea5ac5f78e3f78a5971e89` | Ruta `reservas-inventario`, `[Authorize]` y `RequierePermiso(ModuloSistema.MovimientosInventario,...)`; no es API de disponibilidad/calendario |
| `backend/src/Domain/Entities/Sucursal.cs` | `8746b11fa30cdac0b97241b6221100bedd813b35` | `EmpresaId` puede ser nullable por compatibilidad histórica; `ObtenerEmpresaIdTenant()` exige positivo y falla cerrado; una agenda futura debe validar sede y tenant, no confiar en un ID de cliente |
| `backend/src/API/Controllers/SucursalesController.cs` | `bc6a469d19f2bb883205dd7cdec7e18ce72e69c1` | Usa `TenantPermissionContext.GetAuthorizedEmpresaId(HttpContext)`, `[Authorize]` y permisos de sucursal; referencia de patrón actual, **no prueba** permisos de cita futuros |
| `frontend/src/app/app.routes.ts` | `2f560c14693455b79ac4546e9333fb74586ef234` | Incluye rutas de `tienda`, `login` y guardias; el inventario selectivo no identifica rutas nombradas `citas`/`agenda`; no inferir ausencia universal ni Playwright certificado |
| Árbol `frontend/src/app/features` | Lectura de directorio de 50 entradas, no blob de un archivo | En esa lectura aparecen `clientes` y `sucursales`; ningún directorio con nombre `citas`, `agenda` o `reservas-horarias`. No equivale a búsqueda exhaustiva de todas las aplicaciones |
| Árbol `backend/src/Domain/Entities` | Lectura de directorio de 100 entradas, no blob de un archivo | Coinciden `ReservaInventario`, `Sucursal`, `Cliente`; ningún nombre directo `Cita` o `Agenda` observado. No equivale a auditoría de toda la BD |

Los SHA anteriores son **blobs**, no hashes de pruebas ni evidencia de despliegue. El HEAD debe reevaluarse antes de cambios posteriores. El inventario no ejecutó solicitudes HTTP, MySQL, xUnit, Angular, Playwright ni workers.

## 3. Brechas G01–G10 y riesgo causal, pendientes de contrato y pruebas

- **G01 — Identidad de cita:** no se evidenció en los archivos selectivos una entidad independiente con `EmpresaId`, `SucursalId`, servicio, profesional, inicio/fin UTC y estado; diseñar M02 con ownership explícito.
- **G02 — Servicio y duración:** no se comprobó un catálogo de duración/buffer de cita. Los productos o el stock no dan automáticamente reglas de ocupación temporal.
- **G03 — Disponibilidad/concurrencia:** lock de `StockReservado` no demuestra no-solapamiento de citas; exigir transacción MySQL efímera y doble solicitud simultánea.
- **G04 — Horarios y ausencias:** falta inventariar turnos, descansos, feriados y ausencias por profesional/recurso; no confundir con estados de sucursal.
- **G05 — UTC/IANA y DST:** falta prueba real de hora ambigua/no existente, zona de sede frente a zona del visitante y conversiones deterministas.
- **G06 — Tenancy A/B e IDOR:** los filtros de inventario/sucursal no bastan para autorización de una API de agenda; exigir 403/404 y no divulgación de disponibilidad privada.
- **G07 — Idempotencia y transición:** falta diseño durable de clave, estado, replay, cancelación y reprogramación atómicas con rollback.
- **G08 — UI y accesibilidad:** falta prueba real de foco, teclado, 320px, errores y confirmación contra respuesta del servidor; contratos Angular futuros no son PASS.
- **G09 — Auditoría y notificaciones:** faltan especificación de evento post-commit, logs sanitizados y prueba de no exposición de PII/JWT; no activar SMTP.
- **G10 — Integraciones y migración:** no se acreditó ninguna migración, job, webhook o calendario externo para citas; requieren aprobación específica por scope, prueba y reversión.

Estas G01–G10 son **brechas de evidencia/revisión**, no diez bugs certificados. Si en otro módulo existe código relevante, su hallazgo deberá incorporarse mediante lectura exacta, nuevo commit y BITACORA aditiva, sin reescribir estos antecedentes.

## 4. Matriz de trazabilidad y futura evidencia reproducible

| Alcance futuro | Fuentes de partida | Prueba positiva futura | Negativo futuro |
| --- | --- | --- | --- |
| Identidad/sede/tenant | `Sucursal.cs`, `SucursalesController.cs` | Sede A autorizada consultable | Sede/cliente B no accesible aunque ID sea válido |
| Reserva de tiempo y capacidad | `ReservaInventarioService.cs` como **contraejemplo de inventario** | Cita A en hueco libre se confirma una sola vez | Dos citas en recurso único no solapan ni comparten transacción |
| Estados y cancelación | `ReservaInventario.cs` como referencia **no reutilizable sin diseño** | Cancelación propia libera una vez | Replay, versión antigua o estado terminal no alteran otra cita |
| Tiempo | Contrato M01.01/M01.02 | UTC correcto en zona IANA de sede | DST ambiguo/no existente no se interpreta silenciosamente |
| Navegación/UX | `app.routes.ts` como mapa actual | Formulario accesible y confirmación real | Timeout/red lenta no muestra éxito falso ni filtra PII |
| Auditoría e integración | `IReservaInventarioService.cs` como interfaz existente de otro dominio | Evento post-commit correlacionado | No registrar JWT, contacto personal o datos de tenant B |

P01–P08 y N01–N12 del antecedente y esta matriz son **FUTUROS NO_EJECUTADOS**. No certifican la plataforma de citas ni sustituyen pruebas de servidor y BD cuando las microtareas técnicas se habiliten.

## 5. A0–A5, impacto y rollback

**Preflight institucional:** PLAN_MAESTRO fila 1156 originalmente PENDIENTE sin responsable; COLA G/O=SI; DAG_TECNICO fila 1117 F=VALIDADO/G=SI. Estos gates permiten el **análisis documental M01.03**, no cambios en JWT/RBAC, infraestructura, MySQL, M01.06 terminal ni QA/main/PROD. El owner de la microtarea es exclusivamente Task Javier :20 por CAS; el resto del backlog sigue concurrente bajo sus propios responsables.

**Impacto real:** agregar sólo este Markdown en `docs/v5` y sección autocontenida `CHANGELOG_AI.md`; no modificar Domain/Application/Infrastructure/API, Angular, migraciones, esquemas, secretos, usuarios, fechas productivas, pagos, proveedores o schedules.

**A0:** revisar diff, identificadores y blobs exactos, seguridad de secretos, procedencia, estado y DAG. **A1:** verificar nueve fuentes/categorías y G01–G10 con observaciones acotadas y no afirmar inexistencia global. **A2:** enumerar tenant A/B, RBAC/IDOR, privacidad y no regresión de stock. **A3:** concordancia documental con arquitectura vigente y NR01–NR12 de M01.01, A/I de M01.02; CI .NET/Angular/MySQL/Playwright `NO_APLICA` a Markdown y no es PASS de runtime. **A4:** fast-forward a dev con CAS y verificación exact-SHA de Scope Lock y V5 provenance, árbol/blobs y readback PLAN/COLA/BITACORA. **A5:** revisor independiente de una Task Javier distinta a la autora antes de QA/main/PROD.

**Rollback:** revert normal del commit y sección CHANGELOG con nuevo evento compensatorio; no force-push, reset, borrado de BITACORA ni promoción productiva.
