# SOLQARYN V5 FINAL — C01-01.M01.02: actores, entradas, salidas y superficies de identidad universal

**Naturaleza:** preparación documental de la fase M01, no implementación. El resultado futuro de C01-01 debe identificar tenant, sucursal, propiedad, recurso, usuario, membresía y contexto de autorización; esta unidad sólo identifica **actores y flujos candidatos**, sin certificar API, datos ni RBAC integrados. Fuente: Standard de Implementación V5 FINAL y Sheet institucional PLAN_MAESTRO/COLA/DAG_TECNICO. DAG C01-01.M01.02 F=VALIDADO, G=SI para preparación M01.01–05; once prerequisitos C00-01.M02.01–06 y C00-03.M01.01–05 LISTO. No habilita M01.06, M02 ni una modificación ejecutable.

## 1. Actores e identidades

| Actor | Entrada autorizada futura | Salida/efecto permitido únicamente tras verificación | Denegación imprescindible |
| --- | --- | --- | --- |
| Visitante anónimo | Solicitud de acceso público | Sólo rutas y metadatos explícitamente públicos, sin datos tenant | No consultar recurso protegido ni construir contexto de empresa |
| Usuario autenticado | Token JWT y solicitud de empresa | Identidad de usuario validada; acceso a empresa sólo tras comprobar membresía activa server-side | JWT válido no prueba membresía para cualquier empresa |
| Miembro de empresa A | Operación con rol efectivo de A | Recurso de A autorizado por relación empresa/sucursal y capacidad | Cambio de slug, ID, cookie o cabecera a B no proporciona permiso |
| Administrador empresarial | Solicitud de administración dentro de A | Acción sujeta a rol/capacidad y ámbito real de A | No editar roles globales ni recursos de otra empresa |
| Operador de plataforma | Solicitud reservada al ámbito plataforma | Acción de plataforma sólo por rol diferenciado y auditado | No elevar a plataforma a un administrador empresarial |
| Servicio o worker autorizado | Evento y contexto operativo de fuente legítima | Procesar únicamente el tenant, recurso y permisos de la operación | No confiar en IDs sin procedencia o mezclar trabajos de empresas |
| Revisor independiente | Commit y evidencia para A5 | Dictamen A5 de otro TASK_ID, evento idempotente | No autocertificar, cambiar autoría ni promover QA/main/PROD |

Las cuentas ChatGPT, el GitHub actor, el identificador de Task física y los usuarios comerciales no son el mismo principal de seguridad. Se debe mantener su separación documental y operativa.

## 2. Contratos de entradas/salidas a definir (no endpoints implantados)

| ID | Entrada o fuente | Salida o consumidor esperado | Frontera por probar en fases ejecutables |
| --- | --- | --- | --- |
| IO-01 | Credencial/JWT de identidad | Principal de usuario validado y claims limitados | Token firmado, tiempo, audiencia y denegación segura |
| IO-02 | UsuarioId y EmpresaId solicitados | Membresía real UsuarioEmpresa activa y rol efectivo | Nunca resolver a partir del ID del cliente únicamente |
| IO-03 | Empresa activa y su estado | Contexto tenant de la operación autorizada | Empresa inactiva/revocada no puede recibir datos |
| IO-04 | Selección de Sucursal | Sucursal vinculada a Empresa con propiedad positiva | EmpresaId nullable o no positivo falla cerrado para writes |
| IO-05 | Recurso de inventario/ventas/documentos | Propietario tenant/sucursal de la entidad y operación | No filtrar sólo en frontend ni confiar en rutas de navegador |
| IO-06 | Rol y capacidades empresarial | Decisión RBAC en ámbito Empresa | Rol de plataforma no se deriva de rol empresarial |
| IO-07 | Rol de plataforma autorizado | Decisión global explícita, trazable y mínima | Actor Empresa no obtiene permiso global por nombre o flags |
| IO-08 | Cambio de empresa durante sesión | Nuevo contexto desde membresía de la empresa destino | Respuestas tardías de A no aparecen tras cambiar a B |
| IO-09 | Intento no autorizado o payload inválido | 401/403/404/400 seguros según contrato futuro; auditoría sin secretos | No filtrar existencia, PII ni token de otra empresa |
| IO-10 | Evento auditado de cambio | Actor, empresa, recurso, decisión, correlación | Logs sin contraseñas/JWT; idempotencia preservada |

En cada implementación, la salida concreta/API/DTO y sus códigos se determinarán contra código vivo y aceptación de C01-01. La tabla describe consumidores y contratos **propuestos**, no certificaciones sobre rutas actualmente expuestas.

## 3. Referencias de código real observadas en dev

