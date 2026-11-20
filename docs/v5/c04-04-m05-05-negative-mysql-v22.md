# C04-04.M05.05 — pruebas negativas de aislamiento y abuso MySQL 8.4 (V22)

PLAN D714 exige ejecutar un caso negativo de seguridad, aislamiento o abuso. Riesgo **R2**; prueba **test-only** de contrato técnico provider-neutral. No modifica controllers, AppDbContext productivo, autorización, DTO, pagos, despliegues ni dependencias del runtime. PLAN E expresa la meta acumulativa de M05, no permite certificar la fase completa con esta prueba.

- **A0:** revisar cambios limitados a nuevo xUnit, workflow GitHub-hosted, esta evidencia y CHANGELOG, preservando contratos anteriores. Rollback normal con `git revert`.
- **A1:** casos negativos reales: reutilización de hash con monto/factura/payload distintos => `Conflict`; argumentos inválidos e inyección textual en provider => `Denied`; cancelación previa => excepción sin mutación; conteo de filas constante.
- **A2:** inspección con tenant B antes de reservar sólo muestra `Missing` respecto de A; reservation B debe ser físicamente distinta. Evitar extrapolación: el adaptador aislado recibe el tenant como parámetro; **no autentica al cliente ni demuestra verificación JWT/RBAC comercial**.
- **A3:** Oracle EF Core contra MySQL 8.4 **efímero y real** comprueba que CHECK física rechaza `EmpresaId=0` incluso sorteando validaciones del servicio; TRX dirigido 1 PASS/1 TOTAL y 0 FAIL/SKIP. El esquema se destruye al concluir.
- **A4:** Scope Lock, Skill, V5 provenance, backend Release/seguridad pertinentes y xUnit dirigido **COMPLETED SUCCESS exact-SHA** antes de LISTO; otros checks en progreso no son PASS ni se usan como prueba.
- **A5:** revisión genuina por Task Javier distinta del autor o reparador.

No se efectúan pagos reales ni PSP/webhooks ni se certifica un perímetro autenticado HTTP/JWT, titularidad comercial, producción o toda la fase M05. Estas pruebas sólo cubren las propiedades negativas del contrato de almacenamiento aislado; la autorización efectiva debe acreditarse en su scope técnico correspondiente.
