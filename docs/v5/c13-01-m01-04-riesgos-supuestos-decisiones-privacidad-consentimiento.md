# SOLQARYN V5 FINAL — C13-01.M01.04: riesgos, supuestos y decisiones de privacidad y consentimiento

**Alcance:** registro documental de riesgos y decisiones abiertas para privacidad, cookies, términos, consentimiento, retención y derechos. **Autor:** PRIMARY DEV 03 JAVIER, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Fecha:** 2026-11-14. **Proyecto:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT. Esta preparación M01 **no implementa** consentimiento, políticas jurídicas, tracking, cookies, endpoints, MySQL, migraciones, webhooks ni operaciones en producción.

## 1. Autoridades y evidencia causal de lectura

El [Standard de Implementación de Automatizaciones V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), el [PLAN/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit) y las fuentes Git de la fase delimitan este trabajo:
- M01.01 objetivo/no-alcance: `79523b25247b3a2db4bc79ca723ba5adc698d7c3`; especifica NR01–NR16.
- M01.02 actores, entradas/salidas: `635fc95dbe1a32c628c72fd860af0a0d8e2e2dab`; incluye salidas S01–S08 y oráculos P01–P04/N01–N16.
- M01.03 inventario real: `3fa9e8bc3e6a72d17f9bb0912bc03fd5acb9b959`; documenta once blobs y brechas B01–B12. **Estos blobs son fuentes leídas, no pruebas ejecutadas**.
- Código de prueba `N19SerieAuditPrivacyTests.cs` (`5bd3a4b1b7a4bb90c45a27149c408386c7bf1617`) y `P1015BPrivilegedAuditPrivacyTests.cs` (`c3321332fa4102111f03247da6063bb99d3bec1c`) acredita existencia de pruebas focales de auditoría, no un módulo de preferencias ni cumplimiento legal; la segunda utiliza EF InMemory.
- `c03-02-m01-03-inventario-autorizacion-privacidad.md` (`51b5a01a4770977aadcc780145a17aaed8a3fb41`) y `c03-02-m01-05-criterios-aceptacion-tenant-privacidad.md` (`a5f551f1bd2cde2c42e6daf9e6e488f247e36602`) son contratos existentes de tenancy, JWT y RBAC, no un PASS de derechos o cookies.

El inventario M01.03 observa un token bearer en localStorage y un proxy que filtra cabeceras cookie; esto **no demuestra** una vulnerabilidad explotable ni ausencia de cookies reales. No se inspeccionaron sesiones live, redes de navegador, jurisdicciones aplicables ni bases de datos reales en esta microtarea.

## 2. Registro R01–R14: riesgo, control y evidencia exigida

| Riesgo | Posible fallo que debe impedirse | Control causal antes de implementación/certificación | Brecha |
| --- | --- | --- | --- |
| R01 | Un tenant A consulta consentimientos o peticiones de B por IDOR/BOLA | Resolver sujeto, membresía y empresa server-side, negar A→B con HTTP/DB reales | B06 |
| R02 | JWT válido o rol legado sustituye RBAC y propiedad del registro | Verificar grant efectivo, recurso, sucursal y revocación de rol por operación | B06 |
| R03 | Cookie/SDK no esencial se activa sin decisión exigible | Inventario de tráfico real, finalidades y bloqueo por defecto mientras se decide base aplicable | B01/B02/B03 |
| R04 | Cambio de versión de política mantiene aprobación anterior como vigente | Versionado inmutable, lectura exacta y test de invalidación de versión | B04 |
| R05 | Revocación válida no surte efecto en procesamiento posterior | Máquina de estados y prueba causal de siguiente uso de finalidad | B05 |
| R06 | Duplicado/replay entre tenants crea aceptación fantasma | Idempotencia con ámbito tenant+sujeto+categoría+versión y prueba de concurrencia | B04/B05 |
| R07 | Solicitud de derechos exporta PII ajena | Identidad verificada, ownership, export segmentado, no revelar existencia ajena | B06/B08 |
| R08 | Borrado por supuesto plazo afecta facturas, auditoría o backups | Matriz jurídica aprobada por clase de dato y legal hold; ningún DELETE por inferencia | B03/B07 |
| R09 | Logs/cache incluyen JWT, PII excesiva o mezclan A/B | Correlación opaca, saneamiento, caché tenant+actor+versión y pruebas negativas | B06/B10 |
| R10 | Texto legal o plazo se publica sin revisión de país/rol | Aprobación humana competente, idioma, versión y vigencia antes de publicar | B03 |
| R11 | UI inaccesible, consent coercitivo o error activa trackers | Test teclado, foco, lectores, contraste, rechazo funcional y fail-closed | B09 |
| R12 | Transición de token localStorage a cookie rompe CSRF/sesión | Evaluación de amenazas y pruebas auth/CSRF separadas antes de rediseño | B11 |
| R13 | Proveedor externo/timeout habilita tratamiento sin verificación | Allowlist, contractualización, integridad y estado denegado ante fallo | B02/B12 |
| R14 | Documento M01 o DAG SI se interpreta como permiso de migración/PROD | Gates por fase, Scope Lock, A0–A5, aprobación diferenciada de DDL/deploy | B12 |

