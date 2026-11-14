# SOLQARYN V5 FINAL — C14-02.M01.02: actores, interfaces y fronteras de confianza de aceptación de seguridad

**Unidad:** C14-02.M01.02, identificar entradas, salidas, actores y superficies afectadas. **Fecha:** 2026-11-14. **Autor:** PRIMARY DEV 01 JAVIER, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Repo:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Tipo:** contrato preparatorio documental; no modifica ni certifica código, permisos, MySQL, servicios ni entorno.

**Fuentes autorizadas:** [Standard V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), [C14-02.M01.01: alcance](./c14-02-m01-01-alcance-no-regresion-aceptacion-seguridad.md), `.agents/skills/solqaryn-project-governance/references/architecture.md`, `AGENTS.md` y `docs/PROJECT_SCOPE_LOCK.md`. PLAN fila 2055 PENDIENTE sin owner, COLA G/O=SI y DAG fila 2016 F=VALIDADO/G=SI fueron verificados antes del claim. El gate no autoriza ejecutar QA/main/PROD ni liberar etapas M02–M06.

## 1. Intención y límite de las afirmaciones

Este inventario es una **matriz de prueba propuesta** para auditar BOLA/IDOR, spoofing de host o tenant, archivos, jobs, cachés, backups y sesiones. Los actores enumerados son perfiles de prueba y fronteras que deben cotejarse en M01.03 contra el código real; su enumeración no asegura que existan roles o servicios con esos nombres en producción. Las interfaces incluyen entradas potenciales y futuras; donde aún no exista una implementación no se inventarán endpoints, rutas, comandos o permisos.

Se separan **identidad autenticada**, **rol efectivo**, **tenant/empresa**, **sucursal/ownership** y **autorización por recurso**. Cualquier ID o header enviado desde cliente es una solicitud, no autoridad. Los permisos reales pertenecen a la API/backend, nunca a Angular. Los resultados de los casos siguientes son `ESPERADOS_NO_EJECUTADOS`.

## 2. Matriz de actores y amenazas A01–A12

| Actor hipotético para aceptación | Entrada legítima a verificar | Desvío negativo a impedir |
| --- | --- | --- |
| A01 Visitante anónimo | Consultar únicamente superficie pública autorizada | Enumerar recursos privados mediante ID |
| A02 Usuario autenticado tenant A | Leer recursos propios permitidos | Leer o editar recursos de tenant B |
| A03 Usuario autenticado tenant B | Operar su empresa bajo permisos efectivos | Ver estado/cache/datos de tenant A |
| A04 Personal de sucursal | Acceder a sucursal de membresía vigente | Cambiar sucursal en body o URL para escalar acceso |
| A05 Administrador de empresa | Administrar recursos de su ámbito | Aplicar permisos globales no asignados |
| A06 Rol de solo lectura | Consultar lo estrictamente permitido | Mutar recursos mediante POST/PATCH/DELETE |
| A07 Usuario con rol revocado | Recibir rechazo de acciones retiradas | Reusar JWT/sesión con rol obsoleto |
| A08 Operador autorizado de archivos | Obtener archivo de dueño legítimo | Descargar objeto ajeno o con URL caducada |
| A09 Emisor de trabajo diferido | Encolar únicamente trabajo autorizado | Inyectar otro tenant/reintentar efecto duplicado |
| A10 Cuenta técnica de integración | Operar con credenciales y scopes restringidos | Actuar como usuario privilegiado por default |
| A11 Auditor de seguridad autorizado | Ver evidencia mínima y redactada | Extraer JWT, contraseñas, PII o secretos |
| A12 Sujeto adversarial | Sin permiso adicional | Manipular host, path, headers, claves o cache para BOLA/IDOR |

No se concede a ningún actor una facultad real por estar aquí descrita. La existencia y granularidad de roles, auditoría y cuentas técnicas deben leerse del repositorio y política de operación antes de afirmar cobertura.

## 3. Entradas, salidas e interfaces I01–I14

| ID | Entrada/salida de confianza | Validación observable futura |
| --- | --- | --- |
| I01 | Angular ruta/guard → petición HTTP | Guard client-side no sustituye autorización API |
| I02 | HTTP path/query/body → controller | Identificadores ajenos se rechazan sin fuga |
| I03 | JWT Bearer → identidad efectiva | Emisor, firma, tiempo y revocación según política |
| I04 | Roles/membresías → permiso por recurso | Revocación y cambio de empresa no heredan rol |
| I05 | Host/origin/X-Forwarded-* → resolución pública | No confiar en host no aprobado para tenant o URL |
| I06 | API/Application → Infrastructure/repositorio/EF | Filtro por propiedad y tenant antes de I/O sensible |
| I07 | API → respuesta/serialización/errores | Nunca devolver PII, secrets, stack ni objeto ajeno |
| I08 | API → Cloudinary/documento/URL compartible | TTL y pertenencia verificables, cuando apliquen |
| I09 | API → QuestPDF/exportación | PDF respeta ámbito, no mezcla datos de empresas |
| I10 | API → SMTP/servicio externo | No filtra secretos y maneja fallo de forma cerrada |
| I11 | Productor → job/payload/replay (si existe) | Identidad/tenant verificados en ejecución y reintento |
| I12 | Lector → caché compartida (si existe) | Keys, TTL e invalidación separan tenant y permisos |
| I13 | Backup/restore (si existen flujos autorizados) | Restauración conserva límites/PII e integridad |
| I14 | Logs/trazas → auditoría | Correlación útil sin JWT, contraseña ni URL sensible |

