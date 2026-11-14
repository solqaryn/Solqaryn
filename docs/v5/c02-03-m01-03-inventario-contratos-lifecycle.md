# SOLQARYN V5 FINAL — C02-03.M01.03: inventario de contratos, código y evidencia de lifecycle tenant

**Clase:** TIPO 1, inventario documental sin implementación. **Fuente:** Standard V5 FINAL (Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`) y Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. **Autor:** PRIMARY DEV 04 JAVIER; TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`; `solqaryn/Solqaryn@dev`; Scope Lock STRICT. Fecha 2026-11-13.

## 1. Resultado exigido y límites

La fase C02-03 exige **alta, suspensión, reactivación, archivado y baja lógica idempotentes**, con repetición, fallo parcial y recuperación. Esta microtarea **M01.03 sólo identifica** los contratos y evidencia de código que existen; no implementa un lifecycle manager ni demuestra ejecución real de esos cinco procesos. `Empresa.Activa` es binario: no debe presentarse como tabla completa de estados SOLICITADO/PROVISIONANDO/ARCHIVADO/BAJA_LOGICA. La documentación M01.01 está en VALIDANDO de otro responsable; M01.02 está LISTO. Ninguno de esos estados es certificación de lifecycle ejecutado.

## 2. Inventario verificable de seis fuentes Git (HEAD de lectura `6da95d0946e1f179d25ab102522b0e130db84065`)

| Fuente | Blob SHA | Contrato que se observa en el archivo | Evidencia que NO aporta |
| --- | --- | --- | --- |
| `backend/src/Domain/Entities/Empresa.cs` | `c44940df7c30983a4c23b93338e0cb9367962069` | Identidad de empresa y atributo `Activa`; documenta que carece de soft-delete persistido | Máquina de estados de cinco transiciones y baja lógica versionada |
| `backend/src/Domain/Entities/UsuarioEmpresa.cs` | `f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c` | Membresía usuario/empresa/rol con estado `Activa`; `RolId` vinculado a empresa | Facultad de operar un tenant diferente o permisos de plataforma |
| `backend/src/Domain/Security/ContextoTenantActual.cs` | `7377d2d752985cc9baf31d08280f38ad94a6a114` | Fábrica desde membresía verificada y `ExigirEmpresa` para alcance tenant | Checkpoint/retry/idempotencia de aprovisionamiento |
| `backend/src/Infrastructure/Services/UsuarioScopeService.cs` | `14f4acbc5e1dc6a3ed1cf3485d54c7f1121b0e6f` | Contexto de seguridad obtenido por membresía y autenticación; advierte contra ruta legacy | Certificación transversal de cada API o worker |
| `backend/src/API/Controllers/PlataformaEmpresasController.cs` | `87fbf187b7993ba0f05edff393500f24d0856833` | Controlador de alta de plataforma con `[Authorize]`, `IPlataformaScopeService` y BCrypt para onboarding | Autorización comprobada mediante peticiones HTTP live en esta M01 |
| `backend/src/API/Controllers/EmpresasController.cs` | `cfedc20236e71db079529442ebf45b0431d339e2` | Controlador empresarial con RBAC y `TenantPermissionContext.GetAuthorizedEmpresaId` | Operaciones globales de suspender/reactivar/archivar otras empresas |

Las seis rutas y hashes anteriores provienen de lectura directa de GitHub y no de mocks. Se han leído fuentes C#; **no se ha compilado ni ejecutado C#** para esta microtarea. Su revisión demuestra presencia de contratos, no corrección funcional de endpoints o persistencia.

## 3. Contratos futuros pendientes de implementación y prueba

| Capacidad exigida | Contrato mínimo a diseñar y validar antes de M02 | Prueba real futura imprescindible |
| --- | --- | --- |
| Alta | Grant auténtico de plataforma, unicidad, correlación y clave idempotente asociada a payload/tenant | Dos solicitudes idénticas causan una sola alta; payload distinto con misma clave devuelve conflicto seguro |
| Suspensión | Transición autorizada con control de versión, denegación server-side de acciones privadas y retención | Suspensión de A niega escrituras de A sin afectar B; reintentos no duplican efectos |
| Reactivación | Nueva comprobación de grant, membresía y cache/sesión; transición de estado permitida | Rechazar token o membresía revocada; no reactivar B desde A |
| Archivado | Reglas de retención, obligaciones contables/documentales, registro auditable | Archivado deniega escritura, mantiene registros legítimos y deja recuperación comprobable |
| Baja lógica | Autorización elevada, retención y huella auditable; prohibir borrado físico implícito | No ejecutar DROP/DELETE ni borrar Cloudinary/MySQL por solicitud empresarial |
| Fallo parcial | Checkpoint, transacción o compensación, idempotencia de workers, timeout y backoff acotado | Recuperación tras fallo intermedio y replay sin contaminación A/B |

No existe aquí evidencia que acredite la operación real de esta matriz. Su valor es la identificación de brechas y pruebas futuras para el diseño/implementación autorizado; no liberar M01.06 o M02 por este inventario.

## 4. Riesgos, controles y consumidores

- **Autoridad:** ningún `EmpresaId`, host, slug, header, claim o parámetro enviado por cliente concede privilegios de plataforma. JWT y rol empresarial por sí solos no bastan para afectar otro tenant. Autorizar siempre server-side.
- **Aislamiento:** todo evento, worker y repository deberá vincular tenant, actor, versión y operación, con validación de `UsuarioEmpresa`; impedir que tarea de A ejecute efectos en B.
- **Idempotencia/concurrencia:** almacenamiento durable de la clave por alcance y hash de payload; compare-and-set o transacción con lock; no reintentar indefinidamente ni recrear archivos después de rollback.
- **Datos/PII:** registros contables, archivos y auditoría no se borran sin contrato de retención y autorización específica; redactar JWT, passwords, DSN y PII en logs.
- **Consumidores futuros:** API de plataforma, filtros RBAC/tenant, Domain Empresa, EF/MySQL, worker, auditoría y clientes Angular; requieren pruebas dirigidas cuando se implementen.
- **Observabilidad:** correlación sin secretos, operador real, timestamps, transición anterior/nueva, denegación y motivo seguro; evitar doble EVENT_ID ante retry.
- **Riesgo conocido:** el estado `Activa` actual no representa por sí mismo estados intermedios; diseño deberá compatibilizar el código existente y migraciones aditivas si se autoriza.

## 5. Matriz de comprobación de esta M01.03 (no pruebas runtime)

- **A0 aplicable:** cotejar seis rutas/blob SHA, integridad de referencias, diff de sólo documento + CHANGELOG y ausencia de secretos nuevos.
- **A1 aplicable:** resultado documental: seis fuentes con observación y límite, seis capacidades con condiciones de aceptación; positivo: cada fuente existente es rastreable; negativo: fuente inexistente o hash distinto no se certifica.
- **A2 aplicable:** revisar JWT/RBAC/membresía, denegación A/B, escalamiento de privilegios, PII y borrados; positivo: se identifican controles presentes; negativo: no afirmar cobertura global por lectura de un controlador.
- **A3 aplicable:** cotejar Plan V5 FINAL, M01.02, DAG_TECNICO VALIDADO y 19 precondiciones de preparación LISTO en PLAN; sin dependencia ejecutable introducida.
- `requiere_dotnet=NO`, `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO`: no se tocó código ni se demuestra runtime con esta entrega documental.
- `requiere_security=SI_DOCUMENTAL`; `requiere_scope_lock=SI`: tras publicar, `verify-solqaryn-project-scope` debe estar completed/success sobre el SHA exacto, y la hoja/bitácora debe reflejar el mismo ID.
- **A4 posterior:** verificar commit fast-forward, árbol y blobs, Scope Lock exact-SHA, readback PLAN/COLA/BITACORA A:Z, y rollback reversible. Otros jobs CI no requeridos no suplen ni bloquean esta auditoría documental.
- **A5:** otro TASK_ID distinto deberá revisar la evidencia y registrar A5_PASS/FAIL antes de cualquier promoción; A5_PENDIENTE no autoriza QA/main/PROD.

## 6. Dependencias, no-alcance y rollback

DAG_TECNICO: `C02-03.M01.03`, `PREPARACION_DOC_PROVISIONAL`, estado `VALIDADO`, gate `SI`; en el preflight 19/19 dependencias documentales estuvieron LISTO. COLA!G/O=SI y PLAN_MAESTRO PENDIENTE/sin responsable antes de claim. Se deben revalidar condiciones si un predecesor cambia o surge A5_FAIL; la preparación no es autorización de implementar el ciclo de vida.

**Prohibido en este changeset:** modificar código, esquema MySQL, credenciales, datos de empresa, JWT/RBAC, endpoints, jobs, proveedores, archivos históricos, CI, reglas, ramas, PR, despliegues, QA/main/PROD o Tasks. Migración de imágenes históricas: NO_APLICA, inventario cero.

**Rollback:** `git revert` normal del commit documental tras revalidación de HEAD y consumidores, sin force-push. Mantener BITACORA histórica y restaurar el estado institucional únicamente mediante transición autorizada con evento causal, nunca borrar evidencia.
