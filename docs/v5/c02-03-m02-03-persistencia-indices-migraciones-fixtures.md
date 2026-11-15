# SOLQARYN V5 — C02-03.M02.03: contrato de persistencia para ciclo de vida empresarial

**Naturaleza:** diseño documental, NO migración ni runtime. **Autor:** PRIMARY DEV 05 JAVIER, TASK_ID `6adc2e55f47c81919f6374e561687076`. **Rama:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Fecha:** 2026-11-15. **Fuente:** STANDARD V5 FINAL, PLAN_MAESTRO fila 370, COLA G/O=SI, DAG_TECNICO F/G=VALIDADO/SI, C02-03.M01.01–06. M01.06 está LISTO con revisión A5 independiente verificada.

## 1. Brecha de código real y límites

| Fuente actual en dev | Blob Git | Contrato comprobado / límite |
| --- | --- | --- |
| `backend/src/Domain/Entities/Empresa.cs` | `c44940df7c30983a4c23b93338e0cb9367962069` | `Activa`, `Activar()`, `Desactivar()` existen; `Eliminado => false` es **NotMapped**, no baja lógica persistida |
| `backend/src/Domain/Entities/UsuarioEmpresa.cs` | `f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c` | `UsuarioId`, `EmpresaId`, `RolId`, `Activa`; relación por empresa y unicidad lógica; no garantiza lifecycle universal |
| `backend/src/Domain/Security/ContextoTenantActual.cs` | `7377d2d752985cc9baf31d08280f38ad94a6a114` | `DesdeMembresia` y `ExigirEmpresa` obligan membresía activa; ningún header/cliente crea autoridad |
| `backend/src/Infrastructure/Services/UsuarioScopeService.cs` | `14f4acbc5e1dc6a3ed1cf3485d54c7f1121b0e6f` | Scope se lee de MySQL/membresías; sin tenant inequívoco la operación debe denegarse |

**No existe acreditación en este diseño** de estados persistidos SOLICITADO, PROVISIONANDO, SUSPENDIDO, ARCHIVADO o BAJA_LOGICA, de tabla nueva, evento ejecutable, compensación MySQL, migración EF o servicio global de idempotencia. El objetivo de C02-03 es alta, suspensión, reactivación, archivado y baja lógica reproducibles con recuperación, **que se implementarán después bajo gates M03**.

## 2. Modelo de persistencia futuro propuesto, no creado

| Identidad lógica futura | Claves y relaciones | Invariante y razón |
| --- | --- | --- |
| `EmpresaCiclo` | `EmpresaId` PK/FK Empresa, `Estado`, `Version`, `UpdatedAtUtc` | Una sola máquina por empresa; estado ENUM validado en aplicación, nunca inferido de `Empresa.Activa`; versión para CAS |
| `EmpresaCicloSolicitud` | `OperacionId` UUID lógico, `EmpresaId`, `ActorPlataformaId`, `Accion`, `PayloadHash`, `ClaveIdempotenciaHash` | Misma clave y payload producen misma respuesta; misma clave y payload distinto = conflicto sin escritura |
| `EmpresaCicloCheckpoint` | `OperacionId`, `Etapa`, `Version`, `Estado`, `Intentos`, `ActualizadoUtc` | Reanudación acotada de fallo parcial; nunca marcar ACTIVO si falta binding o membresía |
| `EmpresaCicloAuditoria` | `AuditoriaId`, `EmpresaId`, `OperacionId`, `ActorId`, `EstadoAnterior`, `EstadoNuevo`, `Resultado`, `FechaUtc` | Huella inmutable append-only, sin JWT, contraseña, DSN ni PII innecesaria |
| `EmpresaCicloRetencion` | `EmpresaId`, `RetenerHastaUtc`, `LegalHold`, `MotivoCodigo` | Archivar y baja lógica conservan documentos contables y sujetos a retención; ningún DELETE/DROP automático |

Las cinco estructuras son **hipótesis de diseño**; nombres físicos, tablas EF, migración y cardinalidades finales requieren análisis contra entidades existentes, `C02-03.M02.01` (autoría ajena en progreso), compatibilidad de contratos `M02.02` y aprobación de la revisión independiente antes de implementación. No publicar esquema basado en estos nombres todavía.

## 3. Índices y transacción propuesta (no SQL ejecutado)

| Índice/constraint futuro | Finalidad y comportamiento esperado |
| --- | --- |
| `UX_Ciclo_EmpresaId` | Exactamente un estado de ciclo por Empresa, FK y borrado restrictivo |
| `UX_Solicitud_Actor_ClaveHash` | Dedupe por actor/plataforma y clave hash; no guardar el token de idempotencia en claro |
| `UX_Checkpoint_Operacion_Etapa` | Un resultado durable por etapa y operación; replay de la misma etapa seguro |
| `IX_Solicitud_Empresa_Accion_Fecha` | Trazar operación por empresa sin exploración entre tenants |
| `IX_Auditoria_Empresa_Fecha` | Lectura acotada y paginada de huella empresarial |
| `IX_Checkpoint_Estado_Fecha` | Reanudación selectiva de fallos con número máximo de intentos |
| `IX_Ciclo_Estado_Version` | Verificación/compare-and-set de transición sin lectura global no autorizada |
| `IX_Retencion_Bloqueo_Fecha` | Distinguir expiración de retención y legal hold sin borrado masivo |

