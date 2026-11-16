# SOLQARYN V17 — C09-03.M01.06: bloqueo, rollback y liberación de Social Hub y campañas

**Microtarea:** `C09-03.M01.06`, PLAN_MAESTRO fila 1375. **Responsable/autor:** PRIMARY DEV 02 — JAVIER_CHATGPT, Task `6af16bb9492c8191b8e8c7d606f11507`. **Fecha:** 2026-11-15 (America/Tegucigalpa). **Repositorio:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Clasificación:** R0, exclusivamente documental; NO se implementan efectos externos.

**Autoridad ejecutable:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), V15/V16/V17, PLAN_MAESTRO/COLA/DAG_TECNICO, Skill canónica en HEAD dev. **Objetivo exacto:** definir bloqueo, reversión y condición de liberación del lote C09-03.M01 para campañas, contenido, segmentos, calendarios y métricas; evidencias futuras de consentimiento, límites y opt-out. Este cierre es documental y NO habilita por sí solo envíos, redes sociales, trabajadores programados, decisiones comerciales ni M02 sin A5 y DAG.

## 1. Prerrequisitos y evidencias de los cinco antecedentes

Al reclamar exclusivamente esta fila: `PLAN H1375=PENDIENTE`, `I1375` vacío; `COLA G1375/O1375=SI`; `DAG_TECNICO A1336=C09-03.M01.06, F1336=VALIDADO, G1336=SI`. Se obtuvo el candado V17 `SQV17_C09_03_M01_06` (`namedRangeId=2000001375`), luego se cambió únicamente H por `findReplace` con `occurrencesChanged=1` y se estableció I al Task ID propio, con readback. El evento CLAIM es aditivo en BITACORA. Estas condiciones no sustituyen ninguna puerta técnica futura.

| Antecedente | Fuente de dev y blob exacto | Evidencia documental real |
| --- | --- | --- |
| M01.01 | `docs/v5/c09-03-m01-01-alcance-social-hub-campanas.md` — `200a942044bac2b5c0a294ecacdf0ab75068a9ce` | Objetivo, no-alcance, dieciséis invariantes NR01–NR16 |
| M01.02 | `docs/v5/c09-03-m01-02-actores-interfaces-social-hub-campanas.md` — `4e2458b7dec1018de610b485c5aedf8fbf8f3f20` | Actores, interfaces y límites de confianza |
| M01.03 | `docs/v5/c09-03-m01-03-inventario-codigo-brechas-social-hub-campanas.md` — `04d2fd108d784449304a6c34ffc690bf1237832d` | Inventario acotado de catorce rutas, brechas de evidencia G01–G14, NO vulnerabilidades comprobadas |
| M01.04 | `docs/v5/c09-03-m01-04-riesgos-supuestos-decisiones-social-hub.md` — `cb112cc670b49365bcd5f4aec700fc124a007a52` | Riesgos R01–R14 hipotéticos; supuestos S01–S08 y decisiones D01–D08 NO RESUELTAS |
| M01.05 | `docs/v5/c09-03-m01-05-criterios-aceptacion-social-hub.md` — `cfbeb63d2ed1fbcf52a20c93c7c7aae73975ca99` | AC01–AC16, RNF01–RNF10 y pruebas futuras positivas/negativas, NO ejecutadas |

PLAN_MAESTRO registra A5_PASS independiente de la Task Javier DEV01 sobre los cinco antecedentes al corte del claim. La fórmula del DAG incorpora además `C06-03.M01.06+A5` y `C09-01.M01.06+A5`; se conserva la decisión derivada del Sheet y jamás se escribe G/O a mano. Las referencias a C01/C06/C08 son preparatorias, no permisos para ejecutar mensajería ni activar servicios.

## 2. Dieciséis puertas de bloqueo conservadoras

