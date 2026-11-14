# SOLQARYN V5 FINAL — C13-01.M01.01: objetivo, no-alcance y no-regresión de privacidad y consentimiento

**Objeto de la unidad:** definir el marco verificable de *preparación* de privacidad, cookies, términos, consentimiento, retención y derechos de usuarios en el SaaS multitenant SOLQARYN. **Autor:** PRIMARY DEV 01 JAVIER, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Rama exclusiva:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Fuentes únicas:** [Standard V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit).

**Naturaleza:** documento M01.01 de alcance y no-regresión; **NO** es una política legal publicada ni una auditoría jurídica, declaración de conformidad normativa, banner de consentimiento activo, inventario validado de cookies, derecho de supresión implementado, cambio de base de datos o certificación de retención. Ninguna regulación o plazo se afirma aplicable sin identificación expresa de jurisdicción, categoría de datos y revisión especializada. Los escenarios siguientes son FUTUROS_NO_EJECUTADOS.

## 1. Objetivo, responsables y resultados observables futuros

La finalidad de `C13-01` es acordar una política versionada y auditable de información y opciones de privacidad por superficie de procesamiento de datos, compatible con la arquitectura multitenant, con evidencia de consentimiento cuando sea exigible, revocación válida y ciclo de vida/retención del dato bajo reglas expresamente aprobadas.

**Resultados que deberán producir fases posteriores, sin afirmar implementación:**

1. **Inventario de tratamientos y superficies:** identidades y cuentas, datos de empresa, cliente/proveedor, ventas/facturas, pagos (si se autorizan), telemetría, logs, cookies, integraciones, CDN/medios, exportaciones y backups, distinguiendo responsable de negocio, operador y destinatario de cada dato.
2. **Textos y jurisdicción:** versiones de términos y aviso de privacidad por alcance pertinente, idioma, fecha efectiva y autoridad de aprobación; revisar si existen menores, transferencias internacionales, encargados, cookies técnicas o no esenciales, y base de tratamiento. El presente documento no define asesoría ni plazos obligatorios.
3. **Consentimiento o base legítima:** categorizar qué operaciones precisan una elección afirmativa y cuáles no, según revisión legal; impedir que un flag de UX sea autorización para acceder a recursos privados.
4. **Historial y revocación:** registrar exclusivamente metadatos mínimos de una decisión verificable (actor/tenant/política/version/timestamp/resultado) sin JWT, secreto, payload PII innecesario ni datos de otra empresa; permitir revocación donde proceda con efectividad y prueba causal.
5. **Acceso/corrección/supresión/exportación:** canales autenticados, identidad y ownership verificados, tratamiento de obligaciones de retención de registros contables y evidencias de auditoría; jamás borrar registros con obligación de conservación por una regla inventada.
6. **Retención y eliminación:** matriz de clases de datos, plazo condicional aprobado, razón documentada, regla de legal hold/backup y borrado verificable, evitando prometer eliminación instantánea de backups o logs sin diseño operacional.
7. **Experiencia:** texto claro, controles accesibles, granularidad cuando aplique, teclado y estados de carga/error; no interpretar silencio como consentimiento salvo política validada. Diseño ligero y privacidad por defecto.

## 2. Fuentes de código realmente inspeccionadas y limitaciones

| Fuente exacta del árbol dev | Blob Git | Evidencia comprobable y límite |
|---|---|---|
| `backend/tests/Solqaryn.Tests/N19SerieAuditPrivacyTests.cs` | `5bd3a4b1b7a4bb90c45a27149c408386c7bf1617` | Prueba de origen verifica no incorporar número de serie privado en cierto payload de auditoría; **no** cubre consentimiento o cookies de la plataforma. |
| `backend/tests/Solqaryn.Tests/P1015BPrivilegedAuditPrivacyTests.cs` | `c3321332fa4102111f03247da6063bb99d3bec1c` | Test de auditoría privilegiada e in-memory con sentinelas de hashes/PII, declara no certificar MySQL remoto ni logging HTTP general. |
| `docs/v5/c03-02-m01-03-inventario-autorizacion-privacidad.md` | `51b5a01a4770977aadcc780145a17aaed8a3fb41` | Inventario de filtros JWT/RBAC/tenant y pruebas negativas existentes; no política de consentimiento desplegada. |
| `docs/v5/c03-02-m01-05-criterios-aceptacion-tenant-privacidad.md` | `a5f551f1bd2cde2c42e6daf9e6e488f247e36602` | Oráculos de rechazo BOLA/IDOR y exportación intertenant; son criterios de fases futuras, no cumplimiento jurídico. |

