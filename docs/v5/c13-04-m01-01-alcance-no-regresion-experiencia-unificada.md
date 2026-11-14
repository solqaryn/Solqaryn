# SOLQARYN V5 FINAL — C13-04.M01.01: alcance y no-regresión de experiencia unificada

**Unidad:** C13-04.M01.01 — fijar objetivo, no-alcance y criterio de no-regresión. **Autor:** PRIMARY DEV 05 JAVIER, TASK_ID `6adc2e55f47c81919f6374e561687076`. **Fecha:** 2026-11-14. **Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Tipo:** preparación M01 documental; no constituye implementación, auditoría de interfaz ni aprobación de UX final.

**Fuentes:** [Standard V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit); [PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). La autorización de preflight fue PLAN fila 1982 PENDIENTE sin owner, COLA G/O=SI, DAG_TECNICO fila 1943 F=VALIDADO/G=SI. Esta habilitación documental **no libera M01.06/M02–M06, QA/main/PROD, JWT/RBAC/tenant, infraestructura ni compras**.

## 1. Resultado observable que se persigue

Preparar una auditoría reproducible de lenguaje visual, navegación, componentes, estados, responsive y contenido de SOLQARYN, sin transformar la aplicación en una interfaz pesada ni sacrificar seguridad o accesibilidad. El resultado futuro deberá mostrar consistencia entre storefront, login, backoffice y formularios: jerarquía clara, información por prioridad, rutas entendibles, tablas y diálogos previsibles, mensajes recuperables y operaciones cómodas en móvil. La experiencia unificada deberá permitir variaciones de marca por empresa sólo mediante valores autorizados, sin arrastrar tema, identidad, datos ni permisos entre tenants.

La evidencia mínima esperada tras M01–M05 será una **matriz ruta/pantalla × rol/tenant × estado × acción × viewport × resultado accesible × versión SHA**, con diferencias verificables y tratamiento de no-regresión. M01.01 fija aquí los objetivos y límites; M01.02 identificará actores, entradas y salidas; M01.03 auditará código y superficies; M01.04 documentará riesgos; M01.05 definirá aceptación. M01.06 requiere gates auténticos: nunca declarar cierre por este texto.

**Fuera de alcance:** editar Angular/SCSS/HTML, iconos, tokens, menús, layout, rutas, formularios, copy visible, marcas, assets, DTO/API, MySQL, permissions, seguridad JWT/RBAC, CI, Playwright, tracking, CDN o proveedores; ejecutar pruebas de navegador, declarar conformidad WCAG, migrar hosting o publicar fuera de dev.

## 2. Fuentes reales verificadas y límites

| Fuente Git versionada | Blob del archivo | Qué aporta y qué no demuestra |
| --- | --- | --- |
| `frontend/src/styles.scss` | `2eb48562fd77c758662af6b9683649b4f9698a64` | Tokens CSS semánticos (color, texto, borde, focus), reglas de reflow y estados; su existencia no prueba contraste/consistencia en todas las pantallas |
| `frontend/src/app/app.routes.ts` | `2f560c14693455b79ac4546e9333fb74586ef234` | Rutas lazy de tienda, login y backoffice; no inventario completo de pantallas renderizadas ni prueba de guards |
| `frontend/package.json` | `e6d030f643421cb343cc12617e742469e88223c4` | Angular 22.2.1, Material/CDK y scripts; un script no es un PASS de test |
| `docs/v5/c05-01-m01-05-criterios-aceptacion-design-system.md` | `875e6929d0f166e70192f1b6a441997f7bcefb6c` | 14 oráculos documentales de design system, no ejecución Angular/WCAG |
| `docs/v5/c13-02-m01-01-alcance-no-regresion-accesibilidad.md` | `7625e8638e3b228f8773ffe11541b0299228fda9` | Objetivo de teclado, foco, lectores, contraste, reflow; no conformidad demostrada |
| `docs/v5/c13-03-m01-01-alcance-no-regresion-rendimiento-ligereza.md` | `93f4bc4b260fbbf9d93dccdf8fa4b128e2922e80` | Objetivo de carga, error visible y datos íntegros; no benchmark medido |

Los oráculos precedentes se reutilizan **como criterios**, no como autoridad para incorporar CSS ni cambiar dependencias. `styles.scss` declara, entre otros, `--color-primary`, `--color-danger`, `--color-text`, `--focus-ring`, reflow a 320 px y reglas de foco. Las rutas muestran flujos públicos y privados separados; los guards cliente jamás sustituyen control de backend. No se ha leído cada componente ni medido el diseño final.

## 3. Invariantes de no-regresión NR01–NR14 para implementación futura

| ID | Contrato observable | Negativo que bloquearía aceptación |
| --- | --- | --- |
| NR01 | Jerarquía visual ordena título, contexto, acción principal y estado | CTA ambiguo o información esencial escondida |
| NR02 | Token semántico conserva significado entre pantallas y tema | Mismo estado usa colores incoherentes sin razón |
| NR03 | Tipografía y densidad permiten escaneo y lectura con zoom | Texto cortado o tamaño fijo que impide comprender |
| NR04 | Componentes de acción mantienen nombre, rol, feedback y hit area | Icono sin label o control táctil diminuto |
| NR05 | Focus visible y orden de teclado se preservan en navegación y diálogos | Trampa, foco invisible o perdido al cerrar |
| NR06 | Estado loading/empty/success/warning/error/403 aporta texto y recuperación | Spinner infinito, error sólo cromático o oculto |
| NR07 | Formulario vincula label, ayuda, validación y error al campo | Input sin nombre o mensaje sin solución |
| NR08 | Tabla/lista usa cabecera y scroll propio sin romper reflow | Acciones ocultas o pérdida de selección |
| NR09 | Responsive 320/390/768/1280 conserva acción esencial | Scroll horizontal global o CTA cortado |
| NR10 | Reduced-motion y forced-colors no destruyen significado | Animación indispensable o contraste no verificable |
| NR11 | Tema/branding por empresa nunca concede permiso ni ejecuta CSS libre | Valor arbitrario o token de A aparece a B |
| NR12 | Cambio de tenant A→B limpia datos, tema, cache y contexto previo | UI muestra identidad, precio o acción de A a B |
| NR13 | UI ligera no esconde validaciones JWT/RBAC/propiedad del backend | Menú oculto sustituye autorización API |
| NR14 | Una afirmación de PASS requiere prueba causal exact-SHA por tipo de cambio | Archivo .spec o check CANCELLED/SKIPPED/QUEUED contado como éxito |

