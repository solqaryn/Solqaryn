# ADR N6.7.B — Configuración por empresa: contrato funcional y técnico

Estado: Accepted for implementation sequence N6.7.C–N6.7.G  
Parent VAEP: N6.7.B — DESIGN_CONTRACT  
Autoridad: `docs/VAEP_AUTHORITY.md`

## 1. Decisión

Solqaryn tendrá una configuración explícita y aislada por empresa. La autoridad de tenant se resolverá en backend a partir de la identidad autenticada y de la membresía autorizada; ningún `empresaId` recibido desde UI, query string, ruta o body es autoridad suficiente por sí solo.

La configuración se separa conceptualmente en dos grupos:

1. **Identidad legal/comercial de Empresa**: nombre, RTN, dirección y logo. Si estos campos ya existen en `Empresa`, continúan siendo su fuente de verdad y N6.7 no introduce una segunda copia autoritativa.
2. **Configuración operativa por empresa (`ConfigEmpresa`)**: moneda, zona horaria, impuestos, correlativos/numeración documental, parámetros de emisión, configuración de correo y plantillas de correo.

N6.7.C decidirá/materializará la persistencia exacta respetando este límite; este ADR no adelanta una migración ni obliga a duplicar columnas existentes.

## 2. Alcance del contrato

Cada empresa debe poder administrar, como mínimo:

- `nombre`: razón/nombre visible de la empresa cuando el modelo actual lo permita.
- `rtn`: identificador tributario normalizado.
- `direccion`: dirección fiscal/comercial.
- `logo`: referencia segura al activo/logo; no contenido ejecutable arbitrario.
- `moneda`: código ISO 4217 de 3 letras, normalizado a mayúsculas.
- `zonaHoraria`: identificador IANA válido.
- `impuestos`: política/configuración tributaria de la empresa; porcentajes y reglas deben validarse server-side.
- `correlativos`: secuencias por tipo de documento y, cuando aplique, establecimiento/punto de emisión.
- `emision`: parámetros funcionales necesarios para emitir documentos; nunca contiene secretos retornables al cliente.
- `correo`: remitente, display name y estado de configuración; credenciales/secretos son write-only o referencias a secret storage y jamás se devuelven en respuestas.
- `plantillasCorreo`: asunto/cuerpo/estado por tipo de plantilla y empresa, con límites de tamaño y variables permitidas explícitas.

## 3. Límites de dominio y persistencia

### 3.1 Empresa

`Empresa` conserva la identidad legal/comercial que ya sea autoritativa en el modelo actual. Si `nombre`, `rtn`, `direccion` o `logo` ya existen ahí, la implementación debe actualizar esos campos mediante el mismo caso de uso transaccional de configuración, sin duplicarlos en `ConfigEmpresa`.

### 3.2 ConfigEmpresa

`ConfigEmpresa` representa valores operativos que pertenecen a una única empresa. Debe existir como máximo una configuración vigente por `EmpresaId` para los valores escalares. Las colecciones de correlativos y plantillas pueden usar tablas hijas, pero todas deben incluir la clave de tenant y constraints que impidan colisiones cross-tenant.

Restricciones mínimas para N6.7.C:

- FK obligatoria a `Empresa` con política de borrado coherente con la estrategia actual del proyecto.
- unicidad de `ConfigEmpresa.EmpresaId` si se implementa entidad 1:1.
- RTN normalizado único según la regla de negocio vigente; si el sistema permite legalmente duplicados por contexto, N6.7.C debe documentar la excepción antes de crear constraint.
- correlativos únicos por `(EmpresaId, TipoDocumento[, Establecimiento, PuntoEmision])`.
- plantillas únicas por `(EmpresaId, TipoPlantilla)` salvo que el modelo soporte versionado explícito.
- índices siempre tenant-first para búsquedas que puedan cruzar empresas.

No se autoriza una tabla/configuración global que pueda ser compartida accidentalmente entre tenants.

## 4. Contrato HTTP

El backend expone el recurso de configuración de la **empresa efectiva del contexto autenticado**, no de una empresa confiada desde el cliente.

Contrato preferido:

