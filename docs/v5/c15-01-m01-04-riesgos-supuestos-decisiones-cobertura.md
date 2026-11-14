# SOLQARYN V5 FINAL — C15-01.M01.04 riesgos, supuestos y decisiones de auditoría de cobertura

**Fecha:** 2026-11-14. **Autor:** PRIMARY DEV 05 JAVIER (`6adc2e55f47c81919f6374e561687076`). **Repositorio/rama:** `solqaryn/Solqaryn@dev`. **PROJECT_SCOPE_LOCK:** STRICT. **Clase:** M01 documental de planificación de riesgos; NO auditoría de resultados ejecutada.

**Fuentes institucionales:** [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), `AGENTS.md`, Skill canónica, Scope Lock, y contratos [M01.01](./c15-01-m01-01-alcance-auditoria-cobertura-trazabilidad.md), [M01.02](./c15-01-m01-02-actores-interfaces-auditoria-cobertura.md) y [M01.03](./c15-01-m01-03-inventario-fuentes-evidencia-cobertura.md). Estas fuentes son antecedente del contrato, no certificación de su revisión A5.

## 1. Alcance y método

La futura auditoría C15-01 buscará la trazabilidad desde el documento rector V5 hacia los **2340** IDs, PLAN_MAESTRO/COLA/DAG_TECNICO, estado/owner, Git SHA, CI, BITACORA y A5 independiente. Aquí se enumeran peligros **POTENCIALES_NO_VERIFICADOS**, supuestos **PENDIENTES_DE_ACREDITAR** y decisiones **ABIERTAS**. No se realizó el censo exhaustivo del documento, prueba de hallazgo real o ejecución de tests runtime. El objetivo contractual de cero requisitos huérfanos y duplicados críticos sigue **NO_CERTIFICADO**.

El contrato M01.02 identifica 14 roles conceptuales, 12 entradas, 12 salidas y 10 fronteras; M01.03 fija 18 blobs de Git sobre baseline `83089fdbdc9aea9198c0ebec2cba197907354ba7` y 10 brechas NO_ACREDITADAS. Se respetan la separación de fuentes y la necesidad de revalidar `dev` y la elegibilidad dinámica por ID antes de cada acción. Los riesgos aquí no afirman vulnerabilidades detectadas, incidentes reales ni carencia de controles en producción.

## 2. Catorce riesgos POTENCIALES_NO_VERIFICADOS

| Riesgo | Escenario condicionado | Consecuencia que debe prevenirse | Verificación / mitigación futura |
| --- | --- | --- | --- |
| R01 | Requisito del STANDARD sin ID preciso en PLAN | Un requisito queda huérfano y se reporta cobertura ficticia | Requerimiento original + ID exacto + matriz bidireccional |
| R02 | Dos filas o microtareas representan el mismo requisito crítico sin regla | Duplicación de decisiones/implementación | Normalización y revisión de colisiones por dueño |
| R03 | Versión del Doc/Sheet distinta del snapshot de análisis | Hallazgo sobre fuentes no comparables | Guardar revisión, fecha UTC y SHA de cada fuente |
| R04 | Se usa COLA O=SI sin corroborar DAG F/G | Se reclama alcance no habilitado | Gate dual y readback por ID exacto |
| R05 | Predecesor, ciclo o dependencia terminal no satisfechos | Se desbloquea M01.06/M06 o runtime antes de tiempo | DAG real, A5 y bloqueo causal preservados |
| R06 | Dos Tasks reclaman el mismo ID o no leen el último owner | Doble escritura o atribución inválida | CAS de estado, lectura de owner y evento idempotente |
| R07 | BITACORA histórico se edita, duplica o omite readback | Auditoría no reproducible | Append-only A:Z, EVENT_ID/IDEMPOTENCY_KEY y revisión |
| R08 | SHA de fuente difiere del SHA donde se corrió CI | A4 y prueba incorrectamente atribuida | head_sha exacto, artefactos y diff |
| R09 | CI queued/cancelled/skipped/failed se convierte en PASS | Cierre prematuro de una entrega técnica | completed/success de checks pertinentes o NO_APLICA motivado |
| R10 | La autora realiza su propia revisión A5 o revisa Task retirada | Se compromete independencia | TASK_ID autor != revisor del allowlist Javier6 |
| R11 | Guard UI se interpreta como autorización backend tenant A/B | Acceso cruzado no detectado | Tests negativos HTTP/DB específicos de runtime |
| R12 | Código de EF/SQL se interpreta como migración probada | Corrupción o rollback ficticio | CI efímera MySQL, transacción y reversión dirigida |
| R13 | Se confunde LISTO_DEV con permiso QA/main/PROD | Cambio no autorizado de ambiente | Gate humano explícito por entorno |
| R14 | Se copian JWT, PII, secretos o datos de empresa en evidencia | Exposición de datos y credenciales | Seudonimización, redactado y referencia a logs seguros |

**Criterio de clasificación:** registrar el riesgo inicialmente como hipótesis. Sólo una observación reproducible en el entorno autorizado, con versión, SHA, caso, esperado/obtenido, controles y fecha, podrá convertirlo en **FALLO_VERIFICADO**. Si la prueba no existe, queda NO_ACREDITADO, nunca PASS ni FAIL arbitrario.

## 3. Diez supuestos PENDIENTES_DE_ACREDITAR

