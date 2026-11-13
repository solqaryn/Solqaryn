# P5-016.C - Delimitacion de accion visual coherente (DEV)

- Fuente unica: Plan Comercial Inicial V3, P5-016.C; origen P5-016.A, dependencia P1-016.B LISTO en PLAN/COLA.
- TARGET_PATH unico: `frontend/src/app/shared/alerts/app-toast.service.ts`.
- BASELINE_BLOB_SHA: `0478821b246cae5fdf3016a5c25081a1726f925a` en dev. Archivo de runtime sin modificar.
- Escenario: accion de cierre no modal de AppToastService, `options.accion ?? 'Cerrar'`, visible en cuatro tipos de toast y estilos semanticos `app-toast--${tipo}`. Una interfaz unica `mostrar` recibe el mensaje y las opciones; el test acota esta unica accion, no todos los botones del producto.
- Cambio minimo: `scripts/tests/p5-016-c-delimitacion-acciones.test.mjs` con cinco pruebas Node sobre source real; fallan eliminacion de Cerrar, wrapper inconsistente, estilo fijo y HTML sin saneamiento. Una nota delimitada y CHANGELOG aditivo, sin codigo Angular productivo cambiado.
- Test: `node --check scripts/tests/p5-016-c-delimitacion-acciones.test.mjs && node --test scripts/tests/p5-016-c-delimitacion-acciones.test.mjs` con Node v22.16.0; el test sobre el archivo de referencia es estructural, no prueba de click real, lector pantalla ni navegacion Angular.
- Riesgos: no inferir JWT/RBAC, permisos multiempresa A/B, API/DTO, base de datos, migraciones, contenidos, CSS visual renderizado, mobile, QA/main/PROD, regresion del navegador ni deployment. Seguridad server-side no se cambia.
- Rollback: revert normal y selectivo de test, doc y CHANGELOG en dev; nunca force-push ni sobrescribir el commit del Scope P5-008.C que delimita accesibilidad del mismo archivo.
