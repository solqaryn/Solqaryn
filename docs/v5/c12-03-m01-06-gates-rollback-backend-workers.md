# SOLQARYN V5 FINAL — C12-03.M01.06: gates, rollback y salida backend/workers separados

**Autor:** PRIMARY DEV 04 JAVIER, TASK_ID 6aeacb7820ac8191b7d768a6f33dfa09. **Proyecto:** solqaryn/Solqaryn, rama dev, PROJECT_SCOPE_LOCK=STRICT. **Gobierno:** STANDARD V5 FINAL y PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA institucional. **Alcance:** cierre documental M01 de C12-03, no instalación de workers, DNS, servidores o proveedores.

## 1. Prerrequisitos y evidencia restringida

Las cinco unidades documentales M01.01–05 están LISTO con A5 independientes y forman la base de este contrato: NR01–NR16 de alcance/no regresión, A01–A14/F01–F12 de actores y fronteras, quince blobs Git y brechas G01–G12 en el inventario, R01–R14 riesgos preventivos, S01–S10 supuestos no verificados, D01–D10 decisiones abiertas y AC01–AC16/N01–N16/NF01–NF10 FUTUROS_NO_EJECUTADOS. El preflight de C12-03.M01.06 confirmó PLAN pendiente sin dueño, COLA G/O=SI y DAG F=VALIDADO/G=SI. Estas referencias son evidencia documental, no aprobación de despliegue ni migración.

Las fuentes Git relevantes de M01.03 incluyen API/Program.cs blob 01425cff56eb13e12022b1b81a2f322d17fd1fff, Solqaryn.API.csproj 2a7b33a0974d1ebe13676593d84b458b1243e037, Infrastructure.csproj 6210d8b12abd77171d95d17e2292c1a319b89263 y Persistence/AppDbContext.cs c4b32f6637f1b4426655bb0cd12af8dd9d229866. Contratos: ASP.NET Core net10.0, EF 10.0.12, proveedor Oracle MySQL 10.0.9, JWT/RBAC y empresa verificada en backend. Un worker sin HttpContext no hereda permisos de la API ni autorización de EmpresaId del payload. El proyecto de migraciones separado no certifica que exista dueño único en ejecución.

## 2. Barreras de salida B01–B16 para cambios posteriores

| Gate | NO_GO causal antes de implementar o activar | Evidencia futura indispensable |
| --- | --- | --- |
| B01 | Job usa tenant desde payload/header/slug | Binding servidor, prueba negativa tenant A/B |
| B02 | Identidad o membresía del job no se revalida | Authz al consumir y revocación diferida |
| B03 | JWT/RBAC/roles de plataforma se mezclan con tenant | Matriz actor/permiso/recurso y denegación |
| B04 | Worker sin HttpContext obtiene tenant implícito | Sin contexto verificado se rechaza antes de tocar DB |
| B05 | Encolado y efecto de negocio no son transaccionales | MySQL efímero con commit/rollback acoplados |
| B06 | Reentrega causa doble efecto externo | Llave idempotente tenant-bound + carrera con dos consumidores |
| B07 | Ack o entregado se marca antes de commit confirmado | Crash/timeout negativo sin falso éxito |
| B08 | Dos procesos reclaman mismo trabajo | CAS/lock transaccional, exactamente un ganador |
| B09 | API y worker aplican DDL simultáneo | Migrador exclusivo con bloqueo real y rollback probado |
| B10 | Readiness confunde liveness con dependencia sana | DB caída/secret ausente produce not-ready |
| B11 | Logs exponen JWT/OTP/DSN/PII/tenant B | Campos permitidos, errores seguros, escaneo positivo/negativo |
| B12 | Retry no tiene presupuesto, backoff y cuarentena | Cuota/DeadLetter y ausencia de loop infinito |
| B13 | Contrato de mensaje rompe consumidor previo | Versionado, pruebas de compatibilidad y rollback |
| B14 | Timeout Cloudinary/SMTP se interpreta como éxito | Simulación determinista de proveedor y compensación |
| B15 | CI queued/skipped/cancelled/failed se declara PASS | completed/success exact-SHA y análisis causal |
| B16 | A5 propio o documento habilita PROD/Hostinger | Independencia reviewer/autor y permiso de entorno separado |