| Gate | Evidencia exigible antes de habilitar la actividad correspondiente | Resultado si falta evidencia |
| --- | --- | --- |
| G01 | Skill canónica coincidente, identidad Javier, dev y Scope Lock STRICT | NO_GO a escritura/certificación |
| G02 | PLAN/COLA/DAG dinámicos y predecesores LISTO con A5 independiente cuando sea exigido | Mantener PENDIENTE, sin liberar sucesoras |
| G03 | Candado V17 por fila, CAS H, owner I, evento idempotente y readback | Abort con lock/owner ambiguos; no doble escritor |
| G04 | Empresa de campaña, segmento, pieza, calendario y métrica comprobada en backend | Denegar acceso de tenant A a B y ownership NULL sin reconciliar |
| G05 | JWT válido, membresía activa, RBAC granular para redactar/aprobar/publicar/exportar | UI, slug o cabecera no elevan autoridad |
| G06 | Consentimiento vigente por destinatario, finalidad y canal | Contacto comercial no implica opt-in |
| G07 | Opt-out, revocación y supresión antes de programar y al ejecutar/reintentar | No publicar ni enviar luego de baja |
| G08 | Segmentación allowlist y filtros parametrizados, con límites y auditoría | Rechazar SQL/JS o consulta arbitraria |
| G09 | Contenido/URLs/medios saneados, XSS/SSRF y ownership de archivos | No renderizar/publicar payload no confiable |
| G10 | Idempotencia tenant+campaña+canal+destino, y control de carreras/versiones | Replays y duplicaciones NO producen dos efectos |
| G11 | Calendario UTC, versión, cancelación y validación justo antes del efecto | Horario viejo/revocado no se ejecuta |
| G12 | Credenciales/quotas/proveedor expresamente habilitados para el canal | Sin proveedor autorizado, cero emisión |
| G13 | Auditoría con actor/tenant, traza acotada y PII mínima | No registrar secretos, tokens ni contactos crudos |
| G14 | Métricas agregadas con privacidad, mínimos de grupo, RBAC y filtro de empresa | No reidentificar ni exportar datos de B |
| G15 | Pruebas causales .NET/Angular/MySQL/Playwright/proveedor según el diff, CI completed/success exact-SHA, Scope Lock y V5 | FAIL/CANCELLED/SKIPPED/IN_PROGRESS nunca son PASS exigible |
| G16 | A5 independiente por otra de las cinco Tasks Javier, revisión de diff/código/pruebas y ausencia de P0/P1 | NO_GO a sucesoras que exijan A5 y a QA/main/PROD |

G04–G15 son obligaciones para **futuras implementaciones**, no pruebas runtime aprobadas en esta entrega. Cualquier cambio real en consentimiento, mensajes, finanzas, JWT/RBAC, MySQL/EF, integraciones, terceros o producción requiere nuevo scope, evaluación R2/R3, autorización apropiada y pruebas auténticas. No hay certificación de cumplimiento normativo, entrega de campañas ni privacidad de datos reales.

## 3. Doce pares positivos/negativos FUTUROS_NO_EJECUTADOS

| Par | Positivo esperable | Negativo que debe fallar de forma segura |
| --- | --- | --- |
| P01/N01 | Editor A crea borrador para empresa A sin publicarlo | Usuario B o cliente sin empresa verificada no ve borrador de A |
| P02/N02 | Aprobador de A con rol vigente autoriza pieza de A | Lector, rol revocado o token inválido no publica |
| P03/N03 | Consentimiento A vigente para finalidad/canal elegidos | Contacto sin opt-in por canal o con baja rechaza envío |
| P04/N04 | Programación válida UTC se ejecuta bajo autorización vigente | Revocación entre programación y ejecución cancela |
| P05/N05 | Segmento permitido y parametrizado devuelve solo A | SQL/JS dinámico, filtro B o IDOR bloqueado |
| P06/N06 | Reintento de idempotency key no duplica efecto | Dos requests simultáneos/replay no emiten doble campaña |
| P07/N07 | HTML/enlace y medio propios saneados se aceptan | XSS, SSRF, URL privada o media de B rechazados |
| P08/N08 | Proveedor autorizado responde ACK y audita versión | Sin credencial, 429, timeout o cuota agotada no provocan bucle |
| P09/N09 | Métricas agregadas A de grupo suficiente | Grupos de baja cardinalidad, PII o cross-tenant denegados |
| P10/N10 | Cancelación conserva historial de decisiones | Cancelación concurrente no permite envío ni borra auditoría |
| P11/N11 | UI accesible presenta carga, error y revocación | Guard salteado no permite acción HTTP backend |
| P12/N12 | Desactivar función detiene trabajos nuevos y preserva datos | Rollback no modifica ventas, créditos o consentimiento real |

**Métrica de ejecución R0:** `0/12` pares de runtime ejecutados. Son especificaciones para pruebas futuras y NO son PASS de xUnit, API, EF/MySQL, Chromium, proveedor o privacidad. La evidencia de esta unidad es lectura, estructura contractual, seguridad conceptual y CI de procedencia del Markdown publicado.

