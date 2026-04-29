# ADR-0012: Backend services not directly reachable, only via Gateway

- Status: accepted
- Date: 2026-04-29

## Context

ADR-0010 forbids synchronous HTTP between bounded contexts. ADR-0011 makes the Gateway the single authentication point for clients. Neither ADR, however, states explicitly that backend service endpoints must be unreachable from outside the Gateway.

In the current docker-compose setup the backend service containers (`catalog-api`, `cart-api`, `order-api`, `payment-api`, `warehouse-api`, `shipping-api`, `communication-api`) do not publish host ports, so they cannot be reached from outside the host. They share the default Docker network with the frontends, the payment mock and Keycloak though, so any container on that network could call them directly. Backend services do not authenticate incoming requests on their own — they trust that the Gateway has done it.

A regression that publishes a host port, or a future deployment target that places frontends on the same network as backends, would silently bypass the Gateway and ADR-0011's authorization checks.

## Decision

Backend services must not be reachable outside their private network.

- The Gateway is the only ingress point for HTTP traffic to backend services.
- Isolation is enforced at the network layer. No mTLS or shared secret between Gateway and backend services — the Gateway is the single ingress and the overhead is not justified for this project.
- Frontends (`frontend-web`, `backoffice-web`) are never connected to the backend network.

### Local development (docker-compose)

Two named networks:

- `edge` — bridge network used for browser-reachable components and host port publishing.
- `backend` — bridge network with `internal: true`, blocking uplink to host/internet.

Backend services are attached only to `backend`. The Gateway, Keycloak and the payment mock are attached to both networks: Gateway needs ingress from the host (`edge`) and access to backend services and Keycloak introspection (`backend`); Keycloak and the payment mock need to be reached both from the browser (`edge`) and from services on `backend`.

### Production (Kubernetes, future)

Each backend service runs in a namespace with a `default-deny` ingress `NetworkPolicy`. Allow rules permit ingress only from pods labelled `app=gateway-api` (and `app=communication-api` for the SMTP path, if exposed). No backend service may be exposed via a `Service` of type `LoadBalancer` or `NodePort`. A service mesh is not required.

## Consequences

- Backend services cannot be debugged directly from the host. Use `docker compose exec`, port-forward through the Gateway, or the Aspire dashboard.
- Any new deployment target must reproduce the isolation with the platform's equivalent mechanism.
- Backend services on the same `backend` network remain reachable from each other over TCP. The constraint that prevents HTTP cross-context communication remains ADR-0010 (architecture tests), not the network layer.
- Follow-up not done in this change: move the host bindings of `postgres` and `rabbitmq` from `0.0.0.0` to `127.0.0.1` for consistency with the other infrastructure services.

## References

- ADR-0010 No synchronous HTTP between bounded contexts
- ADR-0011 Gateway authentication with Keycloak introspection and capabilities
- R-009 Backend services must not publish host ports
