# SOLQARYN V5 — C12-04.M02.01: modelo de entidades, ownership y no-regresión para MySQL privado/cutover

**Autor:** PRIMARY DEV 03 JAVIER, TASK_ID 6adc2e4bd8a48191ac7d8641ed95a1a3. **Fecha:** 2026-11-15. **Repo/rama:** solqaryn/Solqaryn@dev. **Scope Lock:** STRICT. **Clase:** diseño técnico DOCUMENTAL exclusivamente; no se ejecuta migración ni se aprovisiona destino.

**Fuente de verdad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA) y Sheet institucional PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA (1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU). Al reclamar este ID se comprobaron PLAN H=PENDIENTE/I vacío, COLA G/O=SI, DAG F=VALIDADO/G=SI y los seis C12-04.M01.01–06 LISTO con A5 independiente. M02.01 NO habilita M02.06, M03 o una migración, cada una exige prerequisitos específicos.

## 1. Arquitectura versionada y evidencia delimitada

| Fuente Git de dev | Blob de referencia | Qué demuestra; qué NO demuestra |
| --- | --- | --- |
| backend/src/Infrastructure/Persistence/AppDbContext.cs | c4b32f6637f1b4426655bb0cd12af8dd9d229866 | Contiene las superficies Verified* de tenant y contexto EF; no certifica aislamiento para un worker fuera de HTTP ni esquema de un destino nuevo. |
| backend/src/Infrastructure/Solqaryn.Infrastructure.csproj | 6210d8b12abd77171d95d17e2292c1a319b89263 | net10.0, EF Core 10.0.12 y MySql.EntityFrameworkCore 10.0.9 declarados; no acredita versión/conectividad de un MySQL futuro. |
| backend/src/Infrastructure.Migrations/Solqaryn.Infrastructure.Migrations.csproj | 4fb87115e0c1e38a436d777c898341068c6cc668 | Proyecto de migraciones en repositorio, sin evidencia de DDL aplicado. |
| backend/src/API/Solqaryn.API.csproj | 2a7b33a0974d1ebe13676593d84b458b1243e037 | ASP.NET Core net10.0 y paquetes API/seguridad; no permite conexión administrativa a datos. |
| docs/v5/c12-04-m01-03-inventario-codigo-evidencia-mysql-cutover.md | 4da1375d688b2b96ce09f375baf3993866f18451 | 16 blobs y G01–G14 como brechas de evidencia; inventario de código no equivale a datos, respaldos ni infraestructura verificados. |
| docs/v5/c12-04-m01-06-bloqueos-rollback-liberacion-mysql-privado-cutover.md | 0e1d0699054dc5039a29b766db75bf92aa754ed9 | B01–B16, doce pares P/N y diez rutas RB de preparación, todos sin runtime. |

**Topología actual declarada:** API Render, frontend Vercel, DB Aiven/MySQL; **Hostinger/MySQL privado y Cloudflare son objetivos no activados** en esta fase. Los blobs acreditan contenido versionado, no validación de servidores, costos, secretos, datos empresariales reales, licencia o migración autorizada. El ownership del contexto de soporte global y los Verified* requieren un análisis de privilegios por operación en código y CI dirigida posterior.

## 2. Doce entidades conceptuales (NO son clases EF ni tablas)

| Entidad de diseño FUTURA | Ownership y cardinalidad | Invariante mínima / estado |
| --- | --- | --- |
| PlanCutover | PlanId opaco; propietario de plataforma con autorización explícita, versión e historial | PENDIENTE_APROBACION por defecto; no activa operadores ni proveedor |
| OrigenDatos | PlanId + SourceId + ambiente; acceso mínimo por responsable DBA | Metadatos declarados, no cadenas de conexión ni credenciales |
| DestinoPropuesto | PlanId + TargetId + versión/compatibilidad esperada | No pasa a APROBADO sin permiso humano y pruebas efímeras |
| AlcanceTabla | Origen + esquema/tabla + clasificación de datos + owner | La existencia de un nombre en Git no confirma filas ni estructura real |
| SegmentoTenant | Tabla + empresa propietaria verificada + criterio persistido | Empresas A/B no comparten conjuntos ni se inventan tenant para legados ambiguos |
| EsquemaVersionado | Proyecto EF + migración + fingerprint lógico/semántico | Solo permite comparar; no ejecuta Up/Down ni DDL desde este documento |
| ManifiestoExportacion | Plan + artefacto + versión + SHA-256 real + conteos por tenant | Completitud requiere cadena de custodia y consistencia transaccional |
| IntentoRestore | Manifiesto + destino EFIMERO + actor autorizado + evidencia de hash | No admite restauración productiva ni selección de entorno por navegador |
| ComparacionDatos | Origen y restore por tabla/tenant + conteos, tipos, FK y hash | Diferencia exige investigación; nunca se acepta por tolerancia implícita |
| VentanaCambio | Plan + dueño + freeze + start/stop + aprobación explícita | PENDIENTE hasta política de rollback, comunicaciones y métrica RTO/RPO real |
| LibroReversion | Plan + checkpoint + acciones + compatibilidad app/schema + delta | Describe rollback y reconciliación; no borra escrituras ni habilita DNS |
| EvidenciaAuditoriaCutover | Evento append-only por plan, actor, tenant, correlación y UTC | Sin JWT, password, URI con secretos, datos personales, token o dump |

