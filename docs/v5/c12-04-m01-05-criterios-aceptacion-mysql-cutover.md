# SOLQARYN V5 — C12-04.M01.05: criterios de aceptación MySQL privado, restore y cutover

**Fecha:** 2026-11-14 · **ID:** C12-04.M01.05 · **PLAN_MAESTRO:** fila 1842 · **Autor:** PRIMARY DEV 04 JAVIER / Task `6aeacb7820ac8191b7d768a6f33dfa09` · **Rama:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT.

**Entrega:** contrato documental M01 de aceptación y evidencia futura para export, restore de ensayo, comparación, ventana y vuelta atrás; NO es implementación de migración ni certificación de plataforma. Fuente: [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN/COLA/DAG_TECNICO](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). La microtarea fue seleccionada por PLAN H=PENDIENTE I vacía, COLA G/O=SI, DAG F=VALIDADO G=SI y CAS exclusivo con readback; esta elegibilidad **no** habilita cutover.

## Contratos previos y frontera

- C12-04.M01.01: alcance/no-alcance, invariantes NR01–NR16 y necesidad de autorización nueva para infraestructura/DB.
- C12-04.M01.02: A01–A14 actores, I/O01–I/O12 entradas y salidas, F01–F10 fronteras; todo futuro, sin MySQL live.
- C12-04.M01.03: inventario de 16 blobs Git verificables (HEAD 213d08d6) y G01–G14 brechas; la existencia de código EF no prueba un restore real.
- C12-04.M01.04: R01–R14 riesgos potenciales, S01–S10 NO_VERIFICADOS y D01–D10 decisiones abiertas.
- Código vigente: Angular 22.2.1, ASP.NET Core net10.0, EF Core 10.0.12, Oracle MySql.EntityFrameworkCore 10.0.9, MySQL, JWT/RBAC, tenencia A/B, secuencias y transacciones comerciales.
- Infraestructura vigente documentada Vercel/Render/Aiven/Cloudinary; Hostinger/Cloudflare son **objetivos NO_ACTIVADOS**. Ningún backup, secret, bucket, base de datos o dominio real se inspeccionó en esta entrega.

## AC01–AC16 / N01–N16 — aceptación futura POSITIVA y NEGATIVA

| ID | Positivo que deberá ejecutarse y comprobarse | Negativo requerido que debe fallar cerrado |
| --- | --- | --- |
| AC01 | Acta con fuente, destino, alcance, owner y firma de autorización | N01: ausencia/caducidad de autorización impide export |
| AC02 | Inventario validado de tablas, relaciones y tenant antes de ensayar | N02: tabla faltante o identidad de owner desconocida bloquea ensayo |
| AC03 | Snapshot sintético produce manifiesto, UTC y hash comprobable | N03: backup corrupto, truncado o hash inconsistente se rechaza |
| AC04 | Restore efímero recupera esquema, FK, índices y secuencias | N04: FK rota o collation incompatible impide abrir gate |
| AC05 | Conteos por tabla/tenant antes-después coinciden bajo reglas aprobadas | N05: discrepancia sin justificación no se asume tolerable |
| AC06 | JWT validado conserva exactamente derechos de empresa A | N06: JWT revocado/expirado no recupera privilegios tras restore |
| AC07 | Empresa B solo observa sus filas y objetos autorizados | N07: B intenta leer A o inyectar EmpresaId/host y se deniega |
| AC08 | Soporte temporal mantiene lectura limitada auditada | N08: sesión soporte intenta escribir y el backend deniega |
| AC09 | Migrador único versiona DDL y aplica bloqueo de concurrencia | N09: segundo migrador paralelo no ejecuta ni muta esquema |
| AC10 | API y worker usan vínculo tenant explícito comprobado | N10: worker sin HttpContext no obtiene contexto global |
| AC11 | Facturas, pagos e inventario preservan unicidad e idempotencia | N11: reintento/duplicado no crea doble transacción |
| AC12 | Readiness reconoce DB destino realmente operativa | N12: DB caída o schema incompatible nunca devuelve ready |
| AC13 | Cutover reversible usa freeze, ventana y validación posterior | N13: ventana sin owner o sin rollback ensayado no inicia |
| AC14 | Delta postcorte se registra, reconcilia y conserva en rollback | N14: divergencia de escritura no se pierde silenciosamente |
| AC15 | Secretos, PII y conexiones fuera de logs y artefactos públicos | N15: fuga en log/manifest dispara FAIL y detención |
| AC16 | CI y evidencia exact-SHA trazan causa y versión de provider | N16: checks cancelled/skipped/queued/fail no se usan como PASS |

**ESTADO DE TODOS LOS CASOS:** `FUTUROS_NO_EJECUTADOS`. Ningún caso AC/N se ha ejecutado por añadirlo aquí. Un solo negativo inseguro o discrepancia no justificada impide acreditar restauración/cutover. No reemplazar prueba MySQL efímera por código compilado ni resumen documental.

## NF01–NF10 — requisitos no funcionales y medición necesaria

