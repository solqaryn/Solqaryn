# SOLQARYN V19 — C09-04.M03.04: outbox MySQL 8.4 por tenant

**Autor:** DEV03, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Riesgo:** R2 EF/MySQL, concurrencia/idempotencia. Repositorio exclusivo `solqaryn/Solqaryn@dev`, Scope Lock STRICT, PLAN fila 1421, candado V17 2000001421. Fuente: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES.

## A0 — Cambio causal

El repositorio de outbox existente realiza staging con EF Oracle en la misma unidad de trabajo del caso de negocio, tiene unicidad fisica `(EmpresaId, ClaveIdempotencia)` y actualizacion condicional de claim. Esta microtarea refuerza las lecturas de clave contra controles CR/LF y claves que exceden el maximo del dominio, evitando ambiguedad por normalizacion en accesos internos. No se crea cola externa ni envio.

Se incorpora una regresion marcada `Category=Integration` que realiza bootstrap fresco del esquema real Oracle EF sobre MySQL 8.4 efimero local; demuestra persistencia, aislamiento A/B, clave repetida solo entre tenants distintos, bloqueo de duplicado en el mismo tenant, claim unico y rechazo de claves con controles. La prueba falla si falta el esquema, si no existe el indice unico o si el aislamiento falla. Workflow de GitHub-hosted especifico con cancel-in-progress=false por SHA.

## A1-A3 — Criterios y limites

Positivos: guardar y releer A, guardar y releer B con igual clave, reclamar una sola vez por tenant. Negativos: B no lee A, segundo claim A vacio, mismo tenant con clave duplicada falla por indice MySQL, CR/LF rechazados antes de consulta. Frontera: limite 200 caracteres y validacion preconsulta; errores de base se exponen solo en test, sin datos reales.

MySQL fisico efimero es obligatorio para A3. No se certifican llamadas HTTP, JWT real, SMTP, WhatsApp, consentimiento, entregas o cancelaciones productivas. El servicio Application conserva validacion de membresia tenant server-side anterior. No se alteran migrations, API, DI, schema ni datos productivos.

## A4 y cierre

Exigir para SHA de entrega: workflow MySQL 8.4 exacto `completed/success` con `Category=Integration` ejecutada y 0 fail; backend Release, seguridad, autorizacion si causal, Scope Lock y V5 SUCCESS; readback PLAN/COLA/DAG/BITACORA. Si hay CI pendiente permanecer VALIDANDO con RUN/SHA. Revisor A5 de distinta Task Javier, no autor. Rollback por revert normal de un commit, nunca reset/force-push. Nada autoriza QA/main/PROD.
