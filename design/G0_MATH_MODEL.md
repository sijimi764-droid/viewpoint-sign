# G0 Mathematical Model v0.1

## Scope

G0 proves the deterministic geometry primitives required by PoC-A. It does not evaluate human readability and does not perform optimization.

## 1. Coordinate systems

World coordinates are right-handed and measured in millimetres:

- `+X`: right
- `+Y`: depth
- `+Z`: up

Each installation surface has a local 2D basis `(u,v)` with origin `S0` and unit basis vectors `eu`, `ev` lying in the plane. A world point on the surface is:

`X(u,v) = S0 + u*eu + v*ev`

For the initial PoC, each surface is rectangular and finite:

`0 <= u <= width`, `0 <= v <= height`.

## 2. Plane representation

A plane is represented by a known point `P0` and normalized normal `n`:

`dot(X - P0, n) = 0`

A valid rectangular surface requires:

- width > 0
- height > 0
- `eu` and `ev` non-zero
- `abs(dot(eu,ev))` within orthogonality tolerance
- `n = normalize(cross(eu,ev))` finite and non-zero

## 3. Ray-plane intersection

For viewer `V` and target-plane point `T`, define direction:

`d = T - V`

and ray:

`R(t) = V + t*d`.

For plane `(P0,n)`:

`t = dot(P0 - V, n) / dot(d,n)`.

Reject when:

- `abs(dot(d,n)) <= epsilon_parallel`
- `t <= epsilon_forward`
- result is non-finite

For a finite surface, convert hit point `H` to local coordinates:

`u = dot(H - S0, eu)`

`v = dot(H - S0, ev)`

and accept only within finite bounds plus physical boundary tolerance.

## 4. Target plane

The virtual sign plane also has origin `T0`, local basis `(tx,ty)`, width and height. Target 2D coordinates `(a,b)` map to world coordinates:

`T(a,b) = T0 + a*tx + b*ty`.

Glyph polygons exist in these target-local coordinates.

## 5. Surface visibility footprint on target plane

For each installation-surface corner `Si`, construct the line through `V` and `Si`, then intersect it with the target plane. The resulting target-local points `Fi` form the surface visibility footprint.

Because the installation surface is planar and finite, its projection from a pinhole viewpoint onto another plane is a projective quadrilateral except for degenerate configurations.

Reject or diagnose when:

- a required projection line is parallel to the target plane
- the projected quadrilateral is self-intersecting due to invalid corner ordering
- projected area is below tolerance
- the target plane lies in a configuration that makes the mapping singular

## 6. Projective mapping / homography

Let four non-collinear target-plane footprint coordinates correspond to four installation-surface local coordinates. Compute homography `H_ts` such that homogeneous target coordinate `p_t=[a,b,1]^T` maps to local surface coordinate `p_s ~ H_ts p_t`.

The inverse `H_st` maps surface-local geometry back to the target plane for verification.

Acceptance requires round-trip residuals below the G0 tolerance for deterministic test samples.

The homography is geometry truth for planar fragment mapping. Rendering may tessellate polygons, but tessellation must not change stored authoritative coordinates.

## 7. Polygon clipping

Let `G` be a glyph polygon on the target plane and `F_s` the visible footprint owned by surface `s`.

The source polygon assigned to surface `s` is:

`A_s = G ∩ F_s`.

Glyphs may contain holes. The clipping representation must therefore support multi-polygons and interior rings; assumptions that all glyph polygons are convex are prohibited.

A robust polygon Boolean implementation may be used rather than implementing polygon clipping from scratch, provided its license is acceptable and all geometry is converted through a single documented scale/tolerance policy.

## 8. Occlusion and ownership

Projected footprints of different surfaces may overlap on the target plane. For simple G0/G1 projection, only the physically nearest visible surface may own a target region.

Depth cannot in general be represented by one constant per plane over the whole footprint. Therefore ownership must be resolved as a function of target position/ray, not by sorting entire surfaces using only centre distance.

For an overlapping target region, evaluate ray-intersection parameter `t_s(a,b)` for each candidate surface. The surface with the smallest valid positive `t` owns that location.

Implementation may derive exact ownership-boundary geometry where practical, or partition overlapping footprint polygons into cells and classify them, but G1 contour error must remain within acceptance tolerance. Raster-only ownership is not authoritative.

## 9. Gap

For glyph target region `G_total` and union of visible owned installation regions `U`:

`Gap = G_total - U`.

Gap output must preserve polygons and area, not only a percentage.

## 10. Numerical tolerances

Centralize tolerances. Initial values for implementation tests:

- normalized/vector epsilon: `1e-10` to `1e-8` depending on operation
- physical local/world round trip: `0.001 mm`
- G1 reconstructed contour tolerance: `0.5 mm`

The code must distinguish numerical epsilon from product/installation tolerance. They must not share one constant.

## 11. Determinism

For identical input and algorithm version:

- surface ordering rules are explicit
- polygon output ordering is normalized before serialization/comparison
- no implicit random sampling is allowed in G0
- diagnostics include the exact tolerance profile/version

## 12. Required G0 implementation types

Minimum domain/API concepts:

- `Point2D`, `Point3D`, `Vector3D`
- `Ray3D`
- `Plane3D`
- `RectSurface3D`
- `TargetPlane`
- `Homography2D`
- `Polygon2D` / `MultiPolygon2D`
- `IntersectionResult`
- `SurfaceFootprint`
- `OwnershipRegion`
- `ToleranceProfile`

These types should be independent of WPF types such as `Point`, `Vector3D`, or rendering-library classes.
