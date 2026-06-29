type QueueState = {
  items: Map<string, { id: string }>;
  flush: (ids: string[], options?: { keepalive?: boolean }) => Promise<unknown>;
  onRestore?: (items: { id: string }[]) => void;
  timer: number | null;
  inFlight: Promise<void> | null;
  keepalive: boolean;
};

const queues = new Map<string, QueueState>();
const idleWaiters = new Map<string, Array<() => void>>();

function getOrCreateQueue<T extends { id: string }>(
  key: string,
  flush: (ids: string[], options?: { keepalive?: boolean }) => Promise<unknown>,
  onRestore?: (items: T[]) => void,
): QueueState {
  let queue = queues.get(key);
  if (!queue) {
    queue = {
      items: new Map(),
      flush,
      onRestore: onRestore as QueueState["onRestore"],
      timer: null,
      inFlight: null,
      keepalive: false,
    };
    queues.set(key, queue);
  } else {
    queue.flush = flush;
    queue.onRestore = onRestore as QueueState["onRestore"];
  }
  return queue;
}

function notifyIdle(key: string) {
  const queue = queues.get(key);
  if (queue?.inFlight || (queue && queue.items.size > 0)) {
    return;
  }

  const waiters = idleWaiters.get(key);
  if (!waiters) {
    return;
  }

  idleWaiters.delete(key);
  for (const resolve of waiters) {
    resolve();
  }
}

function waitForIdle(key: string): Promise<void> {
  const queue = queues.get(key);
  if (!queue?.inFlight && (!queue || queue.items.size === 0)) {
    return Promise.resolve();
  }

  return new Promise((resolve) => {
    const waiters = idleWaiters.get(key) ?? [];
    waiters.push(resolve);
    idleWaiters.set(key, waiters);
  });
}

async function flushQueue(key: string, options?: { keepalive?: boolean }) {
  const queue = queues.get(key);
  if (!queue || queue.inFlight) {
    return;
  }

  if (options?.keepalive) {
    queue.keepalive = true;
  }

  if (queue.timer !== null) {
    window.clearTimeout(queue.timer);
    queue.timer = null;
  }

  const items = [...queue.items.values()];
  queue.items.clear();

  if (items.length === 0) {
    notifyIdle(key);
    return;
  }

  const ids = items.map((item) => item.id);
  const useKeepalive = queue.keepalive || options?.keepalive === true;

  queue.inFlight = (async () => {
    try {
      await queue.flush(ids, useKeepalive ? { keepalive: true } : undefined);
    } catch {
      for (const item of items) {
        queue.items.set(item.id, item);
      }
      queue.onRestore?.(items);
    } finally {
      queue.inFlight = null;
      notifyIdle(key);

      if (queue.items.size > 0) {
        await flushQueue(key, { keepalive: queue.keepalive });
      } else {
        queue.keepalive = false;
        queues.delete(key);
        notifyIdle(key);
      }
    }
  })();

  await queue.inFlight;
}

function scheduleFlush(key: string) {
  const queue = queues.get(key);
  if (!queue || queue.inFlight) {
    return;
  }

  if (queue.timer !== null) {
    window.clearTimeout(queue.timer);
  }

  queue.timer = window.setTimeout(() => {
    queue.timer = null;
    void flushQueue(key);
  }, 32);
}

export function enqueueDelete<T extends { id: string }>(
  key: string,
  item: T,
  flush: (ids: string[], options?: { keepalive?: boolean }) => Promise<unknown>,
  onRestore?: (items: T[]) => void,
) {
  const queue = getOrCreateQueue(key, flush, onRestore);
  queue.items.set(item.id, item);
  scheduleFlush(key);
}

export function hasPendingDeletes(key: string): boolean {
  const queue = queues.get(key);
  if (!queue) {
    return false;
  }

  return queue.inFlight !== null || queue.items.size > 0 || queue.timer !== null;
}

export async function flushDeletesNow(key: string, options?: { keepalive?: boolean }) {
  let queue = queues.get(key);
  if (!queue) {
    return;
  }

  if (options?.keepalive) {
    queue.keepalive = true;
  }

  if (queue.timer !== null) {
    window.clearTimeout(queue.timer);
    queue.timer = null;
  }

  if (queue.inFlight) {
    await queue.inFlight;
  }

  while ((queue = queues.get(key)) && queue.items.size > 0) {
    await flushQueue(key, options);
    queue = queues.get(key);
    if (queue?.inFlight) {
      await queue.inFlight;
    }
  }

  await waitForIdle(key);
}

export function clearDeleteQueue(key: string) {
  const queue = queues.get(key);
  if (!queue) {
    return;
  }

  if (queue.timer !== null) {
    window.clearTimeout(queue.timer);
  }

  queues.delete(key);
  notifyIdle(key);
}

if (typeof window !== "undefined") {
  window.addEventListener("pagehide", () => {
    for (const key of [...queues.keys()]) {
      void flushDeletesNow(key, { keepalive: true });
    }
  });
}
