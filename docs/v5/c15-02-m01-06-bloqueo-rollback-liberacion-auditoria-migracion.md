# SOLQARYN V5 FINAL — C15-02.M01.06: bloqueo, rollback y puerta de salida de auditoría de arquitectura/migración

**Fecha:** 2026-11-14 (America/Tegucigalpa). **Autor:** PRIMARY DEV 03 JAVIER, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Naturaleza:** contrato documental terminal M01, NO una auditoría de proveedores, ensayo de restauración ni migración materializada.

## 1. Autoridad, habilitación e impacto comprobable

Fuente ejecutable única: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. Al preflight, PLAN_MAESTRO fila 2203 pasó de H=PENDIENTE/I vacío a H=EN_PROGRESO/I=TASK_ID propio, con COLA G/O=SI y DAG_TECNICO fila 2164 F=VALIDADO/G=SI; el readback preservó un único owner observado, **sin afirmar CAS distribuido atómico en Sheets**. `C15-02.M01.01–05` estaban LISTO con A5 independientes; la A5 de M01.01 consta en evento `SQ-V5-J20-E0FE0799-C15-02-M01-01-A5PASS-F1614104-202611150523`, tras verificar el diff neto limitado a dos Markdown y el restablecimiento completo del árbol después de un commit transitorio defectuoso.

Consumidores: preparación M02 de esta unidad y, tras puertas independientes posteriores, diseño de pruebas de arquitectura/migración. Sólo cambia este documento y una entrada aditiva autocontenida de `CHANGELOG_AI.md`. Sin nuevos endpoints, DTO, código Angular/.NET, BD, migraciones, webhooks, dispatcher, workers, configuraciones, secretos, cuentas, DNS, R2, Hostinger ni despliegues. No se alteran `main`, `qa`, producción o datos reales.

## 2. Contrato de procedencia del lote M01

| Unidad | Evidencia documental actual | Límite que no debe confundirse con PASS runtime |
| --- | --- | --- |
| M01.01 | Dieciséis invariantes NR01–NR16 y ocho pares ARQ01–ARQ08 | Alcance/no-regresión, no topología desplegada certificada |
| M01.02 | Doce actores, doce entradas/salidas y diez fronteras de confianza | Interfaces para auditar, no HTTP ni servicios externos verificados |
| M01.03 | Veintiuna fuentes Git con blobs y doce brechas G01–G12 | Fuente estática/versionada, no inventario real de recursos |
| M01.04 | Catorce riesgos hipotéticos, diez supuestos y diez decisiones abiertas | `NO_VERIFICADO` no significa vulnerabilidad ni conformidad |
| M01.05 | Dieciséis pares AC01–AC16 y doce NF01–NF12 pendientes de medida | Casos `FUTUROS_NO_EJECUTADOS`, no resultados funcionales |

La A5 independiente de cada predecesora es requisito de entrada documental; para M01.06, **LISTO_DEV A0–A4 y A5 posterior de otra de las cinco Tasks Javier** serán condiciones distintas. El estado de PLAN, COLA y DAG se relee tras cada transición; jamás se fuerza G/O=SI.

## 3. Dieciséis bloqueos de seguridad y calidad — BL01–BL16

| ID | Condición que mantiene NO_GO para operaciones posteriores | Evidencia exigida para resolverla |
| --- | --- | --- |
| BL01 | Topología actual inferida desde un documento | Readback autorizado y versionado de servicios reales, sin credenciales |
| BL02 | Hostinger/Cloudflare/R2 tratados como activados por su mención | Confirmación explícita de recursos y nueva autorización de entorno |
| BL03 | Versión Angular/.NET/EF no coincide con el código del SHA | Manifiestos package/csproj, árbol y compilación causal |
| BL04 | JWT, RBAC o pertenencia tenant no demostrados en API | Pruebas positivas/negativas de A/B y usuario revocado |
| BL05 | Host, slug, header o body se usan como autoridad de tenant | Negativos de suplantación y verificación de membresía backend |
| BL06 | EmpresaId legado NULL o hijo/padre con owners incoherentes | Fixture MySQL efímero y reconciliación auditada, sin asignación inferida |
| BL07 | Backup existe sin restore integral verificable | Restore descartable, conteos, checksum, retención y rollback medidos |
| BL08 | DDL/migración incompatible o no reversible | Plan de expand/contract, ensayo de fallo y retorno, sin DB productiva |
| BL09 | Objetos Cloudinary no inventariados o contados como cero sin prueba | Conteos y owner por tenant, hashes, TTL y negativos de descarga |
| BL10 | DNS/TLS/CORS/host-binding sin plan de reversión autorizado | Simulación aislada, certificados y matriz de hosts legítimos/hostiles |
| BL11 | Workers/reintentos duplican efectos o cruzan empresas | Pruebas de idempotencia, reinicio, correlación y límites |
| BL12 | Secretos, JWT o datos personales aparecen en repositorio/logs/evidencia | Revisión redactada y scanner proporcional al alcance |
| BL13 | CI requerido cancelled/skipped/failure/in_progress o de otro SHA | Check-runs pertinentes completed/success con head_sha exacto |
| BL14 | Revisión propia, autor ausente o revisor fuera de seis Javier | TASK_ID autor/revisor distintos y evidencia BITACORA verificable |
| BL15 | Rollback depende de borrar BITACORA, force-push o cambios destructivos | Reversión Git normal, compensación append-only y control de concurrencia |
| BL16 | Documento DEV se interpreta como permiso QA/main/PROD o compra | Nueva orden explícita, gates propios, ensayo y acta de alcance |