| ID | Requisito | Evidencia futura mínima |
| --- | --- | --- |
| NF01 | Segregación de datos por tenant A/B | Pruebas cruzadas y revocación en CI backend |
| NF02 | Integridad relacional, transaccional y contable | FK/conteos/conciliación con dataset de ensayo |
| NF03 | RPO, RTO y ventana de freeze | MEDIR_Y_APROBAR; no se inventan segundos |
| NF04 | Cifrado y cadena de custodia del backup | Auditoría de permisos, transporte y almacenamiento |
| NF05 | Observabilidad y trazabilidad de fallos | Correlation ID, UTC y logs saneados sin PII |
| NF06 | Compatibilidad de versión EF/MySQL | Matriz fuente-destino y prueba SQL con provider vigente |
| NF07 | Rollback sin pérdida de deltas | Prueba de reversión y reconciliación firmada |
| NF08 | Idempotencia y control de reintentos | Sin doble factura/pago/trabajo aun en cortes |
| NF09 | Mínimo privilegio y secreto externo | Sin tokens, credenciales ni IAM global por defecto |
| NF10 | Separación de dev/QA/main/PROD | Permisos humanos diferenciados y Scope Lock |

**RTO, RPO, latencia, número de tenants, capacidad, tamaño de snapshot, ventana de freeze y umbral de diferencias** están `NO_MEDIDOS/NO_APROBADOS`. Nunca expresar como SLO garantizado un dato sin medición, autorización, metodología y readback.

## Casos auditables mínimos para ejecución autorizada posterior

1. **Positivo P-EXPORT:** generar snapshot con dataset sintético, manifest versionado, SHA256, momento UTC, conteos y claves de procedencia, permisos mínimos y cadena de custodia sin datos de clientes.
2. **Negativo N-INTEGRIDAD:** alterar un byte de manifiesto/backup o romper FK de ensayo y comprobar rechazo antes de conmutar.
3. **Positivo P-TENANT:** bajo tokens independientes de A/B, ejecutar consultas y exportaciones permitidas sin cruzar identidades.
4. **Negativo N-TENANT:** inyectar EmpresaId de B, token expirado y host controlado por cliente; toda lectura/escritura no autorizada debe denegarse por API.
5. **Positivo P-RESTORE:** ensayar restauración efímera, comprobar esquema, registros, índices y secuencias, y verificar liveness frente a readiness.
6. **Negativo N-RUNTIME:** provocar DB caída, migradores paralelos, timeout o entrega duplicada; no confirmar transacciones fallidas ni facturas/pagos dobles.
7. **Positivo P-ROLLBACK:** ejecutar freeze autorizado de ensayo, registrar delta, retroceder versión/route y conciliar cada escritura sin pérdida silenciosa.
8. **Negativo N-ROLLBACK:** discrepancia de datos, falta de dueño o pérdida de clave de custodia produce NO_GO para cutover, no un PASS por silencio.

Todos estos pasos requieren autorización y un entorno **no productivo y aislado**, CI dirigida .NET 10 + MySQL efímero, y logs saneados de pruebas positivas/negativas. No se ejecutan en esta M01.05. La tenencia verificada depende del backend; la falta de HttpContext en un worker no concede acceso global a empresas.

## Matriz de decisión para M02–M06 (sin liberar fases)

| Gate | Evidencia exigida antes de aprobar | Si falta |
| --- | --- | --- |
| Contratos M01 + A5 independiente | Todas las preparaciones y sus dependencias completas, revisión de Task Javier distinta | Mantener M01.06/M02 bloqueadas por DAG vigente |
| Contratos M02 | Modelo de esquema/export/import idempotente y rollback especificado | NO_GO a desarrollo de runtime |
| Implementación M03 | Código real con control de tenant/RBAC, migrador único, pruebas MySQL efímeras exact-SHA | No declarar LISTO por documentación |
| Verificación M05 | CI positiva/negativa y ensayo restore/comparación/rollback causal | Permanecer VALIDANDO |
| Liberación M06/cutover | Aprobación humana separada para DNS, secretos, producción, proveedores, ventana, rollback | NO_GO fuera de dev |

Un `CI in_progress` no se convierte en SUCCESS; una prueba SKIPPED/CANCELLED/FAIL no es PASS cuando es exigible. Los checks genéricos del backend son **NO_APLICA** al diff exclusivamente Markdown, pero serán causales cuando el código cambie. El Scope Lock y procedencia V5 sí son obligatorios para este documento.

## A0–A5, impacto y rollback

- **A0:** corroborar Skill canónica/HEAD dev, PLAN/COLA/DAG/owner, dependencias y ausencia de cambios runtime.
- **A1 documental:** verificar 16 positivos, 16 negativos, 10 NF y ocho secuencias de prueba futura; ejecución positiva/negativa = NO_EJECUTADA.
- **A2 documental:** revisar JWT/RBAC/tenant A/B, FK, secrecía, mínimo privilegio, aislamiento, trazabilidad y no confundir host del navegador con identidad.
- **A3 documental:** trazabilidad de G01–G14, R01–R14, fuentes Git 16 y documentos M01.01–.04; sin integración MySQL real.
- **A4:** crear solo este archivo y sección autocontenida CHANGELOG mediante fast-forward CAS en dev; exigir checks `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` sobre commit exacto con completed/success; BITACORA y readback PLAN/COLA.
- **A5:** debe revisar una Task Javier distinta de la autora; A5_PENDIENTE no permite promoción.
- **Rollback:** revertir changeset documental con commit normal y HEAD actual, preservando historial BITACORA, sin force-push ni reset.

**Modificación actual:** solo dos Markdown. No API, frontend, Domain, EF, migraciones, bases de datos, dumps, PII, secretos, proveedores, DNS, R2, QA/main/PROD ni compras. Ninguna decisión de M01 autoriza una migración, y la aceptación real queda pendiente de sus pruebas y permisos.
