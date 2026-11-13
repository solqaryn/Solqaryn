# SOLQARYN V5 FINAL — C02-04.M01.02: actores, entradas, salidas y superficies de consola global auditada

**Unidad:** M01.02 de C02-04, alcance y preparación **exclusivamente documental**. **Ejecutor:** PRIMARY DEV 05 — JAVIER, TASK_ID `6adc2e55f47c81919f6374e561687076`; rama `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`.

**Fuente de autoridad:** [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN_MAESTRO / COLA / DAG_TECNICO](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). El DAG VALIDADO habilita solo preparación M01.01–05, con quince prerequisitos de C02-01/02/03 M01.01–05 LISTO. C02-04.M01.01 define [objetivo y no-regresión](c02-04-m01-01-objetivo-no-regresion-consola-global.md) previamente publicado; ninguno de esos documentos autoriza implementar la consola, modificar grants o habilitar M01.06/M02.

**Resultado de C02-04 a futuro:** separar acceso global de plataforma de permisos empresariales del ERP, con MFA/reautenticación, motivo verificable, excepción temporal explícita, auditoría crítica y pruebas de denegación. **Esta microtarea identifica contratos de interacción**, no afirma que los mecanismos futuros ya operen.

## 1. Actores y límites de autoridad

| Actor conceptual | Superficie / intención | Restricción verificable |
| --- | --- | --- |
| A01 Operador autorizado de plataforma | Consola global futura para una acción puntual | Requiere autoridad global vigente persistida, MFA/reautenticación reciente, motivo y grant de mínimo privilegio |
| A02 Auditor independiente | Consulta de evidencia de accesos y denegaciones | Puede leer solo auditoría autorizada, sin adquirir facultad de modificar tenants ni borrar historia |
| A03 Administrador de empresa A | ERP de A | No puede administrar plataforma ni empresa B por ser administrador de A |
| A04 Usuario empresarial / sucursal | Recurso comercial sujeto a membresía y ámbito | Su rol empresarial no confiere privilegio global ni cross-sucursal/cross-tenant |
| A05 Servicio de autorización backend | Validación de JWT, rol global, tenant, permiso temporal y expiración | La decisión final es server-side, nunca del navegador |
| A06 Servicio MFA/reautenticación FUTURO | Verificación adicional reciente de operación sensible | Proveedor, esquema y obligatoriedad exacta no están implementados ni seleccionados por este documento |
| A07 Proceso de auditoría crítica FUTURO | Evento append-only con actor, motivo, scope y resultado | Sin secretos ni PII innecesaria; un fallo debe negar la operación privilegiada |
| A08 Soporte DEV limitado | Lecturas GET catalogadas bajo `PlatformSupportPolicy` | No puede usar allowlist de soporte para crear/modificar/grantear acceso global |

Los actores A06/A07 son **capacidades por diseñar**, no servicios instalados. A01/A02 son roles de negocio de diseño, no nuevas asignaciones RBAC en base de datos.

## 2. Matriz de entradas / salidas propuestas (IO)

| ID | Entrada solicitada / procedencia | Salida y superficie; control obligatorio |
| --- | --- | --- |
| IO-01 | JWT de identidad autenticada | Identidad del actor obtenida server-side; `401/403` seguro si inválida |
| IO-02 | Rol empresarial, claims de `esAdministrador`, `Usuario.RolId` | **No** autoridad global; consulta del scope de plataforma vigente en persistencia |
| IO-03 | `EmpresaId` / tenant / host / `X-Empresa-Id` proporcionados por cliente | Selector solicitado sin grant; resolver membresía y permiso al tenant exacto |
| IO-04 | Operación y recurso global específico solicitado | Autorizar par `acción-recurso-tenant` restringido; negar expansión implícita |
| IO-05 | Comprobante de MFA reciente FUTURO | Aceptar sólo prueba ligada a usuario/sesión y acción, con ventana expirada fail-closed |
| IO-06 | Reautenticación reciente FUTURA | Validación adicional independiente de una cookie o sesión antigua |
| IO-07 | Motivo y referencia/ticket | Validar formato/longitud e identidad; log con minimización; motivo vacío denegado |
| IO-08 | Fecha de expiración y token/grant excepcional FUTURO | Scope y TTL definidos por servidor; sin fechas arbitrarias ni renovación automática |
| IO-09 | Solicitud de lectura global mínima | DTO filtrado/redactado, paginado, `Cache-Control: no-store` y auditoría crítica |
| IO-10 | Solicitud de mutación sensible | Comprobar permiso mutación, aislamiento A/B, idempotencia, transacción y rastro; sin escribir si falta |
| IO-11 | Revocación del acceso excepcional | Terminar permisos activos sin reabrir con JWT previamente válido |
| IO-12 | Error, denegación, replay o falta de auditoría | Error neutro, cero datos/mutaciones de B y evento de intento sin credenciales |

**Límites reales:** `PlataformaEmpresasController` ya contiene `GET /plataforma/empresas` y `POST /plataforma/empresas/borrador`, con comprobaciones de autoridad global y auditoría para operaciones concretas. No existe evidencia aquí de un flujo universal de MFA, de excepciones con TTL ni de autorización de lectura/escritura sobre cualquier recurso de otros tenants. `ContextoTenantActual` exige membresía activa y `PlatformSupportPolicy` limita asistencia DEV a GET de catálogo.

## 3. Superficies y consumidores que deben revisarse antes de implementar

