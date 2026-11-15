# SOLQARYN V5 FINAL — C11-02.M01.06 cierre documental y gates de API modular

**Proyecto:** solqaryn/Solqaryn, rama dev, PROJECT_SCOPE_LOCK=STRICT. **Autor:** PRIMARY DEV 04 JAVIER, TASK_ID 6aeacb7820ac8191b7d768a6f33dfa09. **Fuente:** STANDARD DE IMPLEMENTACION DE AUTOMATIZACIONES V5 FINAL; PLAN_MAESTRO, COLA, DAG_TECNICO y BITACORA institucional. **Finalidad:** fijar bloqueos, reversión y salida de la preparación M01 de la API modular. No implementa endpoints ni altera permisos.

## Prerrequisitos y trazabilidad verificados al reclamo

- C11-02.M01.01: objetivo, no-alcance y no-regresion. C11-02.M01.02: actores, entradas/salidas y fronteras. Ambas unidades figuran LISTO y revisadas por Task Javier distinta del autor.
- C11-02.M01.03: inventario acotado de 16 archivos reales (AuthController, TenantContextController, PlataformaSesionController, Program, contratos Application, filtros de membresía/permiso, excepciones y soporte solo lectura), con 12 brechas G01–G12 que son evidencia pendiente, NO vulnerabilidades confirmadas. LISTO; A5 independiente de PRIMARY DEV 04.
- C11-02.M01.04: riesgos potenciales R01–R16, supuestos no verificados S01–S10, decisiones D01–D10 sin aprobar. LISTO; A5 independiente de PRIMARY DEV 04.
- C11-02.M01.05: 18 oráculos positivos, 18 negativos y 12 requisitos no funcionales, TODOS criterios FUTUROS_NO_EJECUTADOS. LISTO; A5 independiente de PRIMARY DEV 04.
- C01-03.M01.06: precedente documental terminal del contrato de identidad y membresía, sujeto a su A5 ya registrado. DAG_TECNICO de C11-02.M01.06: F=VALIDADO, G=SI; COLA G/O=SI y PLAN PENDIENTE sin owner antes del claim. Estas condiciones permiten redactar la salida M01, no aceptar cambios de runtime.

## Bloqueos de no-regresión exigibles antes de implementar M02/M03

| Gate | Condición bloqueante | Evidencia futura de salida |
| --- | --- | --- |
| G01 | Ruta de empresa sin identidad backend autenticada | Claims válidos y negativa por sujeto ausente |
| G02 | Empresa solicitada por cliente se interpreta como autoridad | A/B e IDOR sin cross-tenant |
| G03 | Membresía o rol revocado conserva acceso | Revocación y respuestas 401/403 |
| G04 | Permiso de plataforma se confunde con empresa | Matriz plataforma/empresa por ruta |
| G05 | Soporte de lectura puede ejecutar comandos o SQL mutables | HTTP negativo, interceptor y EF/MySQL efímero |
| G06 | DTO acepta mass assignment o campos de autoridad | Whitelist y request manipulado |
| G07 | Cambia ApiResponse/status sin plan de compatibilidad | Snapshot DTO/HTTP y consumidor Angular |
| G08 | Errores muestran SQL, stack, token, correo o PII | Middleware y log depurado con casos negativos |
| G09 | API no valida tamaño, paginación, sort o tasa de solicitudes | límites server-side, rate-limit medido |
| G10 | Persistencia de una operación parcialmente fallida | Transacción y rollback MySQL efímero |
| G11 | Idempotencia permite duplicados bajo carrera | Reintento y prueba concurrente de efecto único |
| G12 | Storefront/host público recibe campos privados | contrato público y negativas tenant A/B |
| G13 | Auditoría expone datos sensibles o pierde correlación | logs redactados, correlation ID no autoritativo |
| G14 | Cambios EF/DDL avanzan sin reversibilidad | script/migración reversible en instancia efímera |
| G15 | CI requerida queued/cancelled/skipped/failed se da por PASS | exact SHA completed/success de checks causales |
| G16 | Sin A5 independiente se habilita QA/main/PROD | aprobación distinta, control Scope Lock y autorización nueva |

**Clasificación:** estos gates son condiciones para transiciones futuras y medidas de prueba, no hallazgos de fallos confirmados en la plataforma actual. No se cambia ni fuerza la fórmula del DAG. La unidad terminal M01.06 sólo habilitará candidatas que realmente cumplan dependencias y A5 conforme a los datos institucionales calculados.

