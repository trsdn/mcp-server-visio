# System Context Diagram

**When:** One system at the centre, the people and systems around it, and what flows between them.
The diagram that answers "what is this thing, and what does it touch" before any internal detail.

Deliberately excludes internals. If the question is how the system is built, use `block-diagram`.

**Stencil:** `BASIC_U.VSSX` — this is a notation-light archetype, and plain shapes are correct.

## Shape vocabulary

Convention carries the meaning, so hold it consistently:

| Element | Shape | Fill |
|---|---|---|
| The system in scope | `Rounded Rectangle`, centred, largest | Accent, e.g. `RGB(31,78,121)` |
| A person or role | `Ellipse` | Neutral, `RGB(240,240,240)` |
| An external system | `Rectangle` | Light grey, `RGB(224,224,224)` |
| A boundary | `Rectangle`, no fill, dashed, sent to back | — |

One accent colour, used **only** for the system in scope. Everything else neutral. The moment a
second thing is coloured, the centre stops being obvious.

## Layout

```
2.60 in   in-scope system width
1.40 in   in-scope system height
1.80 in   external element width
0.90 in   external element height
1.60 in   radius from centre to the surrounding ring
```

Centre the system; arrange the others around it. Users above, downstream consumers to the right,
dependencies below or left. A reader then infers direction from position before reading a label.

## Every connector carries a verb

An unlabelled arrow between two boxes says only "related", which the reader already assumed. The
label is the content:

```
shape(connect-shapes, shape_names='Customer,Booking service')
text(set, shape_name='Dynamic connector', text='Places booking')
```

Prefer "Places booking" over "HTTPS". The protocol belongs in shape data or a second, technical
page; the context diagram answers *what*, not *how*.

## Build order

1. Page size, then the in-scope system at the centre.
2. Surrounding elements, evenly spaced.
3. Boundary rectangles, sent to back with `z_order_cmd=2`.
4. Connectors, then a label on each.

## Anti-patterns

**Internals leaking in.** A database belonging to the system in scope does not appear. If it must,
the diagram has become a `block-diagram` and should be relabelled.

**More than about eight surrounding elements.** Group them — "Partner systems" as one box with the
list in shape data — or the ring becomes unreadable.

**Bidirectional arrows.** Two directions means two interactions with two different meanings. Draw
two connectors and label each.

**Colouring everything.** The accent marks scope. Spend it once.

**No boundary when one exists.** If some of the surrounding systems are inside the organisation and
some are not, that is usually the most consequential fact on the page — draw it.
