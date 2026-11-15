# SOLQARYN V17 — C09-02.M02.03: persistencia conceptual para fidelización y reputación

**Microtarea:** C09-02.M02.03. **Autor:** PRIMARY DEV 02 / JAVIER_CHATGPT (\`6af16bb9492c8191b8e8c7d606f11507\`). **Rama:** solqaryn/Solqaryn@dev. **PROJECT_SCOPE_LOCK:** STRICT. **Riesgo V15:** R0 exclusivamente documental. **PLAN_MAESTRO:** fila 1342; candado V17 SQV17_C09_02_M02_03 / namedRangeId 2000001342.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES (Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA); Skill canónica en dev; PLAN/COLA/DAG. La salida solicitada es **definir** persistencia, índices, migraciones reversibles y datos de prueba para puntos, promociones, reseñas, moderación, respuestas y posventa. Es un contrato de diseño: NO crea entidades físicas, índices, SQL, migraciones EF, usuarios, rutas API, workers ni activación comercial.

## 1. Bases y límites

- M01.01–06 define alcance, actores, brechas, riesgos, criterios y puertas; M02.01 define 16 entidades conceptuales E01–E16 y veinte invariantes INV01–INV20. M02.02 especifica contratos API/DTO independientes; no se asume que ya se implementaron.
- Componentes existentes a reconocer, sin reinterpretar relaciones: Cliente, Venta, CreditoCliente, UsuarioEmpresa, RegistroAuditoria y TiendaCuentaCliente. Cliente.EmpresaId y Venta.EmpresaId históricos NULL no prueban ownership y se mantienen SIN_OWNER_CONFIRMADO, sin backfill inferido.
- El saldo de puntos **no** equivale a dinero, crédito, factura ni inventario. Fidelización, cupones y canjes futuros necesitan antifraude, idempotencia, verificación de venta confirmada y revocación.
- Arquitectura futura: Domain reglas; Application permisos/casos; Infrastructure repositorios y EF Core 10.0.12 con MySql.EntityFrameworkCore 10.0.9 Oracle; API autentica vía JWT y verifica membresía/RBAC; MySQL 8.4 efímero para pruebas posteriores. Ninguna de estas capas cambia aquí.

## 2. Modelo lógico de persistencia PROPUESTO

| Modelo lógico | Clave y aislamiento obligatorios | Invariante para futura implementación |
| --- | --- | --- |
| ProgramaFidelidad | Id, EmpresaId, Version | único por empresa/código y periodo; activación separada |
| CuentaPuntos | Id, EmpresaId, ProgramaId, ClienteId | UNIQUE lógico (EmpresaId, ProgramaId, ClienteId); no admitir NULL |
| MovimientoPuntos | Id, EmpresaId, CuentaId, CausaId, IdempotencyHash | append-only; reversión compensatoria, sin UPDATE destructivo |
| AcumulacionVenta | Id, EmpresaId, VentaId, CuentaId, Version | venta confirmada y propietario verificado; dedupe por causa |
| CanjePuntos | Id, EmpresaId, CuentaId, BeneficioId, Version | transacción y saldo concurrente; no doble gasto |
| BeneficioPromocion | Id, EmpresaId, ProgramaId, FechaInicioUtc, FechaFinUtc | cupo/versión y vigencia UTC, tenant cerrado |
| AplicacionBeneficio | Id, EmpresaId, BeneficioId, ClienteId, OperationHash | idempotencia por empresa/beneficio/operación |
| ReglaAntifraude y AlertaAntifraude | Id, EmpresaId, Version, Referencia | políticas revisables, sin datos sensibles en eventos |
| Resena y DecisionModeracion | Id, EmpresaId, ClienteId/RecursoId, Version | autor/recurso verificados, historia de moderación reversible |
| ApelacionModeracion | Id, EmpresaId, DecisionId, ActorId | no autoaprobación, no borrado de decisión original |
| CasoPosventa y RespuestaPosventa | Id, EmpresaId, ClienteId, VentaId, ActorId | relaciones compuestas y control de recurso en cada consulta |
| ConsentimientoCanal | Id, EmpresaId, ClienteId, Proposito, Canal, Version | revocación efectiva prevalece sobre envío diferido |
| EventoAuditoriaFidelidad | Id, EmpresaId, ActorId, CorrelationId, FechaUtc | datos minimizados, sin JWT ni cuerpos completos |

Las relaciones proponen claves compuestas (EmpresaId, IdRecurso). Una FK simple por Id sin tenant validado **no** acredita aislamiento. Prohibidas cascadas destructivas sobre ventas, libro de puntos, decisiones de moderación y consentimiento.

## 3. Doce índices candidatos — ninguno creado ni medido

| ID | Índice lógico a evaluar en MySQL 8.4 efímero | Uso y riesgo |
| --- | --- | --- |
| I01 | CuentaPuntos(EmpresaId, ProgramaId, ClienteId) UNIQUE | dueño de saldo; evita duplicados |
| I02 | MovimientoPuntos(EmpresaId, CuentaId, FechaUtc DESC, Id DESC) | ledger paginado estable |
| I03 | MovimientoPuntos(EmpresaId, CausaId, IdempotencyHash) UNIQUE | replay de acreditación |
| I04 | AcumulacionVenta(EmpresaId, VentaId, Version) UNIQUE | crédito una sola vez |
| I05 | CanjePuntos(EmpresaId, CuentaId, Estado, FechaUtc) | canjes pendientes/acreditados |
| I06 | BeneficioPromocion(EmpresaId, Estado, FechaFinUtc) | vigencia y caducidad |
| I07 | AplicacionBeneficio(EmpresaId, BeneficioId, OperationHash) UNIQUE | cupo y dedupe |
| I08 | AlertaAntifraude(EmpresaId, Estado, Prioridad, FechaUtc) | revisión por rol |
| I09 | Resena(EmpresaId, RecursoId, Estado, FechaUtc) | publicaciones autorizadas |
| I10 | DecisionModeracion(EmpresaId, ResenaId, Version) | historial y concurrencia |
| I11 | CasoPosventa(EmpresaId, ClienteId, Estado, FechaUtc) | bandeja privada por tenant |
| I12 | ConsentimientoCanal(EmpresaId, ClienteId, Proposito, Canal) | última versión/revocación |

Antes de aprobar índices: EXPLAIN ANALYZE, cardinalidades, distribución por empresa, orden/paginación, coste de escritura y colisiones con constraints existentes. **Rendimiento NO_MEDIDO**.

## 4. Secuencia futura de migración reversible — NO EJECUTADA

| Etapa | Acción únicamente propuesta | Condición y rollback |
| --- | --- | --- |
| M01 | inventario de esquema, relaciones existentes y NULL legacy | snapshot en entorno efímero; no atribuir owner |
| M02 | crear tablas nuevas aditivas con EmpresaId obligatorio | down solo si estructuras vacías; sin destrucción comercial |
| M03 | aplicar UNIQUE y FK compuestas por tenant | fixture A/B y prueba de cross-tenant |
| M04 | conciliación explícita de Cliente/Venta legacy | sin evidencia de propiedad => cuarentena sin escritura |
| M05 | lectura controlada tras autorización backend y flag OFF por defecto | apagar flag sin perder historia |
| M06 | escritura transaccional con CAS/idempotencia y auditoría | pruebas de race/rollback; compensación si ya hay movimientos |
| M07 | reconciliar saldos, cupos, consentimiento, PII y retención | checksum, conteos y log sin secretos |
| M08 | habilitación posterior exclusivamente con aprobación y CI A0–A5 | nunca asumir aprobación para producción |

Después de escrituras, un Down() destructivo no constituye rollback seguro. Conservar historial y compensar; los datos reales Aiven no se modifican por este plan.

## 5. Doce fixtures sintéticos positivos/negativos FUTUROS, no ejecutados

| Caso | Escenario positivo planeado | Rechazo/error que la suite futura debe exigir |
| --- | --- | --- |
| F01 | cuenta de cliente A visible a A | acceso de B o id de B denegado |
| F02 | venta A confirmada acredita puntos una vez | replay o venta anulada no acredita |
| F03 | reversión genera movimiento compensatorio | UPDATE/DELETE del ledger no borra historia |
| F04 | canje A con saldo y versión válida gana una vez | dos writers, saldo insuficiente y CAS obsoleto fallan |
| F05 | promoción A vigente/cupo disponible aplica | promo B, vencida o agotada rechazada |
| F06 | idempotencia A repetida devuelve un efecto | misma clave/payload distinto o tenant B no colisionan |
| F07 | review A verificada y saneada publica | HTML peligroso/PII/autor forjado no publica |
| F08 | moderador A vigente decide con motivo | lector, revocado o moderador B no modifica |
| F09 | apelación legítima preserva historia | autoaprobación o borrado histórico rechazada |
| F10 | posventa A une cliente/venta autorizados | IDOR ticket/venta B devuelve no accesible |
| F11 | opt-in vigente por canal permite flujo futuro | revocación al retry corta envío |
| F12 | fallo transaccional aislado conserva integridad | timeout a mitad de canje no deja gasto parcial |

Datos: dos empresas A/B, clientes sintéticos de ambas, un registro legacy NULL, un usuario revocado, saldo suficiente/insuficiente y cupones caducados. Los fixtures aún no son scripts; la suite futura deberá ejecutar \`dotnet test --filter Category=Integration\` con proveedor Oracle EF y MySQL 8.4 efímero y acreditar realmente tests ejecutados, 0 FAIL y rollback. Build o mock no es integración.

## 6. Calidad, seguridad, dependencias y reversión

- **A0:** especificación del ID C09-02.M02.03, M02.01/alcance M01, doce índices, ocho etapas, doce pares F, fronteras de datos y diff de un documento + CHANGELOG.
- **A1 documental:** oráculos positivos, negativos, frontera y fallo definidos como requisitos futuros; **0/12 tests runtime ejecutados** y calidad runtime NO_MEDIDO.
- **A2 documental:** tenant A/B, legacy NULL, autorización JWT/RBAC real futura, revocación, antifraude, consentimiento, integridad, IDOR y minimización de PII; ninguna prueba de seguridad ejecutable se atribuye hoy.
- **A3:** NO_APLICA_R0 para MySQL/EF/HTTP/Angular/Playwright; este cambio no agrega tablas, migraciones, entidades, pruebas o código ejecutable.
- **A4 pendiente hasta evidencia:** commit único en dev con Scope Lock y V5/procedencia SUCCESS por SHA exacto o equivalencia verificada, readback PLAN/COLA/DAG/BITACORA. SKIPPED/CANCELLED/FAIL/IN_PROGRESS de checks obligatorios no son PASS.
- **A5:** PENDIENTE de otra de las cinco Tasks Javier, TASK_ID distinta del autor. No libera M03 ni fase posterior sin requisitos y A5 verdaderos.
- **Riesgos R2 futuros:** ledger/finanzas, fraude, tenant, EF/MySQL, transacciones, consentimientos y privacidad. R3 producción/irreversible NO_GO sin autorización específica.
- **Rollback R0:** revertir únicamente documento y sección del CHANGELOG mediante commit inverso normal después de revalidar HEAD. Conservar eventos históricos BITACORA y candado V17; nunca force-push ni desactivar seguridad.

**Conclusión limitada:** diseño conceptual verificable de persistencia, índices, migraciones y fixtures para fidelización/reputación. No existe crédito de puntos operativo, descuento, moderación automática, respuesta o contacto habilitado por esta microtarea.