Todas las identidades y relaciones anteriores son **propuestas sujetas a revisión**. PlanCutover 1:N orígenes, destinos, ventanas y evidencias; OrigenDatos 1:N tablas y segmentos; ManifiestoExportacion 1:N ensayos efímeros; ComparacionDatos N:1 ensayo y origen; VentanaCambio 1:1 LibroReversion aprobado. No se añade PK/FK/índice real ni estructura de configuración.

## 3. Dieciséis invariantes que deberá cumplir un proceso futuro

- OWN01: existe aprobación humana y de seguridad identificable antes de tocar proveedor, DNS, MySQL, secretos o tráfico.
- OWN02: identidad de empresa procede de membresía, recurso y contrato server-side; cliente/host/query no determina ownership.
- OWN03: un segmento sin propietario inequívoco queda NO_GO en cuarentena, nunca asignado a EmpresaId predeterminada.
- OWN04: el acceso MySQL para export/import/restore tendrá permisos segregados y duración limitada; no usar usuario de aplicación con privilegio global.
- OWN05: manifest de exportación incluirá versión, fecha UTC, alcance y hash calculado de bytes reales; checksum de archivo no iguala integridad semántica.
- OWN06: se preservarán PK, FK, índices, constraints, collation, tipos y relaciones; no convertir NULL u ordenar sin regla documentada.
- OWN07: comparación requiere conteos por tabla y tenant, hashes/canonicalizaciones reproducibles y diferencias explicadas.
- OWN08: exportación y restore futuros conservan cifrado, retención y control de acceso; los logs no contienen secretos ni PII innecesaria.
- OWN09: destino de primer restore será recurso EFIMERO aislado, no Aiven vivo, QA, main ni PROD.
- OWN10: un solo migrador autorizado obtiene exclusividad transaccional y compatibilidad de versión; no basta 10 minutos de antigüedad.
- OWN11: migración EF solo podrá comenzar con preflight de versión proveedor, lock, backup y rollback ensayado y SHA causal.
- OWN12: JWT, RBAC y aislamiento A/B deben conservarse tanto con contexto HTTP como en jobs/migradores sin HTTP.
- OWN13: operaciones de comercio crítico, pagos, documentos y outbox conservarán orden, idempotencia y no duplicación.
- OWN14: health/readiness reflejan el estado real de DB y migración; jamás 200/ready durante corte parcial o fallido.
- OWN15: ventana y retorno incluyen punto de corte de escrituras, reconciliación de delta y propietario explícito; rollback DNS aislado es insuficiente.
- OWN16: LISTO_DEV documental y A5 independiente sólo recalculan DAG/COLA, no constituyen autorización de SQL, proveedor, QA/main/PROD.

## 4. Estados futuros y gates de transición

Plan: BORRADOR → EVALUACION → APROBADO_EXPLICITO → ENSAYO_EFIMERO_VERIFICADO → VENTANA_AUTORIZADA → CANDIDATO_DE_CORTE. Una ausencia de permisos mantiene BORRADOR/BLOQUEADO; una divergencia mueve a NO_GO, no APROBADO. Exportación: DISEÑADA → AUTORIZADA → CAPTURADA → HASH_VERIFICADO; ensayo: PENDIENTE → EJECUTANDO_SOLO_EFIMERO → COMPARADO → APROBADO_O_FALLIDO. Estos son **estados conceptuales no implementados**. Un evento de BITACORA de microtarea no representa cambio de estado real de MySQL.

