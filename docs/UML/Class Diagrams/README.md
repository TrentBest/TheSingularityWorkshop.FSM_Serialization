# Class Diagrams

**Rule: one canonical diagram per declared type.** Cover classes, structs, records, interfaces, enums, delegates, nested types, and generic declarations. Preserve namespace, generic parameters/constraints, visibility, fields/properties/events, constructors, methods, implemented interfaces, base types, and relevant attributes.

The focused class is the visual anchor. The UML data card uses a colored border and header; clicking/expanding it reveals nested members and relevant details. Keep large member inventories collapsible.

Each generated declaration needs a stable identity and source path/span. Distinguish source declarations from compiled/reflection-only types. Existing hand-authored cards remain until generated cards have been reviewed against them.

Generator: planned under [issue #7](https://github.com/TrentBest/TheSingularityWorkshop.FSM_Serialization/issues/7).