La búsqueda de nombres de archivo relacionados con cookies/consentimiento/retención en el árbol de dev **no encontró un módulo inequívoco de gestión integral de consentimiento**, pero esta búsqueda por nombres no demuestra ausencia de lógica en otros archivos o sistemas. M01.03 deberá hacer inventario completo del backend, Angular, cookies reales y proveedores autorizados antes de afirmar cobertura o carencia. No se ejecutó ningún test de las fuentes anteriores en esta entrega.

## 3. Invariantes de no-regresión (NR01–NR16)

| ID | Invariante que futuras fases deben preservar | Síntoma de incumplimiento |
|---|---|---|
| NR01 | El tenant se resuelve desde membresía activa y contexto servidor, nunca de URL, cookie, claim no validado o header arbitrario | Actor A obtiene decisión o PII de B |
| NR02 | JWT autenticado no sustituye grant RBAC ni ownership de recurso | Un usuario válido lee/revoca consentimiento de otro |
| NR03 | Todo registro de decisión tiene sujeto/ámbito/política/version/timestamp y cadena de procedencia verificable | Registro anónimo sin origen asumido como consentimiento |
| NR04 | Una política antigua no se transforma silenciosamente en aceptación de versión nueva | Consentimiento stale se interpreta como vigente |
| NR05 | Revocación que corresponda bloquea uso futuro de finalidad afectada sin quitar derechos no relacionados | Procesamiento no esencial continúa tras revocación válida |
| NR06 | Cookies y tecnologías no esenciales no se habilitan basándose sólo en un icono de UI o error de carga | Script opcional corre sin decisión suficiente |
| NR07 | Consentimiento comercial nunca concede acceso a datos empresariales o grants de módulos | Activar flag sobrepasa RBAC |
| NR08 | Denegación/rechazo no impide navegación o accesibilidad necesaria de procesos esenciales, según diseño autorizado | Dark pattern, bloqueos injustificados, controles inaccesibles |
| NR09 | Datos de auditoría minimizados, sin credenciales, tokens JWT, PII adicional ni valores de otro tenant | Eventos de consent incluyen payload privado |
| NR10 | La exportación de derechos requiere identidad/verificación y scope antes de cualquier byte | Archivo de A incorpora datos de B |
| NR11 | Solicitud de supresión no borra facturas, auditoría o retenciones obligatorias sin revisión de requisitos | Eliminación destructiva de prueba histórica |
| NR12 | Reglas de retención diferencian base de dato, archivos, cachés, logs y backups; miden estado real | Promesa de borrado total sin confirmación de backups |
| NR13 | Un error/timeout/replay no crea decisiones afirmativas ni duplicados con otra versión/empresa | Consentimiento fantasma o replay cross-tenant |
| NR14 | Información visible sobre tratamiento y opción no presenta plazos o jurisdicción no aprobados | Textos regulatorios inventados |
| NR15 | La API es autoridad de datos y decisión, Angular sólo UX, incluida navegación accesible | Browser cambia el backend sin permiso |
| NR16 | Preparación documental M01 no autoriza cobros, provider, migraciones, eventos, QA/main/PROD ni certificación legal | Completado del documento usado como permiso runtime |

## 4. Interfaces y dependencias futuras — sin código creado

**Backend/API:** la autorización de lectura, exportación y revocación deberá reutilizar guards por usuario/empresa, trazabilidad de intentos, respuesta genérica y denegación por defecto; no exponer registros de consentimiento de terceros. Si se aprueban nuevas entidades, preservar esquema tenant-key y versiones, con SQL aditivo y rollback probado en MySQL 8.4 efímero.

**Angular 22.2.1:** UX de aviso/versiones/cookies y preferencias por categorías, con contraste, etiquetas, foco, teclado y estados deshabilitados cuando no exista autoridad; no renderizar scripts no esenciales antes de la decisión necesaria. El comportamiento de cookies de terceros y proveedores debe inventariarse con evidencia real, no inferirse de rutas.

