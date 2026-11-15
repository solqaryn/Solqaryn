# SOLQARYN V5 — C14-02.M03.01: reglas de dominio reales para aceptación de seguridad

**Microtarea:** `C14-02.M03.01`. **Autor:** PRIMARY DEV 06 JAVIER_CHATGPT (`6af16bbad26881918c283abe06e1f32a`). **Ámbito:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT. **Naturaleza:** código .NET 10 de dominio puro con pruebas xUnit reales; no es pentest ni aprobación de QA/PROD.

## Contrato y alcance de la implementación
- Se agrega `backend/src/Domain/AutomationV5/V5SecurityAcceptanceDomainRules.cs` como preflight **fail-closed** y `backend/tests/Solqaryn.Tests/Domain/V5SecurityAcceptanceDomainRulesTests.cs` con pruebas positivas/negativas.
- Contrato M02.01–06 previamente LISTO y revisado A5. PLAN fila 2066, COLA G/O SI, DAG F=VALIDADO/G=SI. CLAIM mediante transición compare-and-replace de estado y readback del owner Task DEV06.
- `VerifiedAuthority` representa hechos que un adaptador confiable deberá resolver desde JWT, permisos, membresía vigente y tenant de servidor; `PersistedTarget` representa propietario leído desde persistencia, sucursal, host y revocación; `Attempt` representa superficie y condiciones de riesgo. Un llamador no confiable no convierte esos flags en autenticación real.
- Bloqueos explícitos: JWT/membresía/permiso ausentes, role snapshot obsoleto, evidencia SHA distinta, BOLA/IDOR tenant A/B, tenant override de cliente, bypass de filtros EF, enumeración cruzada, propietario/sucursal inconsistente, host no verificado, media expirado, job sin grant o idempotencia y backup/restore sin autorización segregada.
- El único veredicto no bloqueante es `READY_FOR_TRUSTED_SECURITY_INTEGRATION`: **nunca** entrega recursos, modifica autorización, ejecuta API/EF/MySQL, activa worker, ni certifica que P0/P1=0; A1–A3 de integración real siguen pendientes de sus IDs M03 sucesores.

## Validaciones y límites de evidencia
- **A0:** revisión estática del Domain puro; no importa Infrastructure ni permite acceso.
- **A1:** prueba xUnit de contexto legítimo y contrapruebas de JWT, revocación, BOLA, IDOR, spoofing, sucursal, archivos, trabajos, evidencia SHA y RESTORE; CI de backend requerida en el SHA de publicación.
- **A2:** prueba xUnit de aislamiento tenant A/B y default-deny; control multiempresa GitHub Actions requerido. No se afirma cobertura real HTTP/EF/MySQL ni explotación de vulnerabilidades.
- **A3:** compilación y test .NET 10 bajo GitHub Actions; el helper no se conecta aún a controlador ni service. La integración futura deberá verificar servidor y BOLA con fixtures efímeros, permisos y tenancy de confianza.
- **A4:** publicación fast-forward/CAS de `dev`, diff de Domain+tests+documento+CHANGELOG, Scope Lock y V5 exact-SHA `completed/success`, backend/security causal `completed/success` y readback PLAN/COLA/DAG/BITACORA antes de LISTO_DEV.
- **A5:** revisión independiente por otra Task Javier, nunca autoría propia. Se mantiene pendiente aun si A0–A4 están verdes.

## Rollback y exclusiones
- Rollback mediante commit `git revert` normal y revalidación de CI sobre nuevo SHA, sin force-push ni alterar eventos BITACORA.
- Sin modificaciones JWT/RBAC productivo, migraciones, registros reales, Cloudflare/Hostinger, jobs, eventos, webhook, secrets, QA/main/PROD ni autorizaciones de despliegue. Nada en este Domain sustituye el gate de pruebas de aceptación de seguridad de C14-02 M03–M06.
