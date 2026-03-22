# Diagram Generation Pipeline

Use this workflow when building a Visio diagram from a single prompt or brief.

## Pre-Generation Checklist

Before creating any page, clarify these outputs:

| Step | Output | Goal |
|---|---|---|
| 1 | Objective | What the diagram must explain or support |
| 2 | Diagram type | Process, swimlane, org chart, network, layout sketch, etc. |
| 3 | Page plan | Which pages are needed and what each page covers |
| 4 | Primary elements | Core shapes, lanes, nodes, or containers |
| 5 | Relationship model | Connectors, flow direction, hierarchy, or dependencies |
| 6 | Label plan | Text the user must see on the final diagram |
| 7 | Validation checkpoints | Which pages, labels, and shapes must exist before finishing |

## Execution Workflow

1. Open or create the target `.vsdx` file.
2. Create the required pages in order.
3. Add base shapes or drop stencil masters.
4. Set labels immediately after adding shapes.
5. Use ShapeSheet edits only for geometry or formula-backed adjustments.
6. Re-read the page and shape state after each major phase.
7. Save and close the file explicitly.

## Large Diagram Pattern

For larger jobs, keep the orchestration in one controlling client with four logical phases:

1. **Plan** — produce a page-and-shape plan
2. **Execute** — build the diagram through normal sequential calls
3. **Verify** — inspect the generated pages, labels, and geometry
4. **Repair** — fix only the missing or incorrect parts

## Validation Checklist

After generation, confirm:

- the expected pages exist
- required shapes or masters were added
- labels match the plan
- connectors or relationships are present where required
- obvious overlap or crowding has been corrected
- the file was saved and the session was closed

## Practical Advice

- Prefer one purpose per page.
- Prefer stencil masters when a standard Visio shape already exists.
- Use exact names discovered from the live document instead of guessing.
- Keep edits targeted; do not rebuild whole pages unless the structure is wrong.
