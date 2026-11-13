# SOLQARYN V5 FINAL — C02-04.M01.03: inventario contrastado de contratos, código y evidencia

**Resultado exigido:** leer contratos, código y evidencia disponibles **sin asumir certificación**. Entrega DOCUMENTAL de M01.03 de la consola global auditada C02-04. **Autor:** PRIMARY DEV 05 — JAVIER, TASK_ID `6adc2e55f47c81919f6374e561687076`. Ámbito `solqaryn/Solqaryn@dev`, Scope Lock STRICT. Fuente exclusiva [V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN_MAESTRO / COLA / DAG_TECNICO](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit).

**Límite:** esta unidad **no** implementa consola global, MFA, reautenticación, grants temporales, access broker, cambio de rol, migración, UI ni auditoría append-only nueva. M01.01 definió objetivo; M01.02 actores e IO; esta M01.03 registra el código que **sí existe** y las capacidades que **no quedan demostradas**. C02-04.M01.06, M02 y despliegues siguen cerrados por gates propios.

## 1. Inventario de código real inspeccionado en Git

| Archivo | Blob SHA vigente leído | Comportamiento limitado al contrato observado | No demuestra |
| --- | --- | --- | --- |
| `backend/src/API/Controllers/PlataformaEmpresasController.cs` | `87fbf187b7993ba0f05edff393500f24d0856833` | API `/plataforma/empresas` con `[Authorize]`, comprobación `IPlataformaScopeService.ObtenerActualAsync`, paginación GET y borrador POST específicos; registro de auditoría en consultas | MFA por operación, reautenticación o acceso temporal arbitrario |
| `backend/src/Infrastructure/Services/PlataformaScopeService.cs` | `2a71a56750805ef513c7f312fb6c6c2d53a8c314` | Rol global se resuelve con usuario y asignaciones vigentes en MySQL, sin admitir claim `esAdministrador` por sí solo | Política completa de grants temporales, aprobación de emergencias o redacción universal |
| `backend/src/Application/Interfaces/IPlataformaScopeService.cs` | `ba8f5d313117cc4dd4e20a56e2c1959c724544cd` | Contrato `ObtenerActualAsync` para contexto de plataforma | TTL de acceso excepcional, método `ConcederElevacion` o contrato MFA |
| `backend/src/Application/Security/PlatformSupportPolicy.cs` | `b103f0b0ec7b9169c060661f6a4456f8863152cf` | Allowlist de soporte DEV acotada a GET de productos/categorías y rutas exactas | Escritura global, cambio de permisos, lectura de cualquier empresa |
| `backend/src/API/Controllers/TenantContextController.cs` | `2407c2eb6779ed724e841c688ee14dabbf163171` | Contexto tenant se solicita con autenticación y servicio de scope de usuario | Que el identificador tenant enviado por cliente sea un grant |
| `backend/src/API/Filters/RequiereMembresiaEmpresaAttribute.cs` | `17c7d191e3e1b34f56c3d27ba4992b512526542b` | Filtro exige identidad y membresía de empresa positiva antes de marcar scope | Cobertura de todas las rutas globales o pruebas de ataque en vivo |
| `backend/src/Domain/Security/ContextoTenantActual.cs` | `7377d2d752985cc9baf31d08280f38ad94a6a114` | `DesdeMembresia` valida pareja usuario/empresa y rol efectivo | Permiso de plataforma ni acceso a otro tenant |
| `backend/src/Domain/Entities/UsuarioEmpresa.cs` | `f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c` | Membresía activa y rol empresarial; separación de identidades | Consentimiento para bypass de autorización global |
| `backend/src/Infrastructure/Persistence/TenantQueryBypassGuard.cs` | `2ba91bab0420f7e0fc840cf9c96e2b974838694d` | `RespectCurrentTenant` usa `AuthorizedReadEmpresaId` para consultas con filtros omitidos | Garantía de que todos los consumidores y raw queries la invocan |
| `docs/v5/c02-04-m01-02-actores-entradas-salidas-consola-global.md` | `523001b7d82a71d180891e400478278174ab2838` | Matriz preparatoria 8 actores y 12 IO, con casos futuros | Implementación de superficies y resultados runtime |

**Comprobación:** estos diez blobs pertenecen a archivos concretos leídos desde `dev`. La mera presencia de una clase, atributo, filtro o test escrito no certifica funcionamiento end-to-end ni cobertura transversal. Se evitaron conexiones a proveedores reales y datos productivos.

## 2. Matriz de capacidades frente a evidencia disponible

