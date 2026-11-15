# SOLQARYN V5 FINAL — C15-02.M02.01: entidades, ownership y reglas de invariantes de arquitectura/migración

**Fecha de alcance:** 2026-11-14, America/Tegucigalpa. **Autor:** PRIMARY DEV 04 JAVIER, TASK_ID 6aeacb7820ac8191b7d768a6f33dfa09. **Fuente:** solqaryn/Solqaryn en dev; PROJECT_SCOPE_LOCK=STRICT.

## 1. Autoridad, predecesor y alcance

Contrato documental M02.01 de auditoría de arquitectura/migración conforme al único STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA) y al Sheet institucional (PLAN_MAESTRO, COLA, DAG_TECNICO y BITACORA). M01.01–06 están LISTO con A5 independiente; específicamente el M01.06 mantiene BL01–BL16, RB01–RB08 y E01–E08. El gate de esta fila DAG F=VALIDADO/G=SI no es permiso de despliegue.

Alcance: documentar exclusivamente entidades **lógicas** (no EF ni tablas físicas), ownership, relaciones, invariantes y pruebas futuras. No añadir código, endpoints, migraciones, secretos, credenciales, webhooks, workers, servicios o infraestructura real. Sin modificación de DNS, Hostinger, Cloudflare, R2, MySQL real, Vercel/Render/Aiven/Cloudinary ni QA/main/PROD. El inventario efectivo de recursos externos permanece NO_VERIFICADO.

Base tecnológica comprobable: Angular 22.2.1; API ASP.NET Core 10; Domain, Application, Infrastructure, API; MySQL con EF Core 10 y proveedor Oracle declarado; JWT, BCrypt, RBAC, auditoría, CORS y rate limiting. Los recursos Hostinger/Cloudflare/R2 constituyen objetivo futuro, no estado operativo. La UI no concede autorización de tenant.

## 2. Entidades lógicas y ownership (ENT01–ENT16)

| ID | Entidad lógica | Propiedad y relación esperadas | Evidencia futura (no ejecutada) |
| --- | --- | --- | --- |
| ENT01 | TenantAutorizado | EmpresaId autenticado y membresía comprobada | Decisión de pertenencia A/B |
| ENT02 | IdentidadOperadora | UserId y rol vigente en ENT01 | JWT y revocación |
| ENT03 | RecursoProveedor | Proveedor, resourceId y owner acreditado | Inventario versionado |
| ENT04 | LimiteServicio | ENT03, interfaz y permisos de acceso | Fronteras API/worker/BD |
| ENT05 | ArtefactoDespliegue | SHA y digest inmutables por ambiente | CI exact-SHA |
| ENT06 | AdaptadorProveedor | Contrato versionado vinculado a ENT03 | Prueba de compatibilidad |
| ENT07 | ParticiónDatos | ENT01 y ownership padre/hijo | MySQL efímero A/B |
| ENT08 | ManifiestoRespaldo | ENT03/ENT07, checksum, UTC y custodia | Backup íntegro y retención |
| ENT09 | EnsayoRestauración | ENT08 y destino efímero autorizado | Restore y comparación observada |
| ENT10 | InventarioObjeto | ENT01, proveedor, hash, TTL y clave opaca | Conteo real y URL firmada |
| ENT11 | VinculaciónDominio | host normalizado, ENT01, permiso de binding | Host, DNS y certificado |
| ENT12 | MaterialTLS | ENT11, referencia a certificado no privado | Cadena y expiración |
| ENT13 | ReferenciaSecreto | Identificador opaco y política de rotación | Revisión sin valor secreto |
| ENT14 | PlanCorteReversible | Dependencias y autorización diferenciada | Ensayo GO/NO_GO |
| ENT15 | PuntoRetorno | ENT14, snapshot y plan reversión | Restore y resultado |
| ENT16 | EvidenciaAuditada | SHA, autor, revisor distinto, evento UTC | PLAN, BITACORA y CI |

ENT01 posee muchas particiones ENT07, objetos ENT10 y vinculaciones ENT11, pero cada objeto privado tiene un único propietario verificado. ENT02 sólo referencia ENT01 mediante autorización vigente. ENT03 tiene fronteras ENT04 y adaptadores ENT06 versionados; nunca se asume activación por la existencia del contrato. ENT08 describe el origen de los datos; ENT09 debe demostrar un restore independiente. ENT11 puede referenciar ENT12 sólo si el binding está autorizado; ningún host o slug enviado por navegador adjudica empresa. ENT14 exige checkpoint ENT15 y evidencia auditada ENT16 con revisor distinto. Una lista vacía no demuestra inventario cero.