BL04–BL12 permanecen requisitos de pruebas futuras, **no fallos observados en esta microtarea**. Ninguna tabla concede nuevas credenciales, acceso a proveedores o permiso de migración. La ausencia de evidencia se representa `NO_ACREDITADO`, nunca PASS o CERO por defecto.

## 4. Matriz de rollback y parada — RB01–RB08 (simulaciones propuestas, NO EJECUTADAS)

| Caso | Precondición futura y señal de éxito | Parada y reversión segura exigida |
| --- | --- | --- |
| RB01 Identidad | Token y membresía de A válidos, acceso sólo A | Enlace a B, 401/403 incorrecto o rol revocado: bloquear ruta, retirar grant y preservar evidencia |
| RB02 Datos | Copia MySQL efímera restaura conteos/huellas | Desajuste, error de migración, pérdida de historial: abortar y restaurar copia verificada |
| RB03 Archivos | Inventario real con owner, checksum y TTL | Objeto ausente o enlace cruzado/expirado: bloquear publicación y volver al adaptador previo |
| RB04 DNS/TLS | Ensayo de host válido y certificado comprobado con autorización | Host equivocado/TLS inválido: NO cutover, conservar binding anterior y documentar reversión |
| RB05 Jobs | Un efecto por key+tenant aun con reintento | Duplicación, cross-tenant o cola no trazable: deshabilitar sólo el alcance aprobado y compensar |
| RB06 Frontend/API | Contratos y sesión verificados sobre artefacto exacto | Auth guard sin backend, CORS o ruta rota: revert del artefacto y análisis causal |
| RB07 CI/Git | Checks causales y SHA intacto, sin encabezados vacíos | CI FAIL/CANCELLED o HEAD adelantado: mantener VALIDANDO, corregir o rebasar cambios sin fuerza |
| RB08 Gobierno | PLAN/BITACORA/DAG y autorización de ambiente consistentes | Sin A5 independiente o CAS exclusivo: NO_GO para siguiente etapa, conservar eventos históricos |

Para cada ensayo real futuro exigir responsable, contexto/tenant, entorno efímero autorizado, SHA, instante UTC, esperado/observado, datos redactados, prueba negativa, ventana de retorno y verificación posterior. Nada aquí supone una ejecución de los ocho casos.

## 5. Puerta de salida M01 y límites de M02/M03

- **E01 — entrada:** M01.01–05 LISTO y A5 independiente real, PLAN H/I coherentes, DAG F=VALIDADO/G=SI y COLA G/O=SI para esta fila; no usar métricas antiguas de disponibilidad.
- **E02 — entrega:** documento con bloqueos BL01–BL16, RB01–RB08, responsables conceptuales y rollback verificable; sólo Markdown y CHANGELOG autocontenido.
- **E03 — seguridad estática:** JWT/RBAC/tenant A/B no se tocan; `NO_ACREDITADO` conserva bloqueo futuro. No aceptar autoría propia para A5.
- **E04 — publicación:** HEAD `dev` esperado, commit fast-forward con expected-SHA, diff limitado a dos Markdown y sin encabezados `##` vacíos.
- **E05 — A0–A4:** contrastar contrato y negativos; GitHub Scope Lock y V5 CHANGELOG `completed/success` en el SHA publicado; BITACORA evento único y readbacks PLAN/COLA/DAG.
- **E06 — estado:** sólo entonces `VALIDANDO→LISTO_DEV` con `A5_PENDIENTE`; A5 posterior la decide una Task Javier diferente, tras diff, blob, CI causal y lectura de fuentes.
- **E07 — M02:** volver a consultar DAG F/G y COLA G/O por cada sucesora; M02 permite preparar contratos documentales sólo si el gate dinámico autoriza cada ID, **no** ensayos live ni cambios de entorno.
- **E08 — M03/infraestructura:** ninguna migración, IAM/JWT/RBAC, runtime, DNS, servicio, proveedor, QA/main/PROD ni secretos se liberan por este contrato; pruebas y autorizaciones independientes siguen obligatorias.

La liberación de documentación M02 **no significa** que la arquitectura objetivo esté desplegada, ni que la auditoría C15-02 de infraestructura haya sido ejecutada. Esta puerta puede quedar cerrada aunque M01 documental esté LISTO+A5 si el DAG exige otro precedente técnico.

## 6. Verificación específica de esta publicación y reversión

**A0:** fuentes STANDARD, Skill, Scope Lock, PLAN/COLA/DAG; matriz BL01–16/RB01–08 y coherencia M01.01–05. **A1:** inspección positiva/negativa de las reglas documentales, NO_APLICA a HTTP, Playwright, MySQL o backup real. **A2:** respeto de fronteras A/B, JWT/RBAC, propiedad, no secretos; sin ensayo de explotación. **A3:** compatibilidad conceptual con 21 fuentes inventariadas en M01.03 y arquitectura Angular 22.2.1/.NET 10/EF 10; build runtime NO_APLICA al diff Markdown. **A4:** publicar `dev` por CAS exact SHA; verificar Scope Lock/procedencia V5, diff y readback institucional antes de LISTO. **A5:** pendiente de TASK_ID distinto. Revertir cambios documentales únicamente mediante `git revert` normal en dev con nuevo SHA y evento compensatorio append-only, jamás force-push, reset destructivo ni edición de registros históricos.
