# SOLQARYN V5 — C08-01.M01.04 — Riesgos, supuestos y decisiones de reservas y agenda

**Autor:** SOLQARYN PRIMARY DEV 05 — JAVIER (`6adc2e55f47c81919f6374e561687076`). **Fecha:** 2026-11-14. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, C08-01 (Reservas y agenda), M01.04. **Naturaleza:** análisis documental preparatorio, NO implementación de citas, motor de horarios, workers, migración ni certificación de seguridad o disponibilidad runtime.

## 1. Alcance, autoridades y procedencia

El resultado esperado del bloque C08-01 es una futura solución de **citas, horarios, disponibilidad, personal, recursos y cancelaciones**, con evidencia verificable de conflictos, zona horaria y autorización. Esta unidad registra riesgos, supuestos y decisiones pendientes para impedir que M02/M03/M05 confundan planificación con código certificado. La referencia de origen es `PLAN_MAESTRO!A1157:O1157`, con `COLA!G1157/O1157=SI` y `DAG_TECNICO!A1118:G1118` (F=VALIDADO, G=SI) verificados antes del claim exclusivo.

Los antecedentes documentales disponibles en `dev` son: `C08-01.M01.01` (NR01–NR12, escenarios P01–P08/N01–N12 futuros; blob `26805ab07dd3f20cf986b6da4ebc24fb702fa8f6`), `C08-01.M01.02` (actores A01–A14, interfaces I01–I14 y fronteras F01–F07; blob `912cd97789de5faaacbe9e9abef1b5553a49a009`) y `C08-01.M01.03` (inventario acotado de fuentes y brechas G01–G10; blob `0394cb17b01c6d676d8664d5c1f1df8ae729d551`). Los identificadores P/N son **FUTUROS NO_EJECUTADOS**. La revisión A5 de esos antecedentes debe comprobarse individualmente; su mera publicación en DEV no implica A5_PASS.

La lectura selectiva de M01.03 distingue `ReservaInventario` de una cita temporal; un lock de stock, una ruta de sucursales y un guard Angular **no prueban** prevención de doble reserva, JWT/RBAC de agenda ni aislamiento multitenant. Los vacíos enumerados son hipótesis de diseño y **brechas de evidencia**, no defectos runtime demostrados. El alcance de esta entrega se limita a este Markdown y una entrada autocontenida de `CHANGELOG_AI.md`. No se cambian Domain, Application, Infrastructure, API, Angular, EF/MySQL, secretos, despliegues, configuración de Tasks ni datos.

## 2. Matriz R01–R16 de riesgos abiertos (no fallos certificados)

