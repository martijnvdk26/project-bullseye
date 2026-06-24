from .base import Dart, IDartStrategy, ThrowResult


class NaiveStrategy(IDartStrategy):
    """The simplest possible bot: always throws at triple 20 regardless of
    the remaining score, so it busts often whenever T20 doesn't fit."""

    def decide_turn(self, remaining_score: int, variant: str) -> ThrowResult:
        darts = [Dart(segment="T20", multiplier=3, points=60) for _ in range(3)]
        return ThrowResult(total_points=sum(d.points for d in darts), darts=darts)