- `GET /api/configuracion/empresa` — obtiene la configuración efectiva del tenant actual.
- `PUT /api/configuracion/empresa` — reemplaza/actualiza el agregado editable del tenant actual de forma idempotente.

Si el routing existente exige `empresaId`, el backend debe tratarlo sólo como un identificador solicitado y comprobar que coincide con una empresa autorizada para el usuario antes de leer o mutar.

### 4.1 DTO de lectura

La respuesta debe contener como mínimo:

```text
empresaId
nombre
rtn
direccion
logoUrl
moneda
zonaHoraria
impuestos
correlativos[]
emision
correo: { remitente, nombreRemitente, configurado }
plantillasCorreo[]
version
```

`version` es un token de concurrencia (rowversion/etag o equivalente) cuando el mecanismo de persistencia disponible lo soporte. Nunca se incluye password SMTP, API key, token ni secreto equivalente.

### 4.2 DTO de escritura

Acepta sólo propiedades editables. Los secretos de correo, si se habilita su escritura en N6.7.D, deben tener semántica explícita:

- ausencia => conservar secreto existente;
- valor permitido => reemplazar mediante almacenamiento seguro;
- una operación explícita de limpieza => eliminar/deshabilitar el secreto;
- nunca responder con el secreto persistido.

No se permite mass-assignment de `EmpresaId`, auditoría, propietario, permisos ni campos internos.

## 5. Validación y errores

Toda validación es server-side aunque exista réplica UX.

- nombre: requerido cuando sea editable, trim, longitud acotada.
- RTN: trim + normalización determinística; formato hondureño conforme a la regla ya adoptada por el dominio antes de persistir; conflicto de unicidad => `409`.
- dirección: longitud acotada; texto, no HTML ejecutable.
- logo: URL/referencia generada o permitida por la política de archivos; bloquear esquemas inseguros.
- moneda: código soportado de 3 letras; inválido => `400`.
- zona horaria: IANA soportada; inválida => `400`.
- impuestos: rangos válidos y sin porcentajes negativos/NaN; inválido => `400`.
- correlativos: enteros/rangos válidos, nunca retroceder por una actualización normal que pueda causar reutilización documental; conflicto => `409`.
- email: direcciones válidas y límites de longitud; secretos nunca en logs/respuestas.
- plantillas: tipo conocido, asunto/cuerpo con límites; variables sólo desde allow-list; contenido renderizado debe aplicar escaping acorde al canal.
- token de concurrencia obsoleto, si aplica => `409` o `412`, de forma consistente en toda la API.

Semántica mínima:

- `400` validación/formato.
- `401` no autenticado.
- `403` autenticado sin permiso o intento cross-tenant.
- `404` sólo cuando no revele existencia de recursos ajenos; en consultas cross-tenant se prefiere política uniforme que evite enumeración.
- `409` conflicto de unicidad/correlativo/concurrencia de negocio.

Los errores siguen el envelope estándar actual del backend; N6.7.D no debe introducir un formato paralelo.

## 6. Autorización y aislamiento multiempresa

La autorización es **backend-authoritative**.

Para leer configuración el usuario debe tener membresía activa en la empresa efectiva y el permiso de lectura correspondiente. Para modificarla debe poseer el permiso administrativo definido por RBAC para configuración de empresa.

Reglas invariantes:

1. no aceptar una selección local del frontend como prueba de pertenencia;
2. no leer, mutar, enumerar ni inferir configuración de otra empresa;
3. toda query de configuración/correlativos/plantillas incluye el tenant autorizado;
4. cambios de empresa en sesión invalidan/revalidan el contexto antes de reutilizar datos cacheados;
5. auditoría registra empresa, actor, operación, resultado y campos funcionales cambiados, sin secretos;
6. logs, telemetry y errores no contienen credenciales de correo ni cuerpos sensibles innecesarios.

N6.7.F debe probar explícitamente IDOR/cross-tenant y secreto-redaction.

## 7. Compatibilidad hacia atrás

