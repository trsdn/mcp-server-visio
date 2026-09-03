# Flowchart

**When:** A process with steps, decisions and branches, where the order matters and the performer
does not. If who does each step matters, use `cross-functional-flowchart` instead.

**Stencil:** `BASFLO_M.VSSX` (Basic Flowchart Shapes — installed by default)

## Masters and what each one means

| Master | Use for |
|---|---|
| `Start/End` | The single entry and each terminal outcome |
| `Process` | An action or step |
| `Decision` | A branch. Exactly one question, answerable yes/no |
| `Subprocess` | A step detailed elsewhere — link it with `hyperlink(add, sub_address='Page-3')` |
| `Document` | A step producing a document |
| `Data` | Input or output |
| `Database` | Persistent storage |
| `On-page reference` | A jump within the page, when a connector would cross too much |
| `Off-page reference` | A jump to another page |

## Layout

Top-to-bottom for a process read as a sequence; left-to-right when it parallels a timeline. Pick
one and hold it — a diagram that changes direction halfway reads as two diagrams.

```
0.75 in   node height
2.00 in   node width
0.75 in   vertical gap between nodes
1.50 in   horizontal offset for a decision branch
```

Place the decision's "no" branch to the side and its "yes" branch straight on, so the main path
stays a straight line. A reader follows the straight line first.

## Build order

```
page(create, name='Order fulfilment')
cell(set-formula, sheet_target='page', cell_name='PageWidth',  formula='11 in')
cell(set-formula, sheet_target='page', cell_name='PageHeight', formula='8.5 in')

stencil(drop-master, stencil_path='BASFLO_M.VSSX', master_name='Start/End', x_position=..., y_position=...)
text(set, shape_name='...', text='Order received')
... one drop + one text per node ...

shape(connect-shapes, shape_names='Start,Validate,Decision,Pick,Dispatch')
```

Connect last, in one call: `connect-shapes` chains in the order given, so a straight path needs a
single call rather than one per pair.

For a branch, connect the main path first, then each branch separately:

```
shape(connect-shapes, shape_names='Decision,Reject')      # the "no" side
```

Label a branch by setting text on the connector itself — `text(set, shape_name='Dynamic connector')`
— not by placing a floating text box near it, which does not move when the connector reroutes.

## Anti-patterns

**A decision with three or more outgoing connectors.** That is two decisions. Split it, or the
reader cannot tell which condition produced which path.

**Unlabelled branches.** A `Decision` whose outgoing connectors have no text is unreadable; the
shape asks a question that the diagram never answers.

**A drawn diamond.** `shape(add-shape)` cannot produce a Decision. It gives a rectangle or an
ellipse, and no connection points where a flowchart expects them.

**Connectors that cross the page.** Use `Off-page reference` and a second page. A connector routed
across an entire diagram is harder to follow than a labelled jump.

**A process with no terminal.** Every path ends in a `Start/End`. A branch that simply stops leaves
the reader asking what happens next.
