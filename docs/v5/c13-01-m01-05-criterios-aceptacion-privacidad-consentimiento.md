# SOLQARYN V5 FINAL — C13-01.M01.05: criterios de aceptación de privacidad, consentimiento y derechos

**Fecha:** 2026-11-14. **Autor:** PRIMARY DEV 03 JAVIER, `TASK_ID=6adc2e4bd8a48191ac7d8641ed95a1a3`. **Repositorio:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT. **Naturaleza:** contrato documental de aceptación; **NO** hay políticas legales publicadas, consentimiento activo, cookies adicionales, scripts, endpoints, migraciones ni prueba de cumplimiento certificada por este documento.

**Autoridad:** [Standard V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). Las entregas precedentes del mismo lote son M01.01 (blob `79523b25247b3a2db4bc79ca723ba5adc698d7c3`), M01.02 (`635fc95dbe1a32c628c72fd860af0a0d8e2e2dab`), M01.03 (`3fa9e8bc3e6a72d17f9bb0912bc03fd5acb9b959`) y M01.04 (blob `82d7238a73dba3275921da8349b291ceb0d46a55`). M01.03 documentó B01–B12 (brechas de evidencia), M01.04 documentó R01–R14, S01–S10 y D01–D10 aún abiertas.

## 1. Contrato de prueba y datos de aislamiento

En fases M03–M05, crear fixtures **no productivos** con empresas A y B, dos usuarios por empresa, un actor sin membresía, rol revocado, política vigente v2 y antigua v1, categorías con decisión explícita/no decidida, y proveedor simulado de analítica no esencial. Los contratos deben distinguir entre operaciones necesarias, facultativas y sujetas a decisión jurídica validada **antes de establecer** si procede consentimiento. No introducir plazos ni exigencias de una ley concreta sin autoridad competente.

Cada test de implementación debe aportar SHA y run/job exactos, actor, tenant resuelto en servidor, versión, evento, respuesta HTTP/UX, auditoría mínima, DB antes/después, cero datos cruzados, ausencia de cookies/SDK facultativos cuando corresponde y rollback. El texto siguiente es **FUTURO_NO_EJECUTADO**: no simula ni sustituye pruebas de integración reales.

## 2. AC01–AC16: criterios funcionales positivos futuros

| ID | Estímulo autorizado | Resultado verificable exigido | Prueba causal posterior |
| --- | --- | --- | --- |
| AC01 | Visitante abre aviso público vigente | Texto e idioma/versionado íntegros, sin token ni datos de tenants | Angular/E2E + API/metadata |
| AC02 | Usuario A consulta estado propio con JWT y membresía vigentes | Exclusivamente sus categorías y versión; ningún atributo B | HTTP + MySQL efímero A/B |
| AC03 | Usuario expresa selección válida donde aplica | Se registra estado por sujeto/tenant/política/categoría/versión con evidencia mínima | API + concurrencia DB |
| AC04 | Usuario revoca selección revocable | El siguiente uso de esa finalidad queda bloqueado y registrado sin borrar auditoría legítima | API + proveedor simulado |
| AC05 | Nueva versión v2 aprobada reemplaza v1 | Interfaz muestra v2, estado anterior no se presume v2 | Test de versión/cache |
| AC06 | Navegación esencial tras rechazo no esencial | Funcionalidad esencial no queda bloqueada ni hay SDK opcional activo | Browser red/storage |
| AC07 | Administrador de tenant autorizado consulta configuración permitida | Ve sólo política del ámbito A, sin expandir RBAC por rol comercial | HTTP + grants efectivos |
| AC08 | Responsable autorizado publica texto validado | Mantiene historial/versiones, trazabilidad y retorno a versión anterior por proceso aprobado | Contrato API + pruebas |
| AC09 | Sujeto autenticado solicita consulta de datos propios | Existe comprobante mínimo y flujo de verificación, sin prometer plazo legal no corroborado | API/UX + auditoría |
| AC10 | Solicitud propia de corrección válida | Se registra trazabilidad sin modificar información ajena ni evidencia obligatoria | DB + prueba permisos |
| AC11 | Exportación autorizada de datos propios | Contenido, archivos y metadatos se limitan al sujeto y a su tenant | API + inspección bytes |
| AC12 | Aplicación conserva marca de revocación bajo reintento | Un replay idéntico no duplica efecto ni modifica tenant/versión ajenos | DB concurrente/idempotencia |
| AC13 | Auditor limitado con grant específico solicita prueba | Sólo correlación y campos mínimos; no JWT, hashes, secreto ni PII excesiva | HTTP + inspección log |
| AC14 | Usuario opera controles por teclado/lector | Foco visible, labels, estado en vivo, rechazo y revocación accesibles | Playwright/a11y |
| AC15 | Proveedor no esencial deja de responder | Estado no se vuelve consentimiento tácito; degradación segura | Stub de timeout/browser |
| AC16 | Actualización controlada de versión | Retrocompatibilidad de lectura, sin pérdida de decisiones válidas y con reversión de código | API/MySQL + revert efímero |

Estos resultados requieren decisiones D01–D10 tomadas expresamente y los gates DAG; la redacción no demuestra su existencia actual.

## 3. N01–N16: regresiones negativas obligatorias futuras

