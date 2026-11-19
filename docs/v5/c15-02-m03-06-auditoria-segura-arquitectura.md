# SOLQARYN V17 — C15-02.M03.06: auditoría acotada y errores seguros de revisión arquitectónica

Autor: DEV06 JAVIER_CHATGPT, TASK_ID=6af16bbad26881918c283abe06e1f32a. PROJECT_SCOPE_LOCK=STRICT, rama solqaryn/Solqaryn@dev. Riesgo R2 por tenancy, integraciones y secretos.

## Implementación real

Fachada Infrastructure .NET 10 de previsualización in-process V5ArchitectureMigrationSafeAuditService. Consume únicamente el port acotado de C15-02.M03.05 y requiere SHA/tenant observados, todas las precondiciones y una clave única por intento (máximo 128 por instancia lógica, una sesión, nunca singleton de usuarios). SemaphoreSlim sin espera rechaza auditorías concurrentes. Compara el scope completo de los probes antes de consultar el lector, deniega mutaciones, tenant B, SHA obsoleto, membresías revocadas, errores y doble selección. Si el lector o el audit sink falla, no reporta una respuesta exitosa. Los logs estructurados solo contienen código fijo y número de evidencias (no tenant, user, host, DNS, DSN, JWT, credenciales, claves de operación ni contenido de excepción). El sink ILogger no es durable.

La salida es siempre una demanda de evidencia externa, no certifica backup/restore, infraestructura, MySQL, DNS, Cloudflare, Render, Vercel, R2, migraciones ni seguridad productiva; ningún proveedor real se invoca. No se crea hosted worker, scheduler, webhook ni grants A5. El backend tendrá que verificar credenciales y proveedores por separado bajo autorización específica.

## R2 A0–A5

A0: servicio, port/sink, 13 xUnit, CHANGELOG, documentación, sin tocar endpoints, EF, Angular, DNS o producción. A1: positivos, errores, límites, concurrencia, repetición, cancelación y denegación completa. A2: pruebas tenant B, revocación, contexto falso, SHA y mutaciones. A3: .NET 10 Release/xUnit ejecutados en GitHub-hosted; integraciones reales MySQL/HTTP/proveedores son NO_APLICA para este diff aislado, no se declaran PASS. A4: checks Backend Release, seguridad, autorización, Scope Lock, V5 exactos sobre el commit, junto con readback institucional de PLAN/COLA/DAG/BITACORA. A5: solo otro Task Javier, revisión independiente de código y pruebas. Rollback git revert normal sin QA/main/PROD.

## Reparación correctiva V20 (DEV05, 18 de noviembre de 2026)

El revisor independiente DEV03 detectó A5_FAIL P2 sobre el SHA `ddb236cc3e140847dd608bde70bd9245ce1ba5a3`: el servicio aceptaba un resultado del lector con `Item.AttemptKey` distinto al `Probe.AttemptKey` pedido, siempre que el conteo coincidiera. El fallo de correlación constituye un defecto de integridad de evidencia, no una vulnerabilidad de despliegue demostrada.

La corrección captura los probes en un snapshot previo al primer await, valida claves seguras y únicas antes del lector, y exige correspondencia uno-a-uno del conjunto de claves del resultado (ordinal, sin duplicados, sin sustituciones); admite un orden de respuesta distinto si el conjunto coincide. Se amplía xUnit con respuestas forjadas, duplicados, entradas inseguras y permutación legítima. El recibo sigue siendo no certificante, no durable y sin PII; no cambia API, EF, MySQL, JWT, RBAC, rutas, despliegue ni proveedores. Verificación R2 requerida: pruebas .NET 10 reales sobre SHA de reparación, Backend Release, seguridad/autorización, Scope Lock/V5; ninguna prueba se presume aprobada hasta leer CI. El A5_FAIL histórico se conserva y exige una nueva A5 independiente por Task Javier distinta de DEV06 (autor) y DEV05 (reparador). Rollback: revert normal del commit en dev, sin force-push.