## Escenarios futuros para comprobar reversión y autorización

| Escenario | Esperado |
| --- | --- |
| P01. Token y membresía A vigentes | Acceso sólo A cuando permiso backend autoriza |
| P02. Plataforma con rol global explícito | Operación global separada y auditada |
| P03. DTO válido y versión compatible | Contrato HTTP preservado, sin campos privados |
| P04. Soporte sólo lectura | Consulta acotada sin persistencia |
| P05. Error de dominio controlado | Código estable y sin PII o stack |
| P06. Revert de cambio futuro | Restaurar comportamiento/contrato previo con CI |
| N01. Token inválido o caducado | 401 sin datos |
| N02. Tenant A solicita recurso de B | Denegación sin enumeración |
| N03. Rol o membresía revocados | Denegación efectiva |
| N04. Cliente fuerza EmpresaId o rol | Ignorar/denegar, nunca elevar autoridad |
| N05. Soporte intenta DML/SaveChanges | Denegar y probar cero efectos |
| N06. DTO contiene campos protegidos | Rechazo sin mass assignment |
| N07. Error incluye secreto o SQL | Ninguna filtración HTTP/log |
| N08. Doble request idempotente | Un solo efecto cuando se implemente |
| N09. Migración transaccional falla | Rollback íntegro en entorno efímero |
| N10. CI cancelled/skipped o A5 propio | NO_PASS/NO_GO, conservar validación |

Los seis P y diez N son especificaciones FUTURAS_NO_EJECUTADAS: este Markdown no prueba ni certifica ninguna. Las suites reales .NET, Angular, Playwright, EF/MySQL y seguridad deberán ejecutarse por la microtarea que efectivamente modifique runtime.

## Contrato de rollback futuro y salida M01

1. Identificar commit, actor, owner de microtarea, versión de fuente, revisión A5 y afectados exactos; impedir doble escritor.
2. Congelar sólo el scope inseguro; dejar otros scopes independientes en su estado real, sin reprogramar Tasks.
3. Para cambios de código, revertir con git revert normal hacia dev preservando otros commits; no force-push, reset, rebase destructivo ni modificar main/QA/PROD.
4. Para migraciones futuras, probar reversibilidad en MySQL 8.4 efímero, datos de prueba y conteos/checksums antes de aprobar cualquier transición; no operar datos reales con este contrato.
5. Para API, comprobar status y shape 400/401/403/404/409 y clientes Angular; para seguridad, repetir negativa A/B, revocación y bypass.
6. Exigir Scope Lock STRICT, checks exact-SHA completed/success, BITACORA aditiva y PLAN/COLA/DAG readback; no usar evidencia de SHA distinto sin equivalencia causal verificable.
7. Registrar causa, daño evitado, resultado y evento compensatorio único; preservar historial institucional inmutable.
8. Sólo levantar el bloqueo con pruebas nuevas pertinentes, A0–A4 y A5 de Task Javier distinta, más permisos explícitos para otro entorno cuando corresponda.

**Salida permitida de esta unidad:** documento M01, changelog autocontenido, revisión A0–A4 documental y A5 posterior por otra Task Javier. Su publicación NO ejecuta API, altera JWT/RBAC, entidad, migración, worker/eventos, secretos, infraestructura ni DNS. M02 no puede asumirse liberada hasta que DAG/PLAN/COLA y revisión independiente lo acrediten.

## Evidencia y auditoría de esta microtarea

A0: delimitación a dos archivos Markdown, revisión de Skill canónica/arquitectura/Scope Lock, DAG y fuente. A1: verificar dieciséis gates, seis positivos, diez negativos y ocho pasos rollback, todos futuros. A2: comprobación conceptual de aislamiento empresa A/B, autoridad plataforma, JWT, revocación, soporte sólo lectura, PII. A3: enlazar API, Application, Infrastructure, MySQL/Angular sin afirmar integración ejecutada; pruebas runtime NO_APLICA por este diff documental. A4: publicar mediante fast-forward/CAS con SHA exacto; comprobar blob y checks Scope Lock + procedencia V5 completed/success, PLAN/COLA/BITACORA readback. A5: únicamente revisor Javier distinto del autor. Revert normal si una premisa falla.
