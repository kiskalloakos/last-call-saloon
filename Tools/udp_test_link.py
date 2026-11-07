"""Local-only UDP impairment relay for testing the unmodified game transport."""
import heapq
import random
import select
import socket
import threading
import time


class ImpairedLink:
    def __init__(self, client_port, host_port, latency=.08, jitter=.025, loss=.02):
        self.front = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.back = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        try:
            self.front.bind(('127.0.0.1', client_port))
            self.back.bind(('127.0.0.1', 0))
        except OSError:
            self.front.close()
            self.back.close()
            raise
        self.host = ('127.0.0.1', host_port)
        self.client = None
        self.latency, self.jitter, self.loss = latency, jitter, loss
        self.random = random.Random(12345)
        self.stopping = threading.Event()
        self.blackout_until = 0
        self.forwarded = self.dropped = 0
        self.error = None
        self.thread = threading.Thread(target=self._run, daemon=True)
        self.thread.start()

    def blackout(self, seconds):
        self.blackout_until = time.monotonic() + seconds

    def _run(self):
        pending = []
        sequence = 0
        try:
            while not self.stopping.is_set():
                ready, _, _ = select.select([self.front, self.back], [], [], .005)
                for source in ready:
                    try:
                        payload, address = source.recvfrom(65535)
                    except ConnectionResetError:
                        # Windows reports asynchronous ICMP port-unreachable here when
                        # a test player has not started listening yet or just quit.
                        continue
                    if source is self.front:
                        self.client = address
                        target, destination = self.back, self.host
                    else:
                        if not self.client:
                            continue
                        target, destination = self.front, self.client
                    now = time.monotonic()
                    if now < self.blackout_until or self.random.random() < self.loss:
                        self.dropped += 1
                        continue
                    sequence += 1
                    due = now + max(0, self.latency + self.random.uniform(-self.jitter, self.jitter))
                    heapq.heappush(pending, (due, sequence, target, destination, payload))
                now = time.monotonic()
                while pending and pending[0][0] <= now:
                    _, _, target, destination, payload = heapq.heappop(pending)
                    if now < self.blackout_until:
                        self.dropped += 1
                    else:
                        target.sendto(payload, destination)
                        self.forwarded += 1
        except OSError as e:
            if not self.stopping.is_set():
                self.error = str(e)

    def close(self):
        self.stopping.set()
        self.thread.join(timeout=2)
        self.front.close()
        self.back.close()