| Frontera | Contrato que debe seguirse | Riesgo |
| --- | --- | --- |
| Navegador Angular / rutas futuras de consola | UI solo solicita, muestra y da accesibilidad/foco; nunca toma decisiones de permiso | Una ruta oculta no reemplaza autorización server-side |
| API `/plataforma/*` existente y futura | Actor global actual validado antes de operaciones; DTOs/errores sin datos empresariales indebidos | Confundir GET global paginado con acceso comercial irrestricto |
| Application / `IPlataformaScopeService` | Validar rol de plataforma persistido y separado de rol ERP | Escalada a través de JWT empresarial |
| Domain `UsuarioEmpresa`, `ContextoTenantActual` | Identidad, membresía activa y propiedad positiva | IDOR/BOLA, sucursal cruzada y tenant B |
| Infrastructure / MySQL/EF | Predicados explícitos, transacciones y auditoría para acceso excepcional | `IgnoreQueryFilters` amplio, cambios no auditados o PII de otra empresa |
| Auditoría y logs | Registrar quién, por qué, qué, cuándo y resultado con sanitización | Suplantación, filtración de secretos o supresión de denegaciones |
| Servicio MFA/reauth FUTURO | Unir prueba fuerte y frescura al intento específico | Replay de OTP/token, vencimiento ignorado |
| Soporte DEV / allowlist | Sólo acciones GET catalogadas por política actual | Transformar acceso restringido de soporte en llave global |

## 4. Fuentes de código realmente inspeccionadas (no pruebas live)

| Archivo Git dev | Blob SHA observado | Evidencia y límite |
| --- | --- | --- |
| `backend/src/API/Controllers/PlataformaEmpresasController.cs` | `87fbf187b7993ba0f05edff393500f24d0856833` | Autorización y auditoría en rutas concretas de plataforma; no prueba MFA |
| `backend/src/Infrastructure/Services/PlataformaScopeService.cs` | `2a71a56750805ef513c7f312fb6c6c2d53a8c314` | Rol global validado contra DB por solicitud, no desde claim de rol |
| `backend/src/Application/Security/PlatformSupportPolicy.cs` | `b103f0b0ec7b9169c060661f6a4456f8863152cf` | Allowlist DEV de lectura acotada, jamás permiso amplio de edición |
| `backend/src/Domain/Security/ContextoTenantActual.cs` | `7377d2d752985cc9baf31d08280f38ad94a6a114` | Contexto de tenant requiere membresía activa y coincidente |
| `backend/src/Domain/Entities/UsuarioEmpresa.cs` | `f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c` | Rol empresarial no sustituye scope de plataforma |
| `docs/v5/c02-04-m01-01-objetivo-no-regresion-consola-global.md` | `851f59e3ff2870f182fe8be3bf57ae606448c557` | Documento objetivo previo: casos NR-01–15 diseñados, no ejecutados |

Releer blobs en el momento de modificar código. Ninguna matriz IO representa endpoints, tablas, permisos, dispositivos MFA ni integración E2E implementados.

## 5. Pruebas observables FUTURAS y aceptación documental actual

- **POS-01:** operador global correctamente autorizado, MFA/reauth vigente, motivo y scope de consulta A válido: solo datos autorizados de A y evento de éxito.
- **POS-02:** auditor consulta denegación previamente registrada: ve actor, fecha, scope, decisión y motivo sanitizado, sin credenciales.
- **POS-03:** revocación explícita de acceso excepcional: intento posterior se rechaza aun con sesión de identidad vigente.
- **NEG-01:** admin ERP A llama endpoint global: `403`, ninguna lectura de B.
- **NEG-02:** claim de plataforma falso, tenant modificado o host de B: `403`, sin inferir autorización de cliente.
- **NEG-03:** MFA/reautenticación ausente, vieja o no ligada a acción: denegación y auditoría.
- **NEG-04:** motivo vacío, grant expirado, token replay o petición fuera de scope: denegación y cero mutaciones.
- **NEG-05:** transacción/auditoría falla: fail-closed y rollback sin registro comercial espurio.
- **NEG-06:** soporte DEV intenta POST/PUT/DELETE en consola: denegación por lista de rutas.

Estas 9 pruebas son **criterios de aceptación FUTUROS**, no ejecuciones actuales de .NET/MySQL/Angular. El artefacto M01.02 se acepta si identifica los ocho actores, doce contratos IO, superficies y sus fronteras de seguridad sin afirmar autorización global implementada.

## 6. Auditorías aplicables, dependencias y reversión

**Clasificación TIPO 1:** `requiere_dotnet=NO`, `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO`; `requiere_security=SI` documental (JWT/RBAC/tenant, MFA futura), `requiere_scope_lock=SI` por publicación en `dev`. La CI técnica de otros componentes no sustituye ni bloquea esta auditoría proporcional.

- A0: diff aditivo, estructura de matrices, referencias verificables y secreto/PII ausentes.
- A1: coherencia de 8 actores, 12 IO y 3 positivos/6 negativos futuros; no son pruebas de API ejecutadas.
- A2: denegación por defecto, segregación plataforma vs ERP, control de tenant A/B y ausencia de bypass de soporte.
- A3: compatibilidad documental con M01.01 y C02-01/02/03, quince prerequisitos LISTO; sin cambios a contratos implementados.
- A4: commit fast-forward `dev`, Scope Lock SUCCESS exact SHA, diff, readback PLAN/COLA/BITACORA.
- A5: otro TASK_ID físico Javier distinto del autor; permanecerá PENDIENTE tras LISTO_DEV.

**No alterar:** datos MySQL de producción, secretos, cuentas de terceros, grants, aplicaciones, QA/main/PROD. **Rollback:** commit inverso normal con rastro aditivo, corrección institucional sin borrar BITACORA.
