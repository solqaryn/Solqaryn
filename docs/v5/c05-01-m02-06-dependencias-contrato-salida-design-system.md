# SOLQARYN V16 — C05-01.M02.06: dependencias tecnicas, bloqueos y contrato de salida de design system

**Microtarea:** C05-01.M02.06, PLAN_MAESTRO fila 733. **Owner y autor:** PRIMARY DEV 04 JAVIER, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`; **rama:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Fuente oficial:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Fecha:** 2026-11-15. **Riesgo V15:** R0, salida exclusivamente documental sin cambio de código.

## 1. Contrato de alcance y dependencia

La unidad establece las **dependencias técnicas y la condición de salida del lote M02** del sistema de diseño universal: tipografía, color, espaciado, estados, formularios, tablas, modales, navegación, temas de tenant y accesibilidad. **No** crea componentes Angular, archivos CSS, DTO o endpoints .NET, API, EF/MySQL, tablas, migraciones, nuevos JWT/roles, secrets, webhooks, eventos, servicios, pagos o despliegue. Los tokens, configuraciones y fixtures descritos son conceptuales hasta autorización de M03 y CI causal; el tema nunca otorga roles o acceso a recursos.

**Preflight dinámico:** PLAN H733=PENDIENTE/I733 vacío; COLA G733/O733=SI; DAG_TECNICO A694 F=VALIDADO/G=SI tras A5 independientes de C05-01.M02.01–05. El claim de esta unidad se registra con CAS de una sola celda y readback. M01.06 es preparación previa, no aceptación de UI live. Los sucesores deben releer gate y A5 por ID; no heredan un SI estático.

Stack exigido por referencias `dev`: Angular 22.2.1 standalone con Material/CDK y Zone.js 0.16.3; ASP.NET Core 10, JWT/RBAC backend; EF Core 10.0.12 y proveedor Oracle MySql.EntityFrameworkCore 10.0.9. Autoridad empresarial se decide en servidor con membresía y permiso vigentes, nunca desde Angular.

## 2. Cinco artefactos M02 causales y límite de atribución

| ID | Ruta dev | Blob revisado | Alcance verificado |
| --- | --- | --- | --- |
| C05-01.M02.01 | docs/v5/c05-01-m02-01-entidades-ownership-invariantes-design-system.md | 51148593f9bf9dc38eb0534f287092a5aef4912c | 14 entidades DS, 18 invariantes, modelo conceptual |
| C05-01.M02.02 | docs/v5/c05-01-m02-02-contratos-api-dto-errores-compatibilidad-design-system.md | 5f24b365f4aa0ec5afa440be877f205b9fa2cbdd | 8 API/DTO, 12 errores, 16 validaciones conceptuales |
| C05-01.M02.03 | docs/v5/c05-01-m02-03-persistencia-indices-migracion-fixtures-design-system.md | 503292547d77c3905529b9883a0464fe7cc0c635 | 12 persistencias, 12 indices, 12 migraciones condicionales, 10 fixtures futuros |
| C05-01.M02.04 | docs/v5/c05-01-m02-04-configuracion-tipada-flags-design-system.md | 818e4355959501a81ebdb32cda66c9fbd5051683 | 14 opciones tipadas y 18 reglas |
| C05-01.M02.05 | docs/v5/c05-01-m02-05-eventos-idempotencia-correlacion-design-system.md | 4572dc2ef9889440395b479db72b92f4408693d0 | 12 eventos INERTES, 14 reglas de correlacion e idempotencia |

Cada antecedente se encuentra en LISTO con A0-A4 registrados y A5 independiente acreditada a través de PLAN/BITACORA, con autor/revisor diferentes. Especialmente M02.03 registra un Scope Lock FALLIDO de un SHA anterior por otro archivo de test; la revisión independiente debe comprobar descendiente con controles exitosos y blob documental preservado, **sin convertir el fallo original en PASS**. Los oráculos positivos/negativos de M02 son FUTUROS_NO_EJECUTADOS.

## 3. Matriz de dieciseis puertas G01-G16 para fases M03-M06 futuras

| Gate | Evidencia requerida | NO_GO especifico |
| --- | --- | --- |
| G01 | A5 independiente en C05-01.M02.01-.05 y M01.06, HEAD/owner/CI causales vigentes | Sin A5 o con evento A5_FAIL no abrir M03 |
| G02 | Matriz de DS01–DS14/INV01–INV18 y ownership tenant verificado | No crear EF/API de entidad conceptual sin revision de propiedad |
| G03 | API01–API08/DTO tipados con validacion, errores seguros, version y compatibilidad | No publicar endpoint ni consumir permisos sugeridos por cliente |
| G04 | Config OPT01–OPT14 con valores admitidos, precedence, flags fail-closed | No permitir CSS/HTML/URL arbitrario, bypass RBAC ni revelacion tenant |
| G05 | MIG01–MIG12/IDX01–IDX12 aditivos y rollback validado en MySQL 8.4 EFIMERO | No tocar schema Aiven/PROD, ni reportar tests Category=Integration no corridos |
| G06 | JWT, pertenencia persistida, grant RBAC y asignacion tenant A/B comprobados en backend | No aceptar header/slug/Angular/theme como autorizacion |
| G07 | Revocacion, logout y cambio tenant limpian caches, epoch, branding y respuestas tardias | No visualizar preset de A cuando el usuario pertenece a B |
| G08 | Version optimista/CAS e idempotencia por tenant+actor+payload+revision | No activar dos temas con mismo key ni replay entre empresas |
| G09 | Eventos EV01–EV12 solo tienen contrato, no se despachan hasta autorizacion | No crear broker, outbox, webhook, worker ni SMTP por este Markdown |
| G10 | Tokens semanticos, contraste y estados de interfaz con aceptacion medible | No declarar WCAG o contraste aprobado por tabla conceptual |
| G11 | Teclado, labels, foco, ESC, errores, resize 320/768/1280, zoom y motion | No publicar UI sin Playwright/inspeccion real pertinente |
| G12 | Version de assets/media confiables y URI allowlist con fallback neutral | No conservar logo de tenant anterior o cargar tercero no aprobado |
| G13 | Auditoria minima y correlation IDs opacos sin JWT/PII/secretos | No registrar datos sensibles en eventos/CI/logs |
| G14 | Pruebas unitarias positivas, negativas, frontera y error con asserts reales | FUTUROS_NO_EJECUTADOS nunca PASS funcional |
| G15 | Scope Lock STRICT, V5 provenance, checks pertinentes SUCCESS en SHA/descendiente probado | CANCELLED/SKIPPED/queued/in_progress/failed nunca equivalen a PASS |
| G16 | A0-A4 por microtarea; A5 otra Task Javier, plan DAG/COLA y rollback preservados | LISTO_DEV y A5 no autorizan QA/main/PROD/infraestructura |

Ninguna puerta G01–G16 se satisface por afirmar la existencia de un documento. Los checks de M03/M04/M05/M06 deben provenir de commits causales y mostrar pruebas reales del componente. Los sucesores técnicos se liberan únicamente con PLAN H=PENDIENTE/I vacío, COLA G/O=SI, DAG F=VALIDADO/G=SI y dependencias A5 verdaderas.

## 4. Ocho estrategias de rollback condicionadas

| Rollback | Disparador futuro | Accion segura |
| --- | --- | --- |
| RB01 | Incoherencia contrato/blobs | Detener M03 afectada, releer M01/M02 y revertir solo documento con git revert normal |
| RB02 | Branding tenant A aparece en B | Invalidar epoch, limpiar cache/DOM y probar A/B con backend de prueba |
| RB03 | Revocacion/permiso desatendido | Denegar backend, invalidar sesion y bloquear publish de tema |
| RB04 | CSS/URL externa o XSS | Aplicar allowlist tipada y revertir fuente de overrides; test negativo |
| RB05 | CAS/theme duplicado | Revertir activacion propuesta, conservar auditoria e impedir replay |
| RB06 | Migracion DB fail o perdida | NO_GO y rollback ensayado en MySQL efimero; no tocar Aiven/PROD |
| RB07 | Falla contraste, teclado o rendimiento | Revertir componente puntual y volver a medir Playwright |
| RB08 | CI, ScopeLock, A5_FAIL o SHA no equivalente | No promocionar; reparar causal y revalidar PASS con nuevo SHA |

El rollback **realmente disponible para este changeset documental** es `git revert` normal del commit que introduzca este archivo y su seccion CHANGELOG, sin force-push ni reset; las estrategias RB02–RB07 son PROTOCOLOS_FUTUROS_NO_EJECUTADOS que requieren ambiente dev autorizado y pruebas. No se toca Aiven, servidores, QA, main, PROD, Cloudflare o datos de cliente.

## 5. Ocho pares de prueba positivos/negativos FUTUROS_NO_EJECUTADOS

| ID | Positiva futura | Negativa/adversarial futura |
| --- | --- | --- |
| T01 | DS01-14 vinculan token a patron versionado | Token ausente/ciclo de aliases bloquea render |
| T02 | Tenant A aplica tema propio y cambiar a B limpia A | Respuesta asincrona A no modifica estado B |
| T03 | JWT/grant/membresia activos permiten propuesta no-autorizante | Revocacion, usuario ajeno, header suplantado denegados |
| T04 | Override tipado con URI interna autorizada | CSS arbitrario, JS, URL externa y XSS rechazados |
| T05 | CAS/version e idempotencia aceptan replay identico | Dos writers/payload distinto provocan conflicto seguro |
| T06 | Migration/rollback en MySQL 8.4 efimero aislado | Datos B, SQL destructivo o test Integration omitido falla |
| T07 | 320px, foco, contraste, motion y componentes UI medidos | No fingir WCAG PASS por mocks/documentacion |
| T08 | Checks requeridos SHA causal y A5 distinta Task | CANCELLED, FAIL, self-review o PR a main impide cierre |

**Pruebas implementadas en este documento: 0**. No hay ejecuciones de xUnit, Vitest, Chromium/Playwright, MySQL Category=Integration, HTTP, restauraciones, WCAG ni proveedor. Un test propuesto no puede computarse como una prueba PASS en indicadores.

## 6. Calidad V15 A0-A5 y criterio exacto de cierre

- **A0:** el diff de esta microtarea se limita a este contrato y a UNA seccion autocontenida de CHANGELOG_AI.md; verificar las cinco fuentes, blobs, PLAN/COLA/DAG y alcance R0.
- **A1:** lectura cruzada de 16 gates G, ocho rollback RB y ocho pares T frente a DS01–14, INV01–18, API/DTO, DB/IDX/MIG y configuracion/eventos; A1 es documental, no una suite runtime.
- **A2:** JWT/RBAC y aislamiento tenant A/B, revocacion, CSS injection, idempotencia y concurrencia solo como obligaciones fail-closed; pruebas de seguridad ejecutable NO_APLICA a este diff R0.
- **A3:** EF/MySQL 8.4, Angular/Playwright, HTTP y proveedores NO_APLICA porque el diff no modifica runtime, datos, ni integraciones; ninguna prueba inventada.
- **A4:** exigir fast-forward/CAS exacto hacia dev; validacion de blob y diff; checks `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` completed/success sobre SHA exacto; PLAN/COLA/DAG/BITACORA readbacks y registro idempotente. Checks auxiliares in_progress no son PASS.
- **A5:** A5_PENDIENTE hasta revisor de OTRA de las cinco Tasks Javier autorizadas, comprobando requisitos, cambios y tests de manera independiente. LISTO_DEV no autoriza QA/main/PROD.

**Condicion de salida:** LISTO_DEV solo para el CONTRATO DOCUMENTAL si A0–A4 se acreditan y se registra SHA/blobs; M03 y etapas posteriores requieren sus propias reglas, riesgos R1/R2, pruebas reales, revisión A5 independiente y permisos expresos. Toda decisión de arquitectura, branding, UI, migración o tenant que carezca de evidencia sigue NO_GO para su propio scope, sin bloquear entregas independientes.
