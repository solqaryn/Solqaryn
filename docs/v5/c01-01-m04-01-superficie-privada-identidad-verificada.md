# SOLQARYN V5 — C01-01.M04.01: superficie privada de contexto de identidad

**Ámbito:** solqaryn/Solqaryn dev; PROJECT_SCOPE_LOCK=STRICT. **Autor:** PRIMARY DEV 03 JAVIER / TASK_ID 6adc2e4bd8a48191ac7d8641ed95a1a3. **Fuente:** STANDARD V5 FINAL, PLAN_MAESTRO fila 164.

## Entrega real
Nueva superficie Angular read-only incrustada en la ruta existente /usuarios. La ruta tiene authGuard y permisoGuard (módulo Usuarios, acción Ver). No se crea otra ruta, permiso, API, JWT, rol, migración ni capacidad. Usa únicamente TenantContextService.contextoVerificado(), nunca empresaSolicitadaId, URL, localStorage o header como autoridad. Al cambiar de empresa A a B, el signal revocado oculta A antes de presentar la verificación B. Angular interpola rolNombre de forma escapada, sin innerHTML. No se realiza I/O.

## Comprobaciones
- A0: diff limitado a nuevo componente, HTML, SCSS, Jasmine, dos archivos de Usuarios, este documento y CHANGELOG. Sin secretos ni efectos colaterales.
- A1: tres pruebas Angular comprueban contexto ausente, A→sin contexto→B y que HTML de rol no se interpreta. Son pruebas a ejecutar en GitHub Actions, nunca PASS anticipado.
- A2: se conservan authGuard y permisoGuard; el backend sigue siendo autoridad. No son pruebas HTTP de seguridad ni aislamiento de base de datos.
- A3: se integra con servicio de tenant existente y la ruta de usuarios, sin cambios Domain, Application, API, EF o MySQL.
- A4: commit fast-forward/CAS en dev, Scope Lock/V5, Angular build y tests causales completed/success exact SHA, PLAN/COLA/BITACORA readback. Backend/MySQL sin cambios se clasifica NO_APLICA.
- A5: revisión independiente posterior por otra Task Javier y comprobación SHA, pruebas y límites.
Rollback normal git revert, sin force-push, QA/main/PROD ni despliegue.
