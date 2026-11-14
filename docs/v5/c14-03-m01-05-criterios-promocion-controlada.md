# SOLQARYN V5 FINAL — C14-03.M01.05: criterios observables de promoción controlada

**Fecha:** 2026-11-14. **Autor:** PRIMARY DEV 02 JAVIER, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Repositorio autorizado:** `solqaryn/Solqaryn@dev` con `PROJECT_SCOPE_LOCK=STRICT`. **Clase:** criterios documentales de aceptación futura, no release ni certificación QA/main/PROD.

**Fuente única:** [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), [M01.01](./c14-03-m01-01-alcance-promocion-controlada.md), [M01.02](./c14-03-m01-02-actores-interfaces-promocion-controlada.md), [M01.03](./c14-03-m01-03-inventario-fuentes-evidencia-promocion-controlada.md), [M01.04](./c14-03-m01-04-riesgos-supuestos-decisiones-promocion-controlada.md). PLAN fila 2094 PENDIENTE sin owner, COLA G/O=SI y DAG F=VALIDADO/G=SI cotejados antes del claim CAS exclusivo con readback.

## 1. Criterio de aceptación del lote futuro

El objetivo de C14-03 es definir checklist verificable de promociones **separadas** DEV→QA→main/PROD, sujeto a permiso específico de ambiente, artifact digest/HEAD exacto, checks funcionales y de seguridad pertinentes, configuración y observabilidad, plan de rollback con ensayo no destructivo y lectura posterior. Las M01 preparatorias **no** autorizan ejecutar esas promociones, migraciones, compras, DNS o cloud. La exigencia de entorno se obtiene de la política vigente y de autorización humana explícita, no de un documento de diseño.

Los pares positivos/negativos siguientes son todos `FUTUROS_NO_EJECUTADOS`; no son resultados de pruebas HTTP, navegador, base de datos, IAM, contratos, despliegue o recuperación. Cada prueba futura necesita identificador de caso, actor y roles efectivos, tenant/recurso, datos sintéticos, ambiente autorizado, input, resultado esperado y observado, SHA y artifact digest, job/log/artifact, evidencia redactada, revisor, timestamp y rollback.

## 2. Matriz P01–P16 y N01–N16 de no-regresión observable futura

| Caso | Positivo FUTURO a observar | Negativo bloqueante |
| --- | --- | --- |
| P01/N01 | Fuente y destino comparten artifact digest/versiones | Artefacto mutado o HEAD distinto bloquea |
| P02/N02 | Check runs required completed/success del SHA exacto | Queued, skipped, failed o cancelled no cuentan PASS |
| P03/N03 | Scope Lock y procedencia V5 correctos | ScopeLock fallido bloquea cualquier claim/certificación |
| P04/N04 | A5 independiente autor distinto | A5 propio rechaza auditoría |
| P05/N05 | QA tiene permiso nuevo del propietario | LISTO_DEV no habilita QA |
| P06/N06 | PROD tiene autorización separada y paridad causal | A5 o QA aceptado no habilitan PROD |
| P07/N07 | JWT válido autentica sujeto vigente | JWT expirado, revocado o mal firmado se rechaza |
| P08/N08 | RBAC backend respeta rol para recurso propio | UI ocultando un botón no impide POST prohibido |
| P09/N09 | Tenant A accede solo a recursos A | Header/body/ID B no expone recurso de B |
| P10/N10 | MySQL efímero valida integridad/rollback | DDL destructivo o pérdida comercial bloquea |
| P11/N11 | API/DTO conserva consumidores y contratos | DTO incompatible causa bloqueo o versión explícita |
| P12/N12 | Angular/Playwright reales y accesibles | Pantalla rota, foco atrapado o spec no ejecutado no es PASS |
| P13/N13 | Health y alertas correlacionadas al deployment | Errores 5xx o métricas caídas sin rollback bloquean |
| P14/N14 | BITACORA, COLA y PLAN conservan evidencia | Doble owner o reescritura de historia bloquea |
| P15/N15 | Rollback ensayado preserva pedidos, pagos y datos | Reset/force-push o pérdida irreversible bloquean |
| P16/N16 | Secretos inyectados sin exposición ni uso real en pruebas | Token de producción en Git/CI impide promoción |