**Priorización técnica:** R01/R02/R07/R09 (aislamiento/PII) y R03/R05/R08 (tratamiento y retención) requieren evidencia negativa antes de cualquier runtime. La clasificación es riesgo de diseño **no un incidente confirmado** ni una afirmación jurídica.

## 3. Supuestos S01–S10, explícitamente NO_VERIFICADOS

| Supuesto abierto | Forma de falsarlo o confirmarlo |
| --- | --- |
| S01: todas las cookies/almacenamientos reales están inventariados | Traza de navegador/red por origen y navegador en entornos autorizados; resultado no inferido de fixtures |
| S02: cada finalidad tiene fundamento aplicable identificado | Matriz de jurisdicción, responsable, categoría, finalidad y revisión competente |
| S03: existe autoridad formal para aprobar política por tenant o plataforma | Decisión y matriz RBAC efectivas, sin crear rol a partir del documento |
| S04: se puede identificar al sujeto con alcance A/B de forma inequívoca | API + prueba autenticación/membresía/ownership con dos empresas |
| S05: cada versión de términos puede archivarse íntegramente | Contrato de versionado/lectura y evidencia de integridad |
| S06: revocación puede afectar usos futuros sin borrar auditoría legítima | Test de propagación, idempotencia y retención |
| S07: retención/backups están mapeados exhaustivamente | Inventario de DB, archivos, logs, backups y plazos aprobados |
| S08: el flujo de derechos es accesible y admite verificación suficiente | Prueba con identidad, denegación y UX accesible por teclado |
| S09: SDK/proveedores permiten deshabilitación cuando proceda | Inspección real y contract tests con red/timeout |
| S10: un rollback no revierte decisiones válidas del sujeto ni pierde evidencia | Compatibilidad de versión de datos, restauración efímera y caso de replay |

Los S01–S10 no habilitan M02/M03, ni certifican obligaciones legales, cookies instaladas o políticas existentes.

## 4. Decisiones D01–D10: ABIERTAS, no adoptadas

