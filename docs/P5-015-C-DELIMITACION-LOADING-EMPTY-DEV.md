# P5-015.C — Delimitación de estados de carga y vacío (DEV)

- Autoridad: Standard Comercial Inicial V3, alcance P5-015.C, origen P5-015.A; dependencia P1-016.B LISTO.
- TARGET_PATH: `frontend/src/app/features/usuarios/usuario-detail.component.html`, blob de producto `bca657fd3ede944cb10c77ddc72063c11eb0ec65`. Un único escenario: pantalla de detalle de usuario.
- Contrato observado: rama `@if (loading())` con `mat-spinner`, `@else if (notFound())` con texto no identificador, `@else if (usuario(); as u)` con ficha. Interpolaciones Angular para nombre de usuario (sin `innerHTML`).
- Prueba reproducible `scripts/tests/p5-015-c-delimitacion-loading-empty.test.mjs`: 1 positivo de código real y 4 controles negativos para spinner eliminado, vacío sin exclusión, ficha sin guardia y HTML/PII en vacío. SHA256 del test `cf33d0e8cca241de789c920b3c869af64c2f1c90143a5fc620e8a4bc46fdea6c`.
- Impacto: sólo test, documento de delimitación y CHANGELOG; sin modificación funcional de Angular/TS, backend, MySQL, JWT, RBAC, datos ni secretos. No sustituye pruebas visuales de loading/vacío, lector de pantalla, tiempo real, fallo HTTP o seguridad multitenant.
- A0/A1/A2/A3 dirigidas con Node v22.16.0 tras VALIDANDO; A4 sólo tras SHA exacto publicado y readback; A5 independiente antes de QA/main/PROD.
- Riesgo: campos de un usuario sólo deben ser suministrados por API autorizada de su tenant; un template no acredita aislamiento. No usar fixtures para simular autorización.
- Rollback: revert selectivo del commit aditivo en `dev`, sin force-push ni tocar trabajo concurrente.
