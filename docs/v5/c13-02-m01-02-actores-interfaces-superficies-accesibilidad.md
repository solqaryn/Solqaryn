# SOLQARYN V5 FINAL — C13-02.M01.02: actores, entradas, salidas y superficies de accesibilidad

**Fecha:** 2026-11-14. **Autor:** PRIMARY DEV 03 JAVIER, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Repositorio:** `solqaryn/Solqaryn@dev`; PROJECT_SCOPE_LOCK=STRICT. **Tipo:** preparación documental M01, sin cambios de Angular, Playwright, .NET, MySQL ni despliegue. **No equivale a certificación WCAG ni pruebas de accesibilidad ejecutadas.**

**Autoridades:** [Standard de Implementación de Automatizaciones V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). Antecedente C13-02.M01.01 (blob `7625e8638e3b228f8773ffe11541b0299228fda9`) delimita NR01–NR14, P01–P04 y N01–N12 futuros. El alcance de esta M01.02 es independiente por DAG VALIDADO/SI; M01.01 estaba VALIDANDO en preflight, de modo que se releerá su evidencia y A5 antes del cierre terminal M01.06. **VALIDANDO provisional no habilita fases de código ni QA.**

## 1. Actores y superficies A01–A14 (contratos futuros, sin crear usuarios ni roles)

| Actor / superficie | Entrada o interacción prevista | Frontera de responsabilidad y salida |
| --- | --- | --- |
| A01 Visitante anónimo | Navegar sitio público, enlace, banner y contenido | Lectura semántica y foco, sin PII ni datos privados |
| A02 Persona que usa exclusivamente teclado | Tab, Shift+Tab, Enter, Escape y flechas | Secuencia, retorno de foco y acciones disponibles sin ratón |
| A03 Persona con lector de pantalla | Encabezados, landmarks, labels, anuncios | Nombre/rol/estado comprensible, sin contenido innecesariamente duplicado |
| A04 Persona con baja visión | Zoom, escala, contraste y temas | Reflow, texto y controles legibles según criterios medidos |
| A05 Persona con sensibilidad al movimiento | prefers-reduced-motion del sistema | Evitar movimiento no esencial sin perder significado de estado |
| A06 Usuario autenticado de empresa A | Formularios, tablas y navegación de A | Angular accesible; autorización real JWT/RBAC/tenant A en backend |
| A07 Usuario autenticado de empresa B | Mismas superficies, sólo datos propios | Impedir fugas de B desde A y viceversa |
| A08 Administrador de empresa/sucursal | Menús y acciones según grant actual | UI no otorga permiso ni identifica recursos de otra empresa |
| A09 Administrador global autorizado | Configuración transversal por permisos | Gestión accesible, auditada y con scope verificado |
| A10 Soporte autorizado | Lecturas limitadas por política | Ni rol visual ni tecnología asistiva amplían acceso real |
| A11 Equipo UX/contenido | Textos, iconos, traducciones, orden tab | Diseño versionado y revisión antes de publicar |
| A12 Frontend Angular 22.2.1 / Material | Focus management, Signals, router, formularios | Árbol accesible, estado reactivo y feedback de error |
| A13 API .NET 10 / MySQL | Datos de estado/error/catálogo por endpoint | Autorización e integridad server-side; no cambios en esta unidad |
| A14 QA/auditor accesibilidad | Dispositivos, navegadores y lectores reales | Matriz reproducible por superficie, SHA y tecnología probada |

Estas son categorías funcionales: no se infieren discapacidad, permisos personales o obligaciones legales; no se crean nuevas cuentas ni accesos.

## 2. Entradas I01–I12 y validación prevista