- Los consumidores existentes de `Empresa` siguen leyendo sus campos actuales durante N6.7.C/D; no se elimina ni renombra un campo público existente dentro de esta fase sin migración/compatibilidad explícita.
- Si el nuevo `ConfigEmpresa` aún no existe para una empresa histórica, el GET debe producir defaults determinísticos y seguros o crear el registro mediante una estrategia idempotente definida en N6.7.C; no debe fallar con null-reference ni adoptar valores de otra empresa.
- Los valores por defecto no pueden habilitar envío de correo, emisión fiscal o impuestos de forma implícita.
- La transición de correlativos existentes debe conservar continuidad documental y no reiniciar secuencias.

## 8. UX / Frontend para N6.7.E

La superficie vive bajo navegación administrativa **Configuración > Empresa** y sólo se muestra si el usuario tiene permisos suficientes; ocultar controles no sustituye la autorización backend.

UX mínima:

- carga del tenant efectivo confirmada por backend;
- secciones: Datos de empresa, Localización/moneda, Impuestos, Numeración/emisión, Correo, Plantillas;
- estados loading/error/vacío y mensajes de validación por campo;
- confirmación para cambios de alto impacto (por ejemplo, correlativos o limpieza de correo);
- indicador `correo.configurado` sin revelar secretos;
- manejo de conflicto de concurrencia con recarga/reintento consciente, no sobrescritura silenciosa;
- accesibilidad básica, teclado y layout responsive.

## 9. Contrato de pruebas

N6.7.G debe incluir, como mínimo, pruebas automatizadas para:

### Autorización / tenancy
- lectura same-tenant autorizada => success;
- actualización same-tenant autorizada => success;
- usuario sin permiso => denied;
- `empresaId`/contexto de otra empresa => denied sin fuga de datos;
- usuario sin membresía activa => denied.

### Validación / contrato
- RTN normalizado y conflicto de unicidad;
- moneda y timezone inválidos;
- impuestos fuera de rango;
- correlativo inválido o retroceso prohibido;
- plantilla/tamaño/variable no permitida;
- error envelope y códigos HTTP acordados.

### Seguridad
- GET/PUT nunca retornan secreto de correo;
- logs/auditoría no contienen secreto;
- payload con campos internos/EmpresaId no puede hacer mass-assignment;
- logo/template no habilita contenido o esquema inseguro.

### Compatibilidad y concurrencia
- empresa histórica sin configuración obtiene defaults seguros;
- datos existentes de Empresa conservan compatibilidad;
- correlativos existentes no se reinician;
- actualización con versión obsoleta produce conflicto cuando concurrency token esté habilitado.

## 10. Gates y evidencia de cierre

N6.7.B se considera materialmente completo cuando este contrato ha sido REVIEW_FIRST contra PLAN_MAESTRO/COLA y no quedan P0/P1 de diseño. El cierre LISTO_REAL requiere además los gates causales definidos por VAEP sobre exact-head: seguridad/telemetría/build/test y one-shot cuando exista para este scope. La ausencia legítima de un one-shot específico puede registrarse como deuda de observabilidad para la fase de soporte posterior únicamente si la autoridad vigente lo permite; nunca sustituye un gate causal que sí exista.

Las fases siguientes deben citar este ADR como contrato y registrar cualquier desviación mediante una decisión explícita; no pueden cambiar silenciosamente los límites de tenant, secretos, correlativos o compatibilidad.

## 11. REVIEW_FIRST checklist para N6.7.B

- [x] cubre nombre, RTN, dirección, logo, moneda y timezone.
- [x] cubre impuestos, correlativos/numeración y emisión.
- [x] cubre correo y plantillas por empresa con secretos no retornables.
- [x] define límites Empresa / ConfigEmpresa y unicidad tenant-aware.
- [x] define DTO/endpoints, validación y semántica de errores.
- [x] define permisos y aislamiento multiempresa backend-authoritative.
- [x] preserva backward compatibility y continuidad de correlativos.
- [x] define superficie frontend administrativa sin confundir visibilidad con autorización.
- [x] define pruebas contractuales/seguridad/tenancy/concurrencia.
- [x] no adelanta implementación DB/API/UI de N6.7.C–G.

P0 de diseño identificados al aceptar este ADR: **0**.  
P1 de diseño identificados al aceptar este ADR: **0**.
