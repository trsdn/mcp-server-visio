# Cross-Functional Flowchart

**When:** A process where *who performs each step* carries as much meaning as the order — handoffs
between teams, approval chains, anything where the interesting failure is "it sat with Finance for
a week".

**Stencil:** `BASFLO_M.VSSX` for the nodes.

## The lane stencil is not installed by default

`CROSFN_M.VSSX` (Cross-Functional Flowchart) is **not present on a stock install** — verified by
opening it and getting a `COMException`. Do not reference it without a fallback.

Build the lanes instead:

```
stencil(drop-master, stencil_path='BASIC_U.VSSX', master_name='Rectangle', ...)   one band per lane
cell(set-formula, sheet_target='shape', shape_name='Lane1', cell_name='FillForegnd', formula='RGB(245,245,245)')
cell(set-formula, sheet_target='shape', shape_name='Lane1', cell_name='LinePattern',  formula='0')
shape(z-order, shape_name='Lane1', z_order_cmd=2)                                 send behind the nodes
```

A lane is a background band, so it must sit behind everything: `z_order_cmd=2` (SendToBack) once
per lane, before placing nodes. Do it after and the lane hides the process.

Give each lane a `layer(create, layer_name='Lanes')` and add the bands to it, so a reader can hide
the banding without touching the process.

## Layout

```
1.60 in   lane height
0.20 in   gap between lanes
1.20 in   left margin reserved for the lane label
2.00 in   node width
0.60 in   node height
```

Lanes horizontal, process left-to-right: the eye tracks a handoff as a vertical jump, which is
exactly the event worth seeing. Vertical lanes with a top-to-bottom process work too, but do not
mix — a diagram with horizontal lanes and a vertical process is unreadable.

Label each lane at its left edge with a separate text shape, not with the band's own text: band
text centres in the band and collides with the nodes.

## Build order

1. Page size and lane count first — lanes cannot be resized sensibly once nodes sit on them.
2. Drop and colour the bands, send them to back, put them on a `Lanes` layer.
3. Place nodes **within** a lane's vertical span, at the x that reflects sequence.
4. Connect with `shape(connect-shapes)` across lanes.

A connector crossing a lane boundary is a handoff. That is the diagram's point, so do not route
around it to keep things tidy.

## Anti-patterns

**Nodes that straddle a lane boundary.** The reader cannot tell who owns the step. Keep every node
fully inside one band.

**A lane with no nodes.** Either the team is not in this process, or a step is missing. Both are
worth resolving before the diagram ships.

**Lanes drawn on top.** Without `z_order_cmd=2` the bands cover the process, and the result looks
like empty coloured stripes.

**Referencing `CROSFN_M.VSSX`.** It is not installed. An agent that assumes it will fail at
`stencil(drop-master)` with a file-not-found, having already created the page.

**Using colour to mean two things.** If the bands are coloured per team, do not also colour nodes
by status. Pick one; the other becomes text or a callout.
