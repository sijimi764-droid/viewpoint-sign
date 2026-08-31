# STATUS

**Updated:** 2026-09-01  
**Authoritative baseline:** `Warehouse_Viewpoint_Sign_System_BASELINE_v1.0_2026-09-01.md`  
**Current phase:** PoC-A / Gate G0 verification

## Current state

- Requirements baseline v1.0: BASELINED
- HG-01..HG-08: CLOSED
- PoC-A specification v0.2: DEFINED
- G0 mathematical model v0.1: DEFINED
- G0 implementation: FEATURE-COMPLETE FOR CURRENT ACCEPTANCE SCOPE / CI VERIFICATION PENDING

## Implemented on `work/g0-core`

- .NET 8 geometry core project
- independent geometry types (no WPF dependency)
- normalized 3D vectors
- ray/plane intersection with explicit diagnostics
- finite rectangular installation surfaces
- surface-local ↔ world coordinate conversion
- target plane coordinates
- finite-surface intersection
- installation-surface footprint projection to target plane
- exact four-point planar homography with singularity diagnostics
- polygon / multipolygon model including holes
- robust polygon Boolean operations via NetTopologySuite 2.6.0
- per-ray depth ownership and occlusion partitioning using analytic depth-equality boundaries rather than whole-surface centre-distance sorting
- rear-surface partial-occlusion handling
- deterministic geometry normalization including polygon ordering, ring start and ring direction
- centralized numerical/product tolerances
- xUnit coverage for G0 acceptance items 1-13 at implementation level
- GitHub Actions .NET 8 restore/build/test workflow established on `main`

## Gate G0 acceptance mapping

1. analytic ray/plane intersection — IMPLEMENTED + TEST
2. parallel rejection — IMPLEMENTED + TEST
3. behind-viewer rejection — IMPLEMENTED + TEST
4. finite-surface bounds/tolerance — IMPLEMENTED + TEST
5. local/world round trip <= 0.001 mm — IMPLEMENTED + TEST
6. analytic footprint projection — IMPLEMENTED + TEST
7. projective round trip <= 0.001 mm — IMPLEMENTED + TEST
8. collinearity preservation — IMPLEMENTED + TEST
9. polygon clipping including concave subject — IMPLEMENTED + TEST
10. nearest-surface ownership — IMPLEMENTED + TEST
11. partial rear-surface occlusion — IMPLEMENTED + TEST
12. degenerate/singular rejection — IMPLEMENTED + TEST
13. deterministic normalized output — IMPLEMENTED + TEST

## Remaining before G0 PASS

1. GitHub Actions must successfully restore, compile and execute the tests.
2. Any build/test failures must be corrected.
3. Confirm NetTopologySuite package restore and license suitability for the project.
4. Update this status from verification pending to PASS only after executable evidence exists.

## Human Gate

No new Human Gate is currently required. The remaining G0 work is verification and correction against already-baselined acceptance criteria.

## Verification rule

G0 is not declared PASS from code inspection alone. A successful automated build/test run is required as executable evidence.
