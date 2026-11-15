# SOLQARYN V5 — C00-04.M04.01: ruta y superficie protegida de gobierno de cambios

**Alcance aprobado:** implementar una ruta privada y una pantalla Angular 22.2.1 real de consulta sobre el contrato de gobierno de cambios. Rama `dev`; PROJECT_SCOPE_LOCK STRICT. Esta pantalla es una explicación contractual **estática** y NO una consola que lee PLAN, COLA, DAG ni BITACORA.

## A0 — Impacto y límites
- Componente primario: `GobiernoCambiosV5Component`, standalone/OnPush, lazy-loaded desde `app.routes.ts` bajo `plataformaGuard` existente. Consumidor: la navegación de administración de plataforma mediante URL directa o enlace autorizado; no incorpora entrada al menú de empresa.
- Capas: **frontend** solamente. No se modifica backend, Domain, Application, Infrastructure, API, JWT, permisos RBAC, base MySQL, tenant, servicios, workers ni DI. El guard exige sesión vigente y vuelve a consultar el servidor antes de activar la ruta; el cliente no concede autoridad.
- Datos: sin acceso a datos institucionales ni empresariales. No se guarda ni transmite PII/JWT; las cuatro etapas que muestra son referencias genéricas y no estados de microtareas en vivo. Sin flags, HTML arbitrario, editor, eventos o acciones.
- UI: título jerárquico, región explícita, secuencia ordenada, indicación inequívoca de solo lectura, retorno accesible por teclado, responsive (una columna en móvil), preferencia de movimiento reducido y contraste heredado de tokens del tema.
- Dependencias: M03.01–06 y A5 independiente de C00-04.M03.06 comprobados antes del CLAIM. No cambia ni libera gates por sí sola.

## A1–A3 — Comprobaciones causales
- Tests Angular/Vitest nuevos: ruta única, guard server-backed requerido, ausencia de permisos/datos/children/providers/redirect nuevos, lazy-load del componente y render accesible sin controles operativos.
- Prueba positiva prevista: la URL protegida resuelve el componente de consulta. Negativas previstas: no aparece ruta pública equivalente, ausencia de elementos de aprobación y de una vía para ejecutar tareas.
- Validar `ng test` / tipado/build Angular y regresiones proporcionales en GitHub Actions del SHA. Validaciones de backend/MySQL no aplican al diff de UI estática; no se presentan como ejecutadas.
- El `plataformaGuard` proporciona barrera de UX, pero las autorizaciones reales siguen en el backend; ningún PASS de frontend equivale a control de acceso integral en HTTP o MySQL.

## A4–A5 — Evidencia y rollback
- Exigir publicación fast-forward/CAS, `verify-solqaryn-project-scope`, procedencia V5 y controles Angular apropiados `completed/success` sobre el SHA publicado. Los checks `queued`, `in_progress`, `cancelled`, `skipped` o `failure` no acreditan PASS.
- Mantener VALIDANDO hasta reunir A0–A4, revisión del diff, readback PLAN/COLA/DAG/BITACORA; solo entonces LISTO_DEV. A5 independiente deberá ejecutarla otra Task Javier distinta del autor.
- Reversión: `git revert` normal del changeset en `dev`, con readback institucional; sin force-push, QA, main, PROD ni datos reales.
