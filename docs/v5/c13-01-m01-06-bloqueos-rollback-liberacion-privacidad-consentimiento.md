# SOLQARYN V17 — C13-01.M01.06: bloqueo, rollback y liberación documental de privacidad/consentimiento

**Unidad:** C13-01.M01.06, PLAN_MAESTRO fila 1879, DAG_TECNICO fila 1840. **Autor:** PRIMARY DEV01 Javier, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Riesgo:** R0 estrictamente documental. **Rama única:** `solqaryn/Solqaryn@dev`, **PROJECT_SCOPE_LOCK=STRICT**. **Fuente institucional:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL y Sheet PLAN/COLA/DAG/BITACORA; calidad V15 y exclusión V17.

## A0. Resultado y decisión conservadora

Este contrato de **cierre de preparación M01**, no de cumplimiento jurídico, enumera los bloqueos antes de diseñar mecanismos posteriores de consentimiento, cookies, términos, privacidad, retención y derechos de personas en un SaaS multitenant. Su mera aprobación no crea políticas legales aplicables, banners, APIs, reglas de retención, ventanas de respuesta, consentimiento válido, derecho de borrado real ni certificación de tratamiento. Ninguna jurisdicción, plazo legal, finalidad o categoría de datos se asume sin fuente oficial, clasificación concreta y revisión competente.

Este commit sólo puede añadir este Markdown y una sección íntegra en `CHANGELOG_AI.md`. No cambia Angular, backend .NET, EF/MySQL, cookies activas, rastreadores, proveedores, DNS, cuentas, datos, configuración o interfaces, y no accede a producción. JWT, RBAC, autorización por empresa/sucursal, privacidad de tenant A/B y trazabilidad append-only existentes siguen siendo controles separados, no sustituidos por este texto.

## Evidencia antecedente de M01 y límite de inferencia

| Unidad | Archivo documental cotejado en dev | Blob Git |
| --- | --- | --- |
| M01.01 | `docs/v5/c13-01-m01-01-objetivo-no-alcance-privacidad-consentimiento.md` | `79523b25247b3a2db4bc79ca723ba5adc698d7c3` |
| M01.02 | `docs/v5/c13-01-m01-02-actores-interfaces-entradas-salidas-privacidad-consentimiento.md` | `635fc95dbe1a32c628c72fd860af0a0d8e2e2dab` |
| M01.03 | `docs/v5/c13-01-m01-03-inventario-codigo-evidencia-privacidad-consentimiento.md` | `3fa9e8bc3e6a72d17f9bb0912bc03fd5acb9b959` |
| M01.04 | `docs/v5/c13-01-m01-04-riesgos-supuestos-decisiones-privacidad-consentimiento.md` | `82d7238a73dba3275921da8349b291ceb0d46a55` |
| M01.05 | `docs/v5/c13-01-m01-05-criterios-aceptacion-privacidad-consentimiento.md` | `703d0aad6cdfc93a2a39153d9fc5cbd3924dc160` |

M01.01 fijó objetivo y no-regresión NR01–NR16; M01.02 identificó actores, entradas/salidas y fronteras; M01.03 documentó brechas B01–B12; M01.04 dejó riesgos R01–R14, supuestos S01–S10 y decisiones D01–D10 **abiertas**; M01.05 especificó AC01–AC16, N01–N16 y NF01–NF10 como aceptación **FUTURA_NO_EJECUTADA**. Los blobs evidencian texto versionado, no comportamientos de cookies, consentimientos ni cumplimiento en servicios vivos.

## G01–G16: barreras exigibles antes de cualquier implementación o liberación operativa

