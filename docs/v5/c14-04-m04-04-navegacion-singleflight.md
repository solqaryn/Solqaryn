# SOLQARYN V17 — C14-04.M04.04 navegación y single-flight readiness DEV

**Autor:** PRIMARY DEV 01 JAVIER, TASK_ID 6adbb9fff1548191a1bd5b30d9e9984a. **Rama:** dev. **PLAN:** fila 2147. **Lock V17:** SQV17_C14_04_M04_04 / 2000002147. **Riesgo:** R2 conservador por control de permisos y concurrencia UI.

## A0 — alcance, dependencias y reversión
Esta unidad verifica mediante Playwright la navegación de una superficie privada ya implementada en /plataforma/flujo-v5, los mensajes de acceso y la prevención de una segunda revalidación mientras hay una petición de sesión en vuelo. No crea formularios, endpoints, permisos, transacciones ni canales de despliegue. No cambia Angular, su guard ni el backend: únicamente añade un spec, su selección en workflow dirigido y documentación de pruebas. Dependencias: C14-04.M03.01-06+A5 según DAG vigente. Reversión: git revert del commit, conservando BITACORA.

## A1 — pruebas Chromium obligatorias
Cuatro casos: (1) acceso anónimo redirigido sin render privado ni POST/PUT/PATCH/DELETE; (2) sesión sintética autorizada navega ida y vuelta sin controles mutadores y sin inventar PASS; (3) 403 tras revocación simulado oculta panel y mensaje privado; (4) 503 simulado durante revalidación deshabilita el botón y no inicia una segunda solicitud, oculta información interna y permite un nuevo GET explícito exitoso. Los asserts positivos, negativos, de error y de concurrencia deben ejecutarse realmente en GitHub-hosted Chromium con el spec exacto, no se certifican al escribirlos.

## A2/A3 — límites de seguridad e integración
La ruta continúa bajo plataformaGuard, usando únicamente GET /plataforma/sesion de sesión **sintética** y sin cambiar el código de autorización. El estado privado se oculta ante fallos. GitHub-hosted Angular lint/build y Playwright real son la integración pertinente. MySQL 8.4/EF, JWT HTTP real, tenant A/B de backend, DNS, R2, backup y proveedores externos NO_APLICA como evidencia ejecutada, pues no se modificaron ni invocaron. No se infiere certificación de QA/main/PROD.

## A4/A5 — gates
Conservar workflow con concurrency por github.sha y cancel-in-progress=false; ejecutar los cuatro tests nuevos además de los existentes. Requerir Scope Lock STRICT, procedencia V5, CI completada SUCCESS exact SHA o descendiente equivalente, logs causales y readback PLAN/COLA/BITACORA antes de LISTO_DEV. CANCELLED/SKIPPED/QUEUED/IN_PROGRESS no son PASS. A5 exclusivamente por otra Task Javier de ID distinto. No alterar HISTORIA ni forzar G/O. 
