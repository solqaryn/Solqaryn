# SOLQARYN V5 — C10-04.M01.01: objetivo, no-alcance y no-regresión de caché, jobs, backups y exportaciones

**Plan:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA. **Autor:** PRIMARY DEV 05 JAVIER, TASK_ID 6adc2e55f47c81919f6374e561687076. **Unidad:** PLAN_MAESTRO fila 1550; C10-04.M01.01, M01 documental; dev, Scope Lock STRICT, 2026-11-14.

## Objetivo y alcance real

El bloque C10-04 «Caché, jobs, backups y exportaciones» requiere, en fases posteriores, aislamiento empresarial, idempotencia, reintentos acotados, límites de recursos y restauración verificable. Depende de C03-04 y C10-02 conforme al plan; esta preparación también revalida el DAG técnico dinámico, pero su habilitación sólo permite el **claim**, no libera jobs, migraciones, cambios productivos ni fases posteriores.

La unidad **sólo** fija objetivos, exclusiones, invariantes y oráculos de aceptación futuros para: caché pública/privada, ejecución asíncrona, backups y restauración, generación/descarga de exportaciones, auditoría y observabilidad. No configura timers, cron, webhooks, dispatcher, workers adicionales, cola durable, backups reales, SDK, bucket, tokens, proveedor cloud, migraciones MySQL, DNS ni despliegues. La topología en servicio continúa en Vercel/Render/Aiven/Cloudinary; Hostinger/R2 es objetivo futuro no autorizado. No se modifican .NET 10, Angular 22.2.1, EF Core 10/MySQL ni JWT/RBAC.

## Línea base inspeccionada (código no ejecutado)

| Fuente actual en dev | Blob SHA Git | Límite verificable |
| --- | --- | --- |
| backend/src/Infrastructure/Services/PublicStoreMemoryCache.cs | 1a102138b88bd54989eeade5acf7a5ee7bf63867 | `IMemoryCache` in-process, tenant+segmento+generación+hash de parámetros, locks por clave; no acredita caché distribuida |
| backend/src/Infrastructure/Services/DevCloudinaryLegacyNormalizationHostedService.cs | 863e035107b849fb97d9530ef85cf2af1d2311ae | `BackgroundService` DEV opt-in con gates explícitos de host/proveedor/MySQL/tenant; no es cola universal ni prueba de ejecución |
| docs/v5/c10-03-m01-01-alcance-inventario-real-y-almacenamiento-futuro.md | ca7ccfaf423ead017a127acde393d53cf6ab181b | Inventario histórico de archivos **NO_VERIFICADO**, nunca inferir cero |
| docs/v5/c10-01-m01-03-inventario-codigo-evidencia-storage-neutral.md | 1d807b2b9cba1f8b87a0e4e4e571f8e832ff0203 | Contratos de Cloudinary observados; storage neutral/R2 aún no certificado |

La presencia de clases o documentos no verifica la ejecución de pruebas .NET, MySQL, restauración o aislamiento HTTP de dos tenants en esta corrida. No se han consultado datos productivos, credenciales, buckets ni objetos.

## Invariantes NR01–NR16 de no-regresión