| Gate | Falta de evidencia o riesgo que mantiene NO_GO | Prueba/revisión exigible en futura unidad autorizada |
| --- | --- | --- |
| G01 | Jurisdicción, finalidad, responsable y base del tratamiento no determinadas | Inventario por finalidad y revisión jurídica competente, sin inferir obligación universal |
| G02 | Categorías de datos, sensibilidad y procesadores no verificados | Clasificación/flujo por empresa y ambiente con fuentes reales controladas |
| G03 | Inventario de cookies/scripts del navegador no medido | Inspección Chromium/Playwright con consentimientos negados/aceptados y tráfico observable |
| G04 | Publicación de versión vigente de términos/privacidad no acreditada | Texto aprobado, versionado, fecha efectiva y ruta accesible con rollback |
| G05 | Consentimiento granular por finalidad/canal no probado | Aceptación inequívoca, fecha/revisión, prueba verificable y negativa por defecto |
| G06 | Revocación/opt-out no medidos de extremo a extremo | Negativos que detienen uso y futuros efectos incluso ante cache o reintento |
| G07 | Aislamiento de tenant A/B y RBAC no probado en este cambio | Tests autenticados positivos/negativos y pertenencia backend, sin header confiable |
| G08 | Privacidad desde cliente no equivalente a guard servidor | Contratos API con rechazo IDOR/BOLA, sesión revocada y datos ajenos |
| G09 | Retención y supresión sin matriz de obligaciones/excepciones | Política aprobada de ciclo de vida, auditoría, backups y conservación obligatoria |
| G10 | Derechos de usuarios sin identidad verificada ni SLA respaldado | Solicitud autenticada, seguimiento, respuesta segura y límites según jurisdicción |
| G11 | Consentimiento de menores/categorías especiales no caracterizado | Clasificación, autoridad y control reforzado cuando aplique; si incierto NO_GO |
| G12 | Proveedores y transferencias externas sin base ni controles efectivos | DPA/contrato, región y permisos de cuenta realmente comprobados |
| G13 | Evidencia de consentimiento y auditoría con riesgo de PII | Almacenamiento mínimo, redacción, retención y controles de lectura por empresa |
| G14 | Borrado/restauración con integridad y reversión no ensayadas | MySQL 8.4 aislado, transacciones y backups sintéticos con hashes y conteos |
| G15 | Usabilidad/accesibilidad del consentimiento no probadas | Navegación teclado/foco/contraste/lectura, rechazo igual de accesible que aceptación |
| G16 | Faltan revisión independiente, CI y decisión de publicación | A0–A4 exact SHA, A5 distinta, decisiones legales abiertas y autorización de entorno |

**Estado real:** G01–G16 se consideran obligaciones **abiertas para runtime/legal/publicación**. M01.06 sólo las documenta y no las cierra mediante declaraciones. Ninguna fórmula DAG SI implica cumplimiento jurídico o permiso de despliegue. Si falta un control aplicable, el resultado de una futura operación debe ser DENY/NO_GO, no consentimiento implícito.

## R01–R08: runbook futuro de abortar y rollback — NO EJECUTADO

1. **R01 — Identificar fuente:** capturar documento aprobado, versión, propósito, propietarios, jurisdicción/alcance y SHA exacto antes de cambios; ausencia => detener.
2. **R02 — Minimizar:** inventariar cookies/scripts, endpoints, destinatarios, procesadores y datos estrictamente necesarios sin extraer PII a bitácoras abiertas.
3. **R03 — Respaldar:** guardar versión previa de textos/UX/reglas y snapshot **sintético** restaurable con propiedad tenant, sin exportar bases reales por este contrato.
4. **R04 — Validar en aislado:** probar consentimiento negado, posterior aceptación, revocación, expire/TTL, accesibilidad y rechazos A/B usando navegador/API/MySQL efímeros según cambios.
5. **R05 — Abort inmediato:** ante tracker antes de consentimiento requerido, filtración A/B, identidad revocada autorizada, pérdida de opt-out o discrepancia jurídica, bloquear publicación.
6. **R06 — Revertir UX/config:** restaurar versión previa aprobada y negar futuros efectos no autorizados; no reactivar tracking por fallback ni confiar en caché.
7. **R07 — Conservar evidencia mínima:** preservar cronología de decisiones y revocaciones, requisitos de retención y auditoría; no borrar consentimientos/solicitudes históricas para ocultar defectos.
8. **R08 — Certificar salida:** registrar pruebas ejecutadas, numerador/denominador, SHA, revisor distinto, readback, dependencias y estado; si no hay medición, NO_MEDIDO.

