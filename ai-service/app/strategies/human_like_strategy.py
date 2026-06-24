import random

from .base import Dart, IDartStrategy, ThrowResult

# Real dartboard layout, clockwise, used to pick a plausible "miss"
# neighbour when a simulated dart misses its intended number entirely.
DARTBOARD_NUMBERS = [20, 1, 18, 4, 13, 6, 10, 15, 2, 17, 3, 19, 7, 16, 8, 11, 14, 9, 12, 5]

SEGMENT_PREFIX = {1: "S", 2: "D", 3: "T"}


def _neighbors(number: int) -> tuple[int, int]:
    idx = DARTBOARD_NUMBERS.index(number)
    size = len(DARTBOARD_NUMBERS)
    return DARTBOARD_NUMBERS[(idx - 1) % size], DARTBOARD_NUMBERS[(idx + 1) % size]


class HumanLikeStrategy(IDartStrategy):
    """Aims at a checkout-aware target per dart, then perturbs the actual
    landing spot with a normal distribution to simulate human inaccuracy -
    small deviations downgrade the multiplier (the most common real miss),
    larger ones land on a neighbouring number entirely."""

    def __init__(self, accuracy: float = 0.55, seed: int | None = None):
        # Not clamped to [0, 1] on purpose: a believable "beginner" needs a
        # wider miss spread than accuracy=0 gives, so it goes negative too.
        self._accuracy = accuracy
        self._random = random.Random(seed)

    def decide_turn(self, remaining_score: int, variant: str) -> ThrowResult:
        darts: list[Dart] = []
        remaining = remaining_score

        for _ in range(3):
            if remaining <= 0:
                break

            number, multiplier = self._pick_target(remaining)
            dart = self._throw(number, multiplier)
            darts.append(dart)
            remaining -= dart.points

        return ThrowResult(total_points=sum(d.points for d in darts), darts=darts)

    @staticmethod
    def _pick_target(remaining: int) -> tuple[int, int]:
        if remaining == 50:
            return 25, 2  # bullseye
        if remaining <= 40 and remaining % 2 == 0:
            return remaining // 2, 2  # go for the double that finishes the leg
        if remaining < 60:
            # Safe single to set up a finish next visit - but never leave
            # exactly 1, since that's unfinishable on a double either way.
            target = min(remaining, 20)
            if remaining - target == 1:
                target -= 1
            return max(target, 1), 1
        if remaining - 60 == 1:
            return 19, 3  # T20 would leave exactly 1 here - throw T19 instead
        return 20, 3  # maximize score: triple 20

    def _throw(self, number: int, multiplier: int) -> Dart:
        # Lower accuracy -> wider spread -> deviation crosses the buckets
        # below more often. Real beginners miss the board outright sometimes,
        # so there's a genuine 0-point "miss" bucket, not just a worse hit.
        sigma = 1 - self._accuracy
        deviation = abs(self._random.gauss(0, sigma))

        if deviation > 1.5:
            return Dart(segment="Miss", multiplier=0, points=0)

        actual_number, actual_multiplier = number, multiplier
        if deviation > 0.8 and number != 25:
            actual_number = self._random.choice(_neighbors(number))
            actual_multiplier = 1
        elif deviation > 0.3:
            actual_multiplier = max(1, multiplier - 1)

        return Dart(
            segment=self._segment_label(actual_number, actual_multiplier),
            multiplier=actual_multiplier,
            points=self._points(actual_number, actual_multiplier),
        )

    @staticmethod
    def _points(number: int, multiplier: int) -> int:
        if number == 25:
            return 50 if multiplier == 2 else 25
        return number * multiplier

    @staticmethod
    def _segment_label(number: int, multiplier: int) -> str:
        if number == 25:
            return "DBull" if multiplier == 2 else "Bull"
        return f"{SEGMENT_PREFIX[multiplier]}{number}"