| ID | Condición exigible posteriormente |
| --- | --- |
| NR01 | Claves de caché de recursos incluyen tenant validado por backend y namespace de entorno; A/B jamás comparten datos. |
| NR02 | EmpresaId, host, slug, header, cookie y parámetros de navegador nunca conceden autoridad por sí mismos. |
| NR03 | Caché pública excluye JWT, tokens, datos de checkout, PII privada y URLs firmadas. |
| NR04 | TTL, caducidad e invalidación tras escrituras evitan información privada obsoleta. |
| NR05 | Falla de caché degrada sin eludir autenticación, RBAC ni pertenencia. |
| NR06 | Cada job futuro lleva ownership, capacidad server-side, correlación y clave de idempotencia, con payload mínimo. |
| NR07 | Retry es acotado y trazable; ninguna Task de ChatGPT llama a otra para crear bucle permanente. |
| NR08 | Doble consumidor concurrente no ejecuta dos veces una operación no idempotente; CAS distribuido cuando corresponda. |
| NR09 | Workers revalidan tenant, recurso y permisos antes de ejecutar, incluso si el claim previo era válido. |
| NR10 | Queued/in_progress/skipped/cancelled/failed nunca se interpreta como éxito de una operación requerida. |
| NR11 | Backups exigen cifrado, retención, custodia y segregación comprobada por tenant/ambiente. |
| NR12 | RPO/RTO, checksum y restauración se certifican sólo tras ensayo efímero auditable, no por una descripción. |
| NR13 | Restauración nunca pisa PROD, mezcla A/B ni destruye historia; requiere autorización, snapshot y rollback. |
| NR14 | Exportación valida RBAC, ownership, cuotas, paginación/stream, vencimiento y CSV formula injection. |
| NR15 | Trazas de jobs/export/backup registran estado, duración e idempotencia sin secretos, PII ni URL privada. |
| NR16 | Fase documental M01 no libera M01.06/M06.06, DB/IAM/JWT, CDN/R2, QA/main/PROD, pagos ni proveedores. |

## Oráculos de aceptación futura — NO_EJECUTADOS

**Positivos propuestos:** P01 caché A hit e invalidación correcta; P02 cache B independiente; P03 job duplicado sin duplicación de efecto; P04 timeout con retry seguro; P05 restauración en MySQL efímero y conciliación de FK/checksum; P06 export autorizado filtra/redacta PII; P07 URL temporal expira; P08 auditoría minimizada conserva correlación.

**Negativos propuestos:** N01 key A solicitada por B; N02 EmpresaId forjado; N03 datos privados tras TTL vencido; N04 caché caída burla RBAC; N05 doble ejecución factura o altera dos veces; N06 worker sin tenant; N07 retry infinito; N08 backup expone tenant B; N09 restauración hacia PROD sin permiso; N10 CSV con fórmula inyectada; N11 descarga expirada/replay; N12 log incluye JWT/secreto; N13 CI cancelled/skipped contada PASS; N14 dispatcher entre Tasks iniciado sin autorización.

Son **escenarios FUTUROS_NO_EJECUTADOS**, no pruebas aprobadas, hallazgos de vulnerabilidad ni bugs acreditados. Los trabajos ejecutables M03–M05 deberán contrastar .NET 10, EF/MySQL efímero, seguridad tenant A/B, fallas y restauración real; Angular/Playwright sólo si existe superficie UI causal.

## Impacto, auditoría y rollback

Esta entrega modifica exclusivamente este Markdown y `CHANGELOG_AI.md`. **Capas y consumidores futuros:** Application coordina casos; Infrastructure implementaría adaptadores; API aplica JWT/RBAC; frontend sólo presenta estado; caché, outbox y exportaciones deben respetar transacciones y trazabilidad. Ninguna API/DTO, esquema, dato, secreto, configuración o worker cambia aquí.

**A0:** inspección estática de contratos, código, límites, DAG y dependencias. **A1:** coherencia textual NR01–NR16 con P/N, sin declarar runtime PASS. **A2:** separación tenant A/B, controles server-side, mínimos PII/secretos y fail-closed. **A3:** trazabilidad a C03-04/C10-01/C10-02/C10-03, sin suplantar pruebas de integración. **A4:** diff y blobs exactos en dev, fast-forward sin force, Scope Lock y procedencia CHANGELOG en `completed/success` para SHA publicado; compilaciones ajenas al diff Markdown se clasifican NO_APLICA motivado. **A5:** revisión independiente por otra Task Javier, nunca por este autor. Ninguna promoción QA/main/PROD deriva de A5.

**Rollback:** nuevo commit `git revert` no destructivo, sujeto a HEAD y Scope Lock vigentes, para retirar únicamente documentación. Gate M02/implementación requiere evidencia/autoridad posterior; no ejecutar operación real mediante este texto.