- `backend/src/Domain/Entities/Empresa.cs` blob `c44940df7c30983a4c23b93338e0cb9367962069`: raíz empresarial, estado e identidad; existencia no certifica aislamiento integral.
- `backend/src/Domain/Entities/Sucursal.cs` blob `8746b11fa30cdac0b97241b6221100bedd813b35`: EmpresaId nullable de compatibilidad, con resolver propietario fail-closed.
- `backend/src/Domain/Entities/UsuarioEmpresa.cs` blob `f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c`: vínculo explícito y rol efectivo por empresa.
- `backend/src/Domain/Entities/Usuario.cs` blob `cfe319bb61e405379fa632ed18d3183cd2df1a68`: modelo usuario, PasswordHash y compatibilidad legacy.
- `backend/src/Domain/Entities/Rol.cs` blob `a4f913d66f2c6bd2351155d66a93f9d12dc18a09`: ámbito Empresa/Plataforma y propietario.
- `backend/src/Domain/Security/ContextoTenantActual.cs` blob `7377d2d752985cc9baf31d08280f38ad94a6a114`: requiere UsuarioEmpresa comprobada y no claims arbitrarios.

**Limitación:** estos blobs de dominio son referencias de lectura; no sustituyen verificación live EF/MySQL, API, seguridad, Angular o procesos de empresa/sucursal. Revalidar al implementar y después de cambios en dev.

## 4. Pruebas positivas y negativas exigidas a los contratos futuros

| Caso | Situación | Resultado observable futuro |
| --- | --- | --- |
| P-01 (+) | Miembro activo consulta su Empresa A con rol válido | Contexto para A, capacidad autorizada y trazabilidad |
| P-02 (+) | Usuario pertenece a A y B, cambia a B con membresía real | El servidor recalcula rol de B; A no queda reutilizada |
| P-03 (+) | Sucursal pertenece a Empresa A y recurso pertenece a esa sucursal | Lectura limitada al ámbito y entidad propietarios |
| N-01 (-) | Usuario A cambia EmpresaId a B | Denegación y sin datos/mutación de B |
| N-02 (-) | Sucursal sin EmpresaId válido | Escritura rechazada y sin fallback silencioso |
| N-03 (-) | Rol Empresa solicita endpoint de plataforma | Denegación sin elevación |
| N-04 (-) | Token inválido, expirado o sesión revocada | Denegación de API sin fuga ni bypass de UI |
| N-05 (-) | Error de membresía o rol obsoleto | Fail-closed y revalidación efectiva |
| N-06 (-) | Payload `<script>`, IDs cruzados o mass assignment | Validación segura, sin ejecución ni modificación parcial |
| N-07 (-) | Usuario cambia de A a B con respuesta de A pendiente | Descartar dato stale sin mezclar estado o identidad |
| N-08 (-) | Revisor usa mismo TASK_ID o un evento BITACORA incompleto | A5 prohibida; BITACORA institucional A:Z de 26 campos |

Estas son pruebas de aceptación **especificadas**, no ejecutadas en esta microtarea documental; tests API/EF/MySQL/E2E serán obligatorios donde el resultado futuro lo exija y su evidencia vendrá de SHA exacto.

## 5. No-alcance, gobierno y reversión

La unidad no modifica frontend Angular, API .NET, modelo Domain, JWT, BCrypt, RBAC, EF/MySQL, migraciones, datos comerciales, jobs, secretos, API de otros servicios, infraestructura ni despliegues. No añade ni libera usuarios/roles/sucursales ni capacidades. No editar QA/main/PROD, no permitir permisos cruzados de cuentas ni crear nuevas Tasks, PR o ramas. M01.01–05 documentales no autorizan M01.06 o M02; revalidar al cerrar C00-01 y el contrato C01-01 final.

**Gates proporcionales:** `requiere_dotnet=NO`, `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO`; `requiere_security=SI` por análisis de JWT/RBAC/tenant/PII, y `requiere_scope_lock=SI` si hay commit documental. A0 revisar diff, integridad y secretos; A1 actores/IO y casos positivos/negativos; A2 límites de autorización; A3 consistencia con seis blobs fuente, V5, DAG y BITACORA 26; A4 fast-forward `dev`, SHA exacto, Scope Lock SUCCESS, readback PLAN/COLA/BITACORA; A5 revisor con TASK_ID distinto. Un job técnico irrelevante y no requerido no se cuenta PASS ni bloquea esta validación documental. Un fallo real de Scope Lock sí bloquea.

**Rollback:** Git revert normal sólo del commit documental, conservando historia concurrente; no reset/force-push. Sin cambios de DB ni de datos productivos. Este documento no autoriza QA/main/PROD.