Los gates son condiciones PREVENTIVAS, no fallos ya observados. Si una barrera falla, detener únicamente la operación afectada y conservar el resto del backlog elegible; no alterar fórmulas ni liberar dependencias a mano.

## 3. Contrato de rollback y contención R01–R10

| Paso | Acción segura futura |
| --- | --- |
| R01 | Identificar SHA, proceso, owner, tenant afectado, cambio y autorizaciones |
| R02 | Suspender únicamente productor/worker autorizado afectado, no Tasks ajenas |
| R03 | Impedir reemitir pagos o efectos confirmados sin idempotencia acreditada |
| R04 | Preservar intento, ACK real, lease, correlación opaca y error redactado |
| R05 | Recuperar trabajo stale sólo por CAS y sin escritor/worker concurrente |
| R06 | Ensayar crash-before-commit y restart en MySQL efímero, no datos reales |
| R07 | Restaurar esquema compatible y medir conteos/checksums antes de volver |
| R08 | Separar liveness/readiness y validar secretos por proceso sin imprimirlos |
| R09 | Repetir aislamiento A/B, RBAC/revocación, compatibilidad API/DTO y tests negativos |
| R10 | Confirmar CI exact SHA, A5 independiente, BITACORA aditiva, readback y autorización explícita |

Para este Markdown, revert normal con git revert a dev y check posterior; no hay esquema, proceso, secreto, servidor, worker o cola que revertir porque no se ejecutó cambio runtime.

## 4. Escenarios de prueba por desarrollar — NO EJECUTADOS

| Par | Positivo exigible | Negativo obligatorio |
| --- | --- | --- |
| P01/N01 | Job A autenticado y tenant-bound | Payload B o actor sin membresía se deniega |
| P02/N02 | Revalidación de permiso al consumir | Rol revocado no ejecuta |
| P03/N03 | Encolado durable y ACK posterior al commit | Crash no confirma efecto parcial |
| P04/N04 | Reintento idempotente con efecto único | Doble redelivery no duplica pago |
| P05/N05 | Migración de dueño único | API/worker no corren DDL en paralelo |
| P06/N06 | Readiness refleja dependencias | DB/secret ausente no se reporta sano |
| P07/N07 | Log correlacionable y depurado | Token/PII no aparece en telemetría |
| P08/N08 | Rollback de contrato compatible | Consumidor previo no pierde datos |

No se ejecutaron pruebas .NET, HTTP, MySQL, Docker, Playwright, Cloudinary, SMTP ni carga para esta documentación. No se fija RTO/RPO/SLO, cuotas ni topología productiva sin mediciones y autorización.

## 5. Condición de liberación M01 a M02

M01.06 solo queda LISTO_DEV con A0–A4 documentales y Scope Lock/V5 SUCCESS exact SHA de los dos archivos Markdown publicados, seguido de BITACORA y readback. A5 requiere TASK_ID Javier distinto del autor; solo el DAG dinámico validado puede permitir reclamar la siguiente unidad M02 con owner exclusivo. M02 debe diseñar mensajes, tenant binding, idempotencia, contratos de proceso y migrador. M03 requiere código real más CI de .NET/MySQL efímero, pruebas A/B/rollback y evidencia, nunca esta narración.

A0: preflight de identidad/Skill/HEAD y dependencias. A1: 16 barreras, 10 pasos rollback y ocho pares P/N escritos como oráculos futuros. A2: aislamiento tenant, JWT/RBAC, datos privados, idempotencia, secretos y denegación sin HttpContext. A3: trazabilidad arquitectura API/Application/Infrastructure/MySQL, sin pruebas runtime causales al documento. A4: fast-forward expected-SHA, sección CHANGELOG autocontenida, Scope Lock y V5 provenance completed/success, readback. A5: revisión futura independiente. No autoriza QA/main/PROD, hosting Hostinger, Cloudflare, R2, DNS, compras, servicios de pago, secretos, migraciones, eventos o Tasks nuevas.
