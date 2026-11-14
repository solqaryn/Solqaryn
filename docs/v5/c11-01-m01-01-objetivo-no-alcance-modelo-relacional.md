# SOLQARYN V5 — C11-01.M01.01: alcance y no-regresión del modelo relacional

**Autor:** PRIMARY DEV 06 / JAVIER_CHATGPT / Task 6af16bbad26881918c283abe06e1f32a. **Fuente ejecutable:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA. **Plan:** C11-01.M01.01, PLAN_MAESTRO fila 1586, DAG VALIDADO/SI, PROJECT_SCOPE_LOCK=STRICT, 2026-11-14.

## Objetivo y entregables reales

El lote C11-01 busca un contrato relacional verificable de claves primarias, claves foráneas, índices, ownership y compatibilidad con MySQL objetivo. La salida futura requiere diccionario de entidades/tablas, matriz tenant/sucursal/usuario, invariantes de integridad comercial y plan de migraciones reversibles. **Esta microtarea M01.01 únicamente fija objetivo, exclusiones y criterios de no regresión.** No crea, ejecuta ni certifica una migración o esquema efectivo, y no permite acceso a bases de datos productivas.

La autoridad del plan exige referencias C01.01 y C07.01 y bloques C01/C02/C03/C10. El DAG dinámico verificó los predecesores M01.01–05 de esos bloques para PREPARACIÓN DOCUMENTAL. No permite inferir A5, autorización de runtime ni liberar M01.06/M06.06, migraciones, JWT/RBAC, QA/main/PROD.

## Evidencia del HEAD dev, alcance y limitaciones

- references/architecture.md y Skill canónica: backend .NET 10, Domain <- Application <- Infrastructure, API como composition root, EF Core 10.0.12 con MySql.EntityFrameworkCore 10.0.9 (Oracle), MySQL, Angular 22.2.1; el frontend no autoriza por sí solo.
- backend/src/Domain/Entities/Empresa.cs, blob c44940df7c30983a4c23b93338e0cb9367962069: raíz empresarial e inactivación; no introducir soft-delete ficticio.
- backend/src/Domain/Entities/Cliente.cs, blob 8c6d4c4453e44f8c0be9475340465e1aadb3f787: EmpresaId nullable para registros históricos aún no atribuidos; **no asumir ownership completo ni atribuir permisos por un campo nullable**.
- backend/src/Infrastructure/Persistence/AppDbContext.cs, blob c4b32f6637f1b4426655bb0cd12af8dd9d229866: selección de capacidades tenant verificadas en backend y protección read-only de soporte. Las FK, mappings, índices y filtros efectivos se deben inventariar por separado, sin inferir certificación general.
- backend/src/Infrastructure/Solqaryn.Infrastructure.csproj, blob 6210d8b12abd77171d95d17e2292c1a319b89263: versión net10.0 y pins EF/MySQL. Un package pin o una carpeta Migrations **no demuestra migraciones aplicadas** a Aiven.

M01.03 deberá completar inventario y conteos con blobs exactos por tabla, mappings, índices y migraciones: esta muestra de fuentes **no equivale** a auditoría exhaustiva o a ausencia de riesgos.

## Inclusiones futuras y exclusiones presentes

**Incluye preparar:** ownership EmpresaId/sucursal/usuario desde contexto confiable del servidor; PK/FK e índices medidos; unicidad compuesta por ámbito si procede; nulabilidad histórica; transacciones de ventas/inventario/facturación; auditoría proporcional; estrategia expand/contract, backups efímeros y rollback probado.

**Excluye hoy:** escribir SQL, EF migrations, seed, DbContext, entidades, nuevos DTO/endpoints, token/JWT/RBAC, cambiar datos Aiven, corregir históricos a ciegas, acceso PROD, instalar proveedor, crear jobs o eventos/webhooks, cambios a Cloudflare/Hostinger, facturar/cobrar, comprar servicios o tocar QA/main. Una fila o gate DAG no concede autorización operativa.

## Invariantes de no regresión NR01–NR14 (criterios futuros)

