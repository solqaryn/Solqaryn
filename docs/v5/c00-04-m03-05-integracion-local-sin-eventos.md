# SOLQARYN V5 — C00-04.M03.05: integración local inerte de gobierno de cambios

**Microtarea:** `C00-04.M03.05` (PLAN fila 126). **Autor:** PRIMARY DEV 05 / JAVIER, Task `6adc2e55f47c81919f6374e561687076`. **Fecha:** 2026-11-14. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`); `solqaryn/Solqaryn@dev`; Scope Lock STRICT.

## A0: por qué una integración y no un worker

M03.05 permite **worker, proveedor, cola o integración cuando aplique**. Se implementa una integración **real de código .NET 10**, exclusivamente local e inerte entre `GobiernoCambiosAuthorizationService` (Application) y `GobiernoCambiosProyeccionAislada` (Infrastructure). Un worker, dispatcher, webhook, proveedor o tarea recurrente **no está autorizado** en el V5 vigente: no se crea ninguno. No hay API/controller, DI, persistence, MySQL, migración, Sheets, red, secret, UI o cambio productivo.

C00-04.M03.03 ya exige contexto de tenant, permiso relacional actual y denegación de cambios sensibles. M03.04 contiene una lectura institucional no autoritativa en memoria. La integración mantiene la dependencia Infrastructure→Application, valida la propuesta y el JWT/RBAC/tenant mediante el guard antes de consultar cualquier copia local. Solo compara ID, revisión y SHA; una coincidencia indica **diagnóstico local**, no fuente institucional verificada.

## A1: comportamiento funcional y límites

- Positivo P01: contexto A con membresía/rol/permiso Configuración Administrar vigentes, propuesta de código DEV y snapshot local idéntico devuelve `PENDIENTE_AUTORIZACION` y coincidencia diagnóstica sin conceder ejecución.
- Positivo P02: falta de copia o SHA diferente devuelve coincidencia `false`; jamás se presume cache actual ni transición del PLAN.
- Negativo N01: contexto A intenta empresa B; denegación antes de leer la fuente de snapshot; sin fuga cross-tenant.
- Negativo N02: membresía revocada deniega antes de considerar la copia local, aunque esta tenga datos del ID.
- Negativo N03: ID manipulado con LF se rechaza antes del I/O confiable.
- Negativo N04: cambios de seguridad/permisos mantienen `NO_GO` aun con copia local coincidente.
- Negativo N05: dependencias nulas se rechazan, sin habilitar atajos ni autorizaciones.

La clase `GobiernoCambiosIntegracionLocal` se deja sin registro en DI y sin endpoint. El DTO `DiagnosticoIntegracionGobiernoLocal` expone `CopiaLocalCoincideConSolicitud` sólo a consumidores internos y declara **cuatro capacidades falsas**: ejecución, aprobación, persistencia institucional y concesión de permisos. La proyección sigue sin CAS distribuido y no sustituye PLAN_MAESTRO, COLA, DAG_TECNICO ni BITACORA.

## A2: impacto y pruebas

**Capas:** Infrastructure integra Application y un adaptador de Infrastructure, sin acoplar Domain ni UI. **Datos:** cero consultas nuevas SQL, cambios de schema o fuentes externas. **Seguridad:** contexto tenant servidor y RBAC relacional en el guard existente, sin confianza en `EmpresaId` cliente, claims legacy, slug o URL. **Efectos:** sólo lectura local, sin worker/eventos ni orden de ejecución. **Observabilidad:** ninguna telemetría nueva, logs con secretos o alertas. **Rollback:** revert del changeset en dev, no force-push, preservar BITACORA.

Pruebas xUnit positivas y negativas de tenant A/B, revocación, SHA ausente/desigual, solicitud forjada, propuesta sensible y dependencias nulas. El nuevo test usa fixtures `IdentidadUniversalSnapshot` sin base real. Se requieren build/test .NET 10, seguridad multiempresa, autorización y Scope Lock/procedencia CHANGELOG **sobre SHA exacto** en GitHub Actions. In-progress/cancelled/skipped/failed no son PASS. MySQL, Angular, browser y Docker NO_APLICA a esta integración exclusivamente en memoria (no declararlos probados).

## A3–A5 y exclusiones

A3: inspeccionar interacción Application–Infrastructure y xUnit reales; distinguir test de memoria de integración externa. A4: publicación única fast-forward en dev, diffs sólo código/test/doc/CHANGELOG, verificación SHA y readback PLAN/COLA/BITACORA. A5: revisión por otra Task Javier distinta del autor antes de QA/main/PROD. No se implementan eventos, dispatcher ni Tasks y ninguna aprobación A5 autoriza por sí sola producción.

**Rollback:** `git revert` normal tras revalidar HEAD y consumidores. No se compran servicios, crean branches o PR, ni se tocan main/qa/PROD.