| Riesgo | Escenario causal y consecuencia | Mitigación propuesta y evidencia futura exigible |
| --- | --- | --- |
| R01 — Tenant A/B | ID de cita, recurso o profesional de B consultado por A expone o muta datos | Resolver empresa/rol/sede en backend; pruebas HTTP A/B de 403/404 sin filtración ni enumeración |
| R02 — IDOR público | URL pública, host o parámetro `EmpresaId` selecciona datos internos | Publicar sólo proyección mínima ligada a host verificado; negativos de host falso, ID cruzado y forwarded header |
| R03 — Doble ocupación | Dos altas concurrentes confirman el mismo recurso de capacidad uno | Exclusión transaccional durable e índice/versión; prueba de competencia real en MySQL efímero |
| R04 — Profesionales ausentes | Turno cerrado, pausa, licencia o festivo aparece como hora libre | Regla de disponibilidad server-side con calendario, buffers y prioridades; pruebas positivas/negativas |
| R05 — Duración manipulada | DTO de cliente reduce duración o buffer para forzar un hueco | Cargar servicio canónico en servidor; rechazar modificaciones de precio, duración o capacidad |
| R06 — Zona horaria | Hora local sin zona IANA produce instante incorrecto | Normalizar UTC con zona validada, versionar interpretación DST; ensayar zonas distintas sede/cliente |
| R07 — DST ambiguo | Hora inexistente/duplicada causa cita movida o repetida | Denegar hora inexistente y exigir desambiguación de hora repetida; casos frontera reproducibles |
| R08 — Replay / timeout | Reintento de cliente duplica confirmación o cancelación | Clave idempotente persistida, payload y scope idénticos; negativo misma clave/payload distinto |
| R09 — Carrera reprogramar | Liberar slot anterior antes de asegurar nuevo genera pérdida de cita | Cambiar ocupación y estado bajo una sola transacción; rollback de fallo intermedio |
| R10 — Estados terminales | Cancelada/completada puede mutar por operación obsoleta | Máquina de estados explícita, versión y denegación de transiciones inválidas |
| R11 — Fuga de PII | Vista pública, exportación, notificación o log filtra nombre, teléfono, JWT o detalle médico | Proyección mínima, limpieza de logs, autorización por rol y pruebas negativas de exportación |
| R12 — Confusión stock/citas | Reusar `ReservaInventario` como contrato de disponibilidad altera existencias/pedidos | Agregado y contrato distintos; regresión de stock, ventas, facturación e inventario |
| R13 — UX optimista | UI muestra «confirmada» en timeout o desconexión sin persistencia real | Estado «pendiente de consulta», consulta idempotente y accesibilidad con foco/error/320px |
| R14 — Recursos incompatibles | Capacidad/sucursal/profesional de otra empresa se combinan | Comprobación relacional de pertenencia y compatibilidad antes de reservar; negativos de recurso ajeno |
| R15 — Auditoría incompleta | Cancelación o error carece de actor, tenant, versión y correlación | Auditar operación confirmada/denegada sin PII ni secreto; casos rollback con readback |
| R16 — Integración prematura | Activar SMTP, worker, cola, webhook o calendario externo antes de commit genera efectos duplicados | Eventos futuros sólo bajo autorización específica, post-commit e idempotencia; **no implementar eventos en V5 actual** |

Cada mitigación es una **propuesta de aceptación futura** condicionada a código, pruebas y permisos reales. Ni la ausencia de algunos nombres en el árbol ni un documento de amenazas permiten declarar PASS técnico.

## 3. Supuestos S01–S08 que deben validarse antes de implementar

- **S01:** empresa y sucursal se resuelven desde contexto confiable del servidor; jamás desde un campo controlado por el browser.
- **S02:** la agenda necesita sus propias entidades/relaciones y no hereda semántica de `ReservaInventario`.
- **S03:** profesionales y recursos pueden tener capacidades distintas, con buffers y ausencias; la capacidad no se deduce del número de clientes.
- **S04:** la zona IANA de la sede es dato validado; el timezone del dispositivo sólo participa en visualización.
- **S05:** un visitante ve disponibilidad publicada mínima, no calendarios privados ni PII.
- **S06:** las autorizaciones JWT/RBAC, membresía activa y alcance de sucursal se revalidan por operación, no sólo al entrar en la pantalla.
- **S07:** reprogramar y cancelar requieren versión, auditoría e idempotencia durables, no mutex local ni memoria de un worker.
- **S08:** compatibilidad con facturación, reservas de stock, pedidos y notificaciones existentes se demuestra mediante regresiones antes de cualquier despliegue.

Estos supuestos no se han comprobado contra MySQL/HTTP/Playwright en esta unidad. Si una verificación posterior refuta uno, registrar nueva evidencia y bloquear únicamente el scope afectado.

## 4. Decisiones D01–D08 pendientes de autoridad técnica

| Decisión | Opciones que requieren evaluación | Gate antes de elegir o activar |
| --- | --- | --- |
| D01 — Identificador y ownership | ID opaco por cita, EmpresaId/sucursal y clave externa opcional | Modelo M02, validación tenant A/B, IDOR negativo |
| D02 — Modelo de ocupación | Intervalos con capacidad, recursos múltiples, buffers y exclusiones | Diseño transaccional y pruebas de conflicto MySQL |
| D03 — Persistencia/índices | Índice de solapamiento lógico, versión optimista o lock transaccional | Migración versionada y reversión aprobadas separadamente |
| D04 — DST | Rechazar ambigüedad o exigir offset explícito junto a zona | Pruebas zona local/UTC, DST y límites de día |
| D05 — Política de cancelación | Ventana, penalidad informativa, estados y permisos | Decisión comercial explícita; sin cobrar ni sancionar automáticamente |
| D06 — Exposición pública | Consulta de franjas publicada con rate limiting y mínimo DTO | Threat model, host seguro, anonimización, anti-enumeración |
| D07 — Idempotencia | Clave acotada tenant/actor/payload y expiración | Replays positivos/negativos y auditoría persistente |
| D08 — Comunicaciones | Sincronizar avisos futuros tras commit o mantener desactivados | Autorización nueva para integración/eventos, no activar hoy |