El cumplimiento futuro deberá mantener **accesibilidad, integridad y seguridad** incluso cuando cambie espaciado, color, preload o responsive. No se fijan tokens alternativos, SLA ni certificación WCAG sin medición y autorización.

## 4. Matriz de pantallas y estados iniciales que se debe verificar

| Superficie representativa (selección futura) | Estados / acciones a comparar | Viewport y riesgo |
| --- | --- | --- |
| Login y sesión | Validación, error 401, envío, recuperación de foco | 320/390; labels y error asociado |
| Storefront y producto | Carga, imagen, carrito, no disponible, vacío | 320/768; CLS, CTA y jerarquía |
| Cuenta y checkout | Permiso, dirección, resumen, error de red | 390/1280; confirmación sin perder información |
| Dashboard | Carga, KPIs, filtros, permiso denegado | 768/1280; foco y prioridad de dato |
| Listas y tablas administrativas | Filtro, paginación, error, selección y diálogo | 320/1280; scroll propio y acciones |
| Cambios de empresa/sucursal | Cambio de marca, menú, sesión, permisos y resultados | 390/1280; negativo tenant A/B |

Estas filas son **muestras para definir**; no afirman que todas las pantallas o estados estén implementados y no acreditan cobertura ejecutada. La matriz futura debe identificar una ruta real en HEAD, role/membership y fixture sintético por caso.

## 5. Pruebas positivas/negativas FUTURAS_NO_EJECUTADAS

| Caso | Positivo esperado futuro | Negativo futuro que debe fallar |
| --- | --- | --- |
| P01/N01 | Usuario identifica acción principal y error recuperable sin buscar | CTA escondida o mensaje sólo por color |
| P02/N02 | Teclado recorre formulario y restaura foco de modal | Trap/foco invisible/diálogo retorna al body |
| P03/N03 | Reflow a 320px preserva acción y datos clave | Scroll horizontal del body y acción fuera del viewport |
| P04/N04 | Tabla/estado vacío mantiene contenido semántico y feedback | Selección desaparece tras paginación |
| P05/N05 | Branding de A pasa a B sin contaminación | Tema o datos privados de A visibles en B |
| P06/N06 | Error 403 respeta backend y se anuncia accesiblemente | UI concede acceso sin autorización JWT/RBAC |
| P07/N07 | Preferencia movimiento reducido conserva función | Transición obligatoria o estado incomprensible |
| P08/N08 | CI exact-SHA y pruebas proporcionales cierran cambio real | CANCELLED/QUEUED/SKIPPED inventado PASS |

**Todos NO EJECUTADOS** en esta microtarea. Para un PASS posterior harán falta SHA, ruta, nivel de permisos, tenant sintético, dispositivo, resolución/zoom, navegador, pasos, captura/árbol accesible, resultado positivo y negativo, CI y rollback. Una captura estática aislada no certifica la experiencia total.

## 6. Impacto, validación y rollback

- **Frontend/UX:** impacto únicamente documental; consumidores futuros son componentes Angular/Material, rutas storefront/backoffice, design system, tests y redactores de UI. No se altera DOM/HTML/SCSS, layout, dependencias, assets o motion vigente.
- **Backend/API/datos:** ninguna modificación en `.NET 10`, EF Core/MySQL, endpoints, DTO, migraciones, identidad o sesiones. Tenant A/B, JWT y RBAC siguen a cargo del servidor.
- **Integraciones/observabilidad:** no se crea dispatcher, worker, tracker, CDN, servicio de pago ni variable de entorno; cualquier medición futura debe evitar PII/tokens en artefactos.
- **A0:** revisar diff de dos Markdown, sección/viñetas CHANGELOG juntas, NR14/P08–N08, secretos y encabezados vacíos.
- **A1 documental:** criterios de objetivo/no alcance, 14 NR, 6 superficies y 8 pares futuros coherentes con C05-01/C13-02/C13-03.
- **A2 documental:** no degradar privacidad, permisos, tenant ni rutas. HTTP negativo A/B requerido sólo cuando cambie código.
- **A3 documental:** comprobar blobs y contracts, sin atribuir builds/Playwright/MySQL PASS a esta edición Markdown.
- **A4:** publicar fast-forward con CAS en `dev`, exigir `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` completados en SUCCESS sobre SHA exacto, más readback Git y PLAN/COLA/BITACORA.
- **A5:** revisión independiente por Task Javier distinta del autor; no QA/main/PROD ni certificación visual automática.
- **Rollback:** commit `git revert` normal desde HEAD revalidado y evento compensatorio BITACORA append-only. Ningún dato o servicio productivo se modifica.

**Salida atómica:** objetivos y fronteras de una experiencia unificada definidos para inspección y prueba posterior, no implementación de diseño.