## 4. Bloqueos de decisiones abiertas, liberación por niveles

Las decisiones D01–D08 de M01.04 permanecen **PENDIENTES**: consentimiento/finalidad/canal/opt-out; RBAC empresarial; fuente de segmentos/ownership NULL; seguridad del contenido y medios; programación/idempotencia/cancelación; canales, proveedores y costos; métricas, retención y auditoría; estrategia de pruebas, reversión y aceptación. No se inventa aprobación ni se contrata un proveedor. Hasta verificar la decisión aplicable, los efectos comerciales relacionados quedan deshabilitados.

**M01.06 R0:** se permiten únicamente este Markdown y sección autocontenida de CHANGELOG, commit FF en dev, Scope Lock y procedencia V5 completed/success exact-SHA, lectura BITACORA/PLAN/COLA/DAG y liberación segura de candado. La marca LISTO_DEV sólo describe documentación comprobada, **A5_PENDIENTE** hasta revisor distinto.

**M02:** se podrá reclamar una sola unidad de contratos/modelo cuando el DAG de esa ID confirme `F=VALIDADO/G=SI` y COLA `G=SI/O=SI`, H=PENDIENTE/I vacío y candado exclusivo, incluyendo la A5 de este M01.06 cuando el DAG la requiera. No se altera G/O ni se asume aprobación por pertenecer al mismo lote.

**M03–M06:** implementación código real .NET 10/Angular 22.2.1, validación API/RBAC, tenant A/B, consentimientos y opt-out adversariales, EF/MySQL 8.4 aislado si hay persistencia, Playwright/Chromium si hay UI, cuotas/retries/proveedor sólo con permisos efectivos. Requerir A0–A4, A5 distinto cuando aplique, checks exactos o equivalencia auditada de blobs y posterior readback. Nada en M01 habilita QA/main/PROD.

## 5. Rollback y recuperación quirúrgica en ocho pasos

1. Registrar ID, Task owner, SHA origen y probado, HEAD, rutas y versión de condiciones; impedir escritores simultáneos.
2. Si existe una CI obligatoria pendiente, conservar VALIDANDO y owner/run/SHA sin atribuir SUCCESS.
3. Detectar si el defecto es documental, implementación, autorización, consentimiento, proveedor, DAG, carrera o CI antes de intervenir.
4. Si hay fallo R0, corregir con commit aditivo o `git revert` normal del changeset, tras HEAD CAS; no force-push, reset ni eliminación BITACORA.
5. Si falla una función futura, detener sólo nuevas emisiones/publicaciones; no eliminar consentimiento, ventas, auditoría ni datos productivos.
6. Para cambios futuros en DB/efectos, probar rollback compensatorio y transacción en MySQL 8.4 efímero, con controles de doble efecto/carreras; no experimentar en datos reales.
7. Repetir pruebas negativas de tenant A/B, rol, revocación, opt-out, replay, XSS, cuotas y errores aplicables, más Scope Lock/V5 y CI exitosa exact-SHA.
8. Actualizar PLAN/BITACORA con evento único, H/I y readback; reabrir sólo el alcance causal si A5 falla y no liberar sucesoras sin A5 independiente efectiva.

## 6. Auditoría y frontera del cambio

- **A0:** alcance de dos Markdown, fuentes, cinco antecedentes y discrepancias separadas; no cambios de capas ni dependencias.
- **A1:** revisión de los dieciséis gates, doce pares P/N, ocho pasos rollback y criterios de no-regresión; aserciones runtime NO_APLICA en R0.
- **A2:** condiciones de seguridad tenant A/B, JWT/RBAC backend, consentimiento, opt-out, replays, privacidad y secretos como contratos futuros; no se afirman pruebas vivas.
- **A3:** no cambia Domain/Application/Infrastructure/API, Angular, EF/MySQL, servicios, jobs ni proveedores; HTTP, MySQL, browser, correo y redes NO_APLICA motivado.
- **A4:** SHA exacto, diff limitado, CHANGELOG sin encabezados vacíos/intercalados, Scope Lock/V5 completed/success, BITACORA idempotente y readback de PLAN/COLA/DAG.
- **A5:** revisión posterior independiente de otra Task Javier (nunca la autora), con capacidad de rechazar. No QA/main/PROD.
- **Impacto/rollback:** sin migraciones, API/DTO, datos reales, roles, secretos, políticas, UI, contratos de proveedores ni infraestructura; `git revert` normal y nuevas evidencias si el documento fuese incorrecto.
