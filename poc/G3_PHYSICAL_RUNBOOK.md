# G3 Physical PoC Runbook v0.1

## Purpose

Validate that the digitally proven projection and print geometry survives actual printing, placement and viewpoint error.

This is the first gate that requires physical human execution.

## Test package

The generator produces:

- `S1.pdf`
- `S2.pdf`
- `S3.pdf`
- `SETUP.csv`
- `MANIFEST.txt`

Each PDF is A4 portrait and the surface artwork fits on one page.

## Materials

- A4 printer and ordinary A4 paper
- three flat boards/panels at least 190 x 277 mm
- tape or removable adhesive
- tape measure
- ruler with millimetre scale
- floor tape or other means to mark the reference viewpoint

## Coordinate convention

Use a floor coordinate system:

- X: right when looking into the setup
- Y: forward from the viewer
- Z: height from the floor

Reference eye position:

- X = 0 mm
- Y = 0 mm
- Z = 1200 mm
- look direction = +Y

The printed artwork is designed for the geometry in `SETUP.csv`. The surface origin is the **lower-left corner** of the printable 190 x 277 mm rectangle when facing the printed side. `right_dx/right_dy/right_dz` gives the unit direction from the lower-left toward the lower-right corner.

## Print check

1. Print every PDF with **Actual size / 100%**.
2. Disable `Fit`, `Shrink oversized pages`, automatic scaling and similar options.
3. Measure the printed 100 mm calibration line on every sheet.
4. Record the measured length before installation.
5. Do not proceed if a sheet has an unexplained large scale error. Correct printer settings first.

The digital file is already dimensionally verified in G2; this step detects printer/driver scaling.

## Installation

1. Establish the coordinate origin on the floor directly below the reference viewpoint.
2. Mark the reference viewpoint X=0, Y=0.
3. Set the effective eye/camera height to Z=1200 mm.
4. Place S1, S2 and S3 using `SETUP.csv`.
5. Keep every board vertical; only the horizontal direction differs between S1/S3 and the centre surface.
6. Align each sheet to the lower-left origin of its 190 x 277 mm surface rectangle using the registration marks. Do not manually reposition glyph fragments.

Approximate interpretation of the fixture:

- S1 is the nearest/left surface and is yawed slightly one way.
- S2 is the centre surface and faces squarely across X.
- S3 is the far/right surface and is yawed slightly the opposite way.

Use the numeric setup data as authoritative rather than this verbal description.

## Observation positions

Record at minimum:

- reference: X = 0.0 m
- left 0.5 m: X = -0.5 m
- right 0.5 m: X = +0.5 m
- left 1.0 m: X = -1.0 m
- right 1.0 m: X = +1.0 m

Keep Y approximately at the reference line and eye/camera height approximately 1200 mm unless deliberately testing another variable.

## Required observations

At each position record:

- can `GA` be identified unambiguously: YES / NO
- visible deformation or missing stroke
- which surface boundary appears responsible, if identifiable
- viewer position uncertainty
- photo filename, if a photo is taken

At the reference position additionally record:

- actual calibration-line lengths
- measured installed surface origins
- obvious page placement offsets

## G3 PASS rule

G3 passes only if:

1. print scale is acceptable or a documented correction has been applied
2. installation can be performed from page/origin/registration information without locating individual glyph fragments
3. `GA` is unambiguous at the reference viewpoint
4. deformation away from reference qualitatively follows the predicted perspective trend
5. discrepancies can be assigned to a measured category: print scale, geometry input, installation offset, viewing-position error or projection implementation

G3 does **not** freeze the final warehouse readable-zone threshold. That calibration belongs to the next gate.
