# STATUS

**Updated:** 2026-09-01  
**Authoritative baseline:** `Warehouse_Viewpoint_Sign_System_BASELINE_v1.0_2026-09-01.md`  
**Current phase:** PoC-A / Gate G3 PHYSICAL HUMAN GATE

## Current state

- Requirements baseline v1.0: BASELINED
- HG-01..HG-08: CLOSED
- PoC-A specification v0.2: DEFINED
- G0 mathematical model v0.1: DEFINED
- Gate G0: **PASS**
- Gate G1: **PASS**
- Gate G2: **PASS**
- G3 preparation: **PASS**
- Gate G3 physical execution: **HUMAN GATE / READY**

## G0 executable evidence

- PR: #1 `PoC-A G0 geometry core and acceptance coverage`
- merged to `main`: `a0f6570c742089ba2711dec159b968e4824ae1f0`
- GitHub Actions: run #8
- restore/build/tests: PASS

## G1 executable evidence

- PR: #2 `PoC-A G1 virtual reprojection pipeline`
- merged to `main`: `26733c7ed31079caad4ff8e31b64c29c01707d02`
- GitHub Actions: run #12 (`33421204192`)
- restore/build/tests: PASS
- G0 regression: PASS
- G1 TEST-01..03: PASS

## G2 executable evidence

- PR: #3 `PoC-A G2 print dimensional integrity`
- merged to `main`: `3f2c70331a460f34c63356ae8eedd5773406fc12`
- GitHub Actions: run #15 (`33421522858`)
- restore/build/tests: PASS
- A4 physical page dimensions: PASS
- 100 mm encoded calibration length: PASS
- 1:1 artwork dimensional integrity: PASS
- tiling coverage and overlap: PASS
- page identification/orientation/registration data: PASS
- deterministic PDF byte output: PASS

Physical printer/driver scaling is intentionally not claimed by G2.

## G3 preparation evidence

- PR: #4 `PoC-A G3 physical test preparation`
- merged to `main`: `dbcbad3c5fb216180632cdf02451a9d11ef1c781`
- GitHub Actions: run #20 (`33422261351`)
- restore: PASS
- Release build: PASS
- complete regression/fixture tests: PASS
- G3 physical package generation: PASS
- CI artifact upload: PASS
- artifact name: `g3-physical-package`
- artifact SHA-256: `5662315145ab40af096ccd642b8abb2d67ea58bd6217732fe7c73aac7dc4782f`

The exact CI artifact was downloaded and S1/S2/S3 were rendered at 180 dpi and visually preflighted. A footer/calibration overlap found during the first render inspection was corrected before merge. Final PDFs have no observed clipped text or metadata overlap and pass PDF openability preflight.

The G3 fixture also exposed an ownership edge case where a valid analytic half-plane may be empty. The resolver was corrected and the complete G0-G3-preparation regression suite passes after the correction.

## G3 physical package

Generated package contents:

- `S1.pdf`
- `S2.pdf`
- `S3.pdf`
- `SETUP.csv`
- `MANIFEST.txt`

Execution instructions are authoritative in `poc/G3_PHYSICAL_RUNBOOK.md`. Results are recorded in `poc/G3_OBSERVATION_RECORD.md`.

## Human Gate required now

G3 cannot be passed by software or simulation alone. Human physical execution is now required:

1. print S1/S2/S3 using **Actual size / 100%**, with fit/shrink scaling disabled
2. measure and record each printed 100 mm calibration line
3. install the three surfaces using `SETUP.csv` and the runbook coordinate convention
4. observe `GA` at the reference viewpoint and at lateral offsets -0.5 m, +0.5 m, -1.0 m, +1.0 m
5. complete `poc/G3_OBSERVATION_RECORD.md`, including any measured installation/viewpoint errors and discrepancy classification

## G3 exit rule

G3 is **not PASS** until the physical evidence demonstrates all existing acceptance conditions in `tests/POC_A_ACCEPTANCE.md`:

- print scale is acceptable or a documented correction is applied
- installation can be completed from the generated page/origin/registration information without locating individual glyph fragments
- the reference viewpoint produces an unambiguous `GA`
- deformation away from reference qualitatively agrees with the predicted perspective trend
- discrepancies can be attributed to measured categories: print scale, geometry input, installation offset, viewing-position error, or projection implementation

The final warehouse readable-zone threshold remains intentionally unfrozen until the physical G3 results are available.
