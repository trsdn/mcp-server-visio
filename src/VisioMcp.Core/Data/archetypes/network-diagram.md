# Network Diagram

**When:** Servers, devices and the links between them — physical topology, logical segmentation,
or where a boundary sits.

**Stencils (all installed by default):**

| Stencil | Masters | Use for |
|---|---|---|
| `PERIPH_M.VSSX` | Server, Router, Switch, Firewall, Ethernet, Mainframe, Printer, Comm-link | The core devices |
| `NETLOC_M.VSSX` | Cloud, City, Building, House, Government building, University building | Sites and the internet |
| `DTLNET_M.VSSX` | Patch panel, PBX, Radio tower, Satellite, Repeater, Fiber optic transmitter | Physical infrastructure detail |

`Ethernet` is a bus master — a backbone the devices attach to — not a device. Use it when the
topology genuinely is a shared segment; a point-to-point link is a connector.

## Decide logical or physical before drawing

They answer different questions and must not be mixed on one page:

- **Logical** — segments, trust boundaries, traffic direction. `Firewall`, `Router`, `Cloud`.
- **Physical** — racks, cabling, ports. `Patch panel`, `Fiber optic transmitter`, `Repeater`.

A diagram showing both says neither clearly. Two pages: `page(create, name='Logical')` and
`page(create, name='Physical')`.

## Layout

```
1.00 in   device width
0.80 in   device height
1.20 in   gap between devices in a segment
2.50 in   gap between segments
```

Group by segment, not by device type. The reader's question is "what can reach what", and adjacency
answers it faster than a connector across the page.

Put a segment on its own layer so it can be isolated:

```
layer(create, layer_name='DMZ')
layer(add-shape, layer_name='DMZ', shape_name='...')
```

Draw trust boundaries as a background rectangle with no fill and a dashed line, sent to back:

```
cell(set-formula, sheet_target='shape', shape_name='DMZBoundary', cell_name='FillPattern',  formula='0')
cell(set-formula, sheet_target='shape', shape_name='DMZBoundary', cell_name='LinePattern',  formula='2')
shape(z-order, shape_name='DMZBoundary', z_order_cmd=2)
```

## Labelling

A device with no label is decoration. Every node carries at least a hostname or a role; a link that
matters carries its bandwidth or protocol as connector text.

IP addresses belong in shape data rather than the label, so they are queryable without crowding the
picture:

```
shape(set-property, shape_name='web-01', property_name='IPAddress', property_value='10.0.1.15')
```

## Anti-patterns

**Logical and physical on one page.** The commonest reason a network diagram is argued over rather
than used.

**Every device connected to every other.** If the topology really is a mesh, say so with an
`Ethernet` bus or a note — thirty crossing connectors communicate nothing.

**Unlabelled firewalls.** A firewall's value in a diagram is the rule it enforces. Put the direction
and the allowed traffic on the connector.

**Cloud as decoration.** `Cloud` means "outside our control". Using it for an internal service
misleads.

**Redrawing per environment.** Draw production once, then `page(create)` and adjust — or use shape
data plus `master(list-instances)` to find what needs changing.
