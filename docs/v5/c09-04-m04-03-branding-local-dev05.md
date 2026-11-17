# C09-04.M04.03 — branding y configuración de presentación local [DEV05]

Repositorio: solqaryn/Solqaryn; rama dev; PROJECT_SCOPE_LOCK=STRICT.
Autor Task Javier DEV05: 6adc2e55f47c81919f6374e561687076.
PLAN_MAESTRO fila 1426, candado V17 SQV17_C09_04_M04_03 / 2000001426.
Clasificación R2 por superficie de mensajería y límite de seguridad de plataforma.

## A0 — alcance, dependencias y no-alcance
Se añade al componente privado existente C09-04.M04.01 una selección **solo local** de tres presets fijos de presentación: neutro, alto contraste y azul. El cambio es reactivo con Angular Signals y limitado por la sesión de plataforma ya protegida por plataformaGuard. La lista congelada de componentes permitidos incluye exclusivamente tarjetas informativas, estados de desconexión y enlace de retorno. Los tres canales permanecen literalmente NO_CONECTADO.

No hay configuración dinámica de backend/tenant, formularios de envío, tokens o endpoints nuevos. Los presets no provienen de URLs, HTML arbitrario, contenido de empresa ni API de proveedor: valor desconocido restablece el preset neutro. Ninguna preferencia se persiste. El backend sigue siendo autoridad de acceso; la guardia frontend no reemplaza JWT/RBAC reales.

## Impacto y rollback
Capas: frontend standalone Angular y sus tests, sin API, EF, MySQL, migraciones, secretos, configuración remota o proveedores. Consumidores: ruta privada ya existente /plataforma/mensajeria-multicanal-v19 y suite previa M04.01. Se preservan CANALES_DECLARADOS_C0904 y el bloque de denegación sin sesión. Changelog con sección autocontenida. Reversión segura: git revert normal del commit publicado, nunca force-push; no hay cambios de datos.

## A1–A5, evidencias y criterios
A1: 5 casos TestBed nuevos, éxito admin, presets exactos, malformaciones/CSS/URL/HTML descartadas, empresa/no-admin y revocación, lista de componentes. A2: sin POST, sin datos tenant, sin endpoints ni proveedores; sesión A/B sintética en browser no acredita JWT firmado ni control backend. A3: 3 casos Playwright Chromium con ruta privada real de Angular, servidor localhost, HTTP de sesión **simulado** y responsive 320/375/768/1280; CI dirigida GitHub-hosted. Las pruebas antiguas M04.01 siguen siendo causales al cambiar su componente. MySQL/SMTP/WhatsApp/webhooks: NO_APLICA al cambio solo frontend; NO equivale a integración real.
A4: verificar HEAD exacto, Scope Lock, V5, catalogo P0 y frontend compile, workflow dirigido M04.03 y regresión M04.01 en GitHub Actions con status completed y conclusion success; registrar SHA y RUN, PLAN/COLA/DAG/BITACORA readback antes de LISTO.
A5: revisión independiente por Task Javier distinta de DEV05; jamás autoaprobación.

El trabajo publicado puede permanecer VALIDANDO si CI externa se encuentra QUEUED/IN_PROGRESS. Ningún check omitido, cancelado o fallido es PASS. No usar QA, main, producción ni datos reales.