`(si existe)` significa **superficie condicional sin implementación constatada aquí**. No se han creado workers, colas, cachés, webhooks, dispatcher, backups ni integraciones nuevas.

## 4. Fronteras de confianza F01–F12

| Frontera | Activo protegido | Ataque/negativo exigido |
| --- | --- | --- |
| F01 Browser ↔ API | Privilegios del servidor | Forzar botón oculto vía HTTP directo |
| F02 JWT ↔ usuario/rol | Identidad efectiva | Token expirado, adulterado, revocado |
| F03 Tenant A ↔ tenant B | Aislamiento de datos | IDOR, suplantación de host o contexto |
| F04 Sucursal ↔ empresa | Pertenencia de registros | Sucursal A2 accede a A1 sin permiso |
| F05 DTO ↔ dominio | Ownership e invariantes | Body reasigna dueño o tenant |
| F06 Aplicación ↔ DB | Consulta/commit transaccional | Filtro tenant omitido por query indirecta |
| F07 API ↔ archivos | Objetos y URLs firmadas | Ruta o token ajeno, TTL caducado |
| F08 API ↔ cola/job | Comandos diferidos | Payload replay sin autorización o idempotencia |
| F09 Consulta ↔ caché | Datos en memoria | Cache key omite tenant/permiso |
| F10 Backup ↔ restore | Restauración de datos | Restore cruza empresas o divulga secretos |
| F11 App ↔ proveedor externo | Credenciales y datos | Timeout/reintento expone permisos o PII |
| F12 Logs ↔ observador | Auditoría y privacidad | Errores imprimen token, PII o encabezados secretos |

## 5. Matriz de pruebas positivas y negativas a programar después

| Caso | Positivo esperado, no ejecutado | Negativo esperado, no ejecutado |
| --- | --- | --- |
| PN01 | Tenant A accede a recurso A autorizado | Tenant A con ID de B recibe rechazo |
| PN02 | Rol autorizado actualiza su recurso | Rol de lectura o revocado no muta |
| PN03 | JWT válido de ámbito correcto pasa | JWT caducado/forjado falla |
| PN04 | Host válido no altera tenant autenticado | Host/forwarded spoof no deriva propietario |
| PN05 | Archivo propio activo se sirve | Archivo ajeno o URL caducada no se sirve |
| PN06 | Job propio produce efecto único | Replay/cambio de contexto se rechaza |
| PN07 | Cache de A sirve solo a A autorizado | Cambio de tenant o rol invalida resultados |
| PN08 | Restauración en fixture mantiene propiedad | Restore cruzado o PII fuera de scope falla |

Los ocho pares son criterios, no pruebas ejecutadas. Cuando se implementen, deben registrar SHA y job causal; una conclusión `queued`, `in_progress`, `skipped`, `cancelled` o `failure` no es PASS obligatorio.

## 6. Plan de revisión, exclusiones y rollback

M01.03 deberá cotejar cada actor e interfaz con controladores, guards, políticas, servicios, acceso a datos y pruebas reales, dejando `NO_ENCONTRADO` o `NO_APLICA` justificado. M01.04 elaborará riesgos/supuestos; M01.05 fijará criterios mensurables; M01.06 tendrá su propio gate. M02–M06 no se liberan por este inventario. El requisito final de cero P0/P1 sin mitigación aprobada sigue **NO_VERIFICADO**.

**Fuera de alcance:** alterar JWT, RBAC, ABAC, guards, controladores, persistencia MySQL, migrations, cache, workers, Cloudinary, R2, QA, producción, cuentas, secrets, endpoints y CORS, así como ejecutar pentest o pruebas en datos reales. No hay cambio de arquitectura, ni worker/eventos implementados.

**A0–A4 proporcional al documento:** A0 comprobar integridad de tablas A/I/F y ausencia de secretos; A1 leer ocho pares P/N definidos y sus límites NO_EJECUTADOS; A2 revisar tenant A/B y fail-closed conceptual sin atribuir prueba runtime; A3 trazar origen en V5 M01.02, M01.01 y referencia de arquitectura; A4 comprobar diff de solo dos Markdown, commit fast-forward, Scope Lock y verificador V5 `completed/success` del SHA exacto, PLAN/COLA/BITACORA readback. Otros checks de código sin cambios se marcarán `NO_APLICA` causal, no como PASS. A5 requiere una Task Javier distinta de la autora.

**Rollback:** revert normal del commit exacto en `dev` previa revisión de HEAD; sin force-push ni borrado de BITACORA histórica. La tabla de documentación no concede autorización de QA/main/PROD.
