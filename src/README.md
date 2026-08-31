# Source

Implementation begins only after the corresponding gate specification is fixed.

Planned solution structure:

```text
src/
  ViewpointSign.Domain/
  ViewpointSign.Geometry/
  ViewpointSign.Projection/
  ViewpointSign.Readability/
  ViewpointSign.Optimization/
  ViewpointSign.Printing/
  ViewpointSign.ProjectFile/
  ViewpointSign.Desktop/
```

G0/G1 work should start with `Domain`, `Geometry`, `Projection` and automated tests. WPF UI is not on the critical path for proving the projection mathematics.