Las reglas de GitHub Actions requieren `head_sha` exacto y `status=completed`/`conclusion=success`. Un job `queued/in_progress/skipped/cancelled/failed` **no es PASS requerido**. Los workflows enumerados en M01.03 son fuentes, no certificaciones de ejecución. Una CI genérica no sustituye tests dirigidos a código modificado.

## 3. Requisitos no funcionales NF01–NF10 para futuros gates

| Ref | Dimensión | Evidencia que deberá obtenerse |
| --- | --- | --- |
| NF01 | Trazabilidad | Cada evidencia enlaza SHA, test, run/job y fecha |
| NF02 | Reproducibilidad | Fixtures sintéticos y CI determinista |
| NF03 | Aislamiento | Negativos tenant A/B por rol/recurso |
| NF04 | Resiliencia | Rollback ensayado, health check y criterio stop |
| NF05 | Privacidad | Logs y artefactos redactados sin PII/secretos |
| NF06 | Compatibilidad | Contrato API/Angular/MySQL versionado |
| NF07 | Usabilidad | Navegación responsive y teclado sobre viewports relevantes |
| NF08 | Gobernanza | A5 independiente y autorización de cada entorno |
| NF09 | Consistencia | Transacciones de inventario/finanzas preservadas |
| NF10 | Auditoría | Append-only con idempotencia y readback |

Estas expectativas no se midieron en esta tarea. En particular, nadie ha demostrado mediante este documento paridad física de entornos, ausencia de vulnerabilidades P0/P1, conformidad jurídica, SLO o respaldo/restore real. Los proveedores Cloudinary/Render/Vercel/Aiven actuales y cualquier topología futura requieren decisión propia; este archivo no activa Hostinger/R2 ni cambia infraestructura.

## 4. Matriz de evidencia por tecnología

| Ámbito futuro | Prueba causal requerida | Condición que mantiene NO_GO |
| --- | --- | --- |
| Angular 22.2.1 | Build/test y Playwright con captura/traza por SHA | Spec presente sin corrida o regresión de UI |
| ASP.NET Core net10.0 | Compilación, test y HTTP positivos/negativos por rol/recurso | BOLA/IDOR o auth faltante |
| JWT/RBAC/tenancy | Token activo/revocado, rol, empresa y sucursal A/B en API | Client guard se trata como permiso final |
| MySQL/EF Core | Servicio efímero y prueba de rollback/consistencia | Migración destructiva o test no ejecutado |
| Operación/infraestructura | Paridad de configuración no sensible, digest y health | Permiso ausente, secreto expuesto o rollback sin ensayo |
| Trazabilidad | PLAN/COLA/DAG/BITACORA y revisión independiente | Doble owner o A5 de autor, histórico editado |

## 5. Controles A0–A5 documentales

- **A0**: coherencia de 16 pares P/N y 10 requisitos NF, con fuentes M01.01–04.
- **A1**: criterios positivos y negativos concretos y falsables; estado FUTURO_NO_EJECUTADO.
- **A2**: salvaguardas JWT/RBAC, tenant A/B, datos comerciales y secretos sin alterar runtime.
- **A3**: cotejo documental de versiones y arquitectura; jobs .NET, Angular, Playwright y MySQL `NO_APLICA` al changeset de solo dos Markdown.
- **A4**: publicación dev fast-forward/CAS y diff documental, checks exact SHA Scope Lock/V5 `completed/success`, readback PLAN/COLA/BITACORA y rollback disponible.
- **A5**: revisión independiente futura por otra de las seis Tasks Javier, nunca propia. Tampoco un A5 aprobado autoriza QA/main/PROD.

**Reversión**: revert normal en dev preservando BITACORA y datos. Prohibidos force-push, reset destructivo, reescribir historia o operar infraestructura de pago/producción. No se crean eventos, dispatcher, Task ni permisos adicionales.
