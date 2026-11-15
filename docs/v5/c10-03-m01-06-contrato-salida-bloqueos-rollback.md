# SOLQARYN V5 — C10-03.M01.06: contrato de salida, bloqueos y reversión de inventario y archivos

**Unidad:** C10-03.M01.06 · PLAN_MAESTRO fila 1519 · DAG_TECNICO fila 1480 · **Autor:** PRIMARY DEV 03, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3` · **Fuente institucional:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Proyecto:** `solqaryn/Solqaryn`, rama `dev`, `PROJECT_SCOPE_LOCK=STRICT`. Fecha: 2026-11-15.

## Finalidad y alcance exacto
Esta unidad **define** el contrato de salida documental del lote M01 de C10-03 (almacenamiento futuro e inventario condicionado). No verifica un inventario real, no habilita R2, no cambia Cloudinary ni ejecuta migraciones. El inventario histórico conserva el estado **NO_VERIFICADO**, diferente de `CERO_VERIFICADO` y `POSITIVO_VERIFICADO`. El resultado futuro exigido consiste en obtener un inventario autenticado y conciliado antes de decidir si existen históricos por exportar. Un cero real acreditado obliga a omitir exportación, copia y checksum históricos; una cantidad positiva exige manifiesto e integridad antes de cualquier autorización separada. Ninguna autorización de producción o de datos se deriva de este documento.

## Evidencia de predecesores y exclusividad documental
Al preflight institucional, `C10-03.M01.01–05` constaban `LISTO` con A5 independiente acreditado en `PLAN_MAESTRO!K`; `C10-01.M01.06` y `C10-02.M01.06` también constaban `LISTO+A5`. `COLA!G1519/O1519=SI`; `DAG_TECNICO!F1480=VALIDADO/G1480=SI`; fila libre `PLAN_MAESTRO!H1519=PENDIENTE/I1519` vacía antes de claim. Estas condiciones habilitan **solamente esta definición documental**, no el inventario ni M02 ejecutado. La fase M02 tendrá nuevos gates y A5 independiente de M01.06.

Se cotejan los contratos C10-03 M01.01 (estados y catorce invariantes), M01.02 (entradas/salidas/actores), M01.03 (fuentes/código y evidencia sin certificación runtime), M01.04 (riesgos/supuestos/decisiones) y M01.05 (AC01–AC14, P/N y NFR futuros). Los escenarios `P01–P14/N01–N14`, fixtures e integraciones allí descritos son **FUTUROS NO_EJECUTADOS**. No se convierten en resultados de CI o inventario real por citarlos.

## Bloqueos BL01–BL16 que NO pueden considerarse superados por este lote
| Gate | Condición necesaria para una operación futura |
| --- | --- |
| BL01 | Identidad, permiso y entorno de cada fuente de inventario acreditados antes de consultarla |
| BL02 | Conteo histórico autenticado y conciliado entre referencias y proveedor; `NO_VERIFICADO` no es cero |
| BL03 | Divergencias u objetos huérfanos resueltos; estado inconsistente produce NO_GO a migración |
| BL04 | Pertenencia tenant A/B derivada del servidor; header, slug y EmpresaId cliente nunca autorizan |
| BL05 | JWT, roles y capacidades mínimas probados con denegaciones negativas y usuarios revocados |
| BL06 | Propiedad de cada archivo y clasificación histórica/nueva verificables y no enumerables entre tenants |
| BL07 | MIME validado por bytes más extensión, límites de tamaño, cuotas y streaming seguro |
| BL08 | Hash, tamaño, retención y evidencia del manifiesto preservados para cualquier histórico positivo |
| BL09 | URL temporal ligada a objeto, método y TTL; política de replay explícita y revocación comprobable |
| BL10 | Caché/host públicos y privados separados por tenant; cambio de sesión invalida material previo |
| BL11 | Secretos, PII, enlaces firmados y credenciales redactados en evidencia, logs y BITACORA |
| BL12 | Legal hold y retención prevalecen sobre lifecycle/borrado; rollback mantiene auditoría |
| BL13 | Fallos de proveedor y retries usan idempotencia, compensación y ausencia de objetos parciales |
| BL14 | Backups, restauración y reconciliación probados con fixture sintético o entorno autorizado |
| BL15 | CI causal exact-SHA y controles de seguridad pertinentes concluyen SUCCESS antes de certificar código |
| BL16 | Autorización adicional del propietario para R2/DNS/datos/migración/cutover y QA/main/PROD, nunca implícita |

**Bloqueos abiertos:** BL01–BL16 son requisitos de fases futuras; no se declaran pasados porque M01 sea `LISTO`. El alcance de desarrollo de contratos M02 puede comenzar únicamente tras LISTO y A5 acreditado para M01.06 y recalculo reactivo válido de DAG/COLA; M03 técnica y terminales posteriores requieren sus propios predecesores y permisos.

## Escenarios de salida y rollback RB01–RB08 (contratos, NO_EJECUTADOS)
| Escenario | Comportamiento requerido y salida segura |
| --- | --- |
| RB01 | Inventario todavía desconocido: conservar proveedor y datos, bloquear copia y registrar consulta pendiente |
| RB02 | Cero histórico autenticado: omitir exportación/copia/checksum de históricos, registrar prueba de cero; fixture nuevo va aparte |
| RB03 | Inventario positivo autenticado: preparar manifiesto sin PII y plan reversible; no migrar por mera documentación |
| RB04 | Conteos inconsistentes: aislar discrepancias, NO_GO migración, preservar referencias y reintentar con hipótesis documentada |
| RB05 | Upload o lectura cross-tenant rechazados: no exponer objeto ni enumeración, registrar error seguro sin credenciales |
| RB06 | MIME/cuota/retención fallidos: abortar y limpiar fixture sin perder hold ni auditoría |
| RB07 | Timeout/replay/firma inválida: denegar o compensar idempotentemente y verificar ausencia de duplicados |
| RB08 | Futuro cutover autorizado falla: revertir configuración y bindings, verificar integridad y lecturas A/B, no borrar origen hasta certificar |

Estos son criterios de reversión para futuros ensayos, **no pruebas realizadas**. Cada escenario exige resultado positivo y negativo con fecha, SHA, ambiente, owner, identidad, límites de datos, evidencia redactada y readback. No simular llamadas a Cloudinary, MySQL, Cloudflare ni Hostinger para declararlos aprobados.

## Impacto y verificaciones A0–A5
**Componentes editados por esta unidad:** únicamente este documento y una sección autocontenida en `CHANGELOG_AI.md`. Sin cambios en Domain, Application, Infrastructure, API, Angular, EF/MySQL, JWT/RBAC, secretos, workflows, DNS, R2, Cloudinary, backups, trabajadores o datos. La compatibilidad con contratos existentes es documental; ninguna ruta API, esquema ni interacción se publica.

- **A0:** identidad de DEV03, fuente V5 única, owner exclusivo y transiciones PLAN/BITACORA/readback, DAG y COLA reactivos; Scope Lock STRICT.
- **A1:** revisión de BL01–BL16, cuatro estados de inventario y RB01–RB08 como condiciones falsables; sin inventario observado.
- **A2:** límites de confianza server-side JWT/RBAC, aislamiento tenant A/B, retención, integridad, URL firmada, MIME, logs y rollback.
- **A3:** coherencia M01.01–05 y C10-01/C10-02; .NET, Angular, Playwright, MySQL y R2 `NO_APLICA` como pruebas runtime a un diff estrictamente Markdown, nunca PASS.
- **A4:** publicar sólo fast-forward en dev con HEAD/CAS, verificar `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` en el SHA publicado `completed/success`, revisar diff y registros PLAN/COLA/BITACORA posteriores; no declarar LISTO antes.
- **A5:** **PENDIENTE**, requiere otra Task Javier distinta de DEV03 con diff, SHA, evidencias y registro independiente, antes de liberar M02 o QA/main/PROD.

**Reversión:** `git revert` normal del commit de este documento, sin force-push ni reescribir historia; la fila institucional conserva eventos históricos y, si hay corrección, se agrega una nueva evidencia. Esta entrega no cambia estado físico de proveedores, tablas, imágenes ni documentos de usuarios.
