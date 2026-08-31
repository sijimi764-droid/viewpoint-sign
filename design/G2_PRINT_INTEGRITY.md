# G2 Print Dimensional Integrity Design v0.1

## Objective

Convert authoritative surface-local artwork in millimetres into deterministic, physically dimensioned PDF pages without changing geometry truth.

## Unit policy

- domain/printing geometry: millimetres
- PDF output: points
- exact conversion definition: `points = millimetres * 72 / 25.4`
- no pixel or DPI conversion is used to determine physical artwork size

## Paper profile

PoC-A supports ISO A4:

- portrait: 210 x 297 mm
- landscape: 297 x 210 mm

`PrintProfile` contains paper size, printable margin, overlap and calibration-line length. Initial overlap is 10 mm.

## Tiling

The printable artwork span on a page is:

- `paper width - 2 * margin`
- `paper height - 2 * margin`

Adjacent tile origins advance by `printable span - overlap`. Tiles are deterministic and ordered row-major. The final tile may extend beyond the artwork bounds; artwork itself is clipped to the tile window before output.

## PDF writer

PoC-A uses a project-owned minimal PDF 1.4 writer. This is deliberate:

- `/MediaBox` is written directly from ISO dimensions converted to points
- vector paths are written directly from millimetre coordinates converted to points
- no layout engine may rescale artwork
- object order and serialization are deterministic
- Helvetica Type1 is used only for ASCII installation metadata; it is not part of sign geometry

The writer emits:

- project ID
- surface ID
- page index / count
- page orientation
- tile row / column
- explicit `PRINT 1:1` label
- 100 mm calibration line
- four registration crosshairs
- machine-readable `%VPS` comments for acceptance inspection

## Geometry boundary

Surface artwork remains `MultiPolygon2D` in local millimetre coordinates. PDF points are an encoding detail only. Page clipping uses the same robust polygon Boolean engine established in G0.

## Acceptance

G2 CI verifies:

1. A4 portrait/landscape `/MediaBox` dimensions
2. encoded 100 mm calibration-line length
3. 1:1 millimetre-to-point round trip for artwork dimensions
4. complete multi-page coverage
5. exact configured adjacent-tile overlap
6. required page identification/orientation/registration data
7. deterministic byte output for identical input

Physical printer-driver scaling remains a G3 responsibility.
