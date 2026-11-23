## 2026-11-23 — DEV01 V30 C07-01.M06.06: lectura real de marcas por tenant bajo piloto privado
- Runtime .NET 10: habilita exclusivamente GET marcas/activas/detalle con JWT firmado, RBAC Marcas.Ver SQL, empresa verificada y filtros Oracle EF/MySQL. Piloto OFF por defecto; ninguna mutación de marcas ni despliegue.
- Repara el test fiscal obsoleto GET=503 al existir piloto fiscal positivo, preservando pruebas de bloqueo de escrituras; añade aceptación real MySQL 8.4 de marca A/B propia/ajena, spoof, revocación y piloto apagado. Evidencia de CI exact-SHA requerida, no declarada PASS antes de concluir.
- Entrega funcional R2 parcial de C07-01.M06.06; dueño DEV01, candado V17 2000001045 conservado, sin LISTO/A5 ni liberación DAG; rollback mediante nuevo commit correctivo FF/CAS.

