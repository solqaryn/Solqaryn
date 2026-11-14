# SOLQARYN V5 — C05-02.M01.01: alcance, exclusiones y no-regresión del branding

**Programa:** C05 — Builder, design system y configuración segura. **Lote:** C05-02 — Branding y presets. **Fase:** M01 — preparación; microtarea 01. **Autor:** PRIMARY DEV 01 / JAVIER_CHATGPT / TASK_ID \`6adbb9fff1548191a1bd5b30d9e9984a\`. **Fecha:** 2026-11-14. **Repositorio:** \`solqaryn/Solqaryn@dev\`, Scope Lock STRICT. **Fuente:** Standard de Implementación de Automatizaciones V5 FINAL, documento Drive \`1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA\`, PLAN_MAESTRO fila 758.

## Objetivo medible y límite de esta entrega

Establecer la frontera y el contrato de no-regresión para logos, temas visuales, identidad empresarial, presets por rubro y futuras revisiones versionadas del lote C05-02. El objetivo *futuro del lote* es permitir previsualizar, guardar, publicar y revertir branding de forma trazable, segura, accesible y aislada por empresa, conservando una experiencia coherente con C05-01. **Esta microtarea M01.01 solo documenta objetivo, exclusiones y oráculos de aceptación; no implementa presets, publicación versionada ni rollback transaccional.**

Una marca nunca otorga rol, permiso, pertenencia a empresa, visibilidad de registros ni acceso a API. Todo cambio futuro de tema o preset debe atravesar autorización real del backend, saneamiento de entradas, versiones, auditoría, CI de contraste/responsive y rollback comprobable. Una vista previa no equivale a publicación. El texto visible y la selección de tema no sustituyen un JWT válido ni la resolución server-side del tenant.

## Evidencia estática de la implementación actual

Se inspeccionaron en \`dev\` los siguientes archivos y blobs; la lectura de código **no es prueba ejecutada de navegador ni certificación de seguridad**:

| Archivo | Blob git | Hecho observable y límite |
| --- | --- | --- |
| \`frontend/src/app/services/tema-visual.service.ts\` | \`588aa46fc4c10f8d6be85628e919a7bd89f366ac\` | Expone \`get\`, \`update\` y \`restaurar\` sobre \`/tema-visual\`; no acredita historial de revisiones ni publicación por lotes. |
| \`frontend/src/app/services/theme-applier.service.ts\` | \`cbccc2795cb1b010f74ce5b2b4dc1fb20f0b41f3\` | Lista campos permitidos, acepta solo colores hex, limpia tokens anteriores, evita respuesta de tema obsoleta y calcula contraste; no convierte CSS arbitrario en permiso. |
| \`frontend/src/app/services/color-contrast.service.ts\` | \`2b789f4af6c345068dc689c727313b859f438364\` | Calcula contraste y foreground; necesita prueba por estado y combinación de colores. |
| \`frontend/src/app/services/empresa-identidad.service.ts\` | \`88526c37ad3d50dc454e8e23ce7899818a0ada72\` | Mantiene identidad visible con Signals, limpia contexto y evita respuesta tardía de empresa anterior; no representa autorización. |
| \`frontend/src/app/services/empresa-configuracion.service.ts\` | \`0e4f127bc2a7dce504a7318546231fb0776903fe\` | Hay endpoints de logo y configuración; rutas tenant incluyen IDs del contrato HTTP, sin demostrar validación de su propiedad desde el navegador. |
| \`frontend/src/styles.scss\` | \`2eb48562fd77c758662af6b9683649b4f9698a64\` | Colores semánticos y foco global; no certifica todas las marcas y contrastes. |
| \`frontend/src/styles/_enterprise-design-system.scss\` | \`6a188590af9a958bff2e3e0375c0c46cdb2b14e6\` | Escala de tipografía, espaciado, responsive y \`--target-min:44px\`; su aplicación debe probarse en consumidores reales. |

Ninguno de estos blobs demuestra la existencia de un editor de presets por rubro ni auditoría/versionado ya integrada. La ruta de \`getTenant(empresaId)\` es una **superficie de riesgo** que exige prueba backend de que el ID del cliente no autoriza acceso cruzado; no se declara hallazgo de vulnerabilidad sin prueba causal.

## Fronteras de confianza y diseño futuro (sin ejecución)

| Frontera | Regla exigida para implementación posterior | Evidencia futura |
| --- | --- | --- |
| Marca/preset editable → validador | Campos tipados/allowlisted; rechazar CSS, HTML, JS, URLs no admitidas, payload malformado y claves no conocidas | pruebas de validación + negativas |
| Actor empresarial → API | JWT válido, membresía activa, permiso de edición y empresa resuelta server-side; jamás inferir tenant desde URL, tema o localStorage | pruebas HTTP A/B de lectura y escritura |
| Borrador/preview → publicación | Preview aislada, versión esperada, revisión y autoría; nunca persistir por simple renderizado | escenarios concurrentes, previsualización vs. publicación |
| Tema A → contexto B | Restablecer tokens y logos al cambiar empresa; descartar respuestas antiguas y caché cruzada | pruebas de carrera A→B y logout |
| Logo subido → almacenamiento | MIME real, tamaño, checksum, autorización, claves segregadas y URL segura según contrato vigente | pruebas con archivo válido, archivo engañoso y A/B |
| Color de marca → contenido | Contraste por combinación y estado, foco visible, texto de feedback no solo color y reduced-motion | matriz de accesibilidad y E2E |
| Revisión publicada → restauración | Restauración versionada con concurrencia controlada y auditoría; no sobrescribir evidencia histórica | pruebas de rollback y fallos parciales |

La arquitectura declarada sigue Angular 22.2.1 standalone, Material/CDK y Zone.js, .NET 10, MySQL 8.4, JWT/RBAC backend y Secrets fuera del repo. Los componentes del builder y la identidad futura deben reutilizar contratos existentes antes de crear nuevos adaptadores.

## Contrato de no-regresión NR01–NR12

| ID | Invariante verificable | Condición de NO_GO |
| --- | --- | --- |
| NR01 | El branding modifica presentación y nunca la decisión de autorización backend | Tema o logo cambia permisos/roles |
| NR02 | La identidad pública y empresarial no mezcla datos entre tenants A/B | Cache/branding de A visible en B |
| NR03 | Los valores visuales editables pasan por lista de campos y formato válidos | CSS/HTML/JS arbitrario admitido |
| NR04 | El contraste y el foco siguen visibles tras aplicar colores | Texto/foco ilegible por cambio de tema |
| NR05 | Preview y edición no publican estado sin decisión explícita y autorizada | Render de prueba dispara escritura |
| NR06 | Publicar con versión obsoleta falla sin sobrescribir revisión posterior | Lost update entre dos editores |
| NR07 | La reversión preserva historial y produce rastro auditable | Revert altera o pierde evidencia previa |
| NR08 | Logo y medios respetan tipo, tamaño, dueño y URL segura | Payload disfrazado o acceso A→B |
| NR09 | El cambio de empresa invalida respuestas tardías y tokens visuales del tenant anterior | Tema A restaura estado tras entrar B |
| NR10 | Las preferencias responsive, 320px, teclado y labels no se degradan | Scroll global u operación solo por mouse |
| NR11 | Presets por rubro no introducen campos de autorización ni bypass de contratos | Preset se interpreta como atributo RBAC |
| NR12 | Un fallo de API/almacenamiento deja tema seguro y no publica parcialmente | Estado semiaplicado sin rollback |

## Oráculos de aceptación futuros: no son pruebas ejecutadas

**Positivos P01–P06:** P01 tema válido de A aplica solo tokens allowlisted; P02 al pasar de A a B desaparecen los tokens de A; P03 preview no modifica la versión publicada; P04 publicación autorizada persiste versión y auditoría; P05 restaurar revisión anterior genera historia reversible; P06 branding mantiene contraste, foco y responsive de 320px.

**Negativos N01–N12:** N01 CSS arbitrario; N02 script incrustado; N03 clave desconocida; N04 color malformado; N05 tema de A consumido por B; N06 logo de empresa ajena; N07 publicación sin JWT/permiso; N08 preview que persiste; N09 respuesta tardía A→B; N10 edición concurrente sin versión; N11 contraste insuficiente; N12 fallo parcial de upload/publicación. Ninguno de esos oráculos se presenta como PASS de runtime; deben automatizarse y ejecutarse en las etapas reales.

## Dependencias, auditoría y rollback

El DAG dinámico para este ID se obtiene de \`DAG_TECNICO!A719:G719\` y el preflight de \`PLAN_MAESTRO!H758/I758\`, \`COLA!G758/O758\`. Es autorización de **reclamar preparación documental**, no de habilitar publicación de marca, migraciones, JWT/RBAC, cambios productivos, M01.06 o QA/main/PROD. Revalidar dependencias y review A5 de cierres pertinentes cuando el gate técnico lo exija.

- **A0:** inspección del source, diff documental y campos de seguridad; ninguna ruta de runtime cambia.
- **A1:** comprobar especificación de los P01–P06 y N01–N12, sin declarar pruebas funcionales ejecutadas.
- **A2:** control estático de no elevación de JWT/RBAC/tenant, entradas permitidas y aislamiento A/B.
- **A3:** trazabilidad entre servicios, tokens y criterios; no atribuir ejecución de MySQL, HTTP o Playwright.
- **A4:** publicación fast-forward en \`dev\`, verificación de blobs, Scope Lock y procedencia CHANGELOG \`completed/success\` exact SHA, PLAN/COLA/BITACORA readback.
- **A5:** revisión independiente por otro TASK_ID Javier, nunca esta Task autora; pendiente hasta evidencia adicional.

Rollback documental mediante \`git revert\` aditivo del commit correspondiente, sin force-push. La implementación posterior de themes/versiones tendrá su propio plan de reversión y pruebas; quedan prohibidos cambios en QA/main/PROD, datos productivos, servidores, dominios, nuevas Tasks, webhooks, workers, eventos y migraciones en esta unidad.
