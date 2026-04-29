# R-009: Backend services must not publish host ports

- Status: enforced
- Date: 2026-04-29

## Rule

Backend domain services (`catalog-api`, `cart-api`, `order-api`, `payment-api`, `warehouse-api`, `shipping-api`, `communication-api`) must not publish host ports in `docker-compose.yml`, nor be exposed via a Kubernetes `Service` of type `LoadBalancer` or `NodePort`. The Gateway is the only public HTTP ingress.

## Why

Guarantees that the Gateway remains the single ingress point and that ADR-0011 (authentication and capability authorization) cannot be bypassed. Pairs with ADR-0012 (network-level isolation).

## Enforcement

Code review on `docker-compose.yml` and on any deployment manifest (Helm chart, Kustomize, raw manifests).
