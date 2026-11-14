# SOLQARYN V5 FINAL — C14-03.M01.03: inventario de código y brechas de promoción controlada

**Fecha:** 2026-11-14. **Autor:** PRIMARY DEV 02 JAVIER, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Rama:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Tipo:** inventario documental de fuentes Git, **no** certificación de release, ejecución de QA/PROD ni aprobación de migración.

**Autoridades:** [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit); [Sheet PLAN/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit); [M01.01 objetivo y no-regresión](./c14-03-m01-01-alcance-promocion-controlada.md) y [M01.02 actores/interfaces](./c14-03-m01-02-actores-interfaces-promocion-controlada.md). Preflight: PLAN fila 2092 H=PENDIENTE/I vacío, COLA G/O=SI, DAG F=VALIDADO/G=SI, transición condicional a EN_PROGRESO y lectura posterior del único owner.

## 1. Método reproducible y límites

Se inspeccionaron los doce archivos listados en la tabla desde el repositorio original, tomando como referencia de lectura de fuentes el commit `91aac01544fe1a0edd38e1ab0fe7d5bd1ad288f4`, y se cotejó cada hash blob exacto. Los archivos fueron leídos bajo `dev`; un blob prueba **identidad de contenido**, no que el workflow se ejecutó, ni que haya sistema QA funcional, aprobación de despliegue, paridad, backup, test de seguridad o rollback. No se consultan proveedores productivos ni secretos.

El inventario restringe la aceptación futura a evidencia causal sobre un SHA y un ambiente explícitamente autorizado. El programa de promoción C14-03 distingue DEV A0–A4 y A5 independiente de permisos nuevos para QA, main y PROD; ninguna etapa de preparación M01 abre estas puertas. Los cambios de esta unidad se limitan a **este Markdown y una sección autocontenida de CHANGELOG_AI.md**.

## 2. Inventario de fuentes versionadas F01–F12

| ID | Ruta y blob Git verificado | Evidencia observable en fuente | Qué NO demuestra |
| --- | --- | --- | --- |
| F01 | `.github/workflows/dev-ci.yml` — `3f6912458d17a2a049d7b6a804472a5e1cd1b746` | Workflow DEV con rutas backend/frontend | No implica ejecución en el SHA ni despliegue |
| F02 | `.github/workflows/ci.yml` — `c02c3f00c3dccb91e79bfd2b5befe2fb5b517263` | CI con pruebas y MySQL temporal descrito | No demuestra resultados ni datos reales de QA |
| F03 | `.github/workflows/environment-infra-parity.yml` — `de4c17e5e3ec81c442fd2bcd4d9a670f4433682d` | Definición de comparación de infraestructura | El archivo no acredita paridad ejecutada |
| F04 | `.github/workflows/dev-final-functional-certification.yml` — `a14cfb18f550810c94316083f548467e710f6ad3` | Definición de certificación funcional DEV | No equivale a certificación de QA/PROD |
| F05 | `.github/workflows/catalogos-aceptacion.yml` — `57ad4ef590f0329126dd82f544de011e1e873901` | Definición de aceptación funcional | No demuestra Playwright ejecutado ni test PASS |
| F06 | `scripts/verify-project-scope.mjs` — `5d51fe92566cfd0e51b7265b844af5432bac57c3` | Script de alcance repositorio/identidad | No verifica permiso para promoción |
| F07 | `scripts/quality/verify-v5-changelog-provenance.mjs` — `00e0b1414695b433f9c48cf7ad1f099a0e4b010b` | Control de procedencia CHANGELOG | No es aceptación funcional o seguridad |
| F08 | `frontend/package.json` — `e6d030f643421cb343cc12617e742469e88223c4` | Scripts y dependencias Angular | No acredita frontend desplegado ni saludable |
| F09 | `backend/src/API/Solqaryn.API.csproj` — `2a7b33a0974d1ebe13676593d84b458b1243e037` | TargetFramework net10.0 de API | No acredita binario QA ni HTTP exitoso |
| F10 | `backend/src/Infrastructure/Solqaryn.Infrastructure.csproj` — `6210d8b12abd77171d95d17e2292c1a319b89263` | Dependencias infraestructura y MySQL | No acredita migración o restauración |
| F11 | `docs/PROJECT_SCOPE_LOCK.md` — `7dabef6946d75e0381f121a70458240ac702a30f` | Lock estricto dev y seis Tasks Javier | No constituye permiso de QA/main/PROD |
| F12 | `AGENTS.md` — `063d342891ec6e0b23b9adb195ef3b358b51548d` | Gobernanza activa y límites de ejecución | No es autorización física de promoción |

Los workflows con trigger condicionado a rutas o `workflow_dispatch` no prueban que la suite corriera para todos los commits; no se atribuye `completed/success` sin consulta de check-run y `head_sha` exactos. La existencia de `.github/workflows/environment-infra-parity.yml` **no** significa que se haya demostrado paridad ni que la Task pueda escribir en QA/main.