| ID | Condición esperada | Ejemplo explícito de bloqueo |
| --- | --- | --- |
| NR01 | PK estable y FK consistente durante evolución de esquema | Perder referencias históricas o renumerar claves |
| NR02 | Tenant del recurso se deriva del contexto backend autorizado | Tenant A lee/escribe fila B mediante EmpresaId cliente |
| NR03 | Empresa, sucursal, usuario y entidad comparten scope verificable | Mezclar stock/pedidos de empresas distintas |
| NR04 | Nulabilidad histórica se audita antes de hacer NOT NULL | Transformar Cliente.EmpresaId sin inventario previo |
| NR05 | Unicidad e índices tienen ámbito y carga real demostrados | Colisión global indebida entre tenants |
| NR06 | Claves foráneas e índices se diseñan por query y plan medidos | Afirmar índice exitoso sin EXPLAIN/test |
| NR07 | Transacción protege operación financiera/inventario completa | Venta parcial y factura/caja incoherentes |
| NR08 | Inactivación/borrado conservan semántica de cada entidad | Agregar eliminación lógica ficticia a Empresa |
| NR09 | Soporte read-only no puede modificar usando EF tracking | SaveChanges autorizado en soporte temporal |
| NR10 | Capability de storefront no concede inventario/RBAC privado | Autorización administrativa implícita desde público |
| NR11 | Expand/contract conserva consumidores y rollback | DDL irreversible o migración destructiva sin backfill |
| NR12 | Logs/diagnóstico excluyen PII, tokens, SQL sensible | Filtración en trace/migración/export |
| NR13 | DB se prueba con MySQL efímero y proveedor real configurado | Sustituir prueba MySQL obligatoria por in-memory |
| NR14 | Preparación documental no es permiso ni prueba ejecutada | Marcar runtime/QA/PROD PASS con solo Markdown |

## Pruebas positivas FUTURAS, no ejecutadas

- **P01:** inserción con FK válida y lectura desde mismo tenant en MySQL efímero.
- **P02:** dos empresas A/B con claves iguales admitidas sólo según el ámbito de unicidad esperado.
- **P03:** preservar registros Cliente.EmpresaId nullable durante transición auditada, sin elevar privilegios.
- **P04:** verificar mappings EF, compatibilidad lectores/escritores y clave compuesta.
- **P05:** aplicar y revertir migración en CI efímera, comparar registros y checksum.
- **P06:** medir índice bajo datos representativos con EXPLAIN y presupuesto.
- **P07:** simular fallo de operación comercial y comprobar rollback atómico.

## Pruebas negativas FUTURAS, no ejecutadas

- **N01:** tenant A usa ID de B para lectura/escritura.
- **N02:** cliente manipula EmpresaId, slug, ruta, cookie o header.
- **N03:** FK inexistente o sucursal de tenant distinto.
- **N04:** unicidad global impide registro permitido en otra empresa.
- **N05:** NOT NULL elimina o reasigna históricos sin evidencia.
- **N06:** migración rompe compatibilidad API/consulta durante transición.
- **N07:** sesión soporte read-only invoca SaveChanges.
- **N08:** transacción falla dejando factura/inventario divergentes.
- **N09:** error expone PII, cadena de conexión, token o SQL sensible.
- **N10:** DDL productivo sin autorización, backup ni rollback ensayado.

P01–P07/N01–N10 son especificaciones FUTURO_NO_EJECUTADO; no constituyen resultados xUnit, MySQL, Angular, Playwright o benchmark.

## Auditoría, gate A5 y rollback

**A0:** scope, plan, DAG, Skill/HEAD y diff solo docs/v5 + CHANGELOG. **A1:** aceptación documental comprobando NR/P/N, evidencias y límites de inferencia. **A2:** riesgo y aislamiento tenant A/B, JWT/RBAC, PII e históricos. **A3:** correspondencia de modelo con Domain/Application/Infrastructure y fuente V5. **A4:** commit serializado fast-forward con control del HEAD dev, checks exact-SHA Scope Lock y V5 CHANGELOG completed/success y readback institucional; CI de runtime sin causalidad es NO_APLICA, jamás PASS.

**A5_PENDIENTE:** otra Task Javier, distinto TASK_ID, debe revisar blob, diff y evidencia. M01.06 y migraciones no se liberan por M01.01. **Rollback:** git revert ordinario del commit documental; conservar historia y datos, sin force-push ni alteraciones productivas.
