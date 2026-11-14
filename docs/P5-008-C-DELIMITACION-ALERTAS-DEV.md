# P5-008.C - Delimitacion causal de alertas institucionales (DEV)

- Autoridad: Plan Comercial Inicial V3 P5-008.C, origen P5-008.A; predecesor P1-016.B LISTO verificado en PLAN/COLA.
- TARGET_PATH (unico archivo de producto): `frontend/src/app/shared/alerts/app-toast.service.ts`.
- BASELINE_BLOB_SHA: `0478821b246cae5fdf3016a5c25081a1726f925a`, comprobado contra contenido vivo de dev con git hash-object.
- Escenario acotado: avisos toast no modales de `AppToastService`, variantes info/success/warning/error; `error` usa anuncio assertive y duracion 6500 ms; los demas usan politeness polite y 4200 ms por defecto. `MatSnackBar.open` recibe texto, no HTML.
- Cambio minimo: prueba dirigida `scripts/tests/p5-008-c-delimitacion-alertas.test.mjs` que verifica los invariantes sobre el archivo real y cinco casos positivos/negativos. La delimitacion no modifica el runtime Angular ni claim de implementacion de un nuevo sistema de alertas.
- Verificacion reproducible: `node --check scripts/tests/p5-008-c-delimitacion-alertas.test.mjs && node --test scripts/tests/p5-008-c-delimitacion-alertas.test.mjs`; cinco tests PASS Node v22.16.0. Pruebas sinteticas mutan solo strings en memoria y acreditan la puerta estatica, no navegacion con teclado o screen reader.
- Limitacion: sin certificar snackbars en navegador, lector de pantalla real, responsive, JWT/RBAC, empresa A/B, API, MySQL, datos, QA/PROD ni despliegue. Las pruebas de UX integrada son trabajo posterior y necesitan herramientas de browser.
- Impacto: solo esta nota, prueba Node, entrada aditiva en CHANGELOG; ningun cambio de configuracion, dependencias, auth, tenancy, migraciones o datos. Seguridad fail-closed: eliminacion de prioridad assertive o HTML inseguro debe fallar.
- Rollback: revert selectivo del commit que anade nota, test y CHANGELOG mediante commit normal fast-forward en dev, sin force ni borrar trabajo ajeno.
