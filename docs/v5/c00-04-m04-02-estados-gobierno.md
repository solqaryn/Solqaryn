# SOLQARYN V5 — C00-04.M04.02: estados accesibles y revalidación segura

**Alcance:** implementación real Angular 22.2.1 de estados de sesión y consulta V5 para la ruta privada de gobierno. Rama dev y Scope Lock STRICT.

## A0 — Dependencias y fronteras
- Extiende `GobiernoCambiosV5Component` sin crear API o política de autorización, conserva `plataformaGuard` de M04.01 y `PlataformaSesionService.obtener()` que exige respuesta de backend con `ambito=Plataforma` y `esAdministrador=true`.
- Estados explícitos `cargando`, `vacio`, `error`, `exito` (éxito de sesión, **no de una microtarea**) y `sin_permiso`. La vista de datos institucionales sigue vacía por diseño porque no se consulta ningún PLAN/COLA/BITACORA.
- Revalidación única por intento con bloqueo del botón y comprobación single-flight, sin redirigir, ejecutar cambios, aprobar A5, crear tokens ni conceder permisos. `takeUntilDestroyed` evita subscripción al destruir la vista. Fallos HTTP se clasifican 401/403 como denegación y otros como error genérico, nunca se imprime `error.message` o datos privados.
- Se oculta contenido de secuencia en error y denegación; la ruta y el backend conservan su autoridad. UX adaptativa con estado live region accesible, foco preservado y botón deshabilitado durante I/O.

## A1–A3 — Validación dirigida
- Cinco pruebas nuevas de Vitest/Angular para vacío honesto inicial, carga asíncrona+single-flight+éxito autorizado, 403, fallo 503 sin fuga de secretos y ausencia de sesión inicial sin I/O; se ajusta el test de M04.01 para permitir solo botón explícito de revalidación y prohíbe formularios.
- El servidor real de plataforma no se simula en producción; los tests usan `Subject` y un servicio fake solo en Vitest. No prueba HTTP/MySQL real, servidor de identidad en vivo ni autorización JWT/RBAC de backend.
- Requiere ejecutar GitHub Actions dirigido `V5 - Estados frontend y permisos con Vitest`, tipado/build Angular, auditoría de calidad ERP, inventario P0-007.A, Scope Lock y procedencia CHANGELOG en SHA causal.

## A4–A5 — Publicación y rollback
- Publicar mediante fast-forward con lease esperado en dev y verificar blobs, pruebas y check-runs `completed/success` exact-SHA o equivalencia causal de archivos cuando un commit concurrente los preserve. Ningún job cancelado/in_progress cuenta como PASS.
- Conservar VALIDANDO mientras haya CI pendiente, documentar readbacks de PLAN/COLA/DAG/BITACORA. LISTO_DEV solo A0–A4 comprobados; A5 independiente por otra Task Javier distinta del autor.
- Rollback no destructivo por `git revert` del changeset y readback; sin QA/main/PROD, secretos ni migraciones.
