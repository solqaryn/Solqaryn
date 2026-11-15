# SOLQARYN V5 FINAL — C14-02.M01.06: bloqueos, rollback y condiciones de liberación de aceptación de seguridad

**Fecha de preparación:** 2026-11-14 (America/Tegucigalpa). **Autor:** PRIMARY DEV 05 JAVIER, TASK_ID `6adc2e55f47c81919f6374e561687076`. **Rama:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Alcance:** documento de salida del lote M01 y contrato de bloqueo de la futura aceptación de seguridad. **NO** se ejecutó pentest, prueba HTTP/EF, restore MySQL ni verificación productiva en esta microtarea.

## 1. Dependencias y naturaleza del cierre

La fuente ejecutable es el [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y la [hoja PLAN/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). El preflight institucional de este ID exigió PLAN_MAESTRO fila 2059 PENDIENTE libre, COLA G/O=SI, DAG_TECNICO fila 2020 F=VALIDADO/G=SI y las cinco preparatorias C14-02.M01.01–05 en LISTO con A5 independiente documentada. La salida de M01.06 define condiciones, **no** afirma que se haya satisfecho la seguridad de la plataforma universal.

Contratos precedentes: [M01.01 alcance](./c14-02-m01-01-alcance-no-regresion-aceptacion-seguridad.md), [M01.02 actores](./c14-02-m01-02-actores-interfaces-fronteras-seguridad.md), [M01.03 inventario de fuentes](./c14-02-m01-03-inventario-codigo-evidencia-aceptacion-seguridad.md), [M01.04 riesgos](./c14-02-m01-04-riesgos-supuestos-decisiones-aceptacion-seguridad.md) y [M01.05 criterios](./c14-02-m01-05-criterios-aceptacion-seguridad.md). Su A5 fue documental y no acredita resultados runtime ni ausencia de P0/P1. El estado A5 de **esta** M01.06 queda pendiente de otra Task Javier.

## 2. Barreras B01–B12 y evidencia mínima antes de liberar ejecución futura

| Gate | Requisito para habilitación posterior | Bloqueo explícito |
| --- | --- | --- |
| B01 | Plan, COLA y DAG por ID exacto con predecesores y propietario | Falta DAG, fórmula NO o owner duplicado |
| B02 | Skill canónica y Scope Lock del HEAD exacto completos/success | Skill distinta, HEAD cambiado o check no exitoso |
| B03 | Casos positivos y negativos reproducibles, SHA/job exacto | Sólo documento, prueba SKIPPED/CANCELLED/FAIL/in_progress |
| B04 | RBAC, membresía y JWT vigentes en API .NET autorizada | Autoridad derivada de Angular, body o cabecera manipulada |
| B05 | Aislamiento tenant A/B y sucursal/propiedad con datos sintéticos | BOLA/IDOR o fuga entre empresas sin mitigación aprobada |
| B06 | Consultas EF con filtro/ownership y FK del propio tenant | Filtro omitido o FK de B aceptada para A |
| B07 | Hosts y StoreKey con igualdad exacta y autorización | Subdominio parecido, host spoofing o claim externo |
| B08 | Archivos y exportaciones filtrados, caducidad de URL validada | URL de otro tenant, replay o export de PII cruzada |
| B09 | Jobs y caché sólo si existen/están autorizados, con idempotencia | Reintento duplicado, revocación ignorada o key cruzada |
| B10 | Backup/restore en sandbox con rollback y conteos validados | Restauración no probada o dependencia de Aiven live |
| B11 | Hallazgos P0/P1 efectivamente inventariados, mitigados y retesteados | Afirmación `P0/P1=0` sin evidencia o hallazgo crítico abierto |
| B12 | A5 diferente del autor y autorización específica de cada ambiente | Autoaprobación o inferencia de QA/main/PROD desde dev |

**Estado de B01–B12:** criterios de futura liberación, no resultado PASS. B01 y B02 pueden verificarse como preflight de esta entrega, pero B03–B11 **no están certificados por redactar este contrato**. Por tanto, no se declara cero P0/P1, penetración validada ni autorización de desplegar o migrar.

## 3. Matriz de rollback y respuesta causal

| Evento o defecto | Acción de contención autorizada | Evidencia de recuperación exigida |
| --- | --- | --- |
| Check obligatorio de Scope Lock fallido | NO_GO para escribir nuevos scopes; reparar causa real con dueño/autoría | SHA nuevo `completed/success`, diff y readback |
| CI causal de .NET/Angular/MySQL todavía activa | Conservar únicamente el scope afectado VALIDANDO, sin PASS | Resultado de job del SHA exacto, logs y prueba negativa |
| BOLA/IDOR/JWT/RBAC o fuga tenant A/B reproducida | No publicar; bloquear promoción y corregir en rama dev con ownership | Retest positivo/negativo en entorno aislado, revisión A5 independiente |
| Cambio de DB/migración destructiva propuesta | No ejecutar sobre datos reales; exigir copia, migración reversible y rollback | Prueba en servicio MySQL efímero, checksum e integridad |
| Configuración de host/archivos/caché insegura | Mantener deshabilitada la capacidad futura, sin tocar DNS/R2 live | Prueba de host exacto/URL firmada, vencimiento y aislamiento |
| Carrera de edición Git/Sheet | Abort del escritor perdedor; mantener historia y propietario original | CAS readback, sin force-push ni eventos duplicados |
| Error en documento recién publicado | `git revert` normal del changeset, sin borrar BITACORA | Diff de reversión y nuevos checks del SHA exacto |

Una corrección documental nunca justifica borrar un A5_FAIL histórico. Las seis Tasks Javier mantienen títulos, RRULE, IDs y permisos físicos sin cambios; los fallos de una Task no constituyen permiso para usurpar su sesión o revivir cuentas excluidas.

## 4. Contrato de liberación M01 y restricciones para M02–M06

**Liberación documental de M01.06 a LISTO_DEV:** sólo después de comprobar fuentes M01.01–05, precondiciones, diff acotado, A0–A4 documentales, Scope Lock y check V5 `completed/success` sobre su propio SHA, BITACORA/PLAN/COLA readback. Registrar `A5_PENDIENTE`; la revisión debe provenir de otro TASK_ID Javier. Esta salida no certifica el estándar de seguridad de aceptación completo.

**Entrada a M02:** sólo cuando su propio DAG tenga F=VALIDADO/G=SI y COLA G/O=SI, la transición M01.06 sea causal y A5 independiente esté acreditada. Incluso entonces, M02 contractual no da permiso para ejecutar DB, JWT/RBAC/tenant, proveedor o migración. Un gate `SI` documental no equivale al umbral de cero P0/P1 probado.

**QA/main/PROD y proveedores:** siguen vetados sin orden expresa diferenciada. No cambiar Hostinger, Cloudflare, DNS, Aiven, Cloudinary, tokens, secretos, cuentas pagadas o datos reales. No instalar dispatcher, webhook, worker ni nuevos ejecutores como parte de esta fase.

## 5. Auditorías documentales y rollback de esta entrega

**A0:** comprobar presencia de 12 barreras B01–B12, tabla de contingencias, vínculos previos, identidad, ausencia de secretos y encabezados CHANGELOG con cuerpo propio. **A1:** revisar oráculos positivos/negativos de M01.05 frente a B01–B12; no declarar ensayos funcionales ejecutados. **A2:** conservar separación tenant A/B, JWT, RBAC, membresías y control de acceso sin cambios runtime. **A3:** trazabilidad a Standard, M01.01–05, PLAN/COLA/DAG y auditoría A5 histórica sin inventar PASS de seguridad. **A4:** publicar únicamente documento y sección completa CHANGELOG por fast-forward/CAS; verificar SHA, blob, Scope Lock y V5, readback del cambio institucional. **A5_PENDIENTE:** revisión independiente posterior por otra de las cinco Tasks Javier.

**Rollback del changeset:** `git revert` regular luego de verificar conflictos/HEAD, sin force-push ni reescribir históricos. Una reversión no borra hallazgos, decisiones pendientes ni permisos ya denegados.