**Datos y operaciones:** sujetos A/B, cuentas empresariales, usuarios y registros comerciales, eventuales terceros y soporte; separar responsabilidad de permiso empresarial, atención de derechos, consentimiento y obligaciones históricas. Los workers/outbox/webhooks no se implementan por este alcance, incluso si se menciona un flujo futuro.

**Gobierno:** M01.02 actores/entradas/salidas, M01.03 inventario real, M01.04 riesgos/decisiones y M01.05 aceptación, M01.06 cierre de dependencias. M02/M03 requieren nuevos claims CAS, DAG y pruebas; QA/main/PROD y cambios de política pública necesitan autorización separada.

## 5. Oráculos documentales de futuras pruebas (no ejecutados)

| Caso | Tipo | Estímulo | Resultado exigido |
|---|---|---|---|
| P01 | Positivo | Usuario A habilitado acepta versión/purpose habilitado tras explicación | Evento mínimo vinculado A/versión, sin PII innecesaria |
| P02 | Positivo | Usuario A revoca categoría no esencial autorizadamente | Nueva operación rechaza finalidad revocada sin tocar B |
| P03 | Positivo | Sujeto A solicita exportación conforme a identidad y permisos | Solo A y campos aprobados, entrega trazable |
| P04 | Positivo | Usuario usa teclado y lector para revisar opciones sin fines no esenciales | Interfaz accesible sin permiso backend inferido |
| N01 | Negativo | Actor A solicita registro o solicitud de derechos de B | DENY sin revelación de existencia B |
| N02 | Negativo | Cookie no esencial intenta activarse antes del gate aplicable | Bloquear script; no registrar aceptación |
| N03 | Negativo | Silencio, error de UI o falta de respuesta se considera aceptación afirmativa | No crear consentimiento por inferencia |
| N04 | Negativo | Política vieja pasa a nueva versión sin revisión ni nueva decisión exigible | No migrar aceptación automáticamente |
| N05 | Negativo | Payload de evento incluye JWT, password, secreto o PII de B | Rechazar/sanitizar logs |
| N06 | Negativo | Revocación con actor no autenticado o sin ownership | DENY, no cambiar otro sujeto |
| N07 | Negativo | Backup no verificado se declara totalmente eliminado | No certificar supresión sin evidencia |
| N08 | Negativo | Solicitud de supresión sobre registros obligatorios se ejecuta destructivamente | NO_GO, evaluación de conservación |
| N09 | Negativo | Empresa B reutiliza key idempotente/decisión de A | Aislamiento y no replay |
| N10 | Negativo | Documento legal refiere plazo o norma no identificada y no aprobada | NO_GO a publicación legal |
| N11 | Negativo | Token revocado o rol legado intenta exportar datos personales | DENY, sin archivo |
| N12 | Negativo | M01 se usa para activar cookies/proveedor, worker, DDL o QA/PROD | NO_GO; nueva microtarea/gate |

Los cuatro positivos y doce negativos **son diseños de oráculos futuros y NO pruebas de runtime**. No se instrumentaron cookies, endpoints, consentimiento, retención, eliminación ni auditoría legal real en esta unidad.

## 6. Auditoría A0–A5 y reversión proporcional

- **A0:** examinar código/tests y contratos, artefactos de solo Markdown y CHANGELOG con sección autocontenida, sin secretos ni encabezados `##` vacíos.
- **A1:** inspección de objetivos/NR01–16 y P01–04/N01–12, sin atribuir pruebas reales.
- **A2:** revisar JWT/RBAC/tenant A/B, PII, revocación, menor privilegio y fallos cerrados; cumplimiento normativo NO CERTIFICADO.
- **A3:** compatibilidad documental con C03-02 seguridad, pruebas privacy existentes y objetivos M01–M06; runtime `NO_APLICA` causal del diff Markdown.
- **A4:** commit serial fast-forward sobre `dev` con Scope Lock y check V5 `completed/success` exact-SHA, readback PLAN/COLA/BITACORA; nunca contar builds SKIPPED/CANCELLED/PENDING como PASS requerido.
- **A5:** revisión independiente por otra Task Javier, distinta de `6adbb9fff1548191a1bd5b30d9e9984a`. LISTO_DEV no permite legal public, QA/main/PROD.
- **Rollback:** `git revert` normal del commit documental con evento compensatorio aditivo, sin tocar BITACORA anterior; cualquier proceso real de borrado requiere diseño y pruebas propias.

