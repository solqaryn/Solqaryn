# SOLQARYN V5 — C12-04.M01.01: alcance/no-alcance migración, restore y vuelta atrás

**Fecha:** 2026-11-14. **ID:** C12-04.M01.01. **PLAN:** fila 1838. **Autor:** PRIMARY DEV 03 JAVIER / `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Rama única:** `solqaryn/Solqaryn@dev`. **Resultado:** contrato documental preliminar M01, no ejecución de migración.

## Autoridad y objetivo atómico

Fuente ejecutable: [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN/COLA/DAG](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, `ARCHITECTURE.md`, Skill canónica. El plan exige para C12-04 **exportación, restore de ensayo, comparación, ventana de cambio y vuelta atrás**, con **checksums, conteos, migraciones y aprobación explícita**. Se comprobó elegibilidad M01 documental con PLAN H1838 PENDIENTE/I vacía, COLA G/O=SI, DAG F=VALIDADO/G=SI y claim CAS exclusivo antes de redactar.

**Objetivo presente:** definir las fronteras, las evidencias mínimas y los NO_GO para una eventual migración de infraestructura/DB *sin mover ni consultar datos productivos ahora*. Los documentos M01 de C12-01/C12-02/C12-03 y el stack versionado proporcionan contexto; no acreditan que otra infraestructura esté provisionada. La autoridad del HEAD real y la comprobación de Scope Lock prevalecen sobre dibujos de topología.

## Alcance permitido ahora frente a acciones futuras bloqueadas

| Dimensión | Dentro de M01.01 — documentar | Fuera de M01.01 — requiere gate nuevo |
|---|---|---|
| Fuente y destino | Enumerar modelos de datos, límites de API, adaptadores y propietario de cada fuente | Conectar Aiven real, aprovisionar Hostinger/MySQL, crear redes o credenciales |
| Exportación | Diseñar manifiesto: versión, origen, fecha UTC, herramienta aprobada, recuentos y hash de artefacto | Ejecutar dump, descargar PII, copiar blobs o abrir puertos |
| Restore de ensayo | Definir destino efímero, dataset sintético, invariantes y destrucción segura | Restaurar respaldos reales en servicio no autorizado |
| Comparación | Especificar checksums, conteos, claves referenciales y muestras sintéticas antes/después | Declarar igualdad sin bitácora, certificado o prueba |
| Migraciones de esquema | Identificar ejecutor único, ventanas, orden, locks, compatibilidad y reversión | Ejecutar EF migrations o DDL en MySQL |
| Corte y rollback | Definir criterios de apertura/cierre de ventana, freeze, reconciliación y vuelta atrás | Cambiar DNS, tráfico, dominios, hostnames o main/PROD |
| Seguridad | Requerir tenant A/B, JWT/RBAC, mínimo privilegio, auditoría y secretos externos | Reusar secretos productivos o permitir tenant global |
| Costes y proveedores | Registrar decisiones y aprobaciones pendientes | Contratar servicios, elevar cuotas, crear cuentas |

El stack actual declarado es ASP.NET Core net10.0, EF Core 10.0.12, MySql.EntityFrameworkCore 10.0.9, Angular 22.2.1, JWT/RBAC/multitenancy, infraestructura existente Vercel/Render/Aiven/Cloudinary. Hostinger/Cloudflare son **objetivos futuros**, no destinos activos acreditados por este archivo.

## NR01–NR16: criterios de no-regresión para un cambio futuro

| ID | Invariante que debe sobrevivir a export/restore/cutover |
|---|---|
| NR01 | Identidad de empresa/tenant deriva de membresía server-side, nunca de headers o parámetros del cliente |
| NR02 | Tenant A no puede observar filas, archivos, auditoría, métricas privadas ni credenciales de B |
| NR03 | JWT no válido/revocado y RBAC insuficiente continúan denegando operaciones después de restore |
| NR04 | PK/FK, claves únicas, relaciones y datos transaccionales se preservan sin duplicados |
| NR05 | Ningún dato de inventario, contabilidad o historial comercial se descarta para simplificar restore |
| NR06 | Conteos por tabla y tenant y hashes de manifest por archivo se comparan con política documentada |
| NR07 | Se registra versión exacta de esquema EF y proveedor MySQL antes/después |
| NR08 | Un solo migrador autorizado aplica DDL, sin carreras API/worker |
| NR09 | El restore de ensayo utiliza permisos mínimos y secretos externos, nunca credenciales en Git |
| NR10 | Los logs de export/restore omiten contraseñas, JWT, PII, tokens y cadenas de conexión |
| NR11 | Se prueba compatibilidad API/DTO y cliente previo antes del desvío de tráfico |
| NR12 | Health/readiness falla cerrado ante DB inaccesible o cambio parcial |
| NR13 | La ventana de cambio tiene dueño, aprobación, freeze y ruta de abort medible |
| NR14 | Rollback conserva las escrituras posteriores según estrategia de reconciliación aprobada |
| NR15 | DNS/TLS/Cloudflare/Hostinger no cambian hasta una autorización futura verificable |
| NR16 | No se promueve a QA/main/PROD por un cierre meramente documental de M01 |

## P01–P10 / N01–N10: pruebas futuras, **NO_EJECUTADAS**

| Par | Positivo requerido en ensayo autorizado | Negativo y condición NO_GO |
|---|---|---|
| P01/N01 | Export crea manifiesto versionado con conteos y checksums | Artefacto sin SHA o procedencia no se acepta |
| P02/N02 | Restore sintético reproduce relaciones y valores por tenant | Faltante FK o conteo desigual bloquea cutover |
| P03/N03 | Contexto tenant A permite solo registros A | Tenant A intenta cruzar a B y debe denegarse |
| P04/N04 | JWT y RBAC válidos operan tras restauración | Revocado/expirado no recupera privilegios |
| P05/N05 | Esquema EF esperado y un migrador autorizado | Dos procesos no realizan DDL concurrente |
| P06/N06 | Health reporta listo tras sincronizar | DB inalcanzable jamás reporta ready |
| P07/N07 | Restitución mantiene secuencias y transacciones | Retry duplicado no crea facturas/pagos dobles |
| P08/N08 | Rollback devuelve API a versión compatible | Schema o DTO incompatible bloquea reversión |
| P09/N09 | Auditoría conserva timestamps, actor y tenant saneados | Log no muestra secrets, JWT, PII ni datos B |
| P10/N10 | Ventana de migración cierra con aprobación/conciliación | Sin propietario o checksum no hay cambio de DNS |

Todos son **requisitos futuros**, no informes de ejecución ni certificación de MySQL. Umbrales reales, esquema de particionado, volumen, RTO/RPO y duración de ventana permanecen abiertos hasta inventario/autorización/ensayo.

## Evidencias y decisiones que la etapa técnica debe producir

1. **Inventario inicial:** fuente, alcance de cada tabla y objeto, tenant, esquema, versión, relaciones, datos regulados, retención y owner; hashes de código/fuentes, nunca datos exportados por M01.
2. **Plan de exportación:** modalidad online/offline, consistencia transaccional, freeze, cifrado en tránsito/reposo, cadena de custodia y acceso mínimo.
3. **Restore efímero:** entorno autorizado y aislado, credenciales ficticias o controladas, artefacto íntegro, firma/checksum, validación de conteos y relaciones, logs saneados.
4. **Confrontación:** baseline/resultado por tenant, funciones críticas, ACL/RBAC, UI/API, diferencias explicadas; criterios de divergencia pendientes de aprobar.
5. **Reversión:** responsable autorizado, punto de no-retorno, plan de compensación, ventana de escrituras, réplica/backup verificado, restauración sin borrados destructivos.
6. **Autorización explícita futura:** propietario, sistemas, ventanas, DDL, secretos, DNS, proveedores y límites ambientales especificados por separado.

## Impacto y A0–A5

En esta entrega **solo dos Markdown** (este documento y `CHANGELOG_AI.md`), sin editar API, EF, MySQL, datos, DB real, backup, infraestructura, secretos, DNS, QA/main/PROD, providers o Tasks.

- **A0:** preflight PLAN/COLA/DAG y Scope Lock, arquitectura, dependencias y no-alcance.
- **A1:** contrato NR01–NR16 y diez pares de pruebas futuras verificables documentalmente, no tests de ejecución.
- **A2:** revisión estática de JWT/RBAC/tenant A/B y secrets, sin afirmar aislamiento live del destino futuro.
- **A3:** consistencia con fases M01 de plataforma, API, workers, esquema y restore; **integración runtime NO_APLICA** a Markdown.
- **A4:** publicar con fast-forward/CAS en dev y requerir checks `verify-solqaryn-project-scope` y procedencia CHANGELOG V5 SUCCESS sobre SHA exacto, registrar/readback PLAN/COLA/BITACORA.
- **A5:** revisión por Task Javier diferente del autor, antes de cualquier promoción; la entrega propia queda A5_PENDIENTE.

Rollback de este changeset: **git revert normal** de documentos, preservando cambios concurrentes e historia BITACORA, nunca force-push/reset. Ninguna prueba de exportación, restore, checksum de datos o migración se declara ejecutada.