## 3. Invariantes fail-closed (INV01–INV16)

| ID | Regla futura y criterio verificable | Negativo que debe bloquearse |
| --- | --- | --- |
| INV01 | Tenant procede de autenticación backend | EmpresaId/slug del navegador manda |
| INV02 | RBAC y membresía están vigentes | Permiso revocado o usuario sin membresía |
| INV03 | Dato sin tenant inequívoco se pone en cuarentena | NULL heredado se adjudica por inferencia |
| INV04 | Padre/hijo comparten owner tenant | Hijo de A enlazado al padre B |
| INV05 | Proveedor existe en inventario autorizado | Proveedor futuro asumido como operativo |
| INV06 | Secreto sólo por referencia autorizada | Credencial se escribe en Git o log |
| INV07 | Artefacto y pruebas citan SHA exacto | Resultado CI de SHA no equivalente |
| INV08 | CI requerida completed/success | Cancelled/skipped/failed/in_progress como PASS |
| INV09 | Backup reporta integridad y procedencia | Snapshot sin checksum |
| INV10 | Restore se demuestra en destino descartable | Backup se confunde con restore |
| INV11 | Objetos se enumeran con dueño real | Se supone cero sin conteo |
| INV12 | URL de archivo lleva owner/TTL/firma | Link caducado o tenant B |
| INV13 | Binding de host/TLS autorizado | Host arbitrario selecciona tenant |
| INV14 | Job garantiza idempotencia por key+tenant | Reintento duplica o cruza datos |
| INV15 | Cutover exige autorización + rollback probado | Documento LISTO se considera despliegue |
| INV16 | A5 independiente con evento único | Autor se autoaprueba o duplica EVENT_ID |

Estos criterios no son código implantado ni pruebas de explotación. Una fuente ausente se representa NO_ACREDITADO, nunca PASS. Mantener los bloqueos BL01–BL16 de M01.06 hasta resultados auténticos y autorizaciones diferenciadas.

## 4. Pruebas positivas y negativas futuras (P01–P08 / N01–N08)

| Positivo | Resultado esperado | Negativo | Resultado bloqueante |
| --- | --- | --- | --- |
| P01 | tenant A con membresía sólo ve A | N01 | tenant B o rol revocado rechazado |
| P02 | FK de un mismo tenant aceptada | N02 | FK A/B cruzada rechazada |
| P03 | inventario real coincide con contrato | N03 | recurso no acreditado no se activa |
| P04 | restore efímero iguala checksum | N04 | checksum distinto bloquea corte |
| P05 | objeto propio con URL vigente | N05 | objeto ajeno o expirado se rechaza |
| P06 | host válido con TLS correcto | N06 | host falso o TLS inválido bloquea |
| P07 | CI completada SUCCESS sobre SHA | N07 | CANCELLED o SHA distinto no aprueba |
| P08 | A5 de Task Javier distinta | N08 | autoaprobación y duplicado bloqueados |

Los 16 casos son FUTUROS_NO_EJECUTADOS y sólo fijan oráculos de QA técnica futura. No constituyen una prueba MySQL, HTTP, browser, Playwright, pentest, rollback real o medición de infraestructura.

## 5. Modelo de evidencias, puertas, impacto y rollback

Para cada prueba futura debe existir owner, fuente y SHA, entorno autorizado, fecha UTC, valor esperado, observado real, prueba negativa, evidencia redactada, responsable, posibilidad de rollback y revisión independiente. ENT16 referencia eventos BITACORA idempotentes; no se reescribe historia ni se permiten aprobaciones A5 del propio autor. El siguiente contrato M02.02 no se sustituye por este documento, y el M02.06 terminal sigue sujeto a sus predecesores propios; cada sucesora necesita nueva evaluación DAG/PLAN/COLA.

**A0 documental:** contrastar STANDARD, M01.06, ARCHITECTURE, HEAD, PLAN/COLA/DAG. **A1 documental:** verificar integridad ENT01–16, INV01–16 y casos P/N definidos, sin atribuirles ejecución. **A2:** no cambia código JWT/RBAC/tenant, datos ni secretos. **A3:** compatibilidad conceptual con topología y adaptadores existentes. **A4:** diff restringido a documento y CHANGELOG, commit fast-forward y checks Scope Lock/procedencia V5 completed/success sobre SHA exacto, más readback institucional. **A5:** exclusivamente otra Task Javier distinta de autor.

Reversión autorizada: git revert normal de los cambios documentales, acompañado de evento compensatorio append-only. Prohibidos force-push, reset, cambio manual de fórmula DAG/G/O o modificaciones de producción.
