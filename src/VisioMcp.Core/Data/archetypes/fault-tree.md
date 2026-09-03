# Fault Tree

**When:** How component failures combine into one top-level failure — reliability analysis, incident
post-mortems, safety cases. The diagram reads downward from a single undesired event to its causes.

**Stencil:** `FAULT_M.VSSX` (installed by default — 12 masters)

## Masters

| Master | Meaning |
|---|---|
| `Event` | The top event, and any intermediate failure that is itself explained below |
| `AND gate` | The output occurs only if **every** input occurs |
| `OR gate` | The output occurs if **any** input occurs |
| `Basic event` | A root cause, not analysed further. A leaf |
| `Undeveloped event` | Could be analysed further, deliberately is not — say why in the text |
| `House event` | A normal condition, not a failure. Used to model an operating mode |
| `Conditional event` | A constraint on a gate, typically on `Inhibit gate` |
| `Inhibit gate` | Output occurs if the input occurs **and** a condition holds |
| `Priority AND gate` | An AND where the inputs must occur in order |
| `Exclusive OR gate` | Exactly one input, not more |
| `Voting gate` | k-out-of-n |
| `Transfer symbol` | The subtree continues on another page |

## AND versus OR is the entire diagram

Everything else is layout. An OR gate drawn where the logic is AND overstates the failure rate by
orders of magnitude, and the diagram will still look completely plausible.

State the logic in words before drawing, and check each gate against the sentence:

- "Either the primary **or** the backup failing causes the outage" → `OR gate`
- "The outage needs the primary **and** the backup to fail" → `AND gate`

## Layout

```
2.00 in   event width
0.90 in   event height
0.60 in   gate size
1.10 in   vertical gap between tiers
0.50 in   horizontal gap between siblings
```

Top event at the top centre; causes below. A gate sits between the event it explains and its
inputs — event, then gate, then the inputs beneath it. A gate drawn beside its event rather than
below breaks the reading order.

## Build order

```
page(create, name='Unplanned outage')
stencil(drop-master, stencil_path='FAULT_M.VSSX', master_name='Event', ...)        the top event
stencil(drop-master, stencil_path='FAULT_M.VSSX', master_name='OR gate', ...)      directly below
stencil(drop-master, stencil_path='FAULT_M.VSSX', master_name='Basic event', ...)  each input

shape(connect-shapes, shape_names='TopEvent,Gate1')
shape(connect-shapes, shape_names='Gate1,Cause1')
shape(connect-shapes, shape_names='Gate1,Cause2')
```

One call per edge: `connect-shapes` chains, so passing every input at once would connect the causes
to each other rather than to the gate.

Where a subtree grows beyond a page, place a `Transfer symbol` and continue on a new page with a
matching symbol. Label both with the same identifier.

## Anti-patterns

**A gate with one input.** It expresses nothing. Connect the event directly.

**Basic events that are not basic.** A leaf labelled "Database problem" hides the analysis. Either
decompose it or mark it `Undeveloped event` and say why.

**Mixed AND/OR at one gate.** A gate has one logic. Two gates in series if the logic is mixed.

**No probabilities or rates anywhere.** A qualitative tree is legitimate, but say so — otherwise a
reader assumes the omission is an oversight. Put values in shape data.

**Bottom-up construction.** Start from the top event. A tree assembled from causes upward tends to
end with several unconnected roots and no stated failure.