**Rollback de este documento**: nuevo commit aditivo `git revert` después de verificar HEAD, sin force-push, sin borrar BITACORA, datos reales o registros de decisiones. El runbook futuro requiere autorización y pruebas independientes; los ocho pasos no se han ejecutado aquí.

## T01–T12: pares POS/NEG de aceptación FUTUROS_NO_EJECUTADOS

| Caso | Positivo esperado en prueba futura | Negativo que debe detectar defecto |
| --- | --- | --- |
| T01 | Finalidad permitida solicita consentimiento claro | Tracker inicia antes del gate requerido |
| T02 | Usuario rechaza finalidades opcionales fácilmente | Rechazo oculto o banner inaccesible |
| T03 | Consentimiento A se guarda sólo en A | Tenant B lee prueba o sujetos de A |
| T04 | Revocación inmediata bloquea uso futuro | Snapshot/cookie antigua reactiva permiso |
| T05 | Propósito y canal de aceptación coinciden | Consentimiento de otro propósito se reutiliza |
| T06 | Término vigente y versión accesible se presenta | Versión caducada/idioma errado se publica |
| T07 | Solicitud de derechos autentica identidad | Suplantador ve datos o existencia de otra persona |
| T08 | Política de retención permite borrado elegible | Borrado elimina auditoría legalmente conservable |
| T09 | Proveedor autorizado minimiza transferencias | Destino no aprobado/URL arbitraria recibe PII |
| T10 | Cookies requeridas se distinguen de opcionales | Cookie de marketing se disfraza de necesaria |
| T11 | UI funciona con teclado, lector, foco y contraste | Aceptación accesible pero rechazo inaccesible |
| T12 | Rollback conserva decisiones y opt-outs | Revertir UI hace tracking por default o pierde bajas |

**Ejecución efectiva en esta microtarea:** 0/12; no se han lanzado pruebas Chromium, JWT HTTP, MySQL, EF, requests de cookies/proveedores ni inspección jurídica. Los escenarios son obligaciones de fases futuras; no marcar PASS runtime ni afirmar conformidad legal a partir de un documento.

## Calidad, dependencias y condición de cierre de M01

**A0:** sólo este documento y sección íntegra indivisible en CHANGELOG, requisitos de PLAN/COLA/DAG, procedencia M01.01–05 y bloqueo V17 con owner exclusivo. **A1:** inspección estática de 16 gates, ocho pasos de rollback y doce pares de oráculos futuros adversariales. **A2:** tenant A/B, JWT/RBAC, consentimiento granular, privacidad, opt-out, revocación y minimización se exigen a futuras implementaciones, **no son pruebas ejecutadas ahora**. **A3:** MySQL, API, Angular, browser, proveedores o regulador **NO_APLICA al diff R0**, por no modificar runtime ni políticas públicas; no ocultar las pruebas futuras. **A4:** publication fast-forward/CAS al HEAD de dev, Scope Lock y procedencia V5 `completed/success` en SHA exacto, PLAN/COLA/BITACORA readback; CI queued/in_progress/cancelled/skipped no PASS. **A5:** revisión independiente de otra Task Javier de distinta autoría, con capacidad de rechazo.

**Puerta M01→M02:** M01.01–06 deben conservar condiciones de dependencia verdaderas; M01.06 requiere LISTO_DEV A0–A4 más A5 independiente y nueva lectura DAG/COLA. Sólo puede liberar **preparación documental** que el DAG permita; M03/runtime, cookies reales, políticas legales públicas, QA/main/PROD y tratamientos de datos siguen NO_GO sin autorización específica, pruebas y análisis legal pertinentes.
