# SOLQARYN V16 — C13-03.M02.06: contrato de salida de rendimiento y ligereza

**ID:** C13-03.M02.06, PLAN_MAESTRO fila 1957. **Autor:** PRIMARY DEV 02 — JAVIER, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Clasificación V15:** R0, exclusivamente documental. **Repositorio:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), PLAN_MAESTRO/COLA/DAG_TECNICO y `docs/QUALITY_RISK_AUDIT_V15.md`. Salida exigida: dependencias y contrato de salida de M02 para carga inicial, navegación, imágenes, tablas, datos, móvil y accesibilidad. Este contrato no crea ni habilita frontend, middleware, caché, pruebas, servicios, endpoints, migraciones o proveedores.

## 1. A0 — dependencias, arquitectura y cuestiones no resueltas

- El DAG vigente exige `C13-03.M01.01-06+A5`, `C13-03.M02.01-05+A5`, `C05-04.M01.06+A5`, `C12-02.M01.06+A5`. La aptitud de esta documentación se verificó en PLAN H=PENDIENTE/I vacío, COLA G/O=SI y DAG F=VALIDADO/G=SI; la futura ejecución de código exige verificar las puertas otra vez.
- Los cinco contratos M02 anteriores contienen entidades E01–E14, REL01–REL12, INV01–INV18; propuestas API/DTO; persistencia y migraciones conceptuales; OPT01–OPT14, VAL01–VAL16; y EVT01–EVT12/INV01–INV14 de mediciones futuras. Son propuestas y NO prueban implementación, ejecución, ni CI de código.
- **Conflicto de bundle SIN_RESOLVER:** objetivos expresados como **650–750 KB frente a 1–2 MiB**; diferentes rangos y unidades no pueden adoptarse como presupuesto aprobado. No seleccionar uno arbitrariamente. Dejar pendientes unidad (transferido/comprimido/parseado), rutas, perfil, método de medición, aprobador y fecha.
- LCP, INP, CLS, TTFB, p95/p99, uso de memoria, tamaño transferido, imágenes existentes, tiempo en móvil, costo, energía y disponibilidad son **NO_MEDIDO**, sin muestras ni denominadores. Ni CI verde ni ausencia de medidas prueba rendimiento.
- Arquitectura actual: Angular 22.2.1 standalone, Signals, Material/CDK, Zone.js activo; backend ASP.NET Core 10 con autoridad JWT/RBAC/tenant; persistencia EF Core 10/MySQL Oracle. No permitir que front, query, caché, routing, correlation ID o etiqueta enviada por el cliente otorguen acceso. No se cambia ningún archivo ejecutable, secreto ni base de datos.
- Cualquier modificación futura de UI exige build/tipado, accesibilidad, navegación real y Playwright/Chromium proporcional. Backend, JWT/RBAC, cachés privadas o MySQL requieren casos negativos tenant A/B y tests integrados si cambia runtime. Un mock no acredita MySQL.

## 2. G01–G16 — puertas obligatorias futuras

| Gate | Evidencia requerida antes de salida técnica | Sin evidencia |
| --- | --- | --- |
| G01 | Inventario de rutas públicas/privadas, owners y superficies de carga | Sin cobertura declarada |
| G02 | Baseline repetible con SHA, toolchain, dispositivo, red y perfil de cache | NO_MEDIDO |
| G03 | Resolución firmada del conflicto 650–750 KB / 1–2 MiB con unidades | Budget no aprobado |
| G04 | Medidas LCP/INP/CLS/TTFB con número de muestras y método | NO_MEDIDO |
| G05 | Lazy routing y carga inicial reproducible por perfil y permiso | Sin PASS de navegación |
| G06 | Inventario real de imágenes, cero permitido, alt/tamaño/URLs seguras | Sin migración ficticia |
| G07 | Datos/tablas, paginación/virtualización, estados vacíos y límites | Sin PASS de escalado |
| G08 | Experiencia táctil móvil, reflow, zoom y red lenta en navegador real | Sin PASS móvil |
| G09 | Teclado, foco, labels, contraste, estados error/carga y lector de pantalla | Sin PASS accesible |
| G10 | JWT/RBAC, revocación/IDOR y tenant A/B en rutas, cargas y cachés | Denegación fail-closed |
| G11 | El backend sigue validando; optimización no omite errores/roles | No activar UI insegura |
| G12 | Caché y métricas segregadas por tenant/rol/usuario sin PII | Deshabilitar caché privada |
| G13 | Instrumentación versionada sin tokens, cuotas/idempotencia aprobadas | No activar proveedor |
| G14 | Tests funcionales positivos, negativos, frontera y error reales, sin P0/P1 | No certificar A1–A3 |
| G15 | SHA dev, Scope Lock y V5 SUCCESS causal, rollback y diff revisado | A4 no acreditado |
| G16 | PLAN/COLA/DAG/BITACORA readback, A0–A4 y A5 independiente otra Javier | No liberar siguiente etapa |

