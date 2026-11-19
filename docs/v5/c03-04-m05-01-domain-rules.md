# C03-04.M05.01 — xUnit de reglas de incidentes

Riesgo R2 por autorización y revocación. Pruebas sobre la clase real Domain V5ContinuityIncidentDomainRules con positivos de lectura, contención y resolución que exigen revalidación en servicio; contrapruebas JWT/membresía/RBAC atribuidos como fixtures, tenant A/B, sucursales, idempotencia, versiones, revocación, evidencia, restore prohibido, estados y entradas inválidas.

No se añadieron endpoints, migraciones, permisos, esquemas, secretos o servicios productivos. GitHub-hosted verifica build Release .NET 10, xUnit dirigida con TRX >=20 casos sin fallos/omitidos y Scope Lock/V5 por SHA exacto. No afirmar PASS antes de CI.

**A0–A4 R2 todavía incompletos:** la prueba Domain no representa JWT firmado ni HTTP real, persistencia MySQL/version CAS, backup/restore en sandbox ni independencia operativa real. En ausencia de esas evidencias conservar VALIDANDO, no inventar cero P0/P1, no autopase A5. A5 requiere Task Javier distinta. Revert normal del commit; dev exclusivamente, sin QA/main/PROD.
