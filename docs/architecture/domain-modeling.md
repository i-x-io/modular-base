# Domain modeling

## Purpose

Describe how optional domain-driven design concepts can clarify a library’s semantic model without forcing an application architecture.

## Canonical definitions

### Domain model

A domain models business meaning. An entity has continuity of identity; a value object is defined by attributes. An aggregate is a consistency boundary. A domain service contains stateless domain behavior that belongs to no entity or value object.

## Related and contrasting terms

DDD vocabulary is a design aid, not a mandate for repositories, event sourcing, CQRS, or microservices. A DTO transports data; it is not automatically an entity or value object. A bounded context is a meaning boundary, not a namespace convention.

Expected domain and application decisions, such as invalid input, a missing business object, or a rejected state transition, are returned as coded failed results at a service boundary. Cancellation, broken invariants, programming faults, corrupt state, and unexpected infrastructure failures remain exceptions. A specific exception may be translated only when it fully and honestly represents a documented expected outcome.

## Normative rules

- Use domain terms in public names only when they represent stable consumer semantics.
- Preserve aggregate invariants inside the aggregate boundary; do not expose mutation that bypasses them.
- Introduce repositories, domain events, and CQRS only for a documented current need.
- Keep persistence and transport models out of technology-neutral contracts unless they are the deliberate public contract.

## Library-focused examples

An immutable `Money` value object can validate currency and amount together. A `Transfer` aggregate may own the invariant that an operation is recorded once. A database adapter maps those types without making persistence classes public domain API.

## Anti-patterns

Table-shaped “entities” with no behavioral ownership, generic repositories that only mirror an ORM, and an event type used merely as an internal callback are not domain modeling.

## Review questions

- Does this type represent identity, value, transport, or persistence?
- Which invariant must be protected together?
- Is a separate bounded context justified by differing meaning or ownership?

## Analyzer and build enforcement

No analyzer proves a domain model. `IXM1001` documents public data objects, `IXM2001` syntactically suggests records for eligible class-shaped data objects without inferring immutability, and architecture checks prevent infrastructure leakage.

## Authoritative references

- [Domain-driven design terminology](terminology.md#domain-driven-design-vocabulary)
- [CQRS pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs)

## Last research/access date

2026-07-27.