**Decisión vigente de esta unidad:** conservar todas D01–D08 como `PENDIENTE`. No se añade proveedor, secret, worker ni API. No se interpreta `DAG VALIDADO/SI` como permiso para ejecutar D03, D06 o D08.

## 5. Matriz causal y pruebas futuras — ninguna ejecutada aquí

| Aserción futura | Positivo esperado | Negativo obligatorio | Fase sugerida |
| --- | --- | --- | --- |
| TP01 — aislamiento | Tenant A consulta sólo citas A autorizadas | Token/ID/host B no filtra datos B | M03/M05: API + seguridad |
| TP02 — conflicto | Una solicitud de hueco libre confirma una vez | 64 solicitudes simultáneas no generan doble ocupación | M03/M05: MySQL efímero |
| TP03 — zonas | Hora local válida en IANA genera UTC correcto | DST inexistente/ambiguo sin offset se rechaza | M03/M05: unidades + integración |
| TP04 — idempotencia | Replay idéntico devuelve resultado original | Misma clave con payload diferente falla sin mutación | M03/M05: API + DB |
| TP05 — estados | Cancelación propia libera disponibilidad una vez | Cambio de estado terminal o versión antigua falla | M03/M05: integración |
| TP06 — UX | Teclado/lector/móvil muestran respuesta real | Timeout no confirma ni expone PII | M03/M05: Angular + Playwright |
| TP07 — privacidad | Proyección pública no contiene datos sensibles | Exportación cruzada y log con JWT son rechazados | M03/M05: seguridad |
| TP08 — compatibilidad | Inventario y ventas existentes siguen invariantes | Cancelación horaria no modifica reserva de stock | M03/M05: regresión |

**Estado:** TP01–TP08 con sus negativos **FUTUROS NO_EJECUTADOS**; no se confunden con PASS de test, CI ni A5. A5 posterior requiere otra Task Javier distinta del autor y SHA exacto de esta entrega.

## 6. Auditorías A0–A5, impacto y rollback

- **A0 (estática):** ruta/ID/fuente exactos, dependencia documental, head vigente, ownership exclusivo H1157/I1157, inventario de R/S/D y ausencia de secretos/encabezados vacíos; CHANGELOG se inserta con cabecera y viñetas contiguas.
- **A1 (consistencia documental):** cotejar R01–R16, S01–S08, D01–D08, TP01–TP08 con NR01–NR12, A01–A14, F01–F07 y G01–G10. No equivale a ejecutar un solo test funcional.
- **A2 (seguridad de diseño):** tenant A/B, JWT/RBAC server-side, IDOR, logs/PII, concurrencia, DST y fallos cerrados revisados como escenarios, sin afirmar certificación runtime.
- **A3 (integración de fuentes):** trazabilidad de M01.01–03 y arquitectura Domain–Application–Infrastructure–API/Angular; builds .NET/Angular, MySQL y Playwright son **NO_APLICA** para el diff exclusivamente Markdown y nunca son PASS simulados.
- **A4 (publicación y evidencia):** commit fast-forward en `dev`, SHA/tree/blob, check `verify-solqaryn-project-scope` y control `V5 - Sin encabezados vacios ni atribucion cruzada` ambos `completed/success` del SHA publicado, BITACORA A:Z y readback PLAN/COLA; si falta uno permanecer VALIDANDO.
- **A5 (independiente):** queda PENDIENTE para Task Javier distinta de `6adc2e55f47c81919f6374e561687076`; no QA/main/PROD.

**Rollback**: `git revert` normal de la entrega documental y evento compensatorio aditivo; sin reset/force-push, sin modificar historia BITACORA, sin activar servicios ni migraciones. El artefacto no altera runtime, base de datos, roles, UI, DNS, calendarios externos, cuentas ni otras ramas.
