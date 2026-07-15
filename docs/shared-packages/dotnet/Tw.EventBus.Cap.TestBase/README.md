# Tw.EventBus.Cap.TestBase

`Tw.EventBus.Cap.TestBase` provides CAP and RabbitMQ test fixtures and assertions for tests only.

## Stability

The package is currently `experimental`. Promotion to `stable` requires container-backed tests for fixture lifecycle and failure cleanup, plus deterministic Outbox/Inbox assertions covering publish, retry, duplicate delivery, and timeout paths.
