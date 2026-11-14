# FASE M10 — UI/UX empresarial y accesibilidad

Fecha de cierre: 2026-08-10  
Rama exclusiva: `Desarrollo`  
HEAD funcional certificado: `165c264333bc68b55660d16c832a103c7f3d9a8e`  
PR oficial: `#2 Desarrollo -> main`  
Estado: **COMPLETADA / CERTIFICADA AUTOMÁTICAMENTE**

## 1. Objetivo

Consolidar la UI de Solqaryn como una interfaz empresarial consistente, responsive y accesible, convirtiendo prácticas visuales y de accesibilidad ya existentes en un contrato transversal verificable y protegido contra regresiones.

M10 no reemplaza el tema administrable ni duplica componentes. Extiende el design system existente y fija estándares permanentes de tipografía, geometría, estados, reflow, navegación por teclado, foco, contraste, movimiento reducido y compatibilidad con modos de alto contraste.

## 2. Preflight real

Antes de modificar código se comprobó que Solqaryn ya disponía de una base avanzada:

- `frontend/src/styles/_enterprise-design-system.scss` con tokens semánticos;
- objetivos táctiles mínimos de 44px;
- foco visible global;
- `prefers-reduced-motion`;
- `prefers-contrast: more`;
- `forced-colors: active`;
- utilidades reutilizables de página, tarjetas, tablas, estados y feedback;
- componente `FeedbackStateComponent` accesible y reutilizable;
- skip-link al contenido principal;
- navegación semántica `aside/nav/main`;
- drawer móvil con soporte de Escape y devolución de foco;
- `ColorContrastService` con umbral WCAG AA 4.5:1;
- `ThemeApplierService` que corrige foregrounds cuando cambia la paleta administrable;
- auditorías E2E previas de semántica, responsive, labels e imágenes.

Por tanto, M10 cerró gaps de consistencia y verificabilidad en vez de introducir un segundo design system.

## 3. Design system empresarial consolidado

Se conserva y extiende la taxonomía de tokens:

- familia tipográfica;
- escala tipográfica;
- line-height;
- pesos;
- espacios;
- radios;
- sombras/elevación;
- tiempos y curvas de movimiento;
- objetivo táctil mínimo;
- ancho máximo de contenido;
- colores semánticos y foregrounds calculados.

Se añadió `frontend/src/styles/_m10-accessibility.scss` como capa transversal M10, cargada globalmente desde `styles.scss`.

Nuevas utilidades reutilizables:

- `.app-section`;
- `.app-section-heading`;
- `.app-section-title`;
- `.app-section-description`;
- `.app-toolbar`;
- `.app-field-grid`;
- `.app-control-group`;
- `.app-readable`;
- `.app-form-help`;
- `.app-form-error`;
- `.app-selected`;
- `.app-icon-action`.

Estas utilidades no alteran la lógica funcional de módulos existentes y reducen CSS ad hoc futuro.

## 4. Estados UX uniformes

Se mantiene como componente reusable `FeedbackStateComponent` para:

- loading;
- empty;
- success;
- error;
- warning.

Los estados usan texto, iconografía y semántica ARIA; no dependen únicamente del color.

Errores usan `role=alert` / live assertive. Los demás estados usan live polite y loading expone `aria-busy`.

## 5. Navegación SPA, teclado y foco

M10 añadió gestión explícita de foco después de cada `NavigationEnd` autenticado:

1. se identifica `main#main-content`;
2. se lee el `h1` de la pantalla cargada;
3. se actualiza una región live `polite` con `Página cargada: <título>`;
4. el foco se mueve programáticamente al contenido principal;
5. se posiciona el contenido al inicio sin animación forzada.

Esto evita que usuarios de teclado/lector de pantalla queden contextualmente en el enlace o botón de la pantalla anterior después de una navegación SPA.

Se preservan:

- skip-link visible al enfocarse;
- `main` programáticamente enfocable;
- orden natural de tabulación;
- menú móvil con foco inicial en cerrar;
- Escape para cerrar;
- devolución del foco al activador;
- `aria-controls` y `aria-expanded`;
- nombres accesibles de acciones iconográficas.

## 6. Guardas estáticas anti-regresión

`frontend/scripts/lint-quality.mjs` ahora bloquea además:

- `tabindex` positivos, que alteran artificialmente el orden natural del teclado;
- eliminación del foco mediante `outline: none`;
- eliminación del foco mediante `outline: 0`.

Estas reglas se ejecutan dentro del `npm run lint` habitual y, por tanto, afectan los gates generales de la aplicación, no solo M10.

La primera versión de la nueva hoja M10 utilizó internamente `outline: none` para el `main` programáticamente enfocado. Antes de certificar se corrigió a un outline transparente, de modo que la propia implementación respeta la guarda que introduce y mantiene comportamiento válido en forced-colors.

## 7. Contraste WCAG AA y tema administrable

Solqaryn mantiene `ColorContrastService.normalTextRatio = 4.5` para texto normal.

El gate M10 verifica en runtime combinaciones semánticas como:

- texto / superficie;
- texto secundario / superficie;
- encabezado / superficie;
- foreground primario / botón principal;
- foreground sidebar / sidebar.

El tema administrable no puede asumirse estático: `ThemeApplierService` conserva la corrección de foregrounds y asegura colores legibles cuando el administrador cambia la paleta.