Implementación M03 futura: transacción EF/MySQL 8.4 bajo aislamiento elegido y probado, bloqueo de fila mediante patrón proveedor-verificado (p. ej. `SELECT ... FOR UPDATE`) o `UPDATE ... WHERE Version=@esperada` con filas afectadas=1, y índice UNIQUE físico real. Sin retries de API sin límite ni compacción de auditoría. No asumir índices parciales portables en MySQL ni que un lock local en memoria cubre instancias Render.

## 4. Estrategia de migración futura y rollback

1. **Preflight separado:** certificar M02.01–06 y A5, revisar snapshots, integridad de FKs/índices y permiso de ejecución de base efímera; no usar Aiven real como ensayo.
2. **Expandir aditivamente:** tablas futuras sin deshabilitar query filters ni reescribir `Empresa.Activa`; aplicar migración EF solo cuando la fase ejecutable y migraciones estén autorizadas.
3. **Backfill prudente:** valores `Activa=true/false` podrían mapearse SOLO tras definir estados y reconciliar con negocio; no asumir ARCHIVADO ni BAJA_LOGICA de booleanos, sin activar tenants por defecto.
4. **Validar con fixtures A/B:** uniqueness, FK, concurrencia, rollback por fallo parcial, separación de roles global/empresa, rangos de retención y replays duplicados en servicio MySQL efímero.
5. **Activación gradual posterior:** siempre fail-closed; cambios en JWT/RBAC, trabajadores o facturación exigen gates específicos, no se desprenden de un documento.
6. **Rollback/forward-fix:** este documento se revierte con `git revert` normal; el esquema futuro exige backup/restauración **probados** y plan forward-fix sin perder auditoría ni datos legítimos; Git revert no revierte migraciones.

## 5. Fixtures propuestos A1–A12, NO ejecutados

| Caso | Escenario a ejecutar en M03/M04 | Resultado esperado |
| --- | --- | --- |
| A1 | Misma clave/payload repetidos | Una operación/response durable, sin empresa duplicada |
| A2 | Misma clave, hash distinto | Conflicto estable, sin cambios de empresa |
| A3 | Dos solicitudes simultáneas con misma clave | Un solo ganador por constraint, no carrera de check-then-insert |
| A4 | Borrador sin membresía válida | Sin ACTIVO ni grants ni tráfico comercial |
| A5 | Empresa A solicita suspensión B | Denegación y ninguna mutación en B |
| A6 | Rol empresarial intenta operación global | Denegación por scope plataforma, no elevación |
| A7 | Token/rol revocado intenta reactivación | Denegación y revocación conservada |
| A8 | Error tras crear Empresa, antes de checkpoint | Rollback o FALLA_REPARABLE no ACTIVO; replay seguro |
| A9 | Suspensión repetida | Efecto y auditoría idempotentes según contrato, sin duplicaciones |
| A10 | Archivado con legal hold | Retención conservada; no eliminación física |
| A11 | Baja lógica tras expiración sin autorización | Denegación; `Empresa.Eliminado` sigue NotMapped hasta migración aprobada |
| A12 | MySQL/service interrumpidos durante CAS | Una versión ganadora y reconciliación por checkpoint, sin contaminación de tenants |

**No se han ejecutado xUnit, MySQL, Docker, migraciones, HTTP ni Playwright.** La tabla fija futuros oráculos verificables, no PASS de implementación. Los datos de fixtures usarán IDs/actores sintéticos, nunca secretos.

## 6. Comprobación documental A0–A5

- **A0:** alcance solo documento y sección autocontenida `CHANGELOG_AI.md`; 4 blobs de código fuente contrastados; sin edición de archivos de M02.01/M02.02 de otros owners.
- **A1:** inspección verificable de 5 modelos propuestos, 8 índices, 6 pasos de migración y 12 casos A/B/fallo/replay; negativos identifican falta de migración real.
- **A2:** roles plataforma separados de empresa, contexto JWT/RBAC/UsuarioEmpresa, aislamiento A/B, idempotencia en constraint, auditabilidad y retención; secretos nunca persistidos.
- **A3:** corresponde a C02-03, M01.06 LISTO+A5 independiente, DAG VALIDADO/SI y coherencia de entidades actuales; no se concede dependencia por M02.01/02 EN_PROGRESO.
- **A4:** exige Git fast-forward con HEAD esperado, checks Scope Lock y V5 completed/success **exact SHA** y readback PLAN/COLA/BITACORA. Backend/Angular/MySQL CI = NO_APLICA al diff Markdown, no PASS.
- **A5:** solo una Task Javier diferente de la autora podrá certificar revisión técnica; no se habilita M03, QA/main/PROD ni migración.

**Fuentes:** Standard V5 FINAL; `docs/v5/c02-03-m01-01-objetivo-ciclo-vida-provisionamiento.md`, `c02-03-m01-03-inventario-contratos-lifecycle.md`, `c02-03-m01-06-bloqueos-rollback-salida-ciclo-vida.md`, y archivos Domain/Infrastructure citados con sus blobs de HEAD.
