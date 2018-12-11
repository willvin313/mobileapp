Background Sync
===============

As of now, we don't support real-time background sync using push notifications. Instead, we perform full sync at regular intervals using the `RunBackgroundSyncInteractor`. Further down the line we might simplify this sync in order to make it less taxing on the system.

In order to preserve battery life we only schedule syncs when the user is logged in. We ensure that by using `IBackgroundSyncService` and `BaseBackgroundSyncService`. This singleton subscribes to login and logout events emitted by the `LoginManager` and enables and disables syncing accordingly.

---

Previous topic: [Syncing tests](tests.md)
Next topic: [Background Fetch on iOS](bg-fetch-ios.md)
Go to the [Index](index.md)
