# PoC-A Detailed Specification v0.1

## Purpose

Validate the end-to-end chain **manual geometry → reverse projection → virtual reconstruction → true-scale print → physical installation** before implementing smartphone capture.

PoC-A intentionally excludes Android, marker detection, AR, AI plane detection and warehouse-photo processing.

## Inputs

### Sign

- text: `GA`
- target width/height in mm
- target-plane pose
- glyph outlines as vector polygons
- foreground/background: black on white for PoC-A

### Viewer

- reference eye position `(x,y,z)`
- look target
- up vector
- vertical field of view
- render dimensions

### Installation surfaces

At least 1 and up to 3 manually defined rectangular planes for the initial PoC. Each surface has:

- ID
- four 3D corners
- physical width/height
- availability status
- local 2D coordinate system `(u,v)`

### Viewing zone

G0/G1 requires a reference point. The data model must already allow multiple sample viewpoints so G4 does not require a model rewrite.

## Core mathematical operation

For a target-plane sample point `T` and reference viewer position `V`, construct:

`R(t) = V + t(T - V), t > 0`

For installation plane with point `P` and normal `n`, solve:

`t = dot(P - V, n) / dot(T - V, n)`

An intersection is valid only when:

- denominator is not approximately zero
- `t > 0`
- the 3D intersection lies inside the finite installation surface
- no nearer selected installation surface occludes it along the same ray

The valid 3D hit point is converted to the surface-local 2D `(u,v)` coordinates for artwork generation.

## Surface ownership rule

For a reference ray crossing multiple usable installation surfaces, the nearest valid intersection to the viewer owns that ray for the initial simple projection. This matches physical visibility and prevents drawing hidden fragments on rear surfaces.

Later multi-view duplicate projection may intentionally add redundant artwork, but it is outside G0/G1.

## Vector processing strategy

Do not project only raster pixels as the final representation. Glyphs must remain vector polygons.

For PoC-A implementation it is acceptable to use adaptive polygon subdivision to approximate the perspective mapping onto each plane, provided reconstruction error stays within G1 acceptance tolerance.

Each projected polygon fragment must retain:

- source glyph ID
- source contour ID
- installation surface ID
- local polygon coordinates

## Gap definition

A target-sign region whose viewing ray hits no usable installation surface is a Gap. PoC-A records the Gap mask/area but does not yet optimize around it.

## Test geometries

### TEST-01 — single plane

A single installation plane parallel to the target sign plane. Expected result: equivalent to an ordinary perspective-correct sign with no artificial fragmentation.

### TEST-02 — three parallel depth planes

Three finite planes at different depths cover different target-ray regions. Expected result: fragments reconstructed from the reference view form `GA`.

### TEST-03 — three differently oriented planes

Three finite planes emulate a wall/column/rack-side arrangement. Expected result: perspective-distorted fragments reconstruct to `GA` from the reference view.

## Required outputs for G0/G1

- machine-readable projected fragments
- per-surface 2D artwork preview
- reference-view reconstruction image/vector scene
- Gap statistics
- diagnostic geometry dump containing rays/intersections for selected deterministic samples

## G2 print preparation

Printing is implemented only after G1 passes. Required print concepts are already fixed:

- physical units in mm
- A4 portrait/landscape selection
- 1:1 output
- 100 mm calibration line
- page/surface IDs
- orientation marks
- registration marks
- configurable overlap, initial default 10 mm

## G3 physical PoC

Use manually measured mock surfaces. Print and install the generated pages, then inspect from the reference location and lateral offsets. G3 does not establish the final human-readability threshold; it validates that the digital prediction survives printing and installation.

## Non-goals

- optimization of size/position
- duplicate projection
- camera geometry acquisition
- OCR-based scoring
- curved surfaces
- production UI polish
