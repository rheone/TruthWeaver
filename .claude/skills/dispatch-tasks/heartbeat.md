# Batch heartbeat

Background bundles run detached, so between dispatch and the next completion notification the session sits idle, and an idle session is where a scheduled prompt can land. Each tick costs a full model turn, so the plan suggests a heartbeat only when the silence would be long: a background bundle of weight 6 or more, or bundles queued behind the cap. A short run skips it. A foreground bundle has none, since a blocking call never returns control to the schedule. The plan states the suggestion, and the user can accept, decline or change the interval.

Default interval: 10 minutes.

`CronCreate` and `CronDelete` are deferred tools. Load their schemas with `ToolSearch` (`select:CronCreate,CronDelete`) before the first call.

At dispatch, once the plan is approved:

```js
CronCreate({
  cron: "*/10 * * * *",
  recurring: true,
  prompt: "dispatch-tasks heartbeat: check the running checklist for this batch; for every task still unchecked, post 'still working on: <task>' to the thread."
})
```

Keep the returned job id with the run's state. On each tick, re-check the checklist and ping whatever is still open.

When the last task resolves, call `CronDelete` on that id as part of step 8, even if the job never fired. The job self-expires after 7 days.

`ScheduleWakeup` belongs to `/loop`'s pacing, and `Monitor` watches shell output; neither fits a background agent.