| ID | Supuesto a contrastar antes de auditoría integral |
| --- | --- |
| S01 | El STANDARD V5 y Sheet indicados siguen siendo autoridad; releer revisión en cada trabajo |
| S02 | El universo indicado es 2340 IDs; no asumir integridad sin recuento del snapshot |
| S03 | Una fila disponible puede cambiar mientras otra Task escribe; releer antes del CAS |
| S04 | Los seis IDs Javier son el allowlist máximo; peek no constituye permiso positivo por sí solo |
| S05 | Los datos de GitHub y Sheets se obtienen en lecturas separadas, sin transacción entre servicios |
| S06 | Cada blob SHA fija contenido del corte leído, no comportamiento futuro |
| S07 | Una prueba documental sólo certifica el contrato documental publicado |
| S08 | Estados A5_FAIL previos sólo cambian tras reparación y revisión causal independiente |
| S09 | Rollback de Markdown se hace con revert normal y sin editar BITACORA |
| S10 | Cualquier integración, migración o QA/PROD exige autorización y tests independientes |

Estos supuestos no autorizan nuevas dependencias, apertura de cuentas, cambios de programación ni inferencia de permisos. Las cantidades observadas en una corrida son temporales; `is_enabled` y el estado nativo requieren readback de Automations, sin que la Skill se convierta en prueba de ejecución física.

## 4. Diez decisiones ABIERTAS (no implementadas)

| ID | Decisión pendiente | Condición de cierre |
| --- | --- | --- |
| D01 | Seleccionar clave canónica requisito + microtarea + revisión de fuente | Pendiente aprobar matriz de correspondencia |
| D02 | Definir semántica de duplicado crítico vs duplicado documental benigno | Pendiente validar criterios y casos negativos |
| D03 | Fijar procedimiento reproducible para detectar predecesor ausente y ciclo | Pendiente auditoría DAG completa |
| D04 | Definir snapshot verificable Doc+Sheet+HEAD con fecha y hash | Pendiente exportación autorizada |
| D05 | Establecer clasificación NO_ACREDITADO / FALLO_VERIFICADO / PASS | Pendiente unificar reglas de cierre |
| D06 | Acordar evidencia mínima por fase documental versus M03 runtime | Pendiente matriz por componente |
| D07 | Precisar validación concurrente CAS y recuperación de fallo de append | Pendiente prueba con escenarios sintéticos |
| D08 | Definir muestreo de auditoría y criterio de cobertura completa | Pendiente denominador real y exhaustividad |
| D09 | Definir severidad P0/P1 basada en prueba causal, no inferencia | Pendiente clasificación por impacto y reproducibilidad |
| D10 | Definir formato de acta de hallazgos, mitigación, retest y A5 | Pendiente revisión independiente |

Una decisión de metodología no reemplaza el resultado exigido por el plan ni elimina un A5_FAIL. Los acuerdos futuros deben respetar los gates de ámbito, predecesores, datos, seguridad y fuente autorizada.

## 5. Diez pares de pruebas propuestas (TODOS FUTUROS_NO_EJECUTADOS)

| Caso | Positivo a demostrar | Negativo que debe bloquear |
| --- | --- | --- |
| P01/N01 | Requisito ↔ ID exacto con denominador completo | ID huérfano pasa por cubierto |
| P02/N02 | Claves PLAN/COLA/DAG sin colisión crítica | Duplicado se omite |
| P03/N03 | Prerrequisitos de DAG causalmente satisfechos | Un SI deriva de dependencia NO |
| P04/N04 | CLAIM CAS de un único owner con readback | Escritura simultánea de dos Tasks |
| P05/N05 | BITACORA A:Z idempotente conserva historial | Evento repetido/historia modificada |
| P06/N06 | Checks causales completed/success para head_sha | Skipped/cancelled/in_progress cuentan PASS |
| P07/N07 | Autor y A5 reviewer poseen TASK_ID Javier distintos | Autorrevisión o Task excluida |
| P08/N08 | JWT/RBAC en API niega recurso B a tenant A | Guard UI reemplaza denegación backend |
| P09/N09 | Hallazgo demostrado con retest y rollback | No acreditado se declara P0 real |
| P10/N10 | Dev LISTO conserva barrera QA/main/PROD | Documento autoriza despliegue |

Los casos son **FUTUROS_NO_EJECUTADOS**, no pruebas ejecutadas, y no se extrapolan a servicios de pago, base MySQL de producción, datos de clientes, Cloudflare/R2, workers, correo ni eventos. Un test de backend/Angular no se considera PASS por estar declarado en YAML; exige resultado del SHA exacto.

## 6. Impacto arquitectónico, calidad y rollback

Cambio **sólo** en este Markdown y entrada autocontenida de `CHANGELOG_AI.md`. No toca `Domain/Application/Infrastructure/API`, Angular 22.2.1, .NET 10, EF/MySQL, JWT/RBAC, tenant, migraciones, tokens, CI, datos, proveedores, seis Tasks físicas, ni QA/main/PROD. Consumidor documental futuro: C15-01.M01.05 (criterios) y auditoría de cobertura real separada; sin liberar M01.06/M06.

**A0:** matriz de 14 riesgos, 10 supuestos, 10 decisiones y 10 P/N, con revisión estática de encabezados y procedencia. **A1:** positivos/negativos diseñados pero NO_EJECUTADOS, sin funcionalidad modificada. **A2:** no se toca seguridad ni PII; pruebas HTTP/JWT/tenant runtime NO_APLICA al diff documental. **A3:** correlación de fuentes y contratos, sin builds MySQL/Angular/.NET causales. **A4:** commit directo fast-forward, checks Scope Lock y procedencia V5 del SHA exacto completados success, diff de dos Markdown y readback institucional. **A5:** PENDIENTE de otra Task Javier con TASK_ID diferente; ningún permiso QA/PROD.

**Rollback:** revert normal del commit posterior a revisión HEAD; eventos históricos permanecen append-only. No hacer force-push, reset, autoaprobación, compras, webhooks o workers.