### Fronteras de confianza a preservar

Angular → API: sesión, JWT y RBAC validados en servidor. API → Application/Domain: EmpresaId y pertenencia de recurso verificados, sin acceso implícito por plataforma. Application → AppDbContext: query filtrada y Verified* no se reemplazan por claims no comprobados. Exportador/restore futuro → DB: credenciales externas, mínimo privilegio, manifiesto, exclusividad y destino efímero. Plan → DNS/hosting: autorización independiente y comprobación de proveedor; ninguna de estas conexiones se ejecuta aquí.

## 5. Diez pares de aceptación FUTUROS_NO_EJECUTADOS

| Par | Positivo esperado posteriormente | Denegación negativa obligatoria |
| --- | --- | --- |
| P01/N01 | Manifiesto sintético conserva hash, versión, alcance | Dump sin checksum o sin fuente aprobada no se usa |
| P02/N02 | Tenant A conserva sólo A tras restore efímero | A obtiene filas B por ID, header o token alterado |
| P03/N03 | Comparación de PK/FK, conteos y tipos coincide | Tipo truncado, FK rota o conteo divergente bloquea |
| P04/N04 | Migrador único aplica versión esperada en CI | Dos procesos aplican DDL concurrente |
| P05/N05 | Soporte global autorizado sigue alcance mínimo | Soporte sin autorización gana lectura cross-tenant |
| P06/N06 | Token y permiso vigente conservan denegación | JWT revocado o RBAC insuficiente recupera privilegios |
| P07/N07 | Factura/pago/outbox conservan idempotencia | Retry por corte genera duplicado |
| P08/N08 | Health/readiness sólo es listo al estar DB listo | DB sin conexión se anuncia saludable |
| P09/N09 | Ventana y retorno documentan delta reconciliado | Rollback pierde escritura o mezcla tenants |
| P10/N10 | Log sin PII ni credenciales evidencia actor/UTC | Token, DSN o dump aparece en log público |

Todos los casos anteriores son **futuros y NO EJECUTADOS**: no se realizaron queries de datos, EF migration, MySQL efímero, HTTPS a proveedores, medición RPO/RTO, respaldo, restauración, cutover ni prueba de autorización runtime. En implementación M03/M05 serían obligatorias pruebas .NET 10 y MySQL 8.4 efímero, cases A/B positivos/negativos, firma/hashes, concurrency del migrador, rollback y CI exact SHA.

## 6. Evidencia, gobernanza y rollback

**Impacto del changeset:** únicamente este Markdown + sección autocontenida CHANGELOG_AI.md. Domain/Application mantendrán reglas de negocio; EF/Infrastructure permitirá plan futuro solo con contrato aprobado, migrador único y DbContext; API/Angular no conceden autoridad a nivel de infraestructura. No se modifican datos, tablas, API, CORS, JWT/RBAC, servicios, cloud, DNS/TLS, secretos, horarios, Tasks ni branches fuera de dev.

**A0:** ID exacto, M01.01–06 LISTO+A5, Source V5, Scope Lock, autor/owner, ausencia de secretos, diff de dos Markdown. **A1:** doce entidades, dieciséis invariantes y diez pares P/N completos como requisitos documentales, sin tests ejecutados. **A2:** denegación explícita de tenant cruzado, soporte indebido, SQL sin autorización, proveedor no habilitado, pérdida de datos, PII/secretos. **A3:** compatibilidad conceptual con AppDbContext Verified*, migraciones separadas, versiones .NET/EF/MySQL y los contratos M01; pruebas runtime = NO_APLICA al diff Markdown, nunca PASS. **A4:** commit fast-forward/CAS sobre dev, Scope Lock y procedencia CHANGELOG V5 completed/success exact SHA, readback de PLAN/COLA/DAG/BITACORA. **A5:** revisión independiente por otra Task Javier, nunca el autor.

**Rollback real permitido:** git revert normal del commit documental tras leer HEAD y cambios de otros autores, sin force-push ni reescribir BITACORA. No se puede revertir un cutover porque no ha ocurrido. Ningún proveedor, cuenta de pago, base viva, secreto, producción, QA/main o DNS fue modificado por este documento.