Las dieciséis entradas son **gates contractuales futuros**, NO dieciséis pruebas ejecutadas. Toda futura medición debe conservar SHA, herramienta/versión, ambiente, perfil, muestra/denominador, autor, fecha, resultado y regresión; de faltar, reportar NO_MEDIDO. Una futura integración real con Cloudinary u otro proveedor requiere permiso explícito y cuotas; no se compra ni despliega nada en este scope.

## 3. A1–A3 — T01–T12, oráculos FUTUROS_NO_EJECUTADOS (0/12)

| Test | Positivo futuro | Negativo, error o frontera futura |
| --- | --- | --- |
| T01 | Presupuesto firmado con unidad y perfil sirve para comparar | 650–750 KB y 1–2 MiB contradictorios bloquean |
| T02 | Mismo SHA/red/herramienta admite baseline comparable | Otra configuración o SHA no hereda PASS |
| T03 | Lazy route autorizada carga el mínimo | Ruta sin permiso no precarga recursos privados |
| T04 | Usuario válido conserva scope al navegar/refrescar | Tenant B no accede a caché de tenant A |
| T05 | Imagen existente usa dimensiones/alt/carga proporcional | Inventario cero no genera carga o migración ficticia |
| T06 | Tabla larga pagina o virtualiza sin bloquear teclado | Datos/cardinalidad extremos no congelan ni fugan PII |
| T07 | Vacío/error/reintento limitado dan feedback claro | Timeout o doble acción no duplica operación |
| T08 | Mobile reflow/zoom navega sin overflow | Teclado virtual o viewport estrecho no oculta foco |
| T09 | Modal preserva labels, foco y accesibilidad | Cerrarlo no deja foco atrapado |
| T10 | SLI usa numerador/denominador, N, unidad y ventana | Sin N o perfil: NO_MEDIDO |
| T11 | Evento sintético autorizado es idempotente y sin datos privados | JWT/cookie/tenant como etiqueta se rechaza |
| T12 | CI completed/success del SHA y revert conservan evidencia | CANCELLED/SKIPPED/IN_PROGRESS nunca PASS |

**Ejecución en esta microtarea = 0/12.** Son criterios de aceptación futuros, no Lighthouse, Playwright, Chrome, xUnit, API, MySQL, integración o benchmark ya ejecutados. **A2** es revisión estática de reglas de privacidad, tenant A/B, revocación, IDOR, roles y seguridad de caché. **A3** para runtime/Angular/Playwright/MySQL/HTTP = **NO_APLICA** al diff Markdown R0, con justificación de ausencia de cambios ejecutables.

## 4. RB01–RB08 — rollback y contención futura

- **RB01:** deshabilitar opción de rendimiento insegura solo desde backend autorizado y tipado, manteniendo auditoría.
- **RB02:** revertir lazy routing/precarga que afecte seguridad o navegación sin eliminar historia.
- **RB03:** invalidar cachés privadas en tenant/usuario/rol, preservar separación A/B.
- **RB04:** retirar optimización de imagen o tabla que cause regresión, sin borrar originales ni migrar inventario cero.
- **RB05:** restaurar enfoque, teclado, zoom y estados de error antes de insistir en throughput.
- **RB06:** suspender experimento sintético no comparable, contar muestras excluidas, reportar NO_MEDIDO.
- **RB07:** cortar instrumentación con PII/costo/cardinalidad excesivos, conservar señales seguras.
- **RB08:** revertir cambios en dev por `git revert` aditivo con HEAD verificado; mantener BITACORA y exigir A5 nuevo cuando cambie el artefacto.

## 5. A4/A5, alcance real y condición de salida

**A0:** requisitos M02.06, límites, dependencias y artefactos revisados; solo este documento y sección autocontenida `CHANGELOG_AI.md`. **A1:** 12 oráculos positivos/negativos especificados, 0 ejecutados. **A2:** revisión estática JWT/RBAC/tenant, IDOR, privacidad y caché, sin modificación de seguridad real. **A3:** runtime y tests integración NO_APLICA motivado. **A4:** publicar commit fast-forward/CAS solo en dev, verificar en exact-SHA `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` como completed/success, comprobar diff/blobs y readback PLAN/COLA/DAG/BITACORA. CI auxiliar no causal no se transforma en PASS. **A5:** otra Task JAVIER, TASK_ID distinto, debe revisar contratos, autoría, pruebas y riesgos y puede rechazar.

`LISTO_DEV` de la especificación no certifica G01–G16, web vitals, budget, infraestructura ni promueve a QA/main/PROD. Ningún evento, endpoint, worker, selector de presupuestos, migración ni servicio queda activado por redactarla. En futura tarea ejecutable R1/R2 se requieren tests reales y autorización vigente.
