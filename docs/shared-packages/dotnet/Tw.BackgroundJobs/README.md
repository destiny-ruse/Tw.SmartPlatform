# Tw.BackgroundJobs

`Tw.BackgroundJobs` executes scheduled work through `ISender` and records audit, trace, and metric events.

## Usage

```csharp
await pipeline.ExecuteAsync(new BackgroundJobCommand(request, context), cancellationToken);
```