| Entrada | Superficie | Contrato de tratamiento |
| --- | --- | --- |
| I01 Navegación ruta/enlace | Menú, breadcrumb, router | Foco y título al cambiar ruta, sin saltos confusos |
| I02 Tab/Shift+Tab/Enter/Escape | Botones, enlaces, menú | Operabilidad y orden lógico de interacción |
| I03 Apertura/cierre de modal | Dialog Angular Material | Foco inicial contextual, trampa sólo mientras corresponda, retorno al invocador |
| I04 Cambio de pestaña/selector | Tabs, filtros, switch | Rol y estado anunciado, sin elección dependiente sólo de color |
| I05 Input requerido o inválido | Formulario y fecha | Label asociado, descripción textual, error vinculado |
| I06 Respuesta de red 200/403/500 | Datos privados y error | Estado de carga, denegación y recuperación legibles; 403 no habilita acción |
| I07 Resultado vacío o paginación | Tabla/listado | Caption/encabezados, anuncio de cambios, scroll accesible |
| I08 Zoom/reflow/viewport | 320/390/1280 px y zoom a acordar | Contenido esencial disponible, sin scroll bidimensional innecesario |
| I09 Tema/alto contraste | Tokens de UI y OS forced-colors | Foco, texto, bordes y error distinguibles |
| I10 Movimiento reducido | OS prefers-reduced-motion | Sin transiciones indispensables o parpadeos que impidan uso |
| I11 Idioma/localización | Avisos, números, controles | Etiquetas y mensajes coherentes; no idioma incoherente para lector |
| I12 Cambio de tenant, rol o logout | Estado de sesión | UI refleja rechazo, backend revalida JWT/RBAC/tenant sin depender del cliente |

Toda I es una entrada potencial para pruebas futuras, no prueba ejecutada en este commit documental.

## 3. Salidas S01–S10 exigibles como oráculos futuros

- **S01:** ruta principal identificable mediante título y región `main`, con destino de skip-link.
- **S02:** control con nombre accesible y función semántica, operable por teclado.
- **S03:** modal abierto con foco contextual y restauración al cierre/escape.
- **S04:** error de validación asociado al campo, sin dependencia exclusiva de color ni icono.
- **S05:** estado de carga, vacío, éxito y fallo anunciado sin robo arbitrario de foco.
- **S06:** tabla con encabezados/caption y región desplazable por teclado cuando requiera scroll.
- **S07:** reflow y zoom medidos con superficie/viewport explícitos, sin pérdida de controles.
- **S08:** contraste, foco y modo alto contraste medidos, no declarados por la mera existencia de tokens CSS.
- **S09:** respeto de preferencias de movimiento y lectura de imágenes/alternativas pertinentes.
- **S10:** acceso autorizado sólo a datos propios; denegación A/B, revocación de rol y ausencia de PII en fallos.

## 4. Evidencia de código vigente y límites (blobs leídos)

| Origen del HEAD dev | Blob de archivo | Prueba que su código pretende cubrir y su límite |
| --- | --- | --- |
| `frontend/e2e/c00-03-m04-05-accessibility.spec.ts` | `e16a8ea375e12723a65786930dd3c4f73f69e63a` | Playwright de ruta de cobertura, teclado, 390/1280 px, 403 simulado; **no certifica JWT/MySQL real ni toda la app** |
| `frontend/src/app/features/centros-costo/centros-costo.a11y.spec.ts` | `5eeecfba3c039765c0d0f088e5f2db73abef408c` | Tests unitarios de labels, botones y error; no auditoría con lector real |
| `frontend/src/app/features/estados-financieros/estados-financieros.accessibility.spec.ts` | `f4edd5996a0c816b6fb8d2c2804149f4ac3263f1` | Labels, error aria-live, caption y headers en TestBed; no medidor global WCAG |
| `frontend/src/styles/_m10-accessibility.scss` | `6140d41b8a7e1e98f4fa1e43e0ab72f04b6ae29f` | Scroll margin, reduced-motion, forced-colors y utilidades; la presencia de reglas CSS no demuestra contraste final |
| `docs/v5/c00-02-m04-05-a11y-responsive.md` | `4f59f87620f79ea60cab78555da997ffcf97268a` | Contrato que exige Playwright real a 390/1280; su texto no es resultado de la presente ejecución |
| `docs/N4.10_ESTADOS_FINANCIEROS_A11Y_MATRIX.md` | `1f61347d54ef4617bdaa293b4e827ec1701d4fa1` | Matriz histórica N4.10, **no autoridad operativa ni certificación V5** |
| `docs/N4.11_CENTROS_COSTO_ACCESSIBILITY.md` | `86ab74b36aac398eea0f9e5627c904f035c93c72` | Nota histórica de evidencia, sin CI actual ni aprobación global |

