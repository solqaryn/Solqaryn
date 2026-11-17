# C09-04.M05.06 - regresion rendimiento y accesibilidad V19

**R2 test-only**: Angular/Chromium real con respuesta de sesion HTTP simulada. Archivos afectados: nuevo Playwright, nuevo workflow dirigido, este documento y CHANGELOG_AI.md. No cambia rutas, componentes de runtime, API, JWT, EF, MySQL ni proveedores. Rollback: git revert normal con HEAD/concurrencia revalidada.

## A0 criterios
- Ruta privada /plataforma/mensajeria-multicanal-v19. Reutiliza componentes de M04.01–06 y contratos de M05.01–05; sin nuevos gates inventados.
- Cuatro escenarios, positiva/negativa/error, frontera 320/375/768/1280.

## A1–A3 pruebas reales dirigidas
- Playwright Chromium con proceso Angular efimero: tres canales y main semantico, DOM privado menor a 350 nodos, DOMContentLoaded menor a 20 s en CI (presupuesto orientativo, NO SLO del producto), cero overflow horizontal.
- Tab real desde inicio sin .focus(), outline de 2 px y destino tactil 44 px; Enter vuelve a plataforma.
- Revocacion de la respuesta de sesion a HTTP 403 tras cambio local de tema y error HTTP 503 inicial ocultan tarjetas y mensaje sensible; cero solicitudes mutantes.
- A2 cubre solo guard de UI y mocks de sesion. No certifica JWT firmado, permisos del backend, MySQL/Aiven, SMTP, WhatsApp ni deduplicacion de proveedor.
- A3 Chromium real proporcional; MySQL/EF/Docker no aplican a diff exclusivamente pruebas frontend.

## A4/A5 y salida
- Requerir exact SHA con Scope Lock, V5, tipado/build Angular y Chromium cuatro casos; SKIPPED, CANCELLED e IN_PROGRESS no son PASS.
- A5 exclusivamente Task Javier distinta del autor DEV04. QA/main/PROD excluidos. Defecto P1 de wildcard en formulas DAG permanece abierto como scope separado.