| Requisito C02-04 | Evidencia concreta | Estado documental actual | Gate técnico posterior |
| --- | --- | --- | --- |
| Identidad global separada de ERP | `PlataformaScopeService.ObtenerActualAsync` consulta rol plataforma persistido | CONTRATO_PARCIAL_OBSERVADO | Prueba HTTP denegando admin ERP sin rol global y claims falsos |
| Directorio global mínimo | `PlataformaEmpresasController.Listar` paginado con auditoría y `no-store` | CÓDIGO_PUNTUAL_OBSERVADO | Pruebas API/DB de redacción, paginación, enumeración y filtración PII |
| Borrador empresarial | `CrearBorrador` protegida por scope plataforma | CÓDIGO_PUNTUAL_OBSERVADO | Caso positivo/negativo, idempotencia y auditoría de alta |
| Separación tenant A/B | `ContextoTenantActual`, membresía y filtro `RequiereMembresiaEmpresa` | CONTROLES_PARCIALES | Pruebas de BOLA/IDOR y joins/exports interempresa reales |
| Soporte DEV no privilegiado | `PlatformSupportPolicy` para GET catálogo | LÍMITE_EN_CÓDIGO | Rechazo POST/PUT/DELETE y bypass rutas; no activar privilegios extra |
| MFA con reautenticación reciente por acción | Ninguna de las fuentes inspeccionadas demuestra la cadena completa | **NO_CERTIFICADO** | Proveedor aprobado, binding a acción/usuario, frescura, test replay/expired |
| Motivo y autorización excepcional temporal | No se verificó contrato completo de grant TTL/revocación | **NO_CERTIFICADO** | Matriz de grants, persistencia de TTL, revocación, idempotencia |
| Auditoría crítica completa | `RegistroAuditoria` en endpoints concretos, sin prueba global | PARCIAL, NO_CERTIFICADA | Casos éxito/denegación/falla DB, retención, PII redactada y consulta segregada |
| Reversión segura | Historial Git y transacciones actuales puntuales; no ensayo de acceso excepcional | **NO_CERTIFICADO** | Test de rollback, fallo parcial, pérdida de auditoría y revocación |
| Consola UI global | No inspeccionada como producto funcional completo | **NO_CERTIFICADO** | Accesibilidad/teclado/403, route guard y E2E con backend real |

## 3. Contratos positivos/negativos para auditoría futura (NO ejecutados)

- **POS-01:** actor de plataforma vigente usa directorio permitido; HTTP 200 únicamente con datos mínimos y auditoría.
- **POS-02:** acción excepcional futura exige MFA reciente, motivo, recurso, plazo y grant exactos; registra trazabilidad.
- **POS-03:** auditor con permiso segregado visualiza una denegación sanitizada sin poder modificar tenant.
- **NEG-01:** administrador ERP de A pretende operación global o datos de B → 403, cero mutación.
- **NEG-02:** JWT manipulado, rol de plataforma inventado o header tenant B → 401/403 sin filtración.
- **NEG-03:** MFA ausente/vencida, reauth antigua o token replay → denegar, auditar intento.
- **NEG-04:** ausencia de motivo, grant revocado o recurso fuera de scope → denegar.
- **NEG-05:** falla de auditoría o transacción en operación privilegiada → fail-closed y sin escritura confirmada.
- **NEG-06:** usuario de soporte DEV solicita escritura global → denegar de acuerdo con ruta/metodo.

Los nueve casos son **escenarios de prueba propuestos**, no evidencia de 9 tests ejecutados ni certificación de MFA o de proveedor real.

## 4. Superficies, dependencias, riesgo y siguiente paso

- **Frontend Angular:** futuro componente/control global, routing y accesibilidad; no modificado ahora. Guards del navegador nunca son prueba suficiente de autorización.
- **Backend API/Application/Infrastructure:** los archivos listados contienen contratos parciales; cambios futuros deben mantener capas, errores seguros y separación de privilegios.
- **MySQL y EF:** sin migraciones ni SQL nuevos; datos PII y seguridad A/B requieren pruebas reales antes de habilitar ejecución.
- **JWT/RBAC:** no inferir rol global desde membresía empresarial, ni usar claims o headers como permiso; cualquier excepción requiere autoridad independiente por acción.
- **Observabilidad y rollback:** exigir correlación y eventos inmutables sanitizados de intentos positivos/negativos; revertir futuros cambios de código con commit aditivo/inverso y pruebas de datos segregados.
- **Dependencias:** `DAG_TECNICO` VALIDADO y 15 predecesores C02-01/02/03 M01.01–05 LISTO solo permiten este análisis M01.03, sin liberar M01.06 ni M02.

## 5. Clasificación de pruebas y cierre de M01.03

`requiere_dotnet=NO`, `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO`; `requiere_security=SI` (inspección estática proporcional de permisos/plataforma/tenant), `requiere_scope_lock=SI` (integridad del commit documental).

- **A0:** 10 rutas y blobs auditados, diff solo documental, no secretos ni modificación a permisos reales.
- **A1:** inventario verificable de capacidades existentes/ausentes y 3 positivos/6 negativos FUTUROS; no confundir lectura con prueba funcional ejecutada.
- **A2:** hallazgos de separación de privilegios, tenant, soporte DEV GET, MFA/TTL no certificados.
- **A3:** coherencia con M01.01/M01.02, seis clases backend y DAG validado; toolchain .NET/MySQL/Angular/E2E no aplica al diff.
- **A4 pendiente postpublicación:** verificar SHA exacto, diff, Scope Lock SUCCESS, readback PLAN/COLA/BITACORA. Registrar `A5_PENDIENTE` para otro TASK_ID Javier, no autoaprobar ni promover.

**Reversión:** commit Git inverso normal en `dev` para el documento y CHANGELOG; no borrar BITACORA ni reescribir historia. Este estudio no es un permiso para operar cuentas o datos de producción.