M10 certifica contraste **WCAG AA para los pares semánticos automatizados**, sin afirmar una auditoría humana exhaustiva de cada combinación visual posible.

## 8. Responsive y reflow

M10 refuerza:

- `min-width: 0` en layouts flex/grid reutilizables;
- grids auto-fit sin ancho fijo obligatorio;
- toolbars y grupos de controles que envuelven contenido;
- controles apilables en móvil;
- scroll horizontal contenido dentro de tablas cuando corresponde, sin desbordar el documento;
- `scroll-margin-top` para destinos bajo barras sticky;
- diálogos limitados al viewport;
- contenido legible con ancho máximo semántico.

El gate específico comprueba 320px y 390px sin overflow horizontal del documento en rutas críticas.

Las auditorías integrales existentes cubren además extremos móviles y escritorio amplio.

## 9. Objetivos táctiles y controles

El design system conserva `--target-min: 44px`.

M10 certifica en runtime los controles del shell, incluyendo:

- navegación lateral;
- perfil;
- acciones de topbar;
- botones Material visibles.

No se limita a declarar la variable CSS: Playwright mide la geometría renderizada.

## 10. Movimiento, contraste reforzado y forced colors

Se preserva soporte para:

- `prefers-reduced-motion: reduce`;
- `prefers-contrast: more`;
- `forced-colors: active`.

Bajo reduced motion:

- scroll suave se desactiva;
- animaciones/transiciones no esenciales se reducen a duración prácticamente nula;
- las utilidades M10 no agregan movimiento necesario para comprender estados.

Bajo forced colors:

- focos y bordes continúan representables;
- selección no depende solo de un background personalizado;
- mensajes de error conservan señal adicional.

## 11. Regresión permanente M10

Archivo:

`frontend/e2e/m10-ui-ux-accesibilidad.spec.ts`

Resultado certificado desde el artifact del workflow:

**6 pruebas / 6 aprobadas / 0 fallos / 0 omitidas / 0 errores.**

Cobertura:

1. design tokens semánticos y contraste WCAG AA en tema aplicado;
2. skip-link, foco visible y targets táctiles mínimos;
3. drawer móvil captura y devuelve foco;
4. controles visibles de rutas críticas con nombre accesible;
5. 320px y 390px sin overflow horizontal del documento;
6. `prefers-reduced-motion` elimina movimiento no esencial.

Además continúa activa la regresión integral de Fase 8 sobre todas las rutas administrativas.

## 12. Gate específico M10

Workflow:

`M10 - UI UX empresarial y accesibilidad`

Run funcional certificado:

`31403468817` — **SUCCESS**

HEAD:

`165c264333bc68b55660d16c832a103c7f3d9a8e`

Pasos aprobados:

- MySQL 8.4 descartable;
- backend Release;
- API real contra MySQL;
- Node/npm;
- lint con guardas M10;
- build Angular de producción;
- Angular servido;
- Playwright M10;
- publicación de evidencia.

Artifact:

- nombre: `m10-ui-ux-accesibilidad`;
- ID: `9068739857`;
- SHA-256: `d1f498ac5ba97a9dd179bc648706fd53f7cb6090921848b41de229bc7bf93acd`;
- resultado Playwright verificado desde JUnit: 6/6, 0 fallos, 0 omitidas, 0 errores.

## 13. Gate transversal de compilación

Sobre el mismo HEAD funcional:

- `Desarrollo - Compilación y pruebas` run `31403468818` — **SUCCESS**.

Este gate volvió a aprobar, entre otros, frontend producción, backend Release, higiene y las validaciones MySQL/migraciones/variantes/cargas correspondientes.

La aceptación funcional integral se ejecuta adicionalmente antes del cierre definitivo del checkpoint y se registra en él una vez concluida.

## 14. Archivos principales

- `frontend/src/styles/_enterprise-design-system.scss` — base preexistente conservada;
- `frontend/src/styles/_m10-accessibility.scss` — contrato transversal M10;
- `frontend/src/styles.scss` — activación global;
- `frontend/src/app/app.component.ts` — foco SPA y live announcement;
- `frontend/src/app/app.component.scss` — shell accesible existente;
- `frontend/src/app/shared/feedback-state/feedback-state.component.ts` — estados reutilizables existentes;
- `frontend/src/app/services/color-contrast.service.ts` — contraste dinámico existente;
- `frontend/src/app/services/theme-applier.service.ts` — aplicación segura del tema;
- `frontend/scripts/lint-quality.mjs` — guardas anti-regresión;
- `frontend/e2e/m10-ui-ux-accesibilidad.spec.ts` — regresión específica;
- `.github/workflows/m10-ui-ux-accesibilidad.yml` — gate M10.

## 15. Seguridad del repositorio

M10 no requiere DDL/DML productivo ni migraciones nuevas de esquema.

Durante la fase:

- el trabajo funcional se publicó en `Desarrollo`;
- `main` no forma parte de la implementación;
- no se requiere desplegar Producción para certificar M10;
- no se modifican credenciales, dominios, bases, servicios ni activos productivos.

## 16. Cierre

**FASE M10 — UI/UX empresarial y accesibilidad: COMPLETADA / CERTIFICADA AUTOMÁTICAMENTE.**

Siguiente fase del Plan Maestro:

**M11 — Backups y restauración en Desarrollo.**

Producción queda fuera del alcance de M11 según el propio Plan Maestro.