Los siete blobs son fuentes de lectura, no un PASS causal de CI. M01.03 debe ampliar inventario de flujos reales, temas, lector de pantalla y superficies públicas/privadas. Los documentos históricos no reactivan el flujo VAEP retirado.

## 5. Fronteras de confianza y responsabilidades F01–F08

1. **F01 Browser/OS → Angular:** teclado, idioma, zoom y reduced-motion son señales de experiencia, no autenticación ni permisos.
2. **F02 Angular → API:** un guard de ruta accesible no concede acceso: JWT, RBAC, membresía/tenant A/B y recurso se validan por backend.
3. **F03 Datos/API → UI:** error de permisos no debe revelar registros o metadatos de B; mensajes accesibles pueden ser genéricos sin PII.
4. **F04 Librería Material/CSS → componente:** estilos de foco, estados, overlays, temas y color no bastan sin interacción real y pruebas.
5. **F05 Componente → lector de pantalla:** etiquetas y ARIA se verifican contra árbol accesible; atributos colocados sin prueba no equivalen a uso correcto.
6. **F06 Contenido/traducción → usuario:** instrucciones y errores dependen del contexto y deben revisarse lingüísticamente, sin texto jurídico inventado.
7. **F07 Browser E2E → CI:** tests con sesión sintética no prueban autenticación real, y suites SKIPPED/CANCELLED/QUEUED nunca son PASS obligatorios.
8. **F08 Repo dev → QA/PROD:** sólo documentación bajo Scope Lock; cualquier código, infraestructura, nueva dependencia o despliegue requiere su gate propio.

## 6. Pruebas conceptuales positivas y negativas (NO_EJECUTADAS)

| Par | Positivo futuro | Negativo futuro |
| --- | --- | --- |
| P01/N01 | Navegación con Tab y foco visible a acción válida | Botón solo mouse impide operar |
| P02/N02 | Dialog restaura foco al invocador | Escape cierra y foco se pierde en `body` |
| P03/N03 | Error del formulario anunciado y asociado a campo | Error solo cromático/sin nombre accesible |
| P04/N04 | Tabla desplazable y etiquetada en móvil | Región sin teclado o scroll que oculta la acción |
| P05/N05 | Estado 403 accesible sin datos ajenos | JWT A + selector B expone datos B |
| P06/N06 | Tema con contraste/foco medidos | Alto contraste o reduced-motion rompe funciones |
| P07/N07 | Actualización async anunciada sin perder foco | Mensaje nuevo silencioso o foco robado |
| P08/N08 | Imagen informativa tiene alternativa pertinente | Alt ausente o redundante impide comprensión |

Se exige, en fases ejecutables, artefacto Playwright/Chromium (y evaluación humana cuando corresponda), SHA, viewport, navegador/lector/OS, pasos, foco/árbol accesible, contrastes medidos y captura de caso negativo. Estos P/N son **oráculos FUTUROS**, no tests pasados de esta M01.02.

## 7. A0–A5, dependencias y rollback

- **A0:** diff sólo nuevo Markdown y sección completa de CHANGELOG, sin secretos, encabezados vacíos ni atribución cruzada.
- **A1:** inspección documental de A01–A14, I01–I12, S01–S10, F01–F08 y P/N01–08; casos NO_EJECUTADOS explícitos.
- **A2:** JWT/RBAC/tenant A/B y PII permanecen bajo API; mejorar UI nunca habilita un recurso ni un rol.
- **A3:** consistencia con antecedente M01.01 y siete fuentes Git; Angular build, .NET, MySQL y Playwright **NO_APLICA al diff Markdown**, no contabilizar PASS runtime.
- **A4:** publicación fast-forward CAS sobre HEAD vigente; Scope Lock y check V5 de procedencia `completed/success` del SHA exacto; readback Git/PLAN/COLA/BITACORA.
- **A5:** revisión técnica independiente por otra Task Javier, pendiente tras LISTO_DEV; no autoaprobación, no QA/main/PROD.
- **Reversión:** nuevo commit de git revert con HEAD revalidado y evento aditivo en BITACORA; no force-push, no eliminación histórica. No cambia ninguna UI/DB/permiso real; M01.06/M02 se sujetan a DAG y A5 causal.
