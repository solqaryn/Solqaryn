# SOLQARYN V16 — C13-03.M01.06: bloqueos, rollback y liberación documental de rendimiento

**Unidad:** C13-03.M01.06; PLAN_MAESTRO fila 1951; clasificación **R0 documental**; PRIMARY DEV 04, TASK_ID \`6aeacb7820ac8191b7d768a6f33dfa09\`; \`solqaryn/Solqaryn@dev\`; \`PROJECT_SCOPE_LOCK=STRICT\`. Documento de preparación, **no implementación**, no certificación de performance ni aceptación del capítulo.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive \`1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA\`); Sheet \`1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU\`; PLAN H/I, COLA G/O y DAG F/G consultados por ID. Una única EN_PROGRESO propia. No crea tareas, ramas, servicios ni contratos de producción.

## 1. Dependencias y alcance

La salida documental requiere \`C13-03.M01.01–05\` en LISTO y A5 independiente; DAG también declara predecesoras de preparación \`C05-04.M01.06+A5\`, \`C12-02.M01.06+A5\` y dependencias transversales de C03/C05/C06/C12. La fórmula dinámica DAG constituye **un filtro de elegibilidad**, no sustituye pruebas reales ni autoriza QA, producción o cutover. Revalidar predecesoras, revocaciones A5 y propietario antes de reclamar una M02.

| Fuente M01 | Resultado reutilizable solo como contrato | Límite |
| --- | --- | --- |
| M01.01: alcance/no regresión | NR01–NR14 y oráculos futuros de experiencia ligera | No hay medición de navegadores ni uptime |
| M01.02: actores, entradas y salidas | Usuarios, contextos tenant y fronteras de UX | No equivalen a pruebas E2E |
| M01.03: inventario de código y brechas | Blobs de rutas, cache, medios, tests y presupuesto | Conflicto **650–750 KB vs 1–2 MiB** aún sin resolver en implementación |
| M01.04: riesgos y decisiones | Riesgos de caché, navegación, carga de datos y exposición PII | Los riesgos son hipótesis, no defectos runtime probados |
| M01.05: criterios de aceptación | Positivos, negativos y casos frontera propuestos | FUTUROS_NO_EJECUTADOS; no pasar benchmarks inventados |

La arquitectura actual permanece Angular 22.2.1, ASP.NET Core 10, EF Core 10.0.12/Oracle MySQL provider y MySQL 8.4. En este alcance no se toca frontend, API, infraestructura ni persistencia; no se emiten trabajos de carga, browser o migración. Las mediciones deben ser reproducibles por **dispositivo, red, build SHA, rol y tenant A/B** con denominador y umbrales explícitos, no mediante impresión subjetiva.

## 2. Dieciséis puertas G01–G16 de no liberación

| Gate | Exigencia para futura implementación o certificación |
| --- | --- |
| G01 | Trazar cada criterio de M01.01–05 a cambio/caso con ID y evidencia |
| G02 | Resolver por decisión registrada el conflicto de presupuesto 650–750 KB vs 1–2 MiB; no escoger un valor por comodidad |
| G03 | No declarar LCP/INP/CLS ni TTFB alcanzados sin resultados de herramienta, entorno, muestreo y umbral |
| G04 | Conservar el contenido privado fuera de HTML/cache pública/CDN y bloquear filtraciones de tenant B hacia A |
| G05 | Backend sigue verificando JWT, rol, membresía y revocación; los guards Angular no otorgan autoridad |
| G06 | Eliminar secretos, identificadores de sesión y PII de RUM, trazas, errores y cabeceras registradas |
| G07 | Navegación teclado, foco, estados de error, contraste y movimiento reducido no se degradan por lazy loading |
| G08 | Recursos críticos visibles mantienen tamaño/reserva para evitar CLS al cargar imágenes y tablas |
| G09 | Datos comerciales, inventario, importes y transacciones conservan consistencia; no hacer caché de decisiones de autorización |
| G10 | Comprobar variantes móvil 320 px, tablet y escritorio, con redes limitadas y errores/reintentos |
| G11 | A/B y roles se prueban con negativos que FALLAN si el cliente ve datos de otro contexto |
| G12 | Revalidar ETag/304/TTL y \`Cache-Control\` en flujos públicos; privados fail-closed |
| G13 | No usar datos reales ni servicios de pago para pruebas; fixtures sintéticos sin secretos |
| G14 | CI relevante exact-SHA completada SUCCESS; SKIPPED/CANCELLED/QUEUED/IN_PROGRESS no valen PASS |
| G15 | Documentar rollback reversible de artefactos/configuración antes de alterar chunking/cache/medios |
| G16 | No habilitar M02 ni aceptar A5 propio si faltan predecesores, A0–A4, Scope Lock, procedencia V5 y revisión independiente |

Los gates aquí son controles **propuestos/pendientes**, no evidencias de su ejecución. El documento no modifica bundles, CSP, CDN, cabeceras, rutas, middleware, imágenes ni reglas de tenancy.

## 3. Ocho acciones RB01–RB08 (plan, no ejecutadas)

| ID | Reversión condicionada |
| --- | --- |
| RB01 | Registrar SHA anterior, lockfiles, config tipada y thresholds antes de probar cambios |
| RB02 | Revertir con commit normal cambios UI o de carga solo tras comparar HEAD con otros escritores |
| RB03 | Invalidar únicamente caché pública con scope y TTL acordados; nunca borrar estados de otros tenants |
| RB04 | Reponer carga crítica y estilos de reserva si aparece CLS/regresión responsive |
| RB05 | Restaurar configuración versionada del presupuesto y conservar evidencia del conflicto hasta su decisión |
| RB06 | Comprobar auth 401/403, roles revocados y aislamiento A/B tras revertir |
| RB07 | Verificar funcionalidad de inventario, precios, formularios y navegación E2E con fixtures de prueba |
| RB08 | Adjuntar métricas antes/después, runs, logs, owner, criterio de liberación y readback institucional |

Para el **cambio presente exclusivamente Markdown**, rollback autorizado es \`git revert\` normal del commit tras revalidar dev/HEAD, sin borrar BITACORA. RB01–RB08 no han sido ejecutados.

## 4. Diez pares de pruebas T01–T10 — FUTUROS_NO_EJECUTADOS

| Par | Positivo futuro | Negativo/frontera futuro |
| --- | --- | --- |
| T01 | Bundle respeta presupuesto formalmente aprobado | Exceso de KB o MiB hace FALLAR el umbral elegido |
| T02 | LCP/INP/CLS medidos reproducibles por perfil | Métrica sin dispositivo/denominador no permite PASS |
| T03 | Primer contenido móvil legible y accesible | Red lenta y 320 px sin contenido o con salto visual falla |
| T04 | Caché pública retorna 304/ETag sin PII | Respuesta autenticada cacheada públicamente falla |
| T05 | Sesión A solo ve sus datos y roles | Cambio a tenant B o revocación exhibe datos privados |
| T06 | Movimiento reducido conserva funcionalidad | Animación indispensable, foco perdido o teclado bloqueado falla |
| T07 | Imagen principal tiene prioridad y dimensiones | Imagen lazy above-the-fold o CLS detectado falla |
| T08 | Tabla grande degrada progresivamente con datos íntegros | Datos truncados, importes incorrectos o filas cruzadas fallan |
| T09 | Ruta protegida maneja 401/403 sin contenido sensible | Refresh o retry muestra pantalla privada a no autorizado |
| T10 | Reversión deja build y smoke equivalentes al baseline | Revert incompleto/CI cancelada se intenta contabilizar PASS |

**Cobertura de ejecución de estos pares en esta microtarea: 0/10.** Se especifican oráculos futuros, sin alegar mediciones de LCP, INP, CLS, Playwright, MySQL, .NET, Angular ni seguridad HTTP. No usar conclusiones de otra CI ni otro SHA como prueba de estos escenarios.

## 5. A0–A5, salida y no autorización

- **A0 (documental):** correspondencia con M01.01–05, alcance y diferencia limitada a este Markdown + sección autocontenida de CHANGELOG; clasificación R0; plan de reversión.
- **A1 (documental):** matrices de 16 gates, 8 acciones RB y 10 pares positivos/negativos definidos, sin declararlos ejecutados. Calidad de redacción y consistencia de identificadores comprobables por inspección.
- **A2 (documental):** requisitos explícitos JWT/RBAC/tenant A/B, revocación, PII, caché y denegación por defecto; no se altera seguridad de runtime.
- **A3 (documental):** referencias coherentes con fuentes M01 y arquitectura; .NET/MySQL/browser tests NO_APLICA al diff Markdown, pero serán obligatorios si futura M02 modifica esas capas.
- **A4:** publicar con fast-forward y expected HEAD, Scope Lock y check "V5 - Sin encabezados vacios ni atribucion cruzada" \`completed/success\` en SHA aplicable, readback PLAN/COLA/DAG/BITACORA; no inventar PASS.
- **A5:** revisión real por otra de las cinco Tasks Javier, ID diferente del autor; si falla revisión preservar defecto hasta reparación y nueva evidencia.

**Condición de liberación:** LISTO_DEV acredita solo **contrato R0**, no optimización realizada. A5 independiente y prerequisitos verdaderos condicionan M02; discrepancia de presupuesto permanece ABIERTA y métricas \`NO_MEDIDO\`. Nada aquí autoriza cambios en QA, main, PROD, DNS, Cloudflare/Hostinger, servicios pagos, credenciales o bases de datos reales.
