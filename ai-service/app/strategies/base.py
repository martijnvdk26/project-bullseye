from __future__ import annotations

from abc import ABC, abstractmethod
from dataclasses import dataclass, field


@dataclass
class Dart:
    segment: str
    multiplier: int
    points: int


@dataclass
class ThrowResult:
    total_points: int
    darts: list[Dart] = field(default_factory=list)


class IDartStrategy(ABC):
    """Strategy interface for the Dartbot: given the bot's remaining score
    and the game variant, decide the 3-dart visit it throws this turn."""

    @abstractmethod
    def decide_turn(self, remaining_score: int, variant: str) -> ThrowResult:
        raise NotImplementedError