## 3. Brechas de evidencia B01–B12

| ID | Ámbito | Evidencia que requerirá una promoción FUTURA | Estado de esta microtarea |
| --- | --- | --- | --- |
| B01 | Permiso QA | Autorización expresa vigente para QA y scope limitado | NO_ACREDITADO |
| B02 | Permiso producción | Autorización nueva para main/PROD con responsable y alcance | NO_ACREDITADO |
| B03 | Checks CI exactos | Logs y resultados de cada suite pertinente sobre SHA de release futuro | NO_ACREDITADO |
| B04 | Paridad ejecutada | Comparación real de configuración no sensible entre entornos | NO_ACREDITADO |
| B05 | Digest de artefactos | SHA/digest inmutable de build y release candidato | NO_ACREDITADO |
| B06 | JWT/RBAC/tenant | Pruebas positivas y negativas de API con tenant A/B en destino | NO_ACREDITADO |
| B07 | Contratos frontend | Smoke Angular/Playwright en entorno y viewport acordados | NO_ACREDITADO |
| B08 | MySQL efímero | Prueba migración, integridad, backups y reversión dirigida | NO_ACREDITADO |
| B09 | Rollback ensayado | Restauración de artefacto/versión sin pérdida de datos de negocio | NO_ACREDITADO |
| B10 | Observabilidad | Health, readiness, alertas y latencia antes/después | NO_ACREDITADO |
| B11 | Secretos | Demostración de inyección segura y ausencia de exposición en logs | NO_ACREDITADO |
| B12 | Trazabilidad final | Evidencia A5 independiente, permisos por entorno y readbacks | NO_ACREDITADO |

`NO_ACREDITADO` significa ausencia de prueba **en este inventario**, no vulnerabilidad confirmada ni afirmación de indisponibilidad física. Cualquier dato real del ambiente debe leerse únicamente en una ejecución futura con autorización separada.

## 4. Ocho pares positivos/negativos propuestos (NO_EJECUTADOS)

| Caso | Oráculo positivo FUTURO | Oráculo negativo bloqueante |
| --- | --- | --- |
| P01/N01 | Listar fuentes/versiones por blob en SHA exacto | Una ruta o blob faltante invalida inventario |
| P02/N02 | Comprobar Scope Lock exact SHA concluido en success | Check skipped/queued/failed no se declara PASS |
| P03/N03 | Comprobar que DEV y QA tienen aprobación separada | A5 documental no se usa como permiso QA |
| P04/N04 | Comprobar digest de build y artefacto de release | SHA diferente bloquea promoción |
| P05/N05 | Probar tenant A/B y RBAC server-side en destino aprobado | Guard Angular no sustituye rechazo backend |
| P06/N06 | Verificar MySQL con datos sintéticos y rollback | DDL o transacción sin respaldo bloquea |
| P07/N07 | Playwright y pruebas HTTP reales con logs del SHA | Source .spec sin ejecución no es PASS |
| P08/N08 | Readback de PLAN/COLA/BITACORA y aprobación del owner | Evento inventado o doble escritor bloquean |

Todos los casos anteriores son `FUTUROS_NO_EJECUTADOS`, incluidos Playwright, HTTP, MySQL, rollback y paridad. No se promueve un PASS por la existencia de archivos o tests de fuente.

## 5. Trazabilidad, riesgos, seguridad y reversión

La matriz está alineada con NR01–NR16 de M01.01 y actores A01–A12, superficies I01–I12 y límites de M01.02. La aceptación futura debe preservar JWT y RBAC **del servidor**, identidad tenant A/B, sucursal, permisos de objeto, consistencia de inventario/finanzas, autorización diferenciada por entorno, inyección de secretos fuera de Git y auditoría inmutable. Un SHA `completed/success` en Scope Lock demuestra solo ese control, nunca pruebas E2E o autorización de producción.

**A0**: verificar doce rutas+blobs y doce brechas con estados explícitos. **A1**: comprobar coherencia de los ocho pares P/N documentales, sin afirmar ejecución funcional. **A2**: mantener controles tenant/JWT/RBAC/secretos como exigencias futuras, sin modificar código ni datos. **A3**: cotejar autoridad, documentación precedente y fuentes Git; suites de runtime `NO_APLICA` al cambio solo Markdown. **A4**: publicar exactamente dos Markdown por fast-forward/CAS en `dev`, comprobar Scope Lock y procedencia V5 `completed/success` sobre SHA publicado, diff y readback institucional. **A5**: revisión independiente de otra Task Javier; esta Task autora no puede aprobarse.

**Rollback de documentación:** revert normal del commit en dev con registro aditivo, nunca reset/force-push, modificación histórica de BITACORA, migración, despliegue ni intervención en QA/main/PROD. No se habilitan eventos, dispatcher, credenciales o proveedores nuevos.