| ID | Decisión exigida | Autoridad / evidencia de cierre |
| --- | --- | --- |
| D01 | Jurisdicción, responsable de datos y propósito por superficie | Propietario + revisión legal competente + inventario de tratamientos |
| D02 | Categorías técnicas/no esenciales, bases y proveedor | Privacidad + Seguridad + trazas navegador/SDK |
| D03 | Formato y firma/versionado de políticas/consentimientos | Arquitectura/Seguridad + pruebas de integridad y cambio de versión |
| D04 | Ownership tenant+sujeto y permisos de lectura, revocación y auditoría | API/Application + JWT/RBAC + pruebas A/B e IDOR |
| D05 | Efecto, latencia e idempotencia de revocación | Product/Backend + tests HTTP/DB, fallos y duplicados |
| D06 | Retención, legal hold, borrado seguro y backups | Responsable legal/DB + inventario y restore probado |
| D07 | Canal y prueba de identidad para derechos/expor­taciones | Seguridad + pruebas de acceso ajeno, minimización, respuesta |
| D08 | UX accesible, textos traducidos y negativa no coercitiva | Producto/UX + Playwright/teclado/lector en fase UI |
| D09 | Redacción de logs, correlación y nivel de soporte | Seguridad/Observabilidad + pruebas de fuga PII/JWT |
| D10 | Secuencia de implementación, rollback y proveedor autorizado | Propietario + gates DAG, A0–A5 y autorización de entorno específica |

Ninguna D queda cerrada por este documento. **No se selecciona** proveedor, ley, TTL, sistema de eventos o flujo de borrado. Hasta la decisión, el comportamiento nuevo permanece deshabilitado.

## 5. Ocho pares de oráculos (FUTUROS_NO_EJECUTADOS)

| Par | Positivo requerido en fase de implementación | Negativo exigido |
| --- | --- | --- |
| P01/N01 | Sujeto A lee sólo su versión/estado | JWT A intenta ID de B, denegación y sin filtración |
| P02/N02 | Revocación propia afecta siguiente uso de categoría | Replay previo intenta reactivar consentimiento revocado |
| P03/N03 | Rechazo de seguimiento opcional preserva navegación | Error del SDK activa scripts no esenciales |
| P04/N04 | Cambio aprobado de versión solicita nueva decisión | Consentimiento de versión vieja se considera vigente |
| P05/N05 | Auditor autorizado ve correlación mínima | Auditor sin grant obtiene PII o JWT en respuesta/log |
| P06/N06 | Solicitud de derechos propia produce estado verificable | Export o petición de actor A incluye datos B |
| P07/N07 | UI accesible permite revisar y rechazar opciones | Diálogo bloquea teclado o es coercitivo |
| P08/N08 | Retención autorizada respeta hold y auditoría | Eliminación destructiva por un plazo documental sin validación |

Los 16 escenarios describen contratos de aceptación, **no casos de .NET, Angular, Playwright, MySQL ni tráfico real ejecutados**. Los ensayos posteriores deben registrar SHA, solicitud/estado, filas antes/después, aislamiento, ausencia de efectos negativos y logs saneados.

## 6. Impacto, auditorías, rollback y condiciones de salida

- **A0 documental:** verificar dos archivos Markdown (este y CHANGELOG), sección nueva completa, encabezados no vacíos, sin secretos ni atribución de viñetas a otro autor; no cambios a controllers, UI, esquema ni CI.
- **A1 documental:** comprobar correspondencia de R01–R14, S01–S10, D01–D10 y P/N01–08 con B01–B12. Ningún P/N se certifica como prueba de runtime.
- **A2 documental:** revisión fail-closed tenant A/B, JWT/RBAC, PII, revocación, retención, cookies, versiones y proveedor; controles de backend permanecen autoridad.
- **A3 documental:** contrastar M01.01/.02/.03 y las fuentes Git citadas. Build .NET/Angular, MySQL, Docker y E2E son **NO_APLICA a este diff exclusivamente Markdown**, nunca se anotan como PASS de implementación.
- **A4 publicado:** commit serial fast-forward/CAS hacia dev; comprobar `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` en completed/success sobre SHA exacto; readback PLAN/COLA y evento BITACORA con 26 columnas.
- **A5 pendiente:** revisión independiente de otra Task Javier distinta del autor, sin autoaprobación, QA/main/PROD ni certificación jurídica.
- **Rollback:** revertir por nuevo commit Git (sin force-push) tras validar HEAD y registrar compensación en BITACORA sin alterar historia. Como este alcance no toca DB/API/UX, no hay migración real que revertir. M01.05 y etapas posteriores revalidan DAG, contratos y checks propios.