| ID | Estímulo hostil o fallo | Comportamiento requerido |
| --- | --- | --- |
| N01 | JWT A solicita estado/ID de B | 403/404 sin metadatos ni lecturas de B |
| N02 | Usuario revocado usa JWT aún válido | Denegación por membresía/rol vigentes, cero escritura |
| N03 | EmpresaId o slug manipulado en query/header/body | No reemplaza tenant server-side ni concede propiedad |
| N04 | Rol de sucursal invoca configuración global | Rechazo y auditoría mínima sin PII ajena |
| N05 | Consentimiento v1 se reusa para v2 | No se declara aceptación v2, requiere flujo apropiado |
| N06 | Script desconocido dispara antes de decisión aplicable | Bloqueo/log permitido, no carga facultativa |
| N07 | Fallo de red o UI de preferencias | No crea aceptación por defecto ni activa tracking |
| N08 | Revocación duplicada/carrera | Idempotencia, no restaura autorización ni escribe dos veces |
| N09 | Exportación con ID sujeto de B | Denegación; ningún byte, URL firmada o archivo B |
| N10 | Error de cache comparte decisión A/B | Clave incluye tenant+actor+versión; no filtración cruzada |
| N11 | Auditoría registra JWT/email completo/hash | Test falla, log/response deben sanearse |
| N12 | Supresión sin base, plazo ni hold validados | Sin DELETE físico ni pérdida de factura/auditoría |
| N13 | Petición anónima intenta datos personales | Rechazo sin enumeración ni identificación |
| N14 | Selector UI bloquea teclado o condiciona navegación | Test a11y falla; no asumir consentimiento |
| N15 | Cookie HttpOnly futura sin protección CSRF | Prueba negativa de cambio de estado; no despliegue hasta resolver |
| N16 | Check obligatorio CANCELLED/SKIPPED/PENDING | No registrar PASS, no pasar a LISTO/certificación prematuramente |

Todo N debe comprobar denegación y **ausencia de efecto** en DB/cache/proveedor/log, no sólo código HTTP. MySQL debe ser efímero; no consultar datos personales productivos.

## 4. NF01–NF10: requisitos técnicos no funcionales verificables

| ID | Requisito | Señal medible (sin inventar umbrales) |
| --- | --- | --- |
| NF01 | Aislamiento A/B en lectura/escritura | Assertions de filas/respuestas/caché por tenant |
| NF02 | Mínimo privilegio y revocación | Resultado tras cambio de membresía/grant |
| NF03 | Integridad y versionado | Hash/version y transición v1→v2 observados |
| NF04 | Trazabilidad mínima | Correlación opaca sin PII ni secretos |
| NF05 | Idempotencia y concurrencia | Un resultado lógico con dos requests simultáneas |
| NF06 | Privacidad por defecto | Ausencia de script opcional ante estado no permitido |
| NF07 | Resiliencia | Timeout de proveedor no crea consentimiento |
| NF08 | Accesibilidad | Teclado, foco, lector y mensajes comprensibles |
| NF09 | Reversibilidad | Revert de código preserva registros/decisiones válidas |
| NF10 | Cumplimiento normativo no inferido | Aprobación por jurisdicción y finalidad registrada antes de publicar |

No se inventan tiempos de retención, RPO/RTO, SLA, WCAG certificado ni porcentajes de cobertura. Los valores aceptables se acuerdan por decisión explícita y evidencia pertinente.

## 5. Matriz de trazabilidad y puertas de implementación

| Objetivo | Dependencias y pruebas mínimas posteriores |
| --- | --- |
| AC01/05/08 y N05 | B03/B04, D01/D03, versión y aprobación humana de textos |
| AC02/07/11, N01–N04/N09/N10 | B06/B08, D04/D07; JWT, RBAC y ownership server-side + HTTP/MySQL A/B |
| AC03/04/12, N06–N08 | B01/B02/B05, D02/D05; proveedor, storage, replay, no trackers facultativos |
| AC09/10, N12/N13 | B07/B08, D06/D07; identidad, auditoría, retención/hold y datos propios |
| AC13, N11 | B10, D09; redacción de JWT/PII y consultas restringidas |
| AC14, N14/N15 | B09/B11, D08; Playwright teclado/lector y threat-model de cookies/CSRF |
| AC15/16, N16 | B12, D10; fallo de proveedor, rollback, exact SHA y gates completos |

C13-01.M01.06 y M02 requieren predecesores y revisiones A5 previstos en DAG, sin liberación por un documento de aceptación. El inventario de auth en localStorage del blob `9c9b58c4b289ccc3269b8e99b614a2a9dbaae557` demuestra implementación de almacenamiento de token, **no** un sistema completo de cookies, consentimiento, conformidad jurídica o vulnerabilidad probada.

## 6. Auditoría de esta M01.05 y rollback

- **A0:** diff sólo de este archivo Markdown y sección autocontenida en CHANGELOG, encabezados ## con contenido y sin credenciales; correspondencia de AC/N/NF y referencias.
- **A1:** revisión manual documental de 16 positivos/16 negativos/10 NF con oráculo y fase futura explícita; **no son PASS de API o navegador**.
- **A2:** continuidad de límites tenant A/B, JWT/RBAC, propiedad, protección PII, consentimiento/retención y revocación fail-closed.
- **A3:** cotejo de cuatro documentos M01.01–04, once fuentes del inventario M01.03 y decisiones pendientes D01–D10; .NET, Angular, MySQL, Docker, Playwright y API **NO_APLICA por diff sólo Markdown**, sin afirmar ejecución.
- **A4:** publicación fast-forward con CAS exact-HEAD; checks Scope Lock y procedencia CHANGELOG V5 deben concluir success en el SHA publicado; readback Git, PLAN/COLA y BITACORA A:Z.
- **A5:** revisión posterior por Task Javier diferente de la autora. A5_PENDIENTE no autoriza QA/main/PROD ni cumplimiento legal.
- **Rollback:** commit git revert normal, preflight HEAD y registro compensatorio aditivo; nunca borrar BITACORA ni hacer force-push. No hay runtime, datos ni scripts que revertir en esta unidad.
